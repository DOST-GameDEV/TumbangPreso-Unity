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
        // -------------------------------------------------------------------
        // § VOLUMES: the three shapes that are not flat.
        //
        // ⚠️⚠️ EVERYTHING ABOVE THIS LINE IS A GROUND DECAL, AND THAT WAS THE WHOLE PROBLEM
        // WITH THE ULTIMATES. `Hero_Strike_Balance.md` § 8.5 item 1: *"Supernova, Thunderstrike
        // and Glacial Nova are still expanding spheres and rings, which is four more circles,
        // and they are the biggest things on screen. They want the same pass: a slam should be a
        // shockwave with a FRONT, a lightning strike should be vertical, a nova should be radial
        // but crystalline."* The silhouette work reached the five SKILLS and stopped.
        //
        // ⚠️ AND `PrimitiveType.Sphere` IS WHY THEY LOOKED WRONG RATHER THAN JUST SAME-Y. Unity's
        // sphere is smooth-shaded and near-perfectly round, in a game whose entire cast and city
        // are built from boxes. `docs/VISION.md` § 6: *"his UI art is the design system. Anything
        // drawn in a different visual language is the thing that looks broken, not the thing that
        // looks new."* A chrome-smooth ball expanding out of a voxel character is exactly that.
        //
        // ⚠️ THESE ARE FACETED ON PURPOSE, VIA `Faceted`. No vertex is shared between two
        // triangles, so `RecalculateNormals` gives every face its own normal and the light breaks
        // across the surface in flat planes. That is what makes a low-poly form read as CUT
        // rather than as a coarse sphere, and it costs nothing at these vertex counts.
        // -------------------------------------------------------------------

        /// <summary>
        /// A faceted shell, unit radius, centred on the origin. Radial, and unmistakably cut.
        ///
        /// ⚠️ THE NOVA IS THE ONE EFFECT WHOSE OUTLINE SHOULD NOT CHANGE, for the same reason
        /// § 8.4 gives about the void: a nova genuinely IS radial and squaring it off would be a
        /// lie about where the danger is. So it keeps its roundness and spends the difference on
        /// FACETING instead, which separates it from a primitive sphere without moving a metre
        /// of its footprint.
        ///
        /// ⚠️ SIX RINGS AND TEN SECTORS, deliberately low. At twenty it converges back on the
        /// smooth ball this exists to replace; at six the facets are large enough to catch the
        /// key light individually, which is the entire read.
        /// </summary>
        public static Mesh NovaShell(int rings = 6, int sectors = 10, float jitter = 0.0f, int seed = 0)
        {
            rings = Mathf.Max(2, rings);
            sectors = Mathf.Max(3, sectors);

            var state = Random.state;
            Random.InitState(seed);

            // The lattice first, so neighbouring faces agree on where their shared corner is.
            // Jitter has to be applied HERE rather than per triangle, or the shell splits open
            // along every seam.
            var grid = new Vector3[rings + 1, sectors];
            for (int r = 0; r <= rings; r++)
            {
                float phi = r / (float)rings * Mathf.PI;          // 0 at the top pole
                float y = Mathf.Cos(phi);
                float ring = Mathf.Sin(phi);

                for (int s = 0; s < sectors; s++)
                {
                    float theta = s / (float)sectors * Mathf.PI * 2.0f;
                    float rough = 1.0f - Random.Range(0.0f, jitter);

                    grid[r, s] = new Vector3(Mathf.Cos(theta) * ring, y, Mathf.Sin(theta) * ring) * rough;
                }
            }

            var tris = new System.Collections.Generic.List<Vector3>((rings * sectors) * 6);

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    int s2 = (s + 1) % sectors;

                    Vector3 a = grid[r, s], b = grid[r, s2];
                    Vector3 c = grid[r + 1, s2], d = grid[r + 1, s];

                    // The poles are single points, so the quad there degenerates to one triangle
                    // and emitting both halves would leave a zero-area face behind.
                    if (r > 0) { tris.Add(a); tris.Add(b); tris.Add(c); }
                    if (r < rings - 1) { tris.Add(a); tris.Add(c); tris.Add(d); }
                }
            }

            Random.state = state;
            return Faceted(tris, "VfxNovaShell");
        }

        /// <summary>
        /// A tapered, faceted column standing on the origin: unit radius at the foot, unit height.
        ///
        /// ⚠️ VERTICAL IS THE POINT, and § 8.4 argues why it is worth more than any outline:
        /// *"horizontal versus vertical is a bigger difference than any two outlines on the same
        /// plane."* Thunderstrike already drops a bolt from the sky and then marked the ground
        /// with a flat `Cylinder` ion disc, so the one ability in the game whose fiction is
        /// entirely about the vertical axis was announcing itself with another circle.
        ///
        /// ⚠️ THE TAPER IS UNEVEN PER FACE. A clean cone reads as a traffic cone; pulling each
        /// column of the shaft in by a different amount reads as something that was struck.
        /// </summary>
        public static Mesh Spire(int sides = 7, float topScale = 0.22f, float jitter = 0.30f, int seed = 0)
        {
            sides = Mathf.Max(3, sides);

            var state = Random.state;
            Random.InitState(seed);

            var baseRing = new Vector3[sides];
            var topRing = new Vector3[sides];

            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2.0f;
                var dir = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a));

                baseRing[i] = dir * (1.0f - Random.Range(0.0f, jitter * 0.5f));
                topRing[i] = dir * topScale * (1.0f - Random.Range(0.0f, jitter)) + Vector3.up;
            }

            var tris = new System.Collections.Generic.List<Vector3>(sides * 9);
            var apex = new Vector3(0.0f, 1.18f, 0.0f);

            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;

                // Shaft, two triangles per face.
                tris.Add(baseRing[i]); tris.Add(topRing[i]); tris.Add(topRing[j]);
                tris.Add(baseRing[i]); tris.Add(topRing[j]); tris.Add(baseRing[j]);

                // A short cap, so the column ends in a point rather than a flat lid.
                tris.Add(topRing[i]); tris.Add(apex); tris.Add(topRing[j]);
            }

            Random.state = state;
            return Faceted(tris, "VfxSpire");
        }

        /// <summary>
        /// A wave with a leading edge: an arc of ground that RISES toward its outer rim, facing
        /// +Z so a caller can yaw it to the direction of the slam.
        ///
        /// ⚠️⚠️ THIS IS THE "SHOCKWAVE WITH A FRONT" § 8.5 ITEM 1 ASKS FOR, and the reason a
        /// slam wanted one is gameplay rather than looks. Dante's Titan Fissure is cast 2.2 m
        /// AHEAD of him, so it is the one blast in the game that is aimed, and it was drawing a
        /// full 360 degree ring that said nothing about where it was aimed. An arc tells the
        /// other three players which way it went.
        ///
        /// ⚠️ THE RIM IS RAISED AND THE INNER EDGE IS NOT. That is what makes it a wave rather
        /// than a painted arc: the front is a low wall of displaced ground travelling outward,
        /// and the inside it has already passed through is flat.
        /// </summary>
        public static Mesh Shockfront(float arcDegrees = 150.0f, int sides = 14,
                                      float rimHeight = 0.34f, float innerRatio = 0.44f,
                                      float jitter = 0.16f, int seed = 0)
        {
            sides = Mathf.Max(3, sides);

            var state = Random.state;
            Random.InitState(seed);

            float half = arcDegrees * 0.5f * Mathf.Deg2Rad;

            var inner = new Vector3[sides + 1];
            var outer = new Vector3[sides + 1];

            for (int i = 0; i <= sides; i++)
            {
                // 0 is +Z, so the arc is centred on the direction the caller faces.
                float a = Mathf.Lerp(-half, half, i / (float)sides);
                var dir = new Vector3(Mathf.Sin(a), 0.0f, Mathf.Cos(a));

                float rough = 1.0f - Random.Range(0.0f, jitter);

                inner[i] = dir * innerRatio;
                outer[i] = dir * rough + Vector3.up * rimHeight * rough;
            }

            var tris = new System.Collections.Generic.List<Vector3>(sides * 6);

            for (int i = 0; i < sides; i++)
            {
                tris.Add(inner[i]); tris.Add(outer[i]); tris.Add(outer[i + 1]);
                tris.Add(inner[i]); tris.Add(outer[i + 1]); tris.Add(inner[i + 1]);
            }

            Random.state = state;
            return Faceted(tris, "VfxShockfront");
        }

        /// <summary>
        /// Builds a mesh from a raw triangle list with NO shared vertices, so every face gets its
        /// own normal and the surface reads as cut planes rather than as a smooth curve.
        ///
        /// ⚠️ THE COST IS THREE VERTICES PER TRIANGLE AND IT IS THE RIGHT TRADE HERE. These are
        /// built once at spawn, live under two seconds and never exceed a few hundred faces.
        /// Sharing vertices to save memory would smooth-shade them straight back into the
        /// primitive sphere this whole section exists to get away from.
        /// </summary>
        private static Mesh Faceted(System.Collections.Generic.List<Vector3> tris, string name)
        {
            var verts = new Vector3[tris.Count];
            var indices = new int[tris.Count];
            var uvs = new Vector2[tris.Count];

            for (int i = 0; i < tris.Count; i++)
            {
                verts[i] = tris[i];
                indices[i] = i;
                uvs[i] = new Vector2(tris[i].x * 0.5f + 0.5f, tris[i].z * 0.5f + 0.5f);
            }

            var mesh = new Mesh { name = name };

            // ⚠️ 16-BIT INDICES TOP OUT AT 65535 AND A DENSE SHELL CAN PASS THAT. Six rings by
            // ten sectors is 108 triangles, so it does not today, but the format is set from the
            // count rather than assumed so a caller raising the subdivision gets a mesh instead
            // of a silently truncated one.
            if (verts.Length > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = verts;
            mesh.triangles = indices;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

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

            Own(go, mesh);
            return go;
        }

        /// <summary>
        /// Ties a generated mesh's lifetime to the object drawing it.
        ///
        /// ⚠️⚠️ WITHOUT THIS EVERY SHAPE IN THIS FILE LEAKS, AND THE WORST OFFENDER IS THE ONE
        /// THAT RUNS MOST. A `Mesh` built with `new Mesh()` at runtime is an unmanaged object:
        /// destroying the `GameObject` that references it frees the component, NOT the mesh, and
        /// the mesh then sits in memory until the scene unloads. Nothing in the game destroyed
        /// one.
        ///
        /// ⚠️ THE NUMBERS ARE WHY THIS MATTERS RATHER THAN BEING A TIDINESS NOTE. `HeroHazards`
        /// records that a dashing hero drops a trail disc every 0.10 s and each disc builds TWO
        /// of these (a char and a rim), so a single 3 s dash leaks about sixty meshes. Four
        /// players trading dashes across a 90 s round leak them by the thousand, and every
        /// explosion now adds a core and a shockwave on top.
        ///
        /// ⚠️ IT IS `Destroy`, NOT `DestroyImmediate`. These are torn down during play, and
        /// `DestroyImmediate` on a mesh a renderer is still submitting that frame is how you get
        /// a null-mesh render error instead of a freed mesh.
        /// </summary>
        public static void Own(GameObject host, Mesh mesh)
        {
            if (host == null || mesh == null) return;
            host.AddComponent<GeneratedMeshOwner>().Owned = mesh;
        }

        /// <summary>Frees a generated mesh when the object that draws it goes away.</summary>
        [DisallowMultipleComponent]
        public sealed class GeneratedMeshOwner : MonoBehaviour
        {
            public Mesh Owned;

            private void OnDestroy()
            {
                if (Owned == null) return;

                // ⚠️ The editor tears scenes down outside play mode too, and `Destroy` is a
                // no-op-with-a-warning there. The probes build these shapes, so both paths run.
                if (Application.isPlaying) Destroy(Owned);
                else DestroyImmediate(Owned);

                Owned = null;
            }
        }
    }
}
