# INSPIRATION.md: what to steal from games that work, and how it lands here

**What this file is.** A study of the systems that keep people playing other games, each one
turned into something specific for a four-player Filipino street game with a rotating taya.
It is the WHY behind [`FUTURE.md`](FUTURE.md), which is the WHAT and the WHEN.

**What this file is not.** It is not a decision that any of it ships. It is not balance. Where it
disagrees with [`VISION.md`](VISION.md) about what the game IS, `VISION.md` wins.

Written 2026-08-31, expanding 🧑's brief: *"thoroughly plan what else we could get or use as
inspiration from games that work and how we could implement in ours"*, *"maybe a separate ranked /
classic mode"*, *"maybe add achievements too and shit"*, and the clarification that reshaped § 5:
*"like skill / ability / passive variations, for example in drg theres diff guns"*, *"u get them
like in cod u finish quests or like ror2"*.

---

## 0 · The one rule for reading this

⚠️⚠️ **STEAL THE MECHANISM, NEVER THE SURFACE.** Every entry below says what a game does, why it
actually works, and what it becomes here. The last column is the only one that matters. A feature
copied because a big game has it, without the reason, is how a small game ends up with six systems
that each cost a month and none of which fit.

⚠️ **AND THE FOUR-PLAYER FREE FOR ALL IS THE FILTER EVERYTHING PASSES THROUGH.** Almost every
system worth copying was designed for a team game, and a team game hides something this game
cannot: in a 5v5, half the lobby wins. **Here three of four players lose every single match.**
That one fact is why § 4 exists and why it is the most important section in this document.

---

## 1 · The ten ideas worth doing, ranked

If nothing else on this page gets built, build these.

| # | Idea | Comes from | Why it is first |
|---|---|---|---|
| 1 | **A per-match performance grade, S to D, independent of placement, graded separately as attacker and as taya** | League mastery grades, Dead by Daylight emblems | Three of four players lose every match. A grade is how the other three still had a good night. **This is the single highest-value system in this document.** § 4.1 |
| 2 | **A banner with three stat trackers you choose** | Apex legends banners | The cheapest status object in games. It makes a stat page a thing people build rather than read. § 2.5 |
| 3 | **Loadout variants unlocked by character challenges** | Risk of Rain 2, Deep Rock, CoD | The thing he actually asked for, and it doubles as the best character tutorial you can write. § 5 |
| 4 | **Trophy Road: a reward track tied to rank that never takes anything back** | Brawl Stars | Solves "rank goes down and I feel worse" for a game whose population cannot afford churn. § 2.1 |
| 5 | **Endorsements** | Overwatch | One button. The only anti-toxicity system with evidence behind it. § 2.4 |
| 6 | **A daily seed with one attempt and a leaderboard** | Tetris 99 dailies, Wordle | Retention with no matchmaking, no population and almost no code. § 2.9 |
| 7 | **In-client tournaments** | Rocket League | This project is going to nationals. The tournament tooling is 70 per cent built already. § 2.6 |
| 8 | **Rank floors and a ratcheted reward road** | Marvel Snap, Brawl Stars | A bad night can never undo a good week, which is the most common reason people stop queueing ranked. § 2.19 |
| 9 | **A scheduled weekly hour** | Pokemon GO community day | Not content, a schedule. It concentrates thirty players into full lobbies instead of spreading them across empty ones, and it is nearly free. § 2.24 |
| 10 | **A training room that draws the invisible ranges** | Fighting game training modes | Contact resolves by distance, never by a volume, so every range in this game is a number that can be drawn exactly. § 2.21 |

---

## 2 · Game by game

Each entry: what they do, why it works, **what it becomes here**, and what not to copy.

### 2.1 Brawl Stars: the closest structural cousin

Short matches, small teams, a huge roster, mobile-first, and a progression built for a player with
five minutes.

- **Trophy Road.** Trophies go up and down per brawler, but the ROAD gives a reward the first time
  you pass each threshold and never takes it back. You can drop 200 trophies and keep everything.
- **Star Powers and Gadgets.** Each brawler has a couple of build options, not upgrades.
- **Club system.** A lightweight guild that mostly exists so you have people to queue with.
- **Mastery per brawler**, separate from account level.

**What it becomes here:** the reward track hangs off **rank**, not just XP, and it is
**ratcheted**. Reaching KANTO III for the first time pays out permanently. A player who falls back
to BATA keeps the reward and re-earns nothing. That is how you get a competitive ladder that a
losing player still wants to queue into, which matters enormously at low population where a
frustrated player leaving is a queue that stops filling.

⚠️ **Do not copy the brawler unlock model.** In Brawl Stars characters are the monetisation. Here
every character is free and always has been, and turning the roster into a grind would be a
downgrade dressed as progression.

### 2.2 League of Legends: grades, mastery and the ladder

- **Champion mastery grades.** Every match gives a grade, S to D, computed against other players'
  performance on that champion, independent of whether you won. Mastery levels come from grades.
- **Honor**, a peer-voted level with cosmetic rewards.
- **ARAM**, a second, lower-stakes queue that is enormously popular precisely because it is not the
  serious one.
- **Ranked with tiers, divisions and an apex leaderboard.**

**What it becomes here: § 4.1, the performance grade, and it is the headline recommendation of
this whole file.** Grade the player against the distribution of that character's performances at
that rating band, not against the other three seats. Then a player who came third with a career
knockdown rate and two clutch retrievals gets an **A** and a mastery point, and the night was
still good.

⚠️ **Do not copy promotion series.** League removed them because a best-of-three gate at every
tier turns a ladder into an anxiety machine. Use a demotion buffer instead: `FUTURE.md` § 9.

⚠️ **Do not copy the ranked reset that erases the badge.** Keep the peak on the profile forever.

### 2.3 Valorant: contracts and act badges

- **Agent contracts.** A free, linear, per-agent track. Play the agent, fill the track, unlock that
  agent's cosmetics. Simple, legible, and it teaches you the roster by making you try people.
- **Act rank badge.** A little triangle built from your **nine best wins of the act**, each tile
  coloured by the rank you beat. It is one image that is your whole season.
- **Performance bonus at low ranks**, so a clearly-better player climbs out fast.

**What it becomes here:** per-character contracts are exactly the mastery track in `FUTURE.md`
§ 10, and the **act badge is the best single-image flex in any competitive game.** A Tumbang Preso
version: nine tiles from your nine best matches of the season, each tile coloured by the grade
(§ 4.1) rather than by rank, so it works for a game where you place rather than win. That badge
belongs on the profile header, the nameplate and the end-of-season card.

