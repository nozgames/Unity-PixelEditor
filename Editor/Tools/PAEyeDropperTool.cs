using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    /// <summary>
    /// Implements a tool that will set the foreground color to any pixel that is clicked
    /// with the left mouse button and background color to any pixel clicked with the
    /// right mouse button.
    /// </summary>
    internal class PAEyeDropperTool : PATool
    {
        private static readonly Vector2 _cursorHotspot = new Vector2(0, 31);
        private Texture2D _cursor = null;

        public PAEyeDropperTool(PACanvas canvas) : base(canvas)
        {
            _cursor = PAUtils.LoadCursor("EyeDropper.psd");
        }

        public override void SetCursor(Vector2Int canvasPosition) =>
            Canvas.SetCursor(_cursor, _cursorHotspot);

        public override void OnDrawStart(PADrawEvent evt) =>
            SampleColor(evt.button, evt.imagePosition);

        public override void OnDrawContinue(PADrawEvent evt) =>
            SampleColor(evt.button, evt.imagePosition);

        /// <summary>
        /// Sample a color from the current canvas
        /// </summary>
        private void SampleColor (MouseButton button, Vector2Int canvasPosition)
        {
            if (canvasPosition.x < 0 ||
                canvasPosition.y < 0 ||
                canvasPosition.x >= Canvas.ImageWidth ||
                canvasPosition.y >= Canvas.ImageHeight)
                return;

            if (button == MouseButton.MiddleMouse)
                return;

            var target = Canvas.File.FindImage(Canvas.SelectedFrame, Canvas.SelectedLayer);
            if (null == target)
                return;

            // TODO: option for sample all layers or sample current layer in toolbar
            var color = target.texture.GetPixel(
                canvasPosition.x,
                Canvas.ImageHeight - 1 - canvasPosition.y);
            if (color == Color.clear)
                return;

            if (button == MouseButton.LeftMouse)
                Canvas.ForegroundColor = color;
            else
                Canvas.BackgroundColor = color;
        }
    }
}
