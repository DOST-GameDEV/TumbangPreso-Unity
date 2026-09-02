const { DataApi } = require("@unity-services/cloud-save-1.4");

// ⚠️⚠️ TWO KEYS PER PLAYER AND NOT ONE. `LIST_KEY` is written by the owner and by anybody who
// sends them a friend request, so it is contended; `PRESENCE_KEY` is written by the owner alone
// every 60 s. Keeping them apart means a heartbeat can never clobber a list, which is exactly
// the race that would otherwise eat a friend request every minute.
const LIST_KEY = "socialList";
const PRESENCE_KEY = "presence";

// ⚠️⚠️ THESE MIRROR `SocialRules` IN THE CORE. This file cannot import a C# constant, so these
// are the places the numbers are written twice; `SocialAndCloudCodeTests` compares them against
// the file on disk and fails if they split. `docs/TODO.md` § 102.
const MAX_FRIENDS = 100;
const MAX_BLOCKED = 100;
const MAX_PENDING = 20;
const PRESENCE_STALE_SECONDS = 180;

// ⚠️⚠️ HOW MANY FRIENDS ONE `load` WILL LOOK UP THE PRESENCE OF. Cloud Save is keyed per player,
// so there is no "read these thirty documents" call: it is thirty reads, server-side, inside one
// invocation the client pays one round trip for. **The cap is what stops a hundred-friend account
// turning one press into a hundred reads**, and the rail draws far fewer than this. Friends past
// it are still listed, they are just drawn from their last known state until they move up.
const PRESENCE_FANOUT = 30;

// ⚠️ MIRRORS `AccountRules.HandleMax` (14 + 1 + 4).
const HANDLE_MAX = 19;

function oneLine(value, max) {
    return String(value || "")
        .replace(/[\u0000-\u001f\u007f]/g, "")
        .trim()
        .slice(0, max);
}

function read(item) {
    if (!item || !item.value) return null;
    try {
        return typeof item.value === "string" ? JSON.parse(item.value) : item.value;
    } catch (e) {
        return null;
    }
}

function emptyList() {
    return { Friends: [], Incoming: [], Outgoing: [], Blocked: [] };
}

function addressable(id) {
    return typeof id === "string" && id.trim().length > 0;
}

function findIn(rows, id) {
    if (!Array.isArray(rows)) return null;
    for (const row of rows) if (row && row.PlayerId === id) return row;
    return null;
}

function without(rows, id) {
    if (!Array.isArray(rows)) return [];
    return rows.filter(row => row && row.PlayerId !== id);
}

function rowFrom(raw) {
    return {
        PlayerId: oneLine(raw && raw.PlayerId, 64),
        Handle: oneLine(raw && raw.Handle, HANDLE_MAX),
        Presence: Number.isInteger(raw && raw.Presence) ? raw.Presence : 0,
        JoinCode: oneLine(raw && raw.JoinCode, 8),
        SeenUtc: oneLine(raw && raw.SeenUtc, 40),
    };
}

// ⚠️⚠️ THIS IS `SocialRules.Normalise` AND IT MUST STAY THE SAME FUNCTION. A document that is
// legal on one side and not on the other is a list that grows on the server and is silently
// trimmed on the client, or the reverse. Blocking removes from every other list here for the
// same reason it does there: a block that leaves a friendship standing is a label, not a
// boundary.
function normalise(raw) {
    const source = raw || {};
    const clean = emptyList();

    for (const id of Array.isArray(source.Blocked) ? source.Blocked : []) {
        const trimmed = oneLine(id, 64);
        if (!addressable(trimmed) || clean.Blocked.includes(trimmed)) continue;
        if (clean.Blocked.length >= MAX_BLOCKED) break;
        clean.Blocked.push(trimmed);
    }

    const trim = (rows, cap) => {
        const kept = [];
        for (const raw of Array.isArray(rows) ? rows : []) {
            const row = rowFrom(raw);
            if (!addressable(row.PlayerId)) continue;
            if (clean.Blocked.includes(row.PlayerId)) continue;
            if (findIn(kept, row.PlayerId)) continue;
            if (kept.length >= cap) break;
            kept.push(row);
        }
        return kept;
    };

    clean.Friends = trim(source.Friends, MAX_FRIENDS);
    clean.Incoming = trim(source.Incoming, MAX_PENDING);
    clean.Outgoing = trim(source.Outgoing, MAX_PENDING);

    // A row in two lists is a contradiction and friends wins. See the C#.
    clean.Incoming = clean.Incoming.filter(row => !findIn(clean.Friends, row.PlayerId));
    clean.Outgoing = clean.Outgoing.filter(row => !findIn(clean.Friends, row.PlayerId));

    return clean;
}

