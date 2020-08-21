using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NoZ.PA
{
    internal class PAFile
    {
        public static int CurrentVersion = 1;

        public int version = CurrentVersion;

        public string name;

        public int width;

        public int height;

        public List<PAAnimation> animations;

        public List<PALayer> layers;

        public List<PAImage> images;

        public List<PAFrame> frames;

        public PAFile()
        {
            layers = new List<PALayer>();
            images = new List<PAImage>();
            frames = new List<PAFrame>();
            animations = new List<PAAnimation>();
        }

        /// <summary>
        /// Find the animation that matches the given identifier
        /// </summary>
        public PAAnimation FindAnimation(string id) =>
            animations.Where(a => a.id == id).FirstOrDefault();

        /// <summary>
        /// Find the animation that matches the given identifier
        /// </summary>
        public PAAnimation FindAnimationByName(string name) =>
            animations.Where(a => 0 == string.Compare(a.name,name,true)).FirstOrDefault();

        /// <summary>
        /// Find the frame that matches the given identifier
        /// </summary>
        public PAFrame FindFrame(string id) =>
            frames.Where(f => f.id == id).FirstOrDefault();

        /// <summary>
        /// Find the layer that matches the given identifier
        /// </summary>
        public PALayer FindLayer (string id) =>
            layers.Where(l => l.id == id).FirstOrDefault();

        /// <summary>
        /// Find the texture for the given frame and layer
        /// </summary>
        /// <returns></returns>
        public PAImage FindImage(PAFrame frame, PALayer layer) =>
            images.Where(t => t.frame == frame && t.layer == layer).FirstOrDefault();

        /// <summary>
        /// Add a new texture or return the existing one
        /// </summary>
        public PAImage AddImage(PAFrame frame, PALayer layer)
        {
            var image = FindImage(frame, layer);
            if (null != image)
                return image;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.Clear(Color.clear);
            texture.Apply();

            image = new PAImage
            {
                frame = frame,
                layer = layer,
                texture = texture
            };

            images.Add(image);

            return image;
        }

        /// <summary>
        /// Add a new animation with the given name to the file
        /// </summary>
        public PAAnimation AddAnimation(string name)
        {
            // ensure name is unique
            var prefix = name;
            for(var index=1; null != FindAnimationByName(name); index++)
                name = $"{prefix} 1";                    

            var animation = new PAAnimation
            {
                id = System.Guid.NewGuid().ToString(),
                name = name
            };
            animations.Add(animation);
            return animation;
        }

        /// <summary>
        /// Add a new frame to the given animation
        /// </summary>
        public PAFrame AddFrame(PAAnimation animation)
        {
            var frame = new PAFrame(this)
            {
                id = System.Guid.NewGuid().ToString(),
                animation = animation,
                order = frames.Count
            };

            frames.Add(frame);
            return frame;
        }

        /// <summary>
        /// Insert a new frame at the given index within the animation
        /// </summary>
        public PAFrame InsertFrame(PAAnimation animation, int index)
        {
            var frame = AddFrame(animation);
            if (index == -1 || index >= frame.order)
                return frame;

            foreach (var f in frames)
                if (f.order >= index)
                    f.order++;

            frame.order = index;
            return frame;
        }

        /// <summary>
        /// Remove a frame from the file
        /// </summary>
        public void DeleteFrame(PAFrame remove)
        {
            // Remove all textures that reference the frame
            images = images.Where(t => t.frame != remove).ToList();

            // Adjust the order for all frames after the frame being removed
            foreach (var frame in frames)
                if (frame.order > remove.order)
                    frame.order--;

            // Remove the frame from the list
            frames.Remove(remove);
        }

        /// <summary>
        /// Add a new layer to the file
        /// </summary>
        public PALayer AddLayer ()
        {
            // Determine the unique name index for the layer based on the 
            // maximum layer named "Layer #"
            var nameIndex = layers
                .Where(l => l.name.StartsWith("Layer "))
                .Select(l => int.Parse(l.name.Substring(6)))
                .DefaultIfEmpty()
                .Max() + 1;

            var layer = new PALayer(this)
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
        public void RenderFrame (PAFrame frame, Texture2D renderTarget)
        {
            renderTarget.Clear(Color.clear);

            foreach (var image in images.Where(t => t.frame == frame).OrderBy(t => t.layer.order))
                if(image.layer.visible && image.texture != null)
                    renderTarget.Blend(image.texture, image.layer.opacity);

            renderTarget.Apply();
        }

        /// <summary>
        /// Render the given frame into a new texture
        /// </summary>
        public Texture2D RenderFrame(PAFrame frame)
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
        public void DeleteLayer (PALayer remove)
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
        public static PAFile Load (string filename)
        {
            var file = new PAFile { name = Path.GetFileNameWithoutExtension(filename) };

            using (var reader = new BinaryReader(File.OpenRead(filename)))
            {
                var version = reader.ReadInt32();
                file.width = reader.ReadInt32();
                file.height = reader.ReadInt32();

                var textureSize = 4 * file.width * file.height;

                // Read the layers
                var layerCount = reader.ReadInt32();
                for (var layerIndex=0; layerIndex<layerCount; layerIndex++)
                {
                    var layer = new PALayer(file);
                    layer.id = reader.ReadString();
                    layer.name = reader.ReadString();
                    layer.opacity = reader.ReadSingle();
                    layer.order = reader.ReadInt32();
                    layer.visible = reader.ReadBoolean();
                    file.layers.Add(layer);
                }

                // Read the animations
                var animationCount = reader.ReadInt32();
                for(var animationIndex=0; animationIndex < animationCount; animationIndex++)
                {
                    var animation = new PAAnimation();
                    animation.id = reader.ReadString();
                    animation.name = reader.ReadString();
                    file.animations.Add(animation);
                }

                // Read the frames
                var frameCount = reader.ReadInt32();
                for(var frameIndex=0; frameIndex < frameCount; frameIndex++)
                {
                    var frame = new PAFrame(file);
                    frame.id = reader.ReadString();
                    frame.animation = file.FindAnimation(reader.ReadString());
                    frame.order = reader.ReadInt32();
                    file.frames.Add(frame);
                }

                // Read the textures
                var imageCount = reader.ReadInt32();
                for (var imageIndex=0; imageIndex<imageCount; imageIndex++)
                {
                    var image = new PAImage();
                    image.frame = file.FindFrame(reader.ReadString());
                    image.layer = file.FindLayer(reader.ReadString());

                    if (reader.ReadBoolean())
                    {
                        image.texture = new Texture2D(file.width, file.height, TextureFormat.RGBA32, false);
                        image.texture.LoadRawTextureData(reader.ReadBytes(textureSize));
                        image.texture.filterMode = FilterMode.Point;
                        image.texture.Apply();
                    }

                    file.images.Add(image);
                }
            }

            return file;
        }

        internal void Save(string filename)
        {
            using(var writer = new BinaryWriter(File.Create(filename)))
            {
                writer.Write(version);
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

                    writer.Write(image.texture != null);
                    
                    if(image.texture != null)
                        writer.Write(image.texture.GetRawTextureData());
                }
            }
        }
    }
}
