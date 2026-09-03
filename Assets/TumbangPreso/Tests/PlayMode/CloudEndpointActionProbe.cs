using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// One case per ACTION on all three Cloud Code endpoints, each asserting a string only that
    /// branch can produce, plus the one check `FUTURE.md` § 4.5.1 calls the most valuable in the
    /// phase: that a real submission moves the profile's XP by exactly what
    /// `ProgressionRules.MatchXp` says it should.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE `docs/TODO.md` § 90.5 HAPPENED. Cloud Code strips any parameter a
    /// script does not declare in `module.exports.params`, silently, so for two phases every call
    /// arrived with `action` undefined, fell through to the branch `params.action || "default"`
    /// picks, and answered normally. `player-account`'s save and delete and `match-record`'s
    /// submit had **never once run**, and every probe was green throughout, because every probe
    /// used `load` and `load` IS the default branch. **"It answered" is not a test of an
    /// endpoint. Only a string the wrong branch cannot produce is.**
    ///
    /// ⚠️⚠️ THE PLAN FILE SAID NINE ACTIONS AND THERE ARE TEN. `FUTURE.md` § 4.5.2 lists
    /// `player-account`: load, save, delete, verify. It has a fifth, `attest`, which is the half
    /// of the impersonation guard that MINTS a proof, and it was the only branch of any script
    /// with no coverage at all in either direction. § 4.5.2 is corrected in the same commit as
    /// this file, per `FUTURE.md` § 0.5 rule 2.
    ///
    /// ⚠️⚠️ EVERY CASE RUNS ON A THROWAWAY UGS PROFILE, BECAUSE THREE OF THEM WRITE AND ONE OF
    /// THEM DELETES. `save` overwrites a display name, `delete` wipes a profile and its handle
    /// proof, and `submit` writes a career document and pays it XP. None of those may land on an
    /// identity anything else uses. `AuthenticationService.SwitchProfile` gives this probe a
    /// player id of its own inside the same project; `Restore` puts the session back.
    ///
    /// ⚠️⚠️ MEASURED 2026-08-30, BECAUSE THE FIRST VERSION OF THIS NOTE GOT IT WRONG AND SAID
    /// SOMETHING SCARIER THAN THE TRUTH: **the editor and the built player do NOT share a UGS
    /// session.** Authentication caches into `PlayerPrefs`, and on Windows the editor's live in
    /// `HKCU\Software\Unity\UnityEditor\<Company>\<Product>` while a player's live in
    /// `HKCU\Software\<Company>\<Product>`. The run that proved it: the editor's `default`
    /// profile signed in as `qmSg3PKwe...` while the built player on the same machine is
    /// `mNThUUFy...`.
    /// ⚠️ **WHAT THEY DO SHARE IS `Application.persistentDataPath`**, which is `settings.json` and
    /// `career.json`, so a probe that writes THOSE really does edit the player's save. That is why
    /// `MatchRecordIdentityProbe` destroys `CareerStore` for the duration, and it is a different
    /// hazard from this one.
    /// ⚠️ THE SIGN-OUT IS STILL `SignOut(false)`, NEVER `SignOut(true)`. Clearing credentials on
    /// the way past `default` discards the editor's cached anonymous session and mints it a new
    /// player id, which throws away whatever career that identity had on the project.
    ///
    /// ⚠️ `[Category("Ugs")]`, for the reason `UgsServicesProbe` gives: it is slow, it needs a
    /// network, and it spends real free-tier quota.
    ///
    ///   Unity.exe -batchmode -runTests -projectPath . -testPlatform PlayMode
    ///             -testCategory "Ugs" -testResults Logs/ugs.xml -logFile Logs/ugs.log
    /// </summary>
    [Category("Ugs")]
    public class CloudEndpointActionProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE SETUP HALF OF `docs/TODO.md` § 126.8'S FIX, AND THIS FIXTURE GETS ONLY THE
        /// SETUP HALF ON PURPOSE. `PlayModeWorld`'s header asks for both hooks; this class
        /// already owns a `[UnityTearDown]` doing its own cleanup, and NUnit does not define an
        /// order between two teardowns of the same kind. **The setup reset is the half that
        /// protects THIS fixture**: it guarantees the world is empty and settled when the test
        /// below starts, whatever ran before it. With every fixture in the folder carrying it,
        /// no test can inherit a world at all, which is the property the entry actually wants.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        private const string ProbeProfile = "qa45";

        /// <summary>The name this probe writes, so a leaked profile is identifiable at a glance.</summary>
        private const string ProbeDisplayName = "QA Four Five";

        private static string _restoreProfile;

        /// <summary>
        /// ⚠️ AWAITED BY POLLING, NEVER BY `.Wait()`. UGS posts its continuations to Unity's
        /// synchronisation context, which only advances while frames are pumped, so blocking the
        /// main thread on one deadlocks rather than timing out. `UgsServicesProbe` and `UgsCheck`
        /// both carry the same note.
        /// </summary>
        private static IEnumerator Await(Task task, float timeoutSeconds = 30.0f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!task.IsCompleted)
                throw new TimeoutException($"a UGS call did not answer inside {timeoutSeconds} s");
            if (task.IsFaulted)
                throw task.Exception?.GetBaseException() ?? new Exception("unknown UGS failure");
        }

        private static IEnumerator SignedInAsTheProbePlayer()
        {
            Assert.IsNotEmpty(Application.cloudProjectId,
                "no cloudProjectId, so there is no project to ask. ProjectSettings.asset.");

            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                yield return Await(UnityServices.InitializeAsync());

            Assert.AreEqual(ServicesInitializationState.Initialized, UnityServices.State,
                "the project id resolves but services did not come up");

            var auth = AuthenticationService.Instance;

            if (auth.Profile != ProbeProfile)
            {
                _restoreProfile ??= auth.Profile;
                if (auth.IsSignedIn) auth.SignOut(false);
                auth.SwitchProfile(ProbeProfile);
            }

            // ⚠️ THIS ONE STAYS A DIRECT CALL, AND THE REASON IS THE PROFILE SWITCH ABOVE IT.
            // `NetIdentity.EnsureSignedInAsync` signs in on the GAME's profile; this probe has
            // just switched to `ProbeProfile` precisely so its writes cannot land on a real
            // player, and routing it through the shared path would undo that in the line below.
            // ⚠️ It is still the only other sign-in in the repository, and it is safe from the
            // race that took `UgsServicesProbe` down six times because the `SignOut` above
            // guarantees no attempt is in flight on this profile.
            if (!auth.IsSignedIn) yield return Await(auth.SignInAnonymouslyAsync());

            Assert.IsTrue(auth.IsSignedIn, "anonymous sign-in failed on the probe profile");
            Assert.AreEqual(ProbeProfile, auth.Profile,
                "the probe is signed in on the wrong profile, so every write below would land " +
                "on a real player. Read this class's header before changing anything about it.");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            var auth = AuthenticationService.Instance;
            if (_restoreProfile == null || auth == null || auth.Profile != ProbeProfile) yield break;

            if (auth.IsSignedIn) auth.SignOut(false);
            auth.SwitchProfile(_restoreProfile);
            _restoreProfile = null;
        }

        private static string Flat(string s) => (s ?? "").Replace(" ", "");

        // -------------------------------------------------------------------
        // § player-account: load, save, delete, attest, verify
        // -------------------------------------------------------------------

        /// <summary>
        /// `save`, then `load`, then `delete`, then `load` again, as one case.
        ///
        /// ⚠️⚠️ THE FOUR ARE ONE TEST ON PURPOSE AND SPLITTING THEM WOULD WEAKEN ALL OF THEM.
        /// `load` and `delete` both answer `{"profile":""}` for a player who has never saved, and
        /// that is precisely the answer § 90.5's broken deployment gave to everything. The only
        /// string a WRITE branch can produce that no other branch can is the value it just wrote,
        /// read back through a different branch, so the sequence is the assertion.
        /// </summary>
        [UnityTest]
        public IEnumerator TheAccountEndpointSavesLoadsAndDeletesAProfile()
        {
            yield return SignedInAsTheProbePlayer();
            string id = AuthenticationService.Instance.PlayerId;

            // `save`. The endpoint answers `handle`, which no other branch of this script returns.
            string profileJson = JsonUtility.ToJson(new AccountProfile
            {
                PlayerId = id,
                DisplayName = ProbeDisplayName,
                Discriminator = AccountRules.DerivedTag(id),
            });

            var save = Net.CloudCode.CallAsync(
                "player-account", new { action = "save", profile = profileJson });
            yield return Await(save);

            StringAssert.Contains("\"handle\"", Flat(save.Result),
                "the save branch did not run. `handle` is the only key it returns and no other " +
                "branch of player-account.js produces it, so this is the § 90.5 shape: the " +
                "action was stripped and `load` answered instead.");
            StringAssert.Contains("QAFourFive", Flat(save.Result).Replace("\\u0020", ""),
                "the save branch answered without the name it was given");

            // `load`. The only string this can produce that the default cannot is the name above.
            var load = Net.CloudCode.CallAsync("player-account", new { action = "load", profile = "" });
            yield return Await(load);

            StringAssert.Contains("QAFourFive", Flat(load.Result).Replace("\\u0020", ""),
                "a profile was saved and the load branch did not return it");

            // `delete`. Proven by the load that follows it rather than by its own empty answer.
            var del = Net.CloudCode.CallAsync("player-account", new { action = "delete" });
            yield return Await(del);
            Assert.IsNotNull(del.Result, "player-account did not answer a delete");

            var after = Net.CloudCode.CallAsync("player-account", new { action = "load", profile = "" });
            yield return Await(after);

            StringAssert.DoesNotContain("QAFourFive", Flat(after.Result).Replace("\\u0020", ""),
                "the delete branch did not run: the saved profile is still there. This branch " +
                "had never been exercised by anything before this probe. docs/TODO.md § 90.5.");

            Debug.Log($"[CloudEndpointActionProbe] player-account save/load/delete round trip clean for {id}");
        }

        /// <summary>
        /// `attest` mints a proof for a player who HAS a saved handle, which is the half of the
        /// impersonation guard nothing had ever run.
        ///
        /// ⚠️⚠️ `UgsServicesProbe.TheAccountEndpointRefusesAHandleProofItNeverMinted` COVERS THE
        /// OTHER HALF AND ONLY THE OTHER HALF, and it says so in its own header: a probe player
        /// has never saved a profile, so `attest` has no handle to vouch for and mints nothing.
        /// That test therefore passes against an endpoint whose attest branch does not work at
        /// all. This one saves a profile first, so the mint is reachable, and then verifies the
        /// minted proof through the OTHER branch. A round trip through two branches is the only
        /// thing that proves either of them.
        ///
        /// ⚠️ IT DELETES WHAT IT WROTE, in the same test rather than in a teardown, for the
        /// reason `UgsServicesProbe.LobbyCreatesAndIsCleanedUp` gives about a leaked lobby.
        /// </summary>
        [UnityTest]
        public IEnumerator TheAccountEndpointMintsAProofItThenVouchesFor()
        {
            yield return SignedInAsTheProbePlayer();
            string id = AuthenticationService.Instance.PlayerId;

            string profileJson = JsonUtility.ToJson(new AccountProfile
            {
                PlayerId = id,
                DisplayName = ProbeDisplayName,
                Discriminator = AccountRules.DerivedTag(id),
            });

            var save = Net.CloudCode.CallAsync(
                "player-account", new { action = "save", profile = profileJson });
            yield return Await(save);

            // `attest`. `proof` and `expires` are keys no other branch of this script returns.
            var attest = Net.CloudCode.CallAsync("player-account", new { action = "attest" });
            yield return Await(attest);

            string flat = Flat(attest.Result);
            StringAssert.Contains("\"proof\":\"", flat,
                "the attest branch did not run, or minted nothing for a player that has a saved " +
                "handle. `proof` is a key only this branch returns.");
            StringAssert.Contains("\"expires\":\"", flat, "a minted proof carried no expiry");

            var minted = JsonUtility.FromJson<AttestResponse>(attest.Result);
            Assert.IsNotEmpty(minted?.proof, "attest answered with an empty proof");
            Assert.IsNotEmpty(minted?.handle, "attest answered with no handle for a saved profile");

            // `verify`, with the proof the previous branch minted. `owned:true` is a string only
            // a working attest AND a working verify can produce together.
            var verify = Net.CloudCode.CallAsync("player-account", new
            {
                action = "verify",
                playerId = id,
                proof = minted.proof,
            });
            yield return Await(verify);

            StringAssert.Contains("\"owned\":true", Flat(verify.Result),
                "the endpoint refused a proof it had just minted for this player, which is the " +
                "impersonation guard failing CLOSED: every honest player would lose their tag. " +
                "docs/TODO.md § 90.1.");

            var cleanup = Net.CloudCode.CallAsync("player-account", new { action = "delete" });
            yield return Await(cleanup);

            Debug.Log($"[CloudEndpointActionProbe] player-account attest minted and verify vouched for {minted.handle}");
        }

        /// <summary>
        /// `resolve` turns an exact NAME#TAG into a player id, against the LIVE index.
        ///
        /// ⚠️⚠️ THIS IS THE ONLY THING THAT CAN SAY THE HANDLE INDEX IS REAL.
        /// `CareerAndCloudCodeTests` compares the C# to the FILE ON DISK and was green through
        /// § 94.2b, where the service was running a copy of `match-record.js` published six hours
        /// before the code that was being tested. `docs/TODO.md` § 90.5 is the other half: an
        /// undeclared parameter is stripped silently, so a `resolve` whose `handle` never arrived
        /// would compare `""` against a real handle, answer `{"playerId":"","handle":""}`, and
        /// look exactly like "no account has that name".
        ///
        /// ⚠️⚠️ SO THE SEQUENCE IS THE ASSERTION AND A SINGLE CALL WOULD PROVE NOTHING. It saves
        /// a profile, resolves its own handle back to its own player id — a string no other
        /// branch of this script can produce for this input — then deletes the profile and
        /// resolves again, which must come back EMPTY. That second half is the safety argument in
        /// § 102.2: the index is a route and never the authority, so a row that outlives the
        /// account it pointed at has to answer NOT FOUND rather than sending a friend request to
        /// a stranger.
        ///
        /// ⚠️ IT RUNS ON THE THROWAWAY `qa45` PROFILE like everything else in this file. `save`
        /// and `delete` here would overwrite and then wipe his real account on the `default` one.
        /// </summary>
        [UnityTest]
        public IEnumerator TheAccountEndpointResolvesAnExactHandleAndForgetsADeletedOne()
        {
            yield return SignedInAsTheProbePlayer();
            string id = AuthenticationService.Instance.PlayerId;
            string handle = ProbeDisplayName + "#" + AccountRules.DerivedTag(id);

            string profileJson = JsonUtility.ToJson(new AccountProfile
            {
                PlayerId = id,
                DisplayName = ProbeDisplayName,
                Discriminator = AccountRules.DerivedTag(id),
            });

            var save = Net.CloudCode.CallAsync(
                "player-account", new { action = "save", profile = profileJson });
            yield return Await(save);

            StringAssert.Contains(handle.Replace(" ", ""), Flat(save.Result).Replace("\\u0020", ""),
                "the save branch answered a different handle from the one the core derives, so "
                + "the search below would be looking for a name this account is not called. "
                + "AccountRules.DerivedTag and player-account.js's derivedTag are the two halves.");

            var found = Net.CloudCode.CallAsync(
                "player-account", new { action = "resolve", handle });
            yield return Await(found);

            StringAssert.Contains(id, Flat(found.Result),
                $"`resolve` did not find '{handle}', which was saved one call ago. Either the "
                + "index was not written, or `handle` was stripped because it is not declared in "
                + "module.exports.params, or the resolve branch is not deployed and `load` "
                + "answered instead. docs/TODO.md § 90.5 and § 102.2.");

            var cleanup = Net.CloudCode.CallAsync("player-account", new { action = "delete" });
            yield return Await(cleanup);

            var gone = Net.CloudCode.CallAsync(
                "player-account", new { action = "resolve", handle });
            yield return Await(gone);

            StringAssert.DoesNotContain(id, Flat(gone.Result),
                "`resolve` still hands out a player id for a handle whose account has been "
                + "deleted. The index is a route and never the authority: the branch re-reads "
                + "the target's own protected profile and must refuse when the handle no longer "
                + "matches, or a rename would send a friend request to the wrong account.");

            Debug.Log($"[CloudEndpointActionProbe] player-account resolved {handle} to {id} "
                      + "and forgot it after delete");
        }

        // -------------------------------------------------------------------
        // § match-record: load, history, submit
        // -------------------------------------------------------------------

        /// <summary>
        /// `load` answers the profile document, and `history` answers a different shape.
        ///
        /// ⚠️ `total` IS THE ASSERTION FOR `history` AND IT IS THE ONLY KEY THAT BRANCH RETURNS
        /// THAT `load` DOES NOT. `load` returns `profile` and `applied`; a stripped action lands
        /// on `load`, so a history test that only checked for `history` in the output would still
        /// need to distinguish an empty array from an absent key, and `""` is the honest answer
        /// for both.
        /// </summary>
        [UnityTest]
        public IEnumerator TheCareerEndpointAnswersLoadAndHistoryFromDifferentBranches()
        {
            yield return SignedInAsTheProbePlayer();

            var load = Net.CloudCode.CallAsync(Net.CareerStore.ScriptName, new { action = "load" });
            yield return Await(load);
            StringAssert.Contains("\"applied\":false", Flat(load.Result),
                "the load branch did not run. It is also the default branch, so this failing " +
                "means the endpoint is not deployed at all rather than that an action was stripped.");

            var history = Net.CloudCode.CallAsync(
                Net.CareerStore.ScriptName, new { action = "history", offset = 0, limit = 5 });
            yield return Await(history);
            StringAssert.Contains("\"total\":", Flat(history.Result),
                "the history branch did not run: `total` is the only key it returns that `load` " +
                "does not, and a stripped action lands on `load`. docs/TODO.md § 90.5.");

            Debug.Log($"[CloudEndpointActionProbe] match-record load and history answer from different branches");
        }

        /// <summary>
        /// ⚠️⚠️ THE CHECK `FUTURE.md` § 4.5.1 CALLS THE MOST VALUABLE IN THE PHASE: a real record
        /// through `submit`, the profile read back, and the XP asserted against the core's own
        /// arithmetic. It is the one thing that can catch `ProgressionRules.cs` and the
        /// `matchXp` copy inside `match-record.js` drifting apart, in the only place a player
        /// would ever notice: their level.
        ///
        /// ⚠️⚠️ THE MATCH ID IS UNIQUE PER RUN AND THAT IS LOad-BEARING. `applyRecord` refuses a
        /// `MatchId` it has already counted and answers `applied:false` with a 200, which is a
        /// SUCCESS. A fixed id would pass this test on its second run by counting nothing, and
        /// the delta would be zero against an expectation of zero.
        ///
        /// ⚠️⚠️ AND IT ASSERTS `applied:true` BEFORE IT ASSERTS THE DELTA, because those are two
        /// different failures. `applied:false` means the record was refused or replayed; a wrong
        /// delta on `applied:true` means the two copies of the arithmetic disagree. Reading only
        /// the delta would report the first as the second.
        ///
        /// ⚠️ THE RECORD IS BUILT TO EXERCISE EVERY TERM IN `MatchXp`: it finishes first, it has
        /// a knockdown, a retrieval under pressure, a tag, a sabotage and no penalties, so all
        /// five objectives and the placement bonus are live. A record that scored nothing would
        /// assert that completion XP alone matches, and four of the six terms would be untested.
        /// `ActiveRounds` equals `Rounds`, so the AFK check pays rather than refusing.
        /// </summary>
        [UnityTest]
        public IEnumerator ARealSubmissionPaysExactlyWhatProgressionRulesSays()
        {
            yield return SignedInAsTheProbePlayer();
            string id = AuthenticationService.Instance.PlayerId;

            var before = Net.CloudCode.CallAsync(Net.CareerStore.ScriptName, new { action = "load" });
            yield return Await(before);
            int xpBefore = XpIn(before.Result);

            var record = ProbeRecord(id);
            var line = MatchRecordRules.LineFor(record, id);
            Assert.IsNotNull(line, "the probe built a record it is not in, which is this test's own bug");

            int expected = ProgressionRules.MatchXp(record, line);
            Assert.Greater(expected, ProgressionRules.CompletionXp,
                "the probe record does not exercise the objective terms, so a drift in five of " +
                "the six would pass unnoticed");

            var submit = Net.CloudCode.CallAsync(Net.CareerStore.ScriptName, new
            {
                action = "submit",
                record = JsonUtility.ToJson(record),
            });
            yield return Await(submit);

            StringAssert.Contains("\"applied\":true", Flat(submit.Result),
                "the endpoint did not count the record. `applied:false` means it was refused or " +
                "replayed, not that the arithmetic disagrees: check that this run's MatchId is " +
                "unique and that a non-bot line carries this player's id. " +
                "MatchRecordRules.Submittable names both refusals.");

            var after = Net.CloudCode.CallAsync(Net.CareerStore.ScriptName, new { action = "load" });
            yield return Await(after);
            int xpAfter = XpIn(after.Result);

            Assert.AreEqual(expected, xpAfter - xpBefore,
                $"the server paid {xpAfter - xpBefore} XP for a match ProgressionRules.MatchXp " +
                $"values at {expected}. ProgressionRules.cs and the matchXp copy in " +
                "ugs/cloud-code/match-record.js have drifted; the C# is the specification and " +
                "the JS is the bug. docs/TODO.md § 91.6.");

            Assert.AreEqual(ProgressionRules.LevelForXp(xpAfter), LevelIn(after.Result),
                "the server's level does not agree with its own XP through " +
                "ProgressionRules.LevelForXp, so levelForXp in the script has drifted too");

            Debug.Log($"[CloudEndpointActionProbe] match-record submit paid {expected} XP, " +
                      $"{xpBefore} -> {xpAfter}, exactly what ProgressionRules.MatchXp says");
        }

        // -------------------------------------------------------------------
        // § telemetry: submit, report
        // -------------------------------------------------------------------

        /// <summary>
        /// `report` reads the counters back, and returns two keys `submit` never does.
        ///
        /// ⚠️ `rollup` IS THE BRANCH MARKER. `submit` answers `accepted`, `refused`, `rolled` and
        /// a `funnel` OBJECT of steps first reached; `report` answers `profile`, `rollup` and a
        /// `funnel` ARRAY of the step names. `rollup` appears in one of them and nowhere else.
        ///
        /// ⚠️ IT SENDS NO EVENT, so it cannot touch the funnel. `UgsServicesProbe` carries the
        /// full note: a funnel step is impossible to un-record by construction, so no probe may
        /// ever record one.
        /// </summary>
        [UnityTest]
        public IEnumerator TheTelemetryEndpointReportsFromItsOwnBranch()
        {
            yield return SignedInAsTheProbePlayer();

            var report = Net.CloudCode.CallAsync("telemetry", new { action = "report" });
            yield return Await(report);

            string flat = Flat(report.Result);
            StringAssert.Contains("\"rollup\"", flat,
                "the report branch did not run: `rollup` is the only key it returns that the " +
                "submit branch does not, and a stripped action lands on submit. " +
                "docs/TODO.md § 90.5.");
            StringAssert.Contains("\"funnel\":[", flat,
                "the report branch answered a funnel OBJECT rather than the step-name ARRAY, " +
                "which is the submit branch's shape");

            Debug.Log($"[CloudEndpointActionProbe] telemetry report answered from its own branch");
        }

        // -------------------------------------------------------------------
        // § THE SOCIAL ENDPOINT. `docs/TODO.md` § 102.
        // -------------------------------------------------------------------

        /// <summary>
        /// The social endpoint answers `load` and `presence` from their own branches, and the
        /// list it hands back is a document this build can read.
        ///
        /// ⚠️⚠️ THE POINT OF THIS CASE IS `docs/TODO.md` § 94.2b'S STANDING RULE: **for anything
        /// under `ugs/`, finished means deployed, and a commit is not a deployment.**
        /// `CareerAndCloudCodeTests` compares the C# to the FILE ON DISK and was green throughout
        /// the session in which Phase 4's script had never been published; only a call to the live
        /// service can tell the two apart.
        ///
        /// ⚠️⚠️ AND IT ASSERTS A KEY ONLY THE NAMED BRANCH PRODUCES, WHICH IS § 90.5. A stripped
        /// `action` parameter lands on `load`, which answers well-formed JSON, so "it answered" is
        /// not evidence that the branch ran. `written` appears in `presence` and nowhere else.
        ///
        /// ⚠️ IT WRITES ONLY ITS OWN TWO DOCUMENTS AND NAMES NO OTHER PLAYER. Every action that
        /// touches somebody else's account needs a second real account to be honest about, which
        /// this probe does not have; `WhenTwoAccountsAreNeeded` below says what is therefore not
        /// covered here rather than leaving it implied.
        /// </summary>
        [UnityTest]
        public IEnumerator TheSocialEndpointAnswersLoadAndPresenceFromDifferentBranches()
        {
            yield return SignedInAsTheProbePlayer();

            var presence = Net.CloudCode.CallAsync("social", new
            {
                action = "presence",
                state = 1,
                joinCode = "",
                handle = "QA45#0000",
            });
            yield return Await(presence);

            string wrote = Flat(presence.Result);
            StringAssert.Contains("\"written\":true", wrote,
                "the presence branch did not run. `written` is the only key it returns, and a " +
                "stripped action lands on `load`, which answers a well-formed list instead. " +
                "docs/TODO.md § 90.5.");

            var load = Net.CloudCode.CallAsync("social", new { action = "load" });
            yield return Await(load);

            string flat = Flat(load.Result);
            StringAssert.Contains("\"list\"", flat,
                "the load branch answered no list at all");

            // ⚠️⚠️ THE DOCUMENT IS PARSED WITH THE SHIPPING TYPES RATHER THAN GREPPED, because
            // the failure this catches is a shape change and not a missing key. `SocialStore`
            // reads it with exactly these two calls, so a payload this cannot parse is a payload
            // the game silently discards into a warning nobody reads.
            var envelope = JsonUtility.FromJson<SocialEnvelope>(load.Result);
            Assert.IsNotNull(envelope, "the endpoint's answer is not the envelope SocialStore reads");

            var list = Core.SocialRules.Normalise(
                JsonUtility.FromJson<Core.SocialList>(envelope.list));

            Assert.IsNotNull(list.Friends, "the list has no Friends array");
            Assert.IsNotNull(list.Incoming, "the list has no Incoming array");
            Assert.IsNotNull(list.Blocked, "the list has no Blocked array");

            Debug.Log($"[CloudEndpointActionProbe] social load answered " +
                      $"{list.Friends.Count} friend(s), {list.Incoming.Count} request(s), " +
                      $"{list.Blocked.Count} blocked");
        }

        /// <summary>
        /// Blocking and unblocking, which are the two actions the probe player can take entirely
        /// against its own document.
        ///
        /// ⚠️⚠️ THE BLOCK IS THE HALF OF THIS PHASE THAT HAS TEETH, and it is the one that must
        /// survive the round trip: `NetSession.ApproveConnection` refuses a peer whose account id
        /// is on this list, so a block that does not persist is a block that stops working when
        /// the player restarts the game.
        ///
        /// ⚠️ IT BLOCKS AN ID THAT BELONGS TO NOBODY, DELIBERATELY. `block` clears the subject
        /// from both sides, so blocking a real account would edit a stranger's document; an id
        /// that names nobody exercises the same branch and touches one document.
        /// </summary>
        [UnityTest]
        public IEnumerator TheSocialEndpointStoresABlockAndTakesItBack()
        {
            yield return SignedInAsTheProbePlayer();

            string ghost = $"qa45-ghost-{Guid.NewGuid():N}";

            var block = Net.CloudCode.CallAsync("social", new
            {
                action = "block",
                playerId = ghost,
                handle = "QA45#0000",
            });
            yield return Await(block);

            StringAssert.Contains(ghost, Flat(block.Result),
                "the endpoint did not store the block. This is the only rule in Phase 6 that " +
                "refuses a connection, so one that does not persist is one that stops working " +
                "on the next launch.");

            var unblock = Net.CloudCode.CallAsync("social", new
            {
                action = "unblock",
                playerId = ghost,
                handle = "QA45#0000",
            });
            yield return Await(unblock);

            Assert.IsFalse(Flat(unblock.Result).Contains(ghost),
                "the block survived an unblock, so the block list is a one-way door");

            Debug.Log("[CloudEndpointActionProbe] social stored a block and took it back");
        }

        [Serializable]
        private sealed class SocialEnvelope
        {
            public string list = "";
        }

        /// <summary>
        /// `submit` refuses a name the deployed script does not know, and says so in `refused`.
        ///
        /// ⚠️⚠️ THIS IS THE ONE ASSERTION IN THE PROJECT THAT CAN FAIL WHEN THE SERVICE IS RIGHT
        /// AND THE REPOSITORY IS WRONG, WHICH IS WHY IT IS HERE. `UgsServicesProbe` asserts
        /// `refused:0` for events the game sends. Nothing asserted that the count MOVES, so a
        /// deployed script that accepted everything, including typos, would have satisfied every
        /// existing check while quietly turning a renamed event into a silently lost one.
        /// `docs/TODO.md` § 90.3's whole point is that a renamed event is a broken history and
        /// nothing errors.
        /// </summary>
        [UnityTest]
        public IEnumerator TheTelemetryEndpointRefusesAnEventNameItDoesNotKnow()
        {
            yield return SignedInAsTheProbePlayer();

            string batch = "[{\"Name\":\"not_an_event_this_game_sends\",\"Count\":1,\"Params\":{}}]";
            var call = Net.CloudCode.CallAsync("telemetry", new { action = "submit", events = batch });
            yield return Await(call);

            string flat = Flat(call.Result);
            StringAssert.Contains("\"refused\":1", flat,
                "the deployed telemetry.js accepted an event name that is in no list anywhere. " +
                "An endpoint that accepts everything makes `refused:0` meaningless, and " +
                "`refused:0` is the only thing standing between a renamed event and a silently " +
                "broken history. docs/TODO.md § 90.3.");
            StringAssert.Contains("\"accepted\":0", flat,
                "an unknown event was counted as accepted");
            StringAssert.Contains("\"funnel\":{}", flat,
                "a refused event recorded a funnel step");

            Debug.Log($"[CloudEndpointActionProbe] telemetry refused an unknown event: {call.Result}");
        }

        // -------------------------------------------------------------------
        // § FIXTURES
        // -------------------------------------------------------------------

        [Serializable]
        private sealed class AttestResponse
        {
            public string handle;
            public string proof;
            public string expires;
        }

        [Serializable]
        private sealed class CareerResponse
        {
            public string profile;
            public bool applied;
        }

        private static int XpIn(string response) => ProfileIn(response).Xp;

        private static int LevelIn(string response) => ProfileIn(response).Level;

        /// <summary>
        /// ⚠️ THE PROFILE COMES BACK AS A JSON STRING INSIDE A JSON OBJECT, which is the shape
        /// `CareerStore.AdoptRemoteProfile` reads and the shape every branch of the script
        /// returns. Parsing it the same way the game does is deliberate: a probe that parsed it
        /// some other way could pass against a response the game cannot read.
        /// </summary>
        private static PlayerProfile ProfileIn(string response)
        {
            var answer = JsonUtility.FromJson<CareerResponse>(response);
            if (answer == null || string.IsNullOrWhiteSpace(answer.profile)) return new PlayerProfile();
            return ProfileRules.Normalise(JsonUtility.FromJson<PlayerProfile>(answer.profile))
                   ?? new PlayerProfile();
        }

        /// <summary>
        /// A record this player is really in, with every XP term live and a match id that cannot
        /// collide with a previous run of this test.
        /// </summary>
        private static MatchRecord ProbeRecord(string playerId)
        {
            var players = new PlayerMatchStats[Balance.PlayerCount];
            for (int i = 0; i < players.Length; i++)
                players[i] = new PlayerMatchStats
                {
                    Slot = i,
                    IsBot = i != 0,
                    PlayerId = i == 0 ? playerId : "",
                    Handle = i == 0 ? "QA Four Five#0000" : $"BOT {i}",

                    // ⚠️ NOT A HERO ID. A mastery path would credit a hero this player has never
                    // played, and the account XP assertion does not need one. `HasMasteryPath`
                    // is asserted separately by the core tests.
                    CharacterId = "",
                    Score = i == 0 ? 900 : 100 * i,
                    Knockdowns = i == 0 ? 2 : 0,
                    Retrievals = i == 0 ? 3 : 0,
                    RetrievalsUnderPressure = i == 0 ? 1 : 0,
                    Tags = i == 0 ? 1 : 0,
                    Sabotages = i == 0 ? 1 : 0,
                    Throws = i == 0 ? 5 : 0,
                    TayaCampPenalties = 0,
                    UnretrievedSlipperPenalties = 0,
                    TimeToFirstThrow = 4.0f,
                    ActiveRounds = Balance.Rounds,
                    RoundsDefended = 1,
                };

            var record = new MatchRecord
            {
                MatchId = $"qa45-{Guid.NewGuid():N}",
                Mode = GameMode.Classic.ToString(),
                MapId = "eskinita",
                Rounds = Balance.Rounds,
                DurationSeconds = Balance.Rounds * Balance.RoundTime,
                PlayedUtc = DateTime.UtcNow.ToString("O"),
                WinningSlot = 0,
                Online = true,
                DefenderByRound = new int[Balance.Rounds],
                Players = players,
            };

            for (int i = 0; i < record.DefenderByRound.Length; i++)
                record.DefenderByRound[i] = i % Balance.PlayerCount;

            return MatchRecordRules.Normalise(record);
        }
    }
}
