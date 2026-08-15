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

            GameServices.Match.RoundStarted += OnRoundStarted;
            GameServices.Match.IntermissionStarted += OnIntermission;
            GameServices.Match.MatchEnded += OnMatchEnded;

            GameServices.Match.StartMatch();
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

        private void OnMatchEnded(int winningSlot)
        {
            GameServices.Round.EndRound();
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
