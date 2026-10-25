using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ DISORIENTED, ON THE VICTIM'S OWN SCREEN ONLY (ABILITY-2, owner 2026-09-26: *"Fake screen/ fake
    /// slipper Aim. Make them hallucinate"*, *"hallucinations but make it so that some of the shit they see
    /// are real"*). While the local player is Disoriented, phantom copies of the other players and of the
    /// slippers drift among the real ones, drawn exactly like them (the same meshes and materials, a
    /// frozen pose re-baked every 0.2 s), so the real and the false cannot be told apart by looking. Nothing
    /// is networked and nothing is real to the game: another peer sees none of it, a phantom cannot be
    /// tagged, grabbed or blocked.
    ///
    /// Each phantom stands off its source by a hand-set offset and wanders, so it reads as "one of them is
    /// over there" rather than a double image. They fade in and out at the edges of the 4 s.
    /// </summary>
    [DefaultExecutionOrder(29000)]
    public sealed class DisorientedHallucinations : MonoBehaviour
    {
        private static DisorientedHallucinations _live;
        private CharacterMotor _victim;
        private readonly List<Phantom> _phantoms = new List<Phantom>();
        private float _age, _rebake;

        // Where each phantom stands off its source (metres) and how it wanders. Typed, not stamped.
        private static readonly Vector3[] Offsets = { new Vector3(2.6f, 0, 1.4f), new Vector3(-2.2f, 0, 2.3f), new Vector3(1.1f, 0, -2.7f),
                                                      new Vector3(-1.8f, 0, -1.6f), new Vector3(3.1f, 0, -0.6f), new Vector3(-0.9f, 0, 3.0f) };

        private sealed class Phantom
        {
            public Transform Source; public GameObject Body; public Vector3 Offset; public float Phase;
            public byte Kind, SourceSeat;
            public ushort Skin;
            public uint Id;
            public readonly List<(SkinnedMeshRenderer skin, MeshFilter filter)> Skins = new List<(SkinnedMeshRenderer, MeshFilter)>();
        }

        /// <summary>Start (or refresh) the hallucination for the local player's body.</summary>
        public static void Begin(CharacterMotor victim)
        {
            if (victim == null) return;
            if (_live != null)
            {
                if (_live._victim == victim) { _live._age = Mathf.Min(_live._age, 0.3f); return; }
                _live.End();
            }
            var go = new GameObject("~DisorientedHallucinations");
            _live = go.AddComponent<DisorientedHallucinations>();
            _live._victim = victim;
            _live.Build();
        }

        private void Build()
        {
            var round = GameServices.Round;
            int used = 0;
            if (round != null)
                foreach (var p in round.Players)
                {
                    if (p == null || p == _victim || used >= StatusRules.DisorientedPhantomPlayers) continue;
                    _phantoms.Add(Copy(p.transform, Offsets[used % Offsets.Length], used * 1.7f));
                    used++;
                }
            int shoes = 0;
            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (s == null || shoes >= StatusRules.DisorientedPhantomSlippers) continue;
                _phantoms.Add(Copy(s.transform, Offsets[(used + shoes + 2) % Offsets.Length] * 0.6f, shoes * 2.3f + 0.5f));
                shoes++;
            }
            for (int i = 0; i < _phantoms.Count; i++) _phantoms[i].Id = (uint)(i + 1);
        }

        private static Phantom Copy(Transform source, Vector3 offset, float phase)
        {
            var ph = new Phantom { Source = source, Offset = offset, Phase = phase, Body = new GameObject("phantom-" + source.name) };
            var actor = source.GetComponent<CharacterMotor>(); var shoe = source.GetComponent<Slipper>();
            ph.Kind = actor != null ? Net.SpectatorPhantom.DisorientedPlayer : Net.SpectatorPhantom.DisorientedShoe;
            int seat = actor != null ? actor.PlayerSlot : shoe != null ? shoe.SeatOfOrigin : -1;
            ph.SourceSeat = seat >= 0 && seat < Balance.PlayerCount ? (byte)seat : byte.MaxValue;
            ph.Skin = (ushort)(shoe != null ? Mathf.Clamp(shoe.SkinIndex, 0, Roster.Slippers.Count - 1) : 0);
            foreach (var r in source.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
                var part = new GameObject(r.name);
                part.transform.SetParent(ph.Body.transform, false);
                var filter = part.AddComponent<MeshFilter>();
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = r.sharedMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (r is SkinnedMeshRenderer skin) { filter.sharedMesh = new Mesh(); ph.Skins.Add((skin, filter)); }
                else if (r.TryGetComponent<MeshFilter>(out var mf)) filter.sharedMesh = mf.sharedMesh;
                // Keep the part where it sits relative to its source.
                part.transform.localPosition = source.InverseTransformPoint(r.transform.position);
                part.transform.localRotation = Quaternion.Inverse(source.rotation) * r.transform.rotation;
                part.transform.localScale = r is SkinnedMeshRenderer ? Vector3.one : r.transform.lossyScale;
            }
            return ph;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_victim == null || !_victim.IsDisoriented) { End(); return; }
            _rebake -= Time.deltaTime;
            bool rebake = _rebake <= 0f;
            if (rebake) _rebake = 0.2f;
            bool visible = SourceView(_victim);
            foreach (var ph in _phantoms)
            {
                if (ph.Source == null || ph.Body == null) continue;
                if (ph.Body.activeSelf != visible) ph.Body.SetActive(visible);
                // A slow wander so it never sits still like a mirror.
                float t = _age + ph.Phase;
                Vector3 wander = new Vector3(Mathf.Sin(t * 0.9f) * 0.6f, 0f, Mathf.Cos(t * 0.7f) * 0.6f);
                ph.Body.transform.SetPositionAndRotation(ph.Source.position + ph.Offset + wander,
                                                         ph.Source.rotation * Quaternion.Euler(0f, Mathf.Sin(t * 0.5f) * 40f, 0f));
                ph.Body.transform.localScale = ph.Source.lossyScale;
                if (rebake)
                    foreach (var (skin, filter) in ph.Skins)
                        if (skin != null && filter != null) skin.BakeMesh(filter.sharedMesh);
            }
        }
        private static bool SourceView(CharacterMotor victim)
        {
            var camera = Camera.main; var rig = camera != null ? camera.GetComponent<CameraSystem.CameraRig>() : null;
            return rig != null && rig.IsFollowing(victim);
        }
        private void LateUpdate()
        {
            bool visible = _victim != null && SourceView(_victim);
            foreach (var phantom in _phantoms)
                if (phantom.Body != null && phantom.Body.activeSelf != visible) phantom.Body.SetActive(visible);
        }
        internal static void CaptureObserved(CharacterMotor victim, ref Net.SpectatorPrivateView view)
        {
            if (_live == null || _live._victim != victim || !SourceView(victim)) return;
            foreach (var phantom in _live._phantoms)
            {
                if (view.Phantoms.Length >= Net.SpectatorPrivateView.MaxPhantoms) break;
                if (phantom.Body == null || !phantom.Body.activeInHierarchy) continue;
                var root = phantom.Body.transform;
                var record = new Net.SpectatorPhantom { Kind = phantom.Kind, Id = phantom.Id,
                    SourceSeat = phantom.SourceSeat, Skin = phantom.Skin,
                    Position = root.position, Rotation = root.rotation, Scale = root.lossyScale };
                if (record.IsValid) view.Phantoms.Add(record);
            }
        }

        private void End()
        {
            ReleasePhantoms();
            if (_live == this) _live = null;
            Destroy(gameObject);
        }
        private void ReleasePhantoms()
        {
            foreach (var ph in _phantoms)
            {
                if (ph.Body == null) continue;
                foreach (var (_, filter) in ph.Skins) if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);
                Destroy(ph.Body);
            }
            _phantoms.Clear();
        }
        private void OnDestroy() { ReleasePhantoms(); if (_live == this) _live = null; }
    }
}
