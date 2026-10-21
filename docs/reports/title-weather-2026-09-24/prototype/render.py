import sys, os, subprocess, numpy as np, cv2
from multiprocessing import Pool
import air
MODE = sys.argv[1]; T0 = float(sys.argv[2]); DUR = float(sys.argv[3]); FPS = 30; SCALE = float(sys.argv[4]) if len(sys.argv) > 4 else .5
OUT = f"frames_{MODE}"
def one(i):
    t = T0 + i / FPS
    im = air.frame(t, SCALE, old=(MODE == 'old'))
    cv2.imwrite(f"{OUT}/{i:05d}.png", (np.clip(im, 0, 1)[..., ::-1] * 255 + .5).astype('uint8'))
if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(OUT): os.remove(os.path.join(OUT, f))
    n = int(DUR * FPS)
    with Pool(11) as p: p.map(one, range(n), chunksize=4)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", f"{OUT}/%05d.png", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "16", f"{MODE}_{int(T0)}_{int(DUR)}.mp4"], check=True)
    print("done", n)
