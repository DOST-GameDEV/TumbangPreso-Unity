using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class ReducedActionFocusProbe
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(60000)]
        public IEnumerator SelectedPaintedButtonRemainsVisibleWithoutMotionOrArtworkChanges()
        {
            bool boot = SceneFlow.BootedThroughSplash;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                // ⚠️⚠️ THE SUBJECT MOVED FROM THE TITLE'S PLAY PENNANT TO THE LOGIN'S CREATE
                // PLATE, BECAUSE THE PENNANT NO LONGER EXISTS. 🧑 2026-09-18 asked for a title
                // screen with no buttons on it at all (`HomeCourtView`), so the last painted
                // action a player meets before the lobby is this one. Nothing this fixture
                // actually asserts is about the title: it is about `OwnerUiMotion` giving a
                // keyboard or pad player visible feedback with motion reduced, without moving
                // the hit box, rescaling the artwork or swapping her sprite.
                SceneFlow.BootedThroughSplash = false;
                Settings.SettingsStore.Current.ReducedUiMotion = true;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
                yield return null;
                Settings.SettingsStore.Current.ReducedUiMotion = true;
                var login = Object.FindFirstObjectByType<ConvertedMainMenu>().GetComponent<SignInScreen>();
                login.Install(); login.OpenAtBoot();
                // ⚠️ THE ENTRY EASE RUNS ON UNSCALED TIME EVEN WITH MOTION REDUCED, because it is
                // an opacity rather than a movement. Both captures have to be taken after it has
                // settled or the difference between them is the form still arriving.
                yield return new WaitForSecondsRealtime(1.2f);
                var canvas = GameObject.Find("OwnerSignInCanvas").GetComponent<Canvas>();
                var button = canvas.GetComponentsInChildren<OwnerPaintedAction>().Single(b => b.name == "SubmitAccount");
                var hit = (RectTransform)button.transform;
                var bounds = hit.sizeDelta;
                var position = hit.anchoredPosition;
                var art = button.transform.Find("PaintedArtwork").GetComponent<Image>();
                var sprite = art.sprite;
                EventSystem.current.SetSelectedGameObject(null);
                yield return TumpUiCapture.Capture("ReducedFocus-rest", canvas, 1920, 1080, false);
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                yield return TumpUiCapture.Capture("ReducedFocus-selected", canvas, 1920, 1080, false);
                var a = Read("rest"); var b = Read("selected");
                int changed = 0, artworkChanged = 0, outside = 0;
                // ⚠️ THE WINDOWS ARE HER OWN LAYOUT, NOT PADDING. CREATE sits at 746,787 and is
                // 424x92 (login-layout-v3.json), so the artwork window is exactly that rect and
                // the band around it is 44 units to the left for the static focus marker, which
                // `OwnerUiMotion.UpdateStaticFocus` hangs 20 units outside the control.
                // ⚠️ AND THE WHOLE FRAME IS SCANNED HERE, unlike the title version of this
                // fixture, because the login's woven background does not move.
                for (int y = 0; y < 1080; y++) for (int x = 0; x < 1920; x++)
                {
                    int at = (1079 - y) * 1920 + x;
                    if (Difference(a[at], b[at]) <= 3) continue;
                    changed++;
                    if (x >= 746 && x < 1170 && y >= 787 && y < 879) artworkChanged++;
                    if (x < 702 || x > 1194 || y < 777 || y > 889) outside++;
                }
                Debug.Log($"[ReducedFocus] changed={changed} artwork={artworkChanged} outside={outside}");
                Assert.Greater(changed, 150, "Keyboard/controller selection has no visible feedback with motion reduced");
                Assert.AreEqual(0, artworkChanged, "Supplied button pixels must not be recolored or redrawn");
                Assert.AreEqual(0, outside);
                Assert.AreEqual(Vector3.one, art.transform.localScale);
                Assert.AreEqual(bounds, hit.sizeDelta); Assert.AreEqual(position, hit.anchoredPosition);
                Assert.AreSame(sprite, art.sprite);
                yield return TumpUiCapture.Capture("ReducedFocus-small", canvas, 960, 540, false);
                Settings.SettingsStore.Current.ReducedUiMotion = false;
                yield return new WaitForSecondsRealtime(.3f);
                Assert.Greater(art.transform.localScale.x, 1, "Normal animated focus must still work");
                Assert.IsEmpty(art.GetComponentsInChildren<StreetIcon>(), "Normal focus must hide the static marker");
                Settings.SettingsStore.Current.ReducedUiMotion = true;
                yield return null; yield return null;
                Assert.AreEqual(Vector3.one, art.transform.localScale);
                Assert.AreEqual(1, art.GetComponentsInChildren<StreetIcon>().Length);
                EventSystem.current.SetSelectedGameObject(null);
                yield return TumpUiCapture.Capture("ReducedFocus-cleared", canvas, 1920, 1080, false);
                var cleared = Read("cleared");
                int uncleared = 0;
                for (int y = 0; y < 1080; y++) for (int x = 0; x < 1920; x++)
                    if (!a[y * 1920 + x].Equals(cleared[y * 1920 + x])) uncleared++;
                Assert.AreEqual(0, uncleared, "Losing focus must remove the static cue");
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                button.interactable = false;
                yield return null; yield return null;
                Assert.IsFalse(art.GetComponentsInChildren<Graphic>().Any(i => !(i is Text) && i != art && i.enabled && i.canvasRenderer.GetInheritedAlpha() > .9f),
                    "A disabled action must not retain its focus indicator");
                // The same feedback component also drives text actions in secondary dialogs.
                var textAction = OwnerTextAction.Create(canvas.transform, "FocusTextCheck", "BACK", null, 1000, 700, 180, 60);
                EventSystem.current.SetSelectedGameObject(textAction.gameObject);
                yield return TumpUiCapture.Capture("ReducedFocus-text", canvas, 1920, 1080, false);
                var cue = textAction.GetComponentInChildren<StreetIcon>();
                Assert.IsNotNull(cue); Assert.False(cue.raycastTarget);
                Assert.Greater(cue.canvasRenderer.GetMesh().vertexCount, 0);
                Assert.Greater(cue.canvasRenderer.GetInheritedAlpha(), .95f);
                Assert.AreEqual(Vector3.one, textAction.GetComponentInChildren<OwnerUiMotion>().transform.localScale);
            }
            finally
            {
                SceneFlow.BootedThroughSplash = boot;
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }

        private static Color32[] Read(string state)
        {
            var image = new Texture2D(2, 2);
            try { image.LoadImage(File.ReadAllBytes("Logs/shots-native-ui/ReducedFocus-" + state + ".png")); return image.GetPixels32(); }
            finally { Object.DestroyImmediate(image); }
        }
        private static int Difference(Color32 a, Color32 b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }
}
