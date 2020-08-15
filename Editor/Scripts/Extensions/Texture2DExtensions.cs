using System;
using UnityEngine;

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
    }
}
