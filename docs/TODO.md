# TODO: Tumbang Preso Unity

**The open worklist. If it is not open, it is in [`TODO_Archive.md`](TODO_Archive.md).**

Read [`../CLAUDE.md`](../CLAUDE.md) first and [`VISION.md`](VISION.md) second. Check this file
before inventing a task, and update it in the same commit as the work.

---

## What is open right now

Twelve sections, and this list is the whole of it. Everything else in this repository's history
is in the archive with its number unchanged.

| § | Open work | Where it bites |
|---|---|---|
| **126** | This session: the full PlayMode suite, the thumb floor, the move stick, rumble, the device toggle, the .apk | § 126.8 is the big one: **the full PlayMode run is not a reliable gate** |
| **126.11** | Crossplay is argued, not demonstrated | Both players exist; nobody has watched them join |
| **93** | A held tsinelas drifts 0.084 m from the hand | Four samples now, all outside the bound. Not a flake |
| **96** | He has never found the way into the hub | The door, not the layout. `CLAUDE.md` § 6.3 |
| **95b** | Nothing asserts that a menu label FITS, only that it is legible | Two probes, neither asking the question |
| **72** | Two lobby controls reported dead that every headless check says are alive | Reported by a person, green in every test |
| **68** | The lobby is a form and it should be a room | Planned, not built |
| **69** | No chat, in the lobby or in a match | Planned, not built |
| **88, 89** | Accounts and the career layer, in progress | Read § 89.6 before touching `ProfileRules`, written in C# and again in JS |
| **118, 119, 121** | The paper front end: what is coherent and what is not finished | |

⚠️⚠️ **AND THE SIX UNSTARTED PHASES ARE NOT IN THIS FILE AT ALL.** Bots and population (11),
modes and maps (12), seasons (13), accessibility (16), tournaments and replays (17) and getting it
in front of people (18) live in [`FUTURE.md`](FUTURE.md) with a written prompt each. **They are
things somebody might decide to do; an entry here is something somebody should do.** That
distinction is the reason they are separate files.

---

## The five things worth knowing before you touch anything

⚠️⚠️ **`NetSession.ProtocolVersion` IS 21, AND READ IT FROM THE FILE RATHER THAN FROM ANY
DOCUMENT.** This preamble carried **both 19 and 21** as "the" number for two days, four paragraphs
apart, because each session appended its own line and nobody deleted the last one. Peers on
different numbers refuse each other by design, so a stale number here sends somebody hunting a
network bug that is a rebuild. `grep -n ProtocolVersion Assets/TumbangPreso/Runtime/Net/NetSession.cs`.

⚠️⚠️ **A GREEN LAYOUT PROBE IS NOT A GOOD SCREEN, AND A GREEN FULL PLAYMODE RUN IS NOT A GATE.**
The first is `CLAUDE.md` § 6.2a; the second is § 126.8 and it is new. Verify with `-testFilter`
over the suites you touched.

⚠️ **Every door in the front end gets read before it gets moved.** § 96 and § 115.6 are the same
fault twice: he commissioned a feature, read the entry that shipped it, and could not find it.

⚠️ **The unplugged LAN run is DONE** (2026-08-31) and must not be re-raised as outstanding. The
requirement it protects stands: a full four-player match starts and finishes on LAN with the
internet unplugged, and the account layer may not break that. General Santos City is why.

⚠️ **LAST TSINELAS STANDING has rules, tests and a document and no match half**, and it is
deliberately absent from the lobby's RULES row until it has one. § 115.10, in the archive.

---

## How this file stays short

⚠️⚠️ **IT REACHED 22,930 LINES AND STOPPED DOING ITS JOB, WHICH IS TO BE READ.** 🧑, 2026-09-03:
*"todo md so long can u clean that shit up"*, *"its not supposed to be that long"*, and the
instruction that shaped the split: *"no ned to delete the batch reports ... js idk rename them or
smth"*, *"make the todo md that will be read very brief"*, *"future proof docs to not clutter like
taht again"*.

**So the rule, and it is mechanical on purpose:**

1. **A section lives here while its HEADING says `OPEN`, `IN PROGRESS` or `NOT DONE`.** Nothing
   else is consulted. Status in the heading, never buried in the prose, because prose is what made
   134 sections impossible to sort.
2. **When you finish something, change its heading and move the whole section to
   [`TODO_Archive.md`](TODO_Archive.md)**, keeping its number, and leave its row in the archive
   index at the bottom of this file. ⚠️ **Do not delete it and do not summarise it away.** The
   reasoning is the part that stays valuable, and every ⚠️ in this repository was written because
   something went wrong once.
3. ⚠️⚠️ **A SESSION REPORT IS NOT AN OPEN ITEM.** *"The 2026-08-29 evening batch"* was 525 lines,
   *"the 2026-08-29 balance-and-controls batch"* was 973, and neither was ever open work: they were
   records of a day. **Write the batch report, then archive it in the same commit.** Twelve of the
   twenty biggest sections in this file were dated batch reports.
4. **The numbers are not unique and that is not being fixed.** § 53, § 63, § 64 and § 65 each
   appear more than once. Renumbering would break every pointer in `CLAUDE.md`, `VISION.md`,
   `FUTURE.md` and the code comments, which is a worse trade than a duplicate heading. **Search by
   title as well as by number.**

---

## 88 · Accounts and identity ⚠️ IN PROGRESS 2026-08-31

Phase 1 of `FUTURE.md` was explicitly commissioned. Its preflight was stale in an important way:
Authentication was not unused. `NetIdentity` already signed in anonymously at boot, cached the
attempt, persisted the UGS session, and degraded to a local token for LAN. `UgsCheck` also exercised
that path. The account layer extends that proven seam rather than creating a second sign-in owner.

**Built in this phase:** `PlayerAccount` owns the player id, display name, discriminator, bio,
country, pronouns and signed-in/local state and raises `Changed`; the splash awaits its bounded
boot barrier before activating the menu; username/password linking uses
`AddUsernamePasswordAsync` and asserts the anonymous PlayerId did not change; username sign-in can
move the account to a second device; deletion clears Cloud Save before deleting Authentication;
the first score queues the upgrade offer for the next menu; and offline tournament guests get an
ephemeral local identity without overwriting the machine owner's account.

**Database shape:** Authentication owns credentials and the stable id. The server-side Cloud Code
script `ugs/cloud-code/player-account.js` validates profile writes and stores one protected
`accountProfile` record in Cloud Save. The client never writes Cloud Save directly. Local JSON is
the offline cache and remote valid fields win when the endpoint answers.

**Lobby identity:** every local hello, identify message and beacon reads `PlayerAccount.LobbyName`
rather than `Settings.PlayerName`; the host accepts only a core-validated `display#1234` handle and
allocates a deterministic fallback tag for an invalid claim.

**Done looks like:** the Phase 1 acceptance list in `FUTURE.md` § 1 plus a deployed
`player-account` Cloud Code endpoint, Core, EditMode and focused PlayMode green, an unplugged LAN
run, a clean Windows player on the Desktop, committed and pushed.

### 88.1 · Three things the first pass got wrong, found by running its own tests

The account work above was written but never committed and never run: the session that wrote it
went looking for a Unity package-manager fault instead. There was none. `Temp/UnityLockfile` was
stale, which is § 7 of `CLAUDE.md` verbatim, and after removing it the project compiles and the
suites run. **Nothing was wrong with the editor, the `Library`, or the machine's UPM cache.**
`AppData\Local\Unity\cache\upm.accounts-backup` is that session's moved-aside copy and can be
deleted; Unity rebuilt `upm` beside it.

Running the suites then found three real defects, all in the new code.

**88.1a · Two name lengths, and the shorter one is on the wire.** `AccountRules.DisplayNameMax`
was written as 16 while `Balance.PlayerNameMax` has been 14. That is not a cosmetic
disagreement. `LanBeacon` truncates the name it broadcasts to the `Balance` value, the settings
field sets `characterLimit` from it, and `Hud`'s row width was measured against that many "W"s
(`Hud.cs` ~2895). A 16-character account name therefore renders past a measured layout and
arrives over LAN clipped, so the name in the profile and the name on the scoreboard stop being
the same string. `DisplayNameMax` is now `= Balance.PlayerNameMax`, one constant, and
`TheAccountNameLimitIsTheOneTheWireAndTheHudUse` fails if they are ever split again.
⚠️ The Cloud Code validator had the same 16 hardcoded, and the server is the authority: it would
have stored a 15-character name that every client then silently clipped. It reads 14 now, with a
comment saying it is the one place the number is written twice.

**88.1b · Every LAN peer was renamed `Player#tag`, which is the nationals case.** Arrival kept
the claimed name only when it was already a full `name#1234` handle and rewrote everything else to
`Player#tag`. Everything else is: every LAN peer, every build older than this branch, and every
client whose profile has not finished loading. Four machines joining off the beacon in a hall
would have rendered as four rows nobody could tell apart, in the one venue where that matters
most. `AccountRules.ArrivalHandle` now keeps a usable claimed name and allocates the tag from the
durable token, and falls back to `Player` only for a name that cannot be shown at all. It sits in
the core rather than in `LobbySession` because it is a rule, per `FUTURE.md` § 0.5 rule 3.

**88.1c · ✅ CLOSED 2026-08-30 BY § 90.1, WHICH ALSO CORRECTS THE FIX THIS ENTRY PRESCRIBES.**
⚠️ The last paragraph below says to have the host ask the endpoint whether a player id owns a
handle. **That does not work on its own**: the player id arrives in the same message as the
handle, from the same peer, so an impostor claims both and the endpoint truthfully says yes. The
missing half is proof that the peer IS the account it names. § 90.1 has what shipped. The
statement of the problem below is still exactly right and is kept for it.

**The problem, as first written:** ⚠️⚠️ **THE IMPERSONATION GUARD DOES NOT EXIST YET, AND THE
TEST SAID IT DID.** The
brief's reason for routing the lobby name through the account was that *"the first thing anybody
does with a new account system is impersonate somebody"*. The rule written for it was backwards.
It rewrote a bare `Maria Clara` on the theory that it was forging `Maria Clara#4417`, while
admitting a claimed `Maria Clara#4417` **verbatim** in the assertion directly above it. So it
punished the honest case and waved the actual attack through, and
`LobbyAcceptsOnlyCanonicalAccountHandles` asserted that as correct behaviour.

**A peer-hosted lobby cannot close this on its own.** The tag of a real account is allocated by
UGS Player Names, so the host cannot recompute it from the token and cannot tell a genuine
`Maria Clara#4417` from a claimed one. Closing it needs the host to ask the `player-account`
endpoint whether this player id owns this handle, cache the answer, and **fall through to the
claim on LAN or when the endpoint is unreachable**, because § 0.5 rule 7 says a LAN match may
never sit behind a login. That is the next piece of work on this phase and it is not built.
The test is now `LobbyKeepsAUsableNameAndAlwaysTagsIt` and asserts only what is true today.

**88.1d · The splash could wait forever on a service that answers slowly rather than not at
all.** The splash holds the menu until `PlayerAccount.InitializeAsync` completes, and the splash's
own `MaxWait` only logs a warning: its loop has no upper bound. The boot budget raced **only the
sign-in**, leaving `RefreshFromAuthenticationAsync` unbounded behind it, and that awaits Player
Names and then Cloud Save. A service that accepts the connection and then never answers is exactly
what venue Wi-Fi behind a captive portal does, which is the network the nationals will be played
on, and it is the one failure a try/catch cannot see. The budget now covers the whole remote path.
⚠️ A late answer is not thrown away: the work keeps running and applies itself through `Changed`,
so a slow connection costs a few seconds of showing the local name rather than the account.

