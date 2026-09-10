using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CheskaIceContractProbe
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator FracturedWallBlocksItsVisibleFaceAndReleasesWithoutPhysicalDebris()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(20,.5f,20);
            Physics.SyncTransforms();
            var wall=HeroHazards.SpawnIceBarricade(Vector3.zero,Vector3.forward,6);
            Physics.SyncTransforms();
            var colliders=wall.GetComponentsInChildren<MeshCollider>();
            Assert.AreEqual(3,colliders.Length);
            foreach (float x in new[] {-.75f,0,.75f})
            {
                Assert.IsTrue(Physics.Raycast(new Vector3(x,1,-2),Vector3.forward,out var hit,4));
                Assert.IsTrue(hit.collider.transform.IsChildOf(wall.transform));
                Assert.AreSame(hit.collider.GetComponent<MeshFilter>().sharedMesh,((MeshCollider)hit.collider).sharedMesh);
            }
            Assert.IsFalse(Physics.Raycast(new Vector3(1.65f,1,-2),Vector3.forward,4),"The gap beyond the visible wall is blocked.");
            Assert.IsFalse(Physics.Raycast(new Vector3(0,2.9f,-2),Vector3.forward,out var overhead,4),"The space above the crown is blocked by " + overhead.collider?.name + " at " + overhead.point);
            Assert.IsEmpty(wall.GetComponentsInChildren<Light>());
            wall.GetComponent<HeroHazards.IceBarricadeComponent>().Shatter();
            Assert.IsTrue(colliders.All(c=>!c.enabled),"An expired wall blocks for an extra frame.");
            yield return null;
            Assert.IsTrue(wall==null);
            var thaw=GameObject.Find("BarricadeThaw");Assert.IsNotNull(thaw);
            Assert.IsEmpty(thaw.GetComponentsInChildren<Collider>());
            Assert.IsEmpty(thaw.GetComponentsInChildren<Rigidbody>());
            yield return new WaitForSeconds(.8f);
            Assert.IsTrue(thaw==null);
        }

        [UnityTest]
        public IEnumerator EscapingIceReleasesTheRestraintBeforeItsOriginalTimer()
        {
            var victim=new GameObject("Frozen player");
            var motor=victim.AddComponent<CharacterMotor>();
            motor.ApplyStagger(2.5f,StunElement.Ice,9);
            var prison=HeroHazards.SpawnIceCubePrison(victim.transform,2.5f);
            yield return null;
            Assert.IsNotNull(prison);
            Assert.IsEmpty(prison.GetComponentsInChildren<Collider>());
            var renderers=prison.GetComponentsInChildren<Renderer>();
            Assert.IsTrue(renderers.All(r=>r.bounds.max.y < 1.1f),"Ice hides the victim's face/eye line.");
            for (int i=0;i<12;i++)
            {
                motor.MashOutOfStun();
                yield return new WaitForSeconds(.12f);
            }
            Assert.LessOrEqual(motor.StunLeft,Balance.MinStunDown+.03f);
            yield return new WaitForSeconds(Balance.MinStunDown+.12f);
            yield return null;
            Assert.IsTrue(prison==null,"The restraint outlives the actual ice stun after escape.");
        }
    }
}
