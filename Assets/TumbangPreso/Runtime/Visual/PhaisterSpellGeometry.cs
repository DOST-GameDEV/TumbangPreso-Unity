using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // A binding knot, a vertical passage and a closing fold have different jobs.
    // They share an ink language, not one enlarged circle or repeated rune stamp.
    internal static class PhaisterSpellGeometry
    {
        public static Mesh Binding(bool tight)
        {
            var lines=new List<Vector3>();
            if(tight)
            {
                Ground(lines,.025f,-.48f,.25f,-.25f,.33f,.27f,.28f,.47f,.15f);
                Ground(lines,.025f,.47f,-.15f,.26f,-.29f,-.29f,-.31f,-.48f,-.20f);
                Ground(lines,.021f,-.28f,.36f,-.16f,-.34f);
                Ground(lines,.022f,.02f,.31f,.10f,-.28f);
                Ground(lines,.018f,.31f,.25f,.27f,-.30f);
            }
            else
            {
                Ground(lines,.022f,-.53f,0,-.31f,.25f,-.02f,.36f,.27f,.26f,.52f,.02f);
                Ground(lines,.022f,.52f,.02f,.30f,-.23f,.02f,-.34f,-.27f,-.24f,-.53f,0);
                Ground(lines,.023f,-.12f,-.16f,.11f,.15f);
                Ground(lines,.017f,-.12f,.14f,.11f,-.15f);
            }
            Ground(lines,.022f,-.79f,.23f,-.64f,.46f,-.44f,.52f);
            Ground(lines,.024f,.61f,.54f,.80f,.23f,.72f,-.03f);
            Ground(lines,.020f,.19f,-.77f,-.11f,-.80f,-.30f,-.64f);
            return MeshFrom(lines,tight?"TightLunarBinding":"OpenLunarBinding");
        }

        public static Mesh Fold()
        {
            var lines=new List<Vector3>();
            Ground(lines,.027f,-.62f,-.18f,-.28f,-.11f,0,.10f,.32f,.12f,.60f,.26f);
            Ground(lines,.018f,-.48f,.08f,-.18f,.17f,.12f,.02f,.45f,-.03f);
            return MeshFrom(lines,"ClosingShadowFold");
        }

        // ⚠️ HER CRESCENT (HERO-10, the middle of her aim sigil; Capul's moon story, `CHARACTER_ORIGINS.md`). Typed as pairs down
        // the moon, outer edge then inner edge, from the top horn to the bottom: thick through the middle, the horns to points,
        // opening to the right, a little lopsided (a drawn moon, not a compass one).
        private static readonly Vector2[] CrescentPairs=
        {
            new Vector2(.30f,.92f),new Vector2(.30f,.92f),
            new Vector2(-.06f,.99f),new Vector2(.05f,.80f),
            new Vector2(-.46f,.86f),new Vector2(-.22f,.62f),
            new Vector2(-.78f,.52f),new Vector2(-.44f,.34f),
            new Vector2(-.96f,.06f),new Vector2(-.55f,.02f),
            new Vector2(-.90f,-.38f),new Vector2(-.47f,-.30f),
            new Vector2(-.66f,-.74f),new Vector2(-.28f,-.58f),
            new Vector2(-.28f,-.97f),new Vector2(-.02f,-.78f),
            new Vector2(.16f,-.95f),new Vector2(.16f,-.95f),
        };

        /// <summary>
        /// ⚠️⚠️ ONE OF HER RUNES LAID FLAT ON THE COURT (HERO-10, film v8). `VfxShapes.Rune` is drawn in the XY plane, UPRIGHT (its
        /// own note: every triangle lies in XY, normals face -z), and her ground marks passed it to `VfxShapes.Lay`, which lays a
        /// mesh AS IT IS: so every "sigil on the court" (SPOTLIGHT PIN's sweep, OMEN's ring, the moonlight's runes) stood on edge,
        /// and `DrapeToGround` then flattened each upright glyph onto a line. That is why the pin's sweep "did not show at court
        /// distance" (v7) and OMEN's ring never wrote itself: the geometry was a sliver. This turns the same letter to lie in XZ,
        /// each triangle wound to face up.
        /// </summary>
        public static Mesh FlatRune(int seed,float bar)
        {
            var upright=VfxShapes.Rune(seed,bar);
            var v=upright.vertices;var tris=upright.triangles;
            if(Application.isPlaying)Object.Destroy(upright);else Object.DestroyImmediate(upright);
            var flat=new List<Vector3>(tris.Length);
            for(int i=0;i+2<tris.Length;i+=3)
            {
                var a=new Vector3(v[tris[i]].x,0,v[tris[i]].y);var b=new Vector3(v[tris[i+1]].x,0,v[tris[i+1]].y);var c=new Vector3(v[tris[i+2]].x,0,v[tris[i+2]].y);
                if(Vector3.Cross(b-a,c-a).y<0){var t=b;b=c;c=t;}
                flat.Add(a);flat.Add(b);flat.Add(c);
            }
            return MeshFrom(flat,"FlatLunarRune"+seed);
        }

        public static Mesh Crescent()
        {
            var triangles=new List<Vector3>();
            for(int i=0;i+3<CrescentPairs.Length;i+=2)
            {
                var o0=CrescentPairs[i];var n0=CrescentPairs[i+1];var o1=CrescentPairs[i+2];var n1=CrescentPairs[i+3];
                triangles.Add(new Vector3(o0.x,0,o0.y));triangles.Add(new Vector3(o1.x,0,o1.y));triangles.Add(new Vector3(n1.x,0,n1.y));
                triangles.Add(new Vector3(o0.x,0,o0.y));triangles.Add(new Vector3(n1.x,0,n1.y));triangles.Add(new Vector3(n0.x,0,n0.y));
            }
            return VfxShapes.TwoSided(MeshFrom(triangles,"LunarCrescent"));
        }

        private static readonly Vector2[] TearOutline={new Vector2(0,-1),new Vector2(.10f,-.65f),
            new Vector2(.24f,-.16f),new Vector2(.11f,.17f),new Vector2(.15f,.61f),new Vector2(0,1),
            new Vector2(-.13f,.51f),new Vector2(-.22f,.07f),new Vector2(-.08f,-.32f),new Vector2(-.11f,-.76f)};

        public static Mesh Passage(bool edge)
        {
            var triangles=new List<Vector3>();
            for(int i=0;i<TearOutline.Length;i++)
            {
                var a=TearOutline[i];var b=TearOutline[(i+1)%TearOutline.Length];
                if(!edge)
                {triangles.Add(Vector3.zero);triangles.Add(new Vector3(a.x,a.y,0));triangles.Add(new Vector3(b.x,b.y,0));}
                else
                {
                    var inwardA=new Vector2(a.x*.85f,a.y*.975f);var inwardB=new Vector2(b.x*.85f,b.y*.975f);
                    triangles.Add(new Vector3(a.x,a.y,-.003f));triangles.Add(new Vector3(b.x,b.y,-.003f));triangles.Add(new Vector3(inwardB.x,inwardB.y,-.003f));
                    triangles.Add(new Vector3(a.x,a.y,-.003f));triangles.Add(new Vector3(inwardB.x,inwardB.y,-.003f));triangles.Add(new Vector3(inwardA.x,inwardA.y,-.003f));
                }
            }
            return VfxShapes.TwoSided(MeshFrom(triangles,edge?"PassageInkEdge":"ShadowPassage"));
        }

        private static void Ground(List<Vector3> triangles,float width,params float[] xz)
        {
            for(int i=0;i<xz.Length-2;i+=2)
            {
                var a=new Vector3(xz[i],0,xz[i+1]);var b=new Vector3(xz[i+2],0,xz[i+3]);
                var side=Vector3.Cross(Vector3.up,b-a).normalized*width;
                triangles.Add(a-side);triangles.Add(b-side);triangles.Add(b+side);
                triangles.Add(a-side);triangles.Add(b+side);triangles.Add(a+side);
            }
        }

        private static Mesh MeshFrom(List<Vector3> vertices,string name)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);var indices=new int[vertices.Count];
            for(int i=0;i<indices.Length;i++)indices[i]=i;
            mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
