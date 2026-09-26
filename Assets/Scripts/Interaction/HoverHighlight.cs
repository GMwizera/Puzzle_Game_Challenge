using System.Collections.Generic;
using UnityEngine;

// Makes whatever the player is looking at glow softly, and restores it afterwards.
// Works on URP Lit (and most built-in) materials through the _EmissionColor property.
public class HoverHighlight
{
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    struct Saved
    {
        public Material material;
        public Color emission;
        public bool keyword;
    }

    readonly Color glow;
    readonly List<Saved> saved = new();
    GameObject current;

    public HoverHighlight(Color glow) => this.glow = glow;

    public void Set(GameObject target)
    {
        if (target == current) return;
        Clear();
        current = target;
        if (target == null) return;

        foreach (Renderer r in target.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || !r.enabled) continue;

            foreach (Material m in r.materials)
            {
                if (!m.HasProperty(EmissionColor)) continue;

                saved.Add(new Saved { material = m, emission = m.GetColor(EmissionColor), keyword = m.IsKeywordEnabled("_EMISSION") });
                m.EnableKeyword("_EMISSION");
                m.SetColor(EmissionColor, m.GetColor(EmissionColor) + glow);
            }
        }
    }

    public void Clear()
    {
        foreach (Saved s in saved)
        {
            if (s.material == null) continue;
            s.material.SetColor(EmissionColor, s.emission);
            if (!s.keyword) s.material.DisableKeyword("_EMISSION");
        }

        saved.Clear();
        current = null;
    }
}
