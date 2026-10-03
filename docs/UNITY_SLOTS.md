# Local Unity slots

Claude's current Editor uses the main ASTRAReworks checkout. Preserve it and its
processes. Codex tests must run in a separate validation-only worktree with a
physical independent Library, different company/product identity, named profile
and dedicated logs/results. Never run a second Editor against the same project.

The runner supports one verified outside Editor beside one isolated headless
EditMode worker. Use `--allow-parallel --coexist-editor-project <outside-project>`
and `--coexist-reserve-mb 1024` or more. Detection verifies every outside Unity
process's project path. Unknown processes, standalone players, shared preferences,
shared caches, graphics/build jobs and a third slot are refused. Outside processes
are never adopted or stopped by the runner.

Admission retains the worker budget, normal reserve and an additional growth
reserve for the outside Editor. Every two seconds during execution, the runner
also checks outside processes and physical memory. A new incompatible Editor,
unverified outside process or crossed reserve ends only our verified Editor,
waits for its guard to restore preferences and records interruption exit125.
An interruption is not a product failure or a passed test. This PC has 16 GiB RAM; two graphics workloads
are not an assumed safe default. If memory is insufficient, work on source while
the other Editor runs. Full builds and graphics acceptance remain coordinated.

Always target tools by explicit project path. Do not install another MCP bridge
to obtain another Editor slot. Close only owned Editors after a test; retain the
worker only while it is used and prune obsolete derived outputs after completion.

Runner qualification: 33 local safety/lifecycle cases pass, including coexistence,
project/preferences collision, graphics/network refusal and extra memory reserve.
This includes monitored interruption with restoration and preserved outside
applications. Independent native slot smoke passed1/1; safe simultaneous
headless/graphics execution still needs its own accepted overlap receipt.

Unity documents explicit project and log targeting in its
[Editor command-line reference](https://docs.unity.com/en-us/engine/6000.5/manual/unity-editor/command-line-arguments/editor).
