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

            // ⚠️ SEEDED START, so one seed still produces one exact mesh. The walk below is
            // cumulative, which is the whole point: an error that accumulates is what stops the
            // plates snapping back onto the grid on every iteration.
            float _wedgeWalk = Random.Range(0.0f, 360.0f);

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
                // ⚠️⚠️ THE JITTER IS A THIRD OF ONE GAP AND THAT IS NOT ENOUGH, WHICH
                // `ability_lava_decal_v22.png` SHOWS THREE PASSES LATER. § 19.2d called this
                // exact read (*"identical width at identical spacing reads as a black FLOWER"*)
                // and answered it by jittering an EVEN step, which is still an even step: the
                // plates land within a few degrees of where a pinwheel would put them, and from
                // above that is what it draws. `Upheaval` was written against the same trap and
                // walks a varying gap instead; this does the same, in the one line that decides
                // it, so the phase of each plate accumulates rather than resetting to the grid
                // every iteration.
                _wedgeWalk += sector * Random.Range(0.55f, 1.5f);
                float mid = (_wedgeWalk + Random.Range(-gapDegrees, gapDegrees) * 0.35f) * Mathf.Deg2Rad;
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

        /// <summary>
        /// A witch's sigil: a ring, an inner ring, a star drawn as LINES rather than filled, and
        /// rune ticks around the rim. Flat, unit radius, in the XZ plane.
        ///
        /// ⚠️⚠️ IT IS LINE ART, AND NOTHING ELSE IN THIS FILE IS. Every other builder here makes
        /// a SOLID: a fan, a slab, a shell, a funnel. A sigil is the opposite kind of object,
        /// strokes with the ground showing between them, and that is not a variation on a filled
        /// shape, it is a different way of making geometry. 🧑, on the sixth hero: *"the abilities
        /// i want for phaister are witch based and she does hexes curses and spells and has
        /// glyphs effects during spells or abilities casting"*.
        ///
        /// ⚠️⚠️ IT ALSO COSTS ALMOST NO FLOOR, WHICH IS WHY IT IS THE RIGHT ANSWER HERE RATHER
        /// THAN A LUCKY ONE. `docs/VISION.md` § 2 is a budget on painted AREA, and a hero whose
        /// whole identity is drawing symbols on the ground is exactly the hero that could break
        /// it. At the shipped bar width a 2.4 m sigil paints about **8 per cent of its own
        /// circle** and the rest is road: a full disc of the same radius is twelve times the
        /// pixels. Strokes are how this hero can be the most ornate in the game and the cheapest
        /// on screen at the same time.
        ///
        /// ⚠️ THE STAR IS A {points/skip} POLYGON, which is what makes it read as occult rather
        /// than as decoration. 5 and 2 is a pentagram: every vertex joined to the one two along,
        /// so the strokes cross and never retrace. 7 and 3 is a heptagram, and 6 and 2 degenerates
        /// into two triangles (a hexagram), which is why `skip` and `points` must be coprime and
        /// the caller picks the pair rather than a "complexity" number.
        ///
        /// ⚠️ SEEDED, and the seed only moves the RUNE TICKS. The ring and the star are exact on
        /// purpose: a hand-wobbled pentagram reads as a mistake, while uneven tick marks read as
        /// writing. It is the same argument `Crystal` makes for having no jitter at all.
        /// </summary>
        public static Mesh Sigil(int points = 5, int skip = 2, float bar = 0.045f,
                                 float innerRatio = 0.74f, int runes = 12,
                                 int segments = 40, int seed = 0)
        {
            points = Mathf.Max(3, points);
            skip = Mathf.Clamp(skip, 1, points - 1);
            segments = Mathf.Max(8, segments);
            innerRatio = Mathf.Clamp(innerRatio, 0.2f, 0.95f);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(segments * 12);

            // The two rings.
            FlatRing(tris, 1.0f - bar, 1.0f, segments);
            FlatRing(tris, innerRatio - bar * 0.5f, innerRatio + bar * 0.5f, segments);

            // The star, inside the inner ring so the strokes never touch it.
            float starR = innerRatio - bar * 1.2f;
            for (int i = 0; i < points; i++)
            {
                float a0 = i / (float)points * Mathf.PI * 2.0f - Mathf.PI * 0.5f;
                float a1 = ((i + skip) % points) / (float)points * Mathf.PI * 2.0f - Mathf.PI * 0.5f;

                FlatBar(tris,
                        new Vector3(Mathf.Cos(a0) * starR, 0.0f, Mathf.Sin(a0) * starR),
                        new Vector3(Mathf.Cos(a1) * starR, 0.0f, Mathf.Sin(a1) * starR),
                        bar * 0.5f);
            }

            // The runes: short radial strokes in the band between the rings, at uneven lengths
            // so the rim reads as inscribed rather than as a gear.
            for (int r = 0; r < runes; r++)
            {
                float a = r / (float)runes * Mathf.PI * 2.0f;
                var dir = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a));

                float from = innerRatio + bar * Random.Range(1.2f, 2.4f);
                float to = 1.0f - bar * Random.Range(1.2f, 2.6f);
                if (to <= from) continue;

                FlatBar(tris, dir * from, dir * to, bar * 0.38f);
            }

            Random.state = state;

            // Ground art: "outward" is up, so the reference point sits far below the road. Same
            // convention `Wedges` uses and for the same reason.
            return FacetedOriented(tris, "VfxSigil", new Vector3(0.0f, -10.0f, 0.0f));
        }

        /// <summary>
        /// One small written GLYPH, standing upright, for a particle to be made of.
        ///
        /// ⚠️⚠️ THE WITCH'S PARTICLES WERE GENERIC CHIPS AND IT READ AS CONFETTI. Off the
        /// played build: *"the effects it has look like party shit haha"*, and what he asked for
        /// instead: *"i want sigils to come out of here not wtv effect that is / sigils glyphs
        /// ancient letters"*, with a reference of letters lifting off a book, and the bound
        /// *"i dont want english letters or in that color"*.
        ///
        /// ⚠️ SO THESE ARE INVENTED MARKS, NOT AN ALPHABET. Every glyph is 3 to 5 straight
        /// strokes on a small grid, chosen from a seed: a stem, one or two arms, sometimes a
        /// crossbar, sometimes a detached dot. That is enough for the eye to read "writing"
        /// without any of them being a letter anybody can name, which is the whole point. Using
        /// real characters would make it a language, and the wrong one.
        ///
        /// ⚠️ STRAIGHT STROKES ONLY, AND THAT IS THE HOUSE STYLE RATHER THAN A SHORTCUT. The
        /// hex circle these rise out of is built from `Sigil`, which is `FlatBar` strokes and
        /// two `FlatRing`s. A curved, calligraphic glyph beside it would look like a different
        /// game. Same vocabulary, smaller.
        ///
        /// ⚠️ IT STANDS IN THE XY PLANE, NOT THE XZ PLANE LIKE THE GROUND ART. A particle mesh
        /// is billboarded by rotation, not laid on the floor, so a glyph built flat would be
        /// edge-on to the camera and invisible. This is the one shape in this file that is
        /// authored upright, and it is why it does not go through `FacetedOriented` with a
        /// downward reference point the way `Sigil` and `Wedges` do.
        /// </summary>
        public static Mesh Rune(int seed = 0, float bar = 0.13f)
        {
            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(64);

            // The stem. Every glyph has one, which is what makes a set of them look like one
            // hand wrote them all.
            float h = Random.Range(0.62f, 1.0f);
            float lean = Random.Range(-0.16f, 0.16f);
            UprightBar(tris, new Vector3(-lean, -h * 0.5f, 0.0f),
                             new Vector3(lean, h * 0.5f, 0.0f), bar * 0.5f);

            // One or two arms off the stem, at the angles a chisel makes.
            int arms = Random.Range(1, 3);
            for (int i = 0; i < arms; i++)
            {
                float at = Random.Range(-0.34f, 0.40f);
                float len = Random.Range(0.28f, 0.52f);
                float rise = Random.Range(0.18f, 0.46f) * (Random.value < 0.5f ? -1.0f : 1.0f);
                float side = Random.value < 0.5f ? -1.0f : 1.0f;

                var from = new Vector3(lean * (at * 2.0f), at * h, 0.0f);
                UprightBar(tris, from, from + new Vector3(len * side, rise, 0.0f), bar * 0.42f);
            }

            // A crossbar on some of them, which is most of what separates a glyph from a twig.
            if (Random.value < 0.55f)
            {
                float at = Random.Range(-0.22f, 0.28f) * h;
                float w = Random.Range(0.24f, 0.44f);
                UprightBar(tris, new Vector3(-w, at, 0.0f), new Vector3(w, at, 0.0f), bar * 0.38f);
            }

            // A detached mark. Rare, and it is the thing that makes a row of these look
            // deliberate rather than procedural.
            if (Random.value < 0.3f)
            {
                float dx = Random.Range(0.26f, 0.44f) * (Random.value < 0.5f ? -1.0f : 1.0f);
                float dy = Random.Range(0.3f, 0.55f) * h;
                UprightBar(tris, new Vector3(dx - bar * 0.5f, dy, 0.0f),
                                 new Vector3(dx + bar * 0.5f, dy, 0.0f), bar * 0.5f);
            }

            Random.state = state;

            var mesh = new Mesh { name = "VfxRune" + seed };
            var verts = tris.ToArray();
            var idx = new int[verts.Length];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;

            mesh.SetVertices(new System.Collections.Generic.List<Vector3>(verts));
            mesh.SetTriangles(idx, 0);

            // ⚠️ FLAT FORWARD NORMALS, NOT `RecalculateNormals`. Every triangle here lies in
            // the XY plane, so the recalculated normal is the same vector for all of them and
            // the call is pure cost. Writing it directly also means a glyph lit from the front
            // is lit evenly rather than picking up a gradient from winding order.
            var normals = new Vector3[verts.Length];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.back;
            mesh.normals = normals;

            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A flat stroke from a to b in the XY plane, for <see cref="Rune"/>.</summary>
        private static void UprightBar(System.Collections.Generic.List<Vector3> tris,
                                       Vector3 a, Vector3 b, float halfWidth)
        {
            Vector3 along = b - a;
            if (along.sqrMagnitude < 1e-8f) return;

            along.Normalize();
            var side = new Vector3(-along.y, along.x, 0.0f) * halfWidth;

            Vector3 a0 = a - side, a1 = a + side;
            Vector3 b0 = b - side, b1 = b + side;

            tris.Add(a0); tris.Add(b0); tris.Add(b1);
            tris.Add(a0); tris.Add(b1); tris.Add(a1);
        }

        // ===================================================================
        // THREE BUILDERS FOR ONE HERO, BECAUSE ONE BUILDER FOR ONE HERO IS THE BUG
        //
        // ⚠️⚠️ `Sigil` USED TO DRAW ALL THREE OF PHAISTER'S POWERS AND THAT IS WHY THEY LOOKED
        // THE SAME. 🧑 2026-08-26, off the played build: *"the fucking abilities of phaister are
        // repetitive they use the same magic circle i want them to have different colors and
        // different animations and different symbols. DIFFERENT EVERYTHING"*, and, precisely:
        // *"her Q is just 2 stars on top of each other"*.
        //
        // ⚠️⚠️ HE WAS DESCRIBING THE CODE, NOT A FEELING. `HeroHazards.SpawnWitchSigil` drew
        // `Sigil` TWICE, an outer and an inner star polygon, and `SpawnCastGlyph` called it with
        // a hard-coded `5, 2`. So the hex, BOTH ends of the blink and the eclipse were the same
        // pentagram stacked on itself, separated by radius and by a seed that only jitters the
        // rim ticks. `docs/TODO.md` § 19 already named the class (*"fifteen poses sharing one
        // construction"*) and § 21.5 then argued the sixth hero into it deliberately, on the
        // grounds that her kit is one CRAFT. **That argument was wrong in one specific way:**
        // a shared visual LANGUAGE does not require a shared MESH BUILDER, and taking it to mean
        // that is how one hero's whole kit became three sizes of one object.
        //
        // ⚠️ WHAT SURVIVES OF § 21.5 IS THE PALETTE AND THE VOCABULARY. All three are still
        // strokes with the road showing through, still magenta into gold, still written rather
        // than splashed. What changes is that each one is now BUILT differently, which is the
        // fifth channel `docs/TODO.md` § 19.1 added and the only one that had never been spent
        // on her:
        //
        //   Q, KULAM HEX     `WardCircle`  concentric rings, a dense rune band, nested rotated
        //                                  squares, radial dividers, medallions. Static.
        //   E, SHADOW BLINK  `Rift`        a torn VERTICAL sheet. No circle anywhere in it.
        //   R, GRAND COVEN   `Corona`      a ring of tapering teeth around an empty middle,
        //                                  built to be seen from BELOW.
        //
        // ⚠️ AND NONE OF THE THREE IS A {points/skip} STAR POLYGON, which is what `Sigil` is made
        // of and the one shape 🧑 named. `Sigil` itself is left in the file and is now unused by
        // the kit; deleting it is a separate decision from fixing the hero.
        // ===================================================================

        /// <summary>
        /// A drawn WARD: rings, a written band, nested squares and medallions. Phaister's hex.
        ///
        /// ⚠️⚠️ ITS CONSTRUCTION IS RECTILINEAR WHERE `Sigil`'S IS RADIAL, AND THAT IS THE WHOLE
        /// SEPARATION. A star polygon is one loop of strokes chasing itself around a centre, so
        /// however many points it has it reads as a spinner. This is built from CLOSED FIGURES
        /// laid over each other: two axis-aligned squares turned 45 degrees apart make an
        /// eight-pointed frame with straight sides and real corners, and a square has an inside
        /// and an outside in a way a star never does. The eye reads it as a diagram somebody
        /// ruled, which is what a ward is.
        ///
        /// ⚠️ THE RUNE BAND IS THE POINT, NOT DECORATION. 🧑's references are all the same thing:
        /// a continuous ring of small written characters between two rules. `Sigil` had radial
        /// TICKS there, which is a gear. Real glyphs standing upright around the rim are what
        /// separate "a circle with marks on it" from "something written down", and this hero's
        /// entire motif is written language (`docs/TODO.md` § 19's per-hero motif table).
        ///
        /// ⚠️ THE MEDALLIONS ARE WHY IT SURVIVES BEING SEEN FROM EYE HEIGHT. A ground mark is
        /// nearly edge-on at 1.65 m, and at that angle a rim band compresses to a line. Four
        /// small circles at the compass points each hold their own glyph, so there are four
        /// places on the mark that stay legible from the side, and they also say which way it is
        /// facing, which a rotationally symmetric ring cannot.
        ///
        /// ⚠️ EVERYTHING IS BUILT AT ONE UNIT OF RADIUS, like every other builder here, so
        /// <see cref="Lay"/> and every footprint in `Hero_Strike_Balance.md` § 1 keep working.
        /// The painted fraction is about 11 per cent of its own circle: denser than `Sigil`'s 8
        /// because there is more writing, and still a ninth of what a filled disc costs.
        /// </summary>
        /// <param name="cells">How many cells the rune band is divided into by radial rules.</param>
        /// <param name="medallions">Small glyph circles on the rim. 4 is the compass.</param>
        public static Mesh WardCircle(int cells = 12, int medallions = 4, float bar = 0.030f,
                                      int seed = 0)
        {
            cells = Mathf.Clamp(cells, 6, 24);
            medallions = Mathf.Clamp(medallions, 0, 8);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(1024);

            // ⚠️ FOUR RINGS AND TWO OF THEM ARE A PAIR. A single line at the rim is a circle; two
            // lines 4 per cent apart with writing between them is a BAND, and a band is the thing
            // that reads as inscribed. The references are unanimous about this and it is the
            // cheapest part of the whole shape.
            const float rimOuter = 1.0f;
            const float rimInner = 0.845f;

            FlatRing(tris, rimOuter - bar, rimOuter, 72);
            FlatRing(tris, rimInner - bar, rimInner, 72);

            // The inner rule the squares sit against, and the small hub the glyph sits in.
            FlatRing(tris, 0.615f - bar * 0.8f, 0.615f, 64);
            FlatRing(tris, 0.185f - bar * 0.9f, 0.185f, 40);

            // ⚠️ THE RADIAL DIVIDERS RUN ONLY ACROSS THE BAND. Taken to the centre they become
            // spokes and the ward turns into a wheel, which is the exact read `Sigil`'s rim ticks
            // were criticised for. Between two rules they are cell walls, and cells are what make
            // the writing look organised rather than scattered.
            for (int c = 0; c < cells; c++)
            {
                float a = c / (float)cells * Mathf.PI * 2.0f;
                var dir = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a));
                FlatBar(tris, dir * rimInner, dir * (rimOuter - bar), bar * 0.42f);
            }

            // A glyph in every cell, sitting on the band's midline and turned to face out.
            float bandMid = (rimInner + rimOuter - bar) * 0.5f;
            for (int c = 0; c < cells; c++)
            {
                float a = (c + 0.5f) / cells * Mathf.PI * 2.0f;
                FlatRune(tris, new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a)) * bandMid,
                         0.105f, a, bar * 0.40f, Random.Range(0, 4096));
            }

            // ⚠️⚠️ THE TWO SQUARES, WHICH ARE THE SHAPE NOTHING ELSE IN THIS FILE MAKES. Built
            // corner to corner as four straight bars each, then the second one turned 45 degrees:
            // the union is an octagram with FLAT sides, and every crossing is a real intersection
            // of two rules rather than a vertex of one loop. `Star` and `Sigil` both produce
            // points radiating from a middle; this produces a frame.
            const float squareR = 0.615f;
            Square(tris, squareR, 0.0f, bar * 0.5f);
            Square(tris, squareR, Mathf.PI * 0.25f, bar * 0.5f);

            // An inscribed triangle, which is the one figure that breaks the eight-fold symmetry
            // and stops the middle reading as a compass rose. Three is coprime with four and
            // eight, so no stroke of it lands on a square's corner.
            Polygon(tris, 3, 0.44f, Mathf.PI * 0.5f, bar * 0.46f);

            // The medallions: a small ring with a glyph inside, straddling the inner rule.
            for (int m = 0; m < medallions; m++)
            {
                float a = m / (float)medallions * Mathf.PI * 2.0f + Mathf.PI * 0.25f;
                Vector3 at = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a)) * rimInner;

                RingAt(tris, at, 0.105f, bar * 0.62f, 22);
                FlatRune(tris, at, 0.085f, a, bar * 0.42f, Random.Range(0, 4096));
            }

            // The hub glyph. One mark at the middle, larger than the rest: what the ward is FOR.
            FlatRune(tris, Vector3.zero, 0.16f, Mathf.PI * 0.5f, bar * 0.6f, seed * 31 + 7);

            Random.state = state;

            // Ground art: outward is up, so the reference point is far below the road. Same
            // convention `Sigil` and `Wedges` use.
            return FacetedOriented(tris, "VfxWardCircle", new Vector3(0.0f, -10.0f, 0.0f));
        }

        /// <summary>
        /// A TORN SHEET standing upright: the hole a blink leaves behind.
        ///
        /// ⚠️⚠️ IT IS DELIBERATELY NOT A CIRCLE AND NOT ON THE FLOOR, WHICH IS THE ENTIRE FIX FOR
        /// THE BLINK. Both ends of `SpawnShadowBlinkBurst` used to stamp a cast glyph on the
        /// road: the same pentagram as her hex, twice, half a second apart, for an ability whose
        /// fiction is that space came APART. A mark on the ground says a spell was cast at a
        /// place. A vertical tear says the place itself broke, and it is the only thing in her
        /// kit that stands up.
        ///
        /// ⚠️ IT IS BUILT AS A SPLIT RATHER THAN AS AN OUTLINE. Two ragged edges are walked from
        /// the bottom to the top, pushed apart by a width that swells in the middle and closes at
        /// both ends, and the strokes are the EDGES only: the gap between them is empty, so what
        /// the player sees through the tear is the street behind it. An outline of a lens shape
        /// would be one closed curve; this is two independent curves that happen to meet, which
        /// is why it looks torn rather than drawn.
        ///
        /// ⚠️ STANDS IN THE XY PLANE, so a caller rotates it to face however the blink went. Use
        /// <see cref="Stand"/>, never <see cref="Lay"/>: `Lay` leaves the Y scale at 1 and would
        /// hand a 4 m tear on a 1.55 m mark. § 19.1 records the 2 m ball that came from exactly
        /// that.
        /// </summary>
        /// <param name="steps">Segments up each edge. More is more ragged, not larger.</param>
        /// <param name="bite">How far each edge wanders sideways, as a fraction of the width.</param>
        public static Mesh Rift(int steps = 9, float width = 0.34f, float bite = 0.42f,
                                float bar = 0.05f, int seed = 0)
        {
            steps = Mathf.Clamp(steps, 4, 24);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(256);

            var left = new Vector3[steps + 1];
            var right = new Vector3[steps + 1];

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float y = Mathf.Lerp(-1.0f, 1.0f, t);

                // ⚠️ THE MOUTH IS A SINE, SO IT CLOSES AT BOTH ENDS BY CONSTRUCTION. A tear that
                // is widest at the top or is a constant width is a gap in a wall; one that pinches
                // to nothing at each end is a split in a surface, and the surface here is the air.
                float open = Mathf.Sin(t * Mathf.PI);
                float half = width * open * 0.5f;

                float wanderL = (Random.value - 0.5f) * bite * width * open;
                float wanderR = (Random.value - 0.5f) * bite * width * open;

                left[i] = new Vector3(-half + wanderL, y, 0.0f);
                right[i] = new Vector3(half + wanderR, y, 0.0f);
            }

            for (int i = 0; i < steps; i++)
            {
                UprightBar(tris, left[i], left[i + 1], bar * 0.5f);
                UprightBar(tris, right[i], right[i + 1], bar * 0.5f);
            }

            // ⚠️ THE CROSS-STROKES ARE WHAT MAKE IT A TEAR AND NOT A LEAF. A few short bars
            // spanning the gap read as the last threads of a surface that has not finished
            // parting. Three of them, at uneven heights, because four evenly spaced would be a
            // ladder.
            for (int i = 0; i < 3; i++)
            {
                int at = Mathf.Clamp(Mathf.RoundToInt(steps * (0.28f + i * 0.22f)), 1, steps - 1);
                UprightBar(tris, left[at], right[at], bar * 0.30f);
            }

            var mesh = new Mesh { name = "VfxRift" + seed };
            var verts = tris.ToArray();
            var idx = new int[verts.Length];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;

            mesh.SetVertices(new System.Collections.Generic.List<Vector3>(verts));
            mesh.SetTriangles(idx, 0);

            // Flat forward normals, for the reason `Rune` gives: every triangle is in the XY
            // plane, so a recalculation returns one vector and costs a pass to do it.
            var normals = new Vector3[verts.Length];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.back;
            mesh.normals = normals;

            mesh.RecalculateBounds();

            Random.state = state;
            return mesh;
        }

        /// <summary>
        /// A ring of tapering teeth around an EMPTY middle, built to be looked up at.
        ///
        /// ⚠️⚠️ THE MIDDLE IS THE EFFECT. Every previous version of the eclipse put something
        /// solid in the centre and paid for it: `docs/TODO.md` § 21.2 measured the merged corona
        /// at **78.5 m², 40 per cent of the box in one plate**, and the version that replaced it
        /// still laid a dark disc on the road. An eclipse is a hole with light round the edge, so
        /// the hole is modelled as nothing at all and only the corona is geometry.
        ///
        /// ⚠️⚠️ AND IT HANGS OVERHEAD RATHER THAN LYING ON THE ROAD, WHICH IS THE POINT OF THE
        /// WHOLE ABILITY. 🧑: *"i want the sky to look ominous"*. `Visual.UltimateColumn` made
        /// the argument first and `Visual.SkyEvent` generalises it: the floor is full, up is
        /// empty, and an object in the sky costs zero square metres of the 196 m² this game is
        /// fought in. It is also the only power in the game a player finds by looking UP.
        ///
        /// ⚠️ THE TEETH ARE UNEVEN AND THAT IS LOAD-BEARING. `Wedges` already recorded what
        /// evenly spaced identical plates look like (`docs/TODO.md` § 19.2d: *"nine plates of
        /// identical width at identical spacing read as a black FLOWER"*). A corona is a plasma
        /// edge; every tooth gets its own length and its own width, and the ones that are nearly
        /// gone are what stop it reading as a gear.
        ///
        /// ⚠️ IT IS BUILT FLAT IN XZ AND HUNG FACE-DOWN BY THE CALLER, so `FacetedOriented` is
        /// given a point far ABOVE it: the audience is underneath.
        /// </summary>
        public static Mesh Corona(int teeth = 22, float innerRatio = 0.62f, float ragged = 0.45f,
                                  int seed = 0)
        {
            teeth = Mathf.Clamp(teeth, 8, 48);
            innerRatio = Mathf.Clamp(innerRatio, 0.3f, 0.9f);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(teeth * 12);

            // The lip: a thin continuous ring at the hole's edge, so the corona is attached to
            // something. Without it the teeth float in a circle and read as a sunburst.
            FlatRing(tris, innerRatio, innerRatio + 0.035f, 64);

            float step = Mathf.PI * 2.0f / teeth;
            for (int t = 0; t < teeth; t++)
            {
                float a = t * step;

                // ⚠️ THE ANGULAR JITTER IS SMALL AND THE LENGTH JITTER IS LARGE. Moving a tooth
                // sideways makes the ring look badly made; making it shorter makes the edge look
                // alive. Same distinction `Sigil` draws about wobbling a pentagram versus
                // wobbling its tick marks.
                a += (Random.value - 0.5f) * step * 0.35f;

                float reach = Mathf.Lerp(innerRatio + 0.06f, 1.0f,
                                         1.0f - Random.value * ragged);
                float halfWidth = step * Mathf.Lerp(0.16f, 0.40f, Random.value);

                Vector3 baseL = Ring(a - halfWidth, innerRatio, 0.0f);
                Vector3 baseR = Ring(a + halfWidth, innerRatio, 0.0f);
                Vector3 tip = Ring(a, reach, 0.0f);

                tris.Add(baseL); tris.Add(baseR); tris.Add(tip);
            }

            Random.state = state;

            return FacetedOriented(tris, "VfxCorona", new Vector3(0.0f, 10.0f, 0.0f));
        }

        // ------------------------------------------------------------------ ward helpers

        /// <summary>
        /// One written glyph laid FLAT in the XZ plane, turned so its top points outward.
        ///
        /// ⚠️⚠️ IT IS NOT `Rune` ROTATED, AND THE DIFFERENCE IS NOT COSMETIC. `Rune` builds in
        /// the XY plane because it is a PARTICLE mesh, billboarded toward the camera; laying that
        /// on the road would need a transform per glyph and a separate GameObject to carry it,
        /// which for twelve cells plus four medallions plus a hub is seventeen renderers for one
        /// decal. Emitting the strokes straight into the ward's own triangle list keeps the whole
        /// mark ONE mesh, which is also why it can be faded as one thing.
        ///
        /// ⚠️ THE ALPHABET IS THE SAME INVENTED ONE. Three to five straight strokes: a stem, arms
        /// off it, sometimes a bar. `Rune`'s note has the bound it is built to (*"i dont want
        /// english letters"*), and using the same construction here is what makes the writing on
        /// the ground and the writing lifting off it visibly one hand.
        /// </summary>
        private static void FlatRune(System.Collections.Generic.List<Vector3> tris,
                                     Vector3 at, float size, float facing,
                                     float halfWidth, int seed)
        {
            var state = Random.state;
            Random.InitState(seed);

            // "Up" for a glyph on the ground is radially outward; "across" is the tangent.
            var up = new Vector3(Mathf.Cos(facing), 0.0f, Mathf.Sin(facing));
            var across = new Vector3(-up.z, 0.0f, up.x);

            Vector3 P(float x, float y) => at + across * (x * size) + up * (y * size);

            float h = Random.Range(0.62f, 1.0f);
            float lean = Random.Range(-0.16f, 0.16f);
            FlatBar(tris, P(-lean, -h * 0.5f), P(lean, h * 0.5f), halfWidth);

            int arms = Random.Range(1, 3);
            for (int i = 0; i < arms; i++)
            {
                float atY = Random.Range(-0.34f, 0.40f);
                float len = Random.Range(0.28f, 0.52f);
                float rise = Random.Range(0.18f, 0.46f) * (Random.value < 0.5f ? -1.0f : 1.0f);
                float side = Random.value < 0.5f ? -1.0f : 1.0f;

                float fx = lean * (atY * 2.0f);
                float fy = atY * h;
                FlatBar(tris, P(fx, fy), P(fx + len * side, fy + rise), halfWidth * 0.82f);
            }

            if (Random.value < 0.55f)
            {
                float atY = Random.Range(-0.22f, 0.28f) * h;
                float w = Random.Range(0.24f, 0.44f);
                FlatBar(tris, P(-w, atY), P(w, atY), halfWidth * 0.74f);
            }

            Random.state = state;
        }

        /// <summary>Four straight rules closing a square of the given half-diagonal.</summary>
        private static void Square(System.Collections.Generic.List<Vector3> tris,
                                   float radius, float rotation, float halfWidth)
            => Polygon(tris, 4, radius, rotation, halfWidth);

        /// <summary>A closed straight-sided figure, corner to corner. Not a star: no skip.</summary>
        private static void Polygon(System.Collections.Generic.List<Vector3> tris,
                                    int sides, float radius, float rotation, float halfWidth)
        {
            if (sides < 3) return;

            for (int i = 0; i < sides; i++)
            {
                float a0 = rotation + i / (float)sides * Mathf.PI * 2.0f;
                float a1 = rotation + (i + 1) / (float)sides * Mathf.PI * 2.0f;

                FlatBar(tris, Ring(a0, radius, 0.0f), Ring(a1, radius, 0.0f), halfWidth);
            }
        }

        /// <summary>A small ring somewhere other than the origin, for a medallion.</summary>
        private static void RingAt(System.Collections.Generic.List<Vector3> tris,
                                   Vector3 centre, float radius, float bar, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2.0f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2.0f;

                FlatBar(tris,
                        centre + Ring(a0, radius, 0.0f),
                        centre + Ring(a1, radius, 0.0f),
                        bar * 0.5f);
            }
        }

        /// <summary>A flat annulus band in the XZ plane, as a strip of quads.</summary>
        private static void FlatRing(System.Collections.Generic.List<Vector3> tris,
                                     float rInner, float rOuter, int segments)
        {
            if (rOuter <= rInner) return;

            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2.0f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2.0f;

                var d0 = new Vector3(Mathf.Cos(a0), 0.0f, Mathf.Sin(a0));
                var d1 = new Vector3(Mathf.Cos(a1), 0.0f, Mathf.Sin(a1));

                Vector3 i0 = d0 * rInner, i1 = d1 * rInner;
                Vector3 o0 = d0 * rOuter, o1 = d1 * rOuter;

                tris.Add(i0); tris.Add(o0); tris.Add(o1);
                tris.Add(i0); tris.Add(o1); tris.Add(i1);
            }
        }

        /// <summary>
        /// A flat stroke from a to b, <paramref name="halfWidth"/> either side, in the XZ plane.
        ///
        /// ⚠️ IT IS A QUAD AND NOT A `Tube`. `Bolt` uses tubes because a bolt stands up in the
        /// world and needs a silhouette from the side; a sigil lies on the road and is only ever
        /// seen from above, so a triangular prism per stroke would triple the triangles to draw
        /// two faces nobody can see.
        /// </summary>
        private static void FlatBar(System.Collections.Generic.List<Vector3> tris,
                                    Vector3 a, Vector3 b, float halfWidth)
        {
            Vector3 along = b - a;
            along.y = 0.0f;

            float len = along.magnitude;
            if (len < 0.0001f) return;

            along /= len;

            // Perpendicular in the plane. Cross with up rather than normalising a swapped pair,
            // so a stroke through the origin cannot produce a zero vector.
            var side = new Vector3(-along.z, 0.0f, along.x) * halfWidth;

            Vector3 p0 = a - side, p1 = a + side, p2 = b + side, p3 = b - side;

            tris.Add(p0); tris.Add(p1); tris.Add(p2);
            tris.Add(p0); tris.Add(p2); tris.Add(p3);
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

        // ===================================================================
        // ONE CONSTRUCTION PER HERO, AND THE RULE IS THAT THEY DO NOT SHARE
        //
        // ⚠️⚠️ 🧑 2026-08-26, after the Phaister rebuild: *"improve all other abilities
        // thoroughly too thank you, in their own ways u figure out how, make sure they dont
        // share builders"*, and earlier: *"look for a motif OR something else we can try to add
        // to increase the quality or experience of playing the characters, so that it doesnt
        // feel like party confetti or some shit"*.
        //
        // ⚠️⚠️ THE TRAP IS COPYING PHAISTER, AND IT IS THE OBVIOUS THING TO DO. Her motif is
        // WRITTEN LANGUAGE because she is a witch; giving Zack runes in yellow would be
        // `docs/TODO.md` § 19's fault at the level of the roster instead of the level of one
        // function. So each of these answers a different question about what its element LEAVES
        // BEHIND, and each is a different kind of geometry:
        //
        //   `Fracture`  Cheska   ice PROPAGATES along cracks       a branching walk
        //   `Upheaval`  Dante    earth is DISPLACED, not removed   plates tipped from a dish
        //   `Cinder`    Sean     fire SPREADS and outlives you     a field of separate pieces
        //   `Filament`  Zack     current wants a CIRCUIT           a web between terminals
        //   `Hollow`    Nemu     spirit TAKES SOMETHING AWAY       a rim around nothing
        //
        // ⚠️ AND NOT ONE OF THEM IS A FAN, WHICH IS WHAT EVERY EFFECT IN THIS GAME USED TO BE.
        // `docs/TODO.md` § 19: *"`Splat`, `Star`, `Streak` and `Crystal` are four different
        // POLYGONS handed to ONE builder."* Four of the five below emit their triangles directly
        // and the fifth walks a tree; none of them triangulates a rim around a centre vertex.
        // ===================================================================

        /// <summary>
        /// A CRACK that branches. Cheska's ice, which propagates rather than appears.
        ///
        /// ⚠️⚠️ IT IS A WALK, NOT AN OUTLINE, AND THAT IS THE WHOLE SEPARATION FROM `Wedges`.
        /// `Wedges` makes separate plates with gaps between them, which is right for ground that
        /// is genuinely in pieces (Dante's crust) and wrong for ice: a crack is CONNECTED, and
        /// what makes it read as ice rather than as a drawing is that every branch is narrower
        /// than the one it came off. The recursion carries the width down, so the tips are
        /// hairlines and the trunk is a finger's width, which no fan can do at all.
        ///
        /// ⚠️ IT SPREADS FROM THE CENTRE OUTWARD, so a caller can animate it by scale and get a
        /// propagating crack for free rather than a growing disc. That is `HeroHazards`' job,
        /// but the mesh has to be built the right way round for it to be possible.
        ///
        /// ⚠️ SEEDED, AND THE SEED IS THE POINT. Two Permafrost Sheets cast in one round should
        /// not be the same object twice. `Sigil`'s note argues the opposite for a pentagram (a
        /// hand-wobbled one reads as a mistake); a crack is the case where identical twice is
        /// the mistake.
        /// </summary>
        /// <param name="from">Where the arms START, as a fraction of the radius. A crack running
        /// out of a slab has to begin AT the slab's edge: `ability_ice_sheet_v21.png` is what
        /// starting at the centre looks like, which is a set of white spokes laid over the ice
        /// rather than damage spreading out of it.</param>
        public static Mesh Fracture(int arms = 5, int depth = 3, float bar = 0.055f, int seed = 0,
                                    float from = 0.0f)
        {
            arms = Mathf.Clamp(arms, 3, 9);
            depth = Mathf.Clamp(depth, 1, 4);
            from = Mathf.Clamp01(from);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(512);

            for (int a = 0; a < arms; a++)
            {
                float angle = a / (float)arms * Mathf.PI * 2.0f
                              + Random.Range(-0.25f, 0.25f);

                var start = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)) * from;

                Branch(tris, start, angle, (1.0f - from) * 0.46f, bar, depth);
            }

            Random.state = state;

            return FacetedOriented(tris, "VfxFracture", new Vector3(0.0f, -10.0f, 0.0f));
        }

        /// <summary>One limb of a <see cref="Fracture"/>, and the children it throws.</summary>
        private static void Branch(System.Collections.Generic.List<Vector3> tris,
                                   Vector3 from, float angle, float length,
                                   float halfWidth, int depth)
        {
            if (depth <= 0 || halfWidth < 0.004f) return;

            // ⚠️ EACH SEGMENT KINKS. A straight limb reads as a spoke; ice runs along whatever
            // was weakest, so the direction is perturbed once per segment and the perturbation
            // is what makes the whole shape look found rather than drawn.
            var to = from + new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)) * length;
            FlatBar(tris, from, to, halfWidth);

            int children = Random.value < 0.45f ? 2 : 1;
            for (int c = 0; c < children; c++)
            {
                float turn = Random.Range(0.25f, 0.75f) * (Random.value < 0.5f ? -1.0f : 1.0f);

                Branch(tris, to, angle + turn,
                       length * Random.Range(0.45f, 0.72f),
                       halfWidth * 0.62f,
                       depth - 1);
            }

            // The trunk continues past the fork more often than not, which is what stops the
            // shape becoming a tidy binary tree.
            if (Random.value < 0.6f)
            {
                Branch(tris, to, angle + Random.Range(-0.18f, 0.18f),
                       length * Random.Range(0.55f, 0.8f),
                       halfWidth * 0.78f,
                       depth - 1);
            }
        }

        /// <summary>
        /// Ground TIPPED UP out of a dish. Dante's earth, which is displaced rather than removed.
        ///
        /// ⚠️⚠️ IT CONSERVES MATERIAL AND `Wedges` DOES NOT, WHICH IS THE ENTIRE DIFFERENCE.
        /// `Wedges` lays flat plates with gaps: broken ground seen from above. This one drops the
        /// middle and stands the displaced slabs up around the rim, so what came out of the hole
        /// is visibly what is now leaning over it. That is the only one of these five shapes with
        /// real HEIGHT on purpose, because Dante's motif is that you can see where he has been.
        ///
        /// ⚠️ USE `Stand`, NEVER `Lay`. It has height and `Lay` leaves the Y scale at 1.0;
        /// § 19.1 records the 2 m ball that shipped from exactly that.
        ///
        /// ⚠️ THE DISH IS SHALLOW AND THAT IS DELIBERATE. It is a decal, not collision: a hole a
        /// player could stand in is a hole the bots will path into, and `MapGeometryCheck`
        /// refuses geometry that floats or buries. The read comes from the tipped slabs.
        /// </summary>
        public static Mesh Upheaval(int slabs = 8, float depth = 0.10f, float rise = 0.34f,
                                    int seed = 0)
        {
            slabs = Mathf.Clamp(slabs, 4, 16);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(slabs * 24);

            // The dish: a cone pressed into the road, wound so it is seen from above.
            for (int i = 0; i < slabs * 2; i++)
            {
                float a0 = i / (float)(slabs * 2) * Mathf.PI * 2.0f;
                float a1 = (i + 1) / (float)(slabs * 2) * Mathf.PI * 2.0f;

                tris.Add(new Vector3(0.0f, -depth, 0.0f));
                tris.Add(Ring(a0, 0.62f, 0.0f));
                tris.Add(Ring(a1, 0.62f, 0.0f));
            }

            // ⚠️⚠️ THE SLABS ARE PLACED BY WALKING A VARYING GAP, NOT BY DIVIDING THE CIRCLE, AND
            // THAT IS THE FIX FOR THE BLACK FLOWER. `docs/TODO.md` § 19.2d says it about `Wedges`
            // and it happened here too (`ability_lava_decal_v21.png`): *"nine plates of identical
            // width at identical spacing read as a black FLOWER, which is a manufactured object
            // and the one thing broken ground must never look like."* Jittering an even step by
            // 18 per cent is still an even step; walking a gap drawn from 0.5x to 1.8x means two
            // slabs can genuinely touch and the next gap can be twice as wide as either.
            //
            // ⚠️ AND THE RING IS LEFT OPEN. The walk is scaled to cover about three quarters of
            // the circle, so there is a side the ground did NOT come up on. A stomp is a shove in
            // a direction; a complete ring is a manufactured object again, one ring further out.
            var gaps = new float[slabs];
            float total = 0.0f;
            for (int g = 0; g < slabs; g++)
            {
                gaps[g] = Random.Range(0.5f, 1.8f);
                total += gaps[g];
            }

            float span = Mathf.PI * 2.0f * 0.76f;
            float at = Random.Range(0.0f, Mathf.PI * 2.0f);

            for (int s = 0; s < slabs; s++)
            {
                float step = span * (gaps[s] / total);
                float a = at + step * 0.5f;
                at += step;

                // ⚠️ WIDTH IS DRAWN INDEPENDENTLY OF THE GAP, so a wide slab can sit in a narrow
                // space and overlap its neighbour. Tying the two together is what makes a ring of
                // plates look laid out rather than broken.
                float half = step * Random.Range(0.30f, 0.62f);
                float lean = Random.Range(0.35f, 1.0f);

                // Each slab hinges on the rim and leans OUTWARD, so it is unmistakably the
                // material that used to be in the middle.
                Vector3 hingeL = Ring(a - half, 0.62f, 0.0f);
                Vector3 hingeR = Ring(a + half, 0.62f, 0.0f);
                Vector3 tipL = Ring(a - half * 0.7f, 0.62f + 0.34f * lean, rise * lean);
                Vector3 tipR = Ring(a + half * 0.7f, 0.62f + 0.34f * lean, rise * lean);

                tris.Add(hingeL); tris.Add(hingeR); tris.Add(tipR);
                tris.Add(hingeL); tris.Add(tipR); tris.Add(tipL);
            }

            Random.state = state;

            return FacetedOriented(tris, "VfxUpheaval", new Vector3(0.0f, -10.0f, 0.0f));
        }

        /// <summary>
        /// A field of SEPARATE burning pieces with an advancing edge. Sean's fire.
        ///
        /// ⚠️⚠️ IT IS NOT A SHAPE WITH AN OUTLINE, WHICH IS WHY IT IS NOT `Splat`. `Splat` is one
        /// irregular blob: a thing that landed. Fire is not a thing that landed, it is a set of
        /// places that are burning, and the gaps between them are what say it is spreading. The
        /// pieces are laid on rings of increasing radius with FALLING density, so the middle
        /// reads as consumed and the edge as the front.
        ///
        /// ⚠️ EACH PIECE IS A QUADRILATERAL WITH FOUR INDEPENDENT CORNERS, not a scaled square.
        /// Identical pieces at different sizes read as a texture; corners that disagree read as
        /// char. That is the same argument `Wedges` lost the first time (§ 19.2d, *"nine plates
        /// of identical width at identical spacing read as a black FLOWER"*).
        /// </summary>
        public static Mesh Cinder(int rings = 4, int perRing = 9, float bite = 0.42f, int seed = 0)
        {
            rings = Mathf.Clamp(rings, 2, 8);
            perRing = Mathf.Clamp(perRing, 4, 20);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(rings * perRing * 6);

            for (int r = 0; r < rings; r++)
            {
                float t = (r + 1) / (float)rings;
                float radius = Mathf.Lerp(0.18f, 1.0f, t);

                // ⚠️ DENSITY FALLS OUTWARD, so the front is sparse. A uniform field is a
                // texture; a thinning one is a thing moving.
                int count = Mathf.Max(3, Mathf.RoundToInt(perRing * (1.25f - t * 0.75f)));

                for (int i = 0; i < count; i++)
                {
                    float a = i / (float)count * Mathf.PI * 2.0f
                              + Random.Range(-0.3f, 0.3f) + r * 0.4f;

                    float rr = radius * Random.Range(0.86f, 1.06f);
                    var at = new Vector3(Mathf.Cos(a) * rr, 0.0f, Mathf.Sin(a) * rr);

                    float size = Mathf.Lerp(0.16f, 0.06f, t) * Random.Range(0.7f, 1.35f);

                    Vector3 c0 = at + Jitter(size, bite);
                    Vector3 c1 = at + Jitter(size, bite);
                    Vector3 c2 = at + Jitter(size, bite);
                    Vector3 c3 = at + Jitter(size, bite);

                    tris.Add(c0); tris.Add(c1); tris.Add(c2);
                    tris.Add(c0); tris.Add(c2); tris.Add(c3);
                }
            }

            Random.state = state;

            return FacetedOriented(tris, "VfxCinder", new Vector3(0.0f, -10.0f, 0.0f));
        }

        private static Vector3 Jitter(float size, float bite)
        {
            return new Vector3(Random.Range(-size, size) * (1.0f + bite),
                               0.0f,
                               Random.Range(-size, size) * (1.0f + bite));
        }

        /// <summary>
        /// A WEB between terminals. Zack's current, which wants somewhere to go.
        ///
        /// ⚠️⚠️ IT IS THE ONLY SHAPE IN THIS FILE BUILT FROM POINTS THE CALLER SUPPLIES, and that
        /// is the motif rather than a convenience. Every other effect in this game happens in
        /// empty space; electricity is the one element whose fiction is that it CONNECTS things
        /// that already exist. `HeroHazards.SpawnCircuitArcs` picks the ends off the live scene,
        /// so standing next to the lata while Zack is charged looks different from standing in an
        /// empty corner, which is the whole point.
        ///
        /// ⚠️ IT IS NOT `Bolt`, WHICH IS A TUBE FROM A TO B. A bolt is one jagged span with
        /// volume, right for a strike out of the sky. This is flat, thin and BRANCHED: several
        /// paths leaving one terminal and only some of them arriving, which is what a discharge
        /// looking for a route actually does.
        ///
        /// ⚠️ LOCAL SPACE IS THE UNIT CIRCLE, like every other builder here, so the caller scales
        /// it. Ends are given as directions and reaches rather than as world points for exactly
        /// that reason.
        /// </summary>
        public static Mesh Filament(Vector3[] ends, int forks = 2, float bar = 0.035f, int seed = 0)
        {
            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(256);

            if (ends != null)
            {
                foreach (var end in ends)
                {
                    // The main run, in three kinked segments, so it is a path rather than a line.
                    Vector3 previous = Vector3.zero;
                    for (int seg = 1; seg <= 3; seg++)
                    {
                        float t = seg / 3.0f;
                        Vector3 on = Vector3.Lerp(Vector3.zero, end, t);

                        var side = new Vector3(-end.z, 0.0f, end.x).normalized;
                        on += side * Random.Range(-0.09f, 0.09f) * (1.0f - Mathf.Abs(t - 0.5f) * 2.0f);

                        FlatBar(tris, previous, on, bar * 0.5f);

                        // ⚠️ THE FORKS DIE IN MID-AIR, WHICH IS WHAT SAYS "LOOKING FOR A ROUTE".
                        // A discharge that only ever arrives is a wire; the failed branches are
                        // the difference between a circuit diagram and a live arc.
                        for (int f = 0; f < forks && seg < 3; f++)
                        {
                            if (Random.value > 0.55f) continue;

                            Vector3 stray = on
                                + side * Random.Range(-0.30f, 0.30f)
                                + end.normalized * Random.Range(0.05f, 0.22f);

                            FlatBar(tris, on, stray, bar * 0.28f);
                        }

                        previous = on;
                    }
                }
            }

            Random.state = state;

            return FacetedOriented(tris, "VfxFilament", new Vector3(0.0f, -10.0f, 0.0f));
        }

        /// <summary>
        /// A rim around NOTHING. Nemu's spirit, whose motif is absence.
        ///
        /// ⚠️⚠️ EVERY OTHER EFFECT IN THIS GAME ADDS SOMETHING TO THE FRAME AND THIS ONE HAS TO
        /// LOOK LIKE IT REMOVED SOMETHING. `docs/TODO.md` § 27.5: *"a screenshot of her ultimate
        /// has fewer things in it than the same frame without it"*. The way to draw a hole is to
        /// draw only its EDGE and leave the middle empty, which is the same argument
        /// `VfxShapes.Corona` makes about an eclipse and is why those two are the only shapes
        /// here with nothing in the centre.
        ///
        /// ⚠️ THE INNER EDGE IS TORN AND THE OUTER ONE IS CLEAN. A clean annulus is a washer.
        /// What makes this read as something missing is that the boundary facing the hole is
        /// ragged, as though the surface gave way, while the far side is still ordinary road.
        ///
        /// ⚠️ IT IS NOT `Collar`, WHICH IS A REAL ANNULUS WITH WALLS AND IS THE BOUNDARY BUILDER
        /// EVERY OTHER RIM IN THE GAME USES. A `Collar` says "here is an edge"; this says "here
        /// is where something stopped existing", and the two must not be the same object.
        /// </summary>
        public static Mesh Hollow(int segments = 40, float innerRatio = 0.66f, float tear = 0.16f,
                                  int seed = 0)
        {
            segments = Mathf.Clamp(segments, 12, 96);
            innerRatio = Mathf.Clamp(innerRatio, 0.2f, 0.92f);

            var state = Random.state;
            Random.InitState(seed);

            var tris = new System.Collections.Generic.List<Vector3>(segments * 6);

            var inner = new float[segments + 1];
            for (int i = 0; i <= segments; i++)
                inner[i] = innerRatio * (1.0f + Random.Range(-tear, tear));

            // Close the ring exactly, or the first and last segments leave a wedge-shaped scar
            // that reads as a modelling error rather than as a tear.
            inner[segments] = inner[0];

            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2.0f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2.0f;

                Vector3 i0 = Ring(a0, inner[i], 0.0f);
                Vector3 i1 = Ring(a1, inner[i + 1], 0.0f);
                Vector3 o0 = Ring(a0, 1.0f, 0.0f);
                Vector3 o1 = Ring(a1, 1.0f, 0.0f);

                tris.Add(i0); tris.Add(o0); tris.Add(o1);
                tris.Add(i0); tris.Add(o1); tris.Add(i1);
            }

            Random.state = state;

            return FacetedOriented(tris, "VfxHollow", new Vector3(0.0f, -10.0f, 0.0f));
        }

        /// <summary>
        /// The same mesh, drawable from BOTH sides.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE AN UPRIGHT SHAPE IS CULLED FROM HALF THE ARENA AND THAT LOOKS
        /// EXACTLY LIKE A SPAWN FAILURE. `Fan`'s note is the standing record of this class:
        /// *"the object exists, the renderer is enabled, the material is correct and the
        /// hierarchy looks right in every inspector; the shape is simply not in the frame"*, and
        /// it cost a whole capture pass the first time. `ability_blink_rift_eye_v19.png` is the
        /// second time: the tear was there, its light was spilling on the road, and the geometry
        /// was turned a quarter turn away from the only camera in the shot.
        ///
        /// ⚠️ GROUND ART DOES NOT NEED THIS AND MUST NOT GET IT. A decal is seen from above, from
        /// above only, and `FacetedOriented` already turns every triangle the right way for that.
        /// Doubling those would double the triangle count of the busiest meshes in the game to
        /// draw faces under the road.
        ///
        /// ⚠️⚠️ WHAT NEEDS IT IS ANYTHING THAT STANDS UP AND CAN BE WALKED AROUND, which in a
        /// four-player arena is all of them: the tear a blink leaves, the characters standing on
        /// a ward, the glyphs falling onto an arrival. There is no facing that is right for four
        /// players at once, so the honest answer is that a rip in the world is a rip from behind
        /// too.
        /// </summary>
        public static Mesh TwoSided(Mesh mesh)
        {
            if (mesh == null) return null;

            var verts = new System.Collections.Generic.List<Vector3>();
            mesh.GetVertices(verts);

            var tris = mesh.triangles;
            int count = tris.Length;

            var doubled = new int[count * 2];
            for (int i = 0; i < count; i += 3)
            {
                doubled[i] = tris[i];
                doubled[i + 1] = tris[i + 1];
                doubled[i + 2] = tris[i + 2];

                // The back face is the same triangle with two indices swapped, which is the one
                // operation that reverses winding without moving a vertex.
                doubled[count + i] = tris[i];
                doubled[count + i + 1] = tris[i + 2];
                doubled[count + i + 2] = tris[i + 1];
            }

            mesh.SetTriangles(doubled, 0);

            // ⚠️ NORMALS ARE RECALCULATED RATHER THAN KEPT. A vertex shared by a front and a back
            // triangle averages to something facing neither way, so this deliberately lets Unity
            // produce the average: these are ghosted, emissive shapes whose look comes from
            // `VfxMaterial.Ghost` rather than from being shaded, and a flat unlit read is the
            // house style for them anyway.
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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
