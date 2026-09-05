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

        // -------------------------------------------------------------------
        // § 145.9  AN UNTRACKED SOURCE FILE IS NOT A CLEAN TREE
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE GATE DROPPED EVERY `??` ROW AND IN A UNITY PROJECT THAT IS UNSAFE. An
        /// untracked `.cs` compiles, an untracked `.shader`, `.prefab`, `.unity` or `Resources/`
        /// asset ships, and `ProjectSettings/` decides the build target and the UGS project.
        /// The report said **SHA X / tree clean** over the top of all three.
        /// </summary>
        [Test]
        public void UntrackedSourceMakesTheTreeDirtyAndUntrackedOutputDoesNot()
        {
            string[] source =
            {
                "Assets/TumbangPreso/Runtime/SecretVerb.cs",
                "Assets/TumbangPreso/Runtime/SecretVerb.cs.meta",
                "Assets/TumbangPreso/Art/shaders/Toon2.shader",
                "Assets/TumbangPreso/Resources/UI/new_art.png",
                "Assets/Scenes/Eskinita2.unity",
                "Assets/StreamingAssets/rules.json",
                "Packages/com.tumbangpreso.core/Runtime/NewRule.cs",
                "Core/Extra.cs",
                "Core.Tests/ExtraTests.cs",
                "ProjectSettings/ProjectSettings.asset",
                "tools/qualify.py",
                "ugs/cloud-code/match-record.js",
            };

            foreach (string path in source)
            {
                Assert.IsTrue(WorkingTreeRules.IsSourceSensitive(path),
                    $"{path} is untracked source. It compiles or ships, so a report calling the " +
                    $"tree clean is certifying a commit that does not contain it.");
            }

            string[] output =
            {
                "Logs/play.xml",
                "Logs/ui/character-select.png",
                "Library/ArtifactDB",
                "Temp/UnityLockfile",
                "Builds/macOS/TumbangPreso.app",
                "build/apk/tumbangpreso.apk",
                "obj/Debug/x.dll",
                "scratchpad/patch_thing.py",
                "docs/reports/qualification-abc123456789.md",
            };

            foreach (string path in output)
            {
                Assert.IsFalse(WorkingTreeRules.IsSourceSensitive(path),
                    $"{path} cannot reach a build, and a gate that failed on it is a gate every " +
                    $"developer learns to pass with a flag.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ DEFAULT-DENY IS THE WHOLE DESIGN AND THIS IS THE TEST THAT SAYS SO. The brittle
        /// shape is an allowlist of source directories: somebody adds one next year, nobody edits
        /// the list, and the gate goes quiet in the direction nobody checks. A directory nobody
        /// has thought about has to be DIRTY.
        /// </summary>
        [Test]
        public void ADirectoryNobodyHasThoughtAboutIsSourceRatherThanForgiven()
        {
            Assert.IsTrue(WorkingTreeRules.IsSourceSensitive("Assets/BrandNewFolder/Thing.cs"));
            Assert.IsTrue(WorkingTreeRules.IsSourceSensitive("SomeFolderInventedIn2027/thing.txt"));
            Assert.IsTrue(WorkingTreeRules.IsSourceSensitive("shaders/"),
                "Git collapses an untracked DIRECTORY into one row, so refusing to judge one "
                + "would be a hole the size of a folder.");

            Assert.IsFalse(WorkingTreeRules.IsSourceSensitive(""));
            Assert.IsFalse(WorkingTreeRules.IsSourceSensitive("   "));
        }

        /// <summary>
        /// ⚠️⚠️ GIT QUOTES A PATH WITH A SPACE IN IT AND `git ls-files --others` IS WHERE THOSE
        /// ARRIVE FROM. The surrounding quotes are stripped; the C-escapes inside one are not
        /// decoded, and that is safe **because the rule is default-deny**: a path the parser
        /// mangles is still classified as source and still makes the tree dirty. Every parsing
        /// mistake fails towards refusing to certify.
        /// </summary>
        [Test]
        public void AQuotedPathIsClassifiedByWhatIsInsideTheQuotes()
        {
            Assert.IsTrue(WorkingTreeRules.IsSourceSensitive("\"Assets/Space Name.cs\""));
            Assert.IsFalse(WorkingTreeRules.IsSourceSensitive("\"Logs/a b.log\""));
            Assert.IsTrue(WorkingTreeRules.IsSourceSensitive("Assets\\Windows\\Path.cs"),
                "A backslash separator is the same path. Both callers run on Windows as often as "
                + "not.");
        }

        /// <summary>
        /// ⚠️⚠️ BOTH SIDES OF THIS GATE HAVE TO MEAN THE SAME THING, AND ONE OF THEM IS PYTHON.
        /// `IntegrityRules.Digest` set the precedent for what a rule written twice costs, and
        /// `tools/check_digest_contract.js` is the answer that was built for it. This asserts the
        /// Unity half against the file `tools/qualify.py` reads its own list out of, so a root
        /// added on one side and not the other fails here rather than going quiet in a release.
        /// </summary>
        [Test]
        public void ThePythonQualificationSharesTheOneWorkingTreeRule()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string qualify = Path.Combine(root, "tools", "qualify.py");
            Assert.IsTrue(File.Exists(qualify), "tools/qualify.py is the gate. It has to exist.");

            string text = File.ReadAllText(qualify);
            var block = Regex.Match(text,
                @"NON_SOURCE_UNTRACKED_ROOTS\s*=\s*\((?<body>.*?)\)", RegexOptions.Singleline);
            Assert.IsTrue(block.Success,
                "tools/qualify.py must carry NON_SOURCE_UNTRACKED_ROOTS. If it was renamed, this "
                + "test is the thing that has to be updated with it.");

            // ⚠️⚠️ THE `#` COMMENTS ARE STRIPPED FIRST AND THE FIRST RUN OF THIS TEST PROVED WHY.
            // Every root in that tuple carries a written reason beside it, and one of those
            // reasons QUOTES `.gitignore`: *"they are worthless the moment they have run"*. A
            // naive sweep for quoted strings reads that sentence as a seventeenth root and the
            // test fails on its own parser rather than on any drift. `tools/audit_harness_
            // contracts.py` strips `//` from the C# side for the same reason, in the same shape.
            string body = Regex.Replace(block.Groups["body"].Value, "#[^\n]*", "");

            var python = new List<string>();
            foreach (Match m in Regex.Matches(body, "\"(?<root>[^\"]+)\""))
                python.Add(m.Groups["root"].Value);

            CollectionAssert.AreEquivalent(WorkingTreeRules.NonSourceUntrackedRoots, python,
                "The C# and the Python copies of the non-source roots have drifted. A build "
                + "stamp saying `clean` and a qualification saying `dirty` about one tree is "
                + "worse than either being wrong on its own, because each looks authoritative.");
        }

        // -------------------------------------------------------------------
        // § 145.10  A DIRTY OR UNKNOWN ARTIFACT IS NOT A CANDIDATE
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE EXACT SCENARIO: HEAD X, EDIT A TRACKED `.cs`, BUILD, REVERT, QUALIFY. The
        /// artifact's SHA is X and its contents are not, the working tree is clean, and every
        /// SHA comparison in the gate passes. `treeState` is the only field that can refuse it.
        /// </summary>
        [Test]
        public void ADirtyArtifactIsNotACandidateEvenAtTheRightSha()
        {
            var record = new BuildIdentity.Record
            {
                sha = "837eb0a321b08fea03c75916a312e6001a11413a",
                protocol = Net.NetSession.ProtocolVersion,
                treeState = "dirty",
                dirty = true,
            };

            Assert.AreEqual(BuildIdentity.TreeState.Dirty, BuildIdentity.StateOf(record));
            Assert.AreNotEqual(BuildIdentity.TreeState.Clean, BuildIdentity.StateOf(record),
                "Reverting the working tree afterwards does not change what is inside the "
                + "artifact. The SHA it names is not what ran.");
        }

        /// <summary>⚠️ EMPTY IS `unknown`, WHICH IS THE STATE EVERY PRE-2026-09-05 STAMP IS IN.</summary>
        [Test]
        public void AStampWithNoTreeStateIsUnknownAndTheOneLineSaysSo()
        {
            var legacy = new BuildIdentity.Record { sha = "abcdef1234567890", treeState = "" };
            Assert.AreEqual(BuildIdentity.TreeState.Unknown, BuildIdentity.StateOf(legacy));

            var nonsense = new BuildIdentity.Record { treeState = "probably fine" };
            Assert.AreEqual(BuildIdentity.TreeState.Unknown, BuildIdentity.StateOf(nonsense),
                "A value this build does not recognise is unknown, not clean. Fail closed.");

            Assert.AreEqual(BuildIdentity.TreeState.Clean,
                BuildIdentity.StateOf(new BuildIdentity.Record { treeState = "CLEAN" }),
                "The reader is case insensitive; the writer is `ToString().ToLowerInvariant()`.");
        }

        /// <summary>
        /// ⚠️⚠️ TWO ARTIFACTS ON ONE COMMIT AND ONE PROTOCOL CAN STILL NOT PLAY TOGETHER, AND
        /// THIS IS THE FIELD THAT DECIDES IT. `CLAUDE.md` § 4a: a machine on a different UGS
        /// project resolves a join code in a different namespace, **so the room is simply not
        /// there and it reads as an EMPTY LOBBY rather than as an error.** Nothing refuses, so
        /// nothing logs, so nobody can debug it at a venue.
        /// </summary>
        [Test]
        public void TheUgsIdentityIsPartOfWhetherTwoArtifactsAreOneRelease()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string qualify = File.ReadAllText(Path.Combine(root, "tools", "qualify.py"));

            foreach (string field in new[] { "sha", "protocol", "ugsProject",
                                             "ugsEnvironment", "appVersion" })
            {
                StringAssert.Contains($"(\"{field}\"", qualify,
                    $"{field} must be one of the fields two candidate artifacts are compared on. "
                    + $"Dropping one is how two builds that cannot see each other pass a gate.");
            }

            // ⚠️ AND THE BUILD ACTUALLY STAMPS THEM, which is the half a Python-side list cannot
            // assert. A comparison of a field nothing writes compares two blanks and passes.
            var stamped = new BuildIdentity.Record();
            Assert.IsNotNull(stamped.ugsProject);
            Assert.IsNotNull(stamped.ugsEnvironment);
            Assert.IsNotNull(stamped.appVersion);
            Assert.IsNotNull(stamped.target);
        }

        // -------------------------------------------------------------------
        // § 145.12  THE SLIDE'S REFUSAL COULD NEVER BE ROLLED BACK
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ `OnVerbDeniedMsg` BOUNDED THE VERB BYTE AT `DeniedVerb.Shove`, WHICH IS 2, AND
        /// `Slide` IS 3. Every slide refusal the host sent was discarded by the client one
        /// function before `RollBackRefusedVerb`, so the whole `case DeniedVerb.Slide` arm, which
        /// returns a 2.45 s cooldown, 25 stamina and a commitment that narrows steering to 0.35,
        /// was unreachable code from the day it was written.
        /// </summary>
        [Test]
        public void EveryDeniedVerbValueCanTravelAndBeRolledBack()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string rpc = File.ReadAllText(Path.Combine(
                root, "Assets", "TumbangPreso", "Runtime", "Net", "MatchRpc.cs"));

            StringAssert.DoesNotContain("verb > (byte)DeniedVerb.Shove", rpc,
                "A bound named after the last verb somebody remembered is a bound that drops the "
                + "next one. It is the enum's own length now.");

            string combat = File.ReadAllText(Path.Combine(
                root, "Assets", "TumbangPreso", "Runtime", "CombatVerbs.cs"));

            foreach (string name in System.Enum.GetNames(typeof(Net.MatchRpc.DeniedVerb)))
            {
                StringAssert.Contains($"case Net.MatchRpc.DeniedVerb.{name}:", combat,
                    $"{name} can be refused, so it needs an arm in RollBackRefusedVerb. A verb "
                    + $"the host can refuse and the client cannot take back leaves a player "
                    + $"paying for something that never happened.");
            }
        }

        // -------------------------------------------------------------------
        // § 145.4b  WHO EVER SAT IN A SEAT IS THE PERSISTENT FACT
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ `IsBot` IS NOT CONSTANT FOR A MATCH AND THE STRUCTURAL HASH WAS TREATING IT AS
        /// IF IT WERE. The measurement is `docs/TODO.md` § 145.4b's second run: a seatless
        /// referee and two idle clients, no `-tp-allbots`. The clients agreed with each other
        /// exactly and the referee called every chair a bot, because it outlives them on purpose
        /// and by the time it sampled both players had quit and their chairs had been handed
        /// over. Three findings, and the game was right in all three.
        /// </summary>
        [Test]
        public void WhetherAPersonEverSatInASeatSurvivesThemLeaving()
        {
            Assert.IsTrue(SeatHandover.APersonSatHere(SeatOrigin.Human));
            Assert.IsTrue(SeatHandover.APersonSatHere(SeatOrigin.HandedToBot),
                "A chair somebody sat in and left is still a chair somebody sat in. That is the "
                + "whole reason this can be compared between peers that stopped at different "
                + "moments and `IsBot` cannot.");
            Assert.IsFalse(SeatHandover.APersonSatHere(SeatOrigin.Bot));

            Assert.IsFalse(SeatHandover.RatingMovesFor(SeatOrigin.HandedToBot),
                "And it is a different question from whether the ladder moves, which is still "
                + "Human only. Collapsing the two would pay a player for a bot's stretch.");
        }

        /// <summary>
        /// ⚠️⚠️ A HOST THAT OPENS THE ARENA BEFORE ITS PEERS ARRIVE RECORDED EVERY PLAYER'S CHAIR
        /// AS A BOT'S FOR THE WHOLE MATCH. That is every `-tp-dedicated` referee and every
        /// `-tp-autostart` host: `MatchInstaller.BuildSeat` asks the lobby who is sitting where
        /// and the answer at boot is "nobody", and `NoteSeatOrigin(Human)` is a deliberate no-op.
        /// `SeatHandover.RatingMovesFor` reads that field, so a referee-hosted bracket match
        /// would have submitted four bot seats and moved nothing.
        /// </summary>
        [Test]
        public void ASeatInstalledAsABotBecomesHumanWhenARealPeerClaimsIt()
        {
            var go = new GameObject("SeatOriginProbe", typeof(CharacterController));
            try
            {
                var motor = go.AddComponent<CharacterMotor>();

                motor.NoteSeatOrigin(SeatOrigin.Bot);
                Assert.AreEqual(SeatOrigin.Bot, motor.SeatOrigin);

                motor.NoteSeatClaimedByAPerson(midMatch: false);
                Assert.AreEqual(SeatOrigin.Human, motor.SeatOrigin,
                    "Before the whistle a chair changing hands is the roster settling.");

                motor.NoteSeatOrigin(SeatOrigin.HandedToBot);
                Assert.AreEqual(SeatOrigin.HandedToBot, motor.SeatOrigin);

                motor.NoteSeatClaimedByAPerson(midMatch: false);
                Assert.AreEqual(SeatOrigin.HandedToBot, motor.SeatOrigin,
                    "⚠️ A HANDOVER IS NEVER WALKED BACK. The bot's stretch happened, so a "
                    + "reconnecting player may not have the whole result credited to them.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// ⚠️ AFTER THE WHISTLE, A BOT HAS ALREADY PLAYED PART OF THE MATCH IN THAT CHAIR, so a
        /// person arriving into it is `HandedToBot` and not `Human`. Calling it Human would pay
        /// somebody in full for a match they joined half way through.
        /// </summary>
        [Test]
        public void APersonTakingABotsChairMidMatchIsAHandover()
        {
            var go = new GameObject("SeatOriginMidMatchProbe", typeof(CharacterController));
            try
            {
                var motor = go.AddComponent<CharacterMotor>();
                motor.NoteSeatOrigin(SeatOrigin.Bot);

                motor.NoteSeatClaimedByAPerson(midMatch: true);
                Assert.AreEqual(SeatOrigin.HandedToBot, motor.SeatOrigin);
                Assert.IsTrue(SeatHandover.APersonSatHere(motor.SeatOrigin));
                Assert.IsFalse(SeatHandover.RatingMovesFor(motor.SeatOrigin));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // -------------------------------------------------------------------
        // § 145.8  THE TOURNAMENT COLD START
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE COLD START HAS TO BE ABLE TO ASSERT THE PRESET AND NOT MERELY THAT A MATCH
        /// HAPPENED. The recorded green run at `87346b8` is a macOS player playing HERO STRIKE,
        /// and `docs/VISION.md` § 1.1 says CLASSIC is the tournament ruleset. A harness cannot
        /// assert what it cannot read, so the report has to carry both sentences.
        /// </summary>
        [Test]
        public void TheStateReportCarriesTheTournamentPresetAndTheModifiersLeftSet()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string report = File.ReadAllText(Path.Combine(
                root, "Assets", "TumbangPreso", "Runtime", "Diagnostics", "NetStateReport.cs"));

            StringAssert.Contains("tournament ruleset", report);
            StringAssert.Contains("tournament modifiers", report);
            StringAssert.Contains("build identity", report);

            // ⚠️ AND THE HARNESS PARSES EXACTLY THOSE THREE. A field printed under a name nothing
            // reads is `docs/TODO.md` § 143.15 with a longer report.
            string cold = File.ReadAllText(Path.Combine(root, "tools", "cold_start.py"));
            StringAssert.Contains("tournament ruleset", cold);
            StringAssert.Contains("tournament modifiers", cold);
            StringAssert.Contains(Net.NetBootstrap.TournamentSwitch, cold);
        }

        // -------------------------------------------------------------------
        // § 149.2  ADMISSION HAPPENS ONCE, UNDER THE TOKEN THE HOST APPROVED
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE REPRODUCTION, DRIVEN THROUGH `LobbySession` ITSELF. This is what
        /// `MatchRpc.HandleIdentify` used to hand a CLIENT-CHOSEN token to: `Admit` treats a
        /// matching token as a fast reconnect, so a second live peer arriving with the first
        /// one's token **takes its chair and removes it from the lobby**, with no disconnect, so
        /// the victim's socket keeps submitting movement for a seat that is no longer theirs.
        ///
        /// ⚠️ THE BEHAVIOUR ITSELF IS CORRECT AND MUST NOT CHANGE. It is the fast-reconnect path
        /// and `NetSession.ApproveConnection` disconnects the stale transport straight after it.
        /// What was wrong is that `Identify` could reach it with a token the host never approved.
        /// `docs/TODO.md` § 149.2.
        /// </summary>
        [Test]
        public void AdmittingASecondPeerUnderTheSameTokenTakesTheFirstOnesChair()
        {
            var lobby = new Net.LobbySession { IsDedicated = false };

            var first = lobby.Admit(1, "durable-token-of-the-victim", "VICTIM");
            Assert.GreaterOrEqual(first.Seat, 0, "the fixture needs the first peer seated.");
            int stolen = first.Seat;

            var second = lobby.Admit(2, "durable-token-of-the-victim", "ATTACKER",
                                     out int replacedPeerId);

            Assert.AreEqual(stolen, second.Seat,
                "This IS the fast-reconnect rule and it is right: the same durable token means "
                + "the same player, so the chair moves to the new transport.");
            Assert.AreEqual(1, replacedPeerId,
                "And it reports the transport it replaced, which is what "
                + "NetSession.ApproveConnection disconnects. A caller that discards this leaves "
                + "two sockets driving one seat.");
            Assert.IsNull(lobby.PeerById(1),
                "The victim's record is GONE. That is why the token reaching this method may "
                + "never be one a client typed.");
        }

        /// <summary>
        /// ⚠️⚠️ AND A TOKEN NOBODY HOLDS RE-SEATS THE SENDER, which needs no stolen secret at
        /// all: no match in `_peers`, so `RuleOnArrival` runs afresh and hands out whatever is
        /// free. With a seat free the peer MOVES; with none it turns itself into a spectator
        /// mid-match.
        /// </summary>
        [Test]
        public void AdmittingTheSamePeerUnderAFreshTokenMovesItToADifferentChair()
        {
            var lobby = new Net.LobbySession { IsDedicated = false };

            var one = lobby.Admit(1, "token-one", "P1");
            var two = lobby.Admit(2, "token-two", "P2");
            Assert.AreNotEqual(one.Seat, two.Seat, "the fixture needs two distinct seats.");

            var moved = lobby.Admit(2, "a-token-nobody-holds", "P2");

            Assert.AreNotEqual(two.Seat, moved.Seat,
                "Re-admitting one peer under a new token handed it a different chair. That is "
                + "reachable with nothing but the message a client already sends, which is why "
                + "MatchRpc.HandleIdentify pins the token the host approved.");
        }

        /// <summary>
        /// ⚠️⚠️ THE FIX, ASSERTED AT THE ONE PLACE A CLIENT-SUPPLIED TOKEN COULD REACH `Admit`.
        /// This is a source assertion and it is the honest tool here: the behaviour needs a live
        /// `NetworkManager`, an approved connection and a second transport, which is two built
        /// players and a link (`tools/net_matrix.py`), and the property being protected is a
        /// single substitution in one method.
        /// </summary>
        [Test]
        public void TheIdentifyHandlerUsesTheApprovedTokenAndAdmitsOncePerSession()
        {
            string rpc = File.ReadAllText(Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "Assets", "TumbangPreso", "Runtime", "Net", "MatchRpc.cs"));

            StringAssert.Contains("ApprovedTokenFor(senderClientId)", rpc,
                "HandleIdentify must replace the message's token with the one this host approved "
                + "before it reaches LobbySession.Admit.");

            StringAssert.Contains("_identified.Add(peerId)", rpc,
                "Admission is once per transport session; a repeat is a retry.");

            StringAssert.Contains("if (!firstIdentify) return;", rpc,
                "And the retry must stop before the room-wide broadcasts. A client can send "
                + "Identify as fast as it likes, and the arrival fan-out is three ClientRpcs, a "
                + "ready tally, the lobby picks, the seat picks and a WORLD SNAPSHOT.");

            StringAssert.Contains("_identified.Remove(peerId)", rpc,
                "A transport that left and came back is a new session and gets the full arrival "
                + "again, or it stands in an arena nobody told it about.");
        }

        // -------------------------------------------------------------------
        // § 149.3  A ONE-SHOT PRESS IS NOT CONSUMED BEFORE IT CAN BE DELIVERED
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ `IsListening` IS TRUE FROM `StartClient` AND NOT FROM APPROVAL, so a request sent
        /// in the join window goes to a transport with no route, `SendNamedMessage` reports
        /// nothing, and a method that answered TRUE let its caller throw the press away.
        /// `RequestSkipBufferServerRpc` said the right thing in its summary and did the wrong
        /// thing on the next line. `docs/TODO.md` § 149.3.
        ///
        /// ⚠️ THE SWEEP IS OVER THE WHOLE CLASS, not just the one that was wrong: every request
        /// that RETURNS whether it was delivered is one where a silently consumed press is
        /// possible, and there are five of them.
        /// </summary>
        [Test]
        public void EveryRequestThatReportsDeliveryWaitsForARealConnection()
        {
            string rpc = File.ReadAllText(Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "Assets", "TumbangPreso", "Runtime", "Net", "MatchRpc.cs"));

            foreach (Match m in Regex.Matches(
                         rpc, @"public bool (?<name>\w+ServerRpc)\([^)]*\)\s*\{(?<body>.*?)\n        \}",
                         RegexOptions.Singleline))
            {
                string name = m.Groups["name"].Value;
                string body = m.Groups["body"].Value;

                StringAssert.DoesNotContain("!_nm.IsListening", body,
                    $"{name} reports whether the press reached the wire and gates on " +
                    $"IsListening, which is true before approval finishes. The press is thrown " +
                    $"away by a caller that believes it was sent.");

                StringAssert.Contains("_nm.IsConnectedClient", body,
                    $"{name} returns a delivery answer, so it has to wait for a connection that " +
                    $"can carry one.");
            }
        }

        // -------------------------------------------------------------------
        // § 141  THE SCOREBOARD CANNOT DRAW ONE STRING TWICE
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ 🧑 PHOTOGRAPHED ONE PERSON ON TWO ROWS, `docs/TODO.md` § 141, AND THE RULE THAT
        /// FIXES IT IS ASSERTED IN `Core.Tests/BoardNamesTests.cs` RATHER THAN HERE.
        ///
        /// The behavioural version of this test was written in EditMode first and **could not
        /// run**: it needs a live `RoundDirector`, `GameServices.Ensure()` is how you get one,
        /// and that calls `DontDestroyOnLoad`, which throws outright in an editor script
        /// (*"can only be used in play mode"*). Pushing the collision rule into
        /// `Core.BoardNames` turned a test that could not run into six that cost a millisecond,
        /// which is `CLAUDE.md` § 4's argument arriving from the other direction.
        ///
        /// ⚠️ WHAT IS LEFT HERE IS THE HALF THE CORE CANNOT SEE: that both boards actually ASK
        /// the rule. A perfect rule nothing calls is § 96's fault with better arithmetic.
        ///
        /// ⚠️ ONE RULE, TWO CALLERS. `docs/TODO.md` § 94.1 records four hand-written copies of
        /// "which line in a record is mine", all agreeing on the wrong value, as the reason
        /// nothing on the machine could see the fault. These two were the same shape.
        /// </summary>
        [Test]
        public void BothBoardsAskTheOneNamingRule()
        {
            string root = Path.GetDirectoryName(Application.dataPath);

            foreach (string file in new[] { "Hud.cs", "MatchResult.cs" })
            {
                string text = File.ReadAllText(Path.Combine(
                    root, "Assets", "TumbangPreso", "Runtime", "UI", file));

                StringAssert.Contains("SeatLabel.", text,
                    $"{file} draws a board and has to name its seats through SeatLabel, or the "
                    + "two boards can disagree about who is who.");

                StringAssert.DoesNotContain("who.DisplayName() : $\"P{slot + 1}\"", text,
                    $"{file} still carries its own copy of the naming helper.");
            }
        }

        // -------------------------------------------------------------------
        // § 149.8  WHAT A MATCH MAY NOT LEAVE BEHIND
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE LAUNCH BLOCK OUTLIVED A MATCH THAT WAS LEFT RATHER THAN FINISHED, AND ONE
        /// FIELD IN IT HAS TEETH. `GameLaunch.Reset()` was reached only by `SceneFlow
        /// .StartTraining`, so `SceneFlow.LeaveMatchToMainMenu` — which the pause panel, the
        /// results board and both result screens all come through, and whose own header calls it
        /// *"the single exit"* — cleared none of it.
        ///
        /// **`GameLaunch.Spectator` is the one that costs a match.** `MatchInstaller.HumanSeat`
        /// answers -1 while it is set, so a player who spectated one match and then started a
        /// solo one got an arena where **nobody was driving their seat**. `ConvertedMatchSetup`
        /// clears it on the way into the lobby, which covers the lobby route and not the ones
        /// that skip it. `docs/TODO.md` § 149.8.
        /// </summary>
        [Test]
        public void TheLaunchBlockDoesNotSurviveAMatch()
        {
            string flow = File.ReadAllText(Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "Assets", "TumbangPreso", "Runtime", "UI", "SceneFlow.cs"));

            var exit = Regex.Match(flow,
                @"public static void LeaveMatchToMainMenu\(\)\s*\{(?<body>.*?)\n        \}",
                RegexOptions.Singleline);

            Assert.IsTrue(exit.Success,
                "LeaveMatchToMainMenu is the single exit from a match. If it was renamed, this "
                + "test is what has to move with it.");

            StringAssert.Contains("GameLaunch.Reset();", exit.Groups["body"].Value,
                "The single exit from a match has to clear the launch block, or a spectator flag "
                + "set for one match reaches the next one and MatchInstaller.HumanSeat answers "
                + "-1 for a player who is trying to play.");
        }

        /// <summary>
        /// ⚠️ AND `Reset()` HAS TO ACTUALLY CLEAR THE THREE THAT DECIDE WHO DRIVES. This is the
        /// behaviour behind the source assertion above: a test that only checked the call site
        /// would pass if somebody emptied the method.
        /// </summary>
        [Test]
        public void ResettingTheLaunchBlockClearsWhoDrivesAndWhatIsBeingTaught()
        {
            bool spectator = GameLaunch.Spectator;
            bool tutorial = GameLaunch.GuidedTutorial;
            string action = GameLaunch.PendingAction;
            bool sandbox = PracticeSandbox.Wanted;
            bool allBots = GameLaunch.AllBots;

            try
            {
                GameLaunch.Spectator = true;
                GameLaunch.GuidedTutorial = true;
                GameLaunch.PendingAction = "join";
                PracticeSandbox.Wanted = true;
                GameLaunch.AllBots = true;

                GameLaunch.Reset();

                Assert.IsFalse(GameLaunch.Spectator,
                    "A spectator flag reaching the next match is a player with no body.");
                Assert.IsFalse(GameLaunch.GuidedTutorial,
                    "And a tutorial flag reaching it is the guided route installed over a real "
                    + "match.");
                Assert.AreEqual("", GameLaunch.PendingAction,
                    "A pending action is consumed by the arena that read it; carrying it forward "
                    + "makes the next launch try to join something.");
                Assert.IsFalse(PracticeSandbox.Wanted,
                    "The sandbox BUTTON must not read ON in a room it can never apply to, which "
                    + "is a HUD disagreeing with the game.");

                // ⚠️⚠️ AND THIS ONE IS DELIBERATELY LEFT ALONE. `-tp-allbots` belongs to the
                // PROCESS, not to a match: a harness that asked for a driven session expects the
                // second match to be driven too, and clearing it here would make every
                // multi-match probe measure three parked bodies. `TournamentGuard` is what
                // clears it for a bracket match, which is the one place it must not be set.
                Assert.IsTrue(GameLaunch.AllBots,
                    "GameLaunch.AllBots is a launch switch and survives a match on purpose. If "
                    + "this ever changes, tools/net_matrix.py and tools/referee_run.py stop "
                    + "measuring anything after their first match.");
            }
            finally
            {
                GameLaunch.Spectator = spectator;
                GameLaunch.GuidedTutorial = tutorial;
                GameLaunch.PendingAction = action;
                PracticeSandbox.Wanted = sandbox;
                GameLaunch.AllBots = allBots;
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE AUTHORITY LATCH MUST NOT SURVIVE INTO A SECOND MATCH EITHER, and the thing
        /// that clears it is not obvious from any call site. `MatchAbandon.Forget()` lives inside
        /// `NetSession.ConfigureTimeouts`, whose own note says *"every start path and `Stop`
        /// reach this method, which is what makes it the one place a new session can forget the
        /// last one's ending"*. This asserts that placement, because a latch that survived would
        /// be a peer that cannot resolve its own next match, and since § 149.6 it would also be
        /// a peer whose practice sandbox never comes back.
        /// </summary>
        [Test]
        public void ForgettingTheAbandonLatchIsOnTheOneMethodEveryStartAndStopReaches()
        {
            string session = File.ReadAllText(Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "Assets", "TumbangPreso", "Runtime", "Net", "NetSession.cs"));

            var configure = Regex.Match(session,
                @"private void ConfigureTimeouts\(\)\s*\{(?<body>.*?)\n        \}",
                RegexOptions.Singleline);

            Assert.IsTrue(configure.Success, "NetSession.ConfigureTimeouts has moved or gone.");
            StringAssert.Contains("MatchAbandon.Forget();", configure.Groups["body"].Value,
                "A revoked authority latch reaching a second match is a peer that cannot resolve "
                + "anything in it, with nothing in the log to say why.");
        }

        // -------------------------------------------------------------------
        // § 149.6  THE PRACTICE SANDBOX THROUGH A TEARDOWN
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE COMMENT CLAIMED THIS AND THE CODE DID NOT DO IT. `PracticeSandbox`'s header
        /// said the guard is false *"including the frame a session is being torn down, because
        /// the provider is what answers"*, and the provider answers
        /// `_nm != null && _nm.IsListening`, which goes FALSE the moment `Shutdown()` runs. So
        /// through a teardown the predicate said "offline" while the arena was still on screen.
        ///
        /// ⚠️ THE SECOND CLAUSE IS THE EXISTING LATCH RATHER THAN A SECOND COPY OF AUTHORITY
        /// STATE. `MatchAbandon.AuthorityRevoked` was built for exactly this window (§ 143.9).
        /// </summary>
        [Test]
        public void ThePracticeSandboxIsDeniedThroughAHostLossTeardown()
        {
            bool wanted = PracticeSandbox.Wanted;
            try
            {
                MatchAbandon.Forget();
                PracticeSandbox.Wanted = true;

                Assert.IsTrue(PracticeSandbox.Allowed,
                    "Offline with nothing revoked is the case the sandbox exists for.");
                Assert.IsTrue(PracticeSandbox.Active);

                MatchAbandon.Note("[Disconnect Event] transport shutdown", wasLocal: false);

                Assert.IsFalse(PracticeSandbox.Allowed,
                    "A peer whose host vanished still has an arena, bodies and ability systems "
                    + "on screen. `IsNetworked` alone answers 'offline' there, which is the "
                    + "false claim this replaces.");
                Assert.IsFalse(PracticeSandbox.Active);

                MatchAbandon.Forget();
                Assert.IsTrue(PracticeSandbox.Allowed,
                    "And it comes back when the next match begins, or the fix has taken the solo "
                    + "sandbox away permanently.");
            }
            finally
            {
                PracticeSandbox.Wanted = wanted;
                MatchAbandon.Forget();
            }
        }

        /// <summary>
        /// ⚠️ A LOCAL QUIT DELIBERATELY DOES NOT REVOKE AND MUST NOT DENY THE SANDBOX. A player
        /// who pressed QUIT really is offline afterwards, and refusing them their own practice
        /// bench would be the fix causing the bug.
        /// </summary>
        [Test]
        public void LeavingOnPurposeDoesNotTakeTheSandboxAway()
        {
            bool wanted = PracticeSandbox.Wanted;
            try
            {
                MatchAbandon.Forget();
                PracticeSandbox.Wanted = true;

                MatchAbandon.Note("shutting down", wasLocal: true);

                Assert.IsTrue(PracticeSandbox.Allowed);
                Assert.IsTrue(PracticeSandbox.Active);
            }
            finally
            {
                PracticeSandbox.Wanted = wanted;
                MatchAbandon.Forget();
            }
        }

        /// <summary>
        /// ⚠️ THE SWITCH THAT APPLIES THE PRESET IS ITSELF ON THE ROSTER, because
        /// `audit_tournament_defaults.py` refuses a `-tp-` switch nobody has said either sentence
        /// about, and "it only makes the machine more tournament-legal" is a sentence.
        /// </summary>
        [Test]
        public void TheTournamentLaunchSwitchIsAccountedForLikeEveryOtherOne()
        {
            Assert.IsTrue(TournamentPreset.IsAccountedFor(Net.NetBootstrap.TournamentSwitch),
                "A -tp- switch on neither roster is the blind spot § 145.3 closed.");

            Assert.AreEqual(GameMode.Classic, TournamentPreset.Mode,
                "docs/VISION.md § 1.1. If this ever moves it is a tournament ruling and this "
                + "assertion is what makes it a deliberate one.");
        }
    }
}