async function loadList(api, projectId, who) {
    const response = await api.getProtectedItems(projectId, who, [LIST_KEY]);
    return normalise(read(response.data.results.find(x => x.key === LIST_KEY)));
}

async function saveList(api, projectId, who, list) {
    await api.setProtectedItem(projectId, who, {
        key: LIST_KEY,
        value: JSON.stringify(normalise(list)),
    });
}

async function loadPresence(api, projectId, who) {
    try {
        const response = await api.getProtectedItems(projectId, who, [PRESENCE_KEY]);
        return read(response.data.results.find(x => x.key === PRESENCE_KEY));
    } catch (e) {
        // ⚠️ A PRESENCE THAT CANNOT BE READ IS OFFLINE, NEVER AN ERROR. One friend's unreadable
        // document must not fail the whole list: the rail would go blank because somebody else
        // has a problem, which is the least explicable failure a social screen can have.
        return null;
    }
}

// ⚠️⚠️ THE SERVICE TOKEN IS WHAT MAKES THIS ENDPOINT POSSIBLE AT ALL, AND IT IS ALSO THE WHOLE
// RISK SURFACE. Every action below that touches another player's document goes through here, and
// `player-account.js`'s `verify` branch carries the same note: a missing service token must read
// as "could not do it" rather than as a silent success against the caller's own document, which
// is what a plain `new DataApi(context)` would do.
function serviceStore(context) {
    try {
        return new DataApi({ accessToken: context.serviceToken });
    } catch (e) {
        throw new Error("social unavailable");
    }
}

