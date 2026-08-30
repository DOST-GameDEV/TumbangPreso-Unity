const { DataApi } = require("@unity-services/cloud-save-1.4");

// ⚠️⚠️ THIS FILE IS `TelemetryRules.cs` WRITTEN A SECOND TIME, AND THE C# IS THE SPECIFICATION.
// The same trade `match-record.js` records about `ProfileRules` (`docs/TODO.md` § 89.6), applied
// to a smaller file. Cloud Code cannot import the C# and the C# cannot run here, so every
// constant below names the core member it mirrors and `CareerAndCloudCodeTests` fails if the
// numbers split. When the two disagree, THIS is the bug.
//
// ⚠️⚠️ AND THE EVENT NAMES ARE A CONTRACT, NOT A LIST. `FUTURE.md` § 19.3: *"a renamed event is a
// broken history"*. A name that changes does not produce an error anywhere; it produces a counter
// that starts at zero and a year of data that no longer joins to it. The server refuses a name it
// does not know so a typo in a new call site is a failure rather than a silent second series.
// `docs/TODO.md` § 90.3 is the contract in prose.

const KEY = "telemetry";
const ROLLUP_ID = "telemetry-rollup";
const ROLLUP_KEY = "totals";

// Mirrors `TelemetryRules.MaxEventsPerBatch` and `TelemetryRules.MaxParametersPerEvent`.
const MAX_EVENTS_PER_BATCH = 64;
const MAX_PARAMETERS_PER_EVENT = 8;
const MAX_PARAMETER_LENGTH = 32;

// Mirrors `TelemetryEvents.Funnel`, in order. ⚠️ APPEND ONLY, and the ORDER IS THE MEANING:
// "furthest step reached" is an index comparison, so inserting one in the middle rewrites what
// every stored profile is claiming. `FUTURE.md` § 0.5 rule 5 for wire-facing lists is this rule.
const FUNNEL = [
    "first_launch",
    "first_sign_in",
    "first_menu",
    "first_queue",
    "first_match_started",
    "first_match_finished",
];

// Mirrors `TelemetryEvents.All`. Anything not in here is refused.
const EVENTS = FUNNEL.concat([
    "session_start",
    "session_end",
    "match_started",
    "match_finished",
    "match_left",
    "pick",
    "settings_snapshot",
    "disconnect",
]);

function text(value, max) {
    const raw = String(value === undefined || value === null ? "" : value);
    let out = "";
    for (let i = 0; i < raw.length && out.length < max; i++) {
        const code = raw.charCodeAt(i);
        if (code < 32 || code === 127) continue;
        out += raw[i];
    }
    return out.trim();
}

// ⚠️⚠️ THE ONLY THING THAT MAY REACH THIS SCRIPT IS A NAME FROM THE LIST AND A HANDFUL OF SHORT
// LABELS. `FUTURE.md` § 19.3: *"No personally identifying field in any event, ever."* The client
// enforces the same rule in `TelemetryRules`, and this enforces it again because the client is
// the half somebody can edit. There is deliberately no free-text field: a parameter is a bucket
// label like `hero_strike` or `p95`, never a sentence somebody typed.
//
// ⚠️ AND THE PLAYER IS NEVER NAMED IN THE PAYLOAD AT ALL. The caller's id comes from
// `context.playerId`, which the client cannot set, so there is no identifier in the batch to
// leak, to get wrong, or to have to strip later.
// ⚠️⚠️ THIS FUNCTION WAS CALLED `parameters` AND THAT ONE NAME BROKE THE DEPLOY, SILENTLY.
// `docs/TODO.md` § 90.5: with a top-level `function parameters(...)` in the file, `ugs deploy`
// uploaded the code and then reported `params: []` for the whole script, so `action` was stripped
// from every call and every request landed on the default branch. Nothing failed, nothing warned,
// and the endpoint answered normally from the wrong branch. Bisected one function at a time
// against the live service: renaming it is the entire fix. **Do not name anything at the top level
// of a Cloud Code script `parameters`.**
function columnsFrom(raw) {
    const result = {};
    if (!raw || typeof raw !== "object") return result;

    let kept = 0;
    for (const key of Object.keys(raw)) {
        if (kept >= MAX_PARAMETERS_PER_EVENT) break;
        const name = text(key, MAX_PARAMETER_LENGTH);
        if (!/^[a-z][a-z0-9_]*$/.test(name)) continue;

        const value = raw[key];
        if (typeof value === "number" && isFinite(value)) {
            result[name] = Math.round(value * 1000) / 1000;
        } else {
            const label = text(value, MAX_PARAMETER_LENGTH);
            if (!/^[A-Za-z0-9_.-]*$/.test(label)) continue;
            result[name] = label;
        }
        kept++;
    }
    return result;
}

