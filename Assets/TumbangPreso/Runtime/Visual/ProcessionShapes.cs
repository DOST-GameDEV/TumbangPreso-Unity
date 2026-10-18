using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE PROCESSION'S SHAPES (Nemu's PRUSISYON, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 1). The
    /// owner, 2026-10-11: *"i dont want ghosts to be too blocky kahit translucent sila or like not real models"*. So every
    /// ghost is a SMOOTH surface turned from a hand-typed profile (a lathe), not a stack of cubes: a domed hood that pinches
    /// at the neck, a sheet that flares and hangs, and a hem that ripples unevenly. Each ghost has its own profile and its
    /// own hem, typed below: the Kandila souls are small and round-headed, Lola hunches under her shawl, the Sakristan is
    /// square-shouldered, the White Lady is a long narrow veil. Meshes are cached: twelve souls share one.
    ///
    /// ⚠️ Units are metres, y up, the hem at the bottom. glTF's X flip does not apply (built here, not imported).
    /// </summary>
    public static class ProcessionShapes
    {
        /// <summary>A Kandila soul's sheet, top to hem: (radius, height). Hood dome, the pinch under it, the shoulders,
        /// then the drape flaring to the hem. 0.62 m tall: a child of the procession.</summary>
        private static readonly Vector2[] SoulProfile =
        {
            new Vector2(0f, .62f), new Vector2(.055f, .615f), new Vector2(.10f, .595f), new Vector2(.135f, .56f),
            new Vector2(.155f, .51f), new Vector2(.16f, .455f), new Vector2(.15f, .405f), new Vector2(.138f, .375f),
            new Vector2(.15f, .345f), new Vector2(.172f, .30f), new Vector2(.188f, .22f), new Vector2(.20f, .13f),
            new Vector2(.212f, .05f), new Vector2(.222f, 0f),
        };

        /// <summary>The soul's hem: (lobes, amplitude m, phase) summed, so the ripple is uneven, never a gear.</summary>
        private static readonly Vector3[] SoulHem = { new Vector3(3f, .045f, .4f), new Vector3(5f, .022f, 1.9f), new Vector3(8f, .01f, .7f) };

        /// <summary>Lola: hunched, a broad shawl over her head (alampay) falling to a long skirt. 1.05 m, stooped.</summary>
        private static readonly Vector2[] LolaProfile =
        {
            new Vector2(0f, 1.05f), new Vector2(.07f, 1.045f), new Vector2(.13f, 1.02f), new Vector2(.17f, .98f),
            new Vector2(.20f, .92f), new Vector2(.235f, .84f), new Vector2(.25f, .76f), new Vector2(.235f, .70f),
            new Vector2(.215f, .62f), new Vector2(.225f, .45f), new Vector2(.245f, .25f), new Vector2(.265f, .08f),
            new Vector2(.275f, 0f),
        };
        private static readonly Vector3[] LolaHem = { new Vector3(4f, .03f, 1.1f), new Vector3(7f, .015f, .2f) };

        /// <summary>The Sakristan: small and square-shouldered in his cassock, a short cape (sobrepelliz). 0.78 m.</summary>
        private static readonly Vector2[] SakristanProfile =
        {
            new Vector2(0f, .78f), new Vector2(.06f, .775f), new Vector2(.11f, .75f), new Vector2(.135f, .70f),
            new Vector2(.13f, .64f), new Vector2(.11f, .60f), new Vector2(.20f, .57f), new Vector2(.215f, .52f),
            new Vector2(.205f, .44f), new Vector2(.17f, .42f), new Vector2(.175f, .25f), new Vector2(.185f, .08f),
            new Vector2(.19f, 0f),
        };
        private static readonly Vector3[] SakristanHem = { new Vector3(6f, .02f, .5f), new Vector3(2f, .02f, 2.4f) };

        /// <summary>The White Lady: a long narrow veil, faceless, hair under it, no feet. 2.6 m: the parade's scale.</summary>
        private static readonly Vector2[] LadyProfile =
        {
            new Vector2(0f, 2.6f), new Vector2(.08f, 2.595f), new Vector2(.14f, 2.56f), new Vector2(.17f, 2.49f),
            new Vector2(.175f, 2.40f), new Vector2(.16f, 2.30f), new Vector2(.20f, 2.18f), new Vector2(.25f, 1.95f),
            new Vector2(.27f, 1.50f), new Vector2(.29f, 1.00f), new Vector2(.32f, .50f), new Vector2(.36f, .10f),
            new Vector2(.38f, 0f),
        };
        private static readonly Vector3[] LadyHem = { new Vector3(3f, .09f, 2.2f), new Vector3(5f, .05f, .3f), new Vector3(9f, .02f, 1.4f) };

        /// <summary>A sleeve ending in a mitten (the cast's simple hand), along +z: (radius, z).</summary>
        private static readonly Vector2[] SleeveProfile =
        {
            new Vector2(.042f, 0f), new Vector2(.048f, .05f), new Vector2(.046f, .12f), new Vector2(.05f, .16f),
            new Vector2(.062f, .19f), new Vector2(.066f, .225f), new Vector2(.058f, .255f), new Vector2(.036f, .275f),
            new Vector2(0f, .283f),
        };

        /// <summary>A candle: wax with a softened lip. And its flame: a teardrop.</summary>
        private static readonly Vector2[] CandleProfile =
        {
            new Vector2(0f, 0f), new Vector2(.024f, 0f), new Vector2(.025f, .06f), new Vector2(.024f, .13f),
            new Vector2(.02f, .145f), new Vector2(0f, .148f),
        };
        private static readonly Vector2[] FlameProfile =
        {
            new Vector2(0f, 0f), new Vector2(.014f, .006f), new Vector2(.021f, .02f), new Vector2(.02f, .038f),
            new Vector2(.013f, .058f), new Vector2(.005f, .075f), new Vector2(0f, .084f),
        };
        /// <summary>A small rounded bead (eyes, a rosary bead, the bell's clapper).</summary>
        private static readonly Vector2[] BeadProfile =
        {
            new Vector2(0f, -.5f), new Vector2(.26f, -.43f), new Vector2(.43f, -.25f), new Vector2(.5f, 0f),
            new Vector2(.43f, .25f), new Vector2(.26f, .43f), new Vector2(0f, .5f),
        };
        /// <summary>The Sakristan's hand bell: a flared cup on a handle, mouth down.</summary>
        private static readonly Vector2[] BellProfile =
        {
            new Vector2(0f, .17f), new Vector2(.012f, .17f), new Vector2(.012f, .11f), new Vector2(.03f, .10f),
            new Vector2(.042f, .08f), new Vector2(.05f, .04f), new Vector2(.062f, .0f), new Vector2(.055f, -.004f),
            new Vector2(0f, -.004f),
        };

        /// <summary>A moth's forewing and hindwing as one outline from the body out, typed (x out, z forward).</summary>
        private static readonly Vector2[] WingOutline =
        {
            new Vector2(0f, .012f), new Vector2(.03f, .03f), new Vector2(.062f, .036f), new Vector2(.085f, .025f),
            new Vector2(.088f, .005f), new Vector2(.07f, -.008f), new Vector2(.06f, -.022f), new Vector2(.052f, -.042f),
            new Vector2(.035f, -.05f), new Vector2(.016f, -.036f), new Vector2(0f, -.014f),
        };
        /// <summary>A bare sole: heel, arch, ball and five toes merged into one outline (x right, z toes).</summary>
        private static readonly Vector2[] SoleOutline =
        {
            new Vector2(0f, -.095f), new Vector2(.028f, -.088f), new Vector2(.038f, -.06f), new Vector2(.032f, -.02f),
            new Vector2(.04f, .03f), new Vector2(.046f, .06f), new Vector2(.04f, .085f), new Vector2(.02f, .1f),
            new Vector2(-.005f, .102f), new Vector2(-.03f, .09f), new Vector2(-.042f, .062f), new Vector2(-.036f, .025f),
            new Vector2(-.024f, -.02f), new Vector2(-.03f, -.06f), new Vector2(-.022f, -.088f),
        };

        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static Mesh Soul => Get("Soul", () => Lathe(SoulProfile, 28, SoulHem, .9f));
        public static Mesh Lola => Get("Lola", () => Lathe(LolaProfile, 28, LolaHem, .92f));
        public static Mesh Sakristan => Get("Sakristan", () => Lathe(SakristanProfile, 28, SakristanHem, .85f));
        public static Mesh Lady => Get("Lady", () => Lathe(LadyProfile, 30, LadyHem, .8f));
        public static Mesh Sleeve => Get("Sleeve", () => LatheAlongZ(SleeveProfile, 14));
        public static Mesh Candle => Get("Candle", () => Lathe(CandleProfile, 12, null, 1f));
        public static Mesh Flame => Get("Flame", () => Lathe(FlameProfile, 10, null, 1f));
        public static Mesh Bead => Get("Bead", () => Lathe(BeadProfile, 12, null, 1f));
        public static Mesh Bell => Get("Bell", () => Lathe(BellProfile, 16, null, 1f));
        public static Mesh Wing => Get("Wing", () => Flat(WingOutline, true));
        public static Mesh Sole => Get("Sole", () => Flat(SoleOutline, false));

        private static Mesh Get(string key, System.Func<Mesh> build)
        {
            if (Cache.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            mesh = build(); mesh.name = "Procession" + key;
            Cache[key] = mesh;
            return mesh;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Cache.Clear();

        /// <summary>
        /// A surface of revolution round y. `hem` ripples only the LAST ring (the cloth's edge) and lifts it a little where it
        /// rises, so the bottom reads as hanging cloth. `depth` squashes it front to back (a sheet over a body is not round).
        /// </summary>
        private static Mesh Lathe(Vector2[] profile, int sides, Vector3[] hem, float depth)
        {
            int rings = profile.Length;
            var vertices = new List<Vector3>(rings * (sides + 1));
            var uvs = new List<Vector2>(rings * (sides + 1));
            for (int r = 0; r < rings; r++)
            {
                bool edge = r == rings - 1 && hem != null;
                for (int s = 0; s <= sides; s++)
                {
                    float a = s * Mathf.PI * 2f / sides;
                    float radius = profile[r].x, y = profile[r].y;
                    if (edge || (hem != null && r == rings - 2))
                    {
                        float ripple = 0f;
                        foreach (var h in hem) ripple += Mathf.Sin(a * h.x + h.z) * h.y;
                        float k = edge ? 1f : .35f;
                        y += ripple * k;
                        radius += Mathf.Max(0f, -ripple) * .4f * k;
                    }
                    vertices.Add(new Vector3(Mathf.Sin(a) * radius, y, Mathf.Cos(a) * radius * depth));
                    uvs.Add(new Vector2((float)s / sides, (float)r / (rings - 1)));
                }
            }
            var triangles = new List<int>((rings - 1) * sides * 6);
            for (int r = 0; r < rings - 1; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = r * (sides + 1) + s, b = a + sides + 1;
                    // Wound so the faces look OUT (the profile runs top to hem, the angle from +z toward +x).
                    triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                }
            var mesh = new Mesh();
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The same lathe laid along +z (a sleeve reaching forward).</summary>
        private static Mesh LatheAlongZ(Vector2[] profile, int sides)
        {
            var swapped = new Vector2[profile.Length];
            for (int i = 0; i < profile.Length; i++) swapped[i] = new Vector2(profile[i].x, -profile[i].y);
            var mesh = Lathe(swapped, sides, null, 1f);
            var v = mesh.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = new Vector3(v[i].x, v[i].z, -v[i].y);
            mesh.vertices = v;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A flat fan from the outline's centre, in x/z, facing up; two-sided for a wing seen from below.</summary>
        private static Mesh Flat(Vector2[] outline, bool twoSided)
        {
            Vector2 centre = Vector2.zero;
            foreach (var p in outline) centre += p;
            centre /= outline.Length;
            var vertices = new List<Vector3> { new Vector3(centre.x, 0f, centre.y) };
            foreach (var p in outline) vertices.Add(new Vector3(p.x, 0f, p.y));
            var triangles = new List<int>();
            int n = outline.Length;
            for (int i = 0; i < n; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % n;
                triangles.Add(0); triangles.Add(b); triangles.Add(a);
            }
            // Face up whichever way the outline was typed round.
            Vector3 up = Vector3.Cross(vertices[triangles[1]] - vertices[triangles[0]], vertices[triangles[2]] - vertices[triangles[0]]);
            if (up.y < 0f)
                for (int i = 0; i < triangles.Count; i += 3) { int t = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = t; }
            var mesh = new Mesh();
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return twoSided ? VfxShapes.TwoSided(mesh) : mesh;
        }
    }
}