**88.1e · A tournament guest overwrote the machine owner's account.** `SignInAsGuest` documents
that it does not replace the owner's account, and `Persist` broke that promise: every write goes
through `Apply`, so a guest editing a profile wrote the guest id, name, tag and bio straight over
the owner's saved account. That is somebody handing their laptop over for one match at an offline
tournament and getting it back with a different account on it. `Persist` now returns early while a
guest is active, a guest is refused `DeleteAsync` outright (it clears the settings file, and the
settings file is still the owner's), and a late remote answer parks itself as what `LeaveGuest`
returns to rather than applying over the guest mid-session.

**88.1f · The upgrade offer wrote `settings.json` on every point scored.** `MarkWorthKeeping` is
reached from `MatchDirector.AddScore`, which is every point, and passive defence pays +10 a second
while the lata stands. It set the pending flag and saved without first checking whether the flag
was already set, so a round reserialised the settings file about once a second per defender, on
the thread the match steps on. The decision is now `AccountRules.ShouldQueueUpgradeOffer` in the
core, with the already-pending term as a named argument and three tests on it, so it is one write
per session.

**Verified:** Core 135/135 (`dotnet test`), from 69 before the phase. EditMode 236/236, read from
`Logs/tests.xml` rather than the exit code.

### 88.2 · The UGS project belongs to somebody else, and the service half is blocked on that

🧑, 2026-08-31, shown the dashboard steps: *"can we js connect it to my acct instead bcz this
isnt mine"*.

**The project is `090f4720-e3f8-466f-b8f5-7679c6b41fb1` under org `paulandreirecio22`.** The Unity
Hub on this machine is signed in as `M4tyuuu` / `matthewtlabrador@gmail.com`, whose own org is
`matthewtlabrador`. So the editor is his and the cloud project is not, which is why he cannot make
the service account that Cloud Code deployment needs.

⚠️ **THE LINK IS TWO LINES AND NO CODE.** `cloudProjectId` and `organizationId` in
`ProjectSettings/ProjectSettings.asset`, lines 738 and 742. Nothing in `Assets/`,
`Packages/`, `ugs/` or `docs/` holds a copy; `PlayerAccount.CallCloudAsync` reads
`Application.cloudProjectId` at runtime and follows the file. `tools/relink_ugs_project.sh` does
the swap, refuses an id that is not a UUID, and prints what it cannot prove.

**Three things to know before relinking, none of which are reasons not to:**

1. ⚠️ **A UGS PlayerId is scoped to its project, so this is a reset rather than a transfer.** Every
   existing anonymous id dies. **That costs nothing today and will not stay that way**: once
   Phase 2 gives people profiles, stats and match history, the same move throws all of it away.
   **Now is the cheapest this will ever be.**
2. ⚠️⚠️ **EVERY MACHINE MUST BE ON THE SAME PROJECT OR ONLINE PLAY SILENTLY STOPS WORKING BETWEEN
   THEM.** Two builds on different UGS projects resolve a join code in different namespaces, so
   the room is simply not there. It does not read as a misconfiguration, it reads as an empty
   lobby. His second laptop and any teammate's build must be rebuilt off the same branch.
   **LAN discovery is unaffected**, because `LanBeacon` never touches UGS.
3. The new project needs Authentication (anonymous **and** username/password), Relay, Lobby,
   Cloud Save and Cloud Code enabled. Relay and Lobby are already proven on the old project, so
   the shapes are known good; only the toggles move.

⚠️⚠️ **AND `UgsCheck` CANNOT VERIFY ANY OF THIS HEADLESSLY, WHICH COST A RUN TO LEARN.**
`UnityServices.InitializeAsync` refuses outside Play Mode with *"Unity Services can only be
initialized in Play Mode"*, so a batchmode `UgsCheck.Run` reports step 2 and then FAILS step 3 for
a reason that has nothing to do with the project. Batchmode also has no Hub session token, so it
cannot see the signed-in account either, which `UgsCheck`'s own step 1 comment already says.
**The real check is the menu item `Tumbang Preso > Check UGS Wiring` from an open editor.**
⚠️ Also do not pass `-quit` alongside `-executeMethod UgsCheck.Run`: it polls from
`EditorApplication.update` and exits itself, so `-quit` kills it before one UGS call is pumped and
the log just stops after the compile.

**✅ RELINKED 2026-08-31.** The project is now
**`dcf0831e-a5f4-43b4-832e-b687f13a3569`** under org **`matthewtlabrador`**, genesis org
`18968483660152`, created by 🧑 on his own account. `ProjectSettings.asset` lines 738 and 742 are
the whole change and `git diff --numstat` on that file reads `2 2`.

**Verified so far:** `OnlineSignInProbe` passes 2/2 against the new project, including
`ALinkedBuildNeverSettlesOnNotLinked`, so the build resolves the new id rather than reporting
itself unlinked. ⚠️ **That is not proof the services are on.** The probe asserts the boot attempt
happens and settles; it passes offline too. Relay and Lobby are only provable from
`Tumbang Preso > Check UGS Wiring` in an open editor.

⚠️⚠️ **PAUL'S BUILD CANNOT SEE THIS ONE ONLINE UNTIL HE REBUILDS OFF THIS BRANCH.** Different UGS
project means a join code resolves in a different namespace, so the room is not there and it reads
as an empty lobby rather than as an error. **LAN is unaffected.** This is the first thing to check
if anybody reports online play "broken" after pulling.

⚠️ **COPPA on the new project reads "NOT primarily targeting children" and must stay that way.**
Marking it child-directed disables username/password auth and restricts Cloud Save, which is
exactly the account layer § 88 just built.

### 88.3 · What the new project actually needed, which was one click and not five

⚠️⚠️ **RELAY, LOBBY AND CLOUD SAVE HAVE NO ENABLE TOGGLE ANY MORE. THEY AUTO-PROVISION ON FIRST
USE.** Four of the five "services to switch on" written above did not exist as switches. Their
dashboard pages are documentation with no enable button, and a fresh project answers Relay and
Lobby calls immediately. Time was spent hunting toggles that were never there.

⚠️ **AND ANONYMOUS SIGN-IN IS NOT AN IDENTITY PROVIDER.** The Identity Providers page reading
*"You have no identity providers"* is the correct healthy state for a project whose anonymous
sign-in works, and it was briefly read as a misconfiguration. **The one thing that genuinely had to
be added was Username & Password**, which IS a provider, and which the account upgrade path needs.
It is added and Enabled as of 2026-08-31.

✅ **Proven live on `dcf0831e-a5f4-43b4-832e-b687f13a3569`**, `UgsServicesProbe` 3/3:
anonymous sign-in returned `qmSg3PKweGRSWqRcd9g0Bo80UKH4`, Relay allocated join code `WGF96G` for
a host of three, and a private lobby was created and deleted.

**`Assets/TumbangPreso/Tests/PlayMode/UgsServicesProbe.cs` is new and is the answer to the
`UgsCheck` problem in § 88.2.** Play Mode is the one context allowed to initialise UGS, so the
three calls that a batch `UgsCheck` cannot make are made from there instead. ⚠️ It is
`[Category("Ugs")]` and excluded from the default PlayMode run, for the same reason `WallClock`
excludes `AiDiagnosticProbe`: it talks to a live service, needs a network and spends free-tier
quota. Run it on purpose after a relink:

```
Unity.exe -batchmode -runTests -projectPath . -testPlatform PlayMode           -testCategory "Ugs" -testResults Logs/ugsprobe.xml -logFile Logs/ugsprobe.log
```

⚠️ **`OnlineSignInProbe` passing is not the same claim.** It asserts the boot attempt happens and
settles, which is true offline and true against a project with every service off. It answers "did
we try"; this answers "did the service say yes". Both are wanted.

### 88.4 · ✅ The service half is done. Phase 1 is complete.

Service account `tumbangpreso-deploy` exists, `ugs login` is stored locally, and
`ugs deploy ugs/cloud-code` created **`player-account`**, which reports `script is already
active` on a publish attempt because deploy publishes it.

✅ **`UgsServicesProbe` 4/4**, and the fourth is the one that matters: `TheAccountEndpointAnswersALoad`
calls the live endpoint with a real player bearer token and gets `{"output":{"profile":""}}`.
An empty profile is the correct answer for a player who has never saved one. That single test
proves the deploy, the publish, the service-account roles and the client's auth all line up,
which is every part of this that could be misconfigured.

⚠️ **THE CLI NEEDS MORE THAN CLOUD CODE EDITOR AND CLOUD SAVE EDITOR, WHICH COST A ROUND TRIP.**
With only those two, every command fails `403 Forbidden` on **`GetEnvironments`** rather than on
the thing being asked for, because the CLI resolves the environment before it does anything else.
The error names an endpoint nobody asked for, which is what makes it confusing. 🧑 resolved it by
granting the service account everything. ⚠️ **If that is ever tightened, keep an environments read
role**, or the CLI breaks again with an error that does not mention environments.

❌ **SUPERSEDED 2026-08-30, AND THE PARAGRAPH BELOW IS KEPT BECAUSE ITS REASONING IS WHY.**
The duplicate is gone: Phase 2 needed a THIRD copy of this request, so it moved into
`Assets/TumbangPreso/Runtime/Net/CloudCode.cs`, which the game itself calls, and the probe now
calls that. A shared helper the shipping code uses is not the seam this paragraph was refusing,
and `CareerAndCloudCodeTests.EveryCloudCodeRequestGoesThroughTheOneHelper` fails if a fourth is
ever written by hand. § 89.5.

⚠️ **`TheAccountEndpointAnswersALoad` DUPLICATES `PlayerAccount.CallCloudAsync` ON PURPOSE AND THE
TWO MUST MOVE TOGETHER.** That method is private, and widening it so a test could reach it would
put a seam in shipping code for one probe. The duplication is the lesser cost, but if the call
shape drifts, **the probe passes while the game fails**, which is the worst outcome available.
Prefer deleting the test over letting it rot. It probes with `load` rather than `save` or
`delete` so it never writes a real profile or exercises the destructive path against a live
project.

✅ **PHASE 1 IS COMPLETE AS OF 2026-08-30.** The one thing that was never part of it, the
impersonation gap in § 88.1c, is built and is § 90.1. ⚠️ Read § 90.1 rather than § 88.1c for
how it works: the fix § 88.1c prescribed is not sufficient on its own and that entry now says so.

⚠️ **AND THE UNPLUGGED LAN RUN IS NOT OUTSTANDING.** 🧑 confirmed on 2026-08-31 that it has been
done. Several documents said otherwise and they are corrected in the same commit as this line.
**The account layer is the thing most likely to regress it**, because a boot that waits on UGS is
exactly what an unplugged venue produces, so re-run it after any change to `PlayerAccount`'s boot
path. § 88.1d is the bound that protects it.

---

## 89 · The profile, the stats and the match history ⚠️ IN PROGRESS 2026-08-30

Phase 2 of `FUTURE.md`, commissioned off its § 19.2 prompt. Phase 1 (§ 88) is under it, and the
`player-account` endpoint was re-proved before any of this was planned, per that prompt's own
VERIFY FIRST block: `ugs cloud-code scripts list` reports `player-account`, and the `Ugs` PlayMode
category came back **4/4 with `total="4"`**, which is the check `FUTURE.md` § 0.5 rule 9 asks for
rather than a green tick over an empty run.

**What a match now produces.** The host counts one `MatchRecord` covering all four seats, mints it
a `MatchId` at the whistle, broadcasts it to every peer, and each peer submits its own line to the
`match-record` Cloud Code endpoint from its own authenticated session. The endpoint accumulates
that player's `PlayerProfile` and stores the record in a 100-entry history. Nothing in the loop is
per event: a Hero Strike match carrying nine hundred passive-defence ticks costs one call per
player.

**Where the code went**, per `FUTURE.md` § 0.5 rule 3:

| Piece | Where |
|---|---|
| `MatchRecord`, `PlayerMatchStats`, `MatchRecordRules` | `Packages/com.tumbangpreso.core/Runtime/MatchRecord.cs` |
| `PlayerProfile`, `CareerTotals`, `ProfileRules` | `Packages/com.tumbangpreso.core/Runtime/PlayerProfile.cs` |
| Host-side counting | `Assets/TumbangPreso/Runtime/MatchStatsCollector.cs` |
| Local cache, offline queue, submission | `Assets/TumbangPreso/Runtime/Net/CareerStore.cs` |
| The one Cloud Code request | `Assets/TumbangPreso/Runtime/Net/CloudCode.cs` |
| The server, and the authority | `ugs/cloud-code/match-record.js` |
| The career page | `Assets/TumbangPreso/Runtime/UI/ProfileOverlay.cs` |
| The end-of-match summary | `MatchResult.OnRecordReady` |

⚠️ **No rate is ever stored.** Every rate on the profile is two counts divided at read time by
`MatchRecordRules.Rate`. A stored rate cannot be re-derived after a balance change and cannot be
summed across two seasons, and adding two seasons together is the first thing anybody does with a
career page.

⚠️ **Nothing in this phase awards XP, a level or a rank.** The fields are on the document from day
one so no profile written now has to be migrated later, and Phase 4 and Phase 9 fill them.
`PlayingAMatchAwardsNoXpAndNoRank` asserts it. The header draws neither: an empty rank badge on
every account in the game teaches every player that the game has a rank.

### 89.1 · "Retrievals under pressure" needed a threshold, and it is derived rather than picked

`FUTURE.md` § 2.2 asks for retrievals and retrievals under pressure, and names only the reason
(`VISION.md` § 0: the tension is the retrieval). It does not say how close is close.

`MatchRecordRules.PressureRadius` is **2.30 m** and every part of it is measured.
`Balance.LungeSpeed²/(2·Balance.Friction)` is the distance the taya's dash covers, which is
7.746²/(2·30) = **1.00 m**, because `CLAUDE.md` § 4 requires every impulse to be written as a
distance and solved for a speed; `Balance.LungeTagRadius` is the **1.30 m** the sweep then reaches.
So the stat asks the only question worth asking of a pickup: could the defender have taken you for
it, right then, without moving first.

⚠️ **It lives in `MatchRecordRules`, not in `Balance`.** `Balance` holds numbers the MATCH reads,
and nothing in the match reads this one. A stat threshold sitting among them is the next reader's
excuse to make gameplay depend on it. `ThePressureRadiusIsTheTayasStandingLungeReach` fails if
anybody replaces the arithmetic with a literal.

⚠️ **Only your own tsinelas counts as a retrieval**, which is the line `Carrier.NotifyHolding`
already draws for the hero economy. Picking up somebody else's is a denial play and a good one,
but it is not the run the game is built around and it carries none of the same risk.

⚠️ **The per-tsinelas record is keyed on `Slipper.SeatOfOrigin`, never on `OwnerSlot`**, which is
§ 78.1's fault one object further on. `OwnerSlot` is state the game rewrites every round and
`SliceRunner.EquipOwnedSlippers` sets the taya's to -1, so a record read off it would have no
slipper for whichever seat was defending, and a per-tsinelas win rate would under-count the
taya's shoe forever. The scene is searched once per match rather than once per seat.
⚠️ `MatchBootstrap`, the headless probe path, never assigns `SeatOfOrigin`, so a probe match
records an empty slipper id. That is correct rather than a gap: a probe has no player whose
record it could belong to, and `ProfileRules.Apply` skips an empty id instead of inventing one.

### 89.2 · "Longest survival as last attacker" is an interpretation, because nothing is eliminated

`FUTURE.md` § 2.2 asks for *"longest survival as last attacker"*. That is a stat from a game where
players go out. **This game eliminates nobody.** A tag costs a teleport, a stagger and the whole
trip again, and the round carries on with all four.

The reading that survives contact with the rules is **the last of the three attackers not yet
caught this round**: while you are the only one the taya has not taken, you are the only one they
can still take. `MatchStatsCollector` tracks it off `RoundDirector.Tagged`, opens the clock when
exactly one attacker is untagged, and closes it when that one is tagged or the round ends.

**This is written down because it is a decision rather than a derivation**, and the next person to
read § 2.2 will reach for elimination and find none. If it is the wrong reading, the fix is a
different rule here, not a new event in the match.

### 89.3 · ⚠️⚠️ EACH PEER SUBMITS ITS OWN LINE, WHICH DEPARTS FROM THE LETTER OF § 19.2

`FUTURE.md` § 2.3 says *"the host writes the record and that is a known hole, not an oversight"*,
and § 19.2's "Done when" says the whole match costs **one endpoint call**. The obvious build is the
host calling the endpoint once and the endpoint writing all four career documents. **That is not
what shipped**, per § 0.5 rule 11, and this is the argument.

The hole § 2.3 names is that **the host authors the numbers**. It still does: every count in the
record comes from `MatchStatsCollector` on one machine, and a modified host can lie about all of
them. Phase 8 is what closes that, and nothing here claims to.

Letting the host also **write** three other people's career documents is a second hole and a much
worse one. It is the difference between spoofing a match you played in and editing a stranger's
account: a host who never plays another game with you could still rewrite your career forever,
because the endpoint would have to accept any player id its caller named.

So the record is broadcast to every peer, and **each peer submits only its own line, from its own
authenticated session**. `match-record.js` refuses a record that `context.playerId` is not in, and
writes only that player's documents. It costs **one call per player per match** instead of one per
match, which still satisfies what § 0.3 and § 19.2 step 3 are actually protecting: never one call
per event.

⚠️ **A second thing falls out of it, and is the reason not to reverse this casually.** An offline
or LAN-only peer has no UGS id, only the local token `GameSettings.MintToken` mints. Under the
host-writes-everybody design the server would create a career document keyed by that token which
nothing will ever read. Under this one it simply does not call: it keeps a local career and queues.

### 89.4 · What § 2.1 asks for that is not on the screen, and which phase owns each

The career page draws the header card, the career strip, the mode tabs, the stat blocks, the paged
match history and the match detail. Four things in § 2.1 are deliberately absent, and each would be
an empty box today:

| § 2.1 item | Why not now |
|---|---|
| Avatar | `FUTURE.md` § 1.4 is still an open argument (an in-game avatar builder rather than a photo upload) and 🧑 has not answered it. There is nothing to draw. |
| Rank badge, peak rank | Phase 9. The fields are on the document; drawing an empty badge teaches every player the game has a rank. |
| Achievement and highlight shelf | Phase 10 owns achievements. |
| Compare with a friend | Phase 6 owns friends. There is nobody to compare against. |

⚠️ **Level and border are a different case and are not in that table**, because the FIELD shipped
and only the awarding did not. Phase 4 fills it with no migration.

### 89.5 · The protocol is 15, and § 88.4's probe duplication is resolved

⚠️ **`NetSession.ProtocolVersion` is 15**, for the one new message `MatchRecord`
(`MatchRpc.BroadcastMatchRecord`), which carries a whole finished match to every peer. A peer
without the handler plays the match correctly and then silently gets no end-of-match summary and no
career entry for a game it played, which is exactly the quiet kind of wrong this number exists to
turn into a refusal. **Both machines rebuild off this branch.**
⚠️⚠️ **AND THE MESSAGE IS BIGGER THAN A PACKET, WHICH THE DEFAULT DELIVERY WOULD NOT HAVE TOLD
ANYBODY.** Every other named message in `MatchRpc` is tens of bytes and takes the default
`ReliableSequenced`. A `MatchRecord` is four players times twenty-six fields of JSON and
**measures 2312 bytes** at full length;
`ReliableSequenced` cannot split a message, so an oversized one is refused by the transport,
the host logs a line nobody reads, and every client silently gets no summary and no career
entry, which is precisely the failure the version bump above exists to make impossible. It goes
`ReliableFragmentedSequenced`. ⚠️ Do not size this against a 1500-byte MTU:
`MatchRpc.PoseDelivery`'s note records that they play over Hamachi, a VPN with a smaller MTU and
real loss, and that the relay path *"was not better designed, it was luckier"*.
`AFullMatchRecordNeedsMoreThanOnePacketAndIsSentFragmented` measures the record and fails if
somebody puts the delivery back.

⚠️ `ChatAndLobbyChromeTests.TheProtocolCarriesEveryRosterBump` caught the bump on the first
EditMode run of this work, which is the whole reason that tripwire exists; it is re-armed at 15.

⚠️ **§ 88.4 said the probe's hand-written copy of `PlayerAccount.CallCloudAsync` had to stay a
copy, and that is no longer true.** It kept the duplicate because the method was private and
widening it *"would put a seam in shipping code for one probe"*, while naming the cost outright:
*"if the call shape drifts, the probe passes while the game fails, which is the worst outcome
available."* Phase 2 needed a THIRD copy for the career endpoint, which is the point at which two
copies with a note becomes the failure the note was warning about. The request now lives in
`Net.CloudCode`; `PlayerAccount` calls it and so does the probe. A shared helper the game uses is
not a seam, and the drift § 88.4 feared cannot happen. `TheCareerEndpointAnswersALoad` is the new
`Ugs` test, and it is the only thing that catches a script that was written but never deployed,
because `CareerStore` is built to keep a local career quietly when the service is unreachable.

### 89.6 · `ProfileRules` is written twice, in C# and in JavaScript

`ugs/cloud-code/match-record.js` is `ProfileRules.cs` and `MatchRecordRules.cs` again. Cloud Code
cannot import the C#, and the C# cannot run in Cloud Code, so this is the same trade
`player-account.js` records about `DisplayNameMax`, applied to a whole file instead of a constant.

- **The C# is the specification** and carries the tests. When the two disagree, the JS is the bug.
- **The symptom a player sees is a career that changes the moment they come back online**, because
  `CareerStore` is an optimistic local cache and the server's profile REPLACES it whole on the next
  answer. There is no merge and there must not be: two counters that both claim to know how many
  matches you have played cannot be reconciled without the records that produced them, and the
  records are on the server.
- **If a rule changes in `ProfileRules`, it changes in the JS in the same commit.** Every function
  in the script names the C# member it mirrors, so the two can be diffed by eye.

### 89.7 · The offline path, and the LAN run it must not regress

A match played with the internet unplugged updates the local profile, lands in the local history,
and joins a queue capped at 20 matches. `CareerStore.FlushAsync` sends it on the next successful
sign-in, oldest first, and **stops at the first failure** rather than firing nineteen more requests
at a service that is not there.

⚠️ **The queue is written BEFORE the call is tried, not after it fails.** A process killed
mid-request is the case a queue exists for, and one only written in the failure branch has already
lost the record by then. A duplicate submission is free: `ProfileRules.Apply` refuses a match id it
has already counted, on the server as well as on the client.

⚠️⚠️ **§ 88's warning applies to this phase too, and it is the thing to re-test.** The unplugged
LAN run is DONE and is a regression to protect, not an open task. Nothing in this phase sits on the
boot path, but `CareerStore` now writes `Application.persistentDataPath/career.json` and subscribes
to `PlayerAccount.Changed`, so **re-run the unplugged four-player match after this lands**, per
`FUTURE.md` § 0.5 rule 7.

⚠️ **A career cached for a different account is discarded rather than merged.** Two people share
this machine at a tournament; merging would hand somebody else's knockdowns to whoever signs in
next, which is worse than losing an offline queue. `OwnerId` is empty on a fresh install and adopts
the first account to write, so somebody who has never signed in keeps everything they played
offline.

---

### 89.8 · Verified

- **Core 164/164** (`dotnet test`), from 135 before this phase. 29 of the new tests are the
  record and profile rules: placements, clutch, idempotency, streaks, comeback denominators,
  history trimming, the sample-size gate and the pressure radius.
- **EditMode 236/236 -> 241/241**, read from `Logs/tests.xml` rather than from the exit code.
  ⚠️ The first run was 235/236: `TheProtocolCarriesEveryRosterBump` caught the `MatchRecord`
  message, which is the tripwire working. Re-armed at 15.
- **All five editor checks OK in one launch** (`Checks.RunAll`), and a clean Windows player
  on the Desktop. `MapGeometryCheck` prints per-prop FAIL lines for Eskinita dressing that
  it does not gate on; the run's own verdict is OK and nothing in this phase touches a scene.
- **`CareerAndCloudCodeTests` is new and reads source as text**, because both faults it looks
  for are invisible to every other test here: a second hand-written Cloud Code request still
  works until it drifts, and nothing on this machine compiles the JavaScript. It gates the
  one-helper rule and pins `PLAYER_COUNT`, `HISTORY_LIMIT`, `APPLIED_ID_MEMORY`,
  `DISPLAY_NAME_MAX` and `DISPLAY_NAME_MIN` in the two scripts to the core constants. **That
  makes § 88.1a a gate rather than a paragraph** asking somebody to remember.
- **`Ugs` category 5/5.** `player-account` answered `{"profile":""}` and `match-record`
  answered `{"applied":false,"profile":""}`, which is the correct answer for a player who has
  never finished a match.
  ⚠️⚠️ **AND THAT PAIR OF ANSWERS TURNED OUT TO PROVE LESS THAN IT READS AS PROVING. § 90.5.**
  Both probe with `load`, which is the branch an ABSENT action falls through to, and Cloud Code
  was stripping the action because the scripts declared no parameters. `submit` had therefore
  never run and no career had ever reached the server, with every probe green throughout. Fixed
  and redeployed 2026-08-30; a parser now fails if a script reads a parameter it does not declare. Both went through `Net.CloudCode`, so the probe now proves the
  transport the game uses.
- **`BotBehaviourProbe` 3/3**, whole matches in both modes on both maps, which is what exercises
  the throw, retrieval, shove and lunge hooks in `Carrier` and `CombatVerbs`.

⚠️ **STILL TO DO ON THIS PHASE, AND IT IS NOT CODE:** the unplugged four-player LAN run, per
§ 89.7. It is a regression to protect, not an open task, and this is the phase most able to
break it. **§ 90.4 is what is automated in its place, and what to check when the four machines
are on the table.** It applies to § 90 as well as to this phase: the protocol is 16 now, so every
machine has to be rebuilt before the run means anything.

---

### 89.9 · Two things found by reviewing this phase rather than by running it

Both were caught before the branch was played, and both are the kind that pass every test and
then fail in front of people.

**The record broadcast would have been silently refused by the transport.** Covered in § 89.5:
it is 2312 bytes, `ReliableSequenced` cannot split a message, and the whole feature would have
worked perfectly in single player and done nothing at all online. The measurement is now a test.

**The mastery list overlapped the stat block on the career page.** Laid out at 660 by 120 in the
bottom-left of the panel, its top edge sat at 188 px from the floor while the last stat row's
box reached down to 156, and its own bottom ran into the REFRESH and CLOSE row. Eighteen
characters plus six heroes is a grid rather than a footnote, so it moved behind a CHARACTERS
button into its own panel, the same shape the match detail already uses. ⚠️ It is also only
rebuilt while that panel is open: a string for eighteen characters rebuilt on every `Changed`
from a screen nobody is looking at is the shape `Hud`'s per-frame rebuild took an eighth of the
probe's frames with.

⚠️ **And the stat block had 14 slots for a 16-row list**, so the two rows appended after the
fact (average time to first throw, distance per round) fell off the end silently, which is
exactly the pair nobody would notice missing. They are in the list proper now and a
`Debug.Assert` fails if the block is ever smaller than what it is asked to write.

---

## 93 · OPEN: a held tsinelas drifts 0.084 m from the hand, and it is not this branch ⚠️⚠️

**Found 2026-08-30 by the first full `-testCategory "!WallClock"` run this branch has had.**

```
CarryTests.AHeldSlipperStaysOnTheArmThroughMovementAndAMissingAnchor
a held slipper drifted 0.084 m from the hand while its carrier walked
  Expected: less than 0.05
  But was:  0.0837778747
```

⚠️⚠️ **IT IS NOT THE UI WORK AND IT IS NOT PHASE 4.** Nothing in either touches `Carrier`,
`CharacterMotor`, the animator, or anything in `LateUpdate`. The only gameplay file this branch
edited is `MatchStatsCollector`, which added one float accumulation to `SampleDistance` and
cannot move a mesh. `git log` on `CarryTests.cs` and `Carrier.cs` ends at § 78's carry-lift work
and at Phase 2's record commit; neither is from this session.

⚠️ **THE TEST'S OWN MESSAGE NAMES THE CAUSE IT WAS WRITTEN FOR:** *"The carry has to run in
LateUpdate: Unity evaluates the Animator between Update and LateUpdate, so a bone read in Update
is the PREVIOUS frame's pose and the slipper trails the hand by one frame of animation."* The
measured 0.084 m is **1.7 times** the 0.05 m bound, which is the size of one frame of arm swing
rather than of a rounding error, so the shape fits that cause exactly.

**What is not known and has to be measured before anything is changed:**

1. **Whether it is a regression or has been red for a while.** Nobody has run a full PlayMode
   sweep on `profile-stats`; every run before this one was `-testFilter`ed to the suite being
   worked on, which is what § 90.8 and § 90.6 record doing. **Bisect it before fixing it**, or the
   fix will be aimed at whichever commit is convenient.
2. **Whether it is timing-sensitive.** `CLAUDE.md` § 7 records `AiDiagnosticProbe` failing at
   21.6 s, 29.9 s and 37.6 s against one bound on an unchanged build. Run it three times before
   believing one number, per § 16's arithmetic.
   ⚠️ **UPDATE 2026-08-30, AND IT ARGUES AGAINST TIMING:** a second full sweep measured
   **0.092 m** where the first measured **0.084 m**, both against 0.05 m. Two samples 1.7x and
   1.8x over the bound is not the shape of a flake, and both sit close to one frame of arm swing,
   which is the cause the test's own message names. **Question 2 is close to answered; question 1
   is not, and is still the one to spend a bisect on.**

⚠️ **DO NOT WIDEN THE BOUND TO MAKE IT PASS.** 0.05 m is a fifth of a hand and the whole point
of the test; `BotBehaviourProbe`'s header has the standing rule about this and it applies here.

---

## 126 · The full PlayMode suite had never been run on this commit, and it was 42 red ⚠️⚠️ 2026-09-03, branch `ui-redesign`

The handoff into this session said it in its own words: *"A full PlayMode suite was never run on
this commit, do that first."* It was run first. **155 tests, 113 passed, 42 failed, in 22
suites**, against a commit whose targeted runs were all green (core 431/431, EditMode 292/292,
`Checks.RunAll` 6/6, `InputSurfaceProbe` 5/5).

⚠️⚠️ **FORTY OF THE FORTY-TWO WERE ONE PROBE'S LEAKED STATIC, AND THE SORTING RULE THAT PROVED IT
TOOK ONE COMMAND.** Every failing suite except two sorts at or after `InputSurfaceProbe`
alphabetically, and the two that do not (`AspectRatioProbes`, `CarryTests`) are genuine and
pre-existing. That is not a coincidence anybody had to be clever about: it is what a leaked global
looks like in a run that discovers its tests in name order.

### 126.1 One probe threw, left the thumb layer switched on, and twenty suites reported other people's bugs

Three faults in a chain, and every one of them is a rule this repository already had.

1. ⚠️ **`InputSurfaceProbe.Measure` held a `Camera` across a scene change.** `Camera.main` is read
   once at the top of the method and the settle frames below it let a screen's own `Start` run; a
   screen that routes onward calls `SceneFlow.Go`, the scene unloads, and the next
   `camera.targetTexture = next` throws `MissingReferenceException`. **Guarded per shape now**, and
   the report says which scene navigated away rather than the probe dying on it.
2. ⚠️⚠️ **THE RESTORE OF `TouchHud.ForceVisible` SAT AFTER THE SWEEP RATHER THAN IN A `finally`.**
   So the throw above left the thumb layer **on for every test that ran afterwards**, on a machine
   that has no touchscreen and where no other test expects one. It is in a `try`/`finally` now.
   `yield return` inside a `try` with a `finally` is legal in a C# iterator, and the `finally` also
   runs when NUnit disposes an abandoned enumerator, which is the case that matters.
3. ⚠️⚠️ **AND WITH THE LAYER ON, `ScreenFocus`'S TOUCH PAD THREW ON EVERY SCREEN.** `TouchHitArea`
   is a `MaskableGraphic`, `Graphic` requires a `CanvasRenderer`, and `AddComponent` did not apply
   that requirement to the subclass, so the pad object came up with a graphic and no renderer and
   the base class threw `MissingComponentException` the moment it tried to draw.

⚠️⚠️ **AND FAULT 3 IS A SHIPPING BUG ON ANDROID, WHICH IS THE REASON THIS ENTRY IS FIRST.** On a
phone `TouchHud.ShouldShow` is **true**, so a pad is built for every control on every screen: the
front end this exception breaks is the one every phone player opens, on the .apk this batch was
about to build. It was invisible on the desktop for exactly the reason that makes it dangerous, a
Windows machine with no touchscreen never builds a pad at all, and it surfaced only because a probe
forces the layer on and then only because that probe leaked it.

**Fixed by construction rather than by remembering:** `[RequireComponent(typeof(CanvasRenderer))]`
on `TouchHitArea`, `typeof(CanvasRenderer)` in the pad's own constructor call, and a third check on
the reuse path for a pad some earlier layout built without one.

⚠️ **The two genuine reds, kept separate on purpose:**

- **`CarryTests`, 0.092 m against a 0.05 m bound.** This is § 93 and it is now on its third
  recorded sample (0.084, 0.092, 0.092). `CLAUDE.md` § 7.1 and § 94.8 both say not to widen the
  bound and nothing in this batch touches `Carrier`, the animator or `LateUpdate`. **Still open.**
- **`AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio`:** a `Label` authored at 14
  units against the 18-unit readable floor.

### 126.2 The thumb floor was unreachable by construction, because padding cannot make room

§ 125.13 left this as *"the converted screens get a taller row on touch, and that test goes
green"*, with the shortfall at **1519 measurements across twelve shapes** and the cause named as a
layout pass. Reading the report rather than the prose changed what the pass had to be.

⚠️⚠️ **EVERY ONE OF THE 1519 REPORTED A SIZE EQUAL TO THE CONTROL'S OWN ARTWORK, WHICH MEANS THE
PAD HAD GROWN BY ZERO UNITS, NOT BY TOO FEW.** `ScreenFocus.ApplyTouchTargets` grows a hit area
only as far as the nearest neighbour allows, and it takes **half** the gap because the neighbour is
growing too. The settings rows are stacked with **no gap at all** (a rebind row's height IS its
keycap's height, in a list with `spacing = 0`), so the clamp came out at zero every time. **The
number was not "not enough padding". It was "there is nowhere to pad into".**

The shape of it, from `Logs/input-surface.txt`, and it is almost entirely one screen and one axis:

| Count | Screen and control | Size | What is wrong |
|---|---|---|---|
| 864 | `SettingsPanel` rebind keycaps | 428x46 | height |
| 96 | `SettingsPanel` sliders | 344x34 | height |
| 48 | `SettingsPanel` checkboxes | 688x34 | height |
| 48 | `SettingsPanel` BACK / RESET ALL | 228x60 | height |
| ~90 | `ModeSelect`, `MultiplayerSetup` primaries | 6xx x101 | height |
| 24 | `MatchSetup` lobby rail | 40 tall | height |
| 20 | `SettingsPanel` scrollbar | 14 wide | width |

**Widths are almost all fine. This is a row-height problem**, which is why it can be answered once
rather than in 79 places.

⚠️⚠️ **SO THE PASS IS `ScreenFocus.MakeRoomForThumbs`, AND IT RUNS BEFORE THE PADDING RATHER THAN
INSTEAD OF IT.** `Rebuild` now makes room, forces a layout rebuild, and then pads. `CLAUDE.md`
§ 4a's answer to every one of these is construction rather than a checklist, and `ScreenFocus` is
installed by `MenuKit.BuildCanvas` and `ConvertedScreen.Start`, which between them are **every
screen in the game**. A screen added next month gets this without anybody remembering.

- ⚠️ **IT GROWS THE BOX A LAYOUT GROUP OWNS, WHICH IS RARELY THE CONTROL.** On the converted
  settings panel the slider's parent is `MasterVolumeRow` and the vertical group is `Content` above
  that, so writing `minHeight` on the slider would have reached nothing at all. `LayoutRowFor`
  walks up to the child OF the group, which is what makes one rule cover the code-built rows, the
  converted rows, the lobby rails and the tab bar together.
- ⚠️ **AND IT ASKS `childControlHeight`, NOT "IS THERE A GROUP".** A group that does not control
  its children's height ignores `minHeight` entirely, so writing one would be a silent no-op and
  this pass would report itself as done having changed nothing.
- ⚠️⚠️ **THE LAYOUT IS FORCED TO REBUILD BETWEEN THE TWO PASSES.** `LayoutElement.minHeight` only
  marks the group dirty; the rects do not move until Unity's next layout pass, and
  `ApplyTouchTargets` reads rects. Without the forced rebuild the padding pass would compute its
  clamps against the rows it had just replaced. It is forced only when something actually moved.
- ⚠️ **168 UNITS, AND TWO BOUNDS AGREE ON IT.** `TouchMetrics.MinTargetUnits` (144) plus
  `MinGapUnits` (24) is what lets two stacked targets both reach the floor with a gap between them;
  144 plus the deepest inset in `UiRows` (22, the stepper arrows) is 166. 168 satisfies both.
  `UiRows.TouchRowHeight` and `ScreenFocus.TouchRowUnits` are the same sum on purpose.
- ⚠️ **THE DESKTOP IS UNCHANGED, BYTE FOR BYTE.** All of it is behind `TouchHud.ShouldShow`, which
  is false on a machine with no touchscreen, so every layout probe photographing this front end at
  the nine desktop shapes measures exactly what it measured before.

⚠️⚠️ **AND THE SCROLLBAR IS EXEMPT RATHER THAN PADDED, WHICH IS A DISTINCTION AND NOT AN EXCUSE.**
`MinTargetUnits`'s own words are *"the smallest a touch target may be"*, and that number is about
how accurately a thumb can PRESS a discrete control. A scrollbar is dragged, it already says where
it is, and on a phone the thing a thumb drags is the list. Held to 144 it would spend a fifth of
the settings panel on a readout. It is **44 units wide on touch** instead (the width
`UiRows.ArrowWidth` already uses, about 3 mm on a 1080-tall phone, against 14 units which is about
one), and `InputSurfaceProbe` skips scrollbars in the floor check and prints them in the report
instead so the exemption is visible rather than silent.

### 126.3 The move stick drew as a square because `Mathf.SmoothStep` is not `smoothstep`

§ 125.13's entry read *"the code now assigns a generated circular `TouchSkin.Ring` sprite instead,
and the last render still shows the square, so the change is either not reaching the Image or the
generated sprite's alpha is being ignored"*, and it pointed the next reader at the sprite:
*"suspect the sprite, not the layout: a null `Image.sprite` draws as a white rectangle"*.

**It was neither. The sprite was there and its alpha channel was flat**, and what settled it was
measuring the render instead of re-reading the code. Sampling one row through the stick's centre on
`Logs/shots-touch/touch-Classic-20-9-phone-v3.png`:

```
x 140..234   (253,174,123)   base only, composited alpha ~0.111
x 235..424   (252,189,146)   base + knob, the knob adding ~0.27
```

140 is `StickCentreX - 190` and 235 is `StickCentreX - 95`, which are exactly the base and knob
half-widths, and **the fill is uniform with no hole in it**. A null sprite draws at full alpha; a
ring has a hole. Neither matches. A uniform partly-transparent square does.

⚠️⚠️ **THE CAUSE IS ONE LINE OF SEMANTICS. `Mathf.SmoothStep(a, b, t)` RETURNS A VALUE BETWEEN
`a` AND `b`; GLSL's `smoothstep(edge0, edge1, x)` RETURNS ONE BETWEEN 0 AND 1.** `BuildCircle` was
written in the GLSL reading:

```csharp
float alpha = 1.0f - Mathf.SmoothStep(outer - feather, outer, r);   // ~0.50 for every pixel
if (!filled) alpha *= Mathf.SmoothStep(inner - feather, inner, r);  // ~0.42 for every pixel
```

With `outer` 0.5 and `feather` 1/128 the first term is always about 0.4961, so the disc came out at
a uniform alpha 0.50 and the ring at 0.21. Measured against the render: 0.50 x 0.85 (the knob
colour's own alpha) x the layer opacity is the 0.27 the knob added, and 0.21 x 0.85 is the 0.111
the base composited at. **The arithmetic reproduces the photograph.**

`TouchSkin.Edge` is a real edge ramp now and carries this note. ⚠️ **The other four
`Mathf.SmoothStep` calls in the project are correct** and were checked before the fix was written:
`SkyEvent`, `VolcanicCooling` and `GhostPetCompanion` all pass an already-normalised 0..1 as `t`,
which is the signature Unity actually has.

### 126.4 The six stored quality levels are asserted now, and the note that protected them is stale

§ 125.14 asked for *"a cheap EditMode test asserting the six levels against `AntiAliasModes`"*.
`AntiAliasModes.QualityLevelSamples` is that table as DATA rather than as prose in a header, and
`QualitySettingsAssetTests` reads the six stored values straight out of
`ProjectSettings/QualitySettings.asset` with a `SerializedObject`.

⚠️ **IT READS THE ASSET RATHER THAN WALKING `QualitySettings.SetQualityLevel`.** Selecting each
level to read it is a WRITE, on the exact field the test exists to protect. The asset read needs no
play session and no editor camera and the three cases run in milliseconds, which is the bound
§ 124.11 says belongs in the forty-millisecond test rather than in a twelve-minute one.

⚠️⚠️ **AND READING THE HEADER PROPERLY FOUND THAT ITS ARGUMENT NO LONGER HOLDS.** It says Ultra is
4 *"so that it matches `Default` ... so matching the two means the ordinary case touches nothing"*,
and that was true when `Default` was index 3, MSAA 4x + FXAA. **`Default` is 1 now, FXAA alone,
whose `Samples` is 0**, changed for the measured tonemap reason further up that file. So the two no
longer match and the protection that sentence describes is not the one in force. The table is kept
at the RENDERING intent rather than bent to suit the default, because `Apply` overwrites the active
level at boot from the player's own setting: **the stored number is never what the game renders
with.**

⚠️ **MEASURED RATHER THAN ASSUMED: a full batchmode PlayMode suite, 155 tests and eighteen minutes
of play, left the asset completely clean.** The write-through the header warns about is an
INTERACTIVE editor behaviour. It is real, and it is not something a batch run reproduces, so this
test is not fragile under a headless gate. ⚠️ **If it ever goes red where nobody edited the asset,
the level that moved names the editor's build target**: `m_PerPlatformDefaultQuality` puts
Standalone on 5 (Ultra) and **Android on 2 (Medium)**, so building the .apk moves which row is at
risk.

### 126.5 Nothing scrolled a focused control into view, and the scrollbar was carrying it

⚠️⚠️ **A PAD WALKING THE SETTINGS LIST SELECTED ROWS NOBODY COULD SEE.** Unity's input module moves
the selection and does nothing about scrolling, and neither `ScreenFocus` nor `UiInputModule` had a
line about it. The settings panel is about forty rows in a viewport showing around ten, so pressing
DOWN eleven times left the highlight below the fold with the list still at the top.

**That is `CLAUDE.md` § 4a's § 96 in a new costume.** `InputSurfaceProbe` check 1 walks
`selectOnDown` and asserts every control is on the path, and every control was: the probe proved
the plate was there, not that anybody could get to it. `InputSurfaceProbe.InsideOwnViewport` exists
to skip exactly these rows, and its note (*"a control scrolled out of its own viewport is not
blocked, it is below the fold"*) is the other half of this bug written down a fortnight early.

`ScreenFocus.FollowSelectionIntoView` scrolls the owning `ScrollRect` when the selection moves.
⚠️ **Once per selection change, never per frame**, because writing `normalizedPosition` every frame
fights the player's own drag and the wheel, and `SettingsWheelProbe` is the test that would find
that the hard way. ⚠️ **And only when the row is actually out of view**: snapping every selection to
the middle makes the list lurch on every press, which reads as the screen fighting the player.

### 126.6 The rebind list is two pages now, and the panel used to show a pad player a list of keys

§ 125.13's first open item: *"`Rebinding` can now answer per device and rebind per device without
disturbing the other, so the data is all there; what is missing is a device toggle on the panel so
a pad player can SEE their own bindings. Done looks like: one control at the top of the rebind list
switching every row between keyboard and pad."* That is what shipped, and building it found three
things the data being "all there" did not cover.

- ⚠️⚠️ **KEYBOARD AND MOUSE IS ONE PAGE, AND THE EXISTING PER-DEVICE OVERLOAD COULD NOT SAY SO.**
  `DisplayNameFor(asset, action, "<Keyboard>")` matches one prefix, so it answers "-" for SPECIAL
  ABILITY and LUNGE, which are `<Mouse>/leftButton` and `<Mouse>/rightButton`: **two of the
  most-used controls in the game would have read as unbound on the page that exists to show
  them.** `Rebinding.PathIsFor` groups by `InputDeviceKind` instead, and the rebind operation gets
  two `WithControlsHavingToMatchPath` calls for the same reason.
- ⚠️⚠️ **THE CANDIDATES HAVE TO BE RESTRICTED TO THE PAGE'S DEVICE OR THE PAGE IS A LIE.**
  `TryRebind` writes the override onto the binding for the device that was PRESSED, so without a
  restriction a player on the GAMEPAD page could press a key, have their keyboard binding silently
  changed, and watch the row in front of them not move.
- ⚠️⚠️ **AND `TryRebind`'S FALLBACK WAS § 125.6 WAITING TO HAPPEN AGAIN.** When no binding matched
  the pressed device it fell back to `indices[0]`, which is the KEY. `ScreenInputCatalogue` records
  a `null` pad path as a written-down answer (`ToggleFullscreen`: a phone has no window), so a pad
  press aimed at such a row would have written `<Gamepad>/...` straight over the keyboard binding,
  which is the exact fault that method already carries a two-paragraph warning about. **It refuses
  now**, and `Rebinding.HasBindingFor` is the polite half that stops the player getting that far.

⚠️ **The page opens on the device the player is holding**, off `LastInputDevice`, which is that
class's own argument reused rather than a second one: *"a player who picks up a pad mid-match has
told you which glyph they want by picking it up."* ⚠️ **Touch is not a third page**: the thumb layer
is not rebound by path at all, it has its own screen reached from a row further down this same
list, and a third tab where one of the three leads somewhere else entirely is what `CLAUDE.md` § 6.2
calls overwhelming.

⚠️ **The live page is relief, not colour**, which is what every other tab strip in this front end
does and what `GodotTheme` and `CustomCharacterScreen` both already argue: a live tab is a
statement about where you are, not a second "press me". It also survives a colourblind player,
which is `FUTURE.md` § 16.1.

⚠️⚠️ **AND THE PAD CAN BACK OUT OF A REBIND WITHOUT A KEYBOARD, WHICH NEEDED A CHANGE TO A GUARD
WHOSE COMMENT EXPLAINED WHY IT COULD NOT BE CHANGED.** The operation cancels through `<Gamepad>/
buttonEast` on the pad page, so `Update`'s *"ESC is the rebind's cancel while one is listening"*
guard was no longer true there and Escape would have done nothing at all during a pad rebind.
`CLAUDE.md` § 6.3 twice: *"Escape backs out on every screen, always, innermost layer first"*, and
*"a player who learns Escape is reliable and then meets one screen where it is not has learned that
it is unreliable."* The panel cancels the rebind itself on that page and consumes the key.

### 126.7 Rumble, on the four moments that change the player's own situation

`FUTURE.md` § 14 asked for *"rumble on knockdown, tag and can reset"* and § 125.13 recorded it as
the one row of Phase 14 that did not ship. `InputLayer.Rumble` is four cues rather than one volume.

⚠️⚠️ **THE FOURTH CUE IS BEING TAGGED, IT IS THE STRONGEST OF THE FOUR, AND IT IS NOT ON THAT LIST
OF THREE.** Being tagged pays the victim nothing, so `Hud.OnScored` says nothing to them at all,
and that file already records what that leaves: the `TAGGED!` toast is *"the only thing on their
screen that explains why they are suddenly somewhere else and cannot move"*, and in first person it
spawns inside their own head. **The moment a player most needs telling is the moment the score
system has nothing to say to them**, which is the gap a haptic is actually for.

- ⚠️ **THE TWO MOTORS ARE DIFFERENT INSTRUMENTS**, so a thump is mostly low and a snap is mostly
  high. One number at four volumes would have made every event the same event, which is
  `docs/VISION.md` § 2 rule 3 about effects applied to a hand.
- ⚠️ **OVERLAPPING PULSES TAKE THE MAXIMUM, NEVER THE SUM**: `CLAUDE.md` § 4's stun rule applied to
  a motor. Two events in the same tenth of a second are ordinary (a tag and a sabotage, a knockdown
  and a reset) and adding them clips both motors to 1.0 and turns two distinct cues into one buzz.
- ⚠️⚠️ **IT FIRES ON THE LOCAL PLAYER'S OWN EVENTS**, which is `Hud.OnScored`'s existing rule for
  the toast rather than a new one. The exception is the can going back up, which changes what
  everybody may do next and is the softest of the four.
- ⚠️⚠️ **THE CAN-RESET CUE IS IN `MatchInstaller`'S LAMBDA AND NOT IN THE HUD**, because that
  lambda is the one owner of `UprightChanged` and `Hud.TrySubscribeRound` carries the receipt for
  what a second subscriber costs: *"one event, two subscriptions, two identical calls, and a toast
  timer restarted mid-fade."*
- ⚠️⚠️ **A MOTOR LEFT RUNNING DOES NOT STOP WHEN THE GAME DOES.** A pad holds whatever speed it was
  last given until something tells it otherwise, so a quit or a lost focus mid-pulse hands the
  player a controller buzzing on their desk. `Rumble.Stop` is called from `OnDisable`,
  `OnApplicationQuit` and a lost focus. **This is the one piece of state in the project that
  outlives the process.**
- ⚠️ **THE COUNTDOWN IS UNSCALED TIME.** 0.24 s is a number about a person's hand, not about the
  simulation, and the probes drive `Time.timeScale`.
- ⚠️ **IT IS SILENT WITH NO PAD, BEFORE THE DRIVER OBJECT IS EVEN CREATED**, which is what makes it
  free in a batch test run: no `Gamepad.current`, no driver, nothing to leak between scenes.
- ⚠️ **AND IT HAS AN OFF SWITCH**, in the CONTROLS list beside the bindings rather than in the
  display group, because that is where somebody looking for a controller setting will look.
  `FUTURE.md` § 16.2 is an accessibility list and a haptic nobody can turn off is on it.
  `GameSettings.Rumble` is a `bool` initialised `true`, which is the safe shape for an upgrade for
  the reason `RenderStyle`'s note gives about `JsonUtility` and field initialisers.

### 126.8 ⚠️⚠️ OPEN: THE FULL PLAYMODE SUITE IS NOT A RELIABLE GATE ON THIS BRANCH, AND TARGETED RUNS ARE WHY NOBODY KNEW

**This is the largest thing this session found and it is not fixed.** It gets its own entry because
every PlayMode number in every handoff in this file is quoted from a targeted run.

**Two full runs, same machine, an hour apart:**

| | Run 1, HEAD `550ba0f`, no edits | Run 2, after the three fixes in § 126.1 |
|---|---|---|
| Result | 155 cases, 113 passed, **42 failed**, 1072 s | 155 cases, 114 passed, **41 failed**, 969 s |
| The `TouchPad` exception | 13 suites | **gone** |
| `InputSurfaceProbe` | failed (destroyed camera) | **passed** |
| `PlayerHubLayoutProbe`, `PhaseSurfaceLayoutProbe`, `QueueCardLayoutProbe`, `SettingsScrollProbe`, `SoloPracticeTests` | failed | **passed** |
| Red in run 2 and not in run 1 | | `MatchRecordIdentityProbe`, `ToneSweep`, `TrainingStreetProbe` x2, `UgsServicesProbe` x6, two more `MatchRunTests` |

⚠️⚠️ **THE COUNT BARELY MOVED AND THE RED SET LARGELY CHANGED, WHICH IS THE FINDING.** Eleven
suites went green and eleven different ones went red. **A gate whose red set moves is not measuring
the code.**

⚠️⚠️ **AND THE EXPERIMENT THAT SETTLES IT COST 105 SECONDS.** The nine suites carrying about twenty
of run 2's failures were re-run together on their own, with `-testFilter`, on exactly the code that
had just failed them:

```
31 cases, 29 passed, 2 failed, 105 s
```

**Twenty failures became two.** The two survivors are real and are § 126.9. Everything else was the
suite, not the code.

**What the reds actually say**, and none of the stack traces are in code this batch touched:

- `MissingReferenceException: the object of type X has been destroyed`, **inside the test**, at
  `SettingsWheelProbe.cs:117`, `SteeringTests.cs:177`, `UiClickProbe.cs:140`,
  `VolcanicZoneTests.cs:60`, and inside `ModelPreview.IsolateFromForeignLights`. Every one is a
  reference the test is holding across a `yield` that something else destroyed.
- *"the arena built no SliceRunner"*, *"No main camera in the arena"*, *"the guided route never
  installed"*, *"MatchSetup has no CharacterSelectPanel to open"*, *"the lobby must have a door to
  the account screen"*. Every one is a scene that did not come up the way the test expected.
- `UgsServicesProbe` x6: *"You are not signed in to the Authentication Service"* and *"The player
  is already signing in"*. That is a **live service and a shared session**, and six went red in run
  2 having passed in run 1 with nothing changed between them that touches authentication.

**So the class is cross-test lifetime leakage**: objects, statics, scenes and one cloud session
outliving the test that made them. § 126.1 is one instance, found and fixed. It is not the only one.

⚠️⚠️ **WHY IT WAS INVISIBLE.** § 94.8 records *"PlayMode, targeted: 15/15"* and then *"11/11 on a
second pass"*; § 125's verification is *"`InputSurfaceProbe` 5/5"*. **Those runs are honest and
they pass.** The suite only comes apart when it is run as one process, which is the one thing
nobody had done on this branch until this session was told to do it first.

**Done looks like** one of these two, and it is a decision rather than a task:

1. **Every PlayMode fixture tears its world down**, so no test can inherit one. That is the real
   fix and it is a pass over every file in `Assets/TumbangPreso/Tests/PlayMode/`. Start with the
   five named above, because their stack traces name the exact line that holds the stale reference.
2. **Or the suite is declared to run in named groups**, the groups go into `docs/TESTING.md` and
   `CLAUDE.md` § 7, and a single-process full run stops being quoted as a gate at all.

⚠️⚠️ **DO NOT CLOSE IT BY WIDENING A BOUND OR BY ADDING A THIRD CATEGORY EXCLUSION.** `WallClock`
and `ThumbFloor` both exist, both are documented gaps with a measured reason, and both name the
thing they exclude. A category meaning "these tests do not work next to each other" would be hiding
this finding rather than recording it.

⚠️ **`UgsServicesProbe` MAY NEED A DIFFERENT ANSWER FROM THE REST**, and it should be decided
separately: it is the only suite whose state lives on somebody else's server, so "tear the world
down" does not reach it. It is also the suite `FUTURE.md` § 0.5 rule 7 cares about most, because a
venue with no internet is the case it is really testing.

### 126.9 The four reds that are about the game rather than about the suite

These survive in isolation. All four are pre-existing on `550ba0f` and none is caused by this batch.

- ⚠️ **`CarryTests`, § 93, now on its fourth recorded sample.** 0.084, 0.092, 0.092 and 0.084 m
  against a 0.05 m bound. `CLAUDE.md` § 7.1 and § 94.8 both say do not widen it, and nothing here
  touches `Carrier`, the animator or `LateUpdate`. **The two samples § 94.8 called "not a flake"
  are four now**, which closes § 93's open question 2 about timing sensitivity: it is not a flake.
- ⚠️ **`AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio`: a `Label` authored at 14
  units** against the 18-unit readable floor, which is 9.3 physical pixels at 720p. `UiRows`'s
  header records three separate attempts at small text in the Godot original, each answered with
  *"text still small"*.

  ✅ **FIXED, and the receipt is that this exact label had already been "fixed" once.** It is the
  Hero picker's cooldown and charge readout (`ConvertedCharacterSelect`), and the comment above it
  says: *"CREAM AT FULL ALPHA, NOT 0.75. 🧑: 'shit down there is small and cant be seent'. This sat
  at 13 pt and three quarters opacity."* **The answer to "make it bigger" was one unit**, 13 to 14,
  on the one label on that screen carrying numbers, and the probe has been failing on it ever
  since. It is `MenuKit.MinReadableUnits` now.

  ⚠️ **AND THE BOX GREW WITH THE TYPE, WHICH IS THE HALF THAT GETS FORGOTTEN.** The 116-unit
  `minWidth` was measured against 14-unit type; at 18 the same string needs 18/14 of the room, so
  it is 150. `MenuKit.Label` is set to Overflow, so a box left at 116 would not have clipped, wrapped
  or reported anything: it would have drawn straight through the ability's name beside it. That is
  the trap `ConvertedScreen.SetHeadline` and `GameVersion.ApplyTo` each record once already.

  ⚠️ **The 13-unit key chip two lines above it is still 13** (`KeyChip`, 26x18 units, showing Q / E
  / F). It was not touched because the probe reports one label at a time and this pass fixes what
  the probe named; **run `AspectRatioProbes` again and it is the next thing you will see.** ⚠️ It
  needs more than a font bump: the chip is 26 units wide and `Hud.KeyLabelFor` returns
  `BUTTON WEST` on a pad, so raising the type without solving the pad label is trading one
  overflow for a worse one. That is the same authored-glyph gap § 125.13 leaves open.
- ⚠️⚠️ **`ModelPreviewTests.HeroCharacterSelectShowsAbilitiesInsteadOfClassicAttributes`, AND IT IS
  A REAL DESIGN FAULT RATHER THAN A TEST THAT NEEDS UPDATING.** The Hero picker draws
  `Seismic Stomp` and `Demonic Carapace` in title case beside `TITAN FISSURE` in upper case, **in
  one three-row panel**. The cause is honest and § 124's own work: the two skill rows show the
  EQUIPPED VARIANT's name (`HeroLoadoutRules`'s `VariantName`, written in title case: *Long
  Tremor*, *Black Ice*) while the ultimate has no variant and falls back to the ability's own name
  (`DanteHeroKit`'s `"TITAN FISSURE"`, upper). Every other surface in the game
  (`AbilityIcons`, `CustomCharacterScreen`) writes an ability upper. **So the picker ships two
  visual languages in one panel**, which is `CLAUDE.md` § 6.5's complaint exactly, and it does it
  hardest on the DEFAULT loadout, where the variant's name is the ability's name in the wrong case.
  ✅ **FIXED, and in the data rather than on the screen.** The picker's own note already stated
  the invariant it depends on: *"they are the same string on a default build, so nothing moves for
  a fresh account and everything is correct for one that has equipped anything."* **They were not
  the same string, and nothing checked.** All 24 `AbilityVariant` names are upper now, which is
  what every other surface in the game already writes (`AbilityIcons`, the `HeroKit` constructors,
  `CustomCharacterScreen`), so the picker draws one voice and the change reaches the loadout board
  and the lobby nameplates for free rather than only the one screen that was photographed.
  **`Core.Tests/AbilityNamingTests` is the check**, in the core rather than in a UI test, because a
  naming convention only a screen enforces is a convention the next screen breaks.

  ⚠️⚠️ **AND THE TEST IMMEDIATELY FOUND A SECOND ONE THAT WAS NOT A CASE PROBLEM AT ALL.**
  Phaister's slot 1 default variant was named **`HEX SIGIL`** while the ability it is a reading of
  is named **`HEX`** (`PhaisterHeroKit`, and `BaseAbility` in the same table row). Three names for
  one power, in three files, with `AbilityIcons` casting the deciding vote for the wrong one. It is
  `HEX` now, which is what the two independent sources already agreed on. ⚠️ **The test does not
  pick the name, it requires that they agree**: renaming the power to `HEX SIGIL` is still open to
  anybody who wants it, and now they have to change the kit and the table together.
- ⚠️ **`SteeringTests.MouseAimedMovementIsRelativeToTheBody`, a 2.6 per cent near miss**: facing
  east, W moved the seat 1.973 against a 2.0246 bound. It is close enough that it could be a tight
  bound rather than a defect, and § 34 is the entry about body-relative steering that would say
  which. **Do not widen it without reading § 34 first**: that section exists because seat 0 once
  travelled 224 m against 522 to 556 for its siblings, and the symptom was exactly this axis.

### 126.10 The .apk was built, installed and run for the first time, and two of its settings were never taking

`docs/FUTURE.md` § 15 step 1 says *"nothing else here means anything until that has happened
once"*, and § 125.13 recorded it as never done. **It is done.**

```
[Build] SUCCEEDED. 1899 MB, 1631s -> C:\Users\Matthew\Desktop\TumbangPreso-Android\TumbangPreso.apk
adb install -r  ->  Success  (72 s, 235 MB)
ApplicationInfo 'com.bhstudios.tumbangpreso', Version '1.0.0', Min API Level '26', Target API Level '36'
Scripting Backend 'il2cpp', CPU 'arm64-v8a', Stripping 'Enabled'
Device Model 'Google sdk_gphone64_x86_64', OS 'Android OS 14 (API 34)'
[Audio] loaded 123 of 123 cues.
TumbangPreso.UI.BootSting:Play()
```

Evidence is in `Logs/shots-android/`: `logcat-unity-v1.txt` and two screencaps. The game boots,
loads every audio cue, plays the boot sting and reaches its own **preparing shaders** screen.

⚠️⚠️ **AND READING THAT LOG FOUND TWO SETTINGS `ConfigureAndroid` WRITES THAT THE ENGINE DOES NOT
ACCEPT. BOTH ARE THE SAME FAULT AS `CLAUDE.md` § 6.4's `ConfigureSplash`, FROM THE OTHER SIDE:
the write is in code, on every build, and it still did not take.**

1. **`minSdkVersion` 24 is REFUSED.** The build printed, as a `Debug.LogError` out of the exact
   line: *"Minimum supported Android API level is 26 (Android 8.0 Oreo). Please use
   AndroidApiLevel26 or higher."* and then carried on and shipped a player whose manifest says
   26. **The file said 24 for a documented reason and the .apk never once had it.** It is 26 now
   and the original reasoning is kept beside it: the floor moved by one year of phones and the
   argument survives that.
2. ⚠️⚠️ **THE x86_64 SLICE IS NOT IN THE .apk, AND THE COMMENT ABOVE IT INSISTS IT IS THE WHOLE
   REASON THE BUILD IS TESTABLE.** `PlayerSettings.Android.targetArchitectures` is set to
   `ARM64 | X86_64` and the shipped file contains **arm64-v8a only**:

   ```
   arm64-v8a        7 files     119.9 MB
   ```

   The comment reads *"x86_64 is not optional here: 🧑 has no Android handset, so an ARM64-only
   .apk could not be run by anybody on this team."* **The .apk IS ARM64-only and it ran anyway**,
   because the Android 14 x86_64 system image translates arm64: the log says
   `CPU 'arm64-v8a'` on a device whose `ro.product.cpu.abi` is `x86_64`, and the loader picked
   `lib/arm64-v8a` out of the package. So the claim was false and the conclusion it protected was
   also false, in opposite directions, and they cancelled out. **Done looks like:** the comment
   rewritten around the measured fact (the emulator translates), and a decision about whether to
   keep asking for a slice Unity 6 does not emit.

⚠️ **THE EMULATOR IS A 1-CORE, GPU-DISABLED, TRANSLATING DEVICE AND IS NOT A PERFORMANCE
MEASUREMENT.** `SystemInfo CPU = x86-64, Cores = 1, Memory = 2474mb`, `hw.gpu.enabled = no` in
the AVD, and every native instruction translated from ARM. Shader warm-up was slow enough that
Android raised its own "isn't responding" dialog over the game's loading screen. **`FUTURE.md`
§ 15 item 3 (performance on device) cannot be answered here**, and a number taken from this
emulator would be worse than no number.

⚠️ **AND A WARNING WORTH ACTING ON BEFORE A REAL PHONE:** *"PlayerSettings->Active Input Handling
is set to Both, this is unsupported on Android and might cause issues with input and application
performance."* `activeInputHandler: 2` is what `FUTURE.md` § 14 records as the reason
`StandaloneInputModule` ran without erroring while no pad binding could reach it. Moving to the
new system alone would break the legacy `Input.GetKeyDown(KeyCode.Escape)` calls still in
`ConvertedSettingsPanel.Update` and elsewhere, so **it is a change with a real blast radius and it
should be made deliberately, on a device, not on the way past.**

### 126.11 ⚠️ NOT DONE: crossplay is still argued rather than demonstrated

§ 125.13's bullet stands, narrowed. Both halves now exist (a Windows player and an .apk from one
tree) and `NetSession.ProtocolVersion` is untouched at 21, so the claim is stronger than it was.
**Nobody has still watched them join each other.** What this session learned that the next one
needs:

- ⚠️ **The .apk has no UGS session on the device.** The first launch logged *"[Social] presence
  not written: Cloud Code is unavailable: no project id or no signed-in session."* A join by code
  goes through UGS Relay, so that has to resolve first.
- ⚠️ **`UgsServicesProbe` went red six times in one full PlayMode run** with *"You are not signed
  in to the Authentication Service"* and *"The player is already signing in"*, having passed in
  the run an hour before. **Whatever that is, it sits between here and a crossplay demo.**
- ⚠️ **The emulator is fragile and the AVD does not survive a hard kill.** Boot once, leave it
  alone, and never `Stop-Process -Force` it: three relaunches after one forced kill hung at the
  same point, and the emulator's own log showed `Failed to find ColorBuffer` and
  `Failed to load opengl32sw` before it crashed. `-wipe-data` did not clear it; a reboot did.

### 126.12 · Verified

- **Core 433/433** (`dotnet test`, 93 ms). 431 plus the two new `AbilityNamingTests`. ⚠️ **Both new
  tests were watched failing first**, on `Seismic Stomp` and then on Phaister's `HEX SIGIL`, which
  is the only way to know a test can fail at all: `Phase10Tests`'s own header records the version
  of itself that *"compared a constant to itself"* and could not.
- **EditMode 295/295** (6 s). 292 plus the three `QualitySettingsAssetTests`.
- ⚠️⚠️ **THE THUMB FLOOR: 1519 SHORTFALLS TO 50, AND THE SWEEP IS COMPLETE RATHER THAN SHORTER.**
  `InputSurfaceProbe` run **alone**: 5 of 6 pass, and the one red is
  `TheFrontEndMeetsTheThumbFloor` with **50 controls** across twelve shapes. The report also shows
  **12 scrollbars exempted** and **zero** *"the camera was replaced part way through the sweep"*
  lines, so this is a full measurement and not a truncated one. ⚠️ **The first run after the fix
  reported 36 and that number was worthless**: the settings panel had destroyed itself on open and
  three scenes had been cut short by the old camera guard, so it measured less and said less.
  **Compare reports by what they covered, not by the count at the bottom.**
- ⚠️⚠️ **AND `EveryScreenHasAFocusPathAndReachableTouchTargets` PASSES, WHICH IS THE HALF THAT
  MATTERS.** That is check 3, *"a press at a control's centre must land on that control"*, and it
  is the check that caught the padding bug in § 125.4. **Making forty rows taller on touch stole
  no presses.** A pass there is worth more than the shortfall count.
- **What the remaining 50 are**, and they are near misses rather than the old class of failure:
  `MainMenuCanvas/QuitButton` at **676x141** (three units short of the floor),
  `LobbyTopRail/BackButton` at **124x144** (twenty short on WIDTH, which is the axis the make-room
  pass deliberately does not touch), and the lobby tab bar at 137x168. **Done looks like** a width
  answer for the lobby rail and three units on the menu pennants; the height problem is solved.
- **Full PlayMode, `!WallClock;!ThumbFloor`: 42 red, then 41 red after the § 126.1 fixes**, with
  eleven suites swapping sides. § 126.8 is that finding and it is the reason there is no honest
  pass count for this suite today.
- ⚠️ **The nine suites carrying about twenty of those failures: 29 of 31 pass when run together on
  their own**, in 105 s. The two survivors are § 126.9's third and fourth bullets.
- ⚠️⚠️ **A TWELVE-SUITE RUN WAS 15 OF 38, AND THAT IS § 126.8 AGAIN RATHER THAN A REGRESSION.**
  Adding `InputSurfaceProbe` to the group is what did it: it loads every scene in the build
  settings and opens every overlay it can discover, so it is the most destructive fixture in the
  suite and everything after it inherits the wreckage. **`InputSurfaceProbe` is run on its own**,
  and the numbers above are from that run.
- **Android: `[Build] SUCCEEDED. 1899 MB, 1631s`**, installed in 72 s, launched, logcat and two
  screencaps in `Logs/shots-android/`. § 126.10.
- ⚠️ **`ProjectSettings/QualitySettings.asset` stayed clean through every one of these runs**,
  which is the measurement § 126.4 rests on.
- ⚠️ **`NetSession.ProtocolVersion` is untouched at 21.**
  `InputContractTests.TheInputPassDidNotMoveTheProtocolVersion` is green, so the .apk and the
  Windows player from this commit will accept each other.

### 126.13 ⚠️ OPEN: what this batch did NOT do

- ⚠️ **Crossplay is still not demonstrated.** § 126.11 has what the next session needs.
- ⚠️ **`AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio` is still red**, and the
  authored 14-unit label § 126.9 fixed was not the only source of a 14. Three
  `MenuKit.Fit(label, room, 14)` calls in `ConvertedCharacterSelect` (lines 592, 1094, 1205) pass
  **14 as the shrink floor**, so a label that does not fit is allowed down to 14 and the probe
  cannot tell that from an authored 14. ⚠️⚠️ **The comment above the first one says
  *"14 AS THE FLOOR RATHER THAN 18, AND ONLY HERE"* and there are three of them**, which is a local
  exemption that was copied twice and never encoded anywhere a test could see. **Done looks like** a
  decision, taken with a render in hand: either the pills get wider and the floor goes to
  `MenuKit.MinReadableUnits`, or the exemption is written into the probe by name so it stops being
  a permanent red. **Do not lower the probe's floor to make it green.**
- ⚠️ **Pad glyphs are still words.** `Hud.KeyLabelFor` answers `BUTTON WEST`, not a face-button
  glyph, and the Hero picker's 26-unit key chip cannot hold either. § 125.13's last row; it needs
  authored art.
- ⚠️ **On-device performance is still unmeasured**, and § 126.10 says why the emulator cannot
  answer it.
- ⚠️ **`UiClickProbe` still carries its hard-coded five-screen list**, which is § 124.11's fault
  pre-installed. Untouched deliberately.

---

## 121 · The v61 report: one material for the primaries, a hub with a tab column, and the stuck hover ⚠️⚠️ OPEN, 2026-09-02, branch `ui-redesign`

🧑 opened the build off `d7731070` and sent fourteen notes in one sitting, with a crop for
almost every one. He also said how he wanted them handled: **"thorouhly plan how to fix
everything btw"**, *"dont js shit out fixes"*, *"dont worry abt the stuff im liosting, im js
listing them"*, and **"i want u to think abt color and visual harmony in makingh fixes"**.

⚠️⚠️ **THIS ENTRY IS THE PLAN AND IT IS WRITTEN BEFORE THE WORK, WHICH IS THE ORDER
`CLAUDE.md` § 6.2a ASKS FOR AND THE ONE § 118.2 NAMES: run `game-ui-design` as a CRITIC first.**
Every row below has a cause in a named file and, where a colour or a distance is claimed, a
number measured off a render rather than an adjective.

⚠️ **`tools/sample_png.js` IS NEW AND IT IS WHY THE NUMBERS EXIST.** Every colour argument in
this file (§ 119.1's road sampling, § 119.10's 1.7:1, § 120.6's channel split of `TUMP.png`) was
produced by a tool that was not in this repository, so no reader could re-run one. It decodes a
PNG with `zlib` and nothing else, because `python` is not on PATH here, and it prints pixels,
scan lines, the commonest colours in a box, and the WCAG ratio between two hexes.

---

### 121.1 ⚠️⚠️ THE ONE FINDING THAT EXPLAINS FIVE OF HIS NOTES: THE PRIMARY IS THE ONLY CONTROL IN THE PAPER FRONT END STILL DRAWN IN WOOD, AND ITS SHADOW IS GREY

🧑, on four different screens, without connecting them himself:

| Screen | What he said |
|---|---|
| Lobby | *"orange outline when i hover over start match is ugly"*, then **"u really have to redesign start match button, it doesnt FEEL like a start match button"**, then the correction that matters: **"i like the size adn color but it feells so flat, it doesn thave start match energy"** |
| Character maker | *"js make buttons prettier"*, **"bcz i dont get why theres rounded sshit next to square shit or wtbv the design of the shit nexxt to it is"** (BACK, a paper pill, beside KEEP AND USE, a chamfered green slab) |
| Character select | *"these buttons look ugly"* (the four-tab row, three surfaces in one rail) |

**Measured off `Logs/shots-runtime/SignInCreate-v56.png` with `tools/sample_png.js`:**

| What | The pixel | Hue | Saturation | Value |
|---|---|---|---|---|
| The cream field | `f4ecdd` | 39 | 9 % | 96 % |
| **A paper control's edge** (`PLAY AS GUEST`, x=227) | `dcc19a` | 35 | **30 %** | 86 % |
| **The green primary's edge** (`CREATE ACCOUNT`, x=161) | `ada69b` | 37 | **10 %** | **68 %** |

⚠️⚠️ **SAME HUE, A THIRD OF THE CHROMA, EIGHTEEN VALUE STEPS DARKER. That is the "grey" he can
see and cannot name**, and it is `CLAUDE.md` § 6.4's rule caught on the warm axis rather than the
blue one: the section forbids *"cold grey"* and a 10 per cent saturation neutral beside a 30 per
cent warm edge is exactly that, whatever its hue reads as in a hex.

**And the silhouette is the other half.** `WoodCraft.Surface.Action` is chamfered and every paper
control is a pill or an 18-unit round, so on the maker's footer a rounded cream pill stands beside
a chamfered green slab with a grey halo. 🧑 named the silhouette clash before anybody measured the
colour, which is § 6.5's *"A CHAMFER MEANS PRESSABLE AND A ROUND MEANS FURNITURE"* read back at us
from the outside: **the chamfer is now the odd one out, because everything else on these screens
became paper and the primary did not.**

⚠️⚠️ **AND "FLAT" IS A THIRD, SEPARATE FAULT ON THE SAME OBJECT, WHICH IS WHY HE SAID IT AFTER
SAYING HE LIKED THE COLOUR.** § 120.1 gave every paper control an eased hover and press:
`PaperButton` lifts the face two units, scales the object 2.5 per cent, sinks the lettering by
`Drop` and takes the cast shadow off, all in unscaled time and faster down than up.
**`LobbyChrome.BuildActionSlot` disables `ArrowButtonView` on the primary and attaches no
`PaperButton`, so the one control on the screen that most needs to feel pressable is the only one
in the front end with no motion at all.** It has `GodotButton`'s sprite swap and Godot's five-unit
label sink and nothing else. *"it feells so flat"* is a measurement of that gap.

**What done looks like:**

1. ⚠️ **`PaperCraft.Surface.Action`: one new construction, in paper's own language.** A raised
   slab built by `PaintRaised`'s rules (lit top edge, 14 per cent ramped wall, squared-falloff
   cast shadow) at primary weight, with the corner radius of a `Token` rather than a chamfer, and
   **a warm shadow derived from the fill rather than a neutral**. The shadow must sit at or above
   the 30 per cent saturation every other paper edge on the screen carries, which is the number in
   the table above and is the whole of what "harmony" means here.
2. ⚠️⚠️ **THE FILL IS A PARAMETER AND § 6.5 SAYS THAT IS HOW SCREENS BECOME ONE SCREEN, SO READ
   WHY THIS IS THE EXCEPTION.** That rule forbids a fill being the ONLY difference between two
   ROLES. There is one role here, `Action`, it appears once per screen by construction, and the
   two fills are both authored: 🧑's green (`UiTheme.MenuGreenFace`, the measured peak of
   `JOIN BUTTON.png`) and the lobby's brown, which he asked to keep by name (*"i like the size adn
   color"*, and § 119.10's *"u can also still use the brown color ... start match lowk looks
   good"*). **Two authored fills, one construction, one per screen.**
3. **Every primary in the paper front end takes it**: `StartButton` / `PrimaryButton` (lobby),
   `KEEP AND USE` (maker), `CHOOSE` (picker), `CREATE ACCOUNT` / `SIGN IN` (login), the hub's
   footer action. ⚠️ **The main menu keeps `WoodCraft` untouched**, which is the scope line § 119
   draws and the reason `WoodCraft` is not being edited.
4. **`PaperButton` goes on all of them**, which is the motion. ⚠️ **`GodotButton` already owns the
   label's position on these nodes** (its own five-unit sink), so `PaperButton` must not write the
   label offset while a live `GodotButton` is present: two owners of one transform property is
   § 119.9 row 1 and it has already shipped once.

---

### 121.2 ⚠️⚠️ THE STUCK HOVER, AND IT IS A CACHE KEY WITH ONE FIELD MISSING ✅ CAUSE FOUND

🧑, with a crop of the lobby's mode tabs, one lit brown and one outlined: **"theres brown ink left
over if i dont hover back to the buttons on top"**, *"i like it but make it so that i dont have to
hover back to buttons on top to get rid of it"*.

**`PaperSkin.Rebuild` keyed its cache on the rect height and the SURFACE and not on the POSE:**

```
if (_built > 0.0f && Mathf.Abs(height - _built) < 2.0f && _builtSurface == Surface) return;
```

`SetPose` clears `_built` to force the repaint, and **`Rebuild` returns without painting and
without recording anything when the rect reports zero height**, which is every frame the control
is inactive. A drawer that closes over a hovered chip is exactly that: the pose write is dropped,
`_pose` still says `Hover`, and the next `OnEnable` repaints from it. **The plate comes back lit,
on a control nothing is pointing at, and only a fresh enter-and-exit clears it.**

⚠️ **THE SURFACE WAS IN THE KEY AND THE POSE WAS NOT, WHICH IS WHY IT LOOKED LIKE A COLOUR BUG.**
`PaperKit.MarkLive` swapping `Live` for `Ghost` always repainted, so the tab row's SELECTION was
never wrong. Only its lighting was, which is why "brown ink left over" is a better description of
it than anything the code says.

**The fix, and it is two lines plus a guard:**

- `PaperSkin._builtPose` joins the cache key, so a pose written against a zero-height rect is
  re-applied the moment the rect exists instead of being forgotten.
- `PaperButton.OnDisable` already resets `_hovered`, `_held`, the scale and the label. **It must
  also put the SKIN back to `Rest`**, because that method's own header says *"a control that is
  switched off mid-hover never gets its `OnPointerExit`"* and it was fixing the transform half of
  that and not the surface half.
- `PaperButton.OnEnable` re-asserts the pose, so a control that was disabled by something other
  than a pointer (a tab rebuild, a drawer) cannot come back mid-animation.

---

### 121.2b ⚠️⚠️ THE AMBER FOCUS RING, WHICH HE REJECTED THREE TIMES IN ONE SITTING ON THREE DIFFERENT CONTROLS

🧑, in order: *"orange outline when i hover over start match is ugly"*, then, with a crop of the
login screen's USERNAME box, **"i dont like the orange outline for a lot of things"**, then the
question that settles the design rather than the colour: **"why do we even have an orange outline
when we hover or select stuff"**.

**It is the keyboard and controller focus indicator, and it was doing two jobs badly.**

| What | The measurement | What it becomes |
|---|---|---|
| It lit on POINTER HOVER as well as on focus | § 120.1 already gives every paper control an eased hover: the face lifts two units, the object scales 2.5 per cent and the cast shadow grows. **A second, louder hover indicator on top of a hover indicator** is `CLAUDE.md` § 6.2 question 3 answered wrongly. | Hover is the pose. The ring lights for real focus only, which is `game-ui-design`'s `missing-focus-visible` and its controller-navigation pattern, neither of which emits a pointer event. |
| It was `UiTheme.Amber` | `ffba00` on `Paper` `f4ecdd` is **1.46:1** (`tools/sample_png.js contrast`). **A high-chroma shape carrying almost no value difference from the sheet under it**: it shouts and it does not read, which is the worst pair of properties a focus indicator can have. | `UiTheme.WoodMid`, which measures **9.20:1** on the same sheet and adds no colour that is not already this front end's ink. |
| It drew a rounded-rect ring around a chamfered slab | Two silhouettes, so the "outline" was a box near a button rather than an edge on one. | Moot on the primary, which is a pill in the same family as everything else now (§ 121.1). |

⚠️⚠️ **IT IS THE SAME INVERSION `PaperCraft.Surface.Live` AND `Surface.Sign` BOTH ALREADY MADE,
ARRIVING ONE CONTROL LATER.** § 118.4 says *amber is the marker*; that rule was written for a
WOODEN front end where amber was the one LIGHT thing on a dark screen. **Invert the field and the
rule inverts with it: on cream the marker is the one DARK thing.** § 119.10 records him rejecting
that ratio by eye on two other controls before this one.

⚠️ **DELETING THE COMPONENT WAS THE WRONG ANSWER AND IT WAS TEMPTING.** A text field has no pose
of its own (`Surface.Tray` is a recess in every state), so without a focus mark there is nothing
at all saying which of two boxes your typing goes into. This is a narrowing, not a removal.

---

### 121.2c The wordmark on the login card: half its own sign was empty brown

🧑: **"improve tump logo integration in lobby too, I like the current setup but it doesnt have
much impact, especially wiht a brown button thats empty like taht"**.

⚠️⚠️ **THE OLD CODE'S OWN COMMENT IS THE CONFESSION AND NOBODY HAD MULTIPLIED IT OUT.** It read:
*"at 420 wide less 2 x 26 of inset the mark is 368 wide and about 106 tall, and 120 units of
plaque less the inset and the six-unit shadow leaves it 62. So the fit is decided by HEIGHT and
the mark draws about 216 x 62 in the middle of the plaque."* **216 of 420 is 51 per cent.** The
game's name occupied half of its own sign and the rest was bare wood, which is exactly what he
photographed.

**A fitter whose box is a different shape from the thing in it always spends the difference as
margin.** `TUMP.png` is 1835x527 (3.482:1) and the box was 368x62 (5.9:1), so the mark was pinned
by height and the width ran away. The fix is to size the MARK and derive the plaque from it, so
the box and the mark are the same shape and the fitter has no slack to spend: **336 units wide
against 216, a little over half again, bought by deleting empty wood rather than by growing the
sign.**

⚠️ **The plaque grew 26 units taller, so `Logo` moves up 13 to hold the 36-unit gap under it.**
That gap is the number he asked for after *"this part looks too tight"* (§ 119.10), and letting it
close to 23 would have put the identity block back on top of the form block.

---

### 121.3 The lobby, five notes

| # | 🧑's words | The cause | What done looks like |
|---|---|---|---|
| 1 | *"these look ugly"*, *"it looks ugly bcz it isnt centered like both of them and theres big empty space"* (the DANTE and SKILLS rows) | **The SKILLS pair is not centred and DANTE is.** `LobbyChrome.BuildSkillsRow` gives the caption a box ending 10 units left of the row's middle and the value a box STARTING at the middle, left-aligned. So the pair spans from `centre - captionWidth - 10` to `centre + valueWidth`: `SKILLS` is about 62 units and `Standard Build` about 130, which puts the pair's own centre **34 units right of the row's**. `BuildCharacterRow` centres `DANTE` properly, so the two rows in one column have two different centre lines. | The caption and the value become ONE centred object (a horizontal group that sizes to its content and centres as a unit), so the pair's centre IS the row's centre and cannot drift when either string changes. ⚠️ The two boxes must still not overlap, which is why they share an edge today; a layout group gives the same guarantee without the arithmetic. |
| 2 | *"also make everything centered (your tier unranked looks ugly bcz it isnt centered"* | `LobbyChrome.BuildTierPlate` draws all three lines `UpperLeft` / `MiddleLeft` / `LowerLeft` while the fighter column beside it and the mode plate above it are centred. **One plate on the rail is aligned differently from every other.** | All three lines centre. ⚠️ The note wraps to two lines and its box is already measured at 64 units for exactly that (see that method); centring changes the alignment and must not change the height. |
| 3 | *"orange outline when i hover over start match is ugly"* | `FocusRing` lights on POINTER HOVER as well as on real focus, and it draws `UiMaterials.Ring(Amber)`, a **rounded-rect** outline, around a **chamfered** slab. Two faults: a silhouette that does not follow the control, and an accent spent on hovering. Amber on `Paper` measures **1.46:1** (`sample_png.js contrast ffba00 f4ecdd`), which is under even the 1.7 § 119.10 records him rejecting by eye. | The ring stops reacting to the pointer and lights only for keyboard and controller focus, which is what `game-ui-design`'s `missing-focus-visible` actually asks for; hover is already said by the pose (§ 121.1 item 4). ⚠️ **The input fields keep their ring**, because a focused text field has no other state and that is the one place the amber outline reads well in his own screenshot. |
| 4 | **"Taya first is ugly and unreadable, too much empty space too"**, *"maybe tighten its box and add outline to Taya first or smth (its okay if player you and taya first boxes doesnt match), js keep everything centered still"*, and **"ALSO i want taya first to be ABOVE the player you, instead of it being button"** | `LobbyNameplates` draws TAYA FIRST as a full-width bar UNDER the name plate, in the same family as a pressable chip. **It is a badge and it is drawn as a button**, which is `CLAUDE.md` § 6.3's *"one that does nothing must not look pressable"* the wrong way round, and it is below the thing it qualifies. | The badge moves ABOVE the plate, sizes to its own lettering rather than to the plate's width, gets a keyline so it reads as a stamp rather than a slab, and keeps its text centred. ⚠️ **It stops sharing the plate's width on purpose and he said so**: *"its okay if player you and taya first boxes doesnt match"*. |
| 5 | **"chat doesnt work at all btw"**, with the drawer open, an empty log and a live `Say something` field | ⚠️ **NOT DIAGNOSED YET AND IT MUST NOT BE GUESSED AT.** § 79.3 has had *"THE LOBBY CHAT STRIP SHOWS NOTHING"* open since 2026-08-29 and this is either that or a second fault on top of it. `LobbyChat.Submit` routes to `MatchRpc.Instance.SendChatServerRpc`, so the first question is whether `MatchRpc.Instance` is non-null in a lobby that has auto-hosted, and the second is whether anything writes the local line when it is null. **Reproduce it in the running player first**, single machine, then hosted. |

---

⚠️⚠️ **AND THE CHAT IS TWO SEPARATE COMPLAINTS, WHICH IS WHY IT IS NOT ONE ROW.** Besides
**"chat doesnt work at all btw"** he sent, of the same drawer, **"also chat is awkwardly placed,
it looks very ugly"**. One is a function that does not run and one is a composition; fixing either
does not touch the other, and shipping the second without the first would be a beautiful drawer
that still does nothing. **Reproduce the function fault in the running player first**, because
§ 79.3 has had *"THE LOBBY CHAT STRIP SHOWS NOTHING"* open since 2026-08-29 and this is either
that entry or a second fault standing on it. His crop shows the well empty and the `Say something`
field live, which is the same picture § 79.3 describes.

⚠️ **The placement half has a measurement already**: § 118.1 row 1 records the well as *"about 70
units tall to hold one 18-unit line, and the line sits at its BOTTOM because lines fill upward, so
two thirds of it is empty by construction"*. That was written before the drawer existed and the
arithmetic survived the move into it.

---

### 121.4 The login screen, two notes, and the first is not what it looks like

- **"sign in isnt centered"**, with a crop of the two tabs. ⚠️⚠️ **MEASURED ON `SignIn-v56.png`
  AND IT IS CENTRED THERE, WHICH MEANS THE MEASUREMENT HAS TO BE RETAKEN ON THE BUILD HE IS
  ACTUALLY LOOKING AT.** The pill spans x 176 to 365 (centre **270.5**) and the lettering's ink
  spans 242 to 300 (centre **271**): half a pixel. **But v56 predates § 120.3**, which moved
  `SignInScreen.SetTab` onto `PaperKit.MarkLive` and therefore changed the idle tab from `Token`
  to `Ghost` and the live one to `Live` at a different height (`LiveTabHeight` 60 against
  `IdleTabHeight` 52). His crop shows the `Live`/`Ghost` pair, so it is the newer geometry.
  **The first action is a fresh shot at `v61` and the same two scans**, not a fix.
  ⚠️ `PaperKit.CentreOnFace` is the likely suspect and § 120.2 is the receipt for why: two
  correction sites that move a label six units in opposite directions already shipped once on
  BACK, and a `Live` surface has a `Drop` a `Ghost` does not.
- **"ugly ass empty space here"**, *"cant u js use the left side as space like"*, **"like use this
  whole space for login"**, with the card cropped and then a second crop of the card enlarged into
  the space beside it. The card is **557 units wide against a 1920-unit canvas**, sitting at
  `CardMargin` 96 from the left edge, and § 120.6 has just sized its HEIGHT to its content while
  leaving its WIDTH at `ColumnUnits` 560. So the form is a narrow strip with the cast beside it,
  and inside the strip the pitch between blocks is 120 units, which is where the vertical holes he
  is pointing at come from.
  ⚠️⚠️ **THIS REVERSES § 100 AND THE REVERSAL IS HIS, SO READ § 100 BEFORE TOUCHING IT.** That
  entry cut the column from 38 per cent of the window to 580 units *"which is the form plus one
  margin either side"*, because 860 units of wood around a 420-unit form was swallowing the key
  art. **The answer is not to stretch the form back out to 1200**, which is § 94.7 fault 6 exactly
  (the widest control becomes the loudest thing on the screen). It is to give the wider card
  something to PUT there: the wordmark and one line of purpose on one side, the tabs, the two
  fields and the primary on the other, so the space is filled by content rather than by a
  stretched text box.
  ⚠️ **And the key art must be re-fitted in the same commit.** § 6.2c question 2: the picture's
  frame ends where the opaque thing starts, and `BuildKeyArt` currently envelopes the WHOLE canvas
  on the argument that the card floats over it. A card that takes half the screen makes that
  argument false again, which is the exact fault § 100 recorded as *"the art is cut off"*.

---

### 121.5 The character select, three notes, and one of them is the blue arrows

- **"these buttons look ugly"** (the `HERO` / `LATA` / `TSINELAS` / `MAKE YOUR OWN` row). Three
  surfaces in one rail: `HERO` is a `Live` pill, `LATA` and `TSINELAS` are `Ghost` outlines, and
  `MAKE YOUR OWN` is a filled `Token` with no outline, at a smaller type size because it is the
  longest string and `MenuKit.Fit` shrank it. **Four controls that do the same kind of thing must
  look the same** (§ 117's whole complaint), and the fourth is not even the same KIND: it is a
  door out of this screen sitting in a row of tabs within it.
- **"this looks ugly"**, *"i think it can be improved by using diff background"*, **"this used to
  be amazing when it was brown only and the background corresponded to their color"**, then, of
  the version that does tint: *"yea see this doesnt look great"* (NEMU, whose wash is purple).
  ⚠️⚠️ **BOTH NOTES ARE TRUE AND THEY ARE THE WHOLE COLOUR PROBLEM ON THIS SCREEN.** He wants the
  backdrop to respond to the character; he does not want the character's own hue painted across a
  cream sheet, because six heroes' colours include a purple and a magenta and **the front end has
  four hues in it on purpose** (§ 119.1, § 118.4: *"do not add a fifth hue"*). The resolution has
  to be a treatment that varies by character WITHOUT importing an arbitrary hue: vary the
  backdrop's VALUE and its warmth within the paper family and let the character's colour appear as
  a low, contained glow behind the model rather than as a full-screen wash. **Value is an ordering
  tool and hue is the last one**, which is the same inversion § 119.10 records for amber.
- ⚠️⚠️ **THE ARROWS ARE BLUE AND THEY ARE NOW ON A CREAM SCREEN, WHICH IS THE CONTEXT § 120.7
  SAID TO LOOK AT.** Measured on `Assets/TumbangPreso/Art/ui/host-game/Arrow Left 64.png` (which
  is 27x39, not 64x64): of the opaque pixels, about **70 per cent `ffffff`** and about **30 per
  cent the `80bad9` family, hue 201 at 41 per cent saturation.** Against a field at hue 39 they
  are the only cool object in the front end, and they sit beside `DANTE` in his own green.
  **Both of his rules still disagree and § 120.7 is still right that only he settles it, but there
  is now a third option that was not on the table then**: the same decision the lobby already made
  twice, which is to stop DRAWING one of his files on the paper screens while leaving the file,
  the main menu and every other use of it untouched. A warm arrow drawn from his measured
  silhouette is not a repaint of his art; it is the treatment `ConfigPanel` and `MAP MODE DISPLAY`
  already got in § 120.4. ⚠️ **Do not runtime-tint the PNG**, which § 120.7 rules out by name and
  which would multiply his white down to tan and his blue to mud.

---

### 121.6 The player hub, which is § 120.7's own open item and the largest piece

⚠️ **THIS IS NOT ONE OF HIS NOTES. It is what § 120.7 named as unfinished** and what § 119.5
planned from the start: *"an ID card with a tab COLUMN rather than a tab row ... six tabs across a
header is the row that made § 92 unreadable"*.

Measured off `LobbyAccount-v56.png`, and the shape of it survives § 120's repaint: six 168-unit
tabs run across the screen at y = -182, the list starts 232 units below the top, and **on the
PROFILE tab of a fresh account the bottom 45 per cent of the screen is bare cream**. That is
§ 6.2's *"big ass empty sopace"* on the one screen in the game that is entirely about the player.

**What done looks like:**

1. **The navigation becomes a column down the left**, inside an ID card that carries the handle,
   the account state and the XP block above it. One object: who you are, where you can go.
2. **The content region takes the rest** and gets a page of its own, so an empty tab reads as a
   page with room on it rather than as a screen with a hole. ⚠️ Narrowing the list also pulls
   `UiRows.ValueColumn` in with it, which shortens the label-to-value journey § 94.7 fault 1
   measured at 1600 px; the value column must stay above 368 units at the narrowest shape, which
   is the number `UiRows.Cap` records and the reason every control in that file is under it.
3. **Every empty state is designed rather than left short**: a fresh career, an empty match
   history, no friends, and a guest account.
4. ⚠️⚠️ **NOTHING IS LOST.** § 119.3's inventory for the hub is CLOSE, six tabs, the footer
   action, the detail view and its BACK, and every `UiRows` row inside all six. The detail popup
   in particular exists because deleting `ProfileOverlay` would otherwise have deleted a shipped
   feature (§ 92.4), and a redesign that quietly loses a screen is a regression wearing a better
   layout.
5. **`PlayerHubLayoutProbe` presses tabs by their lettering**, so the labels may not change, and
   it drives nine resolutions, so nothing here may be a hand-written offset that is correct at one
   of them.

---

### 121.7 What this pass must NOT do

- ⚠️ **Not the main menu and not the in-match HUD.** Scoped out three times now (§ 118.4, § 119,
  and *"except for main menu and actual game for now"*).
- ⚠️ **Not `WoodCraft`.** It draws the main menu and the match. § 121.1 adds a surface to
  `PaperCraft` instead, which is the file that owns the paper front end.
- ⚠️ **Not a fifth hue, and no blue, navy or cold grey in any layer.** § 6.4. The measurement in
  § 121.1 is that rule applied to a shadow nobody had thought to sample.
- ⚠️ **Not a re-baseline of `CarryTests`.** § 93, § 117.8, § 118.4, § 120.9. It is unrelated
  gameplay work and it is red for a reason that is written down three times.
- ⚠️ **Not a chase of `146/146`.** § 120.9 classifies the open PlayMode failures and two of them
  are a documented design conflict rather than a defect; § 121.8 is where that decision gets made
  on evidence rather than on the assertion's say-so.

---

### 121.8 The 16-unit caption question, which § 120.9 left open on purpose

`PaperKit.Caption` is **16** and `MenuKit.MinReadableUnits` is **18**, `PaperKit`'s header states
the conflict as a deliberate decision, and two probes encode the floor as an assertion that cannot
see the argument.

⚠️⚠️ **IT IS SETTLED BY LOOKING AT THE RUNNING BUILD AND NOT BY EITHER FILE WINNING ON PAPER.**
The measurable half is contrast, and it is already good: `PaperInkSoft` on `Paper` is **5.21:1**
and `PaperInk` on `Paper` is **12.34:1** (`sample_png.js contrast`). The unmeasurable half is
whether 16 units is legible at the size he plays at, and the answer differs per screen: a caption
under a `Title` value is a restatement, and a caption that is the ONLY place a fact appears is
not. **Walk every screen that uses one** (sign in, the queue card, the hub, match settings, the
picker, the maker, the lobby drawers) and split them into those two groups before changing a
constant, because raising the constant grows every caption in the front end by an eighth and
`MenuKit.Fit` cannot rescue an overflow below the same floor.

---

### 121.10 ⚠️⚠️ WHAT THE RENDERS CHANGED, AND THREE OF THESE REVERSED A DECISION TAKEN ONE RENDER EARLIER

**This is the section to read before trusting anything above it**, because the plan in § 121.1 to
§ 121.6 was written from crops and four of its calls were wrong in a way only a fresh picture
could show. `CLAUDE.md` § 6.5's closing line, *"take the picture, then take it again"*, is the
whole of this pass.

| # | What the plan said | What the render said | What it is now |
|---|---|---|---|
| 1 | The primary's chamfer is the odd one out, so make it a pill | ⚠️⚠️ **THE CHAMFER WAS NEVER THE FAULT.** `Logs/crops/start-cap-v61.png` at 6x: **two objects stacked** — a new `Action` pill on the node's own Image and 🧑's chamfered `Artwork` child drawing straight over it. That is what he photographed as *"its a circle and a sharp shape at the same time"*, and it is what "rounded shit next to square shit" meant on the maker's footer too. | The child graphics go (`PaperKit.MakeAction`) and the surface is **chamfered again**, on his instruction after seeing both: **"i kinda preferred the sharper edges on this, i js wanted u to make it mroe 3d"**. `CLAUDE.md` § 6.5 is back the right way up: one chamfer per screen, and it is the one action. |
| 2 | Give the primary depth | *"this still looks ugly, especially the shadow"*, *"it feells so flat"* | The face ramp went from a 34-point value spread to **54**, the wall from 16 per cent of the face to **22**, and the cast shadow from a squared falloff over ten units to a **cubed** one: two thirds of the alpha inside the first third of the drop. A blur became a contact shadow. |
| 3 | `Ghost` is the right idle for a tab | ⚠️⚠️ **A TAB ROW WAS TWO SILHOUETTES.** `Logs/crops/picker-tabs-v61.png`: `HERO` a pill, `LATA` and `TSINELAS` 18-unit rounded rectangles, `MAKE YOUR OWN` a pill. `PaperCraft.Surface.Live`'s own note forbids exactly this in writing (*"Same pill ... Giving the selected tab its own shape would say 'these two controls are different KINDS of thing'"*) and nobody had checked it from the idle side. | `PaperKit.MarkLive`'s default idle is `Token`. ⚠️ **AND THERE IS A SECOND DECIDER**: `PaperDress.ButtonSkin` maps `WoodTabIdleButton` to a surface as well, and it runs AFTER `PlayerHub.Highlight` on every tab press, so the hub's column stayed `Ghost` while the picker's row changed. **Both had to say `Token`.** |
| 4 | One size for every cell in the picker's tab rail | 22 units **overflowed** `TSINELAS` and `MAKE YOUR OWN` past their own pills. The three tabs get about 124 units of cell from the `HorizontalLayoutGroup` and the door about 187; `MenuKit.WoodButton` fits its label to the size it is HANDED, which is the 180 and 300 passed in and discarded a frame later. | The size comes down and every cell is re-fitted against the rect the layout group actually gave it, which is the two-step `BuildCustomDoor` already documented and the other three cells never did. |
| 5 | The hub's identity block is a fixed height | On a fresh account it drew a name, a sentence, a rule and then **79 units of nothing**, because the XP guards empty the words and the space was still reserved. 🧑: **"thhis looks really good just tighten it, i dont want huge empty space"** | The block has two heights and `RefreshHeader` picks, and the card is content-height rather than full height. |
| 6 | (not planned) | `LobbyAccount-v61.png`: the account line reads `PLAYING ON THIS MACHINE ONLY · no` and stops. A 44-unit box, a 46-character sentence at 18 units in a 368-unit rail, and `verticalOverflow = Truncate` **drops whole lines in silence**. | Three lines. ⚠️ Truncate is still right; the fix for a truncation is a box that fits the sentence, not a box that lets it escape. |

⚠️ **`tools/sample_png.js` GREW A `crop` MODE FOR THIS AND IT IS WHY ROW 1 WAS FOUND AT ALL.** A
1920x1080 render shown at chat size draws a 500-unit control's keyline about one pixel wide; the
stacked silhouette was invisible in the full frame and unmistakable at 6x. **`CLAUDE.md` § 6.1
says show, do not describe, and a picture too small to read is a description.**

---

### 121.11 What is NOT done, named rather than left implied

⚠️ **EVERY ITEM HERE IS ONE HE RAISED, SO NONE OF THEM IS A NICE-TO-HAVE.** They are open because
they need either a decision only he can make or a reproduction that batch mode cannot produce.

- ⚠️⚠️ **THE CHAT DOES NOT WORK AND IT IS NOT DIAGNOSED.** 🧑: **"chat doesnt work at all btw"**,
  and separately **"also chat is awkwardly placed, it looks very ugly"**, and
  **"can u figure out where to put chat and hwo to make it work bcz it s lowkey ugly"**. Read this
  far and no further, because the reading is not finished: `LobbyChat.Submit` is wired to
  `InputField.onSubmit`, and on a lobby with no peer `MatchRpc.SendChatServerRpc` returns false and
  `AddLocal` is supposed to push *"Not connected. That line was not sent."* onto the log. **His
  screenshot shows an empty log**, so either the submit never fires or the push never draws, and
  those are different bugs in different files. ⚠️ **The host path has its own echo**
  (`HostRelayChat` ends with `OnChatLine?.Invoke`, because `SendNamedMessageToAll` loops back and
  `OnChatLineMsg` refuses the host), so "the sender never sees its own line" is ruled out for a
  host and NOT for a client. **Reproduce it in the player before writing anything**, and a probe
  that types into the field and photographs the result is the cheapest way to make it repeatable.
- ⚠️ **THE LOGIN CARD STILL DOES NOT USE THE SPACE.** § 121.4 has the measurement and the three
  quotes. It is unstarted rather than half-done, deliberately: § 100 is a whole pass spent undoing
  a guess about this exact rectangle, and the two readings of *"use this whole space"* (a wider
  floating card, or a full-height panel bled off the left edge) produce different screens and
  different key-art crops.
- ⚠️ **THE PICKER'S BACKDROP DOES NOT RESPOND TO THE CHARACTER.** § 121.5 has both of his notes and
  they pull against each other: *"this used to be amazing when it was brown only and the
  background corresponded to their color"* and, of the version that tints, *"yea see this doesnt
  look great"*. The resolution written there (vary VALUE inside the paper family and let the hero
  colour in only at low chroma) is a design, not a change.
- ⚠️ **THE ARROWS ARE STILL BLUE**, and § 121.5 has the fresh measurement: about 30 per cent of
  their opaque pixels are hue 201 at 41 per cent saturation, on a field at hue 39. **Both of his
  rules still disagree** and § 120.7 is still right that only he settles it; what is new is a
  third option that does not repaint the file.
- **TAYA FIRST is above the plate and centred and is still the plate's width.** He asked for the
  box to be tightened to its own lettering and said the two boxes need not match; that half is
  done in position and not in size.
- **The 16-unit caption question (§ 121.8) is answered on one control and not as a policy.** The
  lobby's SKILLS row is one size now because he named it (*"these diff fonts look ugly"*); every
  other `PaperKit.Caption` in the front end is untouched and `PlayerHubLayoutProbe` and
  `QueueCardLayoutProbe` still fail on the sign-in screen's and the queue card's.
- **The hub is photographed on a FRESH account only.** § 121.9 asks for a populated one and for
  the long-name state; the long name is shot and the populated career is not, because a probe
  cannot mint a career without writing one.
- **No non-host client pass.** Every shot in this pass is a host that auto-hosted on load.
- ⚠️⚠️ **THE LAN ADDRESS IS STILL TRUNCATED IN THE HOSTING DRAWER, AND FIVE ATTEMPTS DID NOT MOVE
  IT.** `Logs/crops/address-final4.png`: the tray reads `25.3.149.221:8` and the rest is behind
  the COPY button. The row's COLUMNS are fixed (§ 121.10 and `ShareCaption`) and this is the
  remaining half. **What is known:** there is exactly one writer (`ConvertedMatchSetup` line
  ~3000), it demonstrably runs, and changing `fontSize` there produced **no visible change across
  four renders**, with and without a `MenuKit.Fit` after it, with the fit against the rect and
  against a computed width. **So something else is deciding this label's size and it is not that
  line.** Do not tune the number again; find the second writer first. ⚠️ COPY puts the whole
  string on the clipboard, so the feature works and the display does not, which is why this is a
  defect rather than an outage.

---

### 121.9 Acceptance

- Every screen re-shot at `v61` and a person looks at the picture. ⚠️ `UiRuntimeShots.ShotVersion`
  bumps once per iteration, not once per pass.
- **The hub gets shots it has never had**: all six tabs, on a FRESH account and on a populated
  one, plus a long display name.
- `PaperPurityProbe`, `LobbyStyleProbe`, `PlayerHubLayoutProbe`, `UiClickProbe` and
  `AspectRatioProbes` green; Core, EditMode and `Checks.RunAll` green.
- ⚠️ **The five failures § 120.9 names are re-classified against a run rather than inherited**,
  and the splash-shader one gets the confirmation against `9c85c2f` that § 120.9 says it never
  got.
- A clean Windows player on the Desktop, built after every gate above.

---

## 119 · The whole front end is repainted in PAPER, and the lobby is rebuilt around the room ⚠️⚠️ OPEN, 2026-09-01, branch `ui-redesign`

🧑, with a crop of the lobby, a crop of the join panel, `Art/ui/TUMP.png` and a two-swatch card:
*"game reads as too brown bcz the game itself is brown already (the map and shit)"*, *"Look at the
logo, pic 3"*, *"can we remodel the color of all UI for lobby and login to look like this?"*, *"i
want us to play around the 2 colors i attached"*, and then, widening it three times in a row:
*"not just color overaul u can genuinnely overaul the whole thing bcz its ugly"*, *"u can overhaul
the wole lobby and login bcz its ugly as fuck ... it feels overwhelming and not nice to look at as
a user"*, **"redesign teh whole ass UI (dont touch the camera and shit tho) ... ur goal is to make
it inntuitive and easy for user to traverse and calming. I DONT WANT it to be overwhelming for
htem"**.

⚠️⚠️ **HE ALSO GAVE THE PERMISSION § 118.4 SAYS THIS NEEDS, IN ADVANCE AND BY NAME:** *"i think
handoff says u cant recolor and shit but i give u permission to overhaul"*, and
*"OVERHAUL UI FOR EVERYTHING IN LOBBBY INCLUDING AS WELL EVERYTHING U CAN CLICK IN LOBBY LIKE
CHARACTER SELECT, CHARACTER MAKING, SETTINGS, ETC EVERYTHING (except for main menu and actual game
for now)"*. **The main menu and the in-match HUD are still out of scope and so is the lobby
camera.**

### 119.1 ⚠️⚠️ THE DIAGNOSIS IS HIS AND IT IS MEASURABLE: THE UI AND THE WORLD ARE THE SAME COLOUR

*"game reads as too brown bcz the game itself is brown already"*. Sampling
`Logs/shots-runtime/Lobby-v51.png`: Eskinita's road, houses, poles and fences sit at **hue 18 to
40, saturation 30 to 60 per cent**. `UiTheme.WoodFace` `793e1f`, which is every panel, rail, card
and toggle on that screen, is **hue 22 at 74 per cent**. Every surface in the front end is
therefore a slightly darker version of the picture behind it, and the only thing separating the
two is the keyline. **No amount of bevel, grain, varnish or composition fixes that**, which is why
§ 116, § 117 and § 118 each improved the screen and each left him saying it still looked wrong.

⚠️ **`f4ecdd` AND `efdabe` ARE NOT A FIFTH HUE.** Both are hue 34 to 38 at 6 to 20 per cent
saturation, one step off `UiTheme.Cream` `f5e6c8`, which has been in the palette since
`ui_theme.gd`. `CLAUDE.md` § 6.4 is intact: no blue, no navy, no cold grey, and the wood, amber,
green and ink are all unchanged. **What changes is which member of the palette is the FIELD.**
Paper is the surface now; wood is the ink, the frame and his own authored buttons standing on it.

⚠️ **AND THE SWATCHES ARE ALREADY IN HIS OWN MARK.** `Art/ui/TUMP.png` is white lettering with a
sand halo on a linen field; the linen samples `f2ead9`, within a point of `f4ecdd` on every
channel. The palette was on screen in the game logo before he sent the card.

### 119.2 The two new files, and why they are new files rather than an edit

| File | What it is |
|---|---|
| `Runtime/UI/PaperCraft.cs` | Five CONSTRUCTIONS in cut paper: `Sheet`, `Token`, `Tray`, `Ghost`, `Sign`. Each differs in silhouette and relief, not only in fill. Plus `PaperSkin`, the rect watcher, which **destroys any `WoodSkin` on the same node**. |
| `Runtime/UI/PaperKit.cs` | The atoms and the four-step type scale (44 / 26 / 20 / 16), one `Gap` of 12, and `PaperButton`, the pose driver. **Deliberately a kit of parts and not a screen builder**, because a shared `BuildPanel(title, rows)` is exactly how five screens become one screen five times. |

⚠️ **`WoodCraft` IS NOT DELETED AND MUST NOT BE.** It draws every wooden control in the main menu
and in the match, neither of which this pass may touch, and it is the transcription of his
authored art. The two materials share `WoodCraft.Depth` and `WoodCraft.Finish` (made `internal`)
so the two cannot pick up different corner anti-aliasing.

### 119.3 ⚠️⚠️ THE CONTROL INVENTORY. NOTHING ON THIS LIST MAY DISAPPEAR

🧑, twice: *"MAKE SURE EVERYTHING U REPLACED IS ACCOUNTED FOR AND WE DONT LOSE BUTTONS"*, and
*"Im so worried ull leave old UI int he shhit and itll be a mess ... as well as forget UI"*. This
is the answer to the second half, and `PaperPurityProbe` (§ 119.6) is the answer to the first.

**LOBBY** — the converted node names `ConvertedMatchSetup` resolves by name. Renaming or dropping
any of these breaks the wiring silently, which is what the `LobbyChrome` header warns about:

`BackButton` · `StartButton` · `PrimaryButton` · `SeatButton0..3` · `SeatHeading` · `SeatHint` ·
`CharacterButton` · `CharacterSelectPanel` · `MapPreview` · `MapValueLabel` · `ModeValueLabel` ·
`DifficultyValueLabel` · `DetailLabel` · `MapPrevButton` · `MapNextButton` · `ModePrevButton` ·
`ModeNextButton` · `ModeRow` · `DifficultyPrevButton` · `DifficultyNextButton` · `FormatPrevButton`
· `StatusLabel`

**LOBBY** — the controls built in code:

| Control | Lives in | Where it goes now |
|---|---|---|
| BACK | `LobbyChrome.LiftBack` | top rail, far left |
| PRACTICE / MULTIPLAYER tabs | `LobbyChrome.BuildTabs` | top rail, centre |
| YOUR PROFILE door | `LobbyChrome.BuildProfileButton` | **top rail, far right** (🧑: *"i tink te profile screen should be more up instead of being below character select"*) |
| player name field | `LobbyChrome.BuildNameField` | the hub PROFILE tab behind that door |
| CHARACTER door | `LobbyChrome.BuildCharacterButton` | bottom rail, left column, row 1 |
| LOADOUT door | `LobbyChrome.BuildLoadoutButton` | bottom rail, left column, row 2 |
| ROOM CODE + tap to copy | `LobbyChrome.BuildRoomSign` | bottom rail, right column, on the `Sign` surface |
| MATCH SETTINGS toggle + summary | `LobbyChrome.BuildLeftRail` | bottom rail, centre column, above the primary |
| MAP / MODE / BOTS / RULES dropdowns | `ConvertedMatchSetup.BuildSettingsDropdowns`, `WoodDropdown` | the settings drawer, which opens UPWARD out of the rail |
| START MATCH / READY / CONNECTING | `StartButton`, `PrimaryButton` | bottom rail, centre column: the one thing on the screen |
| QUICK MATCH + queue card | `QueueCard` | a drawer above the right column, never a floating corner card |
| JOIN / SERVERS | `LobbyJoinPanel`, `_joinButton`, `_onlineButton` | one chip in the right column, opening the rebuilt takeover |
| CHAT | `LobbyChat` | one chip in the right column, opening a drawer |
| SPECTATE | `_spectate` | the seat plates: a free seat is pressable and says so |
| SECURE YOUR PROGRESS | `LobbyChrome` footer link | a chip only while the account is a guest |
| version stamp | `VersionStamp` | on the top rail, not floating over the road (§ 118.1 row 8) |

**LOGIN** (`SignInScreen`): SIGN IN tab · CREATE tab · username · password · primary
(SIGN IN / CREATE ACCOUNT) · CONTINUE WITH GOOGLE · PLAY AS GUEST · BACK · the WELCOME BACK state
CONTINUE and SIGN IN AS SOMEBODY ELSE · the footer key hints. **Eleven controls across three
states, and § 6.2b first row is why all three get photographed.**

**JOIN A GAME** (`LobbyJoinPanel`): code/IP field · JOIN · six browser rows · BACK TO LOBBY ·
LEAVE GAME.

**CHARACTER SELECT** (`ConvertedCharacterSelect`): `CharPrevButton` · `CharNextButton` ·
`CharValueLabel` · `CharacterPreview` · `TabBar` · `TraitRows` · `NameRow` · `NameCaption` ·
`TaglineLabel` · `ConfirmButton` · `BackButton` · `BackdropGlow`.

**CHARACTER MAKER** (`CustomCharacterScreen`): SLOT 1..3 tabs · SURPRISE ME · PRESETS · BACK ·
KEEP AND USE · the ten wardrobe categories · the colour dial.

**SETTINGS** (`ConvertedSettingsPanel`): `ApplyButton` · `BackButton` · `ResetAllButton` ·
`FullscreenCheck` · `PlayerNameField` · `BindingsList`.

**HUB** (`PlayerHub`): CLOSE · six tabs (PROFILE, FRIENDS, LOADOUT, CAREER, MATCHES, ACCOUNT) ·
the footer action (SAVE / REFRESH) · the detail view BACK · every `UiRows` row inside them.

### 119.4 The lobby new composition: one room, two rails

⚠️⚠️ **THIS IS § 118.1 ROW 2 ANSWERED STRUCTURALLY RATHER THAN BY FILLING THE HOLE.** That row
measures 680 units of empty screen on the left and 475 on the right, between a top band and a
bottom rail, with four corners and nothing between them. The answer is not more furniture in the
middle; it is **two full-width rails and a middle that is only the room**, which is the mechanism
§ 118.3 credits to Rocket League and Overwatch and which Fall Guys uses for exactly this screen.

```
+----------------------------------------------------------------------+
|  < BACK        PRACTICE  .  MULTIPLAYER            [ YOU  > ]   v1.00 |  top rail, 88
+----------------------------------------------------------------------+
|                                                                      |
|      BOT        [ YOU ]        OPEN SEAT        BOT                   |  seat plates
|       o            o               - -            o                   |  the cast
|                                                                      |
+----------------------------------------------------------------------+
|  YOUR FIGHTER >  |  MATCH SETTINGS v |  ROOM   VQ7A     tap to copy   |  bottom rail, 180
|  DANTE . PASIP   |  +--------------+ |  +-----+ +-----+ +-----+       |
|  YOUR BUILD   >  |  |  START MATCH | |  |QUICK| |JOIN | |CHAT |       |
+----------------------------------------------------------------------+
```

- **The middle is never chrome.** Only the cast and the four seat plates.
- **Every drawer opens UPWARD out of the bottom rail and is attached to the column that opened
  it.** Nothing on this screen floats in a corner any more, which is what made the queue card and
  the chat read as unrelated boxes.
- **A free seat is a `Ghost` and says `OPEN SEAT`**, which is § 118.1 row 3 and the Among Us
  mechanism: an empty seat cannot be drawn with a filled surface however it is coloured.
- **The two player-card rows stop being near-twins** (§ 118.1 row 4): FIGHTER is a two-line row
  and BUILD is a one-line row, in a left column that is the only stack of rows on the screen.
- **BACK stops competing with the tabs** (§ 118.1 row 5): it is a small pill at the far edge of
  the rail and the tabs are the only thing in its centre.
- **`tap to copy` sits on the amber band of a `Sign`** (§ 118.1 row 7), the one accent on the
  screen, instead of being 15-unit muted cream on wood.
- **The version stamp sits on the top rail** (§ 118.1 row 8).

### 119.5 Each screen gets its own device, because the repetition complaint is about method

🧑: *"DONT USE THE SAME METHODS IN MAKING DIFF PAGES AND PANELS unless u have to bcz the comment
last time by everyone is that our ui looked bland and repetitive"*.

| Screen | Its device | Why that one |
|---|---|---|
| LOBBY | two full-width rails, empty middle | the room is the picture |
| LOGIN | ONE sticker card over full-bleed key art | it is the logo own construction, and the screen has one job |
| JOIN A GAME | one sheet, one field, and a SEGMENTED list (NEARBY / ONLINE) rather than two always-open sections | pic 2: *"COULD USE ON SOME WORKING ON BCZ IT FEELS OVERWHELMING"*. Two headed sections plus a field plus a footer is four groups; one field and one switchable list is two |
| QUEUE | a drawer that grows out of the rail | it is a state of the lobby, not a separate screen |
| CHARACTER SELECT | a filmstrip: the model large, the roster as a strip under it | it is a picker, and picker content is pictures |
| CHARACTER MAKER | a workbench: the model left, one category open at a time on the right | ten categories cannot all be on screen |
| SETTINGS | a folder: groups closed by default with a one-line summary on each header | `CLAUDE.md` § 6.2 question 3 |
| HUB | an ID card with a tab COLUMN rather than a tab row | six tabs across a header is the row that made § 92 unreadable |

### 119.6 ⚠️⚠️ `PaperPurityProbe` IS THE GATE ON "NO LEFTOVER OLD UI", AND IT IS THE POINT

🧑: *"MAKE SURE U COMPLETELY REPLACE UI BCZ I DOTN WANT LEFTOVER SHIT FROM OLD UI TO STILL BE
FRIGGING WITH US"*.

**Every previous pass in this file was verified by looking at a picture, and a picture cannot see a
surface that is behind another surface or off the edge of a drawer that is currently shut.** The
probe builds the lobby and the login screen, walks every `Image` under them, and fails on any node
that still carries `WoodSkin`, `GodotPanel`, a `GodotTheme.Box` sprite or a `UiMaterials.Plank`
sprite, unless it is:

- one of the authored textures (anything whose sprite comes from `Art/ui/`), or
- inside the match HUD or the main menu, which are out of scope.

⚠️ **IT ALSO ASSERTS THE INVENTORY IN § 119.3**: every named control resolves, is active in at
least one state, and has at least one `onClick` listener. That is the half § 118.5 acceptance
could not cover, and it is *"we dont lose buttons"* written as a test rather than as care.

### 119.7 Acceptance

- Every state photographed over the real background at 1920x1080 **and at his window shape**, with
  `UiRuntimeShots.ShotVersion` bumped every iteration.
- `PaperPurityProbe`, `LobbyStyleProbe`, `QueueCardLayoutProbe`, `PlayerHubLayoutProbe`,
  `UiClickProbe`, `AspectRatioProbes` green, plus Core, EditMode and the full PlayMode suite.
- `Checks.RunAll` five of five and the three `tools/` audits.
- ⚠️ **A person looks at the picture.**

---

### 119.8 ⚠️⚠️ THE REDUNDANCY 🧑 FOUND, AND THE LADDER THAT HAD NO DOOR

🧑, looking at the first paper build: **"dont quick match and start match do the same thing? kinda
confusing no?"**, then the fix in his own words: *"maybe for lobby separate it into ranked and
custom or other shit"*, *"maybe if ull join other server or use lan thats custom"*, *"you know use
other games as referenc"*, and **"make custom and ranked ladder shit diff dont jsut copy paste, bcz
ranked laddder dont need join code"**, *"make it as well na u cant queue with a friend in ranked
ladder or smth"*.

**He is right and the screen had two primaries.** START MATCH loaded an arena with whoever was in
the four seats; QUICK MATCH joined a queue that would find a room and load an arena. Both said "a
match starts now", they sat 400 units apart in one rail, and no position fixes two controls with
the same verb.

⚠️ **THE MECHANISM IS THE ONE EVERY GAME IN § 118.3 USES: one primary verb, and the MODE chosen
beside it.** Rocket League's home screen is PLAY over Casual / Competitive / Private; Overwatch 2
has one button whose LABEL follows the mode selector above it; Valorant puts a mode dropdown next
to one START. None of them ships two buttons that both start a game.

| Mode | The one thing | The primary | The right column | Settings |
|---|---|---|---|---|
| `Practice` | play now, alone | START MATCH | **nothing, and the rail shrinks** | open |
| `Ranked` | climb | FIND A RANKED MATCH | your TIER and the party rule | **locked plate, not a greyed chip** |
| `Custom` | get friends in | START MATCH / READY | the room code plaque, JOIN, CHAT | open |

⚠️⚠️ **AND THE LADDER HAD NEVER BEEN REACHABLE BY ANY PLAYER.** `QueueCard.OnQuickMatchPressed`
passed `QueueStake.Casual` as a literal. `QueueStake.Ranked` exists in the core, `PartyRules.CanQueue`
refuses a full stack and an unsigned member for it, `BotFillRules` has separate timing for it,
`RatingRules` owns five tiers and `MatchStatsCollector` reads
`Matchmaker.Current.Stake == QueueStake.Ranked` to decide whether a result counts. **All of Phase 9
shipped behind a constant no screen could change**, and nothing logged, because casual is a
perfectly valid queue. `QueueCard.Stake` is a field now and the RANKED tab sets it.

⚠️ **THE PARTY RULE IS STATED BEFORE THE PRESS.** `PartyRules.RefusalLabel` writes a good sentence
and the player only ever saw it AFTER pressing, which `CLAUDE.md` § 6.2 calls the INTUITIVE
failure. The tier plate carries *"Solo, or a party of up to three"* and, for a guest, *"The ladder
keeps a rating, so it needs an account. Practice and custom rooms never ask."*

⚠️ **ONE SLOT, THREE OCCUPANTS, EXACTLY ONE VISIBLE.** A separate `RankedButton` was built first and
`Logs/shots-runtime/LobbyRanked-v53.png` killed it: a rounded green rectangle where every other mode
has 🧑's authored chamfered slab. **The one primary has to be one OBJECT** or "always in the same
place" is true of the position and false of everything else. `OnStartPressed` dispatches on the
mode.

### 119.9 ⚠️⚠️ FIVE FAULTS THE RENDERS FOUND AND NO PROBE COULD, AND ONE OF THEM TOOK THREE PASSES

| # | What the picture showed | The cause |
|---|---|---|
| 1 | START MATCH drawn 110 units wide with its label clipped, on PRACTICE, and still wrong after a 1.5 s wait | ⚠️⚠️ **`ArrowButtonView.SetPivot` RE-APPLIES `_offMin` AND `_offMax` EVERY FRAME UNTIL ITS PIVOT LANDS**, and those are the offsets captured when the component last ran: the AUTHORED rect, not the one `LobbyChrome` just gave it. Correct on the main menu, where the pennant keeps its own rect; fatal on any control this pass reparents. `PaperKit.Paperise` switches it off for every node it touches. **Two of 🧑's reports, *"back is brokenn"* and *"te back button still broken"*, were the same bug on a different control**, and the second one is what proved the inset was never the cause. |
| 2 | `SKILLS` drawn through `Standard Build` | Two labels whose boxes overlapped by 46 units. **An overlap between two labels is silent in every direction**; § 102.4 is the same fault measured horizontally. Two boxes that share an edge cannot overlap by construction. |
| 3 | `ROOM CODE` drawn through the code | Same shape, on the plaque: a caption inset 24 from the bottom and a 44-unit value inset 26 from the top overlap by 12 on a 62-unit plate. |
| 4 | The tier plate showed `UNRANKED` and no sentence | The value's rect stretched to the plate's bottom edge and drew over the note. A `Text` draws nothing where it has no glyphs, so the covering label is invisible. |
| 5 | `v1.0.0` drawn through the word BACK | The rail's bottom-left corner and a 44-unit chip's vertical centre are the same 14 units of padding apart. |

⚠️ **AND THE SHOT PASS ITSELF WAS WRONG TWICE.** It opened `LobbyJoinPanel` with `SetActive(true)`
rather than `Open()`, so `Refresh` never ran and `LobbyJoin-v52.png` is four rows reading
`AVAILABLE GAMES APPEAR HERE`; and it photographed the lobby 0.6 s after a tab switch, inside a
0.45 s unfurl with a stagger. **A render of a state the game cannot reach is worse than no render.**

### 119.10 What 🧑 rejected by eye, with the number that agreed with him each time

| His words | The measurement |
|---|---|
| *"this yellow dont look good withh creme too btw"* | `UiTheme.Amber` `ffba00` on `UiTheme.Paper` `f4ecdd` is **1.7:1**. Amber leaves the front end; the marker role moves from HUE to VALUE and the room code is a wood plaque with cream lettering, 10:1. § 118.4's *"amber is the marker"* was written for a WOODEN front end, where amber was the one light thing on a dark screen. **Invert the field and the rule inverts with it.** |
| *"this yellow shit uglyu"*, of `SECURE YOUR PROGRESS` | Same 1.7:1, and 20 characters in a 200-unit chip. It is `ACCOUNT` in ink now. |
| *"maybe bcz u just recolored them all"* | Every control was a flat pill with a halo and a 2-unit lip: nothing had a below, so nothing had a height. Every raised surface casts a shadow inside its own bounds now, and a press collapses it. |
| *"this 2nd pic ugly too its still 2d"* | A `Tray` was one dark band along its top edge, which is a gradient rather than a hole. It is four things now: a hard inner shadow, a wrap down the side walls, a lit floor and a cut edge. |
| *"big ass empty sopace"* | The fighter column was 400 units around a 154-unit name, so the name sat at one edge and its chevron at the other. Sized to content, and every row centres its own strings. |
| *"why is entire right side empty"* | The bottom rail reserved 420 units for a mode column PRACTICE has nothing to put in. The rail has a `ContentSizeFitter` now and the column comes off, so the island re-centres. |
| *"its still so big too"* then *"make taht start match bigger"* | Not contradictory. **The CHROME got tighter and the ACTION got bigger**: `PaperKit.Pad` 18 to 14 and `Gap` 12 to 10, against a primary that went 88 to 104. The ratio that decides whether the button reads as the biggest thing is its height against the 44-unit chip above it: **2.4 to 1, from 1.6 to 1.** |
| *"why does insert player name still live here"* | `PlayerHub.BuildProfileTab` has had a `Display name` row since Phase 1. The rail's field was a second control writing the same string. |

### 119.11 What is NOT done, named rather than left implied ✅ CLOSED BY § 120, 2026-09-02

⚠️ **THE FIRST THREE ARE DONE AND § 120 IS WHERE THEY ARE WRITTEN UP.** 🧑 asked for exactly this
list back: **"PLS FINSH THE STUFF LEFT UNDONE"**. The last two are still open and are still open
for the reasons stated below rather than by oversight; they are repeated in § 120.7 so the next
reader finds them in the newest entry.

- ✅ ⚠️ **The sign-in screen's tab pair still uses `Token` against `Ghost`.** The lobby's uses
  `Live` against `Ghost` after the render showed 4 per cent was not enough; the login screen was
  not re-shot between those two changes and is inconsistent by omission, not by decision.
  **`SignInScreen.SetTab` goes through `PaperKit.MarkLive` now, with four other tab rows that had
  the same fault in three different forms (§ 120.3).**
- ✅ ⚠️ **The login card is 900 units tall around about 700 of content.** The Y offsets inside it
  are the ones `SignInScreen` has always used and they were spaced for a full-height column.
  **Measured: 809 of content, 68 units of margin above and 23 below, and it OVERFLOWED by 43 on
  the Google branch nobody has a client id for. `FitCardToContent` sizes and centres it on its own
  content now (§ 120.6).**
- ✅ **The character select, the character maker and the settings panel are dressed by
  `PaperKit.PaperDress.Screen` and have not been photographed.** The pass converts them; nobody has
  looked at whether the compositions still work in the new material. **Photographed at `v58` by
  `UiRuntimeShots.TheLobbyDoorsDraw` and `TheSettingsPanelDraws`, and all three were broken:
  § 120.4 rows 2, 4 and 5, and § 120.5 rows 1, 2 and 4.**
- **`LobbyChat`'s in-match instance is deliberately untouched** and still wooden, because the
  in-match HUD is out of scope. ⚠️ **Still true and still deliberate.**
- **`UiRuntimeShots` does not photograph the WELCOME BACK state**, which needs an account with a
  password attached and cannot be made in a probe. It is stated in that method rather than skipped.
  ⚠️ **Still true.**

---

## 118 · The lobby is coherent now and it is not finished ⚠️⚠️ OPEN, 2026-09-01, branch `ui-redesign`

🧑, after § 117 landed and he had looked at every render: *"create handoff to improve lobby ui even
furthre bcz it looks kinda ugly in some parts"*, *"ask that thing to critique it as well"*, and
*"tell it to use other games as referenc"*.

⚠️⚠️ **§ 117 FIXED THE LANGUAGE. THIS IS ABOUT THE COMPOSITION, AND THEY ARE DIFFERENT JOBS.**
Every surface on the lobby is now drawn in the geometry his own art is drawn in (`WoodCraft`), the
accent is spent once, and no control is distinguished by hue alone. **None of that is the same as
the screen being well composed**, and the eight rows below are what is left when the material
question is answered. Measured off `Logs/shots-runtime/Lobby-v51.png` and its three sibling states.

### 118.1 The eight things that still read badly, ranked by how much they cost

| # | What | Why it reads badly | The measurement |
|---|---|---|---|
| 1 | ⚠️⚠️ **The chat is a placeholder.** | An empty asphalt well with one muted line at its bottom-left, under a header, with nothing else on that side of the screen. It is the only surface on the lobby that looks unfinished rather than quiet. | The well is about 70 units tall to hold one 18-unit line, and the line sits at its BOTTOM because lines fill upward, so two thirds of it is empty by construction. |
| 2 | ⚠️⚠️ **The screen is four corners and a hole.** | The cast is the picture and the chrome frames it, which is the intended arrangement (§ 116.4), but there is no middle ground at all: nothing lives between the top band and the bottom rails on either side. | Left side: the tab row ends at y≈100 and MATCH SETTINGS starts at y≈780. **680 units of nothing.** Right side: the player card ends at y≈370 and LOBBY & CHAT starts at y≈845. **475 units of nothing.** |
| 3 | **The three seats that are not you say nothing.** | Three identical `BOT` plates and no statement anywhere that three bots will fill in, or that a friend could take one of those seats. The room code is on the card and the empty seats are in the middle, and nothing connects them. | `LobbyNameplates`. A player who has never played this game cannot tell whether BOT means "a bot is here" or "this seat is empty". |
| 4 | **The player card's two wooden rows are still near-twins.** | The character row (62 units, two lines) and the build row (38, one line) share a fill, a chevron and an inset. The footer link and the paper tag both read as their own thing; these two do not. | `LobbyChrome.BuildCharacterButton` and `BuildLoadoutButton`. |
| 5 | **BACK competes with the tab row.** | Same band, same height family, same material, and it is the one control on the screen that leaves. | `LobbyChrome.LiftBack`. |
| 6 | **Nothing moves.** | The main menu's pennants unfurl on every entry (`ArrowButtonView`, and 🧑 asked for that animation by name), and the lobby has no entrance at all: it cuts in fully drawn. | The drawers open and close with no transition either. |
| 7 | **`tap to copy` is 15 units and low contrast** on the one control the screen exists to produce. | `CreamMuted` at 15 on wood, beside a 30-unit amber code. | `LobbyChrome.BuildRoomSign`. |
| 8 | **The version stamp sits on nothing** in the bottom-right corner, over the road. | Every other word on the screen is on a surface. | `VersionStamp`. |

### 118.2 ⚠️⚠️ THE METHOD, AND IT IS NOT "MAKE IT PRETTIER"

**Run `game-ui-design` (installed at `~/.agents/skills/game-ui-design`) as a CRITIC first**, before
writing anything. 🧑 asked for that in as many words. Its `references/patterns.md`,
`sharp_edges.md` and `validations.md` are the three files; `CLAUDE.md` § 6.2, § 6.2b and § 6.2c
and `FUTURE.md` § 0.5b are this repository's own versions of the same questions and they win where
they disagree.

⚠️ **Answer § 6.2's four questions about the lobby out loud before touching it**, and note that
the answer to the first one has changed: START MATCH is unambiguously the one thing now, which it
was not when § 116 was written.

### 118.3 Other games, which 🧑 asked for by name, and what actually transfers

⚠️⚠️ **`FUTURE.md` § 0.5b's warning applies to every row here: COPY THE MECHANISM, NOT THE LOOK.**
The table in that section exists because the screens in § 92 were built by copying screenshots and
were still wrong. **Name what the mechanism assumes about the content, then check whether this
game's content has that shape.**

| Game | The mechanism worth stealing | What it assumes |
|---|---|---|
| **Among Us** | The room code IS the lobby's headline, drawn enormous, and the empty seats are visibly seats. | That the primary job of the lobby is getting three other people INTO it. That is true here and § 118.1 row 3 is the gap. |
| **Fall Guys / Stumble Guys** | The cast stands in a lit room and the chrome hugs the edges; the middle is never chrome. | That there is something worth looking at in the middle. There is: `LobbyCast`. This is what the lobby already does. |
| **Brawl Stars** | One enormous primary in the bottom-left corner the thumb rests in, and everything else is a small chip. | A touch device. The hierarchy transfers; the sizes do not. |
| **Rocket League** | A wide bottom bar that owns every action, so the play area above it is never interrupted. | That the actions fit on one row. Four here (start, quick, join, settings) probably do. |
| **Overwatch 2 / Valorant** | A persistent top rail for identity and a persistent bottom rail for actions, with the middle reserved. | Both have far more chrome than this game does; taking the rails without the content is how § 118.1 row 2 stays true. |
| **Party Animals** | Seats are physical objects in the room, and joining is walking into one. | A 3D lobby with room to move. This game's cast already stands in the street; the seats are `LobbyNameplates`. |

### 118.4 What NOT to do, because it has already been decided

- ⚠️⚠️ **Do not repaint his authored art.** `VISION.md` § 6, `CLAUDE.md` § 6.4 and § 6.5. START
  MATCH is `BUTTON LONG.png` through `ArrowButtonView`, the pennants are his, `TUMP.png` is his.
  The wordmark's CARVE is a tint treatment he asked for by name and the file is untouched.
- ⚠️⚠️ **Do not add a fifth hue, and do not add blue or navy in any layer.** § 6.4, which he has
  now had to state seven times.
- ⚠️ **Do not put the accent back on a drawer toggle or a tab.** § 117.3. Amber is the marker (the
  room code), green is the action (`JOIN BUTTON.png` is authored green), wood is everything else.
- ⚠️ **Do not draw a new surface with `GodotTheme.Box` or `UiMaterials.Plank`.** § 6.5: pick a
  `WoodCraft.Surface` role. Those two are the old language and are kept only for callers that
  cannot know their own height.
- ⚠️ **Do not touch the main menu or the in-match HUD.** Scoped out twice: *"dont touch main menu
  and inngame ui"*.
- ⚠️ **Do not re-baseline `CarryTests`.** § 117.8.

### 118.5 Acceptance

- Every state photographed, over the real background, at 1920x1080 **and at his window shape**
  (`CLAUDE.md` § 6.2b: `Fullscreen` is false in his `settings.json`). `UiRuntimeShots.TheLobbyDraws`
  takes four of the states; **bump `ShotVersion` every iteration** or the review is conducted
  against a cached image.
- `LobbyStyleProbe`, `QueueCardLayoutProbe`, `PlayerHubLayoutProbe`, `UiClickProbe` and
  `AspectRatioProbes` green, plus the full PlayMode suite.
- ⚠️ **A person looks at the picture.** A green layout probe is not a good screen, and § 117.7 is
  seven faults that every probe in this repository was green through.

---

## 96 · OPEN: he has never found the way into the hub ⚠️⚠️

**Reported 2026-08-30, by 🧑, about the shipped screens themselves.** Sent the hub's PROFILE tab
and the sign-in screen: *"i didnnt see that at all bruhh"*, *"didnt see this too"*.

⚠️⚠️ **THE SCREENS ARE FINE AND THAT IS WHY THIS IS WORTH AN ENTRY.** Both are built, both are
reachable, both are measured at nine resolutions by `PlayerHubLayoutProbe`, and both have been
green since § 92. **He has been playing the build and has never opened either of them.**

**The sign-in screen is expected and is not a bug.** § 92.3: *"Signing in never opens by itself."*
It is reached only by pressing something, deliberately, because Phase 1's rule is never to block a
first-time player on a form. A player who has not gone looking for it has not missed anything.

⚠️⚠️ **THE HUB IS THE PROBLEM, AND `PlayerNameplate` IS THE ONLY DOOR.** § 92.4 records that the
plate *"replaces both floating buttons and is the only way in"*, and that was the right call: the
two buttons it replaced were what he complained about (*"look wtf why are these buttons here"*).
**But one small chip in the corner of the title screen is now the sole entrance to four tabs, a
career, a match history and the whole account system**, and the person who commissioned it did not
find it.

⚠️ **`FUTURE.md` § 4.5.3 PREDICTED EXACTLY THIS AND NAMED IT AS THE THING A PROBE CANNOT SEE:**
*"it cannot see a screen that is ugly, and it cannot see a control nobody can find."* The probe
asserts the plate is on screen at all nine resolutions. It is. That is not the same claim as
"somebody looks at it".

**What is NOT known, and must not be guessed at.** Whether the plate is too small, too quiet, in a
corner nobody looks at, or simply does not read as pressable. **Do not fix this by adding a second
door**, which is how the six-button panel happened in the first place. Ask him what he expected to
press, or watch one launch.

**Candidates, cheapest first, none of them chosen:**
1. The plate does not look like a control. It has no press affordance and no hover state.
2. It says the handle and the level and never says what pressing it does.
3. `PLAY / SETTINGS / TUTORIAL / QUIT` is a strong vertical rail and the eye may never leave it.

**Done looks like:** he opens the hub without being told where it is.

---

## 95b · OPEN: nothing asserts that a menu label fits, only that it is legible ⚠️

**Split out of § 95 rather than fixed with it, because the fix and the gate are different jobs.**
`AspectRatioProbes.EveryShippedResolutionKeepsTheWholeAuthoredLayoutOnScreen` checks
`fontSize >= MenuKit.MinReadableUnits` and that rects are inside the canvas. It does **not** check
`preferredWidth <= rect.width`, which is the check that would have caught § 95 the day the
pennants were imported.

⚠️ **IT IS NOT A ONE-LINE ADDITION AND THAT IS WHY IT IS OPEN.** Turning the dump into an
assertion across the whole title screen will surface every other authored label in the converted
`.tscn` set at once, and some of those may be deliberate. **Run the dump first and read the list**,
then decide per label, then assert. `PhaseSurfaceLayoutProbe.DumpOverflowing` is the tool and it
already prints path, string, box, need and font size.

**Done looks like:** `AspectRatioProbes` fails on an overflowing menu label, and every exception
is named in code with a reason rather than being absent from the check.

---

## 72 · Two lobby controls reported dead that every headless check says are alive ⚠️ OPEN

🧑 2026-08-29: *"sa lobby hindi nagana yung player name, hindi makapag input ng name
(singleplayer)"*, *"hindi maka input ng code and lobby code sa lobby"*, and, confirming the
first: *"apparently u cant set ur name in singleplayer too"*.

**Both reproduce for the player and neither reproduces headlessly.** Written down rather than
guessed at, because the obvious fix was tried first and came back green.

What has been ruled out, and how:

- **Something covering the control.** `UiClickProbe` was widened to enumerate `InputField`,
  which its own note asked to be done deliberately rather than by accident, and the lobby's
  join card was added to its overlay list (it is built from code and parked inactive, so
  nothing in the suite had ever opened it). `PlayerNameEdit` and `JoinAddressEdit` both report
  `ok`: the topmost raycast hit at each field's centre is the field itself.
- **The click not taking the caret, or losing it.** `LobbyTypingProbe` is new and walks the next
  two steps of the same press: pointer-down and click through the EventSystem, then the
  selection re-read ten frames later. Both fields take the selection and keep it.
- **Legacy input being switched off.** `activeInputHandler` is 2, which is Both, and the
  `MatchSetup` scene's EventSystem carries a `StandaloneInputModule`.
- **The lobby chat stealing focus.** `LobbyChat` calls `ActivateInputField` from three places,
  but the only one reachable without typing in the chat first is gated on Return, and its
  `Update` returns early in the lobby before reaching it.

⚠️ The probes run a lobby with `SceneFlow.Networked` false, which IS the singleplayer case both
name-field reports name, so that is not the gap either.

**What has not been ruled out:** ✅ **the live NETWORKED lobby is ruled out as of § 77.4.**
`NetworkedLobbyTypingProbe` starts a real host through `NetSession.StartHostAsync`, sets
`SceneFlow.Networked`, and runs `LobbyTypingProbe.Check` itself rather than a copy of it, so the
two probes differ in exactly one thing. It passes, and it logs an inventory naming every
`InputField` and whether `LobbyChat` was live while it passed, because a pass with no chat in the
scene would rule out nothing.

⚠⚠ **SO ONE SUSPECT IS LEFT AND IT IS THE BUILT PLAYER AS OPPOSED TO THE EDITOR**, which is
the only item on this list no probe in this repository can reach. Everything else named here has
now been driven. `LobbyChat`'s `OnPointerClick` exists because a press
that missed its field was being swallowed by a plate, which is evidence this class of failure is
real on this screen even though neither probe can currently produce it.

**Done looks like:** a probe that reproduces it, then a fix. ⚠️ Do not "fix" this blind by
adding an `ActivateInputField` to both fields. That is the workaround `LobbyChat` already
carries, it would make the report go away without anybody knowing what was wrong, and the same
cause would surface on the next field somebody adds.

---

## 68 · The lobby is a form, and it should be a room ⚠️ OPEN, PLANNED 2026-08-28

🧑, 2026-08-28, with a PUBG lobby screenshot beside a capture of ours: *"i want our boring ass
lobby to look like this"*, *"i want multiplayer to go straight to lobby and thats where u can
join"*, *"all the shit like map mode bots character join yk everything is togglable"*, *"its okay
if character select still makes u go to a dif screen. i dont want character select to be
touched"*, *"make sure u dont break lobby"*, *"dont delete old huds and ui tho keep them incase ur
shit turns ugly"*, *"organize everything and make sure theres no redudnant shit"*, *"make sure
shit works like end to end"*.

**The reference is the LAYOUT, never the skin.** PUBG's lobby is grey military chrome and this
game's brand is painted wood, cream and amber (`UiTheme`, and `Art_Direction.md` § 1 says the
palette file is the only place a colour is named). What is borrowed is the ARRANGEMENT: the room
is the picture, the cast stands in it, the controls are small furniture pushed to the edges.

### 68.0 The four decisions, taken 2026-08-28 before any code

Asked and answered rather than assumed, because each one changes what gets built:

| Question | Answer |
|---|---|
| Network state on arriving at the lobby | **Auto-host on LAN.** § 68.5 |
| Which of the two references | **Hybrid**: PUBG Mobile's chrome, PC PUBG's four-person line-up. § 68.7 |
| Chat | **Lobby AND in-match, on this branch.** § 69, and it is the only wire change |
| Nav bar | **`PRACTICE ǀ MULTIPLAYER` tabs.** § 68.7 |

### 68.1 What the two screens actually are today

`ConvertedMatchSetup` is ALREADY both screens. It draws `PRACTICE MODE` offline and `LOBBY`
when `NetSession.IsNetworked`, off one `isNetworked` branch in `Refresh()`. Nothing has to be
invented to make multiplayer land here; the screen has been the lobby since § 55.

What lives on the OTHER screen, `ConvertedMultiplayerSetup`, is only the four ways IN: host
online (Relay), host LAN, a join address/code field, and the two browsers (`LanBeacon` and
`ServerQuery`). Those are the only things that have to move.

⚠️ **THE FOUR SELECTORS ALREADY REPLICATE AND THE READY TALLY ALREADY ARRIVES.** Map
(`SelectMapServerRpc`), mode (`SelectModeServerRpc`), difficulty (`SelectDifficultyServerRpc`),
picks (`SelectLobbyPickServerRpc`), the seat table (`LobbySeatInfo`, which carries `Name`,
`Occupied` and `CharacterPick`) and the ready tally (`OnLobbyReadyChanged`) are all on the wire
already. **The lobby redesign is therefore a pure client-side reskin.**

### 68.2 ⚠️⚠️ THE LOBBY WORK ADDS NO WIRE CHANGE. CHAT IS THE ONLY ONE, AND IT BUMPS THE PROTOCOL

Everything the new lobby draws is already replicated (§ 68.1), so § 68's own work moves
`NetSession.ProtocolVersion` not at all. **§ 69's chat does**, 5 → 6, because a chat line is a
named message that has never existed.

⚠️ **THAT MEANS BOTH LAPTOPS MUST BE REBUILT FROM THIS BRANCH.** § 59.4 records what a bump
costs: a peer on a different protocol is REFUSED at approval, by design, because a build that
"mostly works" presents as wrong characters and frozen bodies. § 59.2 is what makes the refusal
say so out loud instead of hanging. **Land and verify § 68 with the protocol still at 5, bump it
once in § 69, and never twice.**

⚠️ If a step in § 68 seems to need a new field, that step is wrong.
`tools/audit_wire_payloads.py` is the check, and `audit_request_call_sites.py` catches the other
half (a protocol added and never called).

### 68.3 ⚠️⚠️ THE OLD CHROME IS KEPT AND SWITCHABLE, NOT DELETED

🧑: *"dont delete old huds and ui tho keep them incase ur shit turns ugly"*.

`LobbyStyle` is one enum with two values, read once in `ConvertedMatchSetup.Wire()`:

* `Classic`, the authored converted panels exactly as they are today. Nothing new is drawn.
* `Street`, the new arrangement. Default.

⚠️ **THE OLD NODES ARE DEACTIVATED, NEVER DESTROYED**, and the new chrome is BUILT FROM CODE in
`Wire()` the way `BuildRightPanelNetwork` already builds the address and code rows. So
`MatchSetup.unity` barely changes on disk, `Classic` is a working screen at every commit, and
reverting is a one-line default rather than git archaeology.

⚠️ **`MultiplayerSetup.unity` AND `ConvertedMultiplayerSetup.cs` STAY ON DISK AND STAY IN THE
BUILD ORDER.** They cost one scene entry and they are the fallback if the in-lobby join panel
turns out worse. Only the LINK from `ConvertedModeSelect` is removed. `UiClickProbe`,
`ScreenshotTool` and `UiRuntimeShots` keep passing because the scene still exists.

### 68.4 ⚠️⚠️ RE-SKIN, DO NOT RENAME

`ConvertedScreen` finds every control by the name Godot gave it and `Node()` logs an error on a
miss. `SeatButton0..3`, `PrimaryButton`, `StartButton`, `BackButton`, `MapValueLabel`,
`ModeValueLabel`, `DifficultyValueLabel`, `CharacterButton`, `BannerLabel`, `DetailLabel`,
`SeatHeading`, `SeatHint`, `StatusLabel`, `MapPreview` and `CharacterSelectPanel` keep their
names and their handlers. **Renaming one is how this breaks silently**, and it is the exact
failure that class header exists to describe.

### 68.5 The navigation change, and the third lobby state it creates

`ConvertedModeSelect` sends MULTIPLAYER to `SceneFlow.MultiplayerSetup`. It goes to
`SceneFlow.MatchSetup` with `Networked = true`, **and the lobby auto-hosts on LAN the moment it
arrives.**

That gives the screen a state it has never had: networked, but no transport yet. Today
`IsNetworked` is false until somebody hosts or joins and the screen reads that as practice.

| State | Headline | Selector rows | Join panel |
|---|---|---|---|
| Practice (offline) | `PRACTICE MODE` | all live | hidden |
| Lobby, host bind failed | `LOBBY` + the reason | live, local only | OPEN |
| Lobby, hosting | `LOBBY · YOU ARE HOSTING` | live, replicated | available |
| Lobby, connected | `LOBBY · CONNECTED` | greyed, host picks | LEAVE |

⚠️⚠️ **A REFUSED PORT BIND MUST FALL BACK, NEVER HARD-FAIL.** Auto-hosting binds 8910 the moment
somebody presses MULTIPLAYER, and the usual reason it is already bound is the player's own second
copy of the game. The screen drops to row 2 with `NetSession.Status` on the status label and the
join panel already open, so the path OUT is on screen rather than being an error message. That is
`ConvertedMultiplayerSetup.Reason()`'s finding applied one screen earlier.

### 68.6 ⚠️⚠️ HOST → LEAVE → JOIN IN ONE LAUNCH IS THE PATH THIS FEATURE LIVES OR DIES ON

Auto-hosting means joining somebody else is STOPPING a host and STARTING a client in the same
process. That is § 65.1's fault (`NetworkManager.Shutdown()` does not shut anything down; a
second host or join in one launch was refused, silently) and § 63.1's (handlers registered once
per process, not once per session). **Both are fixed. Neither has ever been exercised in this
order.** § 68.14's two-process run exists for this and nothing else.

