const { DataApi } = require("@unity-services/cloud-save-1.4");

// TANSAN: the soft currency, the shop, the starter set, migration and tasks.
//
// ⚠️⚠️ THIS FILE IS `EconomyRules` (Packages/com.tumbangpreso.core/Runtime/Economy.cs) WRITTEN A
// SECOND TIME, AND THE C# IS THE SPECIFICATION. Cloud Code cannot import C#, so every rule is here
// again and names the member it mirrors; `Core.Tests/EconomyTests.cs` reads this file as text and
// fails when a constant drifts. When the two disagree, THIS is the bug, exactly as
// `match-record.js` says about `ProfileRules`.
//
// ⚠️⚠️ THIS SCRIPT IS THE ONLY WRITER OF A BALANCE. The wallet is a PROTECTED Cloud Save item,
// which a client token can read and cannot write. Payouts and task progress are computed from
// `matchHistory`, which only `match-record.js` writes, and a purchase names an id whose price is
// looked up here. Nothing a client sends is a number of TANSAN.
//
// ⚠️ NO REAL MONEY. There is no action that adds currency for anything but play.

const WALLET_KEY = "wallet";
const PROFILE_KEY = "careerProfile";
const HISTORY_KEY = "matchHistory";

// Mirrors EconomyRules.
const STARTING_BALANCE = 300;
const HERO_PRICE = 1200;
const SLIPPER_PRICE = 300;
const CAN_PRICE = 250;
const COMPLETION_PAY = 25;
const PLACEMENT_PAY = [40, 25, 15, 10];
const DAILY_MATCH_CAP = 450;
const PAID_ID_MEMORY = 200;
const DAILY_TASK_COUNT = 3;
const WEEKLY_TASK_COUNT = 3;
const CLAIM_MEMORY = 64;

// ⚠️⚠️ TEMPORARY PLAYTEST GRANT (owner, 2026-09-26: *"give us all 999999 tansan so we can unlock all"*). While this is above
// zero, every wallet this script loads is topped up to it before anything else happens, so the playtesters can unlock every
// hero and item. It breaks the rule in this file's header (currency only from play) ON PURPOSE AND FOR NOW: set it back to 0
// and deploy before any public build. Tracked in `docs/TODO.md` (the TEMPORARY PLAYTEST GRANT row). Not in `EconomyRules`,
// because it is not an economy rule.
const PLAYTEST_TOPUP = 999999;

// Mirrors EconomyRules.StarterHeroes / StarterSlippers / StarterCans.
const STARTER_HEROES = ["dante", "cheska", "sean"];
const STARTER_SLIPPERS = ["tsinelas", "crocs", "pantulog", "sike"];
const STARTER_CANS = ["pasip", "boyben", "decades", "metal"];

// Mirrors Roster.HeroPeople, Roster.Slippers and Roster.Cans, in roster order. ⚠️ A new hero or
// prop appended to the roster must be appended here too, or the shop cannot sell it.
const HEROES = ["dante", "cheska", "sean", "zack", "nemu", "phaister", "rafi", "amihan", "paete"];
const SLIPPERS = ["tsinelas", "crocs", "pantulog", "sike", "spartan", "alpombra", "pambahay",
                  "heels", "sandals", "loafers"];
const CANS = ["pasip", "boyben", "decades", "metal", "piyesta", "karne"];

// Mirrors EconomyRules.DailyPool and WeeklyPool: id, stat, target, reward.
const DAILY_POOL = [
    ["d_play2", "Matches", 2, 60],
    ["d_knock3", "Knockdowns", 3, 70],
    ["d_fetch4", "Retrievals", 4, 60],
    ["d_tag2", "Tags", 2, 70],
    ["d_throw12", "Throws", 12, 50],
    ["d_hero1", "HeroStrikeMatches", 1, 50],
    ["d_classic1", "ClassicMatches", 1, 50],
    ["d_podium1", "Podiums", 1, 80],
];
const WEEKLY_POOL = [
    ["w_play10", "Matches", 10, 300],
    ["w_win3", "Wins", 3, 400],
    ["w_pressure5", "PressureRetrievals", 5, 350],
    ["w_knock15", "Knockdowns", 15, 350],
    ["w_tag10", "Tags", 10, 350],
];

