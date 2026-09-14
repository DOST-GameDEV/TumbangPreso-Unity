# Owner theme: play, credits and loading

First implementation of U2 is complete for its recorded scope. The remaining
UI stages and final motion/input/art review remain active.

OwnerPlayView supplies the mode choices, real character portraits and the
existing offline/friends/ranked routes. Original painted action sprites retain
their aspect ratios. Classic hides the entire ranked route and recenters the
remaining choices. OwnerCreditsView retains every existing credit/license in a
separate reading layout. Previous Tump view builders remain inactive behind
their nonvisual adapters.

SplashScreen now chooses an owner-art loading surface with the actual logo,
pattern and a clipped original divider stroke as progress. Stories/tips remain
optional and clickable. Existing preload/account barriers, reading dwell, audio,
fallback assets and readiness semantics remain unchanged. Stage copy is friendly.

## Evidence and fixes

The frontend route case passes, covering home, credits, final-license reachability,
settings return, both modes and back. The loading visual/control case passes,
including bounded progress and opening/advancing/closing stories. No credentials
or real account creation were submitted. Editor/profile guards restored state.

The credits assertion originally failed after an alternate-resolution screenshot.
Diagnostics showed the content and child-height sum agreed3266units, with56units
of bottom padding. The native bottom image showed the whole final license, but
the capture's viewport change had shifted normalized scroll from0to.03562653
(contentY2442to2355) on return. The helper now preserves/restores normalized
position and velocity after the original layout settles. No production padding
was inflated and no assertion weakened. The corrected final text bottom is
-356within viewport minimum-412. A later test expectation was corrected from
activeSelf to activeInHierarchy because Ranked is hidden with its complete route
group; the actual visibility contract is retained.

Captures now wait for normal entry animation to settle instead of documenting
a partly faded logo as the finished screen. Original failed XMLs and corrected
evidence are preserved under owner-ui-u2-evidence. Native player/full cold preload
execution and physical input devices are not claimed by these scoped tests.

## Critique and next pass

Play is easy to compare and uses actual people instead of text-only choices.
Credits and loading clearly share the supplied palette and lettering. The mode
panels are currently quieter and more geometric than her original brush edges;
refine that character during the whole-UI coherence pass rather than multiplying
the same reading-sheet shape into every future screen. Keep larger art, low
clutter and distinct component families. The story panel deliberately reserves
space for longer existing entries; it is not a newly imposed reading gate.

Continue U3 preparation/lobby/browser/queue/custom flows, then U4-U8. Startup
login remains without Back. Gameplay resumes only after the full UI overhaul.
