using UnityEngine;
using UnityEditor;

namespace NoZ.PA
{
    /// <summary>
    /// Pan the scroll view around
    /// </summary>
    internal class PAPanTool : PATool
    {
        private Vector2 _scrollStart;
        private Vector2 _mouseStart;

        public PAPanTool(PACanvas canvas) : base (canvas) 
        {
            IsEnabledDuringPlay = true;
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Canvas.SetCursor(MouseCursor.Pan);
        }

        public override void OnDrawStart(PADrawEvent evt)
        {
            _mouseStart = Canvas.Workspace.CanvasToViewport(evt.canvasPosition);
            _scrollStart = Canvas.Workspace.ViewportOffset;
        }

        public override void OnDrawContinue(PADrawEvent evt)
        {
            Canvas.Workspace.ViewportOffset = _scrollStart - (Canvas.Workspace.CanvasToViewport(evt.canvasPosition) - _mouseStart);
        }
    }
}
