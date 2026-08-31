# INSPIRATION.md: what to steal from games that work, and how it lands here

**What this file is.** A study of the systems that keep people playing other games, each one
turned into something specific for a four-player Filipino street game with a rotating taya.
It is the WHY behind [`FUTURE.md`](FUTURE.md), which is the WHAT and the WHEN. **Its ten
prompts live in § 8**, and they inherit `FUTURE.md` § 0.5, the standing preamble, and § 0.6, the
staleness protocol.

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

## 1 · The nine ideas worth doing, ranked

If nothing else on this page gets built, build these.

| # | Idea | Comes from | Why it is first |
|---|---|---|---|
| 1 | **A per-match performance grade, S to D, independent of placement, graded separately as attacker and as taya** | League mastery grades, Dead by Daylight emblems | Three of four players lose every match. A grade is how the other three still had a good night. **This is the single highest-value system in this document.** § 4.1 |
| 2 | **A banner with three stat trackers you choose** | Apex legends banners | The cheapest status object in games. It makes a stat page a thing people build rather than read. § 2.5 |
| 3 | **Loadout variants unlocked by character challenges** | Risk of Rain 2, Deep Rock, CoD | The thing he actually asked for, and it doubles as the best character tutorial you can write. § 5 |
| 4 | **Trophy Road: a reward track tied to rank that never takes anything back** | Brawl Stars | Solves "rank goes down and I feel worse" for a game whose population cannot afford churn. § 2.1 |
| 5 | **Endorsements** | Overwatch | One button. The only anti-toxicity system with evidence behind it. § 2.4 |
| 6 | **In-client tournaments** | Rocket League | This project is going to nationals. The tournament tooling is 70 per cent built already. § 2.6 |
| 7 | **Rank floors and a ratcheted reward road** | Marvel Snap, Brawl Stars | A bad night can never undo a good week, which is the most common reason people stop queueing ranked. § 2.19 |
| 8 | **A scheduled weekly hour** | Pokemon GO community day | Not content, a schedule. It concentrates thirty players into full lobbies instead of spreading them across empty ones, and it is nearly free. § 2.24 |
| 9 | **A training room that draws the invisible ranges** | Fighting game training modes | Contact resolves by distance, never by a volume, so every range in this game is a number that can be drawn exactly. § 2.21 |

---

## 2 · Game by game

Each entry: what they do, why it works, **what it becomes here**, and what not to copy.

⚠️⚠️ **ALL THIRTY ENTRIES IN THIS SECTION ARE APPROVED IN PRINCIPLE. 🧑, 2026-08-31, on being shown
the list: *"thats goated ok i approve all"*.**

**What that approval means and does not mean, because the difference matters and a later session
will otherwise read it as a mandate:**

- ✅ **It means the DIRECTION is signed off.** Nobody needs to re-argue whether the game should have
  a performance grade, a banner, rank floors or a weekly hour. Those questions are
  settled and re-opening them wastes a session.
- ❌ **It does not mean any of it is scheduled.** Nothing here is open work. `docs/TODO.md` is the
  worklist and none of this is in it.
- ❌ **It does not approve a number.** Every threshold, curve, tier count and timing in this file is
  an illustration for a measurement, not a value to ship. `docs/Design.md` still governs balance
  and § 0.6 of `FUTURE.md` says so at length.
- ❌ **It does not override `VISION.md`.** Where an entry here and `VISION.md` disagree about what
  the game IS, `VISION.md` wins, and § 1's rule that Classic never gets powers is not negotiable by
  anything on this page.
- ⚠️ **And two entries are approved as things to PROTOTYPE AND MEASURE, not to build.** They change
  rules rather than adding systems, and shipping one on the strength of a paragraph here would be
  exactly the mistake this repository keeps a measurement discipline to avoid: **§ 2.15** (shrinking
  the box in the last 20 seconds) and **§ 5.5** (swapping loadout at the role change). Each carries
  its own note saying so. `BotBehaviourProbe` over several runs, then `docs/Design.md` with the
  measurement.
- ❌ **§ 2.28 IS REJECTED, and the approval above does not cover it.** 🧑 killed the passive-defence
  banking proposal the same day it was written. The entry stays as a record so nobody re-derives it.


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

**What it becomes here:** endorsements go in, one button on the end-of-match screen, a count on the
banner. ⚠️ **A count, not a decaying level. CUT 2026-08-31.** Overwatch decays its endorsement level
so the number reflects recent behaviour rather than a good week two years ago; here that is a timer,
a rule and a paragraph of explanation bought for a difference nobody would notice. **Just count
them.** **Street Hype already IS the on-fire meter** and `VISION.md` says it is Classic's identity,
so extend it rather than adding a second one. Play of the Game becomes the **best moment card** in
§ 4.3: a still image rather than a replay is far cheaper and gets posted more.

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

### 2.8 Counter-Strike and the trust problem ❌ REJECTED 2026-08-31

- **Prime and Trust Factor.** A hidden score from account age, matches finished, reports and
  behaviour, used to sort players into cleaner or dirtier pools.

⚠️⚠️ **THE ADAPTATION IS CUT, AND THE REASON IS ARITHMETIC RATHER THAN TASTE.** It was proposed as a
cheap score from account age, matches finished, report count and leave rate, used to keep brand-new
and heavily-reported accounts in their own pool.

**A trust score's entire purpose is to sort players into separate pools, and this game does not
have the population to have pools.** § 3.4 already argues that four queues at thirty concurrent
players is zero queues; a hidden fifth and sixth pool underneath them is the same mistake with the
seam hidden, and the players who land in the small pool get the worst queue times in the game as a
reward for being new.

**What does the job instead, at a fraction of the cost:** reporting, and the avoid list in § 2.23.
Smurfs are handled by Glicko-2's rating deviation, which climbs a clearly-stronger account out of a
low band in a handful of games for free (`FUTURE.md` § 9).

⚠️ **Revisit this only when the population supports pools**, which is the same condition § 3.4
already sets for splitting a queue at all. Not before.

### 2.9 Wordle, Tetris 99 dailies, and the one-attempt hook ❌ REJECTED 2026-08-31

One puzzle a day, the same for everybody, one attempt, a shareable result.

⚠️⚠️ **THE DAILY WAS PROPOSED TWICE, REWORKED ONCE, AND CUT. DO NOT BRING IT BACK.** 🧑, on the
reworked version: *"remove daily seed lowkey"*, *"too much shit"*.

