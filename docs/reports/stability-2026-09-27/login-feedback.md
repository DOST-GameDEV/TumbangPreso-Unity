# Login field feedback, 2026-09-27

Owner request: wrong input should play an error cue,pulse the affected field and
show a specific message beneath it in the supplied login reference's style.

Base `55aca6cb`. Existing field messages and MenuSfx.Error were partial. Required
confirmation was erased by the live watcher on its next frame; server pair errors
tracked only username edits; there was no field pulse. Native submission now
evaluates all fields together,focuses the first fault and sends no account request
while any is invalid. Required messages stay until corrected; sign-in does not
enforce newer registration password rules on an existing credential.

OwnerFieldPulse gives two gentle tint pulses over0.72s using the existing error
ink,not movement or scaling. Reduced motion uses a steady tint instead. It restores
the original colour on completion,edit,mode change or disable. Good fields are
left unchanged. The existing per-cue sound dedup gives one error cue per press.
Ambiguous service credentials still use one honest pair message and pulse both
inputs; editing either removes that stale verdict. General service failures remain
in the attempt status rather than being labelled as a bad password.

All four assemblies compile with the installed Unity Roslyn toolchain on105 frozen
source/dependency inputs (7changed). [Receipt](checks/login-feedback-compile.json).
One focused native case covers required messages surviving frames,valid fields,
steady reduced-motion tint,fixed hitboxes,edit cleanup and credential-pair retry.
It is NOT RUN under the recorded native disk limitation. No broad suite or old
film repeated,and no live sign-in or profile mutation was performed. Visual and
auditory judgment across mouse,pad and touch remain OPEN,not inferred from compile.
