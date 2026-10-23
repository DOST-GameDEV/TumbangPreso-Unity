"""C1: Home Before Dark. Explicit composition and MIDI, rendered by physical/string synthesis."""
from pathlib import Path
import hashlib
import json
import argparse
import os
import urllib.request
import subprocess
import sys

import mido
import numpy as np
from pedalboard import Pedalboard, Compressor, HighpassFilter, LowpassFilter, PeakFilter, Reverb

from tondo_challenger_synth import Renderer, write_wav

REPO = Path(__file__).resolve().parents[1]
OUT = REPO / 'docs/reports/sfx-2026-10-23/challenger/regenerated'
ROOT = OUT / 'work'
CLIPS = OUT / 'clips'
PLUGIN = None


def configure():
    global OUT, ROOT, CLIPS, PLUGIN
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', type=Path, default=OUT, help='Audition output directory; never installs Resources assets')
    parser.add_argument('--plugin', type=Path, help='Path to the inner Surge XT VST3 binary')
    parser.add_argument('--composition-only', action='store_true', help='Write timed notes and MIDI without loading a synth')
    parser.add_argument('--mix-only', action='store_true', help='Reuse this output directory unscaled renders')
    args = parser.parse_args()
    OUT = args.out.resolve()
    ROOT = OUT / 'work'
    CLIPS = OUT / 'clips'
    PLUGIN = args.plugin
    return args


def prepare_synth():
    global PLUGIN
    manifest = json.loads((REPO/'tools/tondo_challenger_sources.json').read_text())
    if PLUGIN is None:
        override = os.environ.get('TUMP_SURGE_VST3')
        local = Path(os.environ.get('LOCALAPPDATA', str(Path.home()/'AppData/Local')))
        relative = 'TUMP-audio-tools/surge-xt-1.3.4/x/Surge XT.vst3/Contents/x86_64-win/Surge XT.vst3'
        candidates = ([Path(override)] if override else []) + [local/relative]
        candidates += sorted((local/'Packages').glob('Claude*/LocalCache/Local/'+relative))
        candidates += [Path('C:/Program Files/Common Files/VST3/Surge Synth Team/Surge XT.vst3/Contents/x86_64-win/Surge XT.vst3')]
        PLUGIN = next((path for path in candidates if path.is_file()), None)
    if PLUGIN is None or not PLUGIN.is_file():
        raise FileNotFoundError('Pass --plugin or set TUMP_SURGE_VST3 to an installed Surge XT VST3 binary')
    actual = hashlib.sha256(PLUGIN.read_bytes()).hexdigest()
    if actual != manifest['engine']['sha256']:
        print('Synth binary differs from the approved render; treat this regeneration as a new audition.', flush=True)
    cache = ROOT/'patches'
    cache.mkdir(parents=True, exist_ok=True)
    for entry in manifest['patches']:
        target = cache/entry['name']
        if not target.exists():
            with urllib.request.urlopen(entry['url'], timeout=60) as response:
                data = response.read()
            if hashlib.sha256(data).hexdigest() != entry['sha256']:
                raise ValueError('Downloaded preset hash differs: '+entry['name'])
            target.write_bytes(data)
        if hashlib.sha256(target.read_bytes()).hexdigest() != entry['sha256']:
            raise ValueError('Cached preset hash differs: '+entry['name'])


SR = 44100
LENGTH = 19.6
YA = 12.9583
ORIGIN = YA - 32 * .4
NAMES = {'C':0,'C#':1,'D':2,'D#':3,'E':4,'F':5,'F#':6,'G':7,'G#':8,'A':9,'A#':10,'B':11}
lead, guitar, accent = [], [], []


def pitch(name):
    return (int(name[-1]) + 1) * 12 + NAMES[name[:-1]]


def add(part, time, name, length, velocity):
    part.append((round(time, 5), pitch(name), length, velocity))


def chord(time, names, velocity, length=.52, spread=.021):
    for i, name in enumerate(names.split()):
        add(guitar, time + i * spread, name, length - i * spread, velocity + [0,-6,-2,1][i % 4])


def phrase(start, sequence):
    for offset, name, length, velocity in sequence:
        add(lead, start + offset, name, length, velocity)