⚠️ **Do not copy the paid battle pass.** There is no money in this game.

### 2.4 Overwatch: endorsements, on-fire, and the moment

- **Endorsements.** After a match you endorse one player. Your level shows on your card.
- **Play of the Game.** A ten-second replay of the best thing that happened.
- **On fire.** A meter that fills as you do well, visible only to you.
- **Arcade**, weekly rotating silly modes with a small reward for three wins.

**What it becomes here:** endorsements go in as-is, one button on the end-of-match screen, level on
the nameplate. **Street Hype already IS the on-fire meter** and `VISION.md` says it is Classic's
identity, so extend it rather than adding a second one. Play of the Game becomes the **best moment
card** in § 4.3: a still image rather than a replay is far cheaper and gets posted more.

⚠️ **Do not copy role queue.** There are no roles here, there is a rotating taya, and it is better.

### 2.5 Apex Legends: the banner

- **Your banner** carries a frame, a pose, a badge and **three stat trackers you choose from a
  long list**. It is shown at the start of every match to everybody.
- Badges are earned by specific feats, most of which are hard.
- Ranked has an **entry cost** in points, so playing a lot without doing well does not climb.

**What it becomes here:** the banner is **the second-highest-value idea in this document** and it
is almost free once `FUTURE.md` § 2 has the stats. Three chosen trackers turn a stats page from
something you read once into something you build. "Most knockdowns in a match: 7" next to your name
in the lobby is worth more retention per line of code than any other feature here.

⚠️ **Do not copy ranked entry cost yet.** It is the right tool at high population and at low
population it just makes a thin queue feel punishing.

### 2.6 Rocket League: playlists, training and tournaments

- **A separate rank per playlist.** 1v1, 2v2 and 3v3 are separate ladders. Nobody is confused by
  this and it is the direct answer to § 3.
- **Custom training packs** made by players, plus freeplay. A huge amount of the skill ceiling is
  practised outside matches.
- **In-client tournaments** on a schedule, with brackets, that anybody can enter.
- **Full replay control**: any camera, any player, scrub anywhere.

**What it becomes here:** § 3 takes the playlist model directly. The **in-client tournament** is
the one to notice: this project is going to nationals, `SpectatorCamera` and the broadcast clock
already exist, and `FUTURE.md` § 17 is mostly wiring rather than invention. A scheduled weekly
in-client tournament is also the single best thing a small competitive game can do for its
population, because it gives a fixed time when everybody is online at once.

### 2.7 Deep Rock Galactic and Risk of Rain 2: unlock by doing

- **DRG**: several primaries and secondaries per class, all viable, plus overclocks that are real
  sidegrades with drawbacks.
- **RoR2**: every survivor has alternate skills, each unlocked by a **specific, characterful
  challenge**. "Land five direct hits with one grenade." Doing the challenge teaches the skill.
- Both are co-op, which is why their unlock gating is safe there and needs care here.

**What it becomes here: § 5, and it is what he actually asked for.** The RoR2 model is the good
one: challenge-gated, not level-gated, because a challenge teaches the character while it unlocks.

### 2.8 Counter-Strike and the trust problem

- **Prime and Trust Factor.** A hidden score from account age, matches finished, reports and
  behaviour, used to sort players into cleaner or dirtier pools.

**What it becomes here:** a cheap version costs nothing and is worth having before ranked ships:
account age, matches finished, report count, leave rate. Use it to keep brand-new accounts and
heavily-reported accounts in their own pool. It is also the smurf answer that does not need
anti-cheat. `FUTURE.md` § 8.3.

### 2.9 Wordle, Tetris 99 dailies, and the one-attempt hook

One puzzle a day, the same for everybody, one attempt, a shareable result.

**What it becomes here: the DAILY SEED, and it is absurdly cheap.** Same map, same taya order, same
bot opponents, same starting state for everybody that day. One attempt. A leaderboard. A short
shareable result string. **It needs no matchmaking, no population and no other player**, which
makes it the only retention feature on this page that works on day one with thirty players.

### 2.10 Splatoon and the event that picks a side

Splatfests: pick a team, play the weekend, one side wins, everybody gets a memento.

**What it becomes here: LIGA NG BARANGAY.** Pick a barangay or a region, play for the weekend, the
side with the most points wins, everybody gets a badge and the winners get a colour variant. For a
Filipino street game this is not a copied mechanic, it is the actual social structure the game is
about. It costs a counter, a leaderboard and a badge, and it is the most on-theme event this game
could run.

### 2.11 Fall Guys and Stumble Guys: the party-game cousins

Short rounds, chaos, and progression that is entirely costumes.

**What it becomes here:** confirmation, from the closest genre neighbours, that **cosmetics alone
are enough progression for a game of this shape**. Neither of them needed to sell power and neither
needed a skill tree. It is also a warning: both bleed players fast when content stops, which is why
`FUTURE.md` § 12 insists on a cadence the team can actually keep.

### 2.12 Call of Duty: challenges as the whole meta

Weapon levels, attachment unlocks, camo challenges, calling cards. Almost all of it is "do this
specific thing N times", and it works because the challenges are visible, trackable and stacked.

**What it becomes here:** the **challenge system is one piece of machinery used four ways**:
dailies, weeklies, loadout unlocks (§ 5) and achievements (§ 5.6). Build it once, in the core, with
a clean condition type, and all four are content rather than code.

⚠️ **Do not copy the challenge that fights the game.** "Get 10 knockdowns" teaches a player to
ignore the can, and the can is the whole game.

### 2.13 Mario Kart: the comeback, and when to switch it off

Items scale to position: last place gets the bullet, first place gets a banana. It keeps eight
players in a race that only one can win. **And competitive Mario Kart turns items off**, which is
the more useful half of the lesson.

**What it becomes here:** the answer to § 4.2's dead round, **in casual only**. A player far behind
at the final round gets something diegetic rather than a stat buff: first pick of spawn, or a
slightly shorter stamina recovery, or their tsinelas returned to hand once. **Ranked never gets
it.** The split is the point: casual keeps four people playing, ranked keeps the result honest.

### 2.14 Trackmania: medals, and a solo ladder that needs nobody

Every track carries author, gold, silver and bronze times. You are not racing people, you are
racing a bar, and the bar does not care how many players are online.

**What it becomes here, and it is bigger than it sounds:** put **medals on the daily seed and on
time attack**. A solo skill target is the only kind of progression that works at thirty concurrent
players, it gives a returning player something to do at 3am, and it is a leaderboard that cannot
feel empty. Also steal the **ghost**: race the replay of your own best run, which this codebase can
actually do because the match is deterministic from `InputIntent`.

