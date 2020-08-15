using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NoZ.PixelEditor
{
    internal class PEFile
    {
        public int width;

        public int height;

        public List<PEAnimation> animations;

        public List<PELayer> layers;

        public List<PETexture> textures;

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
        public PETexture FindTexture(PEFrame frame, PELayer layer) =>
            textures.Where(t => t.frame == frame && t.layer == layer).FirstOrDefault();

        /// <summary>
        /// Render the given frame into the given texture
        /// </summary>
        public void RenderFrame (PEFrame frame, Texture2D renderTarget)
        {
            renderTarget.Clear(Color.clear);

            foreach (var texture in textures.Where(t => t.frame == frame).OrderBy(t => t.layer.order))
                renderTarget.Blend(texture.texture, texture.layer.opacity);
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
        /// Load a pixel edtior file from the given filename
        /// </summary>
        public static PEFile Load (string filename)
        {
            var file = new PEFile();

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
                var textureCount = reader.ReadInt32();
                file.textures = new List<PETexture>(textureCount);
                for(var textureIndex=0; textureIndex<textureCount; textureIndex++)
                {
                    var petexture = new PETexture();
                    petexture.frame = file.FindFrame(reader.ReadString());
                    petexture.layer = file.FindLayer(reader.ReadString());

                    petexture.texture = new Texture2D(file.width, file.height, TextureFormat.RGBA32, false);
                    petexture.texture.LoadRawTextureData(reader.ReadBytes(textureSize));
                    petexture.texture.filterMode = FilterMode.Point;
                    petexture.texture.Apply();

                    file.textures.Add(petexture);
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

                // Write textures
                writer.Write(textures.Count);
                foreach(var texture in textures)
                {
                    writer.Write(texture.frame.id);
                    writer.Write(texture.layer.id);
                    writer.Write(texture.texture.GetRawTextureData());
                }
            }
        }
    }
}
