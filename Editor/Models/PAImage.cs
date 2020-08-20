using UnityEngine;

namespace NoZ.PA
{
    internal class PAImage
    {
        /// <summary>
        /// Frame the texure belongs to
        /// </summary>
        public PAFrame frame;

        /// <summary>
        /// Layer the texture belongs to
        /// </summary>
        public PALayer layer;

        /// <summary>
        /// Internal texture
        /// </summary>
        public Texture2D texture;
    }
}
