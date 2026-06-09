using UnityEngine;
using UnityEngine.AI;

namespace RTS
{
    public class BuildPlacement : MonoBehaviour
    {
        public static BuildPlacement Instance { get; private set; }
        public bool IsPlacing => current != null;
        BuildingData current;
        GameObject ghost;
        LayerMask groundMask, blockMask;
        static readonly Color okC = new Color(0.2f, 1f, 0.3f, 0.45f);
        static readonly Color badC = new Color(1f, 0.2f, 0.2f, 0.45f);

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
        }

        void Tint(bool ok)
        {
            var mat = MatUtil.Unlit(ok ? okC : badC);
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
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
                    Cancel();
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
            bool near = false;
            foreach (var d in Damageable.All)
            {
                if (d == null || d.team != Team.Player || !(d is Building b) || b.data == null) continue;
                if (b.data.id == "ConYard" && Vector3.Distance(b.transform.position, pos) < 32f)
                { near = true; break; }
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
        }
    }
}
