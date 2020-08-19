using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    /// <summary>
    /// Implements a tool that draw pixels to the current layer using either 
    /// a foreground or background color depending on which mouse button is pressed.
    /// </summary>
    internal class PEPencilTool : PEBrushTool
    {
        private static readonly Vector2 _cursorHotspot = new Vector2(0, 31);
        private Texture2D _cursor = null;

        public PEPencilTool(PEWindow window) : base(window)
        {
            _cursor = PEUtils.LoadCursor("Pencil.psd");
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Window.SetCursor(_cursor, _cursorHotspot);
        }

        protected override Color GetDrawColor(MouseButton button) =>
            button == MouseButton.RightMouse ? Window.BackgroundColor : Window.ForegroundColor;
    }
}
