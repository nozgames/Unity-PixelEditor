
using System.IO;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using NoZ.PixelEditor;

namespace Assets.PE.Editor
{
    [ScriptedImporter(1, "pixelart")]
    public class PixelArtImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using (var reader = new BinaryReader(File.OpenRead(ctx.assetPath)))
            {
                var width = reader.ReadInt32();
                var height = reader.ReadInt32();
                var pixels = reader.ReadBytes(width * height * 4);

                var texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture2D.LoadRawTextureData(pixels);
                texture2D.filterMode = FilterMode.Point;
                texture2D.name = "Texture";

                var pixelArt = ScriptableObject.CreateInstance<PixelArt>();
                pixelArt.texture = texture2D;
                pixelArt.width = width;
                pixelArt.height = height;

                ctx.AddObjectToAsset("pixelArt", pixelArt, texture2D);
                ctx.AddObjectToAsset("Texture", texture2D, texture2D);

                var sprite = Sprite.Create(texture2D, new Rect(0, 0, width, height), new Vector2(width/2, height/2));
                sprite.name = "Sprite1";
                ctx.AddObjectToAsset("Sprite1", sprite);

                ctx.SetMainObject(pixelArt);
            }
        }
    }
}

