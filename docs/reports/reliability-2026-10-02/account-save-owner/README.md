# Keep delayed profile saves with their submitted account and edit

`SaveCloudProfileAsync` previously merged its delayed canonical reply into whatever
profile was current at completion. A response from a previous account could replace
the new account or guest. A late reply could also overwrite a newer local edit, and
a canonical payload with a different player ID could be persisted.

The save captures the submitting profile, owner, JSON and request generation. It
applies a reply only while that exact signed-in, non-guest profile and edit remain
current and the request is still the latest save. A canonical profile must carry
the same owner ID. Normal current-owner replies retain their existing merge and
persistence behavior. No account schema or endpoint changed.

Native EditMode baseline: **four reproduced failures and one passing control**.
Candidate: **5/5 passed**, zero repairs. The tests await the actual save method
through a private nullable dispatch seam and a controlled delayed task. Production
continues through the existing Cloud Code call whenever the seam is unset. Cases
cover account replacement, guest replacement, newer edits, foreign canonical
owner and a successful current-owner canonical save.

Both native jobs exited, restored profile/input state and released their leases.
The inactive account component avoids boot authentication; the controlled replies
make no SDK or service calls. The existing settings seam and named test profile
isolate writes. This proves local completion ownership, not live authentication or
the deployed endpoint.

Raw XML and guarded job receipts are retained here. Source before the fix was
`066bb51eeb4e08ba3182955388c9e2b8715fa509` plus only the dispatch seam. Qualification
logs: `C:/Users/matth/Documents/Codex/work/tump-feedback-0930/Logs/account-save-owner1002`.
Baseline session54240, final10551; the five-case fixture is unchanged.
