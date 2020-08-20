using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace NoZ.PA
{
    /// <summary>
    /// PixelArt editor window
    /// </summary>
    internal class PAEditor : EditorWindow
    {
        private PixelArt _target = null;

        private ColorField _foregroundColor = null;
        private ColorField _backgroundColor = null;
        private Slider _zoomSlider = null;
        private PAReorderableList _layers = null;
        private PAReorderableList _frames = null;

        public PAWorkspace Workspace { get; private set; }
        public VisualElement Toolbox { get; private set; }
        public Toolbar Toolbar { get; private set; }

        [MenuItem("Window/2D/PixelArt Editor")]
        public static void OpenWindow ()
        {
            GetWindow<PAEditor>();
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (Selection.activeObject as PixelArt)
            {
                GetWindow<PAEditor>().OpenFile((PixelArt)Selection.activeObject);
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            Workspace?.OnGUI();
        }

        public void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;

            SetTitle("PixelArt Editor");

            // Add style sheet
            rootVisualElement.AddStyleSheetPath("PAEditor");
            rootVisualElement.name = "Editor";
            rootVisualElement.focusable = true;

            var toolbar = CreateToolbar();
            toolbar.name = "Toolbar";
            rootVisualElement.Add(toolbar);

            var topPane = new VisualElement { name = "TopPane" };
            rootVisualElement.Add(topPane);

            var toolbox = CreateToolBox();
            topPane.Add(toolbox);

            var rightSplitter = new VisualElement(); //  new TwoPaneSplitView(1, 200, TwoPaneSplitViewOrientation.Horizontal);
            rightSplitter.name = "RightSplitter";
            topPane.Add(rightSplitter);

            var centerPane = new VisualElement { name = "CenterPane" };
            rightSplitter.Add(centerPane);

            var rightPane = new VisualElement { name = "RightPane" };
            rightSplitter.Add(rightPane);

            Workspace = new PAWorkspace();
            Workspace.name = "Workspace";
            Workspace.ZoomChangedEvent += () => _zoomSlider.SetValueWithoutNotify(Workspace.Zoom);
            Workspace.ToolChangedEvent += OnToolChanged;
            Workspace.ForegroundColorChangedEvent += () => _foregroundColor.value = Workspace.ForegroundColor;
            Workspace.BackgroundColorChangedEvent += () => _backgroundColor.value = Workspace.BackgroundColor;
            Workspace.SelectedLayerChangedEvent += () => _layers.Select(Workspace.SelectedLayer.Item);
            Workspace.SelectedFrameChangedEvent += OnSelectedFrameChanged;
            centerPane.Add(Workspace);

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

                Workspace.RefreshImage();
            };
            _layers.onItemSelected += (i) => Workspace.SelectedLayer = ((PALayerItem)_layers.ItemAt(i)).Layer;
            layersScrollView.contentContainer.Add(_layers);

            var bottomPane = new VisualElement { name = "BottomPane" };
            centerPane.Add(bottomPane);

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

                Workspace.RefreshImage();
            };
            _frames.onItemSelected += (i) => Workspace.SelectedFrame = ((PAFrameItem)_frames.ItemAt(i)).Frame;

            framesScrollView.contentContainer.Add(_frames);

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

            // Load the Saved preferences
            Workspace.ForegroundColor = ColorUtility.TryParseHtmlString(EditorPrefs.GetString("PixelEditor.ForegroundColor"), out var foregroundColor) ?
                foregroundColor :
                Color.white;
            Workspace.BackgroundColor = ColorUtility.TryParseHtmlString(EditorPrefs.GetString("PixelEditor.BackgroundColor"), out var backgroundColor) ?
                backgroundColor :
                Color.white;

            //var openButton = new Button();
            //openButton.text = "Open PixelArt";
            //openButton.clickable.clicked += () => { EditorGUIUtility.ShowObjectPicker<PixelArt>(_target, false, null, 0); };
            //_empty.Add(openButton);

            if (_target != null)
                OpenFile(_target);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;

            // Save the colors
            EditorPrefs.SetString("PixelEditor.ForegroundColor", $"#{ColorUtility.ToHtmlStringRGBA(Workspace.ForegroundColor)}");
            EditorPrefs.SetString("PixelEditor.BackgroundColor", $"#{ColorUtility.ToHtmlStringRGBA(Workspace.BackgroundColor)}");
        }

        private void OnFocus()
        {
            if (Workspace != null)
                Workspace.SetFocusToCanvas();
        }

        private void Update()
        {
            // Automatically close the current file if the asset is deleted
            if (_target == null && Workspace.File != null)
                CloseFile();
           
            // Handle asset renaming
            if(_target != null && Workspace.File != null && _target.name != Workspace.File.name)
            {
                Workspace.File.name = _target.name;
                SetTitle(_target.name);
            }
        }

        /// <summary>
        /// Set the window title with with the pixel art icon
        /// </summary>
        private void SetTitle(string title) =>
            titleContent = new GUIContent(
                title,
                PAUtils.LoadImage("PixelArtEditor.psd"));


        /// <summary>
        /// Open the given pixel art file in the editor
        /// </summary>
        public void OpenFile(PixelArt target)
        {
            _target = target;

            Workspace.File = PAFile.Load(AssetDatabase.GetAssetPath(target));

            Workspace.SelectedTool = Workspace.PencilTool;

            RefreshFrameList();
            RefreshLayersList();

            //UpdateScrollView();

            Workspace.SelectedFrame = Workspace.File.frames[0];
            Workspace.SelectedLayer = Workspace.File.layers.Last();

            Workspace.ZoomToFit();

            EditorApplication.quitting += CloseFile;

            SetTitle(target.name);
        }

        public void SaveFile()
        {
            if (_target == null || null == Workspace.File)
                return;

            Workspace.File.Save(AssetDatabase.GetAssetPath(_target));
            AssetDatabase.Refresh();
        }

        public void CloseFile()
        {
            // Save existing artwork first
            SaveFile();

            _target = null;
            Workspace.File = null;

            EditorApplication.quitting -= CloseFile;
        }

        private void OnUndoRedo()
        {
            //_layer.MarkDirtyRepaint();
            ///CurrentTool.MarkDirtyRepaint();
        }



        /// <summary>
        /// Called when the selected frame changes
        /// </summary>
        private void OnSelectedFrameChanged()
        {
            _frames.Select(Workspace.SelectedFrame.Item);

            // Refresh the layer prevew images to use the new frame
            foreach (var layer in Workspace.File.layers)
                layer.Item?.RefreshPreview(Workspace.SelectedFrame);
        }

        /// <summary>
        /// Handle the selected tool changing by ensuring the matching
        /// toolbox button is selected.
        /// </summary>
        private void OnToolChanged()
        {
            foreach (var child in Toolbox.Children())
            {
                if ((Type)child.userData == Workspace.SelectedTool.GetType())
                    child.AddToClassList("selected");
                else
                    child.RemoveFromClassList("selected");
            }
        }

        /// <summary>
        /// Create the toolbar at the top of the edit view
        /// </summary>
        private Toolbar CreateToolbar()
        {
            Toolbar = new Toolbar();
            Toolbar.pickingMode = PickingMode.Position;
            Toolbar.AddToClassList("toolbar");

            var modeMenu = new ToolbarMenu();
            modeMenu.text = "Pixel Editor";
            modeMenu.menu.AppendAction("Pixel Editor", (a) => { }, (a) => DropdownMenuAction.Status.Checked);
            modeMenu.menu.AppendAction("Bone Editor", (a) => { }, (a) => DropdownMenuAction.Status.Normal);
            Toolbar.Add(modeMenu);

            var toolbarSpacer = new VisualElement();
            toolbarSpacer.style.flexGrow = 1.0f;
            Toolbar.Add(toolbarSpacer);

            var zoomImage = new Image();
            zoomImage.style.width = 16;
            zoomImage.style.height = 16;
            zoomImage.image = PAUtils.LoadImage("ZoomIcon.psd");
            Toolbar.Add(zoomImage);

            _zoomSlider = new Slider { name = "ZoomSlider" };
            _zoomSlider.lowValue = PAWorkspace.ZoomMin;
            _zoomSlider.highValue = PAWorkspace.ZoomMax;
            _zoomSlider.AddToClassList("zoom");
            _zoomSlider.RegisterValueChangedCallback((e) => Workspace.SetZoom(e.newValue, Workspace.ViewportToCanvas(Workspace.ViewportSize * 0.5f)));
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
            gridToggle.onValueChanged = (v) => Workspace.Grid.ShowPixels = v;
            gridToggle.tooltip = "Toggle pixel grid";
            Toolbar.Add(gridToggle);

            var checkerboardToggle = new PAImageToggle();
            checkerboardToggle.checkedImage = PAUtils.LoadImage("Grid.psd");
            checkerboardToggle.value = true;
            checkerboardToggle.onValueChanged = (v) =>
            {
                Workspace.ShowCheckerboard = v;
                Workspace.RefreshImage();
            };
            checkerboardToggle.tooltip = "Toggle checkerboard";
            Toolbar.Add(checkerboardToggle);

            var saveButton = new ToolbarButton();
            saveButton.text = "Save";
            saveButton.clickable.clicked += SaveFile;
            Toolbar.Add(saveButton);

            return Toolbar;
        }

        /// <summary>
        /// Create the toolbox
        /// </summary>
        private VisualElement CreateToolBox()
        {
            Toolbox = new VisualElement { name = "Toolbox" };

            // Tool buttons
            var selectionToolButton = PAUtils.CreateImageButton("SelectionTool.psd", "Rectangular Marquee Tool (M)", () => Workspace.SelectedTool = Workspace.SelectionTool);
            selectionToolButton.userData = typeof(PASelectionTool);
            Toolbox.Add(selectionToolButton);

            var pencilToolButton = PAUtils.CreateImageButton("PencilTool.psd", "Pencil Tool (B)", () => Workspace.SelectedTool = Workspace.PencilTool);
            pencilToolButton.userData = typeof(PAPencilTool);
            Toolbox.Add(pencilToolButton);

            var eraserToolButton = PAUtils.CreateImageButton("EraserTool.psd", "Eraser Tool (E)", () => Workspace.SelectedTool = Workspace.EraserTool);
            eraserToolButton.userData = typeof(PAEraserTool);
            Toolbox.Add(eraserToolButton);

            var eyeDropperToolButton = PAUtils.CreateImageButton("EyeDropperTool.psd", "Eyedropper Tool (I)", () => Workspace.SelectedTool = Workspace.EyeDropperTool);
            eyeDropperToolButton.userData = typeof(PAEyeDropperTool);
            Toolbox.Add(eyeDropperToolButton);

            // Foreground color selector
            _foregroundColor = new ColorField();
            _foregroundColor.showEyeDropper = false;
            _foregroundColor.value = Color.white;
            _foregroundColor.RegisterValueChangedCallback((evt) => { Workspace.ForegroundColor = evt.newValue; });
            Toolbox.Add(_foregroundColor);

            // Background color selector
            _backgroundColor = new ColorField();
            _backgroundColor.showEyeDropper = false;
            _backgroundColor.value = Color.white;
            _backgroundColor.RegisterValueChangedCallback((evt) => { Workspace.BackgroundColor = evt.newValue; });
            Toolbox.Add(_backgroundColor);

            return Toolbox;
        }

        /// <summary>
        /// Refresh the list of layers
        /// </summary>
        private void RefreshLayersList()
        {
            _layers.RemoveAllItems();

            if (Workspace.File == null)
                return;

            foreach (var layer in Workspace.File.layers.OrderByDescending(l => l.order))
                _layers.AddItem(new PALayerItem(Workspace, layer));

            _layers.Select(0);
        }

        /// <summary>
        /// Refresh the list of frames
        /// </summary>
        private void RefreshFrameList()
        {
            _frames.RemoveAllItems();

            if (null == Workspace.File)
                return;

            foreach (var frame in Workspace.File.frames.OrderBy(f => f.order))
                _frames.AddItem(new PAFrameItem(frame));
        }

        private void AddLayer()
        {
            var addedLayer = Workspace.File.AddLayer();
            RefreshLayersList();
            Workspace.SelectedLayer = addedLayer;
        }

        private void RemoveLayer()
        {
            // Dont allow the last layer to be removed
            if (Workspace.File.layers.Count < 2)
                return;

            var order = Workspace.SelectedLayer.order;
            Workspace.File.RemoveLayer(Workspace.SelectedLayer);
            RefreshLayersList();
            _layers.Select(Mathf.Min(order, Workspace.File.layers.Count - 1));

            Workspace.RefreshImage();
        }

        /// <summary>
        /// Add a new empty frame
        /// </summary>
        private void AddFrame()
        {
            Workspace.File.AddFrame(Workspace.CurrentAnimation);
            RefreshFrameList();
        }

        /// <summary>
        /// Duplicate the selected frame
        /// </summary>
        private void DuplicatFrame()
        {
            if (Workspace.File == null)
                return;

            var frame = Workspace.File.InsertFrame(Workspace.CurrentAnimation, Workspace.SelectedFrame.order + 1);
            Workspace.File.images.AddRange(
                Workspace.File.images.Where(i => i.frame == Workspace.SelectedFrame).Select(i => new PAImage
                {
                    frame = frame,
                    layer = i.layer,
                    texture = i.texture.Clone()
                }).ToList());

            RefreshFrameList();
            Workspace.SelectedFrame = frame;
        }

        /// <summary>
        /// Remove the selected frame
        /// </summary>
        private void RemoveFrame()
        {
            // Dont allow the last layer to be removed
            if (Workspace.File.frames.Count < 2)
                return;

            var order = Workspace.SelectedFrame.order;
            Workspace.File.RemoveFrame(Workspace.SelectedFrame);
            RefreshFrameList();
            _frames.Select(Mathf.Min(order, Workspace.File.frames.Count - 1));
            Workspace.RefreshImage();
        }


        private void OnKeyDown(KeyDownEvent evt)
        {
            // Send the key to the current tool
            if (!Workspace.SelectedTool?.OnKeyDown(PAKeyEvent.Create(evt)) ?? true)
            {
                evt.StopImmediatePropagation();
                return;
            }

            // Handle window level key commands
            switch (evt.keyCode)
            {
                case KeyCode.F:
                    Workspace.ZoomToFit();
                    break;

                case KeyCode.A:
                    // Ctrl+a = select all
                    if (evt.ctrlKey)
                    {
                        Workspace.SelectedTool = Workspace.SelectionTool;
                        Workspace.SelectionTool.Selection = new RectInt(0, 0, Workspace.ImageWidth, Workspace.ImageHeight);
                        evt.StopImmediatePropagation();
                    }
                    break;

                // Swap foreground and background colors
                case KeyCode.X:
                {
                    var swap = Workspace.ForegroundColor;
                    Workspace.ForegroundColor = Workspace.BackgroundColor;
                    Workspace.BackgroundColor = swap;
                    evt.StopImmediatePropagation();
                    break;
                }

                // Change to eyedropper tool
                case KeyCode.I:
                    Workspace.SelectedTool = Workspace.EyeDropperTool;
                    evt.StopImmediatePropagation();
                    break;

                // Change to eraser tool
                case KeyCode.E:
                    Workspace.SelectedTool = Workspace.EraserTool;
                    evt.StopImmediatePropagation();
                    break;

                // Change to pencil tool
                case KeyCode.B:
                    Workspace.SelectedTool = Workspace.PencilTool;
                    evt.StopImmediatePropagation();
                    break;

                // Change to selection tool
                case KeyCode.M:
                    Workspace.SelectedTool = Workspace.SelectionTool;
                    evt.StopImmediatePropagation();
                    break;
            }
        }

    }
}
