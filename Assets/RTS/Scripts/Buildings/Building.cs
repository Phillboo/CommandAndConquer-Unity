using UnityEngine;

namespace RTS
{
    public class Building : Damageable
    {
        public BuildingData data;
        public bool Constructed { get; private set; }
        public float BuildProgress { get; private set; }
        public Vector3 rallyPoint;
        protected Transform model;
        GameObject ring;
        float buildEnd;
        Vector3 fullScale = Vector3.one;

        public virtual void Init(BuildingData d, Team t, bool instant)
        {
            data = d;
            team = t;
            maxHp = d.maxHp;
            hp = instant ? d.maxHp : d.maxHp * 0.1f;
            hbHeight = d.hbHeight;
            gameObject.tag = "Building";
            gameObject.layer = LayerMask.NameToLayer("Buildings");
            TeamUtil.ApplyTeamColor(gameObject, t);
            var blip = GetComponentInChildren<MinimapBlip>(true);
            if (blip != null) blip.SetTeam(t);
            model = transform.Find("Model");
            var r = Unit.FindDeep(transform, "SelectionRing");
            ring = r != null ? r.gameObject : null;
            if (ring != null) ring.SetActive(false);
            rallyPoint = transform.position + transform.forward * 8f;
            if (model != null) fullScale = model.localScale;
            if (instant) { Constructed = true; BuildProgress = 1f; }
            else
            {
                buildEnd = Time.time + d.buildTime;
                if (model != null)
                    model.localScale = new Vector3(fullScale.x, fullScale.y * 0.12f, fullScale.z);
            }
        }

        protected virtual void Update()
        {
            if (Constructed || data == null) return;
            float remain = buildEnd - Time.time;
            float pct = 1f - Mathf.Clamp01(remain / Mathf.Max(0.1f, data.buildTime));
            BuildProgress = pct;
            if (model != null)
                model.localScale = new Vector3(fullScale.x, fullScale.y * Mathf.Lerp(0.12f, 1f, pct), fullScale.z);
            hp = Mathf.Max(hp, maxHp * Mathf.Lerp(0.1f, 1f, pct));
            if (remain <= 0f)
            {
                Constructed = true;
                if (model != null) model.localScale = fullScale;
                hp = maxHp;
            }
        }

        public void SetSelected(bool sel) { if (ring != null) ring.SetActive(sel); }
        public void SetRally(Vector3 p) { rallyPoint = p; }
    }
}