### 2.15 PUBG: the circle, which this game already half has

The shrinking play area is the most-copied mechanic in modern multiplayer because it solves one
problem completely. It converts a slow endgame into a forced fight, on a timer, without asking
anybody to be brave.

**What it becomes here:** `CONFINEMENT_RADIUS` already exists and spawns are already computed from
it. **Shrinking the box in the last 20 seconds of a round** is close to free, it is the most
thematically honest answer to § 4.2, and it makes the end of every round a fight instead of a
stalemate where the taya circles a can nobody can approach.

⚠️ **Measure it before believing it.** A smaller box favours the taya, and passive defence already
pays 900 a round uncontested against 100 for a knockdown, which `Design.md` carries as a known
risk. This could make that worse rather than better, which is exactly why it goes through
`BotBehaviourProbe` over several runs and not through taste.

### 2.16 Rocket League overtime, and Smash's stocks

- **Overtime:** the game does not end tied, it ends when somebody scores. Next goal wins, forever.
- **Stocks:** Smash's competitive format is lives, not a timer, because a timer rewards running
  away.

**What it becomes here:** two arcade variants that cost almost nothing, because the rules core
already owns the round.

- **SUDDEN DEATH.** The round timer expires with the can standing and the score tied: the box
  shrinks every 10 seconds until somebody knocks it down. No draws, ever.
- **STOCK, or LAST TSINELAS STANDING.** Each attacker gets three tsinelas. Lose them all and you
  are out. The last attacker standing wins the round. A completely different game from the same
  parts, and the best candidate on this page for a rotating featured mode.

### 2.17 Dead by Daylight: grading two roles separately

Asymmetric 1v4, and the thing it gets right that almost nobody copies: **it grades the killer and
the survivors on different criteria**, because they are playing different games.

**What it becomes here, and it directly extends § 4.1:** every player is taya for one round and an
attacker for the others, so **the performance grade should be two grades**, weighted by rounds
played in each role. An ATTACKER grade from knockdown rate, retrievals under pressure and time
alive. A TAYA grade from tags per round, passive defence held and time to reset. The profile can
then say "A attacker, C taya", which is a genuinely interesting thing to know about yourself and
points straight at what to practise.

⚠️ **And Evolve is the cautionary half.** Asymmetric balance is brutally hard and it died of it.
Here the rotation saves us: everybody plays both sides every match, so an imbalance is felt by all
four players equally rather than by one unlucky one. **Say that in the ranked explainer.**

### 2.18 Hades: losing has to advance something

You die, and the game gives you a new conversation. The run failed and the story moved. It is the
most effective anti-frustration design in modern games and it costs writing rather than systems.

**What it becomes here, and it is the other half of § 4.1's problem:** three of four players lose
every match, so **losing must advance something narrative**, not only a number. Unlock character
dialogue, barangay stories, a line of announcer flavour, a piece of a map's history. Cheap in code,
entirely writing, and it makes a losing streak feel like progress through something instead of
failure at something.

### 2.19 Marvel Snap: rank floors

The ladder has milestone floors. Once you pass one you cannot fall below it for the season. Losing
still costs, but it cannot erase the week.

**What it becomes here:** combine it with Brawl Stars' ratcheted rewards from § 2.1. **Each tier
boundary is a floor.** Reaching KANTO I means finishing the season no lower than KANTO. It costs
one comparison in the rating update and it removes the single most common reason people stop
queueing ranked, which is watching a bad night undo a good week.

### 2.20 Guilty Gear Strive: towers instead of a ladder, and replay takeover

- **The tower.** Instead of a number you sit on a floor with other players and move up or down. It
  is matchmaking dressed as a place, and it feels far less like a judgement.
- **Replay takeover.** Pause any replay, take control from that moment, and play it out. The best
  training feature in any fighting game.

**What it becomes here:** the tower is worth considering for the **casual** queue at low population
(§ 3.4), because a room-based structure fills faster than a rating band and reads as a hangout
rather than a ladder. Call the floors streets. **And replay takeover is genuinely available to this
project**, because a replay is the `InputIntent` stream and the simulation is deterministic: pause,
take the seat, play the retrieval again. No other feature on this page teaches the game as fast.

### 2.21 Fighting games in general: the training room

Street Fighter, Tekken and Skullgirls all ship a training mode that shows the invisible: hitboxes,
frame data, ranges, recovery.

**What it becomes here, and this game is unusually suited to it:** contact resolves **by distance
on the host**, never by a trigger volume, which means every combat range in this game is a number
rather than a mesh. **A training room can draw them exactly.** Show the tag window as a ring on the
taya, the throw arc as a live trajectory, the shove cone, the confinement box, and the exact moment
the lata's fall registers. That turns the game's most opaque interactions into something a player
can see and practise, and the data is already there.

### 2.22 Halo and Fortnite: the map editor is how a game outlives its team

Forge and Creative both did more for their games' lifespans than any content patch.

**What it becomes here:** the largest project on this page, and the only one that makes the game
survive the team graduating. Not now. But **build the map format so it stays possible**:
data-driven, serialisable, loadable at runtime. `IlalimNgTulayBuilder` and `SceneBuilder` already
build maps from code, which is most of the way to building them from data.

### 2.23 Dota's behaviour score and Overwatch's avoid list

Dota sorts players by a hidden behaviour number. Overwatch lets you avoid a small number of
players.

**What it becomes here:** the trust score in § 2.8, plus **an avoid list of three**, which is
cheaper than a report system and does more for how the game feels. At four players a single
unpleasant person is a quarter of the lobby, so this matters more here than in a ten-player game.

### 2.24 Pokemon GO and Animal Crossing: the ritual and the fixed hour

- **Community Day**: a fixed three-hour window when everybody is online. It is not content, it is a
  schedule, and it is why the game still fills.
- **Animal Crossing's daily**: a small, warm, low-effort reason to open the game that never
  punishes a miss.

**What it becomes here, and it is the cheapest population fix in this entire document:** a
**scheduled weekly hour**. Friday 8pm, everybody plays, the queue fills, an in-client tournament
runs (§ 2.6), and the Discord announces it. **A small game does not have a population problem at
every hour, it has one at most hours**, and a schedule concentrates thirty players into a full
lobby instead of spreading them across an empty one.

### 2.25 Clash Royale and the chest: pacing without paying

Rewards arrive on a timer as well as on performance, so a session has a shape.

**What it becomes here, carefully:** a small daily and weekly rhythm is fine, and `FUTURE.md` § 13
has it. ⚠️ **But never a timer that makes waiting the optimal play.** Chest slots exist to sell you
time. With no monetisation there is no reason to build the frustration that funds it.