// ⚠️⚠️ THE BATCH ARRIVES AS A JSON STRING, NOT AS AN ARRAY, AND THAT IS A MEASURED CONSTRAINT
// RATHER THAN A STYLE. `docs/TODO.md` § 90.5: this script first declared `events: "JSON"` and the
// service dropped the ENTIRE parameter block, `params: []`, so `action` went missing too and
// every call landed on the default branch again. `String` is the only type these three scripts
// have proven, and `match-record` already passes its whole record the same way.
function parseBatch(raw) {
    if (Array.isArray(raw)) return raw;
    try {
        const parsed = JSON.parse(String(raw || "[]"));
        return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
        return [];
    }
}

function readJson(item, fallback) {
    if (!item || !item.value) return fallback;
    try {
        return typeof item.value === "string" ? JSON.parse(item.value) : item.value;
    } catch (e) {
        return fallback;
    }
}

function emptyProfile() {
    return { Funnel: {}, Counters: {}, Params: {}, Sessions: 0, Batches: 0, LastUtc: "" };
}

function emptyRollup() {
    return { Funnel: {}, Counters: {}, Params: {}, Players: 0, Batches: 0, UpdatedUtc: "" };
}

function addCount(map, name, count) {
    map[name] = (Number(map[name]) || 0) + count;
}

/**
 * Folds a parameter into the aggregate as one counter per distinct label, and as a running
 * mean per numeric key.
 *
 * ⚠️ A NUMBER IS KEPT AS A SUM AND A COUNT, NEVER AS A STORED AVERAGE. `docs/TODO.md` § 89
 * makes the same argument about the career page: a stored rate cannot be re-derived after a
 * change and cannot be added to another period's. Two counts divided at read time can.
 */
function foldParameters(into, eventName, params) {
    for (const key of Object.keys(params)) {
        const value = params[key];
        if (typeof value === "number") {
            const bucket = eventName + "." + key;
            into[bucket] = into[bucket] || { Sum: 0, Count: 0 };
            into[bucket].Sum = (Number(into[bucket].Sum) || 0) + value;
            into[bucket].Count = (Number(into[bucket].Count) || 0) + 1;
        } else if (value) {
            const bucket = eventName + "." + key + "." + value;
            into[bucket] = into[bucket] || { Sum: 0, Count: 0 };
            into[bucket].Count = (Number(into[bucket].Count) || 0) + 1;
        }
    }
}

async function loadRollup(api, projectId) {
    if (typeof api.getCustomItems !== "function" || typeof api.setCustomItem !== "function")
        throw new Error("custom data is not available in this Cloud Save module");

    const response = await api.getCustomItems(projectId, ROLLUP_ID, [ROLLUP_KEY]);
    const rollup = readJson(response.data.results.find(x => x.key === ROLLUP_KEY), emptyRollup());
    rollup.Funnel = rollup.Funnel || {};
    rollup.Counters = rollup.Counters || {};
    rollup.Params = rollup.Params || {};
    return rollup;
}

