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
            ClockId = Shader.PropertyToID("_Clock"), SwayId = Shader.PropertyToID("_Sway"), RimId = Shader.PropertyToID("_Rim");

        private readonly MeshRenderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly Color _rim;
        public Transform Transform { get; }

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
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _rim = rim;
        }

        public void Pose(Vector3 at, Quaternion facing, float scale, float fade, float clock, float sway = .05f, float flash = 0f)
        {
            if (_renderer == null) return;
            Transform.SetPositionAndRotation(at, facing);
            Transform.localScale = Vector3.one * scale;
            _renderer.enabled = fade > .005f && _material != null;
            _block.SetFloat(FadeId, Mathf.Clamp01(fade));
            _block.SetFloat(FlashId, flash);
            _block.SetFloat(ClockId, clock);
            _block.SetFloat(SwayId, sway);
            _block.SetColor(RimId, _rim);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
