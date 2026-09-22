using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using TumbangPreso.CameraSystem;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        private static Camera StageCamera(Vector3 position,Vector3 target)
        {
            var camera=new GameObject("World stage witness").AddComponent<Camera>();camera.CopyFrom(Camera.main);
            camera.enabled=false;camera.fieldOfView=58;camera.transform.position=position;camera.transform.LookAt(target);
            camera.gameObject.AddComponent<WorldLookCamera>();camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled=true;return camera;
        }
        private static void StageWeights(float weight)
        {var p=WorldCueProfile.Current;p.WorldLighting=weight;p.CourtSurface=weight;p.HeroObjects=weight;}
        [UnityTest] public IEnumerator FiveMapStageCapturesPreserveGeometryAndRestoreOriginalLighting()
        {
            foreach(string map in new[]{SceneFlow.BayanPlaza,SceneFlow.Eskinita,SceneFlow.IlalimNgTulay,SceneFlow.SaBubong,SceneFlow.Lagoon})
            {
                yield return Load(map,GameMode.HeroStrike);var look=WorldLookPresentation.Current;Assert.IsNotNull(look,map);
                var floor=Object.FindAnyObjectByType<CourtSurfacePresentation>();Assert.IsNotNull(floor);
                string chalk=string.Join("; ",Object.FindObjectsByType<MeshRenderer>().Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.name.ToLowerInvariant().Contains("chalk")))
                    .Take(14).Select(r=>r.name+" "+r.bounds+" tag="+(r.GetComponentInParent<VfxRenderTag>()!=null)+" scene="+r.gameObject.scene.name));
                Assert.Greater(floor.AuthoredMarkRenderers,4,map+" floor="+look.Floor+" chalk="+chalk);
                Assert.AreEqual(0,floor.GetComponentsInChildren<Collider>(true).Length,"Decorative court treatment cannot create blockers.");
                foreach(var actor in GameServices.Round.Players)
                {
                    actor.Teleport(new Vector3(-2.1f+actor.PlayerSlot*1.4f,look.Floor+.02f,1.4f));actor.transform.forward=Vector3.back;
                }
                yield return new WaitForSeconds(GameServices.Round.Lata.ProtectionLeft+.05f);
                var observer=StageCamera(new Vector3(5,look.Floor+3.7f,-8),new Vector3(0,look.Floor+.65f,0));
                Time.timeScale=0;StageWeights(0);yield return null;
                Color beforeSky=RenderSettings.ambientSkyColor;float beforeFog=RenderSettings.fogStartDistance;
                yield return GameplayShots.Render(observer,map+"-stage-before",false,Output,GameServices.Round.PlayerAt(1),960,540);
                StageWeights(1);yield return null;
                Assert.Less(RenderSettings.ambientSkyColor.maxColorComponent,beforeSky.maxColorComponent);
                Assert.Less(RenderSettings.fogStartDistance,beforeFog);
                float shaderBefore=Shader.GetGlobalFloat("_WorldLookWeight");
                Private(look,"BeginCamera",observer);Assert.AreEqual(1,Shader.GetGlobalFloat("_WorldLookWeight"));
                var preview=new GameObject("Unowned portrait").AddComponent<Camera>();preview.enabled=false;
                Private(look,"BeginCamera",preview);Assert.AreEqual(0,Shader.GetGlobalFloat("_WorldLookWeight"));
                Private(look,"EndCamera",preview);Assert.AreEqual(1,Shader.GetGlobalFloat("_WorldLookWeight"));
                Private(look,"EndCamera",observer);Assert.AreEqual(shaderBefore,Shader.GetGlobalFloat("_WorldLookWeight"));Object.Destroy(preview.gameObject);
                yield return GameplayShots.Render(observer,map+"-stage-after",false,Output,GameServices.Round.PlayerAt(1),960,540);
                SettingsStore.Current.ReducedEffects=true;SettingsStore.Current.HighContrastHud=true;SettingsStore.Current.HudScale=1.2f;
                yield return GameplayShots.Render(observer,map+"-stage-comfort",false,Output,GameServices.Round.PlayerAt(1),960,540);
                if(map==SceneFlow.Eskinita)
                {
                    WorldCueProfile.Current.CourtSurface=0;yield return null;
                    yield return GameplayShots.Render(observer,"Eskinita-lighting-only",false,Output,GameServices.Round.PlayerAt(1),960,540);
                    WorldCueProfile.Current.CourtSurface=1;WorldCueProfile.Current.WorldLighting=0;yield return null;
                    yield return GameplayShots.Render(observer,"Eskinita-surface-only",false,Output,GameServices.Round.PlayerAt(1),960,540);
                }
                StageWeights(0);yield return null;Assert.AreEqual(beforeSky,RenderSettings.ambientSkyColor);Assert.AreEqual(beforeFog,RenderSettings.fogStartDistance);
                StageWeights(1);Time.timeScale=1;SettingsStore.Current.ReducedEffects=false;SettingsStore.Current.HighContrastHud=false;SettingsStore.Current.HudScale=1;
                Object.Destroy(observer.gameObject);
            }
        }
        [UnityTest] public IEnumerator CanContactFollowsActualToppleElevatedPoseAndIndependentRecordedSupport()
        {
            yield return Load(SceneFlow.BayanPlaza);var lata=GameServices.Round.Lata;var contacts=WorldContactPresentation.Current;
            Assert.IsNotNull(contacts);yield return new WaitForSeconds(lata.ProtectionLeft+.05f);
            lata.HostKnockDown(1);Hitstop.End();yield return null;yield return null;
            Assert.IsTrue(contacts.LandingVisible,"Actual falling/settling state should carry its small footprint.");
            var camera=StageCamera(lata.transform.position+new Vector3(2,1.5f,-3),lata.transform.position);
            Time.timeScale=0;yield return GameplayShots.Render(camera,"can-settling-footprint",false,Output,null,960,540);
            Time.timeScale=1;yield return new WaitForSeconds(Balance.ToppleTime+.08f);
            yield return null;
            WorldGround.TryBelow(lata.transform.position,.5f,3.5f,out float contactFloor);
            Assert.IsFalse(contacts.LandingVisible,"Settled can cannot keep a false landing prediction: root="+lata.transform.position+" floor="+contactFloor+" support="+lata.PresentationSupportOffset);
            Vector3 settled=lata.transform.position;Quaternion rotation=lata.transform.rotation;
            // Use the actual authoritative-pose seam. Normal knocks remain
            // grounded; this exercises an elevated received pose without adding
            // or pretending there is new ballistic gameplay.
            lata.ApplySnapshotPose(settled+Vector3.up*.8f,rotation);yield return null;yield return null;
            Assert.IsTrue(contacts.LandingVisible);
            Time.timeScale=0;yield return GameplayShots.Render(camera,"can-elevated-footprint",false,Output,null,960,540);
            WorldCueProfile.Current.HeroObjects=0;yield return null;Assert.IsFalse(contacts.LandingVisible);WorldCueProfile.Current.HeroObjects=1;
            var recorded=new GroundContactVisual(camera.transform,"Recorded support contract",true);
            Vector3 live=lata.transform.position;
            try
            {
                Assert.IsTrue(recorded.Place(settled,settled.y,new Vector2(.25f,.22f),.2f));
                Assert.AreEqual(live,lata.transform.position);Assert.IsTrue(recorded.Renderer.forceRenderingOff);
                recorded.Visible(true);Assert.IsFalse(recorded.Renderer.forceRenderingOff);recorded.Visible(false);
                Assert.AreEqual(0,recorded.Renderer.GetComponents<Collider>().Length);
            }
            finally{recorded.Dispose();Object.Destroy(camera.gameObject);Time.timeScale=1;}
        }
        [UnityTest] public IEnumerator OrdinaryFlightInkUsesActualThrowerAndReplaysItsRecordedStroke()
        {
            yield return Load(SceneFlow.Eskinita);var round=GameServices.Round;var source=round.PlayerAt(1);var thrower=round.PlayerAt(2);
            // Another player's owned shoe is correctly refused by HostThrow.
            // The real supported different-owner case is guided training stock,
            // not a live-match ownership bypass. Teardown restores the flag.
            Assert.IsTrue(PracticeSandbox.Allowed);GameLaunch.GuidedTutorial=true;
            var shoe=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.OwnerSlot<0);
            shoe.gameObject.SetActive(true);Assert.IsTrue(shoe.HostForceEquip(thrower));int owner=shoe.OwnerSlot;
            var camera=StageCamera(new Vector3(2,2,-6),new Vector3(2,1,-1));
            shoe.HostThrow(thrower,new Vector3(2,1.5f,-2),Vector3.right*5+Vector3.up*2);
            yield return new WaitForSeconds(.09f);yield return null;
            var accent=shoe.GetComponent<SlipperMotionAccent>();Assert.IsTrue(accent.Emitting,"state="+shoe.State+" position="+shoe.transform.position+" velocity="+shoe.Velocity+" enabled="+accent.enabled+" active="+shoe.gameObject.activeInHierarchy+" thrower="+shoe.ThrowerSlot+" owner="+shoe.OwnerSlot);
            var stroke=shoe.GetComponentsInChildren<TrailRenderer>().First(t=>t.name=="SlipperMotionStroke");
            Assert.AreNotEqual(owner,shoe.ThrowerSlot);
            Assert.AreEqual("TumbangPreso/InkFlight",stroke.sharedMaterial.shader.name);
            // TrailRenderer stores its gradient colours at8bit precision.
            // Compare the actual encoded identity, not an unrepresentable float.
            Color wanted=(Color32)Color.Lerp(PlayerIdentity.Colour(thrower.PlayerSlot),Color.white,.55f);
            Assert.AreEqual(wanted.r,stroke.startColor.r,.001f);Assert.AreEqual(wanted.g,stroke.startColor.g,.001f);
            var saved=RecordedTrail.Capture().First(s=>s.Kind==0 && s.Id==shoe.SeatOfOrigin*4);Assert.Greater(saved.Points.Length,1);
            Time.timeScale=0;
            yield return GameplayShots.Render(camera,"ordinary-ink-flight",false,Output,null,960,540);
            using(var recorded=new RecordedFlightStroke(camera.transform,saved))
            {
                recorded.Sample(saved,null,0);recorded.Visible(true);
                var line=camera.GetComponentInChildren<LineRenderer>();Assert.AreEqual("TumbangPreso/InkFlight",line.sharedMaterial.shader.name);
                Assert.AreEqual(stroke.startColor,line.startColor);
            }
            WorldCueProfile.Current.HeroObjects=0;yield return null;Assert.AreEqual("Sprites/Default",stroke.sharedMaterial.shader.name);
            WorldCueProfile.Current.HeroObjects=1;Assert.IsTrue(shoe.HostForceEquip(thrower));yield return null;Assert.IsFalse(accent.Emitting);
            Object.Destroy(camera.gameObject);Time.timeScale=1;
        }
        [UnityTest] public IEnumerator ToonRampChangesMeasuredLightingAndLeavesUnownedPreviewGlobalsAlone()
        {
            yield return Load(SceneFlow.BayanPlaza);
            var look=WorldLookPresentation.Current;var sun=SkyEvent.RecordedSun;Assert.IsNotNull(sun);
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Temporary lighting calibration only";
            cube.GetComponent<Collider>().enabled=false;
            var material=new Material(Shader.Find("TumbangPreso/Toon"));material.SetColor("_Color",new Color(.4f,.4f,.4f));material.SetFloat("_OutlineWidth",0);
            var surface=cube.GetComponent<Renderer>();surface.sharedMaterial=material;surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            surface.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
            Vector3 away=sun.transform.forward;away.y=0;away.Normalize();
            cube.transform.position=new Vector3(0,look.Floor+4,0);cube.transform.rotation=Quaternion.LookRotation(away,Vector3.up);
            var camera=new GameObject("Raw lighting witness").AddComponent<Camera>();camera.enabled=false;
            camera.gameObject.AddComponent<WorldLookCamera>();camera.allowHDR=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.fieldOfView=40;camera.transform.position=cube.transform.position+away*3+Vector3.up*2.4f;camera.transform.LookAt(cube.transform.position);
            var output=new System.Text.StringBuilder("state,global_weight,top,side,linear_ratio\n");
            float oldRatio=0,newRatio=0;
            try
            {
                foreach(float weight in new[]{0f,1f})
                {
                    WorldCueProfile.Current.WorldLighting=weight;yield return null;yield return null;
                    var target=RenderTexture.GetTemporary(512,512,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
                    var pixels=new Texture2D(512,512,TextureFormat.RGBAFloat,false,true);
                    var previous=RenderTexture.active;float applied=-1;
                    void ReadScope(Camera view){if(view==camera)applied=Shader.GetGlobalFloat("_WorldLookWeight");}
                    Camera.onPreRender+=ReadScope;
                    try
                    {
                        camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                        pixels.ReadPixels(new Rect(0,0,512,512),0,0);pixels.Apply();
                        Vector3 top=camera.WorldToViewportPoint(cube.transform.position+Vector3.up*.501f);
                        Vector3 side=camera.WorldToViewportPoint(cube.transform.position+away*.501f);
                        float Luma(Vector3 at)
                        {
                            Color colour=pixels.GetPixel(Mathf.Clamp(Mathf.RoundToInt(at.x*512),0,511),Mathf.Clamp(Mathf.RoundToInt(at.y*512),0,511));
                            return colour.r*.2126f+colour.g*.7152f+colour.b*.0722f;
                        }
                        float lit=Luma(top),shade=Luma(side),ratio=lit/Mathf.Max(.0001f,shade);
                        if(weight==0)oldRatio=ratio;else newRatio=ratio;
                        output.AppendLine(System.FormattableString.Invariant($"{weight},{applied},{lit:F6},{shade:F6},{ratio:F4}"));
                        Assert.AreEqual(weight,applied,"Actual camera render must receive the map shader scope.");
                        Assert.Greater(lit,0);Assert.Greater(shade,0);
                    }
                    finally{Camera.onPreRender-=ReadScope;camera.targetTexture=null;RenderTexture.active=previous;Object.Destroy(pixels);RenderTexture.ReleaseTemporary(target);}
                }
                var actor=GameServices.Round.PlayerAt(1);var body=actor.GetComponent<CharacterVisual>().Model.GetComponentInChildren<Renderer>();
                var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);
                output.AppendLine("body_shader="+body.sharedMaterial.shader.name+",flash="+block.GetFloat("_FlashAmount")+",probe="+body.lightProbeUsage);
                System.IO.Directory.CreateDirectory(Output);System.IO.File.WriteAllText(System.IO.Path.Combine(Output,"lighting-response.csv"),output.ToString());
                Assert.Greater(newRatio,oldRatio*1.04f,"The chosen map lighting must produce a measurable contrast increase.");
                Assert.That(newRatio,Is.InRange(2f,2.5f),"The selected default must meet the2..2.5:1 linear lighting target.");
            }
            finally{WorldCueProfile.Current.WorldLighting=1;Object.Destroy(cube);Object.Destroy(material);Object.Destroy(camera.gameObject);}
        }
    }
}
