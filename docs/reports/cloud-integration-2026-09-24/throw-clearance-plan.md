# Throw clearance follow-up, 2026-09-24

Native cloud-review handoff reports two gameplay-visible defects in current ASTRAReworks: the charged first-person grip approaches within about0.08m of the lens, and Bayan right pektus can pass fitted slipper geometry through a rigid head. These failures predate the cloud merge; they remain real defects worth fixing before the broader skill-art pass.

Use the existing ThrowMotionTests grip case and ThrowEquipmentClearanceTests once for a current exact baseline. Keep their generated CSV and inspect the precise character, slipper, charge and spin. Then fit the affected first-person pose and the specific third-person body without a global action rewrite. Preserve actual release timing, path, authority, smooth cancel, simple hands, other characters and existing successful poses. Consult research-and-analysis.md for the agreed sidearm and overhand silhouettes.

Baseline correction: the EditMode CSV reported penetration for all190
person/equipment pairs, mostly at charge.35/right spin1. Its first Bayan rows
were only the start of the failure output. A real-input Bayan PlayMode pass at
spin.75 measured0penetrating vertices across98charged samples and passed; its
native action frames were inspected. Before changing the shared body path,
exercise the exact reported boundary with Bayan, alpombra and spin1 using the
existing review route's optional right-spin input. If the real pose stays clear,
record the EditMode check as a fixture discrepancy and do not repaint a healthy
throw just to clear it. The first-person depth defect remains separately open.

Exact native boundary: Bayan with alpombra at legal spin1 passed1/1 in19.5313924s;
all quick/left/right sequences sampled real input and the moving right hold had
0of41696fitted shoe vertices inside the head across92charged samples. The
EditMode assertion's190/190failures therefore cannot establish a shipped-body
collision. Preserve that red fixture and fix its representation separately at
the final validation gate; changing the shared body throw here would risk a
healthy pose. Current product edit only corrects the first-person charged grip.

First FPP correction removed the8cm depth defect but placed every charged grip
below the intended frame. That failed XML is preserved. The follow-up anchors
the actual fingertip to the established `CarryAnchor`, plus small distinct
straight/left/right travel, while allowing the forearm rotation and wrist roll
to communicate the throw. This is a product pose change, not an assertion edit.

The first anchor implementation targeted the pivot's up-axis, while the actual
shoe follows the child Arm after that child rotates. Its depth stayed at.22-.27m;
the failure is preserved. The correction now measures the same child fingertip
used by the held prop and applies the offset there. No test threshold changed.

Validation is bounded: one focused baseline, product edit, one focused after check and normal-sized visual witness if the numbers alone cannot establish quality. One fixture repair maximum across this unit. Do not grow a capture framework or rerun the whole gate. TODO, ledger and this report get updated with the source/evidence in the same commit. Final native peer and full-regression work remains P7.
