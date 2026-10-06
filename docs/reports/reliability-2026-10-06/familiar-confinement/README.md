# Familiar movement shares the current map boundary

After the Arena adopted circular confinement, GhostPetMotion still independently clamped X/Z for defenders. A familiar or recall candidate on a diagonal could remain outside the circle. The helper now delegates to Core.Confinement.ClampToBox, preserving Y and the existing attacker/ownerless per-side court branches. No hero kit, art, collision sweep or recall search behavior changed.

A lightweight .NET9 source-linked check compiles the real GhostPetMotion file against installed Unity managed math and the actual Core project. Role/map dependencies are explicit shims; this is not native physics or integrated-role acceptance. Original five circle-radius cases fail and four square/interior/non-defender controls pass. Candidate all nine pass. Raw original/candidate output, linked project, source and shims are retained.

Nine native real-CharacterMotor regression cases are added for the five actual Arena layouts and the controls. They are prepared for laptop validation while the owner studies; not run on the PC. Existing current153 artifact8bd still contains the old helper. Current native integration, collision/recall and packaged acceptance remain pending and must not be claimed from the source check.