### 68.7 The arrangement: PUBG Mobile's chrome, PC PUBG's line-up

Hybrid, as decided. The bottom nav from the mobile shot is DROPPED rather than invented: its
tabs are RANK / SEASON / WORKSHOP / MISSIONS / INVENTORY and this game has none of them. The two
tabs that are real go along the top.

| Reference | Here | Built from |
|---|---|---|
| Full-bleed 3D scene | The chosen arena, live, still swaying | `MapPreviewSurface`, already shipped |
| Four players standing in it (pic 1) | The four seats as their picked characters | NEW, § 68.8 |
| Names + ready ticks over their heads | Same, plus a `TAYA FIRST` tag | NEW, § 68.9 |
| Top nav (pic 1) | `PRACTICE ǀ MULTIPLAYER` tabs | `MenuKit`, wood |
| Player card top-left (pic 3) | Name, avatar, the character you picked | `MenuKit` |
| Stacked selectors bottom-left | MAP / MODE / BOTS / CHARACTER | The existing selectors, restyled |
| Big yellow START bottom-left (pic 3) | `PrimaryButton` / `StartButton`, amber | Existing nodes, restyled |
| Bottom-right LEAVE + ticks + cog | LEAVE / SPECTATE / settings | Existing `BackButton`, `SpectateButton` |
| Party code / region | Join code + address + JOIN | Lifted, § 68.11 |
| (none) | Chat, bottom-left above START | § 69 |

