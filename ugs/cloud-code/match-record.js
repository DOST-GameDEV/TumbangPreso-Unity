const { DataApi } = require("@unity-services/cloud-save-1.4");

const PROFILE_KEY = "careerProfile";
const HISTORY_KEY = "matchHistory";

// ⚠️⚠️ THIS FILE IS `ProfileRules.cs` AND `MatchRecordRules.cs` WRITTEN A SECOND TIME, AND THE
// C# COPY IS THE SPECIFICATION. Cloud Code cannot import the C#, and the C# cannot run here, so
// the same trade `player-account.js` records about `DisplayNameMax` applies to a whole file
// instead of a constant. The C# has the tests (`Core.Tests/PlayerProfileTests.cs`,
// `Core.Tests/MatchRecordTests.cs`); when the two disagree, THIS is the bug, and the symptom a
// player sees is a career that changes the moment they come back online.
//
// ⚠️ IF A RULE CHANGES IN `ProfileRules`, IT CHANGES HERE IN THE SAME COMMIT. Every rule below
// names the C# member it mirrors so a reader can diff them by eye.
//
// ⚠️⚠️ THE HOST AUTHORS THE NUMBERS AND THAT IS A KNOWN HOLE, PER `FUTURE.md` § 2.3. This script
// closes the OTHER half of it: it writes only `context.playerId`, so a peer can submit a record
// full of lies about a match it played in and cannot touch anybody else's document. Phase 8 is
// what makes the numbers themselves trustworthy; do not read this validation as doing that job.

// ⚠️⚠️ PHASE 8 AND PHASE 9 LIVE IN THIS FILE BECAUSE THE ENDPOINT IS THE ONLY WRITER OF
// A RESULT. `FUTURE.md` § 0.5 rule 6 and § 19.9's constraint: ratings are computed from a match
// record by a Cloud Code endpoint, never sent by a client and never written by the host. The
// C# copies (`IntegrityRules`, `RatingRules`) are the specification and carry the tests; when
// the two disagree, THIS is the bug, exactly as the header above says for `ProfileRules`.

const VERDICT_KEY = "matchVerdicts";
const VERDICT_MEMORY = 60;

// ⚠️⚠️ MIRRORS `IntegrityRules`. Every constant below names the C# member it copies.
const WITNESSES_REQUIRED = 2;          // IntegrityRules.WitnessesRequired
const WRITE_FLOOR_SECONDS = 5;         // IntegrityRules.WriteFloorSeconds
const WRITES_PER_HOUR = 60;            // IntegrityRules.WritesPerHour
const ABANDON_MEMORY_DAYS = 7;         // IntegrityRules.AbandonMemoryDays
const COOLDOWN_SECONDS = [0, 120, 600, 1800, 3600];   // IntegrityRules.CooldownSeconds

// Mirrors `Balance`, for the score ceiling only. These are the four numbers
// `IntegrityRules.ScoreCeiling` reads and nothing else in this file uses them.
const ROUND_TIME = 90.0;
const SCORE_DEFENCE_PER_TICK = 10;
const DEFENCE_TICK_INTERVAL = 1.0;
const SCORE_LATA_KNOCKED = 100;
const SPRINT_SPEED = 4.6 * 1.5;

// ⚠️⚠️ MIRRORS `RatingRules`. `FUTURE.md` § 9 and `Core.Tests/RatingTests.cs`.
const START_RATING = 1500.0;
const START_DEVIATION = 350.0;
const START_VOLATILITY = 0.06;
const TAU = 0.5;
const GLICKO_SCALE = 173.7178;
const TIER_FLOORS = [0, 1250, 1400, 1600, 1800];
const TIER_NAMES = ["BATA", "KANTO", "BARANGAY", "KAMPEON", "ALAMAT"];
const SEASON_WEEKS = 10;
const SEASON_PULL_TO_MEAN = 0.4;
const SEASON_DEVIATION = 200.0;
const SEASON_ONE_START_MS = Date.UTC(2026, 8, 1, 0, 0, 0);   // 2026-09-01, month is 0-based

const PLAYER_COUNT = 4;
const HISTORY_LIMIT = 100;
const APPLIED_ID_MEMORY = 200;

// ⚠️ A SELF-IMPOSED BACKSTOP, NOT A VENDOR QUOTA, AND DELIBERATELY NOT QUOTED FROM A PRICING
// PAGE (`FUTURE.md` § 0.6: never quote a specific quota). `HISTORY_LIMIT` is the design rule;
// this is what stops a hundred unusually large records becoming one item nobody can read back.
// Records are dropped oldest-first until the stored value fits, and the totals already hold
// everything those records contributed.
const HISTORY_MAX_BYTES = 180000;

function clampInt(value, lo, hi) {
    const n = Math.trunc(Number(value) || 0);
    return n < lo ? lo : (n > hi ? hi : n);
}

function clampFloat(value, lo, hi) {
    const n = Number(value);
    if (!isFinite(n)) return lo;
    return n < lo ? lo : (n > hi ? hi : n);
}

// The same one-line clean `player-account.js` applies, for the same reason: a control
// character in a stored handle reaches a legacy `Text` component and draws as a box or
// breaks the row it is in.
function text(value, max) {
    return String(value || "")
        .replace(/[\u0000-\u001f\u007f]/g, "")
        .replace(/\s+/g, " ")
        .trim()
        .slice(0, max);
}

/** Mirrors `MatchRecordRules.Placements`: competition ranking, 1, 2, 2, 4. */
function placements(scores) {
    return scores.map(s => scores.filter(other => other > s).length + 1);
}

