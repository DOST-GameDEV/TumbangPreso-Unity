# Windows player quit access violation

The source1d401 Windows UI review player19352 exited with C0000005 after
its Rules pages were inspected. Windows Application Error confirms the same
process, executable and UnityPlayer.dll version6000.5.8.47071 at18:05:12 Manila
on October5. The fault address is module RVA16f9c36. This is an unresolved
release defect, not a clean exit or a demonstrated managed exception.

The retained parent receipt is terminal and confirms restored named-profile
settings and shared input preferences, with unchanged executable/runtime hashes.
The owner-requested screenshot player22960 is a separate live use of this package;
it was not closed or interrupted to reproduce the fault.

## Read-only symbol result

Using the existing Windows dbghelp library and installed matching Unity public
symbols maps RVA16f9c36 to remove_free_block with displacement22. The inspected
UnityPlayer.dll SHA256 is
5dcd2af9e14f6416d73c361369559666c31263e4863822e1241cbdbc00c2f667,
identical to the engine module used for the earlier symbolized quit failure.
The local symbol session was cleaned up. No software was installed or downloaded.

The first symbol lookup failed126; the corrected typed API invocation succeeds.
SymLoadModuleExW returns a nonzero base. Its last-error value after success is
not a load failure. Raw receipts preserve both the observed result and that limit.

An allocator fault address does not identify the earlier corrupting operation.
No full call stack or managed source cause was captured for this access violation.
The earlier quit failure had a different exception/address and must not be used
to claim this fault is caused by accessibility or input. No such systems were
disabled and no speculative workaround was applied.

## Next discriminating check

After the owner releases the PC, use one coherent current package and isolated
profile to compare the game's normal Quit action with the original window-close
route. Retain the exit result and any crash dump/shutdown-origin trace. Do not
retry close shortcuts while the owner uses the game or from stale window handles.
Current and latest-package quit acceptance remains open.

Exact retained Windows event, terminal player receipt and local symbol result
are inventoried in raw-hashes.json. No new runtime acceptance is claimed here.
