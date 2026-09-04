using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The 2026-09-05 nationals pass, asserted.
    ///
    /// ⚠️⚠️ THESE ARE EDITMODE AND NOT `Core.Tests`, AND THAT IS A MACHINE FACT RATHER THAN A
    /// PREFERENCE. `CLAUDE.md` § 7's Mac table: **dotnet is not installed**, so
    /// `dotnet test Core.Tests/...`, the cheapest signal in the repository and the one § 2.1b
    /// says to run freely, cannot run at all there. A rule asserted only in a project that
    /// cannot be built on the machine somebody is sitting at is a rule nobody is running, and
    /// this assembly references `TumbangPreso.Core` directly, so every core rule below is
    /// asserted with no engine dependency beyond the runner.
    ///
    /// ⚠️ THE UNITY-SIDE ONES ARE HERE FOR THE OPPOSITE REASON: they read `NetSession`,
    /// `BuildIdentity` and `TournamentGuard`, which the core may never see (`CLAUDE.md` § 4).
    /// </summary>
    public class NationalsHardeningTests
    {
        // -------------------------------------------------------------------
        // § 143.9  HOST LOSS
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE ONE THAT MATTERS. `NetSession.IsHost` is
        /// <c>_nm == null || !_nm.IsListening || _nm.IsServer</c>, so a CLIENT whose transport has
        /// just stopped satisfies the middle clause and starts answering true, and
        /// `NetAuthority.ShouldResolve()` was exactly `IsHost`. Four clients that lose one host
        /// become four referees in four copies of the same arena, each awarding its own points.
        /// </summary>
        [Test]
        public void LosingTheHostRevokesThisPeersAuthorityAndQuittingDoesNot()
        {
            Assert.IsTrue(SessionEndRules.RevokesAuthority(SessionEndCause.HostLost),
                "A peer whose host vanished must stop deciding. It is the only thing standing " +
                "between a lost host and four peers refereeing four different matches.");

            Assert.IsTrue(SessionEndRules.RevokesAuthority(SessionEndCause.RemovedByHost),
                "A peer the host removed is in identical local state to one whose host died: a " +
                "stopped transport with an arena still on screen. Either resolving a tag is the " +
                "same defect.");

            Assert.IsTrue(SessionEndRules.RevokesAuthority(SessionEndCause.VersionMismatch));
            Assert.IsTrue(SessionEndRules.RevokesAuthority(SessionEndCause.LobbyFull));

            Assert.IsFalse(SessionEndRules.RevokesAuthority(SessionEndCause.LocalQuit),
                "A player who pressed QUIT is navigating away under their own steam. Revoking " +
                "here would take authority off the solo match they are about to start.");

            Assert.IsFalse(SessionEndRules.RevokesAuthority(SessionEndCause.None),
                "Nothing has ended. This is the resting value and every offline match is in it.");
        }

        /// <summary>
        /// ⚠️ THE CLASSIFICATION IS WHAT THREE DIFFERENT READERS USED TO DERIVE SEPARATELY: the
        /// telemetry bucket, the player-facing line and now the authority latch. `docs/TODO.md`
        /// § 94.1 is what happens when a derived fact has more than one derivation.
        /// </summary>
        [Test]
        public void EveryDisconnectReasonThisGameCanProduceLandsOnOneCause()
        {
            Assert.AreEqual(SessionEndCause.LocalQuit,
                SessionEndRules.Classify("anything at all", wasLocal: true),
                "A local shutdown wins over whatever the transport says behind it.");

            Assert.AreEqual(SessionEndCause.HostLost, SessionEndRules.Classify(null, false));
            Assert.AreEqual(SessionEndCause.HostLost, SessionEndRules.Classify("   ", false));

            // Netcode's own envelope. `PlayerFacingDisconnectReason` records this exact string.
            Assert.AreEqual(SessionEndCause.HostLost, SessionEndRules.Classify(
                "[Disconnect Event][Client-0][TransportClientId-0][TransportShutdown] " +
                "NetworkConnectionManager was shutdown. The transport was shutdown.", false),
                "A bracketed reason describes the mechanism and never the cause, so the honest " +
                "reading is that the transport gave up and nobody said why.");

            Assert.AreEqual(SessionEndCause.HostLost,
                SessionEndRules.Classify(Net.NetSession.HostLeftMessage, false),
                "The host's own goodbye and its silence are one event on the wire, which is what " +
                "docs/TODO.md § 140.5 says in as many words.");

            // The three sentences `ApproveConnection` actually authors.
            Assert.AreEqual(SessionEndCause.VersionMismatch, SessionEndRules.Classify(
                $"Game version mismatch (network protocol {Net.NetSession.ProtocolVersion})", false));
            Assert.AreEqual(SessionEndCause.LobbyFull, SessionEndRules.Classify(
                "This game is full: 4 players and 4 spectators.", false));
            Assert.AreEqual(SessionEndCause.Replaced,
                SessionEndRules.Classify("Replaced by reconnect", false));

            Assert.AreEqual(SessionEndCause.RemovedByHost,
                SessionEndRules.Classify("Could not join this game.", false),
                "A block is the referee alive and making a decision, which is not host loss.");
        }

        /// <summary>
        /// ⚠️ THE DIAGNOSTIC MUST NAME HOST LOSS RATHER THAN LOOKING LIKE A NORMAL ENDING.
        /// `docs/TODO.md` § 143.9's brief: *"the diagnostic reason must identify host loss rather
        /// than pretending the match ended normally."*
        /// </summary>
        [Test]
        public void TheDiagnosticSaysTheMatchWasAbandonedAndAtWhichRound()
        {
            string line = SessionEndRules.Diagnostic(SessionEndCause.HostLost, 3, 4);

            StringAssert.Contains("HostLost", line);
            StringAssert.Contains("ABANDONED", line);
            StringAssert.Contains("round 3 of 4", line);

            StringAssert.DoesNotContain("ABANDONED",
                SessionEndRules.Diagnostic(SessionEndCause.LocalQuit, 3, 4),
                "Leaving on purpose is not an abandonment and must not read as one.");

            StringAssert.Contains("host left",
                SessionEndRules.PlayerLine(SessionEndCause.HostLost).ToLowerInvariant(),
                "A player told only that they were disconnected goes looking for a fault in " +
                "their own wifi.");
        }

        /// <summary>
        /// ⚠️⚠️ THE LATCH HAS TO BE DISARMABLE OR IT TAKES THE SOLO GAME WITH IT.
        /// `NetAuthority.ShouldResolve()` is what runs single player, so a revocation that
        /// outlived its match would leave a player unable to resolve their own practice round.
        /// </summary>
        [Test]
        public void TheAuthorityLatchClearsAndForgets()
        {
            MatchAbandon.Forget();
            Assert.IsFalse(MatchAbandon.AuthorityRevoked);
            Assert.AreEqual(SessionEndCause.None, MatchAbandon.Cause);

            MatchAbandon.Note("[Disconnect Event] transport shutdown", wasLocal: false);
            Assert.IsTrue(MatchAbandon.AuthorityRevoked, "Host loss revokes.");
            Assert.AreEqual(SessionEndCause.HostLost, MatchAbandon.Cause);
            Assert.IsFalse(NetAuthority.ShouldResolve(),
                "ShouldResolve is IsHost AND not-revoked. With no transport IsHost is true, " +
                "which is exactly the state a client is in the moment its host vanishes.");

            MatchAbandon.Clear();
            Assert.IsFalse(MatchAbandon.AuthorityRevoked,
                "Leaving the arena hands authority back, or the next solo match cannot resolve.");
            Assert.AreEqual(SessionEndCause.HostLost, MatchAbandon.Cause,
                "The REASON survives a Clear: the screen the player lands on wants to print it.");
            Assert.IsTrue(NetAuthority.ShouldResolve());

            MatchAbandon.Forget();
            Assert.AreEqual(SessionEndCause.None, MatchAbandon.Cause,
                "A new session is not a continuation of the last one.");
        }

        // -------------------------------------------------------------------
        // § 144.7  THE SEAT HANDOVER'S RATING
        // -------------------------------------------------------------------

        /// <summary>
        /// The acceptance test § 144.7 asked for, in its own numbers.
        ///
        /// ⚠️ THE BAND EDGES ARE DERIVED AND ARE NOT RESTATED HERE. `SeatHandover.BataCeiling`
        /// and `AstigFloor` are `RatingRules.StartRating` +/- `MatchmakingRules.MaxHalfWidth`;
        /// asserting 1000 and 2000 as literals would be a second copy of a number that is
        /// deliberately computed.
        /// </summary>
        [Test]
        public void ARatingOf2400GetsAstigAnd700GetsBata()
        {
            Assert.AreEqual(Difficulty.Astig, SeatHandover.TierFor(2400));
            Assert.AreEqual(Difficulty.Bata, SeatHandover.TierFor(700));
            Assert.AreEqual(Difficulty.Normal,
                SeatHandover.TierFor((int)RatingRules.StartRating));
        }

        /// <summary>
        /// ⚠️⚠️ THE FIELD HAS TO SURVIVE THE SECOND ARRIVAL OR IT IS ZERO FOR EVERY PEER THAT
        /// EVER COMPLETED A JOIN. A peer reaches `Admit` twice: once from the approval hello,
        /// which is the only message carrying a rating, and again from `MatchRpc.HandleIdentify`.
        /// The second call builds a fresh record, so anything `Admit` does not copy forward is
        /// silently zeroed by the peer introducing itself.
        /// </summary>
        [Test]
        public void ARatingSurvivesThePeerIntroducingItselfASecondTime()
        {
            var lobby = new Net.LobbySession();

            var first = lobby.Admit(7, "token-abc", "Player");
            first.Rating = 2400;

            var again = lobby.Admit(9, "token-abc", "Player", out int replaced);

            Assert.AreEqual(7, replaced, "The same durable token replaces the earlier transport.");
            Assert.AreEqual(2400, again.Rating,
                "The rating was zeroed by the peer's own Identify. `MatchRpc" +
                ".RatingForDepartedPeer` would then read 0 for every peer in the game and the " +
                "seat handover would keep the lobby's tier forever, silently.");
            Assert.AreEqual(Difficulty.Astig, SeatHandover.TierFor(again.Rating));
        }

        /// <summary>
        /// ⚠️ A PEER THAT SAYS NOTHING LEAVES THE TIER ALONE, which is what the seat did before
        /// the field existed. A LAN guest with no career and a peer that never signed in both
        /// arrive with nothing to say, and inventing a mid-ladder guess for them would hand a
        /// stranger's chair a bot matched to a number nobody measured.
        /// </summary>
        [Test]
        public void ARatingOfZeroIsNotATierItIsSilence()
        {
            var lobby = new Net.LobbySession();
            var record = lobby.Admit(3, "token-quiet", "Guest");

            Assert.AreEqual(0, record.Rating,
                "The default has to be 0 rather than the start rating, or 'did not say' and " +
                "'is average' become the same claim.");
        }

        /// <summary>
        /// ⚠️⚠️ THE BUMP IS A DELIBERATE ACT AND THIS IS ONE OF THE THINGS THAT MAKES IT ONE.
        /// `CLAUDE.md` § 4a: when the protocol moves, the Windows and Android players are rebuilt
        /// from the same commit and shipped together, or they refuse each other correctly and it
        /// reads as a bug.
        /// </summary>
        [Test]
        public void TheHelloCarriesARatingAndTheProtocolMovedForIt()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "TumbangPreso/Runtime/Net/NetSession.cs"));

            StringAssert.Contains("public int Rating;", source,
                "ConnectionHello no longer carries a rating, so MatchRpc.RatingForDepartedPeer " +
                "is back to answering 0 and Attention.md § 16.1's ruling is unbuilt again.");

            Assert.GreaterOrEqual(Net.NetSession.ProtocolVersion, 24,
                "The rating is on the connection hello, so a peer without it hands an abandoned " +
                "seat over by a different rule. A room with one of each obeys the ruling for " +
                "some seats and not others, and nobody in it can tell which they got.");
        }

        // -------------------------------------------------------------------
        // § 141  SEAT OWNERSHIP, BOTH DIRECTIONS
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE DIRECTION THE OLD MODEL COULD NOT EXPRESS. `MatchSnapshot.SeatOwners` is a
        /// string per seat, so "two peers claim seat 2" is not a state it can be IN: whichever
        /// wrote the cell last is the only one a checker ever sees. `CheckSeatOwnership`'s own
        /// note claimed both directions for months while the data carried one.
        /// </summary>
        [Test]
        public void TwoPeersDrivingOneSeatIsRepresentableAndCaught()
        {
            var claims = new[]
            {
                new SeatClaim("alice", 0, driving: true, spectating: false),
                new SeatClaim("bob", 2, driving: true, spectating: false),
                new SeatClaim("carol", 2, driving: true, spectating: false),
            };

            var faults = MatchInvariants.CheckSeatClaims(claims);

            Assert.AreEqual(1, faults.Count, "Exactly one fault: seat 2 has two drivers.");
            StringAssert.Contains("seat 2 is driven by", faults[0]);
        }

        [Test]
        public void OnePeerDrivingTwoSeatsIsCaught()
        {
            var claims = new[]
            {
                new SeatClaim("alice", 0, driving: true, spectating: false),
                new SeatClaim("alice", 3, driving: true, spectating: false),
            };

            var faults = MatchInvariants.CheckSeatClaims(claims);

            Assert.AreEqual(1, faults.Count);
            StringAssert.Contains("drives seat 0 and seat 3", faults[0]);
        }

        /// <summary>
        /// ⚠️ § 141'S HEADLINE, IN 🧑'S WORDS: **"IF IT isnt spectator why do i see spectator
        /// hud"**. The cause was found and fixed and no invariant could state it, because a model
        /// with one owner string per seat has nowhere to put "and this one thinks it is watching".
        /// </summary>
        [Test]
        public void ASpectatorDrivingASeatIsCaught()
        {
            var faults = MatchInvariants.CheckSeatClaims(new[]
            {
                new SeatClaim("watcher", 1, driving: true, spectating: true),
            });

            Assert.AreEqual(1, faults.Count);
            StringAssert.Contains("spectating and driving seat 1", faults[0]);
        }

        /// <summary>
        /// ⚠️⚠️ THE RECONNECT WINDOW IS NOT A FAULT AND MUST NOT READ AS ONE. `LobbySession
        /// .Depart` holds a chair against a durable token so a returning player gets their own
        /// back, and for that window the seat has a claimant driving nothing. A checker that
        /// counted it would report the feature every time it worked.
        /// </summary>
        [Test]
        public void AHeldSeatIsAClaimThatIsNotADriver()
        {
            var faults = MatchInvariants.CheckSeatClaims(new[]
            {
                new SeatClaim("returning", 2, driving: false, spectating: false),
                new SeatClaim("standin", 2, driving: true, spectating: false),
            });

            CollectionAssert.IsEmpty(faults,
                "A held chair beside a driven one is the reconnect window, which is a feature.");
        }

        /// <summary>
        /// ⚠️⚠️ OWNERSHIP WAS MISSING FROM THE PEER COMPARISON AND ITS OWN HEADER SAID IT SHOULD
        /// NOT BE (*"§ 141 is the seat"*). Two peers can hold identical rounds, tayas, scores and
        /// winners and still disagree about who seat 2 is, and the seat decides whose tsinelas is
        /// whose and which line a point is written to.
        /// </summary>
        [Test]
        public void TwoPeersThatAgreeOnEveryNumberAndDisagreeAboutAChairAreCaught()
        {
            var scores = new[] { 100, 0, 0, 0 };

            var host = new MatchSnapshot(2, 4, MatchRules.DefenderSlotFor(2), true, false, scores,
                new[] { "alice", "bob", "carol", "dave" });

            var client = new MatchSnapshot(2, 4, MatchRules.DefenderSlotFor(2), true, false, scores,
                new[] { "alice", "carol", "bob", "dave" });

            var faults = MatchInvariants.CheckPeersAgree("host", host, "client", client);

            Assert.AreEqual(2, faults.Count,
                "Seats 1 and 2 are swapped between the peers and nothing else differs.");
            foreach (string fault in faults) StringAssert.Contains("says", fault);
        }

        /// <summary>
        /// ⚠️ AN EMPTY CHAIR ON ONE SIDE IS NOT A DISAGREEMENT. A client is routinely mid-build
        /// (`docs/TODO.md` § 82.1) and a bot-filled seat has no owner token at all; the fault
        /// worth failing on is two peers naming DIFFERENT people.
        /// </summary>
        [Test]
        public void ASeatOneSideHasNotHeardAboutIsNotADisagreement()
        {
            var scores = new int[4];

            var host = new MatchSnapshot(1, 4, 0, true, false, scores,
                new[] { "alice", "bob", null, null });
            var client = new MatchSnapshot(1, 4, 0, true, false, scores,
                new[] { "alice", null, null, null });

            CollectionAssert.IsEmpty(
                MatchInvariants.CheckPeersAgree("host", host, "client", client));
        }

        // -------------------------------------------------------------------
        // § 147  HIGHLIGHT MARKERS
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ ONE GAMEPLAY EVENT REACHES THIS LAYER SEVERAL TIMES AND MUST PRODUCE ONE MARKER.
        /// `SpectatorCamera`'s own header records the cost of not having this rule: *"a knockdown,
        /// a tag and a sabotage are three separate triggers, and `PollHighlights` adds a fourth"*,
        /// which produced the replay spam 🧑 reported and got a whole trigger deleted.
        /// </summary>
        [Test]
        public void OneEventReportedFourTimesIsOneMarker()
        {
            var log = new HighlightLog();

            for (int i = 0; i < 4; i++)
            {
                log.Add(new HighlightMarker(HighlightKind.Tag, 10.0f + (i * 0.05f), 1, 2, 3,
                                            0.0f, 0.5f));
            }

            Assert.AreEqual(1, log.Markers.Count);
            Assert.AreEqual(1, log.Recorded);
            Assert.AreEqual(3, log.Deduplicated);
        }

        /// <summary>
        /// ⚠️ THE WINDOW IS NOT A MUTE. Two genuinely separate events past it are two markers, or
        /// the rule would be silently swallowing a match.
        /// </summary>
        [Test]
        public void TwoRealEventsPastTheWindowAreTwoMarkers()
        {
            var log = new HighlightLog();

            log.Add(new HighlightMarker(HighlightKind.Tag, 10.0f, 1, 2, 3, 0.0f, 0.5f));
            log.Add(new HighlightMarker(HighlightKind.Tag,
                10.0f + HighlightRules.SameEventSeconds + 0.01f, 1, 2, 3, 0.0f, 0.5f));

            Assert.AreEqual(2, log.Markers.Count);

            // ⚠️ AND THE WINDOW IS SHORTER THAN THE FASTEST REAL REPEAT. The taya cannot tag
            // twice inside `PunchCooldown`, so no genuine second event is being swallowed.
            Assert.Less(HighlightRules.SameEventSeconds, Balance.LungeCooldown + 0.01f);
        }

        /// <summary>Two different seats doing the same thing at once are two moments.</summary>
        [Test]
        public void TheDedupeIsPerSeatAndNotGlobal()
        {
            var log = new HighlightLog();

            log.Add(new HighlightMarker(HighlightKind.CloseCall, 5.0f, 1, 1, 0, 0.4f, 0.7f));
            log.Add(new HighlightMarker(HighlightKind.CloseCall, 5.02f, 1, 2, 0, 0.9f, 0.3f));

            Assert.AreEqual(2, log.Markers.Count,
                "Two attackers escaping the same taya in the same instant is two escapes.");
        }

        /// <summary>
        /// ⚠️ THE SAME INPUT PRODUCES THE SAME LOG. `docs/TODO.md` § 147: *"tests should prove
        /// the markers are deterministic"*, which is a claim about the RULES having no clock and
        /// no randomness in them, and is why they live in the engine-free core.
        /// </summary>
        [Test]
        public void TheSameSequenceOfEventsProducesTheSameLogEveryTime()
        {
            var events = new[]
            {
                (HighlightKind.BankShot, 3.0f, 0, 0.0f),
                (HighlightKind.CloseCall, 4.1f, 1, 0.42f),
                (HighlightKind.CloseCall, 4.2f, 1, 0.42f),
                (HighlightKind.LongKnockdown, 9.5f, 2, 11.4f),
                (HighlightKind.Tag, 12.0f, 3, 0.0f),
            };

            List<string> First()
            {
                var log = new HighlightLog();
                foreach (var (kind, at, actor, measure) in events)
                {
                    log.Add(new HighlightMarker(kind, at, 1, actor, -1, measure,
                                                HighlightRules.ImportanceFor(kind, measure)));
                }

                return log.Report();
            }

            CollectionAssert.AreEqual(First(), First());
            Assert.AreEqual(4, First().Count, "The repeated close call folds into one.");
        }

        /// <summary>
        /// ⚠️⚠️ THE THRESHOLDS ARE DERIVED AND THIS IS WHAT SAYS SO. A close call is the taya's
        /// own reach, a long knockdown is the box, and last-second is one full throw wind-up.
        /// Each is a number the game already has an argument for.
        /// </summary>
        [Test]
        public void EveryHighlightThresholdIsANumberTheGameAlreadyHad()
        {
            Assert.AreEqual(Balance.LungeTagRadius, HighlightRules.CloseCallMetres,
                "A close call is 'inside the distance the taya's dash actually catches you'.");
            Assert.AreEqual(Balance.ConfinementRadius, HighlightRules.LongKnockdownMetres,
                "A long knockdown is one thrown from outside the danger zone.");
            Assert.AreEqual(Balance.ChargeFullTime, HighlightRules.LastSecondSeconds,
                "Last-second is 'nothing started now can still land', which is one full charge.");
            Assert.AreEqual(Balance.TagStunTime, HighlightRules.EvasionWindowSeconds,
                "An evasion run is escapes inside the time one failure would have removed you.");
        }

        /// <summary>The replay's join: a marker names a window a clip can be cut around.</summary>
        [Test]
        public void AMarkerNamesAWindowAReplayCanFind()
        {
            var log = new HighlightLog();
            log.Add(new HighlightMarker(HighlightKind.Tag, 40.0f, 2, 1, 3, 0.0f, 0.5f));

            Assert.IsTrue(log.NewestWindow(3.5f, 1.3f, out float from, out float to,
                                           out var marker));

            Assert.AreEqual(36.5f, from, 0.001f);
            Assert.AreEqual(41.3f, to, 0.001f);
            Assert.AreEqual(HighlightKind.Tag, marker.Kind);

            Assert.IsFalse(new HighlightLog().NewestWindow(3.5f, 1.3f, out _, out _, out _),
                "An empty log answers false rather than a window around zero.");
        }

        /// <summary>
        /// ⚠️⚠️ IT MAY NEVER TOUCH THE SCORE. `docs/VISION.md` § 4: every point is awarded in one
        /// function, host-side. This reads the highlight sources as TEXT for
        /// `SceneScriptCheck`'s reason: a rule about what a file may NOT reference cannot be
        /// checked by running it, because the absence of a call is invisible at runtime.
        /// </summary>
        [Test]
        public void NothingInTheHighlightLayerCanAwardAPoint()
        {
            string[] files =
            {
                "TumbangPreso/Runtime/Diagnostics/MatchHighlights.cs",
                "TumbangPreso/Runtime/Diagnostics/HighlightWatch.cs",
            };

            foreach (string relative in files)
            {
                string source = CodeOnly(
                    File.ReadAllText(Path.Combine(Application.dataPath, relative)));

                StringAssert.DoesNotContain("AddScore", source,
                    $"{relative} reaches the scoreboard. A highlight is a record of something " +
                    $"that happened, and a record that pays is a balance change wearing a " +
                    $"reporting layer's name.");
                StringAssert.DoesNotContain("ReportStyle", source,
                    $"{relative} awards Street Hype. That is Classic's own cosmetic bar and it " +
                    $"has its own call sites; a second writer would double every award.");
            }

            string core = CodeOnly(File.ReadAllText(Path.Combine(
                Application.dataPath, "../Packages/com.tumbangpreso.core/Runtime/Highlights.cs")));

            StringAssert.DoesNotContain("UnityEngine", core,
                "The highlight rules are in the engine-free core (CLAUDE.md § 4).");
        }

        /// <summary>
        /// A source file with its line comments removed.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE THE FIRST VERSION OF THE TEST ABOVE FAILED ON ITS OWN SUBJECT'S
        /// COMMENT, and `tools/audit_audio_reach.py` made the identical mistake in the other
        /// direction and LIED for its whole life: `CLAUDE.md` § 7.1 records it as *"the only
        /// audit that did not strip comments before looking for a gate"*, so a header explaining
        /// the gate it replaces registered as a gate. **A text check that reads prose as code is
        /// a text check that answers a different question than the one it prints**, and this file
        /// is written in a house style where the explanation of a rule names the rule.
        ///
        /// ⚠️ LINE COMMENTS ONLY, WHICH COVERS `//` AND `///`. Nothing in this repository's
        /// runtime uses block comments for documentation, and a half-written block stripper that
        /// swallowed a string containing an asterisk-slash would be worse than none.
        /// </summary>
        private static string CodeOnly(string source)
        {
            var kept = new System.Text.StringBuilder(source.Length);

            foreach (string line in source.Split('\n'))
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("//")) continue;

                int at = line.IndexOf("//", System.StringComparison.Ordinal);
                kept.AppendLine(at >= 0 ? line.Substring(0, at) : line);
            }

            return kept.ToString();
        }

        // -------------------------------------------------------------------
        // § 145.2  BUILD IDENTITY
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ "CANNOT DETERMINE" IS NOT "CLEAN", AND A STAMP FROM BEFORE THE THREE-WAY FIELD
        /// CANNOT BE UPGRADED BY GUESSING. Its `dirty` flag came from an mtime heuristic that
        /// could not see an unstaged edit, so reading its `false` as Clean would carry that blind
        /// spot forward into the gate built to replace it.
        /// </summary>
        [Test]
        public void AnUnknownWorkingTreeIsNeverReadAsClean()
        {
            Assert.AreEqual(BuildIdentity.TreeState.Unknown, BuildIdentity.StateOf(null));

            Assert.AreEqual(BuildIdentity.TreeState.Unknown,
                BuildIdentity.StateOf(new BuildIdentity.Record { dirty = false }),
                "An old stamp carries `dirty` and no `treeState`. It is a tree nobody knows.");

            Assert.AreEqual(BuildIdentity.TreeState.Clean,
                BuildIdentity.StateOf(new BuildIdentity.Record { treeState = "clean" }));
            Assert.AreEqual(BuildIdentity.TreeState.Dirty,
                BuildIdentity.StateOf(new BuildIdentity.Record { treeState = "dirty" }));
            Assert.AreEqual(BuildIdentity.TreeState.Unknown,
                BuildIdentity.StateOf(new BuildIdentity.Record { treeState = "wat" }));
        }

        /// <summary>
        /// ⚠️ THE HEURISTIC IS GONE FROM THE BUILDER. It could not see an ordinary unstaged edit
        /// editing a file does not rewrite `.git/index`, and it answered "clean" outright on a
        /// packed ref, which is the state a freshly cloned build machine is in.
        /// </summary>
        [Test]
        public void TheBuilderAsksGitRatherThanComparingTimestamps()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "TumbangPreso/Editor/GameBuilder.cs"));

            StringAssert.Contains("status --porcelain", source,
                "The build no longer establishes the working tree from git, so the SHA it " +
                "stamps is a claim nothing checked.");

            StringAssert.DoesNotContain("File.GetLastWriteTimeUtc(index)", source,
                "The mtime heuristic is back. It cannot see an unstaged edit and answers clean " +
                "on a packed ref: both of the commonest cases, both in the direction that makes " +
                "the flag worse than absent.");
        }

        // -------------------------------------------------------------------
        // § 145.3  THE TOURNAMENT MODIFIER REGISTER
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE AUDIT PROVED TWO LISTS AGREED AND COULD NOT SEE SWITCH NUMBER NINE. This is
        /// its claim asserted from inside Unity: every SETTABLE static bool in the gameplay
        /// runtime is either a tournament modifier or is written down as not being one.
        ///
        /// ⚠️ SETTABLE IS THE FILTER. There are forty-one static bools in the runtime and most
        /// are derived properties nothing outside can write, so nothing can leave one set, which
        /// is the entire hazard. Listing all of them would be the noisy gate developers learn to
        /// ignore.
        /// </summary>
        [Test]
        public void EverySettableStaticSwitchIsEitherAModifierOrWrittenDownAsNotOne()
        {
            var pattern = new Regex(
                @"^\s*(?:public|internal)\s+static\s+bool\s+([A-Z][A-Za-z0-9_]*)\s*(?:;|=[^>]|\{\s*get;\s*set;)",
                RegexOptions.Multiline);

            string runtime = Path.Combine(Application.dataPath, "TumbangPreso/Runtime");
            var unaccounted = new List<string>();

            foreach (string file in Directory.GetFiles(runtime, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                string type = Path.GetFileNameWithoutExtension(file);

                foreach (Match m in pattern.Matches(text))
                {
                    string name = $"{type}.{m.Groups[1].Value}";
                    if (!TournamentPreset.IsAccountedFor(name)) unaccounted.Add(name);
                }
            }

            CollectionAssert.IsEmpty(unaccounted,
                "A settable static gameplay switch is on neither TournamentPreset.Modifiers nor " +
                "TournamentPreset.NotModifiers: " + string.Join(", ", unaccounted) + ". Every " +
                "switch this game has for testing survives a scene change by definition, and an " +
                "operator starting the next bracket match inherits whatever the last one left " +
                "behind. Add it to the roster with its reason, or to the exemption list with the " +
                "reason it cannot change a match.");
        }

        /// <summary>Every exemption carries a reason, for the roster's own argument.</summary>
        [Test]
        public void EveryTournamentExemptionSaysWhy()
        {
            foreach (var m in TournamentPreset.NotModifiers)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(m.Name));
                Assert.Greater(m.Why.Length, 40,
                    $"{m.Name} is exempted with no reason written down. A bare list gets a row " +
                    $"deleted in a tidy-up by somebody who cannot tell whether it was ever " +
                    $"thought about.");
            }

            // ⚠️ AND NOTHING IS ON BOTH LISTS, which would be a switch the guard clears and the
            // roster says is none of its business.
            foreach (var m in TournamentPreset.Modifiers)
            {
                foreach (var n in TournamentPreset.NotModifiers)
                    Assert.AreNotEqual(m.Name, n.Name,
                        $"{m.Name} is a modifier AND an exemption. One of the two is wrong.");
            }
        }

        // -------------------------------------------------------------------
        // § 146  THE RETRIEVAL SLIDE'S NUMBERS
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ EVERY SLIDE CONSTANT IS SOLVED FROM ONE THE GAME ALREADY HAD, AND THIS IS THE
        /// ARITHMETIC. `CLAUDE.md` § 4: *"Write the distance you want and solve for the speed;
        /// never hard-code a distance beside a speed."*
        /// </summary>
        [Test]
        public void TheSlideIsSolvedFromFrictionAndThePickupRadius()
        {
            Assert.AreEqual(Balance.PickupRadius, Balance.SlideDistance,
                "The slide converts the LAST APPROACH into a commitment, and the last approach " +
                "is by definition the distance at which a pickup starts working.");

            float solved = Mathf.Sqrt(2.0f * Balance.Friction * Balance.SlideDistance);
            Assert.AreEqual(solved, Balance.SlideSpeed, 0.001f);

            // The impulse decays against Friction, so this is exactly how long it lives.
            Assert.AreEqual(Balance.SlideSpeed / Balance.Friction, Balance.SlideActiveTime,
                            0.001f);

            // ⚠️ THE ADVANTAGE IS ABOUT A THIRD OF A SECOND, AND IT IS BOUNDED BY BEING LESS
            // THAN ONE TAYA DECISION. Walking the same ground at the attacker's speed is 0.69 s.
            float walking = Balance.SlideDistance
                            / (Balance.Speed * Balance.AttackerSpeedScale);

            Assert.Greater(walking - Balance.SlideActiveTime, 0.2f,
                "The slide has to be worth pressing.");
            Assert.Less(walking - Balance.SlideActiveTime, Balance.PunchCooldown,
                "And it must be worth less than one taya decision, or it is a free escape.");
        }

        /// <summary>
        /// ⚠️⚠️ THE COMMITMENT MUST OUTLAST A PERFECT READ OR THE SLIDE IS A FREE MOBILITY BUFF.
        /// A taya who reads it has to charge (`LungeChargeTime`) and then dash
        /// (`LungeActiveTime`); if the attacker were free before that, the read could not be
        /// cashed in and the verb would be strictly better than walking with no downside.
        /// </summary>
        [Test]
        public void TheCommittedWindowIsExactlyTheTayasPunishCycle()
        {
            float committed = Balance.SlideActiveTime + Balance.SlideRecoveryTime;
            float punish = Balance.LungeChargeTime + Balance.LungeActiveTime;

            Assert.AreEqual(punish, committed, 0.001f);

            // ⚠️ AND THE COOLDOWN CARRIES THE TAYA'S OWN COOLDOWN ON TOP, so an attacker cannot
            // slide back out of the consequence the first slide invited: by the time the second
            // is available, the taya who spent a lunge has theirs back.
            Assert.AreEqual(punish + Balance.LungeCooldown, Balance.SlideCooldown, 0.001f);

            Assert.AreEqual(Balance.ShoveStaminaCost, Balance.SlideStaminaCost,
                "It is priced against the attacker's other committed verb, out of a bar the " +
                "player already watches. docs/VISION.md § 1.1 forbids Classic another one.");

            Assert.AreEqual(Balance.LungeMinPower, Balance.SlideSteerScale,
                "The game already had an answer to 'how much of a committed move is still " +
                "yours'. A second constant here would be one that can disagree with it.");
        }

        /// <summary>
        /// ⚠️ IT ADDS NO VERB, WHICH IS WHY IT NEEDS NO PAD ANSWER AND NO THUMB TARGET.
        /// `CLAUDE.md` § 4a: a new `Verb` does not compile until it has both. This reuses
        /// `Verb.Lunge`, which had a key, a trigger and a touch button and did NOTHING at all on
        /// the three attackers in every round.
        /// </summary>
        [Test]
        public void TheSlideAddsNoNewVerbAndReusesADeadControl()
        {
            var verbs = System.Enum.GetNames(typeof(Verb));
            CollectionAssert.DoesNotContain(verbs, "Slide",
                "A new verb would need a pad binding and a thumb target and an InputAssetSync " +
                "regeneration. The point of this feature is that it needs none of them.");

            var lunge = InputLayer.InputCatalogue.For(Verb.Lunge);
            Assert.IsFalse(string.IsNullOrEmpty(lunge.GamepadPath),
                "Lunge already answers the pad, so an attacker can reach the slide on one.");
            Assert.IsFalse(string.IsNullOrEmpty(lunge.TouchLabel),
                "And a thumb, which is the button that used to do nothing for an attacker.");
        }
    }
}
