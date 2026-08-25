import os
import math
import struct
import random

SAMPLE_RATE = 44100

def clamp(v, min_v=-1.0, max_v=1.0):
    return max(min_v, min(max_v, v))

def write_wav(filename, samples, sample_rate=SAMPLE_RATE):
    # Normalize
    max_amp = max(abs(s) for s in samples) if samples else 0.0
    if max_amp > 0.001:
        target_peak = 0.88
        gain = target_peak / max_amp
        samples = [s * gain for s in samples]

    num_samples = len(samples)
    num_channels = 1
    bits_per_sample = 16
    byte_rate = sample_rate * num_channels * bits_per_sample // 8
    block_align = num_channels * bits_per_sample // 8
    data_size = num_samples * block_align

    with open(filename, 'wb') as f:
        # RIFF header
        f.write(b'RIFF')
        f.write(struct.pack('<I', 36 + data_size))
        f.write(b'WAVE')

        # fmt chunk
        f.write(b'fmt ')
        f.write(struct.pack('<I', 16)) # Subchunk1Size
        f.write(struct.pack('<H', 1))  # AudioFormat (PCM)
        f.write(struct.pack('<H', num_channels))
        f.write(struct.pack('<I', sample_rate))
        f.write(struct.pack('<I', byte_rate))
        f.write(struct.pack('<H', block_align))
        f.write(struct.pack('<H', bits_per_sample))

        # data chunk
        f.write(b'data')
        f.write(struct.pack('<I', data_size))

        # samples
        for s in samples:
            val = int(clamp(s) * 32767)
            f.write(struct.pack('<h', val))

def generate_noise(duration):
    return [random.uniform(-1.0, 1.0) for _ in range(int(duration * SAMPLE_RATE))]

def synth_explosion_heavy(duration=1.4):
    num_samples = int(duration * SAMPLE_RATE)
    samples = [0.0] * num_samples
    
    # Sub bass drop (65Hz -> 28Hz)
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        freq = 65.0 * math.exp(-t * 3.5) + 25.0
        phase = 2.0 * math.pi * freq * t
        sub = math.sin(phase) * math.exp(-t * 2.8)
        
        # Crunchy noise blast
        noise_env = math.exp(-t * 4.2)
        n = random.uniform(-1.0, 1.0) * noise_env
        
        # Mid punch body
        mid_punch = math.sin(2.0 * math.pi * (140.0 * math.exp(-t * 8.0)) * t) * math.exp(-t * 5.0)
        
        val = sub * 0.7 + n * 0.6 + mid_punch * 0.4
        # Soft distortion
        val = math.tanh(val * 1.5)
        samples[i] = val
    return samples

def synth_lightning_strike(duration=1.2):
    num_samples = int(duration * SAMPLE_RATE)
    samples = [0.0] * num_samples
    
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        # Initial sharp crack transient
        crack_env = math.exp(-t * 28.0)
        crack = random.uniform(-1.0, 1.0) * crack_env * 1.5
        
        # Sizzling electric zap frequency modulation
        mod_freq = 480.0 + 320.0 * math.sin(2.0 * math.pi * 38.0 * t)
        zap_phase = 2.0 * math.pi * mod_freq * t
        zap = (math.sin(zap_phase) + 0.5 * math.sin(zap_phase * 2.0) + 0.3 * math.sin(zap_phase * 3.0)) * math.exp(-t * 3.2)
        
        # Crackling arcs
        spark = (random.uniform(-1.0, 1.0) if random.random() < 0.25 else 0.0) * math.exp(-t * 2.5)
        
        # Low electric hum
        hum = math.sin(2.0 * math.pi * 75.0 * t) * math.exp(-t * 2.0) * 0.4
        
        val = crack + zap * 0.7 + spark * 0.4 + hum
        samples[i] = math.tanh(val * 1.3)
    return samples

def synth_ice_freeze(duration=1.1):
    num_samples = int(duration * SAMPLE_RATE)
    samples = [0.0] * num_samples
    
    # Crystalline harmonics (chimes)
    chimes = [1200.0, 1780.0, 2450.0, 3560.0, 4920.0]
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        val = 0.0
        for idx, freq in enumerate(chimes):
            decay = 3.0 + idx * 1.2
            phase = 2.0 * math.pi * freq * t
            val += math.sin(phase) * math.exp(-t * decay) * (1.0 / (idx + 1))
        
        # Frost wind noise
        wind_env = math.sin(math.pi * min(1.0, t / 0.8)) * math.exp(-t * 2.0)
        wind = random.uniform(-0.5, 0.5) * wind_env * 0.4
        
        # Ice glass crunch
        crunch = (random.uniform(-1.0, 1.0) if random.random() < 0.15 else 0.0) * math.exp(-t * 4.0) * 0.3
        
        samples[i] = math.tanh((val + wind + crunch) * 1.2)
    return samples