### 2.26 Arcade high scores: three letters and a machine in a room

The original retention system. Your initials, on the cabinet, in the shop, where the next person
sees them.

**What it becomes here, and it fits this game's soul better than any online leaderboard:** a
**local machine leaderboard**. Best daily seed on this PC, best knockdown streak on this PC,
initials and all. This game is played by five friends in one room and it will be played in school
computer labs. **A leaderboard that needs no internet, no account and no population is the one that
will actually get looked at**, and it is a text file.

### 2.27 Titanfall and the movement ceiling

The reason people stayed was not the guns. It was that moving well was its own deep skill that
anybody could see you doing.

**What it becomes here:** there is already a sprint, a jump, a lunge, a shove and a hop. **Ask
whether the movement has a ceiling worth practising**, and if it does, teach it and put it on the
scoreboard. If a good player can bank a tsinelas off a wall or cut a corner in a way a new player
cannot, that is skill expression the game should name out loud rather than leave hidden. Street
Hype is already the mechanism for naming it.

### 2.28 The Finals: the objective that creates comebacks

A cashout takes time and can be stolen. The lead is never safe, so nobody stops playing.

**What it becomes here:** the third candidate answer in § 4.2, given a shape. **Passive defence
could BANK rather than score**, and a knockdown could steal the bank. The taya's 900 uncontested
points become 900 points at risk, which addresses `Design.md`'s known balance risk and the dead
round with one change.

⚠️ **That is a large rules change to Classic.** `VISION.md` § 1 does not forbid it and `Design.md`
governs it. Prototype it in a custom game and measure it across several `BotBehaviourProbe` runs.
Do not ship it into Classic on the strength of this paragraph.

### 2.29 Among Us: the frictionless invite

A four-character code, free on mobile, cross-play, and no account needed to play with your friend
tonight.

**What it becomes here:** already true, and **that is worth protecting rather than building**.
`ServerQuery` resolves a 4-character code LAN-first then online, and `FUTURE.md` § 1 makes accounts
optional and silent for exactly this reason. ⚠️ **Every phase in `FUTURE.md` is a chance to
accidentally put a login wall in front of "play with my friend right now". Do not.**

### 2.30 Slay the Spire's daily and Balatro's run history

A daily climb with modifiers, and a history of every run you have made with the numbers attached.

**What it becomes here:** modifiers on the daily seed from § 2.9, once it exists. One tsinelas only.
Half stamina. Double the taya's reach. It is a new mode a week, forever, out of a single field.

---

## 3 · The queue and mode structure, which is his ranked-versus-classic question

🧑: *"maybe a separate ranked / classic mode"*. There are two readings and the answer to both is
the same shape.

### 3.1 The rule: MODES are rulesets, QUEUES are stakes

⚠️⚠️ **DO NOT MAKE "RANKED" A THIRD MODE.** `VISION.md` § 1 says Classic and Hero Strike are both
first class and neither is a variant of the other. A ranked mode with its own rules would quietly
become a third game to balance, and the first thing that happens is that practice in casual stops
transferring to ranked, which is the fastest way to make a competitive game feel unfair.

**The mode is the ruleset. The queue is what is at stake.** That gives a grid:

| | **CLASSIC** (4 rounds, no powers) | **HERO STRIKE** (8 rounds, kits) |
|---|---|---|
| **CASUAL** | Quick match, bots allowed to backfill, no rating | Quick match, bots allowed to backfill, no rating |
| **RANKED** | Own ladder, no bots ever, stricter leaver rules | Own ladder, no bots ever, stricter leaver rules |
| **ARCADE** | Daily seed, rotating featured mode, events | Same |
| **CUSTOM** | Private code, full rule toggles, tournament mode | Same |
| **PRACTICE** | Bots, training, the range, challenge completion | Same |

### 3.2 So yes: RANKED is its own top-level menu entry

Which is what he is asking for, done in a way that does not fork the game.

```
PLAY
 ├─ QUICK MATCH      -> pick CLASSIC or HERO STRIKE
 ├─ RANKED           -> pick CLASSIC or HERO STRIKE, separate ladders, own badge
 ├─ ARCADE           -> daily seed, featured mode, live event
 ├─ CUSTOM           -> create or join by 4-character code
 └─ PRACTICE         -> bots, guided training, the range
```

RANKED gets its own screen, its own art, its own music sting, its own badge on the profile, and its
own place in the menu. **It is a different experience without being a different game.**

### 3.3 What ranked changes, and what it must not

**Changes:** no bots ever, no full four-stack party, stricter leaver penalty, a rank on the line, a
result that must be corroborated by all four peers (`FUTURE.md` § 8), and a public season
leaderboard.

⚠️ **Does NOT change: the rules.** Same round count, same scoring, same map pool, same everything
in `Design.md`. Every ruleset difference between casual and ranked is a place where practice stops
counting.

**One exception worth considering, and it is a real design question rather than a copy:**
**unique character selection in ranked.** Right now four players can all pick Cheska, and the
scoreboard from a real match shows exactly that. First-lock-wins uniqueness in ranked only would
add counterplay and roster variety at the cost of one rule. It is worth prototyping in custom games
before deciding, and it is the only ruleset difference this document is willing to argue for.

### 3.4 ⚠️⚠️ The population trap, and the rule that avoids it

**Four queues at thirty concurrent players is zero queues.** Splitting a small population is how a
game with a healthy Discord ends up with a five-minute wait and no players.

**So gate the queues on measurement, not on ambition:**

- **Open** a queue when its median wait sits under 60 seconds for a week.
- **Merge** it back when its 90th percentile wait exceeds 180 seconds for a week.
- Start with **one** casual queue that picks the mode by vote. Split casual by mode next. Add
  ranked only when casual alone fills reliably. Split ranked by mode last.
- Tell the player the truth in the UI. "RANKED HERO STRIKE opens at a bigger population" is
  respectable. A queue that never fills is not.

`FUTURE.md` § 3's telemetry is what makes this a measurement instead of an argument.

---

## 4 · The problems this game has that no other game's system solves for free

**This is the section that is not borrowed.** These are consequences of the four-player free for
all and the taya rotation, and they need answers designed here.

### 4.1 ⚠️⚠️ THREE OF FOUR PLAYERS LOSE EVERY MATCH

In a 5v5, half the lobby wins. Here **75 per cent of every session is a loss** by placement, which
is a brutal retention curve if placement is the only feedback the player gets.

**The answer is a per-match performance grade, S to D, computed independently of placement.**
Grade against the distribution of that character's performances in that rating band: knockdown
rate, retrievals under pressure, tags per round defended, sabotages, time alive as last attacker.
A player who came third with an A grade earned mastery, earned XP, and had a good match.

