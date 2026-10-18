# Refined Home mode card qualification

The original full-height poster crosses the mode headline in all three modes at
both viewports. Its native geometry case fails; the choice-door/back control passes.
The desktop candidate moves the poster into the upper section and the headline
and ruleset into a separate lower caption. Both native cases pass without skips.

## Evidence

Base af6c2144fd75530ab1bd7eae9e762068bd81ab60; laptop gamergmae,
Windows 11, Unity 6000.5.8f1, isolated qa-a profile. Original Unity43428/
parent74862 exits2 with1fail/1pass. Candidate Unity28316/parent67005 exits0
with2passes. Each protects21126 inputs and restores279 generated deltas,
QualitySettings and13 existing editor preferences. Both parents are terminal.

Exact normalized candidate LF SHA256:
f5e8f7ad921f3833105eaa294ac7a396d42f5ce1c3dcf620c50954ac5ce5deb8.
Fixture LF b1075acdb660c1db85c2043de44f53395bb12242ceda6790779a8c08a47c29ed.
Metadata d3cecd6d741bfb2851e8ff5dee52ba2cc68126c1bc8795b499fe32e52d911f0f.
The previous07bb hash identifies CRLF bytes, not normalized LF.
The production source and fixture belong to the desktop; this commit is evidence only.

## Actual visual review

All six candidate original PNGs were individually inspected: Ranked,
Casual/Classic and Casual/Hero Strike at1280x720 and1920x1080.
The full title and ruleset fit with visible separation; ESKINITA and the change
arrow remain visible. No headline/character overlap appears in these frames.
The original Classic720 frame visibly places CASUAL over the character artwork.

The506x166 artwork container does not make the poster fill its width:
HeightControlsWidth retains the authored aspect ratio, leaving compact artwork
at the upper right. Its height is about111px at720 and166px at1080, larger than
the earlier110-reference-unit thumbnail (about73/110px). Character silhouettes
remain visible, but the Ranked composition is narrower than the casual images.
This is a readability improvement with an explicit compact-art tradeoff;
it is not evidence of a full-width poster or owner aesthetic acceptance.

## Limits

These are native Canvas render captures and real UI navigation tests, not physical
monitor screenshots or packaged-player acceptance. The fixture checks all mode
poster/headline bounds and a Choice2 door/back control; it does not test every
rank label, map name, aspect ratio, device or full gameplay. Older tiny-thumbnail
passes are not reused. Current protocol152 packaging, real peer matches,
network loss/recovery, QA Relay timeout and non-host lag remain unqualified.
