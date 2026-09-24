using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Diagnostics
{
    /// <summary>Opt-in short native render baseline, never ordinary gameplay.</summary>
    public sealed class WorldGraphicsProbe:MonoBehaviour
    {
        public static bool Active;
        private static string _output;
        [Serializable] private sealed class Coverage { public string[] maps,qualities; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-tp-graphicsreport");
            Active=index>=0&&index+1<args.Length&&!args.Contains("-tp-tournament");
            _output=Active?Path.GetFullPath(args[index+1]):null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(!Active)return;
            var go=new GameObject("~WorldGraphicsProbe");DontDestroyOnLoad(go);go.AddComponent<WorldGraphicsProbe>();
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(_output);var run=Run();bool failed=false;
            while(Active)
            {
                bool more;
                try{more=run.MoveNext();}
                catch(Exception error){Debug.LogException(error);failed=true;more=false;}
                if(!more)break;yield return run.Current;
            }
            (run as IDisposable)?.Dispose();Application.Quit(failed?2:0);
        }
        private IEnumerator Run()
        {
            const string Header="map,quality,samples,mean_player_frame_ms,p95_player_frame_ms,mean_draw_calls,mean_triangles,mean_setpass,mesh_renderers,static_batched_renderers,unique_materials,world_width,world_height\n";
            var report=new StringBuilder(Header);
            // ⚠️ THE SAME FRAME WITH THE BRIGHT LOOK OFF, IN ITS OWN FILE. LIGHT-1.9 asks what the
            // look's bloom and coloured edges cost, and a baseline from another build is a
            // different binary on a different day. WorldLighting 0 is the scene's own lighting,
            // no bloom and the ink edges. It stays out of world-render.csv because
            // tools/graphics_review.py requires exactly one row per map and quality.
            var lookOff=new StringBuilder(Header);
            var materials=new StringBuilder("map,shader,renderers,material_slots,unique_materials,static_batched_renderers\n");
            // Declare the whole registry before sampling. The runner verifies the
            // exact matrix, including the lagoon, rather than an obsolete12rows.
            File.WriteAllText(Path.Combine(_output,"coverage.json"),JsonUtility.ToJson(new Coverage{
                maps=SceneFlow.Maps.ToArray(),qualities=GraphicsProfiles.All.Select(p=>p.Label).ToArray()}));
            File.WriteAllText(Path.Combine(_output,"scope.txt"),
                "Native player world-camera baseline1920x1080 HDR, four parked players. Not worst-case combat FPS. Ambient life stays active.\n"+
                SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType+" / "+SystemInfo.processorType+"\n");
            try
            {
                foreach(string map in SceneFlow.Maps)
                {
                    if(!Active)yield break;
                    SceneFlow.Networked=false;SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
                    GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
                    yield return SceneManager.LoadSceneAsync(map);
                    for(int warm=0;warm<30;warm++)yield return null;
                    var runner=FindFirstObjectByType<SliceRunner>();if(runner==null)throw new InvalidOperationException("No scene runner");
                    if(!runner.Running)runner.Begin();
                    foreach(var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
                    foreach(var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))input.enabled=false;
                    foreach(var actor in GameServices.Round.Players){actor.Intent.Clear();actor.Intent.Parked=true;}
                    GameServices.Round.BeginRound();Time.timeScale=1;
                    var who=GameServices.Round.PlayerAt(1);
                    float court=Visual.WorldLookPresentation.Current!=null?Visual.WorldLookPresentation.Current.Floor:GameServices.Round.Lata.transform.position.y;
                    var standing=new Vector3(0,court,-10);
                    if(Visual.WorldGround.TryBelow(standing,1,4,out float support))standing.y=support+.03f;
                    who.Teleport(standing);who.transform.rotation=Quaternion.identity;
                    var rig=FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
                    var camera=rig.Camera;var previous=camera.targetTexture;
                    var target=new RenderTexture(1920,1080,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear){antiAliasing=Mathf.Max(1,QualitySettings.antiAliasing)};
                    target.Create();camera.targetTexture=target;
                    try
                    {
                        var environment=FindFirstObjectByType<Visual.EnvColourPass>();
                        var dressing=environment.transform.Find("Dressing")??environment.transform;
                        var renderers=dressing.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                        foreach(var group in renderers.GroupBy(r=>string.Join("+",r.sharedMaterials.Select(m=>m!=null?m.shader.name:"missing"))))
                            materials.AppendLine($"{map},{group.Key.Replace(',',' ')},{group.Count()},{group.Sum(r=>r.sharedMaterials.Length)},{group.SelectMany(r=>r.sharedMaterials).Distinct().Count()},{group.Count(r=>r.isPartOfStaticBatch)}");
                        for(int quality=0;quality<GraphicsProfiles.All.Length;quality++)
                        {
                            GraphicsProfiles.Apply(quality);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                            for(int warm=0;warm<30;warm++)yield return null;
                            string Row(Sampled at)=>FormattableString.Invariant($"{map},{GraphicsProfiles.Of(quality).Label},120,{at.Mean:F3},{at.P95:F3},{at.Draw:F1},{at.Triangles:F0},{at.SetPass:F1},{renderers.Length},{renderers.Count(r=>r.isPartOfStaticBatch)},{renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count()},1920,1080");
                            var shipped=new Sampled();yield return Measure(shipped);report.AppendLine(Row(shipped));
                            Debug.Log("[WorldGraphicsProbe] recorded "+map+" / "+GraphicsProfiles.Of(quality).Label);
                            var readback=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                            var active=RenderTexture.active;
                            var resolved=RenderTexture.GetTemporary(1920,1080,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                            try
                            {
                                // HDR world pixels are linear; resolve to the display
                                // transfer function before writing an sRGB PNG.
                                Graphics.Blit(target,resolved);
                                RenderTexture.active=resolved;readback.ReadPixels(new Rect(0,0,1920,1080),0,0);readback.Apply();
                                File.WriteAllBytes(Path.Combine(_output,map+"-"+GraphicsProfiles.Of(quality).Label+".png"),readback.EncodeToPNG());
                            }
                            finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(resolved);Destroy(readback);}
                            if(Visual.WorldLookPresentation.Current!=null)
                            {
                                var cue=Visual.WorldCueProfile.Current;float weight=cue.WorldLighting;
                                cue.WorldLighting=0;
                                try
                                {
                                    for(int warm=0;warm<30;warm++)yield return null;
                                    var off=new Sampled();yield return Measure(off);lookOff.AppendLine(Row(off));
                                }
                                finally{cue.WorldLighting=weight;}
                            }
                        }
                    }
                    finally{camera.targetTexture=previous;target.Release();Destroy(target);}
                }
            }
            finally
            {
                File.WriteAllText(Path.Combine(_output,"world-render.csv"),report.ToString());
                File.WriteAllText(Path.Combine(_output,"world-render-look-off.csv"),lookOff.ToString());
                File.WriteAllText(Path.Combine(_output,"materials.csv"),materials.ToString());
            }
        }
        private sealed class Sampled{public float Mean,P95;public double Draw,Triangles,SetPass;}
        // 120 uncapped frames. A counter the player does not support reads -1.
        private static IEnumerator Measure(Sampled into)
        {
            using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            using var tris=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);
            using var passes=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count",1);
            var times=new float[120];long draw=0,triangles=0,setpass=0;
            for(int sample=0;sample<times.Length;sample++)
            {
                yield return null;times[sample]=Time.unscaledDeltaTime*1000;
                if(draws.Valid)draw+=draws.LastValue;if(tris.Valid)triangles+=tris.LastValue;if(passes.Valid)setpass+=passes.LastValue;
            }
            into.Mean=times.Average();Array.Sort(times);into.P95=times[(int)(times.Length*.95f)-1];
            into.Draw=draw>0?draw/120d:-1;into.Triangles=triangles>0?triangles/120d:-1;into.SetPass=setpass>0?setpass/120d:-1;
        }
    }
}
