# FUTURE.md: turning Tumbang Preso into a game people come back to

**What this file is.** An ordered build plan for the live-service and competitive half of the
game: accounts, a database, profiles, stats, matchmaking, ranked, progression, unlockables,
customisation, controller, mobile, tournaments. Every phase carries a **PROMPT** block written to
be pasted straight into a new session as its whole brief. **Every prompt lives in § 19**, in
one place, with an index. § 0.5 is the standing preamble each one inherits, and § 0.6 is the
staleness protocol: what to re-verify before trusting any of them.

**What this file is not.** It is not a decision that any of it ships, it is not balance, and it is
not `docs/Design.md`. Where this file and `docs/VISION.md` disagree about what the game IS,
`VISION.md` wins.

⚠️ **Its companion is [`INSPIRATION.md`](INSPIRATION.md)**, which is the study of what other games
do and what each thing becomes here. This file is the WHAT and the WHEN; that one is the WHY, and
it carries eleven more prompts plus the combined order for both files in its § 8.6.

Written 2026-08-31, from 🧑's own words: *"I want this game to be an actual esport like league or
valorant or overwatch"*, *"competitive ranking, player profile, shit u can grind for like diff
skills"*, *"ppl can make an acct in it and there is a matchmaking system as well as different
ranks and different points"*, *"customizable characters in the future with unlockable skills"*,
*"stats too and shit as well as mobile + controller support"*, *"upload profile pic, add email,
add bio, show stats, match history"*, and the constraint that governs every line below: *"keep in
mind we dont have budget for paying for anything"*.

---

## 0 · Read this before picking a phase

### 0.1 The six rules that constrain everything here

1. ⚠️⚠️ **BOTH MODES SHIP AND CLASSIC NEVER GETS POWERS.** `VISION.md` § 1. A progression system
   that hands out abilities applies to **Hero Strike only**, or it deletes the reason Classic
   exists. Phase 10 is built around this and it is the easiest thing on this page to get wrong.
2. ⚠️⚠️ **NOTHING COSTS MONEY.** Every service named is a free tier that exists today, and each
   phase states what breaks first when that tier runs out. If something cannot be done free, it
   says so in its own words instead of being quietly skipped.
3. ⚠️⚠️ **THE GAME IS HOST-AUTHORITATIVE PEER TO PEER.** `MatchDirector.AddScore` runs on the host
   and the host is a player. **Ranked on that architecture is cheatable by the host.** Phase 8 is
   not decoration: it decides whether a rank means anything. Do not ship Phase 9 without it.
4. **The rules core stays engine-free.** `Packages/com.tumbangpreso.core/` must never acquire a
   `UnityEngine` reference (`CLAUDE.md` § 4). Rating maths, XP curves, unlock rules and season
   arithmetic belong there, where they can be asserted in a second instead of playtested for an
   afternoon. **Put the numbers there.**
5. **Every wire-facing list is append-only.** `Roster.Slippers` records why: an index that crosses
   the wire cannot be reordered without two peers rendering different things for the same pick,
   silently. Characters, cosmetics, ranks and seasons all inherit this.
6. **Nothing on a progression track may change a gameplay number.** Said once here and twice more
   below, because it is the single mistake that cannot be undone after players have ground for it.

### 0.2 What already exists, so no phase re-derives it

| | |
|---|---|
| Netcode | `com.unity.netcode.gameobjects` 2.13.1 over `UnityTransport`. `NetSession` owns it. |
| Online discovery | **UGS Lobby**, live. `ServerQuery` browses, heartbeats at 15 s, resolves 4-character join codes LAN-first then online. |
| Relay | **UGS Relay**, live, through `com.unity.services.multiplayer` 2.3.0. |
| Auth package | **`com.unity.services.authentication` 3.7.4 is already installed and unused.** Phase 1 is smaller than it looks. |
| LAN | `LanBeacon`, with persistent peer identity so a reconnecting player gets their seat back. |
| Reconnect | `LobbySession` already implements seat reclamation, a fast-reconnect window and leader election. **Do not rebuild this.** |
| Protocol gate | `NetSession.ProtocolVersion`, 13. Peers on different versions refuse each other at approval, by design. |
| Bots | `AIController` plus `GameLaunch.AllBots`. A bot presses the same buttons a human does, one physics step serves both. |
| Spectating | `SpectatorCamera` with free, follow and POV modes, plus a spectator pause that crosses the wire. |
| Chat | `LobbyChat`, lobby and in-match, with hard-won layout notes. Extend it; never write a second one. |
| Content | 18 characters, 6 heroes, 6 lata, 10 tsinelas, 3 maps, `RosterBook` resolving id to model and palette. |
| Recolouring | `ToonSkin`'s 16-slot palette remap, per renderer, cached. **A colour variant of any character is already nearly free.** |
| Settings | `Settings.SettingsStore` for persistence, `Rebinding` for the input map. |
| Input | `TumbangPreso.inputactions`: **Keyboard and Mouse only.** Zero gamepad bindings, zero touch bindings, no control schemes. |
| Build targets | Windows Standalone, WebGL, Linux Dedicated Server. **No Android, no iOS.** |
| Localisation | **None.** No language setting, no string table. Every string is inline. |
| Accessibility | **None beyond rebinding.** No colourblind mode, no UI scale, no subtitle system. |

⚠️ **THE FIXED VPS POOL IS RETIRED AND ANY NOTE SAYING OTHERWISE IS STALE.** `ServerQuery`'s header
records it: `139.180.212.110` ports 8910-8917 with a +10 status offset was the Godot
implementation, and UGS Lobby replaced it on 2026-08-19. Do not plan around that pool and do not
resurrect the UDP query loop.

### 0.3 The free-tier budget, named

| Service | Used for | What breaks first |
|---|---|---|
| UGS Authentication | Accounts, `PlayerId` | Nothing. Genuinely free at any size this game will see. |
| UGS Cloud Save | Profile, stats, inventory, progression | Item size, not user count. One small document per concern. |
| UGS Cloud Save Files | Avatars, replays | Per-player storage quota. Cap the upload size hard. |
| UGS Leaderboards | Ranked ladders, seasonal boards | Entries per board and reset frequency. |
| UGS Cloud Code | The only server-authoritative writer | Monthly invocations. **Call it once per match, never per event.** |
| UGS Lobby | Discovery, parties, the matchmaking queue | Queries per second. `ServerQuery.QueryInterval` is 4 s for exactly this reason. |
| UGS Relay | The connection itself | Concurrent users and GB. **This is the first thing that will actually bite.** |
| UGS Matchmaker | Skill-based queueing | Match requests per month. Phase 7 gives a zero-cost fallback. |
| UGS Analytics | Telemetry | Events per month. Batch per session, never per event. |
| GitHub | Repo, CI, releases | Nothing at this size. |
| itch.io | Distribution | Nothing. |

⚠️ **Multiplay (dedicated game servers) is the one thing on the shopping list that is not free.**
Everything below is arranged so that the day it is affordable, it slots in behind Phase 8.2
without rewriting anything else.

### 0.4 The order, and why it is this order

```
1  ACCOUNTS ────> 2  PROFILE + STATS ──> 3  TELEMETRY
                       │
                       ├──> 4  PROGRESSION ──> 5  COSMETICS ──> 10 MASTERY PATHS
                       ├──> 6  SOCIAL
                       ├──> 7  MATCHMAKING ──> 8  INTEGRITY ──> 9  RANKED
                       ├──> 11 BOTS + POPULATION
                       ├──> 12 MODES + MAPS
                       └──> 13 SEASONS + LIVE OPS

14 CONTROLLER ──> 15 MOBILE                (independent of the whole column above)
16 ACCESSIBILITY + LOCALISATION            (independent, and overdue)
17 TOURNAMENT, LAN, SPECTATE, REPLAYS      (partly urgent: see § 17)
18 DISTRIBUTION                            (last, and smaller than it looks)
```

**Do 1, then 2, then 3, then stop and play it for a week.** Everything after that is worth more
when there is real data to point at.

### 0.5 The standing preamble, which every prompt in both files inherits

⚠️⚠️ **A SESSION RUNNING ANY PROMPT IN `FUTURE.md` OR `INSPIRATION.md` IS BOUND BY THIS SECTION.**
Each prompt names it rather than repeating it, so there is one copy to fix when it changes and no
chance of nineteen copies drifting apart. **If you are a session that has just been handed a
prompt: this is part of your brief. Read it now.**

**1. Read order, before touching anything.**
`CLAUDE.md`, then `docs/VISION.md`, then `docs/TODO.md`, then this section, then the phase section
your prompt names. The summary in a prompt is never the rules.

**2. Verify before you trust.**
Every factual claim in these two documents was true on the date in the file header and may not be
now. § 0.6 lists the ones most likely to have moved. **Where this document and the code disagree,
the code is right.** Fix the document in the same commit and say so in your handoff.

**3. Where the code goes.**
Rules, curves, tables, thresholds, validation and any number that could be argued about go in
`Packages/com.tumbangpreso.core/`, which must never acquire a `UnityEngine` reference. The Unity
side is presentation, input and transport. If you find yourself writing an `if` about game rules
inside a `MonoBehaviour`, it is in the wrong file.

**4. Nothing on any progression track may change a gameplay number.**
Cosmetic or expressive only. Write the test that proves it.

**5. Wire-facing identity is string ids, and lists are append-only.**
`Roster.Slippers` records why an index that crosses the wire can never be reordered.

**6. Server-authoritative writes only.**
Profiles, stats, ratings, XP, currency and unlocks are written by a Cloud Code endpoint computed
from a match record, never sent by a client and never written by the host directly.

**7. Offline and LAN must keep working.**
Every feature degrades to a local profile when the network is unreachable. Practice, Training,
LAN and joining by code must never sit behind a login. See § 17 for why this is not negotiable.

**8. Free tier only.**
If the design needs a paid service, stop and say so in the handoff rather than building half of it.

**9. Definition of done, for every prompt.**
- The feature works and you have exercised it yourself, not reasoned that it should work.
- `dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj` passes.
- EditMode passes. Assert on `Logs/tests.xml`, never on the exit code.
- The PlayMode probes that touch what you changed pass, run with `-testFilter`.
- ⚠️ **A test run that reports `total="0"` is not a pass.** Check the count, not just the result.
- New rules have new tests in the core.
- `docs/TODO.md` has a section for the work, in the same commit as the work.
- Any claim in `FUTURE.md` or `INSPIRATION.md` you found to be stale is corrected, same commit.
- Committed, pushed, and the handoff written in the chat reply per `CLAUDE.md` § 2.4.

**10. How to work while Unity runs.**
Every editor launch goes in the background and you keep coding. Never sit and watch a test run.
Never edit a `.cs` file while a Unity run is in flight. `CLAUDE.md` § 2.1b.

**11. What to do when the prompt is wrong.**
These prompts were written before the code they describe existed. If the design in one is
impossible, already done, or made obsolete by something that shipped since, **do not build it
anyway and do not silently skip it.** Do the part that still makes sense, write what changed into
`docs/TODO.md`, correct the prompt in the plan file, and put the disagreement at the top of your
handoff.

