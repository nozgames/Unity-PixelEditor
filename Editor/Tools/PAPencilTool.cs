using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    /// <summary>
    /// Implements a tool that draw pixels to the current layer using either 
    /// a foreground or background color depending on which mouse button is pressed.
    /// </summary>
    internal class PAPencilTool : PABrushTool
    {
        private static readonly Vector2 _cursorHotspot = new Vector2(0, 31);
        private Texture2D _cursor = null;

        public PAPencilTool(PAWorkspace workspace) : base(workspace)
        {
            _cursor = PAUtils.LoadCursor("Pencil.psd");
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Workspace.SetCursor(_cursor, _cursorHotspot);
        }

        protected override Color GetDrawColor(MouseButton button) =>
            button == MouseButton.RightMouse ? Workspace.BackgroundColor : Workspace.ForegroundColor;
    }
}
