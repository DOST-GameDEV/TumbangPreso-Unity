using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class NewMapHandoffVisibilityTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private CustomRules _rules;private bool _pinned,_bots;private string _settings;
        [UnitySetUp]public IEnumerator Before()
        {
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;_bots=GameLaunch.AllBots;
            _settings=JsonUtility.ToJson(Settings.SettingsStore.Current);yield return PlayModeWorld.Reset();
        }
        [UnityTearDown]public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
            GameLaunch.AllBots=_bots;JsonUtility.FromJsonOverwrite(_settings,Settings.SettingsStore.Current);
        }
        [UnityTest]public IEnumerator BridgeHandoff()=>Flow(SceneFlow.IlalimNgTulay,false,true,true);
        [UnityTest]public IEnumerator CoveHandoff()=>Flow(SceneFlow.LagoonCove,false,true,true);
        [UnityTest]public IEnumerator KantoHandoff()=>Flow(SceneFlow.Kanto,false,true,true);
        [UnityTest]public IEnumerator BridgeHumanRigHandoff()=>Flow(SceneFlow.IlalimNgTulay,false,true,false);
        [UnityTest]public IEnumerator BridgeReduced()=>Flow(SceneFlow.IlalimNgTulay,true,true,true);
        [UnityTest]public IEnumerator CoveReduced()=>Flow(SceneFlow.LagoonCove,true,true,true);
        [UnityTest]public IEnumerator KantoReduced()=>Flow(SceneFlow.Kanto,true,true,true);
        [UnityTest]public IEnumerator BridgeMotionOff()=>Flow(SceneFlow.IlalimNgTulay,false,false,true);
        [UnityTest]public IEnumerator CoveMotionOff()=>Flow(SceneFlow.LagoonCove,false,false,true);
        [UnityTest]public IEnumerator KantoMotionOff()=>Flow(SceneFlow.Kanto,false,false,true);
        private static object Read(object owner,string field)=>owner.GetType().GetField(field,Hidden).GetValue(owner);
        private static bool InFrame(Camera camera,Vector3 point)
        {
            Vector3 view=camera.WorldToViewportPoint(point);
            return view.z>camera.nearClipPlane&&view.x>.05f&&view.x<.95f&&view.y>.05f&&view.y<.95f;
        }
        private IEnumerator Flow(string map,bool reduced,bool motion,bool bots)
        {
            SceneFlow.Networked=false;var rules=CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Bots=CustomGameRules.MaxBots;rules.ManualReady=false;SceneFlow.PinSelectedRules(rules);
            GameLaunch.AllBots=bots;Settings.SettingsStore.Current.CinematicCameraMotion=motion;
            Settings.SettingsStore.Current.ReducedUiMotion=reduced;
            var lens=new GameObject("Handoff test widescreen lens").AddComponent<OpeningCaptureWideLens>();
            Object.DontDestroyOnLoad(lens.gameObject);
            yield return SceneManager.LoadSceneAsync(map);
            var observer=new GameObject("Handoff visibility observer").AddComponent<DirectedArrivalFrameObserver>();
            float until=Time.realtimeSinceStartup+60;int frames=0,empty=0,banked=0;float maxBank=0;
            var lines=new System.Text.StringBuilder("age,visible,right_y,eye,forward\n");
            var audit=System.Environment.GetEnvironmentVariable("TUMP_OPENING_CAPTURE");
            try
            {
                while(GameServices.Round?.RoundActive!=true&&Time.realtimeSinceStartup<until)
                {
                    foreach(var reader in Object.FindObjectsByType<PlayerInputReader>())reader.enabled=false;
                    bool observed=false;System.Exception failure=null;
                    observer.Observe=()=>
                    {
                        try
                        {
                            var arrival=Object.FindAnyObjectByType<MatchArrivalPresentation>();
                            if(arrival==null||!MatchArrivalPresentation.Active||!(bool)Read(arrival,"_directed"))return;
                            float age=(float)Read(arrival,"_directionDrawAge");var direction=Read(arrival,"_direction");
                            float end=(float)direction.GetType().GetField("End").GetValue(direction);
                            if(age<end-1.4f||age>end)return;
                            var camera=(Camera)Read(arrival,"_camera");Assert.IsNotNull(camera);
                            bool visible=false;var lata=GameServices.Round?.Lata;
                            if(lata!=null)visible=InFrame(camera,lata.transform.position+Vector3.up*.4f);
                            if(GameServices.Round!=null)foreach(var player in GameServices.Round.Players)
                                if(player!=null)visible|=InFrame(camera,player.transform.position+Vector3.up*.9f);
                            float bank=Mathf.Abs(camera.transform.right.y);maxBank=Mathf.Max(maxBank,bank);
                            if(!visible)empty++;if(bank>.03f)banked++;frames++;
                            lines.AppendLine($"{age:F4},{visible},{camera.transform.right.y:F5},{camera.transform.position},{camera.transform.forward}");
                        }
                        catch(System.Exception error){failure=error;}
                        finally{observed=true;}
                    };
                    while(!observed)yield return null;
                    if(failure!=null)throw failure;
                }
                Assert.Greater(frames,5,"Observe the actual held handoff before countdown.");
                Assert.IsTrue(GameServices.Round.RoundActive);Assert.IsFalse(PresentationClock.Held);
                Debug.Log($"[NewMapHandoff] map={map} reduced={reduced} motion={motion} bots={bots} frames={frames} empty={empty} banked={banked} maxBank={maxBank:F5}");
                Assert.Zero(empty,"The return must keep a player or the can in the frame instead of looking at empty scenery.");
                Assert.Zero(banked,"The return must stay upright; downward pitch is allowed.");
            }
            finally
            {
                if(!string.IsNullOrEmpty(audit)){Directory.CreateDirectory(audit);File.WriteAllText(Path.Combine(audit,$"{map}-reduced{reduced}-motion{motion}-bots{bots}.csv"),lines.ToString());}
                observer.Observe=null;Object.Destroy(observer.gameObject);Object.Destroy(lens.gameObject);
            }
        }
    }
}
