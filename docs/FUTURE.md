# FUTURE.md: turning Tumbang Preso into a game people come back to

**What this file is.** An ordered build plan for the live-service and competitive half of the
game: accounts, a database, profiles, stats, matchmaking, ranked, progression, unlockables,
customisation, controller, mobile, tournaments. Every phase carries a **PROMPT** block written to
be pasted straight into a new session as its whole brief.

**What this file is not.** It is not a decision that any of it ships, it is not balance, and it is
not `docs/Design.md`. Where this file and `docs/VISION.md` disagree about what the game IS,
`VISION.md` wins.

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

> ### PROMPT 1
>
> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` § 1.
> They carry the rules of the repo, what the game is for, what is open, and this phase's brief. Do
> not skip them because this prompt summarises the task; the summary is not the rules.
>
> Build the account layer on UGS Authentication, which is already in `Packages/manifest.json` at
> 3.7.4 and unused. Anonymous sign-in on first launch with no prompt and no blocking UI, an
> optional username and password upgrade that preserves the anonymous player's data, and sign-in
> on a second device. Add a `PlayerAccount` service beside `NetSession` that owns the `PlayerId`,
> the display name and discriminator, the bio, the privacy flags and the signed-in state, and that
> raises an event when any of them change. Route the lobby's player name through it instead of
> through whatever the peer sends. Build the avatar as an in-game portrait composed from ids
> rendered through `ModelPreview`, NOT as an image upload: `docs/FUTURE.md` § 1.4 has the reasoning
> and it is a decision, not a preference. Implement account deletion and data export in this phase
> rather than later. Everything degrades to a local-only profile when UGS is unreachable, including
> LAN, Practice and Training. Put name validation, the discriminator allocation and the local
> versus remote precedence in `Packages/com.tumbangpreso.core/` with tests.

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

> ### PROMPT 2
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 2. Phase 1 must be in.
>
> Build the player profile, the stat set and the match history on UGS Cloud Save, plus one Cloud
> Code endpoint that is the only writer of a match record. Define the record and the profile as
> engine-free types in `Packages/com.tumbangpreso.core/` with their own tests so the shape is
> asserted without an editor and both toolchains compile it. Collect the stats listed in
> `docs/FUTURE.md` § 2.2 host-side off the events `MatchDirector` already raises and submit ONE
> payload at match end. Build the profile screen to the layout in § 2.1 using the existing UI kit
> and nine-patch art, following `ConvertedScreen`'s conventions rather than inventing a screen
> type. Add the offline path: a match played with no connection queues its record locally and
> submits on the next sign-in. Keep 100 full records per player and roll older ones into totals.

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

> ### PROMPT 3
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 3. Do this alongside
> Phase 2.
>
> Add telemetry through UGS Analytics, batched and sent once per session rather than per event,
> with a visible opt-out in Settings that is honoured completely and a clear statement of what is
> collected. Instrument the first-launch funnel first, then the match-level events in
> `docs/FUTURE.md` § 3. Keep payloads small and event names stable, because a renamed event is a
> broken history. Add no personally identifying field to any event.

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

> ### PROMPT 4
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 4. Phases 1 and 2 must
> be in.
>
> Build account XP, account level, per-character mastery, a soft currency and a 50-tier free season
> track. Put every curve and reward table in `Packages/com.tumbangpreso.core/` as data with tests,
> including a test that asserts NO reward on any track changes a gameplay number. XP is awarded by
> the same Cloud Code endpoint that writes the match record, computed server-side from the record,
> never sent by a client. Implement the AFK check first and pay an inactive seat nothing. Use
> diminishing returns rather than a daily cap. Build the end-of-match XP bar and the season track
> screen with the existing UI kit.

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

> ### PROMPT 5
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md`, `docs/wearables_catalog.md` and
> `docs/FUTURE.md` § 5. Phases 1, 2 and 4 must be in.
>
> Build the inventory, the per-character loadout and the customisation screen. Use STRING ids for
> every cosmetic rather than wire indices, for the reason `Roster.Slippers` gives about append-only
> lists. Extend `RosterEntryAsset` and `RosterBook` rather than building a parallel content system,
> and drive character colour variants through `ToonSkin`'s existing 16-slot palette remap.
> Replicate the loadout through the seat info that already crosses at match start; do not add a
> protocol. Preview through `ModelPreview` with the real shader. Add a test asserting no cosmetic
> changes any value read by `Packages/com.tumbangpreso.core/`, and a bound on wearable volume so a
> cosmetic cannot change how a silhouette reads at range.

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

