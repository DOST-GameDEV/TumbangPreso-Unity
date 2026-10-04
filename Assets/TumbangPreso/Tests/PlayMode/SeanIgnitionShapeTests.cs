using System.Collections;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SeanIgnitionShapeTests
    {
        [UnityTest]
        public IEnumerator RecordedEmberKeepsReadableShoeScaleAndAgeOnlyMotion()
        {
            var shoe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var filter = shoe.GetComponent<MeshFilter>();
            var mesh = Object.Instantiate(filter.sharedMesh);
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = Vector3.Scale(vertices[i], new Vector3(.46f,.04f,.18f));
            mesh.vertices = vertices; mesh.RecalculateBounds(); filter.sharedMesh = mesh;
            try
            {
                var visual = SeanIgnitionVisual.Recorded(filter);
                Assert.IsFalse(visual.enabled, "Recorded view must not run live kit ownership logic.");
                var renderers = visual.GetComponentsInChildren<MeshRenderer>();
                Assert.AreEqual(3, renderers.Length);
                Assert.IsEmpty(visual.GetComponentsInChildren<Collider>());
                Assert.IsEmpty(visual.GetComponentsInChildren<ParticleSystem>());
                visual.StepTo(.5f);
                var toe = visual.transform.Find("KindledFlame_0");
                float width = toe.GetComponent<MeshFilter>().sharedMesh.bounds.size.x * toe.localScale.x;
                Assert.That(width, Is.InRange(.07f,.13f), "Toe flame must read as a compact tongue, not a detached needle or oversized fireball.");
                Assert.That(toe.localPosition.y, Is.InRange(0,.02f), "Ember base must sit on the thin shoe surface.");
                Vector3 scale = toe.localScale; Quaternion rotation = toe.localRotation;
                var sameMesh = toe.GetComponent<MeshFilter>().sharedMesh;
                visual.StepTo(4); yield return null; visual.StepTo(.5f);
                Assert.Less(Vector3.Distance(scale,toe.localScale), .000001f);
                Assert.Less(Quaternion.Angle(rotation,toe.localRotation), .001f);
                Assert.AreSame(sameMesh,toe.GetComponent<MeshFilter>().sharedMesh);
                visual.StepTo(0);
                Assert.Less(toe.localScale.sqrMagnitude,.000001f, "Initial kindle begins at zero without new geometry.");
            }
            finally { Object.Destroy(shoe); Object.Destroy(mesh); }
            yield return null;
        }
    }
}
