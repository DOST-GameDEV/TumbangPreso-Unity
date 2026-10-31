using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ FEATHERFALL, REVAMPED, BEHIND <see cref="Enabled"/> (on). False draws exactly what a match drew before: the
    /// rising streaks of `AmihanUpdraftLaunch` and the streak column, gust rings and dust of `AmihanHoverRing`.
    ///
    /// What the picture has to tell the truth about (`AmihanRules`): she alone is thrown 2.8 m up in 0.45 s, is held
    /// there for what is left of 5 s while she moves and throws, out of every taya's reach, and comes down at 3.5 m a
    /// second when she presses again, grabs, or the time runs out. Nobody else is touched. So a bystander and a taya
    /// under her must read three things: the air under her is what holds her, how long it will go on holding her, and
    /// that she is on her way down. She must read the time left too, looking down, and nothing may cross her own eyes.
    ///
    /// ⚠️ THE PICTURE IS THE BENCH'S OWN SAMPLE, CALLED AND NOT COPIED (`FxStudyFeatherfall`, `docs/VFX_BENCH_METHOD.md`
    /// section 4): the whirlwind's noise horn upside down as the column under her soles, a flat ring of trail ribbon under
    /// her that closes as the seconds go and a column whose foot lets go of the court as they go (the time left, read from
    /// under her and from across the court), two trail tapes from her soles to her waist, the gust and the wisp's dish
    /// streaks at take-off and landing, and the match's cloud puffs (`CloudPuffs`). All in the whirlwind's colours, hue
    /// wandering green to blue. Every piece was made for a body of her size, so nothing is stretched past its job.
    ///
    /// The one setting that differs from the bench is how much of it is light laid on: the court whirlwind's
    /// `AmihanWhirlwind.DayLight`, which is what held that colour on the sunlit road with no bloom.
    ///
    /// ⚠️ THE OLD LOOK STILL RUNS UNDER IT, UNDRAWN. `AmihanHoverRing` also answers her pose (`Push`) and plays the gust
    /// and settle sounds, so it is kept alive and only its renderers are switched off (`forceRenderingOff`); the launch's
    /// streaks likewise. Nothing on the wire, nothing decided here: it reads her replicated flight state on every peer.
    /// </summary>
    public sealed class AmihanFeatherfallLook : MonoBehaviour
    {
        public static bool Enabled = true;
        /// <summary>
        /// ⚠️ HER OWN BRUSH (`AmihanFeatherfallPainted`), on. The owner, of the sample this replaces (still drawn when this is
        /// false): "the trail suddenly shortens. the vfx itself looks pretty low effort". Its updraft let go of the court as
        /// the seconds went, which read as the trail cut short, and every piece of it was the whirlwind's.
        /// </summary>
        public static bool Painted = false;
        /// <summary>How much of every piece is light laid on: the court whirlwind's daylight share.</summary>
        public const float Light = AmihanWhirlwind.DayLight;

        private static readonly List<AmihanFeatherfallLook> Live = new List<AmihanFeatherfallLook>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Live.Clear();

        private CharacterMotor _body;
        private Abilities.HeroKit _kit;
        private int _epoch;
        private FxStudyFeatherfall _fx;
        private AmihanFeatherfallPainted _painted;
        // Where her soles have been, for the painted trail and the updraft's foot: (age, x, y, z), a ring of the last 1.5 s.
        private readonly Vector4[] _path = new Vector4[120];
        private int _pathHead, _pathCount;
        private float _releaseAge = 1e4f, _releaseGap;
        private Vector3 _releaseGround;
        private float _age, _sinceLaunch = -1f, _sinceLand = -1f;
        private Vector3 _launchGround, _landGround;
        private bool _flew;
        private readonly List<AmihanHoverRing> _rings = new List<AmihanHoverRing>();
        private readonly List<Renderer> _renderers = new List<Renderer>();

        /// <summary>Called on every peer whenever she is put in the air (`AmihanFlightPose.Attach`). A second call for the
        /// same flight keeps the one already drawn.</summary>
        public static AmihanFeatherfallLook Attach(CharacterMotor body)
        {
            if (!Enabled || body == null) return null;
            var kit = body.AbilitySystem?.Kit;
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                var existing = Live[i];
                if (existing == null || existing._body != body) continue;
                if (existing._sinceLand < 0f && existing._kit == kit && existing._epoch == body.MovementEpoch) return existing;
                // A flight that is over is left to finish its landing; a stale one goes now.
                if (existing._sinceLand < 0f) Destroy(existing.gameObject);
            }
            var go = new GameObject("AmihanFeatherfallLook") { hideFlags = HideFlags.DontSave };
            var look = go.AddComponent<AmihanFeatherfallLook>();
            look._body = body; look._kit = kit; look._epoch = body.MovementEpoch;
            if (Painted) look._painted = new AmihanFeatherfallPainted(go.transform);
            else look._fx = new FxStudyFeatherfall(go.transform, Light, false);
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);

            Vector3 feet = body.transform.position;
            Vector3 ground = new Vector3(feet.x, VfxShapes.GroundAt(feet, feet.y - 4f, 5f), feet.z);
            go.transform.position = ground;
            // Still on the court: this is the take-off, so the gust and the puff go now. Already up (a late joiner's
            // restore): the take-off happened before this peer saw her, so it is not drawn again.
            if (feet.y - ground.y < 0.4f)
            {
                look._sinceLaunch = 0f; look._launchGround = ground;
                if (!Painted) CloudPuffs.Spawn(FxStudyFeatherfall.TakeOffPuff(ground));
            }
            else { look._sinceLaunch = 10f; look._launchGround = ground; }
            HideLaunches(ground);
            look.Step(0f);
            Live.Add(look);
            return look;
        }

        private void LateUpdate() => Step(Time.deltaTime);

        private void Step(float dt)
        {
            if (_body == null || (_fx == null && _painted == null)) { Destroy(gameObject); return; }
            _age += dt;
            if (_sinceLaunch >= 0f) _sinceLaunch += dt;
            if (_sinceLand >= 0f) _sinceLand += dt;
            HideOld();

            Vector3 feet = _body.transform.position;
            float court = VfxShapes.GroundAt(feet, feet.y - 4f, 5f);
            float gap = Mathf.Max(0f, feet.y - court);
            var ground = new Vector3(feet.x, court, feet.z);
            if (_body.IsFlying) _flew = true;

            // Over: she is down, or the flight was taken from her (the kit's own cancel conditions), or it never began.
            bool cancelled = _body.AbilitySystem?.Kit != _kit || _body.MovementEpoch != _epoch || _body.IsTagged || _body.IsRooted
                || _body.IsTripped || _body.IsSwimming || _body.IsEdgeRecovering;
            if (_sinceLand < 0f && (cancelled || (_flew && !_body.IsFlying) || (!_flew && _age > 1f)))
            {
                _sinceLand = 0f; _landGround = ground;
                if (!cancelled && _flew && _fx != null) CloudPuffs.Spawn(FxStudyFeatherfall.LandingPuff(ground));
            }
            bool holding = _sinceLand < 0f && _body.IsAloft;
            if (_painted != null)
            {
                StepPainted(feet, ground, gap, holding);
                return;
            }
            if (_sinceLand > FxStudyFeatherfall.LandLife + 0.05f) { Destroy(gameObject); return; }

            transform.position = ground;
            _fx.LaunchAt(_launchGround);
            _fx.LandAt(_sinceLand >= 0f ? _landGround : ground);
            _fx.Pose(_age, _sinceLand >= 0f ? 0f : gap, holding ? TimeLeft() : -1f, _sinceLaunch, _sinceLand);
        }

        // ⚠️ THE PAINTED LOOK IS FED HISTORY, NOT A SNAPSHOT. The moment the wind lets her go (the hold ends, or the flight is
        // taken from her) is kept, with where she was, so the feathers are let go from where they were; and where her soles
        // have been is kept for the trail, which is drawn from that and never from a guess.
        private void StepPainted(Vector3 feet, Vector3 ground, float gap, bool holding)
        {
            if (_pathCount == 0 || _age > _path[(_pathHead + _path.Length - 1) % _path.Length].x + 1e-4f)
            {
                _path[_pathHead] = new Vector4(_age, feet.x, feet.y, feet.z);
                _pathHead = (_pathHead + 1) % _path.Length;
                _pathCount = Mathf.Min(_pathCount + 1, _path.Length);
            }
            if (!holding && _releaseAge > 1e3f && _flew) { _releaseAge = _age; _releaseGap = gap; _releaseGround = ground; }
            if (_sinceLand >= 0f && _releaseAge > 1e3f) { _releaseAge = _age; _releaseGap = gap; _releaseGround = ground; }
            if (AmihanFeatherfallPainted.Over(_age - Mathf.Max(0f, LaunchAge), _releaseAge - Mathf.Max(0f, LaunchAge), _sinceLand)) { Destroy(gameObject); return; }
            transform.position = Vector3.zero;
            float launchAge = Mathf.Max(0f, LaunchAge);
            _painted.Pose(new AmihanFeatherfallPainted.Moment
            {
                Age = _age - launchAge, Gap = _sinceLand >= 0f ? 0f : gap, Ground = ground, Left = holding ? TimeLeft() : -1f,
                ReleaseAge = _releaseAge > 1e3f ? 1e4f : _releaseAge - launchAge, ReleaseGap = _releaseGap, ReleaseGround = _releaseGround,
                SinceLand = _sinceLand, LandGround = _sinceLand >= 0f ? _landGround : ground, LaunchGround = _launchGround,
                Feet = t => FeetAt(t + launchAge),
            });
        }

        // This look's age when she took off (a late joiner's restore never saw it, so its own start stands in).
        private float LaunchAge => _sinceLaunch >= 0f && _sinceLaunch < 9f ? _age - _sinceLaunch : 0f;

        // Where her soles were at an age of this look: read between the kept points; before the oldest, the oldest.
        private Vector3 FeetAt(float age)
        {
            if (_pathCount == 0) return _body != null ? _body.transform.position : Vector3.zero;
            int newest = (_pathHead + _path.Length - 1) % _path.Length;
            Vector4 later = _path[newest];
            if (age >= later.x) return new Vector3(later.y, later.z, later.w);
            for (int k = 1; k < _pathCount; k++)
            {
                Vector4 earlier = _path[(newest - k + _path.Length) % _path.Length];
                if (age >= earlier.x)
                {
                    float u = Mathf.InverseLerp(earlier.x, later.x, age);
                    return Vector3.Lerp(new Vector3(earlier.y, earlier.z, earlier.w), new Vector3(later.y, later.z, later.w), u);
                }
                later = earlier;
            }
            return new Vector3(later.y, later.z, later.w);
        }

        /// <summary>The share of her 5 s still to come: the ability's own clock where this peer has it (every peer restores
        /// it, `AmihanHeroKit.RestoreFeatherfall`), else this look's own since the take-off.</summary>
        private float TimeLeft()
        {
            var skill = (_kit as Abilities.AmihanHeroKit)?.AttackingSkill;
            if (skill != null && skill.IsActive) return Mathf.Clamp01(skill.DurationRemaining / AmihanRules.UpdraftSeconds);
            return _sinceLaunch >= 0f && _sinceLaunch < 9f ? Mathf.Clamp01(1f - _sinceLaunch / AmihanRules.UpdraftSeconds) : 0.5f;
        }

        // The old support stays alive for its sounds and the lift it hands her pose; only what it draws is switched off.
        private void HideOld()
        {
            _body.GetComponentsInChildren(true, _rings);
            foreach (var ring in _rings)
            {
                ring.GetComponentsInChildren(true, _renderers);
                foreach (var r in _renderers) if (!r.forceRenderingOff) r.forceRenderingOff = true;
            }
        }

        private static readonly List<Renderer> Scratch = new List<Renderer>();
        // The take-off's old streaks are built just before this look (`Updraft.OnActivate`), where she stood.
        private static void HideLaunches(Vector3 ground)
        {
            foreach (var launch in Object.FindObjectsByType<AmihanUpdraftLaunch>())
            {
                if (launch == null) continue;
                Vector3 d = launch.transform.position - ground; d.y = 0f;
                if (d.sqrMagnitude > 2.25f) continue;
                launch.GetComponentsInChildren(true, Scratch);
                foreach (var r in Scratch) r.forceRenderingOff = true;
            }
        }

        private void OnDestroy()
        {
            _fx?.Dispose();
            _fx = null;
            _painted?.Dispose();
            _painted = null;
            Live.Remove(this);
        }
    }
}
