using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    /// <summary>
    /// Implements a tool that erases pixels from the current layer
    /// </summary>
    internal class PEEraserTool : PETool
    {
        private static readonly Vector2 _cursorHotspot = new Vector2(0, 31);
        private Texture2D _cursor = null;
        private PEImage _target = null;

        public PEEraserTool (PEWindow window) : base(window)
        {
            _cursor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PixelEditor/Editor/Cursors/Pencil.psd");
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Window.SetCursor(_cursor, _cursorHotspot);
        }

        private void ErasePixel (MouseButton button, Vector2Int canvasPosition)
        {
            if (canvasPosition.x < 0 ||
                canvasPosition.y < 0 ||
                canvasPosition.x >= Window.CanvasWidth ||
                canvasPosition.y >= Window.CanvasHeight)
                return;

            if (button == MouseButton.MiddleMouse)
                return;

            _target = _target ?? Window.CurrentFile.AddImage(Window.CurrentFrame, Window.CurrentLayer);

            _target.texture.SetPixel(
                canvasPosition.x,
                Window.CanvasHeight - 1 - canvasPosition.y,
                Color.clear);
            _target.texture.Apply();
            Window.Canvas.MarkDirtyRepaint();
        }

        public override void OnDrawStart(MouseButton button, Vector2Int canvasPosition) =>
            ErasePixel(button, canvasPosition);

        public override void OnDrawContinue(MouseButton button, Vector2Int canvasPosition) =>
            ErasePixel(button, canvasPosition);

        public override void OnDrawEnd(MouseButton button, Vector2Int canvasPosition) =>
            _target = null;

        public override void OnDrawCancel(MouseButton button) =>
            _target = null;
    }
}