function priceOf(id) {
    const [kind, ref] = String(id || "").split(":");
    if (kind === "hero" && HEROES.indexOf(ref) >= 0) return HERO_PRICE;
    if (kind === "slipper" && SLIPPERS.indexOf(ref) >= 0) return SLIPPER_PRICE;
    if (kind === "can" && CANS.indexOf(ref) >= 0) return CAN_PRICE;
    return -1;
}

function starterIds() {
    return STARTER_HEROES.map(x => "hero:" + x)
        .concat(STARTER_SLIPPERS.map(x => "slipper:" + x))
        .concat(STARTER_CANS.map(x => "can:" + x));
}

function owns(wallet, id) {
    return starterIds().indexOf(id) >= 0 || wallet.Owned.indexOf(id) >= 0;
}

/** Mirrors EconomyRules.Create: migration, never reset. */
function createWallet(career, nowIso) {
    const wallet = {
        Balance: STARTING_BALANCE, Owned: starterIds(), PaidMatchIds: [],
        EarnedDay: 0, EarnedToday: 0, Claimed: [], Version: 1, CreatedUtc: nowIso,
    };
    const grant = id => { if (wallet.Owned.indexOf(id) < 0) wallet.Owned.push(id); };
    if (career) {
        (Array.isArray(career.Characters) ? career.Characters : []).forEach(p => {
            if (p && HEROES.indexOf(p.Id) >= 0) grant("hero:" + p.Id);
        });
        (Array.isArray(career.Mastery) ? career.Mastery : []).forEach(m => {
            if (m && Number(m.Xp) > 0 && HEROES.indexOf(m.Id) >= 0) grant("hero:" + m.Id);
        });
        (Array.isArray(career.Slippers) ? career.Slippers : []).forEach(p => {
            if (p && SLIPPERS.indexOf(p.Id) >= 0) grant("slipper:" + p.Id);
        });
    }
    return wallet;
}

function normaliseWallet(raw) {
    const w = raw || {};
    const ints = v => Math.max(0, Math.trunc(Number(v) || 0));
    const list = v => Array.isArray(v) ? v.map(String) : [];
    return {
        Balance: ints(w.Balance), Owned: list(w.Owned), PaidMatchIds: list(w.PaidMatchIds),
        EarnedDay: ints(w.EarnedDay), EarnedToday: ints(w.EarnedToday), Claimed: list(w.Claimed),
        Version: 1, CreatedUtc: String(w.CreatedUtc || ""),
    };
}

/** Mirrors EconomyRules.DayOf / WeekOf / PeriodIndex. */
function dayOf(ms) { return Math.floor(ms / 86400000); }
function weekOf(day) { return Math.floor((day + 3) / 7); }
function periodIndex(period, day) { return period === 0 ? day : weekOf(day); }

/** Mirrors ProgressionRules.WasAfk. */
function wasAfk(record, line) {
    if (!line || line.IsBot) return false;
    const rounds = Math.trunc(Number(record.Rounds) || 0);
    const active = Number(line.ActiveRounds);
    if (rounds <= 0) return false;
    if (!isFinite(active) || active < 0) return false;
    return active < rounds;
}

/** Mirrors EconomyRules.TryRead. */
function earnedFrom(record, playerId) {
    if (!record || !record.MatchId) return null;
    const line = (Array.isArray(record.Players) ? record.Players : [])
        .find(p => p && !p.IsBot && p.PlayerId === playerId);
    if (!line) return null;
    const played = Date.parse(record.PlayedUtc);
    if (!isFinite(played)) return null;
    const n = v => Math.max(0, Math.trunc(Number(v) || 0));
    return {
        MatchId: String(record.MatchId), Day: dayOf(played), Mode: String(record.Mode || ""),
        Placement: n(line.Placement), Afk: wasAfk(record, line),
        Throws: n(line.Throws), Knockdowns: n(line.Knockdowns), Retrievals: n(line.Retrievals),
        PressureRetrievals: n(line.RetrievalsUnderPressure), Tags: n(line.Tags),
    };
}

