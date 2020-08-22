using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PALayerItem : VisualElement
    {
        private PACanvas _canvas;
        private Image _preview = null;

        public PALayer Layer { get; private set; }

        public PALayerItem(PACanvas canvas, PALayer layer)
        {
            _canvas = canvas;
            Layer = layer;
            Layer.Item = this;

            pickingMode = PickingMode.Position;
            focusable = true;
            AddToClassList("layer");

            Add(PAUtils.CreateIconToggle("LayerVisibilityToggle", "Toggle layer visibility", OnVisibilityChanged));

            _preview = new Image() { name = "Preview" };
            Add(_preview);

            Add(new Label(layer.name));
        }

        private void OnVisibilityChanged(bool value)
        {
            _canvas.Workspace.Undo.Record("Layer Visibility");
            Layer.visible = value;
            _canvas.RefreshImage();
        }

        public void RefreshPreview (PAFrame frame)
        {
            _preview.image = frame.File.FindImage(frame, Layer)?.texture;
            _preview.MarkDirtyRepaint();
        }
    }
}
