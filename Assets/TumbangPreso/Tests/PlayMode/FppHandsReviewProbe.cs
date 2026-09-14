using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class FppHandsReviewProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private CustomRules _rules;
        private INetProvider _net;
        private static string Output => Environment.GetEnvironmentVariable("TUMP_FPP_HANDS_REVIEW") ?? "Logs/fpp-hands-review-v1";
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();_net=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_net;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest,Timeout(180000)]
        public IEnumerator AllEighteenPeopleShowTheirOwnHandsInTheActualFirstPersonCamera()
        {
            Directory.CreateDirectory(Output);var rows=new List<string>();int people=0;
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,mode);
                NetAuthority.Provider=new SoloProvider();GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);var carrier=who.GetComponent<Carrier>();var shoe=carrier.Held;
                var visual=who.GetComponent<CharacterVisual>();
                who.Teleport(GameServices.Round.Lata.transform.position+new Vector3(0,.12f,-10));
                who.transform.rotation=Quaternion.identity;
                var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);
                rig.SetAimSource(CameraSystem.AimSource.Movement);
                var witness=new GameObject("Hand identity body witness").AddComponent<Camera>();
                witness.enabled=false;witness.fieldOfView=42;witness.nearClipPlane=.05f;witness.farClipPlane=200;witness.allowHDR=true;
                witness.cullingMask&=~(1<<5);witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                var entries=Roster.GetPeople(mode);
                for(int index=0;index<entries.Count;index++)
                {
                    var entry=RosterBook.Load().PersonArt(index,mode);who.CharacterIndex=index;
                    if(mode==GameMode.HeroStrike)who.AbilitySystem.BindHero(entry.Id);
                    visual.ApplyModel(entry.Model,entry.Tint,entry.Clips,entry.Palette,entry.PetModel);
                    who.Intent.Clear();who.Intent.Parked=true;
                    shoe.HostForceEquip(who);yield return null;yield return null;
                    var arms=rig.GetComponentInChildren<CameraSystem.ViewmodelArms>(true);
                    Assert.IsNotNull(arms);arms.MatchCharacter(who);
                    yield return GameplayShots.Render(rig.Camera,entry.Id+"-carrying",false,Output);
                    foreach(string side in new[]{"Right","Left"})
                    {
                        var renderer=arms.transform.Find(side+"Pivot/Arm").GetComponent<MeshRenderer>();
                        var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;var material=renderer.sharedMaterial;
                        var palette=material.GetVectorArray("_Palette");var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                        rows.Add($"{entry.Id}/{side}: mesh={mesh.name}; size={mesh.bounds.size}; scale={renderer.transform.localScale}; shader={material.shader.name}; palette={material.GetFloat("_UsePalette")}; skin15={(palette!=null&&palette.Length>15?palette[15].ToString():"none")}; color={material.GetColor("_Color")}; blockEmpty={block.isEmpty}; quality={QualitySettings.activeColorSpace}; expectedSkin={entry.Palette[15]}");
                    }
                    Assert.True(shoe.HostDisarm());
                    var floor=who.transform.position+new Vector3(-2,0,-2);
                    floor.y=Slipper.GroundY(floor)+shoe.RestHeight;shoe.transform.position=floor;
                    yield return null;
                    yield return GameplayShots.Render(rig.Camera,entry.Id+"-empty",false,Output);
                    witness.transform.position=who.transform.position+new Vector3(1.3f,1.45f,3.0f);
                    witness.transform.LookAt(who.transform.position+Vector3.up*.85f);
                    yield return GameplayShots.Render(witness,entry.Id+"-body",false,Output,who);
                    people++;
                }
                yield return PlayModeWorld.Reset();
            }
            File.WriteAllLines(Path.Combine(Output,"materials.txt"),rows);
            Assert.AreEqual(18,people);
        }

        [UnityTest]
        public IEnumerator ZackSkinCanBeComparedUnderLiveAndNeutralCameraGrading()
        {
            string folder=Environment.GetEnvironmentVariable("TUMP_FPP_COLOUR_REVIEW")??"Logs/fpp-colour-isolation-v1";Directory.CreateDirectory(folder);
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            NetAuthority.Provider=new SoloProvider();GameServices.Round.BeginRound();
            var who=GameServices.Round.PlayerAt(1);var art=RosterBook.Load().FindPersonArt("zack");
            who.CharacterIndex=Roster.IndexIn(Roster.GetPeople(GameMode.HeroStrike),"zack");
            who.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            who.AbilitySystem.BindHero("zack");who.Intent.Clear();who.Intent.Parked=true;
            who.Teleport(GameServices.Round.Lata.transform.position+new Vector3(0,.12f,-10));
            var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);
            rig.SetAimSource(CameraSystem.AimSource.Movement);yield return null;yield return null;
            var grade=rig.GetComponent<ColourGrade>();
            try
            {
                yield return GameplayShots.Render(rig.Camera,"live-grade",false,folder);
                grade.Set(1,1,1,0,1.9f);
                yield return GameplayShots.Render(rig.Camera,"neutral-grade",false,folder);
                Assert.AreEqual(art.Model,who.GetComponent<CharacterVisual>().SourceModel);
            }
            finally { grade.AdoptFromScene(); }
        }
    }
}
