using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class PaeteAnimationTimingTests
    {
        private static T Read<T>(object instance,string name) => (T)instance.GetType()
            .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(instance);
        [UnityTest]
        public IEnumerator PlantPreparesBeforeLaunchAndKeepsItsAuthoredGeometry()
        {
            var root = new GameObject("Plant timing witness");
            try
            {
                var body = PaetePlantBody.Build(root.transform);
                yield return null; // Primitive collider stripping uses deferred Destroy.
                var pod = Read<Transform>(body,"_pod");
                var rest = Read<Quaternion>(body,"_podRest");
                var shoe = Read<Transform>(body,"_shoe");
                var meshes = body.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
                body.Pose(5,0,false,1f-.3f/TumbangPreso.Core.PaeteRules.PlantReloadSeconds,99);
                Assert.Less(Mathf.DeltaAngle(0,(Quaternion.Inverse(rest)*pod.localRotation).eulerAngles.x),-5,
                    "Shorter automatic reload must retain a readable pre-launch charge window.");
                body.Pose(5,0,false,1,99);
                float prepared = Mathf.DeltaAngle(0,(Quaternion.Inverse(rest)*pod.localRotation).eulerAngles.x);
                Assert.Less(prepared,-17,"Loaded pitcher must store its wind-up before the shot command.");
                Assert.IsTrue(shoe.gameObject.activeSelf,"The grown bakya must remain visible while charged.");
                body.Pose(5,0,false,0,.01f);
                float release = Mathf.DeltaAngle(0,(Quaternion.Inverse(rest)*pod.localRotation).eulerAngles.x);
                Assert.Greater(release,20,"The launch beat must already throw forward, not begin a late wind-up.");
                Assert.IsFalse(shoe.gameObject.activeSelf,"Loaded prop cannot duplicate the launched slipper.");
                body.Pose(5.8f,0,false,.04f,.8f);
                float recovered = Mathf.DeltaAngle(0,(Quaternion.Inverse(rest)*pod.localRotation).eulerAngles.x);
                Assert.Less(Mathf.Abs(recovered),7,"Pitcher must settle back into its existing idle.");
                CollectionAssert.AreEqual(meshes,body.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray());
                Assert.IsEmpty(body.GetComponentsInChildren<Collider>(true));
            }
            finally { Object.Destroy(root); }
            yield return null;
        }
    }
}
