namespace NoZ.PixelEditor
{
    internal class PELayer 
    {
        /// <summary>
        /// Unique identifier of the layer
        /// </summary>
        public string id;

        /// <summary>
        /// Name of the layer
        /// </summary>
        public string name;

        /// <summary>
        /// Order of the layer 
        /// </summary>
        public int order;

        /// <summary>
        /// Layer opacity
        /// </summary>
        public float opacity = 1.0f;
    }
}