module.exports = async ({ params, context, logger }) => {
    const api = new DataApi(context);
    const { projectId, playerId } = context;
    const action = String(params.action || "load");
    const subject = oneLine(params.playerId, 64);
    const handle = oneLine(params.handle, HANDLE_MAX);

    // -----------------------------------------------------------------------
    // LOAD, with the presence of the friends the rail is about to draw.
    // -----------------------------------------------------------------------
    if (action === "load") {
        const mine = await loadList(api, projectId, playerId);
        const store = serviceStore(context);
        const cutoff = Date.now() - PRESENCE_STALE_SECONDS * 1000;

        for (let i = 0; i < mine.Friends.length && i < PRESENCE_FANOUT; i++) {
            const friend = mine.Friends[i];
            const seen = await loadPresence(store, projectId, friend.PlayerId);
            if (!seen) continue;

            const when = Date.parse(String(seen.SeenUtc || ""));

            // ⚠️ THE STALENESS RULE RUNS HERE TOO, so a client on an older build cannot be handed
            // a lit row for somebody who quit an hour ago. The client applies it again; that is
            // not redundancy, it is the same rule at both ends of a document that is also cached
            // to disk (`SocialStore`).
            if (!Number.isFinite(when) || when < cutoff) continue;

            friend.Presence = Number.isInteger(seen.State) ? seen.State : 1;
            friend.JoinCode = oneLine(seen.JoinCode, 8);
            friend.SeenUtc = oneLine(seen.SeenUtc, 40);

            // ⚠️ THE HANDLE IS REFRESHED FROM THE PRESENCE DOCUMENT RATHER THAN LEFT AS IT WAS
            // WHEN THEY WERE ADDED, so a friend who renames themselves is not a stale name on
            // forty other people's screens for ever.
            if (seen.Handle) friend.Handle = oneLine(seen.Handle, HANDLE_MAX);
        }

        return { list: JSON.stringify(mine) };
    }

    // -----------------------------------------------------------------------
    // PRESENCE, which is the only thing written on a timer.
    // -----------------------------------------------------------------------
    if (action === "presence") {
        const state = Number.isInteger(params.state) ? params.state : 1;

        await api.setProtectedItem(projectId, playerId, {
            key: PRESENCE_KEY,
            value: JSON.stringify({
                State: state < 0 || state > 4 ? 1 : state,
                JoinCode: oneLine(params.joinCode, 8),
                Handle: handle,
                SeenUtc: new Date().toISOString(),
            }),
        });

        return { written: true };
    }

    // -----------------------------------------------------------------------
    // REQUEST. The one action that writes into a stranger's document.
    // -----------------------------------------------------------------------
    if (action === "request") {
        if (!addressable(subject) || subject === playerId) throw new Error("no player to add");

        const mine = await loadList(api, projectId, playerId);

        if (mine.Blocked.includes(subject)) throw new Error("unblock them first");
        if (findIn(mine.Friends, subject)) return { list: JSON.stringify(mine) };
        if (mine.Outgoing.length >= MAX_PENDING) throw new Error("too many requests pending");
        if (mine.Friends.length >= MAX_FRIENDS) throw new Error("your friends list is full");

        const store = serviceStore(context);
        const theirs = await loadList(store, projectId, subject);

        // ⚠️⚠️ THEIR BLOCK LIST DECIDES, AND THE CALLER IS NOT TOLD. `SocialRules` carries the
        // reasoning: telling somebody they have been blocked is how a block becomes an argument.
        // The request is dropped and the caller sees a pending row that is never accepted, which
        // is what every shipping game does.
        const refused = theirs.Blocked.includes(playerId) ||
                        theirs.Incoming.length >= MAX_PENDING;

        // ⚠️ THE MUTUAL CASE IS AN ACCEPT, NOT A SECOND REQUEST. Two people adding each other at
        // once is the commonest race a friends list has, and this is where it resolves.
        if (findIn(mine.Incoming, subject)) {
            // ⚠️ THE HANDLE COMES OFF THE ROW THEY CREATED, NOT OFF THE CALLER'S PARAMETERS.
            // They wrote their own name into our inbox when they sent the request, which is the
            // only value here that they own; `params.theirHandle` is what WE last saw and would
            // be empty on the mutual path, because nobody has drawn them yet.
            const pending = findIn(mine.Incoming, subject);
            mine.Incoming = without(mine.Incoming, subject);
            mine.Friends.push(pending);

            theirs.Outgoing = without(theirs.Outgoing, playerId);
            theirs.Friends.push(rowFrom({ PlayerId: playerId, Handle: handle }));

            await saveList(store, projectId, subject, theirs);
            await saveList(api, projectId, playerId, mine);
            return { list: JSON.stringify(normalise(mine)) };
        }

        if (!findIn(mine.Outgoing, subject))
            mine.Outgoing.push(rowFrom({ PlayerId: subject, Handle: oneLine(params.theirHandle, HANDLE_MAX) }));

        await saveList(api, projectId, playerId, mine);

        if (!refused && !findIn(theirs.Incoming, playerId) && !findIn(theirs.Friends, playerId)) {
            theirs.Incoming.push(rowFrom({ PlayerId: playerId, Handle: handle }));
            await saveList(store, projectId, subject, theirs);
        }

        return { list: JSON.stringify(normalise(mine)) };
    }

    // -----------------------------------------------------------------------
    // ACCEPT and DECLINE, which act on a row somebody else created.
    // -----------------------------------------------------------------------
    if (action === "accept" || action === "decline") {
        if (!addressable(subject)) throw new Error("no player named");

        const mine = await loadList(api, projectId, playerId);
        const pending = findIn(mine.Incoming, subject);
        if (!pending) return { list: JSON.stringify(mine) };

        mine.Incoming = without(mine.Incoming, subject);

        const store = serviceStore(context);
        const theirs = await loadList(store, projectId, subject);
        theirs.Outgoing = without(theirs.Outgoing, playerId);

        if (action === "accept") {
            if (!findIn(mine.Friends, subject)) mine.Friends.push(pending);
            if (!findIn(theirs.Friends, playerId))
                theirs.Friends.push(rowFrom({ PlayerId: playerId, Handle: handle }));
        }

        await saveList(store, projectId, subject, theirs);
        await saveList(api, projectId, playerId, mine);

        return { list: JSON.stringify(normalise(mine)) };
    }

    // -----------------------------------------------------------------------
    // REMOVE and BLOCK. Both end a friendship on BOTH sides.
    // -----------------------------------------------------------------------
    if (action === "remove" || action === "block") {
        if (!addressable(subject) || subject === playerId) throw new Error("no player named");

        const mine = await loadList(api, projectId, playerId);

        mine.Friends = without(mine.Friends, subject);
        mine.Incoming = without(mine.Incoming, subject);
        mine.Outgoing = without(mine.Outgoing, subject);

        if (action === "block" && !mine.Blocked.includes(subject)) {
            if (mine.Blocked.length >= MAX_BLOCKED) throw new Error("your block list is full");
            mine.Blocked.push(subject);
        }

        // ⚠️⚠️ THE OTHER SIDE IS CLEARED TOO, AND SKIPPING IT WOULD LEAVE A ONE-WAY FRIENDSHIP.
        // They would keep drawing your presence, keep offering JOIN, and keep being told you are
        // online, from a list you are no longer on. **A friendship is one fact stored twice**, so
        // both copies move together or neither does.
        const store = serviceStore(context);
        const theirs = await loadList(store, projectId, subject);

        theirs.Friends = without(theirs.Friends, playerId);
        theirs.Incoming = without(theirs.Incoming, playerId);
        theirs.Outgoing = without(theirs.Outgoing, playerId);

        await saveList(store, projectId, subject, theirs);
        await saveList(api, projectId, playerId, mine);

        return { list: JSON.stringify(normalise(mine)) };
    }

    if (action === "unblock") {
        const mine = await loadList(api, projectId, playerId);
        mine.Blocked = mine.Blocked.filter(id => id !== subject);
        await saveList(api, projectId, playerId, mine);
        return { list: JSON.stringify(normalise(mine)) };
    }

    throw new Error("unknown social action");
};

