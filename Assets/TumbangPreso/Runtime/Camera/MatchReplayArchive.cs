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
        {public float Contact,Start,End;public int Actor,Subject,Importance;public string Reason;public Dictionary<MatchPoseHistory.Track,RecordedPoseTrack.Sample> ContactPoses;public RecordedFieldFrame ContactFields;}
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
        private readonly List<RecordedWorldCue> _sounds=new List<RecordedWorldCue>(256);
        private readonly Dictionary<GameObject,int> _fieldIds=new Dictionary<GameObject,int>();
        private readonly List<RecordedFieldFrame> _fields=new List<RecordedFieldFrame>(MatchPoseHistory.Samples);
        private int _fieldSequence;
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
        private void OnEnable(){MatchFlair.Presented+=Moment;AudioDirector.WorldCuePlayed+=RecordSound;}
        private void OnDisable(){MatchFlair.Presented-=Moment;AudioDirector.WorldCuePlayed-=RecordSound;Bind(null);_props.Clear();_pending.Clear();_clips.Clear();}
        private void Update()=>CheckIdentity();
        private void CheckIdentity()
        {
            var match=GameServices.Match;
            long identity=match!=null&&(match.MatchInProgress||match.RoundNumber>0)?match.PresentationMatchId:0;int round=match!=null?match.RoundNumber:0;
            if(identity!=_match){_match=identity;_sequence=0;_clips.Clear();_pending.Clear();_sounds.Clear();}
            if(round!=_round){_round=round;_props.Clear();_pending.Clear();_sounds.Clear();_unsafeAt=-100;_fieldIds.Clear();_fields.Clear();_fieldSequence=0;}
        }
        private void RecordSound(string id,Vector3 position,float pitch,float gain)
        {
            if(!NetAuthority.ShouldResolve()||PresentationClock.Held||GameServices.Round?.RoundActive!=true)return;
            float now=Time.time;_sounds.RemoveAll(c=>c.Time<now-8);
            if(_sounds.Count>=512)_sounds.RemoveAt(0);
            _sounds.Add(new RecordedWorldCue{Time=now,Id=id,Position=position,Pitch=pitch,Gain=gain});
        }
        private void Sample(float time)
        {
            CheckIdentity();
            if(!NetAuthority.ShouldResolve()||_history==null||_match<=0||GameServices.Round==null)return;
            if(_props.Count==0)BindProps();
            foreach(var prop in _props)if(prop.Source!=null)prop.Track.Record(time);
            _fields.Add(CaptureFields(time));if(_fields.Count>MatchPoseHistory.Samples)_fields.RemoveAt(0);
            // Unsupported active performances need their own recorded visual state.
            // Ordinary Hero exchanges and the seven recorded field families are eligible.
            foreach(var actor in GameServices.Round.Players)
            {
                if(actor?.AbilitySystem?.Kit?.Ultimate?.IsWindingUp==true||actor?.AbilitySystem?.Kit?.Ultimate?.IsActive==true)_unsafeAt=time;
            }
            for(int i=0;i<_pending.Count;)
            {
                var pending=_pending[i];if(time<pending.End){i++;continue;}
                _pending.RemoveAt(i);Retain(pending);
            }
        }
        public static string VisualKey(GameObject root)
        {
            // Gameplay can stats rotate with the taya while the installed art stays.
            // Compare the actual stable rendering assets, not the gameplay skin index.
            var text=new System.Text.StringBuilder();
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))
            {
                var filter=transform.GetComponent<MeshFilter>();var skin=transform.GetComponent<SkinnedMeshRenderer>();
                var mesh=filter!=null?filter.sharedMesh:skin!=null?skin.sharedMesh:null;
                if(mesh==null)continue;
                text.Append(transform.name).Append('/').Append(mesh.name).Append(':').Append(mesh.vertexCount).Append(':').Append(mesh.subMeshCount).Append(';');
            }
            using var sha=System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","");
        }
        public static GameObject PropModel(GameObject root)=>root.transform.Find("Visual")?.gameObject??root;
        private RecordedFieldFrame CaptureFields(float time)
        {
            var captured=Net.WorldEffectSnapshot.Capture();
            if(captured.Count>Net.WorldEffectSnapshot.MaxFields){_unsafeAt=time;return new RecordedFieldFrame{Time=time,Fields=Array.Empty<RecordedField>()};}
            var fields=new RecordedField[captured.Count];
            for(int i=0;i<fields.Length;i++)
            {
                var state=captured[i];
                if(!_fieldIds.TryGetValue(state.Source,out int id))_fieldIds[state.Source]=id=++_fieldSequence;
                state.Source=null;fields[i]=new RecordedField{Id=id,State=state};
            }
            return new RecordedFieldFrame{Time=time,Fields=fields};
        }
        private void BindProps()
        {
            var round=GameServices.Round;
            foreach(var shoe in FindObjectsByType<Slipper>())
            {
                int seat=shoe.SeatOfOrigin;
                _props.Add(new Prop{Source=shoe.gameObject,Kind=RecordedObjectKind.Slipper,Seat=seat,Skin=shoe.SkinIndex,
                    Track=new MatchPoseHistory.Track(round.PlayerAt(Mathf.Clamp(seat,0,3)),PropModel(shoe.gameObject))});
            }
            if(round.Lata!=null)_props.Add(new Prop{Source=round.Lata.gameObject,Kind=RecordedObjectKind.Can,Seat=-1,Skin=round.Lata.SkinIndex,
                Track=new MatchPoseHistory.Track(round.PlayerAt(0),PropModel(round.Lata.gameObject))});
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
            var contactPoses=new Dictionary<MatchPoseHistory.Track,RecordedPoseTrack.Sample>();
            for(int seat=0;seat<4;seat++){var track=_history?.ForSeat(seat);if(track!=null)contactPoses[track]=track.Capture(now);}
            foreach(var prop in _props)contactPoses[prop.Track]=prop.Track.Capture(now);
            _pending.Add(new Pending{ContactPoses=contactPoses,ContactFields=CaptureFields(now),Contact=now,Start=now-2,End=now+1.4f,Actor=actor,Subject=subject,
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
                if(pose!=null&&pending.ContactPoses.TryGetValue(track,out var key))pose=pose.WithKey(key);
                if(actor==null||pose==null){LastSkip="Incomplete body lead-in or aftermath";return;}
                objects.Add(new RecordedObjectTrack{Kind=RecordedObjectKind.Player,Seat=seat,Skin=actor.CharacterIndex,
                    Person=Roster.PersonIdAt(actor.Mode,actor.CharacterIndex),VisualKey=VisualKey(track.Source),Pose=pose});
            }
            foreach(var prop in _props)
            {
                var pose=prop.Track.Retain(pending.Start,pending.End);
                if(pose!=null&&pending.ContactPoses.TryGetValue(prop.Track,out var key))pose=pose.WithKey(key);
                if(pose==null){LastSkip=$"Incomplete {prop.Kind} P{prop.Seat+1}: source={prop.Source!=null}, ready={prop.Track.Ready}, recorded={prop.Track.Oldest:F3}..{prop.Track.Newest:F3}, needed={pending.Start:F3}..{pending.End:F3}";return;}
                objects.Add(new RecordedObjectTrack{Kind=prop.Kind,Seat=prop.Seat,Skin=prop.Skin,Person=prop.Person,VisualKey=VisualKey(prop.Track.Source),Pose=pose});
            }
            int from=_fields.FindLastIndex(f=>f.Time<=pending.Start),to=_fields.FindIndex(f=>f.Time>=pending.End);
            if(from<0||to<from){LastSkip="Incomplete field history";return;}
            var frames=_fields.GetRange(from,to-from+1);frames.RemoveAll(f=>Mathf.Abs(f.Time-pending.Contact)<.00001f);
            frames.Add(pending.ContactFields);frames.Sort((a,b)=>a.Time.CompareTo(b.Time));
            var clip=new RecordedMatchClip{MatchId=_match,Id=++_sequence,Round=_round,Actor=pending.Actor,Subject=pending.Subject,
                Mode=UI.SceneFlow.SelectedMode,Map=SceneManager.GetActiveScene().name,Reason=pending.Reason,
                Start=pending.Start,End=pending.End,Contact=pending.Contact,Objects=objects.ToArray(),FieldFrames=frames.ToArray(),
                Sounds=_sounds.Where(c=>c.Time>=pending.Start&&c.Time<=pending.End).ToArray()};
            try
            {
                var retained=new Retained(clip,clip.Encode(),pending.Importance);
                _clips.Add(retained);_clips.Sort((a,b)=>b.Importance!=a.Importance?b.Importance.CompareTo(a.Importance):b.Clip.Id.CompareTo(a.Clip.Id));
                if(_clips.Count>Capacity)_clips.RemoveAt(_clips.Count-1);
                LastSkip=null;if(_clips.Contains(retained)){RetainedClip?.Invoke(retained);Net.MatchRpc.Instance?.StageReplay(retained);}
            }
            catch(System.IO.InvalidDataException error){LastSkip=error.Message;}
        }
    }
}
