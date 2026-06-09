using System.Collections.Generic;
using UnityEngine;

namespace RTS
{
    public enum Team { Player = 0, Enemy = 1 }
    public enum Factory { Barracks, WarFactory }

    public static class TeamUtil
    {
        public static Color TeamTint(Team t) =>
            t == Team.Player ? new Color(0.25f, 0.5f, 0.95f) : new Color(0.85f, 0.18f, 0.12f);

        public static string TagFor(Team t) => t == Team.Player ? "Player" : "Enemy";

        public static void ApplyTeamColor(GameObject go, Team team)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].name.Contains("TeamColor"))
                    {
                        mats[i].color = TeamTint(team);
                        changed = true;
                    }
                }
                if (changed) r.materials = mats;
            }
        }
    }

    public static class MatUtil
    {
        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();
        static Material particleMat;

        public static Material Unlit(Color c)
        {
            if (cache.TryGetValue(c, out var m) && m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            m = new Material(shader);
            m.color = c;
            if (c.a < 0.99f) MakeTransparent(m);
            cache[c] = m;
            return m;
        }

        public static Material Particle()
        {
            if (particleMat != null) return particleMat;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            particleMat = new Material(shader);
            return particleMat;
        }

        public static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
    }

    public static class Explosion
    {
        public static void Spawn(Vector3 pos, float scale, Color color)
        {
            var go = new GameObject("Explosion");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 5.5f * scale;
            main.startSize = 0.55f * scale;
            main.startColor = color;
            main.duration = 0.4f;
            main.loop = false;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(14 * scale, 8, 60)) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(new Color(0.25f, 0.22f, 0.2f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.25f));
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = MatUtil.Particle();
            ps.Play();
            Object.Destroy(go, 1.6f);
        }
    }
}
