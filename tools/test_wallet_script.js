// Runs ugs/cloud-code/wallet.js under node with a stub Cloud Save, so the server copy of
// EconomyRules can be exercised without a UGS login. `node tools/test_wallet_script.js`
// ⚠️ The golden task ids below are asserted by Core.Tests/EconomyTests.cs as well, which is
// what proves the C# specification and this script pick the same tasks.
const Module = require("module");
const path = require("path");
const assert = require("assert");

const store = {};
class DataApi {
    constructor() {}
    async getProtectedItems(projectId, playerId, keys) {
        return { data: { results: keys.filter(k => store[playerId + "/" + k] !== undefined)
            .map(k => ({ key: k, value: store[playerId + "/" + k], writeLock: "lock" })) } };
    }
    async setProtectedItem(projectId, playerId, item) { store[playerId + "/" + item.key] = item.value; }
}
const load = Module._load;
Module._load = function (request, parent, isMain) {
    if (request === "@unity-services/cloud-save-1.4") return { DataApi };
    return load.apply(this, arguments);
};
const wallet = require(path.join(__dirname, "..", "ugs", "cloud-code", "wallet.js"));
const rules = wallet.rules;

const GOLDEN = {
    daily: rules.tasksFor("player-1", 0, 20000).map(t => t.Id).join(","),
    weekly: rules.tasksFor("p", 1, 20000).map(t => t.Id).join(","),
};

