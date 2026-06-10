using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using RTS;

public static class RTSSetup
{
    const string DataDir = "Assets/RTS/Resources/Data";
    const string PrefabDir = "Assets/RTS/Resources/Prefabs";
    const string MatDir = "Assets/RTS/Materials";
    const float MODEL_YAW = 0f;

    [MenuItem("RTS/Setup Scene")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("RTS Setup: Bitte zuerst den Play-Modus beenden!");
            return;
        }
        EnsureTagsAndLayers();
        EnsureFolders();
        CreateData();
        CreatePrefabs();
        BuildWorld();
        BakeNavMesh();
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("RTS Setup fertig!");
    }

    // ---------- Tags & Layer ----------
    static void EnsureTagsAndLayers()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tm.FindProperty("tags");
        foreach (var t in new[] { "Player", "Enemy", "Resource", "Building" })
        {
            bool found = false;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == t) { found = true; break; }
            if (!found)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = t;
            }
        }
        var layers = tm.FindProperty("layers");
        SetLayer(layers, 6, "Ground");
        SetLayer(layers, 7, "Units");
        SetLayer(layers, 8, "Buildings");
        SetLayer(layers, 9, "Minimap");
        tm.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    static void SetLayer(SerializedProperty layers, int idx, string name)
    {
        var el = layers.GetArrayElementAtIndex(idx);
        if (string.IsNullOrEmpty(el.stringValue)) el.stringValue = name;
    }

    static void EnsureFolders()
    {
        foreach (var d in new[] { "Assets/RTS", "Assets/RTS/Resources", DataDir, PrefabDir, MatDir })
            if (!AssetDatabase.IsValidFolder(d))
            {
                var parent = d.Substring(0, d.LastIndexOf('/'));
                var leaf = d.Substring(d.LastIndexOf('/') + 1);
                AssetDatabase.CreateFolder(parent, leaf);
            }
    }

    // ---------- Daten ----------
    static void CreateData()
    {
        U("Infantry", "Infanterist", "Unit_Infantry", 100, 5f, 80, 5f, 8f, 7f, 1f, 11f, 22f, false, Factory.Barracks, 0.35f, 2f);
        U("RocketSquad", "Raketentrupp", "Unit_RocketSquad", 300, 7f, 70, 4.5f, 30f, 10f, 2.2f, 13f, 16f, false, Factory.Barracks, 0.35f, 2f);
        U("Harvester", "Sammler", "Unit_Harvester", 1400, 12f, 600, 4.2f, 0f, 0f, 1f, 0f, 0f, true, Factory.WarFactory, 0.95f, 2.6f);
        U("TankLight", "Leichter Panzer", "Unit_TankLight", 700, 9f, 300, 6f, 22f, 9f, 1.4f, 12f, 30f, false, Factory.WarFactory, 0.8f, 2f);
        U("TankHeavy", "Schwerer Panzer", "Unit_TankHeavy", 1500, 14f, 700, 3.6f, 55f, 10f, 2.4f, 13f, 30f, false, Factory.WarFactory, 0.95f, 2.4f);
        U("V2Launcher", "V2-Werfer", "Unit_V2Launcher", 900, 11f, 220, 4.5f, 90f, 18f, 6f, 16f, 11f, false, Factory.WarFactory, 0.8f, 2.4f);
        U("MCV", "MBF (Bauhof)", "Unit_MCV", 2500, 18f, 800, 3.5f, 0f, 0f, 1f, 0f, 0f, false, Factory.WarFactory, 1.0f, 2.8f);

        B("ConYard", "Bauhof", "Bldg_ConstructionYard", 0, 0f, 1500, 25, new Vector2(5.5f, 5.5f), false, false, 5f);
        B("PowerPlant", "Kraftwerk", "Bldg_PowerPlant", 300, 8f, 400, 100, new Vector2(4.5f, 4.5f), true, false, 5.5f);
        B("Refinery", "Raffinerie", "Bldg_Refinery", 1500, 12f, 600, -30, new Vector2(5.5f, 4.5f), true, false, 5f);
        B("Barracks", "Kaserne", "Bldg_Barracks", 400, 8f, 500, -20, new Vector2(4.5f, 3.5f), true, false, 4f);
        B("WarFactory", "Waffenfabrik", "Bldg_WarFactory", 1000, 12f, 800, -30, new Vector2(5.5f, 4.5f), true, false, 4.5f);
        B("DefenseTurret", "Geschuetzturm", "Bldg_DefenseTurret", 600, 8f, 400, -20, new Vector2(2.5f, 2.5f), true, true, 3.5f);
        B("HeavyTurret", "Kanonenturm", "Bldg_HeavyTurret", 1200, 10f, 650, -30, new Vector2(2.8f, 2.8f), true, true, 4f);
        B("Radar", "Radar", "Bldg_Radar", 1000, 10f, 600, -40, new Vector2(4.5f, 4.5f), true, false, 5f);
        B("Wall", "Mauer", "Bldg_Wall", 50, 1.5f, 300, 0, new Vector2(2f, 1f), true, false, 2.2f);
        AssetDatabase.SaveAssets();
    }

    static void U(string id, string name, string model, int cost, float bt, int hp, float spd,
        float dmg, float rng, float rate, float aggro, float proj, bool harv, Factory fac, float rad, float hb)
    {
        string path = DataDir + "/" + id + ".asset";
        AssetDatabase.DeleteAsset(path);
        var so = ScriptableObject.CreateInstance<UnitData>();
        so.id = id; so.displayName = name; so.modelFile = model;
        so.cost = cost; so.buildTime = bt; so.maxHp = hp; so.speed = spd;
        so.damage = dmg; so.attackRange = rng; so.attackRate = rate; so.aggroRange = aggro;
        so.projectileSpeed = proj; so.isHarvester = harv; so.builtAt = fac;
        so.agentRadius = rad; so.hbHeight = hb;
        AssetDatabase.CreateAsset(so, path);
    }

    static void B(string id, string name, string model, int cost, float bt, int hp, int energy,
        Vector2 fp, bool buildable, bool defense, float hb)
    {
        string path = DataDir + "/" + id + ".asset";
        AssetDatabase.DeleteAsset(path);
        var so = ScriptableObject.CreateInstance<BuildingData>();
        so.id = id; so.displayName = name; so.modelFile = model;
        so.cost = cost; so.buildTime = bt; so.maxHp = hp; so.energyDelta = energy;
        so.footprint = fp; so.buildable = buildable; so.isDefense = defense; so.hbHeight = hb;
        if (defense) { so.damage = 20f; so.attackRange = 14f; so.attackRate = 0.7f; }
        AssetDatabase.CreateAsset(so, path);
    }

    // ---------- Prefabs ----------
    static void CreatePrefabs()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:RTS.UnitData", new[] { DataDir }))
            CreateUnitPrefab(AssetDatabase.LoadAssetAtPath<UnitData>(AssetDatabase.GUIDToAssetPath(guid)));
        foreach (var guid in AssetDatabase.FindAssets("t:RTS.BuildingData", new[] { DataDir }))
            CreateBuildingPrefab(AssetDatabase.LoadAssetAtPath<BuildingData>(AssetDatabase.GUIDToAssetPath(guid)));
        AssetDatabase.SaveAssets();
    }

    static GameObject ModelInstance(string modelFile, Transform parent)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/" + modelFile + ".fbx");
        if (fbx == null) { Debug.LogError("FBX fehlt: " + modelFile); return null; }
        var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        model.name = "Model";
        model.transform.SetParent(parent, false);
        model.transform.localRotation = Quaternion.Euler(0f, MODEL_YAW, 0f);
        return model;
    }

    static void CreateUnitPrefab(UnitData d)
    {
        var root = new GameObject(d.id);
        ModelInstance(d.modelFile, root.transform);
        float h = Mathf.Max(1.2f, d.hbHeight - 0.4f);
        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, h * 0.5f, 0f);
        col.height = h;
        col.radius = Mathf.Max(0.45f, d.agentRadius);
        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = d.agentRadius;
        agent.speed = d.speed;
        Unit u = d.isHarvester ? root.AddComponent<Harvester>() : root.AddComponent<Unit>();
        u.data = d;
        AddRing(root, Mathf.Max(1.8f, d.agentRadius * 4f));
        AddBlip(root, 3f);
        Save(root, PrefabDir + "/" + d.id + ".prefab");
    }

    static void CreateBuildingPrefab(BuildingData d)
    {
        var root = new GameObject(d.id);
        ModelInstance(d.modelFile, root.transform);
        float h = 3f;
        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, h * 0.5f, 0f);
        col.size = new Vector3(d.footprint.x, h, d.footprint.y);
        var obs = root.AddComponent<NavMeshObstacle>();
        obs.shape = NavMeshObstacleShape.Box;
        obs.center = col.center;
        obs.size = col.size;
        obs.carving = true;
        Building b;
        if (d.isDefense) b = root.AddComponent<DefenseTurret>();
        else if (d.id == "Barracks" || d.id == "WarFactory") b = root.AddComponent<ProductionBuilding>();
        else b = root.AddComponent<Building>();
        b.data = d;
        AddRing(root, Mathf.Max(d.footprint.x, d.footprint.y) * 1.5f);
        AddBlip(root, 5f);
        Save(root, PrefabDir + "/" + d.id + ".prefab");
    }

    static void AddRing(GameObject root, float scale)
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        ring.name = "SelectionRing";
        ring.transform.SetParent(root.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = new Vector3(scale, scale, 1f);
        ring.GetComponent<Renderer>().sharedMaterial = RingMat();
        ring.SetActive(false);
    }

    static void AddBlip(GameObject root, float scale)
    {
        var blip = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(blip.GetComponent<Collider>());
        blip.name = "Blip";
        blip.transform.SetParent(root.transform, false);
        blip.transform.localPosition = new Vector3(0f, 14f, 0f);
        blip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        blip.transform.localScale = new Vector3(scale, scale, 1f);
        blip.layer = LayerMask.NameToLayer("Minimap");
        blip.AddComponent<MinimapBlip>();
    }

    static void Save(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // ---------- Material-Assets ----------
    static Material RingMat()
    {
        string p = MatDir + "/SelectionRing.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetTexture("_BaseMap", RingTex());
        m.color = new Color(0.25f, 1f, 0.45f, 0.85f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", 5);
        m.SetInt("_DstBlend", 10);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    static Texture2D RingTex()
    {
        string p = MatDir + "/RingTex.asset";
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (t != null) return t;
        t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = (r > 0.68f && r < 0.95f) ? 1f : 0f;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        AssetDatabase.CreateAsset(t, p);
        return t;
    }

    static Material SandMat()
    {
        string p = MatDir + "/Sand.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", SandTex());
        m.SetTextureScale("_BaseMap", new Vector2(12f, 12f));
        m.SetFloat("_Smoothness", 0.02f);
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    static Material WaterMat()
    {
        string p = MatDir + "/Water.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", new Color(0.16f, 0.42f, 0.62f));
        m.SetFloat("_Smoothness", 0.9f);
        m.SetFloat("_Metallic", 0.1f);
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    static Material SkirtMat()
    {
        string p = MatDir + "/SandSkirt.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", SandTex());
        m.SetTextureScale("_BaseMap", new Vector2(60f, 60f));
        m.SetColor("_BaseColor", new Color(0.72f, 0.62f, 0.47f));
        m.SetFloat("_Smoothness", 0.02f);
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    static Texture2D SandTex()
    {
        string p = MatDir + "/SandTex.asset";
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (t != null) return t;
        t = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        var baseC = new Color(0.76f, 0.62f, 0.42f);
        for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.04f, y * 0.04f) * 0.5f
                        + Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.3f;
                float v = 0.86f + n * 0.28f;
                t.SetPixel(x, y, new Color(baseC.r * v, baseC.g * v, baseC.b * v));
            }
        t.Apply();
        AssetDatabase.CreateAsset(t, p);
        return t;
    }

    // ---------- Welt ----------
    static void BuildWorld()
    {
        foreach (var name in new[] { "RTS_World", "RTS_Managers" })
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        var world = new GameObject("RTS_World");

        // Boden
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(world.transform);
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
        ground.layer = LayerMask.NameToLayer("Ground");
        ground.GetComponent<Renderer>().sharedMaterial = SandMat();
        SetNavStatic(ground);

        // Optische Sand-Schuerze rund um die Karte (nicht bespielbar)
        var skirt = GameObject.CreatePrimitive(PrimitiveType.Plane);
        skirt.name = "Skirt";
        skirt.transform.SetParent(world.transform);
        skirt.transform.position = new Vector3(0f, -0.4f, 0f);
        skirt.transform.localScale = new Vector3(90f, 1f, 90f);
        Object.DestroyImmediate(skirt.GetComponent<Collider>());
        skirt.GetComponent<Renderer>().sharedMaterial = SkirtMat();

        // Felsen als natuerliche Barriere (mit Luecken)
        var rocksFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Prop_Rocks.fbx");
        Vector2[] rockPos = {
            new Vector2(-64,54), new Vector2(-50,43), new Vector2(-36,29), new Vector2(-22,18),
            new Vector2(-11,7), new Vector2(14,-14), new Vector2(25,-25), new Vector2(40,-40),
            new Vector2(54,-50), new Vector2(65,-61), new Vector2(-86,-4), new Vector2(90,11),
            new Vector2(-4,52), new Vector2(4,-56)
        };
        var rnd = new System.Random(42);
        foreach (var rp in rockPos)
        {
            var rock = (GameObject)PrefabUtility.InstantiatePrefab(rocksFbx);
            rock.name = "Rocks";
            rock.transform.SetParent(world.transform);
            rock.transform.position = new Vector3(rp.x, 0f, rp.y);
            rock.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            float s = 1.5f + (float)rnd.NextDouble() * 1.1f;
            rock.transform.localScale = new Vector3(s, s, s);
            foreach (var mf in rock.GetComponentsInChildren<MeshFilter>())
            {
                mf.gameObject.AddComponent<MeshCollider>();
                mf.gameObject.layer = LayerMask.NameToLayer("Buildings");
            }
            rock.layer = LayerMask.NameToLayer("Buildings");
            SetNavStatic(rock);
        }

        // Gebirgszuege (grosse Felscluster, blockieren Wege)
        Vector2[] ridgePos = {
            new Vector2(-30,62), new Vector2(-42,68), new Vector2(-78,52),
            new Vector2(34,-64), new Vector2(46,-70), new Vector2(80,-46)
        };
        foreach (var rp in ridgePos)
        {
            var mt = (GameObject)PrefabUtility.InstantiatePrefab(rocksFbx);
            mt.name = "Mountain";
            mt.transform.SetParent(world.transform);
            mt.transform.position = new Vector3(rp.x, 0f, rp.y);
            mt.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            float ms = 3.2f + (float)rnd.NextDouble() * 1.4f;
            mt.transform.localScale = new Vector3(ms, ms * 1.3f, ms);
            foreach (var mf in mt.GetComponentsInChildren<MeshFilter>())
            {
                mf.gameObject.AddComponent<MeshCollider>();
                mf.gameObject.layer = LayerMask.NameToLayer("Buildings");
            }
            SetNavStatic(mt);
        }

        // Deko-Berge ausserhalb der Spielflaeche
        Vector2[] decoPos = {
            new Vector2(-130,40), new Vector2(-120,-90), new Vector2(125,80),
            new Vector2(135,-50), new Vector2(40,128), new Vector2(-60,-128)
        };
        foreach (var dp in decoPos)
        {
            var deco = (GameObject)PrefabUtility.InstantiatePrefab(rocksFbx);
            deco.name = "DecoMountain";
            deco.transform.SetParent(world.transform);
            deco.transform.position = new Vector3(dp.x, -0.3f, dp.y);
            deco.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            float ds = 6f + (float)rnd.NextDouble() * 3f;
            deco.transform.localScale = new Vector3(ds, ds * 1.5f, ds);
        }

        // Fluss quer ueber die Karte (zwei Furten als Engstellen)
        var waterMat = WaterMat();
        for (float x = -98f; x <= 98f; x += 4f)
        {
            if ((x > -62f && x < -46f) || (x > 42f && x < 58f)) continue; // Furten
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "River";
            seg.transform.SetParent(world.transform);
            float zMid = 10f + Mathf.Sin(x * 0.05f) * 6f;
            seg.transform.position = new Vector3(x, 0.03f, zMid);
            seg.transform.localScale = new Vector3(4.3f, 0.12f, 10f);
            seg.GetComponent<Renderer>().sharedMaterial = waterMat;
            seg.layer = LayerMask.NameToLayer("Buildings");
            var bc = seg.GetComponent<BoxCollider>();
            bc.size = new Vector3(1f, 20f, 1f); // unsichtbarer Bau-Blocker
            SetNavStatic(seg);
            SetNotWalkable(seg);
        }

        // Tiberium-Felder
        var tibFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Prop_Tiberium.fbx");
        Vector2[] fields = {
            new Vector2(-42,-70), new Vector2(-70,-42),
            new Vector2(42,70), new Vector2(70,42),
            new Vector2(-26,26), new Vector2(26,-26),
            new Vector2(-72,30), new Vector2(72,-30)
        };
        foreach (var fp in fields)
        {
            var parent = new GameObject("TiberiumField");
            parent.transform.SetParent(world.transform);
            parent.transform.position = new Vector3(fp.x, 0f, fp.y);
            parent.tag = "Resource";
            parent.AddComponent<TiberiumField>();
            for (int i = 0; i < 3; i++)
            {
                var crys = (GameObject)PrefabUtility.InstantiatePrefab(tibFbx);
                crys.transform.SetParent(parent.transform, false);
                float ang = i * 2.1f + fp.x;
                crys.transform.localPosition = new Vector3(Mathf.Cos(ang) * 1.8f, 0f, Mathf.Sin(ang) * 1.8f);
                crys.transform.localRotation = Quaternion.Euler(0f, ang * 57f, 0f);
                crys.transform.localScale = Vector3.one * (1.1f + 0.3f * i);
            }
        }

        // Licht & Atmosphaere
        var lightGo = GameObject.Find("Directional Light");
        if (lightGo == null)
        {
            lightGo = new GameObject("Directional Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;
        }
        var light = lightGo.GetComponent<Light>();
        light.color = new Color(1f, 0.95f, 0.84f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.48f, 0.44f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.79f, 0.71f, 0.56f);
        RenderSettings.fogStartDistance = 130f;
        RenderSettings.fogEndDistance = 340f;

        // Kamera
        var camGo = GameObject.Find("Main Camera");
        if (camGo == null)
        {
            camGo = new GameObject("Main Camera");
            camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }
        var cam = camGo.GetComponent<Camera>();
        cam.farClipPlane = 400f;
        cam.cullingMask = ~(1 << LayerMask.NameToLayer("Minimap"));
        camGo.transform.position = new Vector3(-88f, 30f, -88f);
        camGo.transform.rotation = Quaternion.Euler(50f, 45f, 0f);
        var rtsCam = camGo.GetComponent<RTSCamera>();
        if (rtsCam == null) rtsCam = camGo.AddComponent<RTSCamera>();
        rtsCam.limit = new Vector2(105f, 105f);
        rtsCam.maxY = 72f;

        // Manager
        var mgr = new GameObject("RTS_Managers");
        mgr.AddComponent<GameManager>();
        mgr.AddComponent<ResourceManager>();
        mgr.AddComponent<SelectionManager>();
        mgr.AddComponent<BuildPlacement>();
        mgr.AddComponent<UIManager>();
        var ai = mgr.AddComponent<AIController>();
        ai.basePos = new Vector3(70f, 0f, 70f);
        mgr.AddComponent<GameBootstrap>();
    }

    static void SetNavStatic(GameObject go)
    {
#pragma warning disable 618
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.NavigationStatic);
#pragma warning restore 618
    }

    static void SetNotWalkable(GameObject go)
    {
#pragma warning disable 618
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetNavMeshArea(t.gameObject, 1); // 1 = Not Walkable
#pragma warning restore 618
    }

    static void BakeNavMesh()
    {
#pragma warning disable 618
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore 618
    }
}
