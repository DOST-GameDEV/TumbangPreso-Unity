# Custom room bot policy reaches the start gate and installer

Actual same-package LAN and online matches showed BotsNONE in the shared rules
but two active bots in the completed full records. Current custom rules and
the global preference-derived bot state were different owners of one decision.
The existing custom StartGame gate already requires four human seats when bots
are off; a stale enabled global bypassed it. Conversely stale disabled state
blocked a room that explicitly allowed bots. Arena installation then reloaded
the saved difficulty, undoing even a correct session policy.

AIController.ApplyCustomRoomPolicy applies current Bots/BotDifficulty without
rewriting the machine preference. Custom network StartGame applies it before
the EXISTING four-human gate. Networked custom installation applies the same
current policy before constructing seats. Queued rooms retain their explicit
accepted-bot/saved-tier path. Offline, guided and training-range installations
keep the original saved-preference route. No actor removal, default/Core rule
retuning, protocol/message, UI appearance, hero or artwork change.

## Causal and control evidence

Native Unity6000.5.8f1/graphicsPlayMode/isolatedqa-a on laptopgamergmae.
Base6ea575270, affected production unchanged through617c recovery integration.
Start original44164/98178: two causal failures, three controls pass. Exact
candidate31252/47962: five passes. Actual listening LANhost18765/publicStartGame;
four-human control uses logical admissions, not three real remote sockets.
Queue acceptance and already-disabled NONE controls are preserved.

Actual SceneManager SingleArena install original44340/11077: two causal failures,
one queue control pass. Candidate17796/10887: three passes. NONE survives saved
Normal on a full logical roster; enabledAstig installs actual brains despite
savedNONE; queue ownership/accepted bots survive into the actual installer.
Saved preference values stay unchanged. Offline candidate13072/33741: one pass,
savedNONE retains only the human seat despite enabled custom-session rules.
All9 candidate cases pass, no skips. These are separate scoped native cases,
not a claim of nine real multiplayer clients or full packaged acceptance.

Each parent is terminal, source inputs restored:21128 start/21130 load/21132offline,
279 generated metadata/Auditor deltas, QualitySettings and13 existing editor
preferences. Exact production/fixture/meta hashes retained. No main source was
changed until qualified tests finished. The first fixture used nonexistent
Difficulty.Hard; compile-only19912/44861 ran ZEROcases/0deltas. It is preserved
unqualified and corrected to the existing Astig enum with identical assertions.

## Remaining limits

This corrects the demonstrated custom policy/start/install disagreement and
preserves the required four-seat gate. New shared-package operator confirmation
must verify NONE refusal, turning bots on, four-person NONE and the corrected
host-departure path. LAN discovery/code, loss/recovery, QA intermittentRelay
timeout/hostkick, non-host lag, hardware/input/performance and whole-readiness
remain separate. Earlier8db matches are original behavioral evidence, not
confirmation of this newly published fix. Private prefs/profiles stay local.
