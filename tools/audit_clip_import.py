"""Check every clip the roster serialises against Unity's own imported artifact.

    python tools/audit_clip_import.py [report.json]

Run after Build Roster Book. No Unity scripts or gameplay code are generated. This
narrowly reads Unity 6000.5's version-23 artifact object table, and unexpected layouts
fail closed. ⚠️⚠️ **IT PROVES THAT WHAT THE ROSTER POINTS AT IS A CLIP WITH THE RIGHT
NAME. IT IS NOT A RENDER, NOT A NETWORK TEST, AND NOT MOTION PHOTOGRAPHY**
(`docs/TODO.md` § 151.16).

⚠️⚠️ IT USED TO BE `audit_slide_import.py` AND ASKED ABOUT ONE CLIP BY ITS HARD-CODED
`fileID`, WHICH IS THE `UiClickProbe` FAULT `CLAUDE.md` § 4a WARNS ABOUT: a checker
that carries a list cannot see the thing added after the list was written. Sean's three
casts landed and it had nothing to say about them. **It walks the roster's whole `Clips`
array now and compares the names it resolves against the animations actually in the
`.glb`**, so a clip authored next month is covered without editing this file, and a
clip that fails to import is a MISSING name rather than an absent question.

⚠️ WHY THAT COMPARISON IS THE RIGHT ONE. `RosterEntryAsset.Clips` exists because an
asset nothing points at is stripped from a player, and `CharacterAnimator` resolves a
body action by NAME against what shipped. So the two things that matter are that every
serialised reference resolves, and that the names on the other end are the names the
chain asks for. Counting them is not enough: 36 references that all resolve to `idle`
would pass a count.

⚠️ IT DOES NOT SHELL OUT. It used to call `rg`, which is not a dependency of this
repository, and died with a `WinError 2` on the profile where the only copy on disk is
inside a VS Code extension folder. One python pass over `Library/Artifacts`, about
2.1 GB in 3130 files here.

⚠️⚠️ AN ARTIFACT IS KEYED BY THE HIERARCHY ITS CURVES ADDRESS, because the one that
matters carries nothing else: no source path, no guid in any byte order, not even the
file name. That is the better key anyway, since the hierarchy is the exact thing that
has to match the rig at runtime. **The root name is READ from each `.glb` rather than
assumed from the file name: three team rigs carry another character's name at their
root** (`team-dante`'s is `team-bayan`, `team-cheska`'s is `team-inday`, `team-sean`'s
is `team-iggy`), left over from whichever rig each was branched off. Harmless in the
game, where every clip in a file addresses that file's own hierarchy. Not harmless to
anything matching by name, which is how an earlier version reported `team-dante` as
having no imported slide at all.
"""
import json
from pathlib import Path
import re
import struct
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from glb_mesh_dump import read_glb

ARTIFACTS = Path('Library/Artifacts')
PERSONS = Path('Assets/TumbangPreso/Art/characters/persons')
ROSTER = Path('Assets/TumbangPreso/Resources/Roster')
BOOK = Path('Assets/TumbangPreso/Resources/RosterBook.asset')

# Clips this project authors into the rigs itself, so the report can say out loud
# whether that work actually shipped and a missing one is a headline rather than a row.
#
# ⚠️ IT IS READ FROM THE AUTHORING TOOL RATHER THAN LISTED HERE. A hand-written list is
# what this file was replaced for: a checker carrying a list cannot see the thing added
# after the list was written, and the next hero session adds three more names.
#
# ⚠️⚠️ AS TEXT, NOT BY IMPORTING IT, and that is not laziness. `author_hero_action`
# imports `glb_action`, which imports `mathutils`, which exists only inside Blender: an
# import here would make this audit runnable only under `blender --background`, which is
# a ten-second launch to ask a question about a dictionary. Reading the source as text
# is `SceneScriptCheck`'s technique and it is the right one for the same reason.
AUTHORED = ('slide',) + tuple(sorted(set(re.findall(
    r'^\s+"(hero-[a-z]+-[a-z]+)": \{$',
    (Path(__file__).resolve().parent / 'author_hero_action.py').read_text(
        encoding='utf-8'), re.M))))

