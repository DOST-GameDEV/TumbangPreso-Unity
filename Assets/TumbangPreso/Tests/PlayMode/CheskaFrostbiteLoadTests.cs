using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
namespace TumbangPreso.PlayTests
{
    public class CheskaFrostbiteLoadTests
    {
        private readonly List<GameObject> _objects=new List<GameObject>();
        private CharacterMotor _actor; private Carrier _carrier; private Slipper _shoe;
        private INetProvider _provider; private int _seat; private CustomRules _rules; private bool _pinned;
        private CheskaHeroKit Kit => (CheskaHeroKit)_actor.AbilitySystem.Kit;
        private AbilityContext Context => new AbilityContext(_actor,_carrier,_actor.GetComponent<CombatVerbs>());
        private GameObject Keep(GameObject go){_objects.Add(go);return go;}
        [UnitySetUp] public IEnumerator Before()
        {
            _provider=NetAuthority.Provider;_seat=GameLaunch.SoloSeat;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();NetAuthority.Provider=new SoloProvider();GameLaunch.SoloSeat=1;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));GameServices.Ensure();GameServices.Round.Clear();
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(30,1,30);
            var can=Keep(new GameObject("Rime test can"));can.transform.position=new Vector3(6,0,6);GameServices.Round.Lata=can.AddComponent<Lata>();
            var root=Keep(new GameObject("Rime actor",typeof(CharacterController)));
            var cc=root.GetComponent<CharacterController>();cc.height=1.6f;cc.radius=.35f;cc.center=new Vector3(0,.8f,0);
            _actor=root.AddComponent<CharacterMotor>();_actor.PlayerSlot=1;_actor.Mode=GameMode.HeroStrike;_actor.IsBot=false;
            _carrier=root.AddComponent<Carrier>();root.AddComponent<CombatVerbs>();root.AddComponent<HeroAbilitySystem>().BindHero("cheska");
            root.transform.position=new Vector3(0,.13f,-8);GameServices.Round.Register(_actor);GameServices.Match.StartMatch();GameServices.Round.BeginRound();
            var shoeObject=Keep(new GameObject("Rime own shoe"));
            var art=Resources.Load<RosterEntryAsset>("Roster/slipper_loafers");Assert.IsNotNull(art);Assert.IsNotNull(art.Model);
            var model=Object.Instantiate(art.Model,shoeObject.transform);model.name="Visual";
            foreach(var collider in model.GetComponentsInChildren<Collider>())Object.Destroy(collider);
            TumbangPreso.Visual.ToonSkin.ApplySlipper(model,TumbangPreso.Visual.ToonSkin.PropOutlineWidth);
            _shoe=shoeObject.AddComponent<Slipper>();_shoe.OwnerSlot=1;_shoe.SeatOfOrigin=1;
            Assert.IsTrue(_shoe.HostForceEquip(_actor));yield return new WaitForSeconds(.2f);
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach(var go in _objects)if(go!=null)Object.Destroy(go);_objects.Clear();yield return PlayModeWorld.Reset();
            NetAuthority.Provider=_provider;GameLaunch.SoloSeat=_seat;SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        private Renderer Rime()=>_shoe.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name=="FrostbiteSoleRime");
        [UnityTest] public IEnumerator LoadedCueUsesTheRealShoeAndRetiresWithTheLoad()
        {
            var mesh=_shoe.GetComponentInChildren<MeshFilter>();Assert.IsNotNull(mesh);
            var original=mesh.sharedMesh;var renderer=mesh.GetComponent<Renderer>();var materials=renderer.sharedMaterials;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            Shot(renderer,"before");
            Kit.AttackingSkill.Activate(Context);Assert.IsTrue(Kit.IsFrostbiteLoaded);
            yield return new WaitForSeconds(.25f);
            var rime=Rime();Assert.IsNotNull(rime,"A loaded Frostbite slipper has no visible surface cue.");
            Assert.IsTrue(rime.enabled);Assert.AreSame(original,rime.GetComponent<MeshFilter>().sharedMesh);
            CollectionAssert.AreEqual(materials,renderer.sharedMaterials,"The cue replaced authored shoe materials.");
            Shot(renderer,"loaded");
            Assert.IsTrue(_shoe.HostDisarm());yield return null;yield return null;
            Assert.IsFalse(rime.enabled,"A loose shoe must not advertise a held Frostbite load.");
            Assert.IsTrue(_shoe.HostForceEquip(_actor));yield return null;yield return null;
            Assert.IsTrue(rime.enabled,"Returning the same still-loaded shoe lost its cue.");
            Kit.CancelFrostbiteWindow();yield return null;yield return null;
            Assert.IsTrue(rime==null||!rime.gameObject.activeInHierarchy);Shot(renderer,"consumed");
            Assert.AreSame(original,mesh.sharedMesh);CollectionAssert.AreEqual(materials,renderer.sharedMaterials);
        }
        [UnityTest] public IEnumerator AnUnloadedKitLeavesTheShoeUntouched()
        {
            var mesh=_shoe.GetComponentInChildren<MeshFilter>();var original=mesh.sharedMesh;var materials=mesh.GetComponent<Renderer>().sharedMaterials;
            yield return new WaitForSeconds(.3f);Assert.IsFalse(Kit.IsFrostbiteLoaded);Assert.IsNull(Rime());
            Assert.AreSame(original,mesh.sharedMesh);CollectionAssert.AreEqual(materials,mesh.GetComponent<Renderer>().sharedMaterials);
        }
        [UnityTest] public IEnumerator ExpiryAndResetRetireTheCue()
        {
            Kit.AttackingSkill.Activate(Context);yield return new WaitForSeconds(.25f);
            var rime=Rime();Assert.IsNotNull(rime);
            Kit.Tick(Context,CryoRules.FrostbiteLoadSeconds+.1f);yield return null;yield return null;
            Assert.IsFalse(Kit.IsFrostbiteLoaded);Assert.IsTrue(rime==null||!rime.gameObject.activeInHierarchy);
            Kit.Reset();Kit.AttackingSkill.Activate(Context);yield return new WaitForSeconds(.25f);
            rime=Rime();Assert.IsNotNull(rime);
            Kit.ResetForRound(Context);yield return null;yield return null;
            Assert.IsFalse(Kit.IsFrostbiteLoaded);Assert.IsTrue(rime==null||!rime.gameObject.activeInHierarchy);
        }
        [UnityTest] public IEnumerator RestoredLoadWaitsForEquipmentWithoutRefreshingTheClock()
        {
            Assert.IsTrue(_shoe.HostDisarm());
            Assert.IsTrue(Kit.RestoreTimedKit(_actor,new TimedKitSnapshot(Kit.AttackingSkill,3f)));
            yield return new WaitForSeconds(.2f);Assert.IsNull(Rime());
            float before=Kit.AttackingSkill.DurationRemaining;
            Assert.IsTrue(_shoe.HostForceEquip(_actor));yield return null;yield return null;
            Assert.IsNotNull(Rime());Assert.IsTrue(Rime().enabled);
            Assert.LessOrEqual(Kit.AttackingSkill.DurationRemaining,before);
            Assert.Less(Kit.AttackingSkill.DurationRemaining,3f);
        }
        [UnityTest] public IEnumerator OwnerMeshUsesTheSameCueAndKeepsAuthoredSurfaces()
        {
            var root=Keep(new GameObject("Rime owner arms"));
            var arms=root.AddComponent<TumbangPreso.CameraSystem.ViewmodelArms>();
            arms.SetHolding(true);arms.MatchSkin(_shoe);
            var owner=root.GetComponentsInChildren<MeshFilter>(true).First(x=>x.name=="HeldSlipper");
            var original=owner.sharedMesh;var surfaces=owner.GetComponent<Renderer>().sharedMaterials;
            Kit.AttackingSkill.Activate(Context);arms.MatchSkin(_shoe);yield return new WaitForSeconds(.25f);
            var cue=owner.GetComponentInChildren<TumbangPreso.Visual.CheskaFrostbiteCoating>(true);
            Assert.IsNotNull(cue);Assert.Greater(cue.VisibleStrength,0);
            Assert.AreSame(original,cue.GetComponent<MeshFilter>().sharedMesh);
            CollectionAssert.AreEqual(surfaces,owner.GetComponent<Renderer>().sharedMaterials);
            // Isolated owner-shoe render, not a full player-camera framing claim.
            Shot(owner.GetComponent<Renderer>(),"owner-loaded");
            Kit.CancelFrostbiteWindow();yield return null;yield return null;
            Assert.IsTrue(cue==null||!cue.gameObject.activeInHierarchy);
        }
        [UnityTest] public IEnumerator WikiFrostbiteLoadLastsFifteenSeconds()
        {
            Kit.AttackingSkill.Activate(Context);
            Kit.Tick(Context,10.1f);
            Assert.IsTrue(Kit.IsFrostbiteLoaded,"The new Wiki load must survive the former ten-second limit.");
            Kit.Tick(Context,4.8f);
            Assert.IsTrue(Kit.IsFrostbiteLoaded);
            Kit.Tick(Context,.2f);
            Assert.IsFalse(Kit.IsFrostbiteLoaded,"An unthrown Frostbite load must expire at fifteen seconds.");
            Assert.AreEqual(35,Kit.AttackingSkill.Cooldown);
            yield return null;
        }

        [UnityTest] public IEnumerator JoiningFrostbiteCanRestoreFourteenSeconds()
        {
            Assert.IsTrue(Kit.RestoreTimedKit(_actor,new TimedKitSnapshot(Kit.AttackingSkill,14f)));
            Assert.IsTrue(Kit.IsFrostbiteLoaded);
            Assert.AreEqual(14f,Kit.AttackingSkill.DurationRemaining,.001f);
            yield return null;
        }

        [UnityTest] public IEnumerator FrostbiteWindowCoatsEveryThrowWithoutRefreshing()
        {
            Kit.AttackingSkill.Activate(Context);Kit.Tick(Context,2f);
            _carrier.HostThrowAt(_actor.transform.position+Vector3.up,new Vector3(0,0,5),.5f);
            Assert.AreEqual(SlipperAffinity.Frost,_shoe.Affinity);
            Assert.IsTrue(Kit.IsFrostbiteLoaded,"Throwing must not consume the fifteen-second window.");
            Assert.AreEqual(13f,Kit.AttackingSkill.DurationRemaining,.001f);
            Assert.IsTrue(_shoe.HostForceEquip(_actor));Kit.Tick(Context,2f);
            _carrier.HostThrowAt(_actor.transform.position+Vector3.up,new Vector3(0,0,5),.5f);
            Assert.AreEqual(SlipperAffinity.Frost,_shoe.Affinity);
            Assert.IsTrue(Kit.IsFrostbiteLoaded);Assert.AreEqual(11f,Kit.AttackingSkill.DurationRemaining,.001f);
            Assert.IsTrue(_shoe.HostForceEquip(_actor));Kit.Tick(Context,11.1f);
            _carrier.HostThrowAt(_actor.transform.position+Vector3.up,new Vector3(0,0,5),.5f);
            Assert.AreEqual(SlipperAffinity.Normal,_shoe.Affinity,"Throws after expiry must be ordinary.");
            yield return null;
        }
        [UnityTest] public IEnumerator CryoPlacementUsesTheNewOwnerNumbers()
        {
            Assert.AreEqual(2.5f,CryoRules.ColdFeetRadius);
            Assert.AreEqual(.5f,Kit.Skill1.AimMinRange);Assert.AreEqual(5f,Kit.Skill1.AimMaxRange);
            Assert.AreEqual(1f,Kit.Skill1.AimRampSeconds);Assert.AreEqual(5f,Kit.Skill1.AimRangeFor(1));
            Assert.AreEqual(10f,CryoRules.GlacialWallSeconds);Assert.AreEqual(3,CryoRules.GlacialWallHits);
            Assert.AreEqual(5f,CryoRules.GlacialWallArcLength);Assert.AreEqual(3f,CryoRules.GlacialWallArcRadius);
            Assert.AreEqual(1.5f,Kit.DefendingSkill.AimMinRange);Assert.AreEqual(4f,Kit.DefendingSkill.AimMaxRange);
            Assert.AreEqual(1f,Kit.DefendingSkill.AimRampSeconds);Assert.AreEqual(4f,Kit.DefendingSkill.AimRangeFor(1));
            yield return null;
        }
        private CharacterMotor ChillVictim(Vector3 position)
        {
            var go=Keep(new GameObject("Chill duration victim",typeof(CharacterController)));
            var cc=go.GetComponent<CharacterController>();cc.height=1.6f;cc.radius=.35f;cc.center=Vector3.up*.8f;
            var victim=go.AddComponent<CharacterMotor>();victim.PlayerSlot=2;victim.Mode=GameMode.HeroStrike;
            victim.RoundActive=true;victim.IsBot=true;victim.Intent.Parked=true;
            go.AddComponent<Carrier>();GameServices.Round.Register(victim);victim.Teleport(position);return victim;
        }
        [UnityTest,Timeout(15000)] public IEnumerator ActualCryoShoveChillExpiresAfterFiveSeconds()
        {
            var victim=ChillVictim(_actor.transform.position+Vector3.forward);Physics.SyncTransforms();
            Assert.IsTrue(_actor.GetComponent<CombatVerbs>().HostResolveShove(_actor.transform.position,Vector3.forward));
            Assert.AreEqual(Time.time,_actor.GetComponent<CombatVerbs>().LastShoveLandedAt,.001f,"The duration control must exercise a real hit, not an accepted miss.");
            Assert.AreEqual(5f,victim.ChilledLeft,.001f);
            yield return new WaitForSeconds(4.7f);Assert.IsTrue(victim.IsChilled);
            yield return new WaitForSeconds(.5f);Assert.IsFalse(victim.IsChilled,"Ordinary shove Chill must not use the ultimate's combined timer.");
        }
        [UnityTest,Timeout(15000)] public IEnumerator ColdFeetChillExpiresFiveSecondsAfterLeavingTheField()
        {
            var victim=ChillVictim(new Vector3(4,.13f,-4));
            var field=Keep(HeroHazards.SpawnIceSheet(new Vector3(4,0,-4),CryoRules.ColdFeetRadius,7.5f,_actor.PlayerSlot,1));
            yield return new WaitForSeconds(.2f);Assert.That(victim.ChilledLeft,Is.InRange(4.8f,5.01f));
            victim.Teleport(new Vector3(8,.13f,-4));
            yield return new WaitForSeconds(5.2f);
            Assert.IsTrue(field!=null,"The field must still exist so this verifies leaving it, not field destruction.");
            Assert.IsFalse(victim.IsChilled,"Leaving the ice must not leave a seven-and-a-half-second slow.");
        }

        [UnityTest] public IEnumerator ActualCryoFieldsUseNewRadiusLifeAndThreeHitWall()
        {
            Kit.Skill1.Activate(Context);
            var ice=Object.FindFirstObjectByType<HeroHazards.IceSheetComponent>();
            Assert.IsNotNull(ice);Assert.AreEqual(2.5f,ice.Radius);Assert.AreEqual(7.5f,ice.Duration);
            _actor.IsDefender=true;
            Kit.DefendingSkill.Activate(Context);
            var wall=Object.FindFirstObjectByType<HeroHazards.IceBarricadeComponent>();
            Assert.IsNotNull(wall);Assert.AreEqual(10f,wall.Duration);Assert.AreEqual(3,wall.HitsToShatter);
            wall.HostSlipperHit();wall.HostSlipperHit();yield return null;
            Assert.IsTrue(wall!=null);wall.HostSlipperHit();yield return null;
            Assert.IsTrue(wall==null,"The third slipper hit must shatter the actual wall.");
        }

        [UnityTest] public IEnumerator GlacialWallHasNoGapsAcrossItsFiveMetreArc()
        {
            var wall=Keep(HeroHazards.SpawnIceBarricade(new Vector3(0,0,2),Vector3.forward,10,arcLength:5,arcRadius:3));
            yield return null;Physics.SyncTransforms();
            var blockers=wall.GetComponentsInChildren<Collider>();int gaps=0;
            foreach(float height in new[]{.2f,.9f,1.4f})
                for(int i=0;i<=80;i++)
                {
                    float angle=Mathf.Lerp(-5f/6f+.025f,5f/6f-.025f,i/80f);
                    var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                    var origin=wall.transform.position+new Vector3(0,height,-3)+radial*4;
                    var ray=new Ray(origin,-radial);
                    if(!blockers.Any(c=>c.Raycast(ray,out _,2f)))gaps++;
                }
            var light=Keep(new GameObject("Wall coverage light")).AddComponent<Light>();
            light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-25,0);
            var eye=Keep(new GameObject("Compact wall witness")).AddComponent<Camera>();eye.enabled=false;
            eye.transform.position=new Vector3(0,2.5f,-5);eye.transform.LookAt(new Vector3(0,1,1.7f));eye.fieldOfView=48;
            string folder=System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/compact-wall";
            yield return GameplayShots.Render(eye,"glacial-wall-front",false,outDir:folder,width:960,height:540);
            eye.transform.position=new Vector3(-5,3,-4);eye.transform.LookAt(new Vector3(0,1,1.7f));
            yield return GameplayShots.Render(eye,"glacial-wall-side",false,outDir:folder,width:960,height:540);
            Assert.AreEqual(0,gaps,"The actual five-metre wall has unblocked gaps between its authored ice slabs.");
        }

        private void Shot(Renderer source,string name)
        {
            var go=Keep(new GameObject("Rime asset camera"));var camera=go.AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.15f);camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.fieldOfView=40;
            // Owner arms are camera-local and can sit below the fixture floor.
            // Isolate the target shoe, so world geometry cannot hide the witness.
            var layers=source.GetComponentsInChildren<Transform>(true).Select(x=>(node:x,layer:x.gameObject.layer)).ToArray();
            foreach(var pair in layers)pair.node.gameObject.layer=31;
            camera.cullingMask=1<<31;
            var modes=source.GetComponentsInChildren<Renderer>(true).Select(x=>(renderer:x,mode:x.shadowCastingMode)).ToArray();
            foreach(var pair in modes)if(pair.mode==ShadowCastingMode.ShadowsOnly)pair.renderer.shadowCastingMode=ShadowCastingMode.Off;
            var b=source.bounds;float extent=Mathf.Max(b.size.x,b.size.y,b.size.z);
            camera.transform.position=b.center+new Vector3(.8f,.8f,1.5f)*extent;camera.transform.LookAt(b.center);
            var target=new RenderTexture(400,400,24);var texture=new Texture2D(400,400,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,400,400),0,0);texture.Apply();
                string output=System.Environment.GetEnvironmentVariable("TUMP_RIME_EVIDENCE")??"Logs/cheska-rime/frames";
                Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
            }
            finally {foreach(var pair in layers)if(pair.node!=null)pair.node.gameObject.layer=pair.layer;foreach(var pair in modes)if(pair.renderer!=null)pair.renderer.shadowCastingMode=pair.mode;RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(target);Object.Destroy(texture);}
        }
    }
}