⚠️⚠️ **THE TABS SWITCH IN PLACE, WITH NO SCENE CHANGE.** `PRACTICE` stops the transport and
clears `SceneFlow.Networked`; `MULTIPLAYER` sets it and auto-hosts. Both then re-run the same
`Refresh()`. A scene reload here would tear down the cast, the cached arenas and both render
textures the screen just built, and `SceneFlow.Go`'s one-load-per-frame latch will not save a
same-scene reload because it is scoped to a single frame on purpose.

⚠️ **THE PANELS GET SMALLER, THEY DO NOT GET TRANSPARENT.** `UiTheme.HeroPlate`'s note is
explicit that translucent near-black is COMBAT chrome, where the court behind it is the subject,
and that menu chrome is FURNITURE and may be opaque. The room becomes the picture by pushing
opaque wooden furniture to the EDGES, which is what both references actually do. A translucent
wood panel is the "brown shit" 🧑 already rejected once.

⚠️ **THE SCRIM CHANGES SHAPE, NOT STRENGTH.** `Scrim` currently flattens the whole backdrop. It
becomes a top and bottom gradient so the middle of the room is clean and the text still reads.

### 68.8 The cast, which is the whole feature and the only real engineering

Four characters standing on the map, lit by the map's own sun and graded by the map's own
`MapGrade`.

