# Preserve saved characters after a missing slot

The loader stopped at the first null or empty character wire. A profile with a
missing first or second slot therefore replaced later valid saved characters,
including the active character, with starters. A subsequent save could persist
those replacements.

The loader now starts with the three existing distinct starters and replaces each
nonempty saved slot at its original index. A missing slot keeps its own starter;
loading does not write the stored wires. Character wire format, normalisation,
appearance and three-slot limit are unchanged.

Native EditMode baseline: **four reproduced failures and two passing controls**.
Candidate: **6/6 passed**, zero fixture or tooling repairs. Cases cover null/empty
first/second slots, active selection, exact encoded character preservation,
unchanged stored wires, a complete profile and a short profile with distinct
starters. Both native jobs terminated and profile/input preservation completed.

The test replaces only in-memory settings through the existing test seam, restores
the previous settings and retires the character cache. No user save was edited.
This proves store loading, not a rendered creator screen or cross-device save.

Raw XML, job receipts and preservation output are retained beside this report.
Qualification checkout: `C:/Users/matth/Documents/Codex/work/tump-feedback-0930`.
Logs: `custom-slot-recovery-baseline1002` and `custom-slot-recovery-final1002`.
