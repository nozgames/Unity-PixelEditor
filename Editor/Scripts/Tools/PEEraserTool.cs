using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    /// <summary>
    /// Implements a tool that erases pixels from the current layer
    /// </summary>
    internal class PEEraserTool : PEBrushTool
    {
        private static readonly Vector2 _cursorHotspot = new Vector2(11, 19);
        private Texture2D _cursor = null;

        public PEEraserTool(PEWindow window) : base(window)
        {
            _cursor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PixelEditor/Editor/Cursors/Crosshair.psd");
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Window.SetCursor(_cursor, _cursorHotspot);
        }

        protected override Color GetDrawColor(MouseButton button) => Color.clear;
    }
}