/** Mirrors `MatchRecordRules.Normalise`. */
function normaliseRecord(raw) {
    const r = raw || {};
    const rounds = clampInt(r.Rounds, 0, 64);
    const duration = clampFloat(r.DurationSeconds, 0, 24 * 3600);

    // ⚠️  `Ranked` IS CARRIED THROUGH AND IS IN THE DIGEST. `MatchRecord.Ranked` says why a
    // flag that decides whether a rating moves has to be one the other three peers hash too.
    const ranked = r.Ranked === true || r.Ranked === "true" || r.Ranked === 1;

    const players = Array.isArray(r.Players) ? r.Players.slice(0, PLAYER_COUNT) : [];
    const lines = players.map((raw, index) => {
        const p = raw || {};
        const retrievals = clampInt(p.Retrievals, 0, Number.MAX_SAFE_INTEGER);
        const shoveHits = clampInt(p.ShoveHits, 0, Number.MAX_SAFE_INTEGER);
        const lungeHits = clampInt(p.LungeHits, 0, Number.MAX_SAFE_INTEGER);
        const score = clampInt(p.Score, 0, Number.MAX_SAFE_INTEGER);

        // -1 is "never threw" and is not the same as 0. See `PlayerMatchStats.TimeToFirstThrow`.
        const rawFirst = Number(p.TimeToFirstThrow);
        const firstThrow = isFinite(rawFirst) && rawFirst >= 0 ? clampFloat(rawFirst, 0, duration) : -1;

        // Same sentinel, same reason: -1 is "nobody measured this round count", which every
        // record written before Phase 4 and every peer on an older build sends. Reading it as
        // zero would mark those matches AFK. `PlayerMatchStats.ActiveRounds` has the argument.
        const rawActive = Number(p.ActiveRounds);
        const activeRounds = isFinite(rawActive) && rawActive >= 0 ? clampInt(rawActive, 0, rounds) : -1;

        return {
            Slot: clampInt(p.Slot, 0, PLAYER_COUNT - 1),
            PlayerId: text(p.PlayerId, 128),
            Handle: text(p.Handle, 32),
            IsBot: !!p.IsBot,
            CharacterId: text(p.CharacterId, 32),
            SlipperId: text(p.SlipperId, 32),
            Score: score,
            Placement: 0,
            Throws: clampInt(p.Throws, 0, Number.MAX_SAFE_INTEGER),
            Knockdowns: clampInt(p.Knockdowns, 0, Number.MAX_SAFE_INTEGER),
            Retrievals: retrievals,
            RetrievalsUnderPressure: clampInt(p.RetrievalsUnderPressure, 0, retrievals),
            Tags: clampInt(p.Tags, 0, Number.MAX_SAFE_INTEGER),
            Sabotages: clampInt(p.Sabotages, 0, Number.MAX_SAFE_INTEGER),
            RoundsDefended: clampInt(p.RoundsDefended, 0, rounds),
            DefenceTicks: clampInt(p.DefenceTicks, 0, Number.MAX_SAFE_INTEGER),
            TayaCampPenalties: clampInt(p.TayaCampPenalties, 0, Number.MAX_SAFE_INTEGER),
            UnretrievedSlipperPenalties: clampInt(p.UnretrievedSlipperPenalties, 0, Number.MAX_SAFE_INTEGER),
            TimeToFirstThrow: firstThrow,
            LongestLastAttacker: clampFloat(p.LongestLastAttacker, 0, duration),
            ShoveHits: shoveHits,
            ShoveAttempts: clampInt(p.ShoveAttempts, shoveHits, Number.MAX_SAFE_INTEGER),
            LungeHits: lungeHits,
            LungeAttempts: clampInt(p.LungeAttempts, lungeHits, Number.MAX_SAFE_INTEGER),
            DistanceTravelled: clampFloat(p.DistanceTravelled, 0, 1000000),
            ScoreAtFinalRound: clampInt(p.ScoreAtFinalRound, 0, score),
            ActiveRounds: activeRounds,
        };
    });

    const places = placements(lines.map(l => l.Score));
    lines.forEach((l, i) => { l.Placement = places[i]; });

    const defenders = Array.isArray(r.DefenderByRound)
        ? r.DefenderByRound.slice(0, rounds).map(v => clampInt(v, 0, PLAYER_COUNT - 1))
        : [];

    return {
        MatchId: text(r.MatchId, 64),
        Mode: text(r.Mode, 24) || "Classic",
        MapId: text(r.MapId, 32),
        Rounds: rounds,
        DurationSeconds: duration,
        PlayedUtc: text(r.PlayedUtc, 40),
        WinningSlot: clampInt(r.WinningSlot, -1, PLAYER_COUNT - 1),
        Ranked: ranked,
        Online: !!r.Online,
        DefenderByRound: defenders,
        Players: lines,
    };
}

// ---------------------------------------------------------------------------
// PROGRESSION. Mirrors `ProgressionRules.cs`; see this file header for the rule that the C#
// is the specification and a disagreement here is the bug.
//
// ⚠️⚠️ XP IS COMPUTED HERE AND NEVER SENT BY A CLIENT, which is `FUTURE.md` 0.5 rule 6.
// The client runs the same arithmetic so the end-of-match bar can animate before this endpoint
// answers, and `ProfileRules.LastAward` says in its own comment that the client copy is never the
// authority. If a player ever sees a bar that disagrees with their profile, THIS is the number
// that is right.
//
// ⚠️ THE HERO IDS ARE WRITTEN TWICE AND THERE IS NO WAY AROUND IT. `Roster.HeroPeople` is the
// C# original and Cloud Code cannot import it, the same trade `player-account.js` records about
// `DisplayNameMax`. `ProgressionTests.TheServerScriptsCopyOfTheHeroListMatchesTheRoster` pins
// them, so adding a hero without touching this line fails a test rather than silently shipping a
// hero with no mastery path.
// ---------------------------------------------------------------------------

const MASTERY_HEROES = ["dante", "cheska", "sean", "zack", "nemu", "phaister"];

const COMPLETION_XP = 100;
const PLACEMENT_XP = [40, 25, 15, 10];
const OBJECTIVE_KNOCKDOWN_XP = 15;
const OBJECTIVE_PRESSURE_RETRIEVAL_XP = 20;
const OBJECTIVE_TAG_XP = 15;
const OBJECTIVE_SABOTAGE_XP = 10;
const OBJECTIVE_CLEAN_XP = 15;

const XP_PER_LEVEL = 1000;
const MASTERY_XP_PER_LEVEL = 2000;

const AFK_STRIKES_BEFORE_PENALTY = 3;
const AFK_PENALTY_MATCHES = 3;

/** Mirrors `ProgressionRules.WasAfk`. -1 active rounds is unmeasured, never AFK. */
function wasAfk(record, line) {
    if (!line || line.IsBot) return false;
    if (record.Rounds <= 0) return false;
    if (line.ActiveRounds < 0) return false;
    return line.ActiveRounds < record.Rounds;
}

/** Mirrors `ProgressionRules.MatchXp`, which is the sum of `Breakdown`. */
function matchXp(record, line) {
    if (!line || line.IsBot) return 0;
    if (wasAfk(record, line)) return 0;

    let total = COMPLETION_XP;
    if (line.Placement >= 1 && line.Placement <= PLACEMENT_XP.length)
        total += PLACEMENT_XP[line.Placement - 1];

    if (line.Knockdowns > 0) total += OBJECTIVE_KNOCKDOWN_XP;
    if (line.RetrievalsUnderPressure > 0) total += OBJECTIVE_PRESSURE_RETRIEVAL_XP;
    if (line.Tags > 0) total += OBJECTIVE_TAG_XP;
    if (line.Sabotages > 0) total += OBJECTIVE_SABOTAGE_XP;
    if (line.TayaCampPenalties === 0 && line.UnretrievedSlipperPenalties === 0)
        total += OBJECTIVE_CLEAN_XP;

    return total;
}

/** Mirrors `ProgressionRules.LevelForXp`. Flat cost per level, uncapped, never below 1. */
function levelForXp(xp) {
    return xp <= 0 ? 1 : 1 + Math.floor(xp / XP_PER_LEVEL);
}

/** Mirrors `ProgressionRules.MasteryLevelForXp`. */
function masteryLevelForXp(xp) {
    return xp <= 0 ? 1 : 1 + Math.floor(xp / MASTERY_XP_PER_LEVEL);
}

/** Mirrors `ProgressionRules.MasteryFor`. */
function masteryFor(profile, id) {
    let found = profile.Mastery.find(m => m && m.Id === id);
    if (!found) {
        found = { Id: id, Xp: 0, Level: 1 };
        profile.Mastery.push(found);
    }
    return found;
}

/**
 * Mirrors `ProgressionRules.Award`, and is called from the same place: inside `applyRecord`,
 * after every refusal has already happened.
 *
 * ⚠️⚠️ THAT PLACEMENT IS THE ONLY THING MAKING IT IDEMPOTENT. The offline queue resubmits,
 * `applyRecord` refuses a `MatchId` it has already counted, and paying XP from a second call site
 * would double a career the first time somebody Wi-Fi dropped at the wrong moment.
 */
