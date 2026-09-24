using System;
using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class AmbientLife
    {
#if UNITY_EDITOR
        public void StageBirdArrivalForReview(string id,int perch=0)
        {
            for(int i=0;i<Animals.Length;i++)if(Animals[i].Id==id){_actors[i].StageArrival(perch);return;}
            throw new InvalidOperationException("Unknown visiting bird "+id);
        }
#endif
        private sealed partial class Actor
        {
            private bool HasVisits=>_data.Bird&&!_data.AerialWander&&_data.Perches!=null&&_data.Perches.Length>1;
            private int _perch=-1,_landings;
            private Transform _visitTail;
            private float _tailVisitWait,_tailVisitTime;
            private void InitializeVisits(GameObject model)
            {
                if(!HasVisits||!_data.FanWatch)return;
                foreach(var bone in model.GetComponentsInChildren<Transform>())if(bone.name=="Tail"){_visitTail=bone;break;}
                _tailVisitWait=_owner.Range(1,3);
            }
            private bool PerchOccupied(Vector3 point,out Vector3 source)
            {
                source=default;
                if(GameServices.Round==null)return false;
                foreach(var player in GameServices.Round.Players)
                {
                    if(player==null||!player.gameObject.activeInHierarchy)continue;
                    var at=player.transform.position+Vector3.up*.65f;
                    if((at-point).sqrMagnitude<2.8f*2.8f){source=at;return true;}
                }
                return false;
            }
            private bool TryVisit()
            {
                int start=_owner._random.Next(_data.Perches.Length);
                for(int i=0;i<_data.Perches.Length;i++)
                {
                    int next=(start+i)%_data.Perches.Length;
                    if(next==_perch||PerchOccupied(_data.Perches[next],out _))continue;
                    ArriveVisit(next);return true;
                }
                return false;
            }
            private void ArriveVisit(int perch)
            {
                _perch=perch;var target=_data.Perches[perch];
                var approach=Quaternion.Euler(0,_owner.Range(0,360),0)*Vector3.forward;
                if(_data.VisitAxis.sqrMagnitude>.01f)approach=_data.VisitAxis.normalized*(_owner._random.NextDouble()<.5?-1:1);
                _flightFrom=VisitAirPoint(target+approach*_owner.Range(25,34)+Vector3.up*_owner.Range(7,10));
                _flightTo=target;
                var side=Vector3.Cross(Vector3.up,approach)*_owner.Range(-3,3);
                _flightControl1=VisitAirPoint(Vector3.Lerp(_flightFrom,target,.5f)+side+Vector3.up*1.4f);
                _flightControl2=VisitAirPoint(target+approach*1.2f+Vector3.up*.6f);
                _flightDuration=Vector3.Distance(_flightFrom,target)/_owner.Range(4.6f,5.6f);
                _flightTime=0;_birdPhase=1;_bank=0;
                _root.position=_flightFrom;_root.rotation=Quaternion.LookRotation((_flightControl1-_flightFrom).normalized);
                _root.gameObject.SetActive(true);Play("fly");
            }
            private void DepartVisit(Vector3? disturbance=null)
            {
                Vector3 away;
                if(disturbance.HasValue){away=_root.position-disturbance.Value;away.y=0;}
                else away=Quaternion.Euler(0,_owner.Range(-100,100),0)*_root.forward;
                away.y=0;
                if(away.sqrMagnitude<.01f)away=Vector3.forward;
                if(_data.VisitAxis.sqrMagnitude>.01f)
                {
                    var axis=_data.VisitAxis.normalized;float side=Vector3.Dot(away,axis);
                    if(Mathf.Abs(side)<.15f)side=_owner._random.NextDouble()<.5?-1:1;
                    away=axis*Mathf.Sign(side);
                }
                away.Normalize();float distance=_owner.Range(32,40);
                _flightFrom=_root.position;_flightTo=VisitAirPoint(_flightFrom+away*distance+Vector3.up*_owner.Range(8,12));
                _flightControl1=VisitAirPoint(_flightFrom+away*distance*.26f+Vector3.up*3.4f);
                _flightControl2=VisitAirPoint(_flightTo-away*7+Vector3.up*1.2f);
                _flightDuration=Vector3.Distance(_flightFrom,_flightTo)/_owner.Range(4.8f,6.0f);
                _flightTime=0;_birdPhase=3;_bank=0;Play("fly");
            }
            private Vector3 VisitAirPoint(Vector3 point)
            {
                // An authored underpass uses its open centre lane and ceiling.
                // Empty bounds preserve the open-sky map visits exactly.
                var bounds=_data.FlightBounds;if(bounds.size.y<=0)return point;
                return new Vector3(Mathf.Clamp(point.x,bounds.min.x,bounds.max.x),
                    Mathf.Clamp(point.y,bounds.min.y,bounds.max.y),Mathf.Clamp(point.z,bounds.min.z,bounds.max.z));
            }
            private void VisitingBird(float dt)
            {
                if(_birdPhase==0)
                {
                    _wait-=dt;if(_wait>0)return;
                    if(_owner._random.NextDouble()>.55||!TryVisit()){_wait=_owner.Range(7,25);return;}
                }
                if(_birdPhase==1&&(PerchOccupied(_flightTo,out var blocker)||Threat(out blocker)))DepartVisit(blocker);
                if(_birdPhase==1||_birdPhase==3)
                {
                    _flightTime+=dt;float raw=Mathf.Clamp01(_flightTime/_flightDuration);
                    // Arrival eases into contact. Departure keeps travelling far
                    // beyond the court, instead of hiding at a nearby air point.
                    float t=_birdPhase==1?1-Mathf.Pow(1-raw,1.35f):raw,u=1-t;
                    var next=u*u*u*_flightFrom+3*u*u*t*_flightControl1+3*u*t*t*_flightControl2+t*t*t*_flightTo;
                    var tangent=3*u*u*(_flightControl1-_flightFrom)+6*u*t*(_flightControl2-_flightControl1)+3*t*t*(_flightTo-_flightControl2);
                    _speed=Vector3.Distance(next,_root.position)/Mathf.Max(dt,.0001f);_root.position=next;
                    if(tangent.sqrMagnitude>.00001f)
                    {
                        var flat=new Vector3(tangent.x,0,tangent.z).normalized;
                        float settle=_birdPhase==1?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,1,raw)):0;
                        var direction=Vector3.Slerp(tangent.normalized,flat,settle);
                        float turn=Vector3.SignedAngle(_root.forward,flat,Vector3.up);
                        _bank=Mathf.MoveTowards(_bank,Mathf.Clamp(-turn,-18,18)*(1-settle),dt*70);
                        var facing=Quaternion.LookRotation(direction,Vector3.up)*Quaternion.AngleAxis(_bank,Vector3.forward);
                        _root.rotation=Quaternion.Slerp(_root.rotation,facing,1-Mathf.Exp(-dt*12));
                    }
                    if(_birdPhase==1&&raw>.94f)Play("idle");
                    if(raw<1)return;
                    if(_birdPhase==3){_root.gameObject.SetActive(false);_birdPhase=0;_wait=_owner.Range(10,35);return;}
                    _birdPhase=2;_landings++;_speed=0;_wait=_owner.Range(_data.PerchWait.x,_data.PerchWait.y);
                    _fidgetWait=_owner.Range(1,3);Play("idle");return;
                }
                _wait-=dt;
                if(Threat(out var threat)){DepartVisit(threat);return;}
                if(_wait<=0){DepartVisit();return;}
                _fidgetWait-=dt;
                if(_fidgetWait<=0)
                {
                    bool peck=!_data.FanWatch&&_action!="peck"&&_owner._random.NextDouble()<_data.PeckChance;
                    Play(peck?"peck":"idle");_fidgetWait=peck?_owner.Range(.8f,1.4f):_owner.Range(1.5f,4);
                }
            }
            private void PoseVisit(float dt)
            {
                if(_visitTail==null||_birdPhase!=2)return;
                _tailVisitWait-=dt;
                if(_tailVisitWait<=0){_tailVisitTime=.65f;_tailVisitWait=_owner.Range(1.8f,4.5f);}
                _tailVisitTime=Mathf.Max(0,_tailVisitTime-dt);
                float flick=Mathf.Sin((1-_tailVisitTime/.65f)*Mathf.PI*2)*12;
                // The source fan is one bone, so this is a tail flick rather
                // than a claim that its individual feathers open and close.
                _visitTail.rotation=Quaternion.AngleAxis(flick,_root.up)*_visitTail.rotation;
            }
#if UNITY_EDITOR
            public void StageArrival(int perch)
            {
                if(HasVisits){ArriveVisit(perch);return;}
                _root.position=_data.Route[0];BeginFlight(_data.Route[0],_data.Route[1],1);
            }
#endif
        }
    }
}
