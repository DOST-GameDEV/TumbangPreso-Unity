# Wallet state follows the current account

Native baseline five cases: two normal-owner controls pass; old reply overwrites
current balance77with420, account notification retains the prior known wallet,
and Busy consumes its scheduled refresh. Current fix rejects stale reply/error
owners, clears only another account's cache/payout, captures/releases the account
notification hook and leaves a scheduled refresh pending until Busy clears.
Final5/5 passes. Same-owner balance/payout and ordinary completion remain intact.
No wallet schema, prices, reward rules, purchase policy or offline play changes.

Unity6000.5.8f1 EditMode, isolated tump-feedback-0930, wallet-response-owner1002
profile. Two frozen source hashes unchanged. Synthetic wallet answers and offline
identity only: no buy/claim/service/account-authentication call. Guard restored
profile/input. One orchestration repair used: UTF8 log decode failed before
baseline-copy completion; premature wrong-input launch was stopped and preserved,
with no result claimed. baseline-ready is the actual product baseline. No fixture
repair or repeated unchanged test. Logs remain in isolated Logs/wallet-response-owner1002.
