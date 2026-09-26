using System.Collections.Generic;
using System.Linq;
using Xunit;
using TumbangPreso.Core;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The hero voice script (VOICE-1, owner 2026-09-24) as rules rather than hopes: every hero
    /// answers every moment, no line is long enough to become noise, every exchange is between two
    /// real heroes, and the ids that name the audio files are unique.
    /// </summary>
    public class HeroLinesTests
    {
        [Fact]
        public void EveryHeroHasAtLeastOneLineForEveryMoment()
        {
            foreach (var hero in Roster.HeroPeople)
                foreach (var trigger in HeroLines.SoloTriggers)
                    Assert.True(HeroLines.For(hero.Id, trigger).Count > 0,
                        $"{hero.Id} has nothing to say for {trigger}, so that moment is silent for one hero only.");
        }

        [Fact]
        public void EverySkillAndUltimateHasMoreThanOneReading()
        {
            // A line heard every cast becomes the thing a player mutes. Two at least, cycled.
            foreach (var hero in Roster.HeroPeople)
                foreach (var trigger in new[] { HeroLineTrigger.Skill1, HeroLineTrigger.Skill2,
                                                HeroLineTrigger.UltimateAlly, HeroLineTrigger.UltimateOpponent })
                    Assert.True(HeroLines.For(hero.Id, trigger).Count >= 2, $"{hero.Id} {trigger}");
        }

        [Fact]
        public void LinesAreShortAndWrittenInTheRepoVoice()
        {
            foreach (var line in HeroLines.All)
            {
                Assert.True(HeroLines.WordCount(line.Text) <= 7,
                    $"'{line.Id}' is {HeroLines.WordCount(line.Text)} words: \"{line.Text}\". A bark is heard many times.");
                Assert.DoesNotContain("—", line.Text);
                Assert.DoesNotContain("–", line.Text);
                Assert.False(string.IsNullOrWhiteSpace(line.Text), line.Id);
            }
        }

        [Fact]
        public void IdsAreUniqueAndNameTheirSpeaker()
        {
            var ids = HeroLines.All.Select(l => l.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            var heroes = new HashSet<string>(Roster.HeroPeople.Select(h => h.Id));
            foreach (var line in HeroLines.All)
                Assert.Contains(line.HeroId, heroes);
        }

        [Fact]
        public void BanterPairsTwoDifferentRealHeroesAndPicksTheSameOnEveryPeer()
        {
            foreach (var banter in HeroLines.Banters)
            {
                Assert.NotEqual(banter.Opener.HeroId, banter.Reply.HeroId);
                Assert.Equal(HeroLineTrigger.Banter, banter.Opener.Trigger);
                Assert.Equal(HeroLineTrigger.Reply, banter.Reply.Trigger);
            }

            var lineup = new List<string> { "sean", "zack", "dante", "nemu" };
            var first = HeroLines.PickBanter(lineup, 12345);
            Assert.NotNull(first);
            Assert.Same(first, HeroLines.PickBanter(lineup, 12345));
            Assert.Contains(first.Opener.HeroId, lineup);
            Assert.Contains(first.Reply.HeroId, lineup);

            // Nobody to talk to: no exchange rather than half of one.
            Assert.Null(HeroLines.PickBanter(new List<string> { "dante", "dante" }, 1));
            Assert.Null(HeroLines.PickBanter(new List<string> { "nemu" }, -7));
        }

        [Fact]
        public void TheCycleNeverRepeatsALineUntilItHasUsedTheOthers()
        {
            var cycle = new HeroLineCycle();
            var options = HeroLines.For("sean", HeroLineTrigger.Skill1);
            var heard = Enumerable.Range(0, options.Count).Select(_ => cycle.Next("sean", HeroLineTrigger.Skill1).Id).ToList();
            Assert.Equal(options.Count, heard.Distinct().Count());
            Assert.Equal(heard[0], cycle.Next("sean", HeroLineTrigger.Skill1).Id);
        }

        [Fact]
        public void TheUltimateOutranksRoutineChatter()
        {
            Assert.True(HeroLines.Priority(HeroLineTrigger.UltimateAlly) > HeroLines.Priority(HeroLineTrigger.Banter));
            Assert.True(HeroLines.Priority(HeroLineTrigger.CanKnocked) > HeroLines.Priority(HeroLineTrigger.Skill1));
            Assert.Equal("hvo_sean_skill1_1", HeroLines.ClipName("sean.skill1.1"));
        }
    }
}
