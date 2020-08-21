using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    /// <summary>
    /// Implements a tool that erases pixels from the current layer
    /// </summary>
    internal class PAEraserTool : PABrushTool
    {
        private static readonly Vector2 _cursorHotspot = new Vector2(11, 19);
        private Texture2D _cursor = null;

        public PAEraserTool(PACanvas canvas) : base(canvas)
        {
            _cursor = PAUtils.LoadCursor("Crosshair.psd");
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Canvas.SetCursor(_cursor, _cursorHotspot);
        }

        protected override Color GetDrawColor(MouseButton button) => Color.clear;
    }
}
