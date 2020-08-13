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

        [MenuItem("Assets/Create/PixelArt")]
        private static void CreatePixelArt()
        {
            var filename = Path.Combine(Application.dataPath, AssetDatabase.GenerateUniqueAssetPath($"{GetSelectedPathOrFallback()}/New PixelArt.pixelart").Substring(7));
            using (var writer = new BinaryWriter(File.Create(filename)))
            {
                var width = 32;
                var height = 32;

                writer.Write(width);
                writer.Write(height);

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        texture.SetPixel(x, y, Color.clear);

                writer.Write(texture.GetRawTextureData());
            }
            
            AssetDatabase.Refresh();
        }

        public void Serialize()
        {

        }
    }
}
