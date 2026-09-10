# Ground placement and Zack function correction

Ground effects inherited the caster's Y coordinate while the aim marker projected
onto the floor. Reproduction put ice, fire and an ice wall 2.4 m above a raised
floor and left ice at y=8 over an overhead deck. Eleven floor spawners now use the
same ground projection as the marker. The sampler excludes actors, loose objects,
effects and generated barriers, preferring the authored court surfaces. Ice/fire
floor meshes also conform across kerbs using the existing draping helper.

Actual-map checks place both ice and fire at y=0.1000 in Eskinita and Bayan Plaza,
and y=0.0000 in Ilalim ng Tulay, matching the physical floor at each sampled point.
The kerb test checks bottom mesh vertices, not only a root transform. These images
show the grounding correction. **The owner rejected the ice skill's appearance;
its art redesign and the complete ability presentation pass remain open.**

- [Ground measurements](ground-skills/ground-placement.csv)
- [Eskinita](ground-skills/Eskinita-ground-skills.png)
- [Bayan Plaza](ground-skills/BayanPlaza-ground-skills.png)
- [Ilalim ng Tulay](ground-skills/IlalimNgTulay-ground-skills.png)

Zack's sustained sprint impulse was smaller than the motor's friction, so it did
not deliver the described sustained speed. A 25% wish-speed multiplier now lasts
for the existing active duration. Real input measured 2.6565 / 3.3206 / 2.6565 m/s
before, during and after reset. Initial dash, cooldown and bounded trail remain.
Thunderstrike no longer adds the old overdrive's self-impulse; its existing charged
throw window remains. Tests reproduced that impulse changing incoming knockback
at 30/60/144 update rates before removal. This is not a measured standing-drift claim.

Magnet draws a narrow, short-lived source-to-hand connection instead of arcing to
nearby objects. Equip remains host-authoritative and immediate. The incorrect
unused-flight-time comment is removed. Hand auras use the measured grip anchor;
Magnet owns and clears its trace/charge effect on end/reset/consumption.

Verification on this candidate: Core 559/559, EditMode 470/470, all fourteen gating
source audits pass. Seven focused PlayMode cases pass across the final ground/
trace/anchor and map/kerb/speed runs. All three Zack real-input casts were accepted
and captured; full animation and SFX/VFX approval is not claimed. The informational
audio audit still flags seven of 119 files. Final twice-through PlayMode gate,
Windows build and exact-executable verification remain open.
