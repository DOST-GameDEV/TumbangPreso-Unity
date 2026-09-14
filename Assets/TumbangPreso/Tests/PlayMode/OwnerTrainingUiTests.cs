using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerTrainingUiTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest,Timeout(90000)]
        public IEnumerator TrainingUsesOwnerThemeAndRealSkipQuitCallbacks()
        {
            bool training=GameLaunch.GuidedTutorial,networked=SceneFlow.Networked;
            try
            {
                SceneFlow.Networked=false;SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                GameLaunch.GuidedTutorial=true;
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                GuidedTrainingHud hud=null;float until=Time.realtimeSinceStartup+15;
                while(hud==null && Time.realtimeSinceStartup<until){hud=Object.FindFirstObjectByType<GuidedTrainingHud>();yield return null;}
                Assert.IsNotNull(hud);var canvas=hud.GetComponent<Canvas>();Assert.IsNotNull(canvas.GetComponent<OwnerUiCanvas>());
                var counter=hud.GetComponentsInChildren<Text>().First(t=>t.name=="LessonCounter");
                var body=hud.GetComponentsInChildren<Text>().First(t=>t.name=="LessonBody");
                Assert.IsNotEmpty(body.text);string first=counter.text;
                yield return TumpUiCapture.Capture("OwnerTraining-look-v1",canvas,1920,1080,false,true);
                Press(hud.GetComponentsInChildren<Button>().First(b=>b.name=="SkipTrainingLesson"));
                until=Time.realtimeSinceStartup+5;
                while(counter.text==first && Time.realtimeSinceStartup<until)yield return null;
                Assert.AreNotEqual(first,counter.text,"Skip must advance the real lesson controller.");
                // Check the renderer setter before the lesson controller next publishes its real progress.
                hud.SetProgress(.5f);
                var fill=hud.GetComponentsInChildren<Image>().First(i=>i.name=="ProgressFill");
                Assert.That(fill.rectTransform.anchorMax.x,Is.EqualTo(.5f).Within(.001f));
                yield return TumpUiCapture.Capture("OwnerTraining-move-v1",canvas,1280,720,false,true);
                Press(hud.GetComponentsInChildren<Button>().First(b=>b.name=="QuitTraining"));
                until=Time.realtimeSinceStartup+10;
                while(SceneManager.GetActiveScene().name!=SceneFlow.MainMenu && Time.realtimeSinceStartup<until)yield return null;
                Assert.AreEqual(SceneFlow.MainMenu,SceneManager.GetActiveScene().name);Assert.False(GameLaunch.GuidedTutorial);
            }
            finally{GameLaunch.GuidedTutorial=training;SceneFlow.Networked=networked;}
        }
        private static void Press(Button button)
        {
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.IsNotEmpty(hits);Assert.AreEqual(button,hits[0].gameObject.GetComponentInParent<Button>());
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
    }
}
