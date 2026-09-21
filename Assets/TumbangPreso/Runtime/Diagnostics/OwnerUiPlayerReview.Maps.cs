using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator MapReady(string map)
        {
            float deadline=Time.realtimeSinceStartup+45;
            ReadyGate gate=null;
            while(Time.realtimeSinceStartup<deadline)
            {
                var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                gate=Object.FindObjectsByType<ReadyGate>().FirstOrDefault(g=>g.gameObject.scene==active&&g.isActiveAndEnabled);
                if(active.name==map&&Hud.Instance!=null&&gate!=null&&(gate.AwaitingReady||gate.CountingDown))break;
                yield return new WaitForSecondsRealtime(.1f);
            }
            string state="scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" HUD="+(Hud.Instance!=null)+
                " gate="+(gate!=null)+" awaiting="+(gate!=null&&gate.AwaitingReady)+" countdown="+(gate!=null&&gate.CountingDown)+
                " scale="+Time.timeScale+" activeRound="+(GameServices.Round?.RoundActive==true);
            Stage(map+" readiness: "+state);yield return Shot(map+"-ready-boundary");
            if(gate==null||(!gate.AwaitingReady&&!gate.CountingDown))throw new InvalidOperationException("Map did not offer ready: "+state);
            if(gate.AwaitingReady)gate.StartLocalCountdown();
            yield return WaitFor(()=>GameServices.Round.RoundActive,10);
        }

        private IEnumerator MapSurfacesOnly()
        {
            yield return WaitFor(()=>Find("GuestAccount")!=null||Find("ContinueAccount")!=null||Find("StartButton")!=null,80);
            if(Find("GuestAccount")!=null)yield return Click("GuestAccount");
            else if(Find("ContinueAccount")!=null)yield return Click("ContinueAccount");
            SettingsStore.Current.Fullscreen=false;Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            yield return WaitFor(()=>Screen.width==1920&&Screen.height==1080,8);
            var evidence=new StringBuilder("map,mode,quality,view,static_batch_renderers,finished_materials\n");
            File.WriteAllText(Path.Combine(_folder,"map-scope.txt"),
                "Exact native player at 1920x1080. Legal staged owner poses, both modes and all graphics profiles.\n"+
                "Cost windows freeze simulation and compare identical cameras with authored detail disabled/enabled; no image encoding during measurement.\n"+
                "Frame times are process-level measurements on this host, not isolated GPU timings or device certification.\n");
            string selected=Environment.GetEnvironmentVariable("TUMP_NATIVE_MAP");
            if(!string.IsNullOrEmpty(selected)&&!SceneFlow.Maps.Contains(selected))throw new InvalidOperationException("Unknown native map filter");
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            foreach(string map in SceneFlow.Maps.Where(m=>string.IsNullOrEmpty(selected)||m==selected))
            {
                Stage(map+" "+mode+" native material review");
                Time.timeScale=1;SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
                yield return Click("StartButton");yield return Click(mode==GameMode.Classic?"ClassicButton":"HeroStrikeButton");
                yield return Click("PracticeButton");yield return WaitFor(()=>Find("PrimaryButton")!=null);
                // The screen owns a map index and refreshes SelectedMap from it.
                // Select through its real controls rather than overwrite that mirror.
                for(int attempt=0;SceneFlow.SelectedMap!=map&&attempt<SceneFlow.Maps.Length;attempt++)yield return Click("MapNextButton");
                if(SceneFlow.SelectedMap!=map)throw new InvalidOperationException("Map selector did not reach "+map);
                SettingsStore.Current.CharacterPick=mode==GameMode.Classic?Roster.IndexIn(Roster.ClassicPeople,"inday"):0;
                if(GameLaunch.Spectator)yield return Click("SpectateButton");
                Stage(map+" "+mode+" selected; pressing the actual practice button");
                yield return Click("PrimaryButton");
                yield return MapReady(map);
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!=map)throw new InvalidOperationException("Native review loaded the wrong map");
                foreach(var brain in Object.FindObjectsByType<AIController>())brain.enabled=false;
                foreach(var reader in Object.FindObjectsByType<PlayerInputReader>())reader.enabled=false;
                foreach(var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>())switcher.enabled=false;
                var watcher=Object.FindAnyObjectByType<PauseWatcher>();var who=watcher.Local;
                if(mode==GameMode.Classic&&who.GetComponent<Visual.CharacterVisual>().SourceModel!=RosterBook.Load().FindPersonArt("inday").Model)
                    throw new InvalidOperationException("The native Classic review must actually use Inday");
                foreach(var person in GameServices.Round.Players)
                {
                    person.IsBot=true;person.Intent.Clear();person.Intent.Parked=true;
                    if(person!=who)person.Teleport(new Vector3(-5,Slipper.GroundY(new Vector3(-5,0,0)),-5+person.PlayerSlot*3));
                }
                var rig=Camera.main.GetComponent<CameraRig>();rig.Follow(who,true);rig.SetAimSource(AimSource.Mouse);
                var renderers=Object.FindObjectsByType<MeshRenderer>();
                int batches=renderers.Count(r=>r.isPartOfStaticBatch);
                var surfaces=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.HasProperty("_SurfaceKind")&&m.GetFloat("_SurfaceKind")>0).Distinct().ToArray();
                if(surfaces.Length==0)throw new InvalidOperationException("Native map lost all authored finishes");
                if(surfaces.Any(m=>m.GetFloat("_SurfaceDebug")!=0))throw new InvalidOperationException("Diagnostic color mode reached a game material");
                var kinds=surfaces.Select(m=>m.GetFloat("_SurfaceKind")).ToArray();
                var roles=surfaces.Select(m=>m.GetFloat("_SurfaceVertexRoles")).ToArray();
                void Detail(bool enabled)
                {for(int i=0;i<surfaces.Length;i++){surfaces[i].SetFloat("_SurfaceKind",enabled?kinds[i]:0);surfaces[i].SetFloat("_SurfaceVertexRoles",enabled?roles[i]:0);}}
                try
                {
                    var stations=new[]{new Vector3(0,0,-8.8f),new Vector3(7.2f,0,0)};
                    for(int quality=0;quality<GraphicsProfiles.All.Length;quality++)
                    {
                        GraphicsProfiles.Apply(quality);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                        string label=map+"-"+mode+"-"+GraphicsProfiles.Of(quality).Label;
                        for(int station=0;station<stations.Length;station++)
                        {
                            Time.timeScale=1;var at=stations[station];at.y=Slipper.GroundY(at);who.Teleport(at);
                            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Time.timeScale=0;
                            for(int facing=0;facing<2;facing++)
                            {
                                who.transform.rotation=Quaternion.LookRotation((facing==0?-stations[station]:stations[station]).normalized);
                                yield return null;
                                if(!rig.IsLocalFpp||!rig.IsFollowing(who))throw new InvalidOperationException("Review lost the ordinary owner camera");
                                string view=station+(facing==0?"-court":"-edge");yield return Shot(label+"-"+view);
                                evidence.AppendLine($"{map},{mode},{quality},{view},{batches},{surfaces.Length}");
                            }
                        }
                        // Same frozen scene and camera, alternate order between modes.
                        foreach(bool enabled in mode==GameMode.Classic?new[]{false,true}:new[]{true,false})
                        {
                            Detail(enabled);yield return new WaitForSecondsRealtime(.4f);
                            StartFrameWindow(label+(enabled?"-detail":"-plain"));yield return new WaitForSecondsRealtime(1.5f);StopFrameWindow();
                        }
                        Detail(true);
                    }
                    GraphicsProfiles.Apply(2);Time.timeScale=1;
                    if(map==SceneFlow.Eskinita&&mode==GameMode.Classic)
                    {Stage("Native sky drift at real speed, fixed owner view");yield return RecordCatchMotion("sky-drift-realtime",12);}
                    who.Teleport(new Vector3(0,Slipper.GroundY(Vector3.zero),-8));who.transform.rotation=Quaternion.identity;
                    rig.SetAimSource(AimSource.Movement);who.Intent.Parked=false;who.Intent.Move=new Vector2(.45f,.25f);
                    var movie=StartCoroutine(RecordCatchMotion(map+"-"+mode+"-walk",4));
                    yield return movie;who.Intent.Clear();who.Intent.Parked=true;
                    Stage(map+" "+mode+" finishes, quality views, paired cost and normal-speed walk captured");
                }
                finally{Detail(true);Time.timeScale=1;StopFrameWindow();File.WriteAllText(Path.Combine(_folder,"map-views.csv"),evidence.ToString());}
                var pause=Panel.Open<PausePanel>(watcher);pause.Local=who;
                yield return WaitFor(()=>Find("LeaveMatch")!=null);yield return Click("LeaveMatch");
                yield return WaitFor(()=>GameObject.Find("OwnerHomeCanvas")!=null);
            }
        }
    }
}
