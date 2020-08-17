using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Rendering;
using System.Diagnostics.Eventing.Reader;

namespace NoZ.PixelEditor
{
    /// <summary>
    /// PixelEditor window
    /// </summary>
    internal class PEWindow : EditorWindow
    {
        private const string USS = "Assets/PixelEditor/Editor/Scripts/PixelEditor.uss";
        private const float ZoomMin = 1.0f;
        private const float ZoomMax = 50.0f;
        private const float ZoomIncrementUp = 1.1f;
        private const float ZoomIncrementDown = 1.0f / ZoomIncrementUp;

        private ColorField _foregroundColor = null;
        private ColorField _backgroundColor = null;
        private VisualElement _workspace = null;
        private VisualElement _toolbox = null;
        private VisualElement _layers = null;
        private PEGrid _grid = null;
        private Toolbar _toolbar = null;
        private WorkspaceCursorManager _workspaceCursor = null;
        private Slider _zoomSlider = null;

        private PEEyeDropperTool _toolEyeDropper;
        private PEPencilTool _toolPencil;
        private PEEraserTool _toolEraser;
        private PESelectionTool _toolSelection;

        private int _drawButton = -1;
        private Vector2 _drawStart;
        private Vector2 _drawLast;
        private PETool _currentTool = null;
        private PELayer _currentLayer = null;
        private Vector2 _lastMousePosition;

        private PixelArt _pixelArt;

        /// <summary>
        /// Returns true if a drawing operation is currently in progress
        /// </summary>
        public bool IsDrawing { get; private set; }

        public float Zoom => _zoomSlider.value;

        public int CanvasWidth => CurrentFile?.width ?? 0;

        public int CanvasHeight => CurrentFile?.height ?? 0;

        public PECanvas Canvas { get; private set; }

        public Vector2Int CanvasSize => new Vector2Int(CanvasWidth, CanvasHeight);

        public PEFile CurrentFile { get; private set; }

        public PEAnimation CurrentAnimation => CurrentFrame?.animation;

        public PELayer CurrentLayer {
            get => _currentLayer;
            set {
                if (_currentLayer == value)
                    return;

                _currentLayer = value;

                foreach(var child in _layers.Children())
                {
                    var layer = (PELayer)child.userData;
                    if (layer == _currentLayer)
                        child.AddToClassList("selected");
                    else
                        child.RemoveFromClassList("selected");
                }
            }
        }

        public PEFrame CurrentFrame { get; private set; }

        public Color ForegroundColor {
            get => _foregroundColor.value;
            set => _foregroundColor.value = value;
        }

        public Color BackgroundColor {
            get => _backgroundColor.value;
            set => _backgroundColor.value = value;
        }

        public PETool CurrentTool {
            get => _currentTool;
            set {
                if (_currentTool == value)
                    return;

                if (_currentTool != null)
                {
                    _currentTool.visible = false;
                    _currentTool.OnDisable();
                }

                _currentTool = value;
                if (_currentTool != null)
                {
                    _currentTool.visible = true;
                    _currentTool.OnEnable();
                    _currentTool.MarkDirtyRepaint();
                }
                
                foreach(var child in _toolbox.Children())
                {
                    if ((PETool)child.userData == _currentTool)
                        child.AddToClassList("selected");
                    else
                        child.RemoveFromClassList("selected");
                }

                RefreshCursor();
            }
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (Selection.activeObject as PixelArt)
            {
                var pixelArt = (PixelArt)Selection.activeObject;
                
                var window = GetWindow<PEWindow>();
                window.titleContent = new GUIContent(
                    pixelArt.name,
                    AssetDatabase.LoadAssetAtPath<Texture>("Assets/PixelEditor/Editor/Icons/PixelArtEditor.psd"));
                window.Load(pixelArt);

                return true;
            }

            return false;
        }

        public void OnEnable()
        {
            // Add style sheet
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(USS));

            CreateToolbar();

            _workspace = new VisualElement();
            _workspace.focusable = true;
            _workspace.AddToClassList("workspace");
            _workspace.RegisterCallback<MouseDownEvent>(OnWorkspaceMouseDown);
            _workspace.RegisterCallback<MouseMoveEvent>(OnWorkspaceMouseMove);
            _workspace.RegisterCallback<MouseUpEvent>(OnWorkspaceMouseUp);
            _workspace.RegisterCallback<MouseCaptureOutEvent>(OnWorkspaceMouseCaptureOut);
            _workspace.RegisterCallback<WheelEvent>(OnWorkspaceWheel);
            _workspace.RegisterCallback<MouseEnterEvent>(OnWorkspaceMouseEnter);
            _workspace.RegisterCallback<MouseLeaveEvent>(OnWorkspaceMouseLeave);
            rootVisualElement.RegisterCallback<KeyDownEvent>(this.OnWorkspaceKeyDown);
            rootVisualElement.focusable = true;
            _workspace.pickingMode = PickingMode.Position;
            _workspace.Focus();
            rootVisualElement.Add(_workspace);

