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
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ThrowMotionReviewProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private CustomRules _rules;
        private INetProvider _net;
        private string _evidence;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();_net=NetAuthority.Provider;
            _evidence=Environment.GetEnvironmentVariable("TUMP_EVIDENCE");
            Environment.SetEnvironmentVariable("TUMP_EVIDENCE",Output);
            yield return PlayModeWorld.Reset();
        }

        private static string Output=>Environment.GetEnvironmentVariable("TUMP_THROW_MOTION_REVIEW")??"Logs/throw-motion-review-v5";
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_net;
            Environment.SetEnvironmentVariable("TUMP_EVIDENCE",_evidence);
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest,Timeout(120000)]
        public IEnumerator ActualQuickHeldAndMovingThrowsHaveNormalSpeedOwnerAndBodyEvidence()
        {
            var clipping=new System.Collections.Generic.List<string>();
            foreach(var shot in new[]{(Name:"quick",Hold:.20f,Spin:0f,Move:0f),
                (Name:"held-left",Hold:2.8f,Spin:-.75f,Move:0f),
                (Name:"moving-right",Hold:2.8f,Spin:.75f,Move:.6f)})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);
                NetAuthority.Provider=new SoloProvider();GameServices.Round.Lata.HostRestore();GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);who.Teleport(new Vector3(0,.12f,-10));who.transform.rotation=Quaternion.identity;
                who.CharacterIndex=Roster.GetPeople(GameMode.Classic).Select((p,i)=>(p,i)).First(row=>row.p.Id=="bayan").i;
                var person=Resources.Load<RosterBook>("RosterBook").PersonArt(who.CharacterIndex,GameMode.Classic);
                who.GetComponent<CharacterVisual>().ApplyModel(person.Model,person.Tint,person.Clips,person.Palette,person.PetModel);
                var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);rig.SetAimSource(CameraSystem.AimSource.Movement);
                var carrier=who.GetComponent<Carrier>();var shoe=carrier.Held;Assert.IsNotNull(shoe);
                var head=HeadVolume(who.GetComponent<CharacterVisual>().ModelRoot);
                var shoeMesh=shoe.GetComponentInChildren<MeshFilter>();Assert.IsNotNull(shoeMesh);
                var shoeVertices=shoeMesh.sharedMesh.vertices;
                int worstInside=0,clearanceSamples=0;
                var clearance=who.gameObject.AddComponent<LateClearance>();
                clearance.Read=()=>
                {
                    if(carrier==null || !carrier.IsCharging || carrier.Held==null)return;
                    if(head.Bone==null)head=HeadVolume(who.GetComponent<CharacterVisual>().ModelRoot);
                    clearanceSamples++;
                    var toHead=head.Bone.worldToLocalMatrix*shoeMesh.transform.localToWorldMatrix;
                    int inside=0;foreach(var vertex in shoeVertices)if(head.Box.Contains(toHead.MultiplyPoint3x4(vertex)))inside++;
                    worstInside=Mathf.Max(worstInside,inside);
                };
                who.Intent.Clear();who.Intent.Parked=false;
                var camera=new GameObject("Throw motion witness").AddComponent<Camera>();
                camera.enabled=false;camera.fieldOfView=52;camera.nearClipPlane=.05f;camera.farClipPlane=160;camera.allowHDR=true;
                camera.cullingMask&=~(1<<5);camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                var trace=new StringBuilder("time,speed,charging,power,yaw,pitch,held\n");bool released=false;
                try
                {
                    yield return ImprovementEvidenceProbe.Record(camera,shot.Name,shot.Hold+2.0f,who,t=>
                    {
                        who.Intent.Move=t<.5f+shot.Hold+.65f?new Vector2(shot.Move,0):Vector2.zero;
                        who.Intent.AimPoint=new Vector3(2,.18f,0);who.Intent.FaceAimPoint=true;
                        who.Intent.SpinInput=shot.Spin;
                        who.Intent.Set(Verb.SpecialAbility,t>=.5f && t<.5f+shot.Hold);
                        released|=shoe.State==SlipperState.InFlight;
                        var aim=carrier.AimAngularOffset;
                        trace.AppendLine(FormattableString.Invariant($"{t:F4},{who.Velocity.magnitude:F4},{carrier.IsCharging},{carrier.ChargeRatio:F4},{aim.x:F5},{aim.y:F5},{carrier.Held!=null}"));
                    },new Vector3(3,1.5f,-3));
                    Assert.True(released,shot.Name+" did not release through the normal input/Carrier path.");
                    Assert.False(carrier.IsCharging);
                    File.WriteAllText(Path.Combine(Output,shot.Name+"-head-clearance.txt"),$"Maximum shoe vertices inside the inset head volume: {worstInside} / {shoeVertices.Length}; samples {clearanceSamples}\n");
                    Assert.Greater(clearanceSamples,2,"Head clearance must include actual held frames.");
                    if(worstInside>0)clipping.Add(shot.Name+": "+worstInside+" shoe vertices inside head");
                }
                finally
                {
                    Directory.CreateDirectory(Output);
                    File.WriteAllText(Path.Combine(Output,shot.Name+"-aim.csv"),trace.ToString());
                    who.Intent.Clear();clearance.Read=null;clearance.enabled=false;Object.Destroy(clearance);Object.Destroy(camera.gameObject);
                }
                yield return PlayModeWorld.Reset();
            }
            Assert.IsEmpty(clipping,string.Join("; ",clipping));
        }

        private static (Transform Bone,Bounds Box) HeadVolume(Transform model)
        {
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var head=skins.SelectMany(s=>s.bones).First(b=>b!=null&&b.name=="head");
            bool found=false;var box=new Bounds();
            foreach(var skin in skins)
            {
                int bone=Array.IndexOf(skin.bones,head);if(bone<0)continue;
                var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes[bone];
                for(int i=0;i<vertices.Length;i++)
                {
                    var w=weights[i];float influence=(w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)
                        +(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
                    if(influence<.5f)continue;
                    var point=bind.MultiplyPoint3x4(vertices[i]);
                    if(!found){box=new Bounds(point,Vector3.zero);found=true;}else box.Encapsulate(point);
                }
            }
            Assert.True(found,"No weighted head geometry available for grip clearance.");
            box.Expand(-.006f);return(head,box);
        }

        [DefaultExecutionOrder(9999)]
        private sealed class LateClearance:MonoBehaviour
        {
            public Action Read;
            private void LateUpdate()=>Read?.Invoke();
        }
    }
}
