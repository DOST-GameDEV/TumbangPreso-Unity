# Integrated source findings,9716e31b8

Current local core run:626/626passed. Required source audits:11/14passed. The clock
finite-validator recognition correction now passes. The new three failures occur
in the incoming65b8a05dfUI/economy implementation; no earlier world production
logic was changed by that merge. These are exact audit findings, not yet proof of
runtime defects, and are not suppressed or marked passed.

- Subscription audit: HubCustom.cs62 `_map.Changed`,184 `_codeField.onValidateInput`;
  HubLoadout.cs188 `tile.Focused`; HubModeSelect.cs178 `card.Attention`;
  HubSkillTree.cs175 `node.Focused`. Anonymous handlers need either appropriate
  release or a supported shared-lifetime classification. Do not assume a leak
  solely from the text audit; retain the ownership evidence when resolving.
- Tournament classification: ConvertedMatchSetup.Hub.HubEnabled and launch switch
  `-tp-preparation-board` have no modifier/exemption row. Determine their actual
  effect and record the correct classification; do not label a visual switch a
  gameplay modifier merely to make the checker happy.
- Clock classification: WalletStore.cs155 uses DateTime.UtcNow without a persistent-
  timestamp classification. Confirm its daily-economy purpose and keep real-world
  accounting time separate from simulation time.

The incoming implementation is preserved unchanged. These remain shared integration
items. Source gate is FAILED until resolved; no whole-project qualification claim.
The six informational waveform DCflags remain visible separately. The first scoped
Editor pass is still importing on the clean owned checkout; no extra retry started.
