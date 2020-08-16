using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;

namespace NoZ.PixelEditor
{
    internal class PECanvas : VisualElement
    {
        private Texture2D _background = null;
        private PEWindow _window = null;        

        public PECanvas(PEWindow window)
        {
            style.position = new StyleEnum<Position>(Position.Absolute);
            style.left = 0;
            style.right = 0;
            style.bottom = 0;
            style.top = 0;

            pickingMode = PickingMode.Ignore;

            _window = window;
            _background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PixelEditor/Editor/Icons/Grid.psd");

            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var center = (Vector3)contentRect.center;
            var zoom = _window.Zoom;
            var hwidth = _window.CanvasWidth * 0.5f * zoom;
            var hheight = _window.CanvasHeight * 0.5f * zoom;
            //var gridScale = Mathf.NextPowerOfTwo((int)Mathf.Max(zoom / 4.0f, 1.0f)) / 8.0f; //  4 * 0.5f * Mathf.NextPowerOfTwo((int)Mathf.Max(zoom, 1.0f));
            var gridScale = 16.0f / zoom;
            var uvmax = new Vector2(
                (int)(_window.CanvasWidth / gridScale),
                (int)(_window.CanvasHeight / gridScale));
            uvmax = new Vector2(
                Mathf.NextPowerOfTwo((int)(uvmax.x - uvmax.x * 0.5f)),
                Mathf.NextPowerOfTwo((int)(uvmax.y - uvmax.y * 0.5f)));
            uvmax = Vector2.Max(Vector2.one, uvmax);

            var tl = center + new Vector3(-hwidth, hheight, 0);
            var tr = center + new Vector3(hwidth, hheight, 0);
            var br = center + new Vector3(hwidth, -hheight, 0);
            var bl = center + new Vector3(-hwidth, -hheight, 0);

            // Draw the background grid
            var mesh = context.Allocate(4, 6, _background);
            mesh.SetAllVertices(new Vertex[]
            {
                new Vertex { position = tl, uv = new Vector2(0,0), tint = Color.white },
                new Vertex { position = tr, uv = new Vector2(uvmax.x,0), tint = Color.white },
                new Vertex { position = br, uv = uvmax, tint = Color.white },
                new Vertex { position = bl, uv = new Vector2(0,uvmax.y), tint = Color.white}
            });
            mesh.SetAllIndices(new ushort[] { 2, 1, 0, 3, 2, 0 });

            if (_window.CurrentFile == null)
                return;

            // Draw each layer
            foreach (var petexture in _window.CurrentFile.images.Where(t => t.frame == _window.CurrentFrame).OrderBy(t => t.layer.order))
            {
                var tint = Color.white.MultiplyAlpha(petexture.layer.opacity);
                mesh = context.Allocate(4, 6, petexture.texture);
                mesh.SetAllVertices(new Vertex[]
                {
                    new Vertex { position = tl, uv = Vector2.zero, tint = tint },
                    new Vertex { position = tr, uv = new Vector2(1,0), tint = tint },
                    new Vertex { position = br, uv = Vector2.one, tint = tint },
                    new Vertex { position = bl, uv = new Vector2(0,1), tint = tint }
                });
                mesh.SetAllIndices(new ushort[] { 2, 1, 0, 3, 2, 0 });
            }
        }
    }
}