**What was proposed, so nobody re-derives either version.** First, a solo match a day against bots,
identical for everybody, with a leaderboard. He rejected that on the grounds that the marquee
retention feature of a multiplayer game should not be the one you play alone. It was then reworked
into three shapes: a daily rule twist on the live queue, a solo time trial with medals and the
ghosts of other players' runs, and a full-lobby squad version. **He cut all three.**

⚠️ **THE REASON IS SCOPE, NOT DESIGN, AND THAT IS THE MORE IMPORTANT KIND OF NO.** The reworked
daily was a better feature than the original. It was still a whole mode, a leaderboard, a medal
table, a seed system and a ghost pipeline, sitting beside a season track, a challenge engine,
achievements, mastery and a rank ladder. **A plan can be full of individually good features and
still be too much GAME**, and that is what this one was. ⚠️ This read "too much for five students"
until 2026-08-31; § 10.3 records why team size is no longer the argument, and note that the season
track and the challenge engine named in this sentence are both cut now too.

**What is genuinely dead:** the daily mode, the daily leaderboard, medals, ghosts as a daily
feature, and the squad variant. `INSPIRATION.md` prompt I6 is deleted.

**What survives elsewhere, and is not a way to smuggle this back in:** § 2.14's medals still apply
if a time-trial mode is ever built for its own sake, ghosts remain a natural use of the replay work
in `FUTURE.md` § 17 if that ships, and § 2.30's modifiers remain available as **custom-game rule
toggles** in `FUTURE.md` § 12, which is where a rule toggle belongs. None of those is a daily.

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

**What it becomes here, if a time-trial mode is ever built for its own sake:** medals on a
time attack. A solo skill target is the only kind of progression that works at thirty concurrent
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

**What it becomes here:** one of the two extra modes the game will ever have.

- ✅ **LAST TSINELAS STANDING**, from the stocks half. Each attacker gets three tsinelas; lose them
  all and you are out; the last attacker standing takes the round. **A completely different game
  out of parts that already exist**, which is why it earns one of the two slots.
- ❌ **SUDDEN DEATH. CUT 2026-08-31.** A tied round with the can standing would have shrunk the box
  every 10 seconds until it fell. It is a fine idea and it was cut with four other modes on
  population grounds: `FUTURE.md` § 12. ⚠️ **If draws turn out to be common enough to matter, this
  is a tie-break RULE for the existing modes, not a mode of its own**, and that is the shape to
  bring it back in.

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
- ❌ **Replay takeover. CUT 2026-08-31.** Pause a replay, take that seat, play it out. The best
  training feature in any fighting game, an enormous amount of work, and an audience of about four
  people. The tower below is the part of this entry that survives.

**What it becomes here:** the tower is worth considering for the **casual** queue at low population
(§ 3.4), because a room-based structure fills faster than a rating band and reads as a hangout
rather than a ladder. Call the floors streets.

❌ **Replay takeover is cut**, and it is worth recording that it was genuinely available here rather
than merely wanted: a replay is the input stream and the simulation is deterministic from it, so
pausing, taking the seat and playing the retrieval again is a real possibility for this codebase.
It teaches the game faster than anything else on this page and it is still not worth what it costs
at this size.

### 2.21 Fighting games in general: the training room

Street Fighter, Tekken and Skullgirls all ship a training mode that shows the invisible: hitboxes,
frame data, ranges, recovery.

**What it becomes here, and this game is unusually suited to it:** contact resolves **by distance
on the host**, never by a trigger volume, which means every combat range in this game is a number
rather than a mesh. **A training room can draw them exactly**, which turns the game's most opaque
interactions into something a player can see and practise, and the data is already there.

⚠️ **DRAW TWO THINGS, NOT FIVE. Cut back on 2026-08-31.** The tag window as a ring on the taya, and
the throw arc as a live trajectory. **Those are the two a player actually needs to learn.** The
shove cone, the confinement box and the frame on which the lata's fall registers were all on the
list and they are clutter: a training room that draws everything is as unreadable as one that draws
nothing.

### 2.22 Halo and Fortnite: the map editor is how a game outlives its team

Forge and Creative both did more for their games' lifespans than any content patch.

**What it becomes here:** the largest project on this page, and the only one that makes the game
survive the team graduating. Not now. But **build the map format so it stays possible**:
data-driven, serialisable, loadable at runtime. `IlalimNgTulayBuilder` and `SceneBuilder` already
build maps from code, which is most of the way to building them from data.

### 2.23 Dota's behaviour score and Overwatch's avoid list

Dota sorts players by a hidden behaviour number. Overwatch lets you avoid a small number of
players.

**What it becomes here: the avoid list of three, and only that.** ⚠️ Dota-style behaviour scoring is cut, § 2.8. An avoid list is
cheaper than a sorting system and does more for how the game feels. At four players a single
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
**local machine leaderboard**. Best knockdown streak and best round on this PC,
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

### 2.28 The Finals: the objective that creates comebacks ❌ REJECTED 2026-08-31

A cashout takes time and can be stolen. The lead is never safe, so nobody stops playing.

⚠️⚠️ **THE ADAPTATION WAS PROPOSED AND 🧑 REJECTED IT THE SAME DAY. DO NOT RE-PROPOSE IT.** The
proposal was that passive defence should BANK into a pot attached to the can rather than score
directly, with a knockdown stealing the pot, so the taya's 900 uncontested points became 900 points
at risk. It was argued as a single change that addressed both the known balance risk and the dead
round in § 4.2.

**It is recorded here rather than deleted because it is the kind of idea that gets re-derived.** It
looks elegant on paper, it maps cleanly onto a real problem the game has, and the next person to
read The Finals' cashout will think of it again. **He has already said no.**

⚠️ **The failure mode is the reason it is a bad fit, and it is worth understanding before proposing
anything shaped like it.** A taya who defends brilliantly for 80 of 90 seconds and eats one lucky
throw walks away with nothing. That is a punishing, swingy outcome landing on the role that
everybody is forced to play once per match, and no amount of tuning the steal percentage changes
what it does to the feel of defending.

**What survives from The Finals:** only the general observation that a lead which cannot be taken
is a lead that ends the match early. § 4.2's remaining candidates are the live ones.

### 2.29 Among Us: the frictionless invite

A four-character code, free on mobile, cross-play, and no account needed to play with your friend
tonight.

