using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Every front-end screen HOME opens, captured at 1920x1080 into Logs/shots-native-ui as
    /// Restyle-&lt;door&gt;.png, for the screen-by-screen restyle onto the hand-made HOME system. Review
    /// evidence only: it asserts that each door opens something, not how it looks.
    /// </summary>
    public sealed class HubRestyleShots
    {
        [UnitySetUp] public IEnumerator Before() { yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After() { yield return PlayModeWorld.Reset(); }

        private static readonly string[] Doors =
        {
            "HeroButton", "LoadoutButton", "ShopButton", "TaskButton", "ModeCard", "MenuButton", "AvatarButton", "NamePlate",
        };

        [UnityTest] public IEnumerator EveryHomeDoorCaptured()
        {
            foreach (var door in Doors)
            {
                yield return HubFlowTests.OpenHome();
                var hub = TumpHub.Current;
                yield return HubFlowTests.Press(door);
                if (door == "HeroButton") yield return HubFlowTests.AwaitDeferredHero();
                yield return new WaitForSecondsRealtime(.6f);
                Debug.Log($"[Restyle] {door} -> {TumpHub.Current.Top?.GetType().Name}");
                var under = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .Where(c => c != hub.Canvas && c.isRootCanvas && c.gameObject.activeInHierarchy)
                    .OrderBy(c => c.sortingOrder).ToArray();
                yield return TumpUiCapture.Capture("Restyle-" + door, hub.Canvas, 1920, 1080, false, includeWorld: true, underlays: under);
            }

            // Settings draws on its own canvas, above the hub.
            yield return HubFlowTests.OpenHome();
            TumpHub.Current.Host.OpenSettings();
            yield return new WaitForSecondsRealtime(.8f);
            var top = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.enabled && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            yield return TumpUiCapture.Capture("Restyle-Settings", top.Last(), 1920, 1080, false, includeWorld: true,
                                               underlays: top.Take(top.Length - 1).ToArray());
            Assert.Pass();
        }
    }
}
