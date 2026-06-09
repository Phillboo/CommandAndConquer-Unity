using System.Collections.Generic;
using UnityEngine;

namespace RTS
{
    public class Damageable : MonoBehaviour
    {
        public static readonly List<Damageable> All = new List<Damageable>();

        public Team team;
        public float maxHp = 100f;
        public float hp = 100f;
        public float hbHeight = 2.5f;
        public bool IsAlive => hp > 0f;

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        protected virtual void Start()
        {
            Healthbar.Create(this, hbHeight);
        }

        public virtual void TakeDamage(float dmg, Damageable attacker)
        {
            if (!IsAlive) return;
            hp -= dmg;
            if (hp <= 0f) { hp = 0f; Die(); }
        }

        protected virtual void Die()
        {
            float scale = this is Building ? 3f : 1.3f;
            Explosion.Spawn(transform.position + Vector3.up * 0.8f, scale, new Color(1f, 0.55f, 0.12f));
            Destroy(gameObject);
        }

        public static int CountBuildings(Team t)
        {
            int n = 0;
            foreach (var d in All) if (d != null && d.team == t && d is Building) n++;
            return n;
        }

        public static Damageable FindNearestEnemy(Team myTeam, Vector3 pos, float maxRange)
        {
            Damageable best = null;
            float bestD = maxRange * maxRange;
            foreach (var d in All)
            {
                if (d == null || d.team == myTeam || !d.IsAlive) continue;
                float dd = (d.transform.position - pos).sqrMagnitude;
                if (dd < bestD) { bestD = dd; best = d; }
            }
            return best;
        }
    }
}