⚠️⚠️ **THEY GO INSIDE THE PREVIEW ARENA, NOT INTO A SECOND RENDER TEXTURE.** `ModelPreview` draws
ONE subject on layer 30 with its own lights and its own camera; four of those composited over the
map would be four cameras, four targets and four subjects lit by nothing the map knows about,
which is the pasted-on look. `MapPreviewSurface` already loads the arena, strips the match out of
it, confines it to layer 29, copies the arena's ambient, fog, sky and colour grade, **and already
finds the map's `SpawnPoints` because it averages them for its camera pivot.** The cast stands on
those same markers.

Shape of the work:

* `MapPreviewSurface` grows `ShowCast(...)`. Models are parented into the cached arena scene
  **AFTER `StripMatchObjects`**, because that method destroys every `CharacterMotor` GameObject it can
  reach, and although a plain roster prefab has no motor, ordering it wrongly is a silent
  deletion of the cast. They are then re-layered to 29 with the existing `SetLayerRecursively`.
* Art comes from `RosterBook.PersonArt(index, mode)` → `.Model`, `.Clips`, `.Palette`, exactly as
  `ConvertedCharacterSelect` resolves it. ⚠️ Rigs are imported **Generic**, so
  `ModelPreview.EnsureAvatar` is required or every model stands in its bind pose, arms out. That
  is `ModelPreview`'s own recorded fault 4, and it also wrecks any framing measured off the
  silhouette.
