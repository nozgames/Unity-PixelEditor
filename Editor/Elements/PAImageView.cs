using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PAImageView : VisualElement
    {
        private Texture _background = null;
        private PAWorkspace _workspace = null;

        public bool ShowCheckerboard { get; set; } = true;

        public PAImageView(PAWorkspace workspace)
        {
            AddToClassList("canvas");
            
            pickingMode = PickingMode.Ignore;

            _workspace = workspace;
            _background = PAUtils.LoadImage("Grid.psd");

            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var center = (Vector3)contentRect.center;
            var zoom = _workspace.Zoom;
            var hwidth = _workspace.ImageWidth * 0.5f * zoom;
            var hheight = _workspace.ImageHeight * 0.5f * zoom;            
            var uvmax = Vector2.zero;
            var uvmin = Vector2.zero;

            var tl = center + new Vector3(-hwidth, hheight, 0);
            var tr = center + new Vector3(hwidth, hheight, 0);
            var br = center + new Vector3(hwidth, -hheight, 0);
            var bl = center + new Vector3(-hwidth, -hheight, 0);

            if(ShowCheckerboard)
            {
                var gridScale = 16.0f / zoom;
                uvmax = new Vector2(
                    (int)(_workspace.ImageWidth / gridScale),
                    (int)(_workspace.ImageHeight / gridScale));

                uvmax = new Vector2(
                    Mathf.NextPowerOfTwo((int)(uvmax.x - uvmax.x * 0.5f)),
                    Mathf.NextPowerOfTwo((int)(uvmax.y - uvmax.y * 0.5f)));
                uvmax = Vector2.Max(Vector2.one, uvmax);
            }
            else
            {
                uvmin = uvmax = new Vector2(0.75f, 0.25f);
            }

            // Draw the background grid
            var mesh = context.Allocate(4, 6, _background);
            mesh.SetAllVertices(new Vertex[]
            {
                new Vertex { position = tl, uv = uvmin, tint = Color.white },
                new Vertex { position = tr, uv = new Vector2(uvmax.x,uvmin.y), tint = Color.white },
                new Vertex { position = br, uv = uvmax, tint = Color.white },
                new Vertex { position = bl, uv = new Vector2(uvmin.x,uvmax.y), tint = Color.white}
            });
            mesh.SetAllIndices(new ushort[] { 2, 1, 0, 3, 2, 0 });

            if (_workspace.File == null)
                return;

            // Draw each layer
            foreach (var petexture in _workspace.File.images.Where(t => t.frame == _workspace.SelectedFrame).OrderBy(t => t.layer.order))
            {
                if (!petexture.layer.visible)
                    continue;

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
