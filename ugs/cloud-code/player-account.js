const { DataApi } = require("@unity-services/cloud-save-1.4");

const KEY = "accountProfile";

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
        Discriminator: /^\d{4}$/.test(String(p.Discriminator || "")) ? String(p.Discriminator) : "",
        Bio: oneLine(p.Bio, 140),
        Country: /^[A-Za-z]{2}$/.test(String(p.Country || "")) ? String(p.Country).toUpperCase() : "",
        Pronouns: oneLine(p.Pronouns, 32),
        Email: "",
        CreatedUtc: oneLine(p.CreatedUtc, 40),
    };
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
        const parsed = JSON.parse(String(params.profile || "{}"));
        const profile = profileFrom(parsed, playerId);
        await api.setProtectedItem(projectId, playerId, { key: KEY, value: JSON.stringify(profile) });
        return { profile: JSON.stringify(profile) };
    }

    if (action === "delete") {
        // Authentication deletion is the account-level deletion. Clearing the protected value
        // first makes this endpoint idempotent even when the auth request is retried later.
        await api.setProtectedItem(projectId, playerId, { key: KEY, value: "" });
        return { profile: "" };
    }

    throw new Error("unknown account action");
};
