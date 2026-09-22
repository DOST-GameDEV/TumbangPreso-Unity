using System.Collections;
using System.Reflection;
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
    public sealed partial class WorldCourtCueTests
    {
        private static T ArmField<T>(ViewmodelArms arms,string name)
            =>(T)typeof(ViewmodelArms).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(arms);
        private static Rect MeshViewport(Camera camera,Renderer surface)
        {
            var bounds=surface.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector2 min=Vector2.one*100,max=Vector2.one*-100;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                Vector3 local=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                Vector3 point=camera.WorldToViewportPoint(surface.transform.TransformPoint(local));
                min=Vector2.Min(min,point);max=Vector2.Max(max,point);
            }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        [UnityTest] public IEnumerator ViewmodelLensStaysFixedAcrossWorldFovAndRestoresCameraScopes()
        {
            yield return Load(SceneFlow.Eskinita);
            var cam=Camera.main;var rig=cam.GetComponent<CameraRig>();var actor=GameServices.Round.PlayerAt(1);
            var arms=cam.GetComponentInChildren<ViewmodelArms>(true);Assert.IsNotNull(arms);
            var held=ArmField<Renderer>(arms,"_heldRenderer");var arm=ArmField<Renderer>(arms,"_rightArmRenderer");
            Assert.AreEqual("TumbangPreso/Toon",arm.sharedMaterial.shader.name);
            rig.enabled=false;Time.timeScale=0;
            actor.Intent.AimPoint=actor.transform.position+Vector3.forward*10+Vector3.up;
            Vector3 launch=actor.GetComponent<Carrier>().ThrowOrigin();
            Vector3 position=arms.transform.localPosition,scale=arms.transform.localScale;
            var originalShadows=held.shadowCastingMode;var block=new MaterialPropertyBlock();held.GetPropertyBlock(block);
            float oldRim=block.GetFloat("_ViewmodelRimStrength");
            WorldCueProfile.Current.ViewmodelFraming=0;cam.fieldOfView=95;
            yield return GameplayShots.Render(cam,"viewmodel-rest-before",false,Output,null,960,540);
            WorldCueProfile.Current.ViewmodelFraming=1;Rect reference=default;
            var stranger=new GameObject("Viewmodel ownership witness").AddComponent<Camera>();stranger.enabled=false;
            foreach(float fov in new[]{75f,95f,110f})
            {
                cam.fieldOfView=fov;Private(arms,"BeginViewFrame",cam);
                Rect rect=MeshViewport(cam,held);
                if(fov==75)reference=rect;
                else
                {
                    Assert.AreEqual(reference.xMin,rect.xMin,.0002f);Assert.AreEqual(reference.xMax,rect.xMax,.0002f);
                    Assert.AreEqual(reference.yMin,rect.yMin,.0002f);Assert.AreEqual(reference.yMax,rect.yMax,.0002f);
                }
                Assert.AreEqual(fov,cam.fieldOfView);Assert.Less(Vector3.Distance(launch,actor.GetComponent<Carrier>().ThrowOrigin()),.00001f);
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off,held.shadowCastingMode);
                held.GetPropertyBlock(block);Assert.Greater(block.GetFloat("_ViewmodelRimStrength"),0);
                Private(arms,"BeginViewFrame",stranger);Assert.IsTrue(held.forceRenderingOff);
                Private(arms,"EndViewFrame",stranger);Assert.IsFalse(held.forceRenderingOff);
                Private(arms,"EndViewFrame",cam);
                Assert.AreEqual(position,arms.transform.localPosition);Assert.AreEqual(scale,arms.transform.localScale);
                Assert.AreEqual(originalShadows,held.shadowCastingMode);held.GetPropertyBlock(block);Assert.AreEqual(oldRim,block.GetFloat("_ViewmodelRimStrength"));
                yield return GameplayShots.Render(cam,"viewmodel-fov-"+(int)fov,false,Output,null,960,540);
            }
            // A nested owner view inside an unrelated render also gets its own
            // original visibility, then returns to that unrelated hidden state.
            Private(arms,"BeginViewFrame",stranger);Private(arms,"BeginViewFrame",cam);Assert.IsFalse(held.forceRenderingOff);
            Private(arms,"EndViewFrame",cam);Assert.IsTrue(held.forceRenderingOff);Private(arms,"EndViewFrame",stranger);
            cam.fieldOfView=95;SettingsStore.Current.ReducedEffects=true;SettingsStore.Current.ReducedUiMotion=true;
            SettingsStore.Current.HighContrastHud=true;SettingsStore.Current.HudScale=1.2f;
            yield return GameplayShots.Render(cam,"viewmodel-rest-comfort",false,Output,null,960,540);
            WorldCueProfile.Current.ViewmodelFraming=0;Private(arms,"BeginViewFrame",cam);
            Assert.AreEqual(position,arms.transform.localPosition);Assert.AreEqual(scale,arms.transform.localScale);
            Assert.AreEqual(Vector3.one,arms.transform.parent.localScale);Private(arms,"EndViewFrame",cam);
            Object.Destroy(stranger.gameObject);Time.timeScale=1;rig.enabled=true;
        }
        [UnityTest] public IEnumerator ActualChargeAnticipatesWithoutDelayingReleaseAndRefusalKeepsTheShoe()
        {
            yield return Load(SceneFlow.BayanPlaza);
            SettingsStore.Current.ReducedUiMotion=false;SettingsStore.Current.ReducedEffects=false;
            var actor=GameServices.Round.PlayerAt(1);var carrier=actor.GetComponent<Carrier>();var lata=GameServices.Round.Lata;
            var cam=Camera.main;var arms=cam.GetComponentInChildren<ViewmodelArms>(true);
            actor.Teleport(new Vector3(-3,actor.transform.position.y,-8));actor.Intent.Parked=false;
            actor.Intent.AimPoint=new Vector3(-3,1.6f,8);
            foreach(var other in GameServices.Round.Players)if(other!=actor)other.Teleport(new Vector3(5,other.transform.position.y,5));
            yield return new WaitForSeconds(lata.ProtectionLeft+.05f);
            actor.Intent.Set(Verb.SpecialAbility,true);float until=Time.time+.5f;
            while(!carrier.IsCharging && Time.time<until)yield return null;
            Assert.IsTrue(carrier.IsCharging);yield return new WaitForSeconds(.04f);
            Assert.Greater(arms.ChargeAnticipation,0,"The real charge must have one early preparation pulse.");
            var shoe=carrier.Held;Assert.IsNotNull(shoe);
            carrier.enabled=false;Time.timeScale=0;yield return null;
            foreach(string state in new[]{"before","after"})
            {
                WorldCueProfile.Current.ViewmodelFraming=state=="before"?0:1;yield return null;
                Assert.IsTrue(carrier.IsCharging,"Photograph the actual held charge, not a cancelled pause.");
                yield return GameplayShots.Render(cam,"viewmodel-charge-"+state,false,Output,null,960,540);
            }
            carrier.enabled=true;Time.timeScale=1;
            actor.Intent.Set(Verb.SpecialAbility,false);float releasedAt=Time.time;
            while(carrier.Held!=null && Time.time-releasedAt<.15f)yield return null;
            Assert.IsNull(carrier.Held,"Framing/anticipation may not hold an accepted release.");
            yield return null; // Let the viewmodel's LateUpdate observe release.
            Assert.AreEqual(SlipperState.InFlight,shoe.State);Assert.AreEqual(0,arms.ChargeAnticipation);
            yield return new WaitForSeconds(ThrowGesture.ReleaseSeconds+.05f);
            Assert.IsTrue(shoe.HostForceEquip(actor));
            actor.Intent.Set(Verb.SpecialAbility,true);yield return new WaitForSeconds(.07f);
            Assert.IsTrue(carrier.IsCharging);lata.HostRestore();
            actor.Intent.Set(Verb.SpecialAbility,false);yield return null;yield return null;
            Assert.AreSame(shoe,carrier.Held,"Protected can refuses release and keeps the real item.");
            Assert.AreEqual(0,arms.ChargeAnticipation);
            yield return new WaitForSeconds(ThrowGesture.CancelSeconds+.06f);
            Assert.AreSame(shoe,carrier.Held);
            Assert.LessOrEqual(ArmField<float>(arms,"_actionReturnLeft"),0);
        }
        [DefaultExecutionOrder(20000)]
        public sealed class ReleaseFramePump : MonoBehaviour
        {
            public System.Action Tick;
            private void LateUpdate()=>Tick?.Invoke();
        }
        [UnityTest] public IEnumerator ActualSightlineReleaseVisualStudy()
        {
            yield return Load(SceneFlow.BayanPlaza);
            var actor=GameServices.Round.PlayerAt(1);var carrier=actor.GetComponent<Carrier>();
            var camera=Camera.main;var rig=camera.GetComponent<CameraRig>();
            actor.Teleport(new Vector3(-3,actor.transform.position.y,-8));actor.transform.rotation=Quaternion.identity;
            actor.Intent.Parked=false;rig.Follow(actor);
            typeof(CameraRig).GetField("_pitchDeg",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig,0f);
            SettingsStore.Current.FirstPersonFov=95;SettingsStore.Current.ReducedUiMotion=false;SettingsStore.Current.ReducedEffects=false;
            foreach(var other in GameServices.Round.Players)if(other!=actor)other.Teleport(new Vector3(5,other.transform.position.y,5));
            yield return new WaitForSeconds(GameServices.Round.Lata.ProtectionLeft+.05f);
            actor.Intent.AimPoint=rig.AimEye+Vector3.forward*18;
            float previousCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;
            var log=new System.Text.StringBuilder("frame,time,state,x,y,z,screen_x,screen_y,hand_x,hand_y\n");
            var pumpGo=new GameObject("After-pose release witness");var pump=pumpGo.AddComponent<ReleaseFramePump>();
            try
            {
                actor.Intent.Set(Verb.SpecialAbility,true);yield return new WaitForSeconds(.22f);
                Assert.IsTrue(carrier.IsCharging);var shoe=carrier.Held;Assert.IsNotNull(shoe);
                Vector3 muzzle=camera.WorldToViewportPoint(carrier.ThrowOrigin());
                Assert.AreEqual(.5f,muzzle.x,.005f);Assert.AreEqual(.5f,muzzle.y,.005f);
                CaptureReleaseFrame(camera,"release-held");actor.Intent.Set(Verb.SpecialAbility,false);
                float start=Time.time;int frame=0;bool seenFlight=false;Vector3 firstFlight=default;
                var arms=camera.GetComponentInChildren<ViewmodelArms>(true);var right=ArmField<Transform>(arms,"_rightArm");
                float handTop=right.GetComponent<MeshFilter>().sharedMesh.bounds.max.y,minHandX=1;
                System.Exception captureError=null;
                // A coroutine resumes before LateUpdate. Photograph AFTER the
                // camera and arm pose writers, as the normal player render does.
                pump.Tick=()=>
                {
                    try
                    {
                        if(!seenFlight)
                        {
                            if(shoe.State!=SlipperState.InFlight)return;
                            seenFlight=true;firstFlight=camera.WorldToViewportPoint(shoe.transform.position);
                        }
                        Vector3 position=shoe.transform.position,screen=camera.WorldToViewportPoint(position);
                        Vector3 hand=default;
                        void ReadHand(Camera view){if(view==camera)hand=camera.WorldToViewportPoint(right.TransformPoint(Vector3.up*handTop));}
                        Camera.onPreRender+=ReadHand;
                        try{CaptureReleaseFrame(camera,"release-"+frame.ToString("00"));}
                        finally{Camera.onPreRender-=ReadHand;}
                        minHandX=Mathf.Min(minHandX,hand.x);
                        log.AppendLine(System.FormattableString.Invariant($"{frame},{Time.time-start:F4},{shoe.State},{position.x:F4},{position.y:F4},{position.z:F4},{screen.x:F4},{screen.y:F4},{hand.x:F4},{hand.y:F4}"));
                        frame++;if(frame>=34)pump.Tick=null;
                    }
                    catch(System.Exception error){captureError=error;pump.Tick=null;}
                };
                float deadline=Time.realtimeSinceStartup+20;
                while(pump.Tick!=null && Time.realtimeSinceStartup<deadline)yield return null;
                if(captureError!=null)throw captureError;
                Assert.AreEqual(34,frame);Assert.AreEqual(.5f,firstFlight.x,.025f);Assert.AreEqual(.5f,firstFlight.y,.04f);
                Assert.Less(minHandX,.5f,"The real follow-through must pass the lower screen centre.");
                Assert.Greater(minHandX,.2f,"Follow-through must stay within the useful view.");
                Assert.AreEqual(Vector3.zero,ArmField<Vector3>(arms,"_releaseSweep"),"The transient sweep must settle completely.");
                Assert.IsTrue(seenFlight);Assert.IsNull(carrier.Held);
                System.IO.File.WriteAllText(System.IO.Path.Combine(Output,"release-poses.csv"),log.ToString());
            }
            finally{pump.Tick=null;Object.Destroy(pumpGo);Time.captureDeltaTime=previousCapture;}
        }
        private static void CaptureReleaseFrame(Camera camera,string name)
        {
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            var source=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear);
            var resolved=RenderTexture.GetTemporary(960,540,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=source;camera.Render();Graphics.Blit(source,resolved);RenderTexture.active=resolved;
                pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                System.IO.Directory.CreateDirectory(Output);System.IO.File.WriteAllBytes(System.IO.Path.Combine(Output,name+".png"),pixels.EncodeToPNG());
            }
            finally
            {camera.targetTexture=previousTarget;RenderTexture.active=previousActive;Object.Destroy(pixels);RenderTexture.ReleaseTemporary(source);RenderTexture.ReleaseTemporary(resolved);}
        }
    }
}
