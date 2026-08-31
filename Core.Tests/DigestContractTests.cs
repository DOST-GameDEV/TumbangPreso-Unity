using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The one value in this project that two different languages have to agree about exactly.
    ///
    /// ⚠️⚠️ `IntegrityRules.Digest` IS WRITTEN TWICE, IN C# AND IN `ugs/cloud-code/match-record.js`,
    /// AND A SILENT DISAGREEMENT WOULD DISPUTE EVERY MATCH IN THE GAME. `docs/TODO.md` § 89.6 is
    /// the existing entry about a rule written twice on purpose; this is the same trade with a
    /// much sharper failure mode, because the symptom is not a number that drifts, it is a ladder
    /// where nobody ever gains a point and nothing anywhere logs an error.
    ///
    /// ⚠️⚠️ SO THE CONTRACT IS A FROZEN STRING RATHER THAN A PROPERTY. Asserting that the C#
    /// agrees with itself proves nothing. `tools/check_digest_contract.js` computes the same value
    /// with the deployed script's own functions and compares it to the literal below, so the two
    /// implementations are pinned to one number that a person had to type.
    ///
    /// ⚠️ IF THIS TEST FAILS AFTER A DELIBERATE CHANGE TO `Canonical`, THE FIX IS TO CHANGE BOTH
    /// SIDES AND THEN THIS LITERAL, IN ONE COMMIT, AND TO SAY SO IN THE HANDOFF. Changing only the
    /// literal makes the test green and the ladder dead.
    /// </summary>
    public class DigestContractTests
    {
        /// <summary>
        /// ⚠️ EVERY FIELD THE DIGEST READS HAS A DISTINCT, NON-DEFAULT VALUE, so a copy that drops
        /// a field or writes them in the wrong order cannot accidentally agree. That includes the
        /// bot line: an all-human fixture would not prove the bot branch writes an empty id.
        /// </summary>
        public static MatchRecord Reference()
        {
            var record = new MatchRecord
            {
                MatchId = "ref-2026-08-31",
                Mode = "HeroStrike",
                MapId = "ilalim_ng_tulay",
                Rounds = 8,
                DurationSeconds = 812.5f,
                PlayedUtc = "2026-08-31T12:34:56Z",
                Ranked = true,
                Players = new[]
                {
                    new PlayerMatchStats { Slot = 0, PlayerId = "aaa", CharacterId = "dante",  Score = 1450 },
                    new PlayerMatchStats { Slot = 1, PlayerId = "bbb", CharacterId = "cheska", Score = 1100 },
                    new PlayerMatchStats { Slot = 2, PlayerId = "",    CharacterId = "sean",   Score = 900, IsBot = true },
                    new PlayerMatchStats { Slot = 3, PlayerId = "ddd", CharacterId = "zack",   Score = 700 },
                },
            };

            MatchRecordRules.AssignPlacements(record);
            record.WinningSlot = 0;
            return record;
        }

        /// <summary>
        /// The frozen digest. ⚠️ `tools/check_digest_contract.js` ASSERTS THE JAVASCRIPT SIDE
        /// PRODUCES THIS SAME STRING.
        /// </summary>
        public const string ReferenceDigest = "7b135cbb69492fa5";

        [Fact]
        public void TheReferenceRecordHashesToTheValueTheCloudCodeScriptAlsoProduces()
        {
            Assert.Equal(ReferenceDigest, IntegrityRules.Digest(Reference()));
        }

        /// <summary>
        /// ⚠️ THE CANONICAL STRING IS PART OF THE CONTRACT AND NOT AN IMPLEMENTATION DETAIL,
        /// because the JS builds it independently. Pinning it too is what turns a mismatch into a
        /// diff a person can read rather than two hex strings that differ.
        /// </summary>
        [Fact]
        public void TheCanonicalStringIsTheFieldOrderBothSidesWrite()
        {
            Assert.Equal(
                "ref-2026-08-31|HeroStrike|ilalim_ng_tulay|8|0|r|" +
                "0,h,aaa,dante,1450,1|" +
                "1,h,bbb,cheska,1100,2|" +
                "2,b,,sean,900,3|" +
                "3,h,ddd,zack,700,4|",
                IntegrityRules.Canonical(Reference()));
        }
    }
}
