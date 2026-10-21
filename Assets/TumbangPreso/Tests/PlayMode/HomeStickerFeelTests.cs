using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The hand-made HOME stickers answer the hand (owner, 2026-10-21: "make it amazing to press"):
    /// a press sinks the face onto its slab and squashes it, a release springs it back, a hover lifts
    /// the picture, and every board piece is drawn where the owner's board has it, at 2x.
    /// </summary>
    public sealed class HomeStickerFeelTests
    {
        [UnitySetUp] public IEnumerator Before() { yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After() { yield return PlayModeWorld.Reset(); }

        private static HubStickerFeel FeelOf(string button) =>
            TumpHub.Current.Canvas.GetComponentsInChildren<HubStickerFeel>().Single(f => f.name == button);

        [UnityTest] public IEnumerator PlaySinksOntoItsSlabAndSpringsBack()
        {
            yield return HubFlowTests.OpenHome();
            yield return new WaitForSecondsRealtime(0.6f);   // entrance slaps settle
            var feel = FeelOf("PlayButton");
            var face = feel.Face;
            var hit = (RectTransform)feel.transform;
            // A pointer that is not on PLAY: hovering elsewhere clears the open-time selection lift.
            EventSystem.current.SetSelectedGameObject(null);
            yield return new WaitForSecondsRealtime(0.5f);
            Vector2 rest = face.anchoredPosition;

            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(hit.gameObject, data, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(hit.gameObject, data, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.Less(face.anchoredPosition.y, rest.y - 5f, "A press must sit PLAY's face down onto its slab.");
            Assert.Less(face.localScale.y, 0.985f, "A press must squash the face.");
            Assert.Greater(face.localScale.x, 1.0f, "A squash widens the face as it flattens.");
            yield return TumpUiCapture.Capture("Home-play-pressed", TumpHub.Current.Canvas, 1920, 1080, false);

            ExecuteEvents.Execute(hit.gameObject, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit.gameObject, data, ExecuteEvents.pointerExitHandler);
            // The release overshoots: within a short window the face passes above its rest.
            float highest = float.MinValue;
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                highest = Mathf.Max(highest, face.anchoredPosition.y);
                yield return null;
            }
            Assert.Greater(highest, rest.y - 0.5f, "The release must spring the face back up.");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(rest.y, face.anchoredPosition.y, 0.75f, "PLAY settles back at rest.");
        }

        [UnityTest] public IEnumerator HoverLiftsTheHeroPicture()
        {
            yield return HubFlowTests.OpenHome();
            yield return new WaitForSecondsRealtime(0.6f);
            EventSystem.current.SetSelectedGameObject(null);
            yield return new WaitForSecondsRealtime(0.4f);
            var feel = FeelOf("HeroButton");
            var picture = feel.Pictures[0];
            float before = picture.anchoredPosition.y;
            var data = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(feel.gameObject, data, ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.Greater(picture.anchoredPosition.y, before + 4f, "Hovering HERO lifts its picture off the tile.");
            Assert.Greater(picture.localScale.x, 1.04f, "Hovering HERO grows its picture.");
            yield return TumpUiCapture.Capture("Home-hero-hover", TumpHub.Current.Canvas, 1920, 1080, false);
            ExecuteEvents.Execute(feel.gameObject, data, ExecuteEvents.pointerExitHandler);
        }

        [UnityTest] public IEnumerator BoardPiecesAreDrawnAtTwiceTheLayout()
        {
            yield return HubFlowTests.OpenHome();
            foreach (var image in TumpHub.Current.Canvas.GetComponentsInChildren<Image>()
                         .Where(i => i.sprite != null && i.sprite.texture != null && i.sprite.texture.name.StartsWith("tile_")))
            {
                Assert.AreEqual(200f, image.sprite.pixelsPerUnit, image.name + " must import at 200 pixels per unit.");
                var rect = image.rectTransform.rect;
                Assert.AreEqual(image.sprite.rect.width / 2f, rect.width, 0.6f, image.name + " is not drawn at its native 2x size.");
            }
        }
    }
}
