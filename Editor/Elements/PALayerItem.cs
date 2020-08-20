using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PALayerItem : VisualElement
    {
        private PAWorkspace _workspace;
        private Image _preview = null;

        public PALayer Layer { get; private set; }

        public PALayerItem(PAWorkspace workspace, PALayer layer)
        {
            _workspace = workspace;
            Layer = layer;
            Layer.Item = this;

            pickingMode = PickingMode.Position;
            focusable = true;
            AddToClassList("layer");

            var visibilityToggle = new PAImageToggle();
            visibilityToggle.checkedImage = PAUtils.LoadImage("!scenevis_visible");
            visibilityToggle.uncheckedImage = PAUtils.LoadImage("!scenevis_hidden");
            visibilityToggle.value = layer.visible;
            visibilityToggle.tooltip = "Toggle layer visibility";
            visibilityToggle.onValueChanged = OnVisibilityChanged;
            Add(visibilityToggle);

            _preview = new Image() { name = "Preview" };
            Add(_preview);

            Add(new Label(layer.name));
        }

        private void OnVisibilityChanged(bool value)
        {
            Layer.visible = value;
            _workspace.RefreshCanvas();
        }

        public void RefreshPreview (PAFrame frame)
        {
            _preview.image = frame.File.FindImage(frame, Layer)?.texture;
            _preview.MarkDirtyRepaint();
        }
    }
}
