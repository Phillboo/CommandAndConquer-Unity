using System.Collections.Generic;
using UnityEngine;

namespace RTS
{
    public class ProductionBuilding : Building
    {
        public readonly List<UnitData> queue = new List<UnitData>();
        public float progress;
        float remaining;

        public bool CanProduce(UnitData u) =>
            data != null &&
            ((data.id == "Barracks" && u.builtAt == Factory.Barracks) ||
             (data.id == "WarFactory" && u.builtAt == Factory.WarFactory));

        public void Enqueue(UnitData u)
        {
            if (queue.Count >= 9) return;
            queue.Add(u);
            if (queue.Count == 1) remaining = u.buildTime;
        }

        protected override void Update()
        {
            base.Update();
            if (!Constructed || queue.Count == 0) return;
            float mult = ResourceManager.Instance != null && ResourceManager.Instance.HasPower(team) ? 1f : 0.4f;
            remaining -= Time.deltaTime * mult;
            progress = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.1f, queue[0].buildTime));
            if (remaining <= 0f)
            {
                var u = queue[0];
                queue.RemoveAt(0);
                Vector3 spawn = transform.position + transform.forward * 5f;
                var unit = GameManager.Instance.SpawnUnit(u, team, spawn);
                if (unit != null) unit.MoveTo(rallyPoint);
                if (queue.Count > 0) remaining = queue[0].buildTime;
                progress = 0f;
            }
        }
    }
}
