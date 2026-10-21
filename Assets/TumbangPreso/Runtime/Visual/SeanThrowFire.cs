using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// EMPOWERED THROW'S NEW LOOK IN A MATCH: the effects study's `FxStudySeanThrow` at its painting s2 (three crisp bands),
    /// driven by the real loaded tsinelas. The owner, 2026-10-18, choosing between the boards: "s2 for empowered".
    ///
    /// IT IS A LOOK ONLY. The load, the throw, the first impact, the 1.25 m push and the can are `SeanHeroKit`'s and
    /// `Slipper.ResolveEmpoweredThrowImpact`'s, and know nothing of this; nothing here is sent anywhere. Every peer draws it
    /// from state every peer already has:
    ///   * `Follow`, from `Slipper.CreateFireFlightVisual`, which runs on the host at the launch and on every other peer
    ///     (a late joiner included) the moment the slipper's snapshot first says it is flying with `FireExplosive`;
    ///   * the shoe's own transform each frame, written into the trail's history;
    ///   * the first impact: on the host, `Burst` with the exact centre the push is resolved from; on every other peer, the
    ///     moment the snapshot stops saying `FireExplosive` in flight (the host clears it at that same impact and the
    ///     change is a watched field, so it travels reliably), at where the shoe is then.
    ///
    /// ⚠️ A TRAIL IS HISTORY. The effect lives on its own object at the world's origin, never under the shoe, so a point
    /// written where the shoe was stays there: it is never re-pointed when the shoe turns or banks, it only ages, thins
    /// and is eaten where it lies (`SeanFireThrow.shader`). After the impact nothing more is written; a shoe that banks
    /// on as an ordinary slipper leaves the fire behind it to die in place.
    ///
    /// ⚠️ POOLED: one effect per tsinelas, built at its first loaded throw and reused for every one after, so a throw makes
    /// nothing.
    /// </summary>
    public sealed class SeanThrowFire : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ THE ONE SWITCH. On, the loaded tsinelas flies as the s2 fireball with its history trail, and its first impact is
        /// the s2 burst, ring, embers and smoke, on every peer. Off, it is drawn exactly as before 2026-10-19: the
        /// `InkFlight` trail and the point light on the shoe (`Slipper.CreateFireFlightVisual`) and the Ignition blast on the
        /// host (`HeroHazards.CreateExplosionVisual`). The owner, 2026-10-19: "the ones that ive said are good to go should
        /// be put in game already". TO GO BACK, MAKE THE LINE BELOW `false`.
        /// </summary>
        public static bool Painted = true;

        /// <summary>The painting the owner chose (`FxStudySeanThrow.Stylise`): 2, three crisp bands, bolder.</summary>
        public const int Painting = 2;

        // How long after the impact the effect is still drawing: the burst and ring 0.5 s, the embers 0.9 s, the smoke 1.0 s.
        private const float AfterImpact = 1.05f;
        // A flight that never reports an impact (a round reset in the air) lets its trail die and ends.
        private const float TrailDies = 0.4f;
        // An impact this close over the ground is drawn ON the ground, as the bench draws its landing: the ring is on the
        // floor at the rule's radius and the burst stands on it.
        private const float OnTheGround = 0.6f;
        // The burst's middle stands this far over the impact the effect is told of (`SeanFireThrow.shader`, the burst card
        // at `_Impact + 0.35` as it opens). An impact high on a wall is told this much lower, so the burst is centred where
        // the push is.
        private const float BurstLift = 0.35f;
        // ⚠️ A POINT IS WRITTEN AT MOST SIXTY TIMES A SECOND, THE BENCH'S OWN RATE. The history holds 32 points
        // (`FxStudySeanThrow.History`); written every frame at 144 a second they would cover 0.22 s of flight against a
        // trail that lives 0.30 s, and the long trail would come out short on a fast screen only.
        private const float WriteEvery = 1f / 60f;

        private static readonly Dictionary<Slipper, SeanThrowFire> Pool = new Dictionary<Slipper, SeanThrowFire>();

        private FxStudySeanThrow _fx;
        private Slipper _shoe;
        private float _age, _endsAt, _writtenAt;
        private bool _live, _flying, _pending;
        private Vector3 _last, _going = Vector3.forward, _pendingAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetPool() => Pool.Clear();

        /// <summary>The loaded shoe has just taken flight on this peer: start its fire where it is.</summary>
        public static void Follow(Slipper shoe)
        {
            if (!Painted || !Application.isPlaying || shoe == null) return;
            var fire = For(shoe);
            if (fire != null) fire.Begin();
        }

        /// <summary>The host's first impact, at the exact centre the push is measured from.</summary>
        public static void Burst(Slipper shoe, Vector3 centre)
        {
            if (!Painted || !Application.isPlaying || shoe == null) return;
            if (!Pool.TryGetValue(shoe, out var fire) || fire == null || !fire._live || !fire._flying) return;
            fire._pending = true; fire._pendingAt = centre;
        }

        private static SeanThrowFire For(Slipper shoe)
        {
            if (Pool.TryGetValue(shoe, out var fire) && fire != null) return fire;
            var go = new GameObject("SeanThrowFire");
            fire = go.AddComponent<SeanThrowFire>();
            fire._shoe = shoe;
            fire._fx = NewLook(go.transform);
            go.SetActive(false);
            Pool[shoe] = fire;
            return fire;
        }

        /// <summary>
        /// The look exactly as a match builds it, under `parent` (a film calls this so it shows the game's own construction).
        /// The bench class as it is, at the chosen painting: `Stylise` is read once, in the constructor, so it is set for the
        /// construction and put back, exactly as the bench's film does, and the bench keeps its own default.
        /// </summary>
        public static FxStudySeanThrow NewLook(Transform parent)
        {
            int keep = FxStudySeanThrow.Stylise;
            FxStudySeanThrow.Stylise = Painting;
            FxStudySeanThrow fx;
            try { fx = new FxStudySeanThrow(parent, AmihanWhirlwind.DayLight); }
            finally { FxStudySeanThrow.Stylise = keep; }
            foreach (var renderer in parent.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
            return fx;
        }

        private void Begin()
        {
            _age = 0f; _writtenAt = 0f; _live = true; _flying = true; _pending = false; _endsAt = float.PositiveInfinity;
            _fx.Restart();
            _last = _shoe.transform.position;
            _fx.Record(_last, 0f);
            gameObject.SetActive(true);
            Pose();
        }

        private void LateUpdate()
        {
            if (!_live) return;
            if (_shoe == null) { End(); return; }
            _age += Time.deltaTime;
            if (_flying)
            {
                bool still = _shoe.State == SlipperState.InFlight && _shoe.Affinity == SlipperAffinity.FireExplosive;
                if (_pending) Land(_pendingAt);
                // Only another peer guesses the impact from the snapshot. The host is told it (`Burst`), so a host flight that
                // ends without one (a reset in the air) simply lets its trail die.
                else if (!still && !NetAuthority.ShouldResolve()) Land(_shoe.transform.position);
                else if (!still) { _flying = false; _endsAt = _age + TrailDies; }
                else
                {
                    Vector3 now = _shoe.transform.position;
                    if ((now - _last).sqrMagnitude > 1e-6f) _going = (now - _last).normalized;
                    _last = now;
                    if (_age - _writtenAt >= WriteEvery - 1e-4f) { _fx.Record(now, _age); _writtenAt = _age; }
                }
            }
            Pose();
            if (_age > _endsAt) End();
        }

        private void Land(Vector3 centre)
        {
            _flying = false; _pending = false;
            _fx.Record(centre, _age);
            Vector3 ground = VfxShapes.GroundPoint(centre);
            Vector3 told = centre.y - ground.y < OnTheGround ? ground : centre - Vector3.up * BurstLift;
            _fx.Impact(told, _age);
            _endsAt = _age + AfterImpact;
        }

        private void Pose()
        {
            var lens = Camera.main;
            Vector3 eye = lens != null ? lens.transform.position : _last + new Vector3(0f, 1.6f, -6f);
            Vector3 head = _flying && _shoe != null ? _shoe.transform.position : _last;
            _fx.Pose(_age, eye, head, _flying, _going);
        }

        private void End() { _live = false; _flying = false; gameObject.SetActive(false); }

        private void OnDestroy()
        {
            if (_shoe != null && Pool.TryGetValue(_shoe, out var mine) && mine == this) Pool.Remove(_shoe);
            _fx?.Release(); _fx = null;
        }
    }
}
