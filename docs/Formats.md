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
