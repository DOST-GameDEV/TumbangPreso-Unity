"""Package the verified silent picture, editable timeline and selected inputs without machine-bound paths."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

ROOT=Path(__file__).resolve().parents[1]
MEDIA=ROOT/'Logs/trailer/bring-it-home'


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def tc(frame):return f'{frame//108000:02}:{frame//1800%60:02}:{frame//30%60:02}:{frame%30:02}'


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('tag')
    parser.add_argument('--otio-path',type=Path,default=MEDIA/'work/python-libs')
    args=parser.parse_args()
    sys.path.insert(0,str(args.otio_path))
    import opentimelineio as otio
    work=MEDIA/'edits'/args.tag
    witness=json.loads((work/'picture-witness.json').read_text(encoding='utf-8'))
    master=Path(witness['master'])
    if sha(master)!=witness['sha256']:raise ValueError('Master differs from accepted witness')
    package=MEDIA/'delivery'/f'TUMP_BringItHome_{args.tag}'
    package.mkdir(parents=True,exist_ok=False)
    clips=package/'clips';clips.mkdir()
    shutil.copy2(master,package/'TUMP_BringItHome_1080p30_SILENT.mp4')
    if witness.get('share'):shutil.copy2(witness['share'],package/'TUMP_BringItHome_720p_SILENT.mp4')
    for name in ('TRAILER_SYNC.csv','SOUND_BRIEF.md'):
        shutil.copy2(work/name,package/name)
    sound=package/'SOUND_BRIEF.md'
    sound.write_text(sound.read_text(encoding='utf-8').replace(
        f'Picture: `{args.tag}.mp4`.',
        f'Picture: `TUMP_BringItHome_1080p30_SILENT.mp4` (source cut `{args.tag}`).'),encoding='utf-8')
    shutil.copy2(ROOT/'docs/trailer/PICTURE_EDL.json',package/'PICTURE_EDL.json')
    timeline=otio.schema.Timeline(name='TUMP Bring It Home - SILENT 30fps')
    timeline.global_start_time=otio.opentime.RationalTime(0,30)
    track=otio.schema.Track(name='Silent picture',kind=otio.schema.TrackKind.Video)
    timeline.tracks.append(track)
    edit=['TITLE: TUMP BRING IT HOME SILENT','FCM: NON-DROP FRAME','']
    sources=[]
    for number,cut in enumerate(witness['cuts'],1):
        original=work/'clips'/cut['encoded'];target=clips/original.name
        if sha(original)!=cut['sha256']:raise ValueError('Encoded input differs: '+str(original))
        shutil.copy2(original,target)
        duration=cut['frames']
        available=otio.opentime.TimeRange(otio.opentime.RationalTime(0,30),otio.opentime.RationalTime(duration,30))
        reference=otio.schema.ExternalReference(target_url='clips/'+target.name,available_range=available)
        clip=otio.schema.Clip(name=cut['id'],media_reference=reference,source_range=available)
        clip.metadata['TUMP']={key:value for key,value in cut.items() if key!='sha256'}
        track.append(clip)
        reel=f'R{number:07d}'
        edit.extend([f'{number:03d}  {reel} V     C        {tc(0)} {tc(duration)} {tc(cut["master_from"])} {tc(cut["master_to"])}',
            '* FROM CLIP NAME: '+target.name,'* SOURCE FILE: clips/'+target.name,''])
        if cut['kind']!='graphics':sources.append(cut['take'])
    timeline_path=package/'TUMP_BringItHome.otio'
    otio.adapters.write_to_file(timeline,str(timeline_path))
    restored=otio.adapters.read_from_file(str(timeline_path))
    if restored.duration().to_frames()!=3600:raise ValueError('Portable timeline duration drift')
    for clip in restored.find_clips():
        if not (package/clip.media_reference.target_url).is_file():raise ValueError('Machine-bound/missing timeline source')
    (package/'TUMP_BringItHome.edl').write_text('\n'.join(edit),encoding='utf-8')
    (clips/'concat.txt').write_text(''.join("file '"+cut['encoded']+"'\n" for cut in witness['cuts']),encoding='utf-8')
    evidence=package/'capture-evidence';evidence.mkdir()
    for tag in sorted(set(sources)):
        take=MEDIA/tag;out=evidence/tag;out.mkdir()
        for name in ('capture.json','job.json','results.xml','telemetry.tsv','events.tsv','frame-times.tsv','geometry.tsv','shots.tsv','replay-transfer.json'):
            if (take/name).is_file():shutil.copy2(take/name,out/name)
        if (take/'replays').is_dir():shutil.copytree(take/'replays',out/'replays')
    source=package/'source';source.mkdir()
    owned=subprocess.check_output(['git','ls-files','Assets/TumbangPreso/Tests/PlayMode/Trailer','tools/*trailer*.py','docs/trailer','.claude/skills/tump-trailer/SKILL.md'],cwd=ROOT,text=True).splitlines()
    for name in owned:
        if Path(name).name in ('ANIMATIC.md','STORY.md','TRAILER_PLAN.md','trailer_animatic.py','trailer_sheet.py'):continue
        target=source/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/name,target)
    revision=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    clean_witness={key:value for key,value in witness.items() if key not in ('master','share','probe')}
    clean_witness['master']='TUMP_BringItHome_1080p30_SILENT.mp4'
    clean_witness['source_commit']=revision
    (package/'picture-witness.json').write_text(json.dumps(clean_witness,indent=2),encoding='utf-8')
    rebuild='''"""Run from any directory. Requires ffmpeg on PATH. No paid service or Unity needed."""
from pathlib import Path
import subprocess
root=Path(__file__).resolve().parent
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-n','-f','concat','-safe','1','-i',str(root/'clips/concat.txt'),'-map','0:v:0','-c','copy','-an','-movflags','+faststart',str(root/'REBUILT_SILENT.mp4')],check=True)
print(root/'REBUILT_SILENT.mp4')
'''
    (package/'rebuild_picture.py').write_text(rebuild,encoding='utf-8')
    # Exercise the delivered helper from a different working directory before shipping it.
    subprocess.run([sys.executable,str(package/'rebuild_picture.py')],cwd=MEDIA/'work',check=True)
    rebuilt=package/'REBUILT_SILENT.mp4'
    def video_hash(path):
        return subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-map','0:v:0','-c','copy','-f','hash','-hash','sha256','-'],text=True).strip()
    expected=video_hash(master);actual=video_hash(rebuilt)
    if actual!=expected:raise ValueError('Portable rebuild changed the picture bitstream')
    validation={'rebuild_from_different_working_directory':'passed','video_bitstream':actual,
        'full_file_sha256_equal':sha(rebuilt)==witness['sha256'],'timeline_roundtrip_frames':3600}
    (package/'PORTABILITY_CHECK.json').write_text(json.dumps(validation,indent=2),encoding='utf-8')
    rebuilt.unlink()
    if (work/'qa').is_dir():
        review=package/'picture-review';review.mkdir()
        for name in ('overview-01.jpg','overview-02.jpg','overview-03.jpg','opening-contact.jpg','pickup.jpg','escape.jpg','identity.jpg','review-evidence.json'):
            if (work/'qa'/name).is_file():shutil.copy2(work/'qa'/name,review/name)
        for path in sorted((work/'qa').glob('sweep-*.jpg')):shutil.copy2(path,review/path.name)
    for source_name,delivered_name in (('refinement-comparison.json','REFINEMENT_CHECK.json'),('cinema-geometry.json','CINEMA_GEOMETRY_CHECK.json')):
        if (work/source_name).is_file():shutil.copy2(work/source_name,package/delivered_name)
    readme=f'''# TUMP: Bring It Home

Start with TUMP_BringItHome_1080p30_SILENT.mp4. It is 120 seconds, 3600 frames,
1920x1080, 30 fps non-drop, H.264, BT.709 limited-range and has ZERO audio streams.
The team adds all music, voices, ambience and effects. The 720p copy is for review.

## Continue editing anywhere

Keep this folder intact. Open TUMP_BringItHome.otio in an editor with OTIO import,
then verify the 120-second timeline. Media references are relative to this folder.
The CMX EDL is an alternative for editors that support it; relink by clip name.
Editor-specific import has not been UI-tested overnight. As a universal fallback,
import clips in the order in PICTURE_EDL.json onto a 30fps timeline with no gaps.
Each supplied clip is already the exact selected length, with picture-only mode
labels where applicable. Native source selections/ranges are in the witness.

With Python 3 and ffmpeg on PATH, run `python rebuild_picture.py` to concatenate
the supplied selected inputs into REBUILT_SILENT.mp4 without re-encoding.
No Unity installation, account, internet connection or paid service is needed
for this assembly or for adding sound. SHA256SUMS.json verifies supplied files.

## Direction and boundaries

0-20 seconds is new original cinema; 20-88 is real native gameplay; 88-112 is new
original cinema; 112-120 is title/invitation. Keep gameplay inside its reserved
block. No old map films, ultimate films or existing game cutscenes are inserted.
No temp score, synthetic narrator, release date or invented platform promise.

TRAILER_SYNC.csv gives frame/timecode cue locations. SOUND_BRIEF.md gives the arc.
Accepted native event callbacks use a documented +1 capture-frame alignment.
Verify actual visible impacts before final sound spotting. The owner has not yet
approved playback or emotional quality. Background-only review used extracted
frame sequences; decoding/metadata checks do not replace human viewing.

## Regenerate original images

Repository: https://github.com/DOST-GameDEV/TumbangPreso-Unity.git
Branch: trailer/pandaigdig-plan. Source commit: {revision}
Unity: 6000.5.8f1. Full project/assets are needed to render, not to edit these clips.
`source/` mirrors the trailer directors, recording code, edit tools and documents.
The README in that repository plus docs/trailer/PRODUCTION_CHECKPOINT.md own the
current render workflow. Never reset a workspace/profile or discard unrelated work.

capture-evidence includes accepted scene/mode/event telemetry and actual replay
archives from local native bot matches. It preserves real outcomes. Replay rendering parity on another machine
is not certified. Selected encoded clips are the portable visual source of truth.
No unrelated profiles, saves, credentials or private owner files are included.
'''
    (package/'START_HERE.md').write_text(readme,encoding='utf-8')
    sums={path.relative_to(package).as_posix():sha(path) for path in sorted(package.rglob('*')) if path.is_file()}
    (package/'SHA256SUMS.json').write_text(json.dumps(sums,indent=2),encoding='utf-8')
    for name,digest in sums.items():
        if sha(package/name)!=digest:raise ValueError('Packaged file checksum failed')
    bundle=package.with_suffix('.zip')
    with zipfile.ZipFile(bundle,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=2) as archive:
        for path in sorted(package.rglob('*')):
            if path.is_file():archive.write(path,path.relative_to(package.parent))
    with zipfile.ZipFile(bundle) as archive:
        if archive.testzip() is not None:raise ValueError('ZIP CRC failed')
    print(json.dumps({'folder':str(package),'zip':str(bundle),'zip_bytes':bundle.stat().st_size,'files':len(sums)+1,'timeline_frames':3600,'source_commit':revision},indent=2))


if __name__=='__main__':main()