(async () => {
    const ctx = { projectId: "x", playerId: "me" };
    const now = new Date().toISOString();
    store["me/careerProfile"] = JSON.stringify({ Characters: [{ Id: "zack" }, { Id: "maring" }], Slippers: [{ Id: "heels" }], Mastery: [] });
    const players = (score) => [{ PlayerId: "me", IsBot: false, Placement: 1, Throws: 5, Knockdowns: 3, Retrievals: 4, RetrievalsUnderPressure: 1, Tags: 2, ActiveRounds: 8 },
                                { PlayerId: "bot", IsBot: true, Placement: 2 }];
    const history = [
        { MatchId: "practice-classic", PlayedUtc: now, Mode: "Classic", Online: false, Rounds: 8, Players: players() },
        { MatchId: "practice-hero", PlayedUtc: now, Mode: "HeroStrike", Online: false, Rounds: 8, Players: players() },
        { MatchId: "m3-lan", PlayedUtc: now, Mode: "Classic", Online: true, Rounds: 8, Players: players() },
        { MatchId: "m2", PlayedUtc: now, Mode: "Classic", Online: true, Rounds: 8, Players: players() },
        { MatchId: "m1", PlayedUtc: now, Mode: "HeroStrike", Online: true, Ranked: true, Rounds: 8, Players: players() },
    ];
    store["me/matchHistory"] = JSON.stringify(history);

    let out = await wallet({ params: { action: "load" }, context: ctx });
    let w = JSON.parse(out.wallet);
    assert.ok(w.Owned.includes("hero:zack") && w.Owned.includes("slipper:heels"), "migration keeps played picks");
    assert.ok(!w.Owned.includes("hero:maring"), "classic people are not sold");
    assert.strictEqual(out.paid, 3 * (25 + 40), "only the three online first places pay");
    assert.strictEqual(w.EarnedToday, out.paid);
    assert.ok(!w.PaidMatchIds.includes("practice-classic") && !w.PaidMatchIds.includes("practice-hero"));
    assert.strictEqual(rules.earnedFrom(history[0], "me"), null);
    assert.strictEqual(rules.earnedFrom(history[1], "me"), null);
    const eligible = history.map(record => rules.earnedFrom(record, "me")).filter(Boolean);
    assert.strictEqual(rules.progress({ Period: 0, Stat: "Matches", Target: 10 }, eligible, rules.dayOf(Date.now())), 3);
    // The TEMPORARY PLAYTEST GRANT (wallet.js `PLAYTEST_TOPUP`) tops every loaded wallet up; 0 restores the real numbers.
    const topup = wallet.rules.PLAYTEST_TOPUP || 0;
    assert.strictEqual(w.Balance, Math.max(300 + 195, topup));

    out = await wallet({ params: { action: "load" }, context: ctx });
    assert.strictEqual(out.paid, 0, "settling twice pays once");

    out = await wallet({ params: { action: "buy", item: "hero:dante" }, context: ctx });
    assert.strictEqual(out.result, "owned");
    out = await wallet({ params: { action: "buy", item: "slipper:loafers" }, context: ctx });
    assert.strictEqual(out.result, "bought");
    assert.strictEqual(JSON.parse(out.wallet).Balance, Math.max(495, topup) - 300);
    out = await wallet({ params: { action: "buy", item: "hero:nemu" }, context: ctx });
    assert.strictEqual(out.result, topup > 0 ? "bought" : "poor");
    out = await wallet({ params: { action: "buy", item: "hero:maring" }, context: ctx });
    assert.strictEqual(out.result, "unknown");

    const board = JSON.parse(out.tasks);
    assert.strictEqual(board.length, 6);
    const done = board.find(t => t.Progress >= t.Target && !t.Claimed);
    if (done) {
        const before = JSON.parse(out.wallet).Balance;
        out = await wallet({ params: { action: "claim", task: done.Id }, context: ctx });
        assert.strictEqual(out.result, "claimed");
        assert.strictEqual(JSON.parse(out.wallet).Balance, before + done.Reward);
        out = await wallet({ params: { action: "claim", task: done.Id }, context: ctx });
        assert.strictEqual(out.result, "claimed-already");
    }
    out = await wallet({ params: { action: "claim", task: "nope" }, context: ctx });
    assert.strictEqual(out.result, "unassigned");

    assert.deepStrictEqual(Object.keys(wallet.params).sort(), ["action", "item", "task"]);
    assert.strictEqual(GOLDEN.daily, "d_tag2,d_classic1,d_fetch4", "must match EconomyTests");
    assert.strictEqual(GOLDEN.weekly, "w_play10,w_pressure5,w_win3", "must match EconomyTests");

    const matchRecord = require(path.join(__dirname, "..", "ugs", "cloud-code", "match-record.js"));
    const record = {
        MatchId: "queued-practice", Mode: "Classic", Online: false, Ranked: false,
        PlayedUtc: now, Rounds: 1, DurationSeconds: 90, WinningSlot: 0,
        DefenderByRound: [0], Players: [
            { Slot: 0, PlayerId: "me", IsBot: false, Score: 10, Placement: 1, ActiveRounds: 1,
              Throws: 0, Knockdowns: 0, Retrievals: 0 },
            { Slot: 1, PlayerId: "", IsBot: true, Score: 0, Placement: 2, ActiveRounds: 1,
              Throws: 0, Knockdowns: 0, Retrievals: 0 },
        ],
    };
    const profileBefore = store["me/careerProfile"];
    const historyBefore = store["me/matchHistory"];
    const offline = await matchRecord({ params: { action: "submit", record: JSON.stringify(record) }, context: ctx });
    assert.strictEqual(offline.applied, false, "old queued practice must be a successful no-op");
    assert.strictEqual(store["me/careerProfile"], profileBefore, "practice changed protected career");
    assert.strictEqual(store["me/matchHistory"], historyBefore, "practice entered protected history");

    record.MatchId = "eligible-after-practice";
    record.Online = true;
    const eligibleSubmit = await matchRecord({ params: { action: "submit", record: JSON.stringify(record) }, context: ctx });
    assert.strictEqual(eligibleSubmit.applied, true, "practice refusal blocked the next eligible record");
    assert.ok(JSON.parse(store["me/matchHistory"]).some(row => row.MatchId === record.MatchId));
    console.log("wallet.js: all checks passed");
})().catch(e => { console.error(e); process.exit(1); });
