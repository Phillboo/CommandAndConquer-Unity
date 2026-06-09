using UnityEngine;
using UnityEngine.AI;

namespace RTS
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Unit : Damageable
    {
        public UnitData data;
        protected NavMeshAgent agent;
        protected Transform turret;
        protected Transform model;
        GameObject ring;
        protected Damageable target;
        bool hasOrder;
        float nextShot;
        float nextScan;
        public bool IsSelected { get; private set; }

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            model = transform.Find("Model");
            turret = FindDeep(transform, "Turret");
        }

        public virtual void Init(UnitData d, Team t)
        {
            data = d;
            team = t;
            maxHp = hp = d.maxHp;
            hbHeight = d.hbHeight;
            agent.speed = d.speed;
            agent.acceleration = 18f;
            agent.angularSpeed = 420f;
            agent.radius = d.agentRadius;
            gameObject.tag = TeamUtil.TagFor(t);
            gameObject.layer = LayerMask.NameToLayer("Units");
            TeamUtil.ApplyTeamColor(gameObject, t);
            var blip = GetComponentInChildren<MinimapBlip>(true);
            if (blip != null) blip.SetTeam(t);
            var r = FindDeep(transform, "SelectionRing");
            ring = r != null ? r.gameObject : null;
            if (ring != null) ring.SetActive(false);
        }

        public void SetSelected(bool sel)
        {
            IsSelected = sel;
            if (ring != null) ring.SetActive(sel);
        }

        public void MoveTo(Vector3 pos)
        {
            target = null;
            hasOrder = true;
            agent.stoppingDistance = 0.6f;
            if (agent.isOnNavMesh) agent.SetDestination(pos);
        }

        public void Attack(Damageable t)
        {
            if (t == null) return;
            target = t;
            hasOrder = true;
        }

        protected virtual void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.GameOver) return;
            if (data == null || data.damage <= 0f) return;

            if (target != null && !target.IsAlive) { target = null; hasOrder = false; }
            if (hasOrder && target == null && !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance + 0.2f) hasOrder = false;

            if (target == null && Time.time >= nextScan)
            {
                nextScan = Time.time + 0.4f;
                if (!hasOrder || agent.velocity.sqrMagnitude < 0.3f)
                    target = FindNearestEnemy(team, transform.position, data.aggroRange);
            }

            if (target != null) CombatTick();
            else if (turret != null)
                turret.localRotation = Quaternion.Slerp(turret.localRotation, Quaternion.identity, Time.deltaTime * 3f);
        }

        void CombatTick()
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist > data.attackRange)
            {
                agent.stoppingDistance = Mathf.Max(0.6f, data.attackRange - 1.5f);
                if (agent.isOnNavMesh) agent.SetDestination(target.transform.position);
                return;
            }
            if (agent.isOnNavMesh && agent.hasPath) agent.ResetPath();
            Vector3 dir = target.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                var look = Quaternion.LookRotation(dir);
                if (turret != null)
                    turret.rotation = Quaternion.Slerp(turret.rotation, look, Time.deltaTime * 8f);
                else
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
            }
            if (Time.time >= nextShot)
            {
                nextShot = Time.time + data.attackRate;
                Vector3 muzzle = (turret != null ? turret.position : transform.position) + Vector3.up * 0.5f;
                Projectile.Fire(muzzle, target, data.damage, data.projectileSpeed, team, this);
            }
        }

        public static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != root && (t.name == name || t.name.StartsWith(name))) return t;
            return null;
        }
    }
}
