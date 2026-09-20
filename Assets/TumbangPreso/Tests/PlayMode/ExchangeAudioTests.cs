using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Audio;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ExchangeAudioTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator AnnouncerHasOneLineAndEssentialCallsInterruptChatter()
        {
            var voice = new GameObject("VoicePriorityTest").AddComponent<VoiceDirector>();
            Settings.SettingsStore.Current.MasterVolume = 1;
            Settings.SettingsStore.Current.AnnouncerVolume = 1;
            yield return null;
            // This checkout ships countdown/clock/result recordings. Optional
            // taya/ayos/tumbang recordings are absent and must stay a silent no-op.
            voice.OnAttackerTagged();
            Assert.IsFalse(voice.GetComponentsInChildren<AudioSource>().Any(s => s.isPlaying));
            voice.Play("clock_30");
            var sources = voice.GetComponentsInChildren<AudioSource>();
            Assert.AreEqual(1, sources.Count(s => s.isPlaying));
            var first = sources.First(s => s.isPlaying).clip;
            voice.Play("clock_10");
            Assert.AreEqual(first, sources.First(s => s.isPlaying).clip, "Routine chatter must not interrupt a current line.");
            voice.PlayCountdown("3");
            Assert.AreEqual(1, sources.Count(s => s.isPlaying));
            Assert.IsTrue(sources.First(s => s.isPlaying).clip.name.StartsWith("vo_count_3"));
            voice.PlayCountdown("2");
            Assert.IsTrue(sources.First(s => s.isPlaying).clip.name.StartsWith("vo_count_2"), "A long take must not swallow the next count.");
            voice.OnMatchWon(1);
            Assert.IsTrue(sources.First(s => s.isPlaying).clip.name.StartsWith("vo_match_win"));
            Object.Destroy(voice.gameObject);
        }

        [UnityTest]
        public IEnumerator DistinctShoveAndLungeDoNotUseGameplayRandomOrReplayTheirTell()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            yield return SceneManager.LoadSceneAsync("Eskinita"); yield return null;
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            Assert.AreEqual("bump_swing", AudioCues.FileStemFor("bump_swing"));
            var shove = Resources.Load<AudioClip>("Sfx/bump_swing"); var lunge = Resources.Load<AudioClip>("Sfx/dash");
            Assert.IsNotNull(shove); Assert.IsNotNull(lunge); Assert.AreNotEqual(shove, lunge);
            Assert.AreEqual(.17f, shove.length, .015f, "The short preparation must not become a contact or long movement cue.");
            var before = Random.state;
            GameServices.Audio.PlayAtVaried("bump_swing", Vector3.zero);
            GameServices.Audio.PlayUiVaried("score_award", .94f, 1.04f);
            Assert.AreEqual(before, Random.state, "Listener feedback must not change simulation randomness.");
            var actor = GameServices.Round.PlayerAt(0).GetComponent<CharacterAnimator>();
            actor.PlayAction("lunge");
            int sounding = Object.FindObjectsByType<AudioSource>().Count(s => s.clip == lunge && s.isPlaying);
            Assert.Greater(sounding, 0, "The action bridge must emit the positional lunge tell.");
            actor.PlayActionAt("lunge", "lunge", .1f);
            Assert.AreEqual(sounding, Object.FindObjectsByType<AudioSource>().Count(s => s.clip == lunge && s.isPlaying),
                "Restoring a pose must not emit another action onset.");
        }
    }
}