* A seat's character is `LobbySeatInfo.CharacterPick` when occupied and the roster default when
  it is a bot. ⚠️ `RosterBook`'s header: a missing entry must render SOMEBODY, never nothing.
* Scale is `ModelPreview.PreviewScale` 2.38, which is the match's own PERSON_SCALE. Previewing at
  native scale frames a doll.
* Framing: the registry's per-map `Yaw`/`Distance`/`Height` were tuned for an EMPTY street
  (Eskinita 0/22/16). A four-person line-up needs its own shot, so one `LobbyFraming` joins the
  three existing fields on `MapEntry`. ⚠️ It goes in the registry and not in the map scene, for
  the reason that struct's own note gives: `tools/maps/build_*.py` emit the map scenes
  WHOLESALE, so a camera placed by hand survives exactly until the next layout run.
* ⚠️ The sway stays. `SwayDegrees` 7 over `SwayPeriod` 26 is what stops the shot being a
  photograph, and it is a sway rather than an orbit so the camera never swings behind the facades.

### 68.9 Nameplates

A name, a ready tick and a `TAYA FIRST` tag floating over each character. UI, not world geometry:
projected with the preview camera's `WorldToViewportPoint` and mapped into the `MapPreview`
RawImage rect.

⚠️ **THEY ARE NOT TINTED WITH `Offense` OR `Defense`.** Those two colours mean "attacker" and
"defender", and `UiTheme.ForRole`'s note is explicit that the taya ROTATES every round, so a
fixed per-seat colour tells the player the wrong thing three rounds out of four. Cream for names,
Amber for the taya tag, nothing else.

