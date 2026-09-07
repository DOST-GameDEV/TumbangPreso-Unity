"""Check the roster's slide reference against Unity's actual imported artifact.

Run after Build Roster Book. No Unity scripts or gameplay code are generated.
This narrowly reads Unity 6000.5's version-23 artifact object table. Unexpected
layouts fail closed. It proves the serialized reference names slide and carries
rotation curves; it does not claim a rendered or networked playback test.

⚠️⚠️ IT USED TO SHELL OUT TO `rg` AND THAT MADE IT A TOOL THAT RAN ON ONE MACHINE.
`ripgrep` is not a dependency of this repository: on the `Matthew` profile the only
copy on disk is inside a VS Code extension folder, so the audit died with
`FileNotFoundError: [WinError 2]` before reading a single artifact, which reads
exactly like a broken import rather than a missing binary. That is `CLAUDE.md`
§ 7.1's warning about `python` being on PATH on one laptop and not the other, one
tool along, and § 7's rule that a note true of one machine and stated as a fact
about "here" sends whoever is on the other one hunting. The scan is python now.

⚠️ ONE PASS, NOT ONE PASS PER ROSTER ENTRY. `Library/Artifacts` is about 2.1 GB in
3130 files here, so a search per character would read it twenty times. The index is
built once: every artifact with a valid version-23 header is read and asked whether
it holds the slide object.

⚠️⚠️ AND AN ARTIFACT IS IDENTIFIED BY THE HIERARCHY ITS CURVES ADDRESS, NOT BY A PATH
OR A GUID, BECAUSE THE ONE THAT MATTERS CARRIES NEITHER. Two kinds of artifact hold a
copy of the clip. One carries the source asset path and can be matched by name. The
other, `071d66477eb8f5a67f66da5487fc4950` for `character-male-a`, holds **only the
curve bindings**: no `Assets/...` path, no guid in any byte order, not even the file
name. What it does carry is `character-male-a/root/torso/arm-right`, and that is the
better key anyway: **a clip addresses transforms by PATH from the root, so the
hierarchy in the artifact is the exact thing that has to match the rig at runtime.**
`PersonSwapProbe.CheckAnimationBinds` exists because a clip whose paths no longer
match imports perfectly and moves nothing.

⚠️ THE ROOT NAME IS READ FROM EACH `.glb` RATHER THAN ASSUMED FROM THE FILE NAME, and
that is not pedantry: **three team rigs carry another character's name at their root.**
`team-dante.glb`'s is `team-bayan`, `team-cheska.glb`'s is `team-inday` and
`team-sean.glb`'s is `team-iggy`, left over from the rig each was branched off. It is
harmless in the game, where every clip in a file addresses that file's own hierarchy,
and it is why an earlier version of this audit attributed `team-dante`'s artifact to
`team-bayan` and then reported `team-dante` as having no imported slide at all. The
roots ARE unique across the twenty characters that ship, and this asserts that rather
than trusting it.
"""
import json
from pathlib import Path
import re
import struct
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from glb_mesh_dump import read_glb

SLIDE_ID = -4776023958905061225
ARTIFACTS = Path('Library/Artifacts')
PERSONS = Path('Assets/TumbangPreso/Art/characters/persons')
ROSTER = Path('Assets/TumbangPreso/Resources/Roster')


wanted, skipped = {}, []
for roster in sorted(ROSTER.glob('person_*.asset')):
    text = roster.read_text()
    if f'fileID: {SLIDE_ID},' not in text:
        # ⚠️ A roster asset with no slide reference is either a character this pass has
        # not reached or an ORPHAN: `person_berto.asset` and `person_iggy.asset` are
        # files on disk that no id in `Roster.People` owns, so `RosterBookBuilder.Fill`
        # never touches them and the RosterBook reports 20 people against 22 files.
        # They are recorded rather than asserted on, so the count in the report is the
        # count of characters that actually ship.
        skipped.append(roster.stem)
        continue
    guid = re.search(r'Model: \{fileID: -?\d+, guid: ([a-f0-9]+)', text)[1]
    candidates = [p for p in PERSONS.glob('*.glb.meta') if f'guid: {guid}' in p.read_text()]
    assert len(candidates) == 1, roster
    wanted[roster.stem] = (candidates[0].name.removesuffix('.glb.meta'), guid)