CLIP_TYPE = 3


def shipped():
    """The person entries the RosterBook actually holds, by asset stem.

    ⚠️⚠️ THE BOOK IS THE TRUTH AND THE FOLDER IS NOT, WHICH IS THE WHOLE REASON THIS
    FUNCTION EXISTS. `Assets/TumbangPreso/Resources/Roster/` holds 22 `person_*.asset`
    files and `Roster.People` owns 20 ids: `person_berto.asset` and `person_iggy.asset`
    match nothing, so `RosterBookBuilder.Fill` never opens them and they sit there
    carrying a model, a palette and a STALE clip list from whenever they were last
    written. Reading the folder therefore hands you two extra characters and two extra
    rigs, `team-bayan.glb` and `team-iggy.glb`, that no player can select.

    ⚠️ IT IS NOT A COSMETIC DIFFERENCE HERE. Those two rigs share a root node name with
    `team-dante` and `team-sean`, so including them makes the artifact key ambiguous and
    this tool refused to run at all rather than answering wrongly. `docs/TODO.md`
    § 151.18 is the entry for the orphans themselves.
    """
    text = BOOK.read_text()
    people = re.search(r'People:\n((?:  - \{fileID:[^\n]*\n)+)', text)
    assert people, BOOK
    guids = set(re.findall(r'guid: ([a-f0-9]+)', people[1]))
    live = {}
    for meta in ROSTER.glob('person_*.asset.meta'):
        guid = re.search(r'guid: ([a-f0-9]+)', meta.read_text())[1]
        if guid in guids:
            live[meta.name.removesuffix('.asset.meta')] = guid
    assert len(live) == len(guids), (sorted(guids - set(live.values())),
                                     'a RosterBook entry has no asset on disk')
    return live


def roster_entries():
    """Every SHIPPED roster person, with its model and the clip fileIDs it serialises."""
    live = shipped()
    wanted = {}
    skipped = sorted(p.stem for p in ROSTER.glob('person_*.asset')
                     if p.stem not in live)

    for stem in sorted(live):
        path = ROSTER / f'{stem}.asset'
        text = path.read_text()
        model = re.search(r'Model: \{fileID: -?\d+, guid: ([a-f0-9]+)', text)
        clips = re.search(r'Clips:\n((?:  - \{fileID:[^\n]*\n)+)', text)
        assert model and clips, (path, 'a shipped roster entry has no model or no clips')
        guid = model[1]
        candidates = [p for p in PERSONS.glob('*.glb.meta')
                      if f'guid: {guid}' in p.read_text()]
        assert len(candidates) == 1, path
        ids = [int(m) for m in re.findall(r'fileID: (-?\d+), guid: ' + guid,
                                          clips[1])]
        assert ids, path
        wanted[stem] = {'model': candidates[0].name.removesuffix('.glb.meta'),
                        'guid': guid, 'file_ids': ids}
    return wanted, skipped


def clip_names(model):
    gltf, _ = read_glb(str(PERSONS / f'{model}.glb'))
    root = gltf['nodes'][0].get('name')
    assert root, model
    names = [a.get('name') for a in gltf.get('animations', [])
             if not a.get('name', '').startswith('__preview')]
    return root, names


