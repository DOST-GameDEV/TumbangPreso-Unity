#if UNITY_EDITOR
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// A body that runs after the player in the prototype map and never tires, for trying the movement rework against a
    /// chase. Owner, 2026-10-09: *"i want a bot with max stamina that chases me around the map. play a sound when he
    /// catches up"*. EDITOR ONLY, like `PrototypeMapPlay`, which spawns it on B.
    ///
    /// ⚠️ IT IS NOT ONE OF THE GAME'S BOTS. Those need a match, a role and a seat (`AIController`); this is a capsule
    /// that runs straight at the player at the taya's run speed, the speed an attacker has to beat, hops when
    /// something stops it, and sounds the game's tag when it is within reach. It tags nothing: no stun, no score.
    /// </summary>
    public sealed class PrototypeChaser : MonoBehaviour
    {
        /// <summary>The taya's run, held for ever: a bar that never empties.</summary>
        public float Speed = Balance.DefenderRunSpeed;
        /// <summary>`RestSeconds`: how long after a tag before he can tag again. He does not stand still for it.</summary>
        public float CatchRadius = 1.2f, RestSeconds = 1.0f;
        public Transform Target;
        public AnimationClip Run, Idle;
        public GameObject Model;
        public int Catches { get; private set; }
        public float Distance { get; private set; }
        /// <summary>Seconds the player has lasted in the chase now running, in the last one, the longest, and in all of them.</summary>
        public float Survived { get; private set; }
        public float LastSurvived { get; private set; }
        public float BestSurvived { get; private set; }
        public float TotalSurvived { get; private set; }
        public float AverageSurvived => Catches > 0 ? TotalSurvived / Catches : 0f;

        /// <summary>How late he is to where the player has gone, how fast he turns (the bots' own rate), how fast he gets up to speed.</summary>
        public float ReactionSeconds = 0.25f, TurnDegreesPerSecond = 520f, Acceleration = 30f;
        /// <summary>Off: he sprints for ever. On: the taya's real bar (`Core.Stamina`), sprint, fatigue, walk and all.</summary>
        public bool RealStamina;
        public bool Sprinting { get; private set; }

        private CharacterController _cc;
        private float _vertical, _rest, _stuck, _clock, _pace;
        private readonly Stamina _stamina = new Stamina();
        private readonly System.Collections.Generic.Queue<(float at, Vector3 spot)> _trail = new System.Collections.Generic.Queue<(float, Vector3)>();

        private void Awake()
        {
            _cc = gameObject.AddComponent<CharacterController>();
            // The game's own Person capsule (`MatchInstaller.BuildSeat`).
            _cc.height = 1.6f; _cc.radius = 0.35f; _cc.center = new Vector3(0f, 0.8f, 0f);
            _cc.slopeLimit = 45f; _cc.stepOffset = 0.3f;
        }

        private void Update()
        {
            if (Target == null) return;
            float dt = Time.deltaTime;
            Vector3 to = Target.position - transform.position;
            float height = to.y; to.y = 0f;
            Distance = to.magnitude;

            // ⚠️ HE DOES NOT STOP AFTER A TAG (owner, 2026-10-09: "make the chaser not stop moving after each tag"). He stood
            // for `RestSeconds` after each one; now he keeps coming, and that time is only how long before he can tag
            // again, so standing in his arms is one tag every `RestSeconds` and not one every frame. The clock for
            // the next chase starts at the tag.
            bool resting = false;
            if (_rest > 0f) _rest -= dt;
            Survived += dt;
            if (_rest <= 0f && Distance < CatchRadius && Mathf.Abs(height) < 1.6f)
            {
                LastSurvived = Survived; TotalSurvived += Survived; Survived = 0f;
                if (LastSurvived > BestSurvived) BestSurvived = LastSurvived;
                Catches++;
                _rest = RestSeconds;
                NetCue.PlayVaried("tag", transform.position, 0.95f, 1.05f, 1.0f);
            }

            // ⚠️ HE IS HELD TO WHAT A PLAYER IS HELD TO (owner, 2026-10-09, of a chaser who turned on the spot, was at full
            // speed from his first step and knew where the player was this very frame: "im still never able to get away
            // from the taya, even with perfect strafing"). He now chases where the player WAS a reaction ago, turns at
            // the rate the game's own bots turn, runs the way he is FACING (so a cut inside his turn gets away from
            // him), and builds and loses speed. And with `RealStamina` he has the taya's real bar: two and a half
            // seconds of sprint, then the walk and the wait every taya has.
            _trail.Enqueue((Time.time, Target.position));
            while (_trail.Count > 1 && Time.time - _trail.Peek().at > ReactionSeconds) _trail.Dequeue();
            Vector3 aim = _trail.Peek().spot - transform.position; aim.y = 0f;

            float top = Speed;
            if (RealStamina)
            {
                _stamina.StepFatigue(dt);
                _stamina.Step(dt, !resting, !resting);
                top = Stamina.MovementSpeed(true, _stamina.IsSprinting);
            }
            Sprinting = !RealStamina || _stamina.IsSprinting;

            Vector3 step = Vector3.zero;
            if (!resting && aim.sqrMagnitude > 0.0025f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(aim.normalized), TurnDegreesPerSecond * dt);
                _pace = Mathf.MoveTowards(_pace, top, Acceleration * dt);
            }
            else _pace = Mathf.MoveTowards(_pace, 0f, Acceleration * 2f * dt);
            step = transform.forward * _pace;

            if (_cc.isGrounded && _vertical <= 0f)
            {
                _vertical = -2f;
                // Something is in the way: hop it.
                if (_stuck > 0.25f) { _vertical = Balance.JumpVelocity; _stuck = 0f; }
            }
            else _vertical = Mathf.Max(-Balance.MaxFallSpeed, _vertical - Balance.CharacterGravity * dt);

            Vector3 before = transform.position;
            _cc.Move((step + Vector3.up * _vertical) * dt);
            Vector3 moved = transform.position - before; moved.y = 0f;
            _stuck = !resting && _pace > 1f && moved.magnitude < _pace * dt * 0.3f ? _stuck + dt : 0f;

            // Fell out of the map: back above the player's side of it.
            if (transform.position.y < -10f)
            {
                _cc.enabled = false; transform.position = Target.position + Vector3.up * 2f - Target.forward * 6f; _cc.enabled = true;
                _vertical = 0f;
            }

            var clip = resting || _pace < 0.3f ? Idle : Run;
            if (clip != null && Model != null)
            {
                _clock += dt;
                clip.SampleAnimation(Model, Mathf.Repeat(_clock, Mathf.Max(0.05f, clip.length)));
            }
        }
    }
}
#endif
