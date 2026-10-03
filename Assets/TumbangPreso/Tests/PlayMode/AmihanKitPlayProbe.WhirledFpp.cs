using System;
using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed partial class AmihanKitPlayProbe
    {
        /// <summary>
        /// WHIRLED FROM THE INSIDE (owner, 2026-10-03: *"think abt what the ppl in fpp would see (if they get hit)"*): the game's
        /// own camera follows the player in the fan, in first person, and they are thrown and Whirled as Airburst's release
        /// throws them (`AmihanStorm.BlowDirection`, `StormSurgeSpeed`, `StormSurgeLift`). The film is their screen: the hit,
        /// the blown view, and back in first person the wind whirling round their view (`WhirledView`) until it runs out.
        /// Runs only with TUMP_AIRBURST_FILM=1; frames under TUMP_EVIDENCE/whirled-fpp.
        /// </summary>
        [UnityTest, Timeout(300000)]
        public IEnumerator FilmWhirledInFirstPerson()
        {
            if (Environment.GetEnvironmentVariable("TUMP_AIRBURST_FILM") != "1") Assert.Ignore("Film only: set TUMP_AIRBURST_FILM=1.");
            var stage = StageAirburst();
            var victim = stage.Victim;
            Assert.IsNotNull(victim);
            var rig = Camera.main.GetComponent<CameraRig>();
            rig.Follow(victim);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(rig.IsLocalFpp, "The victim's own view should be first person.");
            int whirledFrames = 0;
            using (var film = new Film("whirled-fpp", "fpp"))
            {
                for (int f = 0; f < 30 * 4; f++)
                {
                    film.Frame = f;
                    if (f == 12)
                    {
                        var blow = AmihanStorm.BlowDirection(stage.Origin, stage.Forward, victim.transform.position);
                        victim.ApplyWhirled();
                        victim.ApplyResolvedCarry(blow * AmihanRules.StormSurgeSpeed + Vector3.up * AmihanRules.StormSurgeLift, AmihanRules.StormSurgeHoldSeconds);
                    }
                    yield return null;
                    if (victim.IsWhirled) whirledFrames++;
                    film.Shoot(Camera.main, "fpp");
                }
            }
            Assert.Greater(whirledFrames, 30, "The victim was never Whirled.");
            rig.Follow(stage.Caster);
        }
    }
}