function award(profile, record, line) {
    if (profile.Level < 1) profile.Level = 1;

    if (wasAfk(record, line)) {
        profile.AfkStrikes += 1;
        if (profile.AfkStrikes >= AFK_STRIKES_BEFORE_PENALTY) {
            profile.AfkStrikes = 0;
            profile.XpPenaltyMatches = AFK_PENALTY_MATCHES;
        }
        return;
    }

    // A clean match clears the strikes rather than decrementing them: three in a row is somebody
    // who walked away, three across a month is somebody whose connection dropped.
    profile.AfkStrikes = 0;

    // The suspension is spent by a match that would otherwise have paid, never by another AFK
    // one, or the fastest way out of it would be to keep standing still.
    if (profile.XpPenaltyMatches > 0) {
        profile.XpPenaltyMatches -= 1;
        return;
    }

    const paid = matchXp(record, line);
    profile.Xp += paid;
    profile.Level = levelForXp(profile.Xp);

    if (line.CharacterId && MASTERY_HEROES.indexOf(line.CharacterId) >= 0) {
        const mastery = masteryFor(profile, line.CharacterId);
        mastery.Xp += paid;
        mastery.Level = masteryLevelForXp(mastery.Xp);
    }
}

function emptyTotals() {
    return {
        Matches: 0, Wins: 0, Draws: 0,
        Placements: new Array(PLAYER_COUNT).fill(0),
        SecondsPlayed: 0,
        Throws: 0, Knockdowns: 0, Retrievals: 0, RetrievalsUnderPressure: 0,
        Tags: 0, Sabotages: 0, RoundsDefended: 0, DefenceTicks: 0,
        TayaCampPenalties: 0, UnretrievedSlipperPenalties: 0,
        ShoveAttempts: 0, ShoveHits: 0, LungeAttempts: 0, LungeHits: 0,
        DistanceTravelled: 0,
        FirstThrowSecondsTotal: 0, MatchesWithAThrow: 0,
        LongestLastAttacker: 0, Clutches: 0, ComebackChances: 0,
        CurrentWinStreak: 0, LongestWinStreak: 0,
        TotalScore: 0, BestScore: 0,
    };
}

function emptyProfile(playerId) {
    return {
        PlayerId: playerId,
        Level: 1,
        Xp: 0,
        Mastery: [],
        AfkStrikes: 0,
        XpPenaltyMatches: 0,
        RankTier: "",
        RankPoints: 0,
        PeakRankTier: "",
        Inventory: [],
        CreatedUtc: "",
        UpdatedUtc: "",
        Modes: [],
        Characters: [],
        Slippers: [],
        AppliedMatchIds: [],
    };
}

/** Mirrors `ProfileRules.ModeFor`. */
function modeFor(profile, mode) {
    const name = mode || "Classic";
    let found = profile.Modes.find(m => m && m.Mode === name);
    if (!found) {
        found = { Mode: name, Totals: emptyTotals() };
        profile.Modes.push(found);
    }
    if (!found.Totals) found.Totals = emptyTotals();
    if (!Array.isArray(found.Totals.Placements)) found.Totals.Placements = new Array(PLAYER_COUNT).fill(0);
    return found;
}

/** Mirrors `ProfileRules.Favourite`'s list, and `PickFor`. */
function pickFor(list, id) {
    let found = list.find(p => p && p.Id === id);
    if (!found) {
        found = { Id: id, Games: 0, Wins: 0, Score: 0 };
        list.push(found);
    }
    return found;
}

/** Mirrors `ProfileRules.WasLastEnteringTheFinalRound`. Tied-last still counts. */
function wasLastEnteringTheFinalRound(record, line) {
    if (!record.Players || record.Players.length < 2) return false;
    const entering = record.Players.map(p => (p && p.ScoreAtFinalRound) || 0);
    const places = placements(entering);
    const self = record.Players.indexOf(line);
    if (self < 0) return false;
    const worst = Math.max.apply(null, places);
    return places[self] === worst;
}

/**
 * Mirrors `ProfileRules.Apply`. All-or-nothing: everything that can refuse the record is
 * checked before the first field is written, so a replayed queue entry can never leave a career
 * half-counted.
 */
function applyRecord(profile, record, playerId) {
    if (!record.MatchId) return false;
    if (profile.AppliedMatchIds.indexOf(record.MatchId) >= 0) return false;

    const line = record.Players.find(p => p && !p.IsBot && p.PlayerId === playerId);
    if (!line) return false;

    if (!profile.PlayerId) profile.PlayerId = playerId;

    const totals = modeFor(profile, record.Mode).Totals;
    const won = record.WinningSlot === line.Slot;
    const drew = record.WinningSlot < 0 && line.Placement === 1;

    totals.Matches += 1;
    if (won) totals.Wins += 1;
    if (drew) totals.Draws += 1;
    if (line.Placement >= 1 && line.Placement <= PLAYER_COUNT) totals.Placements[line.Placement - 1] += 1;

    totals.SecondsPlayed += record.DurationSeconds;
    totals.Throws += line.Throws;
    totals.Knockdowns += line.Knockdowns;
    totals.Retrievals += line.Retrievals;
    totals.RetrievalsUnderPressure += line.RetrievalsUnderPressure;
    totals.Tags += line.Tags;
    totals.Sabotages += line.Sabotages;
    totals.RoundsDefended += line.RoundsDefended;
    totals.DefenceTicks += line.DefenceTicks;
    totals.TayaCampPenalties += line.TayaCampPenalties;
    totals.UnretrievedSlipperPenalties += line.UnretrievedSlipperPenalties;
    totals.ShoveAttempts += line.ShoveAttempts;
    totals.ShoveHits += line.ShoveHits;
    totals.LungeAttempts += line.LungeAttempts;
    totals.LungeHits += line.LungeHits;
    totals.DistanceTravelled += line.DistanceTravelled;
    totals.TotalScore += line.Score;
    if (line.Score > totals.BestScore) totals.BestScore = line.Score;

    // A never-threw match is left out of the average rather than counted as zero.
    if (line.TimeToFirstThrow >= 0) {
        totals.FirstThrowSecondsTotal += line.TimeToFirstThrow;
        totals.MatchesWithAThrow += 1;
    }

    if (line.LongestLastAttacker > totals.LongestLastAttacker)
        totals.LongestLastAttacker = line.LongestLastAttacker;

    // The denominator is counted whether or not the comeback landed.
    if (wasLastEnteringTheFinalRound(record, line)) {
        totals.ComebackChances += 1;
        if (won) totals.Clutches += 1;
    }

    // A draw breaks a streak rather than extending it.
    if (won) {
        totals.CurrentWinStreak += 1;
        if (totals.CurrentWinStreak > totals.LongestWinStreak)
            totals.LongestWinStreak = totals.CurrentWinStreak;
    } else {
        totals.CurrentWinStreak = 0;
    }

    if (line.CharacterId) {
        const pick = pickFor(profile.Characters, line.CharacterId);
        pick.Games += 1;
        pick.Score += line.Score;
        if (won) pick.Wins += 1;
    }

    if (line.SlipperId) {
        const pick = pickFor(profile.Slippers, line.SlipperId);
        pick.Games += 1;
        pick.Score += line.Score;
        if (won) pick.Wins += 1;
    }

    profile.AppliedMatchIds.push(record.MatchId);
    while (profile.AppliedMatchIds.length > APPLIED_ID_MEMORY) profile.AppliedMatchIds.shift();

    profile.UpdatedUtc = record.PlayedUtc;
    if (!profile.CreatedUtc) profile.CreatedUtc = record.PlayedUtc;

    // Inside the guard, per `award`'s own note and `ProfileRules.Apply`'s.
    award(profile, record, line);
    return true;
}

