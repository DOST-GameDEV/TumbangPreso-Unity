import { env, loopNoise, s } from './time';

/**
 * ⚠️ ONE WIND FOR THE WHOLE SET. Hair, jacket, laundry, banderitas and clouds all read it, so
 * the gust before the hero moment arrives everywhere at once. That simultaneity is what makes
 * it read as weather rather than as six unrelated animations. It is periodic on the loop.
 */
export const gust = (f: number) => env(f, s(1.9), s(3.2), s(8.9), s(10.6));

export const wind = (f: number) => 0.28 + 0.14 * loopNoise('breeze', f, hz(0.25)) + 0.82 * gust(f);

/**
 * Radius on the noise circle for a wanted rate of change. ⚠️ Going round a SMALL circle many
 * times per loop would repeat the same half second sixty times, which reads as a machine; one
 * trip round a LARGE circle changes just as fast and never repeats inside the loop.
 */
export function hz(featuresPerSecond: number) {
  return (featuresPerSecond * 900) / (30 * 2 * Math.PI);
}

/** A fast flutter for cloth, scaled by the wind. */
export const flutter = (f: number, seed: string, rate = 2, amount = 1) =>
  loopNoise(seed, f, hz(rate)) * amount * (0.35 + wind(f));
