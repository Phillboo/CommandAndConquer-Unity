using UnityEngine;

namespace RTS
{
    public class Projectile : MonoBehaviour
    {
        Damageable target;
        Damageable owner;
        Vector3 lastPos;
        float dmg, speed;

        public static void Fire(Vector3 from, Damageable target, float dmg, float speed, Team team, Damageable owner)
        {
            if (target == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Projectile";
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.24f;
            Color c = team == Team.Player ? new Color(0.45f, 0.85f, 1f) : new Color(1f, 0.5f, 0.2f);
            go.GetComponent<Renderer>().material = MatUtil.Unlit(c);
            var p = go.AddComponent<Projectile>();
            p.target = target;
            p.owner = owner;
            p.dmg = dmg;
            p.speed = speed;
            p.lastPos = target.transform.position + Vector3.up * 0.6f;
            Destroy(go, 6f);
        }

        void Update()
        {
            Vector3 aim = lastPos;
            if (target != null && target.IsAlive)
            {
                aim = target.transform.position + Vector3.up * 0.6f;
                lastPos = aim;
            }
            Vector3 d = aim - transform.position;
            float step = speed * Time.deltaTime;
            if (d.magnitude <= step)
            {
                if (target != null && target.IsAlive) target.TakeDamage(dmg, owner);
                Explosion.Spawn(transform.position, dmg >= 60f ? 1.6f : 0.6f, new Color(1f, 0.7f, 0.25f));
                Destroy(gameObject);
                return;
            }
            transform.position += d.normalized * step;
        }
    }
}
