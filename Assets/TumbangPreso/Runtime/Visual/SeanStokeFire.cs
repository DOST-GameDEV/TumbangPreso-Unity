using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STOKE STEP'S NEW LOOK IN A MATCH: the effects study's `FxStudySeanStoke` (the fireball wake, c2) set down where Rago
    /// pushes off and driven by his real body, the way `ZackCircuitFlash` drives Quick Circuit.
    ///
    /// IT IS A LOOK ONLY. The dash, its distance, its timing and who may cast it are `SeanHeroKit.StokeStepAbility`'s and
    /// know nothing of this; nothing here is sent anywhere. Every peer that runs that ability's `Activate` (the press) and
    /// `OnActivate` (the launch, 0.18 s later) draws this from those two calls:
    ///   * `Brace` at the press: the heat and cracks under his heel, the embers off his fists;
    ///   * `Burst` at the launch: the fireball and everything after it.
    /// ⚠️ THE WAKE'S FRONT IS HIS BODY. Each frame it reads how far he really is along the dash (his transform, which every
    /// peer has), never lets that go back, and when he has not moved on for a fifteenth of a second it is over there: a dash
    /// stopped short by a wall is a short wake. A brace whose launch never comes (he was stunned) simply ends.
    ///
    /// ⚠️ POOLED: one effect per Rago, built at his first cast and reused for every cast after, so a cast makes nothing.
    /// </summary>
    public sealed class SeanStokeFire : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ THE ONE SWITCH. On, STOKE STEP is drawn as above and the shared aura is not attached. Off, it is drawn as before
        /// 2026-10-17: the shared fire aura on his body (`AbilityVfx.Aura.FireEmber`), untouched. ON IN THIS WORKTREE for the
        /// owner's Play test after the court film d2 (2026-10-18). TO GO BACK, MAKE THE LINE BELOW `false`.
        /// </summary>
        public static bool Streak = true;

        private static readonly Dictionary<CharacterMotor, SeanStokeFire> Pool = new Dictionary<CharacterMotor, SeanStokeFire>();

        private FxStudySeanStoke _fx;
        private CharacterMotor _who;
        private Vector3 _from, _dir;
        private float _age, _front, _movedAt, _stoppedAt = -1f;
        private bool _launched, _live;

        /// <summary>The press: the brace begins where he stands, facing his aim.</summary>
        public static void Brace(AbilityContext ctx)
        {
            if (!Streak || !Application.isPlaying || ctx?.Motor == null) return;
            Vector3 dir = ctx.Forward; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            var fire = For(ctx.Motor);
            if (fire != null) fire.Begin(dir.normalized);
        }

        /// <summary>The launch, along `dir` (the same heading the ability throws him along).</summary>
        public static void Burst(CharacterMotor who, Vector3 dir)
        {
            if (!Streak || !Application.isPlaying || who == null) return;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            var fire = For(who);
            if (fire == null) return;
            // A launch with no brace before it (a peer that joined inside the 0.18 s) starts the clock at the launch.
            if (!fire._live) fire.Begin(dir.normalized, FxStudySeanStoke.Brace);
            fire._launched = true; fire._movedAt = fire._age;
        }

        private static SeanStokeFire For(CharacterMotor who)
        {
            if (Pool.TryGetValue(who, out var fire) && fire != null) return fire;
            var go = new GameObject("SeanStokeFire");
            fire = go.AddComponent<SeanStokeFire>();
            fire._who = who;
            // Half of it covers, as the approved whirlwind's does in daylight: light alone goes pale on a sunlit road.
            fire._fx = new FxStudySeanStoke(go.transform, AmihanWhirlwind.DayLight);
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
            go.SetActive(false);
            Pool[who] = fire;
            return fire;
        }

        private void Begin(Vector3 dir, float at = 0f)
        {
            _dir = dir;
            _from = VfxShapes.GroundPoint(_who.transform.position);
            transform.SetPositionAndRotation(_from, Quaternion.LookRotation(dir, Vector3.up));
            _age = at; _front = 0f; _movedAt = at; _stoppedAt = -1f; _launched = at > 0f; _live = true;
            _fx.Restart();
            gameObject.SetActive(true);
            Pose();
        }

        private void Update()
        {
            if (!_live) return;
            if (_who == null) { End(); return; }
            _age += Time.deltaTime;
            // A brace whose launch has not come a quarter of a second late was a cast that was stopped.
            if (!_launched && _age > FxStudySeanStoke.Brace + 0.25f) { End(); return; }
            Pose();
            if (_age > FxStudySeanStoke.Life + 0.1f) End();
        }

        private void Pose()
        {
            float since = _age - FxStudySeanStoke.Brace;
            if (_launched && since >= 0f)
            {
                float now = Vector3.Dot(_who.transform.position - _from, _dir);
                if (now > _front + 0.004f) { _front = now; _movedAt = _age; }
                else if (_stoppedAt < 0f && since > 0.06f && _age - _movedAt > 0.07f) _stoppedAt = _movedAt;
            }
            // The rule stops him `Travel` after the launch; only a stop well before that (a wall, a body) moves the ending.
            float stopped = since - FxStudySeanStoke.Travel;
            if (_stoppedAt >= 0f) stopped = Mathf.Max(stopped, _age - _stoppedAt);
            var lens = Camera.main;
            Vector3 eye = lens != null ? lens.transform.position : transform.position + new Vector3(0f, 1.6f, -6f);
            _fx.Pose(_age, eye, Mathf.Max(0f, _front), stopped);
        }

        private void End() { _live = false; gameObject.SetActive(false); }

        private void OnDestroy()
        {
            if (_who != null && Pool.TryGetValue(_who, out var mine) && mine == this) Pool.Remove(_who);
            _fx?.Release(); _fx = null;
        }
    }
}