> ### PROMPT 6
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 6. Phase 1 must be in.
>
> Build friends, presence, parties, recent players and blocking on UGS. Extend the existing
> `LobbyChat` rather than adding a second chat system. Parties queue together through the Phase 7
> queue and a block is honoured by matchmaking. Add the end-of-match add-friend prompt. Respect the
> UGS Lobby rate limits: presence polling must not raise the query rate against the free tier, so
> piggyback on the interval `ServerQuery` already runs.

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

> ### PROMPT 7
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 7. Phases 1, 2 and 6
> must be in.
>
> Build QUICK MATCH as a rating-banded queue on top of the UGS Lobby integration `ServerQuery`
> already runs, without adding the Matchmaker service. Advertise a band in the lobby data, search
> for a joinable lobby whose band contains the local player, host one if there is none, and widen
> the band on the timer in `docs/FUTURE.md` § 7 with the widening visible in the UI. Respect
> `ServerQuery.QueryInterval`: this must not raise the query rate against the free tier. Support
> backfill and honour blocks. Put the band arithmetic and the match-quality metric in
> `Packages/com.tumbangpreso.core/` with tests, including the case that makes a four-player free
> for all different from a team game: quality is the SPREAD of four ratings.

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

> ### PROMPT 8
>
> Read `CLAUDE.md` including § 4 on architecture invariants, then `docs/VISION.md`, `docs/TODO.md`
> and `docs/FUTURE.md` § 8. Phase 2 must be in. **Phase 9 must NOT be started until this is done.**
>
> Make a match result trustworthy without dedicated servers. Have every peer independently derive
> the final scoreboard from the scoring events it already receives, submit it, and have one Cloud
> Code endpoint accept only on unanimous agreement and flag disagreement for review. Do not change
> how scoring works: `MatchDirector.AddScore` stays the single host-side writer during the match,
> per `CLAUDE.md` § 4. This is a second independent derivation for verification only. Add leaver
> penalties that distinguish a leave from a disconnect using the reconnect window `LobbySession`
> already implements, add reporting, and rate-limit every write. Write into `docs/TODO.md` exactly
> what this scheme does and does not stop, including that it does not stop four colluding players.

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

> ### PROMPT 9
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 9. Phases 1, 2, 6, 7
> and 8.1 must all be in. Do not start without 8.1.
>
> Implement ranked. Two independent ladders, Classic and Hero Strike, never one shared number.
> Implement Glicko-2 for a four-player free for all by expanding each result into six pairwise
> outcomes with a capped score-margin multiplier, entirely inside
> `Packages/com.tumbangpreso.core/`, with tests asserting convergence over a simulated season, that
> a placement player settles inside ten matches, and that a smurf climbs out of a low band quickly.
> The Unity side is only submission, display and the season boundary. Build tiers, placements, the
> soft reset, the demotion buffer and the permanent peak. No decay. Ratings are written ONLY by the
> Cloud Code endpoint from Phase 8.1, never by a client and never by the host directly. Implement
> the party rule chosen in Phase 6 and assert it.

---

## PHASE 10 · MASTERY PATHS AND ABILITY VARIANTS

🧑: *"you know how overwatch or drg has unlockable skill paths or some shit?"*