### 0.6 How these documents go stale, and what to re-verify

⚠️⚠️ **THESE ARE PLANS WRITTEN AHEAD OF THE WORK. THE FURTHER YOU ARE FROM 2026-08-31 THE LESS OF
THIS IS TRUE.** The prose about design intent ages well. The claims about the codebase do not.

**Re-verify these before acting on any prompt.** Each is one command or one file.

| Claim in these documents | How to check it in one step | If it moved |
|---|---|---|
| The auth package is installed and unused | `grep authentication Packages/manifest.json`, then `grep -r AuthenticationService Assets` | Phase 1 may be partly done. Read it before rebuilding it. |
| Discovery is UGS Lobby, connection is UGS Relay | Read the header of `Assets/TumbangPreso/Runtime/Net/ServerQuery.cs` | The whole of §§ 0.3, 7 and 8 assumes UGS. Re-cost them. |
| The input map has no gamepad or touch bindings | `grep -c Gamepad Assets/TumbangPreso/Resources/TumbangPreso.inputactions` | Phases 14 and 15 shrink a lot. |
| Build targets are Windows, WebGL, Linux server only | `ls "/c/Program Files/Unity/Hub/Editor/*/Editor/Data/PlaybackEngines/"` | Phase 15 step 1 may already be done. |
| There is no localisation and no colourblind support | `grep -rn "Locale\|colourblind\|colorblind" --include=*.cs Assets` | Phase 16 shrinks. |
| The roster is 18 characters, 6 lata, 10 tsinelas, 3 maps | `Packages/com.tumbangpreso.core/Runtime/Roster.cs` | Every content count in these files is wrong. Fix them. |
| Scoring is one host-side writer | `grep -n AddScore Assets/TumbangPreso/Runtime/MatchDirector.cs` | § 8's corroboration design may no longer be the right shape. |
| `NetSession.ProtocolVersion` is a gate between builds | `grep -n ProtocolVersion Assets/TumbangPreso/Runtime/Net/NetSession.cs` | Read the current number rather than quoting one from here. |
| The free tiers named in § 0.3 still exist at those shapes | Check the service's own pricing page | ⚠️ **Vendor free tiers change without notice. Never quote a specific quota from this file to anybody.** |
| Passive defence pays 900 a round against 100 for a knockdown | `docs/Design.md` and `Balance.cs` | Arguments in `INSPIRATION.md` §§ 2.15 and 4.2 rest on this. |

⚠️ **AND THE NUMBERS IN THESE FILES ARE ILLUSTRATIONS, NOT BALANCE.** Every rating step, XP curve,
tier name, challenge target, band width and threshold written here is a starting point for a
measurement, not a value to ship. `docs/Design.md` is the balance source of truth and nothing in
these two files may contradict it without going through `Design.md` first.

**Maintenance rule.** When a phase ships, mark its heading `✅ SHIPPED <date>`, move its numbers
into `docs/Design.md` or `docs/TODO.md` where they belong, and leave the phase text in place as the
record of why it is shaped the way it is. **Do not delete a shipped phase.** The reasoning is the
part that stays valuable.


---

## PHASE 1 · ACCOUNTS AND IDENTITY

**The first part of the overhaul, in his words.** Everything else keys off a stable player id.

**What exists:** `com.unity.services.authentication` 3.7.4, installed and unused.
`UnityServices.InitializeAsync()` already runs for Lobby and Relay, so the SDK is up before
anything here needs it.

### 1.1 Sign-in, in the order that does not annoy anybody

- **Anonymous on first launch, silently, before the menu is interactive.** A player who never
  makes an account still gets a `PlayerId`, a profile and progression. **This is the whole trick:
  never block a first-time player on a form.**
- **Upgrade to a real account later**, offered at the moment they first earn something worth
  keeping rather than on a settings page nobody opens. Username plus password
  (`AddUsernamePasswordAsync`) needs no mail server and therefore costs nothing.
- **Sign in on a second device** by username, migrating the anonymous progress across.
- **Session persistence** so a returning player is signed in before the menu draws.

### 1.2 Email, and the honest version of it

🧑 asked for email. Be clear about what it buys and what it costs:

- **UGS username and password does not do email recovery.** There is no built-in "forgot
  password" mail. Adding one needs a transactional mail sender, and the free tiers that exist are
  Brevo at 300 mails a day and Resend at 3000 a month. Either is enough forever at this size.
- **So: username and password first, email as an OPTIONAL recovery field second.** Store the
  address in the profile, verify it with a code, and use it only for password reset and for a
  season-end summary if he ever wants one. **Never make it required to play**, because a required
  email at first launch is measurably the largest drop-off point any game of this size has.
- ⚠️ **An email address is personal data and this changes the obligations.** Once one address is
  stored, account deletion and data export stop being nice-to-haves. Build both in this phase,
  where they are an afternoon, rather than after launch, where they are a migration.

### 1.3 The identity fields, in full

| Field | Rule |
|---|---|
| `PlayerId` | UGS, immutable, never shown. |
| Username | Sign-in credential, unique, immutable after creation. |
| Display name | 3 to 16 characters, changeable once every 30 days, profanity filtered, **not unique**. |
| Discriminator | A 4-digit tag appended to the display name so uniqueness is not needed. `MATTHEW#4417`. |
| Email | Optional, verified, recovery and nothing else. |
| Bio | 140 characters, filtered, reportable, off by default until the player writes one. |
| Country flag | Optional, chosen not detected. Matters for a regional esport and it is free. |
| Pronouns | Optional, from a short list plus a free field. Cheap, and it costs nothing to be decent. |
| Avatar | See 1.4. |
| Privacy | Who can see the profile, the match history and the stats: everyone, friends, nobody. |
| Created date | Shown on the profile. Founding players like knowing they were early. |

### 1.4 The profile picture, and a strong recommendation

🧑 asked for photo upload. **Do the in-game avatar builder first and the photo upload second, or
possibly never.** The reasons are practical rather than squeamish:

- An uploaded image is **content moderation**, permanently, run by five students. The first time
  somebody uploads something vile it is on a nameplate in a tournament stream.
- It is **storage and bandwidth** against a free tier, forever, growing.
- It **fights the art direction**. A JPEG face beside the voxel cast and the nine-patch UI looks
  like a bug.

**The avatar builder gets 90 per cent of the want at 5 per cent of the cost**, and this codebase is
unusually ready for it: pick a character, a pose, a palette, a background and a frame, and the
game renders the portrait locally from those ids. **The avatar is five integers on the wire**, it
needs no storage, no upload, no moderation and no bandwidth, it looks like the game, and it
becomes a Phase 5 unlockable surface immediately. `ModelPreview` already renders exactly this.

**If real uploads are still wanted after that:** cap at 256x256 and 100 KB, put them in Cloud Save
Files, allow them only for accounts with a verified email, show them only to friends until a
player has finished 10 matches, and put a report button on every one. Write that policy down
before the first upload, not after.

### 1.5 Traps

- ⚠️ **The lobby name today is whatever the peer sends.** Once accounts exist the name must come
  from the profile and be validated server-side, or the first thing anyone does with the new
  system is impersonate somebody. `LobbySeatInfo` is where it lands.
- ⚠️ **Anonymous credentials live in `PlayerPrefs` and a player who clears them is gone forever.**
  Say so in the UI at the moment they earn their first unlock.
- ⚠️ **Offline must still boot.** The game is played daily off a Windows build, sometimes with no
  connection. A failed sign-in degrades to a local profile and a visible "not signed in" state; it
  never blocks Practice, Training or LAN.
- ⚠️ **Age.** If under-13s play, storing an email brings COPPA-shaped obligations. The safe design
  is the one above: email optional, and no email at all for an account that has not confirmed an
  age gate.

**Done looks like:** a fresh install reaches the menu signed in with no prompt, the id survives a
restart, a username can be attached later without losing anything, an account can be deleted and
its data exported, and pulling the network cable still lets a LAN match start.

