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

        private VisualElement _canvas;
        private PALayer _selectedLayer;
        private PAFrame _selectedFrame;
        private ScrollView _scrollView;
        private Color _foregroundColor;
        private Color _backgroundColor;
        private PAFile _file;
        private int _drawButton = -1;
        private Vector2 _drawStart;
        private Vector2 _drawLast;
        private Vector2 _lastMousePosition;
        private PATool _previousTool;
        private PATool _selectedTool;
        private PACursorManager _workspaceCursor;
        private PAImageView _image;

        public event Action ZoomChangedEvent;
        public event Action ToolChangedEvent;
        public event Action SelectedLayerChangedEvent;
        public event Action SelectedFrameChangedEvent;
        public event Action ForegroundColorChangedEvent;
        public event Action BackgroundColorChangedEvent;

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

        public Vector2 CanvasSize {
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
        /// Exeternal access to enabling/disabling the checkerboard background
        /// </summary>
        public bool ShowCheckerboard {
            get => _image.ShowCheckerboard;
            set => _image.ShowCheckerboard = value;
        }

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

                RefreshImage();
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

                RefreshImage();
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
        public float Zoom { get; private set; }

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
        public int ImageWidth => File?.width ?? 0;

        /// <summary>
        /// Height of the canvas in pixels
        /// </summary>
        public int ImageHeight => File?.height ?? 0;

        /// <summary>
        /// Size of the canvas in pixels
        /// </summary>
        public Vector2Int ImageSize => new Vector2Int(ImageWidth, ImageHeight);

        /// <summary>
        /// Returns the image rectangle within the canvas
        /// </summary>
        public Rect ImageRect =>
            new Rect(
                _canvas.contentRect.min + _canvas.contentRect.size * 0.5f - (Vector2)ImageSize * Zoom * 0.5f, 
                (Vector2)ImageSize * Zoom);

        public PAWorkspace()
        {            
            // Canvas is within the scrollview
            _scrollView = new ScrollView();
            _scrollView.StretchToParentSize();
            Add(_scrollView);

            _canvas = new VisualElement();
            _canvas.StretchToParentSize();
            _canvas.RegisterCallback<WheelEvent>(OnWheel);
            _canvas.RegisterCallback<MouseDownEvent>(OnMouseDown);
            _canvas.RegisterCallback<MouseUpEvent>(OnMouseUp);
            _canvas.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _canvas.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
            _canvas.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            _canvas.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            _scrollView.Add(_canvas);

            // Canvas
            _image = new PAImageView(this) { name = "Canvas" };
            _image.pickingMode = PickingMode.Ignore;
            _canvas.Add(_image);

            // Pixel grid
            Grid = new PAGrid(this);
            _canvas.Add(Grid);

            // Create the tools
            PencilTool = new PAPencilTool(this);
            _canvas.Add(PencilTool);

            EraserTool = new PAEraserTool(this);
            _canvas.Add(EraserTool);

            EyeDropperTool = new PAEyeDropperTool(this);
            _canvas.Add(EraserTool);

            SelectionTool = new PASelectionTool(this);
            _canvas.Add(SelectionTool);

            PanTool = new PAPanTool(this);
            _canvas.Add(PanTool);

            // Create an element to manage the workspace cursor
            _workspaceCursor = new PACursorManager();
            _workspaceCursor.StretchToParentSize();
            _workspaceCursor.pickingMode = PickingMode.Ignore;
            _canvas.Add(_workspaceCursor);
        }

        /// <summary>
        /// Refresh the image and optionally and previews of the canvas as well
        /// </summary>
        public void RefreshImage (bool includePreviews=true)
        {
            if(includePreviews)
            {
                // Update frame preview of selected frame
                SelectedFrame.Item?.RefreshPreview();

                // Update all layer previews
                foreach (var layer in File.layers)
                    layer.Item?.RefreshPreview(SelectedFrame);
            }

            _image.MarkDirtyRepaint();
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
            CanvasSize = Vector2.Max(
                ViewportSize * 2.0f - (Vector2)ImageSize * Zoom,
                (Vector2)ImageSize * Zoom + ViewportSize);
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
            var zoom = _scrollView.contentViewport.contentRect.size * 0.9f / (Vector2)ImageSize;
            SetZoom(Mathf.Min(zoom.x, zoom.y), Vector2.zero);

            // Offset the scroll view to center the content
            ScrollOffset = (CanvasSize - ViewportSize) * 0.5f;

            RefreshImage();
        }

        /// <summary>
        /// Helper function to clamp the given image position to the image bounds
        /// </summary>
        public Vector2Int ClampImagePosition(Vector2Int imagePosition) =>
            new Vector2Int(
                Mathf.Clamp(imagePosition.x, 0, ImageWidth - 1), 
                Mathf.Clamp(imagePosition.y, 0, ImageHeight - 1));

        /// <summary>
        /// Convert the given image position to a canvas position
        /// </summary>
        public Vector2 ImageToCanvas(Vector2Int imagePosition) =>
            ImageRect.min + (Vector2)imagePosition * Zoom;

        /// <summary>
        /// Convert a coordinate from the workspace to the canvas.  Note that this 
        /// coordinate is not clamped to the canvas, use ClampCanvasPosition to do so.
        /// </summary>
        public Vector2Int CanvasToImage(Vector2 canvasPosition)
        {
            canvasPosition -= _canvas.contentRect.center;
            canvasPosition /= new Vector2(ImageWidth * Zoom, ImageHeight * Zoom);
            canvasPosition += new Vector2(0.5f, 0.5f);
            canvasPosition *= new Vector2(ImageWidth, ImageHeight);
            return new Vector2Int(
                (int)Mathf.Floor(canvasPosition.x),
                (int)Mathf.Floor(canvasPosition.y));
        }

        /// <summary>
        /// Convert a canvas position to a viewport position
        /// </summary>
        public Vector2 CanvasToViewport(Vector2 canvasPosition) =>
            _canvas.ChangeCoordinatesTo(_scrollView.contentViewport, canvasPosition);

        /// <summary>
        /// Convert a viewport position to a canvas position
        /// </summary>
        public Vector2 ViewportToCanvas (Vector2 viewportPosition) => ScrollOffset + viewportPosition;

        public void SetCursor(MouseCursor cursor) => _workspaceCursor.Cursor = cursor;

        public void SetCursor(Texture2D texture, Vector2 hotspot) => _workspaceCursor.SetCursor(texture, hotspot);

        public Vector2 SetZoom(float zoom, Vector2 referencePosition)
        {
            if (zoom == Zoom)
                return referencePosition;

            var oldzoom = Zoom;
            Zoom = zoom;

            // Determine where on the canvas the mouse was previously
            var oldWorkspaceSize = CanvasSize;
            var oldImageSize = (Vector2)ImageSize * oldzoom;
            var referenceImageSize = (referencePosition - (oldWorkspaceSize - oldImageSize) * 0.5f) / oldImageSize;

            // Resize the canvas.
            _image.style.width = ImageWidth * Zoom;
            _image.style.height = ImageHeight * Zoom;

            UpdateScrollView();

            // Position the cursor over the same pixel in the canvas that it was over before the zoom
            var newCanvasSize = CanvasSize;
            var viewPosition = _canvas.ChangeCoordinatesTo(_scrollView.contentViewport, referencePosition);
            var newImageSize = (Vector2)ImageSize * Zoom;
            referencePosition = (newCanvasSize - newImageSize) * 0.5f + referenceImageSize * newImageSize;
            ScrollOffset = referencePosition - viewPosition;

            ZoomChangedEvent?.Invoke();

            RefreshImage();
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
                imagePosition = CanvasToImage(evt.localMousePosition),
                canvasPosition = evt.localMousePosition
            });

            // Ignore all mouse buttons when drawing
            if (IsDrawing)
                return;

            // Alwasys capture the mouse between mouse down/up
            MouseCaptureController.CaptureMouse(_canvas);

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
                    imagePosition = CanvasToImage(_drawStart),
                    canvasPosition = _drawStart
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
            if (MouseCaptureController.HasMouseCapture(_canvas))
                MouseCaptureController.ReleaseMouse();
        }


        /// <summary>
        /// Handle the mouse moving over the workspace
        /// </summary>
        private void OnMouseMove (MouseMoveEvent evt)
        {
            SelectedTool?.OnMouseMove(PAMouseEvent.Create(this, evt));

            _lastMousePosition = evt.localMousePosition;
            var canvasPosition = CanvasToImage(evt.localMousePosition);

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
                    imagePosition = CanvasToImage(_drawLast),
                    canvasPosition = _drawLast,
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

            if (MouseCaptureController.IsMouseCaptured() && !MouseCaptureController.HasMouseCapture(_canvas))
                return;

            SelectedTool.SetCursor(CanvasToImage(_lastMousePosition));
            _workspaceCursor.Refresh();
        }

        public void SetFocusToCanvas() => _canvas.Focus();
    }
}
