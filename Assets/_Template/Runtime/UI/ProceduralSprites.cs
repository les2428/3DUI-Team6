using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Small textures made in code, drawn with the built-in UI shader. That shader is always in a web build, so these
    /// work without any material asset (a shader that only runtime code asks for can be stripped from the build).
    /// </summary>
    public static class ProceduralSprites
    {
        static Sprite disc, arrow;
        static readonly System.Collections.Generic.Dictionary<int, Sprite> vignettes = new System.Collections.Generic.Dictionary<int, Sprite>();

        /// <summary>A soft-edged white disc.</summary>
        public static Sprite Disc => disc != null ? disc : disc = Make(64, (x, y) => 1f - Edge(0.42f, 0.5f, Mathf.Sqrt(x * x + y * y)));

        /// <summary>A white arrow pointing up (+y): the "you are here" marker.</summary>
        public static Sprite Arrow => arrow != null ? arrow : arrow = Make(64, (x, y) =>
        {
            // Triangle with its tip at the top, notched at the bottom.
            float tip = 0.46f, bottom = -0.40f;
            if (y < bottom || y > tip) return 0f;
            float halfWidth = (tip - y) / (tip - bottom) * 0.36f;
            float notch = Mathf.Max(0f, 0.22f - Mathf.Abs(x) * 0.9f);       // V-cut in the base: reads as an arrow, not a cone
            bool inside = Mathf.Abs(x) <= halfWidth && y > bottom + notch;
            return inside ? 1f : 0f;
        });

        /// <summary>
        /// Black ring around a clear centre: transparent inside innerRadius, opaque from outerRadius
        /// (both as a fraction of the half-width; corners are opaque).
        /// </summary>
        public static Sprite Vignette(float innerRadius = 0.55f, float outerRadius = 0.9f)
        {
            int key = Mathf.RoundToInt(innerRadius * 1000f) * 10000 + Mathf.RoundToInt(outerRadius * 1000f);
            if (vignettes.TryGetValue(key, out var s) && s != null) return s;
            s = Make(256, (x, y) => Edge(innerRadius, outerRadius, Mathf.Sqrt(x * x + y * y) * 2f), black: true);
            vignettes[key] = s;
            return s;
        }

        /// <summary>0 below e0, 1 above e1, smooth in between (GLSL smoothstep; Mathf.SmoothStep is an interpolation instead).</summary>
        static float Edge(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>f(x, y) gets coordinates in -0.5..0.5 and returns the alpha.</summary>
        static Sprite Make(int size, System.Func<float, float, float> alpha, bool black = false)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            byte c = black ? (byte)0 : (byte)255;
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float a = Mathf.Clamp01(alpha((i + 0.5f) / size - 0.5f, (j + 0.5f) / size - 0.5f));
                    px[j * size + i] = new Color32(c, c, c, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
