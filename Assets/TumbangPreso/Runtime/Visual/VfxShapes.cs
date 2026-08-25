using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Flat ground shapes for ability effects, built as meshes instead of scaled cylinders.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE EVERY SINGLE FLOOR EFFECT IN THE GAME WAS A CIRCLE. 🧑, looking at
    /// the first ability showcase capture: *"look at this shit all of them look like circles
    /// lang"*. He is right and it is the deeper version of the puddle complaint in
    /// `docs/VISION.md` § 2. Fire, lightning, ice, magma and a tear in the world were six
    /// different fictions rendered as the same primitive in six different colours, so the only
    /// thing telling a player which was which was HUE, and hue is the one channel the game
    /// already spends on something else: `Art_Direction.md` § 1 reserves orange for OFFENSE and
    /// blue for DEFENCE, and `UiTheme` spends five more on hero identity.
    ///
    /// ⚠️ A SILHOUETTE IS THE CHANNEL THAT WAS FREE. It reads at any distance, it survives being
    /// seen edge-on in first person, it works for a colour-blind player, and it costs nothing
    /// per frame because the shape is baked once at spawn. `docs/VISION.md` § 2 rule 3 says the
    /// readability budget is spent on DETAIL rather than AREA, and this is the cheapest detail
    /// available: the same footprint, a different outline.
    ///
    /// ⚠️⚠️ THE SHAPES ARE ANGULAR ON PURPOSE, NOT FOR SPEED. This game is voxel and low-poly
    /// (`docs/Voxel_Person_Guide.md`, and the whole cast is built from boxes). A smooth
    /// airbrushed decal would be the thing that looked broken next to it, which is
    /// `docs/VISION.md` § 6: *"his UI art is the design system. Anything drawn in a different
    /// visual language is the thing that looks broken, not the thing that looks new."*
    ///
    /// ⚠️ EVERY SHAPE IS BUILT IN THE XZ PLANE AT y = 0, ONE UNIT OF RADIUS, so a caller scales
    /// it exactly the way it scaled a cylinder before and no footprint arithmetic anywhere else
    /// has to change. `docs/Hero_Strike_Balance.md` § 1 measures those footprints.
    ///
    /// ⚠️ AND EVERY SHAPE IS SEEDED. `Random.InitState` off a caller-supplied seed means two
    /// scorch marks in the same trail differ from each other but a given mark is the same every
    /// time it is captured, which is what makes `AbilityShowcaseProbe`'s renders comparable
    /// between versions. An unseeded probe is a probe that measured 110 and then 467 penalties
    /// on consecutive runs; `CLAUDE.md` § 7.1 records that one.
    /// </summary>
    public static class VfxShapes
    {
        /// <summary>
        /// An irregular fractured polygon. Burnt ground, cracked asphalt, anything that got
        /// broken rather than drawn.
        ///
        /// ⚠️ THE JITTER IS ON THE RADIUS AND NOT ON THE ANGLE. Moving the angles bunches
        /// vertices and produces long thin slivers that alias badly at this size; moving only
        /// how far out each vertex sits keeps the edges roughly even and still destroys the
        /// circle, which is the whole job.
        /// </summary>
        public static Mesh Splat(int sides = 11, float jitter = 0.26f, int seed = 0)
        {
            var state = Random.state;
            Random.InitState(seed);

            var verts = new Vector3[sides + 1];
            verts[0] = Vector3.zero;

            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2.0f;
                float r = 1.0f - Random.Range(0.0f, jitter);
                verts[i + 1] = new Vector3(Mathf.Cos(a) * r, 0.0f, Mathf.Sin(a) * r);
            }

            Random.state = state;
            return Fan(verts, sides);
        }

        /// <summary>
        /// A pointed star. Lightning, impact cracks, anything that arrived fast.
        ///
        /// ⚠️ THE POINTS ARE UNEVEN IN LENGTH. A regular star is a sheriff's badge; alternating
        /// long and short arms with a little variation is what reads as a discharge. It is also
        /// what stops it looking like the manhole cover that is already on this map.
        /// </summary>
        public static Mesh Star(int points = 7, float innerRatio = 0.42f, int seed = 0)
        {
            var state = Random.state;
            Random.InitState(seed);

            int sides = points * 2;
            var verts = new Vector3[sides + 1];
            verts[0] = Vector3.zero;

            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2.0f;
                bool tip = (i % 2) == 0;
                float r = tip ? Random.Range(0.80f, 1.0f) : innerRatio * Random.Range(0.85f, 1.15f);
                verts[i + 1] = new Vector3(Mathf.Cos(a) * r, 0.0f, Mathf.Sin(a) * r);
            }

            Random.state = state;
            return Fan(verts, sides);
        }

        /// <summary>
        /// A lozenge stretched along +Z and pinched at both ends. A smear left by something
        /// moving, which is what a dash trail actually is.
        ///
        /// ⚠️⚠️ THIS IS THE ONE THAT FIXES THE TRAILS, and it is a gameplay read as well as an
        /// art one. A dash leaves a STREAK, and a streak points: a player who sees one knows
        /// which way the caster went, which a chain of circles cannot tell them. The caller
        /// rotates it to the direction of travel.
        ///
        /// Width is a fraction of length, so the same mesh serves a short scorch and a long one.
        /// </summary>
        public static Mesh Streak(float width = 0.62f, int sides = 12, int seed = 0)
        {
            var state = Random.state;
            Random.InitState(seed);

            var verts = new Vector3[sides + 1];
            verts[0] = Vector3.zero;

            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2.0f;

                // An ellipse, then roughened. `Sin` is the +Z axis and keeps its full length;
                // `Cos` is across and is squeezed to `width`.
                float x = Mathf.Cos(a) * width;
                float z = Mathf.Sin(a);

                float rough = 1.0f - Random.Range(0.0f, 0.18f);
                verts[i + 1] = new Vector3(x * rough, 0.0f, z * rough);
            }

            Random.state = state;
            return Fan(verts, sides);
        }

        /// <summary>
        /// A hard-edged crystal outline: few sides, no jitter, deliberately faceted. Ice, and
        /// anything that grew rather than spread.
        ///
        /// ⚠️ SIX SIDES AND NOT ELEVEN. The low count IS the read. At eleven sides with jitter
        /// this is a `Splat`; at six it is unmistakably crystalline from any distance, and it is
        /// the only shape in the set that reads as ORDERED rather than as damage.
        /// </summary>
        public static Mesh Crystal(int sides = 6, float twist = 0.0f)
        {
            var verts = new Vector3[sides + 1];
            verts[0] = Vector3.zero;

            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2.0f + twist;
                verts[i + 1] = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a));
            }

            return Fan(verts, sides);
        }

        /// <summary>
        /// ⚠️⚠️ ONE TRIANGLE FAN, WOUND SO THE FACE POINTS UP, AND IT WAS WOUND THE OTHER WAY
        /// FIRST. This note existed before the bug and did not prevent it, so here is the
        /// arithmetic instead of the warning.
        ///
        /// The rim vertices are generated as `(cos a, 0, sin a)` with `a` increasing, which
        /// traces **counter-clockwise in the XZ plane as seen from above**. Unity treats
        /// **clockwise-from-the-front** as the front face. So the naive fan `(0, i+1, i+2)`
        /// produces a surface whose front points DOWN, at the road, and every one of these
        /// shapes was culled from the only angle the game ever looks at them from.
        ///
        /// ⚠️ IT FAILS SILENTLY AND IT DOES NOT LOOK LIKE A WINDING BUG. The object exists, the
        /// renderer is enabled, the material is correct and the hierarchy looks right in every
        /// inspector; the shape is simply not in the frame. It cost a full capture pass
        /// (`Logs/shots-abilities/*_v5.png`, where every mesh shape is missing and only the
        /// cube-built details survive) to notice, because "invisible" reads as "not spawned".
        ///
        /// The last two indices are swapped below. Do not "tidy" them back into order.
        /// </summary>
        private static Mesh Fan(Vector3[] verts, int sides)
        {
            var tris = new int[sides * 3];

            for (int i = 0; i < sides; i++)
            {
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = (i + 1) % sides + 1;
                tris[i * 3 + 2] = i + 1;
            }

            var uvs = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
                uvs[i] = new Vector2(verts[i].x * 0.5f + 0.5f, verts[i].z * 0.5f + 0.5f);

            var mesh = new Mesh { name = "VfxShape" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // ⚠️ NOT `MarkDynamic`. These are built once at spawn and never written again; the
            // flicker in `ArcFlicker` moves TRANSFORMS, not vertices.
            return mesh;
        }

        /// <summary>
        /// Drops a shape onto a fresh child object under <paramref name="parent"/>, flat on the
        /// ground, scaled to <paramref name="radius"/>.
        ///
        /// ⚠️ IT RETURNS THE `GameObject` SO THE CALLER CAN COLOUR IT. Colour belongs to the
        /// ability, shape belongs here, and mixing the two is how a helper ends up with a
        /// parameter per effect.
        /// </summary>
        public static GameObject Lay(Transform parent, string name, Mesh mesh,
                                     float radius, float height, float yaw = 0.0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.0f, height, 0.0f);
            go.transform.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            go.transform.localScale = new Vector3(radius, 1.0f, radius);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();

            return go;
        }
    }
}
