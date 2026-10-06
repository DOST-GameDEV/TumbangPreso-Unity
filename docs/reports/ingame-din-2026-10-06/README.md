# Supplied in-game DIN typography

The owner requested DIN Next LT Arabic for in-game interface text and explicitly
kept only the central match clock in Darumadrop. The subsequent supplied Bold
file is used for headings, scores, status names and actions. Light is used for
descriptions and reading text. Front-end theme references are unchanged.

The shared resolver uses each label's actual map scene, including additive
prefetch and persistent UI opened during play. Shared factories and direct
match/training/replay/result/settings label builders use the same selection.
Symbolic reticle, hit-confirmation and direction glyphs retain their prior font;
world TextMesh nameplates are outside this interface change. No input, network,
hero logic, authored layout or graphics quality settings change.

Both font binaries match their supplied source bytes exactly. The retained
source receipt records SHA256, byte count, actual embedded family, weight,
metadata GUID and HUD Latin/digit/punctuation coverage. Font data is included
with dynamic import settings. Light's actual embedded family is ntaqat, so its
importer uses that family without modifying the source name tables.

Direct Roslyn preflight compiled all805 current runtime source files against
172 retained player reference assemblies with MULTIPLAYER_SDK and
UNITY_STANDALONE_WIN, zero errors. This is source evidence, not Unity import,
editor compilation or rendered acceptance. Local Logs/ingame-din1006 retains
the exact command response file and warning log. No native PC job was launched.

Native validation remains pending on the existing laptop after its active
pre-DIN package pair is terminal and restored. Acceptance covers actual imported
faces, HUD/status/announcement/pause/results at1920x1080 and1280x720, front-end
control and prefetched map context. Root's competition goal remains paused by
the owner's request while this separate typography change is completed.
