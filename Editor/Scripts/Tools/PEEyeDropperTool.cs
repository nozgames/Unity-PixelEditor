using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    /// <summary>
    /// Implements a tool that will set the foreground color to any pixel that is clicked
    /// with the left mouse button and background color to any pixel clicked with the
    /// right mouse button.
    /// </summary>
    internal class PEEyeDropperTool : PETool
    {
        private static readonly Vector2 _cursorHotspot = new Vector2(0, 31);
        private Texture2D _cursor = null;

        public PEEyeDropperTool(PEWindow window) : base(window)
        {
            _cursor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PixelEditor/Editor/Cursors/EyeDropper.psd");
        }

        public override void SetCursor(Vector2Int canvasPosition) =>
            Window.SetCursor(_cursor, _cursorHotspot);

        public override void OnDrawStart(MouseButton button, Vector2Int canvasPosition) =>
            SampleColor(button, canvasPosition);

        public override void OnDrawContinue(MouseButton button, Vector2Int canvasPosition) =>
            SampleColor(button, canvasPosition);

        /// <summary>
        /// Sample a color from the current canvas
        /// </summary>
        private void SampleColor (MouseButton button, Vector2Int canvasPosition)
        {
            if (canvasPosition.x < 0 ||
                canvasPosition.y < 0 ||
                canvasPosition.x >= Window.CanvasWidth ||
                canvasPosition.y >= Window.CanvasHeight)
                return;

            if (button == MouseButton.MiddleMouse)
                return;

            var target = Window.CurrentFile.FindImage(Window.CurrentFrame, Window.CurrentLayer);
            if (null == target)
                return;

            // TODO: option for sample all layers or sample current layer in toolbar
            var color = target.texture.GetPixel(
                canvasPosition.x,
                Window.CanvasHeight - 1 - canvasPosition.y);
            if (color == Color.clear)
                return;

            if (button == MouseButton.LeftMouse)
                Window.ForegroundColor = color;
            else
                Window.BackgroundColor = color;
        }
    }
}
