using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterNameplate
    {
        private static Mesh GroundMarkerMesh(string name)
        {
            const float edge=1.12f;
            var mesh=new Mesh{name=name,hideFlags=HideFlags.HideAndDontSave};
            mesh.vertices=new[]{new Vector3(-edge,0,-edge),new Vector3(-edge,0,edge),new Vector3(edge,0,edge),new Vector3(edge,0,-edge)};
            mesh.uv=new[]{new Vector2(-edge,-edge),new Vector2(-edge,edge),new Vector2(edge,edge),new Vector2(edge,-edge)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();return mesh;
        }
    }
}
