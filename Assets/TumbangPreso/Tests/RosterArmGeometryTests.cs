using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.EditorTools;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class RosterArmGeometryTests
    {
        [Test]
        public void EveryBakedArmMatchesTheCurrentRosterGeometryAndColours()
        {
            foreach (var entry in RosterBook.Load().People)
                foreach (string side in new[] { "right","left" })
                {
                    Mesh expected=ViewmodelArmAuthor.Extract(entry.Model,"arm-"+side);
                    Mesh baked=Resources.Load<Mesh>("Models/RosterArms/"+entry.Id+"_"+side);
                    try
                    {
                        Assert.IsNotNull(baked,entry.Id+"/"+side);
                        CollectionAssert.AreEqual(expected.vertices,baked.vertices,entry.Id+" has a stale arm shape");
                        CollectionAssert.AreEqual(expected.uv,baked.uv,entry.Id+" has stale cloth/skin atlas coordinates");
                        CollectionAssert.AreEqual(expected.triangles,baked.triangles,entry.Id+" lost sleeve or hand geometry");
                    }
                    finally { Object.DestroyImmediate(expected); }
                }
        }

        [Test]
        public void EveryHeroUsesBothHandsAndReturnsCleanly()
        {
            foreach(string hero in new[]{"sean","zack","dante","cheska","nemu","phaister"})
            {
                var kit=HeroAbilitySystem.CreateKitFor(hero);
                foreach(var ability in new[]{kit.Skill1,kit.Skill2,kit.Ultimate})
                {
                    var go=new GameObject("Hand action review");
                    try
                    {
                        var arms=go.AddComponent<ViewmodelArms>();arms.EnsureBuilt();arms.SetCharacter(hero);
                        var right=go.transform.Find("RightPivot/Arm");var left=go.transform.Find("LeftPivot/Arm");
                        Assert.IsTrue(arms.PlayAction(ability.ViewmodelAction));
                        float rightTravel=0,leftTravel=0;
                        for(int frame=0;frame<120;frame++)
                        {
                            arms.StepVisuals(1f/60f);
                            rightTravel=Mathf.Max(rightTravel,Quaternion.Angle(Quaternion.identity,right.localRotation));
                            leftTravel=Mathf.Max(leftTravel,Quaternion.Angle(Quaternion.identity,left.localRotation));
                        }
                        Assert.Greater(rightTravel,5,ability.Id+" never moved the main hand");
                        Assert.Greater(leftTravel,5,ability.Id+" left its supporting hand idle");
                        Assert.Less(Quaternion.Angle(Quaternion.identity,right.localRotation),.01f,ability.Id);
                        Assert.Less(Quaternion.Angle(Quaternion.identity,left.localRotation),.01f,ability.Id);
                    }
                    finally { Object.DestroyImmediate(go); }
                }
            }
        }

        [Test]
        public void ARefusedActionRecoversBothHandsWithoutCancellingADifferentNewCast()
        {
            var go=new GameObject("Interrupted hands");
            try
            {
                var arms=go.AddComponent<ViewmodelArms>();arms.EnsureBuilt();arms.SetCharacter("phaister");
                arms.PlayAction("coven-eclipse");arms.StepVisuals(.12f);
                arms.CancelAction("cast-hex");arms.StepVisuals(.03f);
                Assert.Greater(Quaternion.Angle(Quaternion.identity,go.transform.Find("LeftPivot/Arm").localRotation),5);
                arms.CancelAction("coven-eclipse");arms.StepVisuals(.15f);
                Assert.Less(Quaternion.Angle(Quaternion.identity,go.transform.Find("LeftPivot/Arm").localRotation),.01f);
                Assert.Less(Quaternion.Angle(Quaternion.identity,go.transform.Find("RightPivot/Arm").localRotation),.01f);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