⚠️⚠️ **THIS IS THE PHASE THAT CAN BREAK THE GAME, AND HERE IS EXACTLY HOW.** Deep Rock's
overclocks are a co-op feature: everyone is on the same side, so a player with more unlocks makes
the team stronger and nobody is on the receiving end. **This game is four players against each
other.** A skill tree unlocked by grinding is, in a competitive match, an account that beats a
player. `VISION.md` § 1's Classic rule is one half of the danger. This is the other half, and it
applies to Hero Strike too.

**The design that gets the feeling without the damage: HORIZONTAL, NOT VERTICAL.**

- Every hero's kit stays exactly as balanced today. Nothing unlocks more damage, range, duration or
  a shorter cooldown.
- What unlocks are **variants**: a second way to spend the same ability budget. Cheska's barricade
  gets a version that is shorter-lived but covers a wider arc. Same budget, different shape,
  different matchup answer.
- **Every variant is open to every player in ranked from level zero.** The grind unlocks the
  variant for casual play and unlocks its **look, its name and its place on the profile**
  permanently. In ranked the full set is always available to everybody.
- That keeps every word he asked for: a path to grind, a tree to fill in, a build to talk about, a
  profile that shows what you have mastered. It removes the one thing that would kill the esport
  ambition in the same breath as creating it.

**Then the part that is pure upside: mastery paths that are not power at all.** Per character, a
long path of things worth chasing that change nothing: a signature victory pose, a
character-specific emote, a voice line set, a nameplate, that character's own tsinelas, a colour
variant, a title, and a visible mastery number. **Most of the grind should live here**, and it is
Phase 5 content wearing a Phase 10 structure.

**Classic gets its own path and never gets powers.** Street Hype is already Classic's identity
layer. Extend it: Street Hype titles, curve and bank recognitions, streak records. Depth without
abilities, which is the rule.

> ### PROMPT 10
>
> Read `CLAUDE.md`, then `docs/VISION.md` § 1 twice, then `docs/TODO.md`,
> `docs/Hero_Strike_Balance.md` and `docs/FUTURE.md` § 10. Phases 4 and 5 must be in.
>
> Build the mastery paths. Start with the part that has no balance risk: per-character cosmetic and
> expressive mastery tracks for all eighteen characters, plus a Classic-only Street Hype track
> containing no abilities of any kind. Then build ability VARIANTS for Hero Strike as sidegrades
> that trade one property for another at an unchanged budget, never as upgrades, with every variant
> unconditionally available to every player in ranked regardless of unlock state. Put the variant
> definitions and the budget arithmetic in `Packages/com.tumbangpreso.core/` and write a test that
> fails if any variant is a strict improvement on its base along every axis. Read
> `docs/FUTURE.md` § 10's opening warning before writing a line.

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

> ### PROMPT 11
>
> Read `CLAUDE.md` including § 7.1, `docs/VISION.md`, `docs/TODO.md` §§ 10 and 16, and
> `docs/FUTURE.md` § 11.
>
> Build bot difficulty tiers, bot backfill of an abandoned seat, and disclosed bot fill in the
> casual queue after a wait threshold. Bots are never permitted in ranked and a test must assert
> it. A match a bot joins becomes unranked immediately. Label every bot visibly in the scoreboard
> and the nameplate. Tune the tiers against multiple `BotBehaviourProbe` runs, not one, because
> § 16 records that a single run spreads about 20 per cent.

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

