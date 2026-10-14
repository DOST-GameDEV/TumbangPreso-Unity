using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ NEMU'S WAILING SOULS, DRAWN THE SAME IN THE CUTSCENE AND IN PLAY (LET THEM IN v10, 2026-10-14,
    /// `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 19). The owner on v9: *"i also wnat the ghosts to continue
    /// showing for the whole 15 seconds kahit tapos na"*. A soul is a black head lit violet from inside, a face on its front
    /// along its flight (`Face`), and a long black tail along its own path, all into a `NemuSmokeBatch`. The cutscene
    /// (`HeroIntroductionScene.Nemu.cs`) adds its entries and coils on top; play (`NemuWorld`) draws them circling the gate.
    /// Nothing random: every soul's look is from its index.
    /// </summary>
    public static class NemuSouls
    {
        public static float Hash(int i, int k) => Mathf.Repeat(Mathf.Sin(i * 12.9898f + k * 78.233f) * 43758.5453f, 1f);

        /// <summary>
        /// A soul's tail (world points, newest first; <paramref name="on"/> false where it did not exist yet) and its head at
        /// the first point. <paramref name="eye"/> is the lens the face is turned for.
        /// </summary>
        public static void Draw(NemuSmokeBatch batch, Vector3[] tail, bool[] on, int count, int index, float headSize, float fade, float glow, float clock, Vector3 eye)
        {
            for (int k = 0; k < count - 1; k++)
            {
                if (!on[k] || !on[k + 1]) continue;
                Vector3 a = tail[k], b = tail[k + 1], d = a - b;
                float len = d.magnitude;
                if (len < 1e-3f) continue;
                float u = k / (float)(count - 1);
                float width = headSize * .75f * Mathf.Pow(1f - u, .8f) + .025f;
                batch.Wisp((a + b) * .5f, d, width, len * 1.4f + width, fade * (.9f - .5f * u), index * .071f + k * .13f, u * .55f, .9f);
            }
            if (!on[0]) return;
            Vector3 head = tail[0];
            float dist = Vector3.Distance(head, eye);
            if (dist < 1.1f) return;
            Vector3 heading = on[1] ? head - tail[1] : Vector3.zero;
            heading = heading.sqrMagnitude > 1e-6f ? heading.normalized : (eye - head) / dist;
            batch.Wisp(head, heading, headSize, headSize * 1.18f, fade * .95f, index * .113f, 0f, 1f);
            batch.Light(head, headSize * 1.6f, glow * fade, NemuSmokeBatch.Violet);
            Face(batch, index, clock, head, heading, headSize, 1f, fade, eye, dist);
        }

        /// <summary>
        /// A soul's face, on the front of its head the way it flies: whole as it comes at the lens, in profile as it crosses,
        /// gone as it flies away. Every soul its own (owner on v7: *"faces should be varied for ghosts and not js copy pasted"*):
        /// the size, spacing and slant of its hollow eyes, one sometimes larger, and its mouth (a tall wail, a round scream, a
        /// wide crooked howl), each wailing on its own beat.
        /// </summary>
        public static void Face(NemuSmokeBatch batch, int index, float t, Vector3 head, Vector3 heading, float size, float squash, float fade, Vector3 eye, float dist)
        {
            Vector3 toLens = (eye - head) / dist;
            Vector3 faceUp = Vector3.up - heading * Vector3.Dot(Vector3.up, heading);
            faceUp = faceUp.sqrMagnitude > 1e-4f ? faceUp.normalized : Vector3.Cross(heading, Vector3.Cross(Vector3.up, toLens)).normalized;
            Vector3 faceRight = Vector3.Cross(faceUp, heading);
            float r = size * .5f;

            float spread = .3f + .14f * Hash(index, 22);
            float eyeH = .12f + .1f * Hash(index, 23);
            float slant = (Hash(index, 24) - .4f) * 1.3f;
            float eyeW = .13f + .06f * Hash(index, 25), eyeL = .22f + .1f * Hash(index, 26);
            float odd = Hash(index, 27) > .65f ? .3f * (Hash(index, 28) - .5f) : 0f;
            int mouth = (int)(Hash(index, 29) * 3f);
            float beat = 5f + 6f * Hash(index, 30);
            float wail = .5f + .5f * Mathf.Sin(t * beat + index * 1.7f);

            Vector3 On(float x, float y, out float facing)
            {
                y *= squash;
                float z = Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y));
                Vector3 n = heading * z + faceRight * x + faceUp * y;
                facing = Vector3.Dot(n, toLens);
                return head + n * r;
            }
            for (int e = -1; e <= 1; e += 2)
            {
                Vector3 at = On(e * spread, eyeH, out float facing);
                float show = Mathf.Clamp01(facing * 3f + .65f);
                if (show <= .01f) continue;
                float big = 1f + e * odd;
                Vector3 along = (faceUp + faceRight * (e * slant * .5f)).normalized;
                float narrow = Mathf.Lerp(.35f, 1f, Mathf.Clamp01(facing));
                batch.Mark(at, along, size * eyeW * big * narrow, size * eyeL * big * squash, fade * show, Hash(index, 31 + e));
            }
            {
                float open = mouth == 0 ? .3f + .18f * wail : mouth == 1 ? .22f + .1f * wail : .16f + .08f * wail;
                float wide = mouth == 0 ? .19f : mouth == 1 ? .22f + .06f * wail : .36f;
                float crook = mouth == 2 ? (Hash(index, 33) - .5f) * .5f : 0f;
                Vector3 at = On(crook * .3f, -.32f - open * .45f, out float facing);
                float show = Mathf.Clamp01(facing * 3f + .65f);
                if (show > .01f)
                {
                    Vector3 along = (faceUp + faceRight * crook).normalized;
                    float narrow = Mathf.Lerp(.35f, 1f, Mathf.Clamp01(facing));
                    float across = size * wide * narrow, tall = size * open * squash * 1.2f;
                    if (across > tall) batch.Mark(at, Vector3.Cross(heading, along), tall, across, fade * show, Hash(index, 34));
                    else batch.Mark(at, along, across, tall, fade * show, Hash(index, 34));
                }
            }
        }
    }
}
