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
        public const double Duration = 2.8;
        public static SharedUltimatePhase Instance { get; private set; }
        public static bool BlocksActions => Instance != null && Instance.Active && Instance._sealed;
        public static bool Collecting => Instance != null && Instance.Active && !Instance._sealed;
        public bool Active { get; private set; }
        public bool Sealed => _sealed;
        public long PhaseId { get; private set; }
        public long MatchId { get; private set; }
        public int Round { get; private set; }
        public double Began { get; private set; }
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
                _commits.Clear(); Active = true; _sealed = false; _actorsReady = true;
                _scene = SceneManager.GetActiveScene();
                PresentationClock.Hold();
            }
            _commits.Add(cast);
        }
        internal void Receive(long match, int round, long phase, double began, float resume, UltimateCommit[] commits)
        {
            if (NetAuthority.ShouldResolve() || phase <= 0 || commits == null || commits.Length < 1 || commits.Length > 4
                || match != Net.MatchRpc.Instance?.PresentationMatchId || double.IsNaN(began) || double.IsInfinity(began) || began > Now + .5) return;
            if (GameServices.Match != null && GameServices.Match.RoundNumber > round) return;
            if (MatchId != match) { Cancel(); _lastReceived = 0; }
            if (phase <= _lastReceived) return;
            _lastReceived = phase;
            if (Now >= began + Duration)
            {
                foreach (var cast in commits) GameServices.Round?.PlayerAt(cast.Seat)?.AbilitySystem?.AcknowledgeSharedUltimate(cast.Request);
                Net.MatchRpc.Instance?.RequestWorldSnapshot(); return;
            }
            Cancel(); MatchId=match; Round=round; PhaseId=phase; Began=began;
            _commits.Clear(); _commits.AddRange(commits); Active=true; _sealed=true; _actorsReady=false;
            PresentationClock.RequestScale(resume); PresentationClock.Hold();
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
                try { _view = new CameraSystem.UltimatePhaseView(transform, _commits); }
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
            bool themePlayed = _view != null && _view.SoundPlayed;
            _view?.Dispose(); _view=null;
            Active=false; _sealed=false; _viewAttempted=false; PresentationClock.Release();
            foreach (var cast in accepted)
                round?.PlayerAt(cast.Seat)?.AbilitySystem?.ExecuteSharedUltimate(cast, themePlayed);
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
