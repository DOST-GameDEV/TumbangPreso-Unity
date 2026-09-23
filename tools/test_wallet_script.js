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
    store["me/matchHistory"] = JSON.stringify([
        { MatchId: "m2", PlayedUtc: now, Mode: "Classic", Rounds: 8, Players: players() },
        { MatchId: "m1", PlayedUtc: now, Mode: "HeroStrike", Rounds: 8, Players: players() },
    ]);

    let out = await wallet({ params: { action: "load" }, context: ctx });
    let w = JSON.parse(out.wallet);
    assert.ok(w.Owned.includes("hero:zack") && w.Owned.includes("slipper:heels"), "migration keeps played picks");
    assert.ok(!w.Owned.includes("hero:maring"), "classic people are not sold");
    assert.strictEqual(out.paid, 2 * (25 + 40), "two first places pay");
    assert.strictEqual(w.Balance, 300 + 130);

    out = await wallet({ params: { action: "load" }, context: ctx });
    assert.strictEqual(out.paid, 0, "settling twice pays once");

    out = await wallet({ params: { action: "buy", item: "hero:dante" }, context: ctx });
    assert.strictEqual(out.result, "owned");
    out = await wallet({ params: { action: "buy", item: "slipper:loafers" }, context: ctx });
    assert.strictEqual(out.result, "bought");
    assert.strictEqual(JSON.parse(out.wallet).Balance, 430 - 300);
    out = await wallet({ params: { action: "buy", item: "hero:nemu" }, context: ctx });
    assert.strictEqual(out.result, "poor");
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
    console.log("wallet.js: all checks passed");
})().catch(e => { console.error(e); process.exit(1); });
