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
        Online: !!r.Online,
        DefenderByRound: defenders,
        Players: lines,
    };
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

module.exports = async ({ params, context, logger }) => {
    const api = new DataApi(context);
    const { projectId, playerId } = context;
    const action = String(params.action || "load");

    if (action === "load") {
        const profile = await readJson(api, projectId, playerId, PROFILE_KEY, null);
        return { profile: profile ? JSON.stringify(profile) : "", applied: false };
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

    if (action === "submit") {
        const record = normaliseRecord(JSON.parse(String(params.record || "{}")));
        if (!record.MatchId) throw new Error("a match record without an id cannot be deduplicated");

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

        const applied = applyRecord(profile, record, playerId);

        if (applied) {
            const history = remember(await readJson(api, projectId, playerId, HISTORY_KEY, []), record);
            await api.setProtectedItem(projectId, playerId, { key: PROFILE_KEY, value: JSON.stringify(profile) });
            await api.setProtectedItem(projectId, playerId, { key: HISTORY_KEY, value: JSON.stringify(history) });
        }

        // ⚠️ AN ALREADY-COUNTED RECORD IS A SUCCESS, NOT AN ERROR. The offline queue resubmits,
        // and a client that treated the second answer as a failure would keep the record queued
        // and retry it forever.
        return { profile: JSON.stringify(profile), applied: applied };
    }

    throw new Error("unknown match record action");
};
