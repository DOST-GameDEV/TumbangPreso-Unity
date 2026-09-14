using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ThrowAimIntegrationProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private CustomRules _rules;
        private INetProvider _net;
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();_net=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_net;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest]
        public IEnumerator PreviewAndReleasedThrowRetainTheSameAimInBothModes()
        {
            const string output="Logs/throw-aim-integration-v1";Directory.CreateDirectory(output);
            var rows=new System.Collections.Generic.List<string>{"mode,aim_offset_degrees,velocity_error,point_shift_metres"};
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,mode);
                NetAuthority.Provider=new SoloProvider();GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);var carrier=who.GetComponent<Carrier>();
                var shoe=carrier.Held;Assert.IsNotNull(shoe);
                who.Teleport(new Vector3(0,.12f,-10));who.Intent.Clear();who.Intent.Parked=false;
                who.Intent.AimPoint=new Vector3(0,.18f,0);
                Assert.False(who.IsDefender);Assert.True(GameServices.Round.CanThrow(who));
                var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);
                who.transform.rotation=Quaternion.identity;rig.SetAimSource(CameraSystem.AimSource.Movement);
                yield return null;
                var toTarget=who.Intent.AimPoint-rig.transform.position;
                typeof(CameraSystem.CameraRig).GetField("_pitchDeg",Private).SetValue(rig,
                    Mathf.Atan2(-toTarget.y,new Vector2(toTarget.x,toTarget.z).magnitude)*Mathf.Rad2Deg);
                carrier.enabled=false;
                who.Intent.Set(Verb.SpecialAbility,true);
                Step(carrier,.016f);Assert.True(carrier.IsCharging);
                Set(carrier,"_charge",.35f);Set(carrier,"_aimHeldSeconds",.35f);Set(carrier,"_aimMovement",.8f);
                carrier.ApplyObservedCharge(true,.35f,0);
                if(mode==GameMode.HeroStrike)
                {
                    who.AbilitySystem.BindHero("zack",new HeroBuild{HeroId="zack",Slot2VariantId="zack.2.discharge"});
                    ((ZackHeroKit)who.AbilitySystem.Kit).IsOverchargeThrowActive=true;
                }
                yield return GameplayShots.Render(rig.Camera,mode+"-steady-aim",true,output);
                var aim=carrier.AimPoint();var expected=carrier.LaunchVelocityNow();
                var viewPoint=rig.Camera.WorldToViewportPoint(carrier.AimGuidePoint());
                Assert.Greater(viewPoint.z,0,"Aim review camera must face the actual target.");
                Assert.Less(Vector2.Distance(new Vector2(viewPoint.x,viewPoint.y),new Vector2(.5f,.5f)),.05f);
                var readout=Object.FindFirstObjectByType<TumpMatchReadout>();
                typeof(TumpMatchReadout).GetMethod("LateUpdate",Private).Invoke(readout,null);
                var reticle=(UnityEngine.UI.Text)typeof(TumpMatchReadout).GetField("_crosshair",Private).GetValue(readout);
                Assert.Less(Vector2.Distance(reticle.rectTransform.anchorMin,new Vector2(viewPoint.x,viewPoint.y)),.001f,
                    "The general-direction reticle must not reveal the exact accuracy error.");
                float shift=Vector3.Distance(aim,who.Intent.AimPoint);
                Assert.Greater(shift,.001f,"Fixture must release a visibly offset aim, not a zero crossing.");
                float angle=carrier.AimAngularOffset.magnitude;
                who.Intent.Set(Verb.SpecialAbility,false);
                Step(carrier,.016f);
                Assert.False(carrier.IsCharging);Assert.IsNull(carrier.Held);
                float error=Vector3.Distance(expected,shoe.Velocity);
                Assert.Less(error,.001f,"Clearing charge or consuming the infusion changed the visible launch solution.");
                rows.Add(FormattableString.Invariant($"{mode},{angle:F5},{error:F6},{shift:F5}"));
                carrier.enabled=true;
                yield return PlayModeWorld.Reset();
            }
            File.WriteAllLines(Path.Combine(output,"release.csv"),rows);
        }

        [UnityTest]
        public IEnumerator DirectionGuideStaysVisibleOnRealSurfacesWithoutGivingAwayMovingLanding()
        {
            string output=Environment.GetEnvironmentVariable("TUMP_AIM_GUIDE_REVIEW")??"Logs/aim-guide-surfaces-v2";Directory.CreateDirectory(output);
            foreach(string map in new[]{SceneFlow.BayanPlaza,SceneFlow.IlalimNgTulay,SceneFlow.SaBubong})
            {
                yield return MapRetrievalProbe.Load(map);
                var ready=Object.FindFirstObjectByType<ReadyGate>();
                Assert.IsNotNull(ready);ready.StartLocalCountdown();
                yield return new WaitForSeconds(3.6f);
                Assert.False(ready.AwaitingReady||ready.CountingDown,"Review must enter the active game through its ready countdown.");
                NetAuthority.Provider=new SoloProvider();GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);var carrier=who.GetComponent<Carrier>();
                var can=GameServices.Round.Lata;
                who.Teleport(can.transform.position+new Vector3(0,.12f,-10));
                who.transform.rotation=Quaternion.identity;who.Intent.Clear();who.Intent.Parked=false;
                who.Intent.AimPoint=can.transform.position+Vector3.up*.18f;
                var brain=who.GetComponent<AIController>();if(brain==null)brain=who.gameObject.AddComponent<AIController>();
                brain.enabled=false;
                var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);
                rig.SetAimSource(CameraSystem.AimSource.Movement);yield return null;
                var direction=who.Intent.AimPoint-rig.transform.position;
                typeof(CameraSystem.CameraRig).GetField("_pitchDeg",Private).SetValue(rig,
                    Mathf.Atan2(-direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg);
                carrier.enabled=false;who.Intent.Set(Verb.SpecialAbility,true);Step(carrier,.016f);
                Assert.True(carrier.IsCharging);
                Set(carrier,"_charge",Balance.ChargeFullTime);Set(carrier,"_aimHeldSeconds",2.5f);Set(carrier,"_aimMovement",1f);
                var guide=GameObject.Find($"~AimArc{who.PlayerSlot}")?.GetComponent<TrajectoryPreview>();
                Assert.IsNotNull(guide,"The real match must install the local aiming guide.");
                typeof(TrajectoryPreview).GetMethod("LateUpdate",Private).Invoke(guide,null);
                Assert.True(guide.GetComponent<MeshRenderer>().enabled,"A disabled old bot component hid the local direction guide.");
                var path=(List<Vector3>)typeof(TrajectoryPreview).GetField("_path",Private).GetValue(guide);
                Assert.Greater(path.Count,3);
                float shortLength=Vector3.Distance(path[0],path[path.Count-1]);
                Assert.Greater(Vector3.Distance(path[path.Count-1],who.Intent.AimPoint),3f,
                    "Moving aim must not disclose the actual landing position.");
                foreach(var vertex in guide.GetComponent<MeshFilter>().sharedMesh.vertices)
                    Assert.False(float.IsNaN(vertex.x)||float.IsNaN(vertex.y)||float.IsNaN(vertex.z));
                yield return GameplayShots.Render(rig.Camera,map+"-moving-guide",true,output);
                Assert.True(guide.GetComponent<MeshRenderer>().enabled,"The guide disappeared during actual camera frames.");
                CaptureGuideDifference(rig.Camera,guide,output,map+"-moving");
                Set(carrier,"_aimMovement",0f);
                typeof(TrajectoryPreview).GetMethod("LateUpdate",Private).Invoke(guide,null);
                float settledLength=Vector3.Distance(path[0],path[path.Count-1]);
                Assert.Greater(settledLength,shortLength*1.5f,"Settling must give useful additional direction information.");
                yield return GameplayShots.Render(rig.Camera,map+"-settled-guide",true,output);
                CaptureGuideDifference(rig.Camera,guide,output,map+"-settled");
                who.Intent.Set(Verb.SpecialAbility,false);Step(carrier,.016f);
                typeof(TrajectoryPreview).GetMethod("LateUpdate",Private).Invoke(guide,null);
                Assert.False(guide.GetComponent<MeshRenderer>().enabled,"The guide survived an accepted release.");
                Object.Destroy(guide.gameObject);yield return PlayModeWorld.Reset();
            }
        }

        private static void CaptureGuideDifference(Camera camera,TrajectoryPreview guide,string folder,string name)
        {
            const int width=1280,height=720;
            var renderer=guide.GetComponent<MeshRenderer>();bool wasEnabled=renderer.enabled;
            var prior=camera.targetTexture;var active=RenderTexture.active;
            var hdr=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear);
            var resolved=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=hdr;
                Color32[] Capture(bool shown)
                {
                    renderer.enabled=shown;camera.Render();Graphics.Blit(hdr,resolved);RenderTexture.active=resolved;
                    pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder,name+(shown?"-on.png":"-off.png")),pixels.EncodeToPNG());
                    return pixels.GetPixels32();
                }
                var on=Capture(true);var off=Capture(false);int changed=0;
                for(int i=0;i<on.Length;i++)
                    if(Mathf.Abs(on[i].r-off[i].r)+Mathf.Abs(on[i].g-off[i].g)+Mathf.Abs(on[i].b-off[i].b)>24)changed++;
                File.WriteAllText(Path.Combine(folder,name+"-visibility.txt"),"Guide contributes "+changed+" visible pixels at 1280x720.\n");
                Assert.Greater(changed,100,"A populated guide mesh must contribute a readable stroke to the actual camera image.");
            }
            finally
            {
                renderer.enabled=wasEnabled;camera.targetTexture=prior;RenderTexture.active=active;
                RenderTexture.ReleaseTemporary(hdr);RenderTexture.ReleaseTemporary(resolved);Object.DestroyImmediate(pixels);
            }
        }

        private static void Set(Carrier carrier,string name,float value)=>typeof(Carrier).GetField(name,Private).SetValue(carrier,value);
        private static void Step(Carrier carrier,float dt)=>typeof(Carrier).GetMethod("StepAttacker",Private).Invoke(carrier,new object[]{dt});
    }
}
