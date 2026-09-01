const { DataApi } = require("@unity-services/cloud-save-1.4");

const KEY = "accountProfile";
const PROOF_KEY = "handleProof";

function oneLine(value, max) {
    return String(value || "").replace(/[\u0000-\u001f\u007f]/g, "").replace(/\s+/g, " ").trim().slice(0, max);
}

// ⚠️ THESE TWO MIRROR `AccountRules.DisplayNameMin/Max`, WHICH IS `Balance.PlayerNameMax`.
// This file cannot import the C# constant, so it is the one place the number is written twice.
// It read 16 here while the client clamped to 14, which is not a harmless difference: the
// server is the authority, so it would store a 15-character name that every client then
// silently clipped, and the stored profile stopped matching the name on the scoreboard.
// If `Balance.PlayerNameMax` ever moves, this line moves in the same commit.
const DISPLAY_NAME_MIN = 3;
const DISPLAY_NAME_MAX = 14;

// ⚠️ MIRRORS `AccountRules.HandleProofMinutes`. `CareerAndCloudCodeTests` fails if they split.
const PROOF_MINUTES = 10;

// ⚠️⚠️ THIS MIRRORS `AccountRules.Discriminator` AND IT IS THE REASON THE IMPERSONATION GUARD
// CAN EXIST AT ALL. `docs/TODO.md` § 88.1c wrote the blocker down as "the tag of a real account
// is allocated by UGS Player Names, so the host cannot recompute it". The tag is now derived
// from the stable player id instead, here and in the core, which means:
//
//   1. THE SERVER OWNS IT. `save` below ignores whatever discriminator a client sends. A client
//      that could write its own tag could write somebody else's, attest to it, and the whole
//      guard would prove a lie for them.
//   2. It needs no storage and no allocator, so a verification is a pure function of the id.
//
// ⚠️ FNV-1a IS USED FOR STABILITY, NOT SECURITY, which is the same note the C# carries. The
// loop walks UTF-16 code units by index rather than `for...of`, because `for...of` walks code
// POINTS and would disagree with C#'s `foreach (char c in ...)` on any astral character.
function derivedTag(playerId) {
    let hash = 2166136261 >>> 0;
    const id = String(playerId || "");
    for (let i = 0; i < id.length; i++) {
        hash = (hash ^ id.charCodeAt(i)) >>> 0;
        hash = Math.imul(hash, 16777619) >>> 0;
    }
    return String(hash % 10000).padStart(4, "0");
}

function profileFrom(raw, playerId) {
    const p = raw || {};
    const displayName = oneLine(p.DisplayName, DISPLAY_NAME_MAX);
    if (displayName.length < DISPLAY_NAME_MIN || !/^[\p{L}\p{N} _.-]+$/u.test(displayName)) {
        throw new Error("invalid display name");
    }

    return {
        PlayerId: playerId,
        Username: oneLine(p.Username, 64),
        DisplayName: displayName,

        // ⚠️ DERIVED, NEVER ACCEPTED FROM THE CALLER. This line used to read the client's value
        // back if it was four digits, which made the tag a thing a client asserts rather than a
        // thing an account has. See `derivedTag` above.
        Discriminator: derivedTag(playerId),
        Bio: oneLine(p.Bio, 140),
        Country: /^[A-Za-z]{2}$/.test(String(p.Country || "")) ? String(p.Country).toUpperCase() : "",
        Pronouns: oneLine(p.Pronouns, 32),
        Email: "",
        CreatedUtc: oneLine(p.CreatedUtc, 40),
    };
}

function handleOf(profile) {
    if (!profile || !profile.DisplayName) return "";
    return profile.DisplayName + "#" + derivedTag(profile.PlayerId);
}

function normalHandle(value) {
    return oneLine(value, DISPLAY_NAME_MAX + 5).toLocaleLowerCase("en-US");
}

function indexKey(handle) {
    let hash = 2166136261 >>> 0;
    const text = normalHandle(handle);
    for (let i = 0; i < text.length; i++) {
        hash = (hash ^ text.charCodeAt(i)) >>> 0;
        hash = Math.imul(hash, 16777619) >>> 0;
    }
    return "h" + hash.toString(16).padStart(8, "0");
}

function indexId(handle) {
    const split = String(handle || "").lastIndexOf("#");
    const tag = split >= 0 ? String(handle).slice(split + 1) : "00";
    return "handle-index-" + (/^\d{4}$/.test(tag) ? tag.slice(0, 2) : "00");
}

function serviceStore(context) {
    if (!context.serviceToken) throw new Error("handle index unavailable");
    return new DataApi({ accessToken: context.serviceToken });
}