**What it becomes here:** already true, and **that is worth protecting rather than building**.
`ServerQuery` resolves a 4-character code LAN-first then online, and `FUTURE.md` § 1 makes accounts
optional and silent for exactly this reason. ⚠️ **Every phase in `FUTURE.md` is a chance to
accidentally put a login wall in front of "play with my friend right now". Do not.**

### 2.30 Slay the Spire's daily and Balatro's run history

A daily climb with modifiers, and a history of every run you have made with the numbers attached.

**What it becomes here:** a pool of custom-game rule toggles in `FUTURE.md` § 12. One tsinelas only.
Half stamina. Double the taya reach. A rule toggle belongs in custom games, not in a daily: § 2.9.

---

### 2.31 Character creators, and the one control every one of them uses

🧑, 2026-08-31: *"I WANT U TO USE EXISTING GAMES AS REFERENCE FOR HOW THEY DID THEIR UI SO THAT
OUR GAME WOULD FEEL INTUITIVE"*. This is that reading, for the MAKE YOUR OWN screen
(`docs/TODO.md` § 108).

**What every creator worth copying agrees on, and what it costs to ignore:**

| The convention | Who does it | Why it is not a style choice |
|---|---|---|
| ⚠️⚠️ **`< NAME  n/total >`, one press per step, never a dropdown** | Elden Ring, Animal Crossing, NBA 2K, Monster Hunter, Stardew Valley | **A creator is BROWSING, not choosing.** `CustomCharacterRules` has 48 hairstyles and 48 tops; a 48-row dropdown is taller than the window, costs two presses per change, and asks the player to read forty-eight names to find one. The count on the control is what says the list is long, which way round it goes, and whether the end has been seen. |
| **The subject is large, static, and never waits** | Stardew Valley, Animal Crossing, Elden Ring | The whole activity is *change one thing, look at it*. A preview that is small, or that rebuilds on a delay, breaks the only loop the screen has. `docs/FUTURE.md` PHASE 5 already says preview through `ModelPreview` with the real shader, never a flat icon. |
| **The camera moves to the part being edited** | Elden Ring, Cyberpunk, Baldur's Gate 3 | Choosing a hat while looking at the knees is choosing blind. ⚠️ **And the AIM has to move with the distance**: zooming toward `ModelPreview.AimHeightRatio` 0.54, the waist, pushes the head out of frame, which is what the version this replaced would have done. |
| **Categories, three or four rows each, never one long list** | All of them | `docs/TODO.md` § 92: *"theres liek 20 shits at once"*. Fifteen steppers in one scroll is the same fault with different nouns. Six sections of three or four is one screen with no scrolling on the window he plays in. |
| **Randomise is a first-class button** | The Sims, Elden Ring, NBA 2K | It is how somebody who does not want to spend an evening gets a character in one press, and it is how somebody who does discovers combinations they would not have stepped to. SURPRISE ME sits beside PRESETS for that reason. |
| ⚠️⚠️ **Cancel means cancel** | All of them | A creator writes to a working copy and commits on one button. **A slot you cannot leave without overwriting is not a save slot**, and § 107's brief is *"3 characters u can save at once"*. |

⚠️ **WHAT DOES NOT TRANSFER, AND IT IS THE LOUDEST HALF OF THOSE SCREENS.** Elden Ring's
sliders and Cyberpunk's continuous dials belong to games where nobody has to read a silhouette at
distance. `docs/VISION.md` § 2 is a measured readability budget, and `Roster.HeroPeople`'s header
records that a bigger body is genuinely better at the taya's job. **Height is seven named steps
inside 85 to 115 per cent, not a slider**, and that is the same argument `TintStrengths` makes on
character select: names rather than numbers, three steps, never more.

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
| **ARCADE** | Rotating featured mode, live events | Same |
| **CUSTOM** | Private code, full rule toggles, tournament mode | Same |
| **PRACTICE** | Bots, training, the range, challenge completion | Same |

### 3.2 So yes: RANKED is its own top-level menu entry

Which is what he is asking for, done in a way that does not fork the game.

