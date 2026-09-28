using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ HEXED, ON THE VICTIM'S OWN SCREEN ONLY (HERO-10 v3, the owner's status table: *"Hallucinations of slippers randomly
    /// appear on your screen for 7.5 seconds."*). While the local player is HEXED, phantom slippers turn up among the real ones:
    /// each lies on the court for a second or two and is gone, drawn with a real slipper's own meshes and materials so it cannot
    /// be told apart by its look. Plan 9.5's one tell for a sharp player: A PHANTOM CASTS NO SHADOW. Nothing is networked and
    /// nothing is real to the game: another peer sees none of it, and a phantom cannot be picked up, grabbed or thrown.
    ///
    /// Adapted from `DisorientedHallucinations` (the same copy of a real object's renderers); the difference is what it copies
    /// and how: slippers only, one at a time, placed where a player looks (in front of the victim, or beside a real loose
    /// slipper so a pair lies where one should), popped in and out at the ground rather than wandering.
    /// </summary>
    public sealed class HexedPhantomSlippers : MonoBehaviour
    {
        private static HexedPhantomSlippers _live;
        private CharacterMotor _victim;
        private readonly List<Phantom> _phantoms = new List<Phantom>();
        private float _nextIn;
        private int _count;

        /// <summary>How many may lie on the court at once, and how often a new one turns up (seconds, typed per turn).</summary>
        private const int MaxAtOnce = 4;
        private static readonly float[] Gaps = { 0.25f, 0.8f, 0.55f, 1.1f, 0.7f, 0.95f, 0.5f, 1.25f, 0.65f, 0.9f };
        /// <summary>How long each lies there, typed per turn so no two match.</summary>
        private static readonly float[] Lives = { 1.6f, 2.4f, 1.2f, 2.8f, 1.9f, 2.2f, 1.4f, 2.6f, 1.7f, 2.1f };
        /// <summary>Where each turns up: distance ahead of the victim (m) and its bearing off their view (degrees).</summary>
        private static readonly Vector2[] Spots = { new Vector2(4.5f, -12f), new Vector2(3.0f, 26f), new Vector2(6.2f, 8f),
                                                    new Vector2(2.4f, -34f), new Vector2(5.4f, 30f), new Vector2(3.8f, -4f),
                                                    new Vector2(7.0f, -22f), new Vector2(2.8f, 14f) };

        private const float PopSeconds = 0.12f;

        private sealed class Phantom
        {
            public GameObject Body; public float Age, Life; public Vector3 Scale;
        }

        /// <summary>Start (or refresh) the hallucination for the local player's body.</summary>
        public static void Begin(CharacterMotor victim)
        {
            if (victim == null) return;
            if (_live != null) return;
            var go = new GameObject("~HexedPhantomSlippers");
            _live = go.AddComponent<HexedPhantomSlippers>();
            _live._victim = victim;
            _live._nextIn = 0.15f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_victim == null || !_victim.IsHexed) { End(); return; }

            for (int i = _phantoms.Count - 1; i >= 0; i--)
            {
                var ph = _phantoms[i];
                if (ph.Body == null) { _phantoms.RemoveAt(i); continue; }
                ph.Age += dt;
                if (ph.Age >= ph.Life) { Destroy(ph.Body); _phantoms.RemoveAt(i); continue; }
                // In and out at the ground: a quick pop, never a fade (a lit slipper cannot fade without looking wrong).
                float pop = Mathf.Min(Mathf.Clamp01(ph.Age / PopSeconds), Mathf.Clamp01((ph.Life - ph.Age) / PopSeconds));
                ph.Body.transform.localScale = ph.Scale * Mathf.SmoothStep(0.0f, 1.0f, pop);
            }

            _nextIn -= dt;
            if (_nextIn > 0.0f) return;
            _nextIn = Gaps[_count % Gaps.Length];
            if (_phantoms.Count < MaxAtOnce) Spawn();
        }

        private void Spawn()
        {
            var slippers = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None);
            if (slippers.Length == 0) return;
            int turn = _count++;

            // A real loose one to copy (its rest pose is the truth); any slipper's look if none lies loose.
            Slipper source = null;
            foreach (var s in slippers) if (s != null && s.State == SlipperState.Loose) { source = s; break; }
            bool loose = source != null;
            if (source == null) source = slippers[turn % slippers.Length];
            if (source == null) return;

            // Every other one beside a real loose slipper, the rest ahead of the victim where they are looking.
            Vector3 at;
            if (loose && turn % 2 == 1)
            {
                Vector3 side = Quaternion.Euler(0.0f, 70.0f + 97.0f * turn, 0.0f) * Vector3.forward;
                at = source.transform.position + side * (1.2f + 0.4f * (turn % 3));
            }
            else
            {
                Vector3 look = Camera.main != null ? Camera.main.transform.forward : _victim.transform.forward;
                look.y = 0.0f;
                if (look.sqrMagnitude < 0.0001f) look = _victim.transform.forward;
                var spot = Spots[turn % Spots.Length];
                at = _victim.transform.position + Quaternion.Euler(0.0f, spot.y, 0.0f) * look.normalized * spot.x;
            }

            float lift = loose ? source.transform.position.y - Slipper.GroundY(source.transform.position) : 0.0f;
            at.y = Slipper.GroundY(at) + lift;
            Quaternion turnTo = Quaternion.Euler(0.0f, 53.0f * turn + 20.0f, 0.0f);
            Quaternion rotation = loose ? turnTo * source.transform.rotation : turnTo;

            var body = Copy(source.transform);
            body.transform.SetPositionAndRotation(at, rotation);
            var ph = new Phantom { Body = body, Life = Lives[turn % Lives.Length], Scale = source.transform.lossyScale };
            body.transform.localScale = Vector3.zero;
            _phantoms.Add(ph);
        }

        /// <summary>The slipper's own meshes and materials, with NO SHADOW (the tell), and nothing to collide with.</summary>
        private static GameObject Copy(Transform source)
        {
            var root = new GameObject("phantom-slipper");
            foreach (var r in source.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer || r is SkinnedMeshRenderer) continue;
                if (!r.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null) continue;
                var part = new GameObject(r.name);
                part.transform.SetParent(root.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = r.sharedMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = r.receiveShadows;
                part.transform.localPosition = source.InverseTransformPoint(r.transform.position);
                part.transform.localRotation = Quaternion.Inverse(source.rotation) * r.transform.rotation;
                Vector3 s = source.lossyScale, p = r.transform.lossyScale;
                part.transform.localScale = new Vector3(p.x / Mathf.Max(1e-4f, s.x), p.y / Mathf.Max(1e-4f, s.y), p.z / Mathf.Max(1e-4f, s.z));
            }
            return root;
        }

        private void End()
        {
            foreach (var ph in _phantoms) if (ph.Body != null) Destroy(ph.Body);
            _phantoms.Clear();
            if (_live == this) _live = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            foreach (var ph in _phantoms) if (ph.Body != null) Destroy(ph.Body);
            if (_live == this) _live = null;
        }
    }
}
