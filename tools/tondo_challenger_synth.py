"""Render explicit MIDI events through Surge XT without a DAW or network calls."""
from pathlib import Path
import struct
import xml.etree.ElementTree as ET

import mido
import numpy as np
from pedalboard import VST3Plugin

TABLE = '.ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+'


def juce_base64(data):
    value = bits = 0
    chars = []
    for byte in data:
        value |= byte << bits
        bits += 8
        while bits >= 6:
            chars.append(TABLE[value & 63])
            value >>= 6
            bits -= 6
    if bits:
        chars.append(TABLE[value & 63])
    return str(len(data)) + '.' + ''.join(chars)


def fxp_state(path, dry=True):
    data = Path(path).read_bytes()
    if data[:4] != b'CcnK' or data[8:12] != b'FPCh' or data[60:64] != b'sub3':
        raise ValueError('Expected a Surge chunk FXP')
    payload = data[60:]
    if dry:
        xml_len = struct.unpack_from('<I', payload, 4)[0]
        patch = ET.fromstring(payload[32:32 + xml_len])
        params = patch.find('parameters')
        # Keep instrument synthesis and its voicing/EQ, remove long preset rooms.
        for element in params:
            if element.tag.endswith('_type') and element.tag.startswith('fx'):
                if element.get('value') != '16':  # Retain graphic EQ; replace other preset effects with one room.
                    element.set('value', '0')
        xml = ET.tostring(patch, encoding='utf-8', xml_declaration=True)
        payload = payload[:4] + struct.pack('<I', len(xml)) + payload[8:32] + xml + payload[32 + xml_len:]
    wrapper = ET.Element('VST3PluginState')
    ET.SubElement(wrapper, 'IComponent').text = juce_base64(payload)
    xml = ET.tostring(wrapper, encoding='utf-8', xml_declaration=True)
    return b'VC2!' + struct.pack('<I', len(xml)) + xml + b'\x00'


class Renderer:
    def __init__(self, plugin, sample_rate=44100):
        self.sample_rate = sample_rate
        self.plugin = VST3Plugin(str(Path(plugin).resolve()))

    def render(self, preset, notes, duration, state_path=None):
        self.plugin.raw_state = fxp_state(preset)
        if state_path:
            Path(state_path).write_bytes(self.plugin.raw_state)
        events = []
        for note in notes:
            start, pitch, length, velocity = note
            events.append((mido.Message('note_on', note=int(pitch), velocity=int(velocity)), float(start)))
            events.append((mido.Message('note_off', note=int(pitch), velocity=0), float(start + length)))
        events.sort(key=lambda event: (event[1], event[0].type == 'note_on'))
        events = [(bytes(message.bytes()), at) for message, at in events]
        audio = self.plugin(events, duration=duration, sample_rate=self.sample_rate, num_channels=2, buffer_size=512)
        if not np.isfinite(audio).all() or np.max(np.abs(audio)) < 1e-6:
            raise RuntimeError('Synth produced invalid or silent audio')
        return audio


def write_wav(path, audio, sample_rate=44100):
    from pedalboard.io import AudioFile
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with AudioFile(str(path), 'w', samplerate=sample_rate, num_channels=audio.shape[0], bit_depth=24) as stream:
        stream.write(np.asarray(audio, dtype=np.float32))
