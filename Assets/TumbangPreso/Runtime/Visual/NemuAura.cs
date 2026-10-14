using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ WHAT IS LEFT TO SEE OF A POSSESSED PLAYER (LET THEM IN, 2026-10-13, `docs/reports/nemu-ultimate-2026-10-11/
    /// direction.md` section 11.6). The owner: *"i wanted the ghost to go inside the ppl tapos may parang aura nalang sila that
    /// theyre nasasapian"*, *"HINDI UNG KITA UNG GHHOST IS IN THEM"*. So after the entry no ghost is drawn on or in them: they
    /// are their own body and colours (Viego's possession, research section 6) plus THIS: black smoke burning up off their
    /// silhouette like dark fire with violet edges, a faint violet halo behind them that reads across the court, violet
    /// eyes, embers. The only ghost part that ever shows again is a CLAW (<see cref="Claw"/>): an arm and hand of smoke
    /// pushing out of them when the soul tries to get out, or reaching ahead of them in a surge, yanked back in.
    ///
    /// One drawing for the cutscene's ending and for play, so the hand-back does not change the look. Every row is typed; the
    /// caller's clock poses it (the scene clock in the cutscene, the status clock in play).
    /// </summary>
    public static class NemuAura
    {
        public struct State
        {
            public Vector3 Feet, Forward;
            /// <summary>Which way the smoke streams (the pull), world, flat; zero for straight up.</summary>
            public Vector3 Stream;
            public float Top, Width, Clock;
            /// <summary>0 to 1: how much of the aura there is (arriving, leaving, grip).</summary>
            public float Amount;
            /// <summary>0 to 1: the tell, the aura swelling and darkening before a surge.</summary>
            public float Swell;
            /// <summary>0 to 1: how hard the smoke streams along <see cref="Stream"/> (a surge).</summary>
            public float Drag;
            /// <summary>0 to 1: how much the whole aura burns toward <see cref="Stream"/> all the time, not only in a surge: fiercer,
            /// longer and brighter on that side, the halo pulled that way (owner 2026-10-14: *"the aura or smth while possesed is
            /// more intense towards the direction of where theyre getting pulled towards"*).</summary>
            public float Lean;
            /// <summary>0 to 1: flicker as the player wears the grip down.</summary>
            public float Thin;
            public int Seat;
            public bool Reduced;
        }

        private const int Flames = 22, Embers = 7;
        private static Mesh _baked;

        /// <summary>
        /// Where the face is on a blocky head, measured from the body's REAL surface (film r1: from bounds alone the eyes sat
        /// inside the head and never showed): every vertex above the head bone, skinned ones baked in their current pose. Gives
        /// the direction the face looks in the head bone's own space (taken while the body faces <paramref name="forward"/>),
        /// how far in front of the bone the face's surface is, and how far above the bone the top of the head is.
        /// </summary>
        public static void MeasureFace(Transform head, System.Collections.Generic.IEnumerable<Renderer> renderers, Vector3 forward,
                                       out Vector3 faceLocal, out float depth, out float top)
        {
            forward.y = 0f; forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            faceLocal = head.InverseTransformDirection(forward);
            depth = .3f; top = .5f;
            float front = float.MinValue, high = float.MinValue;
            Vector3 h = head.position;
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer || r.GetComponentInParent<Slipper>() != null) continue;
                Mesh mesh = null;
                if (r is SkinnedMeshRenderer skin)
                {
                    if (_baked == null) _baked = new Mesh { name = "NemuFaceMeasure" };
                    skin.BakeMesh(_baked); mesh = _baked;
                }
                else { var f = r.GetComponent<MeshFilter>(); mesh = f != null ? f.sharedMesh : null; }
                if (mesh == null || !mesh.isReadable && !(r is SkinnedMeshRenderer)) continue;
                var tr = r.transform;
                // A baked skin already carries its scale (film r7 measured the face 0.7 m out with the scale applied twice);
                // a plain mesh needs the whole transform.
                bool baked = r is SkinnedMeshRenderer;
                foreach (var v in mesh.vertices)
                {
                    Vector3 w = baked ? tr.position + tr.rotation * v : tr.TransformPoint(v);
                    if (w.y < h.y - .02f) continue;
                    high = Mathf.Max(high, w.y - h.y);
                    front = Mathf.Max(front, Vector3.Dot(w - h, forward));
                }
            }
            if (front > float.MinValue) depth = Mathf.Clamp(front, .08f, .7f);
            if (high > float.MinValue) top = Mathf.Clamp(high, .15f, 1.4f);
        }

        public static void Draw(NemuSmokeBatch batch, in State s)
        {
            if (batch == null || s.Amount <= .01f) return;
            Vector3 up = Vector3.up;
            Vector3 fwd = s.Forward; fwd.y = 0f; fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(up, fwd);
            Vector3 stream = s.Stream; stream.y = 0f;
            Vector3 toward = stream.sqrMagnitude > 1e-4f ? stream.normalized : Vector3.zero;
            float lean = Mathf.Clamp01(s.Lean) * (toward == Vector3.zero ? 0f : 1f);
            float pulled = Mathf.Clamp01(s.Drag + .45f * lean);
            float flicker = s.Reduced ? 1f : 1f - .35f * s.Thin * (.5f + .5f * Mathf.Sin(s.Clock * 31f + s.Seat));
            float amount = s.Amount * flicker;
            float rate = 1f + .6f * s.Swell + .9f * pulled;

            // THE DARK FIRE: tongues of black smoke leaving their outline and burning upward, their edges violet. Each tongue
            // is born on its own spot round the body (golden-angle spaced, its own height) and rises, swells and frays.
            for (int j = 0; j < Flames; j++)
            {
                float phase = Mathf.Repeat(s.Clock * .9f * rate + j * .618f + s.Seat * .37f, 1f);
                float a = (j * 137.5f + s.Seat * 41f) * Mathf.Deg2Rad;
                float h = .12f + .78f * Mathf.Repeat(j * .381f + .13f, 1f);
                // Never in front of the face (film r1: the fire hid the player whole); it burns off their sides, back and top.
                float round = Mathf.Sin(a);
                if (h > .5f && round > .55f) continue;
                Vector3 ring = (side * Mathf.Cos(a) + fwd * round) * (s.Width * .42f);
                Vector3 born = s.Feet + up * (s.Top * h) + ring;
                Vector3 rise = up * (phase * s.Top * (.4f + .2f * s.Swell)) + ring * (phase * .4f);
                float facing = Vector3.Dot(ring.normalized, toward);
                Vector3 drag = stream * (phase * (.25f + 1.1f * pulled) * s.Width * 1.6f);
                // Owner on v3: the round puffs (and the fog they made round the player) looked *"chonky"*. Each tongue is now a
                // thin strand of smoke drawn along the way it burns: up, leaning with the pull, longer as it rises and frays.
                float width = s.Width * (.07f + .06f * phase) * (1f + .3f * s.Swell);
                float length = s.Width * (.28f + .55f * phase) * (1f + .4f * s.Swell + .5f * pulled) * (1f + .7f * lean * Mathf.Max(0f, facing));
                float life = Mathf.Sin(phase * Mathf.PI);
                float density = amount * (.7f + .25f * s.Swell) * life * (1f + lean * (.6f * facing - .1f));
                Vector3 at = born + rise + drag;
                // Nothing below their feet: the fire leaves the body, it does not pool on the court.
                if (at.y < s.Feet.y + .05f) continue;
                Vector3 along = up + stream * (.4f + 1.4f * pulled) + ring * (.6f / Mathf.Max(.1f, s.Width));
                batch.Wisp(at + up * (length * .3f), along, width, length, density, j * .0731f + s.Seat * .19f, phase * phase * .7f, .95f);
            }

            // THE HALO: one large soft violet light at their middle. Their own body hides its centre, so what shows is a glow round
            // their silhouette that reads from across the court.
            // (v3: at 2.3 times their width the halo was a purple fog over them; it is a rim now.)
            batch.Light(s.Feet + up * (s.Top * .55f) + toward * (s.Width * .3f * lean), s.Width * (1.45f + .35f * s.Swell), .38f * amount * (1f + .6f * s.Swell),
                        NemuSmokeBatch.Violet);
            // The side it drags them to burns hottest: a glow there, stretched the way it pulls.
            if (lean > .01f)
                batch.Light(s.Feet + up * (s.Top * .5f) + toward * (s.Width * .65f), s.Width * .7f, .55f * amount * lean * (1f + .5f * s.Drag),
                            NemuSmokeBatch.Violet, toward * (s.Width * (.5f + .8f * s.Drag)));

            // EMBERS rising off them.
            for (int j = 0; j < Embers; j++)
            {
                float phase = Mathf.Repeat(s.Clock * .6f * rate + j * .29f + s.Seat * .11f, 1f);
                float a = (j * 97f + s.Seat * 23f) * Mathf.Deg2Rad;
                Vector3 at = s.Feet + up * (s.Top * (.3f + phase * 1.05f))
                           + (side * Mathf.Cos(a) + fwd * Mathf.Sin(a)) * (s.Width * (.45f + .25f * phase))
                           + stream * (phase * pulled * s.Width);
                float twinkle = s.Reduced ? 1f : .7f + .3f * Mathf.Sin(s.Clock * 13f + j * 2.1f);
                batch.Light(at, .07f, amount * Mathf.Sin(phase * Mathf.PI) * .9f * twinkle, NemuSmokeBatch.Violet);
            }
        }

        /// <summary>
        /// A claw of smoke pushed out of the body: rooted at <paramref name="root"/> (on or just inside their surface), reaching
        /// <paramref name="reach"/> metres along <paramref name="dir"/>, its fingers spread and hooked. <paramref name="scale"/>
        /// sizes it to the body; <paramref name="amount"/> 0 hides it.
        /// </summary>
        public static void Claw(NemuSmokeBatch batch, Vector3 root, Vector3 dir, float reach, float scale, float amount, float clock, float seed)
        {
            if (batch == null || amount <= .01f || reach <= .01f) return;
            if (dir.sqrMagnitude < 1e-5f) return;
            dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
            side.Normalize();
            Vector3 lift = Vector3.Cross(dir, side);
            Vector3 hand = root + dir * reach;
            // The arm: two thin strands along it, thinning toward the wrist, the first half inside them so it reads as coming
            // OUT (v3: three round puffs read as a fat sausage).
            batch.Wisp(Vector3.Lerp(root, hand, .3f), dir, scale * .13f, reach * .7f, amount * .9f, seed + .13f, .1f, .8f);
            batch.Wisp(Vector3.Lerp(root, hand, .72f), dir, scale * .09f, reach * .55f, amount * .9f, seed + .29f, .1f, .9f);
            batch.Puff(hand, scale * .11f, amount * .9f, seed + .5f, .05f, 1f, seed * 200f);
            // The fingers: four hooked streaks of violet light.
            float curl = .55f + .25f * Mathf.Sin(clock * 9f + seed * 20f);
            for (int f = 0; f < 4; f++)
            {
                float spread = (f - 1.5f) * .33f;
                Vector3 fd = (dir + side * spread + lift * (.15f - curl * .25f)).normalized;
                Vector3 tip = hand + fd * (scale * .14f);
                batch.Light(tip, scale * .045f, amount * 1.1f, NemuSmokeBatch.Violet, fd * (scale * (.12f + .05f * (1.5f - Mathf.Abs(f - 1.5f)))));
            }
        }

        /// <summary>
        /// Two violet eyes on a face: <paramref name="centre"/> between them, slanted, <paramref name="gap"/> apart. Give the
        /// lens (<paramref name="viewer"/>) and they sit a little toward it and fade as the face turns away (film r5: placed on
        /// the measured surface alone, a head's own hair and bangs could still bury them).
        /// </summary>
        public static void Eyes(NemuSmokeBatch batch, Vector3 centre, Vector3 faceForward, float gap, float size, float strength, Vector3? viewer = null)
        {
            if (batch == null || strength <= .01f) return;
            if (viewer.HasValue)
            {
                Vector3 toLens = viewer.Value - centre;
                float dist = toLens.magnitude;
                if (dist > 1e-3f)
                {
                    toLens /= dist;
                    Vector3 flatFace = faceForward; flatFace.y = 0f;
                    Vector3 flatLens = toLens; flatLens.y = 0f;
                    float toward = flatFace.sqrMagnitude > 1e-4f && flatLens.sqrMagnitude > 1e-4f ? Vector3.Dot(flatFace.normalized, flatLens.normalized) : 1f;
                    strength *= Mathf.Clamp01(toward * 2f + .35f);
                    centre += toLens * Mathf.Min(.14f, dist * .3f);
                }
            }
            if (strength <= .01f) return;
            faceForward.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, faceForward.sqrMagnitude > 1e-4f ? faceForward.normalized : Vector3.forward);
            for (int e = -1; e <= 1; e += 2)
            {
                Vector3 at = centre + side * (gap * .5f * e);
                // Slanted: the outer corner higher. Stretched along the slant on every camera.
                Vector3 slant = (side * e + Vector3.up * .5f).normalized * (size * .9f);
                batch.Light(at, size, strength, NemuSmokeBatch.Violet, slant);
            }
        }
    }
}
