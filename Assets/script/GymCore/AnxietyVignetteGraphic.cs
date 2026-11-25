using UnityEngine;
using UnityEngine.UI;

namespace FinalProject.Gym.Core
{
    // Four native UI gradients: no imported textures or render-pipeline changes.
    public sealed class AnxietyVignetteGraphic : MaskableGraphic
    {
        [SerializeField, Range(0.05f, 0.45f)] private float edgeWidth = 0.22f;
        [SerializeField, Range(0f, 0.6f)] private float maximumAlpha = 0.32f;
        public void SetIntensity(float value)
        {
            Color next = new Color(0f, 0f, 0f, Mathf.Clamp01(value) * maximumAlpha);
            if (color != next) color = next;
            raycastTarget = false;
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect outer = rectTransform.rect;
            float x = outer.width * edgeWidth;
            float y = outer.height * edgeWidth;
            Vector2 bl = new Vector2(outer.xMin, outer.yMin), br = new Vector2(outer.xMax, outer.yMin);
            Vector2 tl = new Vector2(outer.xMin, outer.yMax), tr = new Vector2(outer.xMax, outer.yMax);
            Vector2 ibl = bl + new Vector2(x, y), ibr = br + new Vector2(-x, y);
            Vector2 itl = tl + new Vector2(x, -y), itr = tr - new Vector2(x, y);
            Quad(mesh, bl, ibl, itl, tl);
            Quad(mesh, tl, itl, itr, tr);
            Quad(mesh, tr, itr, ibr, br);
            Quad(mesh, br, ibr, ibl, bl);
        }
        private void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int start = mesh.currentVertCount;
            Color clear = new Color(color.r, color.g, color.b, 0f);
            mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, clear, Vector2.zero);
            mesh.AddVert(c, clear, Vector2.zero); mesh.AddVert(d, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
