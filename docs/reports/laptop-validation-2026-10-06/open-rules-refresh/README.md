# Keep an open client Rules view current

During the same-package reverse LAN match, the PC's already-open read-only Rules panel kept 6x90/NONE after the host selected 4x30/NORMAL. Closing/reopening immediately showed the correct session values. This was stale presentation, not a wire failure.

`CustomGameScreen.Open` cloned session rules, but the screen observed no rules-change event. `MatchRpc.OnSyncRulesMsg` already adopts validated host rules before raising `OnRulesChanged`. The focused fix registers/unregisters a screen-lifetime listener and refreshes only an open read-only view from that current session clone. Enabling a retained screen also catches up. Host/offline working copies, preferences, layout, selected page and network semantics are preserved.

Original native source cf1c9d314df264c354f66dd1ebeb0abdde76dc0c: Unity42592/parent52606, seven cases: three causal failures (open rounds, open bot policy, observation after re-enable), four controls passing. Candidate Unity44200/parent45205: the same seven cases pass, exit0, none skipped. Controls exercise closed/reopened state, host broadcast without clobbering its draft, offline editor draft, and non-host packet refusal; visible room-page selection and unchanged saved rule preferences are asserted.

These tests build the actual Unity UI and invoke the actual receive callback with a logical peer provider and a genuine serialized string frame. They are native behavior evidence, not seven remote clients or packaged two-machine acceptance. Updated packaged open-panel acceptance remains open until a matching artifact includes this change.

Both parents are terminal; all21134 inputs restored after278 importer metadata changes plus ProjectAuditor settings churn. All13 existing isolated editor preference values were independently verified restored. Physical worker/company/product/profile differ from the standalone player. No hero, art, scene, package, protocol or saved-format changes. The candidate's three source/fixture/meta raw hashes match the native qualified inputs exactly.

The separate PC-host discovery repeat reproduced empty LAN/code failure while direct-address admission worked. The same existing917 player then stalled after one own in-game Quit and Input System Shutdown; exact owned-PID cleanup was required. That forced player exit is not normal-Quit success and does not change this native test result. See the separate current PC-host report.
