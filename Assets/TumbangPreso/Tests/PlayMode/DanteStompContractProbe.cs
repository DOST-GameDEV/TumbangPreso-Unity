using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteStompContractProbe
    {
        private bool _bots,_spectator,_pinned;private int _seat;
        private CustomRules _rules;private INetProvider _net;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;_net=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_net;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator EarthquakeIsFeltWithoutTurningAimAndSettlesCompletely()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            var who=GameServices.Round.PlayerAt(1);
            who.Intent.Clear();who.Intent.Parked=false;
            var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);
            rig.SetAimSource(CameraSystem.AimSource.Movement);
            yield return new WaitForSeconds(.4f);
            var facing=rig.transform.rotation;float scale=Time.timeScale,max=0,aftershock=0;
            rig.BeginGroundRumble(1);float began=Time.time;
            while(Time.time-began<2.7f)
            {
                yield return null;
                max=Mathf.Max(max,rig.GroundRumbleOffset.magnitude);
                if(Time.time-began>.85f&&Time.time-began<1.15f)aftershock=Mathf.Max(aftershock,rig.GroundRumbleOffset.magnitude);
                Assert.Less(Quaternion.Angle(facing,rig.transform.rotation),.10f,"Earthquake rotation disturbs the player's aim.");
            }
            Assert.That(max,Is.InRange(.003f,.011f),"Earthquake is absent or exceeds its small translation budget.");
            Assert.Greater(aftershock,.0007f,"The settling aftershock never reaches the view.");
            Assert.AreEqual(Vector3.zero,rig.GroundRumbleOffset,"Ground rumble outlives its effect.");
            Assert.AreEqual(scale,Time.timeScale,"Ground rumble changes simulation timing.");
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator HookedMountainFacesBlockTheirStoneLeaveTheGapAndExpire()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(20,.5f,20);
            Physics.SyncTransforms();
            var left=DanteFissurePillar.Create(new Vector3(-1.6f,0,2.9f),Vector3.forward,-1,1.4f);
            var right=DanteFissurePillar.Create(new Vector3(1.6f,0,2.9f),Vector3.forward,1,1.4f);
            yield return new WaitForSeconds(.08f);Physics.SyncTransforms();
            Assert.False(Physics.Raycast(new Vector3(0,.5f,-1),Vector3.forward,6),"The separating stone briefly seals the central route.");
            yield return new WaitForSeconds(.22f);Physics.SyncTransforms();
            foreach(float x in new[]{-1.6f,1.6f})
            {
                Assert.True(Physics.Raycast(new Vector3(x,1,-1),Vector3.forward,out var hit,6),"A mountain face lost its real obstacle.");
                Assert.IsNotNull(hit.collider.GetComponentInParent<DanteFissurePillar>());
            }
            foreach(float x in new[]{-.6f,0,.6f})
                Assert.False(Physics.Raycast(new Vector3(x,1,-1),Vector3.forward,6),"Invisible stone blocks the central route.");
            Assert.False(Physics.Raycast(new Vector3(1.6f,5.5f,-1),Vector3.forward,6),"Collision extends above the crest.");
            Assert.IsEmpty(left.GetComponentsInChildren<Rigidbody>());
            Assert.IsEmpty(right.GetComponentsInChildren<Light>());
            yield return new WaitForSeconds(1.3f);Physics.SyncTransforms();
            Assert.True(left==null&&right==null,"Pillars did not retire after their lifetime.");
            Assert.False(Physics.Raycast(new Vector3(1.6f,1,-1),Vector3.forward,6),"Expired stone still blocks movement.");
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator FracturesStayOnSlopedGroundCoolAndLeaveNoPhysicalDebris()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(20,.5f,20);
            floor.transform.rotation=Quaternion.Euler(0,0,7);Physics.SyncTransforms();
            var state=UnityEngine.Random.state;
            DanteSeismicVisual.Impact(Vector3.zero,Vector3.forward,2.2f,false);
            Assert.AreEqual(state,UnityEngine.Random.state,"Cosmetic fractures changed gameplay randomness.");
            var effect=Object.FindFirstObjectByType<DanteSeismicVisual>();Assert.IsNotNull(effect);
            Assert.IsEmpty(effect.GetComponentsInChildren<Collider>());
            Assert.IsEmpty(effect.GetComponentsInChildren<Rigidbody>());
            var heat=effect.transform.Find("MoltenHairline").GetComponent<MeshFilter>();
            var vertices=heat.sharedMesh.vertices;var initial=new Vector3[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                initial[i]=heat.transform.TransformPoint(vertices[i]);
                Assert.True(Physics.Raycast(initial[i]+Vector3.up*2,Vector3.down,out var hit,4));
                Assert.That(initial[i].y-hit.point.y,Is.InRange(.018f,.04f),"Ground fracture floats or sinks into the sloped court.");
            }
            effect.StepTo(.2f);float early=0;foreach(var colour in heat.sharedMesh.colors)early+=colour.a;
            effect.StepTo(2.5f);float late=0;foreach(var colour in heat.sharedMesh.colors)late+=colour.a;
            Assert.Less(late,early*.2f,"The molten break stays fully bright throughout recovery.");
            for(int i=0;i<vertices.Length;i++)Assert.Less(Vector3.Distance(initial[i],heat.transform.TransformPoint(heat.sharedMesh.vertices[i])),.00001f);
            yield return new WaitForSeconds(1.2f);
            Assert.True(effect==null,"A spent fracture remained in the court.");
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator FissureLaunchesForwardTargetsWhileItsCasterAndRearStaySafe()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider=new SoloProvider();var round=GameServices.Round;
            var caster=round.PlayerAt(1);var forward=round.PlayerAt(2);var rear=round.PlayerAt(3);
            foreach(var player in round.Players){player.Intent.Clear();player.Intent.Parked=false;player.ClearStun();player.ClearTrip();}
            round.PlayerAt(0).Teleport(new Vector3(-9,.12f,0));
            var art=RosterBook.Load().FindPersonArt("dante");
            caster.CharacterIndex=Roster.IndexIn(Roster.GetPeople(GameMode.HeroStrike),"dante");
            caster.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            caster.AbilitySystem.BindHero("dante");caster.AbilitySystem.Kit.AddUltimateCharge(100);
            caster.Teleport(new Vector3(0,.12f,-10));caster.transform.rotation=Quaternion.identity;
            forward.Teleport(new Vector3(0,.12f,-7.5f));rear.Teleport(new Vector3(0,.12f,-11.3f));
            caster.Intent.AimPoint=new Vector3(0,.1f,-5);caster.Intent.FaceAimPoint=true;
            yield return new WaitForSeconds(.2f);
            var casterStart=caster.transform.position;var forwardStart=forward.transform.position;var rearStart=rear.transform.position;
            float time=Time.time,casterRise=0,forwardRise=0,rearTravel=0;bool accepted=false;
            while(Time.time-time<1.65f)
            {
                float elapsed=Time.time-time;caster.Intent.Set(Verb.Ultimate,elapsed<.28f);
                accepted|=caster.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Ultimate)==HeroKit.CastOutcome.Cast;
                casterRise=Mathf.Max(casterRise,caster.transform.position.y-casterStart.y);
                forwardRise=Mathf.Max(forwardRise,forward.transform.position.y-forwardStart.y);
                rearTravel=Mathf.Max(rearTravel,Vector3.Distance(rear.transform.position,rearStart));
                yield return null;
            }
            caster.Intent.Clear();Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/dante-fissure-contact.csv",FormattableString.Invariant($"accepted,caster_rise,forward_rise,rear_travel\n{accepted},{casterRise},{forwardRise},{rearTravel}\n"));
            Assert.True(accepted,"The actual ultimate input was not accepted.");
            Assert.Greater(forwardRise,.5f,"The forward fissure did not launch its target.");
            Assert.Less(casterRise,.08f,"The planted caster was launched by his own fallback blast.");
            Assert.Less(rearTravel,.12f,"A forward fissure hit a target standing behind the caster.");
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator SkillCaptionKeepsItsReadingDirectionAcrossOppositeCameras()
        {
            var first=new GameObject("Caption front camera").AddComponent<Camera>();first.tag="MainCamera";
            var second=new GameObject("Caption rear camera").AddComponent<Camera>();
            var rt=RenderTexture.GetTemporary(512,256,24,RenderTextureFormat.ARGB32);
            var pixels=new Texture2D(512,256,TextureFormat.RGB24,false);var oldActive=RenderTexture.active;
            try
            {
                foreach(var camera in new[]{first,second})
                {
                    camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                    camera.fieldOfView=45;camera.cullingMask=1<<30;camera.targetTexture=rt;
                }
                first.transform.position=new Vector3(0,0,-4);second.transform.position=new Vector3(0,0,4);
                first.transform.LookAt(Vector3.zero);second.transform.LookAt(Vector3.zero);
                ComicPopup.Spawn(Vector3.zero,"THUD!",Color.white,1);
                yield return new WaitForSeconds(.18f);
                var popup=Object.FindFirstObjectByType<ComicPopup>();Assert.IsNotNull(popup);
                typeof(ComicPopup).GetField("_tiltAngle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(popup,0f);
                foreach(var node in popup.GetComponentsInChildren<Transform>())node.gameObject.layer=30;
                foreach(var camera in new[]{first,second})camera.transform.LookAt(popup.transform.position);
                Canvas.ForceUpdateCanvases();Directory.CreateDirectory("Logs/skill-caption-cameras-v1");
                Color32[] Capture(Camera camera,string name)
                {
                    ComicPopup.PrepareView(camera);camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,512,256),0,0);pixels.Apply();
                    File.WriteAllBytes("Logs/skill-caption-cameras-v1/"+name+".png",pixels.EncodeToPNG());return pixels.GetPixels32();
                }
                var a=Capture(first,"front");var b=Capture(second,"rear");var again=Capture(first,"front-again");
                int ink=0,difference=0,returnDifference=0;
                for(int i=0;i<a.Length;i++)
                {
                    if(a[i].r+a[i].g+a[i].b>300)ink++;
                    if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>40)difference++;
                    if(Mathf.Abs(a[i].r-again[i].r)+Mathf.Abs(a[i].g-again[i].g)+Mathf.Abs(a[i].b-again[i].b)>40)returnDifference++;
                }
                Assert.Greater(ink,150,"No readable caption was rendered.");
                Assert.Less(difference,ink*.12f,"The secondary camera mirrored or lost the caption.");
                Assert.Less(returnDifference,ink*.12f,"The main camera retained the previous view's caption transform.");
            }
            finally
            {
                first.targetTexture=null;second.targetTexture=null;RenderTexture.active=oldActive;
                RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(pixels);Object.Destroy(first.gameObject);Object.Destroy(second.gameObject);
            }
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator CarapaceFitsTheTorsoHidesOnlyFromItsWearerAndCleansUp()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider=new SoloProvider();var who=GameServices.Round.PlayerAt(1);
            foreach(var other in GameServices.Round.Players)
                if(other!=who)other.Teleport(new Vector3(-9,.12f,other.PlayerSlot*3));
            who.Teleport(new Vector3(0,.12f,-8));who.transform.rotation=Quaternion.identity;
            var art=RosterBook.Load().FindPersonArt("dante");
            who.CharacterIndex=Roster.IndexIn(Roster.GetPeople(GameMode.HeroStrike),"dante");
            who.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);
            rig.SetAimSource(CameraSystem.AimSource.Movement);who.Intent.Clear();who.Intent.Parked=false;
            var ctx=new AbilityContext(who,who.GetComponent<Carrier>(),who.GetComponent<CombatVerbs>());
            foreach(bool heavy in new[]{false,true})
            {
                who.AbilitySystem.BindHero("dante",heavy?new HeroBuild{HeroId="dante",Slot2VariantId="dante.2.plating"}:null);
                float speed=who.Stamina.SpeedZones.Value;
                who.AbilitySystem.Kit.Skill2.Activate(ctx);yield return new WaitForSeconds(.55f);
                var ward=who.GetComponentInChildren<DanteCarapaceVisual>();Assert.IsNotNull(ward);
                Assert.AreEqual("torso",ward.transform.parent.name);
                Assert.IsEmpty(ward.GetComponentsInChildren<Collider>());
                var fitted=ward.GetComponentsInChildren<Renderer>();Assert.Greater(fitted.Length,4);
                var bounds=fitted[0].bounds;foreach(var renderer in fitted)bounds.Encapsulate(renderer.bounds);
                Assert.Less(bounds.size.magnitude,1.25f,"Armor still encloses the character in a large shell.");
                var orbit=who.transform.Find("DanteOrbitingWard");Assert.IsNotNull(orbit);
                Assert.AreEqual(3,orbit.childCount,"The protective orbit should have three authored stone shields.");
                Assert.IsEmpty(orbit.GetComponentsInChildren<Collider>());
                var first=orbit.GetChild(0);var beforeOrbit=first.localPosition;
                ward.StepTo(1.25f);
                Assert.Greater(Vector3.Distance(first.localPosition,beforeOrbit),.25f,"The floating protectors do not actually orbit.");
                foreach(Transform protector in orbit)
                {
                    var flat=protector.localPosition;flat.y=0;
                    Assert.That(flat.magnitude,Is.InRange(.65f,.86f),"The orbit intrudes into the body or strays beyond its protection.");
                }
                ward.StepTo(.55f);
                var renderers=ward.VisiblePieces;
                string output=Environment.GetEnvironmentVariable("TUMP_DANTE_WARD_REVIEW")??"Logs/dante-ward-review-v2";
                yield return GameplayShots.Render(rig.Camera,heavy?"heavy-owner":"normal-owner",false,output);
                foreach(var renderer in renderers)Assert.False(renderer.enabled,"The wearer's FPP camera draws obstructing ward geometry.");
                var witness=new GameObject("Ward witness").AddComponent<Camera>();witness.enabled=false;
                try
                {
                    witness.allowHDR=true;witness.fieldOfView=42;witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    witness.transform.position=who.transform.position+new Vector3(-1.6f,1.25f,2.7f);
                    witness.transform.LookAt(who.transform.position+Vector3.up*.7f);
                    yield return GameplayShots.Render(witness,heavy?"heavy-body":"normal-body",false,output,who);
                    foreach(var renderer in renderers)Assert.True(renderer.enabled,"Other cameras cannot see the ward.");
                    if(!heavy&&Environment.GetEnvironmentVariable("TUMP_DANTE_ARMOR_DETAIL")=="1")
                    {
                        witness.transform.position=who.transform.position+new Vector3(-1.6f,1.25f,-2.7f);
                        witness.transform.LookAt(who.transform.position+Vector3.up*.7f);
                        yield return GameplayShots.Render(witness,"normal-back",false,output,who);
                        witness.transform.position=who.transform.position+new Vector3(-2.7f,1.1f,.2f);
                        witness.transform.LookAt(who.transform.position+Vector3.up*.65f);
                        yield return GameplayShots.Render(witness,"normal-left",false,output,who);
                    }
                }
                finally{Object.Destroy(witness.gameObject);}
                who.AbilitySystem.ResetKit();yield return null;
                Assert.IsNull(who.GetComponentInChildren<DanteCarapaceVisual>());
                yield return null;
                Assert.IsNull(who.transform.Find("DanteOrbitingWard"),"Detached orbiting stones outlive the skill.");
                Assert.AreEqual(speed,who.Stamina.SpeedZones.Value,.001f,"Ending Heavy Plating left its slowdown behind.");
            }
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator RoundResetBeforeContactCancelsStompAndReleasesItsRoot()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);NetAuthority.Provider=new SoloProvider();
            var caster=GameServices.Round.PlayerAt(1);var victim=GameServices.Round.PlayerAt(2);
            caster.Intent.Clear();caster.Intent.Parked=false;victim.Intent.Clear();victim.Intent.Parked=false;
            caster.Teleport(new Vector3(0,.12f,-10));victim.Teleport(new Vector3(1.2f,.12f,-10));
            victim.ClearStun();victim.ClearTrip();caster.AbilitySystem.BindHero("dante");
            yield return new WaitForSeconds(.15f);
            float speedZone=caster.Stamina.SpeedZones.Value;var start=victim.transform.position;
            caster.Intent.Set(Verb.Skill1,true);yield return new WaitForSeconds(.08f);caster.Intent.Set(Verb.Skill1,false);
            Assert.True(caster.AbilitySystem.Kit.Skill1.IsWindingUp);
            Assert.IsNotNull(GameObject.Find("DanteGroundPressure"),"The real cast has no ground warning.");
            caster.AbilitySystem.ResetKit();
            yield return new WaitForSeconds(.45f);
            Assert.False(caster.AbilitySystem.Kit.Skill1.IsWindingUp);
            Assert.IsNull(GameObject.Find("DanteGroundPressure"),"Cancelled cast left a pressure warning behind.");
            Assert.AreEqual(speedZone,caster.Stamina.SpeedZones.Value,.001f,"Cancelled stomp left a movement root behind.");
            Assert.False(victim.IsStunned||victim.IsTripped,"A cancelled stomp hit after the round reset.");
            Assert.Less(Vector3.Distance(victim.transform.position,start),.04f);
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator ContactIsGroundedAndLongTremorKeepsTargetsClose()
        {
            var errors=new List<string>();
            var rows=new List<string>{"variant,caster_rise,victim_distance,shoe_distance,first_impact,tripped,charges,shoe_outward"};
            foreach(bool tremor in new[]{false,true})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
                var ready=Object.FindFirstObjectByType<ReadyGate>();ready.StartLocalCountdown();
                yield return new WaitForSeconds(3.6f);NetAuthority.Provider=new SoloProvider();
                var round=GameServices.Round;var caster=round.PlayerAt(1);var victim=round.PlayerAt(2);
                foreach(var player in round.Players)
                {
                    player.Intent.Clear();player.Intent.Parked=false;player.ClearStun();
                    player.Teleport(new Vector3(-9,.12f,-12+player.PlayerSlot*3));
                }
                var art=RosterBook.Load().FindPersonArt("dante");
                caster.CharacterIndex=Roster.IndexIn(Roster.GetPeople(GameMode.HeroStrike),"dante");
                caster.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                caster.AbilitySystem.BindHero("dante",tremor?new HeroBuild{HeroId="dante",Slot1VariantId="dante.1.tremor"}:null);
                Assert.AreEqual(tremor,caster.AbilitySystem.HasVariant("dante.1.tremor"));
                caster.Teleport(new Vector3(0,.12f,-8));caster.transform.rotation=Quaternion.identity;
                victim.Teleport(new Vector3(1.2f,.12f,-8));
                var shoe=round.PlayerAt(3).GetComponent<Carrier>().Held;Assert.IsNotNull(shoe);
                Assert.True(shoe.HostDisarm());var parked=new Vector3(-1.2f,0,-8);
                parked.y=Slipper.GroundY(parked)+shoe.RestHeight;shoe.transform.position=parked;
                yield return new WaitForSeconds(.2f);
                var start=caster.transform.position;var targetStart=victim.transform.position;var shoeStart=shoe.transform.position;
                float rise=0,firstImpact=-1,startTime=Time.time;bool tripped=false,sawWarning=false;
                caster.Intent.Set(Verb.Skill1,true);
                while(Time.time-startTime<.9f)
                {
                    float t=Time.time-startTime;if(t>.08f)caster.Intent.Set(Verb.Skill1,false);
                    if(!sawWarning)
                    {
                        var warning=GameObject.Find("DanteGroundPressure");
                        if(warning!=null)
                        {
                            sawWarning=true;float radius=caster.AbilitySystem.Kit.Skill1.TelegraphRadius,maxRadius=0;
                            foreach(var filter in warning.GetComponentsInChildren<MeshFilter>())
                                foreach(var vertex in filter.sharedMesh.vertices)
                                {
                                    var flat=filter.transform.TransformPoint(vertex)-warning.transform.position;flat.y=0;
                                    maxRadius=Mathf.Max(maxRadius,flat.magnitude);
                                }
                            if(maxRadius<radius-.03f||maxRadius>radius+.05f)
                                errors.Add("Stomp warning radius "+maxRadius+" differs from its equipped hit radius "+radius);
                        }
                    }
                    rise=Mathf.Max(rise,caster.transform.position.y-start.y);tripped|=victim.IsTripped;
                    if(firstImpact<0&&(victim.IsStunned||victim.IsTripped||Vector3.Distance(victim.transform.position,targetStart)>.08f))firstImpact=t;
                    yield return null;
                }
                caster.Intent.Clear();
                float travel=Vector3.ProjectOnPlane(victim.transform.position-targetStart,Vector3.up).magnitude;
                float shoeTravel=Vector3.ProjectOnPlane(shoe.transform.position-shoeStart,Vector3.up).magnitude;
                var kickedAway=Vector3.ProjectOnPlane(shoeStart-start,Vector3.up).normalized;
                float outwardTravel=Vector3.Dot(shoe.transform.position-shoeStart,kickedAway);
                int charges=caster.AbilitySystem.Kit.Skill1.ChargesRemaining;
                string label=tremor?"long-tremor":"default";
                if(!sawWarning)errors.Add(label+": no actual windup warning was observed.");
                rows.Add(FormattableString.Invariant($"{label},{rise},{travel},{shoeTravel},{firstImpact},{tripped},{charges},{outwardTravel}"));
                if(charges!=1)errors.Add(label+": actual skill input did not spend exactly one charge.");
                if(rise>.08f)errors.Add(label+": ground stomp launched its caster by "+rise+"m.");
                if(firstImpact<.25f||firstImpact>.48f)errors.Add(label+": impact did not match the authored .30second foot contact: "+firstImpact);
                if(tremor)
                {
                    if(!tripped)errors.Add("Long Tremor never took the victim down.");
                    if(travel>.18f)errors.Add("Long Tremor threw its victim "+travel+"m despite promising to keep them close.");
                    if(shoeTravel>.18f)errors.Add("Long Tremor scattered the nearby loose slipper "+shoeTravel+"m.");
                }
                else
                {
                    if(travel<.5f)errors.Add("Default stomp did not clear space around the caster.");
                    if(shoeTravel<.5f)errors.Add("Default stomp did not move the loose slipper.");
                    if(outwardTravel<.5f)errors.Add("The loose slipper jumped somewhere else instead of flying away from the stomp.");
                }
                yield return PlayModeWorld.Reset();
            }
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/dante-stomp-contact.csv",rows);
            Assert.IsEmpty(errors,string.Join("\n",errors));
        }
    }
}
