// Exercise the actual server rating functions, including confidence and season credit.
// Optional source path permits checking an exported production script before deployment.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const sandbox = { module: { exports: {} }, require: name => {
    assert.match(name, /^@unity-services\/cloud-save-/);
    return { DataApi: function () {} };
} };
vm.createContext(sandbox);
vm.runInContext(fs.readFileSync(process.argv[2] || path.join(__dirname, '../ugs/cloud-code/match-record.js'), 'utf8'), sandbox);
const tests = [
    ['human seat weights', () => {
        const weight = vm.runInContext('botWeight', sandbox);
        for (const [humans, seats, expected] of [[0,4,0],[1,4,0],[2,4,1/3],[3,4,2/3],[4,4,1],[7,4,1],[2,1,0]])
            assert.equal(weight(humans, seats), expected);
    }],
    ['solo bot match changes neither rating nor confidence nor season credit', () => {
        const blend = vm.runInContext('blendRank', sandbox);
        const before = { Rating: 1500, Deviation: 200, Volatility: .06, MatchesThisSeason: 2, FloorTier: 0 };
        const after = { Rating: 1560, Deviation: 170, Volatility: .05, MatchesThisSeason: 3, FloorTier: 0 };
        assert.equal(JSON.stringify(blend(before, after, 0)), JSON.stringify(before));
    }],
    ['two human match scales gains and confidence together', () => {
        const blend = vm.runInContext('blendRank', sandbox);
        const before = { Rating: 1500, Deviation: 200, Volatility: .06, MatchesThisSeason: 2, FloorTier: 0 };
        const after = { Rating: 1560, Deviation: 170, Volatility: .03, MatchesThisSeason: 3, FloorTier: 0 };
        const result = blend(before, after, 1/3);
        assert.equal(result.Rating, 1520);
        assert.equal(result.Deviation, 190);
        assert.ok(Math.abs(result.Volatility - .05) < 1e-12);
        assert.equal(result.MatchesThisSeason, 3);
        assert.equal(before.Rating, 1500);
    }],
    ['two human loss is reduced and a full human match is unchanged', () => {
        const blend = vm.runInContext('blendRank', sandbox);
        const before = { Rating: 1500, Deviation: 200, Volatility: .06, MatchesThisSeason: 2, FloorTier: 0 };
        const after = { Rating: 1440, Deviation: 170, Volatility: .03, MatchesThisSeason: 3, FloorTier: 0 };
        assert.equal(blend(before, after, 1/3).Rating, 1480);
        assert.equal(blend(before, after, 1), after);
    }],
];
(async () => {
    let failed = 0;
    for (const [name, test] of tests) {
        try { test(); console.log('PASS ' + name); }
        catch (error) { failed++; console.error('FAIL ' + name + ': ' + error.message); }
    }
    console.log(JSON.stringify({ passed: tests.length - failed, failed, scope: 'actual server rating functions; no live account data' }));
    process.exitCode = failed ? 1 : 0;
})();
