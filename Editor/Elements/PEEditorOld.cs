using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PEEditorOld : VisualElement
    {
#if false
        public PEEditor (PEWindow window)
        {

        }

        public void OnGUI()
        {
            Workspace.OnGUI();
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            SaveFile();

            EditorPrefs.SetString("PixelEditor.ForegroundColor", $"#{ColorUtility.ToHtmlStringRGBA(Workspace.ForegroundColor)}");
            EditorPrefs.SetString("PixelEditor.BackgroundColor", $"#{ColorUtility.ToHtmlStringRGBA(Workspace.BackgroundColor)}");
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
            zoomImage.image = PEUtils.LoadImage("ZoomIcon.psd");
            Toolbar.Add(zoomImage);

            _zoomSlider = new Slider();
            _zoomSlider.lowValue = PEWorkspace.ZoomMin;
            _zoomSlider.highValue = PEWorkspace.ZoomMax;
            _zoomSlider.AddToClassList("zoom");
            _zoomSlider.RegisterValueChangedCallback((e) => Workspace.SetZoom(e.newValue, Workspace.ViewportToWorkspace(Workspace.ViewportSize * 0.5f)));
            Toolbar.Add(_zoomSlider);

            var framesToggle = new PEImageToggle();
            framesToggle.checkedImage = PEUtils.LoadImage("FramesToggle.psd");
            framesToggle.value = true;
            framesToggle.onValueChanged = (v) => _frames.parent.visible = v;
            framesToggle.tooltip = "Toggle Frames";
            Toolbar.Add(framesToggle);

            var layerToggle = new PEImageToggle();
            layerToggle.checkedImage = PEUtils.LoadImage("LayerToggle.psd");
            layerToggle.value = true;
            layerToggle.onValueChanged = (v) => _layers.parent.parent.visible = v;
            layerToggle.tooltip = "Toggle layers";
            Toolbar.Add(layerToggle);

            var gridToggle = new PEImageToggle();
            gridToggle.checkedImage = PEUtils.LoadImage("GridToggle.psd");
            gridToggle.value = true;
            gridToggle.onValueChanged = (v) => Workspace.Grid.ShowPixels = v;
            gridToggle.tooltip = "Toggle pixel grid";
            Toolbar.Add(gridToggle);

            var checkerboardToggle = new PEImageToggle();
            checkerboardToggle.checkedImage = PEUtils.LoadImage("Grid.psd");
            checkerboardToggle.value = true;
            checkerboardToggle.onValueChanged = (v) =>
            {
                Workspace.Canvas.ShowCheckerboard = v;
                Workspace.RefreshCanvas();
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
            var selectionToolButton = PEUtils.CreateImageButton("SelectionTool.psd", "Rectangular Marquee Tool (M)", () => Workspace.SelectedTool = Workspace.SelectionTool);
            selectionToolButton.userData = typeof(PESelectionTool);
            Toolbox.Add(selectionToolButton);

            var pencilToolButton = PEUtils.CreateImageButton("PencilTool.psd", "Pencil Tool (B)", () => Workspace.SelectedTool = Workspace.PencilTool);
            pencilToolButton.userData = typeof(PEPencilTool);
            Toolbox.Add(pencilToolButton);

            var eraserToolButton = PEUtils.CreateImageButton("EraserTool.psd", "Eraser Tool (E)", () => Workspace.SelectedTool = Workspace.EraserTool);
            eraserToolButton.userData = typeof(PEEraserTool);
            Toolbox.Add(eraserToolButton);

            var eyeDropperToolButton = PEUtils.CreateImageButton("EyeDropperTool.psd", "Eyedropper Tool (I)", () => Workspace.SelectedTool = Workspace.EyeDropperTool);
            eyeDropperToolButton.userData = typeof(PEEyeDropperTool);
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
        private void RefreshLayersList ()
        {
            _layers.RemoveAllItems();

            if (Workspace.File == null)
                return;

            foreach (var layer in Workspace.File.layers.OrderByDescending(l => l.order))
                _layers.AddItem(new PELayerItem(Workspace, layer));

            _layers.Select(0);
        }

        /// <summary>
        /// Refresh the list of frames
        /// </summary>
        private void RefreshFrameList ()
        {
            _frames.RemoveAllItems();

            if (null == Workspace.File)
                return;

            foreach (var frame in Workspace.File.frames.OrderBy(f => f.order))
                _frames.AddItem(new PEFrameItem(frame));
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

            Workspace.RefreshCanvas();
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
                Workspace.File.images.Where(i => i.frame == Workspace.SelectedFrame).Select(i => new PEImage
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
            Workspace.RefreshCanvas();
        }


        private void OnKeyDown(KeyDownEvent evt)
        {
            // Send the key to the current tool
            if (!Workspace.SelectedTool?.OnKeyDown(PEKeyEvent.Create(evt)) ?? true)
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
                        Workspace.SelectionTool.Selection = new RectInt(0, 0, Workspace.CanvasWidth, Workspace.CanvasHeight);
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



        /// <summary>
        /// Called when the zoom changes by dragging the slider or directly setting the zoom falue
        /// </summary>
        //private void OnZoomValueChanged(ChangeEvent<float> evt) =>
        //    OnZoomValueChanged(
        //        evt.previousValue, 
        //        evt.newValue, 
        //        ScrollOffset + _scrollcontentViewport.contentRect.size * 0.5f);
#endif
    }
}
