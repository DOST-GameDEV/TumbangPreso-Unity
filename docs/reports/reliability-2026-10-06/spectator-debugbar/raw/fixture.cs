using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorDebugBarCapture
    {
        [UnityTest] public IEnumerator NativeRoleTransitionsRetireTheGameplayDebugStrip()
        {
            bool launch = GameLaunch.Spectator; var previous = NetAuthority.Provider;
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = null; GameLaunch.Spectator = false; GameServices.Ensure();
            var root = new GameObject("Debug strip review");
            try
            {
                var bar = root.AddComponent<DebugBar>(); yield return null;
                var canvas = bar.GetComponentsInChildren<Canvas>().Single(c => c.name == "DebugBarCanvas");
                Assert.IsTrue(canvas.enabled);
                yield return TumpUiCapture.Capture("debug-strip-gameplay", canvas, 960, 540, false);
                GameLaunch.Spectator = true; yield return null; Assert.IsFalse(canvas.enabled);
                yield return TumpUiCapture.Capture("debug-strip-watching", canvas, 960, 540, false);
                GameLaunch.Spectator = false; var hudRoot = new GameObject("HUD role owner"); hudRoot.transform.SetParent(root.transform);
                var hud = hudRoot.AddComponent<Hud>(); hud.EnterSpectatorMode(); yield return null; Assert.IsFalse(canvas.enabled);
                hud.ExitSpectatorMode(); yield return null; Assert.IsTrue(canvas.enabled);
            }
            finally { Object.Destroy(root); GameLaunch.Spectator = launch; NetAuthority.Provider = previous; }
            yield return PlayModeWorld.Reset();
        }
    }
}
