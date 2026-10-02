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
    public class DanteBoulderLoadTests
    {
        private readonly List<GameObject> _objects=new List<GameObject>();
        private CharacterMotor _actor; private Carrier _carrier; private Slipper _shoe;
        private INetProvider _provider; private int _seat; private CustomRules _rules; private bool _pinned;
        private DanteHeroKit Kit => (DanteHeroKit)_actor.AbilitySystem.Kit;
        private AbilityContext Context => new AbilityContext(_actor,_carrier,_actor.GetComponent<CombatVerbs>());
        private GameObject Keep(GameObject go){_objects.Add(go);return go;}
        [UnitySetUp] public IEnumerator Before()
        {
            _provider=NetAuthority.Provider;_seat=GameLaunch.SoloSeat;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();NetAuthority.Provider=new SoloProvider();GameLaunch.SoloSeat=1;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));GameServices.Ensure();GameServices.Round.Clear();
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(30,1,30);
            var can=Keep(new GameObject("Boulder test can"));can.transform.position=new Vector3(6,0,6);GameServices.Round.Lata=can.AddComponent<Lata>();
            var root=Keep(new GameObject("Boulder actor",typeof(CharacterController)));
            var cc=root.GetComponent<CharacterController>();cc.height=1.6f;cc.radius=.35f;cc.center=new Vector3(0,.8f,0);
            _actor=root.AddComponent<CharacterMotor>();_actor.PlayerSlot=1;_actor.Mode=GameMode.HeroStrike;_actor.IsBot=false;
            _carrier=root.AddComponent<Carrier>();root.AddComponent<CombatVerbs>();root.AddComponent<HeroAbilitySystem>().BindHero("dante");
            root.transform.position=new Vector3(0,.13f,-8);GameServices.Round.Register(_actor);GameServices.Match.StartMatch();GameServices.Round.BeginRound();
            var shoeObject=Keep(new GameObject("Boulder own shoe"));
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
        private Renderer Inlay()=>_shoe.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name=="BoulderStoneInlay");
        [UnityTest] public IEnumerator NormalAffinityLeavesTheShoeUntouched()
        {
            var filter=_shoe.GetComponentInChildren<MeshFilter>();var mesh=filter.sharedMesh;var materials=filter.GetComponent<Renderer>().sharedMaterials;
            yield return new WaitForSeconds(.15f);Assert.AreEqual(SlipperAffinity.Normal,_shoe.Affinity);Assert.IsNull(Inlay());
            Assert.AreSame(mesh,filter.sharedMesh);CollectionAssert.AreEqual(materials,filter.GetComponent<Renderer>().sharedMaterials);
        }
        [UnityTest] public IEnumerator AcceptedLoadSurvivesDropAndRetiresOnClear()
        {
            var filter=_shoe.GetComponentInChildren<MeshFilter>();var mesh=filter.sharedMesh;var source=filter.GetComponent<Renderer>();var materials=source.sharedMaterials;
            source.shadowCastingMode=ShadowCastingMode.Off;Shot(source,"before");
            Kit.AttackingSkill.Activate(Context);Assert.AreEqual(SlipperAffinity.Concussed,_shoe.Affinity);yield return null;yield return null;
            var cue=Inlay();Assert.IsNotNull(cue,"Accepted Boulder has no visible shoe payload cue.");Assert.IsTrue(cue.enabled);
            Assert.AreSame(mesh,cue.GetComponent<MeshFilter>().sharedMesh);CollectionAssert.AreEqual(materials,source.sharedMaterials);Shot(source,"loaded");
            Assert.IsTrue(_shoe.HostDisarm());yield return null;yield return null;
            Assert.AreEqual(SlipperAffinity.Concussed,_shoe.Affinity);Assert.IsTrue(cue.enabled,"An unthrown dropped shoe still carries its real payload.");
            Assert.IsTrue(_shoe.HostForceEquip(_actor));yield return null;Assert.IsTrue(cue.enabled);
            _shoe.Affinity=SlipperAffinity.Normal;yield return null;yield return null;Assert.IsFalse(cue.enabled);Shot(source,"cleared");
            for(int i=0;i<3;i++){_shoe.Affinity=SlipperAffinity.Concussed;yield return null;_shoe.Affinity=SlipperAffinity.Normal;yield return null;}
            Assert.AreEqual(1,_shoe.GetComponentsInChildren<Renderer>(true).Count(r=>r.name=="BoulderStoneInlay"));
            Assert.AreSame(mesh,filter.sharedMesh);CollectionAssert.AreEqual(materials,source.sharedMaterials);
        }
        [UnityTest] public IEnumerator SnapshotAffinityAndDisableFollowTheShoe()
        {
            _actor.AbilitySystem.BindHero("cheska");
            _shoe.ApplySnapshotState(SlipperState.Held,_actor,_shoe.transform.position,_shoe.transform.rotation,Vector3.zero,0,SlipperAffinity.Concussed,-1);
            yield return null;yield return null;var cue=Inlay();Assert.IsNotNull(cue);Assert.IsTrue(cue.enabled);
            _shoe.enabled=false;yield return null;Assert.IsFalse(cue.enabled);
            _shoe.enabled=true;yield return null;Assert.IsTrue(cue.enabled);
            _shoe.ApplySnapshotState(SlipperState.Held,_actor,_shoe.transform.position,_shoe.transform.rotation,Vector3.zero,0,SlipperAffinity.Normal,-1);
            yield return null;Assert.IsFalse(cue.enabled);
        }
        [UnityTest] public IEnumerator OwnerMeshKeepsItsSurfacesAndClearsThePayload()
        {
            var root=Keep(new GameObject("Boulder owner arms"));var arms=root.AddComponent<TumbangPreso.CameraSystem.ViewmodelArms>();arms.SetHero("dante");arms.SetHolding(true);arms.MatchSkin(_shoe);
            var filter=root.GetComponentsInChildren<MeshFilter>(true).First(x=>x.name=="HeldSlipper");var original=filter.sharedMesh;var surfaces=filter.GetComponent<Renderer>().sharedMaterials;
            Kit.AttackingSkill.Activate(Context);arms.MatchSkin(_shoe);yield return null;yield return null;
            var cue=filter.GetComponentsInChildren<Renderer>(true).FirstOrDefault(x=>x.name=="BoulderStoneInlay");Assert.IsNotNull(cue);Assert.IsTrue(cue.enabled);
            Assert.AreSame(original,cue.GetComponent<MeshFilter>().sharedMesh);CollectionAssert.AreEqual(surfaces,filter.GetComponent<Renderer>().sharedMaterials);Shot(filter.GetComponent<Renderer>(),"owner-loaded");
            var replacementRoot=Keep(new GameObject("Normal replacement shoe"));
            Object.Instantiate(Resources.Load<RosterEntryAsset>("Roster/slipper_loafers").Model,replacementRoot.transform);
            var replacement=replacementRoot.AddComponent<Slipper>();
            arms.MatchSkin(replacement);yield return null;Assert.IsFalse(cue.enabled,"Replacing a loaded owner source must clear its old cue.");
            arms.MatchSkin(_shoe);yield return null;Assert.IsTrue(cue.enabled);
            _shoe.Affinity=SlipperAffinity.Normal;arms.MatchSkin(_shoe);yield return null;Assert.IsFalse(cue.enabled);
            Assert.AreEqual(1,filter.GetComponentsInChildren<Renderer>(true).Count(x=>x.name=="BoulderStoneInlay"));
        }
        private void Shot(Renderer source,string name)
        {
            var go=Keep(new GameObject("Boulder asset camera"));var camera=go.AddComponent<Camera>();camera.enabled=false;
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
                string output=System.Environment.GetEnvironmentVariable("TUMP_BOULDER_EVIDENCE")??"Logs/dante-boulder-load/frames";
                Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
            }
            finally {foreach(var pair in layers)if(pair.node!=null)pair.node.gameObject.layer=pair.layer;foreach(var pair in modes)if(pair.renderer!=null)pair.renderer.shadowCastingMode=pair.mode;RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(target);Object.Destroy(texture);}
        }
    }
}