```
PLAY
 ├─ QUICK MATCH      -> pick CLASSIC or HERO STRIKE
 ├─ RANKED           -> pick CLASSIC or HERO STRIKE, separate ladders, own badge
 ├─ ARCADE           -> featured mode, live event
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

⚠️  **PHASE 7 SHIPPED ON 2026-08-31 AND TOOK THIS SECTION'S ADVICE LITERALLY: ONE CASUAL
QUEUE.** `docs/TODO.md` § 103.6. `MatchmakingRules.PoolKey` carries the mode, the stake, the input
device and the platform, so the structure for splitting is in place and unused, and adding RANKED to
the menu is a `QueueStake` rather than new machinery. **The measured GATE below is not built**,
because with one queue there is nothing to gate; it becomes real the day a second queue is added,
and that is the moment to run PROMPT I5 properly.

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

**Four candidate answers, all cheap, none built, none measured:**

1. **Pay all four placements**, not just first. 2nd and 3rd worth real rating and XP means the
   fight for 2nd is a live match inside the lost one.
2. **A final-round multiplier**, so the last round is worth more and a comeback is arithmetically
   possible. Say 1.5x. This is the standard answer and it is also the one most likely to feel
   cheap, so measure it rather than assuming.
3. **Per-round objectives** that pay regardless of score: survive the round without being tagged,
   land a knockdown from outside the box, retrieve while the taya is within 3 m. A losing player
   always has something to chase.
4. **A shrinking box in the last 20 seconds**, built on the confinement radius that already exists
   and that spawns are already computed from. It is the most thematically honest of the four, and
   it is also the one most likely to make the taya STRONGER rather than weaker, which is the
   opposite of what is wanted, so it is the one that most needs measuring. § 2.15.

❌ **A fifth was proposed and rejected on 2026-08-31**: passive defence banking into a pot that a
knockdown steals. § 2.28 records why, so nobody re-derives it.

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

**One place to copy from.** Each block below is a complete brief written to be pasted into a fresh
session on its own. They are uniform on purpose, and they inherit the standing preamble in
`FUTURE.md` § 0.5 rather than repeating it, so there is one copy of the rules to keep correct.

⚠️ **Each carries a VERIFY FIRST block.** These were written on 2026-08-31 against a codebase that
keeps moving. A prompt that turns out to be wrong is handled by `FUTURE.md` § 0.5 rule 11: do the
part that still makes sense, correct the plan, and put the disagreement at the top of the handoff.

⚠️ **If it has been more than a month, run `FUTURE.md` § 19.0 first.** It refreshes the factual
claims in both files in one short session and costs less than building a phase against a stale
brief.

| Prompt | What | Depends on |
|---|---|---|
| [I1](#prompt-i1--the-performance-grade-attacker-and-taya) | Performance grade, attacker and taya | `FUTURE.md` Phase 2 |
| [I2](#prompt-i2--the-banner-and-stat-trackers) | Banner and stat trackers | Phase 2 |
| [I3](#prompt-i3--the-challenge-engine-achievements-and-loadout-unlocks) | Challenge engine and achievements | Phases 2, 4 |
| [I4](#prompt-i4--hero-loadouts) | Hero loadouts | I3 |
| [I5](#prompt-i5--the-queue-and-mode-structure) | Queue and mode structure | Before Phase 7 |
| [I7](#prompt-i7--the-moment-best-moment-card-killcam-and-clip-export) | Best-moment card, killcam, clips | Phase 2 |
| [I8](#prompt-i8--endorsements-reporting-the-avoid-list-and-the-dead-round) | Endorsements, reporting, avoid list, dead round | Alongside Phase 8 |
| [I9](#prompt-i9--the-training-room) | Training room | Nothing |
| [I10](#prompt-i10--the-population-schedule-in-client-tournaments-and-the-local-leaderboard) | Weekly hour, tournaments, local board | Nothing |
| [I11](#prompt-i11--the-two-extra-modes) | The two extra modes | Phase 12 |

---

### PROMPT I1 · The performance grade, attacker and taya

**Run after `FUTURE.md` Phase 2. This is the highest-value item in either document.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/INSPIRATION.md` §§ 4.1, 2.2 and 2.17. They carry the rules of the repo, what
> the game is for, what is open, the standing rules every prompt inherits, and this task's brief.
> Do not skip them because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** `FUTURE.md` Phase 2 shipped, and the stat set in its § 2.2 is actually being
> collected: the grade is computed from those stats and from nothing else.
>
> **Build a per-match performance grade, S to D, computed independently of placement.** Three of
> four players lose every match in this game, and placement alone is a brutal retention curve.
> Grade against the distribution of that character's performances at that rating band.
>
> ⚠️ **It is TWO grades, not one.** Every player is taya for one round and an attacker for the
> others, and those are different games with different inputs. An ATTACKER grade from knockdown
> rate, retrievals under pressure and time alive as last attacker. A TAYA grade from tags per round
> defended, passive defence held and time to reset. Combine by rounds played in each role for a
> headline, and **show both**, because "A attacker, C taya" tells a player what to practise and a
> single letter does not.
>
> **Constraints.**
> - The whole grading model lives in `Packages/com.tumbangpreso.core/`.
> - ⚠️⚠️ **The grade must never feed rating.** Rating is placement, because placement is the game.
>   Mixing them produces a ladder people farm by playing selfishly, which is the failure mode of
>   every performance-based ranked system that has ever been tried. § 4.1 has the argument.
> - Tests: one asserting the grade is mathematically independent of placement, and one asserting a
>   player who placed **fourth** can score an **S**.
>
> **Done when** both grades appear on the end-of-match screen with the two or three stats that
> drove each, they feed character mastery, and `FUTURE.md` § 0.5 rule 9 is satisfied.

---

### PROMPT I2 · The banner and stat trackers

**Run after `FUTURE.md` Phase 2, alongside or after Phase 5. Cheap, and it is the second-highest
value item here.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/INSPIRATION.md` § 2.5. Do not skip them because this prompt summarises the
> task; the summary is not the rules.
>
> **VERIFY FIRST.** `FUTURE.md` Phase 2 shipped, so the stats exist to track. If Phase 5 has
> shipped, frames and poses are cosmetics with string ids and should reuse that inventory rather
> than adding a second one.
>
> **Build the banner:** a frame, a character pose, a badge and **three stat trackers the player
> chooses from the full stat list**. Show it in the lobby, on the scoreboard, on the profile and on
> the end-of-match screen.
>
> **Why it is worth doing early:** three chosen trackers turn a stats page from something read once
> into something a player builds. "Most knockdowns in a match: 7" beside a name in the lobby is
> worth more retention per line of code than anything else in this document.
>
> **Constraints.** Trackers read from the profile; they are never a second source of truth.
> Replicate through the seat info that already crosses at match start rather than adding a
> protocol. Frames, poses and badges are cosmetics with string ids per `FUTURE.md` § 5.
>
> **Done when** a banner is visible to every peer in a lobby, trackers can be changed and persist,
> and `FUTURE.md` § 0.5 rule 9 is satisfied.

---

### PROMPT I3 · The challenge engine, achievements and loadout unlocks

**Run after `FUTURE.md` Phase 4. One system, four features. Build it once, properly.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/INSPIRATION.md` §§ 5.3, 5.4, 5.6 and 2.12, then `docs/FUTURE.md` § 10. Do not
> skip them because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** `FUTURE.md` Phases 2 and 4 shipped. Check whether Phase 13 has already built a
> challenge system: if it has, extend it rather than writing a second.
>
> **Build ONE challenge engine** in `Packages/com.tumbangpreso.core/`, with a single condition
> type, evaluated server-side from the match record, and use it for all four of: daily challenges,
> weekly challenges, achievement unlocks and loadout unlocks. The only difference between them is
> the reward and whether they repeat.
>
> **Then the achievement set**, in three tiers, per § 5.6.
> - **Bronze, the teaching tier**: first knockdown, first retrieval under pressure, first win as
>   taya. These exist to be seen in the first hour and to point at parts of the game.
> - **Silver, the grind tier**: 100 knockdowns, a match with every character, all ten tsinelas used.
>   Visible progress bars, which are most of the appeal.
> - **Gold, the story tier**: win from last place at the final round, three knockdowns in one round,
>   a full round as last attacker without being tagged, win without throwing at all.
>
> **Constraints.**
> - ⚠️⚠️ **Every challenge must be completable in Practice against bots, and a test must assert it
>   for every challenge in the set.** § 5.4: this is the rule that keeps the loadout system out of
>   the competitive integrity problem, because the gate then costs time learning a character rather
>   than matches won against people.
> - ⚠️ **No challenge may reward ignoring the can.** "Get 10 knockdowns" teaches exactly that, and
>   the can is the whole game.
> - Every achievement pays a title, a badge or a banner tracker, so nothing is a dead list.
> - ⚠️ **Nothing competitive is ever gated behind an achievement.** They are bragging, and bragging
>   is enough.
>
> **Done when** one engine serves all four features, the practice-completable test passes for the
> whole set, and `FUTURE.md` § 0.5 rule 9 is satisfied.

