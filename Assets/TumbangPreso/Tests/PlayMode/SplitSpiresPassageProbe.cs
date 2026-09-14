using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SplitSpiresPassageProbe
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator SplitSpiresLeavesItsPromisedBodyAndSlipperPassage()
        {
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Ground";
            ground.transform.position=new Vector3(0,-.25f,0);ground.transform.localScale=new Vector3(30,.5f,30);
            var owner=new GameObject("Barricade variant caster");
            var motor=owner.AddComponent<CharacterMotor>();motor.PlayerSlot=1;
            var system=owner.AddComponent<HeroAbilitySystem>();
            int[] slabs=new int[2];bool[] bodyBlocked=new bool[2],slipperBlocked=new bool[2];
            for(int variant=0;variant<2;variant++)
            {
                system.BindHero("cheska",new HeroBuild{HeroId="cheska",
                    Slot2VariantId=variant==0?"cheska.2.barricade":"cheska.2.spires"});
                Assert.AreEqual(variant==1,system.HasVariant("cheska.2.spires"));
                system.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill2,Vector3.zero,
                    Vector3.forward,new Vector3(0,0,4),.55f,authoritative:false);
                yield return null;Physics.SyncTransforms();
                var wall=Object.FindFirstObjectByType<HeroHazards.IceBarricadeComponent>();Assert.IsNotNull(wall);
                slabs[variant]=wall.GetComponentsInChildren<MeshCollider>().Length;
                float side=variant==0?.75f:1.05f;
                foreach(float x in new[]{-side,side})
                {
                    Assert.IsTrue(Physics.Raycast(new Vector3(x,1,2),Vector3.forward,out var face,4));
                    Assert.IsTrue(face.collider.transform.IsChildOf(wall.transform),"The side slab no longer blocks its drawn face.");
                }
                // Match the shipped body's .35m capsule radius. The low sweep
                // also checks the slipper route, not merely a ray above its base.
                bodyBlocked[variant]=Physics.CapsuleCast(new Vector3(0,.4f,2),new Vector3(0,1.2f,2),.35f,
                    Vector3.forward,out var body,4) && body.collider.transform.IsChildOf(wall.transform);
                slipperBlocked[variant]=Physics.SphereCast(new Vector3(0,.18f,2),.08f,Vector3.forward,out var shoe,4)
                    && shoe.collider.transform.IsChildOf(wall.transform);
                Object.Destroy(wall.gameObject);yield return null;
            }
            File.WriteAllText("Logs/split-spires-passage.csv",
                $"variant,slabs,body_blocked,slipper_blocked\nbarricade,{slabs[0]},{bodyBlocked[0]},{slipperBlocked[0]}\nspires,{slabs[1]},{bodyBlocked[1]},{slipperBlocked[1]}\n");
            Assert.AreEqual(3,slabs[0]);Assert.IsTrue(bodyBlocked[0]);Assert.IsTrue(slipperBlocked[0]);
            Assert.AreEqual(2,slabs[1],"Split Spires still constructs the default centre slab.");
            Assert.IsFalse(bodyBlocked[1],"The advertised passage cannot fit a player.");
            Assert.IsFalse(slipperBlocked[1],"The advertised passage blocks a low slipper.");
        }
    }
}