/** Mirrors `ProfileRules.Remember`, plus the byte backstop the client does not need. */
function remember(history, record) {
    if (history.some(h => h && h.MatchId === record.MatchId)) return history;

    history.unshift(record);
    while (history.length > HISTORY_LIMIT) history.pop();
    while (history.length > 1 && JSON.stringify(history).length > HISTORY_MAX_BYTES) history.pop();
    return history;
}

async function readJson(api, projectId, playerId, key, fallback) {
    const response = await api.getProtectedItems(projectId, playerId, [key]);
    const item = response.data.results.find(x => x.key === key);
    if (!item || !item.value) return fallback;
    try {
        const parsed = JSON.parse(String(item.value));
        return parsed || fallback;
    } catch (e) {
        // ⚠️ A CORRUPT VALUE IS REPLACED, NOT THROWN ON. A career that cannot be parsed would
        // otherwise refuse every future submission for that player permanently, and the records
        // that produced it are gone either way.
        return fallback;
    }
}

// =====================================================================================
// PHASE 8: THE DIGEST, THE SANITY CHECK AND THE VERDICT
// =====================================================================================

/** Mirrors `IntegrityRules.Canonical`. The field order IS the contract. */
function canonical(record) {
    if (!record) return "";

    let out = "";
    out += String(record.MatchId || "") + "|";
    out += String(record.Mode || "") + "|";
    out += String(record.MapId || "") + "|";
    out += String(Math.trunc(record.Rounds || 0)) + "|";
    out += String(Math.trunc(typeof record.WinningSlot === "number" ? record.WinningSlot : -1)) + "|";
    out += (record.Ranked ? "r" : "c") + "|";

    const players = Array.isArray(record.Players) ? record.Players : [];
    for (let i = 0; i < players.length; i++) {
        const p = players[i];
        if (!p) { out += "-|"; continue; }

        out += String(Math.trunc(p.Slot || 0)) + ",";
        out += (p.IsBot ? "b" : "h") + ",";
        out += (p.IsBot ? "" : String(p.PlayerId || "")) + ",";
        out += String(p.CharacterId || "") + ",";
        out += String(Math.trunc(p.Score || 0)) + ",";
        out += String(Math.trunc(p.Placement || 0)) + "|";
    }

    return out;
}

/**
 * Mirrors `IntegrityRules.Digest`: FNV-1a, 64 bit, over UTF-16 code units low byte first.
 *
 * ⚠️⚠️  `BigInt` IS NOT OPTIONAL HERE. JavaScript numbers lose precision above 2^53, so a
 * 64-bit FNV written with `*` and `^` on Numbers produces a different value from the C# every
 * time and every match in the game would read as disputed. The mask keeps it to 64 bits, which
 * is what `ulong` overflow does for free on the other side.
 */
function digest(record) {
    const MASK = (1n << 64n) - 1n;
    const PRIME = 1099511628211n;
    let hash = 14695981039346656037n;

    const text = canonical(record);
    for (let i = 0; i < text.length; i++) {
        const c = text.charCodeAt(i);
        hash = (hash ^ BigInt(c & 0xff)) & MASK;
        hash = (hash * PRIME) & MASK;
        hash = (hash ^ BigInt((c >> 8) & 0xff)) & MASK;
        hash = (hash * PRIME) & MASK;
    }

    return hash.toString(16).padStart(16, "0");
}

/** Mirrors `IntegrityRules.ScoreCeiling`. A ceiling nobody can reach, not a balance estimate. */
function scoreCeiling(rounds, durationSeconds) {
    let seconds = durationSeconds > 0 ? durationSeconds : rounds * ROUND_TIME;
    if (seconds <= 0) seconds = ROUND_TIME;

    const passive = Math.trunc(seconds * (SCORE_DEFENCE_PER_TICK / DEFENCE_TICK_INTERVAL));
    const events = Math.trunc(seconds / 2.0) * SCORE_LATA_KNOCKED;
    return passive + events + 1000;
}

/** Mirrors `MatchRecordRules.PassiveDefenceSeconds`: ticks times the interval. */
function passiveDefenceSeconds(line) {
    return (Math.trunc(line.DefenceTicks || 0)) * DEFENCE_TICK_INTERVAL;
}

/**
 * Mirrors `IntegrityRules.Check`. Returns "" for a record that is possible.
 *
 * ⚠️  IT REFUSES ONLY THE IMPOSSIBLE. Every check is a statement about arithmetic rather
 * than about play, because refusing a real result is worse than accepting a modest lie: the
 * modest lie is caught by the digest and the refusal is a player being told their best game
 * never happened.
 */
function sanityFault(record) {
    if (!record || !record.MatchId) return "NoMatchId";

    const players = Array.isArray(record.Players) ? record.Players : [];
    if (players.length === 0) return "NoPlayers";
    if (players.length > PLAYER_COUNT) return "TooManyPlayers";

    const rounds = Math.trunc(record.Rounds || 0);
    if (rounds < 0 || rounds > 64) return "ImpossibleRounds";

    const duration = Number(record.DurationSeconds) || 0;
    const longest = (rounds + 1) * (ROUND_TIME + 120.0);
    if (duration < 0 || duration > longest) return "ImpossibleDuration";

    const ceiling = scoreCeiling(rounds, duration);
    const defenceCeiling = duration + ROUND_TIME;

    for (const p of players) {
        if (!p) continue;
        if (p.Score < 0 || p.Score > ceiling) return "ImpossibleScore";
        if (p.Knockdowns > p.Throws) return "MoreKnockdownsThanThrows";
        if (p.Retrievals > p.Throws) return "MoreRetrievalsThanThrows";
        if (p.ShoveHits > p.ShoveAttempts) return "MoreHitsThanAttempts";
        if (p.LungeHits > p.LungeAttempts) return "MoreHitsThanAttempts";
        if (passiveDefenceSeconds(p) > defenceCeiling) return "DefenceLongerThanTheMatch";
        if ((Number(p.DistanceTravelled) || 0) > SPRINT_SPEED * defenceCeiling * 2.0)
            return "ImpossibleTravel";
    }

    const places = placements(players.map(p => (p && p.Score) || 0));
    for (let i = 0; i < players.length; i++) {
        if (!players[i]) continue;
        if (players[i].Placement !== places[i]) return "PlacementsDisagreeWithScores";
    }

    return "";
}

/**
 * Which player's document holds the shared verdict for a match.
 *
 * ⚠️⚠️  THE ARBITER IS THE LEXICOGRAPHICALLY SMALLEST HUMAN PLAYER ID IN THE RECORD, AND IT
 * IS DERIVED RATHER THAN NOMINATED FOR A REASON THAT MATTERS. Cloud Save is keyed by player id
 * and has no game-scoped document this endpoint can rely on, so corroboration needs one agreed
 * place to write. A HOST-nominated arbiter would be an arbiter nominated by the suspect. A
 * derived one is computed identically by all four peers from the record they each hold.
 *
 * ⚠️  A HOST THAT FORGES PLAYER IDS TO MOVE THE ARBITER FAILS SAFE. Its submission lands in
 * a document nobody else writes to, so it stays PENDING for ever and never pays a rating.
 */
function arbiterFor(record) {
    const ids = (record.Players || [])
        .filter(p => p && !p.IsBot && p.PlayerId)
        .map(p => String(p.PlayerId))
        .sort();

    return ids.length > 0 ? ids[0] : "";
}

function serviceStore(context) {
    try {
        return new DataApi({ accessToken: context.serviceToken });
    } catch (e) {
        throw new Error("result service unavailable");
    }
}

/**
 * Read, update and write the shared verdict row for one match.
 *
 * ⚠️  THE ROW IS CAPPED AT `VERDICT_MEMORY` AND ROLLS. It is a corroboration window rather
 * than a ledger, the same argument `ProfileRules.AppliedIdMemory` makes one file over.
 */
