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

        public PAPanTool(PAWorkspace workspace) : base (workspace)
        {
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Workspace.SetCursor(MouseCursor.Pan);
        }

        public override void OnDrawStart(PADrawEvent evt)
        {
            _mouseStart = Workspace.WorkspaceToScrollView(evt.workspacePosition);
            _scrollStart = Workspace.ScrollOffset;
        }

        public override void OnDrawContinue(PADrawEvent evt)
        {
            Workspace.ScrollOffset = _scrollStart - (Workspace.WorkspaceToScrollView(evt.workspacePosition) - _mouseStart);
        }
    }
}
