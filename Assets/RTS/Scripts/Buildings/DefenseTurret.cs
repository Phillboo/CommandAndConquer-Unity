using UnityEngine;

namespace RTS
{
    public class DefenseTurret : Building
    {
        Transform turret;
        float nextShot;

        public override void Init(BuildingData d, Team t, bool instant)
        {
            base.Init(d, t, instant);
            turret = Unit.FindDeep(transform, "Turret");
        }

        protected override void Update()
        {
            base.Update();
            if (!Constructed || data == null) return;
            if (GameManager.Instance != null && GameManager.Instance.GameOver) return;
            if (ResourceManager.Instance != null && !ResourceManager.Instance.HasPower(team)) return;

            var target = FindNearestEnemy(team, transform.position, data.attackRange);
            if (target == null) return;
            Vector3 dir = target.transform.position - transform.position;
            dir.y = 0f;
            if (turret != null && dir.sqrMagnitude > 0.01f)
                turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 6f);
            if (Time.time >= nextShot)
            {
                nextShot = Time.time + data.attackRate;
                Vector3 muzzle = turret != null ? turret.position : transform.position + Vector3.up * 2f;
                Projectile.Fire(muzzle, target, data.damage, 32f, team, this);
            }
        }
    }
}
