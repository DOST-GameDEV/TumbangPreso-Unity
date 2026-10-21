# Arena replacement-owner teardown

Disabling or destroying an obsolete Arena stage previously cleared edge sensing
and the movement floor even when a newer stage owned them. Disabling an obsolete
Arena recovery likewise cleared the newer instance's fall/vine hooks because
both instances publish the same static methods. These are confirmed component
ownership defects on the merged QoL source, not claimed Relay timeout causes.

Stage teardown now resets global state only when Instance is this stage. Its
per-instance morph meshes are still disposed regardless of ownership. Recovery
teardown always releases its own watch/dictionaries, but clears shared hooks only
when it is the current Instance. Existing delegate comparisons still preserve
foreign replacement callbacks. No Net source, design, timing, geometry, authored
art or gameplay kit is changed.

## Native causal comparison

Laptop gamergmae/Windows11, Unity6000.5.8f1/D3D11, isolated warm qa-a worker,
named profile, five public lifecycle cases. Tests use real SetActive/Destroy,
current owner references and global values/hooks; no private lifecycle invocation.

- Original merged1389ab500, Unity2688/parent67311: three causal FAIL and two
  current-owner cleanup controls PASS. Retired disable/destroy cleared edge
  sensing; retired recovery removed the current fall callback.
- Candidate merged5aa0140ec plus this correction, Unity40484/parent21061:
  all5PASS, no skips. Stage floor and both hooks remain owned by the new instance;
  disabling the current stage/recovery still restores normal-map defaults.

Original source was already frozen when5aa arrived, so it was finished without
restart. After terminal exact restoration, only the new MatchPoseHistory replay
material delta was applied before candidate freeze. Arena sources were unchanged
between those base commits; the material delta cannot establish or fix these
bare-component lifecycle outcomes. Exact source hashes are in checked-source.json.

Both runs compile/import the merged source and protect21118inputs. Each preserves
and restores265generated metadata/Auditor changes, Quality and13existing preference
values. All parents are terminal. This is merged native compilation and scoped
lifecycle evidence, not a player build, full Arena scene/roster qualification,
impaired transport or current two-machine acceptance. Those affected checks follow
on the coherent merged candidate; prior61a package results are not attributed here.
