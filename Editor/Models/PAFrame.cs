
using UnityEngine;

namespace NoZ.PA
{
    internal class PAFrame
    {
        public PAFile File { get; private set; }

        public PAFrame(PAFile file)
        {
            File = file;
        }

        /// <summary>
        /// Unique identifier of the frame
        /// </summary>
        public string id;

        /// <summary>
        /// Animation the frame belongs to
        /// </summary>
        public PAAnimation animation;

        /// <summary>
        /// Order of the frame within its parent animation
        /// </summary>
        public int order;

        /// <summary>
        /// Link to the frame item element that represents this frame
        /// </summary>
        public PAFrameItem Item { get; set; }

        /// <summary>
        /// Helper function to render a frame
        /// </summary>
        public Texture2D Render() => File.RenderFrame(this);
    }
}
