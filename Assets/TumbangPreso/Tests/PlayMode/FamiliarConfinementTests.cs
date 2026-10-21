using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
namespace TumbangPreso.PlayTests
{
    public sealed class FamiliarConfinementTests
    {
        private GameObject root; private CharacterMotor owner;
        private bool round; private float radius; private Vector4 bounds;
        [UnitySetUp] public IEnumerator Before()
        {
            round=Confinement.Round;radius=Confinement.Radius;
            bounds=new Vector4(AIController.PlayableMinX,AIController.PlayableMaxX,AIController.PlayableMinZ,AIController.PlayableMaxZ);
            yield return PlayModeWorld.Reset();
            root=new GameObject("Familiar confinement owner");owner=root.AddComponent<CharacterMotor>();owner.enabled=false;owner.IsDefender=true;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if(root!=null)Object.Destroy(root);yield return PlayModeWorld.Reset();
            Confinement.Use(round,radius);
            AIController.PlayableMinX=bounds.x;AIController.PlayableMaxX=bounds.y;AIController.PlayableMinZ=bounds.z;AIController.PlayableMaxZ=bounds.w;
        }
        [TestCase("plaza")][TestCase("tore")][TestCase("krus")][TestCase("hukay")][TestCase("entablado")]
        public void DefenderDiagonalUsesTheCurrentLayoutsRoundBoundary(string layout)
        {
            float r=ArenaStage.BoxRadiusFor(layout);Confinement.Use(true,r);
            var point=GhostPetMotion.ClampToCourt(owner,new Vector3(r*.9f,4.75f,r*.9f));
            Assert.LessOrEqual(new Vector2(point.x,point.z).magnitude,r+.0001f);
            Assert.AreEqual(4.75f,point.y);Assert.AreEqual(point.x,point.z,.0001f);
        }
        [Test] public void OrdinarySquareKeepsItsCorner()
        {
            Confinement.Use(false,7);Assert.AreEqual(new Vector3(7,2,7),GhostPetMotion.ClampToCourt(owner,new Vector3(9,2,9)));
        }
        [Test] public void RoundInteriorKeepsItsPositionAndHeight()
        {
            Confinement.Use(true,9.3f);var point=new Vector3(2,3,4);Assert.AreEqual(point,GhostPetMotion.ClampToCourt(owner,point));
        }
        [TestCase(false)][TestCase(true)] public void NonDefenderBranchesKeepPerSideCourtBounds(bool noOwner)
        {
            Confinement.Use(true,9.3f);owner.IsDefender=false;
            AIController.PlayableMinX=-5;AIController.PlayableMaxX=9;AIController.PlayableMinZ=-8;AIController.PlayableMaxZ=15;
            Assert.AreEqual(new Vector3(9,8,-8),GhostPetMotion.ClampToCourt(noOwner?null:owner,new Vector3(20,8,-20)));
        }
    }
}
