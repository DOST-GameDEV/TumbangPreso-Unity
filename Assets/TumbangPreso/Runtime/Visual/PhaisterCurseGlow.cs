using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE CURSE'S LIGHT INSIDE THE VICTIM (owner, 2026-10-31: "add a purple glow inside/to the character"). Every renderer of
    /// the body gets one companion drawing the same mesh on the same bones with `PhaisterCurseGlow.shader`; the hero's own
    /// shaders, materials and textures are not touched, and nothing is rebuilt per frame. Built once when first needed.
    ///
    /// ⚠️ A companion follows its source's `enabled` and `forceRenderingOff` each frame, so a body hidden by anything else
    /// (a teleport, first person, a capture path) takes its glow with it.
    ///
    /// Cost while it shows: one extra draw per renderer of the body, its triangles again, one material. None when it is off.
    /// </summary>
    internal sealed class PhaisterCurseGlow
    {
        private readonly List<(Renderer Source, Renderer Copy)> _pairs = new List<(Renderer, Renderer)>();
        private readonly Material _ink;
        private readonly Transform _model;
        public Transform Model => _model;

        public PhaisterCurseGlow(Transform model)
        {
            _model = model;
            var shader = Resources.Load<Shader>("Shaders/PhaisterCurseGlow");
            if (shader == null || model == null) { Debug.LogWarning("[PhaisterCurseGlow] nothing to light."); return; }
            _ink = new Material(shader) { name = "PhaisterCurseGlow", hideFlags = HideFlags.DontSave };
            _ink.SetTexture("_CloudTex", FxStudyKit.Mask("erode_cloud"));
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.GetComponent<VfxRenderTag>() != null) continue;
                Renderer copy = null;
                if (r is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                {
                    var go = Companion(r);
                    var s = go.AddComponent<SkinnedMeshRenderer>();
                    s.sharedMesh = skinned.sharedMesh; s.bones = skinned.bones; s.rootBone = skinned.rootBone;
                    s.localBounds = skinned.localBounds; s.updateWhenOffscreen = skinned.updateWhenOffscreen;
                    for (int i = 0; i < skinned.sharedMesh.blendShapeCount; i++) s.SetBlendShapeWeight(i, skinned.GetBlendShapeWeight(i));
                    copy = s;
                }
                else if (r is MeshRenderer && r.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                {
                    var go = Companion(r);
                    go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    copy = go.AddComponent<MeshRenderer>();
                }
                if (copy == null) continue;
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = _ink;
                copy.sharedMaterials = mats;
                copy.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                copy.receiveShadows = false;
                copy.enabled = false;
                _pairs.Add((r, copy));
            }
        }

        private static GameObject Companion(Renderer source)
        {
            var go = new GameObject("CurseGlow") { hideFlags = HideFlags.DontSave };
            go.layer = source.gameObject.layer;
            go.transform.SetParent(source.transform, false);
            VfxRenderTag.Attach(go);
            return go;
        }

        /// <summary>This frame's light. <paramref name="level"/> at or below 0 turns every companion off.</summary>
        public void Pose(float level, float clock, Vector3 chest, float reach)
        {
            bool on = level > 0.005f && _ink != null;
            if (on)
            {
                _ink.SetFloat("_Glow", level); _ink.SetFloat("_Age", clock);
                _ink.SetVector("_Chest", new Vector4(chest.x, chest.y, chest.z, reach));
            }
            foreach (var (source, copy) in _pairs)
            {
                if (copy == null) continue;
                bool show = on && source != null && source.enabled && !source.forceRenderingOff && source.gameObject.activeInHierarchy;
                if (copy.enabled != show) copy.enabled = show;
            }
        }

        public void Free()
        {
            foreach (var (_, copy) in _pairs) if (copy != null) PhaisterLook.Free(copy.gameObject);
            _pairs.Clear();
            PhaisterLook.Free(_ink);
        }
    }
}
