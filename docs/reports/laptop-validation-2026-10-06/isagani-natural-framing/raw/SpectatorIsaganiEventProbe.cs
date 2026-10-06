using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
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
    public sealed class SpectatorIsaganiEventProbe
    {
        private bool _bots, _spectator, _guided, _training, _pinned;
        private int _seat;
        private CustomRules _rules;
        private GameMode _mode;
        private INetProvider _provider;
        private string _map; private string Output => "Logs/spectator-isagani-event-1006/"+_map;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots; _spectator=GameLaunch.Spectator; _seat=GameLaunch.SoloSeat;
            _guided=GameLaunch.GuidedTutorial; _training=GameLaunch.TrainingRange;
            _mode=SceneFlow.SelectedMode; _pinned=SceneFlow.RulesPinned; _rules=SceneFlow.SelectedRules.Clone();
            _provider=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots=_bots; GameLaunch.Spectator=_spectator; GameLaunch.SoloSeat=_seat;
            GameLaunch.GuidedTutorial=_guided; GameLaunch.TrainingRange=_training;
            NetAuthority.Provider=_provider; SceneFlow.SelectedMode=_mode;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest] public IEnumerator ObserveNaturalIsaganiThrowFraming()
        {
            yield return CaptureMap(SceneFlow.BayanPlaza);
        }
        private IEnumerator CaptureMap(string map)
        {
            _map=map; Directory.CreateDirectory(Output);
            NetAuthority.Provider=null;
            SceneFlow.SelectedMode=GameMode.HeroStrike;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.AllBots=true; GameLaunch.Spectator=true;
            GameLaunch.GuidedTutorial=false; GameLaunch.TrainingRange=false;
            yield return SceneManager.LoadSceneAsync(map, LoadSceneMode.Single);
            yield return null;
            var runner=Object.FindFirstObjectByType<SliceRunner>(); Assert.IsNotNull(runner);
            runner.Begin();
            var gate=Object.FindFirstObjectByType<ReadyGate>();
            while (MatchArrivalPresentation.Active || (gate!=null && (gate.AwaitingReady || gate.CountingDown))) yield return null;
            var camera=Object.FindFirstObjectByType<SpectatorCamera>(); Assert.IsNotNull(camera);
            var director=camera.GetComponent<SpectatorDirector>(); Assert.IsNotNull(director);
            director.Engaged=true;
            var lens=camera.GetComponent<Camera>(); Assert.IsTrue(lens.enabled);
            var round=GameServices.Round; Assert.IsNotNull(round); Assert.IsTrue(round.RoundActive);
            Assert.AreEqual(4,round.Players.Count(p=>p!=null && p.IsBot));
            var log=new StringBuilder("time,clock,beat,shot,cuts,fallbacks,eye,cast\n");
            float start=Time.unscaledTime, initialClock=round.TimeLeft, sampleAt=0;
            int image=0;
            while (Time.unscaledTime-start<30 && image<1)
            {
                float elapsed=Time.unscaledTime-start;
                if (elapsed>=sampleAt)
                {
                    sampleAt=elapsed+.5f;
                    string cast=string.Join(";",round.Players.Where(p=>p!=null).Select(p=>p.PlayerSlot+":"+Roster.PersonIdAt(p.Mode,p.CharacterIndex)+":"+p.transform.position.ToString("F3")));
                    log.AppendLine(elapsed.ToString("F3")+","+round.TimeLeft.ToString("F3")+","+director.Beat+","+director.Shot+","+director.Cuts+","+director.SafePoseFallbacks+",\""+camera.transform.position.ToString("F3")+"\",\""+cast+"\"");
                    File.WriteAllText(Output+"/timeline.csv",log.ToString());
                }
                if (director.Beat==SpectatorBeat.ThrowPrep && IsIsagani(director))
                {
                    yield return RenderWithProjection(lens,director,"map-natural-"+image);
                    image++;
                }
                yield return null;
            }
            Assert.AreEqual(1,Directory.GetFiles(Output,"map-natural-*.png").Length);
            Assert.Less(round.TimeLeft,initialClock,"Capture must cover active simulation rather than a held introduction.");
            Debug.Log("Natural "+map+" capture completed: "+image+" actual rendered frames, "+director.Cuts+" cuts, "+director.SafePoseFallbacks+" fallbacks.");
        }
        private IEnumerator RenderWithProjection(Camera lens, SpectatorDirector director, string name)
        {
            var render=GameplayShots.Render(lens,name,flipCanvases:true,outDir:Output,width:1600,height:900);
            while (true)
            {
                if (lens.targetTexture!=null && lens.targetTexture.width==1600)
                {
                    var shot=(SpectatorInterest)typeof(SpectatorDirector).GetField("_shot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(director);
                    var main=shot.Main;
                    Vector3 world=main!=null?main.transform.position+Vector3.up*SpectatorDirector.SubjectEyeLine:Vector3.zero;
                    Vector3 viewport=lens.WorldToViewportPoint(world);
                    File.AppendAllText(Output+"/"+name+"-projection.tsv",Time.unscaledTime.ToString("F3")+"\t"+Time.frameCount+"\t"+(main!=null?main.PlayerSlot:-1)+"\t"+director.ShotName()+"\t"+world.ToString("F4")+"\t"+viewport.ToString("F4")+"\t"+lens.transform.position.ToString("F4")+"\t"+lens.transform.eulerAngles.ToString("F4")+"\t"+lens.fieldOfView.ToString("F3")+"\t"+lens.aspect.ToString("F4")+"\n");
                }
                if (!render.MoveNext()) break;
                yield return render.Current;
            }
        }        private static bool IsIsagani(SpectatorDirector director)
        {
            var shot=(SpectatorInterest)typeof(SpectatorDirector).GetField("_shot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(director);
            return shot.Main!=null && Roster.PersonIdAt(shot.Main.Mode,shot.Main.CharacterIndex)=="zack";
        }    }
}
