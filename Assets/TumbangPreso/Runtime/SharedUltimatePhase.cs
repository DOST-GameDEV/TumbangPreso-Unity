using System;
using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso
{
    public readonly struct UltimateCommit
    {
        public readonly int Seat;
        public readonly long Request;
        public readonly Vector3 Position, Forward, Aim, FamiliarPosition;
        public readonly bool HasFamiliar;
        public readonly float Held;
        public UltimateCommit(int seat, long request, Vector3 position, Vector3 forward, Vector3 aim, float held, bool hasFamiliar = false, Vector3 familiarPosition = default)
        { Seat=seat; Request=request; Position=position; Forward=forward; Aim=aim; Held=held; HasFamiliar=hasFamiliar; FamiliarPosition=familiarPosition; }
    }

    // One accepted cohort, one shared boundary. The reservation spends resources;
    // the real abilities start only after the introduction, preserving warnings.
    [DefaultExecutionOrder(2500)]
    public sealed class SharedUltimatePhase : MonoBehaviour
    {
        public const double DefaultDuration = Visual.UltimatePerformance.DefaultSeconds;
        /// <summary>
        /// ⚠️⚠️ THIS PHASE'S LENGTH, NOT A CONSTANT, SINCE REFINE-2.11 (owner 2026-09-24: *"its fine if
        /// its longer than 2.8 seconds part of the work is researching and thinking about how long it
        /// should be"*). It is the LONGEST accepted hero's authored introduction
        /// (`Visual.UltimatePerformance.Seconds`, 2.8 to 4.2 s), derived from the same accepted
        /// commits on the host and on every peer, so the shared boundary is still one number that
        /// nobody sends. Protocol 52 marks the change: a peer on the old fixed 2.8 would release
        /// its clock at a different moment from the host.
        /// </summary>
        public double Duration { get; private set; } = DefaultDuration;
        /// <summary>The shared length for a cohort: its longest member's introduction.</summary>
        public static double CohortSeconds(IReadOnlyList<UltimateCommit> commits)
        {
            double longest = 0;
            if (commits != null)
                foreach (var cast in commits) longest = Math.Max(longest, SecondsFor(cast.Seat));
            return longest > 0 ? longest : DefaultDuration;
        }
        private static double SecondsFor(int seat)
        {
            string hero = GameServices.Round?.PlayerAt(seat)?.AbilitySystem?.HeroId;
            return Visual.UltimatePerformance.SecondsFor(hero);
        }
        public static SharedUltimatePhase Instance { get; private set; }
        public static bool BlocksActions => Instance != null && Instance.Active && Instance._sealed;
        public static bool Collecting => Instance != null && Instance.Active && !Instance._sealed;
        public bool Active { get; private set; }
        public bool Sealed => _sealed;
        public long PhaseId { get; private set; }
        public long MatchId { get; private set; }
        public int Round { get; private set; }
        public double Began { get; private set; }
        public float FrozenRoundTime {get;private set;}
        public double ReleasedAt {get;private set;}
        public double ActivationMilliseconds {get;private set;}
        private float _deferredRoundTime;
        public IReadOnlyList<UltimateCommit> Commits => _commits;
        private readonly List<UltimateCommit> _commits = new List<UltimateCommit>(4);
        private long _sequence, _lastReceived;
        private bool _sealed, _actorsReady, _viewAttempted;
        private int _frame;
        private Scene _scene;
        private CameraSystem.UltimatePhaseView _view;

        public static SharedUltimatePhase Ensure()
        {
            if (Instance != null) return Instance;
            var owner = GameServices.Match;
            if (owner == null) return null;
            var phase = owner.GetComponent<SharedUltimatePhase>();
            return phase != null ? phase : owner.gameObject.AddComponent<SharedUltimatePhase>();
        }
        private void Awake() => Instance = this;
        public static double Now => NetAuthority.IsNetworked && Unity.Netcode.NetworkManager.Singleton != null
            && Unity.Netcode.NetworkManager.Singleton.IsListening ? Unity.Netcode.NetworkManager.Singleton.ServerTime.Time : Time.realtimeSinceStartupAsDouble;
        public bool CanAccept(int seat)
        {
            if (!isActiveAndEnabled || !NetAuthority.ShouldResolve() || GameServices.Match == null || GameServices.Round == null
                || !GameServices.Round.RoundActive || GameServices.Match.IsWarmupBuffer) return false;
            if (!Active) return PresentationClock.RequestedScale > 0;
            if (_sealed || _frame != Time.frameCount || _commits.Count >= 4) return false;
            foreach (var cast in _commits) if (cast.Seat == seat) return false;
            return true;
        }
        internal void Accept(UltimateCommit cast)
        {
            if (!Active)
            {
                MatchId = GameServices.Match.PresentationMatchId; Round = GameServices.Match.RoundNumber;
                PhaseId = ++_sequence; Began = Now; _frame = Time.frameCount;
                FrozenRoundTime=_deferredRoundTime=GameServices.Round.TimeLeft;ReleasedAt=ActivationMilliseconds=0;
                _commits.Clear(); Active = true; _sealed = false; _actorsReady = true; Duration = 0;
                _scene = SceneManager.GetActiveScene();
                PresentationClock.Hold();
            }
            _commits.Add(cast);
            Duration = Math.Max(Duration, SecondsFor(cast.Seat));
        }
        internal void Receive(long match, int round, long phase, double began, float resume, UltimateCommit[] commits, float frozenRoundTime)
        {
            if (NetAuthority.ShouldResolve() || phase <= 0 || commits == null || commits.Length < 1 || commits.Length > 4
                || !float.IsFinite(frozenRoundTime)||frozenRoundTime<0||frozenRoundTime>Core.CustomGameRules.MaxRoundSeconds
                || match != Net.MatchRpc.Instance?.PresentationMatchId || double.IsNaN(began) || double.IsInfinity(began) || began > Now + .5) return;
            if (GameServices.Match != null && GameServices.Match.RoundNumber > round) return;
            if (MatchId != match) { Cancel(); _lastReceived = 0; }
            if (phase <= _lastReceived) return;
            _lastReceived = phase;
            double length = CohortSeconds(commits);
            if (Now >= began + length)
            {
                foreach (var cast in commits) GameServices.Round?.PlayerAt(cast.Seat)?.AbilitySystem?.AcknowledgeSharedUltimate(cast.Request);
                Net.MatchRpc.Instance?.RequestWorldSnapshot(); return;
            }
            Cancel(); MatchId=match; Round=round; PhaseId=phase; Began=began; Duration=length;
            FrozenRoundTime=_deferredRoundTime=frozenRoundTime;
            _commits.Clear(); _commits.AddRange(commits); Active=true; _sealed=true; _actorsReady=false;
            PresentationClock.RequestScale(resume); PresentationClock.Hold();
            if(GameServices.Match?.RoundNumber==Round)GameServices.Round?.ApplyPresentationTime(FrozenRoundTime);
        }
        internal float HoldSnapshotClock(float incoming,bool roundActive,bool inProgress)
        {
            if(!Active||!roundActive||!inProgress||GameServices.Match?.PresentationMatchId!=MatchId||GameServices.Match.RoundNumber!=Round)return incoming;
            // An in-flight snapshot must not move the displayed frozen clock.
            // Keep a later, lower host clock for the shared release boundary.
            _deferredRoundTime=Mathf.Min(_deferredRoundTime,incoming);
            return FrozenRoundTime;
        }
        private bool PrepareActors()
        {
            var match = GameServices.Match; var round = GameServices.Round;
            if (match == null || round == null || match.PresentationMatchId != MatchId || match.RoundNumber != Round || !round.RoundActive) return false;
            foreach (var cast in _commits) if (round.PlayerAt(cast.Seat)?.AbilitySystem?.Kit?.Ultimate == null) return false;
            foreach (var cast in _commits)
            {
                var actor = round.PlayerAt(cast.Seat);
                if (cast.HasFamiliar) actor.GetComponent<Visual.CharacterVisual>()?.Companion?.ApplyCastAnchor(cast.FamiliarPosition);
                actor.AbilitySystem.AdoptSharedUltimate(cast.Request);
            }
            _scene = SceneManager.GetActiveScene(); _actorsReady=true; ClearActions(); return true;
        }
        private void ClearActions()
        {
            var round = GameServices.Round; if (round == null) return;
            foreach (var actor in round.Players)
            {
                actor.Intent.RequireFreshActions(); actor.AbilitySystem?.ClearPresentationInput();
            }
        }
        private void LateUpdate()
        {
            if (!Active)
            {
                if(GameServices.Round!=null && UI.SceneFlow.SelectedMode==Core.GameMode.HeroStrike)
                    foreach(var actor in GameServices.Round.Players)if(Visual.UltimateIntroductionCache.WarmOne(actor))break;
                return;
            }
            var match = GameServices.Match; var round = GameServices.Round;
            if (_actorsReady && (match == null || !match.MatchInProgress || match.RoundNumber != Round
                || match.PresentationMatchId != MatchId || round == null || !round.RoundActive
                || SceneManager.GetActiveScene() != _scene)) { Cancel(); return; }
            if (!_sealed)
            {
                _sealed = true; ClearActions();
                Net.MatchRpc.Instance?.BroadcastUltimatePhase(this);
            }
            if (!_actorsReady) PrepareActors();
            if (Now >= Began + Duration)
            {
                if (_actorsReady) Complete();
                else { Cancel(); Net.MatchRpc.Instance?.RequestWorldSnapshot(); }
                return;
            }
            if (_actorsReady && !_viewAttempted)
            {
                _viewAttempted = true;
                try { _view = new CameraSystem.UltimatePhaseView(transform, _commits, Duration); }
                catch (Exception error) { Debug.LogException(error); }
            }
            try { _view?.Draw((float)Math.Max(0, Now - Began)); }
            catch (Exception error) { _view?.Dispose(); _view=null; Debug.LogException(error); }
            // All supported writers preserve the requested rate through RequestScale.
            Time.timeScale = 0;
        }
        private void Complete()
        {
            var round = GameServices.Round;
            ClearActions();
            var accepted = _commits.ToArray();
            if(!NetAuthority.ShouldResolve())round?.ApplyPresentationTime(_deferredRoundTime);
            bool themePlayed = _view != null && _view.SoundPlayed;
            if (_view != null)
                foreach (int seat in _view.VoicedSeats)
                {
                    var voiced = round?.PlayerAt(seat)?.AbilitySystem?.Kit?.Ultimate;
                    if (voiced != null) voiced.IntroductionVoiced = true;
                }
            _view?.Dispose(); _view=null;
            ReleasedAt=Now;double activationBegan=Time.realtimeSinceStartupAsDouble;
            Active=false; _sealed=false; _viewAttempted=false; PresentationClock.Release();
            foreach (var cast in accepted)
                round?.PlayerAt(cast.Seat)?.AbilitySystem?.ExecuteSharedUltimate(cast, themePlayed);
            ActivationMilliseconds=(Time.realtimeSinceStartupAsDouble-activationBegan)*1000;
            _commits.Clear();
            if (NetAuthority.ShouldResolve()) Net.MatchRpc.Instance?.BroadcastWorldSnapshot();
        }
        public void Cancel()
        {
            if (!Active && _view == null) return;
            _view?.Dispose(); _view=null;
            foreach (var cast in _commits) GameServices.Round?.PlayerAt(cast.Seat)?.AbilitySystem?.CancelSharedUltimate();
            _commits.Clear(); Active=false; _sealed=false; _actorsReady=false; _viewAttempted=false;
            PresentationClock.Release();
        }
        private void OnDisable() => Cancel();
        private void OnDestroy() { Cancel(); if (Instance == this) Instance = null; }
    }
}
