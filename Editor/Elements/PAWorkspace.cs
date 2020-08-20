using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PAWorkspace : VisualElement
    {
        public const float ZoomMin = 1.0f;
        public const float ZoomMax = 50.0f;
        private const float ZoomIncrementUp = 1.1f;
        private const float ZoomIncrementDown = 1.0f / ZoomIncrementUp;

        private VisualElement _content;
        private PALayer _selectedLayer;
        private PAFrame _selectedFrame;
        private ScrollView _scrollView;
        private Color _foregroundColor;
        private Color _backgroundColor;
        private float _zoom;
        private PAFile _file;
        private int _drawButton = -1;
        private Vector2 _drawStart;
        private Vector2 _drawLast;
        private Vector2 _lastMousePosition;
        private PATool _previousTool;
        private PATool _selectedTool;
        private WorkspaceCursorManager _workspaceCursor = null;

        public event Action ZoomChangedEvent;
        public event Action ToolChangedEvent;
        public event Action SelectedLayerChangedEvent;
        public event Action SelectedFrameChangedEvent;
        public event Action ForegroundColorChangedEvent;
        public event Action BackgroundColorChangedEvent;

        public PACanvas Canvas { get; private set; }
        public PAGrid Grid { get; private set; }
        public PAEyeDropperTool EyeDropperTool { get; private set; }
        public PAPencilTool PencilTool { get; private set; }
        public PAEraserTool EraserTool { get; private set; }
        public PASelectionTool SelectionTool { get; private set; }
        public PAPanTool PanTool { get; private set; }

        public PixelArt Target { get; private set; }
        
        public PAFile File {
            get => _file;
            set {
                if (_file == value)
                    return;

                _file = value;

                SelectedFrame = _file.frames[0];
                SelectedLayer = _file.layers[0];
            }
        }

        public Vector2 ViewportSize => new Vector2(
            _scrollView.contentViewport.contentRect.width,
            _scrollView.contentViewport.contentRect.height);

        public Vector2 Size {
            get => new Vector2(
                _scrollView.contentContainer.style.width.value.value,
                _scrollView.contentContainer.style.height.value.value);
            
            set {
                _scrollView.contentContainer.style.width = value.x;
                _scrollView.contentContainer.style.height = value.y;
            }
        }

        public PAAnimation CurrentAnimation => SelectedFrame?.animation;

        /// <summary>
        /// Returns true if a drawing operation is currently in progress
        /// </summary>
        public bool IsDrawing { get; private set; }

        /// <summary>
        /// Current selected tool
        /// </summary>
        public PATool SelectedTool {
            get => _selectedTool;
            set {
                if (_selectedTool == value)
                    return;

                _previousTool = _selectedTool;

                if (_selectedTool != null)
                {
                    _selectedTool.visible = false;
                    _selectedTool.OnDisable();
                }

                _selectedTool = value;
                if (_selectedTool != null)
                {
                    _selectedTool.visible = true;
                    _selectedTool.OnEnable();
                    _selectedTool.MarkDirtyRepaint();
                }

                ToolChangedEvent?.Invoke();

                RefreshCursor();
            }
        }

        /// <summary>
        /// Layer that is currently selected in the workspace
        /// </summary>
        public PALayer SelectedLayer {
            get => _selectedLayer;
            set {
                if (_selectedLayer == value)
                    return;

                _selectedLayer = value;

                SelectedLayerChangedEvent?.Invoke();

                RefreshCanvas();
            }
        }

        /// <summary>
        /// Frame that is currently selected in the workspace
        /// </summary>
        public PAFrame SelectedFrame {
            get => _selectedFrame;
            set {
                if (value == _selectedFrame)
                    return;

                _selectedFrame = value;

                SelectedFrameChangedEvent?.Invoke();

                RefreshCanvas();
            }
        }

        public Vector2 ScrollOffset {
            get => _scrollView.scrollOffset;
            set {
                _scrollView.scrollOffset = value;
                _scrollView.MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Forground color for drawing
        /// </summary>
        public Color ForegroundColor {
            get => _foregroundColor;
            set {
                if (_foregroundColor == value)
                    return;
                
                _foregroundColor = value;

                ForegroundColorChangedEvent?.Invoke();
            }
        }

        /// <summary>
        /// Background color for drawing
        /// </summary>
        public Color BackgroundColor {
            get => _backgroundColor;
            set {
                if (_backgroundColor == value)
                    return;

                _backgroundColor = value;

                BackgroundColorChangedEvent?.Invoke();
            }
        }

        /// <summary>
        /// Current zoom level
        /// </summary>
        public float Zoom => _zoom;

        /// <summary>
        /// Handle gui messages
        /// </summary>
        internal void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
                UpdateScrollView();
        }

        /// <summary>
        /// Width of the canvas in pixels
        /// </summary>
        public int CanvasWidth => File?.width ?? 0;

        /// <summary>
        /// Height of the canvas in pixels
        /// </summary>
        public int CanvasHeight => File?.height ?? 0;

        /// <summary>
        /// Size of the canvas in pixels
        /// </summary>
        public Vector2Int CanvasSize => new Vector2Int(CanvasWidth, CanvasHeight);

        /// <summary>
        /// Returns the canvas rectangle in workspace coordinates
        /// </summary>
        public Rect CanvasRect =>
            new Rect(_content.contentRect.min + _content.contentRect.size * 0.5f - (Vector2)CanvasSize * Zoom * 0.5f, (Vector2)CanvasSize * Zoom);

        public PAWorkspace()
        {            
            // Canvas is within the scrollview
            _scrollView = new ScrollView();
            _scrollView.StretchToParentSize();
            Add(_scrollView);

            _content = new VisualElement();
            _content.StretchToParentSize();
            _content.RegisterCallback<WheelEvent>(OnWheel);
            _content.RegisterCallback<MouseDownEvent>(OnMouseDown);
            _content.RegisterCallback<MouseUpEvent>(OnMouseUp);
            _content.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _content.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
            _content.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            _content.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            _scrollView.Add(_content);

            // Canvas
            Canvas = new PACanvas(this) { name = "Canvas" };
            Canvas.pickingMode = PickingMode.Ignore;
            Canvas.MarkDirtyRepaint();
            _content.Add(Canvas);

            // Pixel grid
            Grid = new PAGrid(this);
            _content.Add(Grid);

            // Create the tools
            PencilTool = new PAPencilTool(this);
            _content.Add(PencilTool);

            EraserTool = new PAEraserTool(this);
            _content.Add(EraserTool);

            EyeDropperTool = new PAEyeDropperTool(this);
            _content.Add(EraserTool);

            SelectionTool = new PASelectionTool(this);
            _content.Add(SelectionTool);

            PanTool = new PAPanTool(this);
            _content.Add(PanTool);

            // Create an element to manage the workspace cursor
            _workspaceCursor = new WorkspaceCursorManager();
            _workspaceCursor.StretchToParentSize();
            _workspaceCursor.pickingMode = PickingMode.Ignore;
            _content.Add(_workspaceCursor);
        }

        /// <summary>
        /// Refresh the canvas renderer
        /// </summary>
        public void RefreshCanvas(bool includePreviews=true)
        {
            if(includePreviews)
            {
                // Update frame preview of selected frame
                SelectedFrame.Item?.RefreshPreview();

                // Update all layer previews
                foreach (var layer in File.layers)
                    layer.Item?.RefreshPreview(SelectedFrame);
            }

            Canvas.MarkDirtyRepaint();
        }

        /// <summary>
        /// Update the scrollview size
        /// </summary>
        private void UpdateScrollView()
        {
            if (_scrollView == null)
                return;

            // Dont update the scrollview until it has been laid out
            if (float.IsNaN(ViewportSize.x) ||
                float.IsNaN(ViewportSize.y))
                return;

            // Set the new workspace size
            Size = Vector2.Max(
                ViewportSize * 2.0f - (Vector2)CanvasSize * Zoom,
                (Vector2)CanvasSize * Zoom + ViewportSize);
        }

        /// <summary>
        /// Set the Zoom and ScrollOffset such that the content fits to the scroll view area.
        /// </summary>
        public void ZoomToFit()
        {
            // If the scrollview isnt ready yet then wait till it is.  This mainly happens on 
            // a file load right when the window is opening.
            if (float.IsNaN(_scrollView.contentViewport.contentRect.width) ||
               float.IsNaN(_scrollView.contentViewport.contentRect.height))
            {
                UpdateScrollView();
                _scrollView.schedule.Execute(ZoomToFit);
                return;
            }

            // Set the new zoom level
            var zoom = _scrollView.contentViewport.contentRect.size * 0.9f / (Vector2)CanvasSize;
            SetZoom(Mathf.Min(zoom.x, zoom.y), Vector2.zero);

            // Offset the scroll view to center the content
            ScrollOffset = (Size - ViewportSize) * 0.5f;

            RefreshCanvas();
        }

        /// <summary>
        /// Helper function to clamp the given canvas position to the cavnas
        /// </summary>
        public Vector2Int ClampCanvasPosition(Vector2Int canvasPosition) =>
            new Vector2Int(Mathf.Clamp(canvasPosition.x, 0, CanvasWidth - 1), Mathf.Clamp(canvasPosition.y, 0, CanvasHeight - 1));

        /// <summary>
        /// Convert the given canvas position to a workspace position
        /// </summary>
        public Vector2 CanvasToWorkspace(Vector2Int canvasPosition) =>
            CanvasRect.min + (Vector2)canvasPosition * Zoom;

        /// <summary>
        /// Convert a coordinate from the workspace to the canvas.  Note that this 
        /// coordinate is not clamped to the canvas, use ClampCanvasPosition to do so.
        /// </summary>
        public Vector2Int WorkspaceToCanvas(Vector2 workspacePosition)
        {
            workspacePosition -= _content.contentRect.center;
            workspacePosition /= new Vector2(CanvasWidth * Zoom, CanvasHeight * Zoom);
            workspacePosition += new Vector2(0.5f, 0.5f);
            workspacePosition *= new Vector2(CanvasWidth, CanvasHeight);
            return new Vector2Int(
                (int)Mathf.Floor(workspacePosition.x),
                (int)Mathf.Floor(workspacePosition.y));
        }

        /// <summary>
        /// Convert a workspace coordinate into a coordinate within the scroll view
        /// </summary>
        public Vector2 WorkspaceToScrollView(Vector2 workspacePosition) =>
            _content.ChangeCoordinatesTo(_scrollView.contentViewport, workspacePosition);

        public Vector2 ViewportToWorkspace(Vector2 viewportPosition) => ScrollOffset + viewportPosition;

        public void SetCursor(MouseCursor cursor) => _workspaceCursor.Cursor = cursor;

        public void SetCursor(Texture2D texture, Vector2 hotspot) => _workspaceCursor.SetCursor(texture, hotspot);

        public Vector2 SetZoom(float zoom, Vector2 referencePosition)
        {
            if (zoom == _zoom)
                return referencePosition;

            var oldzoom = _zoom;
            _zoom = zoom;

            // Determine where on the canvas the mouse was previously
            var oldWorkspaceSize = Size;
            var oldCanvasSize = (Vector2)CanvasSize * oldzoom;
            var referenceCanvasRatio = (referencePosition - (oldWorkspaceSize - oldCanvasSize) * 0.5f) / oldCanvasSize;

            // Resize the canvas.
            Canvas.style.width = CanvasWidth * _zoom;
            Canvas.style.height = CanvasHeight * _zoom;

            UpdateScrollView();

            // Position the cursor over the same pixel in the canvas that it was over before the zoom
            var newWorkspaceSize = Size;
            var viewPosition = _content.ChangeCoordinatesTo(_scrollView.contentViewport, referencePosition);
            var newCanvasSize = (Vector2)CanvasSize * _zoom;
            referencePosition = (newWorkspaceSize - newCanvasSize) * 0.5f + referenceCanvasRatio * newCanvasSize;
            ScrollOffset = referencePosition - viewPosition;

            ZoomChangedEvent?.Invoke();

            RefreshCanvas();
            SelectedTool.MarkDirtyRepaint();
            RefreshCursor();

            return referencePosition;
        }

        /// <summary>
        /// Handle a mouse button down within the workspace
        /// </summary>
        private void OnMouseDown (MouseDownEvent evt)
        {
            // Middle button is pan tool
            if ((MouseButton)evt.button == MouseButton.MiddleMouse)
            {
                _previousTool = SelectedTool;
                SelectedTool = PanTool;
            }

            // Give the tool a chance to handle the mouse down first
            SelectedTool?.OnMouseDown(new PAMouseEvent
            {
                button = (MouseButton)evt.button,
                alt = evt.altKey,
                shift = evt.shiftKey,
                ctrl = evt.ctrlKey,
                canvasPosition = WorkspaceToCanvas(evt.localMousePosition),
                workspacePosition = evt.localMousePosition
            });

            // Ignore all mouse buttons when drawing
            if (IsDrawing)
                return;

            // Alwasys capture the mouse between mouse down/up
            MouseCaptureController.CaptureMouse(_content);

            _drawButton = evt.button;
            _drawStart = evt.localMousePosition;
            _drawLast = _drawStart;

            if (SelectedTool.DrawThreshold <= 0.0f)
            {
                IsDrawing = true;
                SelectedTool?.OnDrawStart(new PADrawEvent
                {
                    start = _drawStart,
                    button = (MouseButton)_drawButton,
                    alt = evt.altKey,
                    shift = evt.shiftKey,
                    ctrl = evt.ctrlKey,
                    canvasPosition = WorkspaceToCanvas(_drawStart),
                    workspacePosition = _drawStart
                });
            }
        }

        /// <summary>
        /// Handle a mouse button up within the workspace
        /// </summary>
        private void OnMouseUp (MouseUpEvent evt)
        {
            SelectedTool?.OnMouseUp(PAMouseEvent.Create(this, evt));

            // If drawing then end the drawing
            if (IsDrawing)
            {
                SelectedTool?.OnDrawEnd(PADrawEvent.Create(this, evt, (MouseButton)_drawButton, _drawStart), false);

                _drawButton = -1;
                IsDrawing = false;

                // If middle button pan was active then return to the previous tool
                if ((MouseButton)evt.button == MouseButton.MiddleMouse && SelectedTool == PanTool)
                    SelectedTool = _previousTool;
            }

            // Release the mouse capture
            if (MouseCaptureController.HasMouseCapture(_content))
                MouseCaptureController.ReleaseMouse();
        }


        /// <summary>
        /// Handle the mouse moving over the workspace
        /// </summary>
        private void OnMouseMove (MouseMoveEvent evt)
        {
            SelectedTool?.OnMouseMove(PAMouseEvent.Create(this, evt));

            _lastMousePosition = evt.localMousePosition;
            var canvasPosition = WorkspaceToCanvas(evt.localMousePosition);

            if (IsDrawing)
            {
                _drawLast = evt.localMousePosition;

                SelectedTool?.OnDrawContinue(PADrawEvent.Create(this, evt, (MouseButton)_drawButton, _drawStart));
            }
            else if (_drawButton != -1 && (evt.localMousePosition - _drawStart).magnitude >= SelectedTool.DrawThreshold)
            {
                IsDrawing = true;
                SelectedTool?.OnDrawStart(PADrawEvent.Create(this, evt, (MouseButton)_drawButton, _drawStart));
            }

            SelectedTool?.SetCursor(canvasPosition);
        }

        /// <summary>
        /// Handle the workspace losing capture
        /// </summary>
        private void OnMouseCaptureOut (MouseCaptureOutEvent evt)
        {
            // If drawing then cancel the draw
            if (IsDrawing)
            {
                // If middle button pan was active then return to the previous tool
                if ((MouseButton)_drawButton == MouseButton.MiddleMouse && SelectedTool == PanTool)
                    SelectedTool = _previousTool;

                SelectedTool?.OnDrawEnd(new PADrawEvent
                {
                    button = (MouseButton)_drawButton,
                    alt = false,
                    ctrl = false,
                    shift = false,
                    canvasPosition = WorkspaceToCanvas(_drawLast),
                    workspacePosition = _drawLast,
                    start = _drawStart
                }, true);
                IsDrawing = false;
            }

            _drawButton = -1;
        }

        /// <summary>
        /// Handle the mouse wheel to zoom in/out
        /// </summary>
        private void OnWheel (WheelEvent evt)
        {
            _lastMousePosition = SetZoom(
                Mathf.Clamp(
                    Zoom * (evt.delta.y < 0 ? ZoomIncrementUp : ZoomIncrementDown),
                    ZoomMin,
                    ZoomMax), 
                _lastMousePosition);

            evt.StopImmediatePropagation();
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            _workspaceCursor.visible = true;
            RefreshCursor();
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _workspaceCursor.visible = false;
        }

        private void RefreshCursor()
        {
            if (!_workspaceCursor.visible)
                return;

            if (SelectedTool == null)
                return;

            if (MouseCaptureController.IsMouseCaptured() && !MouseCaptureController.HasMouseCapture(_content))
                return;

            SelectedTool.SetCursor(WorkspaceToCanvas(_lastMousePosition));
            _workspaceCursor.Refresh();
        }
    }
}