def synth_fire_whoosh(duration=0.9):
    num_samples = int(duration * SAMPLE_RATE)
    samples = [0.0] * num_samples
    
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        env = math.sin(math.pi * (t / duration)) ** 1.5
        # Low flame roar + sweeping pitch
        pitch = 110.0 + 80.0 * math.sin(math.pi * t / duration)
        flame_tone = math.sin(2.0 * math.pi * pitch * t) * 0.4
        noise = random.uniform(-1.0, 1.0) * 0.7
        val = (flame_tone + noise) * env
        samples[i] = math.tanh(val * 1.4)
    return samples

def synth_ghost_teleport(duration=0.85):
    num_samples = int(duration * SAMPLE_RATE)
    samples = [0.0] * num_samples
    
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        # Upward magical shimmer frequency sweep (350Hz -> 1400Hz)
        freq = 350.0 + 1050.0 * (t / duration) ** 2
        phase = 2.0 * math.pi * freq * t
        shimmer = math.sin(phase) + 0.4 * math.sin(phase * 1.5) + 0.25 * math.sin(phase * 2.0)
        
        # Ethereal chorus pulsation
        chorus = math.sin(2.0 * math.pi * 8.0 * t) * 0.3
        env = math.sin(math.pi * (t / duration))
        
        # Sparkle pop at the end
        pop = 0.0
        if t > duration * 0.7:
            pop_t = t - duration * 0.7
            pop = math.sin(2.0 * math.pi * 880.0 * pop_t) * math.exp(-pop_t * 22.0) * 0.6
            
        samples[i] = (shimmer * (1.0 + chorus) * env * 0.6) + pop
    return samples

def synth_hitmarker(duration=0.15):
    num_samples = int(duration * SAMPLE_RATE)
    samples = [0.0] * num_samples
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        # High crisp ping (2200Hz -> 3100Hz) + fast decay
        f = 2200.0 + 900.0 * (t / duration)
        p = 2.0 * math.pi * f * t
        click = math.sin(p) * math.exp(-t * 35.0)
        punch = math.sin(2.0 * math.pi * 120.0 * t) * math.exp(-t * 25.0) * 0.5
        samples[i] = math.tanh((click + punch) * 1.4)
    return samples

def synth_super_ready(duration=1.3):
    num_samples = int(duration * SAMPLE_RATE)
    samples = [0.0] * num_samples
    
    # 4-note ascending fanfare arpeggio: C5 (523.25), E5 (659.25), G5 (783.99), C6 (1046.5)
    notes = [
        (0.00, 523.25, 0.4),
        (0.16, 659.25, 0.4),
        (0.32, 783.99, 0.4),
        (0.48, 1046.50, 0.8),
    ]
    
    for start_t, freq, note_len in notes:
        start_idx = int(start_t * SAMPLE_RATE)
        note_samples = int(note_len * SAMPLE_RATE)
        for j in range(note_samples):
            idx = start_idx + j
            if idx >= num_samples:
                break
            t_rel = j / SAMPLE_RATE
            env = math.exp(-t_rel * 2.8) * (1.0 - math.exp(-t_rel * 40.0))
            phase = 2.0 * math.pi * freq * t_rel
            
            # Brass + chime harmonics
            tone = (math.sin(phase) 
                    + 0.5 * math.sin(phase * 2.0) 
                    + 0.3 * math.sin(phase * 3.0) 
                    + 0.15 * math.sin(phase * 4.0)) * env
            samples[idx] += tone * 0.35
            
    return samples

# Formant vocal shout synthesis
def synth_vocal_shout(base_f0, end_f0, formants, duration, grit=0.2, attack=0.03):
    num_samples = int(duration * SAMPLE_RATE)
    samples = [0.0] * num_samples
    
    for i in range(num_samples):
        t = i / SAMPLE_RATE
        # Envelope
        if t < attack:
            env = t / attack
        else:
            env = math.exp(-(t - attack) * (2.8 / duration))
            
        # Pitch curve
        alpha = t / duration
        f0 = base_f0 * (1.0 - alpha) + end_f0 * alpha
        
        # Vocal buzz glottal wave
        glottal_phase = (t * f0) % 1.0
        # Pseudo glottal pulse (triangle with asymmetric ramp)
        buzz = (2.0 * glottal_phase - 1.0) if glottal_phase < 0.7 else (1.0 - 2.0 * (glottal_phase - 0.7) / 0.3)
        
        # Formant resonant filters
        vocal = 0.0
        for f_res, amp in formants:
            res_phase = 2.0 * math.pi * f_res * t
            vocal += math.sin(res_phase) * buzz * amp
            
        # Add grit/breathiness
        noise = random.uniform(-1.0, 1.0) * grit
        val = (vocal + noise) * env
        samples[i] = math.tanh(val * 1.5)
    return samples

# 1. Dante Ultimate: "EARTH SHATTER!" / "HAAAH!"
def synth_dante_ult():
    # Low booming titan roar Formants: F1=480, F2=950, F3=2200
    formants = [(480, 0.5), (950, 0.4), (2200, 0.25)]
    shout = synth_vocal_shout(160.0, 95.0, formants, 0.95, grit=0.35, attack=0.04)
    # Layer with ground bass punch
    sub = synth_explosion_heavy(0.95)
    return [shout[i] * 0.7 + sub[i] * 0.5 for i in range(min(len(shout), len(sub)))]

