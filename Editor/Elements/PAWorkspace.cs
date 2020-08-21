using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PAWorkspace : VisualElement
    {
        private Slider _zoomSlider = null;
        private ScrollView _scrollView;
        private ColorField _foregroundColor = null;
        private ColorField _backgroundColor = null;
        private PAReorderableList _layers = null;
        private PAReorderableList _frames = null;

        public PixelArt Target { get; private set; }

        public PAEditor Editor { get; private set; }
        public PACanvas Canvas { get; private set; }

        public VisualElement Toolbar { get; private set; }
        public VisualElement Toolbox { get; private set; }

        public Vector2 ViewportSize => new Vector2(
            _scrollView.contentViewport.contentRect.width,
            _scrollView.contentViewport.contentRect.height);

        public Vector2 ViewportOffset {
            get => _scrollView.scrollOffset;
            set {
                _scrollView.scrollOffset = value;
                _scrollView.MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Handle gui messages
        /// </summary>
        internal void OnGUI() => Canvas.OnGUI();

        public PAWorkspace(PAEditor editor)
        {
            Editor = editor;

            // Toolbox on left side of top pane
            var toolbox = CreateToolBox();
            Add(toolbox);

            var centerPane = new VisualElement { name = "CenterPane" };
            Add(centerPane);

            _scrollView = new ScrollView { name = "TopPane" };
            _scrollView.showHorizontal = true;
            _scrollView.showVertical = true;
            centerPane.Add(_scrollView);

            var bottomPane = new VisualElement { name = "BottomPane" };
            centerPane.Add(bottomPane);

            Canvas = new PACanvas(this) { name = "Canvas" };
            //Canvas.StretchToParentSize();
            Canvas.ZoomChangedEvent += () => _zoomSlider?.SetValueWithoutNotify(Canvas.Zoom);
            Canvas.ToolChangedEvent += OnToolChanged;
            Canvas.ForegroundColorChangedEvent += () => _foregroundColor.value = Canvas.ForegroundColor;
            Canvas.BackgroundColorChangedEvent += () => _backgroundColor.value = Canvas.BackgroundColor;
            Canvas.SelectedLayerChangedEvent += () => _layers.Select(Canvas.SelectedLayer?.Item);
            Canvas.SelectedFrameChangedEvent += () => _frames.Select(Canvas.SelectedFrame?.Item); 
            _scrollView.Add(Canvas);

            // Right pane
            var rightPane = new VisualElement { name = "RightPane" };
            Add(rightPane);

            var previewPane = new VisualElement { name = "PreviewPane" };
            rightPane.Add(previewPane);

            var preview = new Image { name = "Preview" };
            previewPane.Add(preview);

            var layersPane = new VisualElement { name = "LayersPane" };
            rightPane.Add(layersPane);

            var layersToolbar = new VisualElement { name = "LayersToolbar" };
            layersPane.Add(layersToolbar);

            var toolbarSpacer = new VisualElement();
            toolbarSpacer.AddToClassList("spacer");

            layersToolbar.Add(toolbarSpacer);
            layersToolbar.Add(PAUtils.CreateImageButton("LayerAdd.psd", "Create a new layer", AddLayer));
            layersToolbar.Add(PAUtils.CreateImageButton("Delete.psd", "Delete layer", RemoveLayer));

            var layersScrollView = new ScrollView();
            layersPane.Add(layersScrollView);

            _layers = new PAReorderableList() { name = "Layers" };
            _layers.onItemMoved += (oldIndex, newIndex) =>
            {
                for (int itemIndex = 0; itemIndex < _layers.itemCount; itemIndex++)
                    ((PALayerItem)_layers.ItemAt(itemIndex)).Layer.order = _layers.itemCount - itemIndex - 1;

                Canvas.RefreshImage();
            };
            _layers.onItemSelected += (i) => Canvas.SelectedLayer = ((PALayerItem)_layers.ItemAt(i)).Layer;
            layersScrollView.contentContainer.Add(_layers);

            toolbarSpacer = new VisualElement();
            toolbarSpacer.AddToClassList("spacer");

            var framesToolbar = new VisualElement();
            framesToolbar.name = "FramesToolbar";
            framesToolbar.Add(toolbarSpacer);
            framesToolbar.Add(PAUtils.CreateImageButton("LayerAdd.psd", "Create a new frame", AddFrame));
            framesToolbar.Add(PAUtils.CreateImageButton("Duplicate.psd", "Duplicate selected frame", DuplicatFrame));
            framesToolbar.Add(PAUtils.CreateImageButton("Delete.psd", "Delete layer", RemoveFrame));
            bottomPane.Add(framesToolbar);

            var framesScrollView = new ScrollView();
            framesScrollView.showHorizontal = true;
            framesScrollView.showVertical = false;
            bottomPane.Add(framesScrollView);

            _frames = new PAReorderableList() { name = "Frames" };
            _frames.direction = ReorderableListDirection.Horizontal;
            _frames.onItemMoved += (oldIndex, newIndex) =>
            {
                for (int itemIndex = 0; itemIndex < _frames.itemCount; itemIndex++)
                    ((PAFrame)_frames.ItemAt(itemIndex).userData).order = itemIndex;

                Canvas.RefreshImage();
            };
            _frames.onItemSelected += (i) => Canvas.SelectedFrame = ((PAFrameItem)_frames.ItemAt(i)).Frame;

            framesScrollView.contentContainer.Add(_frames);

            RegisterCallback<KeyDownEvent>(OnKeyDown);

            CreateToolbar();            
        }

        /// <summary>
        /// Convert a canvas position to a viewport position
        /// </summary>
        public Vector2 CanvasToViewport(Vector2 canvasPosition) =>
            Canvas.ChangeCoordinatesTo(_scrollView.contentViewport, canvasPosition);

        /// <summary>
        /// Convert a viewport position to a canvas position
        /// </summary>
        public Vector2 ViewportToCanvas(Vector2 viewportPosition) => ViewportOffset + viewportPosition;


        /// <summary>
        /// Open the given pixel art file in the editor
        /// </summary>
        public void OpenFile(PixelArt target)
        {
            Target = target;

            Canvas.File = PAFile.Load(AssetDatabase.GetAssetPath(target));
            Canvas.SelectedTool = Canvas.PencilTool;

            RefreshFrameList();
            RefreshLayersList();

            Canvas.ZoomToFit();

            EditorApplication.quitting += CloseFile;
        }

        public void SaveFile()
        {
            if (Target == null || null == Canvas.File)
                return;

            Canvas.File.Save(AssetDatabase.GetAssetPath(Target));
            AssetDatabase.Refresh();
        }

        public void CloseFile()
        {
            // Save existing artwork first
            SaveFile();

            Target = null;
            Canvas.File = null;

            EditorApplication.quitting -= CloseFile;
        }


        /// <summary>
        /// Create the toolbox
        /// </summary>
        private VisualElement CreateToolBox()
        {
            Toolbox = new VisualElement { name = "Toolbox" };

            // Tool buttons
            var selectionToolButton = PAUtils.CreateImageButton("SelectionTool.psd", "Rectangular Marquee Tool (M)", () => Canvas.SelectedTool = Canvas.SelectionTool);
            selectionToolButton.userData = typeof(PASelectionTool);
            Toolbox.Add(selectionToolButton);

            var pencilToolButton = PAUtils.CreateImageButton("PencilTool.psd", "Pencil Tool (B)", () => Canvas.SelectedTool = Canvas.PencilTool);
            pencilToolButton.userData = typeof(PAPencilTool);
            Toolbox.Add(pencilToolButton);

            var eraserToolButton = PAUtils.CreateImageButton("EraserTool.psd", "Eraser Tool (E)", () => Canvas.SelectedTool = Canvas.EraserTool);
            eraserToolButton.userData = typeof(PAEraserTool);
            Toolbox.Add(eraserToolButton);

            var eyeDropperToolButton = PAUtils.CreateImageButton("EyeDropperTool.psd", "Eyedropper Tool (I)", () => Canvas.SelectedTool = Canvas.EyeDropperTool);
            eyeDropperToolButton.userData = typeof(PAEyeDropperTool);
            Toolbox.Add(eyeDropperToolButton);

            // Foreground color selector
            _foregroundColor = new ColorField();
            _foregroundColor.showEyeDropper = false;
            _foregroundColor.value = Color.white;
            _foregroundColor.RegisterValueChangedCallback((evt) => { Canvas.ForegroundColor = evt.newValue; });
            Toolbox.Add(_foregroundColor);

            // Background color selector
            _backgroundColor = new ColorField();
            _backgroundColor.showEyeDropper = false;
            _backgroundColor.value = Color.white;
            _backgroundColor.RegisterValueChangedCallback((evt) => { Canvas.BackgroundColor = evt.newValue; });
            Toolbox.Add(_backgroundColor);

            return Toolbox;
        }

        /// <summary>
        /// Refresh the list of layers
        /// </summary>
        private void RefreshLayersList()
        {
            _layers.RemoveAllItems();

            if (Canvas.File == null)
                return;

            foreach (var layer in Canvas.File.layers.OrderByDescending(l => l.order))
                _layers.AddItem(new PALayerItem(Canvas, layer));

            _layers.Select(0);
        }

        /// <summary>
        /// Refresh the list of frames
        /// </summary>
        private void RefreshFrameList()
        {
            _frames.RemoveAllItems();

            if (null == Canvas.File)
                return;

            foreach (var frame in Canvas.File.frames.OrderBy(f => f.order))
                _frames.AddItem(new PAFrameItem(frame));
        }

        private void AddLayer()
        {
            var addedLayer = Canvas.File.AddLayer();
            RefreshLayersList();
            Canvas.SelectedLayer = addedLayer;
        }

        private void RemoveLayer()
        {
            // Dont allow the last layer to be removed
            if (Canvas.File.layers.Count < 2)
                return;

            var order = Canvas.SelectedLayer.order;
            Canvas.File.RemoveLayer(Canvas.SelectedLayer);
            RefreshLayersList();
            _layers.Select(Mathf.Min(order, Canvas.File.layers.Count - 1));

            Canvas.RefreshImage();
        }

        /// <summary>
        /// Add a new empty frame
        /// </summary>
        private void AddFrame()
        {
            Canvas.File.AddFrame(Canvas.CurrentAnimation);
            RefreshFrameList();
        }

        /// <summary>
        /// Duplicate the selected frame
        /// </summary>
        private void DuplicatFrame()
        {
            if (Canvas.File == null)
                return;

            var frame = Canvas.File.InsertFrame(Canvas.CurrentAnimation, Canvas.SelectedFrame.order + 1);
            Canvas.File.images.AddRange(
                Canvas.File.images.Where(i => i.frame == Canvas.SelectedFrame).Select(i => new PAImage
                {
                    frame = frame,
                    layer = i.layer,
                    texture = i.texture.Clone()
                }).ToList());

            RefreshFrameList();
            Canvas.SelectedFrame = frame;
        }

        /// <summary>
        /// Remove the selected frame
        /// </summary>
        private void RemoveFrame()
        {
            // Dont allow the last layer to be removed
            if (Canvas.File.frames.Count < 2)
                return;

            var order = Canvas.SelectedFrame.order;
            Canvas.File.RemoveFrame(Canvas.SelectedFrame);
            RefreshFrameList();
            _frames.Select(Mathf.Min(order, Canvas.File.frames.Count - 1));
            Canvas.RefreshImage();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Send the key to the current tool
            if (!Canvas.SelectedTool?.OnKeyDown(PAKeyEvent.Create(evt)) ?? true)
            {
                evt.StopImmediatePropagation();
                return;
            }

            // Handle window level key commands
            switch (evt.keyCode)
            {
                case KeyCode.F:
                    Canvas.ZoomToFit();
                    break;

                case KeyCode.A:
                    // Ctrl+a = select all
                    if (evt.ctrlKey)
                    {
                        Canvas.SelectedTool = Canvas.SelectionTool;
                        Canvas.SelectionTool.Selection = new RectInt(0, 0, Canvas.ImageWidth, Canvas.ImageHeight);
                        evt.StopImmediatePropagation();
                    }
                    break;

                // Swap foreground and background colors
                case KeyCode.X:
                {
                    var swap = Canvas.ForegroundColor;
                    Canvas.ForegroundColor = Canvas.BackgroundColor;
                    Canvas.BackgroundColor = swap;
                    evt.StopImmediatePropagation();
                    break;
                }

                // Change to eyedropper tool
                case KeyCode.I:
                    Canvas.SelectedTool = Canvas.EyeDropperTool;
                    evt.StopImmediatePropagation();
                    break;

                // Change to eraser tool
                case KeyCode.E:
                    Canvas.SelectedTool = Canvas.EraserTool;
                    evt.StopImmediatePropagation();
                    break;

                // Change to pencil tool
                case KeyCode.B:
                    Canvas.SelectedTool = Canvas.PencilTool;
                    evt.StopImmediatePropagation();
                    break;

                // Change to selection tool
                case KeyCode.M:
                    Canvas.SelectedTool = Canvas.SelectionTool;
                    evt.StopImmediatePropagation();
                    break;
            }
        }

        /// <summary>
        /// Handle the selected tool changing by ensuring the matching
        /// toolbox button is selected.
        /// </summary>
        private void OnToolChanged()
        {
            foreach (var child in Toolbox.Children())
            {
                if ((Type)child.userData == Canvas.SelectedTool.GetType())
                    child.AddToClassList("selected");
                else
                    child.RemoveFromClassList("selected");
            }
        }

        private void CreateToolbar ()
        {
            Toolbar = new VisualElement { name = "WorkspaceToolbar" };

            var toolbarSpacer = new VisualElement();
            toolbarSpacer.style.flexGrow = 1.0f;
            Toolbar.Add(toolbarSpacer);

            var zoomImage = new Image();
            zoomImage.style.width = 16;
            zoomImage.style.height = 16;
            zoomImage.image = PAUtils.LoadImage("ZoomIcon.psd");
            Toolbar.Add(zoomImage);

            _zoomSlider = new Slider { name = "ZoomSlider" };
            _zoomSlider.lowValue = PACanvas.ZoomMin;
            _zoomSlider.highValue = PACanvas.ZoomMax;
            _zoomSlider.AddToClassList("zoom");
            _zoomSlider.RegisterValueChangedCallback((e) => Canvas.SetZoom(e.newValue, ViewportToCanvas(ViewportSize * 0.5f)));
            Toolbar.Add(_zoomSlider);

            var framesToggle = new PAImageToggle();
            framesToggle.checkedImage = PAUtils.LoadImage("FramesToggle.psd");
            framesToggle.value = true;
            framesToggle.onValueChanged = (v) => _frames.parent.visible = v;
            framesToggle.tooltip = "Toggle Frames";
            Toolbar.Add(framesToggle);

            var layerToggle = new PAImageToggle();
            layerToggle.checkedImage = PAUtils.LoadImage("LayerToggle.psd");
            layerToggle.value = true;
            layerToggle.onValueChanged = (v) => _layers.parent.parent.visible = v;
            layerToggle.tooltip = "Toggle layers";
            Toolbar.Add(layerToggle);

            var gridToggle = new PAImageToggle();
            gridToggle.checkedImage = PAUtils.LoadImage("GridToggle.psd");
            gridToggle.value = true;
            gridToggle.onValueChanged = (v) => Canvas.Grid.ShowPixels = v;
            gridToggle.tooltip = "Toggle pixel grid";
            Toolbar.Add(gridToggle);

            var checkerboardToggle = new PAImageToggle();
            checkerboardToggle.checkedImage = PAUtils.LoadImage("Grid.psd");
            checkerboardToggle.value = true;
            checkerboardToggle.onValueChanged = (v) =>
            {
                Canvas.ShowCheckerboard = v;
                Canvas.RefreshImage();
            };
            checkerboardToggle.tooltip = "Toggle checkerboard";
            Toolbar.Add(checkerboardToggle);

            var saveButton = new ToolbarButton();
            saveButton.text = "Save";
            saveButton.clickable.clicked += SaveFile;
            Toolbar.Add(saveButton);

            // Add the toolbar to the main toolbar
            Editor.Toolbar.Add(Toolbar);
        }
    }
}
