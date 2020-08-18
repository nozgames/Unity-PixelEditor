using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NoZ.PixelEditor
{
    internal class PEFile
    {
        public string name;

        public int width;

        public int height;

        public List<PEAnimation> animations;

        public List<PELayer> layers;

        public List<PEImage> images;

        public List<PEFrame> frames;

        /// <summary>
        /// Find the animation that matches the given identifier
        /// </summary>
        public PEAnimation FindAnimation(string id) =>
            animations.Where(a => a.id == id).FirstOrDefault();

        /// <summary>
        /// Find the frame that matches the given identifier
        /// </summary>
        public PEFrame FindFrame(string id) =>
            frames.Where(f => f.id == id).FirstOrDefault();

        /// <summary>
        /// Find the layer that matches the given identifier
        /// </summary>
        public PELayer FindLayer (string id) =>
            layers.Where(l => l.id == id).FirstOrDefault();

        /// <summary>
        /// Find the texture for the given frame and layer
        /// </summary>
        /// <returns></returns>
        public PEImage FindImage(PEFrame frame, PELayer layer) =>
            images.Where(t => t.frame == frame && t.layer == layer).FirstOrDefault();

        /// <summary>
        /// Add a new texture or return the existing one
        /// </summary>
        public PEImage AddImage(PEFrame frame, PELayer layer)
        {
            var image = FindImage(frame, layer);
            if (null != image)
                return image;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.Clear(Color.clear);
            texture.Apply();

            image = new PEImage
            {
                frame = frame,
                layer = layer,
                texture = texture
            };

            images.Add(image);

            return image;
        }

        /// <summary>
        /// Add a new layer to the file
        /// </summary>
        public PELayer AddLayer ()
        {
            // Determine the unique name index for the layer based on the 
            // maximum layer named "Layer #"
            var nameIndex = layers
                .Where(l => l.name.StartsWith("Layer "))
                .Select(l => int.Parse(l.name.Substring(6))).Max() + 1;

            var layer = new PELayer
            {
                id = System.Guid.NewGuid().ToString(),
                name = $"Layer {nameIndex}",
                opacity = 1.0f,
                order = layers.Count
            };

            layers.Add(layer);
            return layer;
        }


        /// <summary>
        /// Render the given frame into the given texture
        /// </summary>
        public void RenderFrame (PEFrame frame, Texture2D renderTarget)
        {
            renderTarget.Clear(Color.clear);

            foreach (var image in images.Where(t => t.frame == frame).OrderBy(t => t.layer.order))
                renderTarget.Blend(image.texture, image.layer.opacity);
        }

        /// <summary>
        /// Render the given frame into a new texture
        /// </summary>
        public Texture2D RenderFrame(PEFrame frame)
        {
            var renderTarget = new Texture2D(width, height, TextureFormat.RGBA32, false);
            renderTarget.filterMode = FilterMode.Point;
            RenderFrame(frame, renderTarget);
            renderTarget.Apply();
            return renderTarget;
        }

        /// <summary>
        /// Remove a layer from the file
        /// </summary>
        public void RemoveLayer (PELayer remove)
        {
            // Remove all textures that reference the layer
            images = images.Where(t => t.layer != remove).ToList();

            // Adjust the order for all layers after the layer being removed
            foreach (var layer in layers)
                if (layer.order > remove.order)
                    layer.order--;

            // Remove the layer from the list
            layers.Remove(remove);
        }
        
        /// <summary>
        /// Load a pixel edtior file from the given filename
        /// </summary>
        public static PEFile Load (string filename)
        {
            var file = new PEFile { name = Path.GetFileNameWithoutExtension(filename) };

            using (var reader = new BinaryReader(File.OpenRead(filename)))
            {
                file.width = reader.ReadInt32();
                file.height = reader.ReadInt32();

                var textureSize = 4 * file.width * file.height;

                // Read the layers
                var layerCount = reader.ReadInt32();
                file.layers = new List<PELayer>(layerCount);
                for(var layerIndex=0; layerIndex<layerCount; layerIndex++)
                {
                    var layer = new PELayer();
                    layer.id = reader.ReadString();
                    layer.name = reader.ReadString();
                    layer.opacity = reader.ReadSingle();
                    layer.order = reader.ReadInt32();
                    layer.visible = reader.ReadBoolean();
                    file.layers.Add(layer);
                }

                // Read the animations
                var animationCount = reader.ReadInt32();
                file.animations = new List<PEAnimation>(animationCount);
                for(var animationIndex=0; animationIndex < animationCount; animationIndex++)
                {
                    var animation = new PEAnimation();
                    animation.id = reader.ReadString();
                    animation.name = reader.ReadString();
                    file.animations.Add(animation);
                }

                // Read the frames
                var frameCount = reader.ReadInt32();
                file.frames = new List<PEFrame>(frameCount);
                for(var frameIndex=0; frameIndex < frameCount; frameIndex++)
                {
                    var frame = new PEFrame();
                    frame.id = reader.ReadString();
                    frame.animation = file.FindAnimation(reader.ReadString());
                    frame.order = reader.ReadInt32();
                    file.frames.Add(frame);
                }

                // Read the textures
                var imageCount = reader.ReadInt32();
                file.images = new List<PEImage>(imageCount);
                for(var imageIndex=0; imageIndex<imageCount; imageIndex++)
                {
                    var image = new PEImage();
                    image.frame = file.FindFrame(reader.ReadString());
                    image.layer = file.FindLayer(reader.ReadString());

                    image.texture = new Texture2D(file.width, file.height, TextureFormat.RGBA32, false);
                    image.texture.LoadRawTextureData(reader.ReadBytes(textureSize));
                    image.texture.filterMode = FilterMode.Point;
                    image.texture.Apply();

                    file.images.Add(image);
                }
            }

            return file;
        }

        internal void Save(string filename)
        {
            using(var writer = new BinaryWriter(File.Create(filename)))
            {
                writer.Write(width);
                writer.Write(height);

                // Write layers                
                writer.Write(layers.Count);
                foreach(var layer in layers)
                {
                    writer.Write(layer.id);
                    writer.Write(layer.name);
                    writer.Write(layer.opacity);
                    writer.Write(layer.order);
                    writer.Write(layer.visible);
                }

                // Write animations
                writer.Write(animations.Count);
                foreach(var animation in animations)
                {
                    writer.Write(animation.id);
                    writer.Write(animation.name);
                }

                // Write frames
                writer.Write(frames.Count);
                foreach(var frame in frames)
                {
                    writer.Write(frame.id);
                    writer.Write(frame.animation.id);
                    writer.Write(frame.order);
                }

                // Write images
                writer.Write(images.Count);
                foreach(var image in images)
                {
                    writer.Write(image.frame.id);
                    writer.Write(image.layer.id);
                    writer.Write(image.texture.GetRawTextureData());
                }
            }
        }
    }
}
