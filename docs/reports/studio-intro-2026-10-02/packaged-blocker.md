# Isolated Linux packaged startup qualification blocked

Source49e9849b was checked out in a detached isolated worktree with a physically
copied idle Library, unique profile/output and unchanged guards. Main workspace,
unowned source edits and existing profiles remained untouched. Duplicate docs
were omitted through sparse checkout to leave3.3GB free; no managed worktree was
deleted. There is no new player artifact.

The first build stopped at the existing authored-animation prerequisite because
RosterBook.Load returned null. One bounded targeted asset reimport/build retry
also failed: AssetDatabase and Resources both returned false for the tracked book.
No authoring, source replacement, cached-book injection or gate bypass was used.

A separate read-only binding inspection found the book's main asset/type null;
its MonoScript exists at the expected GUID path but GetClass is null, while the
compiled TumbangPreso.Runtime assembly contains the expected RosterBook type.
The inspection then failed while enumerating missing assets; it is not a pass.
Its first launch was refused before Unity due CPU classification; the corrected
exclusive classification admitted this one actual inspection. Raw logs retained.

This identifies a script/asset registration problem in the isolated copied-cache
qualification workspace, not evidence that authored roster content is absent.
Do not regenerate roster/models/animations or call the published native checks
packaged acceptance. Stop repeated full builds at this boundary. A future recovery
must establish correct script binding before another expensive build attempt.

## Later packaged evidence

Windows1003c now qualifies actual visible studio playback before loading and
normal startup/menu navigation for source8ef02cf7/protocol134. See the parent
report. This earlier local Linux binding failure remains unresolved; it is no
longer a statement that packaged startup has no acceptance on any platform.
