using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        [UnityTest] public IEnumerator ArchitectureIsCameraScopedAndFiestaStaysOutsideTheCourt()
        {
            foreach(string map in new[]{SceneFlow.BayanPlaza,SceneFlow.Eskinita,SceneFlow.IlalimNgTulay,SceneFlow.SaBubong,SceneFlow.Lagoon})
            {
                yield return Load(map);var look=WorldLookPresentation.Current;
                var witness=StageCamera(new Vector3(5,look.Floor+3.7f,-8),new Vector3(0,look.Floor+.65f,0));
                var materials=Object.FindObjectsByType<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
                var sourceColours=materials.Where(m=>m.HasProperty("_Color")).ToDictionary(m=>m,m=>m.GetColor("_Color"));
                var bunting=Object.FindAnyObjectByType<FiestaBunting>();
                if(map==SceneFlow.BayanPlaza || map==SceneFlow.Eskinita)
                {
                    Assert.IsNotNull(bunting);Assert.Greater(bunting.SpanCount,0,map+" needs at least one supported peripheral string.");
                    Assert.IsEmpty(bunting.GetComponentsInChildren<Collider>());
                    Assert.IsTrue(bunting.GetComponent<Renderer>().sharedMaterial.shader.isSupported);
                    foreach(var point in bunting.GetComponent<MeshFilter>().sharedMesh.vertices)
                        Assert.Greater(Mathf.Abs(bunting.transform.TransformPoint(point).x)-.102f,Core.Balance.ConfinementRadius+.5f,"Even maximum flutter stays clear of the court.");
                }
                else Assert.IsNull(bunting,"Do not stamp fiesta decorations on every map.");
                var preview=new GameObject("Independent preview witness").AddComponent<Camera>();preview.enabled=false;
                float original=Shader.GetGlobalFloat("_WorldArchitecture");
                Private(look,"BeginCamera",witness);Assert.AreEqual(1,Shader.GetGlobalFloat("_WorldArchitecture"));
                Vector4 palette=Shader.GetGlobalVector("_WorldGlassSky");Assert.Greater(palette.x+palette.y+palette.z,0);
                Private(look,"BeginCamera",preview);Assert.AreEqual(0,Shader.GetGlobalFloat("_WorldArchitecture"));
                Private(look,"EndCamera",preview);Assert.AreEqual(1,Shader.GetGlobalFloat("_WorldArchitecture"));Assert.AreEqual(palette,Shader.GetGlobalVector("_WorldGlassSky"));
                Private(look,"EndCamera",witness);Assert.AreEqual(original,Shader.GetGlobalFloat("_WorldArchitecture"));Object.Destroy(preview.gameObject);
                foreach(string state in new[]{"before","after","comfort"})
                {
                    WorldCueProfile.Current.EnvironmentAppeal=state=="before"?0:1;
                    Settings.SettingsStore.Current.ReducedEffects=state=="comfort";Settings.SettingsStore.Current.ReducedUiMotion=state=="comfort";
                    yield return null;
                    using(NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(witness,map+"-appeal-"+state,false,Output,GameServices.Round.PlayerAt(1),960,540);
                    if(bunting!=null)Assert.AreEqual(state!="before",bunting.GetComponent<Renderer>().enabled);
                }
                foreach(var pair in sourceColours)Assert.AreEqual(pair.Value,pair.Key.GetColor("_Color"),"Do not repaint source material assets.");
                WorldCueProfile.Current.EnvironmentAppeal=1;Settings.SettingsStore.Current.ReducedEffects=false;Settings.SettingsStore.Current.ReducedUiMotion=false;
                Object.Destroy(witness.gameObject);
            }
        }
    }
}
