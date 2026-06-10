using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RTS
{
    public class BuildPlacement : MonoBehaviour
    {
        public static BuildPlacement Instance { get; private set; }
        public bool IsPlacing => current != null;
        public const float BuildRadius = 20f;

        BuildingData current;
        GameObject ghost;
        GameObject radiusViz;
        LayerMask groundMask, blockMask;
        static readonly Color okC = new Color(0.2f, 1f, 0.3f, 0.45f);
        static readonly Color badC = new Color(1f, 0.2f, 0.2f, 0.45f);
        static Texture2D radiusTex;
        static Material radiusMat;

        void Awake()
        {
            Instance = this;
            groundMask = LayerMask.GetMask("Ground");
            blockMask = LayerMask.GetMask("Units", "Buildings");
        }

        public void StartPlacement(BuildingData d)
        {
            Cancel();
            if (ResourceManager.Instance.GetCredits(Team.Player) < d.cost)
            {
                UIManager.Message("Nicht genug Credits! (" + d.cost + ")");
                return;
            }
            current = d;
            var prefab = Resources.Load<GameObject>("Prefabs/" + d.id);
            ghost = Object.Instantiate(prefab);
            foreach (var comp in ghost.GetComponentsInChildren<MonoBehaviour>()) comp.enabled = false;
            foreach (var col in ghost.GetComponentsInChildren<Collider>()) col.enabled = false;
            foreach (var obs in ghost.GetComponentsInChildren<NavMeshObstacle>()) obs.enabled = false;
            ShowRadius();
        }

        // Gruene Kreise: ueberall hier darf gebaut werden
        void ShowRadius()
        {
            HideRadius();
            radiusViz = new GameObject("BuildRadiusViz");
            int i = 0;
            foreach (var d in Damageable.All)
            {
                if (d == null || d.team != Team.Player) continue;
                if (!(d is Building b) || b.data == null || b.data.id == "Wall") continue;
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(radiusViz.transform, false);
                q.transform.position = b.transform.position + Vector3.up * (0.05f + i * 0.004f);
                q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                q.transform.localScale = new Vector3(BuildRadius * 2f, BuildRadius * 2f, 1f);
                q.GetComponent<Renderer>().material = RadiusMat();
                i++;
            }
        }

        void HideRadius()
        {
            if (radiusViz != null) Object.Destroy(radiusViz);
            radiusViz = null;
        }

        static Material RadiusMat()
        {
            if (radiusMat != null) return radiusMat;
            if (radiusTex == null)
            {
                radiusTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 128; x++)
                    {
                        float dx = (x - 63.5f) / 63.5f, dy = (y - 63.5f) / 63.5f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = r <= 0.88f ? 0.13f : (r <= 0.97f ? 0.4f : 0f);
                        radiusTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                radiusTex.Apply();
            }
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            radiusMat = new Material(shader);
            radiusMat.SetTexture("_BaseMap", radiusTex);
            radiusMat.color = new Color(0.3f, 1f, 0.45f, 1f);
            MatUtil.MakeTransparent(radiusMat);
            return radiusMat;
        }

        void Tint(bool ok)
        {
            var m = MatUtil.Unlit(ok ? okC : badC);
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.materials = mats;
            }
        }

        void Update()
        {
            if (!IsPlacing) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) { Cancel(); return; }
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 600f, groundMask)) return;
            Vector3 pos = new Vector3(Mathf.Round(hit.point.x / 2f) * 2f, 0f, Mathf.Round(hit.point.z / 2f) * 2f);
            ghost.transform.position = pos;
            bool valid = IsValid(pos);
            Tint(valid);

            if (Input.GetMouseButtonDown(0) && !UIManager.IsPointerOverUI())
            {
                if (!valid) { UIManager.Message("Hier kann nicht gebaut werden!"); return; }
                if (ResourceManager.Instance.TrySpend(Team.Player, current.cost))
                {
                    GameManager.Instance.SpawnBuilding(current, Team.Player, pos, 45f, false);
                    if (current.id == "Wall")
                    {
                        // Mauern in Serie bauen
                        if (ResourceManager.Instance.GetCredits(Team.Player) < current.cost) Cancel();
                    }
                    else Cancel();
                }
                else
                {
                    UIManager.Message("Nicht genug Credits!");
                    Cancel();
                }
            }
        }

        bool IsValid(Vector3 pos)
        {
            // Im Bau-Radius irgendeines eigenen Gebaeudes (ausser Mauern)?
            bool near = false;
            foreach (var d in Damageable.All)
            {
                if (d == null || d.team != Team.Player || !(d is Building b) || b.data == null) continue;
                if (b.data.id == "Wall") continue;
                if (Vector3.Distance(b.transform.position, pos) < BuildRadius) { near = true; break; }
            }
            if (!near) return false;
            Vector3 half = new Vector3(current.footprint.x * 0.5f + 0.5f, 2f, current.footprint.y * 0.5f + 0.5f);
            return !Physics.CheckBox(pos + Vector3.up * 2.2f, half, Quaternion.identity, blockMask);
        }

        public void Cancel()
        {
            current = null;
            if (ghost != null) Object.Destroy(ghost);
            ghost = null;
            HideRadius();
        }
    }
}