async function recordVerdict(store, projectId, arbiter, matchId, recordDigest, witness, voter) {
    const rows = await readJson(store, projectId, arbiter, VERDICT_KEY, []);
    const list = Array.isArray(rows) ? rows : [];

    let row = list.find(r => r && r.Id === matchId);
    if (!row) {
        row = { Id: matchId, Digest: recordDigest, Agree: [], Disputed: false };
        list.unshift(row);
        while (list.length > VERDICT_MEMORY) list.pop();
    }

    // ⚠️⚠️  TWO DIFFERENT RECORDS UNDER ONE MATCH ID IS ITSELF A DISPUTE. It is the exact
    // shape of "the host told us one thing and submitted another", seen from the outside.
    if (row.Digest !== recordDigest) row.Disputed = true;

    if (witness) {
        if (witness === recordDigest) {
            if (!row.Agree.includes(voter)) row.Agree.push(voter);
        } else {
            // ⚠️⚠️  A DISAGREEMENT BEATS A MAJORITY AND IS NEVER CLEARED. Three agreeing and
            // one dissenting is disputed, not witnessed: a vote would let three colluding players
            // ratify anything, and the point of the scheme is that being honest is cheap and
            // getting everybody to lie is not. `IntegrityRules.Corroborate`.
            row.Disputed = true;
        }
    }

    await store.setProtectedItem(projectId, arbiter, {
        key: VERDICT_KEY, value: JSON.stringify(list),
    });

    if (row.Disputed) return { state: "disputed", row: row };
    if (row.Agree.length >= WITNESSES_REQUIRED) return { state: "witnessed", row: row };
    return { state: "pending", row: row };
}

async function readVerdict(store, projectId, arbiter, matchId) {
    const rows = await readJson(store, projectId, arbiter, VERDICT_KEY, []);
    const list = Array.isArray(rows) ? rows : [];
    const row = list.find(r => r && r.Id === matchId);

    if (!row) return "pending";
    if (row.Disputed) return "disputed";
    return row.Agree.length >= WITNESSES_REQUIRED ? "witnessed" : "pending";
}

// =====================================================================================
// PHASE 9: GLICKO-2, ADAPTED FOR A FOUR-PLAYER FREE FOR ALL
// =====================================================================================

function emptyRank() {
    return {
        Rating: START_RATING,
        Deviation: START_DEVIATION,
        Volatility: START_VOLATILITY,
        MatchesThisSeason: 0,
        Season: 1,
        FloorTier: 0,
        PeakTier: 0,
    };
}

function tierFor(rating) {
    for (let i = TIER_FLOORS.length - 1; i >= 0; i--)
        if (rating >= TIER_FLOORS[i]) return i;
    return 0;
}

function floorRating(floorTier) {
    if (floorTier <= 0) return 0;
    const i = floorTier >= TIER_FLOORS.length ? TIER_FLOORS.length - 1 : floorTier;
    return TIER_FLOORS[i];
}

function seasonAt(ms) {
    if (ms <= SEASON_ONE_START_MS) return 1;
    const weeks = (ms - SEASON_ONE_START_MS) / (7 * 24 * 3600 * 1000);
    return 1 + Math.trunc(weeks / SEASON_WEEKS);
}

/** Mirrors `RatingRules.BeginSeason`: a pull toward the mean, never a wipe, peak survives. */
function beginSeason(rank, season) {
    if (rank.Season === season) return rank;

    rank.Rating = rank.Rating + (START_RATING - rank.Rating) * SEASON_PULL_TO_MEAN;
    rank.Deviation = Math.max(rank.Deviation, SEASON_DEVIATION);
    rank.Volatility = START_VOLATILITY;
    rank.MatchesThisSeason = 0;
    rank.Season = season;
    rank.FloorTier = 0;

    const reached = tierFor(rank.Rating);
    if (reached > rank.PeakTier) rank.PeakTier = reached;
    return rank;
}

/** Mirrors `RatingRules.ApplyFloors`: raise the floor, then enforce it. That order is the promise. */
function applyFloors(rank) {
    const reached = tierFor(rank.Rating);
    if (reached > rank.FloorTier) rank.FloorTier = reached;
    if (rank.FloorTier > rank.PeakTier) rank.PeakTier = rank.FloorTier;

    const floor = floorRating(rank.FloorTier);
    if (rank.Rating < floor) rank.Rating = floor;
    return rank;
}

function glickoG(phi) {
    return 1.0 / Math.sqrt(1.0 + (3.0 * phi * phi) / (Math.PI * Math.PI));
}

function glickoE(mu, muJ, phiJ) {
    return 1.0 / (1.0 + Math.exp(-glickoG(phiJ) * (mu - muJ)));
}

/** Mirrors `RatingRules.Update`. One rating period, one player, every opponent met in it. */
function ratingUpdate(before, opponents, scores) {
    const after = Object.assign({}, before);
    if (!opponents || opponents.length === 0) return after;

    const mu = (before.Rating - START_RATING) / GLICKO_SCALE;
    const phi = before.Deviation / GLICKO_SCALE;
    const sigma = before.Volatility;

    let vInv = 0.0;
    let delta = 0.0;

    for (let i = 0; i < opponents.length && i < scores.length; i++) {
        const muJ = (opponents[i].Rating - START_RATING) / GLICKO_SCALE;
        const phiJ = opponents[i].Deviation / GLICKO_SCALE;
        const g = glickoG(phiJ);
        const e = glickoE(mu, muJ, phiJ);

        vInv += g * g * e * (1.0 - e);
        delta += g * (scores[i] - e);
    }

    if (vInv <= 0.0) return after;

    const v = 1.0 / vInv;
    const deltaHat = v * delta;

    const a = Math.log(sigma * sigma);
    const phiSq = phi * phi;
    const deltaSq = deltaHat * deltaHat;

    const f = x => {
        const ex = Math.exp(x);
        const num = ex * (deltaSq - phiSq - v - ex);
        const den = 2.0 * (phiSq + v + ex) * (phiSq + v + ex);
        return (num / den) - ((x - a) / (TAU * TAU));
    };

    let A = a;
    let B;

    if (deltaSq > phiSq + v) {
        B = Math.log(deltaSq - phiSq - v);
    } else {
        let k = 1;
        while (f(a - k * TAU) < 0.0 && k < 100) k++;
        B = a - k * TAU;
    }

    let fA = f(A);
    let fB = f(B);
    let guard = 0;

    while (Math.abs(B - A) > 0.000001 && guard++ < 200) {
        const C = A + ((A - B) * fA) / (fB - fA);
        const fC = f(C);

        if (fC * fB <= 0.0) { A = B; fA = fB; } else { fA /= 2.0; }
        B = C;
        fB = fC;
    }

    const sigmaPrime = Math.exp(A / 2.0);
    const phiStar = Math.sqrt(phiSq + sigmaPrime * sigmaPrime);
    const phiPrime = 1.0 / Math.sqrt(1.0 / (phiStar * phiStar) + 1.0 / v);
    const muPrime = mu + phiPrime * phiPrime * delta;

    after.Rating = muPrime * GLICKO_SCALE + START_RATING;
    after.Deviation = phiPrime * GLICKO_SCALE;
    after.Volatility = sigmaPrime;

    if (after.Deviation < 30.0) after.Deviation = 30.0;
    if (after.Deviation > START_DEVIATION) after.Deviation = START_DEVIATION;

    after.MatchesThisSeason = (before.MatchesThisSeason || 0) + 1;
    return after;
}

