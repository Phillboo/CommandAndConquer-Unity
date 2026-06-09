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
        GameObject gameOverPanel;
        float msgUntil;
        Font font;
        Canvas canvas;

        class UnitBtn
        {
            public Button btn;
            public Image fill;
            public Text badge;
            public UnitData unit;
        }
        readonly List<UnitBtn> unitButtons = new List<UnitBtn>();
        readonly List<Button> buildingButtons = new List<Button>();
        float nextFactoryScan;
        ProductionBuilding cachedRax, cachedWf;

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

        // ---------- UI-Aufbau ----------

        void BuildUI()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var cgo = new GameObject("RTS_Canvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();

            // Obere Leiste
            var top = Panel(cgo.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -44f), Vector2.zero, new Color(0.07f, 0.08f, 0.1f, 0.92f));
            creditsText = Label(top, "Credits", new Vector2(0f, 0f), new Vector2(0.25f, 1f),
                "Credits: 0", 24, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f));
            creditsText.rectTransform.offsetMin = new Vector2(16f, 0f);
            energyText = Label(top, "Energy", new Vector2(0.25f, 0f), new Vector2(0.5f, 1f),
                "Energie: 0", 24, TextAnchor.MiddleLeft, new Color(0.4f, 0.9f, 1f));

            // Sidebar rechts
            var side = Panel(cgo.transform, "Sidebar", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-280f, 0f), new Vector2(0f, -44f), new Color(0.09f, 0.1f, 0.13f, 0.94f));

            var layout = side.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 12, 12);
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            SectionTitle(side, "GEBAEUDE");
            var bGrid = Grid(side, 96f);
            foreach (var bd in GameManager.Instance.buildingTypes)
            {
                if (!bd.buildable) continue;
                var data = bd;
                var btn = MakeButton(bGrid, bd.displayName + "\n$" + bd.cost,
                    () => BuildPlacement.Instance.StartPlacement(data), out _, out _);
                buildingButtons.Add(btn);
            }

            SectionTitle(side, "EINHEITEN");
            var uGrid = Grid(side, 96f);
            foreach (var ud in GameManager.Instance.unitTypes)
            {
                var data = ud;
                var btn = MakeButton(uGrid, ud.displayName + "\n$" + ud.cost,
                    () => TryQueueUnit(data), out var fill, out var badge);
                unitButtons.Add(new UnitBtn { btn = btn, fill = fill, badge = badge, unit = data });
            }

            var hint = Label(side.transform as RectTransform, "Hint", Vector2.zero, Vector2.one,
                "LMB: Auswaehlen | Ziehen: Box\nRMB: Bewegen/Angreifen\nShift: Hinzufuegen | ESC: Abbrechen",
                15, TextAnchor.MiddleLeft, new Color(0.65f, 0.68f, 0.72f));
            var hintLe = hint.gameObject.AddComponent<LayoutElement>();
            hintLe.minHeight = 70f;

            // Minimap unten links
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

            // Mitteilungstext
            msgText = Label(cgo.transform as RectTransform ?? null, "Msg", new Vector2(0.2f, 0.78f),
                new Vector2(0.8f, 0.88f), "", 30, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.7f));
            var msgGo = msgText.transform.parent != null ? msgText.gameObject : msgText.gameObject;
            var outline = msgGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Game-Over-Anzeige
            gameOverPanel = Panel(cgo.transform, "GameOver", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.65f)).gameObject;
            gameOverText = Label(gameOverPanel.transform as RectTransform, "GOText",
                Vector2.zero, Vector2.one, "", 90, TextAnchor.MiddleCenter, Color.white);
            gameOverPanel.SetActive(false);
        }

        RenderTexture CreateMinimapCamera()
        {
            var mmGo = new GameObject("MinimapCamera");
            var cam = mmGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 64f;
            mmGo.transform.position = new Vector3(0f, 95f, 0f);
            mmGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.1f, 0.07f);
            cam.farClipPlane = 200f;
            var rt = new RenderTexture(256, 256, 16);
            cam.targetTexture = rt;
            return rt;
        }

        // ---------- Hilfsfunktionen ----------

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
            le.minHeight = 26f;
        }

        RectTransform Grid(RectTransform parent, float rowH)
        {
            var go = new GameObject("Grid");
            go.transform.SetParent(parent, false);
            var grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(124f, 56f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = rowH;
            le.flexibleHeight = 0f;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.GetComponent<RectTransform>();
        }

        Button MakeButton(RectTransform parent, string label, System.Action onClick,
            out Image fill, out Text badge)
        {
            var go = new GameObject("Btn_" + label.Replace("\n", "_"));
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

            var txt = Label(go.GetComponent<RectTransform>(), "Label", Vector2.zero, Vector2.one,
                label, 17, TextAnchor.MiddleCenter, Color.white);
            txt.raycastTarget = false;

            badge = Label(go.GetComponent<RectTransform>(), "Badge", new Vector2(0.7f, 0.55f),
                Vector2.one, "", 16, TextAnchor.UpperRight, new Color(1f, 0.9f, 0.4f));
            badge.raycastTarget = false;
            badge.rectTransform.offsetMax = new Vector2(-4f, -2f);

            return btn;
        }

        // ---------- Laufende Updates ----------

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

        void Update()
        {
            var rm = ResourceManager.Instance;
            if (rm != null)
            {
                creditsText.text = "Credits: " + rm.GetCredits(Team.Player);
                int e = rm.GetEnergy(Team.Player);
                energyText.text = "Energie: " + (e >= 0 ? "+" : "") + e;
                energyText.color = e >= 0 ? new Color(0.4f, 0.9f, 1f) : new Color(1f, 0.3f, 0.25f);
            }

            if (Time.unscaledTime > msgUntil && msgText.text.Length > 0) msgText.text = "";

            if (Time.time >= nextFactoryScan)
            {
                nextFactoryScan = Time.time + 0.5f;
                cachedRax = FindPlayerFactory("Barracks");
                cachedWf = FindPlayerFactory("WarFactory");
            }

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
