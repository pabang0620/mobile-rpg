using System.Collections.Generic;
using UnityEngine;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Shared materials for glowing skill VFX (Resources/Shaders/SpriteGlow.shader).
    /// Plain alpha blending made bright energy effects look washed out and muddy
    /// over light grass; a partially additive blend keeps them luminous. Dust/rock
    /// effects (whirlwind, ground slam, dash, war-cry dirt ring) stay on the default
    /// sprite material. Falls back to the default material if the shader is missing.
    /// </summary>
    public static class VfxGlowMaterials
    {
        /// <summary>Mage energy effects (shield, teleport, thunder, ice, lightning).</summary>
        public const float Energy = 0.6f;

        /// <summary>Warrior light effects (sword slash, shield barrier).</summary>
        public const float Light = 0.45f;

        private static readonly Dictionary<float, Material> Cache = new Dictionary<float, Material>();
        private static Shader shader;
        private static bool shaderLookedUp;

        public static void Apply(SpriteRenderer renderer, float additive)
        {
            if (renderer == null || additive <= 0f) return;
            Material material = Get(additive);
            if (material != null) renderer.sharedMaterial = material;
        }

        private static Material Get(float additive)
        {
            if (Cache.TryGetValue(additive, out Material cached) && cached != null) return cached;
            if (!shaderLookedUp)
            {
                shader = Resources.Load<Shader>("Shaders/SpriteGlow");
                shaderLookedUp = true;
            }
            if (shader == null) return null;
            var material = new Material(shader) { name = "SpriteGlow_" + additive.ToString("0.00") };
            material.SetFloat("_Additive", additive);
            Cache[additive] = material;
            return material;
        }
    }
}
