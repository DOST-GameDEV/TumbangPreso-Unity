#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Temporary Editor evidence: observes generated mesh before submission, never modifies it.</summary>
    public sealed class TumpMeshAudit : BaseMeshEffect
    {
        private bool _reported;
        public override void ModifyMesh(VertexHelper vh)
        {
            bool invalid = false; var vertex = new UIVertex(); string bad = "";
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                if (Finite(vertex.position.x) && Finite(vertex.position.y) && Finite(vertex.position.z)) continue;
                invalid = true; bad = $" vertex[{i}]={vertex.position}"; break;
            }
            if (_reported && !invalid) return;
            _reported = true;
            var rect = (RectTransform)transform; var image = GetComponent<Image>(); var raw = GetComponent<RawImage>();
            string sprite = image != null && image.sprite != null
                ? $" spriteRect={image.sprite.rect} spriteBounds={image.sprite.bounds} ppu={image.sprite.pixelsPerUnit}" : "";
            Debug.Log($"[TumpMeshAudit] {name} invalid={invalid} vertices={vh.currentVertCount}{bad} rect={rect.rect} anchors={rect.anchorMin}/{rect.anchorMax} position={rect.anchoredPosition3D} scale={rect.lossyScale} pivot={rect.pivot}{sprite} uv={(raw != null ? raw.uvRect.ToString() : "n/a")}");
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    }
}
#endif