**Everything hangs off this:** character mastery, the act-style badge, the end-of-match screen, the
banner trackers, and the reason to queue again after four losses.

⚠️ **The grade must never touch rating.** Rating is placement, because placement is the game.
Grade is feedback. Mixing them produces a ladder people farm by playing selfishly, which is
precisely the failure mode of every "performance-based ranked" system that has ever been tried.

### 4.2 ⚠️ BEING LAST AT ROUND 3 MEANS HAVING NOTHING TO PLAY FOR

A cumulative-score format with four rounds has a dead zone: if you are 400 behind at the start of
the last round, the match is over for you and you have 90 seconds of nothing.

**Three candidate answers, all cheap, none built:**

1. **Pay all four placements**, not just first. 2nd and 3rd worth real rating and XP means the
   fight for 2nd is a live match inside the lost one.
2. **A final-round multiplier**, so the last round is worth more and a comeback is arithmetically
   possible. Say 1.5x. This is the standard answer and it is also the one most likely to feel
   cheap, so measure it rather than assuming.
3. **Per-round objectives** that pay regardless of score: survive the round without being tagged,
   land a knockdown from outside the box, retrieve while the taya is within 3 m. A losing player
   always has something to chase.

⚠️ **This is a real balance question and it belongs in `Design.md` once it is answered.** It is
listed here because no borrowed system solves it and because it is probably the biggest single
threat to how the game feels in a long session.

### 4.3 THE MOMENT NOBODY SEES

A great retrieval happens in half a second and then it is gone. In a team game a teammate saw it.
Here nobody did.

- **Best moment card**: a still, generated from events already raised, framed, with the stat line
  under it. Designed for a phone screenshot.
- **Clip export**, a "save the last 30 seconds" key. `FUTURE.md` § 17.
- **A three-second replay of the tag that killed you**, shown to the victim only. It is a killcam
  and it converts "that was bullshit" into "I saw what I did wrong", which is the single most
  valuable thing a competitive game can show a losing player.

### 4.4 FOUR PLAYERS MEANS ONE LEAVER RUINS IT

At 5v5 a leaver is an inconvenience. At four players it is 25 per cent of the match.

`FUTURE.md` § 11's bot backfill is the mitigation, and the rules around it matter: backfill
instantly, label the bot, mark the match unranked from that moment, and **do not punish the three
who stayed**. Reduced rating loss for a match that lost a player is standard and correct.

### 4.5 THE TAYA ROTATION IS A GIFT AND NOBODY KNOWS IT

Everyone defends once, so seat luck mostly cancels and a bad first round is not a lost match.
**That is a genuinely elegant fairness property and the game never says it out loud.** Put it in
the queue screen, the tutorial and the ranked explainer. Understanding it is the difference between
"this is rigged" and "I have three more rounds".

---

## 5 · Loadouts and skill variants, which is what he actually meant

🧑: *"like skill / ability / passive variations, for example in drg theres diff guns"*, *"u get
them like in cod u finish quests or like ror2"*.

**This is a loadout system, not a skill tree, and it is a much better feature.** A skill tree is
vertical: you get stronger. A loadout is horizontal: you get different. The first one breaks a
competitive game and the second one is most of its depth.

### 5.1 The shape

Each Hero Strike hero gets a small pool per slot:

| Slot | Options | Example, Cheska |
|---|---|---|
| Skill 1 | 3 | Ice Barricade: long and narrow, or short and wide, or two small ones |
| Skill 2 | 3 | A slow field, or a burst of speed for herself, or a slippery patch |
| Ultimate | 2 | Glacial Nova as an area denial, or as a single hard freeze |
| Passive | 3 | Faster on ice, quieter footsteps, or a shorter reset as taya |

Three by three by two by three is 54 builds per hero from twelve authored pieces. **That is where
the "shit u can grind for" and the thing people argue about both come from**, and it is far less
work than a skill tree because there are no numbers to escalate.

### 5.2 ⚠️⚠️ EVERY OPTION IS A SIDEGRADE AT THE SAME BUDGET

Nothing unlocks more damage, more range, more duration or a shorter cooldown. Every option trades:
wider but shorter, faster but louder, stronger but slower to arrive. **Write a test that fails if
any option is a strict improvement on its siblings along every axis.** That test is the thing that
keeps this system honest three seasons from now when somebody adds option four in a hurry.

### 5.3 Unlocked by challenges, Risk of Rain 2 style

Not "reach level 12". A specific, characterful thing:

- *Freeze two attackers with one barricade* unlocks Cheska's wide barricade.
- *Knock the can down from outside the box* unlocks a throw-flavoured variant.
- *Retrieve your tsinelas three times in one round without being tagged* unlocks a mobility passive.
- *As taya, reset the lata within 2 seconds of it falling, five times* unlocks the fast-reset
  passive.

**Doing the challenge teaches the thing it unlocks.** That is why RoR2's version is better than a
level gate, and it makes the unlock system double as the character tutorial the game does not have.

### 5.4 ⚠️⚠️ AND THE ONE RULE THAT MAKES ALL OF IT SAFE

**EVERY CHALLENGE MUST BE COMPLETABLE IN PRACTICE AGAINST BOTS.**

This single rule dissolves the competitive problem entirely. The gate then costs **time spent
learning a character**, never **matches won against people**. Nobody has to grind ranked to be
equipped for ranked, a returning player is never permanently behind, and a new player who wants a
specific build can go and get it in twenty minutes instead of twenty hours.

It also means the earlier recommendation in this project's notes, that ranked should force all
variants unlocked, is no longer needed. **With practice-completable challenges, ranked can use the
same unlock state as everything else**, which removes a whole class of "the game behaves
differently in two queues" bugs and confusion. That is the better design and it came out of his
clarification, not out of the original plan.

### 5.5 Two more rules worth writing down

⚠️ **THE BUILD IS PUBLIC.** Shown in the lobby before the match and on the scoreboard during it.
Hidden loadouts in a four-player fight are information asymmetry that feels like cheating, and
seeing an opponent's build is where counterplay comes from.

⚠️ **AND CONSIDER LETTING PLAYERS SWAP AT THE ROLE CHANGE.** Every player is taya for one round and
an attacker for the others, and those are different games. Allowing a build swap in the gap between
rounds, the way a buy phase works, turns the loadout from a menu choice into a live decision. **It
is the single most interesting idea in this document for the depth of Hero Strike**, and it is also
a real balance risk, so prototype it in custom games before it goes anywhere near ranked.

