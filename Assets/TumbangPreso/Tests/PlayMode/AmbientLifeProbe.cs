using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    [Category("WallClock")]
    public sealed class AmbientLifeProbe
    {
        private bool _bots,_spectator,_pinned;private int _seat,_quality;private CustomRules _rules;
        private static string Output=>Environment.GetEnvironmentVariable("TUMP_AMBIENT_REVIEW")??"Logs/ambient-life-v1";
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;_quality=GraphicsProfiles.Current;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();yield return PlayModeWorld.Reset();Directory.CreateDirectory(Output);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GraphicsProfiles.Apply(_quality);
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest,Timeout(300000)]
        public IEnumerator OriginalAnimalsRenderAnimateAndReactOnTheirMaps()
        {
            var report=new StringBuilder("map,id,width,height,depth,start_x,start_y,start_z,end_x,end_y,end_z,travel,player_x,player_y,player_z,state\n");
            try
            {
                foreach(string map in new[]{SceneFlow.Eskinita,SceneFlow.BayanPlaza,SceneFlow.IlalimNgTulay,SceneFlow.SaBubong})
                {
                    yield return MapRetrievalProbe.Load(map);GraphicsProfiles.Apply(1);Time.timeScale=1;GameServices.Round.BeginRound();
                    var life=Object.FindFirstObjectByType<AmbientLife>();Assert.IsNotNull(life,map+" has no ambient integration");
                    Assert.AreEqual(map==SceneFlow.SaBubong?3:5,life.Animals.Length);
                    var who=GameServices.Round.PlayerAt(1);var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
                    var witness=new GameObject("Ambient review camera").AddComponent<Camera>();
                    witness.enabled=false;witness.nearClipPlane=.04f;witness.farClipPlane=400;witness.fieldOfView=52;
                    witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    try
                    {
                        foreach(var spec in life.Animals.Where(a=>!a.Bird))
                        {
                            var animal=life.transform.Find("Ambient "+spec.Id);Assert.IsNotNull(animal);
                            var start=animal.position;
                            var renderers=animal.GetComponentsInChildren<Renderer>();Assert.IsNotEmpty(renderers);
                            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                            Assert.That(bounds.size.y,Is.InRange(.35f,1.1f),"Animal scale is outside the intended street cast relationship");
                            var inward=new Vector3(-Mathf.Sign(start.x),0,0);
                            var playerAt=start+inward*3.8f;playerAt.y=Slipper.GroundY(playerAt);who.Teleport(playerAt);
                            var view=start+animal.forward*2+inward*.9f+Vector3.up*.9f;
                            witness.transform.SetPositionAndRotation(view,Quaternion.LookRotation(start+Vector3.up*.4f-view));
                            yield return GameplayShots.Render(witness,map+"-"+spec.Id+"-context",false,Output);
                            var input=who.gameObject.AddComponent<ApproachInput>();who.Intent.Parked=false;input.Move=-new Vector2(inward.x,inward.z);
                            // Carrying has acceleration and movement penalties.
                            // A fixed .4s approach stopped3.24m away and never
                            // entered the animal's3m response radius.
                            float until=Time.time+3;
                            while(Time.time<until&&Vector3.Distance(who.transform.position,start)>2.45f)yield return null;
                            input.Move=Vector2.zero;
                            Assert.Less(Vector3.Distance(who.transform.position,start),2.7f,"The staged player approach never reached the response zone");
                            yield return new WaitForSeconds(1.6f);input.enabled=false;Object.Destroy(input);who.Intent.Clear();
                            var end=animal.position;
                            var player=who.transform.position;
                            report.AppendLine(FormattableString.Invariant($"{map},{spec.Id},{bounds.size.x:F3},{bounds.size.y:F3},{bounds.size.z:F3},{start.x:F3},{start.y:F3},{start.z:F3},{end.x:F3},{end.y:F3},{end.z:F3},{Vector3.Distance(start,end):F3},{player.x:F3},{player.y:F3},{player.z:F3},{life.DescribeForReview(spec.Id)}"));
                            Assert.Greater(Vector3.Distance(start,end),.7f,map+"/"+spec.Id+" did not move away during the approach; "+life.DescribeForReview(spec.Id)+" player="+player);
                            yield return GameplayShots.Render(rig.Camera,map+"-"+spec.Id+"-owner-after-approach",false,Output);
                        }
                        // Stage each bird for art and proximity evidence; this does
                        // not claim to test the random arrival distribution.
                        foreach(var spec in life.Animals.Where(a=>a.Bird))
                        {
                            who.Teleport(new Vector3(0,Slipper.GroundY(new Vector3(0,0,-10)),-10));
                            life.StageBirdVisitForReview(spec.Id);yield return new WaitForSeconds(.1f);
                            var animal=life.transform.Find("Ambient "+spec.Id);Assert.IsNotNull(animal);
                            Assert.IsNotEmpty(animal.GetComponentsInChildren<SkinnedMeshRenderer>(true));
                            var target=animal.position;
                            var view=target+animal.forward*1.5f+Vector3.right+Vector3.up*.8f;
                            witness.transform.SetPositionAndRotation(view,Quaternion.LookRotation(target+Vector3.up*.12f-view));
                            yield return GameplayShots.Render(witness,map+"-"+spec.Id+"-visit",false,Output);
                            who.Teleport(target+Vector3.right*1.5f);
                            yield return new WaitForSeconds(.55f);
                            Assert.Greater(Vector3.Distance(animal.position,target),.7f,map+"/"+spec.Id+" did not fly away from the nearby player");
                        }
                    }
                    finally{Object.Destroy(witness.gameObject);}
                    yield return PlayModeWorld.Reset();
                }
            }
            finally{File.WriteAllText(Path.Combine(Output,"animals.csv"),report.ToString());}
        }
        [UnityTest,Timeout(240000)]
        public IEnumerator GroundAnimalGaitsAndStyleAtOrdinarySpeed()
        {
            var report=new StringBuilder("map,id,maximum_leg_rotation,minimum_ground_gap,maximum_ground_gap\n");
            try
            {
                foreach(string map in new[]{SceneFlow.Eskinita,SceneFlow.BayanPlaza,SceneFlow.IlalimNgTulay})
                {
                    yield return MapRetrievalProbe.Load(map);GraphicsProfiles.Apply(1);Time.timeScale=1;GameServices.Round.BeginRound();
                    var life=Object.FindFirstObjectByType<AmbientLife>();var who=GameServices.Round.PlayerAt(1);
                    foreach(var spec in life.Animals.Where(a=>!a.Bird))
                    {
                        var animal=life.transform.Find("Ambient "+spec.Id);
                        var leg=animal.GetComponentsInChildren<Transform>().First(t=>t.name=="FrontL");
                        var rest=leg.localRotation;float motion=0,minGap=float.PositiveInfinity,maxGap=float.NegativeInfinity;
                        who.Teleport(new Vector3(0,0,-10));
                        var camera=new GameObject("Animal motion camera").AddComponent<Camera>();
                        camera.enabled=false;camera.fieldOfView=52;camera.nearClipPlane=.04f;camera.farClipPlane=400;
                        camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                        var follow=camera.gameObject.AddComponent<AnimalCamera>();follow.Target=animal;
                        var mesh=new Mesh();float next=0;bool approached=false;
                        var privateArms=Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None).SelectMany(a=>a.GetComponentsInChildren<Renderer>()).ToArray();
                        var enabled=privateArms.Select(r=>r.enabled).ToArray();foreach(var r in privateArms)r.enabled=false;
                        follow.Sample=()=>
                        {
                            motion=Mathf.Max(motion,Quaternion.Angle(rest,leg.localRotation));
                            if(Time.time<next)return;next=Time.time+.05f;
                            float bottom=float.PositiveInfinity;
                            foreach(var skin in animal.GetComponentsInChildren<SkinnedMeshRenderer>())
                            {skin.BakeMesh(mesh);foreach(var v in mesh.vertices)bottom=Mathf.Min(bottom,skin.transform.TransformPoint(v).y);}
                            // A short support ray cannot select an overhead awning.
                            float ground=animal.position.y;
                            foreach(var hit in Physics.RaycastAll(animal.position+Vector3.up*.25f,Vector3.down,.6f,~0,QueryTriggerInteraction.Ignore))
                                if(hit.collider.GetComponentInParent<CharacterMotor>()==null&&hit.normal.y>.7f)ground=Mathf.Max(ground,hit.point.y);
                            float gap=bottom-ground;
                            minGap=Mathf.Min(minGap,gap);maxGap=Mathf.Max(maxGap,gap);
                        };
                        try
                        {
                            yield return ImprovementEvidenceProbe.Record(camera,map+"-"+spec.Id+"-motion",5,null,t=>
                            {
                                if(t>.7f&&!approached){who.Teleport(animal.position+Vector3.right*2);approached=true;}
                            });
                            report.AppendLine(FormattableString.Invariant($"{map},{spec.Id},{motion:F3},{minGap:F4},{maxGap:F4}"));
                            Assert.Greater(motion,10,spec.Id+" slides along its route without animating its legs");
                            Assert.Greater(minGap,-.045f,spec.Id+" feet sink through the route support");
                            Assert.Less(minGap,.025f,spec.Id+" floats for its entire gait");
                        }
                        finally
                        {
                            foreach(var pair in privateArms.Select((r,i)=>(r,i)))if(pair.r!=null)pair.r.enabled=enabled[pair.i];
                            Object.Destroy(mesh);Object.Destroy(camera.gameObject);
                        }
                    }
                    yield return PlayModeWorld.Reset();
                }
            }
            finally{File.WriteAllText(Path.Combine(Output,"gait-contact.csv"),report.ToString());}
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator DogSurfacePauseFacesRaisedLegAndInterruptsForPlayers()
        {
            var report=new StringBuilder("map,id,leg_rotation,surface_side_dot,state_after_approach\n");
            try
            {
                foreach(string map in new[]{SceneFlow.Eskinita,SceneFlow.BayanPlaza,SceneFlow.IlalimNgTulay})
                {
                    yield return MapRetrievalProbe.Load(map);GraphicsProfiles.Apply(1);Time.timeScale=1;GameServices.Round.BeginRound();
                    var life=Object.FindFirstObjectByType<AmbientLife>();var who=GameServices.Round.PlayerAt(1);
                    var spec=life.Animals.Single(a=>!a.Bird&&a.Id.StartsWith("aspin",StringComparison.Ordinal));
                    Assert.GreaterOrEqual(spec.PeeWaypoint,0,map+" dog has no sensible surface pause");
                    var animal=life.transform.Find("Ambient "+spec.Id);
                    var leg=animal.GetComponentsInChildren<Transform>().First(t=>t.name=="BackR");
                    var rest=leg.localRotation;
                    foreach(var player in GameServices.Round.Players)if(player!=null)player.Teleport(new Vector3(0,0,-10));
                    life.StageDogPauseForReview(spec.Id);
                    float deadline=Time.time+3;
                    while(Time.time<deadline&&!life.DescribeForReview(spec.Id).StartsWith("pee|",StringComparison.Ordinal))yield return null;
                    yield return new WaitForSeconds(.8f);
                    var arc=animal.GetComponentInChildren<LineRenderer>();Assert.IsNotNull(arc);
                    Assert.IsTrue(arc.enabled,"The staged leg lift did not reach its brief middle phase");
                    float angle=Quaternion.Angle(rest,leg.localRotation);Assert.Greater(angle,20,"The dog never lifted its hind leg");
                    var side=leg.position-animal.position;side.y=0;
                    var target=spec.PeeTarget-animal.position;target.y=0;
                    float dot=Vector3.Dot(side.normalized,target.normalized);Assert.Greater(dot,0,"The lifted leg faces away from its surface");
                    var camera=new GameObject("Dog pause review camera").AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=52;
                    camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    var view=animal.position+animal.forward*1.9f-target.normalized*1.5f+Vector3.up*.9f;
                    camera.transform.SetPositionAndRotation(view,Quaternion.LookRotation(animal.position+Vector3.up*.35f-view));
                    yield return GameplayShots.Render(camera,map+"-dog-surface-pause",false,Output);Object.Destroy(camera.gameObject);
                    var start=animal.position;who.Teleport(start-target.normalized*1.5f);
                    yield return new WaitForSeconds(.2f);
                    Assert.IsFalse(arc.enabled,"Surface arc persists after a person approaches");
                    StringAssert.Contains("peeing=False",life.DescribeForReview(spec.Id));
                    yield return new WaitForSeconds(1.2f);
                    Assert.Greater(Vector3.Distance(start,animal.position),.6f,"Interrupted dog did not retreat");
                    report.AppendLine(FormattableString.Invariant($"{map},{spec.Id},{angle:F2},{dot:F3},{life.DescribeForReview(spec.Id)}"));
                    yield return PlayModeWorld.Reset();
                }
            }
            finally{File.WriteAllText(Path.Combine(Output,"dog-surface-pause.csv"),report.ToString());}
        }
        [DefaultExecutionOrder(9999)] private sealed class AnimalCamera:MonoBehaviour
        {
            public Transform Target;public Action Sample;
            private void LateUpdate()
            {
                if(Target==null)return;
                var at=Target.position+new Vector3(1.7f,.9f,1.7f);
                transform.SetPositionAndRotation(at,Quaternion.LookRotation(Target.position+Vector3.up*.4f-at));Sample?.Invoke();
            }
        }
        [DefaultExecutionOrder(-300)] private sealed class ApproachInput:MonoBehaviour
        {
            public Vector2 Move;
            private void Update(){var who=GetComponent<CharacterMotor>();who.Intent.Move=Move;}
        }
    }
}
