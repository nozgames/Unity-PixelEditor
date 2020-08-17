using System;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

namespace NoZ.PixelEditor
{
    public static class Texture2DExtensions
    {
        /// <summary>
        /// Clear the texture with the given color
        /// </summary>
        public static void Clear (this Texture2D texture, Color color)
        {
            if (texture.format != TextureFormat.RGBA32)
                throw new NotSupportedException();

            for (int x = 0; x < texture.width; x++)
                for (int y = 0; y < texture.height; y++)
                    texture.SetPixel(x, y, color);
        }

        /// <summary>
        /// Blend a texture into the current texture
        /// </summary>
        public static void Blend (this Texture2D dst, Texture2D src, float opacity=1.0f)
        {
            if (src == null)
                throw new ArgumentNullException("src");

            if (dst.width != src.width || dst.height != src.height)
                throw new ArgumentException("size mismatch");

            if (dst.format != TextureFormat.RGBA32 || src.format != TextureFormat.RGBA32)
                throw new NotSupportedException("texture format not supported");

            if (!dst.isReadable | !src.isReadable)
                throw new ArgumentException("both textures must be readable");

            for (int x = 0; x < dst.width; x++)
                for (int y = 0; y < dst.height; y++)
                    dst.SetPixel(x, y, dst.GetPixel(x, y).Blend(src.GetPixel(x, y).MultiplyAlpha(opacity)));            
        }

        public static void SetPixelClamped(this Texture2D dst, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= dst.width || y >= dst.height)
                return;

            dst.SetPixel(x, y, color);
        }

        public static void SetPixelClamped(this Texture2D dst, Vector2Int p, Color color) =>
            dst.SetPixelClamped(p.x, p.y, color);

        public static void DrawLine(this Texture2D dst, Vector2Int p1, Vector2Int p2, Color color) =>
            dst.DrawLine(p1.x, p1.y, p2.x, p2.y, color);

        public static void DrawLine(this Texture2D dst, int x1, int y1, int x2, int y2, Color color) 
        {
            int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
            int dy = Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
            int err = (dx > dy ? dx : -dy) / 2, e2;
            for (; ; )
            {
                dst.SetPixelClamped(x1, y1, color);
                if (x1 == x2 && y1 == y2) break;
                e2 = err;
                if (e2 > -dx) { err -= dy; x1 += sx; }
                if (e2 < dy) { err += dx; y1 += sy; }
            }
        }

        /// <summary>
        /// Fill a rectangle with the given color
        /// </summary>
        public static void FillRect (this Texture2D dst, RectInt rect, Color color)
        {
            for (int y = 0; y < rect.height; y++)
                for (int x = 0; x < rect.width; x++)
                    dst.SetPixelClamped(rect.xMin + x, rect.yMin + y, color);
        }
    }
}
