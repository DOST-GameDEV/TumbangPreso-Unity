using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO'S SHOTS, FRAMED FROM HIS REAL BODY (film r1 to r3, 2026-10-17). The table's numbers assumed a body the size of the
        // others; his is bigger (kneeling, his head bone stands 0.66 m over the court and his hair 1.9 m), so the knock framed his
        // knees, the eye insert his belt and the punch his hips. These shots now take their subject from the copy itself (his head,
        // his knocking hand) and the beast's eyes from its own typed rise, with the table giving only the side and the move.
        // Every shot stays on his right side of the line (+x).
        // =========================================================================================

        // The hand that knocks and punches: his right, or his free left when the right holds a slipper.
        private Vector3 DnKnockPalm => _heldItem != null ? FreePalm : RightPalm;

        // Where the knock and the punch actually met the court, caught the first time the stage is sampled at that beat
        // (the phase samples forward). Until then, a typed fallback on the knocking hand's side.
        private Vector3? _dnKnockAt, _dnFistAt;

        private Vector3 DnKnockSpot => _dnKnockAt ?? new Vector3(_heldItem != null ? -.32f : .32f, DnFloat + .012f, .55f);

        private void DnCatchContacts(float t)
        {
            if (_dnKnockAt == null && t >= DnKnock1 && t < DnKnock2)
            { var p = DnKnockPalm; _dnKnockAt = new Vector3(p.x, DnFloat + .012f, p.z); }
            if (_dnFistAt == null && t >= DnPunch && t < DnPunch + .4f)
            { var p = DnKnockPalm; _dnFistAt = new Vector3(p.x, DnFloat + .02f, p.z); }
        }

        /// <summary>The beast's eyes at time t, from its typed rise alone (no transform needed, so a shot can aim where they WILL be).</summary>
        private Vector3 DnEyesTyped(float t)
        {
            DnBeastPose(t, out float height, out float pitch);
            return new Vector3(0f, height + _dnCourt, DnSurge(t)) + Quaternion.Euler(pitch, 0f, 0f) * DnEyesLocal;
        }

        // THE RISE's fixed lens: in front of its face and on his side, at the height its eyes reach as they ignite.
        // (film r5: 2.6 m off, its eyes were small; the method's climax is the eyes AT the lens.)
        // A little under them, so the lens looks up into the sockets over the plates' edges (r6: level with them, a plate hid them).
        private Vector3 DnRiseLens => DnEyesTyped(DnIgnite) + new Vector3(.6f, -.08f, 1.45f);

        private void DanteFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            var head = HeadPoint;
            switch (index)
            {
                case 0:
                {
                    // 1 THE KNOCK: the whole kneeling figure and the knocking hand, from his right, low, a very slow push in.
                    var subject = Vector3.Lerp(head, DnKnockSpot, .45f);
                    float u = Ease(0f, DnEyeOpen, t);
                    eye = subject + Vector3.Lerp(new Vector3(2.5f, .05f, 2.3f), new Vector3(2.15f, .05f, 1.95f), u);
                    look = subject; fov = 44f;
                    break;
                }
                case 1:
                {
                    // 1b THE EYE: his face, close, from his front left so the gold (left, scarred) eye meets the lens.
                    // (film r5: the gold eye sat in the bottom right corner under his hair; the face is lower and to his left.)
                    var face = head + new Vector3(-.12f, -.4f, .22f);
                    eye = face + new Vector3(-.25f, .04f, 1.35f); look = face; fov = 32f;
                    break;
                }
                case 2:
                {
                    // 2 THE PUNCH: back to the side, now nearer, his head and the cocked fist in.
                    var subject = head + new Vector3(0f, -.3f, .2f);
                    float u = Ease(DnWind, DnPunch, t);
                    eye = subject + Vector3.Lerp(new Vector3(2.4f, .1f, 1.15f), new Vector3(2.15f, .05f, 1.0f), u);
                    look = subject; fov = 46f;
                    break;
                }
                case 4:
                {
                    // 4 THE RISE: the lens holds where its eyes will be; it follows the face up out of the seam.
                    if (_dnTitan == null) break;
                    eye = DnRiseLens;
                    // First its horn tips breaking the surface (above the eyes), then down onto the eyes as they come up and ignite.
                    var eyes = DnEyesTyped(t);
                    look = Vector3.Lerp(new Vector3(eyes.x, Mathf.Max(eyes.y + 1.1f, DnFloat + .3f), eyes.z), eyes, Ease(DnHorns + .1f, DnIgnite, t));
                    fov = 46f;
                    break;
                }
                case 5:
                {
                    // ...then straight back and up through the heaves, from exactly where THE RISE held, to the table's end.
                    if (_dnTitan == null) break;
                    _performance.Shot(index, ShotEnd(index), out var endEye, out var endLook, out var endFov);
                    float u = Ease(ShotStart(index), ShotEnd(index), t);
                    eye = Vector3.Lerp(DnRiseLens, endEye, u);
                    look = Vector3.Lerp(DnEyesTyped(ShotStart(index)), endLook, u);
                    fov = Mathf.Lerp(42f, endFov, u);
                    break;
                }
            }
        }
    }
}
