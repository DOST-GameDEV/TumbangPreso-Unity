# QoLUpdates integration

Owner requested a safe merge of QoLUpdates into ASTRAReworks on October5 and
confirmed that incoming Yasmin takes precedence over the earlier model rollback.
Incoming tip450e44261 adds Arena, Ilalim changes and authored roster redesigns.
Common ancestorf06fa42ea already contains the earlier ASTRA work; the integration
also preserves the later checked player-reader recovery and engineering reports.

The three-way merge applied without conflicts. Added asset metadata has valid
unique GUIDs. Incoming roster model references resolve, stable IDs remain and
Paete's optional first-person ArmModel points to his prior arm source. The new
Yasmin model reference is adopted as requested. The network version becomes152
for Arena map/recovery/balloon state; paired checks require matching versions.

The live train source retained an optional Resources/Troll loader, looping music,
listener-volume bypass and timing override despite the earlier song removal.
These release-path hooks were removed. Ordinary train rumble/clack, shuttle
movement and collision behavior remain. No private ignored audio file was
deleted or added to the repository.

Static checks confirm no merge markers, no added metadata GUID errors and the
retained ASTRA timeout retry, LAN port boundary, input release-before-use and
named Unstoppable countdown. These checks are source/import-boundary evidence.
Merged native compilation, affected map/model/camera/gameplay checks and matching
protocol152 peers are still required. No whole-release or QA-failure closure is
claimed before those checks complete.

The owner is using the PC; Unity validation is coordinated on the laptop after
its existing native unit is terminal and restored. Root's separate Home-card
candidate remains an uncommitted follow-up and is not folded into this merge.
Protected ProjectAuditor dirt and the private cancelled Yasmin prototype script
remain outside the merge commit. No reset, clean or force-push was used.

Follow-up fetched tipfa922a463 adds only MatchPoseHistory.CopySurface clearing
of _FlashAmount, _CaughtAmount and _FrostAmount before assigning the copied
renderer property block. All three properties exist in the current Toon shader.
Other copied material properties are preserved. This integrates the owner's
reported white replay-body fix; a changed replay capture/control is still required
and static inspection does not establish visual acceptance.
