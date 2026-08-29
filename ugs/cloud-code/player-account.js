const { DataApi } = require("@unity-services/cloud-save-1.4");

const KEY = "accountProfile";

function oneLine(value, max) {
    return String(value || "").replace(/[\u0000-\u001f\u007f]/g, "").replace(/\s+/g, " ").trim().slice(0, max);
}

function profileFrom(raw, playerId) {
    const p = raw || {};
    const displayName = oneLine(p.DisplayName, 16);
    if (displayName.length < 3 || !/^[\p{L}\p{N} _.-]+$/u.test(displayName)) {
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
