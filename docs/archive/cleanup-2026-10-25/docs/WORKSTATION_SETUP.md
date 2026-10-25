# Workstation Setup

Discover the actual machine and checkout. Old usernames,absolute paths,PIDs,
installed modules and available builds are historical evidence,not instructions.

## Checkout

- Repository: https://github.com/DOST-GameDEV/TumbangPreso-Unity.git
- Work/integrate on ASTRAReworks. Inspect status,fetch,inspect divergence and
  integrate safely. No main/reset/clean/force,profile copying or discarded dirt.
- Preserve tracked Assets and .meta,ProjectSettings,Packages,Core/Core.Tests,
  tools,ArtSource,MapSource and docs. Embedded packages are intentional.
- Library,Temp,Logs,Builds and player profiles do not arrive through Git.
  An old hash or filename in a report is not a current local build.
- Read [AGENTS](../AGENTS.md),[task routes](README.md),[ledger](ACTIVE_REWORK_LEDGER.md)
  and the latest owner/contributor scope before resuming.

## Installed Tools

Read ProjectSettings/ProjectVersion.txt and Packages/manifest.json plus lockfile.
The current project uses Unity6000.5.8f1; installed location/module availability
must still be checked on the destination machine. run_unity_guarded.py supports
UNITY_EDITOR_PATH and selects a platform-specific default. Do not blindly copy a
Windows command onto another OS.

Use available Python and .NET deliberately. Core.Tests needs the installed SDK;
Unity's bundled compiler can support a labelled compilation fallback but does not
run native tests. Set PYTHONIOENCODING=utf-8 on Windows when needed for tool output.
Do not install paid/unrelated tools or replace package versions just to make a
historical command run.

## Isolation And Profiles

### Cloud recovery receipt, October 1, 2026

The owner authorizes restoring this approved setup without a new request for
routine official software installation. Preserve existing account sessions and
source work. Diagnose missing paths before attributing them to a reset.

The owner explicitly accepted the Hub's presented
[Unity Terms of Service](https://unity.com/legal/terms-of-service) and
[Editor Software Terms](https://unity.com/legal/editor-terms-of-service/software)
on October 1, 2026. The Hub showed Terms last updated June 30, 2026. Reuse this
acceptance for the same agreement; changed agreements need their own review.

Restoration verified Unity6000.5.8f1 extraction from the official changeset archive,
Hub3.22.0 launch, Blender4.3.2 and .NET9.0.318. The eight ThrowAimRulesTests passed
on source9590f4bd. These are managed tooling checks, not native Unity qualification.
Hub Personal activation and two fresh KitRecallParityChecks PlayMode cases then
passed on the isolated9590 candidate. The memory guard requested termination
during first import; Unity nevertheless completed both cases and wrote fresh XML
before orderly exit. This is scoped native evidence, not clean memory-headroom
or full-player qualification. Preserve that warning before heavier runs.

Keep DOTNET_CLI_HOME, NUGET_PACKAGES, NUGET_HTTP_CACHE_PATH and
NUGET_PLUGINS_CACHE_PATH in writable task-owned directories. A read-only default
NuGet cache can otherwise fail package restore even after the SDK installs.
Native desktop DISPLAY and shell process namespaces may differ; verify the
shared files and launch on the actual graphics-capable environment.

Use a named validation profile and guarded runner. Every concurrent candidate needs
its own writable project caches,profile,ports and output. Freeze tested inputs and
limit heavy workloads. Profile hashes and shared input preferences are preserved
by the existing guard; never delete the player profile or copy credentials/saves
from another machine.

Check free disk/memory before a launch. The recent native validation environment
hit its disk reserve during editor startup; headroom readings before launch did
not prove it could finish. Reuse recorded evidence and make independent fixes
instead of repeatedly restarting an unchanged blocked job. See [TESTING](TESTING.md).

## Player Review

Build only when the coherent candidate needs it,with explicit internal
Builds/<candidate>/TumbangPreso.exe. Keep the Desktop player intact. Verify data and
Runtime.dll as well as the launcher; an unchanged executable stub does not prove
source freshness.

tools/run_ui_player_review.py requires --exe,--out and --profile. Use a fresh owned
output and its narrow relevant option (for example --menu-only or --performance-only).
It intentionally runs a normal player,not -batchmode,which NetBootstrap treats as
a dedicated server. Its opt-in diagnostics are not physical-device certification.

## Art And History

Supplied UI originals,regions,palette/font records and source manifests live in
ArtSource/ui and the current resources. Use [OWNER_UI_AUTHORING](OWNER_UI_AUTHORING.md)
and the task's method,not a former user's Downloads directory. Source/internal
render outputs may need regeneration; shipped assets and source references remain.

The complete older transfer/setup notes,including specific tools and asset receipts,
are retained in [the dated snapshot](archive/snapshots-2026-09-27/docs/WORKSTATION_SETUP.md).
Use them for provenance,not current work order or process ownership.
