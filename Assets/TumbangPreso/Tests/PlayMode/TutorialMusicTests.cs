using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class TutorialMusicTests
    {
        bool tutorial;
        [UnitySetUp] public IEnumerator Before() { tutorial = GameLaunch.GuidedTutorial; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After() { yield return PlayModeWorld.Reset(); GameLaunch.GuidedTutorial = tutorial; }

        [UnityTest] public IEnumerator TrimmedTutorialTrackLoadsAndLoopsWithoutReplacingMatchMusic()
        {
            GameServices.Ensure(); var clip = GameServices.TutorialTrack;
            Assert.IsNotNull(clip); Assert.AreEqual(2, clip.channels);
            Assert.AreEqual(81.506f, clip.length, .03f);
            Assert.AreNotSame(GameServices.MatchTrack, clip);
            GameLaunch.GuidedTutorial = true;
            Assert.AreEqual("tutorial", GameServices.ArenaMusicCue); Assert.AreSame(clip, GameServices.ArenaTrack);
            GameServices.Music.Play(GameServices.ArenaMusicCue, GameServices.ArenaTrack);
            yield return null;
            var source = GameServices.Music.GetComponents<AudioSource>().Single(s => s.clip == clip);
            Assert.IsTrue(source.loop); Assert.Zero(source.spatialBlend); Assert.IsTrue(source.isPlaying);
            source.time = clip.length - .05f;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(source.isPlaying); Assert.Less(source.time, 1f, "The trimmed track failed to wrap to its new beginning.");
            GameLaunch.GuidedTutorial = false;
            Assert.AreEqual("match", GameServices.ArenaMusicCue); Assert.AreSame(GameServices.MatchTrack, GameServices.ArenaTrack);
        }

        [UnityTest, Timeout(60000)] public IEnumerator ActualTutorialEntryUsesTheTrackAndCountdownKeepsIt()
        {
            SceneFlow.StartTraining();
            float deadline = Time.realtimeSinceStartup + 40;
            while ((Object.FindFirstObjectByType<GuidedTraining>() == null || GameServices.Music?.Current != "tutorial")
                && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<GuidedTraining>());
            Assert.AreEqual("tutorial", GameServices.Music.Current);
            var hud = Object.FindFirstObjectByType<Hud>(); Assert.IsNotNull(hud);
            hud.ShowCountdownTick("3");
            Assert.AreEqual("tutorial", GameServices.Music.Current);
            Assert.IsTrue(GameServices.Music.GetComponents<AudioSource>().Any(s => s.clip == GameServices.TutorialTrack && s.isPlaying));
        }
    }
}
