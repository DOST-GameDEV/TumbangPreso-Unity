using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class MapPreviewTransitionTests
    {
        private bool _preview,_bots,_spectator;
        private int _seat;
        private string _map;private bool _pinned;private CustomRules _rules;
        [UnitySetUp]public IEnumerator Before()
        {
            _map=SceneFlow.SelectedMap;_pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();
            _preview=MatchInstaller.PreviewOnly;_bots=GameLaunch.AllBots;
            _spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset();
            MatchInstaller.PreviewOnly=false;GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
        }
        [UnityTearDown]public IEnumerator After()
        {
            MatchInstaller.PreviewOnly=false;yield return PlayModeWorld.Reset();
            MatchInstaller.PreviewOnly=_preview;GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.SelectedMap=_map;SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator PreviewUsesSelectedBrightLookAcrossRefreshAndCachedMapSwitches()
        {
            float previousWeight=Visual.WorldCueProfile.Current.WorldLighting;
            Visual.WorldCueProfile.Current.WorldLighting=1;
            var root=new GameObject("Bright map preview",typeof(RectTransform),typeof(RawImage));
            var preview=root.AddComponent<MapPreviewSurface>();
            var menuCamera=new GameObject("Unrelated menu camera").AddComponent<Camera>();
            menuCamera.tag="MainCamera";
            try
            {
                foreach(string map in new[]{SceneFlow.Eskinita,SceneFlow.Lagoon,SceneFlow.Eskinita})
                {
                    preview.Show(map);float until=Time.realtimeSinceStartup+20;
                    while(preview.Showing!=map && Time.realtimeSinceStartup<until)yield return null;
                    Assert.AreEqual(map,preview.Showing);yield return null;
                    var look=Visual.WorldLookPresentation.Current;
                    Assert.IsNotNull(look);Assert.AreEqual(map,look.Look.Map);
                    Assert.IsTrue(Visual.WorldLookPresentation.HandlesCamera(preview.Camera));
                    Assert.IsFalse(Visual.WorldLookPresentation.HandlesCamera(menuCamera),"Preview must not grade the menu's main camera.");
                    var sky=RenderSettings.skybox;
                    // Real setup refreshes call this repeatedly. It must retain the same
                    // rig/sky and restore the selected map's bright fog after every refresh.
                    for(int i=0;i<3;i++)preview.ReapplyEnvironment();
                    Assert.AreSame(look,Visual.WorldLookPresentation.Current);
                    Assert.AreSame(sky,RenderSettings.skybox);
                    Assert.That(RenderSettings.fogEndDistance,Is.EqualTo(look.Look.FogEnd).Within(.01f));
                    Assert.That(look.Floor,Is.LessThan(3),"Use the deck/court, not an elevated camera or spawn marker.");
                    if(look.Look.GroundLift>1)
                    {
                        int lifted=0;var block=new MaterialPropertyBlock();
                        foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                        {
                            var b=renderer.bounds;
                            if(renderer.gameObject.scene!=look.gameObject.scene || b.size.y>.6f || b.size.x<6 || b.size.z<6 ||
                                Mathf.Abs(b.max.y-look.Floor)>.35f || b.min.x>0 || b.max.x<0 || b.min.z>0 || b.max.z<0)continue;
                            var materials=renderer.sharedMaterials;
                            for(int i=0;i<materials.Length;i++)
                            {
                                if(materials[i]==null || !materials[i].HasProperty("_Color"))continue;
                                renderer.GetPropertyBlock(block,i);Color source=materials[i].GetColor("_Color");
                                Color expected=source*look.Look.GroundLift;expected.a=source.a;
                                if(((Vector4)(block.GetColor("_Color")-expected)).sqrMagnitude<.0001f)lifted++;
                            }
                        }
                        Assert.Greater(lifted,0,"Revisiting a cached map must retain the visible court lift.");
                    }
                }
            }
            finally
            {
                Object.Destroy(root);Object.Destroy(menuCamera.gameObject);
                Visual.WorldCueProfile.Current.WorldLighting=previousWeight;
            }
            yield return null;
            Assert.IsNull(Visual.WorldLookPresentation.Current,"Destroying preview must release its look before a real match.");
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator TheLastDestinationWinsWhilePreviewLoadingFinishes()
        {
            var root=new GameObject("Changing destination during preview",typeof(RectTransform),typeof(RawImage));
            root.AddComponent<MapPreviewSurface>().Show(SceneFlow.SaBubong);
            Assert.IsTrue(MatchInstaller.PreviewOnly);
            SceneFlow.Go(SceneFlow.SaBubong);SceneFlow.Go(SceneFlow.MainMenu);
            float until=Time.realtimeSinceStartup+20;
            while(Time.realtimeSinceStartup<until&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!=SceneFlow.MainMenu)yield return null;
            Assert.AreEqual(SceneFlow.MainMenu,UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Assert.IsFalse(MatchInstaller.PreviewOnly);Assert.IsTrue(root==null);
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator StartingWhileTheMapPreviewLoadsBuildsTheRealMatch()
        {
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));SceneFlow.SelectedMap=SceneFlow.SaBubong;
            var root=new GameObject("Pending map preview",typeof(RectTransform),typeof(RawImage));
            var preview=root.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.SaBubong);
            Assert.IsTrue(MatchInstaller.PreviewOnly,"The regression must request start during the actual additive preview load");
            SceneFlow.StartMatch();
            float until=Time.realtimeSinceStartup+20;
            while(Time.realtimeSinceStartup<until&&(Hud.Instance==null||Object.FindAnyObjectByType<ReadyGate>()==null))yield return null;
            Assert.IsFalse(MatchInstaller.PreviewOnly,"A cancelled preview left the real installer disabled");
            Assert.IsTrue(root==null,"The preview setup survived the real single-scene match transition");
            Assert.IsNotNull(Hud.Instance,"The real match did not create its HUD");
            var gate=Object.FindAnyObjectByType<ReadyGate>();Assert.IsNotNull(gate,"The real match did not offer ready");
            Assert.IsTrue(gate.AwaitingReady);gate.StartLocalCountdown();
            until=Time.realtimeSinceStartup+6;
            while(Time.realtimeSinceStartup<until&&!GameServices.Round.RoundActive)yield return null;
            Assert.IsTrue(GameServices.Round.RoundActive,"The actual ready gate never began the round");
        }
    }
}