---

### PROMPT I4 · Hero loadouts

**Run after I3. This is the second half of `FUTURE.md` Phase 10 and it carries the balance risk.**

> Read `CLAUDE.md` first, then `docs/VISION.md` § 1 twice, then `docs/TODO.md`, then
> `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/INSPIRATION.md` § 5 in full, then
> `docs/Hero_Strike_Balance.md` and `docs/FUTURE.md` § 10. Do not skip them because this prompt
> summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** I3 shipped, so the challenge engine exists. Confirm the ability budgets in
> `docs/Hero_Strike_Balance.md` still describe the shipped kits, because every option here is
> defined as a trade at an unchanged budget and that only means something if the budget is real.
>
> **Build the hero loadout:** a pool of options per ability slot as sketched in § 5.1, chosen
> before the match, unlocked through the I3 challenge engine.
>
> **The rules, all load-bearing.**
> - ⚠️⚠️ **Every option is a sidegrade at an unchanged budget.** Wider but shorter, faster but
>   louder, stronger but slower to arrive. Nothing unlocks more damage, range, duration or a shorter
>   cooldown. **Write a test that fails if any option is a strict improvement on its siblings along
>   every axis.**
> - ⚠️ **The build is public**, shown in the lobby and on the scoreboard. Hidden loadouts in a
>   four-player fight are information asymmetry that feels like cheating, and seeing an opponent's
>   build is where counterplay comes from.
> - ⚠️ **Do not build the swap-at-role-change idea from § 5.5 in this pass.** It is the most
>   interesting idea in this document and a real balance risk. Prototype it in custom games
>   afterwards, measure it, and write the measurement into `docs/TODO.md` before it goes near ranked.
> - **Hero Strike only. Classic never gets abilities**, per `VISION.md` § 1.
>
> **Constraints.** Option definitions and budget arithmetic in `Packages/com.tumbangpreso.core/`.
>
> **Done when** a build can be chosen, unlocked by a bot-completable challenge, and seen by
> opponents, the sidegrade test exists and passes, and `FUTURE.md` § 0.5 rule 9 is satisfied.

---

### PROMPT I5 · The queue and mode structure

**Run before `FUTURE.md` Phase 7, because it decides what Phase 7 queues into.**

> Read `CLAUDE.md` first, then `docs/VISION.md` § 1, then `docs/TODO.md`, then `docs/FUTURE.md`
> §§ 0.5 and 0.6, then `docs/INSPIRATION.md` § 3 in full. Do not skip them because this prompt
> summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Read `ConvertedMainMenu` and `ConvertedMatchSetup` before designing any screen:
> the practice and multiplayer tabs already exist and this restructures them rather than replacing
> them.
>
> **Build the structure in § 3.2:** PLAY containing QUICK MATCH, RANKED, ARCADE, CUSTOM and
> PRACTICE, with the mode (Classic or Hero Strike) chosen **inside** each rather than as a peer of
> them.
>
> **The rule this rests on.** ⚠️⚠️ **Modes are rulesets and queues are stakes. Do not create a
> third ruleset called Ranked.** `VISION.md` § 1 says both modes are first class; a ranked mode with
> its own rules becomes a third game to balance and, worse, practice in casual stops transferring to
> ranked, which is the fastest way to make a competitive game feel unfair. RANKED gets its own
> screen, art and badge. It changes stakes and integrity, never the rules in `docs/Design.md`.
>
> **Also build the population gate from § 3.4**, as real logic with the thresholds in
> configuration: a queue opens when its median wait sits under 60 seconds for a week, and merges
> back when its 90th percentile exceeds 180 seconds for a week. ⚠️ **Four queues at thirty
> concurrent players is zero queues.** Start with one casual queue and the structure in place to
> split it, and **tell the player plainly when a queue is closed**, because "RANKED HERO STRIKE
> opens at a bigger population" is respectable and a queue that never fills is not.
>
> **Optional, and flag it rather than deciding it alone:** § 3.3 argues for unique character
> selection in ranked only, since four players can currently all pick the same character.
> Prototype it in custom games and put the question in your handoff.
>
> **Done when** the menu matches § 3.2, the gate opens and merges queues off measured waits, and
> `FUTURE.md` § 0.5 rule 9 is satisfied.

---

### PROMPT I7 · The moment: best-moment card, killcam and clip export

