using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS
{
    public class AIController : MonoBehaviour
    {
        public Vector3 basePos = new Vector3(38f, 0f, 38f);
        float nextThink, nextWave;
        int buildIndex;
        int mixIndex;

        static readonly string[] buildOrder =
        { "PowerPlant", "Refinery", "Barracks", "DefenseTurret", "WarFactory", "PowerPlant", "DefenseTurret", "Refinery" };

        static readonly string[] armyMix =
        { "Infantry", "Infantry", "RocketSquad", "TankLight", "Infantry", "TankLight", "RocketSquad", "TankHeavy" };

        void Start()
        {
            nextThink = Time.time + 4f;
            nextWave = Time.time + 130f;
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.GameOver) return;
            if (Time.time < nextThink) return;
            nextThink = Time.time + 2.5f;
            Think();
            if (Time.time >= nextWave)
            {
                LaunchWave();
                nextWave = Time.time + 90f;
            }
        }

        void Think()
        {
            var gm = GameManager.Instance;
            var rm = ResourceManager.Instance;

            if (buildIndex < buildOrder.Length)
            {
                var bd = gm.GetBuilding(buildOrder[buildIndex]);
                if (bd != null && rm.GetCredits(Team.Enemy) >= bd.cost + 200)
                {
                    Vector3? spot = FindSpot(bd);
                    if (spot.HasValue && rm.TrySpend(Team.Enemy, bd.cost))
                    {
                        gm.SpawnBuilding(bd, Team.Enemy, spot.Value, 225f, false);
                        buildIndex++;
                    }
                }
            }

            var wf = FindFactory("WarFactory");
            var rax = FindFactory("Barracks");

            int harvesters = CountUnits(u => u.data != null && u.data.isHarvester);
            if (harvesters < 2 && wf != null && wf.queue.Count == 0)
            {
                var hv = gm.GetUnit("Harvester");
                if (hv != null && rm.TrySpend(Team.Enemy, hv.cost)) { wf.Enqueue(hv); return; }
            }

            int army = CountUnits(u => u.data != null && !u.data.isHarvester);
            if (army < 24)
            {
                var ud = gm.GetUnit(armyMix[mixIndex % armyMix.Length]);
                if (ud != null)
                {
                    var fac = ud.builtAt == Factory.Barracks ? rax : wf;
                    if (fac != null && fac.queue.Count < 3 && rm.TrySpend(Team.Enemy, ud.cost))
                    {
                        fac.Enqueue(ud);
                        mixIndex++;
                    }
                }
            }

            // Leichtes Grundeinkommen fuer die KI als Ausgleich
            rm.Add(Team.Enemy, 10);
        }

        void LaunchWave()
        {
            var targets = Damageable.All.Where(d => d != null && d.team == Team.Player && d is Building).ToList();
            if (targets.Count == 0) return;
            var target = targets[Random.Range(0, targets.Count)];
            int sent = 0;
            foreach (var d in Damageable.All.ToList())
            {
                if (d == null || d.team != Team.Enemy || !(d is Unit u)) continue;
                if (u is Harvester || (u.data != null && u.data.isHarvester)) continue;
                if (sent >= 18) break;
                u.Attack(target);
                sent++;
            }
            if (sent >= 4) UIManager.Message("Feindlicher Angriff im Anmarsch!");
        }

        Vector3? FindSpot(BuildingData bd)
        {
            for (int tries = 0; tries < 30; tries++)
            {
                Vector2 r = Random.insideUnitCircle * 24f;
                Vector3 p = basePos + new Vector3(r.x, 0f, r.y);
                p = new Vector3(Mathf.Round(p.x / 2f) * 2f, 0f, Mathf.Round(p.z / 2f) * 2f);
                if (Mathf.Abs(p.x) > 56f || Mathf.Abs(p.z) > 56f) continue;
                Vector3 half = new Vector3(bd.footprint.x * 0.5f + 1f, 2f, bd.footprint.y * 0.5f + 1f);
                if (!Physics.CheckBox(p + Vector3.up * 2.2f, half, Quaternion.identity,
                        LayerMask.GetMask("Units", "Buildings")))
                    return p;
            }
            return null;
        }

        ProductionBuilding FindFactory(string id)
        {
            foreach (var d in Damageable.All)
                if (d != null && d.team == Team.Enemy && d is ProductionBuilding p &&
                    p.Constructed && p.data != null && p.data.id == id)
                    return p;
            return null;
        }

        int CountUnits(System.Func<Unit, bool> pred)
        {
            int n = 0;
            foreach (var d in Damageable.All)
                if (d != null && d.team == Team.Enemy && d is Unit u && pred(u)) n++;
            return n;
        }
    }
}
