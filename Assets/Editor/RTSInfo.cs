
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RTSInfo
{
    public const string OutDir = @"C:\Users\blind\AppData\Roaming\Claude\local-agent-mode-sessions\1be9def4-ae4b-4634-bfeb-f0688cac2fc1\c3728a67-9427-425c-b84a-5cf67ce6eda9\local_2b6c8d99-3950-422c-ba0f-71ef387e580d\outputs";

    [MenuItem("RTS/Dump Info")]
    public static void DumpInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("unityVersion=" + Application.unityVersion);
        sb.AppendLine("dataPath=" + Application.dataPath);
        var rp = GraphicsSettings.defaultRenderPipeline;
        sb.AppendLine("renderPipeline=" + (rp != null ? rp.GetType().FullName : "BuiltIn"));
        sb.AppendLine("navMeshSurface=" + (Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation") != null));
        sb.AppendLine("tmp=" + (Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null));
#if ENABLE_LEGACY_INPUT_MANAGER
        sb.AppendLine("legacyInput=True");
#else
        sb.AppendLine("legacyInput=False");
#endif
#if ENABLE_INPUT_SYSTEM
        sb.AppendLine("newInputSystem=True");
#else
        sb.AppendLine("newInputSystem=False");
#endif
        sb.AppendLine("urpLitFound=" + (Shader.Find("Universal Render Pipeline/Lit") != null));
        sb.AppendLine("particleUnlit=" + (Shader.Find("Universal Render Pipeline/Particles/Unlit") != null));
        Directory.CreateDirectory(OutDir);
        File.WriteAllText(Path.Combine(OutDir, "unity_info.txt"), sb.ToString());
        Debug.Log("RTS info dumped.");
    }

    [MenuItem("RTS/Screenshot SceneView")]
    public static void ScreenshotScene()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) { Debug.LogWarning("No scene view"); return; }
        var cam = sv.camera;
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(OutDir, "unity_sceneview.png"), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        Debug.Log("Scene screenshot saved.");
    }
}
