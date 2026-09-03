# Formats: the two extra ways to play, and the rules that make them different

**`docs/Design.md` is the balance source of truth for CLASSIC and `Hero_Strike_Balance.md` for
HERO STRIKE. This file is the third thing: the two FORMATS, which are played inside either mode.**
Written 2026-09-01 with Phase 12.

⚠️ **`FUTURE.md` § 19.12's constraint is why this file exists at all**: *"write each mode's rules
and win condition into `docs/Design.md` or a sibling document in the same commit as the code"*.

---

## 0 · A format is not a mode, and the difference is load-bearing

⚠️⚠️ **`FUTURE.md` § 12 CALLS THESE "MODES" AND THEY ARE NOT `GameMode` VALUES.** The reasoning is
in `CustomGame.cs`'s header and is repeated here because this is the file somebody will read first:

- `docs/VISION.md` § 1: Classic and Hero Strike are **two modes, neither a variant of the other**,
  aimed at different people. That enum is a ruleset identity, not a label.
- `MatchRules.RoundCountFor` branches on it, `ProfileRules` keeps a whole separate career per
  value, and the career tab says so on screen: *"Classic and Hero Strike are separate games and
  their numbers never merge."*
- `MatchRecord.Mode` is a **stored string** that older builds read back.

**Both formats below are playable in either mode**, which is the tell. So `MatchFormat` rides
beside `GameMode`, a Classic Last Tsinelas match is still a Classic match in the career, and
neither format costs a wire break for the mode.

⚠️ **RANKED IS `Standard` ONLY.** `docs/TODO.md` § 105: one ladder, on Hero Strike, on the shipped
rules. A second win condition rated by one number is two games sharing a rating.

---

## 1 · LAST TSINELAS STANDING

**Three tsinelas each. Lose them all and you are out. The last attacker takes the round.**

| Rule | Value | Where |
|---|---|---|
| Tsinelas per attacker | **3** (custom: 1 to 5) | `CustomGameRules.StartingTsinelas` |
| A tsinelas is spent when | it is **LOST**, never when it is thrown | `CustomGameRules.TsinelasLeft` |
| An attacker with 0 left | is out for the rest of the round | `CustomGameRules.IsOut` |
| Round ends when | one attacker is left, or the clock runs out | `CustomGameRules.LastAttackerStanding` |
| The survivor is paid | **100**, a knockdown's worth | `CustomGameRules.LastStandingPoints` |
| Everything else | is the mode's own rules, unchanged | `docs/Design.md` |

⚠️⚠️ **SPENDING ON A LOSS RATHER THAN ON A THROW IS THE WHOLE FORMAT.** If throwing costs a life,
the optimal play is to never throw, and `docs/VISION.md`'s one paragraph is *"throwing is safe and
free; going back in for your tsinelas is the only moment you can be caught"*. Charging the failed
RETRIEVAL keeps every incentive pointing where the base game points it and makes the risk the
thing that is scored. **A tsinelas is lost when the round ends with it still on the floor, or when
the taya tags you while you are carrying it back.**

⚠️ **Zero survivors belongs to the taya.** A round-end sweep can take the last two attackers on
one tick. `LastAttackerStanding` answers `-1` for both "more than one alive" and "none alive"
rather than inventing a winner, and the caller decides; a rule that invents a result is a rule
nobody can check.

---

## 2 · MIRROR

**Everybody plays the same character and the same tsinelas. It changes every week.**

| Rule | Value | Where |
|---|---|---|
| Who everybody is | week index into the mode's roster | `CustomGameRules.MirrorIndex` |
| The week | counted from `RatingRules.SeasonOneStartUtc` | so the rotation and the season line up |
| How long left | whole days, on the lobby caption | `CustomGameRules.DaysUntilMirrorRotates` |

⚠️⚠️ **IT IS DERIVED AND NEVER STORED, WHICH IS WHAT MAKES IT ONE LINE.** Every machine computes
the same answer from the same UTC week with no service, no document and no wire field, so a LAN
lobby in a hall with no internet mirrors the same character as an online one.

⚠️ **The modulo is made positive by hand.** C#'s `%` keeps the sign of the left operand, so a
machine with a clock set before the epoch, which happens at venues, would index off the front of
the roster and throw.

⚠️ **The caption says how long the pick lasts.** A rotation that does not say it is a rotation
reads as a lock, and the player who dislikes this week's character has no way to know it is not
permanent.

---

## 3 · Custom games

**`CustomRules` is the whole rule set** and `CustomGameRules.Defaults(mode)` is exactly what the
game ships with, asked of `MatchRules.RoundCountFor` and `Balance.RoundTime` rather than copied
from them.

| Field | Bounds | Notes |
|---|---|---|
| Format | Standard / Last Tsinelas / Mirror | § 1 and § 2 |
| Rounds | 1 to 12 | default is the mode's own |
| Round seconds | 30 to 180 | default `Balance.RoundTime` |
| Score target | 0 to 5000 | **0 means play every round**, which is how the game ships |
| Tsinelas | 1 to 5 | Last Tsinelas only |
| Bots | 0 to 3 | `AIController` tiers, `Difficulty` |
| Private | bool | keeps the lobby out of the public list |
| Password | empty, or 4 to 16 characters | **host-only, never on the wire** |

