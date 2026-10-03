# Local resumption qualification

Pinned source 5e74518c6, Unity 6000.5.8f1, isolated existing release worker.
This worker has retained importer/settings dirt and a scoped source overlay;
these checks qualify the named behavior, not a clean whole-release candidate.

The first EditMode run executed 21 cases: 15 passed and six deletion cases
failed because NUnit required the exact OperationCanceledException type while
Unity surfaced its TaskCanceledException subclass. The original XML is retained.
One fixture correction uses InstanceOf<OperationCanceledException>; all identity,
settings, dispatch and restart assertions remain intact. Only the eight deletion
cases were rerun, and all eight passed. No product code changed in that repair.

The first graphics PlayMode run passed all four PlayerHub deletion lifetime
cases on the installed footer text reference. There was no additional repair.

Unique qualified cases: deletion8, initialization fallback3, hosted lobby7,
ready countdown abandonment3, deletion UI4, total25. Both runs and the bounded
deletion repair terminated; guards restored profiles and shared preferences and
released their leases. No live account deletion, service request, physical input,
slow scene unload or actual network-peer acceptance is implied.
