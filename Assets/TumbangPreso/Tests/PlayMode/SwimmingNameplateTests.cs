using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SwimmingNameplateTests
    {
        private int _idleDelay;
        [UnitySetUp] public IEnumerator Before()
        {
#if UNITY_EDITOR
            _idleDelay=UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds;
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=1;
#endif
            yield return PlayModeWorld.Reset();
            // Let the Editor retire completed import workers before loading the map.
            yield return new WaitForSecondsRealtime(1);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
#if UNITY_EDITOR
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=_idleDelay;
#endif
        }
        [UnityTest]
        public IEnumerator HollowGlowPreservesCircleAndOpenTayaSilhouettes()
        {
            SceneFlow.Networked=false;SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            yield return SceneManager.LoadSceneAsync(SceneFlow.BayanPlaza);
            yield return new WaitForSecondsRealtime(.4f);
            var actor=Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).First(m=>m.PlayerSlot==2);
            var plate=actor.GetComponentInChildren<CharacterNameplate>();plate.enabled=false;
            var ring=plate.transform.Find("NameplateRing");ring.gameObject.SetActive(true);
            var renderer=ring.GetComponent<Renderer>();var mesh=ring.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual("TumbangPreso/PlayerGroundMarker",renderer.sharedMaterial.shader.name);
            Assert.IsEmpty(ring.GetComponents<Collider>());
            Assert.AreNotEqual(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly,renderer.shadowCastingMode,"Use an observer-visible seat, not the local FPP-hidden body.");
            foreach(var vertex in mesh.vertices)Assert.AreEqual(0,vertex.y);
            Assert.LessOrEqual(CharacterNameplate.RingFloorMargin,.006f);
            int oldLayer=ring.gameObject.layer;ring.gameObject.layer=31;
            bool wasDefense=actor.IsDefender;
            var camera=new GameObject("Ground marker isolated witness").AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=1.25f;camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var rt=new RenderTexture(256,256,24);camera.targetTexture=rt;
            var image=new Texture2D(256,256,TextureFormat.RGB24,false);
            string output=System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/ground-markers";
            System.IO.Directory.CreateDirectory(output);
            try
            {
                for(int role=0;role<2;role++)
                {
                    actor.IsDefender=role==1;plate.Refresh();
                    var capsule=actor.GetComponent<CharacterController>();
                    float expectedRadius=capsule.radius*(role==0?1.75f:1.95f);
                    Assert.AreEqual(expectedRadius,ring.localScale.x,.001f,"Role ring follows the requested capsule-relative radius.");
                    Assert.AreEqual(expectedRadius,ring.localScale.z,.001f);
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);Assert.AreEqual(role,block.GetFloat("_Shape"));
                    camera.transform.position=ring.position+Vector3.up*3;camera.transform.rotation=Quaternion.Euler(90,0,0);
                    camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
                    image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();RenderTexture.active=old;
                    float centre=image.GetPixel(128,128).grayscale;
                    Assert.Less(centre,.005f,"Both roles now have the requested hollow centre.");
                    Assert.Greater(image.GetPixels().Max(c=>c.grayscale),.04f,"Marker must actually render.");
                    System.IO.File.WriteAllBytes(output+(role==0?"/attacker.png":"/defender.png"),image.EncodeToPNG());
                }
            }
            finally
            {
                actor.IsDefender=wasDefense;plate.Refresh();ring.gameObject.layer=oldLayer;plate.enabled=true;
                camera.targetTexture=null;Object.Destroy(camera.gameObject);rt.Release();Object.Destroy(rt);Object.Destroy(image);
            }
        }
        [UnityTest]
        public IEnumerator RoleMarkerFollowsWaterAndReturnsToTheCapsuleFloor()
        {
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            SceneFlow.Networked = false;
            yield return SceneManager.LoadSceneAsync("SaBubong");
            yield return new WaitForSecondsRealtime(.4f);
            var actor = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).First(m => m.PlayerSlot == 1);
            var brain = actor.GetComponent<AIController>(); if (brain != null) brain.enabled = false;
            actor.enabled = false;
            var capsule = actor.GetComponent<CharacterController>(); capsule.enabled = false;
            var plate = actor.GetComponentInChildren<CharacterNameplate>();
            var ring = plate.transform.Find("NameplateRing");
            var label = plate.transform.Find("NameplateLabel");
            Vector3 landLocal = ring.localPosition;
            Vector3 landLabel = label.localPosition;
            actor.transform.position = new Vector3(-13, RooftopPool.SurfaceY - RooftopPool.FloatDepth, 6);
            yield return null;
            yield return null;
            Assert.IsTrue(actor.IsSwimming);
            Assert.That(ring.position.y, Is.EqualTo(RooftopPool.SurfaceY + CharacterNameplate.RingFloorMargin).Within(.002f));
            Assert.Greater(label.position.y, RooftopPool.SurfaceY + .20f);
            Assert.IsFalse(ring.GetComponentsInChildren<Collider>().Any(c => c.enabled), "UI marker must never become physical support.");
            actor.transform.position = new Vector3(0, 0, 6);
            yield return null;
            Assert.IsFalse(actor.IsSwimming);
            Assert.Less(Vector3.Distance(ring.localPosition, landLocal), .001f);
            Assert.Less(Vector3.Distance(label.localPosition, landLabel), .001f);
        }
    }
}