**The prompt for this phase is [§ 19.1](#191-prompt-for-phase-1).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 2 · THE PROFILE, THE STATS AND THE MATCH HISTORY

**Why second:** a rank with nothing under it is a number. Stats are the cheapest retention feature
in the game and they make every later balance argument answerable.

### 2.1 The profile screen, laid out

1. **Header card.** Avatar, display name and tag, country flag, title, level and border, current
   rank badge per mode, peak rank, account age. Designed so one screenshot is the whole flex.
2. **Career strip.** Matches played, matches won, win rate, hours, favourite character, favourite
   tsinelas, longest win streak.
3. **Mode tabs.** Classic and Hero Strike, never merged, because they are separate games.
4. **Stat blocks**, filterable by season, by last 20 matches, and by character.
5. **Match history.** Twenty rows, paged, each row: mode, map, placement, score, character,
   duration, date, and a coloured left edge for placement. Clicking opens the detail.
6. **Match detail.** Full four-player scoreboard, per-round breakdown, who was taya each round,
   every player's per-stat line, and a replay link once Phase 17 exists.
7. **Character mastery grid.** Eighteen tiles, each with level, win rate and games.
8. **Achievement and highlight shelf.** The rare things, pinned by the player.
9. **Compare.** Put a friend's numbers beside yours. Cheap to build, and it is what makes a stat
   page get shared instead of read once.

### 2.2 The stats worth tracking, and why each one earns its row

| Stat | Why it is not filler |
|---|---|
| Knockdowns, and knockdowns per throw | The headline attacker number, with the rate so volume does not fake it. |
| Retrievals, and retrievals under pressure | `VISION.md` § 0: the tension is the retrieval. This measures the actual game. |
| Tags as taya, and tags per round defended | The defender headline. Fair by construction because everyone defends. |
| Passive defence seconds | The known balance risk, on the record where it can be argued about. |
| Sabotages | The third verb, otherwise invisible. |
| Average time to first throw | Reads aggression, and separates two players on the same score. |
| Longest survival as last attacker | The clip-worthy one. |
| Shove and lunge hit rate | The skill floor of the melee game. |
| Distance travelled per round | Reads playstyle: campers and runners look different here. |
| Per-character and per-tsinelas win rate | Feeds Phase 5 and every balance pass forever. |
| Placement distribution | A 4-player game has four outcomes, not two. Show all four. |
| Clutch rate | Matches won from last place at the final round. The stat people brag about. |

⚠️ **DO NOT SHOW A STAT YOU WILL NOT DEFEND.** Every number on a public profile becomes an
argument in a lobby. If a stat is noisy at low sample size, hide it until the sample supports it
and say why.

### 2.3 Where it lives

- `PlayerProfile` in **UGS Cloud Save**: identity, level, XP, career totals, per-mode records,
  per-character records, inventory, rank.
- `MatchRecord` written **once per match, by one writer**, carrying the whole scoreboard.
- ⚠️ **The host writes the record and that is a known hole, not an oversight.** Fine for unranked.
  Phase 8 closes it before ranked exists. **Write it through a Cloud Code endpoint from day one**
  even though it is spoofable now, because retro-fitting the call site later is the expensive half.
- Match history retention: keep 100 full records per player and roll the rest into the totals.
  Storage is the free tier's real limit and 100 is far more than anybody scrolls.

**Done looks like:** finishing a match writes exactly one record, the profile updates without a
reload, career totals survive a reinstall on the same account, and the whole thing is one Cloud
Code invocation per match rather than one per event.

**The prompt for this phase is [§ 19.2](#192-prompt-for-phase-2).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 3 · TELEMETRY, EARLY ON PURPOSE

**Do this alongside Phase 2, not at the end.** Every argument in phases 4 through 17 is settled
faster with a week of real numbers than a week of opinions, and this codebase already believes
that: `docs/VISION.md` § 5 is "verify by measuring" and there are sixty test probes to prove it.

Track: matches started and finished, leave rate by round, mode split, character and tsinelas pick
and win rates, match length distribution, queue times by rating band, crash and disconnect rate,
FPS distribution by hardware, settings actually used, and **where a first-time player stops**.

⚠️ **The first-launch funnel is the most valuable number in this document.** Launch, sign-in, menu,
first queue, first match started, first match FINISHED. It tells you what to fix before anything
else here is worth building, and it is about forty lines of code.

**The prompt for this phase is [§ 19.3](#193-prompt-for-phase-3).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 4 · PROGRESSION: XP, LEVELS AND A FREE SEASON TRACK

**The point:** give a player who just lost a reason to queue again. Rank goes down when you lose.
**Progress must never go down.** That asymmetry is the engine of every live game that works.

- Account XP from completion, placement, and a small set of per-match objectives. **Weight
  completion heavily and placement lightly**, so leaving is the only thing that costs.
- Account level, uncapped, with a new border every 50.
- A **free season track**, about 50 tiers, entirely cosmetic. There is no paid track and there is
  no money in this game.
- Per-character mastery, separately: play Cheska, level Cheska, unlock Cheska's things.
- A soft currency earned per match, spendable on a rotating cosmetic shop. **No purchase path.**
- ⚠️ **Every reward on every track is cosmetic or expressive.** A player queuing ranked against
  someone 40 levels above them must be facing a better player, never a stronger account.

⚠️ **AN AFK PENALTY HAS TO EXIST BEFORE XP DOES.** The moment completion pays, standing still
pays. Reuse the input telemetry the bots already produce to detect a seat that has not acted for a
whole round, pay it nothing, and escalate on repeats.

⚠️ **DIMINISHING RETURNS, NOT A DAILY CAP.** A cap tells a player to stop playing, which is the
opposite of the point. Curve the XP down after a few hours instead, so the tenth match of the day
is still worth something.

**The prompt for this phase is [§ 19.4](#194-prompt-for-phase-4).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 5 · COSMETICS AND CHARACTER CUSTOMISATION

🧑: *"we also wanna have customizable characters in the future"*. This is the cosmetic half; Phase
10 is the skills half, and they are separated on purpose.

**Slots:** body palette, headwear, face, back item, tsinelas skin, can skin, avatar frame,
nameplate, banner, title, emote wheel, victory pose, throw trail, knockdown effect.

**Sources, all free:** season track, character mastery, ranked season rewards, event challenges,
achievements, and the soft currency shop. **No lootboxes, no gacha, no real money.**

**What makes it cheap here:** `RosterBook` and `RosterEntryAsset` already resolve id to model,
palette, tint and clips, `ToonSkin`'s palette remap already recolours a whole character from 16
slots per renderer, and `docs/wearables_catalog.md` already defines the wearable contract. **A
colour variant of any character is nearly free today.**

⚠️⚠️ **GIVE COSMETICS STRING IDS, NOT WIRE INDICES.** Every cosmetic id is something another peer
resolves. `Roster.Slippers` records at length what inserting a row into a wire-facing list does.
Pay the few extra bytes: it removes the entire class of bug permanently and this is the last cheap
moment to decide it.

⚠️ **A COSMETIC MUST NEVER CHANGE A SILHOUETTE ENOUGH TO CHANGE A READ.** This is a game about
seeing which of three attackers is committing. Headwear that doubles a character's height is a
competitive change wearing a cosmetic label. Bound the volume and write the bound down.

⚠️ **Preview through `ModelPreview` with the real shader, never a flat icon.** This project already
learned that a render from one camera is not evidence about another.

**Two extras that are worth more than they cost:** a **favourite loadout per character**, so
switching character does not mean re-dressing, and **duplicate protection** in the shop, so the
currency never buys something already owned.

**The prompt for this phase is [§ 19.5](#195-prompt-for-phase-5).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 6 · SOCIAL

- Friends by id, by display name and tag, and by a share code.
- Presence: online, in menu, in queue, in a match, spectating.
- **Parties**, which is the real retention driver, because a game people come back to is a game
  their friends are in. A party of 2, 3 or 4 queues together.
- Invites from the friends list and **from the end-of-match screen**, which is the
  highest-converting social prompt any game of this shape has.
- Recent players, with the match they were in, and a one-click add.
- Blocking, which must survive matchmaking: a blocked player is never queued into your match.
- Clubs later, and only if friends and parties are being used.

⚠️ **A PARTY OF FOUR IS A FULL MATCH, WHICH IS A RANKED PROBLEM.** Four friends can arrange
results between themselves. Either exclude full parties from ranked or accept only partial ones.
Decide it in Phase 9 and assert it in a test.

⚠️ **Extend `LobbyChat`.** It carries hard-won layout notes and there must never be a second one.

**The prompt for this phase is [§ 19.6](#196-prompt-for-phase-6).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 7 · MATCHMAKING

- **Tier 1, free and available today: a Lobby-backed queue.** The game already browses UGS Lobby
  every 4 s and resolves join codes. QUICK MATCH queries for a joinable lobby whose advertised
  rating band contains the player, joins it, and hosts one advertising its own band if none is
  found. That is skill-based matchmaking with no matchmaker service and no cost.
- **Tier 2, later: UGS Matchmaker** with real tickets and pools. The queue UI does not change.
- Band widening so a queue never dead-ends: start at plus or minus 100 rating, widen by 100 every
  15 s, stop at plus or minus 500 and take anybody. **Show the widening**, so a long queue reads as
  progress rather than as a hang.
- Backfill: a match that loses a player advertises the seat rather than dying.
- Region from Relay's own list. **Manila to Singapore measured 48 ms**, which is the number this
  game is tuned against.
- **Separate pools by input device and by platform** (Phases 14 and 15), which is free and removes
  the entire aim-assist argument before it starts.

⚠️⚠️ **A 4-PLAYER FREE FOR ALL MATCHES DIFFERENTLY FROM A TEAM GAME, AND THIS IS THE PART TO GET
RIGHT.** There is no team to balance. The job is not "make two sides equal", it is "put four
players of similar skill in one room". **The quality metric is the SPREAD of the four ratings**,
not the gap between two averages. A lobby with one 1400 and three 900s is a bad match even though
every team-based fairness formula calls it balanced.

⚠️ **THE TAYA ROTATION IS WHAT MAKES THIS FAIR AT ALL**, and it is worth saying in the queue UI:
everyone defends once, so a bad first round is not a lost match.

**The prompt for this phase is [§ 19.7](#197-prompt-for-phase-7).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 8 · COMPETITIVE INTEGRITY

⚠️⚠️ **PHASE 9 DEPENDS ON THIS. IT IS THE PHASE THAT DECIDES WHETHER ANY RANK MEANS ANYTHING.**

### 8.1 Corroborated results, because the host is a player

`MatchDirector.AddScore` runs host-side and the host is one of the four. A modified client that
hosts can award itself anything.

**The zero-cost answer is corroboration.** Every peer independently derives the scoreboard from the
events it received and submits it. The Cloud Code endpoint accepts the result only when **all four
agree**, and flags the match when they do not. It does not stop four colluding players; it does
stop the overwhelmingly most likely attack, which is one player editing their own client. It costs
one extra invocation per match and no money.

⚠️ **The clients already have everything needed.** Every peer sees every scoring event, because
that is how the HUD stays in sync. Nothing new has to cross the wire.

### 8.2 The real answer, for the day there is a budget

Dedicated servers through Multiplay, with the host role off a player entirely. Everything above is
arranged so this slots in behind the same endpoint without touching ranked, progression or profile
code.

### 8.3 The rest

- Reporting from the end-of-match screen and the profile, with a reason.
- **Leaver penalties that distinguish a leave from a disconnect** using the reconnect window
  `LobbySession` already implements, or a player with bad internet is punished for their ISP.
- Escalating queue cooldowns, and rank loss for a ranked leave.
- Rate limits on every write, because a free tier is a budget an abusive client can spend.
- Sanity checks on submitted records: impossible scores, impossible durations, impossible rates.
- **Smurf handling:** a new account with a very high early win rate gets a wide rating deviation
  and climbs fast. Glicko-2 does this for free if the deviation is not clamped too tightly, which
  is a real argument for it over plain Elo.

**The prompt for this phase is [§ 19.8](#198-prompt-for-phase-8).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 9 · RANKED, RATING AND SEASONS

⚠️⚠️ **DO NOT BUILD THIS BEFORE PHASE 8.1.** A rank a host can award themselves is worse than no
rank, because it turns every good player's win into an accusation.

- **A rating per mode.** Classic and Hero Strike get separate ladders, because they are separate
  games and a player who is 1600 in one may be 900 in the other.
- **Glicko-2, adapted for a 4-player free for all.** Elo is a two-player system and this is not a
  two-player game. Resolve a result as **six pairwise outcomes** (1st beat 2nd, 1st beat 3rd, and
  so on), feed all six in, and scale the step so one match moves a settled player about as much as
  one game should. Glicko-2's rating deviation is what makes a new player converge in ten games
  instead of a hundred, and it is what makes smurf handling free.
- **Score margin matters a little.** Finishing 1st by 40 is not the same as by 400. Cap the margin
  multiplier around 1.25x so a stomp is worth more than a squeak without making the ladder a
  farming exercise.
- **Visible tiers over the hidden number**, named in the game's own voice rather than
  Bronze-to-Diamond. Suggested shape, five tiers of three divisions plus a numbered apex:
  **BATA, KANTO, BARANGAY, KAMPEON, ALAMAT**, with the apex a live leaderboard. 🧑 names them.
- **Placement matches:** five to place, wide deviation until then, no tier shown while placing.
- **Seasons:** ten weeks. Soft reset toward the mean, never a wipe. Keep a permanent peak on the
  profile, because the peak is the thing people brag about.
- **No decay.** Decay punishes people with jobs and school, which is this whole audience. If the
  apex ever needs it, apply it only there.
- **A demotion buffer.** Falling out of a tier needs two losses at the floor, not one, because a
  tier badge lost to one bad game is the most common reason people stop queueing ranked.
- **Rewards:** a season border, a tier emblem on the nameplate, and a tsinelas or can skin earnable
  no other way. All cosmetic, all Phase 5 content, all free.

**The prompt for this phase is [§ 19.9](#199-prompt-for-phase-9).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 10 · LOADOUTS, SKILL VARIANTS AND ACHIEVEMENTS

🧑: *"you know how overwatch or drg has unlockable skill paths or some shit?"*, then, clarifying
what he actually meant: *"like skill / ability / passive variations, for example in drg theres
diff guns"*, and how they are earned: *"u get them like in cod u finish quests or like ror2"*.

**That is a loadout system with challenge-gated unlocks, and it is a better feature than the
"skill tree" reading it replaced.** The full design, the model it borrows from, the swap-at-role-
change idea and the reasoning live in [`INSPIRATION.md`](INSPIRATION.md) § 5. The short version:

- Each hero gets **a small pool of options per slot** (skill 1, skill 2, ultimate, passive), not a
  ladder of upgrades. You pick a build before the match, the way you pick a primary in Deep Rock.
- **Every option is a sidegrade at the same ability budget.** Nothing unlocks more damage, range,
  duration or a shorter cooldown. A test asserts it.
- **Unlocks are Risk of Rain 2 style challenges**, character-specific and characterful, so earning
  the variant teaches the character. Not a level wall.
- ⚠️⚠️ **AND EVERY CHALLENGE MUST BE COMPLETABLE IN PRACTICE AGAINST BOTS.** That single rule is
  what makes the whole system safe in a competitive game: the gate costs time spent learning a
  character, never matches won against people, so nobody has to grind ranked to be equipped for
  ranked. `INSPIRATION.md` § 5.4 has the argument in full.
- **The build is public.** Shown in the lobby and on the scoreboard, because hidden loadouts in a
  four-player fight are information asymmetry that feels like cheating.
- **Achievements** are the same machinery pointed at bragging rather than at unlocks:
  `INSPIRATION.md` § 5.6.

**Then the part that is pure upside and has no balance risk at all:** per-character mastery paths
of things that change nothing. A signature victory pose, a character emote, a voice line set, a
nameplate, that character's own tsinelas, a colour variant, a title, a visible mastery number.
**Most of the grind should live here.** It is Phase 5 content wearing a Phase 10 structure.

**Classic gets its own path and never gets powers.** `VISION.md` § 1. Street Hype is already
Classic's identity layer: extend it with Street Hype titles, curve and bank recognitions and
streak records. Depth without abilities, which is the rule.

**The prompt for this phase is [§ 19.10](#1910-prompt-for-phase-10).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 11 · BOTS, BACKFILL AND THE POPULATION PROBLEM

**This phase is not glamorous and it is the difference between a game that lives and one that
does not.** A 4-player game with 30 concurrent players has a queue problem that no amount of
ranked polish fixes, and the fastest way to make a competitive game feel dead is an empty queue.

- **Difficulty tiers for bots**, exposed in Practice and in custom games. `AIController` exists and
  the bots already press the same buttons a human does, so this is tuning, not architecture.
- **Bot backfill of an abandoned seat**, so a 4-player match that loses somebody continues rather
  than collapsing. Mark the match unranked the moment a bot enters it.
- **Bot fill in casual queue after a wait threshold**, disclosed clearly in the UI. A 45-second
  queue that ends in a playable match beats a 4-minute queue that ends in nothing.
- **Never bots in ranked.** Not once, not "just to fill", not disclosed. That is the line.
- **A named practice ladder against bots**, so a new player can learn the game before meeting
  people. `GuidedTraining` already exists to build on.
- ⚠️ **Bots must be visibly labelled.** A player who thinks they beat a person and did not will be
  angrier when they find out than they would have been to know.

⚠️ **AND THE HARD PART IS THAT THE BOTS ARE A BALANCE INSTRUMENT TOO.** `BotBehaviourProbe`'s
numbers are liveness floors, never comparisons at n=1, and `docs/TODO.md` § 16 carries the noise
floor. Do not tune bot difficulty off one run.

**The prompt for this phase is [§ 19.11](#1911-prompt-for-phase-11).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 12 · MODES, MAPS AND CONTENT CADENCE

**A live game needs a reason to log in on a Tuesday, and new content is the most expensive answer
to that. So spend the cheap answers first.** Every mode below reuses the existing arena, rules and
art, which is why they are worth more per hour than a new map.

**Modes that cost little and add a lot:**

- **Daily seed.** The same starting state for everybody that day, one attempt, a leaderboard.
  Enormous retention for the work, and it needs no matchmaking at all. Build this first.
- **King of the Can.** Continuous rather than round-based, taya changes on knockdown. A five-minute
  mode for people who do not want a full set.
- **Time attack.** Solo, one can, retrieve under pressure from bots, ranked by time. Feeds
  practice and the daily seed.
- **Survival.** Co-op, three attackers against an escalating bot taya. Co-op is the mode that
  brings in players who bounce off competition, and this game has none.
- **Mirror.** Everyone gets the same character and tsinelas, chosen daily. The cheapest possible
  "new mode" and a genuinely good competitive format.
- **2v2**, which the taya rotation does not currently support and is real design work rather than
  a switch. Costed honestly here rather than assumed.

**Maps:** three exist and one has a design document. A map is the most expensive content in the
game. **Map rotation and a map vote are nearly free and buy most of the same freshness.** Build
those before building a fourth map.

**Custom games** are the multiplier on all of it: private lobby, password, round length, score
target, character and tsinelas restrictions, bot count, item toggles. Community formats come out of
custom games for free, and it is also the tournament tool from Phase 17.

⚠️ **AND THE HONEST CONSTRAINT: FIVE STUDENTS CANNOT SHIP A HERO EVERY SEASON.** Plan a cadence
that is actually sustainable, which is roughly one substantial thing per season: a hero OR a map OR
a mode, plus cosmetics, plus balance. A missed cadence is worse than a slow one.

**The prompt for this phase is [§ 19.12](#1912-prompt-for-phase-12).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 13 · SEASONS, DAILIES AND LIVE OPS

- **Daily challenges**, three, one rerollable. Achievable in two matches, worth XP.
- **Weekly challenges**, larger, worth a season tier.
- **A ten-week season** with a theme, a track, and an end-of-season summary card designed to be
  screenshotted.
- **Login streaks that pause rather than reset.** Punishing a break teaches people that missing a
  day is expensive, which is how they decide to stop entirely.
- **A rotating featured mode**, free once Phase 12 exists.
- **A live-ops calendar** in the repo, so the team knows what ships when and the players can see it.

⚠️ **CHALLENGES DRIVE BEHAVIOUR AND BAD ONES DRIVE BAD BEHAVIOUR.** "Get 10 knockdowns" teaches a
player to ignore the can, which is the one thing the game is about. Write challenges against
outcomes the game wants: retrievals under pressure, rounds survived as last attacker, matches
completed, tags as taya.

**The prompt for this phase is [§ 19.13](#1913-prompt-for-phase-13).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 14 · CONTROLLER SUPPORT

**Starting point: zero.** `TumbangPreso.inputactions` has one keyboard binding and one mouse
binding, no gamepad paths and no control schemes.

- Control schemes: Keyboard and Mouse, Gamepad, and later Touch. The Input System does this
  natively and `Rebinding` already exists for the map.
- Full gamepad bindings for every action, including the spectator context that
  `Rebinding.SpectatorContext` already names as a separate set.
- ⚠️ **`E` is contextual and that is the hard part.** Tap picks up, hold 1.25 s shoves, hold 2.5 s
  as taya resets the lata. A hold on a face button is fine; the on-screen prompt is what needs
  rebuilding, because it names a key today.
- **Glyph swapping on every prompt**, driven by the last device used, not by a setting.
- Rumble on knockdown, tag and can reset.
- **Full menu navigation on a stick**, which is a bigger job than the gameplay bindings, is the
  thing that always gets skipped, and is what blocks Phase 15 when it is.
- **No aim assist. Separate the pools instead**, which is free, exact, and removes the argument.

**The prompt for this phase is [§ 19.14](#1914-prompt-for-phase-14).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 15 · MOBILE

**Be honest about the size of this. It is a port, not a feature, and it is the largest item here.**

**Missing before a line is written:** the Android build module is not installed (only Windows
Standalone, WebGL and Linux Dedicated Server are). Installing it is free through Unity Hub. iOS
additionally needs a Mac to build on, which the team does not have, so **Android first and iOS only
if a Mac appears**.

1. Install the module and get a build onto a device, however ugly. Nothing else here means anything
   until that has happened once.
2. Touch controls: left stick, look drag, buttons for throw, grab, jump, sprint. The contextual `E`
   hold becomes a long press with a radial fill.
3. Performance. The toon shader draws an inverted hull per prop, doubling the draw calls on exactly
   the hardware least able to afford it. **Measure it on device**, then decide whether mobile drops
   the hull or gets a cheaper one. `docs/TODO.md` § 63 already records what the outline costs.
4. UI at phone aspect ratios. `AspectRatioProbes` already drives nine resolutions; add the phone
   ones rather than eyeballing it.
5. Cross-play with **separate pools**, same reasoning as controller.
6. Battery, thermals and a 30 FPS cap option.
7. Account continuity: the same account on phone and PC, which Phase 1 already gives.

⚠️ **THE PROTOCOL VERSION GATE IS AN ASSET HERE.** `NetSession.ProtocolVersion` already refuses
peers from different builds. Mobile and desktop must ship the same version at the same time or they
will refuse each other, correctly, and it will look like a bug.

**The prompt for this phase is [§ 19.15](#1915-prompt-for-phase-15).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 16 · ACCESSIBILITY AND LOCALISATION

**Two things the game has none of, both of which are cheaper now than at any later point.**

### 16.1 ⚠️⚠️ The colourblind problem this game specifically has

`Art_Direction.md` § 1 is a law that never bends: **orange is OFFENSE, blue is DEFENCE**, and the
whole HUD, every role marker and every readability decision rests on it.

**Orange versus blue is the one pairing that survives red-green colourblindness**, which is about
8 per cent of men, so the art direction is accidentally excellent for the most common case. But it
is close to the worst possible pairing for **tritanopia**, and more importantly **the roles are
currently distinguished by hue alone**. A colourblind player, a bad projector at a tournament, or a
cheap phone screen all produce the same failure: you cannot tell the taya from the attackers.

**The fix costs almost nothing and helps everybody:** carry the role in a **second channel** as
well as hue. A shape on the nameplate, an icon on the marker, a different outline weight. Then add
palettes for deuteranopia, protanopia and tritanopia that keep the two roles maximally separated in
whatever the player actually sees.

### 16.2 The rest of the accessibility list

Every one of these is small, and together they decide whether some people can play at all.

- UI scale and a larger-text option. The HUD is already nine-patch, so this is mostly layout.
- Full rebinding for every action, which `Rebinding` mostly gives already, plus **hold versus
  toggle** for sprint and for the contextual grab.
- FOV slider, camera shake slider, motion blur off, and a **reduced-effects mode** that also
  doubles as the low-end performance mode. `VISION.md` § 2's readability budget is the same
  argument from the other side.
- Subtitles and captions for callouts and ability sounds, which also help anybody playing muted.
- A high-contrast HUD option.
- Flash and strobe reduction, which `AbilityShowcaseProbe`'s 12 per cent screen-white bound already
  half implements. Extend that probe to assert the reduced mode is genuinely calmer.
- Colour-independent slipper highlights: the landed and owner rims are already player-chosen
  colours, so add a shape or a pulse as the second channel.

### 16.3 Localisation

**There is none: no language setting, no string table, every string inline.**

- Extract every user-facing string into a table. This is the boring half and it is the whole job.
- **English and Tagalog first**, and Tagalog is not a nice-to-have here: it is a Filipino street
  game, made by a Filipino team, entered in a Filipino competition. It is the marketing as much as
  the accessibility.
- Then Cebuano, which matters specifically because the nationals are in General Santos.
- ⚠️ **Darumadrop One has no `×` glyph** and the UI already works around it. Any language added
  must be checked against the font's coverage before it is promised.
- Keep the character names, the mode names and TSINELAS untranslated. They are the identity.

**The prompt for this phase is [§ 19.16](#1916-prompt-for-phase-16).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 17 · TOURNAMENTS, LAN, SPECTATING AND REPLAYS

⚠️ **PART OF THIS IS NOT FUTURE WORK. The nationals are in General Santos City and venue internet
cannot be assumed.** A tournament build that needs UGS to start a match is a tournament build that
can fail in the room. Whatever else is deferred, **make sure a full four-player match can be run
entirely on LAN with no internet at all, and test it with the router unplugged.** `LanBeacon`
exists; the question is whether every screen between the menu and the match survives UGS being
unreachable, and Phase 1 must not break it.

**What exists:** `SpectatorCamera` with free, follow and POV modes, a spectator pause that crosses
the wire, a HUD that already knows how to draw a broadcast clock, and `LobbySession`'s reconnect.
**This phase is closer to done than any other here.**

- **Tournament mode:** lobby password, fixed roster, fixed map, no matchmaking, spectator slots
  that do not consume a seat, and a match that can be restarted by an organiser.
- **Replays: record the input stream and the seed, not the frames.** The match is deterministic
  from `InputIntent` because a bot presses the same buttons a human does and one physics step
  serves both. That makes a 200 KB replay possible instead of a 2 GB video, and it is a genuine
  advantage this codebase already has. **Prove determinism with a test before building on it.**
- **Clip export:** a "save the last 30 seconds" key. This is the growth engine. A game nobody can
  clip is a game nobody posts.
- **Caster overlay:** four scores, four stamina bars, ultimate charge, nameplates, laid out for a
  stream rather than for a player.
- **Spectator delay**, configurable, so a stream cannot be sniped in a tournament.
- **Highlight detection** off the events already raised, so the clip worth posting can be found
  without watching the whole match.
- **A bracket page**, which can be a static site and needs no service.
- **An organiser's checklist document**, because the thing that loses a tournament is a build
  mismatch: `NetSession.ProtocolVersion` refuses peers from different branches by design, so every
  machine in the room must be on the same .exe and somebody has to be responsible for that.

**The prompt for this phase is [§ 19.17](#1917-prompt-for-phase-17).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## PHASE 18 · GETTING IT IN FRONT OF PEOPLE

- **itch.io first.** Free, no gatekeeper, and where a game like this finds its first hundred
  players. Automate the release with butler from a GitHub Action.
- **A WebGL build in the browser.** The module is already installed. One click from a link converts
  far better than a download, and this game is small enough to actually work there.
- **Steam is 100 USD**, the only unavoidable cost anywhere in this document, and worth it only once
  there is a reason for people to arrive.
- **A trailer that shows a retrieval and a near miss**, not a montage of throws. `VISION.md` § 0:
  the tension is the retrieval.
- **A Discord**, which is free, and is where a game this size actually retains its players. One
  channel for finding a fourth is worth more than any matchmaking work at low population.
- **The Filipino angle is the marketing and it is not a gimmick.** Nobody else has made this.
- ⚠️ **No vendor or middleware names in any public material**, per the standing rule.

**The prompt for this phase is [§ 19.18](#1918-prompt-for-phase-18).** Every prompt in
this file lives in § 19 so there is one place to copy from. § 0.5 is the standing preamble each
one inherits and § 0.6 is what to re-verify before trusting any of them.

---

## Appendix A · Ideas ranked by value per hour of work

**Do these. They are all small and they all punch far above their cost:**

- **A post-match "best moment" card**, generated from events already raised, designed to be
  screenshotted. Almost free, and it is the thing that gets posted.
- **Nameplates**: title, emblem, mastery number, visible in the lobby and on the scoreboard. The
  cheapest status system that exists and people care about it enormously.
- **A career page that is one screenshot.** Peak rank, best match, favourite character, hours.
- **Endorsements**, Overwatch style: after a match, endorse one opponent, level shown on the
  nameplate. The only anti-toxicity system that has ever measurably worked, and it costs one button.
- **A ping and comm wheel.** Does most of what voice chat does for none of the cost, none of the
  moderation liability and none of the toxicity.
- **A practice range** with a can, a ghost defender and a retrieval trainer. `GuidedTraining`
  already exists to build on.
- **The daily seed** from Phase 12. Enormous retention for the work, needs no matchmaking.
- **"Save last 30 seconds" clip export** from Phase 17. The single best growth feature here.
- **A first-match funnel fix**, whatever Phase 3 says it is. Cheaper than every feature above and
  usually worth more than all of them.

**Do these later:**

- Ability variants shown as a build on the profile, so people argue about builds.
- Map rotation and map voting.
- Achievements, internal now and on a store later.
- Photo mode, which is free marketing.
- Dynamic music that reacts to the round state, and a Filipino soundtrack angle.
- A creator mode for maps, which is the largest possible project and the only one that makes the
  game outlive the team.

**Do not do these:**

- ⚠️ **Loot boxes, gacha or anything with a purchase.** There is no budget, so there is no payment
  processing, and a game whose progression is honest is worth more to this team than a monetisation
  model it cannot legally operate.
- ⚠️ **Power that unlocks.** Said three times in this document because it cannot be undone after
  players have ground for it.
- ⚠️ **Voice chat.** Cost, moderation liability, and a ping wheel does the job for a fraction of
  everything.
- ⚠️ **A fourth map before map voting exists.** Voting buys most of the same freshness for a tiny
  fraction of the work.
- ⚠️ **Uploaded profile photos before there is somebody whose job is to moderate them.** § 1.4.

## Appendix B · What kills this, ranked by how likely it is

1. **Population.** A 4-player game at 30 concurrent players has a queue problem that no ranked
   polish fixes. **Phase 11 is the mitigation and it is why bots and backfill are a phase rather
   than a footnote.** Also why the Discord in Phase 18 is not decoration.
2. **Scope against five students.** Every phase here is weeks. Doing three of them well beats
   starting eight. Appendix C is the honest order.
3. **Cheating, discovered after ranked ships.** Phase 8 before Phase 9 exists for this. A ladder
   that loses trust does not get it back.
4. **The Relay free tier.** It is the first thing that will actually run out. Watch the bandwidth
   number from the day Phase 7 ships and know what the fallback is before it matters.
5. **Progression that touched power.** Recoverable only by taking something away from players who
   earned it, which is the worst thing a live game can do.
6. **A tournament lost to a build mismatch or venue internet.** Phase 17's urgent half.
7. **The team burning out on live ops.** A cadence that is announced and missed is worse than a
   slower one that is kept.

## Appendix C · The order to actually do it in, if there is one month

1. **Phase 1**, accounts. About a week, and it unblocks everything.
2. **Phase 2**, profile, stats and match history, plus **Phase 3**'s first-launch funnel. A week.
3. **Play it for a week and read the funnel.** Fix what it says, not what feels next.
4. **Phase 4**, XP, levels and the free track. This is the phase that makes a player come back
   tomorrow, and it needs no matchmaking, no ranked and no integrity work to be worth having.
5. Then choose between **Phase 7 plus 8 plus 9** (the competitive spine, all three or none) and
   **Phase 5** (customisation, more fun per hour, no integrity risk).

⚠️ **RANKED IS THE MOST EXCITING PHASE AND IT SHOULD NOT BE FIRST.** It needs matchmaking under it,
integrity beside it and a population to fill it. Built too early it is a ladder with four people on
it, and an empty ladder is the fastest way to make competitive play feel dead.

⚠️ **AND THE URGENT ITEM IS NOT ON THE LEFT COLUMN AT ALL.** § 17's first paragraph: confirm the
game runs a full four-player match on LAN with the internet unplugged, before the nationals, and
keep it true through every phase above.
---

## 19 · THE PROMPTS

**One place to copy from.** Every prompt below is written to be pasted into a fresh session as its
entire brief. They are deliberately uniform: same opening, same shape, same closing, so a session
that has run one knows how to run any of them.

⚠️ **Each one names § 0.5 rather than repeating it.** That is on purpose: nineteen copies of the
same rules is nineteen copies to fix. The prompt tells the session to go and read it, which it will.

⚠️ **And each one carries a VERIFY FIRST block**, because these were written on 2026-08-31 against
a codebase that keeps moving. A prompt that turns out to be wrong is handled by § 0.5 rule 11, not
by building it anyway.

| Prompt | Phase | Depends on | Rough size |
|---|---|---|---|
| [§ 19.1](#191-prompt-for-phase-1) | Accounts and identity | Nothing | About a week |
| [§ 19.2](#192-prompt-for-phase-2) | Profile, stats, match history | 1 | About a week |
| [§ 19.3](#193-prompt-for-phase-3) | Telemetry | 1 | A day, do it with 2 |
| [§ 19.4](#194-prompt-for-phase-4) | XP, levels, season track | 1, 2 | Days |
| [§ 19.5](#195-prompt-for-phase-5) | Cosmetics and customisation | 1, 2, 4 | Over a week, mostly content |
| [§ 19.6](#196-prompt-for-phase-6) | Social, friends, parties | 1 | Days |
| [§ 19.7](#197-prompt-for-phase-7) | Matchmaking | 1, 2, 6 | Days |
| [§ 19.8](#198-prompt-for-phase-8) | Competitive integrity | 2 | Days. **Blocks 9.** |
| [§ 19.9](#199-prompt-for-phase-9) | Ranked | 1, 2, 6, 7, **8.1** | About a week |
| [§ 19.10](#1910-prompt-for-phase-10) | Loadouts and achievements | 4, 5 | Weeks, mostly content |
| [§ 19.11](#1911-prompt-for-phase-11) | Bots, backfill, population | Nothing hard | Days |
| [§ 19.12](#1912-prompt-for-phase-12) | Modes, maps, custom games | Nothing hard | Weeks |
| [§ 19.13](#1913-prompt-for-phase-13) | Seasons and live ops | 2, 4 | Days |
| [§ 19.14](#1914-prompt-for-phase-14) | Controller | Nothing | About a week |
| [§ 19.15](#1915-prompt-for-phase-15) | Mobile | 14 | **The biggest item here** |
| [§ 19.16](#1916-prompt-for-phase-16) | Accessibility and localisation | Nothing | About a week |
| [§ 19.17](#1917-prompt-for-phase-17) | Tournaments, LAN, replays | Nothing | ⚠️ **Its first step is urgent** |
| [§ 19.18](#1918-prompt-for-phase-18) | Distribution | A build worth shipping | Days |

---

### 19.0 PROMPT ZERO: refresh this plan before using it

**Run this first if it has been more than a month, or if any prompt below looks wrong.** It costs
one short session and it is cheaper than building a phase against a stale brief.

> Read `CLAUDE.md`, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5 and
> 0.6, then `docs/INSPIRATION.md` § 0. They carry the rules of the repo, what the game is for, what
> is open, and the standing rules for this task. Do not skip them because this prompt summarises
> the task; the summary is not the rules.
>
> **Task.** `docs/FUTURE.md` and `docs/INSPIRATION.md` are plans written on 2026-08-31 ahead of the
> work they describe. Bring their factual claims back in line with the code, and change nothing
> else.
>
> Work through the table in `docs/FUTURE.md` § 0.6 row by row and run each check. Then sweep both
> documents for anything else that has moved: content counts, file and class names, section
> numbers in other documents, package versions, service names, and any phase that has partly or
> wholly shipped since. For each phase that shipped, mark its heading `✅ SHIPPED <date>`, move its
> numbers into `docs/Design.md` or `docs/TODO.md` where they belong, and **leave the phase text in
> place**, because the reasoning is the part that stays valuable.
>
> ⚠️ **Correct facts. Do not rewrite arguments.** If the design reasoning in a phase now looks
> wrong to you, say so at the top of your handoff and leave the text alone. A session refreshing
> facts is not the session that gets to change the plan.
>
> ⚠️ **Do not quote a vendor's free-tier quota anywhere**, in these files or to anybody. They
> change without notice. Name the service and what runs out first, never the number.
>
> **Done when:** every row of § 0.6 has been checked and the result recorded, both documents match
> the code, `docs/TODO.md` records what changed, and it is committed and pushed. No `.cs` file is
> touched by this task.

---

### 19.1 Prompt for Phase 1

**Accounts and identity. Nothing else in this plan works without it.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 1. They carry the rules of the repo, what the game is for, what
> is open, the standing rules every prompt in that file inherits, and this phase's brief. Do not
> skip them because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** This brief was written 2026-08-31. Check these before acting:
> `grep authentication Packages/manifest.json` shows the UGS authentication package installed, and
> `grep -rn "AuthenticationService" Assets` returns nothing, meaning it is present and unused. If
> either has changed, follow § 0.5 rule 11.
>
> **Build the account layer.**
> 1. Anonymous sign-in on first launch, silently, before the main menu is interactive. No prompt,
>    no form, no blocking UI. A player who never makes an account still gets a stable id, a profile
>    and progression.
> 2. An optional upgrade to username and password, offered at the moment the player first earns
>    something worth keeping rather than buried in settings, preserving everything the anonymous
>    account had.
> 3. Sign-in on a second device by username, migrating progress.
> 4. Session persistence, so a returning player is signed in before the menu draws.
> 5. A `PlayerAccount` service beside `NetSession` owning the id, display name, discriminator, bio,
>    country, pronouns, privacy flags and signed-in state, raising an event when any change.
> 6. Route the lobby's player name through it. Today the name is whatever the peer sends, and the
>    first thing anybody does with a new account system is impersonate somebody.
> 7. Account deletion and data export. Build them now, where they are an afternoon, not after
>    launch where they are a migration.
>
> **Two decisions that are already made, with reasons in § 1.4 and § 1.2. Do not relitigate them
> without saying why in your handoff.**
> - **The avatar is an in-game portrait composed from ids and rendered through `ModelPreview`, not
>   an image upload.** Uploads mean permanent content moderation run by five students, storage and
>   bandwidth against a free tier forever, and a photograph sitting beside a voxel cast.
> - **Email is optional and is recovery only.** It is never required to play. An address is
>   personal data and requiring one at first launch is the single largest drop-off point a game
>   this size has.
>
> **Constraints.** Everything degrades to a local-only profile when the service is unreachable,
> including LAN, joining by code, Practice and Training: § 0.5 rule 7, and § 17 for why. Name
> validation, discriminator allocation and local-versus-remote precedence go in
> `Packages/com.tumbangpreso.core/` with tests.
>
> **Done when** a fresh install reaches the menu signed in with no prompt, the id survives a
> restart, a username attaches later without losing anything, an account can be deleted and
> exported, a LAN match still starts with the network cable pulled, and § 0.5 rule 9 is satisfied.

---

### 19.2 Prompt for Phase 2

**The profile, the stats and the match history.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 2. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> **VERIFY FIRST.** Phase 1 must be shipped: `grep -rn "PlayerAccount" Assets` should find the
> service. Confirm `MatchDirector` still raises the scoring events this phase reads, and confirm
> the stat list in § 2.2 still matches the verbs the game has.
>
> **Build.**
> 1. A `PlayerProfile` document: identity, level, XP, career totals, per-mode records,
>    per-character records, inventory, rank.
> 2. A `MatchRecord` carrying the whole four-player scoreboard, **written once per match by one
>    writer**, through a Cloud Code endpoint. Write it through the endpoint from day one even
>    though it is spoofable until Phase 8, because retro-fitting the call site later is the
>    expensive half.
> 3. Collect the stats in § 2.2 host-side off events that already exist, and submit ONE payload at
>    match end. Never one call per event: § 0.3 says why.
> 4. The profile screen to the layout in § 2.1, using the existing UI kit and nine-patch art and
>    following `ConvertedScreen`'s conventions rather than inventing a screen type.
> 5. The end-of-match summary showing what this match added.
> 6. The offline path: a match played with no connection queues its record locally and submits on
>    the next sign-in.
> 7. Keep 100 full records per player and roll older ones into totals.
>
> **Constraints.** The record and profile shapes are engine-free types in
> `Packages/com.tumbangpreso.core/` with tests, so both toolchains compile them and the shape is
> asserted without an editor. ⚠️ **Do not show a stat you would not defend in an argument**: if a
> stat is noisy at low sample size, hide it until the sample supports it and say so in the UI.
>
> **Done when** finishing a match writes exactly one record, the profile updates without a reload,
> career totals survive a reinstall on the same account, the whole match costs one endpoint call,
> and § 0.5 rule 9 is satisfied.

---

### 19.3 Prompt for Phase 3

**Telemetry. Small, and it decides what to build next. Do it alongside Phase 2.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 3. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> **VERIFY FIRST.** Check whether an analytics package is already present and whether any events
> are already sent, so this does not become a second system.
>
> **Build.**
> 1. **The first-launch funnel first**, before anything else: launch, sign-in, main menu, first
>    queue, first match started, first match FINISHED. It is about forty lines and it is the single
>    most valuable number in this plan.
> 2. Then the match-level events in § 3.
> 3. Batched and sent once per session, never per event.
> 4. A visible opt-out in Settings that is honoured completely, and a plain statement of what is
>    collected.
>
> **Constraints.** No personally identifying field in any event, ever. Keep payloads small and
> event names stable: ⚠️ **a renamed event is a broken history**, so choose names once and write
> them into `docs/TODO.md` as the contract.
>
> **Done when** a full session produces one batch, the funnel can be read end to end for a new
> install, the opt-out actually stops all sending, and § 0.5 rule 9 is satisfied.

---

### 19.4 Prompt for Phase 4

**XP, levels, mastery and a free season track. This is the phase that makes a player come back
tomorrow, and it needs no matchmaking and no ranked to be worth having.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 4. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> **VERIFY FIRST.** Phases 1 and 2 shipped, and the Cloud Code endpoint that writes the match
> record exists, because XP is computed there and never sent by a client.
>
> **Build, in this order.**
> 1. **The AFK check first.** The moment completion pays, standing still pays. Detect a seat that
>    has not acted for a whole round using the input telemetry the bots already produce, pay it
>    nothing, and escalate on repeats. Do not build XP before this exists.
> 2. Account XP from completion, placement and a small set of per-match objectives, weighting
>    completion heavily and placement lightly, so leaving is the only thing that costs.
> 3. Account level, uncapped, a new border every 50.
> 4. Per-character mastery, separate from account level.
> 5. A soft currency earned per match.
> 6. A 50-tier free season track, entirely cosmetic. There is no paid track.
> 7. The end-of-match XP bar and the season track screen, existing UI kit.
>
> **Constraints.** Every curve and reward table is data in `Packages/com.tumbangpreso.core/` with
> tests, including one that asserts **no reward on any track changes a gameplay number** (§ 0.5
> rule 4). ⚠️ **Diminishing returns, never a daily cap**: a cap tells a player to stop playing,
> which is the opposite of the point.
>
> **Done when** a match awards XP computed server-side from its record, an AFK seat earns nothing,
> the track pays out, no reward touches a gameplay value and a test proves it, and § 0.5 rule 9 is
> satisfied.

---

### 19.5 Prompt for Phase 5

**Cosmetics, the inventory and character customisation.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 5 and `docs/wearables_catalog.md`. Do not skip them because this
> prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phases 1, 2 and 4 shipped. Confirm `RosterBook` and `RosterEntryAsset` still
> resolve id to model, palette and tint, and that `ToonSkin`'s 16-slot palette remap still works
> the way § 5 assumes, because a colour variant of any character being nearly free is the reason
> this phase is affordable.
>
> **Build.** The inventory on the profile, a per-character loadout of cosmetic slots, the
> customisation screen, and a rotating soft-currency shop with duplicate protection. Slots: body
> palette, headwear, face, back item, tsinelas skin, can skin, avatar frame, nameplate, banner,
> title, emote wheel, victory pose, throw trail, knockdown effect.
>
> **Constraints, and the first one is the expensive-to-fix one.**
> - ⚠️⚠️ **STRING IDS FOR EVERY COSMETIC, NOT WIRE INDICES.** `Roster.Slippers` records what
>   inserting a row into a wire-facing list does. Pay the few extra bytes; it removes the whole
>   class of bug permanently and this is the last cheap moment to decide it.
> - ⚠️ **A cosmetic must never change a silhouette enough to change a read.** This is a game about
>   seeing which of three attackers is committing. Bound the wearable volume, write the bound into
>   `docs/Art_Direction.md`, and test it.
> - Extend `RosterEntryAsset` and `RosterBook`; do not build a parallel content system.
> - Replicate the loadout through the seat info that already crosses at match start. No new
>   protocol.
> - Preview through `ModelPreview` with the real shader, never a flat icon.
>
> **Done when** a cosmetic can be earned, equipped, seen by every peer and previewed correctly, a
> test asserts no cosmetic changes any value read by the rules core, and § 0.5 rule 9 is satisfied.

---

### 19.6 Prompt for Phase 6

**Friends, presence, parties, blocking.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 6. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> **VERIFY FIRST.** Phase 1 shipped. Read `LobbyChat` before writing any chat code and read
> `ServerQuery`'s query interval before adding any polling.
>
> **Build.** Friends by id, by name and tag, and by share code. Presence: online, in menu, in
> queue, in a match, spectating. Parties of 2 to 4 that queue together. Invites from the friends
> list **and from the end-of-match screen**, which is the highest-converting social prompt a game
> of this shape has. Recent players with a one-click add. Blocking that matchmaking honours.
>
> **Constraints.** ⚠️ **Extend `LobbyChat`. There must never be a second chat system**, and it
> carries hard-won layout notes. Presence polling must not raise the service query rate: piggyback
> on the interval `ServerQuery` already runs. ⚠️ **A party of four is a full match, which is a
> ranked problem**: decide the rule in Phase 9 and assert it in a test there.
>
> **Done when** two accounts can befriend, see each other's presence, party up, queue together, and
> a block prevents a match, and § 0.5 rule 9 is satisfied.

---

### 19.7 Prompt for Phase 7

**Matchmaking, built on the lobby service that is already running.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 7 and `docs/INSPIRATION.md` § 3. Do not skip them because this
> prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phases 1, 2 and 6 shipped, and `docs/INSPIRATION.md` § 3's queue structure has
> been built or at least decided, because it determines what queues exist.
>
> **Build.** QUICK MATCH as a rating-banded queue on top of the existing lobby integration, with no
> matchmaker service. Advertise a band in the lobby data, search for a joinable lobby whose band
> contains the local player, host one if there is none, widen the band on a timer, and **show the
> widening in the UI so a long queue reads as progress rather than as a hang**. Backfill an
> abandoned seat. Honour blocks. Separate pools by input device and platform.
>
> **Constraints.**
> - ⚠️⚠️ **The match-quality metric is the SPREAD of four ratings, not the gap between two
>   averages.** There is no team to balance here. A lobby with one 1400 and three 900s is a bad
>   match even though every team-based fairness formula calls it balanced. Put the metric in
>   `Packages/com.tumbangpreso.core/` with a test for exactly that case.
> - This must not raise the query rate against the free tier.
> - Say in the queue UI that the taya rotates and everyone defends once. It is why a bad first
>   round is not a lost match, and the game has never said it out loud.
>
> **Done when** four clients queue and land in one match, the band widens visibly, backfill works,
> and § 0.5 rule 9 is satisfied.

---

### 19.8 Prompt for Phase 8

**Competitive integrity. ⚠️⚠️ PHASE 9 MUST NOT START UNTIL 8.1 IS DONE.**

> Read `CLAUDE.md` including § 4 on architecture invariants, then `docs/VISION.md`, then
> `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 8. Do not skip them
> because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phase 2 shipped. Confirm that every peer still receives every scoring event,
> because the whole corroboration design rests on the scoreboard being derivable on all four
> machines with nothing new crossing the wire.
>
> **Build.**
> 1. **Corroborated results.** Every peer independently derives the final scoreboard from the
>    events it received and submits it. The endpoint accepts only on unanimous agreement and flags
>    disagreement for review.
> 2. Reporting from the end-of-match screen and the profile, with a reason.
> 3. Leaver penalties that **distinguish a leave from a disconnect** using the reconnect window
>    `LobbySession` already implements, or a player with bad internet is punished for their ISP.
> 4. Escalating queue cooldowns.
> 5. Rate limits on every write, because a free tier is a budget an abusive client can spend.
> 6. Sanity checks on submitted records: impossible scores, durations, rates.
>
> **Constraints.** ⚠️ **Do not change how scoring works.** `MatchDirector.AddScore` stays the
> single host-side writer during the match, per `CLAUDE.md` § 4. This is a second, independent
> derivation for verification only.
>
> **Done when** a match submits four agreeing scoreboards and is accepted, a deliberately altered
> submission is rejected and flagged, `docs/TODO.md` records **exactly what this scheme does and
> does not stop including that it does not stop four colluding players**, and § 0.5 rule 9 is
> satisfied.

---

### 19.9 Prompt for Phase 9

**Ranked. ⚠️⚠️ DO NOT START THIS WITHOUT PHASE 8.1. A rank a host can award themselves is worse
than no rank, because it turns every good player's win into an accusation.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 9 and `docs/INSPIRATION.md` §§ 2.19 and 3.3. Do not skip them
> because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phases 1, 2, 6, 7 and **8.1** all shipped. Confirm the corroboration endpoint
> exists and is the only writer of a result, because ratings go through it and nowhere else.
>
> **Build.**
> 1. **Two independent ladders, Classic and Hero Strike.** Never one shared number: they are
>    separate games and a player can be 1600 in one and 900 in the other.
> 2. **Glicko-2 adapted for a four-player free for all**: expand each result into six pairwise
>    outcomes, feed all six in, scale the step so one match moves a settled player about as much as
>    one game should.
> 3. A score-margin multiplier capped around 1.25x, so a stomp is worth more than a squeak without
>    making the ladder a farming exercise.
> 4. Tiers with divisions and a numbered apex leaderboard. Names come from the game's own voice and
>    🧑 chooses them; § 9 has a suggested shape.
> 5. Five placement matches, wide deviation until placed, no tier shown while placing.
> 6. A seasonal soft reset toward the mean, never a wipe, with a permanent peak on the profile.
> 7. A demotion buffer: two losses at a tier floor, not one.
> 8. **Rank floors**, per `INSPIRATION.md` § 2.19: once a tier is reached the season cannot fall
>    below it. It costs one comparison and it removes the most common reason people stop queueing.
> 9. The party rule chosen in Phase 6, asserted in a test.
>
> **Constraints.** The whole rating model lives in `Packages/com.tumbangpreso.core/` with tests
> that assert convergence over a simulated season, that a placement player settles inside ten
> matches, and that a clearly stronger new account climbs out of a low band quickly. The Unity side
> is submission, display and the season boundary, nothing else. ⚠️ **No rank decay**: it punishes
> people with school and jobs, which is this entire audience. ⚠️ **Ranked changes stakes and
> integrity, never the rules in `docs/Design.md`.**
>
> **Done when** a simulated season converges, placements settle in ten, ratings are written only by
> the Phase 8 endpoint, and § 0.5 rule 9 is satisfied.

---

### 19.10 Prompt for Phase 10

**Loadouts, skill variants and achievements. ⚠️ Read § 5.4 of `INSPIRATION.md` before writing a
line: this is the phase that can quietly turn the competitive mode into an account power check.**

> Read `CLAUDE.md` first, then `docs/VISION.md` § 1 twice, then `docs/TODO.md`, then
> `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 10, `docs/INSPIRATION.md` § 5 and
> `docs/Hero_Strike_Balance.md`. Do not skip them because this prompt summarises the task; the
> summary is not the rules.
>
> **VERIFY FIRST.** Phases 4 and 5 shipped. Confirm the ability budget assumptions in
> `docs/Hero_Strike_Balance.md` still describe the shipped kits, because every option below is
> defined as a trade at an unchanged budget and that only means something if the budget is real.
>
> **Build, in this order, because the first half has no balance risk and the second half does.**
> 1. Per-character cosmetic and expressive mastery tracks for every character: victory pose,
>    character emote, voice line set, nameplate, that character's own tsinelas, a colour variant, a
>    title, a visible mastery number. **Most of the grind should live here.**
> 2. A Classic-only Street Hype track containing no abilities of any kind, extending what Classic
>    already has. `VISION.md` § 1: **Classic never gets powers.**
> 3. The Hero Strike loadout: a pool of options per ability slot, chosen before the match.
> 4. Achievements in the three tiers in `INSPIRATION.md` § 5.6, each paying a title, a badge or a
>    banner tracker so nothing is a dead list.
>
> **The rules that make this safe. All four are load-bearing.**
> - ⚠️⚠️ **Every option is a sidegrade at an unchanged budget.** Nothing unlocks more damage, range,
>   duration or a shorter cooldown. **Write a test that fails if any option is a strict improvement
>   on its siblings along every axis.** That test is what keeps this honest three seasons from now
>   when somebody adds option four in a hurry.
> - ⚠️⚠️ **Every unlock challenge must be completable in Practice against bots**, and a test must
>   assert it for every challenge in the set. This is the rule that dissolves the competitive
>   problem: the gate then costs time learning a character, never matches won against people.
> - ⚠️ **The build is public**, shown in the lobby and on the scoreboard. Hidden loadouts in a
>   four-player fight are information asymmetry that feels like cheating.
> - ⚠️ **Do not build the swap-at-role-change idea in this pass.** `INSPIRATION.md` § 5.5 explains
>   why it is the most interesting idea here and also a real balance risk. Prototype it in custom
>   games afterwards and write the measurement into `docs/TODO.md` before it goes near ranked.
>
> **Constraints.** Option definitions, budget arithmetic and challenge conditions live in
> `Packages/com.tumbangpreso.core/`. Hero Strike only.
>
> **Done when** a build can be chosen, seen by opponents, and unlocked by a challenge completed
> against bots, both tests above exist and pass, and § 0.5 rule 9 is satisfied.

---

### 19.11 Prompt for Phase 11

**Bots, backfill and the population problem. Not glamorous, and it is the difference between a game
that lives and one that does not.**

> Read `CLAUDE.md` including § 7.1, then `docs/VISION.md`, then `docs/TODO.md` §§ 10 and 16, then
> `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 11. Do not skip them because this prompt
> summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Read `docs/TODO.md` § 16 before tuning anything: it records that a single
> `BotBehaviourProbe` run spreads about 20 per cent, so one run is never a comparison.
>
> **Build.** Difficulty tiers for bots exposed in Practice and custom games. Bot backfill of an
> abandoned seat so a match continues rather than collapsing. Disclosed bot fill in the casual queue
> after a wait threshold, because a 45-second queue that ends in a playable match beats a
> four-minute queue that ends in nothing. A named practice ladder against bots for new players.
>
> **Constraints.**
> - ⚠️⚠️ **Never bots in ranked. Not once, not to fill, not disclosed.** A test must assert it.
> - A match a bot joins becomes unranked from that moment, and the humans who stayed take reduced
>   rating loss.
> - ⚠️ **Label every bot visibly** in the scoreboard and the nameplate. A player who thinks they
>   beat a person and did not will be angrier when they find out.
> - Tune tiers across several probe runs, never one.
>
> **Done when** a seat that leaves is filled within seconds, the match is marked unranked, ranked
> refuses bots and a test proves it, and § 0.5 rule 9 is satisfied.

---

### 19.12 Prompt for Phase 12

**Custom games first, then modes, then map rotation. Every mode is cheaper once custom games
exist.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/Design.md`, then `docs/TODO.md`, then
> `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 12. Do not skip them because this prompt
> summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Check what the map list actually is now rather than trusting § 0.2's count, and
> check whether custom lobbies already exist in any form before building a second path.
>
> **Build, in this order.**
> 1. **Custom games**: private lobby, password, round length, score target, character and tsinelas
>    restrictions, bot count, rule toggles. Everything else in this phase gets cheaper afterwards,
>    and it is also the tournament tool for Phase 17.
> 2. The daily seed mode, if `INSPIRATION.md`'s prompt I6 has not already delivered it.
> 3. Map rotation and a map vote. **Do these before building a fourth map**: voting buys most of
>    the same freshness for a fraction of the work.
>
> **Constraints.** Every new mode adds its rules to `Packages/com.tumbangpreso.core/`, never to
> Unity code. ⚠️ **A new mode is a new mode, never a change to Classic.** `docs/Design.md` governs
> Classic and `VISION.md` § 1 governs why. Write each mode's rules and win condition into
> `docs/Design.md` or a sibling document in the same commit as the code.
>
> **Done when** a private lobby can be created, configured, joined by code and played to
> completion, map voting works, and § 0.5 rule 9 is satisfied.

---

### 19.13 Prompt for Phase 13

**Seasons, dailies and the reason to open it on a Tuesday.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 13. Do not skip them because this prompt summarises the task;
> the summary is not the rules.
>
> **VERIFY FIRST.** Phases 2 and 4 shipped. If `INSPIRATION.md` prompt I3 has already built the
> challenge engine, **use it and do not build a second one.**
>
> **Build.** Three daily challenges with one reroll, weekly challenges worth a season tier, a
> ten-week season with a theme and a track, an end-of-season summary card designed to be
> screenshotted, login streaks, and a rotating featured mode. Keep a live-ops calendar in the repo.
>
> **Constraints.**
> - Every challenge is evaluated server-side from the match record. Never claimed by a client.
> - The challenge set is data in `Packages/com.tumbangpreso.core/` with a test that each one is
>   achievable in a single match or declares its own multi-match span.
> - ⚠️ **Challenges drive behaviour and bad ones drive bad behaviour.** "Get 10 knockdowns" teaches
>   a player to ignore the can, and the can is the whole game. Write them against outcomes the game
>   wants: retrievals under pressure, rounds survived as last attacker, tags as taya, matches
>   completed.
> - ⚠️ **A login streak pauses on a missed day. It never resets.** Punishing a break teaches people
>   that missing a day is expensive, which is how they decide to stop entirely.
>
> **Done when** dailies issue and reroll, a season boundary rolls correctly in a test, the summary
> card renders, and § 0.5 rule 9 is satisfied.

---

### 19.14 Prompt for Phase 14

**Controller support. Starting point is zero: no gamepad bindings, no control schemes.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 14. Do not skip them because this prompt summarises the task;
> the summary is not the rules.
>
> **VERIFY FIRST.** `grep -c Gamepad Assets/TumbangPreso/Resources/TumbangPreso.inputactions`. If
> it is no longer zero, someone has started this; read what exists before adding to it.
>
> **Build.**
> 1. Keyboard and Mouse and Gamepad control schemes in the input asset.
> 2. Every gameplay and spectator action bound, including the spectator set that
>    `Rebinding.SpectatorContext` already names.
> 3. ⚠️ **The contextual `E` hold tiers**: tap picks up, a short hold shoves, a long hold as taya
>    resets the lata. A hold on a face button is fine; **the on-screen prompt is the hard part**,
>    because it names a key today.
> 4. Every prompt resolves its glyph from the last device used, not from a setting.
> 5. Rumble on knockdown, tag and can reset.
> 6. **Full menu navigation on a stick with no mouse**: character select, settings, lobby,
>    everything. This is a bigger job than the gameplay bindings, it is the thing that always gets
>    skipped, and skipping it blocks Phase 15.
>
> **Constraints.** Extend `Rebinding`; do not replace it. Keep the one-control-one-action-per-
> context rule that the existing input tests assert. ⚠️ **Do not add aim assist.** Record in
> `docs/TODO.md` that input-based matchmaking pools are the chosen answer: it is free, exact, and
> it removes the argument entirely.
>
> **Done when** a whole match and every menu can be played from a controller with the mouse
> unplugged, and § 0.5 rule 9 is satisfied.

---

### 19.15 Prompt for Phase 15

**Mobile. ⚠️ Be honest about the size of this: it is a port, not a feature, and it is the largest
item in this plan.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/TESTING.md`, then
> `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 15. Do not skip them because this prompt
> summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Phase 14 shipped, because touch is built on the control-scheme work. Then list
> the installed build modules: if Android is not among them, installing it through Unity Hub is
> step zero and it is free.
>
> **Build, strictly in this order.**
> 1. **A build on a real device, however ugly.** Nothing else here means anything until that has
>    happened once. Do not polish before this.
> 2. Touch controls: left stick, look drag, buttons for throw, grab, jump, sprint, and a radial
>    long press with a fill for the contextual grab.
> 3. **A measured performance pass**, not a guessed one. The toon shader draws an inverted hull per
>    prop, which doubles draw calls on exactly the hardware least able to afford it. Measure it on
>    device, write the number into `docs/TODO.md`, then decide whether mobile drops the hull or gets
>    a cheaper one.
> 4. Phone aspect ratios added to the existing aspect-ratio probes rather than eyeballed.
> 5. Cross-play with separate pools, same reasoning as controller.
> 6. Battery, thermals, and a 30 FPS cap option.
>
> **Constraints.** ⚠️ **Keep the protocol version in lockstep with desktop.** Peers from different
> builds refuse each other by design, so shipping mobile and desktop at different versions will
> look like a bug and will not be one. **iOS is out of scope until there is a Mac to build on**:
> say that in the handoff rather than leaving it implied.
>
> **Done when** a full match can be played on an Android device against a desktop peer, the
> performance number is written down, and § 0.5 rule 9 is satisfied.

---

### 19.16 Prompt for Phase 16

**Accessibility and localisation. Both cheaper now than at any later point, and the game has
neither.**

> Read `CLAUDE.md` first, then `docs/VISION.md` § 2, then `docs/Art_Direction.md` § 1, then
> `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/FUTURE.md` § 16. Do not skip
> them because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Confirm there is still no language setting and no colourblind option, and read
> `Art_Direction.md` § 1 in full, because the fix below has to hold its colour law for everybody who
> is not using an accessibility palette.
>
> **Build accessibility first, localisation second.**
> 1. ⚠️⚠️ **A second, non-colour channel for the taya-versus-attacker role, everywhere it is
>    currently hue alone.** A shape on the nameplate, an icon on the marker, a different outline
>    weight. Orange versus blue survives the most common colourblindness well and the roles being
>    distinguished by hue ALONE is the actual problem: it fails for tritanopia, on a bad projector
>    at a tournament, and on a cheap phone screen.
> 2. Then palettes for deuteranopia, protanopia and tritanopia that keep the two roles maximally
>    separated in what the player actually sees.
> 3. UI scale and larger text. Hold versus toggle for sprint and the contextual grab. An FOV
>    slider, a camera shake slider, and a reduced-effects mode that doubles as the low-end
>    performance mode. Subtitles and captions for callouts and ability sounds. A high-contrast HUD.
> 4. Extend the existing ability-showcase probe to **assert the reduced mode is measurably calmer**
>    than the default rather than assuming it.
> 5. Then extract every user-facing string into a table and ship **English and Tagalog**. Tagalog is
>    not a nice-to-have: it is a Filipino street game by a Filipino team in a Filipino competition,
>    and it is marketing as much as access. Cebuano next, because the nationals are in General
>    Santos.
>
> **Constraints.** ⚠️ **Check every added glyph against the UI font's coverage before promising a
> language.** The font already has a known missing glyph and the UI works around it. Keep character
> names, mode names and TSINELAS untranslated: they are the identity.
>
> **Done when** the role is readable with hue removed entirely, a language can be switched at
> runtime, and § 0.5 rule 9 is satisfied.

---

### 19.17 Prompt for Phase 17

**Tournaments, LAN, spectating and replays. ⚠️⚠️ THE FIRST STEP OF THIS IS NOT FUTURE WORK.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 17. Do not skip them because this prompt summarises the task;
> the summary is not the rules.
>
> **VERIFY FIRST.** Read `LobbySession` and `LanBeacon` before writing anything: reconnect, seat
> reclamation and LAN discovery already exist and must not be rebuilt.
>
> **Step one, and do it before anything else in this prompt.** ⚠️⚠️ **Verify that a full
> four-player match can be started and completed with the internet physically disconnected**, and
> fix whatever screen fails. The nationals are in General Santos City and venue internet cannot be
> assumed. A tournament build that needs an online service to start a match is a build that can
> fail in the room. Add a test or a probe that keeps this true, because every later phase is a
> chance to put a login wall in front of it.
>
> **Then build.**
> 1. Tournament mode: lobby password, fixed roster, fixed map, no matchmaking, spectator slots that
>    do not consume a seat, organiser restart.
> 2. **Replays as the input stream plus the seed, not video.** The match is deterministic from the
>    input intent because a bot presses the same buttons a human does and one physics step serves
>    both, which makes a small replay possible instead of a huge one. ⚠️ **Prove determinism first**
>    with a test that replays a recorded match and asserts an identical final scoreboard. If it does
>    not reproduce, find the non-determinism and write it into `docs/TODO.md` before building
>    anything on top of it.
> 3. Clip export: save the last 30 seconds.
> 4. A caster overlay laid out for a stream rather than for a player.
> 5. A configurable spectator delay, so a stream cannot be sniped.
> 6. Highlight detection off events already raised.
> 7. An organiser's checklist document. ⚠️ **The thing that loses a tournament is a build
>    mismatch**: peers from different builds refuse each other by design, so every machine in the
>    room must be on the same executable and somebody must own that.
>
> **Constraints.** Extend `SpectatorCamera`; never write a second spectator path.
>
> **Done when** the offline check passes and is protected by a test, a tournament can be run
> end to end, a replay reproduces a scoreboard exactly, and § 0.5 rule 9 is satisfied.

---

### 19.18 Prompt for Phase 18

**Distribution. Last, and smaller than it looks.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/FUTURE.md` § 18. Do not skip them because this prompt summarises the task;
> the summary is not the rules.
>
> **VERIFY FIRST.** Confirm the WebGL module is still installed and that the game still builds for
> it, before promising a browser build to anybody.
>
> **Build.** Automate an itch.io release from a tagged commit through a GitHub Action. Get the
> WebGL target building and playable in a browser with the online path working. Write the store
> page from `docs/VISION.md`'s own words rather than inventing new ones.
>
> **Constraints.** ⚠️ **Name no vendor, engine, middleware or tooling in any public-facing
> material.** Write the capability, not who supplied it. This is a standing rule and it applies to
> the store page, the trailer description and the press kit.
>
> **Done when** a tag publishes a build without anybody touching a dashboard, the browser build
> plays a real match, and § 0.5 rule 9 is satisfied.
