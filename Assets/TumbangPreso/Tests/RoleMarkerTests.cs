using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class RoleMarkerTests
    {
        [Test]
        public void GroundMarkerHasNoRaisedSideWalls()
        {
            var mesh=(Mesh)typeof(CharacterNameplate).GetMethod("GroundMarkerMesh",BindingFlags.NonPublic|BindingFlags.Static)
                .Invoke(null,new object[]{"Flat marker witness"});
            try
            {
                Assert.AreEqual(4,mesh.vertexCount);
                foreach(var vertex in mesh.vertices)Assert.AreEqual(0,vertex.y);
                Assert.AreEqual(6,mesh.triangles.Length);
                Assert.Greater(mesh.uv[2].x,1,"Include the soft outer edge in the shader footprint.");
                Assert.IsNotNull(Shader.Find("TumbangPreso/PlayerGroundMarker"));
            }
            finally{Object.DestroyImmediate(mesh);}
        }
    }
}
