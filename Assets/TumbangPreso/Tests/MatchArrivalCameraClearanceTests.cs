using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class MatchArrivalCameraClearanceTests
    {
        private readonly List<GameObject> _owned = new();
        private readonly Vector3 _focus = new(1234, 4, -987);
        private MatchArrivalPresentation _arrival;
        private static readonly MethodInfo ClearEye = typeof(MatchArrivalPresentation).GetMethod(
            "ClearEye", BindingFlags.Instance | BindingFlags.NonPublic);
        [SetUp] public void Before()
        {
            var root = Own("Dormant arrival clearance"); root.SetActive(false);
            _arrival = root.AddComponent<MatchArrivalPresentation>();
            Assert.IsNotNull(ClearEye);
        }
        [TearDown] public void After()
        {
            foreach (var root in _owned) if (root != null) Object.DestroyImmediate(root);
            _owned.Clear(); Physics.SyncTransforms();
        }
        private GameObject Own(string name)
        {
            var root = new GameObject(name); _owned.Add(root); return root;
        }
        private BoxCollider Wall(float distance, bool trigger = false)
        {
            var wall = Own("Arrival obstacle"); wall.transform.position = _focus + Vector3.forward * distance;
            var collider = wall.AddComponent<BoxCollider>();
            collider.size = new Vector3(2, 2, .1f); collider.isTrigger = trigger;
            Physics.SyncTransforms(); return collider;
        }
        private Vector3 Eye(float distance = 5)
            => (Vector3)ClearEye.Invoke(_arrival, new object[] { _focus, _focus + Vector3.forward * distance });
        private void VerifyCaptured(BoxCollider wall)
        {
            Assert.IsTrue(Physics.SphereCast(_focus, .2f, Vector3.forward, out var hit, 5,
                ~0, QueryTriggerInteraction.Ignore));
            Assert.AreSame(wall, hit.collider, "The intended wall must be the native collision hit.");
        }
        [Test] public void OpeningRestPoseCannotCaptureTheReplicaInterpolationOffset()
        {
            var body=Own("Replica with a lagged visual").AddComponent<CharacterMotor>();
            body.transform.position=_focus;
            var visual=body.gameObject.AddComponent<TumbangPreso.Visual.CharacterVisual>();
            var model=Own("Authored visual root").transform;model.SetParent(body.transform,false);
            visual.SetModelRoot(model);model.localPosition=new Vector3(0,3,0);
            var filmRoot=Own("Dormant rest capture film");filmRoot.SetActive(false);
            var film=filmRoot.AddComponent<TumbangPreso.Map.TondoFilm>();
            var type=typeof(TumbangPreso.Map.MapFilm);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            try
            {
                type.GetMethod("TakeModels",flags).Invoke(film,new object[]{new[]{body}});
                var rest=(Vector3[])type.GetField("_rest",flags).GetValue(film);
                Assert.AreEqual(0,rest[0].y,.001f,"A remote mesh lag cannot become the film's permanent rest height.");
                Assert.AreEqual(_focus,body.transform.position,"Preparing the picture cannot move its physics body.");
            }
            finally{type.GetField("_posing",flags).SetValue(film,false);}
        }

        [Test] public void AFilmThatKeepsGameLightDoesNotRestoreAnOldWorldTint()
        {
            var filmRoot=Own("Dormant lighting ownership film");filmRoot.SetActive(false);
            var film=filmRoot.AddComponent<TumbangPreso.Map.TondoFilm>();
            var type=typeof(TumbangPreso.Map.MapFilm);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            Color original=RenderSettings.ambientSkyColor;
            try
            {
                RenderSettings.ambientSkyColor=Color.red;
                type.GetMethod("SnapshotLight",flags).Invoke(film,null);
                RenderSettings.ambientSkyColor=Color.green;
                type.GetMethod("RestoreLight",flags).Invoke(film,null);
                Assert.AreEqual(Color.green,RenderSettings.ambientSkyColor,"Unchanged cinematic lighting cannot overwrite the current game look.");
            }
            finally{RenderSettings.ambientSkyColor=original;}
        }

        [Test] public void TemporaryFilmLightsFadeWithoutCompoundingAtTheHandoff()
        {
            var filmRoot=Own("Dormant lamp handoff film");filmRoot.SetActive(false);
            var film=filmRoot.AddComponent<TumbangPreso.Map.TondoFilm>();
            var light=Own("Temporary film lamp").AddComponent<Light>();light.intensity=4;
            var type=typeof(TumbangPreso.Map.MapFilm);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            ((List<Light>)type.GetField("_filmLights",flags).GetValue(film)).Add(light);
            ((List<float>)type.GetField("_filmLightLevels",flags).GetValue(film)).Add(float.NaN);
            type.GetProperty("Timeline").SetValue(film,new TumbangPreso.Map.MapFilm.Times{Handoff=2,End=4});
            var fade=type.GetMethod("FadeFilmLights",flags);var restore=type.GetMethod("RestoreFilmLightLevels",flags);
            fade.Invoke(film,new object[]{3f});Assert.AreEqual(2,light.intensity,.001f);
            restore.Invoke(film,null);Assert.AreEqual(4,light.intensity,.001f);
            fade.Invoke(film,new object[]{3f});Assert.AreEqual(2,light.intensity,.001f);
            restore.Invoke(film,null);fade.Invoke(film,new object[]{4f});Assert.AreEqual(0,light.intensity,.001f);
        }

        [Test] public void NetworkParentCorrectionCannotLiftTheOpeningCastBeforeRender()
        {
            var body=Own("Replicated body").transform;body.position=_focus;
            var model=Own("Authored film root").transform;model.SetParent(body,false);
            model.localPosition=new Vector3(1,.2f,2);model.localRotation=Quaternion.Euler(0,35,0);
            var filmRoot=Own("Dormant client film");filmRoot.SetActive(false);
            var film=filmRoot.AddComponent<TumbangPreso.Map.TondoFilm>();
            var camera=Own("Client opening camera").AddComponent<Camera>();camera.enabled=false;
            var type=typeof(TumbangPreso.Map.MapFilm);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            ((Transform[])type.GetField("_root",flags).GetValue(film))[0]=model;
            type.GetField("_posing",flags).SetValue(film,true);type.GetField("_prepared",flags).SetValue(film,true);
            type.GetProperty("Camera",flags).SetValue(film,camera);
            Vector3 authored=model.position;Quaternion turn=model.rotation;
            try
            {
                type.GetMethod("RememberCastPose",flags).Invoke(film,null);
                body.position+=new Vector3(2,3,1);body.rotation=Quaternion.Euler(0,70,0);
                Vector3 authoritative=body.position;
                type.GetMethod("PinCastBeforeSrpRender",flags).Invoke(film,new object[]{default(UnityEngine.Rendering.ScriptableRenderContext),camera});
                Assert.Less(Vector3.Distance(authored,model.position),.001f);
                Assert.Less(Quaternion.Angle(turn,model.rotation),.001f);
                Assert.AreEqual(authoritative,body.position,"The film must never overwrite the networked physics body.");
            }
            finally{type.GetField("_prepared",flags).SetValue(film,false);type.GetField("_posing",flags).SetValue(film,false);}
        }

        [Test] public void NemuVictimPathUsesItsOwnFloorOnUnevenTerrain()
        {
            var ground=Own("Low floor").AddComponent<BoxCollider>();ground.transform.position=new Vector3(_focus.x,-.1f,_focus.z);ground.size=new Vector3(30,.2f,20);
            var raised=Own("Raised floor").AddComponent<BoxCollider>();raised.transform.position=new Vector3(_focus.x+5,2.9f,_focus.z);raised.size=new Vector3(4,.2f,4);
            var stage=Own("Nemu stage on upper terrace").transform;stage.position=_focus;
            Physics.SyncTransforms();
            var project=typeof(TumbangPreso.Visual.HeroIntroductionScene).GetMethod("NemuTerrainPoint",BindingFlags.Static|BindingFlags.NonPublic);
            var high=(Vector3)project.Invoke(null,new object[]{stage,new Vector3(5,-.7f,0)});
            var low=(Vector3)project.Invoke(null,new object[]{stage,new Vector3(-5,-3.7f,0)});
            Assert.AreEqual(3,stage.TransformPoint(high).y,.001f);
            Assert.AreEqual(0,stage.TransformPoint(low).y,.001f);
            Assert.AreEqual(5,high.x,.001f);Assert.AreEqual(-5,low.x,.001f);
        }

        [TestCase(.55f)] [TestCase(.95f)]
        public void ACloseWallCannotLeaveTheCameraBeyondItsCollisionClearance(float distance)
        {
            var wall = Wall(distance); VerifyCaptured(wall);
            Vector3 eye = Eye();
            Assert.IsFalse(Physics.Linecast(_focus, eye, out _, ~0, QueryTriggerInteraction.Ignore),
                "The chosen eye leaves an opaque wall between the camera and its focus.");
            Assert.GreaterOrEqual(Vector3.Distance(eye, wall.ClosestPoint(eye)), .2f,
                "The chosen eye still overlaps the camera's own collision sphere.");
            Assert.Greater(Vector3.Distance(eye, _focus), .001f, "LookRotation must retain a nonzero viewing vector.");
        }
        [Test] public void ARemoteWallStillShortensTheEyeBeforeTheWall()
        {
            var wall = Wall(3); VerifyCaptured(wall); Vector3 eye = Eye();
            Assert.Greater(Vector3.Distance(_focus, eye), 1);
            Assert.Less(Vector3.Distance(_focus, eye), 3);
            Assert.IsFalse(Physics.Linecast(_focus, eye, out _, ~0, QueryTriggerInteraction.Ignore));
            Assert.GreaterOrEqual(Vector3.Distance(eye, wall.ClosestPoint(eye)), .2f);
        }
        [Test] public void AnUnobstructedShotKeepsTheDesiredEye()
            => Assert.Less(Vector3.Distance(Eye(), _focus + Vector3.forward * 5), .001f);
        [Test] public void AnObstacleBehindTheFocusCannotShortenTheShot()
        {
            Wall(-1); Assert.Less(Vector3.Distance(Eye(), _focus + Vector3.forward * 5), .001f);
        }
        [Test] public void ATriggerInFrontOfTheFocusDoesNotOccludeTheShot()
        {
            var wall = Wall(.55f, true);
            Assert.IsTrue(Physics.Linecast(_focus, _focus + Vector3.forward * 5, out var hit,
                ~0, QueryTriggerInteraction.Collide));
            Assert.AreSame(wall, hit.collider);
            Assert.Less(Vector3.Distance(Eye(), _focus + Vector3.forward * 5), .001f);
        }
    }
}
