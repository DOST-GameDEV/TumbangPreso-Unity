using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso
{
    /// <summary>
    /// The final whistle, held for a moment before the result board. Owner handoff, 2026-11-25: the ending felt abrupt
    /// because the scoreboard appeared immediately. MATCH OVER goes up over the live court at the whistle, the cast is
    /// parked while anything airborne comes down, and the existing board is revealed once, two seconds later.
    ///
    /// ⚠️ THE RESULT IS LOCKED AT THE WHISTLE, NOT AT THE REVEAL. `MatchDirector.ReportMatchEnded` still raises
    /// `MatchEnded` at the whistle on every peer, so the round rules stop, the record is finalised and `AddScore` refuses
    /// from that frame. Only the board's presentation waits for this beat (`UI.MatchResult.OnMatchWon`).
    ///
    /// ⚠️ ONE HOST-AUTHORED START PER MATCH, on the clock `HalftimePresentation` reads (`SharedUltimatePhase.Now`) and
    /// scoped by the same match identity. A peer that hears the whistle late sees what is left of the beat, or the board
    /// at once if the beat has already expired. A peer whose own terminal snapshot arrives first starts a provisional beat
    /// and gives way to the host's start when it lands.
    ///
    /// ⚠️ THE SCORE LOCK AND THE PHYSICAL SETTLE ARE SEPARATE. Bodies settle by ordinary collision-aware movement under
    /// gravity. Whatever has still not come to rest when the reveal's brief transition covers the court is put on a
    /// verified map-local spot through the host's ordinary teleport correction (`CharacterMotor.MatchEnd.cs`).
    /// </summary>
    public sealed class MatchEndBeat : MonoBehaviour
    {
        /// <summary>From the whistle to the board.</summary>
        public const float Duration = 2.0f;

        /// <summary>The reveal's transition (the existing leaving wipe) starts this long before the board.</summary>
        public const float CoverLead = 0.55f;

        /// <summary>Bodies still not at rest are corrected this long before the board, while the wipe covers the court.</summary>
        public const float CorrectionLead = 0.30f;

        public static MatchEndBeat Instance { get; private set; }

        /// <summary>The board's moment on this peer: (match, winning seat or -1 for a draw). Raised once per match.</summary>
        public static event Action<long, int> Revealed;

        public long MatchId { get; private set; }
        public int FinalRound { get; private set; }
        public double Began { get; private set; }
        public bool HostTimed { get; private set; }
        public bool Active { get; private set; }
        public bool IsRevealed { get; private set; }

        /// <summary>For probes: how many reveals this peer raised for <see cref="MatchId"/>. One is the only right answer.</summary>
        public int RevealCount { get; private set; }

        /// <summary>For probes: bodies the host put on a verified spot because they had not come to rest.</summary>
        public int Corrections { get; private set; }

        public float Age => Active ? (float)(SharedUltimatePhase.Now - Began) : 0.0f;
        public float Remaining => Active && !IsRevealed ? Mathf.Max(0.0f, Duration - Age) : 0.0f;

        /// <summary>MATCH OVER is up and the board has not been revealed yet.</summary>
        public static bool Holding => Instance != null && Instance.Active && !Instance.IsRevealed;

        public static bool Defers(long match)
            => match != 0 && Instance != null && Instance.Active && Instance.MatchId == match && !Instance.IsRevealed;

        public static bool RevealedFor(long match)
            => match != 0 && Instance != null && Instance.Active && Instance.MatchId == match && Instance.IsRevealed;

        private Scene _scene;
        private UI.ReplayStinger _cover;
        private bool _coverTried, _corrected;

        public static MatchEndBeat Ensure(MatchDirector owner = null)
        {
            if (owner == null) owner = GameServices.Match;
            if (owner == null) return Instance;
            if (Instance != null && Instance.gameObject == owner.gameObject) return Instance;
            var beat = owner.GetComponent<MatchEndBeat>();
            if (beat == null) beat = owner.gameObject.AddComponent<MatchEndBeat>();
            Instance = beat;
            return beat;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        /// <summary>
        /// The terminal edge on this peer (`MatchDirector.ReportMatchEnded`). On the host or offline it is the
        /// authoritative start and is sent to every peer; on a client it is a provisional start the host's replaces.
        /// </summary>
        internal void Whistle(MatchDirector match)
        {
            if (match == null || match.PresentationMatchId == 0) return;
            if (Active && MatchId == match.PresentationMatchId) return;

            bool authority = NetAuthority.ShouldResolve();
            Begin(match.PresentationMatchId, match.RoundNumber, SharedUltimatePhase.Now, authority);
            if (authority) Net.MatchRpc.Instance?.BroadcastMatchEnd();
        }

        /// <summary>
        /// The host's whistle on a client. Rejects anything outside this match and anything stamped in the future; a
        /// duplicate, a resend on reconnect or a stale match changes nothing.
        /// </summary>
        public bool Receive(long match, int finalRound, double began)
        {
            var director = GameServices.Match;
            if (NetAuthority.ShouldResolve() || director == null || match <= 0 || match != director.PresentationMatchId
                || finalRound < 1 || finalRound > director.TotalRounds + 1
                || double.IsNaN(began) || double.IsInfinity(began) || began > SharedUltimatePhase.Now + 1.0) return false;

            if (Active && MatchId == match)
            {
                if (HostTimed) return false;

                // The provisional start from this peer's own terminal snapshot gives way to the host's.
                Began = began; FinalRound = finalRound; HostTimed = true;
                return true;
            }

            Begin(match, finalRound, began, true);
            return true;
        }

        private void Begin(long match, int finalRound, double began, bool hostTimed)
        {
            Clear();
            MatchId = match; FinalRound = finalRound; Began = began; HostTimed = hostTimed;
            Active = true; IsRevealed = false; RevealCount = 0; Corrections = 0;
            _scene = SceneManager.GetActiveScene();
            _coverTried = _corrected = false;

            // Nothing from the last moments of play may keep holding the picture: an ultimate's shared hold, a hitstop
            // or a break. Each of these releases only what it owns.
            SharedUltimatePhase.Instance?.Cancel();
            if (HalftimePresentation.Playing) HalftimePresentation.Instance.End(false);
            Hitstop.End();

            ParkCast();

            float age = Age;
            if (age >= Duration) return;

            UI.Hud.Instance?.ShowMatchOver(true);

            // One existing cue, the round-end sting every break already plays. A whistle heard late is not replayed.
            if (age < 0.5f) GameServices.Audio?.PlayUi("round_end");
        }

        /// <summary>Every seat stops taking input. `FreezeForMatchEnd` is idempotent, so the runner's own call later is harmless.</summary>
        private static void ParkCast()
        {
            var round = GameServices.Round;
            if (round == null) return;
            foreach (var body in round.Players)
                if (body != null) body.FreezeForMatchEnd();
        }

        private void Update() => Tick();

        /// <summary>One frame of the beat. Public for EditMode tests, which drive the shared clock themselves.</summary>
        public void Tick()
        {
            if (!Active) return;

            var match = GameServices.Match;
            if (match == null || match.PresentationMatchId != MatchId || SceneManager.GetActiveScene() != _scene)
            {
                Clear();
                return;
            }
            if (IsRevealed) return;

            float age = Age;

            if (!_coverTried && age >= Duration - CoverLead)
            {
                _coverTried = true;
                // Only a beat with time left to cover; a late arrival goes straight to the board.
                if (age < Duration - 0.1f) _cover = UI.ReplayStinger.CoverLeaving(transform);
            }

            if (!_corrected && NetAuthority.ShouldResolve() && age >= Duration - (_cover != null ? CorrectionLead : 0.0f))
            {
                _corrected = true;
                Corrections = CharacterMotor.CorrectUnrestedForMatchEnd(GameServices.Round);
            }

            // The board needs this peer's own terminal state as well as the time. A client whose snapshot is late keeps
            // MATCH OVER up until it lands rather than drawing standings it does not have yet.
            if (age < Duration || !match.HasCompleted || match.MatchInProgress) return;

            Reveal(match);
        }

        private void Reveal(MatchDirector match)
        {
            IsRevealed = true;
            RevealCount++;
            UI.Hud.Instance?.ShowMatchOver(false);

            var handlers = Revealed;
            if (handlers != null)
            {
                int winner = match.WinningSlot;
                // ⚠️ EACH LISTENER IN ITS OWN TRY. A C# event stops at the first throw, and the board must not depend on
                // the announcer (the same failure `SliceRunner.Subscribe` records for the round events).
                foreach (Action<long, int> handler in handlers.GetInvocationList())
                {
                    try { handler(MatchId, winner); }
                    catch (Exception failure) { Debug.LogException(failure); }
                }
            }

            // The board is drawn under the cover before it lifts.
            if (_cover != null) { _cover.Release(); _cover = null; }
            Debug.Log($"[MatchEnd] reveal match={MatchId} age={Age:0.00} host={HostTimed} corrections={Corrections}");
        }

        /// <summary>A new match, a rematch, a scene change or a transport change: nothing of this beat survives it.</summary>
        public void Clear()
        {
            bool showing = Active && !IsRevealed;
            Active = false; IsRevealed = false; HostTimed = false;
            MatchId = 0; FinalRound = 0; Began = 0;
            _coverTried = _corrected = false;
            if (_cover != null) { _cover.Release(); _cover = null; }
            if (showing) UI.Hud.Instance?.ShowMatchOver(false);
        }

        private void OnDestroy()
        {
            Clear();
            if (Instance == this) Instance = null;
        }
    }
}
