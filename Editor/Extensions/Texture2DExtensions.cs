using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoZ.PA
{
    public static class Texture2DExtensions
    {
        /// <summary>
        /// Create a clone of the given texture.
        /// </summary>
        public static Texture2D Clone (this Texture2D texture)
        {
            var clone = new Texture2D(texture.width, texture.height, texture.format, false);
            clone.filterMode = texture.filterMode;
            clone.SetPixels32(texture.GetPixels32());
            clone.Apply();
            return clone;
        }

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
        /// Clear the texture with the given color using the given mask
        /// </summary>
        public static void Clear(this Texture2D dst, Color color, Texture2D mask)
        {
            if (mask == null || (mask.width != dst.width || mask.height != dst.height))
                throw new NotSupportedException();

            if (dst.format != TextureFormat.RGBA32)
                throw new NotSupportedException();

            for (int x = 0; x < dst.width; x++)
                for (int y = 0; y < dst.height; y++)
                    if(mask.GetPixel(x,y).a > 0)
                        dst.SetPixel(x, y, color);
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

        public static void SetPixelClamped(this Texture2D dst, int x, int y, Color color, Texture2D mask = null)
        {
            if (mask != null && (mask.width != dst.width || mask.height != dst.height))
                return;

            if (x < 0 || y < 0 || x >= dst.width || y >= dst.height)
                return;

            if (mask != null && mask.GetPixel(x, y).a <= 0)
                return;

            dst.SetPixel(x, y, color);
        }

        public static void SetPixelClamped(this Texture2D dst, Vector2Int p, Color color, Texture2D mask = null) =>
            dst.SetPixelClamped(p.x, p.y, color, mask);

        public static void DrawLine(this Texture2D dst, Vector2Int p1, Vector2Int p2, Color color, Texture2D mask = null) =>
            dst.DrawLine(p1.x, p1.y, p2.x, p2.y, color, mask);

        public static void DrawLine(this Texture2D dst, int x1, int y1, int x2, int y2, Color color, Texture2D mask = null) 
        {
            int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
            int dy = Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
            int err = (dx > dy ? dx : -dy) / 2, e2;
            for (; ; )
            {
                dst.SetPixelClamped(x1, y1, color, mask);
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

        /// <summary>
        /// Returns the minimum rectangle within the texture for the content of the texture.
        /// </summary>
        public static bool TryGetContentRect (this Texture2D texture, out RectInt contentRect)
        {
            var min = new Vector2Int(texture.width, texture.height);
            var max = Vector2Int.zero;

            // TODO: this is a brute force approach, there is likely a more efficient solution

            for(int y=0; y<texture.width; y++)
                for(int x =0; x< texture.height; x++)
                    if(texture.GetPixel(x,y) == Color.white)
                    {
                        min = Vector2Int.Min(min, new Vector2Int(x, y));
                        max = Vector2Int.Max(max, new Vector2Int(x, y));
                    }

            // If the maximum of the two vectors isnt the actual maximum value then
            // no selected pixels were found so return false to indicate no content rect was found
            if (Vector2Int.Max(max, min) != max)
            {
                contentRect = new RectInt(0, 0, 0, 0);
                return false;
            }

            contentRect = new RectInt(min, max - min + Vector2Int.one);
            return true;
        }

        /// <summary>
        /// Returns an array of point pairs that define line segments that outline the
        /// content of the given texture.
        /// </summary>
        public static Vector3[] GetContentOutline(this Texture2D texture, float z=0.0f)
        {
            // Generate mask buffer that contains one bit for each direction a line should be drawn at.
            var masks = new byte[texture.width * texture.height];
            var maskIndex = 0;
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++, maskIndex++)
                {
                    if (texture.GetPixel(x, y).a == 0)
                    {
                        masks[maskIndex] = 0;
                        continue;
                    }

                    // Top (1)
                    if (y == 0 || texture.GetPixel(x, y - 1).a == 0)
                        masks[maskIndex] |= 1;

                    // Bottom (2)
                    if (y >= texture.height - 1 || texture.GetPixel(x, y + 1).a == 0)
                        masks[maskIndex] |= 2;

                    // Left (4)
                    if (x == 0 || texture.GetPixel(x - 1, y).a == 0)
                        masks[maskIndex] |= 4;

                    // Right (8)
                    if (x >= texture.width - 1 || texture.GetPixel(x + 1, y).a == 0)
                        masks[maskIndex] |= 8;
                }

            // Convert the masked pixels into lines, combinding as many straight lines as possible
            var lines = new List<Vector3>();
            maskIndex = 0;
            for (int y = 0; y < texture.width; y++)
                for (int x = 0; x < texture.height; x++, maskIndex++)
                {
                    // Top
                    if ((masks[maskIndex] & 1) == 1)
                    {
                        lines.Add(new Vector3(x, y, z));
                        var maskIndex2 = maskIndex + 1;
                        var xx = x + 1;
                        for (; xx < texture.width && (masks[maskIndex2] & 1) == 1; xx++, maskIndex2++)
                            masks[maskIndex2] = (byte)(masks[maskIndex2] & ~1);
                        
                        lines.Add(new Vector3(xx, y, z));
                    }

                    // Bottom
                    if ((masks[maskIndex] & 2) == 2)
                    {
                        lines.Add(new Vector3(x, y + 1, z));
                        var maskIndex2 = maskIndex + 1;
                        var xx = x + 1;
                        for (; xx < texture.width && (masks[maskIndex2] & 2) == 2; xx++, maskIndex2++)
                            masks[maskIndex2] = (byte)(masks[maskIndex2] & ~2);

                        lines.Add(new Vector3(xx, y + 1, z));
                    }

                    // Left
                    if ((masks[maskIndex] & 4) == 4)
                    {
                        lines.Add(new Vector3(x, y, z));
                        var maskIndex2 = maskIndex + texture.width;
                        var yy = y + 1;
                        for (; yy < texture.height && (masks[maskIndex2] & 4) == 4; yy++, maskIndex2+=texture.width)
                            masks[maskIndex2] = (byte)(masks[maskIndex2] & ~4);

                        lines.Add(new Vector3(x, yy, z));
                    }

                    // Right
                    if ((masks[maskIndex] & 8) == 8)
                    {
                        lines.Add(new Vector3(x + 1, y, z));
                        var maskIndex2 = maskIndex + texture.width;
                        var yy = y + 1;
                        for (; yy < texture.height && (masks[maskIndex2] & 8) == 8; yy++, maskIndex2 += texture.width)
                            masks[maskIndex2] = (byte)(masks[maskIndex2] & ~8);

                        lines.Add(new Vector3(x + 1, yy, z));
                    }
                }

            return lines.ToArray();
        }
    }
}
