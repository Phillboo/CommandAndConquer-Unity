using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RTS
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }
        public readonly List<Unit> selected = new List<Unit>();
        public Building selectedBuilding;
        Vector2 dragStart;
        bool dragging;
        Texture2D boxTex;
        LayerMask groundMask, targetMask;

        void Awake()
        {
            Instance = this;
            boxTex = new Texture2D(1, 1);
            boxTex.SetPixel(0, 0, new Color(0.4f, 1f, 0.4f, 0.18f));
            boxTex.Apply();
            groundMask = LayerMask.GetMask("Ground");
            targetMask = LayerMask.GetMask("Units", "Buildings");
        }

        void Update()
        {
            selected.RemoveAll(u => u == null);
            if (GameManager.Instance != null && GameManager.Instance.GameOver) return;
            if (BuildPlacement.Instance != null && BuildPlacement.Instance.IsPlacing) return;

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                dragStart = Input.mousePosition;
                dragging = true;
            }

            if (Input.GetMouseButtonUp(0) && dragging)
            {
                dragging = false;
                if (Vector2.Distance(dragStart, (Vector2)Input.mousePosition) < 8f) ClickSelect();
                else BoxSelect();
            }

            if (Input.GetMouseButtonDown(1) && !IsPointerOverUI()) RightClick();
        }

        public static bool IsPointerOverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        void ClickSelect()
        {
            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!additive) ClearSelection();
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 600f, targetMask)) return;

            var u = hit.collider.GetComponentInParent<Unit>();
            if (u != null && u.team == Team.Player)
            {
                if (!selected.Contains(u)) { selected.Add(u); u.SetSelected(true); }
                return;
            }
            var b = hit.collider.GetComponentInParent<Building>();
            if (b != null && b.team == Team.Player)
            {
                if (selectedBuilding != null) selectedBuilding.SetSelected(false);
                selectedBuilding = b;
                b.SetSelected(true);
            }
        }

        void BoxSelect()
        {
            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!additive) ClearSelection();
            Rect r = GetScreenRect(dragStart, Input.mousePosition);
            foreach (var d in Damageable.All)
            {
                if (!(d is Unit u) || u.team != Team.Player) continue;
                Vector3 sp = Camera.main.WorldToScreenPoint(u.transform.position);
                if (sp.z > 0f && r.Contains(new Vector2(sp.x, sp.y)))
                    if (!selected.Contains(u)) { selected.Add(u); u.SetSelected(true); }
            }
        }

        void RightClick()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit, 600f, targetMask))
            {
                var dmg = hit.collider.GetComponentInParent<Damageable>();
                if (dmg != null && dmg.team == Team.Enemy && dmg.IsAlive)
                {
                    foreach (var u in selected) u.Attack(dmg);
                    return;
                }
            }

            if (Physics.Raycast(ray, out var ghit, 600f, groundMask))
            {
                Vector3 p = ghit.point;
                if (selected.Count > 0)
                {
                    int side = Mathf.CeilToInt(Mathf.Sqrt(selected.Count));
                    Vector3 corner = new Vector3(side - 1, 0f, side - 1) * 1.1f;
                    for (int i = 0; i < selected.Count; i++)
                    {
                        Vector3 off = new Vector3(i % side * 2.2f, 0f, i / side * 2.2f);
                        selected[i].MoveTo(p + off - corner);
                    }
                }
                else if (selectedBuilding is ProductionBuilding pb)
                {
                    pb.SetRally(p);
                    UIManager.Message("Sammelpunkt gesetzt");
                }
            }
        }

        public void ClearSelection()
        {
            foreach (var u in selected) if (u != null) u.SetSelected(false);
            selected.Clear();
            if (selectedBuilding != null) { selectedBuilding.SetSelected(false); selectedBuilding = null; }
        }

        static Rect GetScreenRect(Vector2 a, Vector2 b)
        {
            Vector2 min = Vector2.Min(a, b), max = Vector2.Max(a, b);
            return new Rect(min, max - min);
        }

        void OnGUI()
        {
            if (!dragging) return;
            Rect r = GetScreenRect(dragStart, Input.mousePosition);
            if (r.width < 4f || r.height < 4f) return;
            Rect gr = new Rect(r.xMin, Screen.height - r.yMax, r.width, r.height);
            GUI.DrawTexture(gr, boxTex);
        }
    }
}
