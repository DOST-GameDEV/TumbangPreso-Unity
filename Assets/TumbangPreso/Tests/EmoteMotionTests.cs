using System.Linq;
using NUnit.Framework;
using TumbangPreso.EditorTools;
using TumbangPreso.Visual;
using UnityEditor;

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
