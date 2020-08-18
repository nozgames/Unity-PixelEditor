using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

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

        private VisualElement _editor = null;
        private VisualElement _empty = null;
        private ColorField _foregroundColor = null;
        private ColorField _backgroundColor = null;
        private VisualElement _workspace = null;
        private VisualElement _toolbox = null;
        private VisualElement _layers = null;
        private ScrollView _scrollView = null;
        private PEGrid _grid = null;
        private Toolbar _toolbar = null;
        private WorkspaceCursorManager _workspaceCursor = null;
        private Slider _zoomSlider = null;


        private PEEyeDropperTool _toolEyeDropper;
        private PEPencilTool _toolPencil;
        private PEEraserTool _toolEraser;
        private PESelectionTool _toolSelection;
        private PEPanTool _toolPan;

        private int _drawButton = -1;
        private Vector2 _drawStart;
        private Vector2 _drawLast;
        private PETool _currentTool = null;
        private PELayer _currentLayer = null;
        private Vector2 _lastMousePosition;
        private PETool _previousTool = null;

        private PixelArt _target;

        public bool IsEditing => _target != null && _editor.visible;

        /// <summary>
        /// Returns true if a drawing operation is currently in progress
        /// </summary>
        public bool IsDrawing { get; private set; }

        public float Zoom => _zoomSlider.value;

        /// <summary>
        /// Returns the current size of the workspace
        /// </summary>
        public Vector2 WorkspaceSize => new Vector2(
            _scrollView.contentContainer.style.width.value.value,
            _scrollView.contentContainer.style.height.value.value);

        public int CanvasWidth => CurrentFile?.width ?? 0;

        public int CanvasHeight => CurrentFile?.height ?? 0;

        public PECanvas Canvas { get; private set; }

        public Vector2Int CanvasSize => new Vector2Int(CanvasWidth, CanvasHeight);

        public PEFile CurrentFile { get; private set; }

        public PEAnimation CurrentAnimation => CurrentFrame?.animation;

        public Vector2 ScrollOffset {
            get => _scrollView.scrollOffset;
            set {
                _scrollView.scrollOffset = value;
                _scrollView.MarkDirtyRepaint();
            }
        }

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

                _previousTool = _currentTool;

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
                GetWindow<PEWindow>().OpenFile((PixelArt)Selection.activeObject);
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
                UpdateScrollView();

            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                var target = EditorGUIUtility.GetObjectPickerObject() as PixelArt;
                if (target != null)
                    OpenFile(target);
                else
                {
                    _target = null;
                    _editor.visible = false;
                    _empty.visible = true;
                    UpdateScrollView();
                }                    
            }                
        }

        public void OnEnable()
        {
            SetTitle("Pixel Editor");

            // Add style sheet
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(USS));

            _empty = new VisualElement();
            _empty.AddToClassList("empty");
            _empty.StretchToParentSize();
            _empty.visible = true;
            rootVisualElement.Add(_empty);

            var openButton = new Button();
            openButton.text = "Open PixelArt";
            openButton.clickable.clicked += () => { EditorGUIUtility.ShowObjectPicker<PixelArt>(_target, false, null, 0); };
            _empty.Add(openButton);

            _editor = new VisualElement();
            _editor.style.position = new StyleEnum<Position>(Position.Absolute);
            _editor.style.left = 0;
            _editor.style.right = 0;
            _editor.style.bottom = 0;
            _editor.style.top = 0;
            _editor.visible = false;
            rootVisualElement.Add(_editor);

            CreateToolbar();

            _scrollView = new ScrollView();
            _scrollView.StretchToParentSize();
            //_scrollView.showHorizontal = true;
            //_scrollView.showVertical = true;
            //_scrollView.verticalPageSize = 10;
            //_scrollView.horizontalPageSize= 10;
            _editor.Add(_scrollView);

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
            _editor.RegisterCallback<KeyDownEvent>(this.OnWorkspaceKeyDown);
            _editor.focusable = true;
            _workspace.pickingMode = PickingMode.Position;
            _workspace.Focus();
            _scrollView.Add(_workspace);

            Canvas = new PECanvas(this);
            Canvas.pickingMode = PickingMode.Ignore;
            Canvas.MarkDirtyRepaint();
            _workspace.Add(Canvas);

            _grid = new PEGrid(this);
            _workspace.Add(_grid);

            // Create the tools
            _toolPencil = new PEPencilTool(this);
            _workspace.Add(_toolPencil);

            _toolEraser = new PEEraserTool(this);            
            _workspace.Add(_toolEraser);

            _toolEyeDropper = new PEEyeDropperTool(this);            
            _workspace.Add(_toolEraser);

            _toolSelection = new PESelectionTool(this);            
            _workspace.Add(_toolSelection);

            _toolPan = new PEPanTool (this);
            _workspace.Add(_toolPan);

            // Create an element to manage the workspace cursor
            _workspaceCursor = new WorkspaceCursorManager();
            _workspaceCursor.AddToClassList("stretchFit");
            _workspaceCursor.pickingMode = PickingMode.Ignore;
            _workspace.Add(_workspaceCursor);

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

            rootVisualElement.MarkDirtyRepaint();

            if (_target != null)
                OpenFile(_target);
        }

        private void OnDisable()
        {
            // Save current file when the window is disabled.  We dont close the file here because
            // that would prevent it from automatically opening up again if the editor is enabled
            SaveFile();
            CurrentFile = null;

            EditorPrefs.SetString("PixelEditor.ForegroundColor", $"#{ColorUtility.ToHtmlStringRGBA(ForegroundColor)}");
            EditorPrefs.SetString("PixelEditor.BackgroundColor", $"#{ColorUtility.ToHtmlStringRGBA(BackgroundColor)}");

            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void Update()
        {
            // Automatically close the current file if the asset is deleted
            if (!IsEditing && _editor.visible)
                CloseFile();
            
            // Handle asset renaming
            if(_target != null && CurrentFile != null && _target.name != CurrentFile.name)
            {
                CurrentFile.name = _target.name;
                SetTitle(_target.name);
            }
        }

        /// <summary>
        /// Set the window title with with the pixel art icon
        /// </summary>
        private void SetTitle(string title) =>
            titleContent = new GUIContent(
                title,
                PEUtils.LoadTexture("Assets/PixelEditor/Editor/Icons/PixelArtEditor.psd"));

        /// <summary>
        /// Convert a workspace coordinate into a coordinate within the scroll view
        /// </summary>
        public Vector2 WorkspaceToScrollView(Vector2 workspacePosition) =>
            _workspace.ChangeCoordinatesTo(_scrollView.contentViewport, workspacePosition);

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

        /// <summary>
        /// Close any pixel art file that is currently open
        /// </summary>
        public void CloseFile ()
        {
            // Save existing artwork first
            if (CurrentFile != null)
                SaveFile();

            _target = null;
            CurrentFile = null;
            _editor.visible = false;
            _empty.visible = true;
            UpdateScrollView();
        }

        /// <summary>
        /// Open the given pixel art file in the editor
        /// </summary>
        public void OpenFile(PixelArt target)
        {
            // Already open?
            if (CurrentFile != null && target == _target)
                return;

            // If the given file is invalid then just close whatever we have open now
            if(null == target)
            {
                CloseFile();
                return;
            }

            _target = target;

            SetTitle(_target.name);            
            CurrentFile = PEFile.Load(AssetDatabase.GetAssetPath(target));

            CurrentTool = _toolSelection;
            CurrentTool.MarkDirtyRepaint();
            Canvas.MarkDirtyRepaint();

            CurrentLayer = CurrentFile.layers[0];
            CurrentFrame = CurrentFile.frames[0];

            UpdateLayers();

            _editor.visible = true;
            _empty.visible = false;
            _editor.MarkDirtyRepaint();
            UpdateScrollView();

            ZoomToFit();
        }

        /// <summary>
        /// Save the currently open file
        /// </summary>
        private void SaveFile()
        {
            if (null == CurrentFile || null == _target)
                return;

            CurrentFile.Save(AssetDatabase.GetAssetPath(_target));
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

        private void UpdateScrollView()
        {
            if (_scrollView == null)
                return;

            if (!IsEditing)
            {
                _scrollView.contentContainer.style.width = 0;
                _scrollView.contentContainer.style.height = 0;
                return;
            }

            // Dont update the scrollview until it has been laid out
            if (float.IsNaN(_scrollView.contentViewport.contentRect.width) ||
                float.IsNaN(_scrollView.contentViewport.contentRect.height))
                return;

            _scrollView.contentContainer.style.width = 
                Mathf.Max(
                    _scrollView.contentViewport.contentRect.width * 2.0f - CanvasWidth * Zoom, 
                    CanvasWidth * Zoom + _scrollView.contentViewport.contentRect.width);

            _scrollView.contentContainer.style.height = 
                Mathf.Max(
                    _scrollView.contentViewport.contentRect.height * 2.0f - CanvasHeight * Zoom, 
                    CanvasHeight * Zoom + _scrollView.contentViewport.contentRect.height);
        }

        /// <summary>
        /// Set the Zoom and ScrollOffset such that the content fits to the scroll view area.
        /// </summary>
        private void ZoomToFit ()
        {
            // If the scrollview isnt ready yet then wait till it is.  This mainly happens on 
            // a file load right when the window is opening.
            if(float.IsNaN(_scrollView.contentViewport.contentRect.width) ||
               float.IsNaN(_scrollView.contentViewport.contentRect.height))
            {
                UpdateScrollView();
                _scrollView.schedule.Execute(ZoomToFit);
                return;
            }

            // Adjust the zoom such that the content is 90% of the available view space.  Note that
            // the zoom changed handler is callled inline to ensure it is done before the scroll offset
            // is calculated.
            var zoom = _scrollView.contentViewport.contentRect.size * 0.9f / (Vector2)CanvasSize;
            _zoomSlider.SetValueWithoutNotify(Mathf.Min(zoom.x, zoom.y));
            OnZoomValueChanged(new ChangeEvent<float>());

            // Offset the scroll view to center the content
            ScrollOffset = (WorkspaceSize - _scrollView.contentViewport.contentRect.size) * 0.5f;
        }

        /// <summary>
        /// Called when the zoom changes by dragging the slider or directly setting the zoom falue
        /// </summary>
        private void OnZoomValueChanged(ChangeEvent<float> evt) =>
            OnZoomValueChanged(
                evt.previousValue, 
                evt.newValue, 
                ScrollOffset + _scrollView.contentViewport.contentRect.size * 0.5f);

        /// <summary>
        /// Called when the zoom value is changed to handle adjusting the viewport
        /// to keep the referencePosition in the same place on the canvas.
        /// </summary>
        private Vector2 OnZoomValueChanged(float oldZoom, float newZoom, Vector2 referencePosition)
        {
            Canvas.MarkDirtyRepaint();
            CurrentTool.MarkDirtyRepaint();
            RefreshCursor();

            // Resize the canvas.
            Canvas.style.width = CanvasWidth * Zoom;
            Canvas.style.height = CanvasHeight * Zoom;
            Canvas.MarkDirtyRepaint();

            // Determine where on the canvas the mouse was previously
            var oldWorkspaceSize = new Vector2(
                _scrollView.contentContainer.style.width.value.value,
                _scrollView.contentContainer.style.height.value.value);
            var oldCanvasSize = (Vector2)CanvasSize * oldZoom;
            var referenceCanvasRatio = (referencePosition - (oldWorkspaceSize - oldCanvasSize) * 0.5f) / oldCanvasSize;

            UpdateScrollView();

            // Position the cursor over the same pixel in the canvas that it was over before the zoom
            var newWorkspaceSize = new Vector2(
                _scrollView.contentContainer.style.width.value.value,
                _scrollView.contentContainer.style.height.value.value);
            var viewPosition = _workspace.ChangeCoordinatesTo(_scrollView.contentViewport, referencePosition);
            var newCanvasSize = (Vector2)CanvasSize * newZoom;
            referencePosition = (newWorkspaceSize - newCanvasSize) * 0.5f + referenceCanvasRatio * newCanvasSize;
            ScrollOffset = referencePosition - viewPosition;

            return referencePosition;
        }

        /// <summary>
        /// Handle the mouse wheel to zoom in/out
        /// </summary>
        private void OnWorkspaceWheel(WheelEvent evt)
        {
            var oldZoom = Zoom;
            _zoomSlider.SetValueWithoutNotify(Mathf.Clamp(
                Zoom * (evt.delta.y < 0 ? ZoomIncrementUp : ZoomIncrementDown), 
                ZoomMin, 
                ZoomMax));

            _lastMousePosition = OnZoomValueChanged(oldZoom, _zoomSlider.value, _lastMousePosition);

            evt.StopImmediatePropagation();
        }

        /// <summary>
        /// Handle a mouse button down within the workspace
        /// </summary>
        private void OnWorkspaceMouseDown(MouseDownEvent evt)
        {
            // Middle button is pan tool
            if((MouseButton)evt.button == MouseButton.MiddleMouse)
            {
                _previousTool = CurrentTool;
                CurrentTool = _toolPan;
            }

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

                // If middle button pan was active then return to the previous tool
                if ((MouseButton)evt.button == MouseButton.MiddleMouse && CurrentTool == _toolPan)
                    CurrentTool = _previousTool;
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
                // If middle button pan was active then return to the previous tool
                if ((MouseButton)_drawButton == MouseButton.MiddleMouse && CurrentTool == _toolPan)
                    CurrentTool = _previousTool;

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

            CurrentTool?.SetCursor(canvasPosition);
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
            zoomImage.image = PEUtils.LoadTexture("Assets/PixelEditor/Editor/Icons/ZoomIcon.psd");
            _toolbar.Add(zoomImage);

            _zoomSlider = new Slider();
            _zoomSlider.lowValue = ZoomMin;
            _zoomSlider.highValue = ZoomMax;
            _zoomSlider.AddToClassList("zoom");
            _zoomSlider.RegisterValueChangedCallback(OnZoomValueChanged);
            _toolbar.Add(_zoomSlider);

            var layerToggle = new PEImageToggle();
            layerToggle.checkedImage = PEUtils.LoadTexture("Assets/PixelEditor/Editor/Icons/LayerToggle.psd");
            layerToggle.value = true;
            layerToggle.onValueChanged = (v) => _layers.parent.visible = v;
            layerToggle.tooltip = "Toggle layers";
            _toolbar.Add(layerToggle);

            var gridToggle = new PEImageToggle();
            gridToggle.checkedImage = PEUtils.LoadTexture("Assets/PixelEditor/Editor/Icons/GridToggle.psd");
            gridToggle.value = true;
            gridToggle.onValueChanged = (v) => _grid.ShowPixels = v;
            gridToggle.tooltip = "Toggle pixel grid";
            _toolbar.Add(gridToggle);

            var checkerboardToggle = new PEImageToggle();
            checkerboardToggle.checkedImage = PEUtils.LoadTexture("Assets/PixelEditor/Editor/Icons/Grid.psd");
            checkerboardToggle.value = true;
            checkerboardToggle.onValueChanged = (v) =>
            {
                Canvas.ShowCheckerboard = v;
                Canvas.MarkDirtyRepaint();
            };
            checkerboardToggle.tooltip = "Toggle checkerboard";
            _toolbar.Add(checkerboardToggle);

            var saveButton = new ToolbarButton();
            saveButton.text = "Save";
            saveButton.clickable.clicked += SaveFile;
            _toolbar.Add(saveButton);
            _editor.Add(_toolbar);
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
            _foregroundColor.RegisterValueChangedCallback((e) => _editor.Focus());
            _toolbox.Add(_foregroundColor);

            _backgroundColor = new ColorField();
            _backgroundColor.showEyeDropper = false;
            _backgroundColor.value = Color.white;
            _backgroundColor.RegisterValueChangedCallback((e) => _editor.Focus());
            _toolbox.Add(_backgroundColor);

            _editor.Add(_toolbox);
        }

        private void CreateToolBoxButton(PETool tool, string image, string tooltip)
        {
            var button = new Image();
            button.image = PEUtils.LoadTexture(image);
            button.RegisterCallback<ClickEvent>((e) => CurrentTool = tool);
            button.userData = tool;
            button.tooltip = tooltip;
            _toolbox.Add(button);
        }

        private void AddLayer()
        {
            var addedLayer = CurrentFile.AddLayer();
            UpdateLayers();
            CurrentLayer = addedLayer;
            Canvas.MarkDirtyRepaint();
        }

        private void RemoveLayer()
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
        }

        private void UpdateLayers()
        {
            _layers.Clear();

            if (CurrentFile == null)
                return;

            foreach(var layer in CurrentFile.layers.OrderByDescending(l => l.order))
            {
                var layerElemnet = new VisualElement();
                layerElemnet.pickingMode = PickingMode.Position;
                layerElemnet.focusable = true;
                layerElemnet.AddToClassList("layer");
                layerElemnet.userData = layer;
                layerElemnet.RegisterCallback<ClickEvent>((e) => CurrentLayer = layer);

                if (layer.order == 0)
                layerElemnet.AddToClassList("selected");

                var visibilityToggle = new PEImageToggle();
                visibilityToggle.checkedImage = PEUtils.LoadTexture("!scenevis_visible");
                visibilityToggle.uncheckedImage = PEUtils.LoadTexture("!scenevis_hidden");
                visibilityToggle.onValueChanged = (v) => { layer.visible = v; Canvas.MarkDirtyRepaint(); };
                visibilityToggle.value = layer.visible;
                visibilityToggle.tooltip = "Toggle layer visibility";
                layerElemnet.Add(visibilityToggle);

                var layerPreview = new Image();
                layerPreview.AddToClassList("layerPreview");
                layerElemnet.Add(layerPreview);

                var layerName = new Label(layer.name);
                layerElemnet.Add(layerName);
                
                _layers.Add(layerElemnet);
            }
        }

        private void OnWorkspaceKeyDown(KeyDownEvent evt)
        {
            // Send the key to the current tool
            if (!_currentTool?.OnKeyDown(PEKeyEvent.Create(evt)) ?? true)
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
                        CurrentTool = _toolSelection;
                        _toolSelection.Selection = new RectInt(0, 0, CanvasWidth, CanvasHeight);
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
                    CurrentTool = _toolEyeDropper;
                    evt.StopImmediatePropagation();
                    break;

                // Change to eraser tool
                case KeyCode.E:
                    CurrentTool = _toolEraser;
                    evt.StopImmediatePropagation();
                    break;

                // Change to pencil tool
                case KeyCode.B:
                    CurrentTool = _toolPencil;
                    evt.StopImmediatePropagation();
                    break;

                // Change to selection tool
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

        private void OnWorkspaceMouseLeave(MouseLeaveEvent evt)
        {
            _workspaceCursor.visible = false;
        }


        private void CreateLayersPopup()
        {
            var popup = new UnityEngine.UIElements.PopupWindow();
            popup.text = "Layers";
            popup.focusable = true;
            popup.AddToClassList("layersPopup");
            _editor.Add(popup);

            var toolbar = new VisualElement();
            toolbar.AddToClassList("layersToolbar");
            popup.Add(toolbar);

            toolbar.Add(PEUtils.CreateImageButton(
                "Assets/PixelEditor/Editor/Icons/LayerAdd.psd",
                "Create a new layer",
                AddLayer));

            toolbar.Add(PEUtils.CreateImageButton(
                "Assets/PixelEditor/Editor/Icons/Delete.psd",
                "Delete layer",
                RemoveLayer));

            _layers = new VisualElement();
            _layers.AddToClassList("layers");
            popup.Add(_layers);

            UpdateLayers();
        }
    }
}