### 5.6 Achievements, which is the same machinery pointed somewhere else

🧑: *"maybe add achievements too and shit"*.

**Build the challenge system once, in the core, with one condition type, and get four features:**
daily challenges, weekly challenges, loadout unlocks, and achievements. The only difference between
them is the reward and whether they repeat.

Three tiers, because a flat list of 200 achievements is wallpaper:

- **Bronze, the teaching tier.** Land your first knockdown. Retrieve under pressure. Win as taya.
  These exist to get seen in the first hour and to point at parts of the game.
- **Silver, the grind tier.** 100 knockdowns. 50 matches with every character. All ten tsinelas
  used. Visible progress bars, which are most of the appeal.
- **Gold, the story tier.** The ones people screenshot: win a match from last place at the final
  round, knock the can down three times in one round, survive a full round as the last attacker
  without being tagged, win a match without throwing at all.

**Every achievement pays a title, a badge or a banner tracker**, so it feeds § 2.5 rather than
being a dead list. Achievements are also the cheapest possible content: they are data, they need no
art, and they make the existing game deeper without changing a rule.

⚠️ **Do not gate anything competitive behind an achievement.** They are bragging, and bragging is
enough.

---

## 6 · Feel, which is not a system but decides whether any of this matters

Cheap, high-value, and none of it needs a service.

- **Confirmation on every hit.** The knockdown, the tag and the sabotage each need a distinct
  sound, a distinct hitstop and a distinct piece of screen feedback. `Hitstop` already exists.
- **The near miss must be legible.** "You were one knockdown from second" at the end of a match is
  worth more than any reward on this page.
- **Menus that open in one press.** Brawl Stars and Valorant both put PLAY under the thumb with
  nothing in the way. Count the presses from launch to in a match and make that number small.
- **A home screen that shows your character.** The lobby already renders the cast through
  `ModelPreview`. Standing your own pick on the main menu is nearly free and it is why people care
  about cosmetics at all.
- **Music that knows the round state.** Last 15 seconds, a comeback, a can that has been standing
  for a minute.
- **A voice for the game that is Filipino and unapologetic.** The callouts, the announcer, the
  titles. This is the thing no other game on this list can copy back.

---

## 7 · What not to steal, consolidated

- ⚠️ **Loot boxes, gacha, battle passes, any purchase.** No budget means no payment processing, and
  an honest progression is worth more to this team than a model it cannot legally operate.
- ⚠️ **Power that unlocks.** § 5.2.
- ⚠️ **Promotion series.** § 2.2.
- ⚠️ **Role queue.** There are no roles; there is a rotating taya, and it is better.
- ⚠️ **Rank decay.** It punishes people with school and jobs, which is this entire audience.
- ⚠️ **Voice chat.** Cost, moderation liability, and a ping wheel does the job for a fraction.
- ⚠️ **Four queues before the population supports them.** § 3.4. This is the one on this list most
  likely to actually happen.
- ⚠️ **A fourth map before map voting exists.** Voting buys most of the same freshness for a tiny
  fraction of the work.
- ⚠️ **Performance-based rating.** § 4.1. Grade the player, rank the placement, never mix them.

---

## 8 · The prompts, and the order to run them in

Each block below is a complete brief. Copy one, paste it into a new session, do nothing else.

⚠️ **These interleave with `FUTURE.md`'s eighteen phases rather than replacing them.** § 8.6 is the
combined order.

### PROMPT I1 · The performance grade, attacker and taya

**Run after `FUTURE.md` Phase 2. This is the highest-value item in either document.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/INSPIRATION.md`
> §§ 4.1, 2.2 and 2.17. They carry the rules of the repo, what the game is for, what is open, and
> this task's brief. Do not skip them because this prompt summarises the task; the summary is not
> the rules.
>
> Build a per-match performance grade, S to D, computed independently of placement, because three
> of four players lose every match in this game and placement alone is a brutal retention curve.
> Grade against the distribution of that character's performances at that rating band using the
> stats `FUTURE.md` § 2.2 already collects.
>
> ⚠️ **It is TWO grades, not one**, per `docs/INSPIRATION.md` § 2.17: every player is taya for one
> round and an attacker for the others, and those are different games with different inputs. An
> ATTACKER grade from knockdown rate, retrievals under pressure and time alive as last attacker. A
> TAYA grade from tags per round defended, passive defence held and time to reset. Combine them by
> rounds played in each role for the headline, and show both, because "A attacker, C taya" tells a
> player what to practise and a single letter does not.
>
> Put the whole grading model in `Packages/com.tumbangpreso.core/` with tests, including one that
> asserts the grade is mathematically independent of placement and one that asserts a player who
> placed fourth can score an S. The grade must never feed rating: read `docs/INSPIRATION.md` § 4.1
> on why performance-based rating fails. Show both grades on the end-of-match screen with the two
> or three stats that drove each, and feed them into character mastery.

### PROMPT I2 · The banner and stat trackers

**Run after `FUTURE.md` Phase 2, alongside or after Phase 5.**

> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/INSPIRATION.md` § 2.5. `FUTURE.md`
> Phase 2 must be in.
>
> Build the player banner: a frame, a character pose, a badge and three stat trackers the player
> chooses from the full stat list. Show it in the lobby, on the scoreboard, on the profile and on
> the end-of-match screen. Trackers read from the profile written in `FUTURE.md` Phase 2 and are
> replicated through the seat info that already crosses at match start rather than through a new
> protocol. Frames, poses and badges are cosmetics with string ids, per `FUTURE.md` § 5.

### PROMPT I3 · The challenge engine, achievements and loadout unlocks

**Run after `FUTURE.md` Phase 4. One system, four features.**

> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md`, `docs/INSPIRATION.md` §§ 5 and 2.12, and
> `docs/FUTURE.md` § 10. `FUTURE.md` Phases 2 and 4 must be in.
>
> Build ONE challenge engine in `Packages/com.tumbangpreso.core/` with a single condition type,
> evaluated server-side from the match record, and use it for all four of: daily challenges, weekly
> challenges, achievement unlocks and loadout unlocks. Build the achievement set in the three tiers
> in `docs/INSPIRATION.md` § 5.6, every one paying a title, a badge or a banner tracker so nothing
> is a dead list. Every challenge must be completable in Practice against bots, which is the rule
> from § 5.4 that keeps the loadout system out of the competitive integrity problem, and a test
> must assert it for every challenge in the set. No challenge may reward ignoring the can.

### PROMPT I4 · Hero loadouts

**Run after I3. This is `FUTURE.md` Phase 10's second half.**

> Read `CLAUDE.md`, then `docs/VISION.md` § 1 twice, then `docs/TODO.md`,
> `docs/Hero_Strike_Balance.md`, `docs/INSPIRATION.md` § 5 and `docs/FUTURE.md` § 10. I3 must be in.
>
> Build the hero loadout: a pool of options per ability slot as sketched in
> `docs/INSPIRATION.md` § 5.1, every option a sidegrade at an unchanged ability budget, chosen
> before the match and shown publicly in the lobby and on the scoreboard. Define the options and
> the budget arithmetic in `Packages/com.tumbangpreso.core/` and write a test that fails if any
> option is a strict improvement on its siblings along every axis. Unlock each option with the
> challenge engine from I3. Do NOT build the swap-at-role-change idea from § 5.5 in this pass:
> prototype it in custom games afterwards and write the measurement into `docs/TODO.md` before it
> goes near ranked. Hero Strike only. Classic gets no abilities, ever.

### PROMPT I5 · The queue and mode structure

**Run before `FUTURE.md` Phase 7 (matchmaking), because it decides what Phase 7 queues into.**

> Read `CLAUDE.md`, `docs/VISION.md` § 1, `docs/TODO.md` and `docs/INSPIRATION.md` § 3.
>
> Restructure the play menu into QUICK MATCH, RANKED, ARCADE, CUSTOM and PRACTICE, with the mode
> (Classic or Hero Strike) chosen inside each rather than as a peer of them. Modes are rulesets and
> queues are stakes: do not create a third ruleset called Ranked. Implement the population gating
> rule from § 3.4 as real logic with the thresholds in configuration, so a queue opens and merges
> on measured wait times rather than on a guess, and tell the player plainly when a queue is closed.
> Start with a single casual queue and the structure in place to split it. Extend
> `ConvertedMainMenu` and `ConvertedMatchSetup` rather than building new screens.

### PROMPT I6 · The daily seed, with medals and a ghost

**Run any time after `FUTURE.md` Phase 2. Independent of everything else.**

> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/INSPIRATION.md` §§ 2.9, 2.14 and
> 2.30.
>
> Build the daily seed mode: one deterministic match a day, identical for every player, same map,
> same taya order, same bots, one attempt, a leaderboard and a short shareable result string. It
> must need no matchmaking and no other human player, which is what makes it the only retention
> feature that works at thirty concurrent players. Derive the seed from the date so every client
> agrees without asking a server. Submit the result through the Cloud Code endpoint from
> `FUTURE.md` Phase 2, and write it to the local machine leaderboard from I10 as well, so the mode
> still has a scoreboard with no internet.
>
> Add medals in the Trackmania sense, per § 2.14: bronze, silver, gold and author score thresholds
> published with the seed, so the player is chasing a bar rather than a population. Add a single
> modifier field per § 2.30, one tsinelas only, half stamina, double taya reach, so a new twist a
> week costs one line of data. If replays exist by then, add the ghost of the player's own best run.

### PROMPT I7 · The moment: best-moment card, killcam and clip export

**Run after `FUTURE.md` Phase 2.**

> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/INSPIRATION.md` § 4.3.
>
> Build the three things that make a good moment survive the half second it happened in: a
> best-moment card generated as a still from events already raised and framed for a phone
> screenshot, a three-second replay of the tag that caught you shown to the victim only, and a save
> the last 30 seconds clip export. Do the card first: it is the cheapest and it is the one that
> gets posted. The killcam should reuse whatever `FUTURE.md` § 17 determines about replay
> determinism, so if § 17 is not done, record just the two bodies and the camera rather than
> building a second replay system.

### PROMPT I8 · Endorsements, trust, the avoid list and the dead round

**Run alongside `FUTURE.md` Phase 8.**

> Read `CLAUDE.md`, `docs/VISION.md`, `docs/Design.md`, `docs/TODO.md` and `docs/INSPIRATION.md`
> §§ 2.4, 2.8, 2.23, 2.13, 2.15, 2.28 and 4.2.
>
> Four things. First, endorsements: one button on the end-of-match screen, a level on the
> nameplate, decaying slowly. Second, a trust score from account age, matches finished, report count
> and leave rate, used to pool brand-new and heavily-reported accounts separately, per § 2.8. Third,
> an avoid list of three players that matchmaking honours, per § 2.23: at four players one
> unpleasant person is a quarter of the lobby, so this is worth more here than in a ten-player game.
>
> Fourth, and this one is design rather than implementation. § 4.2 records that a player far behind
> at the final round has nothing to play for, and there are now FOUR candidate answers on the table:
> pay all four placements rather than only first, a final-round multiplier, per-round objectives
> that pay regardless of score (§ 4.2), a shrinking box in the last 20 seconds built on the existing
> `CONFINEMENT_RADIUS` (§ 2.15), and passive defence that BANKS rather than scores so a knockdown
> can steal it (§ 2.28). The last one would also address the known balance risk that passive defence
> pays 900 a round against 100 for a knockdown, and it is the largest change of the set. Prototype
> them in custom games, measure with `BotBehaviourProbe` across multiple runs because `docs/TODO.md`
> § 16 records a 20 per cent spread on a single run, and write the answer into `docs/Design.md` with
> the measurement rather than picking one by taste. A casual-only comeback assist per § 2.13 is
> acceptable; ranked gets none of it.

### PROMPT I9 · The training room and replay takeover

**Run after `FUTURE.md` Phase 17's determinism proof. Independent of everything else.**

> Read `CLAUDE.md`, `docs/VISION.md`, `docs/Design.md`, `docs/TODO.md`, `docs/Guided_Training.md`
> and `docs/INSPIRATION.md` §§ 2.20 and 2.21.
>
> Build the training room. Contact in this game resolves by DISTANCE on the host and never by a
> trigger volume, so every combat range is a number rather than a mesh and can be drawn exactly:
> render the tag window as a ring on the taya, the throw arc as a live trajectory, the shove cone,
> the confinement box and the frame on which the lata's fall registers. Read the numbers from
> `Packages/com.tumbangpreso.core/` rather than restating them, so the room can never disagree with
> the game. Add a slow-motion step and a reset-to-position key. Then, if `FUTURE.md` § 17 has proved
> replay determinism, add replay takeover: pause any replay, take that seat, and play the rest out
> live. Extend `GuidedTraining` rather than building a second training path.

### PROMPT I10 · The population schedule, in-client tournaments and the local leaderboard

**Run any time. This is the cheapest population work in either document and it is mostly not code.**

> Read `CLAUDE.md`, `docs/VISION.md`, `docs/TODO.md` and `docs/INSPIRATION.md` §§ 2.6, 2.24 and
> 2.26. `FUTURE.md` Phase 17's tournament mode helps but is not required.
>
> Three things, cheapest first. One: a local machine leaderboard, a plain file on disk, best daily
> seed and best knockdown streak on this PC with three-letter initials, which needs no internet, no
> account and no other players and is therefore the only leaderboard that works on day one. Two: a
> scheduled weekly hour shown in the main menu with a countdown, so a small population concentrates
> into full lobbies instead of spreading across empty ones. Three: an in-client tournament that runs
> in that window, using `SpectatorCamera` and the broadcast clock that already exist and the custom
> lobby from `FUTURE.md` § 12. Do not build a bracket service: a static page is enough.

### PROMPT I11 · Arcade variants from parts that already exist

**Run after `FUTURE.md` Phase 12's custom games.**

> Read `CLAUDE.md`, `docs/VISION.md` § 1, `docs/Design.md`, `docs/TODO.md` and
> `docs/INSPIRATION.md` §§ 2.16 and 2.30. `FUTURE.md` Phase 12 must be in.
>
> Build three arcade variants out of rules the core already owns, each as a new mode rather than as
> a change to Classic or Hero Strike. SUDDEN DEATH: a tied round with the can standing shrinks the
> box every 10 seconds until it falls, so there are no draws. LAST TSINELAS STANDING: three tsinelas
> per attacker, lose them all and you are out, last attacker takes the round. DAILY MODIFIERS: one
> field on the daily seed from I6 that applies a single rule twist, one tsinelas only, half stamina,
> double taya reach. Every variant's rules go in `Packages/com.tumbangpreso.core/` with tests and
> get written into `docs/Design.md` or a sibling document in the same commit. Touch nothing in
> Classic's own ruleset.

### 8.6 The combined order

`F` is a `FUTURE.md` phase, `I` is a prompt from this file.

| # | Do | Why here |
|---|---|---|
| 1 | **F1** Accounts | Everything keys off a player id. |
| 2 | **F2** Profile, stats, match history | Nothing below has anything to read without it. |
| 3 | **F3** Telemetry | Start it with F2. The first-launch funnel decides what to do next. |
| 4 | **I1** Performance grade, attacker and taya | Highest value in either document, and F2 just built the inputs. |
| 5 | **I2** Banner and trackers | Turns F2's stats into a thing people build. Cheap. |
| 6 | **I10** Weekly hour and local leaderboard | Mostly not code, and it is the fastest fix for an empty queue. |
| 7 | **F4** XP, levels, season track | The reason to come back tomorrow. |
| 8 | **I6** Daily seed, medals, ghost | Retention that works at thirty players. Independent, do it whenever. |
| 9 | **I7** Best-moment card, then killcam | The growth engine. Cheapest piece first. |
| 10 | **F5** Cosmetics and customisation | Now there is something to spend I2, F4 and I3 on. |
| 11 | **I3** Challenge engine and achievements | One system, four features. Build it once, properly. |
| 12 | **F6** Social, friends, parties | Population compounds from here. |
| 13 | **I5** Queue and mode structure | Must land before matchmaking, because it decides what queues exist. |
| 14 | **F7** Matchmaking | Fills the queues I5 defined. |
| 15 | **F8** Competitive integrity | Never after ranked. |
| 16 | **I8** Endorsements, trust, avoid list, dead round | Alongside F8. The dead-round half is real design work, not implementation. |
| 17 | **F9** Ranked, with rank floors | Only now. It needs matchmaking under it, integrity beside it, population to fill it. |
| 18 | **F11** Bots, backfill, population | Move this earlier the moment queue times get bad. |
| 19 | **I4** Hero loadouts | Depth for players who have put fifty hours in. Needs I3. |
| 20 | **F12** Modes, maps, custom games | Custom games first: every other mode is cheaper afterwards. |
| 21 | **I11** Arcade variants | Needs F12. Nearly free once custom games exist. |
| 22 | **I9** Training room, replay takeover | Needs F17's determinism proof for the takeover half; the room itself does not. |
| 23 | **F13** Seasons and live ops | Needs everything above to have something to season. |
| 24 | **F14** Controller, then **F15** Mobile | Independent of the whole column. Start any time somebody wants a break from services. |
| 25 | **F16** Accessibility and localisation | Independent, overdue, and Tagalog is marketing as much as access. |
| 26 | **F17** Tournaments, LAN, replays | ⚠️ **The LAN half of this is not future work.** See below. |
| 27 | **F18** Distribution | Last. |

⚠️⚠️ **AND THE URGENT ITEM IS NOT ON THAT LIST AT ALL.** `FUTURE.md` § 17's first paragraph: nobody
has ever tested a full four-player match end to end with the internet physically disconnected, and
the nationals are in General Santos City. **Do that before any of the twenty-seven rows above**, and
keep it true through every one of them.

---

## 9 · The small borrowings, in one table

Things too small to earn a section and too useful to leave out. All of them are cheap.

| Borrowed from | The thing | What it becomes here |
|---|---|---|
| Fighting games | Input display in training | Show the contextual `E` hold tiers filling, so tap, shove and reset stop being folklore. |
| Racing games | Ghost of your best run | Race your own daily seed replay. Free once replays exist. |
| Rocket League | Quick chat on one key | A ping and comm wheel: "on the can", "behind you", "nice", "sorry". Replaces voice entirely. |
| Overwatch | Potential Play of the Game teaser | The end-of-round "you were one knockdown from second" line. § 4.3. |
| Halo | Post-match carnage report | The full four-player stat grid on the end screen, not just the score. |
| Valorant | Loadout shown before the round | § 5.5. The build is public. |
| Splatoon | Choose a side event | LIGA NG BARANGAY. § 2.10. |
| Souls games | A message left on the ground | A one-line note left where you were tagged, visible to the next player on that map. Cheap, funny, enormously sticky. |
| Mario Kart | Coin sound on a small win | An audio confirmation for every small good thing, not just the big ones. |
| Trackmania | Medals | § 2.14. A bar to beat that needs no other player. |
| Peggle and pinball | The over-the-top finish | The knockdown that wins a match deserves an absurd amount of screen. It is the moment the whole game exists for. |
| Chess and Elo history | A rating graph | One sparkline of the season on the profile. Trivial, and people stare at it. |
| Any speedrun community | Categories | The daily seed leaderboard split by "any tsinelas" and "default tsinelas". Community rules for free. |
| Streaming culture | Spectator delay | § 2.6 and `FUTURE.md` § 17. Needed the first time a match is streamed. |
| Old LAN shooters | A scoreboard key you hold | Hold TAB mid-match for the full stat grid rather than the score strip. |