⚠️ **EVERY PLATE IS SIZED AGAINST ITS STRING.** Legacy `Text` defaults to WRAP and the converted
labels ship `Overflow`, so a long player name either wraps out of its plate or draws straight past
it. `ConvertedScreen.SetHeadline` records this happening three times in one session and
`GameVersion.ApplyTo` records the fourth. A player name is arbitrary text from another machine,
which makes this the worst case in the game, not the mildest.

⚠️ **`raycastTarget = false` ON EVERY DECORATIVE GRAPHIC**, or `UiClickProbe` reports the controls
underneath as unreachable and it will be right.

### 68.10 Everything togglable, and the greying that is missing today

MAP, MODE, BOTS, CHARACTER, seat, SPECTATE, READY, START, the `PRACTICE ǀ MULTIPLAYER` tabs, and
the LAN/ONLINE choice in the join panel.

⚠️ **A NON-HOST'S CYCLE BUTTONS SILENTLY DO NOTHING TODAY.** `OnMapCycle`, `OnModeCycle` and
`OnDifficultyCycle` all open with `if (!NetAuthority.IsHost && SceneFlow.Networked) return;`,
which is correct authority and a bad control: the button lights, clicks, plays its sound and
changes nothing, which is indistinguishable from broken. They get `interactable = false` and a
line saying the leader picks. **This is a live defect being fixed in passing, not a new feature.**

⚠️ **BOTS STOPS AT HARD IN A NETWORKED LOBBY**, and that is `DifficultyOptionCount`'s existing
rule rather than an oversight: `NONE` removes three seats, and a seat is what a peer joins.

