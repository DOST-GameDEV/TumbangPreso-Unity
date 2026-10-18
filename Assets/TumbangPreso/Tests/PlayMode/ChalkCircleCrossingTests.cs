using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEngine;
namespace TumbangPreso.PlayTests
{
    public sealed class ChalkCircleCrossingTests
    {
        private bool round;private float radius;
        [SetUp]public void Before(){round=Confinement.Round;radius=Confinement.Radius;}
        [TearDown]public void After(){Confinement.Use(round,radius);}
        [TestCase("plaza")][TestCase("tore")][TestCase("krus")][TestCase("hukay")][TestCase("entablado")]
        public void DiagonalCrossingMatchesTheCurrentCircleAndInterpolatesHeight(string layout)
        {
            float r=ArenaStage.BoxRadiusFor(layout);Confinement.Use(true,r);float k=1/Mathf.Sqrt(2);
            Assert.IsTrue(MotionFoley.TryChalkCrossing(new Vector3((r-.2f)*k,2,(r-.2f)*k),new Vector3((r+.2f)*k,4,(r+.2f)*k),out var at));
            Assert.AreEqual(r,new Vector2(at.x,at.z).magnitude,.001f);Assert.AreEqual(3,at.y,.001f);
        }
        [TestCase("plaza")][TestCase("tore")][TestCase("krus")][TestCase("hukay")][TestCase("entablado")]
        public void CrossingTheOldSquareCornerDoesNotInventACircleContact(string layout)
        {
            float r=ArenaStage.BoxRadiusFor(layout);Confinement.Use(true,r);
            Assert.IsFalse(MotionFoley.TryChalkCrossing(new Vector3(r-.2f,0,r-.2f),new Vector3(r+.2f,0,r-.2f),out _));
        }
        [Test]public void AxisCrossingStillContactsTheLine(){Confinement.Use(true,7);Assert.IsTrue(MotionFoley.TryChalkCrossing(new Vector3(6.8f,0,0),new Vector3(7.2f,0,0),out var at));Assert.AreEqual(7,at.x,.001f);}
        [Test]public void StationaryDoesNotCross(){Confinement.Use(true,7);Assert.IsFalse(MotionFoley.TryChalkCrossing(Vector3.zero,Vector3.zero,out _));}
        [Test]public void InteriorDoesNotCross(){Confinement.Use(true,7);Assert.IsFalse(MotionFoley.TryChalkCrossing(Vector3.zero,Vector3.right,out _));}
        [Test]public void PassingThroughUsesFirstContact(){Confinement.Use(true,7);Assert.IsTrue(MotionFoley.TryChalkCrossing(new Vector3(-8,2,0),new Vector3(8,6,0),out var at));Assert.AreEqual(-7,at.x,.001f);Assert.AreEqual(2.25f,at.y,.001f);}
        [Test]public void TangentDoesNotCross(){Confinement.Use(true,7);Assert.IsFalse(MotionFoley.TryChalkCrossing(new Vector3(-1,0,7),new Vector3(1,0,7),out _));}
        [Test]public void StartingOnLineDoesNotRepeatContact(){Confinement.Use(true,7);Assert.IsFalse(MotionFoley.TryChalkCrossing(new Vector3(7,0,0),new Vector3(8,0,0),out _));}
        [Test]public void ShortCrossingRemainsStable(){Confinement.Use(true,7);Assert.IsTrue(MotionFoley.TryChalkCrossing(new Vector3(6.99f,0,0),new Vector3(7.01f,0,0),out var at));Assert.AreEqual(7,at.x,.0001f);}
        [Test]public void SquareCornerRemainsASquare(){Confinement.Use(false,7);Assert.IsTrue(MotionFoley.TryChalkCrossing(new Vector3(6.8f,0,6.8f),new Vector3(7.2f,0,6.8f),out var at));Assert.AreEqual(7,at.x,.001f);}
        [Test]public void SquareEdgeGapRemainsRejected(){Confinement.Use(false,7);Assert.IsFalse(MotionFoley.TryChalkCrossing(new Vector3(6.8f,0,9),new Vector3(7.2f,0,9),out _));}
    }
}
