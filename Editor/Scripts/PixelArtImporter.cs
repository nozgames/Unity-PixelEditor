using UnityEngine;

namespace NoZ.PixelEditor
{
    [UnityEditor.AssetImporters.ScriptedImporter(1, "pixelart")]
    public class PixelArtImporter : UnityEditor.AssetImporters.ScriptedImporter
    {
        public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
        {
            // Load the raw file.
            var file = PEFile.Load(ctx.assetPath);

            foreach(var frame in file.frames)
            {
                var texture = file.RenderFrame(frame);
                texture.name = $"{frame.animation.name}.{frame.order}";
                ctx.AddObjectToAsset(frame.id, texture);

                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                sprite.name = texture.name;
                ctx.AddObjectToAsset($"{frame.id}_sprite", sprite);
            }

            var pixelArt = ScriptableObject.CreateInstance<PixelArt>();
            ctx.AddObjectToAsset("main", pixelArt);
            ctx.SetMainObject(pixelArt);
        }
    }
}

