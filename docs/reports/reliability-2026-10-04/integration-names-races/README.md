# Updated names and asynchronous fixes in the Windows player

Source91aad50f2, protocol145, full-quality Release/BuildOptions.None succeeded
in115seconds. Artifact258files/2557601499bytes. Exact executable/Runtime/Core
hashes and source identity are in artifact.json. Full file manifest and frozen
input maps remain under internal Builds/Logs; no Desktop release replacement.

Unity changed202 UI importer metadata files, ProjectSettings line endings and
two GraphicsSettings always-included shader registrations (DanteBarrier and
BlockyCloud). The203 importer/EOL changes were restored to exact pre-run bytes;
the shader registrations and pre-existing ProjectAuditor generated reference
remain separately tracked. Runtime/Core/authored-asset bytes were stable during
build. The packaged dirty identity is accurately recorded; strict all-input
preservation is false and the artifact is classified for internal player checks.

Exact new player menu check passed: startup video, silent loading/login,
Terms/signup/Guest journey, home music/title motion/reduced motion, settings,
credits and mode selection/back. Exit0, profile/input restored and all artifact
files unchanged. Native1920home/960login screenshots were inspected. Actual
screens and logs remain in Logs/integration-names-races1004/menu-player.

The old full-flow probe failed at EnterSettingsFromHome: it assumed StartButton,
ClassicButton and PracticeButton before SettingsButton. Actual controls were
current Hub ModeCard/PlayButton/MenuButton. The current MenuSETTINGS route already
passed. This is preserved as obsolete probe coverage, not a game crash or pass.

A focused current-hub route now visits all nine actual hero screens and captures
visible headings, rejects old names, checks heading bounds and exercises current
settings/mode back navigation. Direct compilation passes; actual native route
execution and full gameplay/peer acceptance remain open. No claim that the names
are visually accepted merely from the source scan or Core47checks.
