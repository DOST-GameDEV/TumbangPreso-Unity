# Abandon and history requests retain account ownership

Native production completion baseline: current-account abandon/history pass;
replaced-account abandon overwrites XP77 with420 and history returns the old
account's results. Final4/4 pass after capturing the requested cache and rejecting
stale completion. Abandon leaves the new profile untouched; history cancels, and
the existing history screen ignores that expected cancellation. Normal current
profile/history response shapes and local failure fallback remain intact.

Unity6000.5.8f1 EditMode, isolated tump-feedback-0930 checkout, named
career-async-owner1002 profile, no live endpoint/account/authentication calls.
Three source hashes unchanged during final run; zero fixture repairs; guarded
profile and shared-input restoration completed. Native logs remain in isolated
Logs/career-async-owner1002. No refreshed player, full screen navigation, actual
account switch or deployed-backend claim from these four completion cases.