// ⚠️⚠️ THE INDEX IS BEST-EFFORT ON A WRITE AND MUST NEVER FAIL A `save` OR A `delete`.
// § 94.1 is the receipt for why this matters more than it looks: a `save` that throws is a
// profile that does not reach the server, and `CareerStore.FlushAsync` stops at the first
// failure, so ONE refused write wedges the queue behind it permanently. Losing an index row
// costs one player one failed search until their next save rewrites it; losing a save costs
// them their account. `resolve` re-reads the target's protected profile anyway, so a missing
// row and a stale row both degrade to NOT FOUND rather than to a wrong answer.
async function tryIndex(logger, work) {
    try {
        await work();
    } catch (e) {
        if (logger) logger.warning("handle index write skipped: " + (e && e.message));
    }
}

async function putHandleIndex(store, projectId, profile) {
    const handle = handleOf(profile);
    if (!handle) return;
    await store.setCustomItem(projectId, indexId(handle), {
        key: indexKey(handle),
        value: JSON.stringify({ PlayerId: profile.PlayerId, Handle: handle }),
    });
}

async function deleteHandleIndex(store, projectId, profile) {
    const handle = handleOf(profile);
    if (!handle) return;
    try {
        await store.deleteCustomItem(indexKey(handle), projectId, indexId(handle));
    } catch (e) {
        // Deleting a missing stale row is idempotent. A later resolve validates the target's
        // live profile as well, so an undeleted row can never address the wrong player.
    }
}

function readProfile(item) {
    if (!item || !item.value) return null;
    try {
        return typeof item.value === "string" ? JSON.parse(item.value) : item.value;
    } catch (e) {
        return null;
    }
}

// ⚠️ A PROOF IS A SHORT-LIVED CAPABILITY, NOT A SIGNATURE. Cloud Code's runtime does not offer
// a hashing primitive this script can rely on, so the proof is a random value stored beside the
// account and checked by reading it back, which needs nothing but Cloud Save. What it buys the
// holder is one read-only question about the player who minted it; it writes nothing, it names
// no other player, and it expires. That is the whole blast radius of handing it to a stranger,
// which is exactly what a peer-hosted lobby asks a player to do.
function mintProof() {
    let value = "";
    for (let i = 0; i < 4; i++) value += Math.random().toString(36).slice(2, 12);
    return value + Date.now().toString(36);
}

