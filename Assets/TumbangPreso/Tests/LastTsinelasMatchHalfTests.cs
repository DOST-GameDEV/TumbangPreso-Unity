using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// LAST TSINELAS STANDING has a match half now, and this is what stops it quietly losing one
    /// again. `docs/TODO.md` § 130.13.
    ///
    /// ⚠️⚠️ THE FAULT THIS GUARDS IS A FORMAT THE LOBBY CAN SELECT AND THE MATCH CANNOT PLAY,
    /// WHICH IS EXACTLY WHAT SHIPPED BETWEEN 2026-09-01 AND TODAY. `ProtocolVersion` went to 21
    /// for `MatchFormat`, every machine in a room agreed on the word, `CustomGameRules` had the
    /// rules and `Core.Tests` asserted them, and no code anywhere spent a tsinelas. The only
    /// reason a player never met it is that `ConvertedMatchSetup` deliberately kept the row out
    /// of the picker. **The row is in now**, so the thing keeping the two halves together has to
    /// be a test rather than a comment.
    ///
    /// ⚠️⚠️ AND IT READS THE SOURCE AS TEXT, WHICH IS `WorldCameraPassParityTests`,
    /// `SceneScriptCheck` and `InputSurfaceCheck`'s argument. The match half only runs inside a
    /// live round on a host with four bodies in an arena, so a runtime fixture for it is a
    /// PlayMode test in the group `CLAUDE.md` § 7 says is not a gate. The wiring is visible in
    /// the text whether anybody started a match or not, and the wiring is the half that goes
    /// missing.
    ///
    /// ⚠️ IT ASSERTS THE CONNECTIONS, NOT THE BALANCE. Whether three tsinelas makes a good round
    /// is a number `CustomGameRules.StartingTsinelas` owns and `BotBehaviourProbe` measures.
    /// This asserts that a tag reaches the counter, that the counter can end a round, that the
    /// award is created where every other point in the game is created, and that the elimination
    /// reaches the other three machines.
    /// </summary>
    public sealed class LastTsinelasMatchHalfTests
    {
        private const string Runtime = "Assets/TumbangPreso/Runtime/";

        private static string Read(string relative)
        {
            string path = Path.Combine(Runtime, relative);
            Assert.IsTrue(File.Exists(path), $"{relative} has moved; this test names it by path");
            return File.ReadAllText(path);
        }

        /// <summary>
        /// ⚠️ THE PICKER AND THE MATCH MOVE TOGETHER OR NEITHER MOVES. `docs/TODO.md` § 108's
        /// EQUIP button with no listener is this fault's ancestor and § 130.13 says the format
        /// stays out of the row until the match can run it.
        /// </summary>
        [Test]
        public void TheLobbyOffersTheFormatOnlyBecauseTheMatchCanNowRunIt()
        {
            string setup = Read("UI/ConvertedMatchSetup.cs");

            StringAssert.Contains("FormatOptionCount = 3", setup);
            StringAssert.Contains("MatchFormat.LastTsinelas", setup);

            // Every row the picker can produce has to be a format the game names, which is what
            // stops a fourth row being added without a label.
            for (int index = 0; index < 3; index++)
            {
                var format = (MatchFormat)index;
                Assert.IsFalse(string.IsNullOrWhiteSpace(CustomGameRules.FormatName(format)),
                    $"format row {index} has no player-facing name");
            }
        }

        /// <summary>
        /// ⚠️⚠️ A TAG HAS TO REACH THE COUNTER, AND `RoundDirector.Tagged` IS THE ONLY EVENT THAT
        /// CAN CARRY IT. `CharacterMotor.IsTaggable` already requires `HoldingSlipper`, so a tag
        /// and a spent tsinelas are the same event and no second condition exists to forget. If
        /// this subscription goes, the format becomes a caption again and nothing else fails.
        /// </summary>
        [Test]
        public void ATagSpendsATsinelasAndAnEmptiedAttackerIsSwitchedOutForTheRound()
        {
            string director = Read("LastTsinelasDirector.cs");

            StringAssert.Contains("GameServices.Round.Tagged += OnTagged", director);
            StringAssert.Contains("NetAuthority.ShouldResolve()", director);
            StringAssert.Contains("motor.RoundActive = false", director);
            StringAssert.Contains("CustomGameRules.RoundIsDecided", director);
        }

        /// <summary>
        /// ⚠️⚠️ THE AWARD GOES THROUGH `AddScore` LIKE EVERY OTHER POINT IN THE GAME.
        /// `MatchDirector`'s header: a point that can only be created in one function cannot be
        /// created on a client at all. A format that paid its winner directly would be the first
        /// exception to that in the whole repository.
        ///
        /// ⚠️ AND IT IS PAID BEFORE `BeginIntermission`, WHICH IS LOAD-BEARING RATHER THAN
        /// TIDY. `BeginIntermission` sets `IsWarmupBuffer` and `AddScore` returns early on
        /// exactly that flag, so the two lines in the wrong order pay nobody, silently, on the
        /// one award the format exists to make.
        /// </summary>
        [Test]
        public void TheAwardIsCreatedWhereEveryOtherPointIsAndBeforeTheBufferOpens()
        {
            string director = Read("LastTsinelasDirector.cs");

            StringAssert.Contains("AddScore(winner, ScoreEvent.LastTsinelasStanding)", director);

            // ⚠️⚠️ IT MATCHES THE CALLS AND NOT THE NAMES, WHICH IS THE DIFFERENCE BETWEEN
            // ASSERTING THE CODE AND ASSERTING THE PROSE. The first draft searched for
            // "BeginIntermission" and found it in `Decide`'s own doc comment, several hundred
            // characters ahead of the award, and failed a file that was correct. Every ⚠️ note in
            // this repository names the thing it is about, so a bare identifier is nearly always
            // a comment before it is ever a call.
            int award = director.IndexOf("AddScore(winner, ScoreEvent.LastTsinelasStanding)",
                                         System.StringComparison.Ordinal);
            int buffer = director.IndexOf("GameServices.Match?.BeginIntermission()",
                                          System.StringComparison.Ordinal);

            Assert.Greater(award, 0, "the last-standing award has gone");
            Assert.Greater(buffer, 0, "the round no longer ends through the intermission");
            Assert.Less(award, buffer,
                "the award is written after BeginIntermission, which sets IsWarmupBuffer and " +
                "makes AddScore return early. The winner would be paid nothing, silently.");
        }

        /// <summary>
        /// ⚠️⚠️ THE ELIMINATION HAS TO TRAVEL, AND THIS IS THE HALF THAT COST A PROTOCOL BUMP.
        /// `RoundActive` is a local flag: a peer that never hears the stock table lets an
        /// eliminated player go on throwing and grabbing while the host ignores every request,
        /// which is a player being told the game is broken rather than a wrong number.
        /// </summary>
        [Test]
        public void TheStockTableReachesEveryPeerAndTheProtocolMovedForIt()
        {
            string rpc = Read("Net/MatchRpc.cs");
            string session = Read("Net/NetSession.cs");

            StringAssert.Contains("RegisterNamedMessageHandler(\"Tsinelas\", OnTsinelasMsg)", rpc);
            StringAssert.Contains("public void BroadcastTsinelas(int[] stocks)", rpc);
            StringAssert.Contains("ApplyNetworkStocks", rpc);

            StringAssert.Contains("ProtocolVersion = 22", session);
            Assert.AreEqual(22, Net.NetSession.ProtocolVersion,
                "the match half is on the wire, so the protocol must have moved and both " +
                "players must be rebuilt from the same commit.");
        }

        /// <summary>
        /// ⚠️ THE SERVICE EXISTS IN EVERY MATCH AND IS INERT IN EVERY OTHER FORMAT, which is why
        /// it is built in `GameServices` rather than by an arena. `SliceRunner` and
        /// `MatchBootstrap` are two runners, and a rule only one of them installed is a rule the
        /// other silently lacks: that is `MatchDirector.SkipBuffer`'s own recorded argument.
        /// </summary>
        [Test]
        public void TheDirectorIsBuiltOnceForBothRunnersAndAfterTheTwoItListensTo()
        {
            string services = Read("GameServices.cs");

            StringAssert.Contains("AddComponent<LastTsinelasDirector>()", services);
            StringAssert.Contains("Tsinelas = null", services);

            int round = services.IndexOf("AddComponent<RoundDirector>()", System.StringComparison.Ordinal);
            int tsinelas = services.IndexOf("AddComponent<LastTsinelasDirector>()", System.StringComparison.Ordinal);

            Assert.Greater(round, 0);
            Assert.Less(round, tsinelas,
                "LastTsinelasDirector.OnEnable subscribes to Match.RoundStarted and Round.Tagged, " +
                "and AddComponent runs OnEnable immediately. Built first, it subscribes to nulls " +
                "and the format silently never counts a tag.");
        }

        /// <summary>
        /// ⚠️ A PLAYER WHO IS OUT IS TOLD SO, AND TOLD BEFORE THEY ARE. `CLAUDE.md` § 6.2: a
        /// screen that names a control which does nothing is the intuitive claim failed in the
        /// most direct way there is, and "RETRIEVE A SLIPPER" over a body that cannot grab one
        /// is exactly that.
        /// </summary>
        [Test]
        public void TheHudSaysHowManyAreLeftAndSaysWhenThereAreNone()
        {
            string hud = Read("UI/Hud.cs");

            StringAssert.Contains("OUT  ·  NO TSINELAS LEFT", hud);
            StringAssert.Contains("LAST TSINELAS  ·  DO NOT GET TAGGED", hud);
            StringAssert.Contains("case ScoreEvent.LastTsinelasStanding: return \"LAST TSINELAS\"", hud);
        }
    }
}
