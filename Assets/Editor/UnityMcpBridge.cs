// UnityMcpBridge.cs
// -----------------------------------------------------------------------------
// Editor-Bridge fuer den Unity MCP-Server.
//
// Diese Datei gehoert in deinem Unity-Projekt nach:  Assets/Editor/UnityMcpBridge.cs
//
// Sie oeffnet beim Start des Editors einen kleinen TCP-Server auf 127.0.0.1:6400.
// Der Python-MCP-Server verbindet sich dorthin und schickt JSON-Befehle.
// Die Befehle werden auf dem Unity-Main-Thread ausgefuehrt (wichtig, weil die
// Unity-API nur dort aufgerufen werden darf) und das Ergebnis als JSON zurueck-
// geschickt.
//
// Voraussetzung: Newtonsoft.Json. In Unity 6 ueber den Package Manager:
//   Window > Package Manager > "+" > Add package by name > com.unity.nuget.newtonsoft-json
// -----------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class UnityMcpBridge
{
    private const int Port = 6400;

    private static TcpListener _listener;
    private static Thread _listenerThread;
    private static volatile bool _running;

    // Befehle, die vom Netzwerk-Thread kommen und auf dem Main-Thread
    // abgearbeitet werden sollen.
    private static readonly ConcurrentQueue<PendingCommand> _queue =
        new ConcurrentQueue<PendingCommand>();

    static UnityMcpBridge()
    {
        // Wird automatisch beim Laden der Editor-Scripts aufgerufen.
        Start();
    }

    [MenuItem("Tools/MCP Bridge/Start Server")]
    public static void Start()
    {
        if (_running)
        {
            Debug.Log("[UnityMCP] Server laeuft bereits.");
            return;
        }

        try
        {
            _running = true;
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();

            _listenerThread = new Thread(ListenLoop) { IsBackground = true };
            _listenerThread.Start();

            // Main-Thread-Verarbeitung an den Editor-Loop haengen.
            EditorApplication.update -= ProcessQueue;
            EditorApplication.update += ProcessQueue;

            Debug.Log($"[UnityMCP] Server gestartet auf 127.0.0.1:{Port}");
        }
        catch (Exception e)
        {
            _running = false;
            Debug.LogError($"[UnityMCP] Start fehlgeschlagen: {e.Message}");
        }
    }

    [MenuItem("Tools/MCP Bridge/Stop Server")]
    public static void Stop()
    {
        _running = false;
        try { _listener?.Stop(); } catch { /* ignore */ }
        EditorApplication.update -= ProcessQueue;
        Debug.Log("[UnityMCP] Server gestoppt.");
    }

    // ---------------------------------------------------------------------
    // Netzwerk: laeuft auf einem Hintergrund-Thread.
    // ---------------------------------------------------------------------
    private static void ListenLoop()
    {
        while (_running)
        {
            try
            {
                using (TcpClient client = _listener.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                {
                    HandleClient(stream);
                }
            }
            catch (SocketException)
            {
                // Listener wurde gestoppt -> Schleife verlassen.
                break;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityMCP] Verbindungsfehler: {e.Message}");
            }
        }
    }

    private static void HandleClient(NetworkStream stream)
    {
        var buffer = new byte[8192];
        var sb = new StringBuilder();

        // Wir lesen so lange, bis sich der angesammelte Text als JSON parsen laesst.
        while (_running)
        {
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0) return; // Verbindung geschlossen

            sb.Append(Encoding.UTF8.GetString(buffer, 0, read));

            JObject request = TryParse(sb.ToString());
            if (request == null) continue; // noch nicht vollstaendig

            // Befehl in die Warteschlange legen und auf das Ergebnis warten.
            var pending = new PendingCommand { Request = request };
            _queue.Enqueue(pending);
            pending.Done.WaitOne(); // blockiert bis Main-Thread fertig ist

            byte[] response = Encoding.UTF8.GetBytes(pending.Response);
            stream.Write(response, 0, response.Length);
            return; // eine Anfrage pro Verbindung
        }
    }

    private static JObject TryParse(string text)
    {
        try { return JObject.Parse(text); }
        catch { return null; }
    }

    // ---------------------------------------------------------------------
    // Main-Thread: Befehle abarbeiten.
    // ---------------------------------------------------------------------
    private static void ProcessQueue()
    {
        while (_queue.TryDequeue(out PendingCommand cmd))
        {
            string result;
            try
            {
                result = Execute(cmd.Request);
            }
            catch (Exception e)
            {
                result = JsonConvert.SerializeObject(new
                {
                    status = "error",
                    message = e.Message
                });
            }

            cmd.Response = result;
            cmd.Done.Set();
        }
    }

    private static string Execute(JObject request)
    {
        string type = (string)request["type"] ?? "";
        JObject p = (JObject)request["params"] ?? new JObject();

        object result;
        switch (type)
        {
            case "ping":
                result = new { message = "pong" };
                break;

            case "get_scene_info":
                result = GetSceneInfo();
                break;

            case "get_object_info":
                result = GetObjectInfo((string)p["name"]);
                break;

            case "create_object":
                result = CreateObject(p);
                break;

            case "modify_object":
                result = ModifyObject(p);
                break;

            case "delete_object":
                result = DeleteObject((string)p["name"]);
                break;

            case "add_component":
                result = AddComponent((string)p["name"], (string)p["component"]);
                break;

            case "execute_menu_item":
                result = ExecuteMenuItem((string)p["menu_path"]);
                break;

            case "import_asset":
                result = ImportAsset((string)p["source_path"], (string)p["target_path"]);
                break;

            default:
                return JsonConvert.SerializeObject(new
                {
                    status = "error",
                    message = $"Unbekannter Befehl: {type}"
                });
        }

        return JsonConvert.SerializeObject(new { status = "success", result });
    }

    // ---------------------------------------------------------------------
    // Befehls-Implementierungen
    // ---------------------------------------------------------------------
    private static object GetSceneInfo()
    {
        Scene scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var objs = new List<object>();
        foreach (var go in roots)
        {
            objs.Add(new
            {
                name = go.name,
                active = go.activeSelf,
                children = go.transform.childCount,
                position = ToArray(go.transform.position)
            });
        }

        return new
        {
            scene_name = scene.name,
            scene_path = scene.path,
            object_count = roots.Length,
            objects = objs
        };
    }

    private static object GetObjectInfo(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) throw new Exception($"Objekt '{name}' nicht gefunden.");

        var components = new List<string>();
        foreach (var c in go.GetComponents<Component>())
            components.Add(c.GetType().Name);

        return new
        {
            name = go.name,
            active = go.activeSelf,
            position = ToArray(go.transform.position),
            rotation = ToArray(go.transform.eulerAngles),
            scale = ToArray(go.transform.localScale),
            components,
            parent = go.transform.parent ? go.transform.parent.name : null
        };
    }

    private static object CreateObject(JObject p)
    {
        string kind = ((string)p["type"] ?? "cube").ToLower();
        string name = (string)p["name"];

        GameObject go;
        switch (kind)
        {
            case "cube":     go = GameObject.CreatePrimitive(PrimitiveType.Cube); break;
            case "sphere":   go = GameObject.CreatePrimitive(PrimitiveType.Sphere); break;
            case "cylinder": go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); break;
            case "capsule":  go = GameObject.CreatePrimitive(PrimitiveType.Capsule); break;
            case "plane":    go = GameObject.CreatePrimitive(PrimitiveType.Plane); break;
            case "quad":     go = GameObject.CreatePrimitive(PrimitiveType.Quad); break;
            case "empty":    go = new GameObject(); break;
            case "light":
                go = new GameObject();
                go.AddComponent<Light>();
                break;
            case "camera":
                go = new GameObject();
                go.AddComponent<Camera>();
                break;
            default:
                throw new Exception($"Unbekannter Objekttyp: {kind}");
        }

        if (!string.IsNullOrEmpty(name)) go.name = name;
        if (p["position"] != null) go.transform.position = ToVector3(p["position"]);
        if (p["rotation"] != null) go.transform.eulerAngles = ToVector3(p["rotation"]);
        if (p["scale"] != null) go.transform.localScale = ToVector3(p["scale"]);

        Undo.RegisterCreatedObjectUndo(go, "MCP Create " + go.name);
        return new { name = go.name, created = true };
    }

    private static object ModifyObject(JObject p)
    {
        string name = (string)p["name"];
        GameObject go = GameObject.Find(name);
        if (go == null) throw new Exception($"Objekt '{name}' nicht gefunden.");

        Undo.RecordObject(go.transform, "MCP Modify " + name);

        if (p["position"] != null) go.transform.position = ToVector3(p["position"]);
        if (p["rotation"] != null) go.transform.eulerAngles = ToVector3(p["rotation"]);
        if (p["scale"] != null) go.transform.localScale = ToVector3(p["scale"]);
        if (p["new_name"] != null) go.name = (string)p["new_name"];
        if (p["active"] != null) go.SetActive((bool)p["active"]);

        return new { name = go.name, modified = true };
    }

    private static object DeleteObject(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) throw new Exception($"Objekt '{name}' nicht gefunden.");
        Undo.DestroyObjectImmediate(go);
        return new { name, deleted = true };
    }

    private static object AddComponent(string name, string component)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) throw new Exception($"Objekt '{name}' nicht gefunden.");

        Type t = FindType(component);
        if (t == null) throw new Exception($"Komponente '{component}' nicht gefunden.");

        Undo.AddComponent(go, t);
        return new { name, component, added = true };
    }

    private static object ExecuteMenuItem(string menuPath)
    {
        bool ok = EditorApplication.ExecuteMenuItem(menuPath);
        return new { menu_path = menuPath, executed = ok };
    }

    private static object ImportAsset(string sourcePath, string targetPath)
    {
        // sourcePath: absoluter Pfad auf der Festplatte (z.B. eine .fbx aus Blender)
        // targetPath: Pfad innerhalb des Projekts, z.B. "Assets/Models/figur.fbx"
        if (!System.IO.File.Exists(sourcePath))
            throw new Exception($"Quelldatei nicht gefunden: {sourcePath}");

        string fullTarget = System.IO.Path.Combine(
            System.IO.Directory.GetParent(Application.dataPath).FullName, targetPath);

        string dir = System.IO.Path.GetDirectoryName(fullTarget);
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

        System.IO.File.Copy(sourcePath, fullTarget, true);
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(targetPath);

        return new { source = sourcePath, target = targetPath, imported = true };
    }

    // ---------------------------------------------------------------------
    // Hilfsfunktionen
    // ---------------------------------------------------------------------
    private static Vector3 ToVector3(JToken token)
    {
        var a = (JArray)token;
        return new Vector3((float)a[0], (float)a[1], (float)a[2]);
    }

    private static float[] ToArray(Vector3 v) => new[] { v.x, v.y, v.z };

    private static Type FindType(string name)
    {
        // Erst direkt, dann ueber alle geladenen Assemblies suchen.
        Type t = Type.GetType(name) ?? Type.GetType("UnityEngine." + name + ", UnityEngine");
        if (t != null) return t;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(name) ?? asm.GetType("UnityEngine." + name);
            if (t != null) return t;
        }
        return null;
    }

    // Hilfsobjekt fuer die Warteschlange.
    private class PendingCommand
    {
        public JObject Request;
        public string Response;
        public readonly ManualResetEvent Done = new ManualResetEvent(false);
    }
}
