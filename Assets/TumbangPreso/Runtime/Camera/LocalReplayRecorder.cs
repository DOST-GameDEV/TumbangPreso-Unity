using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.CameraSystem
{
    // Samples are detached on the main thread, then encoded/written off-thread.
    // Keep the existing eight-second ring; never retain an entire match in RAM.
    internal sealed class LocalReplayRecorder
    {
        private readonly MatchReplayArchive _archive;
        private LocalReplayStore.Writer _writer;
        private MatchDirector _match;
        private long _identity, _sequence;
        private int _round;
        private float _start = -1, _last;
        private string _warning;
        public LocalReplayRecorder(MatchReplayArchive archive) { _archive = archive; }
        public void Tick()
        {
            var match = GameServices.Match;
            if (_writer != null && (match != _match || match.PresentationMatchId != _identity || !match.MatchInProgress))
                Finish(_match != null && _match.HasCompleted);
            else if (_writer != null && match.RoundNumber != _round)
            { Flush(); _start = -1; _round = match.RoundNumber; }
        }
        public void Sample(float time)
        {
            var match = GameServices.Match;
            if (LocalReplayPlayback.Active || match == null || !match.MatchInProgress || match.IsWarmupBuffer ||
                PracticeRange.Active || GameLaunch.GuidedTutorial) return;
            Tick();
            if (_writer == null)
            {
                _match = match; _identity = match.PresentationMatchId; _round = match.RoundNumber; _sequence = 0;
                _warning = match.RoundNumber>1 || UI.SceneFlow.SelectedRoundSeconds-GameServices.Round.TimeLeft>1
                    ? "Recording began after the match started." : null;
                var net = Net.NetSession.Instance;
                _writer = new LocalReplayStore.Writer(LocalReplayStore.Folder, new LocalReplayManifest {
                    MatchId = _identity, CreatedUtc = DateTime.UtcNow.ToString("O"),
                    Map = SceneManager.GetActiveScene().name, Mode = UI.SceneFlow.SelectedMode.ToString(),
                    Rules = CustomGameRules.ToWire(UI.SceneFlow.SelectedRules), Build = BuildIdentity.OneLine(),
                    Custom = (net != null && net.IsNetworked && !UI.Hub.HubQueueWatch.QueueRoom) ||
                        CustomGameRules.ToWire(UI.SceneFlow.SelectedRules) != CustomGameRules.ToWire(CustomGameRules.Defaults(UI.SceneFlow.SelectedMode)) });
                _match.MatchEnded += Completed;
                _match.RoundStarted += RoundStarted;
                _match.IntermissionStarted += IntermissionStarted;
            }
            if (_start < 0) _start = time;
            _last = time;
            if (_last - _start >= 3) Flush();
        }
        private void Completed(int winner) => Finish(true);
        private void RoundStarted(int number,int taya)
        {
            // Flush before MatchPoseHistory's next LateUpdate clears its ring.
            // Polling in Update can miss a transition started by a coroutine.
            Flush();_start=-1;_round=number;
        }
        private void IntermissionStarted(int number,int taya){Flush();_start=-1;}
        private void Flush()
        {
            if (_writer == null || _start < 0 || _last <= _start) return;
            if (_writer.CanAppend && _archive.TryCaptureSession(_start, _last, ++_sequence, out var clip, out string error))
                _writer.Append(clip);
            else
            {
                _warning = _writer.Error ?? "Some replay footage could not be recorded. " +
                    (_writer.CanAppend ? _archive.SessionCaptureError : "The replay disk writer could not keep up.");
                Debug.LogWarning("[LocalReplay] " + _warning);
            }
            _start = _last;
        }
        public void Finish(bool completed)
        {
            if (_writer == null) return;
            Flush();
            if (_match != null)
            {
                _match.MatchEnded -= Completed;
                _match.RoundStarted -= RoundStarted;
                _match.IntermissionStarted -= IntermissionStarted;
            }
            _writer.Finish(completed, _warning);
            _writer = null; _match = null; _start = -1;
        }
    }
}
