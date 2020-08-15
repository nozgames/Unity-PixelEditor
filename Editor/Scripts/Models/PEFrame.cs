
namespace NoZ.PixelEditor
{
    internal class PEFrame
    {
        /// <summary>
        /// Unique identifier of the frame
        /// </summary>
        public string id;

        /// <summary>
        /// Animation the frame belongs to
        /// </summary>
        public PEAnimation animation;

        /// <summary>
        /// Order of the frame within its parent animation
        /// </summary>
        public int order;
    }
}
