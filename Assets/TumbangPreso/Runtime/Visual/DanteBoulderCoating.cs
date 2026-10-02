using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Observes the existing payload, including loose shoes and replica snapshots.
    // Never changes the shoe's state, mesh or authored material array.
    public sealed class DanteBoulderCoating : MonoBehaviour
    {
        private static Shader _shader;
        private Slipper _shoe;
        private bool _world;
        private MeshFilter _target, _copy;
        private Renderer _source;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private Material _material;
        private MaterialPropertyBlock _block;

        public static void TrackWorld(Slipper shoe)
        {
            if (_shader == null) _shader = Resources.Load<Shader>("Shaders/BoulderLoad");
            var effect = shoe.GetComponent<DanteBoulderCoating>();
            if (effect == null) effect = shoe.gameObject.AddComponent<DanteBoulderCoating>();
            effect._world = true;
            effect._shoe = shoe;
        }

        public static void MatchOwner(MeshFilter target, Slipper shoe)
        {
            var effect = target.GetComponent<DanteBoulderCoating>();
            if (effect == null)
            {
                if (shoe == null || shoe.Affinity != SlipperAffinity.Concussed) return;
                effect = target.gameObject.AddComponent<DanteBoulderCoating>();
            }
            effect._shoe = shoe;
            effect._target = target;
            effect.Sample();
        }

        private void LateUpdate() => Sample();
        private void OnDisable() { if (_renderer != null) _renderer.enabled = false; }
        private void Sample()
        {
            bool loaded = _shoe != null && _shoe.isActiveAndEnabled && _shoe.Affinity == SlipperAffinity.Concussed
                && (_world || _shoe.State == SlipperState.Held);
            if (!loaded)
            {
                if (_renderer != null) _renderer.enabled = false;
                return;
            }
            if (_target == null && _world) _target = _shoe.GetComponentInChildren<MeshFilter>();
            if (_target == null || _target.sharedMesh == null) return;
            if (_renderer == null)
            {
                if (_shader == null) throw new System.InvalidOperationException("Boulder load shader is missing.");
                var go = new GameObject("BoulderStoneInlay");
                go.transform.SetParent(_target.transform, false);
                _copy = go.AddComponent<MeshFilter>();
                _renderer = go.AddComponent<MeshRenderer>();
                _renderer.receiveShadows = false;
                _material = new Material(_shader) { name = "Boulder stone inlay" };
                VfxRenderTag.Own(go, _material);
                _block = new MaterialPropertyBlock();
            }
            _source = _target.GetComponent<Renderer>();
            if (_mesh != _target.sharedMesh) RefreshMesh();
            _renderer.gameObject.layer = _target.gameObject.layer;
            _renderer.enabled = _source == null || _source.enabled;
            if (_source != null)
            {
                _renderer.forceRenderingOff = _source.forceRenderingOff;
                _renderer.shadowCastingMode = _source.shadowCastingMode == ShadowCastingMode.ShadowsOnly
                    ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off;
            }
        }

        private void RefreshMesh()
        {
            _mesh = _target.sharedMesh;
            _copy.sharedMesh = _mesh;
            var materials = new Material[Mathf.Max(1, _mesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++) materials[i] = _material;
            _renderer.sharedMaterials = materials;
            Vector3 size = _mesh.bounds.size;
            Vector3 along = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
            Vector3 normal = size.y <= size.x && size.y <= size.z ? Vector3.up : size.x <= size.z ? Vector3.right : Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(along, normal)) > .5f) normal = along == Vector3.up ? Vector3.right : Vector3.up;
            Vector3 across = Vector3.Cross(normal, along);
            float length = Mathf.Max(size.x, size.y, size.z);
            float width = Mathf.Abs(Vector3.Dot(size, across));
            _block.SetVector("_Centre", _mesh.bounds.center);
            _block.SetVector("_Along", along / Mathf.Max(length, .0001f));
            _block.SetVector("_Across", across / Mathf.Max(width, .0001f));
            _block.SetFloat("_Lift", Mathf.Max(length * .002f, .0001f));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
