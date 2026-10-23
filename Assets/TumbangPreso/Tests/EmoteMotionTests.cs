using System.Linq;
using NUnit.Framework;
using TumbangPreso.EditorTools;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// THE AUTHORED EMOTES SHIP ON EVERY PLAYABLE HERO (owner, 2026-10-23: "emotes are ... boring compared to all
    /// animations and rigid"). Each hero's roster entry carries the seven clips `EmoteMotionAuthor` bakes, and every wheel
    /// emote resolves to its authored clip first, never down its chain to the pack's old one.
    /// </summary>
    public sealed class EmoteMotionTests
    {
        [Test]
        public void EveryPlayableHeroCarriesTheAuthoredEmotes()
        {
            foreach (string id in EmoteMotionAuthor.Heroes)
            {
                var entry = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_" + id + ".asset");
                Assert.IsNotNull(entry, id + " has no roster entry");
                var names = (entry.Clips ?? new UnityEngine.AnimationClip[0]).Where(c => c != null).Select(c => c.name).ToList();
                foreach (string clip in EmoteMotion.All)
                    Assert.Contains(clip, names, id + " is missing " + clip);
            }
        }

        [Test]
        public void NoHeroCarriesTheRetiredFirstSet()
        {
            foreach (string id in EmoteMotionAuthor.Heroes)
            {
                var entry = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_" + id + ".asset");
                Assert.IsNotNull(entry, id + " has no roster entry");
                foreach (var clip in entry.Clips ?? new UnityEngine.AnimationClip[0])
                    Assert.IsFalse(clip != null && EmoteMotion.Retired.Contains(clip.name), id + " still carries " + (clip != null ? clip.name : ""));
            }
        }

        [Test]
        public void EveryWheelEmoteHasItsOwnPicture()
        {
            var seen = new System.Collections.Generic.HashSet<UnityEngine.Sprite>();
            foreach (var emote in Social.Emotes.All)
            {
                var sprite = UI.EmoteGlyphs.For(emote.Id);
                Assert.IsNotNull(sprite, emote.Id + " has no picture");
                Assert.IsTrue(seen.Add(sprite), emote.Id + " shares a picture");
            }
        }

        /// <summary>Every slice's picture and word sit inside its own wedge: clear of the hub, under the rim, and
        /// off both dividers. A picture is checked as the disc its figure is drawn in (`EmoteGlyphs` keeps to 0.9 of
        /// the half-size), a word as a band its height tall across the bisector.</summary>
        [Test]
        public void EverySlicePictureAndWordStaysInsideItsWedge()
        {
            int count = Social.Emotes.Count;
            float half = Mathf.Deg2Rad * 180.0f / count;
            float band = UI.EmoteWheel.RadiusInner + (UI.EmoteWheel.RadiusOuter - UI.EmoteWheel.RadiusInner) * UI.EmoteWheel.LabelRadiusFraction;
            for (int i = 0; i < count; i++)
            {
                float mid = Mathf.Deg2Rad * (360.0f / count * (i + 0.5f));
                var along = new Vector2(Mathf.Sin(mid), Mathf.Cos(mid));
                void Inside(string what, Vector2 centre, float radius)
                {
                    float r = centre.magnitude, aside = Mathf.Abs(along.x * centre.y - along.y * centre.x);
                    Assert.LessOrEqual(r + radius, UI.EmoteWheel.RadiusOuter, Social.Emotes.All[i].Id + " " + what + " crosses the rim");
                    Assert.GreaterOrEqual(r - radius, UI.EmoteWheel.RadiusInner, Social.Emotes.All[i].Id + " " + what + " runs into the hub");
                    Assert.LessOrEqual(aside + radius, r * Mathf.Sin(half), Social.Emotes.All[i].Id + " " + what + " crosses a divider");
                }
                Inside("picture", along * band + new Vector2(0, UI.EmoteWheel.IconLift), UI.EmoteWheel.IconSize * 0.45f);
                Inside("word", along * band - new Vector2(0, UI.EmoteWheel.LabelDrop), UI.EmoteWheel.LabelHeight * 0.5f);
            }
        }

        [Test]
        public void EveryWheelEmoteResolvesToItsAuthoredClipFirst()
        {
            foreach (var emote in Social.Emotes.All)
            {
                Assert.IsTrue(CharacterAnimator.EmoteChains.TryGetValue(emote.Id, out var chain), emote.Id + " has no chain");
                Assert.IsTrue(EmoteMotion.All.Contains(chain[0]), emote.Id + " plays " + chain[0] + " first, not an authored emote");
            }
        }
    }
}
