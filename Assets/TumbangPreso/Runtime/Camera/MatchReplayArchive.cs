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
            public readonly List<(float time,bool visible)> Visibility=new List<(float,bool)>();
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
        private readonly List<Prop> _desiredProps=new List<Prop>(13);
        private readonly List<Retained> _clips=new List<Retained>(Capacity);
        private readonly List<Pending> _pending=new List<Pending>(Capacity);
        private readonly List<RecordedWorldCue> _sounds=new List<RecordedWorldCue>(256);
        private readonly Dictionary<GameObject,int> _fieldIds=new Dictionary<GameObject,int>();
        private readonly List<RecordedFieldFrame> _fields=new List<RecordedFieldFrame>(MatchPoseHistory.Samples);
        private int _fieldSequence;
        private long _match,_sequence;
        private int _round;
        private float _unsafeAt=-100,_propsScanAt;
        private LocalReplayRecorder _local;
        public string SessionCaptureError { get; private set; }
        public IReadOnlyList<Retained> Clips=>_clips;
        public event Action<Retained> RetainedClip;
        public string LastSkip {get;private set;}
        public void Bind(MatchPoseHistory history)
        {
            _local ??= new LocalReplayRecorder(this);
            if(GetComponent<LocalReplayScenerySampler>()==null)gameObject.AddComponent<LocalReplayScenerySampler>();
            if(_history!=null)_history.Sampled-=Sample;
            _history=history;if(_history!=null&&isActiveAndEnabled)_history.Sampled+=Sample;
        }
        private void OnEnable(){Bind(_history);MatchFlair.Presented+=Moment;AudioDirector.WorldCuePlayed+=RecordSound;}
        private void OnDisable()
        {
            _local?.Finish(false);
            MatchFlair.Presented-=Moment;AudioDirector.WorldCuePlayed-=RecordSound;
            // Keep the selected history owner for recovery, but consume no samples
            // while disabled. A new capture window must not bridge the missing time.
            if(_history!=null)_history.Sampled-=Sample;
            _props.Clear();_desiredProps.Clear();_pending.Clear();_clips.Clear();
            _sounds.Clear();_fields.Clear();_fieldIds.Clear();_fieldSequence=0;
            _match=0;_round=0;_unsafeAt=-100;_propsScanAt=0;
        }
        private void Update(){_local?.Tick();CheckIdentity();}
        internal void CaptureSceneryRenderFrame(float time)=>_local?.SceneryFrame(time);
        private void CheckIdentity()
        {
            var match=GameServices.Match;
            long identity=match!=null&&(match.MatchInProgress||match.RoundNumber>0)?match.PresentationMatchId:0;int round=match!=null?match.RoundNumber:0;
            bool changedMatch=identity!=_match;
            if(changedMatch){_match=identity;_sequence=0;_clips.Clear();}
            if(changedMatch||round!=_round){_round=round;_props.Clear();_desiredProps.Clear();_pending.Clear();_sounds.Clear();_unsafeAt=-100;_propsScanAt=0;_fieldIds.Clear();_fields.Clear();_fieldSequence=0;}
        }
        private void RecordSound(string id,Vector3 position,float pitch,float gain)
        {
            if(PresentationClock.Held||GameServices.Round?.RoundActive!=true)return;
            float now=Time.time;_sounds.RemoveAll(c=>c.Time<now-8);
            if(_sounds.Count>=RecordedMatchClip.SoundCueLimit)_sounds.RemoveAt(0);
            _sounds.Add(new RecordedWorldCue{Time=now,Id=id,Position=position,Pitch=pitch,Gain=gain});
        }
        private void Sample(float time)
        {
            CheckIdentity();
            if(_history==null||_match<=0||GameServices.Round==null)return;
            if(_props.Count==0||time>=_propsScanAt){_propsScanAt=time+.2f;BindProps();}
            foreach(var prop in _props)if(prop.Source!=null)
            {prop.Track.Record(time);prop.Visibility.Add((time,prop.Source.activeInHierarchy));if(prop.Visibility.Count>MatchPoseHistory.Samples)prop.Visibility.RemoveAt(0);}
            _fields.Add(CaptureFields(time));if(_fields.Count>MatchPoseHistory.Samples)_fields.RemoveAt(0);
            // The shipped kits have recorded body/prop, status, weather,
            // persistent field and distinctive held/flight presentation paths.
            // Unknown future kits require a coverage decision before nomination.
            foreach(var actor in GameServices.Round.Players)
            {
                var kit=actor?.AbilitySystem?.Kit;if(kit==null)continue;
                string hero=kit.HeroId;
                if(hero!="sean"&&hero!="zack"&&hero!="nemu"&&hero!="phaister"&&hero!="cheska"&&hero!="dante"&&hero!="rafi"&&hero!="amihan"&&hero!="paete")_unsafeAt=time;
            }
            for(int i=0;i<_pending.Count;)
            {
                var pending=_pending[i];if(time<pending.End){i++;continue;}
                _pending.RemoveAt(i);Retain(pending);
            }
            _local?.Sample(time);
        }
        public bool TryCaptureSession(float start,float end,long sequence,out RecordedMatchClip clip,out string error,bool captureEndpoint=false)
        {
            clip=null;error=null;
            // A closing local segment may extend beyond the scheduled pose
            // sample only by capturing the actual current state. Do not append
            // to the shared catch-history ring or relabel a stale pose.
            if(captureEndpoint&&(end!=Time.time||GameServices.Match?.PresentationMatchId!=_match||GameServices.Match.RoundNumber!=_round))
                error="The closing replay identity changed.";
            if(_unsafeAt>=start)error="The recorded world changed within this segment.";
            var objects=new List<RecordedObjectTrack>(13);
            var round=GameServices.Round;
            for(int seat=0;error==null&&seat<4;seat++)
            {
                var track=_history?.ForSeat(seat);var actor=round?.PlayerAt(seat);
                var pose=RetainSession(track,start,end,captureEndpoint);
                if(actor==null||pose==null){error="Player pose history is incomplete.";break;}
                objects.Add(new RecordedObjectTrack{Kind=RecordedObjectKind.Player,Seat=seat,Skin=actor.CharacterIndex,
                    Person=Roster.PersonIdAt(actor.Mode,actor.CharacterIndex),DisplayName=UI.SeatLabel.Raw(seat),
                    VisualKey=VisualKey(track.Source),Pose=pose});
            }
            foreach(var prop in _props)
            {
                if(error!=null)break;
                var pose=RetainProp(prop,start,end,captureEndpoint);
                if(pose==null){error="Prop pose history is incomplete.";break;}
                objects.Add(new RecordedObjectTrack{Kind=prop.Kind,Seat=prop.Seat,Skin=prop.Skin,Person=prop.Person,
                    VisualKey=VisualKey(prop.Track.Source),Pose=pose});
            }
            int from=_fields.FindLastIndex(f=>f.Time<=start),to=_fields.FindIndex(f=>f.Time>=end);
            if(captureEndpoint&&to<0)to=_fields.Count-1;
            if(error==null&&(from<0||to<from))error="World history is incomplete.";
            SessionCaptureError=error;
            if(error!=null)return false;
            var fields=_fields.GetRange(from,to-from+1);
            if(captureEndpoint&&fields[fields.Count-1].Time<end)fields.Add(CaptureFields(end));
            if(_unsafeAt>=start){SessionCaptureError=error="The recorded world changed within this segment.";return false;}
            clip=new RecordedMatchClip{MatchId=_match,Id=sequence,Round=_round,Actor=0,Subject=-1,
                Mode=UI.SceneFlow.SelectedMode,Map=SceneManager.GetActiveScene().name,Reason="MATCH",
                Start=start,End=end,Contact=start,Objects=objects.ToArray(),FieldFrames=fields.ToArray(),
                Sounds=_sounds.Where(c=>(sequence==1?c.Time>=start:c.Time>start)&&c.Time<=end).ToArray()};
            return true;
        }
        private static RecordedPoseTrack RetainSession(MatchPoseHistory.Track track,float start,float end,bool captureEndpoint)
        {
            if(track==null)return null;
            if(!captureEndpoint||end<=track.Newest)return track.Retain(start,end);
            float last=track.Newest;
            float lead=start==last?Mathf.Max(track.Oldest,last-MatchPoseHistory.Interval):start;
            var pose=track.Retain(lead,last);var endpoint=track.Capture(end);
            if(pose==null||endpoint==null)return null;
            var samples=new RecordedPoseTrack.Sample[pose.Samples.Length+1];
            Array.Copy(pose.Samples,samples,pose.Samples.Length);samples[samples.Length-1]=endpoint;
            return new RecordedPoseTrack(pose.Paths,samples);
        }
        private static RecordedPoseTrack RetainProp(Prop prop,float start,float end,bool captureEndpoint=false)
        {
            var pose=RetainSession(prop.Track,start,end,captureEndpoint);if(pose==null)return null;
            foreach(var sample in pose.Samples)
            {
                int at=prop.Visibility.FindLastIndex(v=>v.time<=sample.Time+.00001f);
                if(sample.Active.Length>0)
                {if(captureEndpoint&&sample.Time==end)sample.Active[0]&=prop.Source!=null&&prop.Source.activeInHierarchy;
                 else if(at>=0)sample.Active[0]&=prop.Visibility[at].visible;}
            }
            return pose;
        }
        public static string VisualKey(GameObject root)
        {
            // Gameplay can stats rotate with the taya while the installed art stays.
            // Compare the actual stable rendering assets, not the gameplay skin index.
            var text=new System.Text.StringBuilder();
            foreach(var transform in MatchPoseHistory.StableTransforms(root))
            {
                var filter=transform.GetComponent<MeshFilter>();var skin=transform.GetComponent<SkinnedMeshRenderer>();
                var mesh=filter!=null?filter.sharedMesh:skin!=null?skin.sharedMesh:null;
                if(mesh==null)continue;
                text.Append(transform==root.transform?"":transform.name).Append('/').Append(mesh.name).Append(':').Append(mesh.vertexCount).Append(':').Append(mesh.subMeshCount).Append(';');
            }
            using var sha=System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","");
        }
        public static GameObject PropModel(GameObject root)=>root.transform.Find("Visual")?.gameObject??root;
        private RecordedFieldFrame CaptureFields(float time)
        {
            var captured=RecordedSpecialFields.Capture();
            if(captured.Count>Net.WorldEffectSnapshot.MaxFields){_unsafeAt=time;return new RecordedFieldFrame{Time=time,Fields=Array.Empty<RecordedField>()};}
            var fields=new RecordedField[captured.Count];
            for(int i=0;i<fields.Length;i++)
            {
                var state=captured[i];
                if(!_fieldIds.TryGetValue(state.Source,out int id))_fieldIds[state.Source]=id=++_fieldSequence;
                state.Source=null;fields[i]=new RecordedField{Id=id,State=state};
            }
            return new RecordedFieldFrame{Time=time,Fields=fields,Lighting=RecordedEnvironment.Capture(),Trails=RecordedTrail.Capture()};
        }
        private void BindProps()
        {
            var round=GameServices.Round;_desiredProps.Clear();bool changed=false;
            foreach(var shoe in FindObjectsByType<Slipper>(FindObjectsInactive.Include))AddProp(round,shoe.gameObject,RecordedObjectKind.Slipper,shoe.SeatOfOrigin,shoe.SkinIndex,null,PropModel(shoe.gameObject),ref changed);
            if(round.Lata!=null)AddProp(round,round.Lata.gameObject,RecordedObjectKind.Can,-1,round.Lata.SkinIndex,null,PropModel(round.Lata.gameObject),ref changed);
            foreach(var actor in round.Players)
            {
                var pet=actor.GetComponent<CharacterVisual>()?.Companion;if(pet==null)continue;
                AddProp(round,pet.gameObject,RecordedObjectKind.Familiar,actor.PlayerSlot,0,actor.AbilitySystem?.HeroId,pet.gameObject,ref changed);
            }
            if(_props.Count>0&&(changed||_desiredProps.Count!=_props.Count))_unsafeAt=Time.time;
            _props.Clear();_props.AddRange(_desiredProps);
            _desiredProps.Clear();
        }
        private void AddProp(RoundDirector round,GameObject source,RecordedObjectKind kind,int seat,int skin,string person,GameObject model,ref bool changed)
        {
            Prop entry=null;
            for(int i=0;i<_props.Count;i++)
                if(_props[i].Source==source&&_props[i].Track.Source==model){entry=_props[i];break;}
            if(entry==null){changed=true;entry=new Prop{Source=source,Kind=kind,Seat=seat,Skin=skin,Person=person,
                Track=new MatchPoseHistory.Track(round.PlayerAt(Mathf.Clamp(seat,0,3)),model)};}
            _desiredProps.Add(entry);
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
                    Person=Roster.PersonIdAt(actor.Mode,actor.CharacterIndex),DisplayName=UI.SeatLabel.Raw(seat),VisualKey=VisualKey(track.Source),Pose=pose});
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
