using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.CameraSystem
{
    // Host shortlist survives round history resets. Transport/view code consumes
    // immutable clips and cannot call the match's score or ability entry points.
    public sealed class MatchReplayArchive : MonoBehaviour
    {
        public const int Capacity=3;
        private sealed class Prop
        {
            public GameObject Source;
            public RecordedObjectKind Kind;
            public int Seat,Skin;
            public string Person;
            public MatchPoseHistory.Track Track;
        }
        private struct Pending
        {public float Contact,Start,End;public int Actor,Subject,Importance;public string Reason;}
        public sealed class Retained
        {
            public readonly RecordedMatchClip Clip;
            public readonly byte[] Bytes;
            public readonly int Importance;
            internal Retained(RecordedMatchClip clip,byte[] bytes,int importance){Clip=clip;Bytes=bytes;Importance=importance;}
        }
        private MatchPoseHistory _history;
        private readonly List<Prop> _props=new List<Prop>(9);
        private readonly List<Retained> _clips=new List<Retained>(Capacity);
        private readonly List<Pending> _pending=new List<Pending>(Capacity);
        private long _match,_sequence;
        private int _round;
        private float _unsafeAt=-100;
        public IReadOnlyList<Retained> Clips=>_clips;
        public event Action<Retained> RetainedClip;
        public string LastSkip {get;private set;}
        public void Bind(MatchPoseHistory history)
        {
            if(_history!=null)_history.Sampled-=Sample;
            _history=history;if(_history!=null)_history.Sampled+=Sample;
        }
        private void OnEnable()=>MatchFlair.Presented+=Moment;
        private void OnDisable(){MatchFlair.Presented-=Moment;Bind(null);_props.Clear();_pending.Clear();_clips.Clear();}
        private void Update()=>CheckIdentity();
        private void CheckIdentity()
        {
            var match=GameServices.Match;
            long identity=match!=null&&(match.MatchInProgress||match.RoundNumber>0)?match.PresentationMatchId:0;int round=match!=null?match.RoundNumber:0;
            if(identity!=_match){_match=identity;_sequence=0;_clips.Clear();_pending.Clear();}
            if(round!=_round){_round=round;_props.Clear();_pending.Clear();_unsafeAt=-100;}
        }
        private void Sample(float time)
        {
            CheckIdentity();
            if(!NetAuthority.ShouldResolve()||_history==null||_match<=0||GameServices.Round==null)return;
            if(_props.Count==0)BindProps();
            foreach(var prop in _props)if(prop.Source!=null)prop.Track.Record(time);
            // Do not label a body/prop-only recording as a complete ability clip.
            // The visual-field track joins this same contract next.
            if(UI.SceneFlow.SelectedMode==GameMode.HeroStrike)_unsafeAt=time;
            for(int i=0;i<_pending.Count;)
            {
                var pending=_pending[i];if(time<pending.End){i++;continue;}
                _pending.RemoveAt(i);Retain(pending);
            }
        }
        private void BindProps()
        {
            var round=GameServices.Round;
            foreach(var shoe in FindObjectsByType<Slipper>())
            {
                int seat=shoe.SeatOfOrigin;
                _props.Add(new Prop{Source=shoe.gameObject,Kind=RecordedObjectKind.Slipper,Seat=seat,Skin=shoe.SkinIndex,
                    Track=new MatchPoseHistory.Track(round.PlayerAt(Mathf.Clamp(seat,0,3)),shoe.gameObject)});
            }
            if(round.Lata!=null)_props.Add(new Prop{Source=round.Lata.gameObject,Kind=RecordedObjectKind.Can,Seat=-1,Skin=round.Lata.SkinIndex,
                Track=new MatchPoseHistory.Track(round.PlayerAt(0),round.Lata.gameObject)});
            foreach(var actor in round.Players)
            {
                var pet=actor.GetComponent<CharacterVisual>()?.Companion;if(pet==null)continue;
                _props.Add(new Prop{Source=pet.gameObject,Kind=RecordedObjectKind.Familiar,Seat=actor.PlayerSlot,Skin=0,Person=actor.AbilitySystem?.HeroId,
                    Track=new MatchPoseHistory.Track(actor,pet.gameObject)});
            }
        }
        private void Moment(MatchFlair.Kind kind,int actor,int subject,Vector3 at,float strength)
        {
            CheckIdentity();
            if(!NetAuthority.ShouldResolve()||_match<=0||GameServices.Round?.RoundActive!=true||GameServices.Match.IsWarmupBuffer)return;
            if(kind!=MatchFlair.Kind.Tag&&kind!=MatchFlair.Kind.LataDown)return;
            if(actor<0||actor>=4)return;
            float now=Time.time;
            if(_pending.Any(p=>Mathf.Abs(p.Contact-now)<.03f&&p.Actor==actor&&p.Subject==subject))return;
            if(_pending.Count>=Capacity)_pending.RemoveAt(0);
            _pending.Add(new Pending{Contact=now,Start=now-2,End=now+1.4f,Actor=actor,Subject=subject,
                Importance=kind==MatchFlair.Kind.Tag?2:1,Reason=kind==MatchFlair.Kind.Tag?"CATCH":"CAN KNOCKDOWN"});
        }
        private void Retain(Pending pending)
        {
            if(_unsafeAt>=pending.Start){LastSkip="Ability visual track incomplete for this window";return;}
            var objects=new List<RecordedObjectTrack>(13);var round=GameServices.Round;
            for(int seat=0;seat<4;seat++)
            {
                var actor=round.PlayerAt(seat);var track=_history.ForSeat(seat);
                var pose=track?.Retain(pending.Start,pending.End);
                if(actor==null||pose==null){LastSkip="Incomplete body lead-in or aftermath";return;}
                objects.Add(new RecordedObjectTrack{Kind=RecordedObjectKind.Player,Seat=seat,Skin=actor.CharacterIndex,
                    Person=Roster.PersonIdAt(actor.Mode,actor.CharacterIndex),Pose=pose});
            }
            foreach(var prop in _props)
            {
                var pose=prop.Track.Retain(pending.Start,pending.End);
                if(pose==null){LastSkip="Incomplete prop lead-in or aftermath";return;}
                objects.Add(new RecordedObjectTrack{Kind=prop.Kind,Seat=prop.Seat,Skin=prop.Skin,Person=prop.Person,Pose=pose});
            }
            var clip=new RecordedMatchClip{MatchId=_match,Id=++_sequence,Round=_round,Actor=pending.Actor,Subject=pending.Subject,
                Mode=UI.SceneFlow.SelectedMode,Map=SceneManager.GetActiveScene().name,Reason=pending.Reason,
                Start=pending.Start,End=pending.End,Contact=pending.Contact,Objects=objects.ToArray()};
            try
            {
                var retained=new Retained(clip,clip.Encode(),pending.Importance);
                _clips.Add(retained);_clips.Sort((a,b)=>b.Importance!=a.Importance?b.Importance.CompareTo(a.Importance):b.Clip.Id.CompareTo(a.Clip.Id));
                if(_clips.Count>Capacity)_clips.RemoveAt(_clips.Count-1);
                LastSkip=null;if(_clips.Contains(retained))RetainedClip?.Invoke(retained);
            }
            catch(System.IO.InvalidDataException error){LastSkip=error.Message;}
        }
    }
}
