using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KeyboardEscapeGameplay
{
    sealed class World1CosmeticMaterials : IDisposable
    {
        readonly Dictionary<(Material, bool), Material> materials = new Dictionary<(Material, bool), Material>();
        readonly Shader shader = Resources.Load<Shader>("World1Cosmetic");
        Dictionary<string, Material> auraMaterials;

        public void ApplyAura(GameObject effect)
        {
            if (auraMaterials == null)
            {
                auraMaterials = new Dictionary<string, Material>(StringComparer.Ordinal);
                foreach (var material in Resources.LoadAll<Material>("World1AuraMaterials"))
                    auraMaterials.Add(material.name, material);
            }
            foreach (var renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                var assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                    if (assigned[i] != null && auraMaterials.TryGetValue(assigned[i].name, out var converted))
                        assigned[i] = converted;
                renderer.sharedMaterials = assigned;
                renderer.enableGPUInstancing = false;
            }
        }

        public void Apply(GameObject effect)
        {
            foreach (var renderer in effect.GetComponentsInChildren<Renderer>(true))
            {
                bool isTrail = renderer is TrailRenderer;
                if (!isTrail && !(renderer is ParticleSystemRenderer)) continue;
                var assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                {
                    Material source = assigned[i];
                    if (source == null) continue;
                    bool additive = !isTrail && (source.name.EndsWith("_ADD", StringComparison.OrdinalIgnoreCase) || source.name == "M_ADD");
                    var key = (source, additive);
                    if (!materials.TryGetValue(key, out var converted))
                    {
                        converted = new Material(shader) { name = source.name + " (World 1)" };
                        if (source.HasProperty("_MainTex")) converted.mainTexture = source.mainTexture;
                        if (source.HasProperty("_Color")) converted.color = source.color;
                        converted.SetFloat("_DestinationBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                        materials.Add(key, converted);
                    }
                    assigned[i] = converted;
                }
                renderer.sharedMaterials = assigned;
            }
        }

        public void Dispose()
        {
            foreach (var material in materials.Values) UnityEngine.Object.Destroy(material);
            materials.Clear();
        }
    }
}
