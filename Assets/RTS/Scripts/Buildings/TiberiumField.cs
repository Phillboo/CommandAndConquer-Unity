using System.Collections.Generic;
using UnityEngine;

namespace RTS
{
    public class TiberiumField : MonoBehaviour
    {
        public static readonly List<TiberiumField> All = new List<TiberiumField>();
        public float amount = 4000f;
        public float maxAmount = 4000f;
        Vector3 baseScale;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start()
        {
            baseScale = transform.localScale;
            foreach (var r in GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m.name.Contains("Tiberium"))
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", new Color(0.08f, 1.4f, 0.35f));
                    }
            InvokeRepeating(nameof(Regrow), 5f, 1f);
        }

        void Regrow()
        {
            if (amount < maxAmount)
            {
                amount = Mathf.Min(maxAmount, amount + 3f);
                UpdateVisual();
            }
        }

        public float Harvest(float want)
        {
            float got = Mathf.Min(want, amount);
            amount -= got;
            UpdateVisual();
            return got;
        }

        void UpdateVisual()
        {
            float pct = Mathf.Clamp01(amount / maxAmount);
            transform.localScale = baseScale * Mathf.Lerp(0.35f, 1f, pct);
        }

        public static TiberiumField FindNearest(Vector3 pos)
        {
            TiberiumField best = null;
            float bd = float.MaxValue;
            foreach (var f in All)
            {
                if (f == null || f.amount < 50f) continue;
                float d = (f.transform.position - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = f; }
            }
            return best;
        }
    }
}
