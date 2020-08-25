using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    /// <summary>
    /// Element that renders the selection outline
    /// </summary>
    internal class PASelectionOutline : ImmediateModeElement
    {
        private static readonly Color LineColor1 = Color.black; //  new Color(0.22f, 0.58f, 0.78f, 1.0f);
        private static readonly Color LineColor2 = Color.white; //  new Color(0.22f, 0.58f, 0.78f, 1.0f);
        private const float ZoomAlphaZero = 2.0f;
        private const float ZoomAlphaOne = 10.0f;
        private const float AlphaMin = 0.0f;
        private const float AlphaMax = 0.5f;

        private PACanvas _canvas;

        public PASelectionOutline(PACanvas canvas)
        {
            _canvas = canvas;
            pickingMode = PickingMode.Ignore;
        }

        protected override void ImmediateRepaint()
        {
            if (!_canvas.HasSelection)
                return;

            if (_canvas.SelectionOutline == null || _canvas.SelectionOutline.Length <= 0)
                return;

            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(_canvas.Zoom * Vector3.one);
            Handles.color = LineColor1;
            Handles.DrawLines(_canvas.SelectionOutline);
            Handles.color = LineColor2;
            Handles.DrawDottedLines(_canvas.SelectionOutline, 1);
            GUI.matrix = oldMatrix;
        }
    }
}