/**
 * The caller's new rating for one match.
 *
 * ⚠️⚠️  THE OPPONENTS ARE ASSUMED TO BE AT THE START RATING, AND THAT IS THE ONE PLACE THIS
 * FILE KNOWINGLY APPROXIMATES ITS C# SPECIFICATION. `RatingRules.UpdateAll` takes all four real
 * states because Glicko-2 is a batch system; this endpoint is called once per player, by that
 * player, and reading three other players' rank documents on every submission would be three
 * extra Cloud Save reads per player per match, which is twelve reads a match on a free tier
 * (`FUTURE.md` § 0.5 rule 8). The cost of the approximation is that beating a stronger player
 * pays the same as beating an average one; the ORDER of a ladder is unaffected, because every
 * player is measured against the same reference. `docs/TODO.md` § 105 records it as a real
 * limitation with the exact fix for the day there is a budget: one read of a shared per-match
 * rank snapshot written by the first submitter.
 */
/**
 * Mirrors `BotFillRules.Weight`: every human seat past the first is a quarter of the result.
 * Four humans is 1.0, three is 0.667, two is 0.333, one is 0.0.
 *
 * ⚠⚠  THE C# AND THIS MUST AGREE, and `RatingRules.BotWeight` is the other copy.
 * `IntegrityRules.Digest` is written twice for the same reason and `tools/check_digest_contract.js`
 * is the gate on that pair; this pair is asserted by `Phase11Tests` against the same table.
 */
function botWeight(humans, seats) {
    if (!seats || seats <= 1) return 0.0;

    const capped = Math.max(0, Math.min(seats, humans || 0));
    const w = (capped - 1) / (seats - 1);

    return Math.max(0.0, Math.min(1.0, w));
}

/**
 * Scales a whole Glicko-2 outcome toward "this did not happen".
 *
 * ⚠⚠  IT LERPS THE DEVIATION AND THE VOLATILITY TOO, NOT JUST THE RATING, and taking only
 * the rating would have been the subtle version of the same exploit. Deviation is CONFIDENCE:
 * it shrinks with every match played, and a shrinking deviation is what makes later results
 * move a rating less. Farming bots at a third of the rating gain while collecting a full match's
 * worth of confidence would let somebody lock in a soft rating and then defend it.
 *
 * ⚠⚠  AND A ZERO-WEIGHT MATCH DOES NOT COUNT AS A SEASON MATCH EITHER. `MatchesThisSeason`
 * is what the profile screen calls "Season Matches" and what a placement count would read; a
 * match that moved nothing must not appear to have been played on the ladder. Mirrors
 * `BotFillRules.RatingCounts`.
 */
function blendRank(before, after, weight) {
    if (weight >= 1.0) return after;
    if (weight <= 0.0) return Object.assign({}, before);

    const blended = Object.assign({}, after);

    blended.Rating = before.Rating + (after.Rating - before.Rating) * weight;
    blended.Deviation = before.Deviation + (after.Deviation - before.Deviation) * weight;
    blended.Volatility = before.Volatility + (after.Volatility - before.Volatility) * weight;

    return applyFloors(blended);
}

function rankedResult(rank, myPlacement, allPlacements) {
    const opponents = [];
    const scores = [];

    for (let i = 0; i < allPlacements.length; i++) {
        if (allPlacements[i] === null) continue;
        if (i === myPlacement.index) continue;

        opponents.push({ Rating: START_RATING, Deviation: START_DEVIATION });

        if (myPlacement.place === allPlacements[i]) scores.push(0.5);
        else scores.push(myPlacement.place < allPlacements[i] ? 1.0 : 0.0);
    }

    return applyFloors(ratingUpdate(rank, opponents, scores));
}

// =====================================================================================
// RATE LIMITS. `FUTURE.md` § 19.8 step 5: a free tier is a budget an abusive client can spend.
// =====================================================================================

/**
 * ⚠️  IT IS INVISIBLE TO ANYBODY ACTUALLY PLAYING. A real match is minutes long, so a
 * five-second floor between career writes never touches an honest player and caps a client stuck
 * in a retry loop at twelve writes a minute instead of as many as it can issue.
 */
function refuseIfTooFast(profile, nowMs, applyFloor) {
    // ⚠️⚠️ THE FIVE-SECOND FLOOR DOES NOT APPLY TO `submit`, AND LEAVING IT ON WOULD HAVE
    // BROKEN THE ONE THING THIS ENDPOINT EXISTS FOR. `CareerStore.FlushAsync` sends the offline
    // queue **in a tight loop**, oldest first, which is the entire design of playing on a bad
    // connection and catching up later: a player who was offline for an evening submits four
    // matches back to back. A floor between writes would refuse the second one, and the loop
    // stops at the first failure by design, so catching up would take five seconds per match and
    // every one of them would arrive as an error in the log.
    //
    // ⚠️ THE HOURLY CAP STILL APPLIES TO EVERYTHING. Sixty writes an hour is far above
    // any real player (a match is minutes long) and is what actually caps a client stuck in a
    // retry loop. The floor is kept for `abandon` and `report`, which are the two a client CAN
    // call in a loop without playing anything.
    if (applyFloor) {
        const last = Date.parse(String(profile.LastWriteUtc || "")) || 0;
        if (last > 0 && nowMs - last < WRITE_FLOOR_SECONDS * 1000)
            throw new Error("too many writes; slow down");
    }

    const hourAgo = nowMs - 3600 * 1000;
    profile.WritesThisHour = (profile.WritesThisHour || []).filter(t => Date.parse(t) > hourAgo);

    if (profile.WritesThisHour.length >= WRITES_PER_HOUR)
        throw new Error("too many writes this hour");

    profile.WritesThisHour.push(new Date(nowMs).toISOString());
    profile.LastWriteUtc = new Date(nowMs).toISOString();
}

/** Mirrors `IntegrityRules.CooldownFor` over the stamps `PlayerProfile.AbandonsUtc` keeps. */
function applyAbandon(profile, nowMs) {
    const cutoff = nowMs - ABANDON_MEMORY_DAYS * 24 * 3600 * 1000;
    profile.AbandonsUtc = (profile.AbandonsUtc || []).filter(t => Date.parse(t) > cutoff);
    profile.AbandonsUtc.push(new Date(nowMs).toISOString());

    const n = profile.AbandonsUtc.length;
    const i = n >= COOLDOWN_SECONDS.length ? COOLDOWN_SECONDS.length - 1 : n;
    const seconds = COOLDOWN_SECONDS[i];

    profile.CooldownUntilUtc = seconds > 0
        ? new Date(nowMs + seconds * 1000).toISOString()
        : "";

    return seconds;
}

/**
 * Apply or drop every ranked rating this player is owed a verdict on.
 *
 * ⚠️  IT IS BOUNDED BY THE LIST'S OWN CAP AND READS AT MOST ONE DOCUMENT PER PENDING MATCH.
 * `PendingRanked` is capped at twenty by the submit branch, which is twenty ranked matches
 * played while every one of their opponents stayed offline. In practice it is zero or one.
 */
