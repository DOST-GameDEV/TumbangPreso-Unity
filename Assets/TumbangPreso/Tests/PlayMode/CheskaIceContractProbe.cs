using System.Collections;
using System.Linq;
using System.IO;
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
        private INetProvider _provider;
        private sealed class PredictingOwner : INetProvider
        {
            public bool IsHost=>false;
            public bool IsNetworked=>true;
            public int LocalSlot=>1;
            public int LocalPeerId=>1;
            public bool IsSeatlessReferee=>false;
        }
        [UnitySetUp] public IEnumerator Before()
        {
            _provider=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider=_provider;
        }

        [UnityTest]
        public IEnumerator RefusedInstantIceCastsRemoveTheirPredictedWorldEffects()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(20,.5f,20);
            var owner=new GameObject("Predicted ice caster");
            var motor=owner.AddComponent<CharacterMotor>();motor.PlayerSlot=1;
            var kit=new CheskaHeroKit();
            NetAuthority.Provider=new PredictingOwner();
            var context=new AbilityContext(motor,null,null,Vector3.zero,Vector3.forward,new Vector3(0,0,3));
            var left=new int[2];
            for(int slot=0;slot<2;slot++)
            {
                var ability=slot==0?kit.Skill1:kit.Skill2;
                // Exercise the activation/refusal callbacks for an already
                // predicted cast. This isolated check is not a packet test.
                ability.Activate(context);
                yield return null;
                GameObject effect=slot==0
                    ? Object.FindFirstObjectByType<HeroHazards.IceSheetComponent>()?.gameObject
                    : Object.FindFirstObjectByType<HeroHazards.IceBarricadeComponent>()?.gameObject;
                Assert.Less(ability.ChargesRemaining,ability.MaxCharges,"The predicted cast did not spend its local charge.");
                Assert.Zero(ability.DurationRemaining,"This regression covers instant skills with separately timed world effects.");
                ability.RollBackPredictedCast(context);
                yield return null;yield return null;
                left[slot]=effect!=null && effect.activeInHierarchy?1:0;
                if(effect!=null)Object.Destroy(effect);
                yield return null;
            }
            File.WriteAllText("Logs/cheska-denied-ice.csv",$"effect,remaining_after_refusal\nsheet,{left[0]}\nbarricade,{left[1]}\n");
            Assert.That(left,Is.All.Zero,"A refused instant cast left its timed ice effect in the world.");
        }

        [UnityTest]
        public IEnumerator IcePlacementUsesItsCapturedPoseAfterTheCasterMoves()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(40,.5f,40);
            var owner=new GameObject("Moved ice caster");
            var motor=owner.AddComponent<CharacterMotor>();motor.PlayerSlot=1;
            var system=owner.AddComponent<HeroAbilitySystem>();system.BindHero("cheska");
            owner.transform.SetPositionAndRotation(new Vector3(8,.12f,-8),Quaternion.Euler(0,180,0));
            var context=new AbilityContext(motor,null,null,Vector3.zero,Vector3.forward,new Vector3(0,0,5));
            var skill=system.Kit.Skill1;
            system.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill1,context.Position,
                context.Forward,context.AimPoint,.55f,authoritative:false);
            yield return null;
            var sheet=Object.FindFirstObjectByType<HeroHazards.IceSheetComponent>();Assert.IsNotNull(sheet);
            var expected=context.Position+context.Forward*skill.AimRangeFor(.55f);
            Assert.Less(Vector3.ProjectOnPlane(sheet.transform.position-expected,Vector3.up).magnitude,.02f,
                "Ice followed the live caster instead of the accepted cast pose.");
        }

        [UnityTest]
        public IEnumerator ConfirmedIceDoesNotSpendAgainOrDisappearWithALaterRefusal()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(40,.5f,40);
            var owner=new GameObject("Confirming ice caster");
            var motor=owner.AddComponent<CharacterMotor>();motor.PlayerSlot=1;
            var system=owner.AddComponent<HeroAbilitySystem>();system.BindHero("cheska");
            NetAuthority.Provider=new PredictingOwner();
            var skill=system.Kit.Skill1;
            var first=new AbilityContext(motor,null,null,Vector3.zero,Vector3.forward,new Vector3(0,0,5));
            skill.Activate(first);yield return null;
            Assert.IsNull(Object.FindFirstObjectByType<HeroHazards.IceSheetComponent>(),
                "Prediction created a world field before host acceptance.");
            int spent=skill.ChargesRemaining;
            owner.transform.SetPositionAndRotation(new Vector3(8,.12f,-8),Quaternion.Euler(0,180,0));
            system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1,
                first.Position,first.Forward,first.AimPoint,.55f);
            yield return null;
            var accepted=Object.FindFirstObjectByType<HeroHazards.IceSheetComponent>();Assert.IsNotNull(accepted);
            Assert.AreEqual(spent,skill.ChargesRemaining,"Confirmation spent a second charge.");
            Assert.Less(Vector3.ProjectOnPlane(accepted.transform.position-Vector3.forward*skill.AimRangeFor(.55f),Vector3.up).magnitude,.02f);
            var later=new AbilityContext(motor,null,null,Vector3.zero,Vector3.right,new Vector3(5,0,0));
            skill.Activate(later);skill.RollBackPredictedCast(later);
            yield return null;yield return null;
            Assert.IsTrue(accepted!=null && accepted.gameObject.activeInHierarchy,
                "Refusing a later request removed an earlier accepted sheet.");
            Assert.AreEqual(1,Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None).Length);
            skill.Activate(later);
            system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1,
                later.Position,later.Forward,later.AimPoint,.55f);
            yield return null;
            Assert.AreEqual(2,Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None).Length,
                "Two accepted casts must retain their independent fields.");
            Assert.Zero(skill.ChargesRemaining);
        }

        [UnityTest]
        public IEnumerator ConfirmedBarricadeGetsPhysicalFacesWithoutAnotherCharge()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(40,.5f,40);
            var owner=new GameObject("Confirming wall caster");
            var motor=owner.AddComponent<CharacterMotor>();motor.PlayerSlot=1;
            var system=owner.AddComponent<HeroAbilitySystem>();system.BindHero("cheska");
            NetAuthority.Provider=new PredictingOwner();
            var skill=system.Kit.Skill2;
            var pose=new AbilityContext(motor,null,null,Vector3.zero,Vector3.forward,new Vector3(0,0,4));
            skill.Activate(pose);yield return null;
            Assert.IsNull(Object.FindFirstObjectByType<HeroHazards.IceBarricadeComponent>());
            system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill2,
                pose.Position,pose.Forward,pose.AimPoint,.55f);
            yield return null;Physics.SyncTransforms();
            var wall=Object.FindFirstObjectByType<HeroHazards.IceBarricadeComponent>();Assert.IsNotNull(wall);
            Assert.Zero(skill.ChargesRemaining);
            Assert.IsTrue(Physics.Raycast(new Vector3(0,1,2),Vector3.forward,out var hit,4));
            Assert.IsTrue(hit.collider.transform.IsChildOf(wall.transform));
        }

        [UnityTest]
        public IEnumerator DelayedLightningAlsoUsesItsAcceptedPose()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(40,.5f,40);
            var owner=new GameObject("Turning lightning caster");
            var motor=owner.AddComponent<CharacterMotor>();motor.PlayerSlot=1;
            var system=owner.AddComponent<HeroAbilitySystem>();system.BindHero("zack");
            system.ApplyNetworkCast(HeroAbilitySystem.Slot.Ultimate,Vector3.zero,
                Vector3.forward,Vector3.forward*7,.55f,authoritative:false);
            owner.transform.SetPositionAndRotation(new Vector3(8,.12f,-8),Quaternion.Euler(0,180,0));
            var ability=system.Kit.Ultimate;
            ability.Tick(new AbilityContext(motor,null,null),ability.Windup+.01f);
            yield return null;
            var bolts=Object.FindObjectsByType<DirectedLightningBolt>(FindObjectsSortMode.None);
            Assert.AreEqual(3,bolts.Length);
            Vector3 expected=Vector3.forward*ability.AimRangeFor(.55f);
            Assert.Less(bolts.Min(bolt=>Vector3.ProjectOnPlane(bolt.transform.position-expected,Vector3.up).magnitude),.02f,
                "The delayed lightning landed relative to a later pose.");
        }

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
