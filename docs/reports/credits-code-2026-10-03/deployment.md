# Production activation: October 4 update

Existing authorized deployment access succeeded on October4. Production walletv2
now exactly matches canonical source with all four declared parameters and v1
rollback retained. [Current receipt](../reliability-2026-10-04/wallet-deployment/README.md).
Rendered signed-in reward and Victory audio still require acceptance.

## Retained October 3 failure and activation scope


The Unity project's current shipping ID is dcf0831e-a5f4-43b4-832e-b687f13a3569; environment production; Cloud Code script wallet. Source implementation was published at79e9e2724/integrated3d9f6bb6b.

Official Unity UGS CLI2.0.0 was verified against official release SHA256f1c04e4e71105a3ed3ded94c6ed94dbba527316e5b6c2670e3463bcdfe7b8639. Read-only single-call Unity Hub authentication reached GetEnvironments but returned403 Not authorized. No server update was attempted. Do not describe the cheat as live.

With an account already authorized to deploy this project, publish ONLY ugs/cloud-code/wallet.js, then read back wallet and verify exact source plus four declared parameters: action, item, task, request. Do not deploy the entire cloud-code directory unintentionally. Existing PLAYTEST_TOPUP999999 is retained in this source; this is separate from the fixed5000 Credits grant. Inspect the existing live version before activation to avoid overwriting an unrelated remote edit.

Official instructions: https://docs.unity.com/en-us/cloud-code/scripts/how-to-guides/write-scripts/cli

Live acceptance: isolated authorized test profile, note initial balance, enter eight directions in Credits, confirm +5000 and Victory cue, then repeat with a fresh sequence and verify a new +5000. Never treat source-only or mocked wallet checks as this live test.
