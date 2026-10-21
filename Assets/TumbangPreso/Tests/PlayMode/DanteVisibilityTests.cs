using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteVisibilityTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest] public IEnumerator BarrierKeepsAuthoredPlayVisibleInTheCourt()
        {
            int mip=QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit=2;
            GameObject barrier=null, eye=null;
            try
            {
                yield return MapRetrievalProbe.Load("Eskinita",Core.GameMode.HeroStrike);
                Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
                yield return new WaitForSeconds(3.6f);
                var who=GameServices.Round.PlayerAt(1); var other=GameServices.Round.PlayerAt(2);
                foreach(var player in GameServices.Round.Players)
                {
                    player.Intent.Clear(); player.Intent.Parked=true;
                    player.Teleport(new Vector3(6,.12f,player.PlayerSlot*2));
                }
                who.Teleport(new Vector3(-2,.12f,-5)); who.transform.rotation=Quaternion.identity;
                other.Teleport(new Vector3(-2,.12f,-2));
                var art=RosterBook.Load().FindPersonArt("dante");
                who.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                var reference = new GameObject("Original barrier palette");
                var original=ReworkProp.Spawn("barrier",reference.transform,ReworkProp.GeoPalette);
                reference.SetActive(false);
                var originals=original.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
                barrier=DanteBarrierVisual.Build(who.transform);
                yield return new WaitForSeconds(.7f);
                foreach(var m in originals) Assert.AreEqual(1,m.color.a,"Shared source palette was mutated.");
                foreach(var m in barrier.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials))
                {
                    Assert.AreEqual(16,m.GetVectorArray("_Palette").Length);
                    CollectionAssert.AreEqual(originals[0].GetVectorArray("_Palette"),m.GetVectorArray("_Palette"));
                }
                Object.Destroy(reference);
                var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);
                rig.SetAimSource(CameraSystem.AimSource.Movement);
                yield return null;
                eye=new GameObject("Dante barrier owner viewpoint"); var camera=eye.AddComponent<Camera>();camera.enabled=false;
                camera.transform.position=who.transform.position+Vector3.up*1.25f;
                camera.transform.rotation=who.transform.rotation;camera.fieldOfView=72;
                yield return GameplayShots.Render(camera,"owner-viewpoint",false,"Logs/dante-court-v2",who);

                camera.transform.position=who.transform.position+new Vector3(4,3,6);
                camera.transform.LookAt(who.transform.position+Vector3.up);camera.fieldOfView=48;
                yield return GameplayShots.Render(camera,"observer",false,"Logs/dante-court-v2",who);
            }
            finally
            {
                QualitySettings.globalTextureMipmapLimit=mip;
                if(barrier!=null)Object.DestroyImmediate(barrier);
                if(eye!=null)Object.DestroyImmediate(eye);
            }
        }

        [UnityTest] public IEnumerator BarrierTransmitsPlayFromBothSides()
        {
            var owner = new GameObject("Barrier owner");
            var barrier = DanteBarrierVisual.Build(owner.transform);
            var eye = new GameObject("Barrier review").AddComponent<Camera>();
            eye.enabled = false; eye.clearFlags = CameraClearFlags.SolidColor;
            eye.backgroundColor = Color.gray; eye.fieldOfView = 45;
            var light = new GameObject("Barrier light").AddComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(30,30,0);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Behind barrier target"; marker.transform.localScale = new Vector3(.7f,.7f,.05f);
            var material = new Material(Shader.Find("Unlit/Color"));
            marker.GetComponent<Renderer>().sharedMaterial = material;
            var target = new RenderTexture(640,360,24); target.Create(); eye.targetTexture = target;
            string dir = "Logs/dante-visibility-captures"; Directory.CreateDirectory(dir);
            try
            {
                yield return new WaitForSeconds(.7f);
                var center = barrier.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).First();
                foreach (var renderer in barrier.GetComponentsInChildren<Renderer>()) center.Encapsulate(renderer.bounds);
                float minTransmission=1;
                foreach (int side in new[]{-1,1})
                {
                    var focus = new Vector3(0,1.05f,center.center.z);
                    eye.transform.position = focus + Vector3.forward * (side*3.5f);
                    eye.transform.LookAt(focus);
                    marker.transform.position = focus - Vector3.forward * side;
                    var samples = new Color[4];
                    for(int i=0;i<4;i++)
                    {
                        barrier.SetActive(i<2); material.color = i%2==0 ? Color.red : Color.green;
                        eye.Render(); var previous=RenderTexture.active; RenderTexture.active=target;
                        var image=new Texture2D(640,360,TextureFormat.RGB24,false);
                        image.ReadPixels(new Rect(0,0,640,360),0,0); image.Apply();
                        Color sum=Color.clear;
                        for(int y=175;y<185;y++)for(int x=315;x<325;x++)sum+=image.GetPixel(x,y);
                        samples[i]=sum/100;
                        File.WriteAllBytes(dir+"/side"+side+"-"+i+".png",image.EncodeToPNG());
                        Object.DestroyImmediate(image);RenderTexture.active=previous;
                    }
                    float changed=Vector3.Distance((Vector4)samples[0],(Vector4)samples[1]);
                    float clear=Vector3.Distance((Vector4)samples[2],(Vector4)samples[3]);
                    float transmission=changed/clear; minTransmission=Mathf.Min(minTransmission,transmission);
                    Debug.Log($"[DanteVisibility] side={side} transmission={transmission:F4}");
                }
                barrier.SetActive(true);
                Assert.That(minTransmission,Is.GreaterThan(.35f),"The real barrier hides the target behind it.");
                foreach(var m in barrier.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials))
                    Assert.That(m.color.a,Is.EqualTo(.5f).Within(.001f));
                Assert.IsEmpty(barrier.GetComponentsInChildren<Collider>());
                var owned=barrier.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
                Object.Destroy(barrier);yield return null;yield return null;
                Assert.IsTrue(owned.All(m=>m==null),"Per-cast materials outlive the barrier.");
            }
            finally
            {
                Object.DestroyImmediate(owner);Object.DestroyImmediate(eye.gameObject);
                Object.DestroyImmediate(marker);Object.DestroyImmediate(material);Object.DestroyImmediate(light.gameObject);
                target.Release();Object.DestroyImmediate(target);
            }
        }
    }
}
