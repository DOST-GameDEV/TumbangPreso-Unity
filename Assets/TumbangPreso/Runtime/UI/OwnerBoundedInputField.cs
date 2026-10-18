using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Keep uGUI's caret and selection mesh inside the authored editable area.</summary>
    public sealed class OwnerBoundedInputField : InputField
    {
        private Mesh _bounded;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colours = new List<Color32>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();

        public override void Rebuild(CanvasUpdate update)
        {
            base.Rebuild(update);
            if (update != CanvasUpdate.LatePreRender || textComponent == null) return;
            var caret = textComponent.transform.parent.Find(name + " Input Caret");
            if (caret == null) return;
            var renderer = caret.GetComponent<CanvasRenderer>();
            if (renderer == null) return;
            if (_bounded == null) _bounded = new Mesh { name = "Bounded input caret", hideFlags = HideFlags.DontSave };
            var source = renderer.GetMesh(); if (source == null) return;
            source.GetVertices(_vertices); source.GetColors(_colours);
            source.GetUVs(0, _uvs); source.GetTriangles(_triangles, 0);
            var bounds = textComponent.rectTransform.rect;
            float bottom = bounds.yMin + 3, top = bounds.yMax - 3;
            for (int i = 0; i < _vertices.Count; i++)
            {
                var v = _vertices[i];
                v.x = Mathf.Clamp(v.x, bounds.xMin, bounds.xMax);
                v.y = Mathf.Clamp(v.y, bottom, top); _vertices[i] = v;
            }
            _bounded.Clear(); _bounded.SetVertices(_vertices); _bounded.SetColors(_colours);
            _bounded.SetUVs(0, _uvs); _bounded.SetTriangles(_triangles, 0); renderer.SetMesh(_bounded);
        }

        protected override void OnDestroy()
        {
            if (_bounded != null) Destroy(_bounded);
            base.OnDestroy();
        }
    }
}
