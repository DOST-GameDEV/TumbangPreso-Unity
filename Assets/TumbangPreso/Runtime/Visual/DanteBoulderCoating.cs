using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Observes the existing payload, including loose shoes and replica snapshots.
    // Never changes the shoe's state, mesh or authored material array.
    //
    // BOULDER's crust, beat by beat (`docs/reports/dante-presentation-2026-10-16/plan.md` section 3), from the age this peer
    // has seen the load for (the host's deadline does not cross the wire; a late joiner sees it settled):
    //  * TELL/RELEASE: ten typed grit pieces rise off the court into his palm; the crust closes over the slipper in three
    //    steps from the palm outward (`_Creep`).
    //  * LINGER: a slow molten seam; a grain drops off it when he runs.
    //  * WARNING (last 3 s of the 15): the seam cools, molten to dull red to grey, and the crust flakes at its edges.
    //  * TRAVEL: a short grit trail behind the flying slipper.
    //  * CONTACT: on a player the crust bursts into six chunks (the victim's pebble ring is `ConcussedPebbles`); on the court
    //    it cracks and drops four (the payload stays loaded on a loose slipper, so the crust stays too: the picture never
    //    lies about the rule); expired in hand, it flakes away.
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
        private bool _wasLoaded;
        private float _loadedAge, _grainClock;
        private SlipperState _lastState;
        private Vector3 _lastPos;
        private const float LoadSeconds = Core.GeoRules.BoulderLoadSeconds, WarnSeconds = 3f;

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

        private void LateUpdate() { Beats(); Sample(); }
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
            // The crust's own clock: three steps closing over .25 to .45 s, the last 3 s cooling and flaking.
            float creep = _loadedAge < .25f ? 0f : _loadedAge < .32f ? 1f : _loadedAge < .39f ? 2f : 3f;
            float late = Mathf.Clamp01((_loadedAge - (LoadSeconds - WarnSeconds)) / WarnSeconds);
            if (_world && _shoe.State != SlipperState.Held) { creep = 3f; late = 0f; }
            _block.SetFloat("_Creep", creep);
            _block.SetFloat("_Cool", late);
            _block.SetFloat("_Flake", late * late * .85f);
            _renderer.SetPropertyBlock(_block);
            _renderer.gameObject.layer = _target.gameObject.layer;
            _renderer.enabled = _source == null || _source.enabled;
            if (_source != null)
            {
                _renderer.forceRenderingOff = _source.forceRenderingOff;
                _renderer.shadowCastingMode = _source.shadowCastingMode == ShadowCastingMode.ShadowsOnly
                    ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off;
            }
        }

        /// <summary>The crust's clock on every copy; the bursts, grains and trail on the world shoe only (the first-person copy only wears the crust).</summary>
        private void Beats()
        {
            if (_shoe == null) return;
            bool loaded = _shoe.isActiveAndEnabled && _shoe.Affinity == SlipperAffinity.Concussed;
            var state = _shoe.State;
            Vector3 at = _shoe.transform.position;
            float dt = Time.deltaTime;
            if (loaded && !_wasLoaded)
            {
                _loadedAge = 0f;
                if (_world && state == SlipperState.Held) DanteBoulderGrit.Gather(at);
            }
            if (loaded) _loadedAge += dt;
            if (!_world) { _wasLoaded = loaded; return; }
            if (!loaded && _wasLoaded)
            {
                // Spent on a body in flight: burst. Expired in hand: flake away.
                if (_lastState == SlipperState.InFlight) DanteStatusShatter.Rubble(at, at - _lastPos, 6, 1f, false);
                else DanteStatusShatter.Rubble(at, Vector3.down, 3, .6f, true);
            }
            else if (loaded && _lastState == SlipperState.InFlight && state != SlipperState.InFlight && state != SlipperState.Held)
                DanteStatusShatter.Rubble(at, at - _lastPos, 4, .8f, true);     // landed on the court: it cracks, chips drop
            if (loaded)
            {
                float speed = dt > 0f ? (at - _lastPos).magnitude / dt : 0f;
                _grainClock -= dt;
                if (state == SlipperState.InFlight && _grainClock <= 0f) { DanteBoulderGrit.Drop(at, true); _grainClock = .045f; }
                else if (state == SlipperState.Held && speed > 3.2f && _grainClock <= 0f) { DanteBoulderGrit.Drop(at, false); _grainClock = .42f; }
            }
            _wasLoaded = loaded; _lastState = state; _lastPos = at;
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
