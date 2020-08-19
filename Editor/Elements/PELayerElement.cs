using System;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    internal class PELayerElement : VisualElement
    {
        private PEWindow _window = null;
        private Image _preview = null;

        public PELayer Layer { get; private set; }

        public PELayerElement(PEWindow window, PELayer layer)
        {
            _window = window;
            Layer = layer;

            pickingMode = PickingMode.Position;
            focusable = true;
            AddToClassList("layer");

            var visibilityToggle = new PEImageToggle();
            visibilityToggle.checkedImage = PEUtils.LoadImage("!scenevis_visible");
            visibilityToggle.uncheckedImage = PEUtils.LoadImage("!scenevis_hidden");
            visibilityToggle.onValueChanged = OnVisibilityChanged;
            visibilityToggle.value = layer.visible;
            visibilityToggle.tooltip = "Toggle layer visibility";
            Add(visibilityToggle);

            _preview = new Image();
            _preview.AddToClassList("layerPreview");
            _preview.image = window.CurrentFile.FindImage(window.CurrentFile.frames[0], layer)?.texture;
            Add(_preview);

            Add(new Label(layer.name));
        }

        private void OnVisibilityChanged(bool value)
        {
            Layer.visible = value;
            _window.RefreshCanvas();
        }

        public void RefreshPreview ()
        {
            _preview.image = _window.CurrentFile.FindImage(_window.CurrentFrame, Layer)?.texture;
            _preview.MarkDirtyRepaint();
        }
    }
}
