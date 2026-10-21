using TumbangPreso.Abilities;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE CROSSCURRENT OF ILYAS, DRAWN AS THE WATER WALL'S WATER. It is behind a switch (`RafiWaterVisual.NewCurrent`,
    /// ON); the glassy crest it replaces is still there, one word away. The owner, 2026-10-19, of the bench's rw8:
    /// "cross current looks good", then "the ones that ive said are good to go should be put in game already".
    ///
    /// ⚠️ THE CURRENT ITSELF IS NOT MADE HERE. It is the effects study's (`FxStudyRafiCross`, approved and locked at rw8),
    /// which already takes its width, gather, range and speed from `RafiRules`. This CALLS that, once per cast, and
    /// changes nothing of it. What differs from the bench, and why:
    ///   * how much of it is light laid on (`RafiWallCurtain.DayLight`, 0.27 against the bench's 0.45), the wall's own
    ///     daylight share, which is also what the approved daylight film (`crossviewsdayrw8`) was filmed with;
    ///   * each cast's pictures slide by its event id (the bench's own `seed` input), so two currents in one round do
    ///     not wear the same streaks, and every peer slides them alike because every peer has the id;
    ///   * TIGHT CUT. The rule's column is then narrower and faster (radius 0.65 times 0.65 = 0.42 m, 8 times 1.35 =
    ///     10.8 m/s). The bench's current is 1.3 m wide and runs at 8 m/s, so for that variant it is sized to the rule's
    ///     width as a WHOLE (`Girth`: its cross section, head and splash together, never only one axis), and its clock is
    ///     run faster while it travels (`BenchClock`) so its head is where the column is at every moment.
    ///
    /// ⚠️ WHAT EVERY PEER KNOWS, AND SO ALL IT IS DRAWN FROM: the field's state (where it was cast, the way it runs, its
    /// radius, its speed, its life, and whether it has been spent) and the field's age. A slipper's touch or a wall
    /// spends it (`Split`), and the state carries THAT it was spent and not when, so each peer takes the age it first
    /// sees it spent. On the host that is the frame after the touch; on a client it is when the reliable update lands,
    /// a round trip later at most (8 m/s is about 0.1 m of extra run at a 25 ms trip); a player who joins after it was
    /// spent sees it let go as he arrives. It is a picture of a current that no longer decides anything either way.
    ///
    /// ⚠️ WHY IT IS AN OBJECT OF ITS OWN, AND OUTLIVES THE FIELD (as the wall does, `RafiWallCurtain`). The current lets
    /// go when the rule does (its 6 m run, or a spend) and only THEN drops, splashes and throws its drops, about a
    /// second. But the field destroys itself, and everything under it, at the end of its life. So in a match the stream
    /// is not under the field: it stands beside it, is posed by it while it lives, is told when it has gone (`Release`),
    /// and then runs its death on its own clock and removes itself. A field that goes early (the round ended) takes
    /// the stream with it at once, and so does a round that ends while it is dying.
    ///
    /// ⚠️ LOOK ONLY. No collider, no registry, nothing on the wire, nothing that decides anything.
    ///
    /// In a replay (`RecordedFieldView`) and in a film there is no field: the stream hangs under whoever built it and is
    /// only ever as old as it is posed, so a replay shows it run and be spent, and not the splash after a full run.
    /// </summary>
    public sealed class RafiCurrentStream : MonoBehaviour
    {
        /// <summary>A film or a replay has no main camera: it says where the lens is, and the face and drops turn to it.</summary>
        public static Vector3? StagedEye;
        /// <summary>⚠️ FOR A FILM ONLY, read when one is built: how much of it is light laid on (below 0 is the match's).</summary>
        public static float StagedLight = -1f;
        /// <summary>
        /// How long after the rule lets go of it the stream is still drawn. Not the bench's `Die` (0.70 s) alone: that is
        /// the body's drop, and the drops it throws are still in the air after it. The highest (thrown up at 3.9 m/s from
        /// 1.2 m) lands 1.03 s after the burst and leaves 0.01 s late, so this is that with a frame to spare.
        /// </summary>
        public const float TailSeconds = 1.10f;

        private FxStudyRafiCross _cross;
        private WorldEffectSnapshot.Field _state;
        private float _age, _spentAt = -1f;
        private bool _ownLife, _released;
        private int _round;
        private long _epoch;

        public static RafiCurrentStream Build(RafiWaterVisual owner, WorldEffectSnapshot.Field state)
        {
            var go = new GameObject("IlyasCrosscurrent");
            // Only a stream with a live field over it has to outlive anything.
            bool ownLife = Application.isPlaying && owner.GetComponentInParent<RafiWaterField>() != null;
            if (ownLife) go.transform.SetPositionAndRotation(owner.transform.position, owner.transform.rotation);
            else go.transform.SetParent(owner.transform, false);
            var stream = go.AddComponent<RafiCurrentStream>();
            stream.enabled = false;
            stream._ownLife = ownLife; stream._state = state;
            if (ownLife) { stream._round = GameServices.Match?.RoundNumber ?? 0; stream._epoch = MatchRpc.Instance?.PresentationMatchId ?? 0; }
            var look = FxStudyRafiWall.Look.Bench;
            look.Light = StagedLight >= 0f ? StagedLight : RafiWallCurtain.DayLight;
            stream._cross = new FxStudyRafiCross(go.transform, look, -1f, Mathf.Repeat(state.EventId * 0.37f, 1f), 0);
            // ⚠️ THE WIDTH OF WHAT HITS. The plain current's radius IS the bench's (both `RafiRules.CurrentRadius`), so this is
            // exactly 1 for it; only TIGHT CUT's narrower column makes it smaller. Never above 1: the field refuses a radius
            // over the rule's (`RafiWaterField.Valid`), and the approved width is the largest it is ever drawn.
            stream._cross.Girth = Mathf.Clamp(state.Radius / FxStudyRafiCross.Radius, 0.1f, 1f);
            // An effect, not a prop: the ground queries and the prop checks pass over it.
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
            return stream;
        }

        /// <summary>
        /// The bench's clock for the field's `seconds`. ⚠️ THE HEAD IS WHERE THE RULE'S COLUMN IS: the bench runs its head
        /// at `FxStudyRafiCross.Speed` (8 m/s) from the end of the gather, the rule at the field's own speed
        /// (`FirstScale`, 8 plain and 10.8 for TIGHT CUT). So while it travels the bench's clock runs at their ratio, and
        /// before the gather ends and after it lets go it runs at the field's own rate, so the gather and the death are
        /// the approved ones exactly. For the plain current the ratio is 1 and this is the field's age untouched.
        /// </summary>
        private float BenchClock(float seconds, float letGo)
        {
            float gather = FxStudyRafiCross.Gather;
            if (seconds <= gather) return seconds;
            float rate = _state.FirstScale > 0f ? _state.FirstScale / FxStudyRafiCross.Speed : 1f;
            if (seconds <= letGo) return gather + (seconds - gather) * rate;
            return gather + Mathf.Max(0f, letGo - gather) * rate + (seconds - letGo);
        }

        /// <summary>When the rule let go of it: its life, or the age it was first seen spent if that is sooner.</summary>
        private float LetGo => _spentAt >= 0f ? Mathf.Min(_spentAt, _state.Duration) : _state.Duration;

        /// <summary>
        /// Poses it for the field's age. Nothing is kept from the frame before but the age it was first seen spent, so a
        /// replay can scrub it and every peer draws the same frame. An age past the field's life is its death.
        /// </summary>
        public void Pose(float seconds, WorldEffectSnapshot.Field state)
        {
            if (_cross == null) return;
            _state = state; _age = seconds;
            if (state.Split && _spentAt < 0f) _spentAt = Mathf.Max(0f, seconds);
            Vector3 eye;
            if (StagedEye.HasValue) eye = StagedEye.Value;
            else { var lens = Camera.main; eye = lens != null ? lens.transform.position : transform.position + new Vector3(0f, 1.6f, -6f); }
            float letGo = LetGo;
            // The bench reads a spend at 0 or below as "never spent", so a spend in the first instant is kept above it.
            float spent = _spentAt >= 0f ? Mathf.Max(0.001f, BenchClock(letGo, letGo)) : -1f;
            _cross.Pose(BenchClock(seconds, letGo), spent, eye);
        }

        /// <summary>The field has gone, at this age. A stream whose field lived its whole life now dies by its own clock;
        /// one whose field went early goes with it.</summary>
        public void Release(float seconds, WorldEffectSnapshot.Field state)
        {
            if (!_ownLife || this == null) return;
            if (seconds < state.Duration - 0.05f) { Destroy(gameObject); return; }
            _state = state; _age = seconds; _released = true; enabled = true;
        }

        private void Update()
        {
            if (!_released) return;
            // The field's own test for a round that has moved on, so nothing of it is left standing in the next one.
            if ((GameServices.Round != null && !GameServices.Round.RoundActive)
                || (GameServices.Match?.RoundNumber ?? 0) != _round
                || (MatchRpc.Instance?.PresentationMatchId ?? 0) != _epoch)
            { Destroy(gameObject); return; }
            _age += Time.deltaTime;
            if (_age >= LetGo + TailSeconds) { Destroy(gameObject); return; }
            Pose(_age, _state);
        }

        /// <summary>Frees its renderers. Its meshes and materials are shared by every cast and are not freed.</summary>
        public void Free() { _cross?.Dispose(); _cross = null; }
        private void OnDestroy() => Free();
    }
}
