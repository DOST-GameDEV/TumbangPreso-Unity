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

        /// <summary>The faces a soul can pull (owner on v10: *"change expression of some ghosts, make some look mischievous or :0
        /// ... iba ibahin mo ... its okay if some have same"*).</summary>
        public enum Expression { Shock, Mischief, Wail, Rage, Smirk }

        /// <summary>Which face soul <paramref name="index"/> pulls: the five spread over the storm, some repeating.</summary>
        public static Expression ExpressionOf(int index) => (Expression)Mathf.Min(4, (int)(Hash(index, 29) * 5f));

        /// <summary>
        /// A soul's face, on the front of its head the way it flies: whole as it comes at the lens, in profile as it crosses,
        /// gone as it flies away. Every soul its own (owner on v7: *"faces should be varied for ghosts and not js copy pasted"*):
        /// one of five expressions (<see cref="Expression"/>), and within it its own size, spacing and slant of eyes, one
        /// sometimes larger, its mouth moving on its own beat.
        ///  * SHOCK: round hollow eyes and a round O of a mouth (the :O the owner kept).
        ///  * MISCHIEF: narrow eyes slanted down to the middle, scheming, and a wide jagged grin curling up at the ends (his
        ///    reference: a cartoon ghost's evil grin).
        ///  * WAIL: eyes slanted up in the middle, grieving, and a tall open wail.
        ///  * RAGE: big eyes slanted down hard in a scowl and a tall howling mouth.
        ///  * SMIRK: one eye open, one squinting, and a lopsided jagged grin pulled up on one side.
        /// </summary>
        /// <param name="fierce">The souls the story follows always pull RAGE (owner on v10: the first one's two dots and little
        /// round mouth read as a cute face).</param>
        public static void Face(NemuSmokeBatch batch, int index, float t, Vector3 head, Vector3 heading, float size, float squash, float fade, Vector3 eye, float dist, bool fierce = false)
        {
            Vector3 toLens = (eye - head) / dist;
            Vector3 faceUp = Vector3.up - heading * Vector3.Dot(Vector3.up, heading);
            faceUp = faceUp.sqrMagnitude > 1e-4f ? faceUp.normalized : Vector3.Cross(heading, Vector3.Cross(Vector3.up, toLens)).normalized;
            Vector3 faceRight = Vector3.Cross(faceUp, heading);
            float r = size * .5f;
            var expression = fierce ? Expression.Rage : ExpressionOf(index);

            float spread = .3f + .12f * Hash(index, 22);
            float eyeH = .12f + .08f * Hash(index, 23);
            float odd = Hash(index, 27) > .65f ? .3f * (Hash(index, 28) - .5f) : 0f;
            float jitter = (Hash(index, 24) - .5f) * .3f;
            float slant, eyeW, eyeL;
            switch (expression)
            {
                case Expression.Shock: slant = jitter * .5f; eyeW = .15f + .04f * Hash(index, 25); eyeL = .19f + .05f * Hash(index, 26); break;
                case Expression.Mischief: slant = 1.5f + jitter; eyeW = .1f + .03f * Hash(index, 25); eyeL = .28f + .06f * Hash(index, 26); eyeH -= .02f; break;
                case Expression.Wail: slant = -.8f + jitter; eyeW = .13f + .05f * Hash(index, 25); eyeL = .24f + .08f * Hash(index, 26); break;
                case Expression.Rage: slant = .9f + jitter * .3f; eyeW = .2f; eyeL = .36f; odd = .08f; spread = .36f; eyeH = .14f; break;
                default: slant = .5f + jitter; eyeW = .14f + .03f * Hash(index, 25); eyeL = .24f + .05f * Hash(index, 26); break;
            }
            float beat = 5f + 6f * Hash(index, 30);
            float wail = .5f + .5f * Mathf.Sin(t * beat + index * 1.7f);
            // Which side a smirk pulls up on, and which eye it squints.
            float side = Hash(index, 35) > .5f ? 1f : -1f;

            Vector3 On(float x, float y, out float facing)
            {
                y *= squash;
                float z = Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y));
                Vector3 n = heading * z + faceRight * x + faceUp * y;
                facing = Vector3.Dot(n, toLens);
                return head + n * r;
            }
            float Show(float facing) => Mathf.Clamp01(facing * 3f + .65f);
            float Narrow(float facing) => Mathf.Lerp(.35f, 1f, Mathf.Clamp01(facing));

            for (int e = -1; e <= 1; e += 2)
            {
                Vector3 at = On(e * spread, eyeH, out float facing);
                float show = Show(facing);
                if (show <= .01f) continue;
                float big = 1f + e * odd;
                if (expression == Expression.Smirk && e == (int)side)
                {
                    // The squinting eye: a short flat slit, tipped a little.
                    Vector3 slit = (faceRight + faceUp * (-e * .25f)).normalized;
                    batch.Mark(at + faceUp * (size * .02f), slit, size * .06f * squash, size * .26f * Narrow(facing), fade * show, Hash(index, 31 + e));
                    continue;
                }
                Vector3 along = (faceUp + faceRight * (e * slant * .5f)).normalized;
                batch.Mark(at, along, size * eyeW * big * Narrow(facing), size * eyeL * big * squash, fade * show, Hash(index, 31 + e));
            }

            if (expression == Expression.Mischief || expression == Expression.Smirk)
            {
                // THE GRIN: a jagged stroke along a smile that curls up at its ends, its points alternating up and down so it
                // reads as teeth; a smirk's is shorter and pulled up on one side.
                bool smirk = expression == Expression.Smirk;
                float half = smirk ? .26f : .34f + .04f * Hash(index, 36);
                float curl = smirk ? .25f : .55f + .2f * Hash(index, 37);
                float lop = smirk ? .35f * side : 0f;
                float open = .05f + .02f * wail;
                const int points = 7;
                Vector3 prev = default; float prevFacing = 0f;
                for (int k = 0; k < points; k++)
                {
                    float x = Mathf.Lerp(-half, half, k / (float)(points - 1)) + (smirk ? .06f * side : 0f);
                    float y = -.36f + curl * x * x + lop * x + ((k & 1) == 0 ? open : -open);
                    Vector3 at = On(x, y, out float facing);
                    if (k > 0)
                    {
                        float show = Show(Mathf.Min(facing, prevFacing));
                        Vector3 seg = at - prev;
                        float len = seg.magnitude;
                        if (show > .01f && len > 1e-4f)
                            batch.Mark((at + prev) * .5f, seg, size * .085f * Narrow(facing), len * 1.25f + size * .04f, fade * show, Hash(index, 50 + k));
                    }
                    prev = at; prevFacing = facing;
                }
                return;
            }
            {
                float open = expression == Expression.Shock ? .24f + .1f * wail : .3f + .18f * wail;
                float wide = expression == Expression.Shock ? .2f + .03f * wail : .19f;
                float crook = expression == Expression.Rage ? (Hash(index, 33) - .5f) * .2f : 0f;
                Vector3 at = On(crook * .3f, -.32f - open * .45f, out float facing);
                float show = Show(facing);
                if (show > .01f)
                {
                    Vector3 along = (faceUp + faceRight * crook).normalized;
                    float across = size * wide * Narrow(facing), tall = size * open * squash * 1.2f;
                    if (across > tall) batch.Mark(at, Vector3.Cross(heading, along), tall, across, fade * show, Hash(index, 34));
                    else batch.Mark(at, along, across, tall, fade * show, Hash(index, 34));
                }
            }
        }
    }
}