module.exports = async ({ params, context, logger }) => {
    const api = new DataApi(context);
    const { projectId, playerId } = context;
    const action = String(params.action || "load");

    if (action === "load") {
        const response = await api.getProtectedItems(projectId, playerId, [KEY]);
        const item = response.data.results.find(x => x.key === KEY);
        return { profile: item && item.value ? String(item.value) : "" };
    }

    if (action === "save") {
        const previousResponse = await api.getProtectedItems(projectId, playerId, [KEY]);
        const previous = readProfile(previousResponse.data.results.find(x => x.key === KEY));
        const parsed = JSON.parse(String(params.profile || "{}"));
        const profile = profileFrom(parsed, playerId);
        await api.setProtectedItem(projectId, playerId, { key: KEY, value: JSON.stringify(profile) });
        await tryIndex(logger, async () => {
            const store = serviceStore(context);
            await putHandleIndex(store, projectId, profile);
            if (previous && normalHandle(handleOf(previous)) !== normalHandle(handleOf(profile)))
                await deleteHandleIndex(store, projectId, previous);
        });
        return { profile: JSON.stringify(profile), handle: handleOf(profile) };
    }

    if (action === "delete") {
        const previousResponse = await api.getProtectedItems(projectId, playerId, [KEY]);
        const previous = readProfile(previousResponse.data.results.find(x => x.key === KEY));
        if (previous)
            await tryIndex(logger, () =>
                deleteHandleIndex(serviceStore(context), projectId, previous));
        // Authentication deletion is the account-level deletion. Clearing the protected value
        // first makes this endpoint idempotent even when the auth request is retried later.
        await api.setProtectedItem(projectId, playerId, { key: KEY, value: "" });
        await api.setProtectedItem(projectId, playerId, { key: PROOF_KEY, value: "" });
        return { profile: "" };
    }

    if (action === "resolve") {
        const wanted = oneLine(params.handle, DISPLAY_NAME_MAX + 5);
        if (!/^.{3,14}#\d{4}$/u.test(wanted)) return { playerId: "", handle: "" };

        const store = serviceStore(context);
        const response = await store.getCustomItems(projectId, indexId(wanted), [indexKey(wanted)]);
        const row = readProfile(response.data.results.find(x => x.key === indexKey(wanted)));
        if (!row || !row.PlayerId) return { playerId: "", handle: "" };

        // ⚠️⚠️ THE INDEX IS A ROUTE, NEVER THE AUTHORITY. A rename can crash after writing the
        // new row and before deleting the old one. Re-read the target's protected profile and
        // require the exact current handle, so stale data answers NOT FOUND rather than sending a
        // friend request to the wrong account.
        const subject = await store.getProtectedItems(projectId, String(row.PlayerId), [KEY]);
        const profile = readProfile(subject.data.results.find(x => x.key === KEY));
        const current = handleOf(profile);
        if (normalHandle(current) !== normalHandle(wanted)) return { playerId: "", handle: "" };
        return { playerId: String(row.PlayerId), handle: current };
    }

    // ⚠️⚠️ `attest` AND `verify` ARE THE TWO HALVES OF THE IMPERSONATION GUARD, § 88.1c.
    //
    // A peer-hosted lobby cannot check a handle on its own: the host is another player and has
    // no way to tell a genuine `Maria Clara#4417` from a claimed one. The obvious fix, asking
    // this endpoint whether a player id owns a handle, does not work by itself, because the
    // player id is also just a string the client sent and a liar spoofs both together.
    //
    // So the OWNER asks for a proof from its own authenticated session (`attest`), hands the
    // proof to the host with its claim, and the host asks whether that proof belongs to that
    // player (`verify`). The host learns one boolean and one handle. It never sees a token that
    // could act as the player, which rules out the obvious alternative of shipping the peer's
    // own bearer token to whoever is hosting.
    if (action === "attest") {
        const response = await api.getProtectedItems(projectId, playerId, [KEY]);
        const profile = readProfile(response.data.results.find(x => x.key === KEY));
        const handle = handleOf(profile);

        // Nothing saved yet means there is no account handle to prove. The client is told so and
        // simply arrives with a claim, which lands on the LAN path in `VerifiedArrivalHandle`.
        if (!handle) return { handle: "", proof: "", expires: "" };

        const expires = new Date(Date.now() + PROOF_MINUTES * 60000).toISOString();
        const proof = mintProof();
        await api.setProtectedItem(projectId, playerId, {
            key: PROOF_KEY,
            value: JSON.stringify({ Proof: proof, Expires: expires, Handle: handle }),
        });
        return { handle, proof, expires };
    }

    if (action === "verify") {
        const subject = String(params.playerId || "");
        const proof = String(params.proof || "");
        if (!subject || !proof) return { owned: false, handle: "" };

        // ⚠️ THE SERVICE TOKEN IS NEEDED HERE AND ONLY HERE. Every other action reads and writes
        // the CALLER's own document, which the caller's own token covers. This one reads a
        // different player's, which is what makes it a verification rather than a self-report.
        // ⚠️ IT ANSWERS ONLY `owned` AND `handle`. Neither is a secret: the handle is what that
        // player is about to be called on the scoreboard, and the caller had to already hold a
        // live proof minted by that player to ask at all.
        let store;
        try {
            store = new DataApi({ accessToken: context.serviceToken });
        } catch (e) {
            // A missing service token must read as "could not check", never as "does not own it".
            // `AccountRules.HandleCheck.Unreachable` falls through to the claim; `NotOwned` takes
            // somebody's tag away. Throwing is the safe direction.
            throw new Error("verification unavailable");
        }

        const stored = await store.getProtectedItems(projectId, subject, [PROOF_KEY, KEY]);
        const held = readProfile(stored.data.results.find(x => x.key === PROOF_KEY));
        if (!held || held.Proof !== proof) return { owned: false, handle: "" };
        if (!held.Expires || Date.parse(held.Expires) < Date.now()) return { owned: false, handle: "" };

        // The handle is recomputed from the profile rather than read out of the proof, so a
        // rename between minting and joining shows the new name rather than the old one.
        const profile = readProfile(stored.data.results.find(x => x.key === KEY));
        const handle = handleOf(profile) || String(held.Handle || "");
        return { owned: handle.length > 0, handle };
    }

    throw new Error("unknown account action");
};

// ⚠️⚠️ EVERY PARAMETER A SCRIPT USES MUST BE DECLARED HERE OR CLOUD CODE STRIPS IT, AND THE
// FAILURE IS SILENT AND LOOKS LIKE A WORKING ENDPOINT. Measured on 2026-08-30: this endpoint
// answered a call carrying an action with the payload of the branch an ABSENT action falls
// through to. Nothing errors, nothing logs, and the answer is well-formed, so a probe that
// only asserts "it answered" passes. `docs/TODO.md` § 90.5 is the entry.
//
// ⚠️ IF A NEW ACTION NEEDS A NEW PARAMETER, IT GOES IN THIS BLOCK IN THE SAME EDIT.
module.exports.params = {
    action: "String",
    profile: "String",

    // `verify` asks about ANOTHER player, so the subject id and the proof are its own two
    // parameters rather than being read off the session. See the `verify` branch for why
    // that is safe: without a live proof minted by that player, it answers false.
    playerId: "String",
    proof: "String",
    handle: "String",
};
