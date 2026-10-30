using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// MAGMA FIST'S FIST OF FIRE (owner 2026-10-29: "no, i was thinking a more fieryy design, like its not a solid opaque fist,
    /// but u can make it out to be a fist still"). The shape is `giant_fist_flame` (`tools/build_sean_magma_fist.py`), v5's
    /// size and pitch, so `SeanMagmaFist`'s rule-tied numbers (`GiantHeight`, `GiantScale`, the rise and the sink) hold; it is
    /// drawn by `SeanFireFist.shader` as a core and two flame shells, which says why each part looks as it does.
    /// Not inked and not toon-dressed: fire has no outline. One material per variant for the process; each fist's clock, form
    /// and tear ride in a property block. v5's opaque `giant_fist` stays for the rework's look (switch off).
    /// </summary>
    public sealed class SeanFireFistLook
    {
        public const string ModelName = "giant_fist_flame", ShaderName = "TumbangPreso/SeanFireFist";

        /// <summary>
        /// THE ONE LOOK: a strong core under thinner flame. Three were boarded (A mostly flame, B balanced, C strong core); the
        /// owner picked C on 2026-10-29 ("c looks good to go", then "no we're locking c, no need to keep anything else"), so the
        /// other two and a later deeper-red D were removed. C's numbers below are unchanged from the board it was picked on.
        /// </summary>
        public enum Variant { C_Core }

        /// <summary>The one a match draws.</summary>
        // KEEP: owner 2026-10-29, "c looks good to go" (fire fist board f1). Approved look; do not change.
        public static Variant Current = Variant.C_Core;

        private static readonly Material[] Looks = new Material[3];
        private static readonly int AgeId = Shader.PropertyToID("_Age"), FormId = Shader.PropertyToID("_Form"),
            TearId = Shader.PropertyToID("_Tear");

        private readonly Renderer[] _renderers;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        public GameObject Root { get; }

        private static Material Look(Variant variant)
        {
            int k = (int)variant;
            if (Looks[k] != null) return Looks[k];
            var shader = Shader.Find(ShaderName);
            if (shader == null) return null;
            var m = new Material(shader) { name = "SeanFireFist-" + variant, hideFlags = HideFlags.HideAndDontSave, renderQueue = 3010 };
            var a = FxStudyKit.Mask("erode_streak"); var b = FxStudyKit.Mask("erode_cloud");
            if (a != null) m.SetTexture("_NoiseA", a);
            if (b != null) m.SetTexture("_NoiseB", b);
            // C as picked: core inset (m, the shape's own units), core density, core cover; flame show, flame cover.
            Set(m, 0f, 1.05f, .85f, .7f, .55f);
            Looks[k] = m;
            return m;
        }

        private static void Set(Material m, float shrink, float gain, float coreCover, float shells, float cover)
        {
            m.SetFloat("_CoreShrink", shrink); m.SetFloat("_CoreGain", gain); m.SetFloat("_CoreCover", coreCover);
            m.SetFloat("_ShellAlpha", shells); m.SetFloat("_Cover", cover);
        }

        /// <summary>The fist of fire under `parent`, or null (nothing left behind) if its model or its shader is missing.</summary>
        public static SeanFireFistLook Spawn(Transform parent, Variant variant)
        {
            var look = Look(variant);
            var source = HeroPropAssets.Load(SeanProp.ResourceFolder, ModelName);
            if (look == null || source == null) return null;
            var go = Object.Instantiate(source, parent, false);
            go.name = "SeanFireFist-" + variant;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c); }
            return new SeanFireFistLook(go, look);
        }

        private SeanFireFistLook(GameObject root, Material look)
        {
            Root = root;
            _renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in _renderers)
            {
                r.sharedMaterials = new[] { look };
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                VfxRenderTag.Attach(r.gameObject);
                // Its flames reach past the mesh (the shells, the tongues, the taller column).
                if (r is MeshRenderer mr) mr.localBounds = new Bounds(new Vector3(0f, .6f, .15f), new Vector3(1.6f, 1.8f, 1.6f));
            }
            Pose(0f, 0f, 0f);
        }

        /// <summary>`age` its clock, `form` 0 a column of fire to 1 the fist, `tear` 0 whole to 1 torn away from the top.</summary>
        public void Pose(float age, float form, float tear)
        {
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetFloat(AgeId, age); _block.SetFloat(FormId, Mathf.Clamp01(form)); _block.SetFloat(TearId, Mathf.Clamp01(tear));
                r.SetPropertyBlock(_block);
            }
        }
    }
}
