using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

class PixelGrid : ImmediateModeElement
{
    private const float ZoomAlphaZero = 2.0f;
    private const float ZoomAlphaOne = 10.0f;

    static Material _lineMaterial;

    private NoZ.PixelEditor.Layer _layer;
    private Color _color;

    public PixelGrid(NoZ.PixelEditor.Layer layer)
    {
        _layer = layer;
        AddToClassList("pixelGrid");

        style.position = new StyleEnum<Position>(Position.Absolute);
        style.left = 0;
        style.right = 0;
        style.bottom = 0;
        style.top = 0;

        var rgb = EditorPrefs.GetString("Scene/Grid").Split(';').Skip(1).Select(s => float.Parse(s)).ToArray();
        _color = new Color(rgb[0], rgb[1], rgb[2]);
    }

    protected override void ImmediateRepaint()
    {
        var alpha = Mathf.Clamp((_layer.Zoom - ZoomAlphaZero) / (ZoomAlphaOne - ZoomAlphaZero), 0.0f, 1.0f);
        var width = _layer._texture.width;
        var height = _layer._texture.height;
        var hwidth = width * 0.5f * _layer.Zoom;
        var hheight = height * 0.5f * _layer.Zoom;
        var center = contentRect.center;
        var size = new Vector2(width * _layer.Zoom, height * _layer.Zoom);

        LineMaterial.SetPass(0);

        // Draw the pixel grid
        if (alpha > 0.0f)
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(_color.r, _color.g, _color.b, alpha));
            DrawLines(center, size, 0, 1, _layer._texture.height, 1);
            DrawLines(center, size, 1, 1, _layer._texture.width, 1);
            GL.End();
        }

        // Draw the border
        GL.Begin(GL.LINES);
        GL.Color(_color);
        DrawLines(center, size, 0, 0, _layer._texture.height + 1, _layer._texture.height);
        DrawLines(center, size, 1, 0, _layer._texture.width + 1, _layer._texture.width);
        GL.End();
    }

    private void DrawLines (Vector3 center, Vector2 size, int axis0, int start, int end, int increment)
    {
        var axis1 = (axis0 + 1) % 2;
        var hsize0 = size[axis0] * 0.5f;
        var hsize1 = size[axis1] * 0.5f;        
        var min0 = Mathf.Clamp(center[axis0] - hsize0, contentRect.min[axis0], contentRect.max[axis0]);
        var max0 = Mathf.Clamp(center[axis0] + hsize0, contentRect.min[axis0], contentRect.max[axis0]);
        if (min0 == max0)
            return;

        for (int i = start; i < end; i += increment)
        {
            var coord1 = center[axis1] - hsize1 + i * _layer.Zoom;
            if (coord1 < contentRect.min[axis1] || coord1 > contentRect.max[axis1])
                continue;

            var from = Vector2.zero;
            var to = Vector2.zero;
            from[axis0] = min0;
            from[axis1] = coord1;
            to[axis0] = max0;
            to[axis1] = coord1;

            GL.Vertex3(from.x, from.y, 1);
            GL.Vertex3(to.x, to.y, 1);
        }
    }

    static Material LineMaterial {
        get {
            if (!_lineMaterial)
            {
                _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _lineMaterial.SetInt("_ZWrite", 0);
            }

            return _lineMaterial;
        }
    }
}