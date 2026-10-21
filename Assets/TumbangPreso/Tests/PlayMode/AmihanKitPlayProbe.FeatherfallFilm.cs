using System;
using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // This route uses only the pre-existing runtime API, allowing a real before film
    // without copying the new flight implementation into its baseline candidate.
    public sealed partial class AmihanKitPlayProbe
    {
        private static IEnumerator LocalAttacker()
        {
            if (GameServices.Round.PlayerAt(GameLaunch.SoloSeat).IsDefender)
            {
                GameServices.Round.EndRound();
                GameServices.Match.AdvanceRound();
                yield return null;
            }
            Assert.IsFalse(GameServices.Round.PlayerAt(GameLaunch.SoloSeat).IsDefender);
            Assert.IsTrue(GameServices.Round.RoundActive);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator FilmFeatherfallInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_FEATHERFALL_FILM") != "1") Assert.Ignore("Flight film only: set TUMP_FEATHERFALL_FILM=1.");
            yield return LocalAttacker();
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            var start = can + new Vector3(0, .12f, -11.5f);
            var actor = Amihan(GameLaunch.SoloSeat, start);
            Face(actor, can);
            Camera.main.GetComponent<CameraRig>().Follow(actor);
            yield return null;
            Assert.IsFalse(actor.IsDefender);
            Assert.IsFalse(actor.IsInsideBox());
            Assert.AreEqual("amihan_skill2", actor.AbilitySystem.Kit.Skill2.Id);
            Assert.IsNotNull(actor.GetComponent<Carrier>().Held);
            var court = Film.Make("FeatherfallCourt", 50);
            var pursuer = Film.Make("FeatherfallPursuer", 60);
            court.transform.position = start + new Vector3(6, 3, -5);
            court.transform.LookAt(start + new Vector3(0, 1.8f, 1));
            pursuer.transform.position = can + new Vector3(-2, 1.5f, -5);
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            int graphics = Settings.GraphicsProfiles.Current;
            bool low = Environment.GetEnvironmentVariable("TUMP_FEATHERFALL_LOW") == "1";
            Settings.SettingsStore.Current.ReducedEffects = low;
            Settings.GraphicsProfiles.Apply(low ? 0 : 2);
            bool flew = false, threwAloft = false, landed = false;
            float duration = actor.AbilitySystem.Kit.Skill2.Duration;
            string variant = low ? "featherfall-low-v1" : "featherfall-v1";
            try
            {
                using (var film = new Film(variant, "owner", "wide", "victim"))
                {
                    int frames = Mathf.CeilToInt((duration + 2.4f) * 30);
                    bool wasHolding = true;
                    for (int f = 0; f < frames; f++)
                    {
                        film.Frame = f;
                        float t = f / 30f;
                        actor.Intent.Set(Verb.Skill2, f == 15 || f == 16);
                        actor.Intent.Move = t > 1.7f && t < 2.3f ? Vector2.right * .65f : Vector2.zero;
                        actor.Intent.AimPoint = round.Lata.transform.position;
                        actor.Intent.Set(Verb.SpecialAbility, t > 2.6f && t < 3.3f);
                        yield return null;
                        flew |= actor.IsAloft;
                        bool holding = actor.GetComponent<Carrier>().Held != null;
                        threwAloft |= wasHolding && !holding && actor.IsAloft;
                        wasHolding = holding;
                        landed |= flew && !actor.IsFlying && actor.IsGrounded;
                        pursuer.transform.LookAt(actor.transform.position + Vector3.up * .75f);
                        film.Shoot(Camera.main, "owner");
                        film.Shoot(court, "wide");
                        film.Shoot(pursuer, "victim");
                    }
                }
            }
            finally
            {
                actor.Intent.Clear();
                Object.Destroy(court.gameObject); Object.Destroy(pursuer.gameObject);
                Settings.SettingsStore.Current.ReducedEffects = reduced;
                Settings.GraphicsProfiles.Apply(graphics);
            }
            Note("featherfall_film_seconds", duration);
            Note("featherfall_film_low", low);
            Note("featherfall_film_flew", flew);
            Note("featherfall_film_threw_aloft", threwAloft);
            Note("featherfall_film_landed", landed);
            Assert.IsTrue(flew);
            Assert.IsTrue(threwAloft, "The film did not contain an accepted airborne throw.");
            Assert.IsTrue(landed, "The film ended before actual landing.");
        }
    }
}
