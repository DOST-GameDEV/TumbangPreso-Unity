using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO'S SHOTS, framed from his real body and the real enemies (his body is bigger than the table assumes: kneeling,
        // his head bone stood 0.66 m over the court and his hair 1.9 m, film r1). Every shot stays on his right side (+x), and
        // every shot answers one question in order: WHO (him, winding up), WHAT (the slam), WHERE IT GOES (down the lane at them),
        // WHO IT HAS (each enemy where they stand, gripped), WHAT IT DOES (it throws them all, and play takes over mid-air).
        // =========================================================================================
        private void DanteFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            var head = HeadPoint;
            switch (index)
            {
                case 0:
                {
                    // WHO: a front three-quarter on his right, low, a slow push in as he sinks and the stone packs on.
                    var subject = head + new Vector3(0f, -.25f, 0f);
                    float u = Ease(0f, DnLeap, t);
                    eye = subject + Vector3.Lerp(new Vector3(2.5f, -.35f, 2.7f), new Vector3(2.0f, -.3f, 2.2f), u);
                    look = subject; fov = 42f;
                    break;
                }
                case 1:
                {
                    // THE LEAP: from low on the court, looking up at him against the sky; the lens follows his head up and down.
                    eye = new Vector3(3.2f, DnFloat + .35f, 3.0f);
                    look = head + new Vector3(0f, .2f, 0f);
                    fov = Mathf.Lerp(46f, 54f, Ease(DnLeap, DnLeap + .4f, t));
                    break;
                }
                case 2:
                {
                    // WHAT: low and close on the fists as they land, the crack star under them.
                    var fists = DnFists;
                    eye = fists + new Vector3(1.9f, .55f, 1.5f); look = fists + new Vector3(0f, .25f, .5f); fov = 48f;
                    break;
                }
                case 3:
                {
                    // WHERE IT GOES: over his right shoulder, riding forward behind the front so each band erupts ahead of us down
                    // the lane, high enough to look DOWN it (film r17: at his shoulder a band stood up as a wall).
                    float u = Ease(DnFirstBand - .1f, DnBandAt(4) + .3f, t);
                    float front = DnFront(t);
                    eye = Vector3.Lerp(new Vector3(1.8f, 3.2f, -3.2f), new Vector3(2.6f, 4.6f, Mathf.Clamp(front - 8f, -1.5f, _dnReach * .4f)), u);
                    // Inside the lane and off the raised court (r31, r32: a look past the lane's end sat in the buildings, the shot was borrowed).
                    look = new Vector3(0f, DnFloat + .8f, Mathf.Min(Mathf.Max(front + 3f, _dnReach * .45f), _dnReach * .8f));
                    fov = Mathf.Lerp(56f, 60f, u);
                    break;
                }
                case 4:
                {
                    // THE GRIP: a cut to each enemy where they really stand, side-on and close, as the stone closes on their feet.
                    if (_dnShown.Count == 0)
                    {
                        eye = new Vector3(3.6f, DnFloat + 1.6f, _dnReach * .35f); look = new Vector3(0f, .3f, _dnReach * .65f); fov = 50f;
                        break;
                    }
                    int k = Mathf.Clamp(Mathf.FloorToInt((t - DnGrip) / Mathf.Max(.01f, _dnGripSlot)), 0, _dnShown.Count - 1);
                    var v = _dnShown[k];
                    float u = Mathf.Clamp01((t - DnGrip - k * _dnGripSlot) / _dnGripSlot);
                    // From his side of the lane unless they stand near its right edge; a slow push in.
                    float side = v.Spot.x < _dnHalfWidth - 3f ? 1f : -1f;
                    eye = v.Spot + Vector3.Lerp(new Vector3(side * 2.9f, DnFloat + 1.8f, -1.3f), new Vector3(side * 2.4f, DnFloat + 1.6f, -1.0f), u);
                    look = v.Spot + new Vector3(0f, DnFloat + .5f, 0f);
                    fov = 42f;
                    break;
                }
                case 5:
                {
                    // THE BURST: wide enough to hold every caught enemy wherever they stand, from his side and behind them; the lens
                    // tips up with them as they are thrown, to the cut.
                    Vector3 min = _dnTargets, max = _dnTargets;
                    foreach (var v in _dnVictims) if (v.Caught) { min = Vector3.Min(min, v.Spot); max = Vector3.Max(max, v.Spot); }
                    var centre = (min + max) * .5f;
                    float span = Mathf.Max(max.x - min.x, max.z - min.z);
                    float lift = Ease(DnBurst, DnEnd, t);
                    eye = centre + new Vector3(4.5f + span * .55f, DnFloat + 2.6f + span * .22f, -(4.0f + span * .45f));
                    look = centre + new Vector3(0f, DnFloat + .9f + .7f * lift, .4f);
                    fov = 52f;
                    break;
                }
            }
        }
    }
}
