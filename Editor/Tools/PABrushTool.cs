using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    class PABrushTool : PATool
    {
        private Vector2Int? _drawPosition;
        private PAImage _target;
        private Color _drawColor;

        public PABrushTool(PACanvas canvas) : base(canvas) { }

        protected virtual Color GetDrawColor(MouseButton button) => Color.white;

        public override void OnEnable()
        {
            base.OnEnable();
            _drawPosition = null;
        }

        private void DrawTo(Vector2Int position)
        {
            Canvas.Workspace.Undo.Record ("Draw", _target.texture);

            _drawPosition = _drawPosition ?? position;

            position = CanvasToTexture(position);

            if (_drawPosition == position)
                _target.texture.SetPixelClamped(position, _drawColor);
            else
                _target.texture.DrawLine(_drawPosition.Value, position, _drawColor);

            _drawPosition = position;

            _target.texture.Apply();

            Canvas.RefreshImage(false);
        }

        public override void OnDrawStart(PADrawEvent evt)
        {
            if (!evt.shift || _drawPosition == null)
                _drawPosition = CanvasToTexture(evt.imagePosition);

            _drawColor = GetDrawColor(evt.button);
            _target = Canvas.File.AddImage(Canvas.SelectedFrame, Canvas.SelectedLayer);

            DrawTo(evt.imagePosition);
        }

        public override void OnDrawContinue(PADrawEvent evt) =>
            DrawTo(evt.imagePosition);

        public override void OnDrawEnd(PADrawEvent evt, bool cancelled)
        {
            _target = null;
            Canvas.RefreshImage();
        }
    }
}
