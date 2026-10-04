# Native hub editing refresh lifetime

Same-owner service updates retained draft text but replaced the active Profile
and Friends InputFields, interrupting editing. The PC candidate keeps the actual
field while editing or a UI action is held, coalesces pending refresh, then consumes
it in LateUpdate after release. Account replacement and explicit tab navigation
still retire old editing. Only PlayerHub.cs and PlayerHub.OwnerPainted.cs change
production behavior.

Original source53d6b7e09bd2a2077446269334d7873bb89052d1: two causal failures and
two controls passed. Candidate754aaef3ddaa01a7129f0a966b2dfce6c6321c75:
the SAME four-case fixture/metadata passes4/4. Three separate new controls pass3/3:

- The actual editing field and selection2..5 survive refresh; a nonedited Bio
  remains old while editing and receives changed account data after blur, while
  the typed name draft survives.
- The actual UI module leftClick action observes a supplied Mouse hold; the
  native Button survives until pointer-up/click and receives exactly one click.
- The actual CLOSE button closes a hub with pending refresh and it stays closed.

## Evidence and preservation

Original18302, candidate58576 and corrected controls84095 are CLOSED with exact
counts, zero skips, terminal guards, preservation complete and leases free.
Their3440/3440/3442 frozen inputs stayed unchanged, with quality restored byte
exactly. Original and candidate maps differ only in the two tested UI source files;
all candidate runtime/fixture hashes are unchanged in the additional-controls map.
Unity6000.5.8f1 used the existing isolated qa-a profile and physical Library,
graphics,4096MiB budget plus1024MiB reserve, one worker and300-second deadline.
The original four cases were not repeated in the additional-controls job.

One prelaunch fixture correction opened the collapsed search group and queried
the independent Canvas before any original native run. An invalid placeholder
preparation ref failed before archive, mutation or Unity and its empty output
directory remains preserved. A separate local five-case fixture6db11ee72 was
preserved unrun when the PC fixture reservation arrived.

The first additional-controls job59700 stopped at compilation because its fixture
called private Close. ZERO tests executed; this is tooling evidence, not a product
failure. ONE fixture-only correction uses the actual ClosePlayerHub button, with
metadata and Runtime unchanged. The failed source/error/map/receipt/audit remains
beside the corrected evidence; no second repair or unchanged control retry.

manifest.json preserves SHA256 values for all34 exact raw blobs, including
XML, maps, receipts, audits, quality snapshots, tested original/candidate UI,
unchanged core fixture and both control-fixture versions.

## Limits

Actual Canvas/InputField/Button behavior is tested with a dormant account and
null service endpoints. Account Bio changes and same-owner callbacks are supplied
locally; no authentication, deployed service or profile-write claim. Pointer-up
uses native handlers with a real Input System action hold; it does not establish
physical clicking, GraphicRaycaster hit geometry, keyboard/controller/touch or
IME behavior. Field identity and caret preservation are observed; keeping its
IME composition is an implementation consequence, not a separately injected test.

The source avoids row rebuilding while editing, but these checks do not establish
packaged startup/preload/frame performance or a measured allocation improvement.
Current release artifact, two-machine match/saved results, physical operator,
Friends service exchange and tournament acceptance remain open.
