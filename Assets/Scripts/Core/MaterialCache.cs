using System.Collections.Generic;
using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Cached materials for the procedural 3D parts. The project renders with
    // URP's 3D Universal Renderer (see Assets/Settings/Renderer3D.asset), so
    // world geometry uses real Lit materials and picks up the scene's
    // directional light + shadows — that shading is what makes primitive-built
    // models read as solid shapes instead of flat silhouettes.
    //
    // VFX and UI-ish world elements (swipe discs, glow rings, health bars)
    // deliberately stay Unlit so they keep a constant, punchy color no matter
    // where they are relative to the light.
    public static class MaterialCache
    {
        static readonly Dictionary<Color, Material> LitCache = new Dictionary<Color, Material>();
        static readonly Dictionary<(Color, float, float), Material> MetalCache = new Dictionary<(Color, float, float), Material>();
        static readonly Dictionary<Color, Material> UnlitCache = new Dictionary<Color, Material>();

        static Shader _litShader;
        static Shader _unlitShader;

        public static Material Get(Color color)
        {
            if (LitCache.TryGetValue(color, out var mat) && mat != null) return mat;

            mat = new Material(LitShader());
            ApplyBaseColor(mat, color);
            // Matte by default: these are furry/organic/rocky surfaces, and a
            // broad specular highlight on every sphere reads as plastic.
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Metallic", 0f);
            LitCache[color] = mat;
            return mat;
        }

        // For armor/weapon/crown modifier parts — an actual metallic response
        // is what sells bronze/silver/gold/diamond as distinct tiers.
        public static Material GetMetallic(Color color, float metallic = 0.9f, float smoothness = 0.65f)
        {
            var key = (color, metallic, smoothness);
            if (MetalCache.TryGetValue(key, out var mat) && mat != null) return mat;

            mat = new Material(LitShader());
            ApplyBaseColor(mat, color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            MetalCache[key] = mat;
            return mat;
        }

        public static Material GetUnlit(Color color)
        {
            if (UnlitCache.TryGetValue(color, out var mat) && mat != null) return mat;

            mat = new Material(UnlitShader());
            ApplyBaseColor(mat, color);
            UnlitCache[color] = mat;
            return mat;
        }

        // Emissive lit material — used for pickups/orbs so they glow against
        // the ground even in shadow.
        public static Material GetGlowing(Color color, float intensity = 1.6f)
        {
            var key = (color, intensity, -1f);
            if (MetalCache.TryGetValue(key, out var mat) && mat != null) return mat;

            mat = new Material(LitShader());
            ApplyBaseColor(mat, color);
            mat.SetFloat("_Smoothness", 0.4f);
            mat.SetFloat("_Metallic", 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            MetalCache[key] = mat;
            return mat;
        }

        static void ApplyBaseColor(Material mat, Color color)
        {
            // URP Lit/Unlit expose _BaseColor; Material.color routes to it via
            // the [MainColor] attribute, but setting both keeps this working
            // if the fallback (non-URP) shader is ever used instead.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
        }

        static Shader LitShader()
        {
            if (_litShader == null)
            {
                _litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (_litShader == null) _litShader = Shader.Find("Standard");
            }
            return _litShader;
        }

        static Shader UnlitShader()
        {
            if (_unlitShader == null)
            {
                _unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (_unlitShader == null) _unlitShader = Shader.Find("Unlit/Color");
            }
            return _unlitShader;
        }
    }
}
