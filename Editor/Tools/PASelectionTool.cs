using UnityEditor;
using UnityEngine;

namespace NoZ.PA
{

    internal class PASelectionTool : PATool
    {
        private static readonly Color BorderColor = new Color(0.22f, 0.58f, 0.78f, 1.0f);
        private static readonly Color FillColor = new Color(0.22f, 0.58f, 0.78f, 0.2f);
        private static readonly Color HandleColor = Color.white;
        private static readonly Color HandleBorderColor = Color.black;
        private static readonly Vector2 HandleSize = Vector2.one * 8.0f;

        private static readonly Vector2 _cursorHotspot = new Vector2(11, 19);
        private Texture2D _cursor = null;

        /// <summary>
        /// Pivot point of the selection
        /// </summary>
        private Vector2Int _pivot;

        /// <summary>
        /// Current selection
        /// </summary>
        public RectInt? Selection { get; set; } = null;

        /// <summary>
        /// True if there is an active selection
        /// </summary>
        public bool HasSelection => Selection != null;


        public PASelectionTool(PACanvas canvas) : base(canvas)
        {
            // Must move 1 pixel before drawing begins
            DrawThreshold = 1.0f;

            _cursor = PAUtils.LoadCursor("Crosshair.psd");
        }

        public override void OnDisable()
        {
            Selection = null;
            base.OnDisable();
        }

        public override void OnMouseDown(PAMouseEvent evt)
        {
            base.OnMouseDown(evt);

            Selection = null;
            MarkDirtyRepaint();
        }

        public override void OnMouseUp(PAMouseEvent evt)
        {
            if (evt.button == UnityEngine.UIElements.MouseButton.LeftMouse && Selection == null)
                Canvas.ClearSelection();

            base.OnMouseUp(evt);
        }

        public override void OnDrawStart(PADrawEvent evt)
        {
            // TODO: if the cursor is over the selection area then start moving the pixels


            _pivot = Canvas.ClampImagePosition(evt.imagePosition);
            MarkDirtyRepaint();
        }

        public override void OnDrawContinue(PADrawEvent evt)
        {
            var drawPosition = Canvas.ClampImagePosition(evt.imagePosition);
            var min = Vector2Int.Min(_pivot, drawPosition);
            var max = Vector2Int.Max(_pivot, drawPosition);
            Selection = new RectInt(min, max - min + Vector2Int.one);
            MarkDirtyRepaint();
        }

        public override void OnDrawEnd(PADrawEvent evt, bool cancelled)
        {
            base.OnDrawEnd(evt, cancelled);
            if (Selection != null)
            {
                var textureRect = Canvas.ImageToTexture(Selection.Value);
                if(evt.shift && evt.alt)
                    Canvas.SelectionMask.FillRect(textureRect, Color.clear);
                else
                {
                    if(!evt.shift)
                        Canvas.SelectionMask.Clear(Color.clear);
                    
                    Canvas.SelectionMask.FillRect(textureRect, Color.white);
                }

                Canvas.ApplySelectionMask();

                Selection = null;
            }
            else
                Canvas.ClearSelection();

            MarkDirtyRepaint();
        }

        protected override void OnRepaint() 
        {
            if (Selection.HasValue)
            {
                var min = Canvas.ImageToCanvas(Selection.Value.min);
                var max = Canvas.ImageToCanvas(Selection.Value.max);
                Handles.color = Color.white;
                Handles.DrawSolidRectangleWithOutline(new Rect(min, max - min), IsDrawing ? FillColor : Color.clear, BorderColor);
            }
            else if (Canvas.HasSelection)
            {
                var selection = Canvas.ImageToCanvas(Canvas.SelectionBounds);
                var min = selection.min;
                var max = selection.max;
                var center = selection.center;

                DrawHandle(min);
                DrawHandle(max);
                DrawHandle(new Vector2(min.x, max.y));
                DrawHandle(new Vector2(max.x, min.y));
                DrawHandle(new Vector2(min.x, center.y));
                DrawHandle(new Vector2(max.x, center.y));
                DrawHandle(new Vector2(center.x, min.y));
                DrawHandle(new Vector2(center.x, max.y));
            }
        }

        private void DrawHandle(Vector2 position) =>
            Handles.DrawSolidRectangleWithOutline(
                new Rect(position - HandleSize * 0.5f, HandleSize), HandleColor, HandleBorderColor);

        public override void SetCursor(Vector2 pos)
        {
            // TODO: see if the cursor is over any of the resize handles
            var rect = Canvas.ImageToCanvas(Canvas.SelectionBounds);
            var l = Mathf.Abs(rect.min.x - pos.x) < HandleSize.x;
            var r = Mathf.Abs(rect.max.x - pos.x) < HandleSize.x;
            var t = Mathf.Abs(rect.min.y - pos.y) < HandleSize.y;
            var b = Mathf.Abs(rect.max.y - pos.y) < HandleSize.y;
            var cy = Mathf.Abs(rect.center.y - pos.y) < HandleSize.y;
            var cx = Mathf.Abs(rect.center.x - pos.x) < HandleSize.x;

            if((l && t) || (r && b))
                Canvas.SetCursor(MouseCursor.ResizeUpLeft);
            else if ((l && b) || (r && t))
                Canvas.SetCursor(MouseCursor.ResizeUpRight);
            else if (cy && (l || r))
                Canvas.SetCursor(MouseCursor.ResizeHorizontal);
            else if (cx && (t || b))

                Canvas.SetCursor(MouseCursor.ResizeVertical);
            else if (Canvas.HasSelection && rect.Contains(pos))
                Canvas.SetCursor(MouseCursor.MoveArrow);
            else
                Canvas.SetCursor(_cursor, _cursorHotspot);
        }
    }
}
