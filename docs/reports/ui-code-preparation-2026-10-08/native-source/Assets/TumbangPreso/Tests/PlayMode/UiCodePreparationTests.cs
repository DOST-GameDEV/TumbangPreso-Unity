using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class UiCodePreparationTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]public IEnumerator PreparingCodeDoesNotConstructViewsModelsOrInvokeMenuActions()
        {
            var before=Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            var settings=JsonUtility.ToJson(Settings.SettingsStore.Current);
            var rules=SceneFlow.SelectedRules.Clone();var map=SceneFlow.SelectedMap;var mode=SceneFlow.SelectedMode;
            int previews=Object.FindObjectsByType<ModelPreview>(FindObjectsInactive.Include).Length;
            yield return UiCodePreparation.Prepare();
            Assert.IsTrue(UiCodePreparation.Complete);
#if ENABLE_MONO
            Assert.Greater(UiCodePreparation.PreparedMethods,0);
#endif
            CollectionAssert.AreEquivalent(before,Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include));
            Assert.AreEqual(previews,Object.FindObjectsByType<ModelPreview>(FindObjectsInactive.Include).Length);
            Assert.AreEqual(settings,JsonUtility.ToJson(Settings.SettingsStore.Current));
            Assert.AreEqual(map,SceneFlow.SelectedMap);Assert.AreEqual(mode,SceneFlow.SelectedMode);
            Assert.AreEqual(JsonUtility.ToJson(rules),JsonUtility.ToJson(SceneFlow.SelectedRules));
            int prepared=UiCodePreparation.PreparedMethods;yield return UiCodePreparation.Prepare();
            Assert.AreEqual(prepared,UiCodePreparation.PreparedMethods);
        }
    }
}
