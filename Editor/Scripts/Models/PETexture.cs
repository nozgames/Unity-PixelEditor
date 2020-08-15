using UnityEngine;

namespace NoZ.PixelEditor
{
    internal class PETexture
    {
        /// <summary>
        /// Frame the texure belongs to
        /// </summary>
        public PEFrame frame;

        /// <summary>
        /// Layer the texture belongs to
        /// </summary>
        public PELayer layer;

        /// <summary>
        /// Internal texture
        /// </summary>
        public Texture2D texture;
    }
}
