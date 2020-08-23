using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace NoZ.PA
{
    [ScriptedImporter(1, "pixelart")]
    public class PixelArtImporter : ScriptedImporter
    {
        public enum LayerMode
        {
            Combine,
            Seperate
        }

        [MenuItem("Assets/Create/PixelArt/16x16")]
        private static void CreatePixelArt16x16() => CreatePixelArt(16, 16);

        [MenuItem("Assets/Create/PixelArt/32x32")]
        private static void CreatePixelArt32x32() => CreatePixelArt(32, 32);

        [MenuItem("Assets/Create/PixelArt/64x64")]
        private static void CreatePixelArt64x64() => CreatePixelArt(64, 64);

        private static void CreatePixelArt(int width, int height)
        {
            // Generate a unique filename for the new artwork
            var filename = Path.Combine(
                Application.dataPath,
                AssetDatabase.GenerateUniqueAssetPath($"{PAUtils.GetSelectedPathOrFallback()}/New PixelArt.pixelart").Substring(7));

            // Create an empty file
            var file = new PAFile();
            file.width = width;
            file.height = height;
            file.AddFrame(file.AddAnimation("New Animation"));
            file.AddLayer();
            file.Save(filename);

            AssetDatabase.Refresh();
        }

        public LayerMode layerMode = LayerMode.Combine;
        public int defaultAnimationIndex = 0;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Load the raw file.
            var file = PAFile.Load(ctx.assetPath);

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.name = "atlas";

            var textures = file.frames.Select(f => file.RenderFrame(f)).ToArray();
            var rects = texture.PackTextures(textures, 1).Select(r => new Rect(r.xMin * texture.width, r.yMin * texture.height, r.width * texture.width, r.height * texture.height)).ToArray();
            texture.Apply();

            // TODO: look for duplicates by checking imageContentsHash for all textures


            var sprites = new Dictionary<string, Sprite>();
            for (int frameIndex = 0; frameIndex < file.frames.Count; frameIndex++)
            {
                var frame = file.frames[frameIndex];
                var sprite = Sprite.Create(texture, rects[frameIndex], new Vector2(0.5f, 0.5f));
                sprite.name = $"{frame.animation.name}.{frame.order:D03}";
                ctx.AddObjectToAsset(frame.id, sprite);
                sprites[frame.id] = sprite;
            }

            ctx.AddObjectToAsset("_atlas", texture);

            var pixelArt = PixelArt.Create(
                file.animations.Select(a => new PixelArtAnimation(
                    a.name,
                    file.frames.Where(f => f.animation == a).OrderBy(f => f.order).Select(f => new PixelArtAnimationFrame { sprite = sprites[f.id] }).ToArray()
                )).ToArray(),
                defaultAnimationIndex);
           
            var defaultFrame = file.frames.Where(f => f.animation == file.animations[defaultAnimationIndex]).OrderBy(f => f.order).First();
#if false
            var go = new GameObject();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[defaultFrame.id];;
            go.name = "Prefab";
            var animator = go.AddComponent<PixelArtAnimator>();
            animator.PixelArt = pixelArt;
            animator.layers = new SpriteRenderer[] { sr };

            ctx.AddObjectToAsset("prefab", go, PAUtils.LoadImage("PixelArtIcon.psd"));
#endif

            pixelArt.name = "PixelArt";
            ctx.AddObjectToAsset("pixelArt", pixelArt, PAUtils.LoadImage("PixelArtIcon.psd"));

            //ctx.AddObjectToAsset("main", pixelArt, file.RenderFrame(file.frames[0]));
            ctx.SetMainObject(pixelArt);
        }
    }
}

