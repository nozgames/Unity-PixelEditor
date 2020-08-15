using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    /// <summary>
    /// Implements a tool that draw pixels to the current layer using either 
    /// a foreground or background color depending on which mouse button is pressed.
    /// </summary>
    internal class PEPencilTool : PETool
    {
        private static readonly Vector2 _cursorHotspot = new Vector2(0, 31);
        private Texture2D _cursor = null;

        public PEPencilTool (PEWindow window) : base(window)
        {
            _cursor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PixelEditor/Editor/Cursors/Pencil.psd");
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Window.SetCursor(_cursor, _cursorHotspot);
        }

        private void DrawPixel (MouseButton button, Vector2Int canvasPosition)
        {
            if (canvasPosition.x < 0 || 
                canvasPosition.y < 0 ||
                canvasPosition.x >= Window.CanvasWidth ||
                canvasPosition.y >= Window.CanvasHeight)
                return;

            if (button == MouseButton.MiddleMouse)
                return;

            Window.CurrentTexture.texture.SetPixel(
                canvasPosition.x, 
                Window.CanvasHeight - 1 - canvasPosition.y,
                button == MouseButton.LeftMouse ? Window.ForegroundColor : Window.BackgroundColor);
            Window.CurrentTexture.texture.Apply();
            Window.Canvas.MarkDirtyRepaint();
        }

        public override void OnDrawStart(MouseButton button, Vector2Int canvasPosition)
        {
            DrawPixel(button, canvasPosition);
        }

        public override void OnDrawContinue(MouseButton button, Vector2Int canvasPosition)
        {
            DrawPixel(button, canvasPosition);
        }
    }
}
