using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    class PEBrushTool : PETool
    {
        private Vector2Int? _drawPosition;
        private PEImage _target;
        private Color _drawColor;

        public PEBrushTool(PEWindow window) : base(window)
        {
        }

        protected virtual Color GetDrawColor(MouseButton button) => Color.white;

        public override void OnEnable()
        {
            base.OnEnable();
            _drawPosition = null;
        }

        private void DrawTo(Vector2Int position)
        {
            _drawPosition = _drawPosition ?? position;

            position = CanvasToTexture(position);

            if (_drawPosition == position)
                _target.texture.SetPixelClamped(position, _drawColor);
            else
                _target.texture.DrawLine(_drawPosition.Value, position, _drawColor);

            _drawPosition = position;

            _target.texture.Apply();
            Window.Canvas.MarkDirtyRepaint();
        }

        public override void OnDrawStart(PEDrawEvent evt)
        {
            if (!evt.shift || _drawPosition == null)
                _drawPosition = CanvasToTexture(evt.canvasPosition);

            _drawColor = GetDrawColor(evt.button);
            _target = Window.CurrentFile.AddImage(Window.CurrentFrame, Window.CurrentLayer);
            DrawTo(evt.canvasPosition);
        }

        public override void OnDrawContinue(PEDrawEvent evt) =>
            DrawTo(evt.canvasPosition);

        public override void OnDrawEnd(PEDrawEvent evt, bool cancelled)
        {
            _target = null;
            Window.RefreshCanvas();
        }
    }
}