def compose():
    # Owner direction: ONE tempo and ONE theme. A two-bar tune and A-D guitar
    # vamp remain intact across camera cuts. Only phrase dynamics change.
    motif=[(0,'E5',.31,0),(.4,'C#5',.66,5),(1.2,'B4',.31,-3),
           (1.6,'A4',.66,1),(2.4,'B4',.29,-4),(2.8,'A4',.27,2)]
    for repeat,(lead_velocity,guitar_velocity) in enumerate([(61,46),(69,53),(74,58),(63,48),(75,59),(65,50)]):
        start=ORIGIN+.4+repeat*3.2
        for offset,name,length,weight in motif:
            time=start+offset
            # The YA rake supplies this A; keep the real voice's next breath clear.
            if abs(time-YA)<.02 or 13.0<time<13.55:
                continue
            add(lead,time,name,length,lead_velocity+weight)
        # The same thumb-and-fingers pattern, with light attacks between pulses.
        for offset,name,length,weight in [(0,'A3',.45,0),(.8,'E4',.27,-7),
                                          (1.6,'D3',.45,0),(2.4,'A3',.27,-7)]:
            add(guitar,start+offset,name,length,guitar_velocity+weight)
        for offset,names,weight in [(.4,'C#4 E4',-2),(1.2,'C#4 E4',-7),
                                   (2.0,'D4 F#4',-2),(2.8,'D4 F#4',-7)]:
            chord(start+offset,names,guitar_velocity+weight,.22,.014)
    # One chord from the same string vocabulary, not a new sound or new tune.
    for i,name in enumerate(['A3','C#4','E4','A4']):
        add(accent,.006+i*.012,name,.17-i*.015,77+i*3)
    # Keep each instrument's repicked MIDI pitch physically exclusive.
    for part in [lead,guitar,accent]:
        part.sort()
        for i, note in enumerate(part):
            for nxt in part[i+1:]:
                if nxt[1] == note[1]:
                    part[i] = (note[0],note[1],min(note[2],max(.025,nxt[0]-note[0]-.012)),note[3])
                    break


def save_midi(parts):
    midi = mido.MidiFile(ticks_per_beat=960)
    for index, (name, notes) in enumerate(parts.items()):
        track = mido.MidiTrack()
        midi.tracks.append(track)
        track.append(mido.MetaMessage('track_name',name=name))
        if index == 0:
            track.append(mido.MetaMessage('set_tempo',tempo=400000))
            track.append(mido.MetaMessage('key_signature',key='A'))
        events = []
        for time,note,length,velocity in notes:
            events.extend([(round(time * 2400),0,mido.Message('note_on',note=note,velocity=velocity,channel=index)),(round((time+length)*2400),1,mido.Message('note_off',note=note,velocity=0,channel=index))])
        last = 0
        for tick, _, message in sorted(events,key=lambda v:(v[0],-v[1])):
            track.append(message.copy(time=tick-last)); last=tick
    midi.save(OUT/'c1-composition.mid')


def gain_db(value):
    return 10**(value/20)


def tone(audio, low, high, mid_gain=0):
    effects = Pedalboard([HighpassFilter(cutoff_frequency_hz=low),LowpassFilter(cutoff_frequency_hz=high),PeakFilter(cutoff_frequency_hz=700,gain_db=mid_gain,q=.65),Compressor(threshold_db=-25,ratio=1.7,attack_ms=12,release_ms=105)])
    return effects(audio,SR)


def room(audio):
    wet = Reverb(room_size=.22,damping=.65,wet_level=1,dry_level=0,width=.65)(audio,SR)
    wet = HighpassFilter(cutoff_frequency_hz=330)(wet,SR)
    return audio + wet * .095


def music_mix(score,hit):
    t = np.arange(score.shape[1])/SR
    envelope = np.clip(t/.4,0,1)*(1-np.clip((t-18.2)/1.6,0,1))
    mixed = score*.85*envelope
    at = round(YA*SR)
    mixed[:,at:at+hit.shape[1]] += hit*.8
    return mixed


def measure(path):
    result = subprocess.run(['ffmpeg','-hide_banner','-i',str(path),'-af','loudnorm=I=-18:TP=-1:LRA=11:print_format=json','-f','null','-'],capture_output=True,text=True,check=True)
    result=json.JSONDecoder().raw_decode(result.stderr[result.stderr.rfind('{'):])[0]
    return {'integrated_lufs':float(result['input_i']),'true_peak_dbtp':float(result['input_tp']),'loudness_range_lu':float(result['input_lra'])}


