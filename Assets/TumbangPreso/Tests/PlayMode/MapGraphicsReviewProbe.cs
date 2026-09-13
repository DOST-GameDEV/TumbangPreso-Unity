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
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>Matched world-render evidence. Editor timings are not shipping FPS claims.</summary>
    [Category("WallClock")]
    public sealed class MapGraphicsReviewProbe
    {
        private int _quality,_sync,_target;
        private bool _bots,_spectator,_pinned,_networked;
        private int _seat;
        private CustomRules _rules;
        private static string Output=>Environment.GetEnvironmentVariable("TUMP_GRAPHICS_REVIEW")??"Logs/map-graphics-review-v1";
        [UnitySetUp] public IEnumerator Before()
        {
            _quality=GraphicsProfiles.Current;_sync=QualitySettings.vSyncCount;_target=Application.targetFrameRate;
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_networked=SceneFlow.Networked;_rules=SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();Directory.CreateDirectory(Output);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GraphicsProfiles.Apply(_quality);
            QualitySettings.vSyncCount=_sync;Application.targetFrameRate=_target;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.Networked=_networked;SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest,Timeout(240000)]
        public IEnumerator FourMapsReportMatchedWorldFramesAndQualityDifferences()
        {
            var report=new StringBuilder("map,quality,samples,mean_editor_frame_ms,p95_editor_frame_ms,mean_draw_calls,mean_triangles,mean_setpass,world_width,world_height\n");
            File.WriteAllText(Path.Combine(Output,"scope.txt"),
                "Editor world-render baseline, not a player FPS benchmark. Main world camera renders1920x1080 HDR; overlay UI is outside that target.\n"+
                "Same fixed eye position, no bot inputs. Ambient animals remain active, so small visit variation is expected.\n"+
                SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType+" / "+SystemInfo.processorType+"\n");
            try
            {
                foreach(string map in SceneFlow.Maps)
                {
                    yield return MapRetrievalProbe.Load(map);GameServices.Round.BeginRound();Time.timeScale=1;
                    var who=GameServices.Round.PlayerAt(1);who.Intent.Clear();who.Intent.Parked=true;
                    who.Teleport(new Vector3(0,map==SceneFlow.IlalimNgTulay?.212f:.1f,-10));
                    who.transform.rotation=Quaternion.identity;
                    var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
                    var camera=rig.Camera;
                    var previous=camera.targetTexture;
                    var target=new RenderTexture(1920,1080,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear)
                        {antiAliasing=Mathf.Max(1,QualitySettings.antiAliasing)};
                    target.Create();camera.targetTexture=target;
                    try
                    {
                        for(int quality=0;quality<GraphicsProfiles.All.Length;quality++)
                        {
                            GraphicsProfiles.Apply(quality);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                            for(int warm=0;warm<30;warm++)yield return null;
                            var times=new float[120];long draw=0,triangles=0,setpass=0;
                            for(int sample=0;sample<times.Length;sample++)
                            {
                                yield return null;times[sample]=Time.unscaledDeltaTime*1000;
                                draw+=UnityStats.drawCalls;triangles+=UnityStats.triangles;setpass+=UnityStats.setPassCalls;
                            }
                            Assert.Greater(draw,0,map+" produced no rendered-world counter samples");
                            float mean=times.Average();Array.Sort(times);float p95=times[(int)(times.Length*.95f)-1];
                            report.AppendLine(FormattableString.Invariant($"{map},{GraphicsProfiles.Of(quality).Label},{times.Length},{mean:F3},{p95:F3},{draw/(double)times.Length:F1},{triangles/(double)times.Length:F0},{setpass/(double)times.Length:F1},1920,1080"));
                            yield return GameplayShots.Render(camera,map+"-"+GraphicsProfiles.Of(quality).Label,false,Output);
                        }
                    }
                    finally{camera.targetTexture=previous;target.Release();Object.Destroy(target);}
                    yield return PlayModeWorld.Reset();
                }
            }
            finally{File.WriteAllText(Path.Combine(Output,"world-render.csv"),report.ToString());}
        }
    }
}
