using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    public sealed partial class ArenaFx
    {
        public enum RecordedFacing:byte { Fixed,Billboard,Upright,Stretch,Beam }
        [Serializable] public struct RecordedQuad
        {
            public Vector3 A,B,C,D;
            public Vector3 Eye,Right,Up;
            public RecordedFacing Facing;
            public byte Cell;
            public uint Colour;
        }
        private readonly RecordedFacing[] _recordedFacing=new RecordedFacing[Quads];
        private readonly Vector3[] _recordedEye=new Vector3[Quads],_recordedRight=new Vector3[Quads],_recordedUp=new Vector3[Quads];
        public Material RecordedMaterial=>_material;
        public event Action<float> FrameRendered;
        // Called after the existing LateUpdate mesh has been written. Detached
        // render data only: sampling does not touch pool age, random or events.
        public RecordedQuad[] CaptureRecordedQuads()
        {
            if(_renderer==null||!_renderer.enabled)return Array.Empty<RecordedQuad>();
            var result=new RecordedQuad[_drawn+_lastImmediate];int next=0;
            void Copy(int quad)
            {
                int v=quad*4;var colour=_colours[v];var uv=_uvs[v];
                result[next++]=new RecordedQuad{A=_vertices[v],B=_vertices[v+1],C=_vertices[v+2],D=_vertices[v+3],Facing=_recordedFacing[quad],Eye=_recordedEye[quad],Right=_recordedRight[quad],Up=_recordedUp[quad],
                    Cell=(byte)((3-Mathf.FloorToInt(uv.y*4))*4+Mathf.FloorToInt(uv.x*4)),
                    Colour=(uint)(colour.r|colour.g<<8|colour.b<<16|colour.a<<24)};
            }
            for(int q=0;q<_drawn;q++)Copy(q);
            for(int q=0;q<_lastImmediate;q++)Copy(MaxParticles+q);
            return result;
        }
    }
}