**Run after `FUTURE.md` Phase 2. The card alone is the best growth-per-hour item in either file.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/INSPIRATION.md` § 4.3. Do not skip them because this prompt summarises the
> task; the summary is not the rules.
>
> **VERIFY FIRST.** Check whether `FUTURE.md` § 17 has proved replay determinism yet. It changes
> how the killcam is built and nothing else here depends on it.
>
> **Build three things, cheapest first.**
> 1. **A best-moment card**: a still, generated from events already raised, framed, with the stat
>    line under it, designed for a phone screenshot. Do this one first. A great retrieval happens in
>    half a second and in a team game a teammate saw it; here nobody did.
> 2. **A three-second replay of the tag that caught you, shown to the victim only.** It converts
>    "that was bullshit" into "I saw what I did wrong", which is the most valuable thing a
>    competitive game can show a losing player.
> 3. **Clip export**: save the last 30 seconds.
>
> **Constraints.** ⚠️ **Do not build a second replay system.** If § 17's determinism work is done,
> reuse it. If it is not, record just the two bodies and the camera for the killcam and say so in
> the handoff.
>
> **Done when** a match produces a card worth posting, and `FUTURE.md` § 0.5 rule 9 is satisfied.

---

### PROMPT I8 · Endorsements, reporting, the avoid list and the dead round

**Run alongside `FUTURE.md` Phase 8. The fourth item is design work, not implementation.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/Design.md`, then `docs/TODO.md` § 16,
> then `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/INSPIRATION.md` §§ 2.4, 2.8, 2.23, 2.13, 2.15,
> 2.28 and 4.2. Do not skip them because this prompt summarises the task; the summary is not the
> rules.
>
> **VERIFY FIRST.** Read `docs/Design.md` for the current passive-defence and knockdown values.
> Several arguments below rest on passive defence paying far more per round than a knockdown, and
> if that has been rebalanced since, the fourth item changes shape.
>
> **Build three, then investigate the fourth.**
> 1. **Endorsements**: one button on the end-of-match screen, and a count on the banner. It is the
>    only anti-toxicity system with evidence behind it and it costs one button.
>    ❌ **No decay.** Cut on 2026-08-31: a timer, a rule and a thing to explain, on a nice-to-have,
>    and nobody was going to notice the difference between a count and a decayed count. § 2.4.
> 2. **Reporting**: one button, a reason, from the end-of-match screen and the profile.
>    ❌ **No trust score.** It was cut on 2026-08-31 and § 2.8 has the arithmetic. The short version
>    is that a trust score exists to sort players into separate pools, and this population cannot
>    fill the pools it already has.
> 3. **An avoid list of three** that matchmaking honours. At four players one unpleasant person is a
>    quarter of the lobby, so this matters more here than in a ten-player game.
> 4. **The dead round.** § 4.2 records that a player far behind at the final round has ninety
>    seconds of nothing, and there are four candidates on the table with none built and none
>    measured: pay all four placements rather than only first; a final-round multiplier; per-round
>    objectives that pay regardless of score; and a shrinking box in the last 20 seconds built on
>    the existing confinement radius (§ 2.15). ⚠️ **A fifth was proposed and rejected on
>    2026-08-31**, passive defence banking into a stealable pot: see § 2.28 for why, and do not
>    re-derive it.
>
> **Constraints on the fourth item.** ⚠️ **Prototype in custom games. Measure across several
> `BotBehaviourProbe` runs**, because `docs/TODO.md` § 16 records a 20 per cent spread on a single
> run, so one run is never a comparison. **Write the answer into `docs/Design.md` with the
> measurement** rather than picking one by taste. A casual-only comeback assist per § 2.13 is
> acceptable; ⚠️ **ranked gets none of it**.
>
> **Done when** the first three ship, the fourth has a measured recommendation written into
> `docs/Design.md`, and `FUTURE.md` § 0.5 rule 9 is satisfied.

---

### PROMPT I9 · The training room

