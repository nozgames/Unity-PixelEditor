using UnityEngine;

namespace NoZ.PixelEditor
{
    /// <summary>
    /// Pan the scroll view around
    /// </summary>
    internal class PEPanTool : PETool
    {
        private Vector2 _scrollStart;
        private Vector2 _mouseStart;

        public PEPanTool(PEWindow window) : base (window)
        {
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Window.SetCursor(UnityEditor.MouseCursor.Pan);
        }

        public override void OnDrawStart(PEDrawEvent evt)
        {
            _mouseStart = Window.WorkspaceToScrollView(evt.workspacePosition);
            _scrollStart = Window.ScrollOffset;
        }

        public override void OnDrawContinue(PEDrawEvent evt)
        {
            Window.ScrollOffset = _scrollStart - (Window.WorkspaceToScrollView(evt.workspacePosition - _mouseStart));
        }
    }
}
