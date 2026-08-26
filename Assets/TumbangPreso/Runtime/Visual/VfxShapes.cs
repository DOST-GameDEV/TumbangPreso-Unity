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
        // -------------------------------------------------------------------
        // § CONSTRUCTION: five ways to BUILD a shape, not five more outlines.
        //
        // ⚠️⚠️ EVERYTHING ABOVE THIS LINE IS ONE TECHNIQUE WEARING SEVEN OUTLINES, AND THAT IS
        // THE FAULT THIS SECTION EXISTS TO FIX. 🧑, 2026-08-26, after playing the build: *"the
        // problem i found out earlier that made all powers look ugly was that the same logic and
        // code was used to generate all of them"*, and *"maybe use different techniques to make
        // them if we can"*.
        //
        // ⚠️⚠️ HE IS DESCRIBING SOMETHING THE § 8 SILHOUETTE PASS DID NOT REACH, AND THE PASS
        // ITSELF IS THE EVIDENCE. `Splat`, `Star`, `Streak` and `Crystal` are four different
        // POLYGONS handed to ONE builder: `Fan` triangulates a rim of points around a centre
        // vertex, `Lay` drops the result flat at y ≈ 0.01 with the Y scale left at 1, and
        // `VfxMaterial.Ghost` paints it translucent. Change the vertex radii and you have a
        // different outline; you do not have a different EFFECT. Measured off
        // `Logs/shots-abilities/*_v11.png`, which is what made this legible: fire, ice, lightning
        // and magma all render as a coloured plate lying on the road with a brighter plate under
        // it and four or five cubes standing on top, because that is literally what each one is.
        //
        // ⚠️⚠️ SO THE CHANNEL THAT WAS FREE IS NOT OUTLINE ANY MORE, IT IS CONSTRUCTION.
        // `Hero_Strike_Balance.md` § 8.2 lists four channels an effect has (silhouette, axis,
        // motion, hue) and the honest reading of v11 is that all four are now spent while every
        // effect is still assembled the same way. A fifth channel was sitting unused the whole
        // time: HOW THE GEOMETRY IS MADE. A slab with walls, a field of broken plates, a swept
        // ribbon, a branching tube and a dished funnel are not five outlines. They catch light
        // differently, they read differently edge-on, and no two of them can be mistaken for one
        // another even in grayscale, which is the test § 8.2 sets for silhouette and which a
        // family of flat fans fails by construction.
        //
        // ⚠️ NONE OF THIS BUYS A SINGLE EXTRA SQUARE METRE. `docs/VISION.md` § 2 rule 3 is the
        // whole argument for spending the budget here: *"the same silhouette at 2.2 m with a
        // cracked edge, a rim, depth and particles reads as ice"*. Every shape below is still
        // built at ONE UNIT OF RADIUS so `Lay` and every footprint in `Hero_Strike_Balance.md`
        // § 1 keep working untouched, and the two that have real height say so in their name.
        //
        // ⚠️⚠️ AND THE WINDING TRAP IS SOLVED RATHER THAN DOCUMENTED THIS TIME. `Fan`'s note
        // records a full capture pass lost to a fan wound the wrong way, where every mesh was
        // culled from the only angle the game looks at it from and "invisible" read as "not
        // spawned". Hand-winding five more builders is five more chances at that, so
        // `FacetedOriented` fixes each triangle against a reference point instead: any face whose
        // normal points back toward the inside of the form is flipped. A builder below can emit
        // its triangles in whatever order is convenient and still cannot ship inside out.
        // -------------------------------------------------------------------

        /// <summary>
        /// An extruded slab: a polygon with real THICKNESS and real walls, unit radius at the
        /// foot and <paramref name="height"/> tall.
        ///
        /// ⚠️⚠️ THICKNESS IS THE ENTIRE POINT AND IT IS A FIRST-PERSON READ. A flat fan at
        /// y = 0.01 has no silhouette at all from eye height: `AbilityShowcaseProbe` takes every
        /// solo shot twice for exactly this reason, and in `ability_ice_sheet_eye_v11.png` the
        /// sheet is a pale smear two pixels tall. A slab has an EDGE, and an edge is visible from
        /// the angle a player actually runs at the hazard from.
        ///
        /// ⚠️ THE TOP IS SMALLER THAN THE FOOT, so the walls slope outward and catch the key
        /// light at a different angle from the cap. That is what separates the three surfaces
        /// from each other in a flat-lit toon frame; a straight extrusion reads as one colour and
        /// throws the thickness away again.
        ///
        /// ⚠️ NO BOTTOM FACE. It sits on the road and the road is opaque. Emitting one doubles
        /// the alpha of every translucent slab through its own floor, which is the same
        /// double-darkening that made the fire trail's char read as salmon.
        /// </summary>
        public static Mesh Prism(int sides = 6, float height = 0.30f, float topScale = 0.82f,
                                 float jitter = 0.0f, float twist = 0.0f, int seed = 0)
        {
            sides = Mathf.Max(3, sides);

            var state = Random.state;
            Random.InitState(seed);

            var foot = new Vector3[sides];
            var cap = new Vector3[sides];

            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2.0f;
                float rough = 1.0f - Random.Range(0.0f, jitter);

                // ⚠️⚠️ THE TWIST TURNS THE WHOLE FORM, IT DOES NOT TWIST IT, AND THE FIRST
                // VERSION DID THE SECOND. Applying the offset to the cap only shears the prism:
                // the top face ends up rotated against its own base, which at six sides is a 15
                // degree disagreement and is plainly visible in `ability_ice_sheet_v13.png`,
                // where the slab and its rim collar read as two hexagons at different angles.
                //
                // ⚠️ THE PARAMETER EXISTS TO MATCH `Crystal`, WHERE IT IS A ROTATION. Callers
                // pass the same number to both so a rim lines up with the shape it rings; a
                // parameter that means one thing in one builder and another in the next is how
                // that stops being true without anybody editing a call site.
                float a2 = a + twist;
                var dir = new Vector3(Mathf.Cos(a2), 0.0f, Mathf.Sin(a2));

                foot[i] = dir * rough;
                cap[i] = dir * (rough * topScale) + Vector3.up * height;
            }

            var tris = new System.Collections.Generic.List<Vector3>(sides * 12);
            var apex = new Vector3(0.0f, height, 0.0f);

            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;

                // Wall, two triangles.
                tris.Add(foot[i]); tris.Add(cap[i]); tris.Add(cap[j]);
                tris.Add(foot[i]); tris.Add(cap[j]); tris.Add(foot[j]);

                // Cap, one triangle per side back to the centre.
                tris.Add(cap[i]); tris.Add(apex); tris.Add(cap[j]);
            }

            Random.state = state;
            return FacetedOriented(tris, "VfxPrism", new Vector3(0.0f, height * 0.5f, 0.0f));
        }

        /// <summary>
        /// A ring with real walls: an annulus extruded to <paramref name="height"/>, open in the
        /// middle, unit radius at the outer rim.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE `Prism` HAS A CAP AND I USED IT AS A RIM ANYWAY. Three effects
        /// wanted "a collar around the edge" and got `Prism` at 0.92 of the top scale, which is a
        /// FILLED polygon: the ice sheet, the void's event horizon and the fire mark each drew a
        /// solid disc over the thing they were supposed to be ringing. It is unmistakable in
        /// `ability_worstframe_v12.png`, where Nemu's void is a flat bright magenta plate: that
        /// plate is its 12-sided "lip".
        ///
        /// ⚠️ A RIM IS NOT A SMALL DISC, IT IS A DIFFERENT SHAPE. The distinction matters here
        /// more than it would elsewhere because `docs/VISION.md` § 2 is a budget on AREA: a ring
        /// at 8 per cent thickness costs about a sixth of the painted floor its filled equivalent
        /// does, at the same radius, carrying the same information about where the edge is.
        ///
        /// ⚠️ NO BOTTOM AND NO CAP. Same reason as `Prism`: it stands on something opaque, and a
        /// face nobody can see still doubles the alpha of everything drawn through it.
        /// </summary>
        public static Mesh Collar(int sides = 12, float height = 0.10f, float innerRatio = 0.86f,
                                  float jitter = 0.0f, float twist = 0.0f, int seed = 0)
        {
            sides = Mathf.Max(3, sides);
            innerRatio = Mathf.Clamp(innerRatio, 0.05f, 0.98f);

            var state = Random.state;
            Random.InitState(seed);

            var outerFoot = new Vector3[sides];
            var outerTop = new Vector3[sides];
            var innerFoot = new Vector3[sides];
            var innerTop = new Vector3[sides];

            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2.0f + twist;
                var dir = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a));

                float rough = 1.0f - Random.Range(0.0f, jitter);
                var lift = Vector3.up * height;

                outerFoot[i] = dir * rough;
                outerTop[i] = dir * rough + lift;
                innerFoot[i] = dir * (rough * innerRatio);
                innerTop[i] = dir * (rough * innerRatio) + lift;
            }

            var tris = new System.Collections.Generic.List<Vector3>(sides * 18);

            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;

                // Outer wall.
                tris.Add(outerFoot[i]); tris.Add(outerTop[i]); tris.Add(outerTop[j]);
                tris.Add(outerFoot[i]); tris.Add(outerTop[j]); tris.Add(outerFoot[j]);

                // Inner wall.
                tris.Add(innerFoot[i]); tris.Add(innerTop[i]); tris.Add(innerTop[j]);
                tris.Add(innerFoot[i]); tris.Add(innerTop[j]); tris.Add(innerFoot[j]);

                // The band across the top.
                tris.Add(innerTop[i]); tris.Add(outerTop[i]); tris.Add(outerTop[j]);
                tris.Add(innerTop[i]); tris.Add(outerTop[j]); tris.Add(innerTop[j]);
            }

            Random.state = state;

            // ⚠️ THE REFERENCE POINT IS THE RING'S OWN BAND, NOT THE CENTRE. A collar's inner
            // wall has to face INWARD, so orienting everything away from the axis would turn it
            // inside out. Sitting the reference under the band puts the top face up and lets each
            // wall keep the side it was wound on.
            return FacetedOriented(tris, "VfxCollar",
                                   new Vector3(0.0f, height - 6.0f, 0.0f));
        }

        /// <summary>
        /// Ground broken into separate PLATES with gaps between them, each sitting at its own
        /// height and tilt. Unit radius, flat-ish, and deliberately not one surface.
        ///
        /// ⚠️⚠️ THIS IS WHAT `Splat` WAS PRETENDING TO BE. A ragged eleven-sided outline is still
        /// one continuous plate of colour, so `SpawnCrackedLavaDecal` had to draw its cracks ON
        /// TOP as seven separate cubes: the geometry said "intact disc" and the decoration argued
        /// with it. Here the gaps are HOLES, so the road itself shows through between the pieces
        /// and the cracks cost nothing because they are not drawn at all.
        ///
        /// ⚠️ EVERY PLATE GETS ITS OWN OUTER HEIGHT, WHICH IS WHAT MAKES IT LOOK BROKEN RATHER
        /// THAN SLICED. Ground that has been hit does not stay level; a few millimetres of
        /// disagreement per plate is enough for the light to break across them individually,
        /// which is the same trick `Faceted` plays and the reason this game's forms read as cut.
        ///
        /// ⚠️ THE INNER RING STAYS LOW. A plate lifted at the middle is a tent; lifted at the rim
        /// it is a slab that has been shoved up from underneath, which is what a stomp does.
        /// </summary>
        public static Mesh Wedges(int count = 7, float innerRatio = 0.16f, float gapDegrees = 7.0f,
                                  float lift = 0.07f, float jitter = 0.20f, int seed = 0)
        {
            count = Mathf.Max(3, count);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(count * 6);
            float sector = 360.0f / count;
            float half = Mathf.Max(0.0f, sector - gapDegrees) * 0.5f * Mathf.Deg2Rad;

            for (int i = 0; i < count; i++)
            {
                // ⚠️⚠️ THE ANGULAR SPAN IS JITTERED, NOT ONLY THE RADIUS, AND WITHOUT THIS IT IS
                // A PINWHEEL. `ability_lava_decal_v13.png` is the evidence: nine plates of
                // identical width at identical spacing read as a black FLOWER, which is a
                // manufactured object and the one thing broken ground must never look like.
                // `Splat`'s note makes the opposite call for the opposite reason (moving its
                // angles bunches vertices into slivers that alias); here the plates are separate
                // pieces with gaps between them, so unequal widths cost nothing and are the
                // entire difference between fractured and machined.
                // ⚠️ THE ANGULAR WANDER IS A FRACTION OF THE GAP, NOT THE WHOLE GAP. At the
                // full gap a plate can walk clear into its neighbour's lane, so plates cluster
                // and overlap and the ring stops reading as a ring: on a rim it looks like
                // litter thrown across the road rather than a dashed edge. A third of the gap is
                // enough to kill the machined look `_v13` had and not enough to break the lane.
                float mid = (i * sector + Random.Range(-gapDegrees, gapDegrees) * 0.35f) * Mathf.Deg2Rad;
                float span = Random.Range(0.62f, 1.0f);
                float outerR = 1.0f - Random.Range(0.0f, jitter);
                float y = Random.Range(0.0f, lift);

                // The inner corners sit almost on the road; the outer pair carry the lift, so
                // each plate tips up along the direction the break travelled.
                float h0 = half * span;

                // ⚠️⚠️ THE INNER JITTER IS PROPORTIONALLY SMALL, AND AT 0.7 TO 1.3 IT WAS NOT.
                // This is a multiplier on `innerRatio`, so its effect depends entirely on how
                // thick the band is. On Dante's crust, where the inner ratio is 0.22, a 30 per
                // cent swing is a few centimetres. On a RIM, where it is around 0.9, the same
                // swing moves the inner edge between 0.63 and 1.0 of the radius: plates three
                // times deeper than the band they belong to. `ability_seance_void_eye_v14.png`
                // is what that looks like from a metre away, a ring of huge purple spikes thrown
                // across the road around a telegraph that is supposed to be a dashed line.
                var i0 = Ring(mid - h0, innerRatio * Random.Range(0.9f, 1.1f), 0.0f);
                var i1 = Ring(mid + h0, innerRatio * Random.Range(0.9f, 1.1f), 0.0f);
                var o0 = Ring(mid - h0, outerR, y);
                var o1 = Ring(mid + h0, outerR * Random.Range(0.88f, 1.0f),
                              y * Random.Range(0.55f, 1.0f));

                tris.Add(i0); tris.Add(o0); tris.Add(o1);
                tris.Add(i0); tris.Add(o1); tris.Add(i1);
            }

            Random.state = state;
            return FacetedOriented(tris, "VfxWedges", new Vector3(0.0f, -10.0f, 0.0f));
        }

        /// <summary>
        /// A flame: a tapering blade of triangular cross-section standing on the origin, unit
        /// height, leaning and curling as it rises.
        ///
        /// ⚠️⚠️ FIRE IS THE ONE FICTION THAT DOES NOT LIE ON THE GROUND AND IT WAS BEING DRAWN AS
        /// A PLATE. `ability_fire_trail_v11.png` and the six-drop corridor in
        /// `ability_worstframe_v11.png` are the argument: a `Streak` fan scaled 1.28 along the run
        /// renders as a flat salmon LOZENGE, and six of them in a row read as a row of leaves.
        /// Nothing about it says heat, because the one direction heat has is the one direction a
        /// ground decal cannot use.
        ///
        /// ⚠️ A TRIANGULAR CROSS-SECTION, NOT A CYLINDER. Three sides is the fewest that encloses
        /// a volume, so every flame is nine faces and a corridor of them stays cheap, and the
        /// sharp arris down each side is what reads as a tongue rather than as a rolled cone.
        /// The same argument `Crystal`'s note makes about six sides against eleven.
        ///
        /// ⚠️ THE LEAN IS QUADRATIC IN HEIGHT AND THE TWIST IS LINEAR. A flame that leans from
        /// its foot has been knocked over; one that stands and then bends near the tip is
        /// burning. Curl rotates each cross-section as it climbs so the arris spirals, which
        /// gives the silhouette a different edge from every viewing angle without a second mesh.
        /// </summary>
        public static Mesh Tongue(int segments = 5, float width = 0.34f, float lean = 0.28f,
                                  float curl = 0.55f, float jitter = 0.18f, int seed = 0)
        {
            segments = Mathf.Max(2, segments);

            var state = Random.state;
            Random.InitState(seed);

            var rings = new Vector3[segments + 1][];

            for (int s = 0; s <= segments; s++)
            {
                float t = s / (float)segments;

                // Taper is fast at the top so the flame ends in a point rather than a stub.
                float w = width * (1.0f - t) * (1.0f - t * 0.35f);
                float y = t;
                float x = lean * t * t;
                float spin = curl * t * Mathf.PI * 2.0f;

                var ring = new Vector3[3];
                for (int k = 0; k < 3; k++)
                {
                    float a = k / 3.0f * Mathf.PI * 2.0f + spin;
                    float rough = 1.0f - Random.Range(0.0f, jitter);
                    ring[k] = new Vector3(x + Mathf.Cos(a) * w * rough, y, Mathf.Sin(a) * w * rough);
                }

                rings[s] = ring;
            }

            var tris = new System.Collections.Generic.List<Vector3>(segments * 18);

            for (int s = 0; s < segments; s++)
            {
                for (int k = 0; k < 3; k++)
                {
                    int k2 = (k + 1) % 3;

                    Vector3 a = rings[s][k], b = rings[s][k2];
                    Vector3 c = rings[s + 1][k2], d = rings[s + 1][k];

                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            }

            Random.state = state;
            return FacetedOriented(tris, "VfxTongue", new Vector3(lean * 0.4f, 0.5f, 0.0f));
        }

        /// <summary>
        /// A branching bolt: a jagged tube walked from the origin to <paramref name="height"/>,
        /// with forks that peel off and climb away. Unit height, thickness in world units.
        ///
        /// ⚠️⚠️ IT REPLACES A CHAIN OF STRETCHED CUBES. `ArcFlicker` builds four
        /// `PrimitiveType.Cube`s and re-poses them four times a second, which is four renderers,
        /// four materials and four colliders to strip per arc, and the joints between the legs
        /// are visibly square because they ARE squares. One mesh is one renderer, the joints
        /// share vertices, and the whole thing can be handed to `IVfxTimeline` and photographed.
        ///
        /// ⚠️ THE FORKS ARE THE READ, NOT THE JAG. A single jagged line is a crack; lightning is
        /// recognisable because it SPLITS and the branches never rejoin. They start partway up
        /// (never at the foot, which would read as a tripod) and they get thinner, because a
        /// branch that matches the trunk reads as two bolts.
        ///
        /// ⚠️ SEEDED, SO A GIVEN ARC IS THE SAME SHAPE IN EVERY CAPTURE. `ArcFlicker` reshapes
        /// from unseeded `Random.Range`, which is correct for something that flickers in play and
        /// is exactly why the shock trail could never be compared between two render passes.
        /// </summary>
        public static Mesh Bolt(float height = 1.0f, int segments = 6, float jag = 0.14f,
                                float thickness = 0.045f, int branches = 2, int seed = 0)
        {
            segments = Mathf.Max(2, segments);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(segments * 18);

            // The trunk. The sideways kick converges back toward the axis as it climbs, so the
            // bolt tapers to where it was aimed instead of wandering off it.
            var spine = new Vector3[segments + 1];
            spine[0] = Vector3.zero;

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float spread = jag * (1.0f - t * 0.7f);

                spine[i] = new Vector3(Random.Range(-spread, spread),
                                       height * t,
                                       Random.Range(-spread, spread));
            }

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                Tube(tris, spine[i], spine[i + 1], thickness * (1.0f - t * 0.45f));
            }

            // The forks. Each leaves a joint in the upper two thirds of the trunk and climbs
            // outward, at roughly half the thickness and never all the way to the top.
            for (int b = 0; b < branches; b++)
            {
                int from = Random.Range(Mathf.Max(1, segments / 3), segments - 1);
                Vector3 at = spine[from];

                float ang = Random.Range(0.0f, Mathf.PI * 2.0f);
                float reach = jag * Random.Range(2.2f, 4.0f);
                float rise = height * Random.Range(0.16f, 0.34f);

                var tip = at + new Vector3(Mathf.Cos(ang) * reach, rise, Mathf.Sin(ang) * reach);
                var knee = Vector3.Lerp(at, tip, 0.55f)
                           + new Vector3(Random.Range(-jag, jag), 0.0f, Random.Range(-jag, jag));

                Tube(tris, at, knee, thickness * 0.6f);
                Tube(tris, knee, tip, thickness * 0.4f);
            }

            Random.state = state;
            return FacetedOriented(tris, "VfxBolt", new Vector3(0.0f, height * 0.5f, 0.0f));
        }

        /// <summary>
        /// One triangular prism from <paramref name="a"/> to <paramref name="b"/>, appended to a
        /// triangle list. Three sides for the reason `Tongue` gives: the fewest that encloses a
        /// volume, and the arris is what makes a thin tube visible at all at this scale.
        /// </summary>
        private static void Tube(System.Collections.Generic.List<Vector3> tris,
                                 Vector3 a, Vector3 b, float thickness)
        {
            Vector3 axis = b - a;
            float len = axis.magnitude;
            if (len < 0.0001f) return;

            axis /= len;

            // Any vector not parallel to the axis will do for the first perpendicular; up is
            // wrong exactly when the segment is vertical, which is the common case here.
            Vector3 seed = Mathf.Abs(axis.y) > 0.9f ? Vector3.forward : Vector3.up;
            Vector3 u = Vector3.Normalize(Vector3.Cross(axis, seed)) * thickness;
            Vector3 v = Vector3.Normalize(Vector3.Cross(axis, u)) * thickness;

            var ring = new Vector3[3];
            for (int k = 0; k < 3; k++)
            {
                float ang = k / 3.0f * Mathf.PI * 2.0f;
                ring[k] = u * Mathf.Cos(ang) + v * Mathf.Sin(ang);
            }

            for (int k = 0; k < 3; k++)
            {
                int k2 = (k + 1) % 3;

                Vector3 p0 = a + ring[k], p1 = a + ring[k2];
                Vector3 p2 = b + ring[k2], p3 = b + ring[k];

                tris.Add(p0); tris.Add(p1); tris.Add(p2);
                tris.Add(p0); tris.Add(p2); tris.Add(p3);
            }
        }

        /// <summary>
        /// A dished surface that goes DOWN: unit radius at the lip, deepest at the centre.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE THE VOID WAS FOUR STACKED CYLINDERS AND RENDERED AS A PURPLE
        /// PANCAKE. `ability_seance_void_v11.png` and the worst frame are unambiguous: a mouth, a
        /// throat, a lip and a ground ring, each a `PrimitiveType.Cylinder` scaled flat, stack
        /// into one lilac plate with a darker ellipse painted in the middle. Every one of the
        /// four is a disc, so no amount of arranging them makes a hole; depth drawn as concentric
        /// flat rings is a target, not a funnel.
        ///
        /// ⚠️ THE PROFILE IS A POWER CURVE AND THE EXPONENT IS THE WHOLE READ. At 1.0 this is a
        /// straight cone, which is a hopper. Above 2 the wall is near vertical at the lip and
        /// falls away fast in the middle, which is the shape of something being pulled in, and it
        /// keeps almost all of the depth inside the inner half of the radius where it cannot cost
        /// the footprint anything.
        ///
        /// ⚠️ THE LIP FLARES UP ABOVE THE ROAD. Without it the rim meets the street exactly
        /// tangentially and z-fights along the whole circumference; a couple of centimetres of
        /// flare also gives the eye the edge that says the hole has a near side and a far side.
        ///
        /// ⚠️ AND IT IS STILL ROUND, WHICH IS DELIBERATE. `Hero_Strike_Balance.md` § 8.4 argues
        /// that a vortex genuinely IS radial and squaring it off would be a lie about where the
        /// danger is, so it changes AXIS instead. This changes axis the rest of the way: not a
        /// disc lifted off the floor, a surface that actually leaves it.
        /// </summary>
        public static Mesh Funnel(int rings = 5, int sectors = 12, float depth = 0.55f,
                                  float lip = 0.08f, float power = 2.4f,
                                  float jitter = 0.0f, int seed = 0)
        {
            rings = Mathf.Max(2, rings);
            sectors = Mathf.Max(3, sectors);

            var state = Random.state;
            Random.InitState(seed);

            var grid = new Vector3[rings + 1, sectors];

            for (int r = 0; r <= rings; r++)
            {
                float rad = r / (float)rings;
                float y = -depth * Mathf.Pow(1.0f - rad, power);

                // The flare is the last ring only, so the wall stays a wall and the rim is a rim.
                if (r == rings) y += lip;

                for (int s = 0; s < sectors; s++)
                {
                    float a = s / (float)sectors * Mathf.PI * 2.0f;
                    float rough = 1.0f - Random.Range(0.0f, jitter);

                    grid[r, s] = new Vector3(Mathf.Cos(a) * rad * rough, y, Mathf.Sin(a) * rad * rough);
                }
            }

            var tris = new System.Collections.Generic.List<Vector3>(rings * sectors * 6);

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    int s2 = (s + 1) % sectors;

                    Vector3 a = grid[r, s], b = grid[r, s2];
                    Vector3 c = grid[r + 1, s2], d = grid[r + 1, s];

                    // Ring 0 is the single deepest point repeated, so its quad degenerates.
                    if (r > 0) { tris.Add(a); tris.Add(b); tris.Add(c); }
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            }

            Random.state = state;
            return FacetedOriented(tris, "VfxFunnel", new Vector3(0.0f, -depth - 10.0f, 0.0f));
        }

        private static Vector3 Ring(float angle, float radius, float y)
        {
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }

        /// <summary>
        /// `Faceted`, with every triangle turned to face AWAY from <paramref name="inside"/>.
        ///
        /// ⚠️⚠️ IT IS HOW THE FIVE BUILDERS ABOVE CANNOT SHIP INSIDE OUT, AND `Fan`'s note is the
        /// reason it is worth a helper. Unity's front face is the one whose vertices read
        /// CLOCKWISE from the front, which in this coordinate system means the front normal is
        /// the right-handed `Cross(b - a, c - a)`; a fan generated counter-clockwise from above
        /// therefore faces the ROAD, and a shape facing the road is not culled, not missing and
        /// not broken in any inspector: it is simply not in the picture. That cost a full capture
        /// pass once already.
        ///
        /// ⚠️ THE REFERENCE POINT IS PER SHAPE AND IT IS NOT ALWAYS THE CENTROID. A slab uses its
        /// own middle; a ground surface uses a point far BELOW the road, because "outward" for
        /// something the player looks down at means up. Passing the wrong one turns a shape
        /// inside out just as thoroughly as hand-winding it wrong, so each caller states its own.
        /// </summary>
        private static Mesh FacetedOriented(System.Collections.Generic.List<Vector3> tris,
                                            string name, Vector3 inside)
        {
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                Vector3 a = tris[i], b = tris[i + 1], c = tris[i + 2];

                Vector3 n = Vector3.Cross(b - a, c - a);
                Vector3 mid = (a + b + c) * (1.0f / 3.0f);

                if (Vector3.Dot(n, mid - inside) < 0.0f)
                {
                    tris[i + 1] = c;
                    tris[i + 2] = b;
                }
            }

            return Faceted(tris, name);
        }

        /// <summary>
        /// Drops a shape with real HEIGHT onto a fresh child object, scaled on all three axes.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE `Lay` SCALES X AND Z AND LEAVES Y AT 1.0, AND THAT HAS ALREADY
        /// SHIPPED A BUG. `Lay` is correct for a ground decal, where the mesh is flat and the Y
        /// scale would multiply nothing; hand it one of the volumes and the shape keeps its full
        /// unit height while its footprint shrinks. `docs/TODO.md` § 15.5 records the version of
        /// this that reached a player: a `NovaShell`, which is a unit SPHERE, laid at a ground
        /// ring's radius and drawn as a 2 m ball nobody could account for.
        ///
        /// ⚠️ <paramref name="heightScale"/> IS SEPARATE ON PURPOSE. A slab wants its footprint
        /// from the ability's radius and its thickness from the fiction: an ice sheet that got
        /// 2.3 m of radius does not want 2.3 m of height, it wants about 25 cm. Uniform scaling
        /// is the default because it is what a volume normally means; the override is what stops
        /// a caller reaching for `Lay` and reintroducing the fault above.
        /// </summary>
        public static GameObject Stand(Transform parent, string name, Mesh mesh,
                                       float radius, float heightScale = -1.0f,
                                       float lift = 0.0f, float yaw = 0.0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.0f, lift, 0.0f);
            go.transform.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            go.transform.localScale = new Vector3(radius,
                                                  heightScale < 0.0f ? radius : heightScale,
                                                  radius);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();

            Own(go, mesh);
            return go;
        }

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
