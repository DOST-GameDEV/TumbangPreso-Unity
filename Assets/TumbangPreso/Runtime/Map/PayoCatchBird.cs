using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// WHAT CATCHES A FALLEN BODY ON PAYO: a great hawk spirit, in place of the Arena's rescue drone (2026-10-12). The
    /// rules and the timings are untouched: the host still decides the catch (`ArenaFallRecovery`), the carry is still
    /// the `Drone` edge recovery whose kind, phase and pose every peer already has, and it still costs the tag's five
    /// seconds. This only DRAWS it, on every peer, from that replicated state (`ArenaFallRecovery.NoDrone` set, so the
    /// drone and its beam are not drawn here).
    ///
    /// The hawk stoops onto the body from behind and above as it is caught (and cries: `PayoSound.HawkCry`), carries it
    /// by the shoulders with deep slow wingbeats on the way up and across, spreads and holds its wings to set it down,
    /// and then climbs away over the valley. Its wings are the model's own nodes (any child whose name has "wing" in
    /// it; left and right by "_l" / "_r", ".l" / ".r", "left" / "right"), turned about the bird's long axis.
    ///
    /// ⚠️ PRESENTATION ONLY: no collider, nothing sent. Hidden in the menus' map preview (it only ever appears on a carry).
    /// </summary>
    [DefaultExecutionOrder(710)]
    public sealed class PayoCatchBird : MonoBehaviour
    {
        /// <summary>The hawk's model, left inactive in the scene by the builder and copied for each carry.</summary>
        public GameObject Template;
        /// <summary>How far over the body's feet the hawk's own origin flies while it carries, and how big it is drawn.</summary>
        public float Above = 2.3f, Scale = 1.0f;
        /// <summary>How far behind (minus: ahead of) the hawk's origin its talons hang along its long axis, metres (the art's `carry_point` z).</summary>
        public float CarryBack;

        private sealed class Bird
        {
            public Transform Root;
            public readonly List<Transform> Left = new List<Transform>(), Right = new List<Transform>();
            public readonly List<Quaternion> LeftRest = new List<Quaternion>(), RightRest = new List<Quaternion>();
            public float Since, Leaving = -1.0f;
            public Vector3 At, Heading = Vector3.forward, LeaveFrom;
        }

        private readonly Dictionary<CharacterMotor, Bird> _birds = new Dictionary<CharacterMotor, Bird>();
        private readonly List<CharacterMotor> _done = new List<CharacterMotor>();
        private Transform _root;

        private void OnDisable()
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = null; _birds.Clear();
        }

        private Bird Make()
        {
            if (Template == null) return null;
            if (_root == null) _root = new GameObject("Payo hawks (made at run time)") { hideFlags = HideFlags.HideAndDontSave }.transform;
            var go = Instantiate(Template, _root, false);
            go.name = "hawk"; go.SetActive(true);
            go.transform.localScale = Vector3.one * Scale;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            var bird = new Bird { Root = go.transform };
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (!n.Contains("wing")) continue;
                bool left = n.Contains("_l") || n.Contains(".l") || n.Contains("left") || n.EndsWith("l");
                bool right = n.Contains("_r") || n.Contains(".r") || n.Contains("right") || n.EndsWith("r");
                if (left && !right) { bird.Left.Add(t); bird.LeftRest.Add(t.localRotation); }
                else if (right) { bird.Right.Add(t); bird.RightRest.Add(t.localRotation); }
            }
            return bird;
        }

        private void LateUpdate()
        {
            var round = GameServices.Round;
            if (round == null || Template == null) return;
            float dt = Time.deltaTime;
            foreach (var who in round.Players)
            {
                if (who == null || !who.gameObject.activeInHierarchy || who.EdgeKind != EdgeRecoveryKind.Drone) continue;
                if (!_birds.TryGetValue(who, out var bird) || bird == null || bird.Root == null)
                {
                    bird = Make();
                    if (bird == null) return;
                    _birds[who] = bird;
                    // It comes from behind and above, out of the valley's side of the sky.
                    bird.At = who.transform.position + new Vector3(6.0f, 16.0f, 10.0f);
                    PayoSound.HawkCry(who.transform.position + Vector3.up * 6.0f);
                }
                bird.Leaving = -1.0f;
                bird.Since += dt;
                Vector3 want = who.transform.position + Vector3.up * Above;
                // The stoop: fast at first, then it has the body and is simply where the body is.
                float grip = Mathf.Clamp01(bird.Since / 0.45f);
                Vector3 before = bird.At;
                bird.At = Vector3.Lerp(bird.At, want, Mathf.Clamp01(dt * 9.0f + grip * grip * 0.5f));
                if (grip >= 1.0f) bird.At = want;
                Vector3 travel = bird.At - before; travel.y = 0.0f;
                if (travel.sqrMagnitude > 1e-6f) bird.Heading = Vector3.Slerp(bird.Heading, travel.normalized, Mathf.Clamp01(dt * 6.0f));
                // Phase 0 the catch, 1 the lift and the flight, 2 the set-down: deep beats while it lifts, wings held wide to land.
                float beat = who.EdgePhase == 2 ? 1.1f : 2.6f, depth = who.EdgePhase == 2 ? 14.0f : 38.0f, lift = who.EdgePhase == 2 ? 22.0f : 4.0f;
                Pose(bird, bird.At - Quaternion.LookRotation(bird.Heading.sqrMagnitude > 1e-6f ? bird.Heading : Vector3.forward, Vector3.up) * new Vector3(0.0f, 0.0f, CarryBack),
                     bird.Heading, Mathf.Sin(bird.Since * beat * Mathf.PI * 2.0f) * depth + lift, who.EdgePhase == 2 ? -8.0f : 6.0f);
            }

            // A hawk whose body is down climbs away and is put away.
            _done.Clear();
            foreach (var entry in _birds)
            {
                var who = entry.Key; var bird = entry.Value;
                if (bird == null || bird.Root == null) { _done.Add(who); continue; }
                if (who != null && who.gameObject.activeInHierarchy && who.EdgeKind == EdgeRecoveryKind.Drone) continue;
                if (bird.Leaving < 0.0f) { bird.Leaving = 0.0f; bird.LeaveFrom = bird.At; }
                bird.Leaving += dt;
                float u = bird.Leaving / 2.4f;
                if (u >= 1.0f) { Destroy(bird.Root.gameObject); _done.Add(who); continue; }
                Vector3 away = (bird.Heading + new Vector3(0.35f, 0.0f, 0.8f)).normalized;
                bird.At = bird.LeaveFrom + away * (22.0f * u * u) + Vector3.up * (14.0f * u * (0.4f + u));
                Pose(bird, bird.At, Vector3.Slerp(bird.Heading, away, Mathf.Clamp01(u * 2.0f)), Mathf.Sin(bird.Leaving * 3.1f * Mathf.PI * 2.0f) * 40.0f, 18.0f);
            }
            foreach (var who in _done) _birds.Remove(who);
        }

        /// <summary>The hawk at a place, flying a heading, its wings `flap` degrees up (down if negative), nose `pitch` degrees up.</summary>
        private static void Pose(Bird bird, Vector3 at, Vector3 heading, float flap, float pitch)
        {
            if (heading.sqrMagnitude < 1e-6f) heading = Vector3.forward;
            bird.Root.SetPositionAndRotation(at, Quaternion.LookRotation(heading, Vector3.up) * Quaternion.Euler(-pitch, 0.0f, 0.0f));
            for (int i = 0; i < bird.Left.Count; i++) bird.Left[i].localRotation = bird.LeftRest[i] * Quaternion.Euler(0.0f, 0.0f, -flap);
            for (int i = 0; i < bird.Right.Count; i++) bird.Right[i].localRotation = bird.RightRest[i] * Quaternion.Euler(0.0f, 0.0f, flap);
        }
    }
}
