using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO'S SHOTS, framed from his real body and the real enemies (his body is bigger than the table assumes: kneeling,
        // his head bone stood 0.66 m over the court and his hair 1.9 m, film r1). Every shot stays on his right side (+x), and
        // every shot answers one question in order: WHO (him, winding up), WHAT (the slam), WHERE IT GOES (down the lane at them),
        // WHAT IT DOES (it catches them and throws them, and play takes over with them in the air).
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
                    // erupts ahead of us toward the enemies, and they are in frame the whole time. The lane is the court's full
                    // width, so the lens is high enough to look DOWN it (film r17: at his shoulder a band stood up as a wall).
                    float u = Ease(DnFirstBand - .1f, DnBandAt(4) + .4f, t);
                    float front = DnFront(t);
                    eye = Vector3.Lerp(new Vector3(1.8f, 3.0f, -3.2f), new Vector3(2.6f, 4.2f, Mathf.Clamp(front - 7f, -1.5f, _dnTargets.z - 6f)), u);
                    look = new Vector3(_dnTargets.x * .5f, .3f, Mathf.Max(front + 3f, _dnTargets.z * .8f));
                    fov = Mathf.Lerp(54f, 58f, u);
                    break;
                }
                case 3:
                {
                    // WHAT IT DOES: side-on to the enemies on the heaved lane, a slow push in; the burst throws them up across
                    // the frame, away from him, and the lens tips up with them to the cut (film r17: from beyond them they were
                    // thrown straight into the lens).
                    float u = Ease(DnBandAt(4) + .3f, DanteDriftLive.SceneEnd, t);
                    float lift = Ease(DnBurst, DanteDriftLive.SceneEnd, t);
                    eye = _dnTargets + Vector3.Lerp(new Vector3(6.6f, 2.3f, -1.6f), new Vector3(5.6f, 2.5f, -1.0f), u);
                    look = _dnTargets + new Vector3(0f, 1.0f + .6f * lift, .6f + .6f * lift);
                    fov = 48f;
                    break;
                }
            }
        }
    }
}
