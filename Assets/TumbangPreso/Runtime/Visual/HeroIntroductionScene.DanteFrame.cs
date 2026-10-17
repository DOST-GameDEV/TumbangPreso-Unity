using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO'S SHOTS, framed from his real body (his body is bigger than the table assumes: kneeling, his head bone stood
        // 0.66 m over the court and his hair 1.9 m, film r1). Every shot stays on his right side (+x), and every shot answers one
        // question in order: WHAT HE DOES (fists into the court), WHAT ANSWERS (the earth, torn up onto his fists), WHO HE IS
        // NOW (the boulder fists raised), WHERE HE GOES (up), WHAT HE LAUNCHES (the slam, the crack racing off down the lane).
        // =========================================================================================
        private void DanteFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            var head = HeadPoint;
            switch (index)
            {
                case 0:
                {
                    // THE PLANT: low and close at his right, the fists going into the court.
                    var fists = DnFistMid;
                    eye = new Vector3(1.9f, DnFloat + .55f, 1.9f);
                    look = new Vector3(fists.x * .5f, Mathf.Lerp(head.y - .2f, DnFloat + .35f, Ease(0f, DnPlant, t)), .45f);
                    fov = 46f;
                    break;
                }
                case 1:
                {
                    // THE EARTH ANSWERS: low and wide in front of him, a slow arc round from his right, so the cracks run out
                    // across the court and the stone flies in from both sides onto his fists.
                    float u = Ease(DnCracks, DnRise, t);
                    // Outside the ring of cracks (film r36: at 4.2 m a stone tearing up on his right lay against the lens).
                    float a = Mathf.Lerp(48f, 14f, u) * Mathf.Deg2Rad, r = Mathf.Lerp(5.4f, 4.6f, u);
                    eye = new Vector3(Mathf.Sin(a) * r, DnFloat + Mathf.Lerp(1.3f, .9f, u), Mathf.Cos(a) * r);
                    look = new Vector3(0f, DnFloat + Mathf.Lerp(.35f, .7f, u), .3f);
                    fov = 52f;
                    break;
                }
                case 2:
                {
                    // THE RISE: a low hero angle in front, looking up at him and the boulder fists overhead (film r35: at 2.4 m
                    // the right boulder hid his face).
                    eye = new Vector3(2.2f, DnFloat + .35f, 3.6f);
                    look = head + new Vector3(0f, .45f, 0f);
                    fov = 48f;
                    break;
                }
                case 3:
                {
                    // THE LEAP: from low on the court, looking up at him against the sky; the lens follows his head up and down.
                    eye = new Vector3(3.2f, DnFloat + .35f, 3.0f);
                    look = head + new Vector3(0f, .2f, 0f);
                    fov = Mathf.Lerp(46f, 54f, Ease(DnLeap, DnLeap + .4f, t));
                    break;
                }
                case 4:
                {
                    // THE SLAM AND THE GROUND RISING: high behind him, down the lane at the real players; he comes down into
                    // the frame, the first band heaves toward them, and the lens leans after it as it rises (film r38: low and
                    // close, the first band's slabs filled the frame and nothing read).
                    float u = Ease(DnSlam, DnEnd, t);
                    eye = Vector3.Lerp(new Vector3(1.3f, DnFloat + 3.0f, -3.4f), new Vector3(1.1f, DnFloat + 3.3f, -2.4f), u);
                    look = new Vector3(0f, DnFloat + .4f, Mathf.Lerp(5.0f, 7.0f, u));
                    fov = 56f;
                    break;
                }
            }
        }
    }
}
