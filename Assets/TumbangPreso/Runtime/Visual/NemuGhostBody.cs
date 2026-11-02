using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Draws one of Nemu's voxel ghosts (`NemuGhostModels`) with `Shaders/BlindfoldGhost`: unlit, its own rim colour, glowing
    /// eyes, a swaying tail, and a fade that keeps it one body. The owner poses it every frame; nothing here moves it.
    /// No shadow: a ghost does not cast one.
    /// </summary>
    public sealed class NemuGhostBody
    {
        private static Material _material;
        private static readonly int FadeId = Shader.PropertyToID("_Fade"), FlashId = Shader.PropertyToID("_Flash"),
            ClockId = Shader.PropertyToID("_Clock"), SwayId = Shader.PropertyToID("_Sway"), RimId = Shader.PropertyToID("_Rim"),
            TrailId = Shader.PropertyToID("_Trail"), ShadeKeepId = Shader.PropertyToID("_ShadeKeep");

        private readonly MeshRenderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly Color _rim;
        public Transform Transform { get; }
        /// <summary>How far its tail lags behind its flight (its own space, metres at the tip): `PetGhostFlight.TrailLocal`. Only
        /// `Shaders/PetGhost` reads it; ASTRA's voxel shader ignores it.</summary>
        public Vector3 Trail;
        /// <summary>Its one-piece mesh is not drawn: a parts rig (`PetGhostRig`) hung under <see cref="Transform"/> draws the body
        /// instead, and anything else hung there (the bear's arms) is posed as before.</summary>
        public bool MeshHidden;

        public NemuGhostBody(Transform parent, string name, Mesh mesh, Color rim)
        {
            if (_material == null)
            {
                var shader = Shader.Find("TumbangPreso/BlindfoldGhost");
                if (shader != null) _material = new Material(shader) { name = "NemuGhost" };
                else Debug.LogError("[NemuGhostBody] Shaders/BlindfoldGhost is missing from the build (GameBuilder).");
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Transform = go.transform;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            _renderer = go.AddComponent<MeshRenderer>();
            // A pet ghost's mesh (`PetGhostModels`, behind its switch) brings its own painted material; ASTRA's voxels keep theirs.
            _renderer.sharedMaterial = PetGhostModels.MaterialFor(mesh, _material);
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _rim = rim;
        }

        public void Pose(Vector3 at, Quaternion facing, float scale, float fade, float clock, float sway = .05f, float flash = 0f)
            => Pose(at, facing, Vector3.one * scale, fade, clock, sway, flash);

        /// <summary>Posed with its own squash and stretch (<paramref name="scale"/> per axis, its local axes).</summary>
        public void Pose(Vector3 at, Quaternion facing, Vector3 scale, float fade, float clock, float sway = .05f, float flash = 0f)
        {
            if (_renderer == null) return;
            Transform.SetPositionAndRotation(at, facing);
            Transform.localScale = scale;
            Shade(fade, clock, sway, flash);
        }

        /// <summary>
        /// A part hung on another body (an arm, a lantern): made with that body's <see cref="Transform"/> as its parent, it is
        /// posed in that body's local space, so it squashes, leans and fades with it.
        /// </summary>
        public void PoseLocal(Vector3 local, Quaternion rotation, float fade, float clock, float flash = 0f)
        {
            if (_renderer == null) return;
            Transform.localPosition = local; Transform.localRotation = rotation; Transform.localScale = Vector3.one;
            Shade(fade, clock, 0f, flash);
        }

        private void Shade(float fade, float clock, float sway, float flash)
        {
            _renderer.enabled = !MeshHidden && fade > .005f && _renderer.sharedMaterial != null;
            _block.SetFloat(FadeId, Mathf.Clamp01(fade));
            _block.SetFloat(FlashId, flash);
            _block.SetFloat(ClockId, clock);
            _block.SetFloat(SwayId, sway);
            _block.SetColor(RimId, _rim);
            _block.SetVector(TrailId, Trail);
            // The pet's shade keeps its paint's hue under the painted look (`NemuGhostLook`); 0 is the old cool shade.
            _block.SetFloat(ShadeKeepId, NemuGhostLook.On ? 1f : 0f);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
