using System.Collections.Generic;
using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Cached unlit colored materials for procedural 3D parts — avoids needing
    // textures, and sidesteps needing the project's URP renderer reconfigured
    // for 3D lighting (the project's 2D Renderer doesn't process 3D lights).
    public static class MaterialCache
    {
        static readonly Dictionary<Color, Material> Cache = new Dictionary<Color, Material>();
        static Shader _shader;

        public static Material Get(Color color)
        {
            if (Cache.TryGetValue(color, out var mat) && mat != null) return mat;

            if (_shader == null)
            {
                _shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (_shader == null) _shader = Shader.Find("Unlit/Color");
            }

            mat = new Material(_shader) { color = color };
            Cache[color] = mat;
            return mat;
        }
    }
}
