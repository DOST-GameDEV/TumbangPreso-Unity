using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.EditorTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class RosterArmAuditTests
    {
        [Test]
        public void EveryArmHasARealForearmAxisAndCurrentCharacterGeometry()
        {
            var rows = new List<string> { "id,side,baked_x,baked_y,baked_z,source_x,source_y,source_z,axis,source_submeshes,materials,uv_cells,current_vertices" };
            var errors = new List<string>();
            var details = new List<string>();
            foreach (var entry in RosterBook.Load().People.Where(e=>TumbangPreso.Core.Roster.AllPeople.Any(p=>p.Id==e.Id)))
            foreach (string side in new[] { "left", "right" })
            {
                var points = new List<Vector3>(); var cells = new HashSet<string>();
                int surfaces = 0; var materials = new List<string>();
                foreach (var renderer in entry.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    int bone = Array.FindIndex(renderer.bones, b => b != null && b.name == "arm-" + side);
                    if (bone < 0) continue;
                    var mesh = renderer.sharedMesh; var weights = mesh.boneWeights;
                    var vertices = mesh.vertices; var uv = mesh.uv; var bind = mesh.bindposes[bone];
                    var owned = new List<(Vector3 position,Vector2 uv)>();
                    surfaces += mesh.subMeshCount;
                    materials.AddRange(renderer.sharedMaterials.Select(m => m == null ? "null" : m.name));
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var w = weights[i];
                        float influence=(w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)
                            +(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
                        if (influence < .99f) continue;
                        points.Add(bind.MultiplyPoint3x4(vertices[i]));
                        owned.Add((bind.MultiplyPoint3x4(vertices[i]),uv[i]));
                        cells.Add(Mathf.FloorToInt(uv[i].x*16)+":"+Mathf.FloorToInt(uv[i].y*16));
                    }
                    if(owned.Count>0)
                    {
                        float sign=Mathf.Sign(owned.Average(p=>p.position.x));
                        float far=owned.Max(p=>p.position.x*sign);
                        var tip=owned.Where(p=>p.position.x*sign>far-.06f).Select(p=>
                            Mathf.FloorToInt(p.uv.x*16)+":"+Mathf.FloorToInt(p.uv.y*16)).Distinct();
                        details.Add($"{entry.Id}/{side}: renderer={renderer.name}, enabled={renderer.enabled}, active={renderer.gameObject.activeSelf}, vertices={owned.Count}, tipUV={string.Join("|",tip)}");
                    }
                }
                Assert.IsNotEmpty(points, entry.Id + "/" + side);
                var min=points.Aggregate(Vector3.Min);var max=points.Aggregate(Vector3.Max);var size=max-min;
                int axis=size.x>=size.y&&size.x>=size.z?0:size.y>=size.z?1:2;
                var baked=Resources.Load<Mesh>("Models/RosterArms/"+entry.Id+"_"+side);
                var view=new GameObject("FPP material "+entry.Id);
                try
                {
                    var arms=view.AddComponent<TumbangPreso.CameraSystem.ViewmodelArms>();arms.EnsureBuilt();arms.SetCharacter(entry.Id);
                    var rendered=view.transform.Find((side=="right"?"RightPivot":"LeftPivot")+"/Arm").GetComponent<MeshRenderer>();
                    var material=rendered.sharedMaterial;var palette=material.GetVectorArray("_Palette");
                    var visibleSize=Vector3.Scale(rendered.GetComponent<MeshFilter>().sharedMesh.bounds.size,rendered.transform.localScale);
                    if(entry.Id!="inday" && Mathf.Max(visibleSize.x,visibleSize.z)>.36f)
                        errors.Add(entry.Id+"/"+side+" exceeds the clean reference hand's close-camera cross-section");
                    if(entry.Id=="inday" && rendered.transform.localScale!=Vector3.one)
                        errors.Add("Inday's copied arm proportions were changed after authoring.");
                    details.Add($"{entry.Id}/{side} MATERIAL shader={material.shader.name}, usePalette={material.GetFloat("_UsePalette")}, colour={material.GetColor("_Color")}, slot15={(palette!=null&&palette.Length>15?palette[15].ToString():"missing")}, texScale={material.mainTextureScale}, mesh={rendered.GetComponent<MeshFilter>().sharedMesh.name}");
                }
                finally { Object.DestroyImmediate(view); }
                var expected=ViewmodelArmAuthor.Extract(entry.Model,"arm-"+side);
                bool current=baked!=null&&expected.vertices.SequenceEqual(baked.vertices);
                try
                {
                    Assert.IsNotNull(baked);var span=baked.bounds.size;
                    rows.Add(FormattableString.Invariant($"{entry.Id},{side},{span.x:F5},{span.y:F5},{span.z:F5},{size.x:F5},{size.y:F5},{size.z:F5},{axis},{surfaces},{string.Join("|",materials)},{string.Join("|",cells)},{current}"));
                    if(!current)errors.Add(entry.Id+"/"+side+" is stale versus its current body source");
                }
                finally { Object.DestroyImmediate(expected); }
            }
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/roster-arm-audit.csv",rows);
            File.WriteAllLines("Logs/roster-arm-surface-details.txt",details);
            Assert.IsEmpty(errors,string.Join("\n",errors));
        }
    }
}
