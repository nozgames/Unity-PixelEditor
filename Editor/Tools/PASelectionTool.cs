using UnityEditor;
using UnityEngine;

namespace NoZ.PA
{

    internal class PASelectionTool : PATool
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


        public PASelectionTool(PAWorkspace workspace) : base(workspace)
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

        public override void OnDrawStart(PADrawEvent evt)
        {
            _pivot = Workspace.ClampCanvasPosition(evt.canvasPosition);
            MarkDirtyRepaint();
        }

        public override void OnDrawContinue(PADrawEvent evt)
        {
            var drawPosition = Workspace.ClampCanvasPosition(evt.canvasPosition);
            var min = Vector2Int.Min(_pivot, drawPosition);
            var max = Vector2Int.Max(_pivot, drawPosition);
            Selection = new RectInt(min, max - min + Vector2Int.one);
            MarkDirtyRepaint();
        }

        public override void OnDrawEnd(PADrawEvent evt, bool cancelled)
        {
            base.OnDrawEnd(evt, cancelled);
            MarkDirtyRepaint();
        }

        protected override void OnRepaint() 
        {
            if (null == Selection)
                return;

            var min = Workspace.CanvasToWorkspace(Selection.Value.min);
            var max = Workspace.CanvasToWorkspace(Selection.Value.max);

            Handles.color = Color.white;
            Handles.DrawSolidRectangleWithOutline(new Rect(min, max-min), IsDrawing ? FillColor : Color.clear, BorderColor);

            if (!IsDrawing)
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
            Workspace.SetCursor(_cursor, _cursorHotspot);
        }

        public override bool OnKeyDown(PAKeyEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Backspace:
                { 
                    if (Selection == null)
                        break;

                    var image = Workspace.File.AddImage(Workspace.SelectedFrame, Workspace.SelectedLayer);
                    if (null != image)
                    {
                        image.texture.FillRect(
                            CanvasToTexture(Selection.Value), 
                            evt.ctrl ? Workspace.BackgroundColor : Workspace.ForegroundColor);

                        image.texture.Apply();
                        Workspace.RefreshCanvas();
                    }

                    return false;
                }

                case KeyCode.Delete:
                {
                    if (Selection != null)
                    {
                        var image = Workspace.File.FindImage(Workspace.SelectedFrame, Workspace.SelectedLayer);
                        if (null != image)
                        {
                            image.texture.FillRect(CanvasToTexture(Selection.Value), Color.clear);
                            image.texture.Apply();
                            Workspace.RefreshCanvas();
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
