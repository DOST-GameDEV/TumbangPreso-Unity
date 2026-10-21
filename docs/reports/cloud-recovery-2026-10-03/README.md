# Cloud setup recovery, October 3

Source f9552d58a on ASTRAReworks. Git remote and clean restored checkout matched.
The cause of missing prior local tools/files is unverified; no remote loss was
observed. Official Unity6000.5.8f1, Hub3.22.2 and .NET9.0.318 were restored.
Existing browser authentication was reusable through the supported Unity CLI
sign-in flow; Personal licensing activated and the native editor resolved its
entitlement. No credentials or session URLs belong in this report.

Eight existing ThrowAimRulesTests pass under .NET9. Two existing native PlayMode
KitRecallParityChecks role-change cases pass on a separate detached checkout,
with graphics and a named validation profile. Fresh XML is retained here.
The native test duration was0.411s; the preceding first import was substantial.
Profile/input restoration completed. These are setup smoke checks, not new
full-game or tournament acceptance.

## Resource and admission limits

The pool helper classified already-finished Linux CLI zombies named unity as
live Editors, so it never launched an Editor. That admission was cancelled.
A fresh exact-executable/state inventory confirmed no live Editor/player.
The single native launch used the unchanged run_unity_guarded.py profile guard,
an outer900s deadline,7.25GiB process-tree limit and8GiB disk reserve.

First import crossed the disk reserve. The guard requested SIGTERM only for its
owned Editor; the editor completed both test cases and exited0 with restoration.
Peak observed tree RSS was6114295808bytes. Retain the disk warning: exit0 and
passing XML do not mean resource headroom was clean.

The validation checkout now excludes duplicate docs, ArtSource and MapSource
using sparse checkout. Its Assets, Packages, ProjectSettings and tools remain;
the main source checkout is untouched. About8.4GiB disk is free afterward.
Imported metadata churn was saved as a diff before this change and remains only
in the validation checkout. Protected composition metadata was not committed.
No unchanged native rerun is needed to restate the same smoke result.
