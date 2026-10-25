using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // Actual map installations/graphics and natural automatic arrival. Actors are
    // held still after round start to isolate camera framing, not natural match play.
    public sealed class SpectatorMapCoverageTests
    {
        private INetProvider _provider;
        private CustomRules _rules;
        private bool _pinned, _spectator, _allBots, _networked;
        private string _map, _launchMap;
        private CursorLockMode _cursor;
        private bool _cursorVisible;
        private bool _cinematicMotion;
        private static bool QuickFraming => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-tp-spectator-quick-framing") >= 0;
        private sealed class Solo : INetProvider
        { public bool IsHost => true; public bool IsNetworked => false; public int LocalSlot => -1;
          public int LocalPeerId => 0; public bool IsSeatlessReferee => false; }
        [UnitySetUp] public IEnumerator Before()
        {
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            _spectator=GameLaunch.Spectator;_allBots=GameLaunch.AllBots;_networked=SceneFlow.Networked;
            _map=SceneFlow.SelectedMap;_launchMap=GameLaunch.SelectedMap;_provider=NetAuthority.Provider;
            _cursor=Cursor.lockState;_cursorVisible=Cursor.visible;
            _cinematicMotion=Settings.SettingsStore.Current.CinematicCameraMotion;
            yield return PlayModeWorld.Reset();NetAuthority.Provider=new Solo();
            if(QuickFraming)Settings.SettingsStore.Current.CinematicCameraMotion=false;
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_provider;
            GameLaunch.Spectator=_spectator;GameLaunch.AllBots=_allBots;SceneFlow.Networked=_networked;
            SceneFlow.SelectedMap=_map;GameLaunch.SelectedMap=_launchMap;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
            Cursor.lockState=_cursor;Cursor.visible=_cursorVisible;
            Settings.SettingsStore.Current.CinematicCameraMotion=_cinematicMotion;
        }
        [UnityTest] public IEnumerator Eskinita() => Inspect(SceneFlow.Eskinita);
        [UnityTest] public IEnumerator IlalimNgTulay() => Inspect(SceneFlow.IlalimNgTulay);
        [UnityTest] public IEnumerator LagoonCove() => Inspect(SceneFlow.LagoonCove);
        [UnityTest] public IEnumerator Kanto() => Inspect(SceneFlow.Kanto);
        [UnityTest] public IEnumerator Arena() => Inspect(SceneFlow.Arena);
        [UnityTest] public IEnumerator Payo() => Inspect(SceneFlow.Payo);
        private IEnumerator Inspect(string map)
        {
            GameLaunch.Spectator=true;GameLaunch.AllBots=false;SceneFlow.Networked=false;
            var rules=CustomGameRules.Defaults(GameMode.HeroStrike);rules.ManualReady=false;SceneFlow.PinSelectedRules(rules);
            Net.NetBootstrap.AdoptLaunchMap(map);
            yield return SceneManager.LoadSceneAsync(map);
            float until=Time.realtimeSinceStartup+80;
            while((GameServices.Round?.RoundActive!=true || PresentationClock.Held || SpectatorCutscene.Hides)
                && Time.realtimeSinceStartup<until)yield return null;
            Assert.IsTrue(GameServices.Round?.RoundActive==true,"Natural automatic arrival did not start: "+map);
            Assert.IsFalse(SpectatorCutscene.Hides,map);
            foreach(var brain in Object.FindObjectsByType<AIController>())brain.enabled=false;
            foreach(var reader in Object.FindObjectsByType<PlayerInputReader>())reader.enabled=false;
            foreach(var actor in GameServices.Round.Players)if(actor!=null)actor.enabled=false;
            var watcher=Object.FindAnyObjectByType<SpectatorCamera>();Assert.IsNotNull(watcher,map);
            var lens=watcher.GetComponent<Camera>();
            Assert.IsTrue(watcher.TryWatchPlayer(1),map);for(int i=0;i<8;i++)yield return null;
            Capture(lens,map+"-eyes");
            Assert.IsTrue(watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov),map);
            for(int i=0;i<15;i++)yield return null;
            AssertVisible(lens,SpectatorFraming.BodyFocus(watcher.FollowedPlayer),map+" follow");Capture(lens,map+"-follow");
            Assert.IsTrue(watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Autopilot),map);
            yield return new WaitForSecondsRealtime(6);
            var director=watcher.GetComponent<SpectatorDirector>();Assert.IsNotNull(director,map);Assert.IsTrue(director.Engaged,map);
            var shot=(SpectatorInterest)typeof(SpectatorDirector).GetField("_shot",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(director);
            Assert.IsTrue(shot.Valid,map);AssertVisible(lens,SpectatorFraming.BodyFocus(shot.Main),map+" auto");
            if(shot.Shot==ShotType.QuietEstablish)
                foreach(var actor in GameServices.Round.Players)
                {
                    if(actor==null || !actor.gameObject.activeInHierarchy)continue;
                    AssertVisible(lens,SpectatorFraming.BodyFocus(actor),map+" quiet P"+actor.PlayerSlot);
                    var bounds=SpectatorFraming.BodyBounds(actor);
                    for(int i=0;i<8;i++)
                    {
                        var sign=new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1);
                        var screen=lens.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,sign));
                        Assert.That(screen.x,Is.InRange(.02f,.98f),map+" body width");
                        Assert.That(screen.y,Is.InRange(.02f,.98f),map+" body height");
                    }
                }
            Capture(lens,map+"-auto");
            Debug.Log($"[SpectatorMap] {map}: {director.ShotName()}, cuts={director.Cuts}, fallback={director.SafePoseFallbacks}, eye={lens.transform.position}");
        }
        private static void AssertVisible(Camera lens,Vector3 point,string label)
        {
            var screen=lens.WorldToViewportPoint(point);Assert.Greater(screen.z,.05f,label);
            Assert.That(screen.x,Is.InRange(.02f,.98f),label);Assert.That(screen.y,Is.InRange(.02f,.98f),label);
            var delta=point-lens.transform.position;
            foreach(var hit in Physics.RaycastAll(lens.transform.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                Assert.IsTrue(hit.collider.GetComponentInParent<CharacterMotor>()!=null || hit.collider.GetComponentInParent<Slipper>()!=null
                    || hit.collider.GetComponentInParent<Lata>()!=null,label+" blocked by "+hit.collider.name);
        }
        private static void Capture(Camera lens,string name)
        {
            string folder=QuickFraming?"Logs/spectator-1025/maps-framing":"Logs/spectator-1025/maps";
            Directory.CreateDirectory(folder);
            var previous=lens.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                lens.targetTexture=target;lens.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
                File.WriteAllBytes(folder+"/"+name+".png",pixels.EncodeToPNG());
            }
            finally {lens.targetTexture=previous;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);}
        }
    }
}
