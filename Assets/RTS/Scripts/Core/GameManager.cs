using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace RTS
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public List<UnitData> unitTypes = new List<UnitData>();
        public List<BuildingData> buildingTypes = new List<BuildingData>();
        public bool GameOver { get; private set; }

        void Awake()
        {
            Instance = this;
            unitTypes = Resources.LoadAll<UnitData>("Data").OrderBy(u => u.cost).ToList();
            buildingTypes = Resources.LoadAll<BuildingData>("Data").OrderBy(b => b.cost).ToList();
        }

        public UnitData GetUnit(string id) => unitTypes.FirstOrDefault(u => u.id == id);
        public BuildingData GetBuilding(string id) => buildingTypes.FirstOrDefault(b => b.id == id);

        public Unit SpawnUnit(UnitData d, Team team, Vector3 pos)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/" + d.id);
            if (prefab == null) { Debug.LogError("Prefab fehlt: " + d.id); return null; }
            if (NavMesh.SamplePosition(pos, out var hit, 10f, NavMesh.AllAreas)) pos = hit.position;
            var go = Instantiate(prefab, pos, Quaternion.identity);
            var u = go.GetComponent<Unit>();
            u.Init(d, team);
            return u;
        }

        public Building SpawnBuilding(BuildingData d, Team team, Vector3 pos, float yawDeg, bool instant)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/" + d.id);
            if (prefab == null) { Debug.LogError("Prefab fehlt: " + d.id); return null; }
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, yawDeg, 0f));
            var b = go.GetComponent<Building>();
            b.Init(d, team, instant);
            return b;
        }

        void Start() { InvokeRepeating(nameof(CheckEnd), 6f, 2f); }

        void CheckEnd()
        {
            if (GameOver) return;
            int p = Damageable.CountBuildings(Team.Player);
            int e = Damageable.CountBuildings(Team.Enemy);
            if (e == 0) End(true);
            else if (p == 0) End(false);
        }

        void End(bool victory)
        {
            GameOver = true;
            if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(victory);
            Time.timeScale = 0f;
        }
    }
}
