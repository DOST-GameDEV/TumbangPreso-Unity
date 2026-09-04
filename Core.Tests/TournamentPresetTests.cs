using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The tournament preset, asserted field by field.
    ///
    /// ⚠️⚠️ THE POINT OF THESE IS THAT A TOURNAMENT MATCH CANNOT DRIFT WITHOUT A RED TEST. Every
    /// number here is also asked of the shipped source rather than restated, so a change to
    /// `Balance.RoundTime` moves the preset and this file agrees; a change to what a CUSTOM lobby
    /// opens on moves `CustomGameRules.Defaults` and this file goes red, which is exactly the
    /// direction the alarm should point.
    /// </summary>
    public class TournamentPresetTests
    {
        [Fact]
        public void ClassicIsTheTournamentRuleset()
        {
            // docs/VISION.md § 1.1: "CLASSIC IS THE TOURNAMENT RULESET UNTIL SOMEONE SAYS
            // OTHERWISE." Changing this constant is a tournament ruling, and this assertion is
            // what makes it impossible to change it quietly.
            Assert.Equal(GameMode.Classic, TournamentPreset.Mode);
        }

        [Fact]
        public void TheTournamentPresetIsPinnedFieldByField()
        {
            var r = TournamentPreset.Rules();

            Assert.Equal(GameMode.Classic, r.Mode);
            Assert.Equal(MatchFormat.Standard, r.Format);

            // Four rounds, so everybody defends exactly once. VISION § 1.1.
            Assert.Equal(4, r.Rounds);
            Assert.Equal(MatchRules.RoundCountFor(GameMode.Classic), r.Rounds);

            Assert.Equal((int)Balance.RoundTime, r.RoundSeconds);
            Assert.Equal(0, r.ScoreTarget);
            Assert.Equal(CustomGameRules.StartingTsinelas, r.Tsinelas);
            Assert.Equal(0, r.Bots);
            Assert.Equal("", r.Password);
        }

        [Fact]
        public void TheTournamentPresetIsPlayable()
        {
            // ⚠️ A PRESET THAT THE LOBBY WOULD REFUSE IS WORSE THAN NO PRESET, because the refusal
            // arrives at the moment somebody presses start with a room full of people waiting.
            Assert.Equal("", CustomGameRules.Refusal(TournamentPreset.Rules()));
        }

        [Fact]
        public void EveryCallGetsItsOwnObject()
        {
            // ⚠️ A SHARED MUTABLE PRESET IS THE LEFTOVER THIS FILE EXISTS TO PREVENT. If a lobby
            // screen edits the object it was handed, the next tournament match inherits the edit.
            var a = TournamentPreset.Rules();
            var b = TournamentPreset.Rules();

            Assert.NotSame(a, b);

            a.Rounds = 99;
            Assert.Equal(4, TournamentPreset.Rules().Rounds);
        }

        [Fact]
        public void ARuleSetThatIsNotTheTournamentOneSaysWhichFieldIsWrong()
        {
            var r = TournamentPreset.Rules();
            Assert.Equal("", TournamentPreset.RulesRefusal(r));

            r.Mode = GameMode.HeroStrike;
            Assert.Contains("mode is HeroStrike", TournamentPreset.RulesRefusal(r));

            r = TournamentPreset.Rules();
            r.Rounds = 3;
            Assert.Contains("rounds is 3", TournamentPreset.RulesRefusal(r));

            r = TournamentPreset.Rules();
            r.Bots = 2;
            Assert.Contains("2 seat(s) with bots", TournamentPreset.RulesRefusal(r));

            r = TournamentPreset.Rules();
            r.ScoreTarget = 500;
            Assert.Contains("score target is 500", TournamentPreset.RulesRefusal(r));
        }

        [Fact]
        public void ABareCustomRulesIsNotATournamentMatchAndThatIsWhyThePresetExists()
        {
            // ⚠️⚠️ THE FIELD INITIALISERS ON `CustomRules` READ HERO STRIKE, EIGHT ROUNDS. That is
            // correct for the class (it is the mode with the bigger surface) and it is the exact
            // hazard this preset removes: `new CustomRules()` anywhere in a start path is a
            // Hero Strike match wearing a Classic tournament's name.
            var bare = new CustomRules();
            Assert.NotEqual("", TournamentPreset.RulesRefusal(bare));
        }

        [Fact]
        public void EveryModifierIsNamedWithAReasonAndHasASafeValue()
        {
            Assert.NotEmpty(TournamentPreset.Modifiers);

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in TournamentPreset.Modifiers)
            {
                Assert.False(string.IsNullOrWhiteSpace(m.Name));

                // ⚠️ THE REASON IS NOT DECORATION. A list of eight field names with no sentences
                // is a list somebody deletes a row from during a cleanup.
                Assert.True(m.Why != null && m.Why.Length > 40,
                            $"{m.Name} is on the list with no reason written down");

                Assert.True(names.Add(m.Name), $"{m.Name} is listed twice");
            }
        }

        [Fact]
        public void BotsEnabledIsTheOneModifierWhoseSafeValueIsTrue()
        {
            // Turning bots off does not make a match more human; it makes unfilled seats inert.
            Assert.True(TournamentPreset.SafeValue("AIController.BotsEnabled"));

            foreach (var m in TournamentPreset.Modifiers)
                if (m.Name != "AIController.BotsEnabled")
                    Assert.False(TournamentPreset.SafeValue(m.Name),
                                 $"{m.Name} should be off in a tournament match");
        }

        [Fact]
        public void ThePracticeSandboxIsOnTheListBecauseItsGuardIsTheThingUnderTest()
        {
            var named = new List<string>();
            foreach (var m in TournamentPreset.Modifiers) named.Add(m.Name);

            Assert.Contains("PracticeSandbox.Wanted", named);
            Assert.Contains("GameLaunch.AllBots", named);
            Assert.Contains("GameLaunch.Spectator", named);
            Assert.Contains("GameLaunch.GuidedTutorial", named);
            Assert.Contains("MatchInstaller.PreviewOnly", named);
        }
    }
}
