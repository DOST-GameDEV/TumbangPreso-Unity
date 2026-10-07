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
        private Map.ArenaFx _fx;
        private LocalReplayFxFrame _lastFx;
        private readonly System.Collections.Generic.List<LocalReplayFxFrame> _fxFrames=new System.Collections.Generic.List<LocalReplayFxFrame>();
        private readonly System.Collections.Generic.List<LocalReplaySceneFrame> _sceneFrames=new System.Collections.Generic.List<LocalReplaySceneFrame>(64);
        public LocalReplayRecorder(MatchReplayArchive archive) { _archive = archive; }
        public void Tick()
        {
            var currentFx=Map.ArenaFx.Instance;
            if(_fx!=currentFx){if(_fx!=null)_fx.FrameRendered-=EffectFrame;_fx=currentFx;if(_fx!=null)_fx.FrameRendered+=EffectFrame;}
            var match = GameServices.Match;
            if (_writer != null && (match != _match || match.PresentationMatchId != _identity || !match.MatchInProgress))
                Finish(_match != null && _match.HasCompleted);
            else if (_writer != null && match.RoundNumber != _round)
            { Flush(); _start = -1; _round = match.RoundNumber; _sceneFrames.Clear();_fxFrames.Clear();_lastFx=null; }
        }
        private void EffectFrame(float time)
        {
            var match=GameServices.Match;
            if(_fx==null||LocalReplayPlayback.Active||match==null||!match.MatchInProgress||GameServices.Round?.RoundActive!=true||
                match.IsWarmupBuffer||PracticeRange.Active||GameLaunch.GuidedTutorial)return;
            if(_lastFx!=null&&time<=_lastFx.Time)return;
            _lastFx=new LocalReplayFxFrame{Time=time,Quads=_fx.CaptureRecordedQuads()};
            if(_writer!=null)_fxFrames.Add(_lastFx);
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
            if(_fxFrames.Count==0&&_lastFx!=null&&_lastFx.Time>=_start)_fxFrames.Add(_lastFx);
            _last = time;
            _sceneFrames.Add(LocalReplaySceneState.Capture(time));
            if (_last - _start >= 3) Flush();
        }
        private void Completed(int winner) => Finish(true);
        private void RoundStarted(int number,int taya)
        {
            // Flush before MatchPoseHistory's next LateUpdate clears its ring.
            // Polling in Update can miss a transition started by a coroutine.
            Flush();_start=-1;_round=number;_sceneFrames.Clear();_fxFrames.Clear();_lastFx=null;
        }
        private void IntermissionStarted(int number,int taya){Flush();_start=-1;_sceneFrames.Clear();_fxFrames.Clear();_lastFx=null;}
        private void Flush()
        {
            if (_writer == null || _start < 0 || _last <= _start) return;
            if (_writer.CanAppend && _archive.TryCaptureSession(_start, _last, ++_sequence, out var clip, out string error))
            {
                var effects=new LocalReplayFxSegment();
                // Preserve the latest prior state at a segment edge and every
                // actual render-frame transition within the active timeline.
                LocalReplayFxFrame before=null;
                foreach(var frame in _fxFrames)
                {if(frame.Time<clip.Start){before=frame;continue;}if(before!=null){effects.Frames.Add(before);before=null;}if(frame.Time<=clip.End)effects.Frames.Add(frame);}
                if(effects.Frames.Count==0&&before!=null)effects.Frames.Add(before);
                _writer.Append(clip,new LocalReplaySceneSegment{Frames=new System.Collections.Generic.List<LocalReplaySceneFrame>(_sceneFrames)},effects.Frames.Count>0?effects:null);
            }
            else
            {
                _warning = _writer.Error ?? "Some replay footage could not be recorded. " +
                    (_writer.CanAppend ? _archive.SessionCaptureError : "The replay disk writer could not keep up.");
                Debug.LogWarning("[LocalReplay] " + _warning);
            }
            _fxFrames.RemoveAll(frame=>frame.Time<_last);if(_lastFx!=null&&_lastFx.Time<=_last&&(_fxFrames.Count==0||_fxFrames[0]!=_lastFx))_fxFrames.Insert(0,_lastFx);
            _start = _last;
            var last=_sceneFrames.Count>0?_sceneFrames[_sceneFrames.Count-1]:null;
            _sceneFrames.Clear();if(last!=null)_sceneFrames.Add(last);
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
            if(_fx!=null)_fx.FrameRendered-=EffectFrame;_fx=null;_fxFrames.Clear();_lastFx=null;
            _sceneFrames.Clear();
        }
    }
}
