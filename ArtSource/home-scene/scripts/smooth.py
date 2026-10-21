# Motion audit: prints every frame whose change from the previous frame spikes well above its
# neighbours. Intended cuts and flashes show up too; anything else is a hitch to look at.
# Usage: python scripts/smooth.py out/<video>.mp4
import subprocess, sys
import numpy as np
raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', sys.argv[1], '-vf', 'scale=240:136,format=gray', '-f', 'rawvideo', '-'], capture_output=True).stdout
fr = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 136, 240).astype(np.float32)
d = np.abs(np.diff(fr, axis=0)).mean(axis=(1, 2))
print(len(fr), 'frames')
for i in range(2, len(d) - 2):
    base = np.median(np.r_[d[i - 3:i], d[i + 1:i + 4]])
    if d[i] > max(4, 2.5 * base):
        print('frame %d  change %.1f  around %.1f  t=%.2fs' % (i + 1, d[i], base, (i + 1) / 30))
