using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // A surface-bound readiness cue, with no gameplay or authored-material mutation.
    public sealed class CheskaFrostbiteCoating : MonoBehaviour
    {
        private CheskaHeroKit _kit;
        private Slipper _shoe;
        private MeshFilter _target, _copy;
        private Renderer _source;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private Material _material;
        private MaterialPropertyBlock _block;
        public float VisibleStrength { get; private set; }
        public Slipper Shoe => _shoe;

        public static CheskaFrostbiteCoating Ensure(MeshFilter target, Slipper shoe, CheskaHeroKit kit)
        {
            if (target == null || target.sharedMesh == null || shoe == null || kit == null || !kit.IsFrostbiteLoaded) return null;
            var old = target.GetComponentInChildren<CheskaFrostbiteCoating>(true);
            if (old != null)
            {
                if (old._shoe == shoe && old._kit == kit && old.gameObject.activeSelf) return old;
                old.gameObject.SetActive(false); Destroy(old.gameObject);
            }
            var go = new GameObject("FrostbiteSoleRime");
            go.layer = target.gameObject.layer; go.transform.SetParent(target.transform, false);
            var effect = go.AddComponent<CheskaFrostbiteCoating>();
            effect._target = target; effect._shoe = shoe; effect._kit = kit;
            effect._source = target.GetComponent<Renderer>();
            effect._copy = go.AddComponent<MeshFilter>();
            effect._renderer = go.AddComponent<MeshRenderer>();
            effect._renderer.receiveShadows = false;
            var shader = Resources.Load<Shader>("Shaders/FrostbiteLoad");
            if (shader == null) throw new System.InvalidOperationException("Frostbite load shader is missing.");
            effect._material = new Material(shader) { name = "Frostbite surface rime" };
            VfxRenderTag.Own(go, effect._material);
            effect._block = new MaterialPropertyBlock(); effect.RefreshMesh(); effect.Sample();
            return effect;
        }

        private void RefreshMesh()
        {
            _mesh = _target.sharedMesh; _copy.sharedMesh = _mesh;
            var materials = new Material[Mathf.Max(1, _mesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++) materials[i] = _material;
            _renderer.sharedMaterials = materials;
            var size = _mesh.bounds.size;
            Vector3 along = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
            Vector3 normal = size.y <= size.x && size.y <= size.z ? Vector3.up : size.x <= size.z ? Vector3.right : Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(along, normal)) > .5f) normal = Vector3.up;
            Vector3 across = Vector3.Cross(normal, along);
            float length = Mathf.Max(size.x, size.y, size.z);
            float width = Mathf.Abs(Vector3.Dot(size, across));
            _block.SetVector("_Centre", _mesh.bounds.center);
            _block.SetVector("_Along", along / Mathf.Max(length, .0001f));
            _block.SetVector("_Across", across / Mathf.Max(width, .0001f));
            _block.SetFloat("_Lift", Mathf.Max(length * .003f, .0001f));
        }

        private void LateUpdate() => Sample();
        private void Sample()
        {
            if (_kit == null || !_kit.IsFrostbiteLoaded || _shoe == null || _target == null || _target.sharedMesh == null)
            { gameObject.SetActive(false); Destroy(gameObject); return; }
            if (_target.sharedMesh != _mesh) RefreshMesh();
            bool held = _shoe.State == SlipperState.Held && _shoe.Holder != null
                && !_shoe.Holder.IsDefender && _shoe.Holder.AbilitySystem?.Kit == _kit;
            float remaining = _kit.AttackingSkill.DurationRemaining;
            float age = CryoRules.FrostbiteLoadSeconds - remaining;
            VisibleStrength = held ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / .18f)) * Mathf.Clamp01(remaining / .35f) : 0;
            _block.SetFloat("_Strength", VisibleStrength); _renderer.SetPropertyBlock(_block);
            _renderer.enabled = VisibleStrength > 0 && (_source == null || _source.enabled);
            if (_source != null)
            {
                _renderer.forceRenderingOff = _source.forceRenderingOff;
                _renderer.shadowCastingMode = _source.shadowCastingMode == ShadowCastingMode.ShadowsOnly
                    ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off;
            }
        }
    }
}