⚠️⚠️ **THE PASSWORD IS NEVER SERIALISED.** A lobby advert is readable by everybody in the pool; a
password in it is a lock with the key taped to the door. The host holds it and compares what a
joiner sends.

⚠️⚠️ **EVERY BOUND IS A BOUND ON THE HOST, NOT A SUGGESTION TO IT.** A custom lobby is the one
place a player writes a number that every other machine then plays by, so each is clamped on the
way in and again on the way out of the wire. `docs/VISION.md` § 4: the host decides everything
that scores, which is only safe while the host cannot be handed a number that breaks the match.

⚠️ **The wire form appends and never inserts, and a short string reads as defaults.** That is
`docs/TODO.md` § 70.7's growing-roster rule applied to a record: an older build reading a newer
string gets a playable match rather than an exception. `CustomGameTests` truncates the string field
by field and asserts every prefix still parses into something playable.

---

## 4 · Map rotation and the map vote

**Which map the lobby plays next.** `MapRotationRules`, engine-free, `MapRotationTests` asserts it.

`FUTURE.md` § 12 and § 19.12 both order this **before a fourth map**: *"A map is the most expensive
content in the game. Map rotation and a map vote are nearly free and buy most of the same
freshness."*

⚠️⚠️ **THE ROTATION AND THE VOTE ARE ONE FEATURE AND NOT TWO.** A vote answers *"what do these four
people want"*; a rotation answers *"what happens when nobody says"*. `MapRotationRules.Decide` is
the whole thing: **the vote decides when there are votes and the rotation decides when there are
not.**

- **The vote alone** leaves a lobby where four abstentions replay the same map for ever, which is
  the exact staleness this was bought to remove. And a silent lobby is the COMMON case, not the
  edge case: four people who have just finished a match are looking at a scoreboard, not a ballot.
- **The rotation alone** takes the choice away from a room that has one.

| | Rule | Why |
|---|---|---|
| **Cycle** | `NextInRotation` is `current + 1`, never a random draw | Random repeats. With three maps a uniform draw replays the same one about a third of the time, and a player cannot tell a repeat from a bug. A cycle visits every map before revisiting any |
| **Opening map** | `OpeningMap` is derived from the UTC week | So a fresh install and a veteran open on the same map with no service, no stored state and no wire field. `CustomGameRules.MirrorIndex`'s argument exactly, counted from the same `RatingRules.SeasonOneStartUtc` so the two do not drift past each other |
| **Abstain** | `NoVote` is **-1**, never 0 | 0 is a real map index. A tally that conflates "no answer" with "the first option" hands every silent lobby to Eskinita **and looks exactly like a working vote** |
| ⚠️⚠️ **Tie-break** | The map you are **not** already on wins | The obvious rule is "lowest index", and it gives a 2-2 split to the CURRENT map half the time, which buys none of the freshness this exists for. **A majority may still keep the map it is on; it just cannot keep it by accident** |
| **Second tie-break** | Lowest index, and it is arbitrary | Anything cleverer needs state the lobby does not have, and an arbitrary rule every peer computes identically beats a fair one they can disagree about |
| **Unknown map** | Discarded, never clamped | A clamp turns a peer on a four-map build into a vote for map 2 on a three-map build: a silently wrong answer rather than an absent one |
| **Window** | `VoteSeconds` 20, or early once every occupied seat has answered | Phase 11's impatience argument one level down. It counts SEATS, not votes, so a room of two does not wait for two seats that can never answer |

⚠️⚠️ **EVERY FUNCTION IS PURE AND DETERMINISTIC, AND THAT IS A NETCODE REQUIREMENT RATHER THAN A
STYLE.** The host decides (`VISION.md` § 4) but **every peer draws the result**, and a peer that
computed a different winner from the same ballot would show the wrong map until the next sync
corrected it.

⚠️⚠️ **THE RUNTIME HALF IS ONE CALL AND IT ADDS NO WIRE MESSAGE.** A **rematch** advances the map:
that is the moment worth the most, four people who have just agreed to keep playing and were being
handed the identical street every time. It is host-only and rides the `SelectMap` broadcast that has
existed since the map picker shipped, so **`NetSession.ProtocolVersion` does not move** — which
matters, because moving it forces the Windows player and the .apk to be rebuilt and shipped together
(`CLAUDE.md` § 4a).

⚠️ **The ballot is not wired yet.** `Decide` takes votes when a lobby has a ballot to give it; until
then silence falls through to the cycle, which is the behaviour above. Collecting votes across the
wire is a new message and therefore a protocol move, and it should be spent in the same bump as
LAST TSINELAS's match half rather than on its own. `docs/TODO.md` § 130.12 and § 130.13.

