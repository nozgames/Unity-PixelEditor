using System.Collections.Generic;

namespace NoZ.PixelEditor
{
    internal class PEAnimation
    {
        /// <summary>
        /// Unique identifier of the animation
        /// </summary>
        public string id;

        /// <summary>
        /// Name of the animation
        /// </summary>
        public string name;

        /// <summary>
        /// List of all textures in the animation
        /// </summary>
        public List<PETexture> textures = null;

        /// <summary>
        /// List of all frames in the animation
        /// </summary>
        public List<PEFrame> frames = null;
    }
}
