using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{

    internal class PESelectionTool : PETool
    {
        private static readonly Color BorderColor = new Color(0.22f, 0.58f, 0.78f, 1.0f);
        private static readonly Color FillColor = new Color(0.22f, 0.58f, 0.78f, 0.2f);

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


        public PESelectionTool(PEWindow window) : base(window)
        {
            // Must move 1 pixel before drawing begins
            DrawThreshold = 1.0f;

            _cursor = PEUtils.LoadCursor("Crosshair.psd");
        }

        public override void OnDisable()
        {
            Selection = null;
            base.OnDisable();
        }

        public override void OnMouseDown(PEMouseEvent evt)
        {
            base.OnMouseDown(evt);

            Selection = null;
            MarkDirtyRepaint();
        }

        public override void OnDrawStart(PEDrawEvent evt)
        {
            _pivot = Window.ClampCanvasPosition(evt.canvasPosition);
            MarkDirtyRepaint();
        }

        public override void OnDrawContinue(PEDrawEvent evt)
        {
            var drawPosition = Window.ClampCanvasPosition(evt.canvasPosition);
            var min = Vector2Int.Min(_pivot, drawPosition);
            var max = Vector2Int.Max(_pivot, drawPosition);
            Selection = new RectInt(min, max - min + Vector2Int.one);
            MarkDirtyRepaint();
        }

        public override void OnDrawEnd(PEDrawEvent evt, bool cancelled)
        {
            base.OnDrawEnd(evt, cancelled);
            MarkDirtyRepaint();
        }

        protected override void OnRepaint() 
        {
            if (null == Selection)
                return;

            var min = Window.CanvasToWorkspace(Selection.Value.min);
            var max = Window.CanvasToWorkspace(Selection.Value.max);

            Handles.color = Color.white;
            Handles.DrawSolidRectangleWithOutline(new Rect(min, max-min), IsDrawing ? FillColor : Color.clear, BorderColor);

            if(!IsDrawing)
            {
                var gripSize = new Vector2(8, 8);
                Handles.DrawSolidRectangleWithOutline(new Rect(min - gripSize * 0.5f, gripSize), Color.white, BorderColor);
                Handles.DrawSolidRectangleWithOutline(new Rect(max - gripSize * 0.5f, gripSize), Color.white, BorderColor);
                Handles.DrawSolidRectangleWithOutline(new Rect(new Vector2(min.x-gripSize.x*0.5f, max.y-gripSize.y*0.5f), gripSize), Color.white, BorderColor);
                Handles.DrawSolidRectangleWithOutline(new Rect(new Vector2(max.x-gripSize.x*0.5f, min.y-gripSize.y*0.5f), gripSize), Color.white, BorderColor);
            }
            
        }

        public override void SetCursor(Vector2Int canvasPosition)
        {
            Window.SetCursor(_cursor, _cursorHotspot);
        }

        public override bool OnKeyDown(PEKeyEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Backspace:
                { 
                    if (Selection == null)
                        break;

                    var image = Window.CurrentFile.AddImage(Window.CurrentFrame, Window.CurrentLayer);
                    if (null != image)
                    {
                        image.texture.FillRect(
                            CanvasToTexture(Selection.Value), 
                            evt.ctrl ? Window.BackgroundColor : Window.ForegroundColor);

                        image.texture.Apply();
                        Window.Canvas.MarkDirtyRepaint();
                    }

                    return false;
                }

                case KeyCode.Delete:
                {
                    if (Selection != null)
                    {
                        var image = Window.CurrentFile.FindImage(Window.CurrentFrame, Window.CurrentLayer);
                        if (null != image)
                        {
                            image.texture.FillRect(CanvasToTexture(Selection.Value), Color.clear);
                            image.texture.Apply();
                            Window.Canvas.MarkDirtyRepaint();
                            return false;
                        }
                    }
                    break;
                }
            }

            return base.OnKeyDown(evt);
        }

    }
}