def main():
    args=configure()
    OUT.mkdir(parents=True,exist_ok=True); CLIPS.mkdir(parents=True,exist_ok=True); ROOT.mkdir(parents=True,exist_ok=True)
    compose()
    parts={'Lead':lead,'Guitar':guitar,'YA musical accent':[(t+YA,p,d,v) for t,p,d,v in accent]}
    save_midi(parts)
    (OUT/'c1-composition.json').write_text(json.dumps(parts,indent=2))
    if args.composition_only:
        print('Wrote the 150 BPM composition and MIDI:', OUT, flush=True)
        return
    if args.mix_only:
        from pedalboard.io import AudioFile
        with AudioFile(str(ROOT/'c1-score-unscaled.wav')) as stream:
            score=stream.read(stream.frames)
        with AudioFile(str(ROOT/'c1-hit-unscaled.wav')) as stream:
            hit=stream.read(stream.frames)
    else:
        prepare_synth()
        renderer=Renderer(PLUGIN)
        print('Rendering the melody',flush=True)
        melody=renderer.render(ROOT/'patches/Steel String Guitar.fxp',lead,LENGTH,ROOT/'c1-lead.surge-state')
        print('Rendering accompaniment',flush=True)
        backing=renderer.render(ROOT/'patches/Nylon Acoustic Guitar.fxp',guitar,LENGTH,ROOT/'c1-guitar.surge-state')
        print('Rendering YA accent',flush=True)
        hit=renderer.render(ROOT/'patches/Steel String Guitar.fxp',accent,.78)
        # Authored balance: melody in front, nylon support without a sub-bass layer.
        melody=tone(melody,190,6800,-.8)*1.9
        backing=tone(backing,150,4600,-1.2)*.85
        hit=tone(hit,200,6200,-1.0)
        score=room(melody+backing)
        # Keep the single theme and pulse; thin it for the huddle and real voice.
        t=np.arange(score.shape[1])/SR
        score*=np.interp(t,[0,10.2,11.3,12.75,12.93,13.62,13.74,18.10,19.50],[1,1,.60,.60,0,0,1,1,0])[None,:]
        hit=room(hit)
        write_wav(ROOT/'c1-score-unscaled.wav',score)
        write_wav(ROOT/'c1-hit-unscaled.wav',hit)
    # The game's reference preview mixer is mono. Match that presentation in the music audition.
    score=score.mean(axis=0,keepdims=True)
    hit=hit.mean(axis=0,keepdims=True)
    raw=music_mix(score,hit)
    write_wav(ROOT/'c1-music-unscaled.wav',raw)
    stats=measure(ROOT/'c1-music-unscaled.wav')
    gain=min(-18.8-stats['integrated_lufs'],-3.0-stats['true_peak_dbtp'])
    score*=gain_db(gain); hit*=gain_db(gain)
    write_wav(CLIPS/'sfx_film_tondo_score.wav',score)
    write_wav(CLIPS/'sfx_film_tondo_taya.wav',hit)
    mix=music_mix(score,hit)
    write_wav(OUT/'c1-music-only.wav',mix)
    subprocess.run(['ffmpeg','-y','-hide_banner','-loglevel','error','-i',str(OUT/'c1-music-only.wav'),'-codec:a','libmp3lame','-q:a','2',str(OUT/'c1-music-only.mp3')],check=True)
    # Use the game's existing preview mixer, including the exact recorded kid and all beds.
    subprocess.run([sys.executable,str(REPO/'tools/render_map_film.py'),'Eskinita','--bed','--clips',str(CLIPS),'--out',str(OUT/'c1-film.mp4'),'--wav',str(ROOT/'c1-film-mix.wav')],cwd=REPO,check=True)
    results={'music':measure(OUT/'c1-music-only.wav'),'film':measure(ROOT/'c1-film-mix.wav'),'applied_gain_db':gain,'duration':LENGTH,'sample_rate':SR,'voice':'existing recording, unmodified','integration':'preview only; no shipped resources modified','owner_verdict':'pending'}
    (OUT/'c1-checks.json').write_text(json.dumps(results,indent=2))
    print(json.dumps(results),flush=True)


if __name__=='__main__':
    main()