async function collectPendingRanked(context, projectId, profile) {
    const pending = Array.isArray(profile.PendingRanked) ? profile.PendingRanked : [];
    if (pending.length === 0) return 0;

    // ⚠️ SAME REASONING AS THE SUBMIT BRANCH. A `load` that THREW would leave a player
    // unable to open the career screen at all because a rating they cannot see is unreadable.
    if (!context.serviceToken) return 0;

    let store;
    try { store = serviceStore(context); } catch (e) { return 0; }
    const keep = [];
    let collected = 0;

    for (const entry of pending) {
        if (!entry || !entry.Id || !entry.Arbiter) continue;

        let state;
        try {
            state = await readVerdict(store, projectId, entry.Arbiter, entry.Id);
        } catch (e) {
            // Unreadable is not disputed. Keep it and ask again next time.
            keep.push(entry);
            continue;
        }

        if (state === "witnessed") {
            profile.Rank = entry.Rank;
            collected++;
        } else if (state === "pending") {
            keep.push(entry);
        }
        // ⚠️  A DISPUTED MATCH IS DROPPED AND PAYS NOTHING, which is the entire reason the
        // rating was parked instead of applied optimistically.
    }

    profile.PendingRanked = keep;
    profile.PendingRankedMatchIds = keep.map(x => x.Id);
    return collected;
}

module.exports = async ({ params, context, logger }) => {
    const api = new DataApi(context);
    const { projectId, playerId } = context;
    const action = String(params.action || "load");

    if (action === "load") {
        const profile = await readJson(api, projectId, playerId, PROFILE_KEY, null);
        if (!profile) return { profile: "", applied: false };

        // ⚠️⚠️  A LOAD IS WHERE A WITNESSED RATING IS COLLECTED, AND THAT IS WHY THERE IS NO
        // POLLING ANYWHERE IN PHASE 8. Cloud Save is keyed by player id, so when the SECOND peer
        // corroborates a match the endpoint cannot reach into the FIRST peer's document to pay
        // them. The first peer's new rating is computed at submission and parked on their own
        // profile; this is the collection. Every client already calls `load` at boot and after
        // every match (`CareerStore.RefreshAsync`), so the rating lands on the next menu with no
        // extra request and nothing waiting on a timer.
        const collected = await collectPendingRanked(context, projectId, profile);
        if (collected > 0)
            await api.setProtectedItem(projectId, playerId, { key: PROFILE_KEY, value: JSON.stringify(profile) });

        return { profile: JSON.stringify(profile), applied: false, collected: collected };
    }

    if (action === "history") {
        const offset = Math.max(0, Math.trunc(Number(params.offset) || 0));
        const limit = Math.min(50, Math.max(1, Math.trunc(Number(params.limit) || 20)));
        const history = await readJson(api, projectId, playerId, HISTORY_KEY, []);
        return {
            history: JSON.stringify(history.slice(offset, offset + limit)),
            total: history.length,
        };
    }

    // -----------------------------------------------------------------------
    // ABANDON. `FUTURE.md` § 19.8 step 3 and 4.
    // -----------------------------------------------------------------------
    if (action === "abandon") {
        const profile = (await readJson(api, projectId, playerId, PROFILE_KEY, null)) || emptyProfile(playerId);
        const nowMs = Date.now();

        refuseIfTooFast(profile, nowMs, true);
        const seconds = applyAbandon(profile, nowMs);

        await api.setProtectedItem(projectId, playerId, { key: PROFILE_KEY, value: JSON.stringify(profile) });
        return { profile: JSON.stringify(profile), applied: true, cooldown: seconds };
    }

    if (action === "submit") {
        const record = normaliseRecord(JSON.parse(String(params.record || "{}")));
        if (!record.MatchId) throw new Error("a match record without an id cannot be deduplicated");

        // ⚠️⚠️  THE IMPOSSIBLE IS REFUSED BEFORE ANYBODY IS ASKED TO WITNESS IT, and the
        // refusal is permanent rather than retryable. `MatchRecordRules.Submittable` already
        // stops the client queueing a record it can never submit; this is the same question
        // asked by the party that decides. `CareerStore.DropUnsubmittable` is what stops one
        // permanently-refused record wedging every match behind it (`docs/TODO.md` § 94.1).
        const fault = sanityFault(record);
        if (fault) throw new Error("impossible match record: " + fault);

        // ⚠️⚠️ THE CALLER MUST BE IN THE MATCH. This is the whole reason each peer submits its
        // own line rather than the host submitting four: without it, one player could write any
        // career document on the project. It does not make the NUMBERS trustworthy, which is
        // Phase 8's job and is not being claimed here.
        const line = record.Players.find(p => p && !p.IsBot && p.PlayerId === playerId);
        if (!line) throw new Error("this player has no line in that match record");

        const profile = (await readJson(api, projectId, playerId, PROFILE_KEY, null)) || emptyProfile(playerId);
        profile.Modes = Array.isArray(profile.Modes) ? profile.Modes : [];
        profile.Characters = Array.isArray(profile.Characters) ? profile.Characters : [];
        profile.Slippers = Array.isArray(profile.Slippers) ? profile.Slippers : [];
        profile.AppliedMatchIds = Array.isArray(profile.AppliedMatchIds) ? profile.AppliedMatchIds : [];

        // Every career stored before Phase 4 has none of these. They default rather than throw,
        // and a level is re-derived from the XP for the same reason `ProfileRules.Normalise`
        // re-derives it: the XP is what was earned and the level is a view of it.
        profile.Mastery = Array.isArray(profile.Mastery) ? profile.Mastery : [];
        profile.Xp = Math.max(0, Math.trunc(Number(profile.Xp) || 0));
        profile.AfkStrikes = Math.max(0, Math.trunc(Number(profile.AfkStrikes) || 0));
        profile.XpPenaltyMatches = Math.max(0, Math.trunc(Number(profile.XpPenaltyMatches) || 0));
        profile.Level = levelForXp(profile.Xp);

        profile.Rank = profile.Rank || emptyRank();
        profile.AbandonsUtc = Array.isArray(profile.AbandonsUtc) ? profile.AbandonsUtc : [];
        profile.PendingRankedMatchIds = Array.isArray(profile.PendingRankedMatchIds)
            ? profile.PendingRankedMatchIds : [];
        profile.PendingRanked = Array.isArray(profile.PendingRanked) ? profile.PendingRanked : [];

        const nowMs = Date.now();
        refuseIfTooFast(profile, nowMs, false);

        const applied = applyRecord(profile, record, playerId);

        // ⚠️⚠️  CORROBORATION RUNS WHETHER OR NOT THE RECORD WAS NEWLY APPLIED. A resubmit
        // from the offline queue is a duplicate for the CAREER and is still a vote for the
        // RESULT, and the commonest reason a peer submits late is that it was the one with the
        // bad connection. Skipping the vote on a duplicate would make a flaky client silently
        // unable to witness anything.
        const recordDigest = digest(record);
        const witness = String(params.witness || "");
        const arbiter = arbiterFor(record);

        let verdict = "pending";

        // ⚠️⚠️ CORROBORATION FAILING MUST NEVER COST A PLAYER THEIR CAREER, AND THAT IS
        // WHY THIS IS THE ONE THING IN THIS BRANCH INSIDE A `try`. Phase 8 is an ADDITION to a
        // submission path that has worked since Phase 2. If the service token is unavailable, if
        // the arbiter's document is unreadable, or if Cloud Save is having a bad minute, the
        // right answer is a match nobody witnessed, not a match nobody counted.
        //
        // ⚠️⚠️ `docs/TODO.md` § 90.5 AND § 94.1 ARE THE SAME SHAPE OF DISASTER AND BOTH
        // WERE SILENT: an endpoint that answered normally while no career had ever reached the
        // server, and one refused record wedging every match behind it permanently. An unguarded
        // `serviceStore` here would be the third, and it would look exactly like the first two.
        //
        // ⚠️ A PENDING VERDICT PAYS NO RATING, so the failure is conservative in the
        // direction that matters. It is logged rather than swallowed silently.
        // ⚠️⚠️ AND IT IS NOT ATTEMPTED AT ALL WITHOUT A SERVICE TOKEN, WHICH IS THE
        // DIFFERENCE BETWEEN DEGRADING AND HANGING. `serviceStore` CONSTRUCTS happily from an
        // undefined token and then every request it makes is refused and retried, so the submit
        // that a player is waiting on sits there instead of failing. Measured on 2026-08-31: a
        // `-testCategory "Ugs"` run went silent for three minutes on exactly this call and had to
        // be killed. Asking first turns a stall into one branch not taken.
        if (arbiter && context.serviceToken) {
            try {
                const store = serviceStore(context);
                const outcome = await recordVerdict(store, projectId, arbiter, record.MatchId,
                                                    recordDigest, witness, playerId);
                verdict = outcome.state;
            } catch (e) {
                verdict = "pending";
                if (logger) logger.error("corroboration unavailable: " + (e && e.message));
            }
        }

        // ⚠️⚠️  THE RATING IS COMPUTED NOW AND PAID ONLY WHEN THE RESULT IS WITNESSED.
        // `FUTURE.md` § 9's opening line: a rank a host can award itself is worse than no rank.
        // A pending result parks the computed state on this player's own profile and
        // `collectPendingRanked` applies it on a later load; a disputed one drops it.
        let rankChange = null;

        if (applied && record.Ranked) {
            const season = seasonAt(Date.parse(record.PlayedUtc) || nowMs);
            beginSeason(profile.Rank, season);

            const line = record.Players.find(p => p && !p.IsBot && p.PlayerId === playerId);
            const index = record.Players.indexOf(line);
            const allPlacements = record.Players.map(p => (p && !p.IsBot) ? p.Placement : null);

            // ⚠⚠⚠  PHASE 11: A RESULT WITH BOTS IN IT DOES NOT MOVE A RATING THE SAME AMOUNT
            // AS ONE WITHOUT, AND THIS IS THE SERVER HALF OF `BotFillRules.Weight`. The queue is
            // allowed to fill a ranked match with bots (`FUTURE.md` § 11, reversing its own
            // earlier rule on 🧑's instruction, with the reason and the expiry recorded), and the
            // condition attached to that reversal is this one: *"a result with a bot in it cannot
            // move a rating the same amount as one without, or the fastest climb in the game is
            // queueing at 4 a.m."*
            //
            // ⚠⚠  IT IS COMPUTED FROM THE RECORD RATHER THAN SENT, so a client cannot claim a
            // full-weight result by asserting one. `IsBot` is already in the digest every peer
            // hashes (see `digestOf`), so a host that lies about it fails corroboration.
            const humans = record.Players.filter(p => p && !p.IsBot).length;
            const weight = botWeight(humans, record.Players.length);

            const after = blendRank(profile.Rank,
                                    rankedResult(Object.assign({}, profile.Rank),
                                                 { index: index, place: line.Placement },
                                                 allPlacements),
                                    weight);

            if (verdict === "witnessed") {
                profile.Rank = after;
                rankChange = { before: null, after: after.Rating };
            } else if (verdict === "pending") {
                profile.PendingRanked = profile.PendingRanked
                    .filter(x => x && x.Id !== record.MatchId)
                    .slice(-20);
                profile.PendingRanked.push({ Id: record.MatchId, Arbiter: arbiter, Rank: after });

                profile.PendingRankedMatchIds = profile.PendingRanked.map(x => x.Id);
            }
        }

        const history = remember(await readJson(api, projectId, playerId, HISTORY_KEY, []), record);
        await api.setProtectedItem(projectId, playerId, { key: PROFILE_KEY, value: JSON.stringify(profile) });
        if (applied)
            await api.setProtectedItem(projectId, playerId, { key: HISTORY_KEY, value: JSON.stringify(history) });

        // ⚠️  AN ALREADY-COUNTED RECORD IS A SUCCESS, NOT AN ERROR. The offline queue resubmits,
        // and a client that treated the second answer as a failure would keep the record queued
        // and retry it forever.
        return {
            profile: JSON.stringify(profile),
            applied: applied,
            verdict: verdict,
            rated: rankChange !== null,
        };
    }

    // -----------------------------------------------------------------------
    // REPORT. `FUTURE.md` § 19.8 step 2.
    //
    // ⚠️⚠️  IT WRITES A COUNT AND NOTHING ELSE, AND THERE IS NO CONSOLE TO READ IT.
    // `FUTURE.md` § 0.5b, phase 8 row: "resist building a moderation console. This phase's
    // success is invisible." A report with nobody to read it is still worth taking, because the
    // player needs somewhere to put the feeling and the count is what a future moderation pass
    // would sort by. Pretending to act on it would be worse than saying nothing.
    // -----------------------------------------------------------------------
    if (action === "report") {
        const subject = text(params.playerId, 64);
        const reason = clampInt(params.reason, 0, 6);
        if (!subject || subject === playerId) throw new Error("no player to report");

        const profile = (await readJson(api, projectId, playerId, PROFILE_KEY, null)) || emptyProfile(playerId);
        const nowMs = Date.now();
        refuseIfTooFast(profile, nowMs, true);

        profile.ReportsToday = (profile.ReportsToday || [])
            .filter(t => Date.parse(t) > nowMs - 24 * 3600 * 1000);

        if (profile.ReportsToday.length >= 10) throw new Error("too many reports today");
        profile.ReportsToday.push(new Date(nowMs).toISOString());

        const store = serviceStore(context);
        const theirs = (await readJson(store, projectId, subject, PROFILE_KEY, null));

        if (theirs) {
            theirs.Reports = theirs.Reports || {};
            theirs.Reports[String(reason)] = (theirs.Reports[String(reason)] || 0) + 1;
            await store.setProtectedItem(projectId, subject,
                { key: PROFILE_KEY, value: JSON.stringify(theirs) });
        }

        await api.setProtectedItem(projectId, playerId, { key: PROFILE_KEY, value: JSON.stringify(profile) });
        return { profile: JSON.stringify(profile), applied: true };
    }

    throw new Error("unknown match record action");
};

