using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class KuroMapClearanceProbe
    {
        [UnityTest]
        public IEnumerator GiantFitsTheActualFloorAndGuidewayOnAllThreeMaps()
        {
            bool bots=GameLaunch.AllBots,spectator=GameLaunch.Spectator,pinned=SceneFlow.RulesPinned;
            var rules=SceneFlow.SelectedRules.Clone();
            var report=new StringBuilder("map,x,z,floor,lowest,highest,roof\n");
            string output=Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/kuro-map-clearance";
            try
            {
                foreach(string map in new[]{SceneFlow.Eskinita,SceneFlow.BayanPlaza,SceneFlow.IlalimNgTulay})
                {
                    yield return PlayModeWorld.Reset();
                    SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                    GameLaunch.AllBots=true;GameLaunch.Spectator=true;
                    yield return SceneManager.LoadSceneAsync(map);
                    yield return new WaitForSeconds(.35f);
                    foreach(var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
                    foreach(var point in new[]{new Vector3(-3,.5f,-2),new Vector3(3,.5f,2)})
                    {
                        Assert.IsTrue(Physics.Raycast(point,Vector3.down,out var floor,2,~0,QueryTriggerInteraction.Ignore),map+" has no floor at its review point.");
                        var target=new GameObject("Kuro ground review origin");target.transform.position=floor.point;
                        var pet=Object.Instantiate(RosterBook.Load().People.First(p=>p.Id=="nemu").PetModel);
                        var companion=pet.AddComponent<GhostPetCompanion>();companion.Bind(target.transform,Vector3.up*2,CharacterVisual.PersonScale);
                        companion.enabled=false;companion.Devour(7);
                        var cameraObject=new GameObject("Kuro ordinary eye-height review");var camera=cameraObject.AddComponent<Camera>();
                        camera.enabled=false;camera.fieldOfView=65;camera.nearClipPlane=.04f;
                        cameraObject.AddComponent<ColourGrade>();
                        camera.transform.position=floor.point+new Vector3(2,1.25f,7.5f);
                        camera.transform.LookAt(floor.point+Vector3.up*2);
                        try
                        {
                            float min=float.PositiveInfinity,max=float.NegativeInfinity;
                            for(int sample=0;sample<7;sample++)
                            {
                                companion.StepTo(.8f+sample*.25f);
                                foreach(var skin in pet.GetComponentsInChildren<SkinnedMeshRenderer>())
                                {
                                    var baked=new Mesh();skin.BakeMesh(baked,true);
                                    foreach(var vertex in baked.vertices)
                                    {
                                        float y=skin.transform.TransformPoint(vertex).y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);
                                    }
                                    Object.DestroyImmediate(baked);
                                }
                            }
                            Assert.GreaterOrEqual(min,floor.point.y-.025f,map+" giant clips the physical floor.");
                            Assert.Greater(max-floor.point.y,4.2f,map+" silently shrank the requested giant.");
                            Assert.Less(max-floor.point.y,5.8f,map+" geometry exceeds the intended giant scale.");
                            var roof=Physics.RaycastAll(floor.point+Vector3.up*.2f,Vector3.up,15,~0,QueryTriggerInteraction.Ignore)
                                .Where(h=>h.collider.GetComponentInParent<CharacterMotor>()==null && h.collider.GetComponentInParent<Slipper>()==null)
                                .OrderBy(h=>h.distance).FirstOrDefault();
                            if(roof.collider!=null)Assert.Less(max,roof.point.y-.05f,map+" giant clips the overhead geometry.");
                            report.AppendLine(FormattableString.Invariant($"{map},{point.x},{point.z},{floor.point.y:F3},{min:F3},{max:F3},{(roof.collider!=null?roof.point.y:-1):F3}"));
                            yield return ImprovementEvidenceProbe.Record(camera,map+"-kuro-ground-"+(point.x<0?"west":"east"),2,null,t=>companion.StepTo(.8f+t));
                        }
                        finally {Object.DestroyImmediate(pet);Object.DestroyImmediate(target);Object.DestroyImmediate(cameraObject);}
                    }
                }
            }
            finally
            {
                GameLaunch.AllBots=bots;GameLaunch.Spectator=spectator;
                SceneFlow.AdoptRemoteRules(rules);if(pinned)SceneFlow.PinSelectedRules(rules);else SceneFlow.UnpinSelectedRules();
                Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"kuro-map-clearance.csv"),report.ToString());
            }
            yield return PlayModeWorld.Reset();
        }
    }
}
