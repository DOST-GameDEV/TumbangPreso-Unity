using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CURSE: DRAIN AND HER PASSIVE IN A MATCH, BEHIND `PhaisterLook.Direction` (off while it is `Current`). Body-owned like
    /// `VoodooCursePresenter`, which adds it, and read only off the body's REPLICATED reach, mark and status
    /// (`CharacterMotor.Voodoo.cs`), so the caster, the victim, every observer and a rejoiner draw the same thing, nothing is sent,
    /// and no rule, timing or wire format moves.
    ///
    /// The drain look lives on the caster's body from the moment her reach starts and walks one timeline
    /// (`PhaisterDrainLook`): the reach's elapsed time, then the mark's age, then the Drained status's time so far. The clock
    /// only ever goes forward, so a late snapshot cannot rewind the moths.
    /// </summary>
    public sealed class PhaisterDrainPresenter : MonoBehaviour
    {
        /// <summary>
        /// The crown above the feet (m) when a body has no drawn model to measure, which is a guess for a preview only. A real
        /// body is measured from its own renderers when the reach starts (`CrownOf`), because the cast differ in height.
        /// </summary>
        public const float FallbackCrown = 2.02f;

        /// <summary>The top of what is drawn for this body, metres above its feet.</summary>
        public static float CrownOf(Transform body)
        {
            float top = float.MinValue;
            if (body != null)
                foreach (var r in body.GetComponentsInChildren<Renderer>())
                    if ((r is SkinnedMeshRenderer || r is MeshRenderer) && r.GetComponentInParent<CharacterVisual>() != null) top = Mathf.Max(top, r.bounds.max.y);
            float above = top - (body != null ? body.position.y : 0f);
            return above > 0.8f && above < 3.5f ? above : FallbackCrown;
        }

        /// <summary>What this peer knows of a drain this frame: all of it replicated.</summary>
        public struct Known
        {
            public bool Reaching, ReachSucceeded, Marked, Drained;
            public float ReachElapsed, MarkAge, DrainedLeft;
        }

        /// <summary>The look's timeline, kept between frames.</summary>
        public struct Timeline
        {
            public float T, BrokeAt, NoMark;
            public bool Reached;
            public static Timeline Start => new Timeline { BrokeAt = -1f };

            /// <summary>
            /// One frame of the clock. It only goes forward, so a late snapshot cannot rewind the moths. A pure function of what
            /// it is handed, so the kit film can run the same code through every ending without a match.
            /// </summary>
            public void Step(in Known k, float dt)
            {
                float next = T + dt;
                if (k.Reaching) next = Mathf.Max(T, k.ReachElapsed);
                else if (BrokeAt < 0f)
                {
                    if (k.Marked) { Reached = true; NoMark = 0f; next = Mathf.Max(T, PhaisterDrainLook.ReachEnd + k.MarkAge); }
                    else if (k.Drained) { Reached = true; next = Mathf.Max(T, PhaisterDrainLook.SnapAt + StatusRules.DrainedSeconds - k.DrainedLeft); }
                    else if (T < PhaisterDrainLook.SnapAt)
                    {
                        // No mark and no status yet: a snapshot on its way, a reach that snapped, or a cleanse.
                        NoMark += dt;
                        if ((!k.ReachSucceeded && !Reached) || NoMark >= MarkGrace) BrokeAt = T;
                    }
                }
                T = next;
            }
            public bool Over => T >= PhaisterDrainLook.End || (BrokeAt >= 0f && T > BrokeAt + 1.0f);
        }

        /// <summary>How long a mark may be missing after the reach succeeded before this peer believes it was cleansed.</summary>
        private const float MarkGrace = 0.4f;

        private CharacterMotor _body;
        private PhaisterDrainLook _drain;
        private CharacterMotor _victim;
        private bool _running;
        private Timeline _clock;
        private ViewmodelArms _arms;
        private float _armsSearch;
        private float _crown, _markAge;
        private Vector3 _lastAt, _moving, _herAt;

        /// <summary>True while this look draws her DRAIN reach, in place of `VoodooSoulDraw`'s ghost and motes.</summary>
        public static bool DrawsReach(CharacterMotor caster)
            => PhaisterLook.Direction != PhaisterLook.Style.Current && caster.IsVoodooReaching && caster.VoodooReachKind == VoodooMarkKind.Drain;

        /// <summary>True while this look draws a DRAIN mark, in place of `VoodooCursePresenter`'s knot.</summary>
        public static bool DrawsMark => PhaisterLook.Direction != PhaisterLook.Style.Current;

        private void Awake() { _body = GetComponent<CharacterMotor>(); _lastAt = transform.position; }

        private void LateUpdate()
        {
            if (_body == null) return;
            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                Vector3 moved = (transform.position - _lastAt) / dt;
                _moving = Vector3.Lerp(_moving, moved, Mathf.Clamp01(dt * 12f));
                _lastAt = transform.position;
            }
            bool on = PhaisterLook.Direction != PhaisterLook.Style.Current && _body.isActiveAndEnabled;
            if (!on) { _running = false; _drain?.Hide(); return; }
            StepDrain(dt);
        }

        private void StepDrain(float dt)
        {
            bool reaching = DrawsReach(_body);
            if (reaching && !_running)
            {
                _running = true; _clock = Timeline.Start; _arms = null; _armsSearch = 0f;
                _victim = GameServices.Round?.BodyAt(_body.VoodooReachTarget);
                if (_drain == null) _drain = new PhaisterDrainLook(null);
                _crown = CrownOf(_victim != null ? _victim.transform : null);
            }
            if (!_running) return;
            if (_victim == null) { _running = false; _drain.Hide(); return; }

            bool mine = _victim.VoodooMark == VoodooMarkKind.Drain && _victim.VoodooMarkSource == _body.PlayerSlot;
            _clock.Step(new Known
            {
                Reaching = reaching, ReachElapsed = _body.VoodooReachElapsed, ReachSucceeded = _body.VoodooReachSucceeded,
                Marked = mine, MarkAge = _victim.VoodooMarkAge, Drained = _victim.IsDrained, DrainedLeft = _victim.DrainedLeft,
            }, dt);

            bool ownEyes = OwnEyes(dt);
            Vector3 head = _victim.transform.position + Vector3.up * _crown;
            Vector3 flat = _victim.transform.position - transform.position; flat.y = 0f;
            Vector3 hand = transform.position + Vector3.up * 1.15f + (flat.sqrMagnitude > 0.01f ? flat.normalized : transform.forward) * 0.38f;
            var view = Camera.main;
            Vector3 eye = view != null ? view.transform.position : head + Vector3.back * 6f;
            _drain.Pose(_clock.T, _clock.BrokeAt, head, hand, eye, ownEyes);
            if (_clock.Over) { _running = false; _drain.Hide(); }
        }

        /// <summary>
        /// Whether the victim is looking through their own eyes on this screen. The arms are cached and checked by reference,
        /// and only looked for again a couple of times a second while nobody holds them, because the search allocates.
        /// </summary>
        private bool OwnEyes(float dt)
        {
            if (_arms != null && _arms.isActiveAndEnabled && _arms.BoundCharacter == _victim) return true;
            _arms = null;
            _armsSearch -= dt;
            if (_armsSearch > 0f) return false;
            _armsSearch = 0.5f;
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))
                if (arms != null && arms.isActiveAndEnabled && arms.BoundCharacter == _victim) { _arms = arms; return true; }
            return false;
        }

        private void OnDisable() { _running = false; _drain?.Hide(); }

        private void OnDestroy() { _drain?.Free(); }
    }
}