> ### PROMPT 12
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/Design.md`, `docs/TODO.md` and `docs/FUTURE.md` § 12.
>
> Build custom games first, with a private lobby, a password and rule toggles, because every other
> mode here is cheaper once it exists. Then the daily seed mode, then map rotation and map voting.
> Every new mode reuses the existing rules core and adds its rules there rather than in Unity code.
> Do not touch Classic's rules: a new mode is a new mode, never a change to the one in
> `docs/Design.md`. Write each mode's rules and win condition into `docs/Design.md` or a sibling
> document in the same commit.

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

> ### PROMPT 13
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 13. Phases 2 and 4 must
> be in.
>
> Build daily and weekly challenges, season boundaries, login streaks and the end-of-season summary
> card. Every challenge is evaluated server-side from the match record written in Phase 2, never
> claimed by a client. Define the challenge set as data in `Packages/com.tumbangpreso.core/` with a
> test that every challenge is achievable in a single match or declares its own multi-match span.
> Follow `docs/FUTURE.md` § 13 on challenge design: nothing that rewards ignoring the can, and a
> streak that pauses rather than resets.

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

> ### PROMPT 14
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 14. Independent of
> phases 1 through 13.
>
> Add full controller support. Create Keyboard and Mouse and Gamepad control schemes in
> `TumbangPreso.inputactions`, which today has zero gamepad bindings, and bind every gameplay and
> spectator action including the contextual `E` hold tiers. Make every on-screen prompt resolve its
> glyph from the last-used device rather than from a setting. Make every menu fully navigable on a
> stick with no mouse, including character select, settings and the lobby. Extend `Rebinding`
> rather than replacing it and keep the one-control-one-action-per-context rule that
> `InputMapAndAbilityTests` asserts. Add rumble. Do not add aim assist: record in `docs/TODO.md`
> that input-based matchmaking pools are the chosen answer.

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

> ### PROMPT 15
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md`, `docs/TESTING.md` and `docs/FUTURE.md` § 15.
> Phase 14 must be in: touch is built on the control-scheme work.
>
> Port the game to Android. Step one is a build on a device with the Android module installed
> through Unity Hub, before any polish. Then touch controls including a radial long press for the
> contextual grab, then a measured performance pass on the toon outline hull, then phone aspect
> ratios added to `AspectRatioProbes`. Do not guess at the outline cost: measure it on device and
> write the number into `docs/TODO.md`. Keep `NetSession.ProtocolVersion` in lockstep with desktop.
> iOS is out of scope until there is a Mac, and say so in the handoff rather than leaving it
> implied.

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

> ### PROMPT 16
>
> Read `CLAUDE.md`, `docs/VISION.md` § 2, `docs/Art_Direction.md` § 1, `docs/TODO.md` and
> `docs/FUTURE.md` § 16.
>
> Do accessibility first and localisation second. Add a second, non-colour channel for the
> taya-versus-attacker role everywhere it is currently hue alone, then colourblind palettes for
> deuteranopia, protanopia and tritanopia that keep the two roles maximally separated, without
> breaking `Art_Direction.md` § 1 for players not using them. Then UI scale, hold-versus-toggle,
> an FOV slider, a reduced-effects mode, subtitles for callouts, and a high-contrast HUD. Extend
> `AbilityShowcaseProbe` to assert the reduced mode is measurably calmer than the default rather
> than assuming it. Then extract every user-facing string into a table and ship English and Tagalog,
> checking every added glyph against Darumadrop One's coverage first.

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

> ### PROMPT 17
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 17.
>
> Start with the urgent half: verify that a full four-player match can be started and completed
> entirely on LAN with the internet physically disconnected, fix whatever screen fails, and add a
> test or a probe that keeps it true. Then build tournament mode with a password, fixed rosters,
> spectator slots that do not consume a seat and an organiser restart. Then replays, recorded as
> the `InputIntent` stream plus the seed and replayed through the same fixed physics step the bots
> use; prove determinism first with a test that replays a recorded match and asserts an identical
> final scoreboard, and if it does not reproduce, find the non-determinism and write it into
> `docs/TODO.md` before building anything on it. Add clip export and a spectator delay. Extend
> `SpectatorCamera` rather than writing a second spectator path.

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

> ### PROMPT 18
>
> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/FUTURE.md` § 18.
>
> Ship a public build. Automate the itch.io release with butler from a GitHub Action so a tagged
> commit publishes, and get the existing WebGL target building and playable in a browser with the
> online path working through Relay. Write the store page copy from `docs/VISION.md`'s own words
> rather than inventing new ones, and name no vendor or middleware in anything public.

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
