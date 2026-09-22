using System.Collections;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        [UnityTest] public IEnumerator RealSprintExperimentHonoursOffAndComfortAndCanHasNoDuplicateWords()
        {
            yield return Load(SceneFlow.BayanPlaza);
            var actor=GameServices.Round.PlayerAt(1);var cam=Camera.main;var grade=cam.GetComponent<ColourGrade>();
            int popups=Object.FindObjectsByType<ComicPopup>().Length;
            MatchFlair.Play(MatchFlair.Kind.LataDown,1,-1,GameServices.Round.Lata.transform.position,0);
            Assert.AreEqual(popups,Object.FindObjectsByType<ComicPopup>().Length,"Can-down has its existing moment, not another world sentence.");
            actor.Teleport(new Vector3(-3,actor.transform.position.y,-3));actor.Intent.Parked=false;
            actor.Intent.Move=Vector2.up;yield return new WaitForSeconds(.3f);
            float walked=new Vector2(actor.Velocity.x,actor.Velocity.z).magnitude;
            actor.Intent.Set(Verb.Sprint,true);yield return new WaitForSeconds(.3f);
            Assert.IsTrue(actor.Stamina.IsSprinting);
            Assert.Greater(new Vector2(actor.Velocity.x,actor.Velocity.z).magnitude,walked*1.1f);
            // Freeze the measured live velocity/pose for same-camera comparison,
            // leaving presentation time running so this is not a paused input state.
            actor.enabled=false;var rig=cam.GetComponent<CameraRig>();rig.enabled=false;
            cam.transform.position=actor.transform.position+Vector3.up*1.25f;cam.transform.rotation=Quaternion.Euler(0,0,0);
            foreach(string state in new[]{"before","after","comfort"})
            {
                WorldCueProfile.Current.SpeedLines=state=="before"?0:1;
                SettingsStore.Current.ReducedUiMotion=state=="comfort";SettingsStore.Current.ReducedEffects=state=="comfort";
                SettingsStore.Current.HighContrastHud=state=="comfort";SettingsStore.Current.HudScale=state=="comfort"?1.2f:1;
                Private(grade,"PrepareExperimentValues");
                if(state=="after")Assert.Greater(grade.ExperimentSpeed,0);else Assert.AreEqual(0,grade.ExperimentSpeed);
                // This is the owner's FPP camera. Do not ask the witness helper
                // to reveal the very body whose head surrounds this camera.
                yield return GameplayShots.Render(cam,"speed-"+state,false,Output,null,960,540);
            }
            actor.enabled=true;rig.enabled=true;actor.Intent.Clear();
        }
        [UnityTest] public IEnumerator SoundExperimentUsesHeardContactsAndRejectsSelfFarReplayAndNonSteps()
        {
            yield return Load(SceneFlow.Eskinita);var cam=Camera.main;var grade=cam.GetComponent<ColourGrade>();
            var actor=GameServices.Round.PlayerAt(1);var rig=cam.GetComponent<CameraRig>();rig.enabled=false;
            WorldCueProfile.Current.SoundPips=1;SettingsStore.Current.SfxVolume=0;
            void Clear(){grade.enabled=false;grade.enabled=true;}
            void Read()=>Private(grade,"PrepareExperimentValues");
            GameServices.Audio.PlayAtVaried("step_rubber",actor.transform.position);Read();Assert.AreEqual(0,grade.ExperimentPipCount);
            GameServices.Audio.PlayAtVaried("step_rubber",cam.transform.position+Vector3.right*40);Read();Assert.AreEqual(0,grade.ExperimentPipCount);
            GameServices.Audio.PlayAtVaried("throw_whoosh",cam.transform.position+Vector3.right*5);Read();Assert.AreEqual(0,grade.ExperimentPipCount);
            using(GameServices.Audio.EnterReplayMix())
            {GameServices.Audio.PlayAtVaried("step_rubber",cam.transform.position+cam.transform.right*5);Read();Assert.AreEqual(0,grade.ExperimentPipCount);}
            foreach(string state in new[]{"before","after","comfort"})
            {
                Clear();WorldCueProfile.Current.SoundPips=state=="before"?0:1;
                SettingsStore.Current.ReducedEffects=state=="comfort";SettingsStore.Current.HighContrastHud=state=="comfort";SettingsStore.Current.HudScale=state=="comfort"?1.2f:1;
                GameServices.Audio.PlayAtVaried("step_rubber",cam.transform.position+cam.transform.right*5);
                Read();Assert.AreEqual(state=="before"?0:1,grade.ExperimentPipCount);
                yield return GameplayShots.Render(cam,"sound-"+state,false,Output,null,960,540);
            }
            yield return new WaitForSecondsRealtime(.5f);Read();Assert.AreEqual(0,grade.ExperimentPipCount);rig.enabled=true;
        }
        // A controlled photograph of a real reserved ultimate. This holds only
        // its accepted presentation timestamp, not a synthetic active flag.
        [DefaultExecutionOrder(-3000)]
        public sealed class AcceptedPhaseWitness : MonoBehaviour
        {
            private void Update()
            {
                var phase=SharedUltimatePhase.Instance;
                if(phase!=null && phase.Active)typeof(SharedUltimatePhase).GetProperty("Began").SetValue(phase,SharedUltimatePhase.Now-.8);
            }
        }
        [UnityTest] public IEnumerator AcceptedUltimateGradePreservesVisibleBodyColourAndRespectsOcclusion()
        {
            yield return Load(SceneFlow.Eskinita,GameMode.HeroStrike);
            var actor=GameServices.Round.PlayerAt(1);actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"sean");
            var art=RosterBook.Load().FindPersonArt("sean");actor.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            actor.AbilitySystem.BindHero("sean");actor.AbilitySystem.Kit.AddUltimateCharge(actor.AbilitySystem.Kit.UltimateCost);
            actor.Intent.Parked=false;actor.Teleport(new Vector3(0,actor.transform.position.y,2));
            var cam=Camera.main;var rig=cam.GetComponent<CameraRig>();rig.Follow(GameServices.Round.PlayerAt(0));rig.enabled=false;
            cam.transform.position=new Vector3(1.6f,actor.transform.position.y+1.25f,-2.2f);cam.transform.LookAt(actor.transform.position+Vector3.up*.7f);cam.fieldOfView=58;
            var grade=cam.GetComponent<ColourGrade>();SettingsStore.Current.CinematicCameraMotion=false;
            float until=Time.realtimeSinceStartup+4;
            while(UltimateIntroductionCache.Find(actor,true)==null && Time.realtimeSinceStartup<until)yield return null;
            actor.Intent.Set(Verb.Ultimate,true);actor.Intent.BufferPress(Verb.Ultimate);
            if(actor.AbilitySystem.Kit.Ultimate.HoldToAim)
            {yield return new WaitForSecondsRealtime(.2f);actor.Intent.Set(Verb.Ultimate,false);}
            until=Time.realtimeSinceStartup+1;
            while(!SharedUltimatePhase.BlocksActions && Time.realtimeSinceStartup<until)yield return null;
            var phase=SharedUltimatePhase.Instance;Assert.IsTrue(phase!=null&&phase.Active);
            Assert.AreEqual(0,actor.AbilitySystem.Kit.UltimateCharge);
            var hold=new GameObject("Accepted phase photographic hold");hold.AddComponent<AcceptedPhaseWitness>();
            try
            {
                foreach(string state in new[]{"before","after","comfort"})
                {
                    WorldCueProfile.Current.UltimateDesaturation=state=="before"?0:.72f;
                    SettingsStore.Current.ReducedEffects=state=="comfort";SettingsStore.Current.HighContrastHud=state=="comfort";SettingsStore.Current.HudScale=state=="comfort"?1.2f:1;
                    yield return null;Private(grade,"PrepareExperimentValues");
                    if(state=="after")Assert.Greater(grade.ExperimentWorld,0);else Assert.AreEqual(0,grade.ExperimentWorld);
                    Assert.AreEqual(0,grade.ExperimentPipCount);Assert.AreEqual(0,grade.ExperimentSpeed);
                    yield return GameplayShots.Render(cam,"ultimate-world-"+state,false,Output,actor,960,540);
                }
                var body=actor.transform.position+Vector3.up*.75f;Vector3 screen=cam.WorldToViewportPoint(body);
                var baseline=ReadPixels("ultimate-world-before");var after=ReadPixels("ultimate-world-after");
                try
                {
                    int x=Mathf.RoundToInt(screen.x*960),y=Mathf.RoundToInt(screen.y*540);
                    Assert.Less(PixelChange(baseline,after,x,y,5),.035f,"The actual body must retain its colour.");
                    Assert.Greater(PixelChange(baseline,after,180,290,50),.015f,"The surrounding world must actually change.");
                }
                finally{Object.Destroy(baseline);Object.Destroy(after);}
                SettingsStore.Current.ReducedEffects=false;
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary mask occlusion witness";
                var wallMaterial=new Material(Shader.Find("Standard")){color=new Color(.8f,.08f,.03f)};wall.GetComponent<Renderer>().sharedMaterial=wallMaterial;
                wall.transform.position=Vector3.Lerp(cam.transform.position,body,.72f);wall.transform.localScale=new Vector3(1.1f,1.4f,.16f);
                try
                {
                    foreach(string state in new[]{"before","after"})
                    {
                        WorldCueProfile.Current.UltimateDesaturation=state=="before"?0:.72f;
                        yield return GameplayShots.Render(cam,"mask-occlusion-"+state,false,Output,actor,960,540);
                    }
                    baseline=ReadPixels("mask-occlusion-before");after=ReadPixels("mask-occlusion-after");
                    screen=cam.WorldToViewportPoint(wall.transform.position);
                    Assert.Greater(PixelChange(baseline,after,Mathf.RoundToInt(screen.x*960),Mathf.RoundToInt(screen.y*540),5),.04f,"A hidden body may not preserve the wall's colour.");
                    Object.Destroy(baseline);Object.Destroy(after);
                }
                finally{Object.Destroy(wall);Object.Destroy(wallMaterial);}
            }
            finally{Object.Destroy(hold);phase.Cancel();rig.enabled=true;}
        }
        private static Texture2D ReadPixels(string name)
        {var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(Path.Combine(Output,name+".png")));return texture;}
        private static float PixelChange(Texture2D a,Texture2D b,int x,int y,int radius)
        {
            float sum=0;int count=0;
            for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++)
            {Color p=a.GetPixel(Mathf.Clamp(x+dx,0,a.width-1),Mathf.Clamp(y+dy,0,a.height-1)),q=b.GetPixel(Mathf.Clamp(x+dx,0,b.width-1),Mathf.Clamp(y+dy,0,b.height-1));sum+=Mathf.Abs(p.r-q.r)+Mathf.Abs(p.g-q.g)+Mathf.Abs(p.b-q.b);count++;}
            return sum/(count*3);
        }
    }
}
