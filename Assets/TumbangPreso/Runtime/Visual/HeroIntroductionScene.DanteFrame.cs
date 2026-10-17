using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO'S SHOTS, framed from his real body and the real enemies (his body is bigger than the table assumes: kneeling,
        // his head bone stood 0.66 m over the court and his hair 1.9 m, film r1). Every shot stays on his right side (+x), and
        // every shot answers one question in order: WHO (him, winding up), WHAT (the slam), WHERE IT GOES (down the lane at them),
        // WHAT IT DID (they are thrown), WHAT IS LEFT (the scarred lane).
        // =========================================================================================
        private void DanteFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            var head = HeadPoint;
            switch (index)
            {
                case 0:
                {
                    // WHO: a front three-quarter on his right, low, a slow push in as the fists go up and the stone packs on.
                    var subject = head + new Vector3(0f, -.25f, 0f);
                    float u = Ease(0f, DnSlam, t);
                    eye = subject + Vector3.Lerp(new Vector3(2.5f, -.35f, 2.7f), new Vector3(2.0f, -.25f, 2.2f), u);
                    look = subject + Vector3.up * (.15f * u); fov = 42f;
                    break;
                }
                case 1:
                {
                    // WHAT: low and close on the fists as they land, the crack star under them.
                    var fists = DnFists;
                    eye = fists + new Vector3(1.9f, .55f, 1.5f); look = fists + new Vector3(0f, .25f, .5f); fov = 48f;
                    break;
                }
                case 2:
                {
                    // WHERE IT GOES: over his right shoulder, him in the foreground, riding forward behind the front so each band
                    // erupts ahead of us toward the enemies, and they are in frame the whole time.
                    float u = Ease(DnFirstBand - .1f, DnHit, t);
                    float front = DnFront(t);
                    // High enough to look DOWN the lane (film r17: at his shoulder height the first band stood up as a wall and
                    // hid everything beyond it).
                    eye = Vector3.Lerp(new Vector3(1.6f, 2.7f, -3.0f), new Vector3(2.4f, 3.4f, Mathf.Max(-1f, front - 6f)), u);
                    look = new Vector3(_dnTargets.x * .5f, .4f, Mathf.Max(front + 2f, _dnTargets.z * .85f));
                    fov = Mathf.Lerp(52f, 56f, u);
                    break;
                }
                case 3:
                {
                    // WHAT IT DID: the fifth band bursting under the enemies and throwing them.
                    var at = new Vector3(_dnTargets.x, 0f, (DnBandStart(4) + DnLaneEnd) * .5f);
                    float u = Ease(DnHit, DnAfter, t);
                    // From the side (film r17: from beyond them they were thrown straight into the lens), so they fly across
                    // the frame, away from him, with the bands behind them.
                    // Outside the lane's edge (r18: inside it, its edge slab filled half the frame), a little up.
                    eye = new Vector3(_dnHalfWidth + 3.2f, 0f, at.z - .4f) + Vector3.Lerp(new Vector3(0f, 2.0f, 0f), new Vector3(.4f, 2.4f, .3f), u);
                    look = at + new Vector3(0f, .8f, .7f); fov = 50f;
                    break;
                }
                case 4:
                {
                    // WHAT IS LEFT: behind him, the whole scarred lane, the enemies down at its end.
                    float u = Ease(DnAfter, 7f, t);
                    // Lower and closer than r17 (a flat patch of tiles from high up): down the broken lane to them at its end.
                    // High enough to see over the rubble to them lying past its end (r18: hidden behind it).
                    eye = Vector3.Lerp(new Vector3(2.8f, 4.6f, -2.4f), new Vector3(3.2f, 5.0f, -3.0f), u);
                    look = new Vector3(_dnTargets.x * .5f, .2f, DnLaneEnd + .6f); fov = 52f;
                    break;
                }
            }
        }
    }
}
