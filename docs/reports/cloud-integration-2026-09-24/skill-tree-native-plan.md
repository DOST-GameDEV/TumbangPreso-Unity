# Cloud skill tree: actual HOME route and open branches

The cloud change made every hero branch available for testing through
`HeroLoadoutRules.LockSkillTree=false`, retaining counters and a single switch
back to earned unlocks. Core HeroLoadoutTests passed14/14, but that proves rule
selection, not the player's screen. The runtime hub uses uGUI Canvas objects.

Use the existing `HubFlowTests.HomeAndEveryDoorOpensItsScreenAndBackReturns`
once in the guarded Editor, including its real HOME-to-SKILL TREE button and
BACK path. Inspect the actual SkillTree captures at five viewport shapes for
open-state text, equipped controls, bounds, clipping and map background. The
fixture checks the label floor and active button bounds. If the screen works
and reads well, retain it. Address only a demonstrated defect; do not redesign
the front end or protected login/main menu because an old TODO is open.

This does not prove pad/touch hardware, real server progression, purchases or
online wallet deployment. Those remain explicit UX/P7 gates. No broad UI suite,
new capture tool, usage reset or external service call for this local review.

First native door case passed1/1 in42.053s. Actual 960x540,1600x680 and
1920x1080 SkillTree captures fit, with UNLOCKED alternate tiles. The mastery
line also said "every branch open for testing" to players, duplicating that
state and exposing implementation intent. Remove only that phrase while the
tree is open; retain MASTERY level and the meaningful instruction when unlock
rules are enabled. One existing door case afterward captures the same sizes.

The after case passed1/1 in36.207s. Five after images and the two 25 percent
greyscale thumbnails are in `skill-tree/`; [report](skill-tree/report.md).
No further repeat of this local route is needed unless source behavior changes.