            Canvas = new PECanvas(this);
            Canvas.pickingMode = PickingMode.Ignore;
            Canvas.MarkDirtyRepaint();
            _workspace.Add(Canvas);

            _grid = new PEGrid(this);
            _workspace.Add(_grid);

            // Create an element to manage the workspace cursor
            _workspaceCursor = new WorkspaceCursorManager();
            _workspaceCursor.AddToClassList("stretchFit");
            _workspaceCursor.pickingMode = PickingMode.Ignore;
            _workspace.Add(_workspaceCursor);

            // Create the tools
            _toolPencil = new PEPencilTool(this);
            _workspace.Add(_toolPencil);

            _toolEraser = new PEEraserTool(this);            
            _workspace.Add(_toolEraser);

            _toolEyeDropper = new PEEyeDropperTool(this);            
            _workspace.Add(_toolEraser);

            _toolSelection = new PESelectionTool(this);            
            _workspace.Add(_toolSelection);

            CreateLayersPopup();
            CreateToolBox();

            // Set default tool to pencil
            CurrentTool = _toolPencil;

            Undo.undoRedoPerformed += OnUndoRedo;

            // Load the Saved preferences
            ForegroundColor = ColorUtility.TryParseHtmlString(EditorPrefs.GetString("PixelEditor.ForegroundColor"), out var foregroundColor) ? 
                foregroundColor : 
                Color.white;
            BackgroundColor = ColorUtility.TryParseHtmlString(EditorPrefs.GetString("PixelEditor.BackgroundColor"), out var backgroundColor) ?
                backgroundColor :
                Color.white;

            if (_pixelArt != null)
                Load(_pixelArt);
        }

