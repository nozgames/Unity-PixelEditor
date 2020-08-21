using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NoZ.PA
{
    /// <summary>
    /// Implements a class responsible for serializing all undo/redo data.  Since unity uses
    /// serialized objects for undo/redo and the PixelArt workspace is not serializable this
    /// class will convert the workspace into serialized data when an undo operation is recorded
    /// and deserialize into the workspace when an undo is performed.
    /// </summary>
    [Serializable]
    internal class PAUndo : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        private class SerializedLayer
        {
            public string id;
            public string name;
            public int order;
            public float opacity;
            public bool visible;
        }

        [Serializable]
        private class SerializedFrame
        {
            public string id;
            public string animation;
            public int order;
        }

        [Serializable]
        private class SerializedImage
        {
            public string frame;
            public string layer;
            public Texture2D texture;
        }

        [Serializable]
        private class SerializedAnimation
        {
            public string id;
            public string name;
        }

        [Serializable]
        private class SerializedWorkspace
        {
            public Color foregroundColor;
            public Color backgroundColor;
            public string selectedAnimation;
            public string selectedLayer;
            public string selectedFrame;
            public SerializedAnimation[] animations;
            public SerializedFrame[] frames;
            public SerializedLayer[] layers;
            public SerializedImage[] images;
        }

        public event Action UndoEvent;

        public PAWorkspace Workspace { get; private set; }

        [SerializeField] private SerializedWorkspace serialized;

        public static PAUndo CreateInstance (PAWorkspace workspace)
        {
            var undo = ScriptableObject.CreateInstance<PAUndo>();
            undo.Workspace = workspace;
            return undo;
        }

        /// <summary>
        /// the undo uobject is only deserialized after an undo operation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (null == serialized || Workspace == null)
                return;

            Workspace.Canvas.ForegroundColor = serialized.foregroundColor;
            Workspace.Canvas.BackgroundColor = serialized.backgroundColor;

            Workspace.Canvas.File.animations = serialized.animations.Select(a => new PAAnimation
            {
                name = a.name,
                id = a.id
            }).ToList();

            Workspace.Canvas.File.layers = serialized.layers.Select(l => new PALayer(Workspace.Canvas.File)
            {
                id = l.id,
                name = l.name,
                opacity = l.opacity,
                order = l.order,
                visible = l.visible,
            }).ToList();

            Workspace.Canvas.File.frames = serialized.frames.Select(f => new PAFrame(Workspace.Canvas.File)
            {
                id = f.id,
                animation = Workspace.Canvas.File.FindAnimation(f.animation),
                order = f.order
            }).ToList();

            Workspace.Canvas.File.images = serialized.images.Select(i => new PAImage
            {
                frame = Workspace.Canvas.File.FindFrame(i.frame),
                layer = Workspace.Canvas.File.FindLayer(i.layer),
                texture = i.texture
            }).ToList();

            Workspace.RefreshLayersList();
            Workspace.RefreshFrameList();
            Workspace.RefreshAnimationList();

            Workspace.Canvas.SelectedAnimation = Workspace.Canvas.File.FindAnimation(serialized.selectedAnimation);
            Workspace.Canvas.SelectedLayer = Workspace.Canvas.File.FindLayer(serialized.selectedLayer);
            Workspace.Canvas.SelectedFrame = Workspace.Canvas.File.FindFrame(serialized.selectedFrame);

            Workspace.Canvas.RefreshImage();

            serialized = null;

            UndoEvent?.Invoke();
        }

        /// <summary>
        /// The undo object is only serialized during an undo operation
        /// </summary>
        public void OnBeforeSerialize()
        {

            if (Workspace == null || Workspace.Canvas == null || Workspace.Canvas.File == null)
                return;

            serialized = new SerializedWorkspace
            {
                foregroundColor = Workspace.Canvas.ForegroundColor,
                backgroundColor = Workspace.Canvas.BackgroundColor,
                selectedFrame = Workspace.Canvas.SelectedFrame.id,
                selectedLayer = Workspace.Canvas.SelectedLayer.id,
                selectedAnimation = Workspace.Canvas.SelectedAnimation.id,
                animations = Workspace.Canvas.File.animations.Select(a => new SerializedAnimation { id = a.id, name = a.name }).ToArray(),
                frames = Workspace.Canvas.File.frames.Select(f => new SerializedFrame { id = f.id, order = f.order, animation = f.animation.id }).ToArray(),
                layers = Workspace.Canvas.File.layers.Select(l => new SerializedLayer { id = l.id, name = l.name, opacity = l.opacity, order = l.order, visible = l.visible }).ToArray(),
                images = Workspace.Canvas.File.images.Select(i => new SerializedImage { frame = i.frame.id, layer = i.layer.id, texture = i.texture }).ToArray()
            };            
        }

        public void Record (string name) =>
            Undo.RecordObject(this, name);
    }
}
