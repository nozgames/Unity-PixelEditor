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
        private VisualElement _bottomPane = null;
        private VisualElement _layersPane = null;
        private PAReorderableList _layers = null;
        private PAReorderableList _frames = null;
        private ToolbarMenu _animations = null;
        private VisualElement _animationOptionsButton = null;
        private VisualElement _playButton = null;
        private PAFrame _playFrame = null;
        private IVisualElementScheduledItem _playingScheduledItem;
        private VisualElement _addLayerButton = null;
        private VisualElement _deleteLayerButton = null;
        private VisualElement _deleteFrameButton = null;
        private VisualElement _rightPane;
        private VisualElement _framesToolbar;

        public PixelArt Target { get; private set; }

        public PAEditor Editor { get; private set; }
        public PACanvas Canvas { get; private set; }

        public VisualElement Toolbar { get; private set; }
        public VisualElement Toolbox { get; private set; }

        public bool IsPlaying { get; private set; }

        public PAUndo Undo { get; private set; }

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

            RegisterCallback<KeyDownEvent>(OnKeyDown);

            Undo = PAUndo.CreateInstance(this);

            // Toolbox on left side of top pane
            var toolbox = CreateToolBox();
            Add(toolbox);

            var centerPane = new VisualElement { name = "CenterPane" };
            Add(centerPane);

            _scrollView = new ScrollView { name = "TopPane" };
            _scrollView.showHorizontal = true;
            _scrollView.showVertical = true;
            centerPane.Add(_scrollView);

            _bottomPane = new VisualElement { name = "BottomPane" };
            centerPane.Add(_bottomPane);

            Canvas = new PACanvas(this) { name = "Canvas" };
            Canvas.ZoomChangedEvent += () => _zoomSlider?.SetValueWithoutNotify(Canvas.Zoom);
            Canvas.ToolChangedEvent += OnToolChanged;
            Canvas.ForegroundColorChangedEvent += () => _foregroundColor.SetValueWithoutNotify(Canvas.ForegroundColor);
            Canvas.BackgroundColorChangedEvent += () => _backgroundColor.SetValueWithoutNotify(Canvas.BackgroundColor);
            Canvas.SelectedLayerChangedEvent += () => _layers.Select(Canvas.SelectedLayer?.Item);
            Canvas.SelectedFrameChangedEvent += () => _frames.Select(Canvas.SelectedFrame?.Item);
            Canvas.SelectedAnimationChangedEvent += () =>
            {
                RefreshAnimationList();
                RefreshFrameList();
                Canvas.RefreshImage();
                Canvas.Focus();
            };
            _scrollView.Add(Canvas);

            // Right pane
            _rightPane = new VisualElement { name = "RightPane" };
            Add(_rightPane);

            var previewPane = new VisualElement { name = "PreviewPane" };
            _rightPane.Add(previewPane);

            var preview = new Image { name = "Preview" };
            previewPane.Add(preview);

            _layersPane = new VisualElement { name = "LayersPane" };
            _rightPane.Add(_layersPane);

            var layersToolbar = new VisualElement { name = "LayersToolbar" };
            _layersPane.Add(layersToolbar);

            var toolbarSpacer = new VisualElement();
            toolbarSpacer.AddToClassList("spacer");

            layersToolbar.Add(toolbarSpacer);

            _addLayerButton = PAUtils.CreateIconButton("AddLayerButton", "Add Layer", AddLayer);
            layersToolbar.Add(_addLayerButton);

            _deleteLayerButton = PAUtils.CreateIconButton("DeleteLayerButton", "Delete Layer", DeleteLayer);
            layersToolbar.Add(_deleteLayerButton);

            var layersScrollView = new ScrollView();
            _layersPane.Add(layersScrollView);

            _layers = new PAReorderableList() { name = "Layers" };
            _layers.onItemMoved += (oldIndex, newIndex) =>
            {
                Undo.Record("Reorder Layers");

                for (int itemIndex = 0; itemIndex < _layers.itemCount; itemIndex++)
                    ((PALayerItem)_layers.ItemAt(itemIndex)).Layer.order = _layers.itemCount - itemIndex - 1;

                Canvas.RefreshImage();
            };
            _layers.onItemSelected += (i) => Canvas.SelectedLayer = ((PALayerItem)_layers.ItemAt(i)).Layer;
            layersScrollView.contentContainer.Add(_layers);

            toolbarSpacer = new VisualElement();
            toolbarSpacer.AddToClassList("spacer");

            _animations = new ToolbarMenu() { name = "AnimationDropDown" };            

            var bottomToolbar = new VisualElement();
            bottomToolbar.name = "BottomToolbar";
            bottomToolbar.Add(_animations);

            _animationOptionsButton = PAUtils.CreateIconButton("AnimationOptionsButton", "Animation Options", OpenAnimationOptions);
            bottomToolbar.Add(_animationOptionsButton);

            _playButton = PAUtils.CreateIconButton("AnimationPlayButton", "Play", OnPlay);
            bottomToolbar.Add(_playButton);

            bottomToolbar.Add(toolbarSpacer);

            _framesToolbar = new VisualElement { name = "FramesToolbar" };
            bottomToolbar.Add(_framesToolbar);
            _framesToolbar.Add(PAUtils.CreateIconButton("AddFrameButton", "Create a new frame", AddFrame));
            _framesToolbar.Add(PAUtils.CreateIconButton("DuplicateFrameButton", "Duplicate selected frame", DuplicatFrame));

            _deleteFrameButton = PAUtils.CreateIconButton("DeleteFrameButton", "Delete layer", DeleteFrame);
            _framesToolbar.Add(_deleteFrameButton);
            _bottomPane.Add(bottomToolbar);

            var framesScrollView = new ScrollView();
            framesScrollView.showHorizontal = true;
            framesScrollView.showVertical = false;
            _bottomPane.Add(framesScrollView);

            _frames = new PAReorderableList() { name = "Frames" };
            _frames.direction = ReorderableListDirection.Horizontal;
            _frames.onItemMoved += (oldIndex, newIndex) =>
            {
                Undo.Record("Reorder Frames");

                for (int itemIndex = 0; itemIndex < _frames.itemCount; itemIndex++)
                    ((PAFrameItem)_frames.ItemAt(itemIndex)).Frame.order = itemIndex;

                Canvas.RefreshImage();
            };
            _frames.onItemSelected += (i) => Canvas.SelectedFrame = ((PAFrameItem)_frames.ItemAt(i)).Frame;

            framesScrollView.contentContainer.Add(_frames);            

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
            RefreshAnimationList();

            Canvas.ZoomToFit();            
        }

        public void SaveFile()
        {
            if (Target == null || Canvas.File == null)
                return;

            Canvas.File.Save(AssetDatabase.GetAssetPath(Target));
            AssetDatabase.Refresh();
        }

        public void CloseFile()
        {
            // Make sure the toolbar gets removed
            Toolbar.parent.Remove(Toolbar);

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
            var selectionToolButton = PAUtils.CreateIconToggle("SelectionToolToggle", "Rectangular Marquee Tool (M)", (v) => 
                { if (v) Canvas.SelectedTool = Canvas.SelectionTool; });
            selectionToolButton.userData = typeof(PASelectionTool);
            Toolbox.Add(selectionToolButton);

            var pencilToolButton = PAUtils.CreateIconToggle("PencilToolToggle", "Pencil Tool (B)", (v) => {
                if (v) Canvas.SelectedTool = Canvas.PencilTool; });
            pencilToolButton.userData = typeof(PAPencilTool);
            Toolbox.Add(pencilToolButton);

            var eraserToolButton = PAUtils.CreateIconToggle("EraserToolToggle", "Eraser Tool (E)", (v) => {
                if (v) Canvas.SelectedTool = Canvas.EraserTool;});
            eraserToolButton.userData = typeof(PAEraserTool);
            Toolbox.Add(eraserToolButton);            

            var eyeDropperToolButton = PAUtils.CreateIconToggle("EyeDropperToolToggle", "Eyedropper Tool (I)", (v) => {
                if (v) Canvas.SelectedTool = Canvas.EyeDropperTool; });
            eyeDropperToolButton.userData = typeof(PAEyeDropperTool);
            Toolbox.Add(eyeDropperToolButton);

            // Foreground color selector
            _foregroundColor = new ColorField();
            _foregroundColor.showEyeDropper = false;
            _foregroundColor.value = Color.white;
            _foregroundColor.RegisterValueChangedCallback((evt) => {
                Undo.Record("Set Foreground Color");
                Canvas.ForegroundColor = evt.newValue;
            });
            Toolbox.Add(_foregroundColor);

            // Background color selector
            _backgroundColor = new ColorField();
            _backgroundColor.showEyeDropper = false;
            _backgroundColor.value = Color.white;
            _backgroundColor.RegisterValueChangedCallback((evt) => {
                Undo.Record("Set Background Color");
                Canvas.BackgroundColor = evt.newValue; 
            });
            Toolbox.Add(_backgroundColor);

            return Toolbox;
        }

        /// <summary>
        /// Refresh the list of layers
        /// </summary>
        public void RefreshLayersList()
        {
            _layers.RemoveAllItems();

            if (Canvas.File == null)
                return;

            foreach (var layer in Canvas.File.layers.OrderByDescending(l => l.order))
                _layers.AddItem(new PALayerItem(Canvas, layer));

            _layers.Select(0);

            _deleteLayerButton.SetEnabled(_layers.itemCount > 1);
        }

        /// <summary>
        /// Refresh the list of frames
        /// </summary>
        public void RefreshFrameList()
        {
            _frames.RemoveAllItems();

            if (null == Canvas.File)
                return;

            foreach (var frame in Canvas.File.frames.Where(f => f.animation == Canvas.SelectedAnimation).OrderBy(f => f.order))
                _frames.AddItem(new PAFrameItem(frame));

            _deleteFrameButton.SetEnabled(_frames.itemCount > 1);
        }

        private void AddLayer()
        {
            Undo.Record("Add Layer");
            var addedLayer = Canvas.File.AddLayer();
            RefreshLayersList();
            Canvas.SelectedLayer = addedLayer;
            Canvas.RefreshImage();
        }

        private void DeleteLayer()
        {
            // Dont allow the last layer to be removed
            if (Canvas.File.layers.Count < 2)
                return;

            Undo.Record("Delete Layer");

            var order = Canvas.SelectedLayer.order;
            Canvas.File.DeleteLayer(Canvas.SelectedLayer);
            RefreshLayersList();
            _layers.Select(Mathf.Clamp(Canvas.File.layers.Count - order - 1, 0, Canvas.File.layers.Count - 1));

            Canvas.RefreshImage();
            Canvas.RefreshFramePreviews();
        }

        /// <summary>
        /// Add a new empty frame
        /// </summary>
        private void AddFrame()
        {
            Undo.Record("Add Frame");
            Canvas.File.AddFrame(Canvas.SelectedAnimation);
            RefreshFrameList();
        }

        /// <summary>
        /// Duplicate the selected frame
        /// </summary>
        private void DuplicatFrame()
        {
            if (Canvas.File == null)
                return;

            Undo.Record("Duplicate Frame");

            var frame = Canvas.File.InsertFrame(Canvas.SelectedAnimation, Canvas.SelectedFrame.order + 1);
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
        private void DeleteFrame()
        {
            // Dont allow the last layer to be removed
            if (Canvas.File.frames.Count < 2)
                return;

            Undo.Record("Delete Frame");

            var order = Canvas.SelectedFrame.order;
            Canvas.File.DeleteFrame(Canvas.SelectedFrame);
            RefreshFrameList();
            _frames.Select(Mathf.Min(order, Canvas.File.frames.Count - 1));
            Canvas.RefreshImage();            
        }

        public void RefreshAnimationList()
        {
            if (null == _animations)
                return;

            _animations.menu.MenuItems().Clear();

            if (null == Canvas.File)
                return;

            foreach (var animation in Canvas.File.animations)
                _animations.menu.AppendAction(animation.name,
                    (a) => Canvas.SelectedAnimation = animation, 
                    (m) => animation == Canvas.SelectedAnimation ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            _animations.menu.AppendSeparator();
            _animations.menu.AppendAction("Create New Animation...", (a) => AddAnimation(), DropdownMenuAction.Status.Normal);
            _animations.text = Canvas.SelectedAnimation?.name;
        }

        /// <summary>
        /// Add a new animation
        /// </summary>
        private void AddAnimation()
        {
            Undo.Record("New Animation");
            Canvas.SelectedAnimation = Canvas.File.AddAnimation("New Animation");
            RefreshAnimationList();
        }

        /// <summary>
        /// Open the animation options for the selected animation
        /// </summary>
        private void OpenAnimationOptions()
        {
            UnityEditor.PopupWindow.Show(
                _animationOptionsButton.worldBound, 
                new PAAnimationOptions(this, Canvas.SelectedAnimation));
        }

        /// <summary>
        /// Handle the selected tool changing by ensuring the matching
        /// toolbox button is selected.
        /// </summary>
        private void OnToolChanged()
        {
            foreach (var child in Toolbox.Children())
            {
                var toggle = child as Toggle;
                if (toggle != null)
                    toggle.value = (Type)child.userData == Canvas.SelectedTool.GetType();
            }
        }

        private void CreateToolbar ()
        {
            Toolbar = new VisualElement { name = "WorkspaceToolbar" };

            //var toolbarSpacer = new VisualElement();
            //toolbarSpacer.style.flexGrow = 1.0f;
            //Toolbar.Add(toolbarSpacer);

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

            Toolbar.Add(
                PAUtils.CreateIconToggle("BottomPaneToggle", "Hide/Show animation frame",
                    (v) => _bottomPane.style.display = new StyleEnum<DisplayStyle>(v ? DisplayStyle.Flex : DisplayStyle.None)));

            Toolbar.Add(
                PAUtils.CreateIconToggle("LayerPaneToggle", "Hide/Show layers frame",
                    (v) => _layersPane.style.display = new StyleEnum<DisplayStyle>(v ? DisplayStyle.Flex : DisplayStyle.None)));

            Toolbar.Add(
                PAUtils.CreateIconToggle("GridToggle", "Hide/Show pixel grid",
                    (v) => Canvas.Grid.ShowPixels = v));

            Toolbar.Add(
                PAUtils.CreateIconToggle("CheckerBoardToggle", "Hide/Show pixel checkerboard",
                    (v) =>
                    {
                        Canvas.ShowCheckerboard = v;
                        Canvas.RefreshImage();
                    }));

            // Add the toolbar to the main toolbar
            Editor.Toolbar.Add(Toolbar);
        }

        private void PlayNextFrame()
        {
            if (!IsPlaying)
                return;
            
            Canvas.SelectedFrame = Canvas.File.FindNextFrame(Canvas.SelectedFrame);
            _playingScheduledItem = this.schedule.Execute(PlayNextFrame);
            _playingScheduledItem.ExecuteLater(1000 / Math.Max(1,Canvas.SelectedAnimation.fps));
        }

        private void OnPlay()
        {
            IsPlaying = !IsPlaying;

            if (IsPlaying)
                _playButton.AddToClassList("selected");
            else
                _playButton.RemoveFromClassList("selected");

            if(IsPlaying)
            {
                _playFrame = Canvas.SelectedFrame;
                PlayNextFrame();

                _layers.SetEnabled(false);
                _frames.SetEnabled(false);
                _layersPane.SetEnabled(false);
                _framesToolbar.SetEnabled(false);
                Toolbox.SetEnabled(false);
                Canvas.RefreshCursor();
            }
            else if (null != _playingScheduledItem)
            {
                _playingScheduledItem.Pause();
                Canvas.SelectedFrame = _playFrame;

                _layers.SetEnabled(true);
                _frames.SetEnabled(true);
                _layersPane.SetEnabled(true);
                _framesToolbar.SetEnabled(true);
                Toolbox.SetEnabled(true);
                Canvas.RefreshCursor();
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch(evt.keyCode)
            {
                case KeyCode.Space:
                    OnPlay();
                    evt.StopImmediatePropagation();
                    break;
            }
        }
    }
}
