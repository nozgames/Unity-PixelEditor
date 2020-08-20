using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    class PABrushTool : PATool
    {
        private Vector2Int? _drawPosition;
        private PAImage _target;
        private Color _drawColor;

        public PABrushTool(PAWorkspace workspace) : base(workspace)
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

            Workspace.RefreshCanvas();
        }

        public override void OnDrawStart(PADrawEvent evt)
        {
            if (!evt.shift || _drawPosition == null)
                _drawPosition = CanvasToTexture(evt.canvasPosition);

            _drawColor = GetDrawColor(evt.button);
            _target = Workspace.File.AddImage(Workspace.SelectedFrame, Workspace.SelectedLayer);
            DrawTo(evt.canvasPosition);
        }

        public override void OnDrawContinue(PADrawEvent evt) =>
            DrawTo(evt.canvasPosition);

        public override void OnDrawEnd(PADrawEvent evt, bool cancelled)
        {
            _target = null;
            Workspace.RefreshCanvas();
        }
    }
}
