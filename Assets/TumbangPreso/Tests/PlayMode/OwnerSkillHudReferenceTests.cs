using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerSkillHudReferenceTests
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator CircularSkillDeckKeepsFontsBindingsAndCurrentMatchHud()
        {
            SceneFlow.Networked=false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            yield return SceneManager.LoadSceneAsync("Arena"); yield return new WaitForSecondsRealtime(.4f);
            foreach(var brain in Object.FindObjectsByType<AIController>()) brain.enabled=false;
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSecondsRealtime(3.7f);
            float until=Time.realtimeSinceStartup+40;
            while((Map.ArenaIntro.HidesUi || RoleSwapCard.Showing) && Time.realtimeSinceStartup<until) yield return null;
            var local=GameServices.Round.PlayerAt(GameLaunch.SoloSeat); Assert.IsNotNull(local);
            TumpUiCapture.StageHudReview(local); local.ClearStatuses(); yield return null;
            var canvas=GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            var readout=Object.FindFirstObjectByType<TumpPowerReadout>();
            var system=local.GetComponent<Abilities.HeroAbilitySystem>(); Assert.IsNotNull(system.Kit);
            var deck=(RectTransform)canvas.transform.Find("PowerSeals");
            var seals=deck.GetComponentsInChildren<OwnerAbilitySeal>(); Assert.AreEqual(3,seals.Length);
            Assert.IsNull(deck.Find("Power0/PowerDrain"),"Ring timers must not have the rejected tile/jar overlays");
            string[] actions={"Skill1","Skill2","Ultimate"};
            for(int i=0;i<3;i++)
            {
                var label=deck.Find("LiveBinding"+i).GetComponent<Text>();
                Assert.AreEqual(Hud.KeyLabelFor(actions[i]),label.text,"Saved bindings must drive prompts");
                Assert.AreSame(InGameTypography.Bold,label.font,"Current DIN remains on skill text");
                Assert.AreEqual(new Vector2(26,26),label.rectTransform.sizeDelta,"Compact binding footprint");
                var glyph=label.GetComponentInChildren<Image>(); Assert.IsTrue(glyph.enabled); Assert.IsNotNull(glyph.sprite);
            }
            Assert.AreSame(InGameTypography.Clock,canvas.GetComponentsInChildren<Text>().Single(x=>x.name=="TimeLeft").font);
            foreach(int width in new[]{1920,1280})
                yield return TumpUiCapture.Capture("SkillHudReference1007-"+width,canvas,width,width==1920?1080:720,false,true);
            foreach(var seal in seals)
            {
                seal.State(1,true,false,seal==seals[2]); Canvas.ForceUpdateCanvases();
                var mesh=seal.canvasRenderer.GetMesh(); Assert.Greater(mesh.vertexCount,0);
                var rect=seal.rectTransform.rect;
                Assert.AreEqual(rect.width,rect.height,.01f,"Circular faces retain square source rectangles");
                Assert.LessOrEqual(mesh.bounds.size.x,rect.width+.5f,"Rim must stay inside its slot");
            }
            readout.Tick(system,true);
            Assert.IsNotNull(canvas.transform.Find("RoundClock"));
            Assert.IsNotNull(canvas.transform.Find("MatchScores"));
        }
    }
}
