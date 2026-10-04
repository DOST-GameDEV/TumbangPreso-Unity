// Actual social endpoint with two isolated local accounts and a Cloud Save stand-in.
// node tools/test_social_flow.js
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const source = fs.readFileSync(path.join(__dirname, '../ugs/cloud-code/social.js'), 'utf8');
const empty = () => ({ Friends: [], Incoming: [], Outgoing: [], Blocked: [] });

function fixture() {
    const data = new Map();
    const writes = [];
    class DataApi {
        async getProtectedItems(project, player, keys) {
            return { data: { results: keys.filter(key => data.has(player + '/' + key))
                .map(key => ({ key, value: data.get(player + '/' + key) })) } };
        }
        async setProtectedItem(project, player, item) {
            writes.push(player + '/' + item.key);
            data.set(player + '/' + item.key, item.value);
        }
    }
    const sandbox = { module: { exports: {} }, require: name => {
        assert.match(name, /^@unity-services\/cloud-save-/);
        return { DataApi };
    } };
    vm.createContext(sandbox);
    vm.runInContext(source, sandbox);
    return {
        writes,
        list: who => JSON.parse(data.get(who + '/socialList') || JSON.stringify(empty())),
        seed: (who, list) => data.set(who + '/socialList', JSON.stringify(list)),
        call: (who, action, subject, extra = {}) => sandbox.module.exports({
            params: { action, playerId: subject, handle: who.toUpperCase() + '#1111',
                      theirHandle: (subject || '').toUpperCase() + '#2222', ...extra },
            context: { projectId: 'local', playerId: who, serviceToken: 'local-stand-in' },
        }),
    };
}
function full() {
    const list = empty();
    list.Friends = Array.from({ length: 100 }, (_, i) => ({ PlayerId: 'existing-' + i, Handle: 'OLD#1111' }));
    return list;
}
function mutualFriends(f) {
    assert.equal(f.list('alice').Friends.filter(row => row.PlayerId === 'bob').length, 1);
    assert.equal(f.list('bob').Friends.filter(row => row.PlayerId === 'alice').length, 1);
    for (const who of ['alice', 'bob']) {
        assert.equal(f.list(who).Incoming.length, 0);
        assert.equal(f.list(who).Outgoing.length, 0);
    }
}
const tests = [
    ['request, accept and reload persist both accounts', async () => {
        const f = fixture();
        await f.call('alice', 'request', 'bob');
        assert.equal(f.list('alice').Outgoing[0].PlayerId, 'bob');
        assert.equal(f.list('bob').Incoming[0].PlayerId, 'alice');
        await f.call('bob', 'accept', 'alice');
        mutualFriends(f);
        for (const who of ['alice', 'bob']) {
            const answer = await f.call(who, 'load');
            assert.deepEqual(JSON.parse(answer.list), f.list(who));
        }
    }],
    ['crossed requests resolve into mutual friendship', async () => {
        const f = fixture();
        await f.call('alice', 'request', 'bob');
        await f.call('bob', 'request', 'alice');
        mutualFriends(f);
    }],
    ['duplicate request and acceptance do not duplicate friends', async () => {
        const f = fixture();
        await f.call('alice', 'request', 'bob');
        await f.call('alice', 'request', 'bob');
        assert.equal(f.list('bob').Incoming.length, 1);
        await f.call('bob', 'accept', 'alice');
        await f.call('bob', 'accept', 'alice');
        mutualFriends(f);
    }],
    ['decline clears both pending sides without friendship', async () => {
        const f = fixture();
        await f.call('alice', 'request', 'bob');
        await f.call('bob', 'decline', 'alice');
        for (const who of ['alice', 'bob']) assert.deepEqual(f.list(who), empty());
    }],
    ['remove clears both saved friendships', async () => {
        const f = fixture();
        await f.call('alice', 'request', 'bob');
        await f.call('bob', 'accept', 'alice');
        await f.call('alice', 'remove', 'bob');
        for (const who of ['alice', 'bob']) assert.deepEqual(f.list(who), empty());
    }],
    ['a full accepting account retains its pending request', async () => {
        const f = fixture();
        await f.call('alice', 'request', 'bob');
        const list = full(); list.Incoming = f.list('bob').Incoming; f.seed('bob', list);
        const before = f.writes.length;
        await assert.rejects(f.call('bob', 'accept', 'alice'), /friends list is full/);
        assert.equal(f.writes.length, before);
        assert.equal(f.list('bob').Incoming.length, 1);
        assert.equal(f.list('alice').Outgoing.length, 1);
    }],
    ['a requester who filled their list cannot become a one-way friend', async () => {
        const f = fixture();
        await f.call('alice', 'request', 'bob');
        const list = full(); list.Outgoing = f.list('alice').Outgoing; f.seed('alice', list);
        const before = f.writes.length;
        await assert.rejects(f.call('bob', 'accept', 'alice'), /friends list is full/);
        assert.equal(f.writes.length, before);
        assert.equal(f.list('bob').Friends.length, 0);
        assert.equal(f.list('bob').Incoming.length, 1);
    }],
    ['crossed requests respect the other full account too', async () => {
        const f = fixture();
        await f.call('alice', 'request', 'bob');
        const list = full(); list.Outgoing = f.list('alice').Outgoing; f.seed('alice', list);
        const before = f.writes.length;
        await assert.rejects(f.call('bob', 'request', 'alice'), /friends list is full/);
        assert.equal(f.writes.length, before);
        assert.equal(f.list('bob').Incoming.length, 1);
        assert.equal(f.list('bob').Friends.length, 0);
    }],
];
(async () => {
    let failures = 0;
    for (const [name, test] of tests) {
        try { await test(); console.log('PASS ' + name); }
        catch (error) { failures++; console.error('FAIL ' + name + ': ' + error.message); }
    }
    console.log(JSON.stringify({ passed: tests.length - failures, failed: failures, scope: 'actual endpoint; local two-account Cloud Save' }));
    process.exitCode = failures ? 1 : 0;
})();