⚠️⚠️ **THE BIG AMBER BUTTON IS `START MATCH` FOR THE HOST AND `READY` FOR EVERYONE ELSE.** 🧑,
2026-08-28: *"start should be ready for everyone else except for host"*. One button in one place,
two labels, decided by `NetAuthority.IsHost`. **That is a layout change, not a behaviour change:**
§ 59.3 already made readiness an ANSWER the host reads rather than a trigger, on request (*"i
also dont like that if u click ready it auto starts"*), and the host's START is already live
whatever the tally reads, because a host plus three bots is a legitimate match.

⚠️ **SO `StartButton` AND `PrimaryButton` STOP BEING TWO CONTROLS ON SCREEN AT ONCE.** Today the
host sees both: `PrimaryButton` reads READY and `StartButton` is shown host-only right beside it.
In `Street` exactly one of them is visible per peer. ⚠️ **Both nodes stay in the scene and keep
their handlers** (§ 68.4); the one that is not this peer's is deactivated, not rewired, so
`OnPrimaryPressed` and `OnStartPressed` keep the meanings § 54 and § 59.3 settled.

⚠️ **CHARACTER STILL OPENS THE EXISTING PANEL IN PLACE.** 🧑: *"i dont want character select to be
touched"*. `ConvertedCharacterSelect.cs` and `CharacterSelect.unity` are not edited, and
`OpenCharacterSelect` keeps revealing `CharacterSelectPanel` as a child of this scene. § 68.13.

### 68.11 The join panel, lifted rather than rewritten

`ConvertedMultiplayerSetup`'s LAN browser, online browser, address/code field and Relay host
button move into a `LobbyJoinPanel` opened from the lobby. The logic is transcribed, not
redesigned: `Reason()` (which stopped four different failures reading as one sentence),
`LastDisconnectReason` (read once and cleared), the `host:port` split from § 59.1, and the code
lookup through `ServerQuery.ResolveCodeAsync`.

⚠️⚠️ **ONLINE IS A FIRST-CLASS LOBBY, NOT A LEFTOVER OF THE OLD SCREEN.** 🧑: *"make sure u can do
online server lobby too"*. Auto-hosting on LAN (§ 68.5) is the LANDING state and not the only
one. The lobby carries a `LAN ǀ ONLINE` toggle beside the join code:

* **LAN → ONLINE** stops the local host and calls `NetSession.StartRelayHost()`, then redraws in
  place. The join code row swaps from `address + code` to the Relay code, and `ServerQuery`
  publishes the lobby to the online pool so it shows up in other players' browsers.
* **ONLINE → LAN** is the same move back.
* **Joining** is symmetric and already is: `ResolveCodeAsync` returns `IsLan`, and the panel takes
  `StartClientAsync` or `StartRelayClient` off that flag. A four-character code works for both, so
  a player reading a code out loud never has to know which kind of lobby they are in.

⚠️ **A TOGGLE IS A SECOND HOST → LEAVE → HOST IN ONE LAUNCH**, which is § 65.1 again, from a
third direction. It is on the two-process list (§ 68.14 step 7).

⚠️ **AND ONLINE HAS ONE OPEN FAULT ALREADY: § 65.4**, the online browser can offer a lobby whose
Relay allocation is gone. Moving the browser does not fix it and must not hide it; the failure
has to reach the status label through `Reason()` like every other one.

⚠️⚠️ **`SceneFlow.Go(MatchSetup)` AFTER A SUCCESSFUL JOIN BECOMES A REFRESH IN PLACE.** The player
is already on that scene. Reloading would destroy the cast, the cached arenas and the render
textures, and the one-load-per-frame latch does not cover it.

⚠️⚠️ **AND THE REJOIN PATH MUST STILL FIRE.** `RejoinRunningMatch` runs inside `Wire()` and reads
`Lobby.MatchInProgress` on arrival; joining in place never re-runs `Wire()`. Its header records
what that hole costs: *"you'll only get ported back to the lobby with no way of joining back"*.
Whatever replaces the navigation has to ask the same question again at the same moment.

### 68.12 Organisation, so nothing lives in two places

🧑: *"organize everything and make sure theres no redudnant shit and that everything is easy to
find"*.

| File | Owns | Knows about |
|---|---|---|
| `ConvertedMatchSetup.cs` | The state machine and the wiring | Everything below |
| `LobbyChrome.cs` (new) | Building the `Street` furniture and the tabs | `MenuKit`, `UiTheme` |
| `LobbyCast.cs` (new) | The four models and their nameplates | `RosterBook`, `MapPreviewSurface` |
| `LobbyJoinPanel.cs` (new) | Hosting, joining, both browsers | `NetSession` |
| `LobbyChat.cs` (new, § 69) | The chat log and entry field | `MatchRpc` |
| `MapPreviewSurface.cs` | The room. Grows `ShowCast` + lobby framing | The arena scenes |
| `ConvertedMultiplayerSetup.cs` | Nothing. Unreferenced, kept as the fallback | (nothing) |

⚠️ **ONE COLOUR SOURCE AND ONE CONTROL BUILDER.** Every colour from `UiTheme`, every control
through `MenuKit`/`GodotTheme` so `GodotButton` variations keep applying. `UiTheme`'s header
records the whole hero layer drifting into a slate-blue palette because seventeen colours were
named inline; this is the same trap on a different screen.

### 68.13 What must NOT be touched

* `ConvertedCharacterSelect.cs`, `CharacterSelect.unity`, by request. `ModelPreviewTests` and
  `HeroPickerLayoutProbe` passing unchanged is the proof.
* `Packages/com.tumbangpreso.core/`, engine-free, and no rule changes here.
* `MatchRpc` payloads, until § 69 and then exactly once.
* `ConvertedMultiplayerSetup.cs`, `MultiplayerSetup.unity`, kept per § 68.3.
* `GameVersion` / `BuildBranch`. The corner reads `1.00` on every branch as of 2026-08-28.

### 68.14 Done looks like

1. `dotnet test Core.Tests` green.
2. EditMode green, `LobbyAndSettingsTests` included, plus a new test asserting that BOTH
   `Classic` and `Street` resolve every node `ConvertedMatchSetup` reaches by name. That test is
   what makes § 68.4 an assertion instead of a warning.
3. PlayMode green (no `-nographics`, it crashes the editor; assert on the `.xml`, never the exit
   code, because both a crash and a failure come back as 0): `UiClickProbe` finds every new
   control reachable, `AspectRatioProbes` clears nine resolutions, `HeroPickerLayoutProbe` and
   `ModelPreviewTests` still pass.
4. `Checks.RunAll` green, `SceneScriptCheck` above all: it is the only check that can see a scene
   holding a component the PLAYER cannot bind, and a shipped build once crashed on the map select
   with every other check green.
5. `tools/audit_wire_payloads.py` and `audit_request_call_sites.py` exit zero: § 68 adds no
   protocol, § 69 adds exactly one and it is called.
6. `UiRuntimeShots` captures of the lobby in BOTH styles, versioned filenames per `CLAUDE.md`
   § 6.1, so `Classic` and `Street` can be compared side by side rather than described.
7. ⚠️⚠️ **THE TWO-PROCESS RUN, AND IT IS THE ACCEPTANCE TEST.** § 38.19's driver, in this exact
   order, because the order is what is untested:
   1. A presses MULTIPLAYER, lands hosting, shows a join code.
   2. B presses MULTIPLAYER, lands hosting its OWN lobby.
   3. B opens JOIN, sees A on the LAN browser, joins. **This is the host → leave → join of
      § 68.6.**
   4. A's cast grows a second body wearing B's character, with B's name over it.
   5. B changes character; A sees the model change. B readies; A's tally moves.
   6. A starts; both land in the same arena on the same map in the same mode.
   7. B quits to the lobby and joins A AGAIN in the same launch (§ 65.1).
   8. B rejoins while the match is still running (§ 68.11's `RejoinRunningMatch`).
   9. Chat carries in both directions in the lobby and in the arena (§ 69).
8. Clean Windows build, previous output deleted first, timestamps verified on BOTH
   `TumbangPreso.exe` and `TumbangPreso_Data`. A `SUCCEEDED` line does not prove the launcher
   was re-emitted.

### 68.15 Order of work, so the screen is never half-broken

1. Navigation, auto-host, and the four lobby states. **No visual change at all.** Verify joining
   works from the lobby before anything is made pretty.
2. `LobbyJoinPanel`, lifted.
3. The cast in the backdrop.
4. The `Street` chrome and the tabs, behind the `LobbyStyle` switch.
5. Nameplates.
6. § 69's chat, and the single protocol bump.
7. The full verification pass in § 68.14.

### 68.17 What landed on the `PUBG` branch, and the five things the renders found

Steps 1 to 5 of § 68.15 are in. Every number below was MEASURED off a capture rather than
argued, which is the only reason any of them is right: five of the six were wrong on the first
pass and none of the six would have been caught by a test.

**The files.** `LobbyChrome.cs` (the `Street` arrangement and the tabs), `LobbyCast.cs` (the four
bodies), `LobbyNameplates.cs` (the plates over them), `LobbyJoinPanel.cs` (host, join, both
browsers), `LobbyChat.cs` (§ 69, used by the lobby AND the arena). `ConvertedMatchSetup` gained
the state machine and nothing else; `MapPreviewSurface` gained `Adopt`, `MapShown` and the lobby
shot; `MenuKit` gained `Fit`, `FitBox` and `FitBlock`.

**⚠️⚠️ THE CAST FACED THE WRONG WAY, AND THE NOTE THAT SAYS SO IS MISLEADING.**
`ModelPreview.FacingYaw` is 180 with a header about Godot's handedness, and reading it as "the
model's front is its local -Z" is the wrong inference. `Lobby-v1.png` is four backs.
`LookRotation` aligns local **+Z**, and these rigs face **+Z**, so the direction to point along is
`-forward` (subject toward camera). One sign, and no test in this repo can see it.

**⚠️⚠️ A RECT HANDED TO A LAYOUT GROUP IS A REQUEST, NOT AN INSTRUCTION, AND THAT COST THREE
RENDERS.** `LeftColumn` was set to 580 and `ReportColumns` measured it at 580, and the panel
inside it drew **820**. Three separate things get to overrule the width: the authored
`VerticalLayoutGroup` ships with `childControlWidth` OFF so it positions children without sizing
them, a child's `LayoutElement.minWidth` outranks the group even once control is on, and a child's
own `ContentSizeFitter` rewrites the rect after the group has finished. `Narrow` answers all
three. **`localScale` is what actually settled it**, because nothing in Unity's layout reads it,
and it shrinks the panel WITH its type and its borders, which a width alone does not.
`LeftScale` 0.72 and `RightScale` 0.86 open the middle band from 320 px to about 800.

**⚠️⚠️ THE FIT PASS HAS TO RUN MORE THAN ONCE.** `Lobby-v2.png` still reads `LOBBY · YOU ARE
HOSTIN` under the SPECTATE button after a fit pass that had already run and reported success: the
widths it measured came from a chain of layout groups that had not converged, so it measured
against a width nothing would ever have and concluded the string fitted. It now repeats for
`FitPasses` frames and forces a real `LayoutRebuilder` pass first, because
`Canvas.ForceUpdateCanvases` flushes the canvas and does not run the layout system.

**⚠️ THE NAMEPLATES WERE ALL IN THE BOTTOM-LEFT CORNER**, drawn as four stray `BOT` chips over the
BACK button, which reads as a chrome bug rather than a projection one. A plate is anchored at
(0,0), so its `anchoredPosition` is already measured from the parent's bottom-left; adding
`rect.xMin`, which on a centred-pivot stretched rect is minus half the width, subtracted half a
screen twice.

**⚠️ THE LOBBY SHOT NEEDED ITS OWN LENS, NOT JUST ITS OWN DISTANCE.** Framing four people to fill
half the height at the map shot's 58 degrees puts the camera about 3 m away and leaves the outer
two at 34 degrees off axis, visibly stretched. `LobbyFieldOfView` 32 puts the same framing about
7 m back at 17 degrees off axis, and keeps more of the street readable behind them.
`LobbyDistance` 12.6 and `LobbyHeight` 3.4 with `LobbyLookHeight` 0.85: aiming LOWER is what
lifts the cast clear of the corner furniture without changing how big they are.

**⚠️ THE ACTIVE TAB IS AMBER, NOT GREEN.** `WoodPrimaryButton` is green and means ACT (START
MATCH, READY); a tab is not an action, it is a statement about where you already are, and painting
it green put two "press me" buttons on one screen with the more important one further from the
hand. `WoodAmberButton` is new in `GodotTheme` and introduces no colour: amber is already this
UI's attention colour and is in `UiTheme`.

**What is verified.** `dotnet test` 111 green. `Checks.RunAll` all five green.
`audit_wire_payloads.py` 47 named messages, 0 mismatched, `Chat` and `ChatLine` among them.
`audit_request_call_sites.py` 43 entry points, 0 unreachable. `audit_ability_authority.py` 40
sites, 0 ungated on another body. `TheLobbyDraws` passes and writes
`Logs/shots-runtime/Lobby-v*.png`.

**⚠️ WHAT IS NOT VERIFIED IS THE ONLY THING THAT MATTERS: § 68.14 STEP 7, THE TWO-PROCESS RUN.**
Every fault this batch could still hold is on the far side of a second machine: host to leave to
join in one launch (§ 68.6), the LAN/online toggle, a joiner's cast wearing the right character,
the ready ticks moving on somebody else's screen, and chat in both directions. **Nothing here has
been played by two people.**

---

### 68.18 The second pass, 2026-08-28: navigation, the settings panel, and one rail per side

🧑, off the 4.7x player: *"Rewire clicking play from main menu to directly the lobby bcz we dont
need single player multiplayer selection anymroe as practice is bascally singleplayer already"*,
*"match settings look ugly"*, *"also maybe plan out where to put ui for char select, remove it in
match settings"*, *"pic 3 doesnt have animations or move but everyone else does in here"*, *"Also
rewire tutorial from main menu to the start training already, the text based tutorial is stale and
should be deleted and completley replaced by game tutorial"*, *"Pic 4 fix player name"*, *"Also
sometimes the pillars in the ilalim ng tulay map block the camera of lobby"*, *"put BACK somewhere
else, it looks ugly that its right below start match, it fucks up the visual hierarchy"*, *"also
remove this lobby thing bcz we all know this is lobby already"*, *"make sure all buttons work and
shit works right ennd to end"*, then, mid-pass: *"make these huds or ui look good bruh its so weird
to look at as none of them have visual harmony or shit"*, *"make sure all sfx play the right way"*,
*"i want thgis to say my name instead of YOU"*, *"i want u to make sure that everyone can see the
names in multiplayer (lan or server)"*, *"do u not feel weird that theres b ig ass empty space left
and right"*.

#### 68.18.1 Navigation: two screens left the path, and one panel was deleted

* PLAY goes straight to `MatchSetup` with `Networked = true`, so the landing state is the lobby
  auto-hosting on LAN. `ModeSelect` is unreferenced and **kept on disk and in the build order**,
  per § 68.3, alongside `MultiplayerSetup`. `SceneFlow.ModeSelect`'s own note carries the reasoning
  and `UiClickProbe` still probes both, because a fallback nobody checks is not a fallback.
* `MatchSetup`'s `CancelTarget` and BACK both go to `MainMenu` now. They have to agree or one of
  them is a step the other does not take.
* **The text tutorial is DELETED**, and it is the one place this batch departs from § 68.3's
  keep-the-old-chrome rule: that rule protects a REPLACEMENT that might turn out worse, and this
  was a deletion asked for by name with a shipped, played replacement. Gone:
  `ConvertedTutorialPanel.cs`, `TutorialContent.cs`, `Scenes/Ui/Tutorial.unity` and the
  `TutorialPanel` node inside `MainMenu.unity` (27 GameObjects, removed through the scene API).
  ⚠️ **The node had to go with the script.** A `MonoBehaviour` whose `m_Script` guid resolves to
  nothing is a yellow warning in the editor and a **refused build** under `SceneScriptCheck`, which
  is the only gate that can see it (`CLAUDE.md` § 7.1).
* The route moved to `SceneFlow.StartTraining`, because it was a private static on the deleted
  panel. `TUTORIAL` on the title screen enters it directly. `TutorialContent.ChipWidth` moved to
  `CreditsContent`, which is the only overlay left that draws a chip row.
* `DeadFeatureAudit` now asserts both halves: `SceneFlow` still arms `GuidedTutorial`, and the menu
  still reaches `StartTraining`. Either alone is silent.

#### 68.18.2 ⚠️⚠️ THE MATCH SETTINGS PANEL WAS UGLY FOR ONE MEASURABLE REASON

In `MatchSetup.unity` every caption is authored at **52 units** and every value at **34**, so the
word `MAP:` was drawn half again as large as the map's name. The label shouted and the thing it
labelled whispered. The rebuilt row inverts that (caption 22 amber, value 26 cream) and adds the
half nobody sees: **a fixed caption column**. `MAP:`, `MODE:` and `BOTS:` are three different
widths, so the authored `HorizontalLayoutGroup` started each stepper at a different x and nothing
in the panel lined up vertically.

* Every authored node is **restyled, never rebuilt** (§ 68.4). The arrows keep their
  `TextureButtonFeedback`, the values keep their `GodotOutline`, all of them keep their wiring.
* The colon is dropped, which is worth 54 px: the caption column has to hold the longest caption,
  and `BOTS` is 54 px narrower than `BOTS:`.
* CHARACTER left the panel entirely. See § 68.18.4.

#### 68.18.3 ⚠️⚠️ ONE RAIL PER SIDE, AND THAT IS A STRUCTURAL FIX RATHER THAN BETTER NUMBERS

Measured off `Logs/shots-runtime/Lobby-v35.png`, the bottom-left had **three left edges and three
widths**: the MATCH SETTINGS pill at x=75 running 300 px, its summary at x=60, START MATCH at x=55
running 380. The cause was that the left side was TWO hosts (`LeftColumn` at one anchor, a
`SettingsDrawer` beside it at another) at two different scales. **Two containers cannot share an
edge by arithmetic.** There is one `VerticalLayoutGroup` per side now and the group gives every
child the rail's width by construction.

* `LeftScale` 0.66 is **deleted**. It made every number on that side a lie: a 56 px header drew at
  37 and an 18 unit caption rendered at 12. The rail is authored at its real size.
* `LeftWidth` came down 560 → **460**, and the 100 px came out of the caption column rather than
  out of the type. At 460: 96 caption + 14 gap + a stepper of 20 padding, two 42 px arrows, two
  6 px gaps, leaving **214 px** for a value against `ILALIM NG TULAY` measuring about 195.
* The right-hand furniture **left `Columns` entirely**. `Columns` is a child of `Body`, a
  full-screen `VerticalLayoutGroup`: disabling the group ON `Columns` never stopped `Body` driving
  `Columns` itself, so "48 px in from my parent's right edge" was 48 px in from a moving rect. The
  old code compensated with a `-47` constant; `Lobby-v36.png` still had the pill 145 px from the
  edge against the chat's 48.
* ⚠️ **The lobby drawer stacks off `LobbyChat.PanelHeight`, not off its capacity.** The chat
  reserves six line slots and then collapses onto its content, so an empty log is about 65 px and
  the capacity expression gives 224. It is re-asked every frame because the chat grows as lines
  arrive; the guard makes that free.
* One harmony set decides every edge: `EdgeMargin` 48, `BottomMargin` 40, `TopMargin` 34,
  `RailSpacing` 12, `HeaderHeight` 56 (BACK and both tabs), `ToggleHeight` 52 (both drawers),
  `ActionHeight` 104.
* The three selector values are fitted **as a set**, to the largest size all three accept, and
  **reset to `ValueSize` on every pass**. Fitting them individually is why `Lobby-v35.png` has
  `ESKINITA` and `HARD` at full size and `HERO STRIKE` visibly smaller; `MenuKit.Fit` only shrinks,
  so a pass that measured a half-built rect pinned the type small permanently.
* The closed drawer's summary was **composed once inside `LobbyChrome.Apply`**, which runs before
  the screen's first `Refresh`, so it shipped the authored placeholder: `Lobby-v35.png` reads
  `ESKINITA · CAPTURE · NORMAL` on a lobby set to Hero Strike, and `CAPTURE` is not a mode this
  game has. It hangs off `Refresh` now.
* BACK moved to the top-left corner the banner vacated, and the banner is `SetActive(false)` in
  `Street` (not destroyed: `Refresh` still writes `BannerLabel`, and `Classic` keeps the pennant).
* The status line under START MATCH is **hidden unless it is an alert**. The four messages a player
  has to act on (refused port, dropped connection, relay refused, still connecting) still open it.

#### 68.18.4 The player card, and where character select went

CHARACTER left the match settings for a reason that is **authority, not tidiness**: MAP, MODE and
BOTS are greyed on every client by `RefreshLeaderControls`, so keeping CHARACTER as the fourth row
of a panel that greys out told three players in every four-player lobby that they could not pick a
fighter. It is the one choice on this screen that is always yours.

* The authored `CharacterButton` is **reparented**, keeping its name, its `Button`, its
  `GodotButton` skin and its handler. `OpenCharacterSelect` is untouched and still reveals
  `CharacterSelectPanel` in place. § 68.13 holds.
* The button gained two lines (character at 24, loadout at 18) because one line of
  `CHESKA · KALAWANG · CROCS ▸` at 24 units drew the name you chose at the same size as the slipper
  you did not think about.
* ⚠️ **`›` (U+203A), not `▸` (U+25B8) or `▶` (U+25B6), and `EDIT`, not `✎`.** Checked against
  Darumadrop One's own cmap: 525 glyphs, and it has none of `✎`, `✓`, `◀`, `▶` or `▸`. Unity's
  dynamic-font fallback draws them from a system font at a different weight and baseline.
* `CardWidth` is **330 and deliberately not `RightRailWidth` 392**. Matching the chat looked like
  the harmonious answer and left a visible hole: the gap is between the end of a short left-aligned
  string and an affordance pinned to the right edge, so only the width closes it. **The shared axis
  is the right edge, not the width.** 330 is measured against the worst case in the roster:
  `LOLA PACING` (11 chars, ~154 px at 24 against 244) and `DECADES TUNA  ·  TSINELAS` (25 chars,
  ~225 px at 18 against 244), not against `CHESKA`.

#### 68.18.5 The names, on every machine

* The local nameplate carried the pronoun `YOU`, so the three other people in the lobby saw
  `Matthew` over that body and its owner did not. It carries the name now; the `◀` marker still
  says which one is yours, and an unset name falls back to `YOU` rather than to the literal
  `Player` that `PlayerLabel`'s header already records four seats sharing.
* ⚠️⚠️ **A name edited in the lobby never travelled.** `NetSession.OnClientConnected` sends
  `IdentifyServerRpc(token, PlayerName, ...)` ONCE, on the frame the transport comes up, which was
  the whole story while the only editable field was in Settings on the title screen. The lobby card
  is editable while connected. `PublishName` re-sends `Identify` on commit: `LobbySession.Admit` is
  idempotent for a peer re-identifying under the same durable token (it copies the seat, the
  spectator flag and all three picks across and takes only the new name), which is the
  fast-reconnect path exercised on every relaunch. ⚠️ **No new message, so `ProtocolVersion` stays
  at 6** (§ 68.2, § 69.1).
* Every box that can hold a name already grows-then-fits: the plates size from the measured string
  up to a 420 px cap then shrink the type, the card's field is best-fit 13..20, and the seat rows
  go through `FitLine`. `Balance.PlayerNameMax` is 14.

#### 68.18.6 The cast's third character did not move, and no test could see it

🧑, with it circled: *"pic 3 doesnt have animations or move but everyone else does in here"*.
`LobbyCast.Poses` slot 2 asked for `holding-right`, which is a **carry POSE, not a performance**:
the rig's six `holding-*` clips are what the arm does while a tsinelas is in it, sampled by
`CharacterAnimator` as a state. It is a real clip on all twelve rigs, it has a length,
`SampleAnimation` succeeds, and it returns the same frame at every phase.

⚠️ **`PickPose` resolves by NAME, and a name that resolves is indistinguishable from a name that
animates.** So the pick is measured: `LobbyCast.MotionOf` samples the clip five times across its
length and takes the largest distance any transform travels. A hold measures 0.0 and a breathing
idle measures centimetres; `MotionFloor` is 1 cm. The table now asks for `interact-right` (the
pick-up reach, a real animation) and the check is the floor under it. Five samples rather than two,
because a clip whose first and last keys match is ordinary (`DanceClip` is built that way).

#### 68.18.7 The Ilalim ng Tulay pillars, and why a camera angle was not the fix

The lobby shot is 12.6 m out at a 32 degree lens, which puts the camera **inside the colonnade**
rather than outside it, and `SwayDegrees` 7 over 26 s walks it across the gaps: intermittent by
construction, which is why *"sometimes"* is the word in the report and why no still render was ever
going to settle it.

A per-map lobby yaw aimed down a clear lane would still swing into a pillar at the ends of the
sway, and widening the shot would undo `LobbyFieldOfView`'s finding about the outer two characters
distorting. What is actually wrong is that concrete is between the player and a face, so
`MapPreviewSurface.ClearSightlines` takes it out of the way for as long as it is: a ray from the
camera to each body's **head and chest** (two points, because a pillar is tall and thin and one
sample at chest height clears while the same pillar is still across the face) against every
renderer's AABB.

* ⚠️ `MaxOccluderSpan` 14 m, or **the sweep hides the road**: the floor slab's AABB contains the
  camera, so a ray enters it at t≈0. A viaduct pillar is about 1x1x8 and a jeepney 6 long.
* ⚠️ The hit must be in FRONT of the person: `Bounds.IntersectRay` reports a hit anywhere along an
  infinite ray, so without the distance test every building behind the cast would count.
* ⚠️ Adopted objects are excluded, or the arc's inner two characters delete the outer two: the cast
  is adopted into the SAME scene as the arena.
* ⚠️ **Renderers are disabled, not GameObjects**, which would fight `Park`/`Unpark` over the active
  flag they use to decide which map lights the world. The previous sweep is undone first, every
  time, or the street is stripped one pillar at a time. Rate limited to 6 sweeps a second, and the
  renderer list is cached per map because `GetComponentsInChildren` allocates per root per call.
* It is the LOBBY shot only. The practice screen is a picture OF the map; hiding a pillar there
  hides the thing being chosen.

#### 68.18.8 ⚠️⚠️ ONE PRESS WAS FIRING THE CLICK UP TO THREE TIMES

🧑: *"make sure all sfx play the right way"*. The UI click is added in three independent layers,
each individually correct and none aware of the others:

1. the CONTROL on pointer down (`GodotButton`, `ArrowButtonView`, `TextureButtonFeedback`);
2. the WIRING on click (`ConvertedScreen.WireOne`, so a screen cannot forget);
3. the HANDLER (`Cycle`, `TakeSeat`, `SelectTab`, both COPY buttons).

A map arrow has all three. `AudioDirector.PlayAtVaried` has **no dedupe** and `PlayAt` pins the
pitch at 1.0, so three copies of one 40 ms recording start in the same frame at the same position
and sum to about **+9.5 dB**, undecorrelated. It read as a clipped clack on the arrows and a doubled
one on every wood button, next to a clean single click on the runtime-built controls that only have
layer 1.

⚠️ **The fix is in `MenuSfx`, not at the call sites.** Deleting two of three layers is a nine-file
edit that leaves the rule written down nowhere and regresses to SILENCE the first time somebody
removes the wrong one. **One press is one sound** is a property of the sound layer: all three may
ask, and the first ask per cue per frame plays. Per cue, because a frame may legitimately carry a
click and an error. Per frame rather than per time window, because a time window would also swallow
a genuine fast second press on a map arrow.

Also: **a BACK button plays `ui_back` now**, the same as Escape always has. `ui_back.wav` and its
own mix entry exist because backing out is meant to be audibly distinct, and every BACK button in
the game was playing a plain click. `GodotButton.PressCue` is the field, `ConvertedScreen.WireOne`
sets it, and the wiring names the SAME cue the control does so the frame guard collapses them.
`DeadFeatureAudit.EveryMenuSoundGoesThroughTheOncePerFrameGuard` is the tripwire.

#### 68.18.9 What is verified, and what still is not

`dotnet test` 111 green. EditMode **188/188**. PlayMode `UiRuntimeShots`, `LobbyStyleProbe` and
`UiClickProbe` all green, which means every node the screen reaches by name resolves under BOTH
styles and BOTH tabs and no label draws outside its box in any of the four arms.
⚠️ `LobbyStyleProbe` caught one live defect in passing: `MiniSection`'s headings drew `SHARE THIS
LOBBY` in a **4 px box** in `Classic`, because the holder has no layout group so its preferred
width is zero, and `Street` had only ever hidden it by `Narrow` writing a width onto the column.
`Checks.RunAll` five green. `audit_wire_payloads.py` 47 named messages 0 mismatched;
`audit_request_call_sites.py` 43 entry points 0 unreachable. Renders at
`Logs/shots-runtime/Lobby-v41.png` and `LobbySettings-v41.png`.

#### 68.18.10 ✅ THE CHAT IS PROVEN, ON TWO REAL PROCESSES

🧑: *"does say something even work? can u even chat with people?"*. Fair question, because nothing
on either end said so: the send side printed `sent=True`, which only proves a message reached the
transport, and the receiving end drew a label and logged nothing. A run where the host relayed
correctly and a run where it dropped everything produced identical logs on both machines.
`LobbyChat.Add` logs the receipt now, for the reason `ConvertedScreen.WireOne` gives about menu
presses: in a shipped .exe a line in `Player.log` is the only way to tell "it never arrived" from
"it arrived and the panel did not draw it".

Two built players, one machine, `-tp-lobby` on 8910 and 8911, B joining A with
`-tp-lobbyjoin 127.0.0.1:8910 -tp-lobbychat hello_from_B`:

```
A (host)    [Net] 2 connected, seat 1
            [Chat] received from 'Matthew': hello_from_B
B (client)  [LobbyAuto] join result True
            [Net] connected as seat 2
            [LobbyAuto] chat 'hello_from_B' sent=True
            [Chat] received from 'Matthew': hello_from_B
```

**Both legs are in those six lines.** Client to host is B's send arriving on A. Host to client is B
receiving its OWN line back, which only reaches it through `HostRelayChat`'s
`SendNamedMessageToAll("ChatLine")`: `OnChatLineMsg` refuses the host, so A's copy comes from the
local `OnChatLine?.Invoke` beside it and B's comes off the wire. It also incidentally proves the
name work of § 68.18.5, because the line is attributed to `Matthew` rather than to `P2` or
`SOMEBODY`, which means `LobbySession.PeerById` had the real name at relay time.

⚠️ **This does NOT close § 68.14 step 7.** It closes 7.9 for the lobby. Host to leave to join in one
launch, the LAN/online toggle, a joiner's cast wearing the right character, the ready ticks, the
rejoin path and chat IN THE ARENA are all still unexercised, and the join here was driven by
`NetAutomationProbe` on one machine rather than by two people on two.

#### 68.18.11 The lobby log opens on demand

🧑: *"big empty sapce here for lobby and say something"*, then *"make it so that if u clcik chat u
see like the logs for it and who sent but it clsoes when u click out"*.

* ⚠️⚠️ **An empty log row is DEACTIVATED, not set to zero height.** Zeroing it was the obvious fix
  and it did half the job: a `VerticalLayoutGroup` puts its `spacing` between every pair of ACTIVE
  children whatever their heights are, so six zero-height rows still contributed six 4 px gaps.
  With 20 px of padding the idle panel was 44 px of nothing above a 44 px field.
* The lobby log is **closed by default** and opens on a click anywhere on the panel. The plate eats
  clicks by design, so `OnPointerClick` focuses the field rather than swallowing the press, which
  is also what makes "click chat" mean the whole panel rather than one 56 px strip of it.
* It closes when the field loses focus, **polled rather than evented**: an `InputField` losing
  focus to a click elsewhere on the canvas raises nothing here, so the only honest test is whether
  it still HAS focus. `Typing` is that test and the input reader already asks it.
* ⚠️ **An arriving line opens it anyway**, for `LobbyLogLife` 9 s. A log that only opened on a click
  would be silent about the one thing it exists for, because the message you most need to see is
  the one that arrives while you are looking at the cast. Focus overrides the timer, so a long
  message is never cut off mid-typing. This is the same shape the MATCH log already has.
* `FieldHeight` 44 → 56, so with the rows away the field IS the panel: at that moment the control
  is an invitation to type and it should look like one.


⚠️⚠️ **§ 68.14 STEP 7, THE TWO-PROCESS RUN, IS STILL THE THING THAT HAS NOT HAPPENED**, and this
pass adds one item to it: **the name published after connecting** (§ 68.18.5) has to be seen
arriving on the other machine's plate, in both directions, host and client.

---


### 68.19 The picker was bleeding, the chat still grew, and the roster had four slippers

Four things off one report, 🧑 2026-08-28: *"ui hella broken when i click character changer in
lobby"*, *"thoroughly overhaul and make the TSINELAS Model better because it looks so ugly"*,
*"add new slipper, alpombra"*, and *"chat lowk buns, it justt extends to 3 chats and u cant see
past that"*.

**1 · The character picker is an overlay now, not another piece of lobby furniture.** The lobby
builds its tabs, drawers and chat AT RUNTIME, after the authored `CharacterSelectPanel` exists, so
hierarchy order alone drew every one of them over the picker's backdrop: the screen you opened was
the picker with the lobby printed through it. `EnsureCharacterOverlayIsolation` gives the panel its
own `Canvas` with `overrideSorting` and `sortingOrder` 100, its own `GraphicRaycaster`, and a rect
stretched to the full screen; opening it also closes the join panel and moves it last among its
siblings, which keeps the rule true for any future decoration that does not make its own canvas.

**2 · The chat panel is a fixed two-line box with a door in it.** 🧑: *"i want u to not make the
chat extend anymore bcz theres empty sapce, js keep it at tthe size i sent and u can see other
chats by clicking it"*. ⚠⚠ **This supersedes § 68.18's growing-log bullets.** That pass made the
lobby log open on a click and on an arrival and then GROW through the seat rail one line at a
time, and it still discarded everything past the sixth message, so it was both in the way and
lossy. The panel is `LobbyVisibleLines` 2 rows plus its field, always that size. Clicking it opens
`LobbyChatLog`: a centred, scrollable log of the last `MaxHistory` 100 lines, parented to the ROOT
canvas at `sortingOrder` 90, dismissed by CLOSE, by Escape, or by clicking the shaded backdrop
(*"it clsoes when u click out"*). `LobbyLogLife` and the auto-open are gone with the growth they
served: the two compact rows are always drawn, so an arriving line is already on screen and
covering the cast to announce it would say nothing new. `FieldHeight` stays 56 and § 68.18's
`Debug.Log` receipt on every arrival stays exactly as it was.

Two traps paid for inside that overlay and both are silent failures:
* ⚠⚠ **The text component IS the scroll content, not a child of it.** A `ContentSizeFitter`
  measures the `ILayoutElement`s on its OWN object; an empty `RectTransform` with a `Text` child
  reports a preferred height of zero, so the content stays 0 px tall, the `ScrollRect` finds
  nothing to scroll, and the log opens showing one screenful with the wheel dead.
* ⚠ **`RectMask2D`, not `Mask`.** `Mask` needs the graphic to BE the mask, which would have thrown
  away the wooden inset the viewport draws.

**3 · The slipper roster is nine, and every new row is a licensed source model.** `TSINELAS` itself
is rebuilt: the drawing-derived mesh is still deleted (§ `Art_Direction.md` 4a) and entry 0 is now a
sourced, cleaned flip-flop. `SPARTAN`, `ALPOMBRA`, `PAMBAHAY`, `HEELS` and `SANDALS` join it, each
with its own three-stat FLIGHT / IMPACT / RECOVERY row, its own character-select description and
its own mesh. `tools/build_slipper_roster.py` is the Blender pass that produced them: one shoe per
prop, isolated where the source was a pair, normalised to the game's 0.432 m, recoloured to the
role-safe palette. Sources and their licences sit together in `Art/models/kits/footwear` with
`NEW_SLIPPER_LICENSES.txt`, which is the attribution compliance and must not be deleted.

⚠⚠ **THE ROSTER IS APPEND-ONLY AND THAT IS WHY THIS BUMPS THE PROTOCOL, 6 → 7.** A slipper pick
crosses the wire as an INDEX. Inserting a row above an existing one would make two peers render
different footwear for the same pick with nothing to report, and a build that knows nine rows must
not be told about a pick by a build that knows four. `TSINELAS` stays index 0 because every -1
fallback resolves to it and its row stays neutral. `HeadlessCheck` counts nine and
`ChatAndLobbyChromeTests` holds the version number beside it.

**4 · Nemu's kit reads as Nemu's, not as Kuro's.** 🧑: *"also fix nemu's character skill
descriptioins, make it sound cooler bcz it's all just Kuro's shit"*. GHOST STEP → PHANTOM VEIL,
KURO PROJECTION → ASTRAL HIJACK, SEANCE VOID → DEVOURING SEANCE, with the character blurb rewritten
to match. Names and copy only; no ability numbers moved, so `Balance` and every ability test are
untouched.

⚠ **What this pass did NOT do:** nobody has PLAYED the nine-slipper roster or the new log yet, and
the two-process run of § 68.14 step 7 still has not happened. The stats above are authored, not
measured; the first thing to check when the roster is played is whether `HEELS` at IMPACT 5 /
RECOVERY 1 is a real trade or just the best slipper.

---


## 69 · The game has no chat, in the lobby or in a match ⚠️ OPEN, PLANNED 2026-08-28

🧑, 2026-08-28: *"yea maybe add a chat to our game too that works in lobby and ingame"*.

Four people in a lobby have no way to say anything to each other, and four people in a match have
no way to call a play. Emotes exist (§ 38.3 put them on the wire) and they are not the same thing.

### 69.1 ⚠️⚠️ THIS IS THE ONE THING IN BOTH SECTIONS THAT MOVES `ProtocolVersion`, 5 → 6

A chat line is a named message that has never existed, so both machines must be rebuilt from this
branch or they refuse each other at approval. § 68.2 has the full reasoning. **Bump it once, in
this section, after § 68 has been verified at 5.**

### 69.2 Shape

* One named message, host-relayed: sender's seat, and the text. ⚠️ **The sender is NGO's
  authenticated client id, never a seat carried in the payload.** § 54 records exactly this: a
  field the host has to remember to ignore is a field that gets trusted, and `DeclareReady` was
  cut down to a single `bool` for it.
* ⚠️ **The host clamps length and rate.** § 38.9 found two request channels any client could
  flood; a text channel is the obvious third. A cap on characters and a minimum interval per
  peer, enforced host-side, not client-side.
* ⚠️ **The name is the one already in `LobbySeatInfo`**, not a second name field. There is exactly
  one identity per peer and it crossed the wire before this.
* `tools/audit_wire_payloads.py` must show the writer and the reader agreeing field for field:
  § 38.6 exists because netcode does not check that and a mismatch is silently misread bytes.

### 69.3 In the lobby

Bottom-left above the START button, in the wood set. Always visible, last few lines, entry field
below. No key needed to focus it: the lobby has no gameplay to steal a keystroke from.

### 69.4 In the match, where the rules are different

⚠️⚠️ **A CHAT FIELD THAT SWALLOWS MOVEMENT KEYS IS A WEAPON.** Enter opens it, Enter sends and
closes it, Escape cancels, and while it is open the gameplay input map is suspended.

⚠️ **THE INPUT MAP RULE APPLIES.** `CLAUDE.md` § 4: one control, one action, PER CONTEXT, and
`InputMapAndAbilityTests` asserts it. Chat is a THIRD context after gameplay and spectating, and
it is a narrowing of the same kind § 35.3 records: a player typing has no verbs, so its keys can
never collide with theirs. It goes in the input asset and in the rebinding panel like every other
key. It does not become a ninth `Keyboard.current` read outside the asset.

⚠️ **THE LOG FADES, THE HUD DOES NOT GROW.** `VISION.md` § 2 rule 5: a screenshot mid-fight must
still show the lata, the chalk and every player. Chat lines retire after a few seconds like
§ 46.3's banner rather than accumulating, and they sit clear of the ability deck, which is where
§ 46.1 and § 46.4 both found something drawn on top of something else.

⚠️ **A SPECTATOR CAN READ AND SEND.** They have no body and no seat, and their name is still in
the roster.

### 69.5 Done looks like

The two-process run of § 68.14 step 7.9, both directions, in the lobby and in the arena, plus
`audit_wire_payloads.py` and `audit_request_call_sites.py` green on the new message, plus an
EditMode test on the host-side clamp and rate limit.

---


---

## The archive index

One row per section that now lives in [`TODO_Archive.md`](TODO_Archive.md). Same numbers,
whole bodies, nothing deleted. **This table exists so that a pointer written anywhere in the
repository still lands on something**: follow it here, find the number, read it there.

| § | What it was |
|---|---|
| 90 | The impersonation guard, and telemetry ⚠️ 2026-08-30 |
| 91 | Phase 4: XP, levels and hero mastery ⚠️⚠️ 2026-08-30 |
| 92 | The account and career screens, rebuilt ⚠️⚠️ 2026-08-30 |
| 94 | Phase 4.5: quality control across phases 1 to 4 ⚠️⚠️ 2026-08-30 |
| 125 | Controller, touch and crossplay, built so that forgetting is impossible ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 124 | The skills are aimed and drawn in their own hand, the tutorial stopped lying, and Zack stopped being Sean ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 123 | The match settings go back to steppers, the shadow was retuned on the wrong axis, and a tab pair sat at half its neighbour's contrast ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 122 | The black line everywhere, the picker goes back to wood, and the loadout moves to the hero ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 120 | The buttons get a thickness, and the four screens § 119.11 left get finished ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 117 | The front end was two design systems stacked, and the code-drawn one was the wrong one ⚠️⚠️ 2026-09-01, branch `ui-redesign` |
| 116 | The front end had one material and no focus state ⚠️⚠️ 2026-09-01, branch `ui-redesign` |
| 115 | Eight faults in one build, phases 11 and 12, and the door he could not find ⚠️⚠️ 2026-09-01 |
| 114 | The boot is four screens, the lobby is the home, and the colour dial is deleted ⚠️⚠️ 2026-09-01 |
| 113 | The clothes were not clothes, the screen was see-through, and the door was a chip ⚠️⚠️ 2026-09-01 |
| 112 | The base rig is naked now, and the custom character walks into a match ⚠️⚠️ 2026-08-31 |
| 111 | The build he opened: no studio mark, and the boot screen in the wrong unit space ⚠️⚠️ 2026-08-31 |
| 110 | The character maker gets a wardrobe, and the custom hero borrows a kit ⚠️⚠️ 2026-08-31 |
| 109 | Phase 6's last mile: the three-hour hang, and the presence state nothing had ever lit ⚠️⚠️ 2026-08-31 |
| 108 | The custom character had no screen, and two screens were drawn under the screen that opened them ⚠️⚠️ 2026-08-31 |
| 107 | Roster Integrity and the 3-Slot Custom Character Creator ⚠️⚠️ 2026-08-31 |
| 106 | Phases 5 and 6 finished: the free colour dial, and parties as queue tickets ⚠️⚠️ 2026-08-31 |
| 105 | Phase 9: one ladder, five tiers, Glicko-2 ⚠️⚠️ 2026-08-31 |
| 104 | Phase 8: the witnessed result, and the finding that the plan's design would have been theatre ⚠️⚠️ 2026-08-31 |
| 103 | Phase 7: QUICK MATCH as a rating-banded queue ⚠️⚠️ 2026-08-31 |
| 102 | Phase 6: friends, presence and blocking ⚠️⚠️ 2026-08-31 |
| 101 | Phase 5 continued: the banner on the wire, palettes on remote seats, and the colour picker ⚠️⚠️ 2026-08-31 |
| 100 | ⚠️⚠️ THE BOOT SCREEN'S ART WAS FITTED TO A FRAME NOBODY CAN SEE, AND THE COLUMN WAS SIZED AGAINST THE WINDOW INSTEAD OF AGAINST THE FORM |
| 99 | ⚠️⚠️ EVERY `sortingOrder` A CODE-BUILT SCREEN SET WAS SILENTLY IGNORED, AND § 92.7'S FIX NEVER WORKED |
| 98 | Phase 5 begins: the banner, and wiring the rewards nothing wore ⚠️⚠️ 2026-08-31 |
| 97 | The boot account screen, PUBG-shaped, with the guest escape ⚠️⚠️ 2026-08-31 |
| 95 | ✅ CLOSED: the four title-screen buttons overflowed their own artwork at 720p |
| 95c | CLOSED: the loading screen was a black rectangle for most of the boot |
| 71 | The 2026-08-29 report, and the two faults only a non-host could see |
| 73 | The rest of the 2026-08-29 batch: feel, audio, and the casts nobody could tell apart |
| 74 | Zack's shock trail has the hazard bug that was fixed everywhere else ✅ CLOSED 2026-08-29 |
| 75 | The slipper throw wind-up, and what was actually checked ✅ CLOSED 2026-08-29 |
| 76 | Holding the pickup key does not right the can in the tutorial ✅ CLOSED 2026-08-29 |
| 77 | The network deep-dive: the half of § 71.3 that was never applied, and a refusal that was never sent ✅ CLOSED 2026-08-29 |
| 78 | The two-machine acceptance test, run at last, and the batch it paid for |
| 79 | The 2026-08-29 evening batch: what he reported, what landed, and what is still open |
| 81 | ⚠️⚠️ THE PLAYMODE ARENA SUITE IS NOT A GATE ANY MORE, AND HERE IS THE EVIDENCE |
| 80 | The 2026-08-29 late batch, reported while § 79 was being fixed |
| 82 | The 2026-08-29 night batch: the match that was over before it started |
| 83 | The 2026-08-29 balance-and-controls batch, reported while § 82 was being pushed |
| 84 | The 2026-08-30 batch: twelve reports off the shipped build, and a lighting number read off a dead field |
| 85 | The 2026-08-30 AUDIO and VISUAL list, sent as one block |
| 86 | The spectator pause, and the 35 ms every non-host was standing behind |
| 87 | Every tsinelas rendered flat brown in first person, and the fix for it flattened the shading on all of them ✅ FIXED 2026-08-31 |
| 0 | Hero Strike is being reworked, and the plan is its own file |
| 8 | The abilities still look repetitive, and half the fix is not done |
| 9 | Ilalim ng Tulay dressing defects, reported off the 2026-08-25 player |
| 12 | Everything 🧑 found playing the 2026-08-26 build ✅ ALL CLOSED SAME DAY |
| 13 | Everything the 2026-08-26 evening build showed, and the pattern in it |
| 14 | The 4.69 player's second batch, shipped in `349b0171` |
| 15 | The 4.70 tutorial batch, and why four screenshots were one probe apart |
| 16 | The probe was never deterministic, and § 10 was closed on an argument |
| 17 | The bots are steeply sensitive to the frame step, and a 50 fps machine is in the bad band |
| 18 | HUD strings overflow their boxes, in more than one place |
| 19 | The powers were fifteen poses sharing one construction, at every layer |
| 20 | Cheska's kit played the wrong sounds, and every zone died in silence |
| 21 | Phaister merged in, and everything she arrived without |
| 22 | Everything the 4.71 player showed, and the two entries that were ticked but not wired |
| 23 | Ability stuns are now fought out of, not waited out |
| 24 | Phaister's three powers were one builder at three radii |
| 25 | Which peers actually hear a sound, measured rather than assumed |
| 26 | Every ultimate changes the weather, and each hero changes it differently |
| 27 | The other five heroes need a motif, and it is not more symbols |
| 28 | Nemu's ultimate is her pet now, and her kit is named after him |
| 29 | The other four heroes got their motif, and none of them shares a builder |
| 30 | Two findings from measuring the cue files, and one stale line in `CLAUDE.md` |
| 31 | Everything the 4.72 playtest reported, and the two faults it exposed |
| 32 | The networking was broken by one unreplicated static, and four other faults on top of it |
| 33 | The bots picked a target by seat number, aimed powers at rings they do not cast, and had no keyboard between decisions |
| 34 | Seat 0 was steered by a different movement model in every all-bots run, and it is § 11's second layer |
| 35 | The spectator flies itself, every key is in the panel, and a reconnect stops refunding cooldowns |
| 36 | The host never transmitted its own bodies, so a joiner saw three statues |
| 37 | Two Phaister presentation faults from the 4.72 player ✅ CLOSED, SEE § 43 |
| 38 | The network pass: eleven faults the host cannot see, and the loopback behind four of them |
| 39 | The settings wheel, for the fourth time, and the cause the first three missed |
| 40 | The train is one field recording now, and it plays rarely |
| 41 | The ultimate meter counts events now |
| 42 | Nemu's ride home was being erased by her own body's bot |
| 43 | Two Phaister presentation faults, and a class of fault behind one of them |
| 44 | § 32.3's slider fix was muted by the sweep on the next line ✅ CLOSED 2026-08-27 |
| 45 | The in-match HUD had five ambient sines, three copies of "LATA DOWN" and twelve coloured cells |
| 46 | Both intermission banners were drawn on top of something ✅ CLOSED 2026-08-27 |
| 47 | `Checks.RunAll` has been red since the Phaister merge, in two places |
| 48 | Kuro's projected body deleted itself mid-ability, and took Nemu's way home with it |
| 49 | Seat 0 travels about half what seats 1 to 3 do, in Classic, every run |
| 50 | Fourteen reports off the 4.73 player ✅ CLOSED 2026-08-27 |
| 51 | The four follow-ups off § 50 ✅ CLOSED 2026-08-27 |
| 52 | The ready and rematch gates counted a seat as a peer, and five guards allocated before they guarded |
| 53 | A joining client could not move, and the cause is that its keyboard was left on seat 0 |
| 54 | Which of the two lobby fixes was kept, and why |
| 55 | The lobby was a picture of a lobby ✅ CLOSED 2026-08-27 |
| 56 | What the merged network pass still leaves open |
| 57 | The match ends on one machine, and three other events never reach a client at all |
| 59 | Two machines could discover each other and could not join, and it is one missing string split |
| 60 | The host announces a seat twice, by two protocols, and only one of them does the job |
| 53 | The corner stamp is the branch name ✅ CLOSED 2026-08-27 |
| 62 | Losing the host left a client playing on alone, and § 60.1 did not fix the movement |
| 63 | A game could be joined exactly once per launch, and remote bodies never animated |
| 64 | The bots had no face, no feet, a perfect memory and one opinion |
| 65 | Hosting or joining a SECOND time in one launch was refused, silently |
| 66 | Joining bounced the view about, and rejoining a running match was impossible |
| 67 | What the HARRYDAKS merge was hiding, found by building it |
| 70 | The prop art was replaced wholesale, and IKE was never a bad model ✅ MOSTLY DONE 2026-08-28 |
| 1 | Peer rematch voting across the wire |
| 2 | Cheska's Ice Barricade duration was set by accident ✅ CLOSED 2026-08-25 |
| 3 | The five hero accents have not been seen in a real match |
| 4 | Bayan Plaza's monument stands inside the defender's box |
| 5 | The overclock window has not been measured against a match |
| 11 | Every probe number ever printed was an average over a seat that could not play ✅ CLOSED 2026-08-26 |
| 10 | `BotBehaviourProbe` cannot answer a comparison, and every open balance question is one |
| 6 | `AiDiagnosticProbe`'s Classic round is a real-time test and it flickers red |
| 7 | The test suite costs more to run than it is currently returning |
| 58 | The ink outline tore open at every hard edge ✅ FIXED 2026-08-27 |
| 63 | The world outline was aliased because MSAA was never able to see it |
| 63 | Walking into a utility pole blanks half the screen, and it now dithers away |
| 64 | The player can switch render styles, and the alternative is a chromatic look |
| 65 | The white keyline round every silhouette, measured rather than argued |
| - | Closed |
