using System.Linq;
using NUnit.Framework;
using TumbangPreso.Audio;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ZackAudioTests
    {
        [Test] public void EnabledFamilyHasRealClipsAndPreservesOtherHeroesMute()
        {
            bool old = AudioCues.SkillSfxOn;
            try
            {
                AudioCues.SkillSfxOn = false;
                var cues = AudioCues.Live.Where(c => (c.StartsWith("sfx_") && c.Contains("zack")) || c == "sfx_status_zapped").ToArray();
                Assert.AreEqual(16, cues.Length);
                foreach (var cue in cues)
                {
                    Assert.IsTrue(AudioCues.Audible(cue), cue);
                    var clip = Resources.Load<AudioClip>("Sfx/" + cue);
                    Assert.IsNotNull(clip, cue);
                    Assert.AreEqual(1, clip.channels, cue);
                    Assert.AreEqual(48000, clip.frequency, cue);
                    Assert.Greater(clip.length, .25f, cue);
                }
                Assert.IsFalse(AudioCues.Audible("sfx_cast_sean_magma"));
                Assert.IsTrue(AudioCues.Audible("sfx_cast_paete_vine"));
            }
            finally { AudioCues.SkillSfxOn = old; }
        }
    }
}
