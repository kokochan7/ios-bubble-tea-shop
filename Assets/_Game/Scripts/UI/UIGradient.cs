using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// Colors a UI image with a smooth top-to-bottom gradient (multiplied with its own color),
    /// so backgrounds and buttons can have a soft modern fade without an image file.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class UIGradient : BaseMeshEffect
    {
        [SerializeField] private Color top = Color.white;
        [SerializeField] private Color bottom = Color.white;

        private static readonly List<UIVertex> vertices = new();

        /// <summary>Sets both colors (used by the scene builder).</summary>
        public void Set(Color topColor, Color bottomColor)
        {
            top = topColor;
            bottom = bottomColor;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper.currentVertCount == 0) return;
            vertices.Clear();
            helper.GetUIVertexStream(vertices);

            float min = float.MaxValue, max = float.MinValue;
            foreach (UIVertex vertex in vertices)
            {
                min = Mathf.Min(min, vertex.position.y);
                max = Mathf.Max(max, vertex.position.y);
            }
            float height = Mathf.Max(max - min, 0.0001f);
            for (int i = 0; i < vertices.Count; i++)
            {
                UIVertex vertex = vertices[i];
                Color tint = Color.Lerp(bottom, top, (vertex.position.y - min) / height);
                vertex.color = (Color32)((Color)vertex.color * tint);
                vertices[i] = vertex;
            }
            helper.Clear();
            helper.AddUIVertexTriangleStream(vertices);
        }
    }
}
