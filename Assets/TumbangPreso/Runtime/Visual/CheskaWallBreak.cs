using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>One render-only timeline for chunks originating across the real wall.</summary>
    public sealed class CheskaWallBreak : MonoBehaviour, IVfxTimeline
    {
        public float LifeSeconds => .8f;
        private float _age;
        private PieceState[] _pieces;

        private struct Recipe
        {
            public int Slab;
            public Vector3 At, Size, Velocity, Spin;
            public Recipe(int slab, Vector3 at, Vector3 size, Vector3 velocity, Vector3 spin)
            { Slab=slab; At=at; Size=size; Velocity=velocity; Spin=spin; }
        }

        // Each source pillar has a different weight distribution and split direction.
        // At is a fraction of that authored slab; sizes follow its actual dimensions.
        private static readonly Recipe[] Pieces =
        {
            new Recipe(0,new Vector3(-.12f,.18f,.04f),new Vector3(.74f,.32f,.68f),new Vector3(-.85f,.45f,-.25f),new Vector3(52,-68,-100)),
            new Recipe(0,new Vector3(.08f,.50f,-.06f),new Vector3(.66f,.36f,.74f),new Vector3(-.52f,.75f,.30f),new Vector3(-90,44,-62)),
            new Recipe(0,new Vector3(-.05f,.82f,.02f),new Vector3(.52f,.27f,.62f),new Vector3(-.95f,.32f,-.12f),new Vector3(110,-36,-82)),
            new Recipe(1,new Vector3(.10f,.16f,-.03f),new Vector3(.69f,.29f,.72f),new Vector3(-.35f,.30f,.40f),new Vector3(-62,84,46)),
            new Recipe(1,new Vector3(-.08f,.47f,.07f),new Vector3(.76f,.38f,.66f),new Vector3(-.64f,.58f,-.34f),new Vector3(75,-55,-72)),
            new Recipe(1,new Vector3(.04f,.82f,-.04f),new Vector3(.55f,.28f,.70f),new Vector3(.12f,.92f,.26f),new Vector3(-115,32,68)),
            new Recipe(2,new Vector3(-.07f,.15f,.02f),new Vector3(.78f,.29f,.74f),new Vector3(-.22f,.38f,-.42f),new Vector3(48,65,-38)),
            new Recipe(2,new Vector3(.09f,.48f,-.05f),new Vector3(.71f,.38f,.68f),new Vector3(.28f,.85f,.32f),new Vector3(-72,-88,58)),
            new Recipe(2,new Vector3(-.03f,.83f,.05f),new Vector3(.58f,.29f,.63f),new Vector3(-.12f,.50f,-.28f),new Vector3(98,42,-95)),
            new Recipe(3,new Vector3(.08f,.17f,-.04f),new Vector3(.72f,.31f,.69f),new Vector3(.46f,.32f,.35f),new Vector3(-54,-74,66)),
            new Recipe(3,new Vector3(-.10f,.51f,.05f),new Vector3(.65f,.37f,.75f),new Vector3(.62f,.68f,-.26f),new Vector3(88,56,92)),
            new Recipe(3,new Vector3(.03f,.83f,-.02f),new Vector3(.54f,.26f,.65f),new Vector3(.20f,.90f,.42f),new Vector3(-105,-32,-78)),
            new Recipe(4,new Vector3(-.06f,.17f,.06f),new Vector3(.75f,.30f,.72f),new Vector3(.78f,.42f,-.20f),new Vector3(62,74,105)),
            new Recipe(4,new Vector3(.11f,.49f,-.05f),new Vector3(.68f,.38f,.67f),new Vector3(.50f,.72f,.28f),new Vector3(-86,-46,58)),
            new Recipe(4,new Vector3(-.04f,.82f,.03f),new Vector3(.53f,.28f,.70f),new Vector3(.94f,.28f,-.36f),new Vector3(118,38,88)),
        };

        private sealed class PieceState
        {
            public Transform Pivot;
            public Vector3 Start, Velocity, Spin;
            public Quaternion Rotation;
            public float Floor;
            public Material Material;
            public Color Color;
        }

        public static void Play(Transform wall)
        {
            if (wall == null) return;
            var slabs = new List<MeshFilter>();
            foreach (var mesh in wall.GetComponentsInChildren<MeshFilter>())
                if (mesh.sharedMesh != null && (mesh.name.StartsWith("IceArc_") || mesh.name.StartsWith("IcePillar_"))) slabs.Add(mesh);
            if (slabs.Count == 0) { CheskaIceVisuals.Shatter(wall.position,true); return; }
            var root = new GameObject("BarricadeThaw");
            root.transform.position = wall.position;
            var states = new List<PieceState>(15);
            for (int i=0;i<slabs.Count;i++)
            {
                int group = slabs.Count==5 ? i : slabs.Count==3 ? i*2 : i==0 ? 0 : 4;
                var slab=slabs[i]; var bounds=slab.sharedMesh.bounds;
                foreach (var recipe in Pieces)
                {
                    if (recipe.Slab!=group) continue;
                    var pivot=new GameObject("IceBreakPivot").transform; pivot.SetParent(root.transform,false);
                    var local=bounds.center+new Vector3(bounds.size.x*recipe.At.x,
                        bounds.size.y*(recipe.At.y-.5f),bounds.size.z*recipe.At.z);
                    var at=slab.transform.TransformPoint(local);
                    var chunk=CheskaIceVisuals.Piece(pivot,"IceBreakChunk","thaw_shard",new Color(.36f,.72f,.80f,.62f));
                    var shard=chunk.GetComponent<MeshFilter>().sharedMesh.bounds;
                    Vector3 size=Vector3.Scale(Vector3.Scale(bounds.size,slab.transform.lossyScale),recipe.Size);
                    Vector3 scale=new Vector3(size.x/Mathf.Max(.001f,shard.size.x),size.y/Mathf.Max(.001f,shard.size.y),size.z/Mathf.Max(.001f,shard.size.z));
                    chunk.transform.localScale=scale;
                    chunk.transform.localPosition=-Vector3.Scale(shard.center,scale);
                    var material=chunk.GetComponent<Renderer>().sharedMaterial;
                    states.Add(new PieceState { Pivot=pivot,Start=at,Rotation=slab.transform.rotation,
                        Velocity=wall.TransformDirection(recipe.Velocity),Spin=recipe.Spin,
                        Floor=VfxShapes.GroundPoint(at).y+.025f,Material=material,Color=material.color });
                }
            }
            var effect=root.AddComponent<CheskaWallBreak>(); effect._pieces=states.ToArray(); effect.StepTo(0);
        }

        private void Update()
        {
            StepTo(_age+Time.deltaTime);
            if (_age>=LifeSeconds) Destroy(gameObject);
        }

        public void StepTo(float seconds)
        {
            _age=Mathf.Max(0,seconds);
            if (_pieces==null) return;
            float thaw=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.52f,LifeSeconds,_age));
            foreach (var piece in _pieces)
            {
                if (piece.Pivot==null) continue;
                var at=piece.Start+piece.Velocity*_age+Vector3.down*(5*_age*_age);
                at.y=Mathf.Max(piece.Floor,at.y);
                piece.Pivot.position=at;
                piece.Pivot.rotation=piece.Rotation*Quaternion.Euler(piece.Spin*_age);
                piece.Pivot.localScale=Vector3.one*(1-thaw);
                var color=piece.Color; color.a*=Mathf.Clamp01((LifeSeconds-_age)/.16f);
                if (piece.Material!=null) piece.Material.color=color;
            }
        }
    }
}
