using System.Collections;
using System.Linq;
using System.Reflection;
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
        [UnityTest] public IEnumerator LagoonDeckRefinementKeepsSupportAndRestoresItsOriginalLook()
        {
            yield return Load(SceneFlow.Lagoon);
            var finish=Object.FindAnyObjectByType<LagoonDeckPresentation>();Assert.IsNotNull(finish);Assert.AreEqual(47,finish.BoardRenderers);
            var actor=GameServices.Round.PlayerAt(1);var camera=Camera.main;var rig=camera.GetComponent<CameraRig>();
            actor.Teleport(new Vector3(0,WorldLookPresentation.Current.Floor,-8.2f));actor.transform.rotation=Quaternion.identity;
            foreach(var other in GameServices.Round.Players)if(other!=actor)other.Teleport(new Vector3(10,other.transform.position.y,10));
            yield return new WaitForSeconds(.15f);rig.enabled=false;
            camera.transform.position=new Vector3(0,WorldLookPresentation.Current.Floor+1.286f,-8.2f);camera.transform.rotation=Quaternion.identity;camera.fieldOfView=95;
            var boards=Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name=="Deck board" || r.name.StartsWith("Thin deck surface ")).ToArray();
            var meshes=boards.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).ToArray();
            Assert.IsTrue(WorldGround.TryBelow(actor.transform.position,.5f,2,out float floor));
            var block=new MaterialPropertyBlock();Time.timeScale=0;
            try
            {
                foreach(string state in new[]{"before","after","comfort"})
                {
                    WorldCueProfile.Current.LagoonDeckDetail=state=="before"?0:1;
                    Settings.SettingsStore.Current.ReducedEffects=state=="comfort";Settings.SettingsStore.Current.HighContrastHud=state=="comfort";Settings.SettingsStore.Current.HudScale=state=="comfort"?1.2f:1;
                    yield return null;yield return null;
                    for(int i=0;i<boards.Length;i++)
                    {boards[i].GetPropertyBlock(block);Assert.AreEqual(state=="before"?0:1,block.GetFloat("_DeckSurface"));Assert.AreSame(meshes[i],boards[i].GetComponent<MeshFilter>().sharedMesh);}
                    Assert.IsTrue(WorldGround.TryBelow(actor.transform.position,.5f,2,out float unchanged));Assert.AreEqual(floor,unchanged);
                    yield return GameplayShots.Render(camera,"deck-refined-"+state,false,Output,null,960,540);
                }
                WorldCueProfile.Current.LagoonDeckDetail=0;yield return null;
                boards[0].GetPropertyBlock(block);Assert.AreEqual(0,block.GetFloat("_DeckSurface"));
            }
            finally{WorldCueProfile.Current.LagoonDeckDetail=1;Time.timeScale=1;rig.enabled=true;}
        }
        [UnityTest] public IEnumerator LagoonDeckDistinguishesMaterialDepthAndNormalEdges()
        {
            yield return Load(SceneFlow.Lagoon);
            var actor=GameServices.Round.PlayerAt(1);var camera=Camera.main;var rig=camera.GetComponent<CameraRig>();
            actor.Teleport(new Vector3(0,WorldLookPresentation.Current.Floor,-8.2f));actor.transform.rotation=Quaternion.identity;
            foreach(var other in GameServices.Round.Players)if(other!=actor)other.Teleport(new Vector3(10,other.transform.position.y,10));
            yield return new WaitForSeconds(.15f);rig.enabled=false;
            camera.transform.position=new Vector3(0,WorldLookPresentation.Current.Floor+1.286f,-8.2f);camera.transform.rotation=Quaternion.identity;camera.fieldOfView=95;
            var outline=camera.GetComponent<WorldOutline>();Assert.IsNotNull(outline);
            float Read(string field)=>(float)typeof(WorldOutline).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(outline);
            float thickness=Read("_thickness"),depth=Read("_depthSensitivity"),normal=Read("_normalSensitivity");
            // Saved geometry batches most boards by elevation. Validate actual
            // court coverage, not the authoring generator's renderer count.
            var boards=Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name=="Deck board" || r.name.StartsWith("Thin deck surface ")).ToArray();
            Assert.IsTrue(boards.Any(r=>r.bounds.size.x>27 && r.bounds.size.z>25),"The selected surfaces must include the whole28x26m sporting deck.");
            var saved=boards.Select(r=>new MaterialPropertyBlock()).ToArray();for(int i=0;i<boards.Length;i++)boards[i].GetPropertyBlock(saved[i]);
            var block=new MaterialPropertyBlock();Time.timeScale=0;
            try
            {
                foreach(string state in new[]{"baseline","no-normal","no-depth","no-deck-detail"})
                {
                    outline.SetEdge(thickness,state=="no-depth"?0:depth,state=="no-normal"?0:normal);
                    for(int i=0;i<boards.Length;i++)
                    {
                        boards[i].SetPropertyBlock(saved[i]);
                        if(state=="no-deck-detail")
                        {boards[i].GetPropertyBlock(block);block.SetFloat("_SurfaceStrength",0);boards[i].SetPropertyBlock(block);}
                    }
                    yield return null;yield return GameplayShots.Render(camera,"deck-"+state,false,Output,null,960,540);
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(Output,"deck-discovery.txt"),"Board renderers="+boards.Length+"\n"+
                    string.Join("\n",boards.SelectMany(r=>r.sharedMaterials).Distinct().Select(m=>m.name+" shader="+m.shader.name+" kind="+(m.HasProperty("_SurfaceKind")?m.GetFloat("_SurfaceKind"):-1))));
            }
            finally
            {
                outline.SetEdge(thickness,depth,normal);for(int i=0;i<boards.Length;i++)if(boards[i]!=null)boards[i].SetPropertyBlock(saved[i]);
                Time.timeScale=1;rig.enabled=true;
            }
        }
    }
}
