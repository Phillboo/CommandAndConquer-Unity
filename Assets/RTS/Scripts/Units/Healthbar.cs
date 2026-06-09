using UnityEngine;

namespace RTS
{
    public class Healthbar : MonoBehaviour
    {
        Damageable target;
        Transform fg;
        const float W = 1.4f;

        public static Healthbar Create(Damageable d, float height)
        {
            var root = new GameObject("Healthbar");
            root.transform.SetParent(d.transform, false);
            root.transform.localPosition = new Vector3(0f, height, 0f);
            var hb = root.AddComponent<Healthbar>();
            hb.target = d;

            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(bg.GetComponent<Collider>());
            bg.name = "bg";
            bg.transform.SetParent(root.transform, false);
            bg.transform.localScale = new Vector3(W + 0.1f, 0.2f, 1f);
            bg.GetComponent<Renderer>().material = MatUtil.Unlit(new Color(0.04f, 0.04f, 0.04f, 0.85f));

            var f = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(f.GetComponent<Collider>());
            f.name = "fg";
            f.transform.SetParent(root.transform, false);
            f.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            f.transform.localScale = new Vector3(W, 0.13f, 1f);
            Color c = d.team == Team.Player ? new Color(0.2f, 0.95f, 0.25f) : new Color(0.95f, 0.2f, 0.15f);
            f.GetComponent<Renderer>().material = MatUtil.Unlit(c);
            hb.fg = f.transform;
            return hb;
        }

        void LateUpdate()
        {
            if (target == null) { Destroy(gameObject); return; }
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = cam.transform.rotation;
            float pct = Mathf.Clamp01(target.hp / Mathf.Max(1f, target.maxHp));
            bool show = pct < 0.999f;
            foreach (Transform child in transform)
                if (child.gameObject.activeSelf != show) child.gameObject.SetActive(show);
            fg.localScale = new Vector3(W * pct, 0.13f, 1f);
            fg.localPosition = new Vector3(-(W * (1f - pct)) * 0.5f, 0f, -0.01f);
        }
    }
}
