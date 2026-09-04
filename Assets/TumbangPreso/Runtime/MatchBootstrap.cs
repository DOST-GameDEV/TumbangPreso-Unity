using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Builds and runs a four-seat match: spawns the seats, places the lata and the slippers,
    /// rotates the taya every round, and resets the world at each boundary.
    ///
    /// ⚠️ SPAWNS ARE COMPUTED FROM THE BOX, NEVER READ FROM MAP MARKERS. "Outside the box" is
    /// the rule, and a marker that drifted half a metre inside the radius would spawn an
    /// Attacker VULNERABLE on frame one. That reads as a rules bug and gets debugged as one
    /// when it is a map bug, so the rule is the only thing allowed to decide it.
    /// </summary>
    public sealed class MatchBootstrap : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private CharacterMotor _playerPrefab;
        [SerializeField] private Lata _lataPrefab;
        [SerializeField] private Slipper _slipperPrefab;

        [Header("Seats")]
        [Tooltip("Which seats are human. Every other seat gets an AIController.")]
        [SerializeField] private bool[] _humanSeats = new bool[Balance.PlayerCount];

        [SerializeField] private bool _autoStart = true;

        private readonly CharacterMotor[] _seats = new CharacterMotor[Balance.PlayerCount];
        private readonly Slipper[] _slippers = new Slipper[Balance.PlayerCount];
        private Lata _lata;

        private void Start()
        {
            if (!_autoStart) return;
            BuildAndStart();
        }

        public void BuildAndStart()
        {
            if (_playerPrefab == null || _lataPrefab == null || _slipperPrefab == null)
            {
                Debug.LogError("[MatchBootstrap] prefabs are not assigned; nothing to build.");
                return;
            }

            _lata = Instantiate(_lataPrefab, Vector3.zero, Quaternion.identity);
            GameServices.Round.Lata = _lata;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var m = Instantiate(_playerPrefab);
                m.PlayerSlot = slot;
                m.CharacterIndex = slot; // a real build takes this from the character screen
                m.name = $"Seat{slot}";

                bool human = _humanSeats != null && slot < _humanSeats.Length && _humanSeats[slot];
                if (!human && m.GetComponent<AIController>() == null)
                    m.gameObject.AddComponent<AIController>();

                _seats[slot] = m;
                GameServices.Round.Register(m);

                var s = Instantiate(_slipperPrefab);
                s.OwnerSlot = slot;
                s.SkinIndex = slot;
                _slippers[slot] = s;
            }

            Subscribe(GameServices.Match);

            GameServices.Match.StartMatch();
        }

        /// <summary>
        /// The director this instance is hooked to, or null.
        ///
        /// ⚠️⚠️ THE OBJECT IS CACHED RATHER THAN RE-ASKED, WHICH IS THE ONLY WAY AN UNSUBSCRIBE
        /// CAN BE CORRECT. `GameServices.Match` is a property, and letting go by writing
        /// `GameServices.Match.RoundStarted -= OnRoundStarted` unsubscribes from **whichever
        /// director is current at teardown time**, which is not necessarily the one that was
        /// current when this subscribed. `AIController` already does it this way through
        /// `_hookedMatch` and its comment says so; this file did not.
        /// </summary>
        private MatchDirector _hookedMatch;

        /// <summary>
        /// ⚠️⚠️ THIS COMPONENT LIVES IN THE ARENA SCENE AND THE DIRECTOR IT SUBSCRIBES TO DOES
        /// NOT. `GameServices` is `DontDestroyOnLoad` (its own header: "a scene at build index 0
        /// holding the managers"), so `MatchDirector` outlives every arena. Until 2026-09-04 this
        /// file added four handlers and removed none, which cost two different things:
        ///
        ///  1. **Across matches.** The arena unloads, this component is destroyed, and the four
        ///     handlers stay registered on the surviving director. The next match therefore runs
        ///     `OnRoundStarted` on a DESTROYED `MatchBootstrap`, and `OnRoundStarted` calls
        ///     `ResetWorld`, which teleports all four bodies and hands out the tsinelas. Match
        ///     five was running it five times. This is `docs/TODO.md` § 126.8's leak class as the
        ///     PLAYER meets it rather than as a test suite does, and it does not crash: it is a
        ///     round that resets more than once, which reads as "the game got weird".
        ///  2. **Within one match.** `BuildAndStart` is public and the guard below is why that is
        ///     safe. A second call subscribed a second copy of every handler to the same event on
        ///     the same object.
        ///
        /// ⚠️ IT IS IDEMPOTENT IN BOTH DIRECTIONS. Subscribing releases the previous hook first,
        /// so no path can leave two, and `Unsubscribe` on a null hook is a no-op.
        /// </summary>
        private void Subscribe(MatchDirector match)
        {
            Unsubscribe();
            if (match == null) return;

            _hookedMatch = match;
            _hookedMatch.RoundStarted += OnRoundStarted;
            _hookedMatch.IntermissionStarted += OnIntermission;
            _hookedMatch.BufferSkipRequested += OnBufferSkipped;
            _hookedMatch.MatchEnded += OnMatchEnded;
        }

        private void Unsubscribe()
        {
            if (_hookedMatch == null) return;

            _hookedMatch.RoundStarted -= OnRoundStarted;
            _hookedMatch.IntermissionStarted -= OnIntermission;
            _hookedMatch.BufferSkipRequested -= OnBufferSkipped;
            _hookedMatch.MatchEnded -= OnMatchEnded;
            _hookedMatch = null;
        }

        /// <summary>
        /// ⚠️ THE PENDING `Invoke` GOES TOO, AND IT IS A SECOND LEAK IN THE SAME SHAPE.
        /// `OnIntermission` schedules `AdvanceAfterIntermission` on this component, and an
        /// `Invoke` outliving the object it targets calls `GameServices.Match.AdvanceRound()`
        /// on a director that has moved on to another match. A round advanced by the previous
        /// match's timer is `VISION.md` § 4's first rule broken from the outside.
        /// </summary>
        private void OnDestroy()
        {
            CancelInvoke();
            Unsubscribe();
        }

        private void OnRoundStarted(int roundNumber, int defenderSlot)
        {
            ResetWorld(defenderSlot);
            GameServices.Round.BeginRound();
        }

        private void OnIntermission(int nextRound, int nextDefenderSlot)
        {
            // ⚠️ SCORES PERSIST. Only the role rotates, and there is no per-round winner.
            Invoke(nameof(AdvanceAfterIntermission), Balance.IntermissionDuration);
        }

        private void AdvanceAfterIntermission() => GameServices.Match.AdvanceRound();

        /// <summary>
        /// ⚠️ THE PENDING `Invoke` IS CANCELLED FIRST, or the original timer still fires later
        /// and advances a second round nobody played. See `MatchDirector.SkipBuffer`.
        /// </summary>
        private void OnBufferSkipped()
        {
            CancelInvoke(nameof(AdvanceAfterIntermission));
            AdvanceAfterIntermission();
        }

        private void OnMatchEnded(int winningSlot)
        {
            GameServices.Round.EndRound();

            // ⚠️⚠️ THE CAST FREEZES WHEN THE MATCH IS WON, AND THIS WAS NOT PORTED.
            // `main.gd::_on_match_won_freeze_physics` zeroes every character's velocity the
            // moment the match ends, so the last frame of play is the one the result screen sits
            // over. Without it the winner and three bots carry on walking, throwing and chasing
            // underneath the result panel, which reads as the game having failed to notice it was
            // over. Ending the round is not the same thing: that stops the round rules, not the
            // bodies.
            foreach (var m in _seats)
            {
                if (m == null) continue;
                m.FreezeForMatchEnd();
            }
            Debug.Log(winningSlot < 0
                ? "[Match] draw at the top, reported honestly as -1."
                : $"[Match] seat {winningSlot} wins with {GameServices.Match.ScoreFor(winningSlot)}.");
        }

        /// <summary>
        /// ⚠️ ROLE ROTATION IS EXACTLY WHAT TRIGGERS THE SPAWN-SETTLE BUG, because two players
        /// trade marks and each stands on the other's stale collider for a frame. Every seat
        /// gets BeginSpawnSettle here, deliberately, and it is not an optimisation to remove.
        /// </summary>
        private void ResetWorld(int defenderSlot)
        {
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var m = _seats[slot];
                if (m == null) continue;

                m.IsDefender = slot == defenderSlot;
                m.HoldingSlipper = false;
                m.Stamina.RefillAndClearFatigue();

                // ⚠⚠ THE HERO KIT RESETS WITH THE BODY. Ultimate charge, cooldowns and
                // anything still running are cleared here, at the one place every round
                // boundary already passes through. `HeroKit.Tick` trickles passive charge every
                // frame including practice time, and nothing called `ResetKit` before this, so
                // charge banked before the whistle survived into the next round.
                m.GetComponent<Abilities.HeroAbilitySystem>()?.ResetKit();

                m.Teleport(m.IsDefender ? DefenderMark() : AttackerSpawn(slot));
            }

            if (_lata != null) _lata.HostRestore();

            // ⚠️ EVERY SEAT OWNS ITS OWN LATA AND TSINELAS. Only one lata physically exists,
            // so it wears whichever seat currently DEFENDS, re-applied every round as the role
            // rotates. That rotation is what makes the lata stats fair: your can is on the
            // mark for exactly the one round you defend, and everyone defends exactly once.
            if (_lata != null) _lata.SkinIndex = defenderSlot;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var s = _slippers[slot];
                if (s == null) continue;

                s.transform.position = SlipperHome(slot);
            }
        }

        private static Vector3 DefenderMark() =>
            new Vector3(0.0f, 0.0f, -Balance.DefenderStartOffset);

        /// <summary>The three attackers on a square ring outside the chalk, evenly spread.</summary>
        private static Vector3 AttackerSpawn(int slot)
        {
            float ring = Confinement.AttackerSpawnRing();
            float angle = (slot / (float)Balance.PlayerCount) * Mathf.PI * 2.0f;

            // Projected onto the SQUARE ring, matching the confinement shape.
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            float scale = 1.0f / Mathf.Max(Mathf.Abs(c), Mathf.Abs(s));

            return new Vector3(c * ring * scale, 0.0f, s * ring * scale);
        }

        private static Vector3 SlipperHome(int slot)
        {
            Vector3 p = AttackerSpawn(slot);
            return new Vector3(p.x, 0.045f, p.z);
        }
    }
}
