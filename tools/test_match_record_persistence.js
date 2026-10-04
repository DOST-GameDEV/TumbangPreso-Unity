// Exercise the actual submit endpoint with isolated local Cloud Save failures.
// node tools/test_match_record_persistence.js
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const source = fs.readFileSync(path.join(__dirname, '../ugs/cloud-code/match-record.js'), 'utf8');

function fixture() {
    const store = new Map();
    const writes = [];
    let failure = null;
    class DataApi {
        async getProtectedItems(project, player, keys) {
            return { data: { results: keys.filter(key => store.has(player + '/' + key))
                .map(key => ({ key, value: store.get(player + '/' + key) })) } };
        }
        async setProtectedItem(project, player, item) {
            writes.push(item.key);
            if (failure === item.key) {
                failure = null;
                throw new Error('injected write failure: ' + item.key);
            }
            store.set(player + '/' + item.key, item.value);
        }
    }
    const sandbox = { module: { exports: {} }, require: name => {
        assert.match(name, /^@unity-services\/cloud-save-/);
        return { DataApi };
    } };
    vm.createContext(sandbox);
    vm.runInContext(source, sandbox);
    const record = {
        MatchId: 'durable-match', Mode: 'Classic', MapId: 'eskinita', Rounds: 1,
        DurationSeconds: 30, PlayedUtc: '2026-10-04T00:00:00Z', WinningSlot: 0,
        Online: true, Ranked: false,
        Players: [{ Slot: 0, PlayerId: 'me', CharacterId: 'dante', Score: 100,
                    Throws: 1, Knockdowns: 1, Retrievals: 1, ActiveRounds: 1,
                    TimeToFirstThrow: 2, IsBot: false }],
    };
    return {
        record, store, writes,
        fail: key => { failure = key; },
        read: (key, fallback) => JSON.parse(store.get('me/' + key) || JSON.stringify(fallback)),
        submit: () => sandbox.module.exports({
            params: { action: 'submit', record: JSON.stringify(record) },
            context: { projectId: 'local', playerId: 'me' }, logger: { warning() {} },
        }),
    };
}

function countedOnce(f) {
    const profile = f.read('careerProfile', {});
    assert.deepEqual(profile.AppliedMatchIds, [f.record.MatchId]);
    assert.equal(profile.Modes[0].Totals.Matches, 1);
    const history = f.read('matchHistory', []);
    assert.equal(history.filter(record => record.MatchId === f.record.MatchId).length, 1);
    return profile;
}

const tests = [
    ['history write failure is recovered by resubmission', async () => {
        const f = fixture();
        f.fail('matchHistory');
        await assert.rejects(f.submit(), /injected write failure: matchHistory/);
        await f.submit();
        countedOnce(f);
    }],
    ['profile write failure retries without duplicate history or reward', async () => {
        const f = fixture();
        f.fail('careerProfile');
        await assert.rejects(f.submit(), /injected write failure: careerProfile/);
        await f.submit();
        countedOnce(f);
    }],
    ['ordinary duplicate keeps rewards and history once', async () => {
        const f = fixture();
        const first = await f.submit();
        const xp = countedOnce(f).Xp;
        const second = await f.submit();
        assert.equal(first.applied, true);
        assert.equal(second.applied, false);
        assert.equal(countedOnce(f).Xp, xp);
        assert.equal(f.writes.filter(key => key === 'matchHistory').length, 1);
    }],
    ['new history preserves an existing different match', async () => {
        const f = fixture();
        f.store.set('me/matchHistory', JSON.stringify([{ MatchId: 'earlier-match' }]));
        await f.submit();
        countedOnce(f);
        assert.equal(f.read('matchHistory', []).length, 2);
        assert.equal(f.read('matchHistory', [])[1].MatchId, 'earlier-match');
    }],
    ['offline record remains uncounted and unwritten', async () => {
        const f = fixture();
        f.record.Online = false;
        const result = await f.submit();
        assert.equal(result.verdict, 'offline');
        assert.equal(result.applied, false);
        assert.equal(f.writes.length, 0);
    }],
];

(async () => {
    let failures = 0;
    for (const [name, test] of tests) {
        try { await test(); console.log('PASS ' + name); }
        catch (error) { failures++; console.error('FAIL ' + name + ': ' + error.message); }
    }
    console.log(JSON.stringify({ passed: tests.length - failures, failed: failures, scope: 'actual endpoint; local Cloud Save stand-in' }));
    process.exitCode = failures ? 1 : 0;
})();
