using System;
using System.Collections.Generic;
using TumbangPreso.CameraSystem;
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
    ///
    /// ⚠️ THEN THE HIGHLIGHTS, THEN THE BOARD (owner, 2026-11-26: "do highlights after match ends tapos slowly reveal the
    /// winner bcz sobrang abrupt"). After MATCH OVER the REPLAY band crosses and up to `MaxClips` of the match's retained
    /// moments play (`MatchReplayArchive`, the footage halftime already shows), each in a `ClipSeconds` slot with the
    /// halftime slowdown round its contact. The host chooses the clips at the whistle and sends their ids with the start,
    /// so every peer plays the same ones on the same clock; a peer without a clip's bytes keeps MATCH OVER over the court
    /// for that slot. The board then brings the standings in from last to first (`MatchResult.FinishPerformance`).
    /// </summary>
    public sealed class MatchEndBeat : MonoBehaviour
    {
        /// <summary>MATCH OVER over the live court, from the whistle. With no highlights it is the whole beat.</summary>
        public const float Duration = 2.0f;

        /// <summary>The most highlights the ending plays, and each one's slot: up to `ClipLongest` of footage slowed round
        /// its contact (`HalftimePresentation.ReplayOffset`, 6.4 s at most) with its wipes.</summary>
        public const int MaxClips = 2;
        public const float ClipSeconds = 6.8f, ClipLongest = 4.5f, ClipLead = 0.35f;

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
        public float Remaining => Active && !IsRevealed ? Mathf.Max(0.0f, RevealAt - Age) : 0.0f;

        /// <summary>The highlights the host chose for this ending, in the order they play.</summary>
        public int ClipCount { get; private set; }
        public long ClipAt(int i) => i >= 0 && i < ClipCount ? _clipIds[i] : 0;

        /// <summary>From the whistle to the board: MATCH OVER, then each highlight's slot.</summary>
        public float RevealAt => Duration + ClipCount * ClipSeconds;

        /// <summary>For probes: the highlights this peer actually drew.</summary>
        public int ClipsShown { get; private set; }

        /// <summary>MATCH OVER is up and the board has not been revealed yet.</summary>
        public static bool Holding => Instance != null && Instance.Active && !Instance.IsRevealed;

        public static bool Defers(long match)
            => match != 0 && Instance != null && Instance.Active && Instance.MatchId == match && !Instance.IsRevealed;

        public static bool RevealedFor(long match)
            => match != 0 && Instance != null && Instance.Active && Instance.MatchId == match && Instance.IsRevealed;

        private Scene _scene;
        private UI.ReplayStinger _cover;
        private bool _coverTried, _corrected;
        private readonly long[] _clipIds = new long[MaxClips];
        private int _slot = -1;
        private bool _slotCovered, _slotDrawn;
        private RecordedMatchClip _clip;
        private RecordedWorldView _view;

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
            Begin(match.PresentationMatchId, match.RoundNumber, SharedUltimatePhase.Now, authority,
                authority ? PickClips(match.PresentationMatchId) : null);
            if (authority) Net.MatchRpc.Instance?.BroadcastMatchEnd();
        }

        /// <summary>
        /// The host's whistle on a client. Rejects anything outside this match and anything stamped in the future; a
        /// duplicate, a resend on reconnect or a stale match changes nothing.
        /// </summary>
        public bool Receive(long match, int finalRound, double began, IReadOnlyList<long> clips = null)
        {
            var director = GameServices.Match;
            if (NetAuthority.ShouldResolve() || director == null || match <= 0 || match != director.PresentationMatchId
                || finalRound < 1 || finalRound > director.TotalRounds + 1
                || double.IsNaN(began) || double.IsInfinity(began) || began > SharedUltimatePhase.Now + 1.0
                || !ValidClips(clips)) return false;

            if (Active && MatchId == match)
            {
                if (HostTimed) return false;

                // The provisional start from this peer's own terminal snapshot gives way to the host's, and so do the host's
                // highlights, unless this peer's board is already up.
                Began = began; FinalRound = finalRound; HostTimed = true;
                if (!IsRevealed) SetClips(clips);
                return true;
            }

            Begin(match, finalRound, began, true, clips);
            return true;
        }

        private static bool ValidClips(IReadOnlyList<long> clips)
        {
            if (clips == null) return true;
            if (clips.Count > MaxClips) return false;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] <= 0) return false;
                for (int j = 0; j < i; j++) if (clips[j] == clips[i]) return false;
            }
            return true;
        }

        private void SetClips(IReadOnlyList<long> clips)
        {
            ClipCount = 0;
            if (clips != null) for (int i = 0; i < clips.Count && i < MaxClips; i++) _clipIds[ClipCount++] = clips[i];
        }

        /// <summary>
        /// The host's choice at the whistle: the archive's most important moments of this match that fit a slot, played in
        /// the order they happened. Online only those every peer already holds: a clip still on its way would leave a peer
        /// looking at MATCH OVER for its whole slot while the others watch it.
        /// </summary>
        private static List<long> PickClips(long match)
        {
            var archive = FindAnyObjectByType<MatchReplayArchive>();
            if (archive == null) return null;
            var picked = new List<RecordedMatchClip>(MaxClips);
            var rpc = NetAuthority.IsNetworked ? Net.MatchRpc.Instance : null;
            if (NetAuthority.IsNetworked && rpc == null) return null;
            foreach (var retained in archive.Clips)
            {
                var clip = retained.Clip;
                if (clip == null || clip.MatchId != match || clip.Id <= 0 || clip.Duration <= 0.0f || clip.Duration > ClipLongest
                    || picked.Contains(clip)) continue;
                if (rpc != null && !rpc.ReplayReadyForAudience(clip.Id)) continue;
                picked.Add(clip);
                if (picked.Count == MaxClips) break;
            }
            picked.Sort((a, b) => a.Id.CompareTo(b.Id));
            var ids = new List<long>(picked.Count);
            foreach (var clip in picked) ids.Add(clip.Id);
            return ids;
        }

        private void Begin(long match, int finalRound, double began, bool hostTimed, IReadOnlyList<long> clips = null)
        {
            Clear();
            MatchId = match; FinalRound = finalRound; Began = began; HostTimed = hostTimed;
            SetClips(clips);
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
            if (age >= RevealAt) return;

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
            bool reel = ClipCount > 0 && !Settings.SettingsStore.Current.ReducedUiMotion;

            // The first cut, out of MATCH OVER: into the highlights (the REPLAY band) or to the board (the leaving band).
            if (!_coverTried && age >= Duration - CoverLead)
            {
                _coverTried = true;
                // Only a beat with time left to cover; a late arrival goes straight on.
                if (age < Duration - 0.1f)
                    _cover = reel ? UI.ReplayStinger.Cover(transform, UI.HudDraw.Honey) : UI.ReplayStinger.CoverLeaving(transform);
            }

            if (!_corrected && NetAuthority.ShouldResolve() && age >= Duration - (_cover != null ? CorrectionLead : 0.0f))
            {
                _corrected = true;
                Corrections = CharacterMotor.CorrectUnrestedForMatchEnd(GameServices.Round);
            }

            if (reel && age >= Duration) Highlights(age);

            // The board needs this peer's own terminal state as well as the time. A client whose snapshot is late keeps
            // MATCH OVER up until it lands rather than drawing standings it does not have yet.
            if (age < RevealAt || !match.HasCompleted || match.MatchInProgress) return;

            Reveal(match);
        }

        /// <summary>
        /// One frame of the highlights: the slot `age` falls in, its clip drawn at the halftime pace, and the next cut's
        /// band crossing before the slot ends (REPLAY into the next clip, the leaving band into the board).
        /// </summary>
        private void Highlights(float age)
        {
            int slot = Mathf.FloorToInt((age - Duration) / ClipSeconds);
            if (slot >= ClipCount) { StopClip(); return; }
            float local = age - Duration - slot * ClipSeconds;
            if (slot != _slot) StartClip(slot);

            if (_view != null && _view.Ready && _clip != null)
            {
                float elapsed = Mathf.Max(0.0f, local - ClipLead);
                float offset = Mathf.Min(HalftimePresentation.ReplayOffset(elapsed, _clip.Contact - _clip.Start), _clip.Duration);
                try
                {
                    _view.Draw(_clip.Start + offset);
                    if (!_slotDrawn) { _slotDrawn = true; ClipsShown++; }
                }
                catch (Exception failure)
                {
                    Debug.LogWarning("[MatchEnd] highlight view failed: " + failure.Message);
                    StopClip();
                    UI.Hud.Instance?.ShowMatchOver(true);
                }
            }

            // The band that opened this slot lifts once its first picture is drawn, or at once when there is none to draw.
            if (_cover != null && !_slotCovered && local >= ClipLead && (_slotDrawn || _view == null)) { _cover.Release(); _cover = null; }

            if (!_slotCovered && local >= ClipSeconds - CoverLead)
            {
                _slotCovered = true;
                if (local < ClipSeconds - 0.1f)
                {
                    if (_cover != null) _cover.Release();
                    _cover = slot + 1 < ClipCount ? UI.ReplayStinger.Cover(transform, UI.HudDraw.Honey) : UI.ReplayStinger.CoverLeaving(transform);
                }
            }
        }

        private void StartClip(int slot)
        {
            _slot = slot; _slotCovered = false; _slotDrawn = false;
            long id = _clipIds[slot];
            RecordedMatchClip clip = null;
            var archive = FindAnyObjectByType<MatchReplayArchive>();
            if (archive != null)
                foreach (var retained in archive.Clips)
                    if (retained.Clip != null && retained.Clip.Id == id && retained.Clip.MatchId == MatchId) { clip = retained.Clip; break; }
            if (clip == null) clip = Net.MatchRpc.Instance?.ReceivedReplay(id);
            _clip = clip;
            if (clip == null)
            {
                Debug.Log($"[MatchEnd] highlight {slot + 1}/{ClipCount} clip={id} is not on this peer; MATCH OVER stays");
                StopClip();
                UI.Hud.Instance?.ShowMatchOver(true);
                return;
            }

            try
            {
                if (_view == null || !_view.Ready || !_view.UseClip(clip))
                {
                    _view?.Dispose();
                    _view = new RecordedWorldView(transform, clip);
                }
            }
            catch (Exception failure)
            {
                Debug.LogWarning("[MatchEnd] highlight view unavailable: " + failure.Message);
                _view = null;
            }

            if (_view == null || !_view.Ready)
            {
                Debug.Log($"[MatchEnd] highlight {slot + 1}/{ClipCount} clip={id} unavailable: {_view?.UnavailableReason ?? "no view"}");
                StopClip();
                UI.Hud.Instance?.ShowMatchOver(true);
                return;
            }

            UI.Hud.Instance?.ShowMatchOver(false);
            Debug.Log($"[MatchEnd] highlight {slot + 1}/{ClipCount} clip={id} {clip.Reason} {clip.Duration:0.00}s");
        }

        private void StopClip()
        {
            if (_view != null) { _view.Dispose(); _view = null; }
            _clip = null;
        }

        private void Reveal(MatchDirector match)
        {
            IsRevealed = true;
            RevealCount++;
            UI.Hud.Instance?.ShowMatchOver(false);
            StopClip();

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
            Debug.Log($"[MatchEnd] reveal match={MatchId} age={Age:0.00} host={HostTimed} corrections={Corrections} highlights={ClipsShown}/{ClipCount}");
        }

        /// <summary>A new match, a rematch, a scene change or a transport change: nothing of this beat survives it.</summary>
        public void Clear()
        {
            bool showing = Active && !IsRevealed;
            Active = false; IsRevealed = false; HostTimed = false;
            MatchId = 0; FinalRound = 0; Began = 0;
            _coverTried = _corrected = false;
            ClipCount = 0; ClipsShown = 0; _slot = -1; _slotCovered = _slotDrawn = false;
            StopClip();
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