def read_clip(blob, data, index):
    """Name and rotation-curve count for one object-table entry."""
    path_id, offset, length, type_id = struct.unpack_from('<qQII', blob, index)
    start = data + offset
    assert start + length <= len(blob)
    # Editor AnimationClip: object flags plus three local PPtrs precede the name.
    name_len = struct.unpack_from('<I', blob, start + 40)[0]
    assert 0 < name_len < 256, (name_len,)
    name = blob[start + 44:start + 44 + name_len].decode('utf-8')
    curve_offset = start + 44 + ((name_len + 3) // 4) * 4 + 4
    return name, struct.unpack_from('<I', blob, curve_offset)[0]


def main(out=None):
    wanted, skipped = roster_entries()
    assert wanted, 'No roster entries with clips. Run Build Roster Book first.'

    roots, sources = {}, {}
    for entry in wanted.values():
        model = entry['model']
        if model in sources:
            continue
        root, names = clip_names(model)
        key = f'{root}/root/torso'.encode('utf-8')
        assert key not in roots, (model, root, roots[key],
                                  'two shipped rigs share a root node name')
        roots[key] = model
        sources[model] = names

    # One pass. For every artifact, work out which rig it belongs to and resolve every
    # fileID that rig's roster entry serialises.
    resolved = {}
    for path in sorted(ARTIFACTS.rglob('*')):
        if not path.is_file():
            continue
        blob = path.read_bytes()
        if len(blob) < 48 or struct.unpack_from('>I', blob, 8)[0] != 23:
            continue
        metadata, size, data = struct.unpack_from('>IQQ', blob, 20)
        if size != len(blob):
            continue
        hits = [m for key, m in roots.items() if key in blob]
        if len(hits) != 1:
            continue
        model = hits[0]

        found = {}
        for entry in wanted.values():
            if entry['model'] != model:
                continue
            for pid in entry['file_ids']:
                i = blob.find(struct.pack('<q', pid), 48, data)
                if i < 0:
                    continue
                name, rotations = read_clip(blob, data, i)
                found[pid] = {'name': name, 'rotation_curves': rotations}
        if found:
            resolved.setdefault(model, []).append({'artifact': path.name,
                                                   'clips': found})

    rows, faults = [], []
    for stem, entry in sorted(wanted.items()):
        model = entry['model']
        expected = set(sources[model])
        artifacts = resolved.get(model, [])
        # ⚠️ UNITY KEEPS PRIOR ARTIFACT REVISIONS, so a rig whose clips were re-authored
        # carries an old artifact beside the new one. The claim is that SOME imported
        # artifact resolves every name the `.glb` holds; a stale revision that resolves
        # fewer is expected and is reported rather than failed.
        best, names = None, set()
        for candidate in artifacts:
            got = {c['name'] for c in candidate['clips'].values()}
            if len(got) > len(names):
                best, names = candidate, got

        missing = sorted(expected - names)
        row = {'roster': stem, 'model': model, 'guid': entry['guid'],
               'serialized_clip_count': len(entry['file_ids']),
               'glb_clip_count': len(expected),
               'artifact': best['artifact'] if best else None,
               'resolved_names': len(names),
               'missing_names': missing,
               'authored_present': sorted(n for n in AUTHORED if n in names)}
        if missing or not best:
            faults.append(row)
        rows.append(row)

    source = Path('Assets/TumbangPreso/Runtime/Visual/CharacterAnimator.cs').read_text(
        encoding='utf-8')
    assert re.search(r'\{ "slide", new\[\] \{ "slide",', source)
    assert re.search(r'\{ "hero-sean-dash", new\[\] \{ "hero-sean-dash",', source)
    assert '_clips[c.name] = c;' in source
    assert 'if (_clips.ContainsKey(name)) return name;' in source

    report = {
        'scope': 'Unity imported clips and serialized roster references, '
                 'not motion photography',
        'rosters': rows, 'count': len(rows),
        'roster_assets_the_rosterbook_does_not_hold': skipped,
        'entries_with_unresolved_clips': [r['roster'] for r in faults],
    }
    if out:
        Path(out).write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')

    print(json.dumps({k: v for k, v in report.items() if k != 'rosters'}))
    assert not faults, [(r['roster'], r['missing_names']) for r in faults]


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else None)
