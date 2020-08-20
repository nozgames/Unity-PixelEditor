using UnityEngine;

namespace NoZ.PA
{
    internal static class ColorExtensions
    {
        /// <summary>
        /// Blend the source color into the destination color using alpha blending
        /// </summary>
        public static Color Blend (this Color dst, Color src)
        {
            var a = dst.a * (1.0f - src.a);
            var outA = src.a + a;
            a /= outA;
            return new Color(
                src.r * src.a + dst.r * a,
                src.g * src.a + dst.g * a,
                src.b * src.a + dst.b * a,
                outA
                ) ;
        }

        /// <summary>
        /// Multiply the alpha of the color by a given value
        /// </summary>
        public static Color MultiplyAlpha(this Color color, float alpha) =>
            new Color(color.r, color.g, color.b, color.a * alpha);
    }
}