module.exports = async ({ params, context, logger }) => {
    const api = new DataApi(context);
    const { projectId, playerId } = context;
    const action = String(params.action || "submit");

    const stored = await api.getProtectedItems(projectId, playerId, [KEY]);
    const profile = readJson(stored.data.results.find(x => x.key === KEY), emptyProfile());
    profile.Funnel = profile.Funnel || {};
    profile.Counters = profile.Counters || {};
    profile.Params = profile.Params || {};

    if (action === "report") {
        let rollup = null;
        try {
            rollup = await loadRollup(api, projectId);
        } catch (e) {
            // A rollup this deployment cannot reach is not a failed report. The per-player
            // document is exact either way; the rollup is the convenience view.
            logger.info("rollup unavailable: " + e.message);
        }
        return { profile, rollup, funnel: FUNNEL };
    }

    if (action !== "submit") throw new Error("unknown telemetry action");

    // ⚠️ THE BATCH IS ONE CALL FOR A WHOLE SESSION, WHICH IS `FUTURE.md` § 0.3's ONLY HARD RULE
    // ABOUT CLOUD CODE: *"Call it once per match, never per event."* Telemetry is the feature most
    // able to break that, because every interesting thing in a match is an event. The client
    // counts locally and sends totals; nothing here is shaped to accept a stream.
    const batch = parseBatch(params.events).slice(0, MAX_EVENTS_PER_BATCH);
    const now = new Date().toISOString();

    const firstReached = [];
    let accepted = 0;
    let refused = 0;

    for (const raw of batch) {
        const name = text(raw && raw.Name, MAX_PARAMETER_LENGTH);
        if (EVENTS.indexOf(name) < 0) { refused++; continue; }

        const count = Math.max(1, Math.min(100000, Math.trunc(Number(raw.Count) || 1)));
        const args = columnsFrom(raw.Params);

        addCount(profile.Counters, name, count);
        foldParameters(profile.Params, name, args);
        accepted++;

        // ⚠️⚠️ A FUNNEL STEP IS RECORDED ONCE PER PLAYER, EVER, AND THE FIRST TIMESTAMP WINS.
        // A funnel that could be re-entered is not a funnel, it is a counter with a misleading
        // name: reinstalling, replaying or a client that resends its buffer would each add a
        // second "first" and the conversion rate between two steps could exceed 100 per cent.
        if (FUNNEL.indexOf(name) >= 0 && !profile.Funnel[name]) {
            profile.Funnel[name] = now;
            firstReached.push(name);
        }
    }

    if (accepted > 0) {
        profile.Batches = (Number(profile.Batches) || 0) + 1;
        profile.Sessions = Number(profile.Counters.session_start) || profile.Sessions || 0;
        profile.LastUtc = now;
        await api.setProtectedItem(projectId, playerId, { key: KEY, value: JSON.stringify(profile) });
    }

    // ⚠️⚠️ THE PROJECT-WIDE ROLLUP IS APPROXIMATE AND SAYING SO IS THE POINT. Cloud Save has no
    // atomic increment, so two sessions submitting in the same instant can lose one update. At
    // four to eight concurrent players that is a rounding error against numbers whose job is to
    // say "most people stop here"; at a size where it would matter, this is the wrong storage and
    // the fix is a real analytics sink rather than a lock. The PER-PLAYER document above is exact
    // and is what any number that has to be defended should be recomputed from.
    let rolled = false;
    if (accepted > 0) {
        try {
            const rollup = await loadRollup(api, projectId);
            for (const raw of batch) {
                const name = text(raw && raw.Name, MAX_PARAMETER_LENGTH);
                if (EVENTS.indexOf(name) < 0) continue;
                const count = Math.max(1, Math.min(100000, Math.trunc(Number(raw.Count) || 1)));
                addCount(rollup.Counters, name, count);
                foldParameters(rollup.Params, name, columnsFrom(raw.Params));
            }
            for (const step of firstReached) addCount(rollup.Funnel, step, 1);
            if (firstReached.indexOf(FUNNEL[0]) >= 0) rollup.Players = (Number(rollup.Players) || 0) + 1;
            rollup.Batches = (Number(rollup.Batches) || 0) + 1;
            rollup.UpdatedUtc = now;

            await api.setCustomItem(projectId, ROLLUP_ID,
                { key: ROLLUP_KEY, value: JSON.stringify(rollup) });
            rolled = true;
        } catch (e) {
            // ⚠️ A FAILED ROLLUP MUST NOT FAIL THE SUBMIT. The per-player write above has already
            // landed and it is the exact copy; losing the convenience view would otherwise make
            // the client retry a batch it has already delivered.
            logger.info("rollup write skipped: " + e.message);
        }
    }

    return { accepted, refused, rolled, funnel: profile.Funnel };
};

// ⚠️⚠️ EVERY PARAMETER A SCRIPT USES MUST BE DECLARED HERE OR CLOUD CODE STRIPS IT, AND THE
// FAILURE IS SILENT AND LOOKS LIKE A WORKING ENDPOINT. Measured on 2026-08-30: this endpoint
// answered a call carrying an action with the payload of the branch an ABSENT action falls
// through to. Nothing errors, nothing logs, and the answer is well-formed, so a probe that
// only asserts "it answered" passes. `docs/TODO.md` § 90.5 is the entry.
//
// ⚠️ IF A NEW ACTION NEEDS A NEW PARAMETER, IT GOES IN THIS BLOCK IN THE SAME EDIT.
//
// ⚠️⚠️ NOTHING BUT BARE `name: "Type"` LINES MAY GO INSIDE THE BRACES, AND THAT IS MEASURED.
// `docs/TODO.md` § 90.5: this block first carried a ⚠️-marked comment between its two entries and
// the deploy reported `params: []` for the whole script, twice, while the two scripts whose
// braces held only plain entries kept theirs. A dropped block is not a dropped comment: `action`
// went with it and every call fell back to the default branch, which is the silent failure this
// whole entry exists for. Put the reasoning above the block, as here.
//
// ⚠️ `events` IS A `String`, NOT `JSON`, AND CARRIES A SERIALISED ARRAY. `parseBatch` reads it,
// exactly as `match-record` has always carried its record. `String` and `Numeric` are the only
// two types these three scripts have proven against this project.
module.exports.params = {
    action: "String",
    events: "String",
};