**Run any time. Nothing blocks it.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/Design.md`, then `docs/TODO.md`, then
> `docs/Guided_Training.md`, then `docs/FUTURE.md` §§ 0.5 and 0.6, then `docs/INSPIRATION.md`
> § 2.21. Do not skip them because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Confirm contact still resolves by distance on the host rather than by a trigger
> volume, because the entire value of this feature is that every combat range is a number that can
> be drawn exactly.
>
> **Build the training room, drawing exactly TWO things**: the tag window as a ring on the taya, and
> the throw arc as a live trajectory. Add a slow-motion step and a reset-to-position key.
>
> ⚠️ **Two, not five.** The shove cone, the confinement box and the lata's fall frame were cut on
> 2026-08-31: a training room that draws everything is as unreadable as one that draws nothing.
> ❌ **And replay takeover is cut**, § 2.20. Do not build it.
>
> ⚠️ **Read every number from `Packages/com.tumbangpreso.core/` rather than restating it**, so the
> room can never disagree with the game. A training mode that lies is worse than none.
>
> **Constraints.** Extend `GuidedTraining`; do not build a second training path.
>
> **Done when** the tag radius and the throw arc can be seen and practised, the numbers provably
> come from the core, and `FUTURE.md` § 0.5 rule 9 is satisfied.

---

### PROMPT I10 · The population schedule, in-client tournaments and the local leaderboard

**Run any time. The cheapest population work in either document, and most of it is not code.**

> Read `CLAUDE.md` first, then `docs/VISION.md`, then `docs/TODO.md`, then `docs/FUTURE.md` §§ 0.5
> and 0.6, then `docs/INSPIRATION.md` §§ 2.6, 2.24 and 2.26. Do not skip them because this prompt
> summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** Check what `SpectatorCamera` and the broadcast clock already do before building
> anything for the tournament half: this is mostly wiring rather than invention.
>
> **Build three things, cheapest first.**
> 1. **A local machine leaderboard**: a plain file on disk, best round and best knockdown
>    streak on this PC, three-letter initials, arcade style. ⚠️ **It needs no internet, no account
>    and no other players, which makes it the only leaderboard that works on day one** and in a
>    school computer lab.
> 2. **A scheduled weekly hour**, shown in the main menu with a countdown. A small game does not
>    have a population problem at every hour, it has one at most hours, and a schedule concentrates
>    thirty players into full lobbies instead of spreading them across empty ones. **This is a
>    schedule, not content, and it is nearly free.**
> 3. **An in-client tournament** that runs in that window, using the spectator camera and broadcast
>    clock that already exist and the custom lobby from `FUTURE.md` § 12.
>
> **Constraints.** ⚠️ **Do not build a bracket service.** A static page is enough and it costs
> nothing to host.
>
> **Done when** the local board persists across sessions with no network, the countdown is visible
> from the menu, and `FUTURE.md` § 0.5 rule 9 is satisfied.

---

### PROMPT I11 · The two extra modes

**Run after `FUTURE.md` Phase 12's custom games. Nearly free once those exist.**

> Read `CLAUDE.md` first, then `docs/VISION.md` § 1, then `docs/Design.md`, then `docs/TODO.md`,
> then `docs/FUTURE.md` §§ 0.5, 0.6 and 12, then `docs/INSPIRATION.md` §§ 2.16 and 2.30. Do not skip
> them because this prompt summarises the task; the summary is not the rules.
>
> **VERIFY FIRST.** `FUTURE.md` Phase 12's custom games shipped, and the rules core still owns the
> round, because each mode below is a rules change expressed there rather than in Unity code.
>
> ⚠️⚠️ **TWO MODES, EVER, AND THIS PROMPT IS THE WHOLE ARCADE.** `FUTURE.md` § 12 records the cut:
> seven were proposed, nine modes would split thirty players nine ways, and a mode nobody can fill
> is worse than a mode that does not exist. **Do not add a third.**
>
> **1. LAST TSINELAS STANDING.** Three tsinelas per attacker; lose them all and you are out; the
> last attacker takes the round. The most different game available from parts that already exist.
>
> **2. MIRROR.** Everyone gets the same character and tsinelas, rotated weekly. One line of lobby
> logic and a genuinely good competitive format.
>
> **Also, and it is not a mode:** the modifier pool from § 2.30 belongs in Phase 12's custom-game
> **rule toggles**, which is where a rule toggle belongs. § 2.9 records why the daily that used to
> carry them was cut.
>
> ❌ **Sudden death, King of the Can, time attack, survival and 2v2 are all cut**, § 12. Do not
> build them and do not re-propose them without reading the register in § 10 first.
>
> **Constraints.** ⚠️⚠️ **Touch nothing in Classic's own ruleset.** A new mode is a new mode.
> `docs/Design.md` governs Classic and `VISION.md` § 1 governs why. Both modes' rules go in
> `Packages/com.tumbangpreso.core/` with tests and are written into `docs/Design.md` or a sibling
> document in the same commit as the code.
>
> **Done when** both modes can be selected in custom games and played to completion, Classic is
> unchanged in its rules, and `FUTURE.md` § 0.5 rule 9 is satisfied.

---

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
| 8 | **I7** Best-moment card, then killcam | The growth engine. Cheapest piece first. |
| 9 | **F5** Cosmetics and customisation | Now there is something to spend I2, F4 and I3 on. |
| 10 | **I3** Challenge engine and achievements | One system, four features. Build it once, properly. |
| 11 | **F6** Social, friends, parties | Population compounds from here. |
| 12 | **I5** Queue and mode structure | Must land before matchmaking, because it decides what queues exist. |
| 13 | **F7** Matchmaking | Fills the queues I5 defined. |
| 14 | **F8** Competitive integrity | Never after ranked. |
| 15 | **I8** Endorsements, trust, avoid list, dead round | Alongside F8. The dead-round half is real design work, not implementation. |
| 16 | **F9** Ranked, with rank floors | Only now. It needs matchmaking under it, integrity beside it, population to fill it. |
| 17 | **F11** Bots, backfill, population | Move this earlier the moment queue times get bad. |
| 18 | **I4** Hero loadouts | Depth for players who have put fifty hours in. Needs I3. |
| 19 | **F12** Modes, maps, custom games | Custom games first: every other mode is cheaper afterwards. |
| 20 | **I11** The two extra modes | Needs F12. Nearly free once custom games exist. |
| 21 | **I9** Training room | Independent of everything. Do it whenever somebody wants a self-contained job. |
| 22 | **F13** Seasons and live ops | Needs everything above to have something to season. |
| 23 | **F14** Controller, then **F15** Mobile | Independent of the whole column. Start any time somebody wants a break from services. |
| 24 | **F16** Accessibility | Independent, and overdue. Localisation is cut: English only. |
| 25 | **F17** Tournaments, LAN, replays | ⚠️ **The LAN half of this is not future work.** See below. |
| 26 | **F18** Distribution | Last. |

⚠️⚠️ **AND THE URGENT ITEM IS NOT ON THAT LIST AT ALL.** `FUTURE.md` § 17's first paragraph: nobody
has ever tested a full four-player match end to end with the internet physically disconnected, and
the nationals are in General Santos City. **Do that before any of the rows above**, and
keep it true through every one of them.

---

## 9 · The small borrowings, in one table

Things too small to earn a section and too useful to leave out. All of them are cheap.

| Borrowed from | The thing | What it becomes here |
|---|---|---|
| Fighting games | Input display in training | Show the contextual `E` hold tiers filling, so tap, shove and reset stop being folklore. |
| Racing games | Ghost of your best run | Race your own replay in the training room. Free once replays exist. |
| Rocket League | Quick chat on one key | A ping and comm wheel: "on the can", "behind you", "nice", "sorry". Replaces voice entirely. |
| Overwatch | Potential Play of the Game teaser | The end-of-round "you were one knockdown from second" line. § 4.3. |
| Halo | Post-match carnage report | The full four-player stat grid on the end screen, not just the score. |
| Valorant | Loadout shown before the round | § 5.5. The build is public. |
| Splatoon | Choose a side event | LIGA NG BARANGAY. § 2.10. |
| Mario Kart | Coin sound on a small win | An audio confirmation for every small good thing, not just the big ones. |
| Trackmania | Medals | § 2.14. A bar to beat that needs no other player. |
| Peggle and pinball | The over-the-top finish | The knockdown that wins a match deserves an absurd amount of screen. It is the moment the whole game exists for. |
| Chess and Elo history | A rating graph | One sparkline of the season on the profile. Trivial, and people stare at it. |
| Streaming culture | Spectator delay | § 2.6 and `FUTURE.md` § 17. Needed the first time a match is streamed. |
| Old LAN shooters | A scoreboard key you hold | Hold TAB mid-match for the full stat grid rather than the score strip. |

---

## 10 · The rejected register

⚠️⚠️ **EVERY IDEA 🧑 HAS KILLED, IN ONE PLACE, SO NOBODY RE-DERIVES ONE AND PRESENTS IT AS NEW.**
This is the first thing to check before proposing anything to do with progression, the dead round,
a new mode, or a new system of any kind. A rejected idea that comes back with a new name costs him
a conversation he has already had.

### 10.1 Cut on design

| Idea | Where | Why |
|---|---|---|
| **Passive defence banks into a stealable pot** | § 2.28 | A taya who defends 80 of 90 seconds and eats one throw walks away with nothing, and that lands on the role everybody is forced into once a match. |
| **Throw while moving versus planted** | never written in | It makes a new player feel bad at aiming, which is the wrong first impression for a party game. |
| **Diminishing XP after a long session** | `FUTURE.md` § 4 | Players notice and resent it. A penalty for playing your favourite game too much, arriving mid-session with no warning. |

### 10.2 Cut on scope, which is the more common and more important kind

| Idea | Where | What it removed |
|---|---|---|
| **The daily**, solo and three-shape versions both | § 2.9 | A mode, a leaderboard, a medal table, a seed system and a ghost pipeline. |
| **Rested XP** | `FUTURE.md` § 4 | A pool, a rate, a UI and a rule, solving a problem this game does not have. |
| **Soft currency and the shop** | `FUTURE.md` §§ 4, 5 | An economy, a shop screen, a price on every item forever, and duplicate protection. |
| **Nameplate, title, badge, emblem, frame, border and avatar as SEPARATE cosmetic slots** | `FUTURE.md` § 5 | Seven inventory categories, seven UI rows and seven wire fields collapsed into **the banner**, which does the same job as one object. |
| **King of the Can, time attack, survival, sudden death, 2v2** | `FUTURE.md` § 12, § 2.16 | Five modes. Nine modes would split thirty players nine ways, and a mode nobody can fill is worse than none. **2v2 is the one most worth revisiting**, because it changes the social shape of a session rather than its rules. |
| **Daily challenges and the rotating featured mode** | `FUTURE.md` § 13 | Two of six recurring live-ops commitments. ⚠️ This read "weeklies do the same job at a seventh of the maintenance" until 2026-08-31, when **weeklies were cut too**, on player-facing complexity rather than scope. See § 10.3. |
| **Score-margin multiplier and the demotion buffer** | `FUTURE.md` § 9 | Two of six ranked sub-systems. The multiplier is a permanent tuning surface for a nuance nobody feels; the buffer solves the same feeling rank floors already solve. |
| **Trust score and behaviour-sorted pools** | § 2.8 | Its whole purpose is sorting players into pools, and this population cannot fill the pools it already has. Reporting and the avoid list do the job. |
| **Four-peer unanimous corroboration** | `FUTURE.md` § 8.1 | Simplified rather than cut: **host plus one random witness**. Half the traffic and half the code for the same guarantee, because a lying host cannot know which peer will be asked. |
| **A named practice ladder against bots** | `FUTURE.md` § 11 | A fourth bot feature and a fifth progression track. Practice and `GuidedTraining` already exist. |
| **The 50-tier season track, and seasonal rewards generally** | `FUTURE.md` §§ 4, 4.1, 9, 13 | 50 rewards to author every ten weeks, forever, and the first missed season collapses the framing. His question is the one to keep asking: *"what can we even give as rewards"*. § 4.1 is the answer, sorting every possible reward by what it costs to make. Account level and hero mastery carry the progression instead, and they never reset. |
| **Endorsement decay** | § 2.4 | A timer, a rule and a paragraph of explanation, bought for a difference nobody would notice. Endorsements are a count on the banner and nothing more. |
| **Placement matches** | `FUTURE.md` § 9 | Five games in a hidden state with their own rules and UI, doing a job Glicko-2 already does alone. Everyone starts mid-ladder with a wide deviation and sees their tier from match one. |
| **Localisation, including Tagalog** | `FUTURE.md` § 16.3 | *"english only"*. The cost was never the translation, it was keeping three languages in step for every screen and every future addition, forever. ⚠️ The string extraction gets more expensive every month, so if it is ever revisited, do the extraction as its own job first. |
| **Privacy settings** | `FUTURE.md` § 1.3 | Three visibility levels over three kinds of data is nine states, on a game whose competitive half needs people to be able to look each other up. |
| **Data export** | `FUTURE.md` § 1.2 | Deferred until somebody asks. Account deletion stays. |
| **Replay takeover** | § 2.20 | Genuinely possible here and genuinely not worth it: enormous work, an audience of about four people. |
| **Souls-style ground messages** | § 9 | A moderation surface for near-zero gameplay value. |
| **Three of the training room's five drawn ranges** | § 2.21 | The shove cone, the confinement box and the lata's fall frame. A training room that draws everything is as unreadable as one that draws nothing. |

### 10.3 Cut on player-facing complexity, which is a different question from scope

⚠️⚠️ **THIS CATEGORY EXISTS BECAUSE 🧑 REFUSED THE SCOPE FRAMING ON 2026-08-31.** Asked which
phases to cut on what they cost the team, he answered: *"i have ai dont think abt 5 students
shit"*, and *"the cutting shit i want should be focused onn things that overcomplicate game for
ppl"*. **Do not propose a cut here on the grounds that it is a lot of work.** The question is what
the PLAYER has to hold in their head, and a thing that is cheap to build can still be cut for
making the game harder to understand.

| Idea | Where | What it removed, and why |
|---|---|---|
| **Weekly challenges** | `FUTURE.md` § 13 | The last recurring challenge cadence, after dailies had already gone. A challenge list is a to-do list: it turns opening the game into reading homework and tells a player that the way they want to play is worth less than the way the list wants. **There is now no challenge cadence at all, and that is deliberate.** |
| **Login streaks**, including the pausing kind | `FUTURE.md` § 13 | A streak's whole mechanism is making a missed day feel expensive, and *"I've broken it now"* is the thought immediately before somebody stops. A feature that punishes absence cannot also be the one that survives absence. |
| **Street Hype as a Classic-only progression track** | `FUTURE.md` § 10 | A second progression system whose only reason to exist is which mode you picked, so the same match fed a different bar off a lobby toggle and the profile grew a second level number nobody could explain. ⚠️ **Street Hype survives as an in-match feel in Classic.** `VISION.md` § 1's rule that Classic never gets powers is untouched and was never about Classic needing its own track. |
| **Mastery paths for all eighteen characters** | `FUTURE.md` §§ 4, 10 | Narrowed, not cut: **the six heroes only**. *"for mastery paths only give it to the heroes (6)"*. Eighteen paths is eighteen parallel grinds on one profile, and the twelve non-hero characters have no kit to learn, so a path behind them is a grind attached to nothing. They keep a played count. |
| **A separate ranked ladder per mode** | `FUTURE.md` § 9 | Two ratings leaves a player with two ranks and no answer to *"what rank are you"*, which is the only question a ladder exists to answer. **One ladder; the other mode is unranked.** ⚠️ Which mode carries it is NOT decided. |

⚠️ **Still open from that same conversation:** whether ranked keeps **three divisions inside each
of five tiers plus a numbered apex**, which is sixteen rungs of invented vocabulary before a player
knows if they are any good. 🧑 was asked and chose neither way. **Ask; do not assume.**

⚠️ **Kept on purpose, having been offered as cuts and declined:** endorsements, the Phase 10
loadout and skill variants (*"meh its good"*), and the Phase 6, 8 and 16 simplifications
(*"keep all lowkey"*). Do not re-propose these as fresh ideas.

### 10.4 The pattern, which is worth more than the list

**Most of these were cut on scope rather than on design, and several were the IMPROVED second
version of something.** The daily got better and was still cut. Rested XP was a better mechanism
than the curve it replaced and was still cut.

⚠️⚠️ **A plan can be full of individually good features and still be too much GAME.** This
paragraph read "too much for five students" until 2026-08-31, and 🧑 rejected that framing
directly: he has AI, and team size is not the argument he wants made. **The surviving argument is
better anyway**, because it is about the player rather than the team: when adding anything to these
documents the question is not *is this good*, it is **is this good enough to displace something
already on the list**, and **can a player still explain the game to a friend afterwards**. If the
answer to either is no, it belongs here.

