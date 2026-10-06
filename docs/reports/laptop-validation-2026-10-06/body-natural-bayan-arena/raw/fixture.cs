using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorBodyNaturalReview
    {
        private bool _bots,_watch,_guided,_training,_pinned;
        private int _seat,_pick;
        private GameMode _mode;
        private CustomRules _rules;
        private INetProvider _provider;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_watch=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _guided=GameLaunch.GuidedTutorial;_training=GameLaunch.TrainingRange;
            _mode=SceneFlow.SelectedMode;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            _pick=Settings.SettingsStore.Current.CharacterPick;_provider=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            Settings.SettingsStore.Current.CharacterPick=_pick;NetAuthority.Provider=_provider;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_watch;GameLaunch.SoloSeat=_seat;
            GameLaunch.GuidedTutorial=_guided;GameLaunch.TrainingRange=_training;SceneFlow.SelectedMode=_mode;
            SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest] public IEnumerator BayanNaturalCameraAfterBodyLifetimeFix()
        {
            yield return Capture(GameMode.HeroStrike,1,SceneFlow.BayanPlaza);
        }
        [UnityTest] public IEnumerator ArenaNaturalCameraAfterBodyLifetimeFix()
        {
            yield return Capture(GameMode.HeroStrike,1,SceneFlow.Arena);
        }
        private static List<int> CoveringPicks(GameMode mode)
        {
            int count=Roster.GetPeople(mode).Count;
            var remaining=new HashSet<int>(Enumerable.Range(0,count));var picks=new List<int>();
            while(remaining.Count>0)
            {
                int best=-1,bestGain=0;
                for(int pick=0;pick<count;pick++)
                {
                    var ids=new HashSet<int>{pick};
                    foreach(int slot in new[]{0,2,3})ids.Add(MatchInstaller.ResolveAiCharacterIndex(slot,pick,mode));
                    int gain=ids.Count(remaining.Contains);
                    if(gain>bestGain){best=pick;bestGain=gain;}
                }
                Assert.Greater(bestGain,0);picks.Add(best);remaining.Remove(best);
                foreach(int slot in new[]{0,2,3})remaining.Remove(MatchInstaller.ResolveAiCharacterIndex(slot,best,mode));
            }
            return picks;
        }
        private IEnumerator Capture(GameMode mode,int group,string map)
        {
            var picks=CoveringPicks(mode);Assert.AreEqual(4,picks.Count,"Current complete covering set must match the four declared cases.");
            int pick=picks[group];string output="Logs/spectator-body-natural1006/"+Path.GetFileNameWithoutExtension(map)+"/"+mode+"-"+pick;
            Directory.CreateDirectory(output);
            NetAuthority.Provider=null;SceneFlow.SelectedMode=mode;SceneFlow.PinSelectedRules(CustomGameRules.Defaults(mode));
            Settings.SettingsStore.Current.CharacterPick=pick;
            GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
            GameLaunch.GuidedTutorial=false;GameLaunch.TrainingRange=false;
            yield return SceneManager.LoadSceneAsync(map);yield return null;
            var installer=Object.FindFirstObjectByType<MatchInstaller>();Assert.IsNotNull(installer);
            var round=GameServices.Round;Assert.IsNotNull(round);
            var cast=round.Players.Where(p=>p!=null).OrderBy(p=>p.PlayerSlot).ToArray();Assert.AreEqual(4,cast.Length);
            for(int slot=0;slot<4;slot++)Assert.AreEqual(slot==1?pick:MatchInstaller.ResolveAiCharacterIndex(slot,pick,mode),cast[slot].CharacterIndex);
            GameLaunch.Spectator=true;installer.RebindLocalSeat(-1,true);yield return null;
            foreach(var body in cast)
            {
                Assert.IsFalse(body.GetComponents<PlayerInputReader>().Any(r=>r.enabled));
                Assert.IsTrue(body.GetComponents<AIController>().Any(ai=>ai.enabled));
            }
            Assert.IsFalse(cast[1].IsBot,"One former human retains its seat provenance.");
            var runner=Object.FindFirstObjectByType<SliceRunner>();Assert.IsNotNull(runner);runner.Begin();
            var gate=Object.FindFirstObjectByType<ReadyGate>();
            while(MatchArrivalPresentation.Active || (gate!=null && (gate.AwaitingReady || gate.CountingDown)))yield return null;
            yield return null;yield return null;
            var watcher=Object.FindFirstObjectByType<SpectatorCamera>();Assert.IsNotNull(watcher);
            var director=watcher.GetComponent<SpectatorDirector>();Assert.IsNotNull(director);director.Engaged=true;
            var camera=watcher.GetComponent<Camera>();Assert.IsTrue(camera.enabled);
            File.WriteAllText(output+"/installed-cast.txt",string.Join("\n",cast.Select(p=>p.PlayerSlot+" "+Roster.PersonIdAt(mode,p.CharacterIndex)+" IsBot="+p.IsBot)));
            var paete=cast.First(p=>Roster.PersonIdAt(mode,p.CharacterIndex)=="paete");
            File.WriteAllText(output+"/hierarchy.txt",string.Join("\n",paete.GetComponentsInChildren<Transform>(true).Select(t=>t.name+" parent="+(t.parent!=null?t.parent.name:"none")+" renderers="+t.GetComponents<Renderer>().Length)));
            float start=Time.unscaledTime,clock=round.TimeLeft,sample=0;int image=0,observedFrames=0,enclosedFrames=0;
            var meshes=new List<Renderer>();
            while(Time.unscaledTime-start<30 && image<5)
            {
                float elapsed=Time.unscaledTime-start;
                observedFrames++;
                var enclosed=new List<string>();
                foreach(var body in round.Bodies)
                {
                    if(body==null)continue;var model=body.GetComponent<CharacterVisual>()?.Model;
                    if(model==null)continue;meshes.Clear();model.GetComponentsInChildren(false,meshes);
                    foreach(var mesh in meshes)
                        if(mesh.enabled && (mesh is MeshRenderer || mesh is SkinnedMeshRenderer)
                            && mesh.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                            && mesh.bounds.Contains(camera.transform.position))
                            enclosed.Add(body.PlayerSlot+":"+mesh.name);
                }
                if(enclosed.Count>0)
                {
                    enclosedFrames++;
                    File.AppendAllText(output+"/eye-enclosures.tsv",elapsed.ToString("F3")+"\t"+Time.frameCount+"\t"+camera.transform.position.ToString("F4")+"\t"+string.Join(",",enclosed)+"\n");
                }
                if(elapsed>=sample)
                {
                    sample=elapsed+.5f;
                    File.AppendAllText(output+"/timeline.tsv",elapsed.ToString("F3")+"\t"+round.TimeLeft.ToString("F3")+"\t"+director.Beat+"\t"+director.Shot+"\t"+director.ShotName()+"\t"+director.Cuts+"\t"+director.SafePoseFallbacks+"\n");
                }
                if(image<5 && elapsed>=image*5)
                {
                    yield return RenderWitness(camera,director,paete,output,"cast-natural-"+image);image++;
                }
                yield return null;
            }
            File.WriteAllText(output+"/observation-summary.json", "{\"observedFrames\":"+observedFrames+",\"enclosedFrames\":"+enclosedFrames+",\"elapsed\":"+(Time.unscaledTime-start).ToString("F3")+",\"images\":"+image+",\"cuts\":"+director.Cuts+",\"fallbacks\":"+director.SafePoseFallbacks+"}");
            Assert.AreEqual(5,Directory.GetFiles(output,"cast-natural-*.png").Length);
            Assert.AreEqual(0,enclosedFrames,"A sampled actual spectator eye is in a visible installed body enclosure; retain the raw event and inspect rather than calling this full visual acceptance.");
            Assert.Less(round.TimeLeft,clock);
        }
        private IEnumerator RenderWitness(Camera camera,SpectatorDirector director,CharacterMotor actor,string output,string name)
        {
            var render=GameplayShots.Render(camera,name,flipCanvases:true,outDir:output,width:1600,height:900);
            while(true)
            {
                if(camera.targetTexture!=null && camera.targetTexture.width==1600)
                {
                    foreach(var renderer in actor.GetComponent<CharacterVisual>().Model.GetComponentsInChildren<Renderer>())
                    {
                        if(!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))continue;
                        var b=renderer.bounds;
                        File.AppendAllText(output+"/"+name+"-bounds.tsv",Time.unscaledTime.ToString("F4")+"\t"+Time.frameCount+"\t"+director.ShotName()+"\t"+camera.transform.position.ToString("F4")+"\t"+camera.transform.eulerAngles.ToString("F4")+"\t"+camera.nearClipPlane.ToString("F4")+"\t"+actor.transform.position.ToString("F4")+"\t"+renderer.name+"\t"+b.min.ToString("F4")+"\t"+b.max.ToString("F4")+"\t"+b.Contains(camera.transform.position)+"\t"+renderer.enabled+"\t"+Vector3.Distance(camera.transform.position,b.ClosestPoint(camera.transform.position)).ToString("F4")+"\n");
                    }
                    foreach(var collider in actor.GetComponentsInChildren<Collider>())
                        File.AppendAllText(output+"/"+name+"-colliders.tsv",Time.unscaledTime.ToString("F4")+"\t"+collider.name+"\t"+camera.transform.position.ToString("F4")+"\t"+collider.ClosestPoint(camera.transform.position).ToString("F4")+"\t"+collider.enabled+"\t"+collider.isTrigger+"\n");
                }
                if(!render.MoveNext())break;
                yield return render.Current;
            }
        }    }
}