/** Mirrors EconomyRules.MatchPay. */
function matchPay(m) {
    if (m.Afk) return 0;
    let pay = COMPLETION_PAY;
    if (m.Placement >= 1 && m.Placement <= PLACEMENT_PAY.length) pay += PLACEMENT_PAY[m.Placement - 1];
    return pay;
}

/** Mirrors EconomyRules.Settle. History is newest first, so it is walked oldest first. */
function settle(wallet, matches, today) {
    let paid = 0;
    if (wallet.EarnedDay !== today) { wallet.EarnedDay = today; wallet.EarnedToday = 0; }
    matches.slice().reverse().forEach(m => {
        if (!m.MatchId || wallet.PaidMatchIds.indexOf(m.MatchId) >= 0) return;
        wallet.PaidMatchIds.push(m.MatchId);
        let pay = matchPay(m);
        if (m.Day !== today) pay = 0;
        pay = Math.min(pay, Math.max(0, DAILY_MATCH_CAP - wallet.EarnedToday));
        wallet.EarnedToday += pay;
        wallet.Balance += pay;
        paid += pay;
    });
    while (wallet.PaidMatchIds.length > PAID_ID_MEMORY) wallet.PaidMatchIds.shift();
    return paid;
}

/** Mirrors EconomyRules.Fnv: FNV-1a over UTF-16 code units, like `derivedTag`. */
function fnv(textValue) {
    let hash = 2166136261 >>> 0;
    const s = String(textValue || "");
    for (let i = 0; i < s.length; i++) {
        hash = (hash ^ s.charCodeAt(i)) >>> 0;
        hash = Math.imul(hash, 16777619) >>> 0;
    }
    return hash;
}

/** Mirrors EconomyRules.TasksFor. */
function tasksFor(playerId, period, day) {
    const pool = period === 0 ? DAILY_POOL : WEEKLY_POOL;
    const count = period === 0 ? DAILY_TASK_COUNT : WEEKLY_TASK_COUNT;
    const seed = fnv(playerId + "|" + period + "|" + periodIndex(period, day));
    const order = pool.map((_, i) => i);
    let state = seed === 0 ? 2463534242 : seed;
    for (let i = order.length - 1; i > 0; i--) {
        state = (state ^ (state << 13)) >>> 0;
        state = (state ^ (state >>> 17)) >>> 0;
        state = (state ^ (state << 5)) >>> 0;
        const j = state % (i + 1);
        const t = order[i]; order[i] = order[j]; order[j] = t;
    }
    return order.slice(0, Math.min(count, order.length)).map(i => ({
        Id: pool[i][0], Stat: pool[i][1], Target: pool[i][2], Reward: pool[i][3], Period: period,
    }));
}

/** Mirrors EconomyRules.Count. */
function countFor(stat, m) {
    if (m.Afk) return 0;
    switch (stat) {
        case "Matches": return 1;
        case "Wins": return m.Placement === 1 ? 1 : 0;
        case "Podiums": return m.Placement >= 1 && m.Placement <= 2 ? 1 : 0;
        case "Knockdowns": return m.Knockdowns;
        case "Retrievals": return m.Retrievals;
        case "PressureRetrievals": return m.PressureRetrievals;
        case "Tags": return m.Tags;
        case "Throws": return m.Throws;
        case "HeroStrikeMatches": return m.Mode === "HeroStrike" ? 1 : 0;
        case "ClassicMatches": return m.Mode === "Classic" ? 1 : 0;
        default: return 0;
    }
}

/** Mirrors EconomyRules.Progress. */
function progress(task, matches, today) {
    const period = periodIndex(task.Period, today);
    let total = 0;
    matches.forEach(m => { if (periodIndex(task.Period, m.Day) === period) total += countFor(task.Stat, m); });
    return Math.min(total, task.Target);
}

/** Mirrors EconomyRules.ClaimKey. */
function claimKey(task, today) {
    return (task.Period === 0 ? "d" : "w") + ":" + periodIndex(task.Period, today) + ":" + task.Id;
}