        private void OnWorkspaceMouseLeave(MouseLeaveEvent evt)
        {
            _workspaceCursor.visible = false;
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("PixelEditor.ForegroundColor", $"#{ColorUtility.ToHtmlStringRGBA(ForegroundColor)}");
            EditorPrefs.SetString("PixelEditor.BackgroundColor", $"#{ColorUtility.ToHtmlStringRGBA(BackgroundColor)}");

            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        /// <summary>
        /// Convert a coordinate from the workspace to the canvas.  Note that this 
        /// coordinate is not clamped to the canvas, use ClampCanvasPosition to do so.
        /// </summary>
        public Vector2Int WorkspaceToCanvas(Vector2 workspacePosition)
        {
            workspacePosition -= _workspace.contentRect.center;
            workspacePosition /= new Vector2(CanvasWidth * Zoom, CanvasHeight * Zoom);
            workspacePosition += new Vector2(0.5f, 0.5f);
            workspacePosition *= new Vector2(CanvasWidth, CanvasHeight);
            return new Vector2Int(
                (int)Mathf.Floor(workspacePosition.x),
                (int)Mathf.Floor(workspacePosition.y));
        }

        /// <summary>
        /// Convert the given canvas position to a workspace position
        /// </summary>
        public Vector2 CanvasToWorkspace(Vector2Int canvasPosition) =>
            CanvasRect.min + (Vector2)canvasPosition * Zoom;

        /// <summary>
        /// Returns the canvas rectangle in workspace coordinates
        /// </summary>
        public Rect CanvasRect =>
            new Rect(_workspace.contentRect.center - (Vector2)CanvasSize * Zoom * 0.5f, (Vector2)CanvasSize * Zoom);

        /// <summary>
        /// Helper function to clamp the given canvas position to the cavnas
        /// </summary>
        public Vector2Int ClampCanvasPosition(Vector2Int canvasPosition) =>
            new Vector2Int(Mathf.Clamp(canvasPosition.x, 0, CanvasWidth - 1), Mathf.Clamp(canvasPosition.y, 0, CanvasHeight - 1));

        public void SetCursor(MouseCursor cursor) => _workspaceCursor.Cursor = cursor;

        public void SetCursor(Texture2D texture, Vector2 hotspot) => _workspaceCursor.SetCursor(texture, hotspot);

        private void OnWorkspaceKeyDown(KeyDownEvent evt)
        {
            Debug.Log(evt.keyCode);
            if(!_currentTool?.OnKeyDown(PEKeyEvent.Create(evt)) ?? true)
            {
                evt.StopImmediatePropagation();
                return;
            }

            switch (evt.keyCode)
            {
                case KeyCode.A:
                    if(evt.ctrlKey)
                    {
                        CurrentTool = _toolSelection;
                        _toolSelection.Selection = new RectInt(0,0,CanvasWidth,CanvasHeight);
                        evt.StopImmediatePropagation();
                    }
                    break;

                case KeyCode.X:
                {
                    var swap = ForegroundColor;
                    ForegroundColor = BackgroundColor;
                    BackgroundColor = swap;
                    evt.StopImmediatePropagation();
                    break;
                }

                case KeyCode.I:
                    CurrentTool = _toolEyeDropper;
                    evt.StopImmediatePropagation();
                    break;

                case KeyCode.E:
                    CurrentTool = _toolEraser;
                    evt.StopImmediatePropagation();
                    break;

                case KeyCode.B:
                    CurrentTool = _toolPencil;
                    evt.StopImmediatePropagation();
                    break;

                case KeyCode.M:
                    CurrentTool = _toolSelection;
                    evt.StopImmediatePropagation();
                    break;
            }            
        }

        private void OnWorkspaceMouseEnter(MouseEnterEvent evt)
        {
            _workspaceCursor.visible = true;
            RefreshCursor();
        }

        public void Load(PixelArt pa)
        {
            _pixelArt = pa;
            Load(PEFile.Load(AssetDatabase.GetAssetPath(pa)));
        }

        private void Load(PEFile file)
        {
            CurrentFile = file;

            _workspace.MarkDirtyRepaint();

            CurrentTool = _toolSelection;
            CurrentTool.MarkDirtyRepaint();
            Canvas.MarkDirtyRepaint();

            CurrentLayer = CurrentFile.layers[0];
            CurrentFrame = CurrentFile.frames[0];

            UpdateLayers();
        }

        private void Save()
        {
            if (null == _pixelArt)
                return;

            CurrentFile.Save(AssetDatabase.GetAssetPath(_pixelArt));
            AssetDatabase.Refresh();
        }

        private void RefreshCursor()
        {
            if (!_workspaceCursor.visible)
                return;

            if (MouseCaptureController.IsMouseCaptured() && !MouseCaptureController.HasMouseCapture(_workspace))
                return;

            _currentTool.SetCursor(WorkspaceToCanvas(_lastMousePosition));
            _workspaceCursor.Refresh();
        }

        private void OnUndoRedo()
        {
            //_layer.MarkDirtyRepaint();
            CurrentTool.MarkDirtyRepaint();
        }

        private void OnZoomValueChanged(ChangeEvent<float> evt)
        {
            //_layer.Zoom = evt.newValue;
            Canvas.MarkDirtyRepaint();
            CurrentTool.MarkDirtyRepaint();
            RefreshCursor();
        }

        private void OnWorkspaceWheel(WheelEvent evt)
        {
            var zoom = Zoom;
            if (evt.delta.y < 0)
                zoom *= ZoomIncrementUp;
            else
                zoom *= ZoomIncrementDown;

            _zoomSlider.value = Mathf.Clamp(zoom, ZoomMin, ZoomMax);
        }

        /// <summary>
        /// Handle a mouse button down within the workspace
        /// </summary>
        private void OnWorkspaceMouseDown(MouseDownEvent evt)
        {
            // Give the tool a chance to handle the mouse down first
            CurrentTool?.OnMouseDown(new PEMouseEvent
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
            MouseCaptureController.CaptureMouse(_workspace);

            _drawButton = evt.button;
            _drawStart = evt.localMousePosition;
            _drawLast = _drawStart;

            if (CurrentTool.DrawThreshold <= 0.0f)
            {
                IsDrawing = true;
                CurrentTool?.OnDrawStart(new PEDrawEvent
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
        private void OnWorkspaceMouseUp(MouseUpEvent evt)
        {
            CurrentTool?.OnMouseUp(PEMouseEvent.Create(this, evt));

            // If drawing then end the drawing
            if(IsDrawing)
            {
                CurrentTool?.OnDrawEnd(PEDrawEvent.Create(this, evt, (MouseButton)_drawButton, _drawStart), false);

                _drawButton = -1;
                IsDrawing = false;
            }

            // Release the mouse capture
            if (MouseCaptureController.HasMouseCapture(_workspace))
                MouseCaptureController.ReleaseMouse();
        }

        /// <summary>
        /// Handle the workspace losing capture
        /// </summary>
        private void OnWorkspaceMouseCaptureOut(MouseCaptureOutEvent evt)
        {
            // If drawing then cancel the draw
            if(IsDrawing)
            {
                CurrentTool?.OnDrawEnd(new PEDrawEvent
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
        /// Handle the mouse moving over the workspace
        /// </summary>
        private void OnWorkspaceMouseMove(MouseMoveEvent evt)
        {
            CurrentTool?.OnMouseMove(PEMouseEvent.Create(this, evt));

            _lastMousePosition = evt.localMousePosition;
            var canvasPosition = WorkspaceToCanvas(evt.localMousePosition);

            if (IsDrawing)
            {
                _drawLast = evt.localMousePosition;

                CurrentTool?.OnDrawContinue(PEDrawEvent.Create(this, evt, (MouseButton)_drawButton, _drawStart));
            }
            else if (_drawButton != -1 && (evt.localMousePosition - _drawStart).magnitude >= CurrentTool.DrawThreshold)
            {
                IsDrawing = true;
                CurrentTool?.OnDrawStart(PEDrawEvent.Create(this, evt, (MouseButton)_drawButton, _drawStart));
            }

            CurrentTool.SetCursor(canvasPosition);
        }

        private void CreateToolbar()
        {
            _toolbar = new Toolbar();
            _toolbar.pickingMode = PickingMode.Position;
            _toolbar.AddToClassList("toolbar");

            var modeMenu = new ToolbarMenu();
            modeMenu.text = "Pixel Editor";
            modeMenu.menu.AppendAction("Pixel Editor", (a) => { }, (a) => DropdownMenuAction.Status.Checked);
            modeMenu.menu.AppendAction("Bone Editor", (a) => { }, (a) => DropdownMenuAction.Status.Normal);
            _toolbar.Add(modeMenu);

            var toolbarSpacer = new VisualElement();
            toolbarSpacer.style.flexGrow = 1.0f;
            _toolbar.Add(toolbarSpacer);

            var zoomImage = new Image();
            zoomImage.style.width = 16;
            zoomImage.style.height = 16;
            zoomImage.image = AssetDatabase.LoadAssetAtPath<Texture>("Assets/PixelEditor/Editor/Icons/ZoomIcon.psd");
            _toolbar.Add(zoomImage);

            _zoomSlider = new Slider();
            _zoomSlider.lowValue = ZoomMin;
            _zoomSlider.highValue = ZoomMax;
            _zoomSlider.AddToClassList("zoom");
            _zoomSlider.RegisterValueChangedCallback(OnZoomValueChanged);
            _toolbar.Add(_zoomSlider);

            var layerToggle = new Image();
            layerToggle.pickingMode = PickingMode.Position;
            layerToggle.AddToClassList("toggleImage");
            layerToggle.AddToClassList("checked");
            layerToggle.image = AssetDatabase.LoadAssetAtPath<Texture>("Assets/PixelEditor/Editor/Icons/LayerToggle.psd");
            layerToggle.RegisterCallback<ClickEvent>((e) => {
                e.StopImmediatePropagation();
                if (_layers.parent.visible)
                {
                    _layers.parent.visible = false;
                    layerToggle.RemoveFromClassList("checked");
                }
                else
                {
                    _layers.parent.visible = true;
                    layerToggle.AddToClassList("checked");
                }
            });
            _toolbar.Add(layerToggle);

            var gridToggle = new Image();
            gridToggle.pickingMode = PickingMode.Position;
            gridToggle.AddToClassList("toggleImage");
            gridToggle.AddToClassList("checked");
            gridToggle.image = AssetDatabase.LoadAssetAtPath<Texture>("Assets/PixelEditor/Editor/Icons/GridToggle.psd");
            gridToggle.RegisterCallback<ClickEvent>((e) => {
                e.StopImmediatePropagation();
                if (_grid.ShowPixels)
                {
                    _grid.ShowPixels = false;
                    gridToggle.RemoveFromClassList("checked");
                }
                else
                {
                    _grid.ShowPixels = true;
                    gridToggle.AddToClassList("checked");
                }
            });
            _toolbar.Add(gridToggle);

            var checkerboardToggle = new Image();
            checkerboardToggle.pickingMode = PickingMode.Position;
            checkerboardToggle.AddToClassList("toggleImage");
            checkerboardToggle.AddToClassList("checked");
            checkerboardToggle.image = AssetDatabase.LoadAssetAtPath<Texture>("Assets/PixelEditor/Editor/Icons/Grid.psd");
            checkerboardToggle.RegisterCallback<ClickEvent>((e) => {
                e.StopImmediatePropagation();
                if (Canvas.ShowCheckerboard)
                {
                    Canvas.ShowCheckerboard = false;
                    checkerboardToggle.RemoveFromClassList("checked");
                    Canvas.MarkDirtyRepaint();
                }
                else
                {
                    Canvas.ShowCheckerboard = true;
                    checkerboardToggle.AddToClassList("checked");
                    Canvas.MarkDirtyRepaint();
                }
            });
            _toolbar.Add(checkerboardToggle);

            var saveButton = new ToolbarButton();
            saveButton.text = "Save";
            saveButton.clickable.clicked += Save;
            _toolbar.Add(saveButton);
            rootVisualElement.Add(_toolbar);
        }

        private void CreateToolBox ()
        {
            _toolbox = new UnityEngine.UIElements.PopupWindow();
            _toolbox.AddToClassList("toolbox");

            CreateToolBoxButton(_toolSelection, "Assets/PixelEditor/Editor/Icons/SelectionTool.psd", "Rectangular Marquee Tool (M)");
            CreateToolBoxButton(_toolPencil, "Assets/PixelEditor/Editor/Icons/PencilTool.psd", "Pencil Tool (B)");
            CreateToolBoxButton(_toolEraser, "Assets/PixelEditor/Editor/Icons/EraserTool.psd", "Eraser Tool (E)");
            CreateToolBoxButton(_toolEyeDropper, "Assets/PixelEditor/Editor/Icons/EyeDropperTool.psd", "Eyedropper Tool (I)");

            _foregroundColor = new ColorField();
            _foregroundColor.showEyeDropper = false;
            _foregroundColor.value = Color.white;
            _foregroundColor.RegisterValueChangedCallback((e) => rootVisualElement.Focus());
            _toolbox.Add(_foregroundColor);

            _backgroundColor = new ColorField();
            _backgroundColor.showEyeDropper = false;
            _backgroundColor.value = Color.white;
            _backgroundColor.RegisterValueChangedCallback((e) => rootVisualElement.Focus());
            _toolbox.Add(_backgroundColor);

            rootVisualElement.Add(_toolbox);
        }

        private void CreateToolBoxButton(PETool tool, string image, string tooltip)
        {
            var button = new Image();
            button.image = AssetDatabase.LoadAssetAtPath<Texture>(image);
            button.AddManipulator(new Clickable(() => CurrentTool = tool));
            button.userData = tool;
            button.tooltip = tooltip;
            _toolbox.Add(button);
        }

        private void CreateLayersPopup ()
        {
            var popup = new UnityEngine.UIElements.PopupWindow();
            popup.text = "Layers";
            popup.AddToClassList("layersPopup");
            rootVisualElement.Add(popup);

            var toolbar = new VisualElement();
            toolbar.AddToClassList("layersToolbar");
            popup.Add(toolbar);

            var addLayerButton = new Button();
            addLayerButton.text = "+";
            addLayerButton.clickable.clicked += () =>
            {
                var addedLayer = CurrentFile.AddLayer();
                UpdateLayers();
                CurrentLayer = addedLayer;
                Canvas.MarkDirtyRepaint();
            };
            toolbar.Add(addLayerButton);

            // Remove layer
            var removeLayerButton = new Button();
            removeLayerButton.text = "-";
            removeLayerButton.clickable.clicked += () =>
            {
                // Dont allow the last layer to be removed
                if (CurrentFile.layers.Count < 2)
                    return;

                var order = CurrentLayer.order;
                CurrentFile.RemoveLayer(CurrentLayer);
                UpdateLayers();
                order = Mathf.Min(order, CurrentFile.layers.Count - 1);
                CurrentLayer = CurrentFile.layers.Where(l => l.order == order).FirstOrDefault();
                Canvas.MarkDirtyRepaint();                
            };
            toolbar.Add(removeLayerButton);

            _layers = new VisualElement();
            _layers.AddToClassList("layers");
            popup.Add(_layers);

            UpdateLayers();
        }

        private void UpdateLayers()
        {
            _layers.Clear();

            if (CurrentFile == null)
                return;

            foreach(var layer in CurrentFile.layers.OrderByDescending(l => l.order))
            {
                var layerElemnet = new VisualElement();
                layerElemnet.AddToClassList("layer");
                layerElemnet.userData = layer;
                layerElemnet.AddManipulator(new Clickable(() =>
                {
                    CurrentLayer = layer;
                }));

                if (layer.order == 0)
                layerElemnet.AddToClassList("selected");

                var layerToggle = new Toggle();
                layerElemnet.Add(layerToggle);

                var layerImage = new Image();
                layerElemnet.Add(layerImage);

                var layerName = new Label(layer.name);
                layerElemnet.Add(layerName);
                
                _layers.Add(layerElemnet);
            }
        }
    }
}
