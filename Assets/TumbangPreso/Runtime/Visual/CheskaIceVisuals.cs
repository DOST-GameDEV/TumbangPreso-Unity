using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>Cheska's authored ice slabs, readable restraints and small thaw fragments.</summary>
    public static class CheskaIceVisuals
    {
        private static readonly Color Ice = new Color(.23f,.62f,.73f,.72f);

        public static GameObject Piece(Transform parent, string name, string meshName, Color color)
        {
            var mesh = Resources.Load<Mesh>("Models/CheskaIce/" + meshName);
            if (mesh == null) throw new System.InvalidOperationException("Missing Cheska ice mesh: " + meshName);
            var go = new GameObject(name);
            go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            VfxMaterial.Ghost(renderer,color,.035f);
            renderer.sharedMaterial.SetFloat("_Glossiness",.72f);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        public static void BuildWall(Transform parent, float span, float thickness, bool split = false, bool renderOnly = false)
        {
            string[] meshes = { "wall_left", "wall_center", "wall_right" };
            for (int i=0;i<3;i++)
            {
                if (split && i == 1) continue;
                var slab = Piece(parent,"IcePillar_" + (i-1),meshes[i],Ice);
                slab.transform.localPosition = new Vector3((i-1)*.75f*span,0,0);
                slab.transform.localScale = new Vector3(1,1,thickness);
                // Anchor to the lowest supporting corner, so the base buries into a
                // kerb instead of suspending half the barrier above the street.
                float lowest = float.PositiveInfinity;
                for (int x=-1;x<=1;x+=2)
                    for (int z=-1;z<=1;z+=2)
                        lowest = Mathf.Min(lowest,VfxShapes.GroundPoint(slab.transform.TransformPoint(
                            new Vector3(x*.425f,0,z*.275f))).y);
                var position=slab.transform.position; position.y=lowest; slab.transform.position=position;
                // The same fractured surface blocks slippers and bodies. Decorative
                // toppers previously extended outside the three box colliders.
                if(!renderOnly) { var collider=slab.AddComponent<MeshCollider>();
                collider.sharedMesh=slab.GetComponent<MeshFilter>().sharedMesh;
                collider.convex=true; }
                Cracks(slab.transform,i);
            }
        }

        /// <summary>
        /// GLACIAL WALL (ABILITY-2, owner: *"an arc-shaped icicle wall"*): the barricade's own ice pieces
        /// stood along an arc facing her, five of them at their own heights, each with its collider, so
        /// slippers and bodies stop at the curve. `arcLength` along the arc, bowed on `radius`.
        /// </summary>
        public static void BuildArc(Transform parent, float arcLength, float radius, bool renderOnly = false)
        {
            string[] meshes = { "wall_left", "wall_center", "wall_center", "wall_center", "wall_right" };
            float[] heights = { 0.86f, 1.0f, 1.12f, 0.96f, 0.84f };
            float sweepDeg = arcLength / radius * Mathf.Rad2Deg;
            for (int i = 0; i < 5; i++)
            {
                float a = (-0.5f + i / 4f) * sweepDeg;
                var slab = Piece(parent, "IceArc_" + i, meshes[i], Ice);
                // The arc bows AWAY from her: its centre is behind the aimed point by the radius.
                Vector3 at = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, radius) - new Vector3(0f, 0f, radius);
                slab.transform.localPosition = at;
                slab.transform.localRotation = Quaternion.Euler(0f, a, 0f);
                slab.transform.localScale = new Vector3(arcLength / 5f / 0.85f, heights[i], 1f);
                var position = slab.transform.position; position.y = VfxShapes.GroundPoint(position).y; slab.transform.position = position;
                if (!renderOnly)
                {
                    var collider = slab.AddComponent<MeshCollider>();
                    collider.sharedMesh = slab.GetComponent<MeshFilter>().sharedMesh;
                    collider.convex = true;
                }
                Cracks(slab.transform, i == 0 ? 0 : i == 4 ? 2 : 1);
            }
        }

        private static void Cracks(Transform slab, int index)
        {
            // Narrow seams on both broad faces. The large forms carry the wall;
            // these sparse connected fractures describe ice, without black trim.
            for (int side=-1;side<=1;side+=2)
            {
                var go=new GameObject("IceFracture");go.transform.SetParent(slab,false);
                var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;
                line.positionCount=5;float z=side*.277f;
                float sign=index==2?-1:1;
                line.SetPositions(new[] {new Vector3(-.29f*sign,.18f,z),new Vector3(.04f*sign,.68f,z),
                    new Vector3(-.11f*sign,.94f,z),new Vector3(.19f*sign,1.35f,z),new Vector3(.10f*sign,1.76f,z)});
                line.widthMultiplier=.012f;line.numCornerVertices=0;line.numCapVertices=0;
                VfxMaterial.Ghost(line,new Color(.73f,.92f,.95f,.55f),.08f);
            }
        }

        public static void BuildRestraint(Transform parent)
        {
            // Leave the torso and face open, including the victim's own eye line.
            // The existing frost coat communicates the full-body stun; these shards
            // make its physical arrival readable without an opaque camera-sized cube.
            for (int i=0;i<5;i++)
            {
                float angle=(i*72+18)*Mathf.Deg2Rad;
                var shard=Piece(parent,"IceRestraint_"+i,"thaw_shard",new Color(.33f,.73f,.82f,.55f));
                shard.transform.localPosition=new Vector3(Mathf.Cos(angle)*.47f,.02f,Mathf.Sin(angle)*.47f);
                shard.transform.localRotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0);
                shard.transform.localScale=new Vector3(.30f,.66f+(i%3)*.11f,.55f);
            }
        }

        public static void Shatter(Vector3 center, bool wall)
        {
            var root=new GameObject(wall?"BarricadeThaw":"RestraintThaw");root.transform.position=center;
            int count=wall?8:5;
            for (int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                var shard=Piece(root.transform,"ThawFragment","thaw_shard",new Color(.53f,.82f,.87f,.62f));
                shard.transform.localScale=Vector3.one*(wall?.16f:.10f);
                shard.transform.localPosition=new Vector3(Mathf.Cos(angle)*.22f,wall?.7f:.35f,Mathf.Sin(angle)*.22f);
                var drift=shard.AddComponent<IceThawFragment>();
                drift.Velocity=new Vector3(Mathf.Cos(angle)*1.1f,.8f+(i%3)*.15f,Mathf.Sin(angle)*1.1f);
                drift.Spin=new Vector3(70+i*7,95-i*11,40);
            }
            Object.Destroy(root,.7f);
        }
    }

}
