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
    public sealed class SpectatorNaturalMapsProbe
    {
        private bool _bots, _spectator, _guided, _training, _pinned;
        private int _seat;
        private CustomRules _rules;
        private GameMode _mode;
        private INetProvider _provider;
        private string _map; private string Output => "Logs/natural-spectator-maps-1006/"+_map;
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
        [UnityTest] public IEnumerator ObserveKantoWithNaturalHeroStrikeBots()
        {
            yield return CaptureMap(SceneFlow.Kanto);
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
            while (Time.unscaledTime-start<15)
            {
                float elapsed=Time.unscaledTime-start;
                if (elapsed>=sampleAt)
                {
                    sampleAt=elapsed+.5f;
                    string cast=string.Join(";",round.Players.Where(p=>p!=null).Select(p=>p.PlayerSlot+":"+Roster.PersonIdAt(p.Mode,p.CharacterIndex)+":"+p.transform.position.ToString("F3")));
                    log.AppendLine(elapsed.ToString("F3")+","+round.TimeLeft.ToString("F3")+","+director.Beat+","+director.Shot+","+director.Cuts+","+director.SafePoseFallbacks+",\""+camera.transform.position.ToString("F3")+"\",\""+cast+"\"");
                    File.WriteAllText(Output+"/timeline.csv",log.ToString());
                }
                if (elapsed>=image*5 && image<3)
                {
                    yield return GameplayShots.Render(lens,"map-natural-"+image,flipCanvases:true,outDir:Output,width:1600,height:900);
                    image++;
                }
                yield return null;
            }
            Assert.AreEqual(3,Directory.GetFiles(Output,"map-natural-*.png").Length);
            Assert.Less(round.TimeLeft,initialClock,"Capture must cover active simulation rather than a held introduction.");
            Debug.Log("Natural "+map+" capture completed: "+image+" actual rendered frames, "+director.Cuts+" cuts, "+director.SafePoseFallbacks+" fallbacks.");
        }
    }
}
