using System.Collections.Generic;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // A camera-owned render copy. No input, collision, abilities or actor simulation.
    internal sealed class ObservedPrivateView
    {
        private sealed class Copy
        {
            public GameObject Body, Model;
            public Transform Source;
            public byte Kind, Seat;
            public ushort Skin;
            public readonly List<(SkinnedMeshRenderer source, Mesh mesh)> Skins = new();
            public float RebakeAt;
        }
        private readonly Transform _parent;
        private readonly Dictionary<ulong, Copy> _copies = new(9);
        private readonly HashSet<ulong> _seen = new();
        private readonly List<ulong> _retired = new(9);
        private Slipper[] _shoes;
        private float _scanAt = -99;
        private Visual.GroundReticle _marker;
        public int Count => _copies.Count;
        public bool MarkerVisible => _marker != null && _marker.gameObject.activeInHierarchy;
        public ObservedPrivateView(Transform parent) => _parent = parent;

        public void Apply(SpectatorPrivateView view)
        {
            if (!view.IsValid) { Clear(); return; }
            _seen.Clear();
            foreach (var item in view.Phantoms)
            {
                ulong key = ((ulong)item.Kind << 32) | item.Id;
                Transform source = Resolve(item);
                if (source == null) continue;
                var visual = source.GetComponent<Visual.CharacterVisual>();
                GameObject model = visual != null ? visual.Model : source.gameObject;
                _copies.TryGetValue(key, out var copy);
                if (copy != null && (copy.Source != source || copy.Model != model || copy.Kind != item.Kind
                    || copy.Seat != item.SourceSeat || copy.Skin != item.Skin))
                { DestroyCopy(copy); _copies.Remove(key); copy = null; }
                if (copy == null)
                {
                    copy = Build(source, item); copy.Model = model;
                    if (copy.Body.GetComponentsInChildren<Renderer>().Length == 0) { DestroyCopy(copy); continue; }
                    _copies[key] = copy;
                }
                _seen.Add(key);
                copy.Body.transform.SetPositionAndRotation(item.Position, item.Rotation.normalized);
                Vector3 parentScale = _parent != null ? _parent.lossyScale : Vector3.one;
                copy.Body.transform.localScale = Divide(item.Scale, parentScale);
                if (Time.time >= copy.RebakeAt)
                {
                    copy.RebakeAt = Time.time + .2f;
                    foreach (var skin in copy.Skins) if (skin.source != null && skin.mesh != null) skin.source.BakeMesh(skin.mesh);
                }
            }
            _retired.Clear();
            foreach (var pair in _copies) if (!_seen.Contains(pair.Key)) _retired.Add(pair.Key);
            foreach (var key in _retired) { DestroyCopy(_copies[key]); _copies.Remove(key); }
            if (view.Marker.Present)
            {
                if (_marker == null) _marker = Visual.GroundReticle.Create(_parent);
                _marker.transform.localScale = Divide(Vector3.one, _parent != null ? _parent.lossyScale : Vector3.one);
                _marker.ShowObservedMarker(view.Marker); _marker.Tick(Time.deltaTime);
            }
            else if (_marker != null) _marker.Hide();
        }
        private Transform Resolve(SpectatorPhantom item)
        {
            if (item.Kind == SpectatorPhantom.DisorientedPlayer)
                return GameServices.Round?.PlayerAt(item.SourceSeat)?.transform;
            if (_shoes == null || Time.unscaledTime >= _scanAt)
            {
                _scanAt = Time.unscaledTime + .5f;
                _shoes = Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include);
            }
            Slipper matchingSkin = null;
            foreach (var shoe in _shoes)
            {
                if (shoe == null || Mathf.Max(0, shoe.SkinIndex) != item.Skin) continue;
                if (shoe.SeatOfOrigin == item.SourceSeat) return shoe.transform;
                matchingSkin ??= shoe;
            }
            return matchingSkin != null ? matchingSkin.transform : null;
        }
        private Copy Build(Transform source, SpectatorPhantom item)
        {
            var copy = new Copy { Source = source, Kind = item.Kind, Seat = item.SourceSeat, Skin = item.Skin,
                Body = new GameObject("Observed private phantom") };
            copy.Body.transform.SetParent(_parent, false);
            foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer
                    || !TemplateVisible(renderer.transform, source)) continue;
                bool skin = renderer is SkinnedMeshRenderer;
                if (skin && item.Kind == SpectatorPhantom.HexShoe) continue;
                var original = renderer.GetComponent<MeshFilter>();
                if (!skin && (original == null || original.sharedMesh == null)) continue;
                var part = new GameObject(renderer.name); part.transform.SetParent(copy.Body.transform, false);
                var filter = part.AddComponent<MeshFilter>(); var surface = part.AddComponent<MeshRenderer>();
                surface.sharedMaterials = renderer.sharedMaterials;
                surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                surface.receiveShadows = renderer.receiveShadows;
                if (skin)
                {
                    var mesh = new Mesh { name = "Observed phantom pose" }; filter.sharedMesh = mesh;
                    copy.Skins.Add(((SkinnedMeshRenderer)renderer, mesh));
                }
                else filter.sharedMesh = original.sharedMesh;
                part.transform.localPosition = source.InverseTransformPoint(renderer.transform.position);
                part.transform.localRotation = Quaternion.Inverse(source.rotation) * renderer.transform.rotation;
                part.transform.localScale = skin ? Vector3.one : item.Kind == SpectatorPhantom.HexShoe
                    ? Divide(renderer.transform.lossyScale, source.lossyScale) : renderer.transform.lossyScale;
            }
            return copy;
        }
        private static bool TemplateVisible(Transform part, Transform source)
        {
            for (var current = part; current != null && current != source; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }
        private static Vector3 Divide(Vector3 value, Vector3 scale) => new(value.x / Safe(scale.x), value.y / Safe(scale.y), value.z / Safe(scale.z));
        private static float Safe(float value) => Mathf.Abs(value) < .0001f ? .0001f : value;
        private static void DestroyCopy(Copy copy)
        {
            if (copy.Body != null) { copy.Body.SetActive(false); Object.Destroy(copy.Body); }
            foreach (var skin in copy.Skins) if (skin.mesh != null) Object.Destroy(skin.mesh);
            copy.Skins.Clear();
        }
        public void Clear()
        {
            foreach (var copy in _copies.Values) DestroyCopy(copy);
            _copies.Clear(); _seen.Clear(); _retired.Clear();
            if (_marker != null) _marker.Hide();
        }
        public void Dispose()
        {
            Clear();
            if (_marker != null) { _marker.gameObject.SetActive(false); Object.Destroy(_marker.gameObject); _marker = null; }
            _shoes = null;
        }
    }
}
