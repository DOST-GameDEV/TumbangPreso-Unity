using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class AmbientLife
    {
        [Serializable] public sealed class HabitatNode
        {
            public Vector3 Point;
            public int[] Links=Array.Empty<int>();
        }
        public enum Activity { Transit, Investigate, Watch, Mark, Retreat }
        [Serializable] public sealed class ActivitySite
        {
            public string Name;
            public int Node;
            public Activity Kind;
            public Vector3 LookAt;
            public Vector2 HoldSeconds;
        }
#if UNITY_EDITOR
        public void StageActivityForReview(string id,int site)
        {
            for(int i=0;i<Animals.Length;i++)if(Animals[i].Id==id){_actors[i].StageActivity(site);return;}
            throw new InvalidOperationException("Unknown animal "+id);
        }
#endif

        private sealed partial class Actor
        {
            private bool HasHabitat=>_data.Habitat!=null&&_data.Habitat.Length>1&&_data.Activities!=null&&_data.Activities.Length>1;
            private readonly List<int> _path=new List<int>();
            private int _pathAt,_site=-1,_lastSite=-1,_visits;
            private float _activityTime,_headDip,_reactionQuiet;
            private Activity _activity=Activity.Watch;
            private Transform _head;
            private Transform _tailBase,_tailTip;
            private Quaternion _tailRest,_tipRest;
            private float _tailWait,_tailFlick;

            private void InitializeHabitat(GameObject model)
            {
                if(!HasHabitat)return;
                foreach(var bone in model.GetComponentsInChildren<Transform>())if(bone.name=="Head"){_head=bone;break;}
                _activityTime=_owner.Range(2,5);
                if(_data.QuietCat)
                {
                    foreach(var bone in model.GetComponentsInChildren<Transform>())
                    {if(bone.name=="TailBase")_tailBase=bone;else if(bone.name=="TailTip")_tailTip=bone;}
                    if(_tailBase!=null)_tailRest=_tailBase.localRotation;
                    if(_tailTip!=null)_tipRest=_tailTip.localRotation;
                    _tailWait=_owner.Range(2,6);
                }
            }
            private int NearestNode(Vector3 point)
            {
                int best=0;float distance=float.PositiveInfinity;
                for(int i=0;i<_data.Habitat.Length;i++)
                {float d=(_data.Habitat[i].Point-point).sqrMagnitude;if(d<distance){distance=d;best=i;}}
                return best;
            }
            private bool Intrusion(out Vector3 source)
            {
                source=default;float closest=float.PositiveInfinity;
                if(GameServices.Round==null)return false;
                foreach(var player in GameServices.Round.Players)
                {
                    if(player==null||!player.gameObject.activeInHierarchy)continue;
                    Vector3 delta=_root.position-player.transform.position;
                    if(Mathf.Abs(delta.y)>1.4f)continue;
                    delta.y=0;float distance=delta.magnitude;
                    // A stationary observer at three metres is not a perpetual
                    // emergency. Close personal space or a closing run matters.
                    bool approaching=distance<3&&Vector3.Dot(player.PresentationTravelVelocity,delta.normalized)>1.3f;
                    if((distance<(_data.QuietCat?1.45f:1.65f)||approaching)&&distance<closest){closest=distance;source=player.transform.position;}
                }
                return !float.IsPositiveInfinity(closest);
            }
            private void ChooseActivity()
            {
                float best=float.NegativeInfinity;int chosen=-1;
                for(int i=0;i<_data.Activities.Length;i++)
                {
                    var site=_data.Activities[i];
                    if(i==_site||site.Kind==Activity.Mark&&_peeCooldown>0)continue;
                    float distance=Vector3.Distance(_root.position,_data.Habitat[site.Node].Point);
                    float score=_owner.Range(0,1.4f)-distance*.06f-(i==_lastSite?.8f:0);
                    if(score>best){best=score;chosen=i;}
                }
                if(chosen<0){_activityTime=2;return;}
                _lastSite=_site;_site=chosen;
                PlanPath(_data.Activities[chosen].Node);
                _activity=Activity.Transit;
            }
            private void PlanPath(int destination)
            {
                // If interrupted between nodes, finish that already-safe edge
                // before changing direction. Never cut through a corner to a new
                // nearest waypoint after a scare.
                int start=_pathAt<_path.Count?_path[_pathAt]:NearestNode(_root.position);
                int count=_data.Habitat.Length;var parent=new int[count];var cost=new float[count];var used=new bool[count];
                for(int i=0;i<count;i++){parent[i]=-1;cost[i]=float.PositiveInfinity;}
                cost[start]=0;
                for(int step=0;step<count;step++)
                {
                    int at=-1;float best=float.PositiveInfinity;
                    for(int i=0;i<count;i++)if(!used[i]&&cost[i]<best){best=cost[i];at=i;}
                    if(at<0||at==destination)break;
                    used[at]=true;
                    foreach(int next in _data.Habitat[at].Links)
                    {
                        float candidate=cost[at]+Vector3.Distance(_data.Habitat[at].Point,_data.Habitat[next].Point);
                        if(candidate>=cost[next])continue;
                        cost[next]=candidate;parent[next]=at;
                    }
                }
                _path.Clear();_pathAt=0;
                if(float.IsPositiveInfinity(cost[destination])){_activityTime=3;return;}
                for(int at=destination;at!=-1;at=parent[at])_path.Add(at);
                _path.Reverse();
                if((_data.Habitat[start].Point-_root.position).sqrMagnitude<.0001f)_pathAt=1;
            }
            private void Retreat(Vector3 source)
            {
                if(_peeing)EndDogPause();
                int chosen=NearestNode(_root.position);float best=float.NegativeInfinity;
                for(int i=0;i<_data.Habitat.Length;i++)
                {
                    float travel=Vector3.Distance(_root.position,_data.Habitat[i].Point);
                    if(travel>5.5f)continue;
                    float score=Vector3.Distance(source,_data.Habitat[i].Point)-travel*.15f;
                    if(score>best){best=score;chosen=i;}
                }
                PlanPath(chosen);_activity=Activity.Retreat;_panic=2.4f;_activityTime=0;_reactionQuiet=6;
            }
            private void HabitatAnimal(float dt)
            {
                _peeCooldown=Mathf.Max(0,_peeCooldown-dt);_reactionQuiet=Mathf.Max(0,_reactionQuiet-dt);
                _panic=Mathf.Max(0,_panic-dt);
                if(_reactionQuiet<=0&&Intrusion(out var source))Retreat(source);
                if(_peeing){DogPause(dt);if(!_peeing){_activity=Activity.Watch;_activityTime=2;}return;}
                if(_pathAt<_path.Count)
                {
                    Vector3 target=_data.Habitat[_path[_pathAt]].Point,delta=target-_root.position;
                    if(delta.sqrMagnitude<.000001f){_pathAt++;return;}
                    Vector3 flat=new Vector3(delta.x,0,delta.z);
                    if(flat.sqrMagnitude<.000001f){_pathAt++;return;}
                    Quaternion facing=Quaternion.LookRotation(flat);
                    float angle=Quaternion.Angle(_root.rotation,facing);
                    _root.rotation=Quaternion.RotateTowards(_root.rotation,facing,dt*(_panic>0?260:_data.QuietCat?160:135));
                    float desired=_panic>0?_data.RunSpeed:_data.WalkSpeed;
                    if(_pathAt==_path.Count-1)desired=Mathf.Min(desired,Mathf.Sqrt(2*1.3f*delta.magnitude));
                    desired*=Mathf.InverseLerp(85,15,angle);
                    _speed=Mathf.MoveTowards(_speed,desired,dt*(_panic>0?4:1.1f));
                    _root.position=Vector3.MoveTowards(_root.position,target,_speed*dt);
                    Play(_speed<.035f?"alert":_panic>0?"run":"walk");return;
                }
                _speed=0;
                if(_activity==Activity.Transit)
                {
                    var site=_data.Activities[_site];_activity=site.Kind;_visits++;
                    _activityTime=_data.QuietCat?_owner.Range(_activity==Activity.Watch?7:1.4f,_activity==Activity.Watch?14:3):
                        _owner.Range(_activity==Activity.Watch?5:2.5f,_activity==Activity.Watch?10:5);
                    if(site.HoldSeconds.x>0&&site.HoldSeconds.y>=site.HoldSeconds.x)
                        _activityTime=_owner.Range(site.HoldSeconds.x,site.HoldSeconds.y);
                    if(_activity==Activity.Mark&&_peeCooldown<=0){_peeing=true;_peeTime=0;return;}
                }
                else if(_activity==Activity.Retreat){_activity=Activity.Watch;_activityTime=_owner.Range(2,4);}
                _activityTime-=dt;
                if(_activityTime<=0){ChooseActivity();return;}
                if(_site>=0)
                {
                    Vector3 look=_data.Activities[_site].LookAt-_root.position;look.y=0;
                    if(look.sqrMagnitude>.01f)_root.rotation=Quaternion.RotateTowards(_root.rotation,Quaternion.LookRotation(look),dt*65);
                }
                Play("idle");
            }
            private void PoseActivity(float dt)
            {
                if(_data.QuietCat)PoseQuietTail(dt);
                float target=_activity==Activity.Investigate&&_pathAt>=_path.Count&&!_peeing?(_data.QuietCat?15:24):0;
                _headDip=Mathf.MoveTowards(_headDip,target,dt*45);
                if(_head==null||_headDip<.01f)return;
                float sniff=Mathf.Sin(_activityTime*4.2f)*2*_headDip/24;
                _head.rotation=Quaternion.AngleAxis(_headDip+sniff,_root.right)*_head.rotation;
            }
            private void PoseQuietTail(float dt)
            {
                _tailWait-=dt;
                if(_tailWait<=0){_tailFlick=.65f;_tailWait=_owner.Range(3,8);}
                _tailFlick=Mathf.Max(0,_tailFlick-dt);
                float motion=Mathf.Sin((1-_tailFlick/.65f)*Mathf.PI*2)*4;
                // The tabby's upright silhouette is retained. An occasional
                // tip twitch replaces the source clip's uninterrupted idle wag.
                if(_tailBase!=null)_tailBase.localRotation=Quaternion.Slerp(_tailRest,_tailBase.localRotation,_speed>.05f?.35f:.08f);
                if(_tailTip!=null)
                {_tailTip.localRotation=_tipRest;_tailTip.rotation=Quaternion.AngleAxis(motion,Vector3.up)*_tailTip.rotation;}
            }
#if UNITY_EDITOR
            public void StageActivity(int site)
            {
                _path.Clear();_pathAt=0;_site=site;_panic=0;_speed=0;_reactionQuiet=0;
                _root.position=_data.Habitat[_data.Activities[site].Node].Point;
                _activity=Activity.Transit;_activityTime=0;
                if(_peeing)EndDogPause();
            }
#endif
        }
    }
}
