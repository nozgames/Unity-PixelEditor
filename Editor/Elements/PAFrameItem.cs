using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PAFrameItem : VisualElement
    {
        private Image _preview = null;

        public Texture2D Preview => (Texture2D)_preview.image;

        public PAFrame Frame { get; private set; }

        public PAFrameItem(PAFrame frame)
        {
            Frame = frame;
            Frame.Item = this;            

            AddToClassList("frame");

            _preview = new Image { image = frame.Render() };
            Add(_preview);
        }

        public void RefreshPreview()
        {
            _preview.image = Frame.Render();
            _preview.MarkDirtyRepaint();
        }        
    }
}
