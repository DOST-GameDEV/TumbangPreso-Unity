# Desktop Home mode-card candidate: laptop native qualification

The original poster overlaps the mode headline at both720p and1080p in all three
modes. The desktop's exact candidate confines it to the existing upper-right
artwork area; the corrected fixture passes2/2 with six actual candidate PNGs.
The normal ModeCard door/back control passes on both versions. Desktop owns
source/fixture publication; laptop main HubHome remains unchanged by this unit.

## Exact candidate and behavioral scope

Original normalized HubHome SHA1a0c3fdaf6ac228d1ad44fa919d517a908aea42338958ff00c670fa2450706b5;
candidatea2f91087c711526fe90f1b0bd0f0e4c9d5bc94a255d5c935db6b10de6d8fbde1.
Replace the full-card stretched PosterWindow with a TopRight200x110 region,
offset(-62,-8). Preserve poster bytes, headline90px/460x110 region, card/hit area,
callback and authored character designs. Bounds and actual pixels qualify this
separation; human taste and a current packaged-player review remain separate.

Base8d786d6f550595c453f065140ca5cecb182d6a2a includes the checked reader fix;
its UI matches the desktop's supplied f5 baseline. New test metadata GUID
666d16ac93f7420fb2c55445bae306c7, normalized SHA d3cecd6d741bfb2851e8ff5dee52ba2cc68126c1bc8795b499fe32e52d911f0f.

## Fixture failures retained before valid comparison

The initial desktop fixture e5cb expected RANKED for choice0 but called Tick
after setting Choice. Tick only updates queue/notices; it does not refresh the
mode card. It therefore saw stale CASUAL and failed before geometry or captures.
Original1 Unity20616/parent27869 ended with1fixture failure/1door control pass,
zero accepted captures. That failure is not attributed to poster overlap.

The laptop initially assumed the index mapping was wrong and changed the label
expectation to choice2. That was a mistake: actual source still maps0 to RANKED.
Original2 Unity26800/parent95573 failed the caption later, retaining four partial
captures locally. This intermediate attempt is not qualified geometry evidence.

Source inspection and desktop review isolated the fixture dispatch problem.
Restore the strict choice0 RANKED assertion and replace only Tick with the public
Resumed lifecycle method used on return from mode selection. This refreshes the
actual card without invoking private Update or weakening overlap, labels, door,
resolution or action-bound checks. Correct fixture normalized SHA:
b1075acdb660c1db85c2043de44f53395bb12242ceda6790779a8c08a47c29ed.
The exact corrected fixture is retained as qualified-fixture.cs.txt.

## Valid native comparison

Laptop gamergmae/Windows11, Unity6000.5.8f1/D3D11, isolated warm qa-a worker,
named validation profile. Filter HomeModeCardReadabilityTests, two cases.

- Correct original Unity26376/parent88847: actual geometry FAIL for all six
  mode/viewport combinations; normal door/back control PASS. Six original PNGs.
- Exact candidate Unity3796/parent43161:2/2PASS, no skips. Six candidate PNGs.

The files in original/ and candidate/ have PNG magic and exact1280x720 or
1920x1080 dimensions. They are engine UI render-target captures from the existing
TumpUiCapture path, not Sky JPEGs or desktop viewport/DPI evidence. The original
and candidate1280 Hero Strike frames were visually inspected: the candidate
poster is separate from the CASUAL headline. No original artwork was re-encoded.

All four attempt parents are terminal. Each freezes19318inputs and preserves
and restores265metadata/Auditor changes, Quality and13existing preference values.
Original six captures were copied to their own run before candidate output could
overwrite the shared capture names. No additional broad checks, new workers,
game rebuilds or desktop-PC control were used.

## Remaining limits

This is scoped source/native UI qualification, not proof of the pending source
commit, QoL merge, release artifact, network recovery, physical input or full
tournament readiness. Root is integrating QoLUpdates and owns any incoming UI
conflicts, root queue and final source publication. Preserve the exact candidate
and causal evidence while checking that integration against its actual source.
