using System.IO;
using UnityEditor;
using UnityEngine;

namespace NoZ.PixelEditor
{
    public class PixelArt : ScriptableObject
    {
        public int width;
        public int height;
        public Texture2D texture = null;

        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets";

            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/PixelArt")]
        private static void CreatePixelArt()
        {
            var filename = Path.Combine(Application.dataPath, AssetDatabase.GenerateUniqueAssetPath($"{GetSelectedPathOrFallback()}/New PixelArt.pixelart").Substring(7));
            using (var writer = new BinaryWriter(File.Create(filename)))
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int x = 0; x < texture.width; x++)
                    for (int y = 0; y < texture.height; y++)
                        texture.SetPixel(x, y, Color.clear);

                writer.Write(texture.width);
                writer.Write(texture.height);

                // Write layers
                var layerId = System.Guid.NewGuid().ToString();
                writer.Write(1);
                writer.Write(layerId);
                writer.Write("Layer 1");
                writer.Write(1.0f);
                writer.Write(0);
                writer.Write(true);

                // Write animations
                var animationId = System.Guid.NewGuid().ToString();
                writer.Write(1);
                writer.Write(animationId);
                writer.Write("Default");

                // Write frames
                var frameId = System.Guid.NewGuid().ToString();
                writer.Write(1);
                writer.Write(frameId);
                writer.Write(animationId);
                writer.Write(0);

                // Write textures
                writer.Write(1);
                writer.Write(frameId);
                writer.Write(layerId);
                writer.Write(texture.GetRawTextureData());
            }
            
            AssetDatabase.Refresh();
        }
#endif
    }
}
