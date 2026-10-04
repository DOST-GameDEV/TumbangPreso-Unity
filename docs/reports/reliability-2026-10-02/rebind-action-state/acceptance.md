# Rebind restores the original action state

`RebindSession` previously enabled its action on every close or disposal, including an action that was disabled before listening. It now records `target.enabled` before the temporary disable and restores that state before reporting completion. Cancellation, disposal, accepted bindings and conflict refusals preserve their existing binding and callback behavior.

The final guarded Unity 6000.5.8f1 EditMode run passed all 10 focused `RebindActionStateTests` cases. Each ending was exercised with an initially disabled and enabled action, and two refused gamepad starts checked that state and bindings remain untouched. Tests drive the real rebinding operation's `Cancel` or `AddCandidate` plus `Complete`, or public session disposal. Accepted completion changes the binding; other endings preserve an existing override. No physical keyboard, gamepad or gameplay acceptance is claimed.

## Causal evidence

- Original joint baseline18: Rebind6 passed/4 failed, exactly the disabled cancel/dispose/bound/conflict cases. Ready2 passed/2 fixture failures; Buffer1 passed/3 fixture failures, before their product assertions.
- One jointly authorized fixture repair changed only Ready/Buffer input setup to transient cloned InputSettings. The second joint baseline18 retained Rebind6 passed/4 identical causal failures, but Ready0 passed/4 setup failures and Buffer1 passed/3 setup failures. The installed InputManager settings setter destroys a replaced HideAndDontSave settings object, so those fixtures could not restore their retained original object between cases. No second repair was attempted.
- Independent final10: Rebind10 passed/0 failed, fresh XML and exit0. The Rebind fixture/meta bytes are identical across every run. Only RebindSession's captured enabled state and restoration changed for its final acceptance. ReadyGate and BufferSkipVote remained original production sources in qualification; their fixes are unqualified by these runs.

Sessions: first baseline 85263 (pool 22844, guard 21052), repaired baseline 84619 (pool 22924, guard 18576, Editor 21108), independent final 89649 (pool 22848, guard 10644, Editor 20672). Every preparation completed exit0 before its dependent launch. All runs used the serialized CPU pool, 1536MB budget/2048MB reserve, 450-second child limit and named `rebind-ready-buffer-chat1002` profile. No SDK calls, builds or additional broad tests ran in this unit.

All three guard receipts confirm terminal restoration and no held lease. The final check matched all 1284 original protected source/private hashes plus six Ready/Buffer paths (1290 total), and the three owned native inputs matched their frozen hashes. Guards restored the named profile and one shared Editor input preference; no existing profile files were present. The failed fixtures destroyed transient in-memory input settings, not a persisted asset through an asset-save API; no changed protected disk file or profile was observed. A fresh Editor ran the independent final10.

After all jobs were terminal, root explicitly authorized retiring only the four NEW failed Ready/Buffer fixture/meta files from qualification into the task-owned Logs evidence folder. `retired-fixtures.json` records exact preserved bytes. This happened after `protected-post.json`; no original tests were removed. No task-owned Unity/player process remained when the slot was released.

## Inputs and limits

Frozen Rebind baseline SHA256: `D108277A64D8404258DFBDB57D46A3D49EC9A9436CE261B12B117D153DE25FB2`.

Final source SHA256: `37B74BD7003B59C0DEBFB00E1A04272676174A720E145E14D8001FDCE600BB58`.

Unchanged fixture SHA256: `A48D8079D0922B0D8AE663B2230701CBED85BC74903E6463F087A2BFDD8048A3`; meta `849BEF1EDC5A401DCBFBC81D3D35FD28D90D84319123BB2A7C814FBA2EF90DB4`.

Raw XML, logs, pool receipts and preparation/protected hash manifests are retained beside this report. This validates the action-state lifecycle change, including callback ordering and binding preservation. It does not establish full competition readiness, physical input behavior or a standalone build containing this fix.
