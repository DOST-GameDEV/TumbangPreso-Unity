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
            public readonly List<(SkinnedMeshRenderer skin, MeshFilter filter)> Skins = new List<(SkinnedMeshRenderer, MeshFilter)>();
        }

        /// <summary>Start (or refresh) the hallucination for the local player's body.</summary>
        public static void Begin(CharacterMotor victim)
        {
            if (victim == null) return;
            if (_live != null) { _live._age = Mathf.Min(_live._age, 0.3f); return; }
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
        }

        private static Phantom Copy(Transform source, Vector3 offset, float phase)
        {
            var ph = new Phantom { Source = source, Offset = offset, Phase = phase, Body = new GameObject("phantom-" + source.name) };
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
            foreach (var ph in _phantoms)
            {
                if (ph.Source == null || ph.Body == null) continue;
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

        private void End()
        {
            foreach (var ph in _phantoms)
            {
                if (ph.Body == null) continue;
                foreach (var (_, filter) in ph.Skins) if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);
                Destroy(ph.Body);
            }
            _phantoms.Clear();
            if (_live == this) _live = null;
            Destroy(gameObject);
        }
    }
}
