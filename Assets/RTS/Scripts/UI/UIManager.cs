using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RTS
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        Text creditsText, energyText, msgText, gameOverText;
        GameObject gameOverPanel, radarOverlay;
        MinimapClick mmClick;
        float msgUntil;
        Font font;

        class UnitBtn
        {
            public Button btn;
            public Image fill;
            public Text badge;
            public UnitData unit;
        }
        readonly List<UnitBtn> unitButtons = new List<UnitBtn>();
        readonly List<Image> speedButtons = new List<Image>();

        class ProdRow
        {
            public GameObject go;
            public Image fill;
            public Text label;
        }
        readonly List<ProdRow> prodRows = new List<ProdRow>();

        float nextScan;
        ProductionBuilding cachedRax, cachedWf;
        bool hasRadar;

        void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void Start() { BuildUI(); }

        public static bool IsPointerOverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        public static void Message(string s)
        {
            if (Instance == null || Instance.msgText == null) return;
            Instance.msgText.text = s;
            Instance.msgUntil = Time.unscaledTime + 3f;
        }

        // ------------------------------ UI-Aufbau ------------------------------

        void BuildUI()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var cgo = new GameObject("RTS_Canvas");
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            var canvasRT = cgo.GetComponent<RectTransform>();

            // ---- Obere Leiste ----
            var top = Panel(cgo.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -44f), Vector2.zero, new Color(0.07f, 0.08f, 0.1f, 0.92f));
            creditsText = Label(top, "Credits", new Vector2(0f, 0f), new Vector2(0.22f, 1f),
                "Credits: 0", 24, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f));
            creditsText.rectTransform.offsetMin = new Vector2(16f, 0f);
            energyText = Label(top, "Energy", new Vector2(0.22f, 0f), new Vector2(0.52f, 1f),
                "Energie: 0/0", 24, TextAnchor.MiddleLeft, new Color(0.4f, 0.9f, 1f));

            Label(top, "SpeedL", new Vector2(0.52f, 0f), new Vector2(0.595f, 1f),
                "Tempo:", 22, TextAnchor.MiddleRight, new Color(0.8f, 0.8f, 0.85f));
            for (int i = 0; i < 3; i++)
            {
                int mult = i + 1;
                var sgo = new GameObject("Speed" + mult);
                sgo.transform.SetParent(top, false);
                var simg = sgo.AddComponent<Image>();
                simg.color = new Color(0.18f, 0.21f, 0.26f, 1f);
                var sbtn = sgo.AddComponent<Button>();
                sbtn.targetGraphic = simg;
                var srt = sgo.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.605f + i * 0.048f, 0.12f);
                srt.anchorMax = new Vector2(0.645f + i * 0.048f, 0.88f);
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = Vector2.zero;
                Label(srt, "L", Vector2.zero, Vector2.one, mult + "x", 20, TextAnchor.MiddleCenter, Color.white);
                speedButtons.Add(simg);
                sbtn.onClick.AddListener(() => SetSpeed(mult));
            }

            // ---- Sidebar rechts ----
            var side = Panel(cgo.transform, "Sidebar", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-280f, 0f), new Vector2(0f, -44f), new Color(0.09f, 0.1f, 0.13f, 0.94f));
            var layout = side.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            SectionTitle(side, "GEBAEUDE");
            var bGrid = Grid(side);
            foreach (var bd in GameManager.Instance.buildingTypes)
            {
                if (!bd.buildable) continue;
                var data = bd;
                string energy = bd.energyDelta == 0 ? "" :
                    (bd.energyDelta > 0 ? "  +" + bd.energyDelta + "E" : "  " + bd.energyDelta + "E");
                MakeButton(bGrid, bd.displayName, "$" + bd.cost + energy, bd.id,
                    () => BuildPlacement.Instance.StartPlacement(data), out _, out _);
            }

            SectionTitle(side, "EINHEITEN");
            var uGrid = Grid(side);
            foreach (var ud in GameManager.Instance.unitTypes)
            {
                var data = ud;
                var btn = MakeButton(uGrid, ud.displayName, "$" + ud.cost, ud.id,
                    () => TryQueueUnit(data), out var fill, out var badge);
                unitButtons.Add(new UnitBtn { btn = btn, fill = fill, badge = badge, unit = data });
            }

            var hint = Label(side, "Hint", Vector2.zero, Vector2.one,
                "LMB: Auswaehlen | Ziehen: Box\nRMB: Bewegen/Angreifen\nShift: Hinzu | D: MBF entfalten\nF1-F3: Tempo | ESC: Abbrechen",
                14, TextAnchor.MiddleLeft, new Color(0.65f, 0.68f, 0.72f));
            var hintLe = hint.gameObject.AddComponent<LayoutElement>();
            hintLe.minHeight = 76f;

            // ---- Produktions-Panel oben links ----
            for (int i = 0; i < 6; i++)
            {
                var row = new ProdRow();
                var rgo = new GameObject("ProdRow" + i);
                rgo.transform.SetParent(cgo.transform, false);
                var bg = rgo.AddComponent<Image>();
                bg.color = new Color(0.07f, 0.08f, 0.1f, 0.85f);
                bg.raycastTarget = false;
                var rrt = rgo.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0f, 1f);
                rrt.anchorMax = new Vector2(0f, 1f);
                rrt.pivot = new Vector2(0f, 1f);
                rrt.anchoredPosition = new Vector2(10f, -52f - i * 30f);
                rrt.sizeDelta = new Vector2(330f, 26f);
                var fgo = new GameObject("Fill");
                fgo.transform.SetParent(rgo.transform, false);
                row.fill = fgo.AddComponent<Image>();
                row.fill.color = new Color(0.25f, 0.85f, 0.35f, 0.35f);
                row.fill.raycastTarget = false;
                row.fill.type = Image.Type.Filled;
                row.fill.fillMethod = Image.FillMethod.Horizontal;
                var frt = row.fill.rectTransform;
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
                frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
                row.label = Label(rrt, "L", Vector2.zero, Vector2.one, "", 16,
                    TextAnchor.MiddleLeft, Color.white);
                row.label.rectTransform.offsetMin = new Vector2(8f, 0f);
                row.go = rgo;
                rgo.SetActive(false);
                prodRows.Add(row);
            }

            // ---- Minimap unten links ----
            var mmBorder = Panel(cgo.transform, "MinimapBorder", Vector2.zero, Vector2.zero,
                new Vector2(10f, 10f), new Vector2(258f, 258f), new Color(0.07f, 0.08f, 0.1f, 0.95f));
            mmBorder.anchorMin = Vector2.zero; mmBorder.anchorMax = Vector2.zero;
            mmBorder.offsetMin = new Vector2(10f, 10f);
            mmBorder.offsetMax = new Vector2(258f, 258f);
            var mmGo = new GameObject("Minimap");
            mmGo.transform.SetParent(mmBorder, false);
            var raw = mmGo.AddComponent<RawImage>();
            var mmRect = raw.rectTransform;
            mmRect.anchorMin = Vector2.zero; mmRect.anchorMax = Vector2.one;
            mmRect.offsetMin = new Vector2(4f, 4f); mmRect.offsetMax = new Vector2(-4f, -4f);
            raw.texture = CreateMinimapCamera();
            mmClick = mmGo.AddComponent<MinimapClick>();
            mmClick.worldHalf = 102f;

            // Radar-Sperre (ohne Radar-Gebaeude bleibt die Minimap dunkel)
            radarOverlay = Panel(mmBorder, "NoRadar", Vector2.zero, Vector2.one,
                new Vector2(4f, 4f), new Vector2(-4f, -4f), new Color(0.03f, 0.04f, 0.05f, 0.96f)).gameObject;
            Label(radarOverlay.GetComponent<RectTransform>(), "T", Vector2.zero, Vector2.one,
                "KEIN RADAR", 24, TextAnchor.MiddleCenter, new Color(0.6f, 0.65f, 0.7f));

            // ---- Mitteilung & Game Over ----
            msgText = Label(canvasRT, "Msg", new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.88f),
                "", 30, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.7f));
            var outline = msgText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            gameOverPanel = Panel(cgo.transform, "GameOver", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.65f)).gameObject;
            gameOverText = Label(gameOverPanel.GetComponent<RectTransform>(), "GOText",
                Vector2.zero, Vector2.one, "", 90, TextAnchor.MiddleCenter, Color.white);
            gameOverPanel.SetActive(false);

            SetSpeed(1);
        }

        RenderTexture CreateMinimapCamera()
        {
            var mmGo = new GameObject("MinimapCamera");
            var cam = mmGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 102f;
            mmGo.transform.position = new Vector3(0f, 140f, 0f);
            mmGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.1f, 0.07f);
            cam.farClipPlane = 320f;
            var rt = new RenderTexture(256, 256, 16);
            cam.targetTexture = rt;
            return rt;
        }

        // ------------------------------ Bausteine ------------------------------

        RectTransform Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax,
            Vector2 oMin, Vector2 oMax, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
            return rt;
        }

        Text Label(RectTransform parent, string name, Vector2 aMin, Vector2 aMax,
            string text, int size, TextAnchor anchor, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.text = text;
            t.alignment = anchor;
            t.color = c;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return t;
        }

        void SectionTitle(RectTransform parent, string text)
        {
            var t = Label(parent, "Title_" + text, Vector2.zero, Vector2.one, text, 20,
                TextAnchor.MiddleLeft, new Color(0.85f, 0.75f, 0.45f));
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 24f;
        }

        RectTransform Grid(RectTransform parent)
        {
            var go = new GameObject("Grid");
            go.transform.SetParent(parent, false);
            var grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(124f, 76f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.GetComponent<RectTransform>();
        }

        Button MakeButton(RectTransform parent, string title, string sub, string iconId,
            System.Action onClick, out Image fill, out Text badge)
        {
            var go = new GameObject("Btn_" + title);
            go.transform.SetParent(parent, false);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.21f, 0.26f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());
            var grt = go.GetComponent<RectTransform>();

            // Icon (falls vorhanden)
            var icon = Resources.Load<Texture2D>("Icons/" + iconId);
            if (icon != null)
            {
                var igo = new GameObject("Icon");
                igo.transform.SetParent(go.transform, false);
                var rimg = igo.AddComponent<RawImage>();
                rimg.texture = icon;
                rimg.raycastTarget = false;
                var irt = rimg.rectTransform;
                irt.anchorMin = new Vector2(0f, 0.34f);
                irt.anchorMax = new Vector2(1f, 1f);
                irt.offsetMin = new Vector2(3f, 0f);
                irt.offsetMax = new Vector2(-3f, -3f);
                var lbl = Label(grt, "Label", new Vector2(0f, 0f), new Vector2(1f, 0.36f),
                    title + "  " + sub, 13, TextAnchor.MiddleCenter, Color.white);
                lbl.resizeTextForBestFit = true;
                lbl.resizeTextMinSize = 9;
                lbl.resizeTextMaxSize = 14;
            }
            else
            {
                Label(grt, "Label", Vector2.zero, Vector2.one,
                    title + "\n" + sub, 15, TextAnchor.MiddleCenter, Color.white);
            }

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.3f, 0.9f, 0.4f, 0.3f);
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

            badge = Label(grt, "Badge", new Vector2(0.68f, 0.6f), Vector2.one,
                "", 17, TextAnchor.UpperRight, new Color(1f, 0.9f, 0.4f));
            badge.rectTransform.offsetMax = new Vector2(-4f, -2f);
            var bOutline = badge.gameObject.AddComponent<Outline>();
            bOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            bOutline.effectDistance = new Vector2(1.5f, -1.5f);

            return btn;
        }

        // ------------------------------ Logik ------------------------------

        void TryQueueUnit(UnitData ud)
        {
            if (GameManager.Instance.GameOver) return;
            ProductionBuilding fac = ud.builtAt == Factory.Barracks ? cachedRax : cachedWf;
            if (fac == null)
            {
                Message(ud.builtAt == Factory.Barracks
                    ? "Baue zuerst eine Kaserne!"
                    : "Baue zuerst eine Waffenfabrik!");
                return;
            }
            if (!ResourceManager.Instance.TrySpend(Team.Player, ud.cost))
            {
                Message("Nicht genug Credits! (" + ud.cost + ")");
                return;
            }
            fac.Enqueue(ud);
        }

        public void SetSpeed(int m)
        {
            if (GameManager.Instance != null && GameManager.Instance.GameOver) return;
            Time.timeScale = m;
            for (int i = 0; i < speedButtons.Count; i++)
                speedButtons[i].color = (i == m - 1)
                    ? new Color(0.95f, 0.7f, 0.2f, 1f)
                    : new Color(0.18f, 0.21f, 0.26f, 1f);
            if (Time.timeSinceLevelLoad > 1f) Message("Spieltempo: " + m + "x");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) SetSpeed(1);
            if (Input.GetKeyDown(KeyCode.F2)) SetSpeed(2);
            if (Input.GetKeyDown(KeyCode.F3)) SetSpeed(3);

            var rm = ResourceManager.Instance;
            if (rm != null)
            {
                creditsText.text = "Credits: " + rm.GetCredits(Team.Player);
                rm.GetEnergyDetail(Team.Player, out int prod, out int cons);
                energyText.text = "Energie: " + cons + " / " + prod + " verfuegbar";
                energyText.color = cons <= prod ? new Color(0.4f, 0.9f, 1f) : new Color(1f, 0.3f, 0.25f);
            }

            if (Time.unscaledTime > msgUntil && msgText.text.Length > 0) msgText.text = "";

            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 0.4f;
                cachedRax = FindPlayerFactory("Barracks");
                cachedWf = FindPlayerFactory("WarFactory");
                hasRadar = HasRadar();
                if (radarOverlay != null) radarOverlay.SetActive(!hasRadar);
                if (mmClick != null) mmClick.enabled = hasRadar;
            }

            UpdateUnitButtons();
            UpdateProdRows();
        }

        void UpdateUnitButtons()
        {
            foreach (var ub in unitButtons)
            {
                var fac = ub.unit.builtAt == Factory.Barracks ? cachedRax : cachedWf;
                bool ok = fac != null;
                ub.btn.interactable = ok;
                if (ok)
                {
                    int count = 0;
                    foreach (var q in fac.queue) if (q.id == ub.unit.id) count++;
                    ub.badge.text = count > 0 ? "x" + count : "";
                    ub.fill.fillAmount = fac.queue.Count > 0 && fac.queue[0].id == ub.unit.id
                        ? fac.progress : 0f;
                }
                else { ub.badge.text = ""; ub.fill.fillAmount = 0f; }
            }
        }

        void UpdateProdRows()
        {
            var entries = new List<(string, float)>();
            foreach (var d in Damageable.All)
            {
                if (d == null || d.team != Team.Player || !(d is Building b) || b.data == null) continue;
                if (!b.Constructed)
                    entries.Add(("Bau: " + b.data.displayName, b.BuildProgress));
                else if (b is ProductionBuilding pb && pb.queue.Count > 0)
                {
                    string extra = pb.queue.Count > 1 ? " (+" + (pb.queue.Count - 1) + ")" : "";
                    entries.Add((pb.data.displayName + ": " + pb.queue[0].displayName + extra, pb.progress));
                }
                if (entries.Count >= prodRows.Count) break;
            }
            for (int i = 0; i < prodRows.Count; i++)
            {
                bool active = i < entries.Count;
                if (prodRows[i].go.activeSelf != active) prodRows[i].go.SetActive(active);
                if (active)
                {
                    prodRows[i].label.text = entries[i].Item1;
                    prodRows[i].fill.fillAmount = Mathf.Clamp01(entries[i].Item2);
                }
            }
        }

        bool HasRadar()
        {
            if (ResourceManager.Instance == null || !ResourceManager.Instance.HasPower(Team.Player))
                return false;
            foreach (var d in Damageable.All)
                if (d != null && d.team == Team.Player && d is Building b &&
                    b.Constructed && b.data != null && b.data.id == "Radar")
                    return true;
            return false;
        }

        ProductionBuilding FindPlayerFactory(string id)
        {
            foreach (var d in Damageable.All)
                if (d != null && d.team == Team.Player && d is ProductionBuilding p &&
                    p.Constructed && p.data != null && p.data.id == id)
                    return p;
            return null;
        }

        public void ShowGameOver(bool victory)
        {
            gameOverPanel.SetActive(true);
            gameOverText.text = victory ? "SIEG!" : "NIEDERLAGE";
            gameOverText.color = victory ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.3f, 0.25f);
        }
    }
}
