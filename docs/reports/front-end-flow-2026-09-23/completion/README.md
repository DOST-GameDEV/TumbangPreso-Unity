# UX-1 completion evidence

Baseline source a45cba87a, before fixes, preparation board enabled:53 cases,
51 passed,2 failed,300.260s. Raw XML and exact filter/cases are preserved here.
The inherited machine reported51 cases with hub enabled but did not publish its
raw XML/filter, so these totals are not a paired full-matrix comparison.
Every specifically named failure other than PaperPurity inventory and the Rafi
story case passed under the restored board in this local run. Those shared cases
are migrated through actual HOME/menu/HOST/JOIN/LOBBY controls.

Two baseline failures:
- PaperPurityProbe.NothingOnTheInventoryDisappeared: retained control inventory
  still names TutorialButton; current tutorial is the hamburger LEARN TO PLAY.
  This is not a newly missing tutorial. Inventory route follow-up stays open.
- TumpNativePickerTests.NativeHeroSkillsHaveDistinctSymbolsAndAnActualReturnPath:
  Rafi has no character-stories entry. Added his existing BADJAO_EXPANSION lore to
  the same story source, preserving the other six stories. This repairs a real
  absent READ THE STORY door in the new HERO screen too.

Changed production behavior: observe out-of-room to in-room after HOME installs,
open one lobby, preserve queued selection and an existing lobby's subpage. Button
completions and initial live-room installation use the same idempotent method.

Migration decisions preserve capabilities, not retired layout constants: HERO
inspection is explicitly confirmed with PLAY AS; custom-room portraits publish
picks immediately as specified by UX-1. Equipment inspection/cancel/equip remains
separate. Skill details live in HERO, branches in SKILL TREE; all hero variants,
locks and descriptions remain covered. Profile persistence assertions are intact
behind the new name-plate door. Chat is tested in a real room; no hidden board
InputField is activated manually. Five-shape accessibility uses existing capture
machinery with both Larger text and High contrast enabled and restored afterward.

Post-change evidence is now recorded in the adjacent XML files and the ledger.
Local real-LAN arrival reaches map vote, voted arena, eight-second introduction and
automatic START. Manual-ready and reduced-motion/cleanup cases passed. These are
Editor cases, not a live UGS queue or a two-player native acceptance claim.
Updated Terms/filled consent, all hero variant text, inline loading readiness and
three-image rotation passed; the latest arrow-only Terms capture is personally
inspected. Profile/story/chat and custom rules passed after actual lifecycle fixes.
Core629/629 and14gating source audits passed. The informational cue-audio audit still
flags6files; it is recorded rather than silently counted as a clean audio review.
Complete larger-text route passed at five shapes; latest lobby/loading captures
personally inspected. Final Checks.RunAll passed8/8. Native peers/build and the
full partitioned regression remain open. No full-goal completion is claimed.
