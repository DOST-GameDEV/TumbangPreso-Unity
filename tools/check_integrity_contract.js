// Run the actual Cloud Code sanity check at the current Core travel boundary.
// No service, SDK installation or account data is used.
// node tools/check_integrity_contract.js [actual-Core-results.json]
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const root = path.resolve(__dirname, '..');
const balance = fs.readFileSync(path.join(root, 'Packages/com.tumbangpreso.core/Runtime/Balance.cs'), 'utf8');
function constant(name) {
    const match = balance.match(new RegExp('public const float ' + name + ' = ([0-9.]+)f;'));
    assert.ok(match, 'Missing Core balance constant: ' + name);
    return Number(match[1]);
}
const sandbox = {
    module: { exports: {} },
    require: name => {
        assert.match(name, /^@unity-services\/cloud-save-/);
        return { DataApi: function () {} };
    },
};
vm.createContext(sandbox);
vm.runInContext(fs.readFileSync(path.join(root, 'ugs/cloud-code/match-record.js'), 'utf8'), sandbox);
const check = vm.runInContext('sanityFault', sandbox);
const duration = 30;
const ceiling = constant('DefenderRunSpeed') * (duration + constant('RoundTime')) * 2;
const cases = [
    { name: 'stationary', distance: 0, expected: '' },
    { name: 'ordinary travel', distance: 1000, expected: '' },
    { name: 'legacy ceiling', distance: 1656, expected: '' },
    { name: 'above legacy ceiling', distance: 1700, expected: '' },
    { name: 'current ceiling', distance: ceiling, expected: '' },
    { name: 'above current ceiling', distance: ceiling + 1, expected: 'ImpossibleTravel' },
];
const actualCore = process.argv[2] ? JSON.parse(fs.readFileSync(process.argv[2], 'utf8')) : null;
if (actualCore) assert.equal(actualCore.length, cases.length, 'Core must supply every boundary case');
let failures = 0;
for (let i = 0; i < cases.length; i++) {
    const test = cases[i];
    const record = {
        MatchId: 'travel-contract', Rounds: 1, DurationSeconds: duration,
        Players: [{ Slot: 0, PlayerId: 'local-fixture', Score: 0, Placement: 1,
                    Throws: 0, Knockdowns: 0, Retrievals: 0, ShoveAttempts: 0,
                    ShoveHits: 0, LungeAttempts: 0, LungeHits: 0, DefenceTicks: 0,
                    DistanceTravelled: test.distance }],
    };
    const result = check(record);
    if (actualCore) {
        assert.equal(actualCore[i].distance, test.distance);
        assert.equal(actualCore[i].fault, test.expected || 'None', 'Actual Core: ' + test.name);
    }
    if (result !== test.expected) {
        failures++;
        console.error('FAIL ' + test.name + ': expected ' + (test.expected || 'accepted') + ', got ' + result);
    } else console.log('PASS ' + test.name);
}
console.log(JSON.stringify({ passed: cases.length - failures, failed: failures, actualCoreCompared: !!actualCore }));
process.exitCode = failures ? 1 : 0;
