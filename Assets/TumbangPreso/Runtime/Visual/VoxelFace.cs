using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Amihan's cutscene faces (the Airburst v4 face set, owner-approved: squint, whistle, grin, wink). The cast's faces are
    /// geometry: dark eye and mouth blocks standing a hair proud of the head's skin plane. Each look is a copy of her own
    /// `head-mesh`, baked in the Editor (`AmihanFaceAuthor`): the old blocks re-coloured to her skin texel, the look's new
    /// blocks added in the same layer. Showing a look swaps the COPIED body's head mesh, so the face is lit and shaded exactly
    /// like her head, because it is her head (v4 r4 to r8 drew an overlay card instead, which read as a box on her face).
    /// </summary>
    public sealed class VoxelFace
    {
        public enum Look { Rest, Squint, Whistle, Grin, Wink, Teehee }

        public const string ResourceFolder = "Models/AmihanFace";
        public static string MeshName(Look look) => "amihan-face-" + look.ToString().ToLowerInvariant();

        // Each look as dark blocks: x across the face from its centre, y up from the bottom of the original mouth, in the rig's
        // metres. The authored rest face is eyes 0.046 wide and 0.058 tall at +-0.076, a small smile 0.058 wide below them.
        public static readonly Dictionary<Look, float[,]> Blocks = new Dictionary<Look, float[,]>
        {
            // Reading the wind: both eyes narrowed to slits, the inner ends lifted (she is sizing it up), the mouth a short line.
            [Look.Squint] = new float[,]
            {
                { -.099f, -.072f, .060f, .072f }, { -.072f, -.053f, .066f, .078f },
                {  .053f,  .072f, .066f, .078f }, {  .072f,  .099f, .060f, .072f },
                { -.020f,  .020f, .008f, .016f },
            },
            // The two-finger whistle: eyes half shut, lips pushed out into a small round O.
            [Look.Whistle] = new float[,]
            {
                { -.099f, -.053f, .052f, .074f }, { .053f, .099f, .052f, .074f },
                { -.012f, .012f, -.006f, .000f }, { -.012f, .012f, .018f, .024f },
                { -.018f, -.012f, .000f, .018f }, { .012f, .018f, .000f, .018f },
            },
            // The grin: happy closed eyes (an upturned arc each) and a wide open D of a grin.
            [Look.Grin] = new float[,]
            {
                { -.099f, -.087f, .056f, .068f }, { -.087f, -.065f, .068f, .080f }, { -.065f, -.053f, .056f, .068f },
                {  .053f,  .065f, .056f, .068f }, {  .065f,  .087f, .068f, .080f }, {  .087f,  .099f, .056f, .068f },
                { -.040f, .040f, .006f, .022f }, { -.028f, .028f, -.004f, .006f },
            },
            // The wink: one open eye, one shut in an arc, a crooked grin.
            [Look.Wink] = new float[,]
            {
                { -.099f, -.053f, .041f, .099f },
                {  .053f,  .065f, .056f, .068f }, {  .065f,  .087f, .068f, .080f }, {  .087f,  .099f, .056f, .068f },
                { -.034f, .030f, .008f, .022f }, { -.024f, .018f, .000f, .008f },
            },
            // TEEHEE (owner, 2026-10-03: "TEEHEE pose", "make the pose cuter"): both eyes squeezed shut into > < chevrons pointing
            // in to the nose, and a little open mouth: >v<. Her finish (eight blocks: the head has corners for no more).
            [Look.Teehee] = new float[,]
            {
                // (v11: at 16 mm the chevrons broke up into specks in the close-up; as bold as her other eyes.)
                { .074f, .101f, .080f, .096f }, { .056f, .082f, .066f, .082f }, { .074f, .101f, .052f, .068f },
                { -.101f, -.074f, .080f, .096f }, { -.082f, -.056f, .066f, .082f }, { -.101f, -.074f, .052f, .068f },
                { -.030f, .030f, .012f, .024f }, { -.016f, .016f, .000f, .013f },
            },
        };

        private readonly SkinnedMeshRenderer _head;
        private readonly Dictionary<Look, SkinnedMeshRenderer> _looks;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        public Look Current { get; private set; } = Look.Rest;
        /// <summary>The looks' renderers, so the scene captures them with its own pieces.</summary>
        public IEnumerable<Renderer> Renderers { get { foreach (var r in _looks.Values) yield return r; } }

        private VoxelFace(SkinnedMeshRenderer head, Dictionary<Look, SkinnedMeshRenderer> looks) { _head = head; _looks = looks; }

        /// <summary>The looks for this body's head (the copied cutscene body, never a live player). Null when the head or the
        /// baked meshes are missing.</summary>
        public static VoxelFace Attach(IEnumerable<Renderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                if (!(renderer is SkinnedMeshRenderer skin) || skin.sharedMesh == null) continue;
                if (skin.sharedMesh.name.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                // ⚠️ ONE RENDERER PER LOOK, never a swapped mesh (films v4 r9 and r10: swapping the copied head's mesh stopped
                // its rendering, "does not match the expected mesh data size"). Each is a sibling of the head, bound to the same
                // bones and drawn with the same material; showing a look hides the head and draws that one.
                var looks = new Dictionary<Look, SkinnedMeshRenderer>();
                foreach (Look look in System.Enum.GetValues(typeof(Look)))
                {
                    if (look == Look.Rest) continue;
                    var mesh = Resources.Load<Mesh>(ResourceFolder + "/" + MeshName(look));
                    if (mesh == null || mesh.bindposes.Length != skin.sharedMesh.bindposes.Length)
                    { foreach (var made in looks.Values) Gone(made.gameObject); return null; }
                    var go = new GameObject("head-mesh-" + MeshName(look));
                    go.layer = skin.gameObject.layer;
                    go.transform.SetParent(skin.transform.parent, false);
                    go.transform.SetLocalPositionAndRotation(skin.transform.localPosition, skin.transform.localRotation);
                    go.transform.localScale = skin.transform.localScale;
                    var face = go.AddComponent<SkinnedMeshRenderer>();
                    face.sharedMesh = mesh; face.bones = skin.bones; face.rootBone = skin.rootBone; face.localBounds = skin.localBounds;
                    face.sharedMaterials = skin.sharedMaterials; face.updateWhenOffscreen = true;
                    face.shadowCastingMode = skin.shadowCastingMode; face.receiveShadows = skin.receiveShadows;
                    face.renderingLayerMask = skin.renderingLayerMask; face.lightProbeUsage = skin.lightProbeUsage;
                    face.enabled = false;
                    looks[look] = face;
                }
                return new VoxelFace(skin, looks);
            }
            return null;
        }

        public void Show(Look look)
        {
            if (_head == null) return;
            Current = look;
            bool body = _head.gameObject.activeInHierarchy;
            foreach (var pair in _looks)
            {
                bool on = pair.Key == look && body;
                pair.Value.enabled = on;
                if (on) { _head.GetPropertyBlock(_block); pair.Value.SetPropertyBlock(_block); }
            }
            // The head itself draws only at rest; `forceRenderingOff` stays the capture's to switch.
            _head.enabled = look == Look.Rest;
        }

        /// <summary>Back to her own face, and the look renderers gone.</summary>
        public void Dispose()
        {
            if (_head != null) _head.enabled = true;
            foreach (var r in _looks.Values) if (r != null) Gone(r.gameObject);
            _looks.Clear();
        }

        private static void Gone(Object value)
        {
            if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
