using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    /// <summary>
    /// Element that renders a grid line for each pixel
    /// </summary>
    internal class PAGrid : ImmediateModeElement
    {
        private const float ZoomAlphaZero = 2.0f;
        private const float ZoomAlphaOne = 10.0f;
        private const float AlphaMin = 0.0f;
        private const float AlphaMax = 0.5f;

        private PAWorkspace _workspace;
        private Color _color;

        public bool ShowBorder { get; set; } = true;
        public bool ShowPixels { get; set; } = true;

        public PAGrid(PAWorkspace workspace)
        {
            _workspace = workspace;

            AddToClassList("pixelGrid");

            style.position = new StyleEnum<Position>(Position.Absolute);
            style.left = 0;
            style.right = 0;
            style.bottom = 0;
            style.top = 0;

            // Get the grid line color from the prefs
            var rgb = EditorPrefs.GetString("Scene/Grid").Split(';').Skip(1).Select(s => float.Parse(s)).ToArray();
            _color = new Color(rgb[0], rgb[1], rgb[2]);
        }

        protected override void ImmediateRepaint()
        {
            if (_workspace.CanvasHeight == 0 || _workspace.CanvasWidth == 0)
                return;

            var alpha = Mathf.Clamp((_workspace.Zoom - ZoomAlphaZero) / (ZoomAlphaOne - ZoomAlphaZero), AlphaMin, AlphaMax);
            var size = new Vector2(_workspace.CanvasWidth  * _workspace.Zoom, _workspace.CanvasHeight * _workspace.Zoom);
            var center = contentRect.center;

            GUI.BeginClip(contentRect);

            // Draw the grid
            if(ShowPixels)
            {
                if (alpha > 0.0f)
                {
                    Handles.color = new Color(_color.r, _color.g, _color.b, alpha);
                    DrawLines(center, size, 0, 1, _workspace.CanvasHeight, 1);
                    DrawLines(center, size, 1, 1, _workspace.CanvasWidth, 1);
                }
            }

            // Draw the border
            if (ShowBorder || ShowPixels)
            {
                Handles.color = _color;
                DrawLines(center, size, 0, 0, _workspace.CanvasHeight + 1, _workspace.CanvasHeight);
                DrawLines(center, size, 1, 0, _workspace.CanvasWidth + 1, _workspace.CanvasWidth);
            }

            GUI.EndClip();
        }

        private void DrawLines(Vector3 center, Vector2 size, int axis0, int start, int end, int increment)
        {
            var axis1 = (axis0 + 1) % 2;
            var hsize0 = size[axis0] * 0.5f;
            var hsize1 = size[axis1] * 0.5f;
            var min0 = center[axis0] - hsize0;
            var max0 = center[axis0] + hsize0;

            for (int i = start; i < end; i += increment)
            {
                var coord1 = center[axis1] - hsize1 + i * _workspace.Zoom;
                var from = Vector2.zero;
                var to = Vector2.zero;
                from[axis0] = min0;
                from[axis1] = coord1;
                to[axis0] = max0;
                to[axis1] = coord1;

                Handles.DrawLine(from, to, 0.5f);
            }
        }
    }
}