def synth_dante_grunt():
    formants = [(420, 0.6), (820, 0.4)]
    return synth_vocal_shout(130.0, 85.0, formants, 0.35, grit=0.4, attack=0.02)

# 2. Cheska Ultimate: "FREEZE!" / "ICE NOVA!"
def synth_cheska_ult():
    # Bright assertive cry Formants: F1=620, F2=1950, F3=3100
    formants = [(620, 0.5), (1950, 0.45), (3100, 0.3)]
    shout = synth_vocal_shout(320.0, 240.0, formants, 0.85, grit=0.15, attack=0.02)
    frost = synth_ice_freeze(0.85)
    return [shout[i] * 0.65 + frost[i] * 0.55 for i in range(min(len(shout), len(frost)))]

def synth_cheska_grunt():
    formants = [(580, 0.6), (1800, 0.4)]
    return synth_vocal_shout(290.0, 220.0, formants, 0.30, grit=0.18, attack=0.015)

# 3. Sean Ultimate: "SUPERNOVA!" / "BLAST OFF!"
def synth_sean_ult():
    # Fiery rising anime cry Formants: F1=550, F2=1450, F3=2600
    formants = [(550, 0.5), (1450, 0.45), (2600, 0.3)]
    shout = synth_vocal_shout(220.0, 360.0, formants, 0.90, grit=0.25, attack=0.05)
    fire = synth_fire_whoosh(0.90)
    return [shout[i] * 0.65 + fire[i] * 0.55 for i in range(min(len(shout), len(fire)))]

def synth_sean_grunt():
    formants = [(520, 0.6), (1350, 0.4)]
    return synth_vocal_shout(240.0, 180.0, formants, 0.32, grit=0.28, attack=0.02)

# 4. Zack Ultimate: "THUNDERSTRIKE!"
def synth_zack_ult():
    # Turbo fast electric shout Formants: F1=580, F2=1650, F3=2900
    formants = [(580, 0.5), (1650, 0.45), (2900, 0.35)]
    shout = synth_vocal_shout(270.0, 330.0, formants, 0.80, grit=0.22, attack=0.03)
    zap = synth_lightning_strike(0.80)
    return [shout[i] * 0.65 + zap[i] * 0.55 for i in range(min(len(shout), len(zap)))]

def synth_zack_grunt():
    formants = [(540, 0.6), (1550, 0.4)]
    return synth_vocal_shout(260.0, 200.0, formants, 0.28, grit=0.20, attack=0.015)

# 5. Nemu Ultimate: "VOID SEANCE!"
def synth_nemu_ult():
    # Ethereal ghostly whisper shout Formants: F1=450, F2=2100, F3=3400
    formants = [(450, 0.4), (2100, 0.5), (3400, 0.35)]
    shout = synth_vocal_shout(340.0, 260.0, formants, 0.95, grit=0.45, attack=0.08)
    ghost = synth_ghost_teleport(0.95)
    return [shout[i] * 0.55 + ghost[i] * 0.65 for i in range(min(len(shout), len(ghost)))]

def synth_nemu_grunt():
    formants = [(480, 0.5), (2200, 0.4)]
    return synth_vocal_shout(360.0, 290.0, formants, 0.30, grit=0.35, attack=0.03)

def generate_all():
    out_dirs = [
        r"C:\Users\matth\Documents\GitHub\TumbangPreso-Unity\Assets\TumbangPreso\Art\audio\sfx",
        r"C:\Users\matth\Documents\GitHub\TumbangPreso-Unity\Assets\TumbangPreso\Resources\Sfx",
    ]
    
    generators = {
        # SFX
        "sfx_explosion_heavy.wav": synth_explosion_heavy(),
        "sfx_lightning_strike.wav": synth_lightning_strike(),
        "sfx_ice_freeze.wav": synth_ice_freeze(),
        "sfx_fire_whoosh.wav": synth_fire_whoosh(),
        "sfx_ghost_teleport.wav": synth_ghost_teleport(),
        "sfx_hitmarker.wav": synth_hitmarker(),
        "sfx_super_ready.wav": synth_super_ready(),
        
        # Hero Ult & Grunt Voice SFX
        "hero_dante_ult.wav": synth_dante_ult(),
        "hero_dante_grunt.wav": synth_dante_grunt(),
        "hero_cheska_ult.wav": synth_cheska_ult(),
        "hero_cheska_grunt.wav": synth_cheska_grunt(),
        "hero_sean_ult.wav": synth_sean_ult(),
        "hero_sean_grunt.wav": synth_sean_grunt(),
        "hero_zack_ult.wav": synth_zack_ult(),
        "hero_zack_grunt.wav": synth_zack_grunt(),
        "hero_nemu_ult.wav": synth_nemu_ult(),
        "hero_nemu_grunt.wav": synth_nemu_grunt(),
    }
    
    for filename, samples in generators.items():
        for d in out_dirs:
            os.makedirs(d, exist_ok=True)
            path = os.path.join(d, filename)
            write_wav(path, samples)
            print(f"Generated: {path} ({len(samples)} samples)")

if __name__ == "__main__":
    generate_all()
