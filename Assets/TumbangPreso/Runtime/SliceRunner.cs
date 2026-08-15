using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Drives a match over objects that ALREADY EXIST in the scene, rather than instantiating
    /// prefabs.
    ///
    /// ⚠️ THIS EXISTS BECAUSE PREFABS ARE A PHASE 6 PROBLEM AND THE SLICE IS NEEDED NOW.
    /// `MatchBootstrap` is the real spawner and takes prefab references; it is correct and it
    /// stays. But a prefab has to be authored, and the whole point of the vertical slice is to
    /// get a match RUNNING and MEASURABLE before any authoring exists. So this wires up scene
    /// objects the SceneBuilder placed, and shares every rule with the real path by calling
    /// the same directors.
    ///
    /// ⚠️ IT MUST NOT ACQUIRE RULES OF ITS OWN. The moment this file decides something the real
    /// bootstrap does not, the slice stops measuring the game and starts measuring itself.
    /// Everything here is placement and wiring; every decision routes to MatchDirector and
    /// RoundDirector.
    /// </summary>
    public sealed class SliceRunner : MonoBehaviour
    {
        public Lata Lata;
        public CharacterMotor[] Seats;
        public Slipper[] Slippers;

        [Tooltip("Seconds of round time per real second. 1 is real time; higher runs a whole " +
                 "match faster for a headless probe.")]
        public float TimeScale = 1.0f;

        public bool AutoStart = true;

        public bool Running { get; private set; }

        private void Start()
        {
            if (AutoStart) Begin();
        }

        public void Begin()
        {
            if (Lata == null || Seats == null || Slippers == null)
            {
                Debug.LogError("[SliceRunner] scene references are not wired.");
                return;
            }

            GameServices.Round.Clear();
            GameServices.Round.Lata = Lata;

            foreach (var s in Seats)
                if (s != null) GameServices.Round.Register(s);

            GameServices.Match.RoundStarted += OnRoundStarted;
            GameServices.Match.IntermissionStarted += OnIntermission;
            GameServices.Match.MatchEnded += OnMatchEnded;

            if (TimeScale > 1.0f) Time.timeScale = TimeScale;

            Running = true;
            GameServices.Match.StartMatch();
        }

        private void OnRoundStarted(int roundNumber, int defenderSlot)
        {
            ResetWorld(defenderSlot);
            GameServices.Round.BeginRound();

            Debug.Log($"[Slice] round {roundNumber} begins, taya is seat {defenderSlot}");
        }

        private void OnIntermission(int nextRound, int nextDefenderSlot) =>
            Invoke(nameof(Advance), Balance.IntermissionDuration);

        private void Advance() => GameServices.Match.AdvanceRound();

        private void OnMatchEnded(int winningSlot)
        {
            GameServices.Round.EndRound();
            Running = false;
            Time.timeScale = 1.0f;

            var m = GameServices.Match;
            Debug.Log($"[Slice] match over. scores: " +
                      $"{m.ScoreFor(0)} / {m.ScoreFor(1)} / {m.ScoreFor(2)} / {m.ScoreFor(3)}. " +
                      (winningSlot < 0 ? "draw" : $"seat {winningSlot} wins"));
        }

        /// <summary>
        /// ⚠️ ROLE ROTATION IS EXACTLY WHAT TRIGGERS THE SPAWN-SETTLE BUG, because two seats
        /// trade marks and each stands on the other's stale collider for a frame. Teleport()
        /// arms the settle, and it is not an optimisation to skip.
        /// </summary>
        private void ResetWorld(int defenderSlot)
        {
            for (int slot = 0; slot < Seats.Length; slot++)
            {
                var m = Seats[slot];
                if (m == null) continue;

                m.IsDefender = slot == defenderSlot;
                m.HoldingSlipper = false;
                m.Stamina.RefillAndClearFatigue();

                // ⚠️ THE SPAWN IS RECORDED, NOT JUST USED. The kill plane returns whoever falls
                // off the world to their OWN spawn, and it has no other way to know where that
                // is. Written every round because the mark moves when roles rotate.
                Vector3 mark = m.IsDefender ? DefenderMark() : AttackerSpawn(slot);
                m.SpawnPosition = mark;
                m.Teleport(mark);

                // Roles rotate every round, so the ring and tag have to re-colour with them.
                var plate = m.GetComponentInChildren<Visual.CharacterNameplate>();
                if (plate != null) { plate.ApplySizing(); plate.Refresh(); }
            }

            if (Lata != null)
            {
                // The lata wears whichever seat currently DEFENDS. That rotation is what makes
                // the can stats fair: your can is on the mark for exactly the one round you
                // defend, and everyone defends exactly once.
                Lata.SkinIndex = defenderSlot;
                Lata.HostRestore();
            }

            for (int slot = 0; slot < Slippers.Length; slot++)
            {
                if (Slippers[slot] == null) continue;
                Slippers[slot].transform.position = SlipperHome(slot);
            }
        }

        private static Vector3 DefenderMark() =>
            new Vector3(0.0f, 0.0f, -Balance.DefenderStartOffset);

        /// <summary>Spread around the SQUARE ring, matching the confinement shape.</summary>
        private static Vector3 AttackerSpawn(int slot)
        {
            float ring = Confinement.AttackerSpawnRing();
            float angle = (slot / (float)Balance.PlayerCount) * Mathf.PI * 2.0f;

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
