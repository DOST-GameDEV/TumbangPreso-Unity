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
