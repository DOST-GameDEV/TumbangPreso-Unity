using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace TumbangPreso.PlayTests
{
    // The owner's October4 supplied-art replacement supersedes the old weather rig.
    public sealed class OwnerMenuSkyTests
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest] public IEnumerator SuppliedPaintingStaysStillAfterArrival()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return new WaitForSecondsRealtime(.5f);
            var canvas=GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
            var prompt=canvas.GetComponentsInChildren<Text>().Single(t=>t.name=="ContinuePrompt");prompt.enabled=false;
            yield return TumpUiCapture.Capture("Title-clean-still-before",canvas,1920,1080,false);
            yield return new WaitForSecondsRealtime(.3f);
            yield return TumpUiCapture.Capture("Title-clean-still-after",canvas,1920,1080,false);
            var a=File.ReadAllBytes("Logs/shots-native-ui/Title-clean-still-before.png");
            var b=File.ReadAllBytes("Logs/shots-native-ui/Title-clean-still-after.png");
            CollectionAssert.AreEqual(a,b,"The supplied painting must not swim, warp or receive moving weather overlays.");
        }
        [UnityTest] public IEnumerator ReducedMotionKeepsHintAndPaintingFullyVisible()
        {
            bool before=Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion=true;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;
                var canvas=GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
                var image=canvas.GetComponentsInChildren<RawImage>().Single(i=>i.name=="OwnerMainMenuBackground");
                var prompt=canvas.GetComponentsInChildren<Text>().Single(t=>t.name=="ContinuePrompt");
                Assert.AreEqual(1f,image.color.a,.001f);Assert.AreEqual(1f,prompt.color.a,.001f);
                Assert.AreEqual(Graphic.defaultGraphicMaterial,image.material);
                yield return TumpUiCapture.Capture("Title-clean-reduced",canvas,1920,1080,false);
            }
            finally{Settings.SettingsStore.Current.ReducedUiMotion=before;}
        }
    }
}
