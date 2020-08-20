namespace NoZ.PA
{
    internal class PALayer 
    {
        public PAFile File { get; private set; }

        public PALayer (PAFile file)
        {
            File = file;
        }

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

        /// <summary>
        /// True if the layer is visible
        /// </summary>
        public bool visible = true;

        /// <summary>
        /// Link to the layer item elemnet that represents this layer
        /// </summary>
        public PALayerItem Item { get; set; }
    }
}