// ⚠️⚠️ EVERY PARAMETER A SCRIPT USES MUST BE DECLARED HERE OR CLOUD CODE STRIPS IT, AND THE
// FAILURE IS SILENT AND LOOKS LIKE A WORKING ENDPOINT. `docs/TODO.md` § 90.5: three scripts
// declared none, so every call fell through to whichever branch `params.action || "default"`
// picked, answered normally, and no career had ever reached the server. Every probe was green
// throughout, because they all probed with the default branch.
//
// ⚠️ TWO FURTHER THINGS SILENTLY DROP THE WHOLE DECLARATION AND PRINT `params: []`: a parameter
// typed `JSON` (use `String` and serialise), and a top-level `function parameters()` anywhere in
// the file. `ugs cloud-code scripts get social` is the only way to see what the service holds.
//
// ⚠️ IF A NEW ACTION NEEDS A NEW PARAMETER, IT GOES IN THIS BLOCK IN THE SAME EDIT.
module.exports.params = {
    action: "String",

    // The other player, for every action that names one.
    playerId: "String",

    // The CALLER's own handle, so the row that lands in somebody else's document has a name on
    // it. ⚠️ It is a label and never an identity: the id is what the friendship is keyed on.
    handle: "String",

    // The SUBJECT's handle as the caller last saw it, so an outgoing row can be drawn before
    // they have ever been seen online.
    theirHandle: "String",

    // `presence` only.
    state: "Numeric",
    joinCode: "String",
};
