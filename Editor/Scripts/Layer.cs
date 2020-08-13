using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    internal class Layer : VisualElement
    {
        public Texture2D _texture = null;

        private float _zoom = 10.0f;

        private PixelGrid _grid = null;

        public float Zoom {
            get => _zoom;
            set {
                _zoom = value;
                MarkDirtyRepaint();
            }
        }
        
        public Layer(int width, int height)
        {
            style.position = new StyleEnum<Position>(Position.Absolute);
            style.left = 0;
            style.right = 0;
            style.bottom = 0;
            style.top = 0;

            generateVisualContent += OnGenerateVisualContent;
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for(int x=0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _texture.SetPixel(x, y, Color.clear);

            _texture.filterMode = FilterMode.Point;
            _texture.Apply();

            //RegisterCallback<MouseDownEvent>(OnMouseDown);

            _grid = new PixelGrid(this);
            Add(_grid);
        }

        private Vector2Int LocalToPixel(Vector2 localPosition)
        {
            // Transform the mouse coordinate into texture coordinate space
            localPosition -= contentRect.center;
            localPosition /= new Vector2(_texture.width * _zoom, _texture.height * _zoom);
            localPosition += new Vector2(0.5f, 0.5f);
            localPosition *= new Vector2(_texture.width, _texture.height);

            return new Vector2Int((int)Mathf.Floor(localPosition.x), (int)Mathf.Floor(localPosition.y));
        }

        public bool IsMouseOver (Vector2 localPosition)
        {
            var pixel = LocalToPixel(localPosition);
            return pixel.x >= 0 && pixel.x < _texture.width && pixel.y >= 0 && pixel.y < _texture.height;
        }

        public Color GetColor (Vector2 localPosition, Color defaultColor)
        {
            // Convert the coordinate and make sure its within the texture
            var pixel = LocalToPixel(localPosition);
            if (pixel.x < 0 || pixel.x >= _texture.width)
                return defaultColor;
            if (pixel.y < 0 || pixel.y >= _texture.height)
                return defaultColor;

            return _texture.GetPixel(pixel.x, _texture.height - pixel.y - 1);
        }

        /// <summary>
        /// Draw a pixel to the layer
        /// </summary>
        public void Draw (Vector2 localPosition, Color color)
        {
            // Convert the coordinate and make sure its within the texture
            var pixel = LocalToPixel(localPosition);
            if (pixel.x < 0 || pixel.x >= _texture.width)
                return;
            if (pixel.y < 0 || pixel.y >= _texture.height)
                return;

            // Set the pixel
            _texture.SetPixel(pixel.x, _texture.height - 1 - pixel.y, color);
            _texture.Apply();
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var center = (Vector3)contentRect.center;
            var zoom = _zoom;
            var hwidth = _texture.width * 0.5f * zoom;
            var hheight = _texture.height * 0.5f * zoom;

            var data = context.Allocate(4, 6, _texture);
            data.SetAllVertices(new Vertex[]
            {
                new Vertex { 
                    position = center + new Vector3(-hwidth, hheight, 0),
                    uv = new Vector2(0,0),
                    tint = Color.white
                },
                new Vertex { 
                    position = center + new Vector3(hwidth, hheight, 0), 
                    uv = new Vector2(1,0), 
                    tint = Color.white
                },
                new Vertex { 
                    position = center + new Vector3(hwidth, -hheight,0),
                    uv = new Vector2(1,1),
                    tint = Color.white
                },
                new Vertex { 
                    position = center + new Vector3(-hwidth, -hheight,0), 
                    uv = new Vector2(0,1),
                    tint = Color.white}
            });
            data.SetAllIndices(new ushort[] { 2, 1, 0, 3, 2, 0 });
        }
    }
}
