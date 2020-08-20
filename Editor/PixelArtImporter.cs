using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace NoZ.PA
{
    [UnityEditor.AssetImporters.ScriptedImporterAttribute(1, "pixelart")]
    public class PixelArtImporter : UnityEditor.AssetImporters.ScriptedImporter
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Create/PixelArt")]
        private static void CreatePixelArt()
        {
            // Generate a unique filename for the new artwork
            var filename = Path.Combine(
                Application.dataPath,
                AssetDatabase.GenerateUniqueAssetPath($"{PAUtils.GetSelectedPathOrFallback()}/New PixelArt.pixelart").Substring(7));

            // Create an empty file
            var file = new PAFile();
            file.width = 32;
            file.height = 32;
            file.AddFrame(file.AddAnimation("New Animation"));
            file.AddLayer();
            file.Save(filename);

            AssetDatabase.Refresh();
        }
#endif

        public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
        {
            // Load the raw file.
            var file = PAFile.Load(ctx.assetPath);

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.name = "atlas";

            var textures = file.frames.Select(f => file.RenderFrame(f)).ToArray();
            var rects = texture.PackTextures(textures, 1).Select(r => new Rect(r.xMin * texture.width, r.yMin * texture.height, r.width * texture.width, r.height * texture.height)).ToArray();
            texture.Apply();

            for (int frameIndex = 0; frameIndex < file.frames.Count; frameIndex++)
            {
                var frame = file.frames[frameIndex];
                var sprite = Sprite.Create(texture, rects[frameIndex], new Vector2(0.5f, 0.5f));
                sprite.name = $"{frame.animation.name}.{frame.order:D03}";
                ctx.AddObjectToAsset(frame.id, sprite);
            }

            ctx.AddObjectToAsset("_atlas", texture);

            var pixelArt = ScriptableObject.CreateInstance<PixelArt>();
            ctx.AddObjectToAsset("main", pixelArt, file.RenderFrame(file.frames[0]));
            ctx.SetMainObject(pixelArt);
        }
    }
}