// ⚠️⚠️ EVERY PARAMETER A SCRIPT USES MUST BE DECLARED HERE OR CLOUD CODE STRIPS IT, AND THE
// FAILURE IS SILENT AND LOOKS LIKE A WORKING ENDPOINT. Measured on 2026-08-30: this endpoint
// answered a call carrying an action with the payload of the branch an ABSENT action falls
// through to. Nothing errors, nothing logs, and the answer is well-formed, so a probe that
// only asserts "it answered" passes. `docs/TODO.md` § 90.5 is the entry.
//
// ⚠️ IF A NEW ACTION NEEDS A NEW PARAMETER, IT GOES IN THIS BLOCK IN THE SAME EDIT.
module.exports.params = {
    action: "String",
    record: "String",
    offset: "Numeric",
    limit: "Numeric",

    // ⚠️⚠️  PHASE 8 AND 9 ADDED THREE AND THEY GO HERE IN THE SAME EDIT, WHICH IS THE RULE
    // THE BLOCK ABOVE EXISTS TO ENFORCE. An undeclared parameter arrives `undefined` with no
    // error and no log, so a missing `witness` here would make every match in the game read as
    // uncorroborated and no rating would ever move, exactly as no career ever reached the server
    // in `docs/TODO.md` § 90.5.
    witness: "String",
    playerId: "String",
    reason: "Numeric",
};
