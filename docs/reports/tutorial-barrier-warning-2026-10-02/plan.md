# Attacking tutorial: no false can-barrier warning

Owner16:50UTC prioritizes the latest Wiki Feedback. The new request removes the
barrier warning in the attacking tutorial. Current throw gameplay already exempts
the active offline student's hidden practice can, whose restoration clock does
not advance. The warning view independently tests IsProtected and therefore
reports a barrier refusal even when that exact throw is allowed.

Claim Runtime/UI/TumpMatchReadout.Warnings.cs and the focused addition to
Tests/PlayMode/TutorialLessonHonestyProbe.cs, plus owning report/TODO/ledger.
Use the existing GuidedTraining.HasHiddenPracticeCan contract. Preserve warnings
for a visible protected can, ordinary matches and other refusal types; clear a
stale barrier warning when entering the exempt hidden-can state. No throw,
network, restoration, timing, opacity or broader tutorial mechanic change.

Reproduce through the real guided route and readout: visible-can warning control,
hidden-can suppression and actual charge/release. Include another refusal to
prove the whole warning area was not simply hidden. One Low native graphics case
per fresh process under the unchanged guard, with a small UI capture. Record
baseline, final input hashes and receipts. Existing high-overflight/low-hit fix
and all-six-skin bounds already have retained native evidence; reconcile the
pending Wiki note rather than redo the completed contact implementation.
