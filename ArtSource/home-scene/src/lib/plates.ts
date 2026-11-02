/** Crossfade length of the idle's end into the street that leads into its start (scripts/plates.mjs). */
export const BLEND = 60;

const at = (dir: string, frame: number) => `http://127.0.0.1:3377/${dir}/${String(frame).padStart(5, '0')}.jpg`;

/** One hero's recorded plates. plates/ is served by scripts/platesrv.mjs. */
export const platesOf = (hero: string) => ({
  plateAt: (part: 'intro' | 'idle', frame: number) => at(`${hero}-${part}`, frame),
  preAt: (n: number) => at(`${hero}-idlepre`, n),
});
