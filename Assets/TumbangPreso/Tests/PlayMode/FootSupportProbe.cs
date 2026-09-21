using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    [Category("WallClock")]
    public sealed class FootSupportProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else{SceneFlow.AdoptRemoteRules(_rules);SceneFlow.UnpinSelectedRules();}
        }
        [UnityTest,Timeout(180000)]
        public IEnumerator PlantedFeetAgainstActualSupportAndControllerSkin()
        {
            string output=Environment.GetEnvironmentVariable("TUMP_FOOT_REVIEW")??"Logs/foot-support-baseline";
            Directory.CreateDirectory(output);
            var rows=new List<string>{"id,skin_width,root_y,support_y,drawn_sole_y,renderer_min_y,sole_gap,controller_bottom,grounded"};
            var failures=new List<string>();var mesh=new Mesh();
            try
            {
                foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
                {
                    yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,mode);
                    var who=GameServices.Round.PlayerAt(1);var cc=who.GetComponent<CharacterController>();
                    var visual=who.GetComponent<CharacterVisual>();
                    var input=who.gameObject.AddComponent<FootInput>();var sampler=who.gameObject.AddComponent<FootSample>();who.Intent.Parked=false;
                    foreach(var other in GameServices.Round.Players.Where(p=>p!=who))other.Teleport(new Vector3(-5,0,-5+other.PlayerSlot*3));
                    foreach(string id in Roster.GetPeople(mode).Select(person=>person.Id))
                    {
                        var roster=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id==id);
                        who.CharacterIndex=Roster.GetPeople(mode).Select((p,i)=>(p,i)).First(p=>p.p.Id==id).i;
                        visual.ApplyModel(roster.Model,roster.Tint,roster.Clips,roster.Palette,roster.PetModel);
                        yield return null;
                        foreach(float skin in new[]{.08f,.035f})
                        {
                            cc.skinWidth=skin;who.Teleport(new Vector3(0,.1f,9));
                            input.Move=Vector2.right*.4f;yield return new WaitForSeconds(.4f);
                            input.Move=Vector2.zero;yield return new WaitForSeconds(.6f);
                            bool sampled=false;
                            sampler.Sample=()=>
                            {
                                float sole=float.PositiveInfinity,bounds=float.PositiveInfinity;
                                var bodies=who.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.bones.Any(b=>b!=null && b.name=="leg-left")).ToArray();
                                Assert.IsNotEmpty(bodies,"No real foot-bearing body mesh found.");
                                foreach(var body in bodies)
                                {
                                    body.BakeMesh(mesh);bounds=Mathf.Min(bounds,body.bounds.min.y);
                                    foreach(var vertex in mesh.vertices)sole=Mathf.Min(sole,body.transform.TransformPoint(vertex).y);
                                }
                                float support=Slipper.GroundY(who.transform.position),gap=sole-support;
                                rows.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4},{6:F4},{7:F4},{8}",
                                    id,skin,who.transform.position.y,support,sole,bounds,gap,cc.bounds.min.y,who.IsGrounded));
                                if(Mathf.Abs(gap)>.012f)failures.Add(id+" planted sole is "+gap.ToString("F4",CultureInfo.InvariantCulture)+"m above support");
                                sampled=true;
                            };
                            while(!sampled)yield return null;
                        }
                    }
                    Object.Destroy(input);Object.Destroy(sampler);yield return PlayModeWorld.Reset();
                }
            }
            finally{Object.Destroy(mesh);File.WriteAllLines(Path.Combine(output,"feet.csv"),rows);}
            Assert.AreEqual(37,rows.Count,"All eighteen bodies and both controller widths must be measured.");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest,Timeout(120000)]
        public IEnumerator ContactSurvivesKerbJumpAndTeleportWithoutMovingTheCapsule()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
            foreach(var other in GameServices.Round.Players.Where(p=>p!=who))other.Teleport(new Vector3(-5,0,-5+other.PlayerSlot*3));
            var input=who.gameObject.AddComponent<FootInput>();var sampler=who.gameObject.AddComponent<FootSample>();
            var kerb=GameObject.CreatePrimitive(PrimitiveType.Cube);kerb.name="Foot contact test kerb";
            kerb.transform.position=new Vector3(0,.225f,9);kerb.transform.localScale=new Vector3(3,.25f,2);
            var mesh=new Mesh();var records=new List<string>{"stage,body_y,sole_y,support_y,grounded"};
            try
            {
                who.Teleport(new Vector3(0,.1f,6.7f));
                input.Move=Vector2.up;yield return new WaitForSeconds(.92f);
                input.Move=Vector2.zero;yield return new WaitForSeconds(.55f);
                Assert.Greater(who.transform.position.z,8.4f,"The actual motor did not climb the test kerb.");
                Assert.Less(who.transform.position.z,9.9f,"The test ran beyond the kerb.");
                Assert.IsTrue(who.IsGrounded);
                float capsule=who.transform.position.y;
                Assert.AreEqual(.35f+who.GetComponent<CharacterController>().skinWidth,capsule,.02f,"Visual correction moved the physical capsule.");
                yield return Sample("kerb",true);
                input.Jump=true;yield return new WaitForSeconds(.055f);input.Jump=false;
                yield return new WaitForSeconds(.13f);
                Assert.IsFalse(who.IsGrounded,"The real jump input did not leave support.");
                yield return Sample("air",false);
                float deadline=Time.time+3;
                while(!who.IsGrounded && Time.time<deadline)yield return null;
                Assert.IsTrue(who.IsGrounded,"Jump never landed.");
                yield return new WaitForSeconds(.4f);yield return Sample("landed",true);
                who.Teleport(new Vector3(3,.1f,6.7f));
                yield return new WaitForSeconds(.4f);yield return Sample("teleported",true);
                var visual=who.GetComponent<CharacterVisual>();visual.SmoothRemote=true;
                input.Move=Vector2.right;yield return new WaitForSeconds(.3f);
                input.Move=Vector2.zero;yield return new WaitForSeconds(.5f);yield return Sample("smoothed",true);
                visual.SmoothRemote=false;
                kerb.transform.position=new Vector3(0,.975f,9);
                who.Teleport(new Vector3(0,1.1f,9));yield return new WaitForSeconds(.4f);
                yield return Sample("raised",true);
                Object.Destroy(kerb);kerb=null;yield return new WaitForSeconds(.12f);
                Assert.IsFalse(who.IsGrounded,"Removing support did not produce an actual fall.");
                yield return Sample("unsupported",false);
            }
            finally
            {
                if(kerb!=null)Object.Destroy(kerb);Object.Destroy(mesh);Object.Destroy(input);Object.Destroy(sampler);
                string output=Environment.GetEnvironmentVariable("TUMP_FOOT_REVIEW")??"Logs/foot-support-baseline";
                Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,"transitions.csv"),records);
            }
            IEnumerator Sample(string stage,bool planted)
            {
                bool done=false;Exception failure=null;
                sampler.Sample=()=>
                {
                    try
                    {
                        float sole=float.PositiveInfinity;
                        foreach(var body in who.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.bones.Any(b=>b!=null && b.name=="leg-left")))
                        {body.BakeMesh(mesh);foreach(var vertex in mesh.vertices)sole=Mathf.Min(sole,body.transform.TransformPoint(vertex).y);}
                        float support=Slipper.GroundY(who.transform.position);
                        records.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1:F4},{2:F4},{3:F4},{4}",stage,who.transform.position.y,sole,support,who.IsGrounded));
                        if(planted && Mathf.Abs(support-sole)>.025f)
                        {
                            foreach(var part in who.GetComponentsInChildren<Transform>())
                                if(part.name=="Visual"||part.name=="root"||part.name=="leg-left"||part.name=="leg-right")
                                    Debug.Log("[FootSupport] "+part.name+" parent="+part.parent?.name+" world="+part.position.ToString("F4")+" local="+part.localPosition.ToString("F4"));
                            Debug.Log("[FootSupport] body="+who.transform.position.ToString("F4")+" model="+who.GetComponent<CharacterVisual>().ModelRoot.localPosition.ToString("F4")+" velocity="+who.Velocity);
                        }
                        if(planted)Assert.AreEqual(support,sole,.025f,stage+" foot lost contact.");
                        else Assert.Greater(sole,support+.1f,"Ground contact correction pinned the jumping body to the floor.");
                    }
                    catch(Exception error){failure=error;}
                    finally{done=true;}
                };
                while(!done)yield return null;
                if(failure!=null)throw failure;
            }
        }

        [DefaultExecutionOrder(-300)]
        private sealed class FootInput:MonoBehaviour
        {
            public Vector2 Move;public bool Jump;
            private void Update(){var who=GetComponent<CharacterMotor>();who.Intent.Move=Move;who.Intent.Set(Verb.Jump,Jump);}
        }
        [DefaultExecutionOrder(10000)]
        private sealed class FootSample:MonoBehaviour
        {
            public Action Sample;
            private void LateUpdate(){var callback=Sample;Sample=null;callback?.Invoke();}
        }
    }
}