function taskBoard(wallet, playerId, matches, today) {
    return tasksFor(playerId, 0, today).concat(tasksFor(playerId, 1, today)).map(t => ({
        Id: t.Id, Period: t.Period, Target: t.Target, Reward: t.Reward,
        Progress: progress(t, matches, today),
        Claimed: wallet.Claimed.indexOf(claimKey(t, today)) >= 0,
    }));
}

async function readItems(api, projectId, playerId, keys) {
    const response = await api.getProtectedItems(projectId, playerId, keys);
    const found = {};
    (response.data.results || []).forEach(r => { found[r.key] = r; });
    return found;
}

function parse(item, fallback) {
    if (!item || item.value === undefined || item.value === null) return fallback;
    try { return typeof item.value === "string" ? JSON.parse(item.value) : item.value; }
    catch (e) { return fallback; }
}

module.exports = async ({ params, context, logger }) => {
    const api = new DataApi(context);
    const { projectId, playerId } = context;
    const action = String(params.action || "load");
    const nowMs = Date.now();
    const today = dayOf(nowMs);

    const items = await readItems(api, projectId, playerId, [WALLET_KEY, PROFILE_KEY, HISTORY_KEY]);
    const existing = items[WALLET_KEY];
    const wallet = existing ? normaliseWallet(parse(existing, {}))
                            : createWallet(parse(items[PROFILE_KEY], null), new Date(nowMs).toISOString());

    const history = parse(items[HISTORY_KEY], []);
    const matches = (Array.isArray(history) ? history : [])
        .map(r => earnedFrom(r, playerId)).filter(m => m !== null);

    // ⚠️ EVERY ACTION SETTLES FIRST, so a balance is never shown or spent without the matches the
    // server already recorded. Settling is idempotent: paid ids are remembered.
    const paid = settle(wallet, matches, today);
    if (PLAYTEST_TOPUP > 0 && wallet.Balance < PLAYTEST_TOPUP) wallet.Balance = PLAYTEST_TOPUP;

    let result = "ok";
    if (action === "buy") {
        const id = String(params.item || "");
        const price = priceOf(id);
        if (price < 0) result = "unknown";
        else if (owns(wallet, id)) result = "owned";
        else if (wallet.Balance < price) result = "poor";
        else { wallet.Balance -= price; wallet.Owned.push(id); result = "bought"; }
    } else if (action === "claim") {
        const id = String(params.task || "");
        const task = tasksFor(playerId, 0, today).concat(tasksFor(playerId, 1, today)).find(t => t.Id === id);
        if (!task) result = "unassigned";
        else if (wallet.Claimed.indexOf(claimKey(task, today)) >= 0) result = "claimed-already";
        else if (progress(task, matches, today) < task.Target) result = "unfinished";
        else {
            wallet.Claimed.push(claimKey(task, today));
            while (wallet.Claimed.length > CLAIM_MEMORY) wallet.Claimed.shift();
            wallet.Balance += task.Reward;
            result = "claimed";
        }
    } else if (action !== "load") {
        throw new Error("unknown wallet action");
    }

    // ⚠️ WRITTEN WITH THE LOCK THE READ RETURNED, so two purchases racing from two clients cannot
    // both spend the same balance: the second write is refused and the client reloads.
    const write = { key: WALLET_KEY, value: JSON.stringify(wallet) };
    if (existing && existing.writeLock) write.writeLock = existing.writeLock;
    await api.setProtectedItem(projectId, playerId, write);

    return {
        wallet: JSON.stringify(wallet),
        result: result,
        paid: paid,
        day: today,
        tasks: JSON.stringify(taskBoard(wallet, playerId, matches, today)),
    };
};

// ⚠️⚠️ EVERY PARAMETER A SCRIPT USES MUST BE DECLARED HERE OR CLOUD CODE STRIPS IT, AND THE
// FAILURE IS SILENT (`docs/TODO.md` § 90.5). `EconomyTests.TheCloudScriptMirrorsEveryConstant`
// asserts this block names all three.
module.exports.params = {
    action: "String",
    item: "String",
    task: "String",
};

// Exposed for `tools/test_wallet_script.js`, which runs the rules under node with a stub store.
module.exports.rules = { createWallet, settle, tasksFor, progress, priceOf, fnv, weekOf, dayOf, earnedFrom, PLAYTEST_TOPUP };
