# Tutorial scroll-prompt correction

The owner's screenshot exposed a missed consumer: GuidedTrainingHud.KeyCap
special-cased wheel bindings into its own white procedural mouse drawing. It
never reached InputGlyphs.For, so the earlier shared-sprite replacement could
pass its own row checks while the actual tutorial remained unchanged.

Remove that special case and unused drawing class. Tutorial controls now use
the existing shared owner-scroll sprites, retaining 72-unit square layout,
centered sprite pivots, aspect preservation and the existing keyboard/controller
fallback. CurvePrompt still reads live CurveLeft/CurveRight bindings and places
them next to each other without a slash. Lesson-counter punctuation is unchanged.
No new artwork, binding, gameplay or protocol change.

## Verification and limits

Compilation completed and assemblies reloaded; the subsequent import hit the
container-headroom guard. Original full-card runtime and one bounded retry
also hit the guard before a test result. The retry followed idle cache advice
and closing an unused cloud browser tab. Both failed receipts are retained.
FullCardProbe.cs.txt preserves the unqualified diagnostic, not an automatic test.

A smaller check exercises the actual CurvePrompt, RebuildKeys and KeyCap methods
inside an isolated UI row. It checks both wheel controls use the shared sprites,
no separator Text exists, equal sizes, preserved aspect and non-wheel controls.
The graphic case also requires rendered red pixels and saves the actual row.
The graphic row run also reached the memory guard before producing XML.
The final headless consumer check passes 2/2 in 0.226 seconds, including the
actual production CurvePrompt string, shared wheel sprites and alternate Q/E
controls. This is native UI structure/sprite identity evidence, not a new image
or full-card/player/human-approval claim. The same underlying artwork was already
visually checked in the earlier scroll-prompt report.

Final compile and structural run exit0 with no guard; settings/profile restored
and frozen source hashes unchanged. Final tree RSS3,163,242,496 bytes and total
container7,012,966,400 bytes. The failed visual runs remain recorded instead of
being counted as passes. No unchanged graphical retry is pending.
