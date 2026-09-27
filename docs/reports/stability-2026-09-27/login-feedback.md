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
Its FIRST native run on full committed97d7f397 reached the field-colour assertion
and failed because the fixture captured its expected resting colour during the
first active pulse. Runtime correctly restored original white. Moving that capture
before the first submit was the one bounded test-only correction; assertions and
product code stayed unchanged. The retry passes1/1,0.30016s on that base plus the
one corrected test input,no drift. Minimum free5,931,757,568bytes; the guard restored
the named profile files and shared input preferences on both attempts.

The case now supplies native evidence for required messages surviving frames,
field-only tint without hitbox changes,reduced motion,edit cleanup,credential-pair
retry and service-error separation. All submissions were invalid; server failures
were injected locally,no live sign-in was submitted. No broad suite or old film
was repeated. Visual/auditory judgment and physical mouse,pad/touch use remain OPEN.
[Receipt](checks/login-feedback-native.json),[passing XML](checks/login-feedback-native.xml),
[initial failed XML](checks/login-feedback-native-first.xml). Raw isolated evidence:
Logs/login-first-native-20260927/field-feedback*. This does not qualify live auth.