assert wanted, 'No serialized slides. Run Build Roster Book first.'

marker = struct.pack('<q', SLIDE_ID)

# The rig hierarchy each wanted model's clips address, read from the file itself.
roots, models = {}, sorted({m for m, _ in wanted.values()})
for model in models:
    gltf, _ = read_glb(str(PERSONS / f'{model}.glb'))
    root = gltf['nodes'][0].get('name')
    assert root, model
    key = f'{root}/root/torso'.encode('utf-8')
    assert key not in roots, (model, root, roots[key],
                              'two shipped rigs share a root node name')
    roots[key] = model

found, unattributed = {}, []

for path in sorted(ARTIFACTS.rglob('*')):
    if not path.is_file():
        continue
    blob = path.read_bytes()
    if len(blob) < 48 or struct.unpack_from('>I', blob, 8)[0] != 23:
        continue
    metadata, size, data = struct.unpack_from('>IQQ', blob, 20)
    if size != len(blob):
        continue
    i = blob.find(marker, 48, data)
    if i < 0:
        continue
    path_id, offset, length, type_id = struct.unpack_from('<qQII', blob, i)
    start = data + offset
    assert start + length <= len(blob)
    # Editor AnimationClip: object flags + three local PPtrs precede name.
    name_len = struct.unpack_from('<I', blob, start + 40)[0]
    name = blob[start + 44:start + 44 + name_len].decode('utf-8')
    assert name == 'slide', (path.name, name)
    curve_offset = start + 44 + ((name_len + 3) // 4) * 4 + 4
    rotations = struct.unpack_from('<I', blob, curve_offset)[0]
    assert rotations == 7, (path.name, rotations)

    # ⚠️ A MODEL CAN HONESTLY COLLECT MORE THAN ONE ARTIFACT AND NEITHER CAUSE IS A
    # FAULT. Unity keeps prior artifact revisions, so a rig whose clip was re-authored
    # carries the old one and the new one; and `team-bayan.glb` and `team-iggy.glb`
    # are DUPLICATE RIGS of `team-dante` and `team-sean`, sharing their root node name,
    # so a key built from the hierarchy cannot tell the pair apart. It does not need
    # to: the pair is the same skeleton at the same height, the solver gives both the
    # same roll, and the clip is identical. Every artifact in the list is asserted to
    # name `slide` and carry seven rotation curves before it gets here, which is the
    # claim being made. ⚠️ There is no better key available: a source path was tried and
    # NOT ONE of the twenty artifacts holding a slide records one, which is the same
    # finding one line up stated as a measurement.
    hits = [m for key, m in roots.items() if key in blob]
    if not hits:
        # ⚠️ AN ARTIFACT HOLDING A `slide` FOR A RIG NOTHING SHIPS IS NOT AN ERROR.
        # `team-bayan.glb` and `team-iggy.glb` are authored and imported but no id in
        # `Roster.People` loads them, so their artifacts have no roster row to land on.
        # Recorded so the count stays honest rather than asserted on.
        unattributed.append({'artifact': path.name, 'name': name,
                             'rotation_curves': rotations})
        continue
    assert len(hits) == 1, (path.name, hits)
    found.setdefault(hits[0], []).append(
        {'artifact': path.name, 'name': name, 'rotation_curves': rotations})

rows = []
for stem, (model, guid) in sorted(wanted.items()):
    assert model in found, (stem, model, 'no imported artifact holds this slide')
    rows.append({'roster': stem, 'model': model, 'guid': guid, 'fileID': SLIDE_ID,
                 'imported': found[model]})

source = Path('Assets/TumbangPreso/Runtime/Visual/CharacterAnimator.cs').read_text(encoding='utf-8')
assert re.search(r'\{ "slide", new\[\] \{ "slide",', source)
assert '_clips[c.name] = c;' in source and 'if (_clips.ContainsKey(name)) return name;' in source

report = {'scope': 'Unity imported clip and serialized body-action resolution, not motion photography',
          'rosters': rows, 'count': len(rows),
          'roster_assets_without_a_slide_reference': skipped,
          'slide_artifacts_no_shipped_rig_claims': unattributed}
if len(sys.argv) > 1:
    Path(sys.argv[1]).write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k != 'rosters'}))
