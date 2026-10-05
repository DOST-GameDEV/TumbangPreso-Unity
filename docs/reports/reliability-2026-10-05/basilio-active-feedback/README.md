# Named first-person Unstoppable feedback

The owner could not tell whether Basilio's skill was casting or lacked an
indicator. The existing stone ward and badge deliberately hide from the wearer's
first-person camera. StatusStack already supplies the active skill's effective
name and real timer, but the HUD skipped every timed row when a reticle existed.
The skill dial alone carried the duration with no named active-buff confirmation.

Original native PID22108 failed the actual rendered named-HUD case. Two controls
passed: inactive effects stay absent and the collected row follows the actual
ability clock and disappears on early end. Candidate PID8576 passed all three.
The native capture shows UNSTOPPABLE15.0s alongside the existing reticle and dial.
This is rendered HUD/clock acceptance, not a physical-key or paired-vfx pass.

An explicit status-row flag now lets only Basilio's active signature name/timer
remain visible with the reticle. It uses the existing EffectiveName,
DurationRemaining and Duration. Other timed-row suppression, status mechanics,
cast input, ward/orbiting shields, first-person culling, cooldowns and sound are
unchanged. No duplicate effect, independent timer or new rendering framework is added.

Both native parents are terminal, isolated/shared preferences and Quality are
restored and each run's202 generated UI metadata changes are preserved locally
then restored to exact pre-run bytes. Original failure, candidate XML and the
native image accompany this report. Owner physical-input and Q coating/projectile
questions remain open; synthetic keyboard controls establish only their stated scope.
