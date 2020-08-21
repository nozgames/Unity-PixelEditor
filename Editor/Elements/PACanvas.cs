using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PACanvas : VisualElement
    {
        public const float ZoomMin = 1.0f;
        public const float ZoomMax = 50.0f;
        private const float ZoomIncrementUp = 1.1f;
        private const float ZoomIncrementDown = 1.0f / ZoomIncrementUp;

        private PALayer _selectedLayer;
        private PAFrame _selectedFrame;
        private Color _foregroundColor;
        private Color _backgroundColor;
        private PAFile _file;
        private int _drawButton = -1;
        private Vector2 _drawStart;
        private Vector2 _drawLast;
        private Vector2 _lastMousePosition;
        private PATool _previousTool;
        private PATool _selectedTool;
        private PACursorManager _cursorManager;
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

        public PAWorkspace Workspace { get; private set; }

        public PAFile File {
            get => _file;
            set {
                if (_file == value)
                    return;

                _file = value;

                if(_file != null)
                {
                    SelectedFrame = _file.frames[0];
                    SelectedLayer = _file.layers[0];
                }
                else
                {
                    SelectedFrame = null;
                    SelectedLayer = null;
                }
            }
        }

        public Vector2 Size {
            get => new Vector2(
                ((ScrollView)parent).contentContainer.style.minWidth.value.value,
                ((ScrollView)parent).contentContainer.style.minHeight.value.value);

            set {
                if (Size == value)
                    return;

                ((ScrollView)parent).contentContainer.style.minWidth = (int)value.x;
                ((ScrollView)parent).contentContainer.style.minHeight = (int)value.y;
                style.minWidth = (int)value.x;
                style.minHeight = (int)value.y;
                ((ScrollView)parent).contentContainer.style.width = (int)value.x;
                ((ScrollView)parent).contentContainer.style.height = (int)value.y;
                style.width = (int)value.x;
                style.height = (int)value.y;
            }
        }

        /// <summary>
        /// Returns true if a drawing operation is currently in progress
        /// </summary>
        public bool IsDrawing { get; private set; }

        public PAAnimation CurrentAnimation => SelectedFrame?.animation;

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
        public float Zoom { get; private set; } = 1.0f;


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
                Size * 0.5f - (Vector2)ImageSize * Zoom * 0.5f,
                (Vector2)ImageSize * Zoom);

        public PACanvas(PAWorkspace workspace)
        {
            focusable = true;
            Workspace = workspace;

            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            // Canvas
            _image = new PAImageView(this) { name = "ImageView" };
            _image.pickingMode = PickingMode.Ignore;
            Add(_image);

            // Pixel grid
            Grid = new PAGrid(this);
            Add(Grid);

            // Create the tools
            PencilTool = new PAPencilTool(this);
            Add(PencilTool);

            EraserTool = new PAEraserTool(this);
            Add(EraserTool);

            EyeDropperTool = new PAEyeDropperTool(this);
            Add(EraserTool);

            SelectionTool = new PASelectionTool(this);
            Add(SelectionTool);

            PanTool = new PAPanTool(this);
            Add(PanTool);

            // Create an element to manage the workspace cursor
            _cursorManager = new PACursorManager();
            _cursorManager.StretchToParentSize();
            _cursorManager.pickingMode = PickingMode.Ignore;
            Add(_cursorManager);
        }

        /// <summary>
        /// Refresh the image and optionally and previews of the canvas as well
        /// </summary>
        public void RefreshImage(bool includePreviews = true)
        {
            if (File == null)
                return;

            if (includePreviews)
            {
                // Update frame preview of selected frame
                SelectedFrame?.Item?.RefreshPreview();

                // Update all layer previews
                foreach (var layer in File.layers)
                    layer.Item?.RefreshPreview(SelectedFrame);
            }

            _image.MarkDirtyRepaint();
        }

        /// <summary>
        /// Refresh all frame previews
        /// </summary>
        public void RefreshFramePreviews()
        {
            foreach (var frame in File.frames)
                frame.Item?.RefreshPreview();
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
            canvasPosition -= (Size * 0.5f);
            canvasPosition /= new Vector2(ImageWidth * Zoom, ImageHeight * Zoom);
            canvasPosition += new Vector2(0.5f, 0.5f);
            canvasPosition *= new Vector2(ImageWidth, ImageHeight);
            return new Vector2Int(
                (int)Mathf.Floor(canvasPosition.x),
                (int)Mathf.Floor(canvasPosition.y));
        }


        /// <summary>
        /// Handle a mouse button down within the workspace
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt)
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
            MouseCaptureController.CaptureMouse(this);

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
        private void OnMouseUp(MouseUpEvent evt)
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
            if (MouseCaptureController.HasMouseCapture(this))
                MouseCaptureController.ReleaseMouse();
        }


        /// <summary>
        /// Handle the mouse moving over the workspace
        /// </summary>
        private void OnMouseMove(MouseMoveEvent evt)
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
        private void OnMouseCaptureOut(MouseCaptureOutEvent evt)
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
        private void OnWheel(WheelEvent evt)
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
            _cursorManager.visible = true;
            RefreshCursor();
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _cursorManager.visible = false;
        }

        private void RefreshCursor()
        {
            if (!_cursorManager.visible)
                return;

            if (SelectedTool == null)
                return;

            if (MouseCaptureController.IsMouseCaptured() && !MouseCaptureController.HasMouseCapture(this))
                return;

            SelectedTool.SetCursor(CanvasToImage(_lastMousePosition));
            _cursorManager.Refresh();
        }


        public void SetCursor(MouseCursor cursor) => _cursorManager.Cursor = cursor;

        public void SetCursor(Texture2D texture, Vector2 hotspot) => _cursorManager.SetCursor(texture, hotspot);


        public Vector2 SetZoom(float zoom, Vector2 referencePosition)
        {
            if (zoom == Zoom)
                return referencePosition;

            var oldzoom = Zoom;
            Zoom = zoom;

            // Determine where on the canvas the mouse was previously
            var oldWorkspaceSize = Size;
            var oldImageSize = (Vector2)ImageSize * oldzoom;
            var referenceImageSize = (referencePosition - (oldWorkspaceSize - oldImageSize) * 0.5f) / oldImageSize;

            // Resize the image view.
            _image.style.width = (int)(ImageWidth * Zoom);
            _image.style.height = (int)(ImageHeight * Zoom);

            UpdateSize();

            // Position the cursor over the same pixel in the canvas that it was over before the zoom
            var newCanvasSize = Size;
            var viewPosition = Workspace.CanvasToViewport(referencePosition);
            var newImageSize = (Vector2)ImageSize * Zoom;
            referencePosition = (newCanvasSize - newImageSize) * 0.5f + referenceImageSize * newImageSize;
            Workspace.ViewportOffset = referencePosition - viewPosition;

            ZoomChangedEvent?.Invoke();

            RefreshImage();
            SelectedTool.MarkDirtyRepaint();
            RefreshCursor();

            return referencePosition;
        }

        /// <summary>
        /// Set the Zoom and ScrollOffset such that the content fits to the scroll view area.
        /// </summary>
        public void ZoomToFit()
        {
            if (File == null)
                return;

            // If the scrollview isnt ready yet then wait till it is.  This mainly happens on 
            // a file load right when the window is opening.
            if (float.IsNaN(Workspace.ViewportSize.x) ||
               float.IsNaN(Workspace.ViewportSize.y))
            {
                UpdateSize();
                schedule.Execute(ZoomToFit);
                return;
            }

            // Set the new zoom level
            var zoom = Workspace.ViewportSize * 0.9f / (Vector2)ImageSize;
            SetZoom(Mathf.Min(zoom.x, zoom.y), Vector2.zero);

            // Offset the scroll view to center the content
            Workspace.ViewportOffset = (Size - Workspace.ViewportSize) * 0.5f;

            RefreshImage();
        }

        /// <summary>
        /// Update the scrollview size
        /// </summary>
        private void UpdateSize ()
        {
            // Dont update the scrollview until it has been laid out
            if (float.IsNaN(Workspace.ViewportSize.x) ||
                float.IsNaN(Workspace.ViewportSize.y))
                return;

            Size = Vector2.Max(
                Workspace.ViewportSize * 2.0f - (Vector2)ImageSize * Zoom,
                (Vector2)ImageSize * Zoom + Workspace.ViewportSize);
        }


        private void OnKeyDown(KeyDownEvent evt)
        {
            // Send the key to the current tool
            if (!SelectedTool?.OnKeyDown(PAKeyEvent.Create(evt)) ?? true)
            {
                evt.StopImmediatePropagation();
                return;
            }

            // Handle window level key commands
            switch (evt.keyCode)
            {
                case KeyCode.F:
                    ZoomToFit();
                    break;

                case KeyCode.A:
                    // Ctrl+a = select all
                    if (evt.ctrlKey)
                    {
                        SelectedTool = SelectionTool;
                        SelectionTool.Selection = new RectInt(0, 0, ImageWidth, ImageHeight);
                        evt.StopImmediatePropagation();
                    }
                    break;

                // Swap foreground and background colors
                case KeyCode.X:
                {
                    var swap = ForegroundColor;
                    ForegroundColor = BackgroundColor;
                    BackgroundColor = swap;
                    evt.StopImmediatePropagation();
                    break;
                }

                // Change to eyedropper tool
                case KeyCode.I:
                    SelectedTool = EyeDropperTool;
                    evt.StopImmediatePropagation();
                    break;

                // Change to eraser tool
                case KeyCode.E:
                    SelectedTool = EraserTool;
                    evt.StopImmediatePropagation();
                    break;

                // Change to pencil tool
                case KeyCode.B:
                    SelectedTool = PencilTool;
                    evt.StopImmediatePropagation();
                    break;

                // Change to selection tool
                case KeyCode.M:
                    SelectedTool = SelectionTool;
                    evt.StopImmediatePropagation();
                    break;
            }
        }

        public void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
                UpdateSize();
        }
    }
}
