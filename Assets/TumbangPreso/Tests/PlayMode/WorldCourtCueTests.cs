using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class WorldCourtCueTests
    {
        private string _settings,_profile;
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private float _timeScale;
        private CustomRules _rules;
        private static string Output => System.Environment.GetEnvironmentVariable("TUMP_WORLD_CUE_OUT") ?? "Logs/look-1.1-v1/shots";
        [UnitySetUp] public IEnumerator Before()
        {
            _settings=JsonUtility.ToJson(SettingsStore.Current);_profile=JsonUtility.ToJson(WorldCueProfile.Current);
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();_timeScale=Time.timeScale;
            yield return PlayModeWorld.Reset();Time.timeScale=1;
            SettingsStore.Current.CameraShake=0;SettingsStore.Current.SfxVolume=1;SettingsStore.Current.MasterVolume=1;
        }
        [UnityTearDown] public IEnumerator After()
        {
            Hitstop.End();Time.timeScale=1;
            yield return PlayModeWorld.Reset();
            JsonUtility.FromJsonOverwrite(_profile,WorldCueProfile.Current);
            SettingsStore.Restore(JsonUtility.FromJson<GameSettings>(_settings));
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
            Time.timeScale=_timeScale;
        }
        private static IEnumerator Load(string map)
        {
            yield return MapRetrievalProbe.Load(map);
            var gate=Object.FindFirstObjectByType<ReadyGate>();
            if(gate!=null && gate.AwaitingReady)
            {
                gate.StartLocalCountdown();
                yield return new WaitUntil(()=>!gate.CountingDown);
            }
            GameServices.Match.StartMatch();GameServices.Round.BeginRound();
            foreach(var actor in GameServices.Round.Players)
            {actor.Intent.Clear();actor.Intent.CommitFrame();actor.Intent.Parked=true;}
            yield return null;
        }
        [UnityTest] public IEnumerator CourtMatchesAllFiveMapsAndCapturesRestArmedAndOff()
        {
            foreach(string map in new[]{SceneFlow.BayanPlaza,SceneFlow.Eskinita,SceneFlow.IlalimNgTulay,SceneFlow.SaBubong,SceneFlow.Lagoon})
            {
                yield return Load(map);
                var cue=Object.FindFirstObjectByType<CourtBoundaryPresentation>();Assert.IsNotNull(cue);
                string sources=string.Join("; ",Object.FindObjectsByType<MeshRenderer>()
                    .Where(surface=>surface.sharedMaterials.Any(m=>m!=null && m.name.ToLowerInvariant().Contains("chalk")))
                    .Take(12).Select(surface=>surface.name+" scene="+surface.gameObject.scene.name+" mesh="+(surface.GetComponent<MeshFilter>()?.sharedMesh?.isReadable.ToString()??"none")+" bounds="+surface.bounds));
                Assert.AreEqual(4,cue.AuthoredBoundaryCount,"Replace only four authored edges. Install scanned="+cue.ScannedRenderers+
                    " now="+Object.FindObjectsByType<MeshRenderer>().Length+" cueScene="+cue.gameObject.scene.name+" sources="+sources);
                var renderer=cue.GetComponent<MeshRenderer>();Assert.IsTrue(renderer.sharedMaterial.shader.isSupported);
                float r=Balance.ConfinementRadius;
                // Variant2 widens the visual ink/chalk strip for the thumbnail.
                // Its centre stays exactly on the same confinement square.
                Assert.AreEqual(r*2+.32f,renderer.bounds.size.x,.015f);
                Assert.AreEqual(r*2+.32f,renderer.bounds.size.z,.015f);
                Assert.IsFalse(Confinement.IsInsideBox(CourtBoundaryPresentation.ClosestExit(new Vector3(3,0,6)).x,
                    CourtBoundaryPresentation.ClosestExit(new Vector3(3,0,6)).z));
                Assert.IsTrue(cue.Armed);
                var cameraGo=new GameObject("Court world witness");var witness=cameraGo.AddComponent<Camera>();
                witness.CopyFrom(Camera.main);witness.enabled=false;witness.fieldOfView=58;
                witness.transform.position=new Vector3(12,cue.Floor+12,-15);witness.transform.LookAt(new Vector3(0,cue.Floor,0));
                typeof(GameplayShots).GetMethod("Grade",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{witness});
                var previous=Camera.main;
                try
                {
                    previous.tag="Untagged";witness.tag="MainCamera";
                    Time.timeScale=0;WorldCueProfile.Current.Boundary=0;yield return null;
                    Assert.IsFalse(renderer.enabled);
                    yield return GameplayShots.Render(witness,map+"-before",false,Output,GameServices.Round.PlayerAt(1),960,540);
                    WorldCueProfile.Current.Boundary=1;yield return null;
                    yield return GameplayShots.Render(witness,map+"-armed",false,Output,GameServices.Round.PlayerAt(1),960,540);
                    Time.timeScale=1;
                    yield return new WaitForSeconds(GameServices.Round.Lata.ProtectionLeft+.05f);
                    GameServices.Round.Lata.HostKnockDown(1);Hitstop.End();yield return null;Time.timeScale=0;
                    Assert.IsFalse(cue.Armed);
                    yield return GameplayShots.Render(witness,map+"-rest",false,Output,GameServices.Round.PlayerAt(1),960,540);
                    SettingsStore.Current.ReducedEffects=true;SettingsStore.Current.HighContrastHud=true;SettingsStore.Current.HudScale=1.2f;
                    GameServices.Round.Lata.HostRestore();Hitstop.End();Time.timeScale=1;
                    yield return new WaitForSeconds(.46f);Time.timeScale=0;
                    yield return GameplayShots.Render(witness,map+"-restore-comfort",false,Output,GameServices.Round.PlayerAt(1),960,540);
                }
                finally
                {
                    previous.tag="MainCamera";witness.tag="Untagged";Object.Destroy(cameraGo);Time.timeScale=1;
                    SettingsStore.Current.ReducedEffects=false;SettingsStore.Current.HighContrastHud=false;SettingsStore.Current.HudScale=1;
                    WorldCueProfile.Current.Boundary=1;
                }
            }
        }
        [UnityTest] public IEnumerator ExitIsViewerLocalAndPhysicalEscapeDoesNotFireOnTeleport()
        {
            yield return Load(SceneFlow.BayanPlaza);
            var cue=Object.FindFirstObjectByType<CourtBoundaryPresentation>();var actor=GameServices.Round.PlayerAt(1);
            var lata=GameServices.Round.Lata;Assert.IsTrue(actor.HoldingSlipper);
            actor.Teleport(new Vector3(Balance.ConfinementRadius-.2f,actor.transform.position.y,0));yield return null;
            for(int i=0;i<=Balance.SpawnSettleFrames;i++)yield return new WaitForFixedUpdate();
            yield return null;
            Assert.AreSame(actor,CourtBoundaryPresentation.Viewer(Camera.main));
            var draw=typeof(CourtBoundaryPresentation).GetMethod("BeforeCamera",BindingFlags.Instance|BindingFlags.NonPublic);
            var endDraw=typeof(CourtBoundaryPresentation).GetMethod("AfterCamera",BindingFlags.Instance|BindingFlags.NonPublic);
            var block=new MaterialPropertyBlock();var renderer=cue.GetComponent<Renderer>();
            draw.Invoke(cue,new object[]{Camera.main});renderer.GetPropertyBlock(block);Assert.Greater(block.GetVector("_Exit").z,0);
            var observer=new GameObject("Unowned camera").AddComponent<Camera>();observer.enabled=false;
            draw.Invoke(cue,new object[]{observer});renderer.GetPropertyBlock(block);Assert.AreEqual(0,block.GetVector("_Exit").z);
            endDraw.Invoke(cue,new object[]{observer});renderer.GetPropertyBlock(block);
            Assert.Greater(block.GetVector("_Exit").z,0,"A nested observer render must restore the local camera's cue.");
            endDraw.Invoke(cue,new object[]{Camera.main});
            Object.Destroy(observer.gameObject);
            int before=cue.EscapeCount;actor.GetComponent<CharacterController>().Move(Vector3.right*.4f);yield return null;yield return null;
            Assert.IsFalse(actor.IsInsideBox());Assert.AreEqual(before+1,cue.EscapeCount);
            yield return new WaitForSeconds(.7f);
            actor.Teleport(new Vector3(6,actor.transform.position.y,0));yield return null;
            actor.Teleport(new Vector3(8,actor.transform.position.y,0));yield return null;
            Assert.AreEqual(before+1,cue.EscapeCount,"A teleport must not masquerade as a crossing.");
            actor.Teleport(new Vector3(5,actor.transform.position.y,-2));yield return null;
            string capture="world-owner-exit-"+System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Output));
            var hud=GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture(capture,hud,960,540,false,true);
            System.IO.Directory.CreateDirectory(Output);
            System.IO.File.Copy("Logs/shots-native-ui/"+capture+".png",System.IO.Path.Combine(Output,"owner-exit-armed.png"),true);
            yield return new WaitForSeconds(lata.ProtectionLeft+.05f);lata.HostKnockDown(1);yield return null;
            draw.Invoke(cue,new object[]{Camera.main});renderer.GetPropertyBlock(block);Assert.AreEqual(0,block.GetVector("_Exit").z);
            endDraw.Invoke(cue,new object[]{Camera.main});
        }
        [UnityTest] public IEnumerator RecordedBoundaryIsIndependentAndReplaySilencesPersonalAir()
        {
            yield return Load(SceneFlow.Eskinita);
            var owner=new GameObject("Recorded court contract");
            var recorded=CourtBoundaryPresentation.CreateRecorded(owner.transform,GameServices.Round.Lata);
            var block=new MaterialPropertyBlock();var renderer=recorded.GetComponent<Renderer>();
            recorded.DrawRecorded(false,-1,Vector3.zero);renderer.GetPropertyBlock(block);
            Assert.AreEqual(0,block.GetFloat("_Armed"));Assert.IsTrue(GameServices.Round.Lata.IsUpright);
            recorded.DrawRecorded(true,.4f,Vector3.zero);renderer.GetPropertyBlock(block);
            Assert.AreEqual(1,block.GetFloat("_Armed"));Assert.AreEqual(0,block.GetVector("_Exit").z);
            Assert.Greater(block.GetVector("_Sweep").z,0);
            var viewer=GameServices.Round.PlayerAt(1);viewer.Teleport(new Vector3(2,viewer.transform.position.y,0));
            WorldCueProfile.Current.DangerAudio=.1f;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.Greater(GameServices.Audio.CourtDangerLevel,0,"The live local threat must be audible before replay can silence it.");
            using(GameServices.Audio.EnterReplayMix())
            {
                GameServices.Audio.SetCourtDanger(.1f);yield return null;
                Assert.AreEqual(0,GameServices.Audio.CourtDangerLevel);
            }
            Object.Destroy(owner);
        }
        [UnityTest] public IEnumerator LocalExitAndEscapeBeatVisualStudy()
        {
            yield return Load(SceneFlow.BayanPlaza);
            var actor=GameServices.Round.PlayerAt(1);var court=Object.FindFirstObjectByType<CourtBoundaryPresentation>();
            actor.Teleport(new Vector3(5.8f,actor.transform.position.y,-2));
            for(int i=0;i<=Balance.SpawnSettleFrames;i++)yield return new WaitForFixedUpdate();
            var main=Camera.main;var rig=main.GetComponent<CameraSystem.CameraRig>();rig.enabled=false;
            main.transform.position=actor.transform.position+Vector3.up*1.2f;
            main.transform.LookAt(new Vector3(7,court.Floor,-2));
            Assert.AreSame(actor,CourtBoundaryPresentation.Viewer(main));
            Assert.IsFalse(PresentationClock.BlocksInput,"The local exit study must be in active play, not a blocked-input frame.");
            var hud=GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();Time.timeScale=1;
            foreach(string state in new[]{"before","after","comfort"})
            {
                WorldCueProfile.Current.Boundary=state=="before"?0:1;
                SettingsStore.Current.ReducedEffects=state=="comfort";
                SettingsStore.Current.HighContrastHud=state=="comfort";
                SettingsStore.Current.HudScale=state=="comfort"?1.2f:1;
                yield return null;
                string name="court-local-exit-"+state;
                yield return TumpUiCapture.Capture(name,hud,960,540,false,true);
                System.IO.Directory.CreateDirectory(Output);
                System.IO.File.Copy("Logs/shots-native-ui/"+name+".png",System.IO.Path.Combine(Output,name+".png"),true);
            }
            SettingsStore.Current.ReducedEffects=false;SettingsStore.Current.HighContrastHud=false;
            rig.SetActive(false);main.tag="Untagged";
            var go=new GameObject("Escape beat witness");var witness=go.AddComponent<Camera>();
            witness.CopyFrom(main);witness.enabled=true;witness.tag="MainCamera";witness.fieldOfView=58;
            witness.transform.position=new Vector3(9,court.Floor+1.7f,-4);
            witness.transform.LookAt(new Vector3(7,court.Floor+.12f,-2));
            typeof(GameplayShots).GetMethod("Grade",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{witness});
            Time.timeScale=1;
            for(int step=0;step<18;step++)
            {actor.GetComponent<CharacterController>().Move(Vector3.right*.08f);yield return new WaitForFixedUpdate();}
            yield return null;Time.timeScale=0;
            var puff=Object.FindFirstObjectByType<CourtEscapePuff>();Assert.IsNotNull(puff,"The real crossing must create this studied puff.");
            // Sample the actual triggered effect through the same method replay uses.
            // The13frames represent0..0.4s at30fps; no gameplay timing is changed.
            puff.enabled=false;
            for(int frame=0;frame<13;frame++)
            {
                puff.Sample(frame/30f);
                yield return GameplayShots.Render(witness,"escape-"+frame.ToString("00"),false,Output,actor,960,540);
            }
            witness.tag="Untagged";Object.Destroy(go);main.tag="MainCamera";rig.SetActive(true);rig.enabled=true;
            Time.timeScale=1;
        }
    }
}
