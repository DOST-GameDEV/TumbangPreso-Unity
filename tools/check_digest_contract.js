// Asserts that `ugs/cloud-code/match-record.js` hashes the reference match record to exactly the
// string `Core.Tests/DigestContractTests.ReferenceDigest` pins, so the C# and the deployed
// endpoint cannot silently disagree about what a result IS.
//
// ⚠️⚠️ A DISAGREEMENT HERE IS INVISIBLE IN PRODUCTION AND IS FATAL TO PHASE 9. Every submission
// would carry a witness digest the endpoint computes differently, every match would read as
// disputed, no rating would ever move, and nothing anywhere would log an error. That is the same
// failure shape as `docs/TODO.md` § 90.5, where no career had ever reached the server and every
// probe was green throughout.
//
// ⚠️ IT READS THE REAL SCRIPT RATHER THAN A COPY OF THE TWO FUNCTIONS. A copy is a third
// implementation and would be the one that stays correct while the deployed one drifts.
//
//   node tools/check_digest_contract.js
//
// Exits non-zero on a mismatch, so it can gate a verification pass beside the three python audits.

const fs = require("fs");
const path = require("path");
const vm = require("vm");

const REPO = path.resolve(__dirname, "..");
const SCRIPT = path.join(REPO, "ugs", "cloud-code", "match-record.js");
const CONTRACT = path.join(REPO, "Core.Tests", "DigestContractTests.cs");

// ⚠️ THE `require` OF THE CLOUD SAVE SDK IS STUBBED, NOT INSTALLED. This check is about two pure
// functions and pulling a Unity service package into the repository to reach them would be a
// dependency nobody else needs.
const sandbox = {
    require: name => {
        if (String(name).indexOf("cloud-save") >= 0) return { DataApi: function () {} };
        throw new Error("unexpected require: " + name);
    },
    module: { exports: {} },
    console,
    Date,
    Math,
    JSON,
    BigInt,
    Number,
    String,
    Array,
    Object,
    isFinite,
};
sandbox.exports = sandbox.module.exports;
sandbox.globalThis = sandbox;

vm.createContext(sandbox);
vm.runInContext(fs.readFileSync(SCRIPT, "utf8"), sandbox, { filename: SCRIPT });

// ⚠️ THE FUNCTIONS ARE MODULE-SCOPED, so they are read out of the context rather than off
// `module.exports`. Exporting them purely for this check would change the deployed script's shape
// to suit a test, which is the wrong direction.
const digest = vm.runInContext("digest", sandbox);
const canonical = vm.runInContext("canonical", sandbox);

if (typeof digest !== "function" || typeof canonical !== "function") {
    console.error("FAIL: match-record.js no longer defines canonical() and digest()");
    process.exit(1);
}

// The same fixture as `DigestContractTests.Reference()`, field for field.
const reference = {
    MatchId: "ref-2026-08-31",
    Mode: "HeroStrike",
    MapId: "ilalim_ng_tulay",
    Rounds: 8,
    DurationSeconds: 812.5,
    PlayedUtc: "2026-08-31T12:34:56Z",
    Ranked: true,
    WinningSlot: 0,
    Players: [
        { Slot: 0, PlayerId: "aaa", CharacterId: "dante", Score: 1450, Placement: 1, IsBot: false },
        { Slot: 1, PlayerId: "bbb", CharacterId: "cheska", Score: 1100, Placement: 2, IsBot: false },
        { Slot: 2, PlayerId: "", CharacterId: "sean", Score: 900, Placement: 3, IsBot: true },
        { Slot: 3, PlayerId: "ddd", CharacterId: "zack", Score: 700, Placement: 4, IsBot: false },
    ],
};

// ⚠️ THE EXPECTED VALUE IS READ OUT OF THE C# TEST rather than typed here as well. Two literals
// are two things that can drift, and the one in the test is the one a person froze.
const source = fs.readFileSync(CONTRACT, "utf8");
const match = source.match(/ReferenceDigest\s*=\s*"([0-9a-f]{16})"/);

if (!match) {
    console.error("FAIL: could not read ReferenceDigest out of " + CONTRACT);
    process.exit(1);
}

const expected = match[1];
const actual = digest(reference);

console.log("canonical: " + canonical(reference));
console.log("expected:  " + expected);
console.log("actual:    " + actual);

if (actual !== expected) {
    console.error("");
    console.error("FAIL: the Cloud Code digest and the C# digest disagree.");
    console.error("Every match would read as disputed and no rating would ever move.");
    console.error("Fix BOTH implementations and then the literal, in one commit.");
    process.exit(1);
}

console.log("");
console.log("RESULT: OK, the two implementations agree.");
