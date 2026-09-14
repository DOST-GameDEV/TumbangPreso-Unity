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
    public sealed class PhaisterRitualContractProbe
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
        public IEnumerator GrandCovenWarnsBeforeItsFirstCurse()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider=new SoloProvider();var round=GameServices.Round;
            var caster=round.PlayerAt(1);var victim=round.PlayerAt(2);
            foreach(var player in round.Players){player.Intent.Clear();player.Intent.Parked=false;player.Teleport(new Vector3(-10,.12f,6+player.PlayerSlot*3));}
            var art=RosterBook.Load().FindPersonArt("phaister");
            caster.CharacterIndex=Roster.IndexIn(Roster.GetPeople(GameMode.HeroStrike),"phaister");
            caster.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            caster.AbilitySystem.BindHero("phaister");caster.AbilitySystem.Kit.AddUltimateCharge(100);
            caster.Teleport(new Vector3(0,.12f,-8));victim.Teleport(new Vector3(1.5f,.12f,-8));
            caster.transform.rotation=Quaternion.identity;victim.ClearStun();victim.ClearTrip();
            yield return new WaitForSeconds(.2f);
            float start=Time.time,firstCurse=-1;bool accepted=false,sawPreparation=false;
            while(Time.time-start<2.3f)
            {
                float elapsed=Time.time-start;caster.Intent.Set(Verb.Ultimate,elapsed<.2f);
                accepted|=caster.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Ultimate)==HeroKit.CastOutcome.Cast;
                sawPreparation|=caster.AbilitySystem.Kit.Ultimate.IsWindingUp;
                if(firstCurse<0&&victim.StunElement==StunElement.Hex&&victim.IsStunned)firstCurse=elapsed;
                yield return null;
            }
            caster.Intent.Clear();Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/phaister-ritual-contact.csv",FormattableString.Invariant($"accepted,preparation,first_curse\n{accepted},{sawPreparation},{firstCurse}\n"));
            Assert.True(accepted,"The real ultimate input was not accepted.");
            Assert.True(sawPreparation,"Grand Coven gives no committed ritual warning before its curse.");
            Assert.That(firstCurse,Is.InRange(1.4f,1.95f),"Curse contact does not follow the authored ritual build.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator MovingDuringPreparationKeepsCurseInsideTheDrawnWarning()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            var round = GameServices.Round;
            foreach (var player in round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = false;
                player.Teleport(new Vector3(-15, .12f, 6 + player.PlayerSlot * 3));
            }
            var caster = round.PlayerAt(1);
            var warned = round.PlayerAt(2);
            var outside = round.PlayerAt(3);
            caster.AbilitySystem.BindHero("phaister");
            caster.AbilitySystem.Kit.AddUltimateCharge(100);
            var origin = new Vector3(0, .12f, -8);
            caster.Teleport(origin);
            warned.Teleport(origin + Vector3.left * 9);
            outside.Teleport(origin + Vector3.right * 13);
            warned.ClearStun(); warned.ClearTrip();
            outside.ClearStun(); outside.ClearTrip();
            yield return new WaitForSeconds(.2f);

            float start = Time.time;
            bool accepted = false, moved = false, warnedHit = false, outsideHit = false;
            while (Time.time - start < 2.15f)
            {
                float elapsed = Time.time - start;
                caster.Intent.Set(Verb.Ultimate, elapsed < .2f);
                accepted |= caster.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Ultimate) == HeroKit.CastOutcome.Cast;
                if (!moved && elapsed > .4f)
                {
                    // A relocation during preparation must not detach the actual zone
                    // from its already drawn warning, whatever moved the caster.
                    caster.Teleport(origin + Vector3.right * 6);
                    moved = true;
                }
                warnedHit |= warned.IsStunned && warned.StunElement == StunElement.Hex;
                outsideHit |= outside.IsStunned && outside.StunElement == StunElement.Hex;
                yield return null;
            }
            caster.Intent.Clear();
            File.WriteAllText("Logs/phaister-moving-ritual.csv",
                FormattableString.Invariant($"accepted,moved,warned_hit,outside_hit,caster_x\n{accepted},{moved},{warnedHit},{outsideHit},{caster.transform.position.x}\n"));
            Assert.True(accepted && moved, "The cast and relocation did not execute.");
            Assert.That(caster.transform.position.x, Is.GreaterThan(5));
            Assert.True(warnedHit, "A player inside the original warning escaped because the curse moved with its caster.");
            Assert.False(outsideHit, "A player outside the original warning was hit by an unmarked relocated curse.");
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator ResetAndPredictionRollbackCancelAnUnfinishedRitual()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider=new SoloProvider();var caster=GameServices.Round.PlayerAt(1);var victim=GameServices.Round.PlayerAt(2);
            caster.Teleport(new Vector3(0,.12f,-8));victim.Teleport(new Vector3(1.5f,.12f,-8));
            caster.Intent.Clear();caster.Intent.Parked=false;victim.Intent.Clear();victim.Intent.Parked=false;
            caster.AbilitySystem.BindHero("phaister");
            var context=new AbilityContext(caster,caster.GetComponent<Carrier>(),caster.GetComponent<CombatVerbs>());
            foreach(bool rollback in new[]{false,true})
            {
                victim.ClearStun();caster.AbilitySystem.ResetKit();float speed=caster.Stamina.SpeedZones.Value;
                caster.AbilitySystem.Kit.Ultimate.Activate(context);
                yield return new WaitForSeconds(.35f);
                Assert.True(caster.AbilitySystem.Kit.Ultimate.IsWindingUp);
                Assert.IsNotNull(GameObject.Find("GrandCovenEclipseEffect"));
                if(rollback)caster.AbilitySystem.Kit.Ultimate.RollBackPredictedCast(context);
                else caster.AbilitySystem.ResetKit();
                yield return new WaitForSeconds(1.8f);
                Assert.False(victim.IsStunned,"A cancelled preparation cursed its nearby target.");
                Assert.IsNull(GameObject.Find("GrandCovenEclipseEffect"),"A cancelled ritual left its spell graphics alive.");
                Assert.AreEqual(speed,caster.Stamina.SpeedZones.Value,.001f,"A cancelled ritual left its movement root behind.");
            }
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator BindingAndPassageKeepTheirDistinctFootprintsAndExpire()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.15f,0);floor.transform.localScale=new Vector3(20,.3f,20);
            Physics.SyncTransforms();
            foreach(bool brand in new[]{false,true})
            {
                float radius=brand?1.44f:2.4f;
                var zone=HeroHazards.SpawnHexSigil(Vector3.zero,radius,1.2f,-1,brand?1.4f:1);
                yield return new WaitForSeconds(.55f);
                var ring=zone.transform.Find("Ward").GetComponent<Renderer>();
                Assert.AreEqual(radius,ring.bounds.extents.x,.035f,"The Hex boundary shrank independently of its real footprint.");
                var writing=zone.transform.Find("WardWriting").GetComponent<MeshFilter>();
                var point=writing.sharedMesh.vertices[0];var before=writing.transform.TransformPoint(point);
                yield return new WaitForSeconds(.25f);
                Assert.Less(Vector3.Distance(before,writing.transform.TransformPoint(point)),.001f,"The binding moves off the sampled floor.");
                Assert.IsEmpty(zone.GetComponentsInChildren<Collider>());
                yield return new WaitForSeconds(.55f);Assert.True(zone==null);
            }
            var departure=HeroHazards.SpawnShadowRift(Vector3.zero,Vector3.forward);
            var arrival=PhaisterArrivalSeal.Create(new Vector3(0,0,4),Vector3.forward);
            yield return new WaitForSeconds(.18f);
            Assert.Greater(departure.GetComponentInChildren<Renderer>().bounds.size.y,1.5f,"The departing passage has no standing silhouette.");
            Assert.Less(arrival.GetComponentInChildren<Renderer>().bounds.size.y,.10f,"The arrival repeats a tall departure instead of a ground fold.");
            Assert.IsEmpty(departure.GetComponentsInChildren<Collider>());
            Assert.IsEmpty(arrival.GetComponentsInChildren<Collider>());
            yield return new WaitForSeconds(1.35f);
            Assert.True(departure==null&&arrival==null,"A blink leaves permanent visual debris.");
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator EclipseAndItsGroundBoundaryStayInsideTheArenaSurfaces()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay,GameMode.HeroStrike);
            Assert.True(Physics.Raycast(new Vector3(0,3,0),Vector3.up,out var ceiling,12,~0,QueryTriggerInteraction.Ignore),"The under-bridge fixture has no measured ceiling.");
            var effect=HeroHazards.SpawnGrandCovenEclipse(Vector3.zero,10.5f,7);
            yield return new WaitForSeconds(1.1f);
            var moon=effect.transform.Find("Eclipse/Moon").GetComponent<Renderer>();
            var reach=effect.transform.Find("EclipseReach").GetComponent<Renderer>();
            var errors=new List<string>();
            if(moon.bounds.max.y>=ceiling.point.y-.10f)errors.Add("The eclipse is above the bridge: moon="+moon.bounds.max.y+", ceiling="+ceiling.point.y);
            if(reach.bounds.extents.x<10.3f)errors.Add("The real10.5m curse has a shrunken boundary radius="+reach.bounds.extents.x);
            yield return new WaitForSeconds(.9f);
            var circle=effect.transform.Find("CovenCircle");var rule=circle.Find("Rule_00").GetComponent<MeshFilter>();
            var vertex=rule.sharedMesh.vertices[0];var start=rule.transform.TransformPoint(vertex);
            yield return new WaitForSeconds(.35f);
            float drift=Vector3.Distance(start,rule.transform.TransformPoint(vertex));
            if(drift>.001f)errors.Add("Already drawn ground inscriptions rotate off their sampled surface by "+drift+"m.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/phaister-arena-surface.csv",FormattableString.Invariant($"ceiling,moon_top,boundary_radius,ground_drift\n{ceiling.point.y},{moon.bounds.max.y},{reach.bounds.extents.x},{drift}\n"));
            Assert.IsEmpty(errors,string.Join("\n",errors));
        }

        [UnityTest]
        public IEnumerator GroundProjectionObservesChangedSurfaceOwnership()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Ground"; floor.transform.position = new Vector3(0, -.1f, 0);
            floor.transform.localScale = new Vector3(6, .2f, 6);
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform"; platform.transform.position = new Vector3(0, .3f, 0);
            platform.transform.localScale = new Vector3(4, .2f, 4);
            var mark = new GameObject("ProjectionTest", typeof(MeshFilter));
            var mesh = new Mesh { vertices = new[] { new Vector3(-.5f, 0, -.5f),
                new Vector3(.5f, 0, -.5f), new Vector3(.5f, 0, .5f), new Vector3(-.5f, 0, .5f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 } };
            mark.GetComponent<MeshFilter>().sharedMesh = mesh;
            VfxShapes.Own(mark, mesh);
            var flat = mesh.vertices;
            Physics.SyncTransforms();
            VfxShapes.DrapeToGround(mark);
            Assert.AreEqual(.035f, mesh.vertices[0].y, .001f, "Court precedence changed.");

            floor.name = "OrdinarySlab";
            mesh.vertices = flat;
            VfxShapes.DrapeToGround(mark);
            Assert.AreEqual(.435f, mesh.vertices[0].y, .001f, "A later projection reused the former court classification.");

            platform.AddComponent<Rigidbody>().isKinematic = true;
            mesh.vertices = flat;
            Physics.SyncTransforms();
            VfxShapes.DrapeToGround(mark);
            Assert.AreEqual(.035f, mesh.vertices[0].y, .001f, "A later projection failed to exclude a now-moving prop.");
            yield return null;
        }

        private sealed class ReturningClient : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        private static IEnumerator PreparePlacementCourt()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.ClearStun(); player.ClearTrip();
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator HexAndSlowBrandApplyTheirActualFootprintsAndPulseRates()
        {
            yield return PreparePlacementCourt();
            var caster = GameServices.Round.PlayerAt(1);
            var near = GameServices.Round.PlayerAt(2);
            var edge = GameServices.Round.PlayerAt(3);
            var challenges = Settings.SettingsStore.Current.AbilityChallenges;
            var saved = challenges.ToArray();
            var output = new System.Text.StringBuilder("brand,radius,pulses,peak_stagger,edge_hit,caster_hit,charges\n");
            var pulseCounts = new List<int>();
            var holds = new List<float>();
            try
            {
                challenges.RemoveAll(row => row.VariantId == "phaister.1.brand");
                challenges.Add(new AbilityChallengeProgress { VariantId = "phaister.1.brand", Count = 999 });
                foreach (bool brand in new[] { false, true })
                {
                    caster.Intent.Clear(); caster.Intent.Parked = false;
                    caster.Teleport(new Vector3(0, .12f, -8)); caster.transform.rotation = Quaternion.identity;
                    near.Teleport(new Vector3(.7f, .12f, -2.5f)); edge.Teleport(new Vector3(1.9f, .12f, -2.5f));
                    near.ClearStun(); edge.ClearStun(); caster.ClearStun();
                    caster.AbilitySystem.BindHero("phaister", new HeroBuild { HeroId = "phaister",
                        Slot1VariantId = brand ? "phaister.1.brand" : "phaister.1.hex" });
                    Assert.AreEqual(brand, caster.AbilitySystem.HasVariant("phaister.1.brand"));
                    caster.Intent.AimPoint = new Vector3(0, .12f, 6); caster.Intent.FaceAimPoint = true;
                    caster.Intent.Set(Verb.Skill1, true);
                    yield return new WaitForSeconds(.7f);
                    Assert.IsEmpty(Object.FindObjectsByType<HeroHazards.HexSigilComponent>(FindObjectsSortMode.None),
                        "Holding Hex cast it before release.");
                    caster.Intent.Set(Verb.Skill1, false);
                    float start = Time.time, previous = 0, peak = 0;
                    int pulses = 0; bool edgeHit = false, casterHit = false;
                    HeroHazards.HexSigilComponent zone = null;
                    while (Time.time - start < 2.15f)
                    {
                        if (zone == null) zone = Object.FindFirstObjectByType<HeroHazards.HexSigilComponent>();
                        float left = near.StunLeft;
                        if (left > previous + .08f) pulses++;
                        previous = left; peak = Mathf.Max(peak, left);
                        edgeHit |= edge.IsStunned; casterHit |= caster.IsStunned;
                        yield return null;
                    }
                    Assert.IsNotNull(zone, "The released Hex did not create its real field.");
                    Assert.AreEqual(brand ? 1.44f : 2.4f, zone.Radius, .001f);
                    Assert.AreEqual(!brand, edgeHit, "The variant's actual affected area is wrong.");
                    Assert.False(casterHit);
                    Assert.AreEqual(1, caster.AbilitySystem.Kit.Skill1.ChargesRemaining);
                    output.AppendLine(FormattableString.Invariant($"{brand},{zone.Radius},{pulses},{peak},{edgeHit},{casterHit},{caster.AbilitySystem.Kit.Skill1.ChargesRemaining}"));
                    pulseCounts.Add(pulses); holds.Add(peak);
                    Object.Destroy(zone.gameObject); caster.AbilitySystem.ResetKit();
                    yield return new WaitForSeconds(.6f);
                }
                File.WriteAllText("Logs/phaister-hex-variants.csv", output.ToString());
                Assert.GreaterOrEqual(pulseCounts[0], 2, "Default Hex did not repeat while a target stayed inside.");
                Assert.Greater(pulseCounts[1], pulseCounts[0], "Slow Brand did not pulse more often.");
                Assert.Greater(holds[1], holds[0] + .08f, "Slow Brand did not strengthen the stumble.");
            }
            finally { challenges.Clear(); challenges.AddRange(saved); }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator BothBlinkVariantsWaitForReleaseAndShoveOnlyAtDeparture()
        {
            yield return PreparePlacementCourt();
            var caster = GameServices.Round.PlayerAt(1);
            var near = GameServices.Round.PlayerAt(2);
            var arrival = GameServices.Round.PlayerAt(3);
            var challenges = Settings.SettingsStore.Current.AbilityChallenges;
            var saved = challenges.ToArray();
            var output = new System.Text.StringBuilder("stride,travel,departure_travel,arrival_travel,cooldown\n");
            try
            {
                challenges.RemoveAll(row => row.VariantId == "phaister.2.stride");
                challenges.Add(new AbilityChallengeProgress { VariantId = "phaister.2.stride", Count = 999 });
                foreach (bool stride in new[] { false, true })
                {
                    float distance = stride ? 7.15f : 5.5f;
                    var start = new Vector3(0, .12f, -8);
                    var nearStart = start + Vector3.right;
                    var arrivalStart = start + Vector3.forward * distance + Vector3.right;
                    caster.Teleport(start); caster.transform.rotation = Quaternion.identity;
                    near.Teleport(nearStart); arrival.Teleport(arrivalStart);
                    foreach (var player in new[] { caster, near, arrival }) { player.ClearStun(); player.ClearTrip(); }
                    caster.AbilitySystem.BindHero("phaister", new HeroBuild { HeroId = "phaister",
                        Slot2VariantId = stride ? "phaister.2.stride" : "phaister.2.blink" });
                    Assert.AreEqual(stride, caster.AbilitySystem.HasVariant("phaister.2.stride"));
                    caster.Intent.Clear(); caster.Intent.Parked = false;
                    caster.Intent.AimPoint = start + Vector3.forward * 20; caster.Intent.FaceAimPoint = true;
                    caster.Intent.Set(Verb.Skill2, true);
                    yield return new WaitForSeconds(1);
                    Assert.Less(Mathf.Abs(caster.transform.position.z - start.z), .03f, "Blink teleported before release.");
                    caster.Intent.Set(Verb.Skill2, false);
                    yield return new WaitForSeconds(.3f);
                    float travel = caster.transform.position.z - start.z;
                    float departureTravel = Vector2.Distance(new Vector2(nearStart.x, nearStart.z),
                        new Vector2(near.transform.position.x, near.transform.position.z));
                    float arrivalTravel = Vector2.Distance(new Vector2(arrivalStart.x, arrivalStart.z),
                        new Vector2(arrival.transform.position.x, arrival.transform.position.z));
                    Assert.AreEqual(distance, travel, .15f, "Actual Blink reach differs from its equipped variant.");
                    Assert.Greater(departureTravel, .25f, "Blink did not shove the person left behind.");
                    Assert.Less(arrivalTravel, .05f, "Blink incorrectly shoved a person at arrival.");
                    Assert.False(arrival.IsStunned);
                    Assert.Greater(caster.AbilitySystem.Kit.Skill2.CooldownRemaining, 50);
                    output.AppendLine(FormattableString.Invariant($"{stride},{travel},{departureTravel},{arrivalTravel},{caster.AbilitySystem.Kit.Skill2.CooldownRemaining}"));
                    caster.AbilitySystem.ResetKit();
                    yield return new WaitForSeconds(.6f);
                }
                File.WriteAllText("Logs/phaister-blink-variants.csv", output.ToString());
            }
            finally { challenges.Clear(); challenges.AddRange(saved); }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RestoredCovenKeepsItsClockWithoutSpendingOrResolvingHits()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var player in GameServices.Round.Players) { player.Intent.Clear(); player.Intent.Parked = true; }
            var caster = GameServices.Round.PlayerAt(1);
            var victim = GameServices.Round.PlayerAt(2);
            caster.Teleport(new Vector3(0, .12f, -8));
            victim.Teleport(new Vector3(1, .12f, -8));
            victim.ClearStun(); victim.ClearTrip();
            caster.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "phaister");
            caster.AbilitySystem.BindHero("phaister");
            var kit = (PhaisterHeroKit)caster.AbilitySystem.Kit;
            kit.AddUltimateCharge(3.25f);
            NetAuthority.Provider = new ReturningClient();
            float movement = caster.Stamina.SpeedZones.Value;

            kit.RestoreCoven(caster, new Vector3(0, 0, -8), 0, .6f);
            kit.RestoreCoven(caster, new Vector3(0, 0, -8), 0, .55f);
            Assert.True(kit.Ultimate.IsActive);
            Assert.AreEqual(.6f, kit.Ultimate.DurationRemaining, .001f);
            Assert.AreEqual(3.25f, kit.UltimateCharge, .001f);
            Assert.AreEqual(1, Object.FindObjectsByType<HeroHazards.CovenCircleBuild>(FindObjectsSortMode.None).Length);
            yield return new WaitForSeconds(.8f);
            Assert.False(kit.Ultimate.IsActive);
            Assert.IsNull(GameObject.Find("GrandCovenEclipseEffect"));
            Assert.False(victim.IsStunned, "A returning client resolved a curse outcome.");

            kit.RestoreCoven(caster, new Vector3(0, 0, -8), .3f, 7);
            Assert.True(kit.Ultimate.IsWindingUp);
            Assert.AreEqual(0, caster.Stamina.SpeedZones.Value, .001f);
            yield return new WaitForSeconds(.45f);
            Assert.True(kit.Ultimate.IsActive);
            Assert.AreEqual(movement, caster.Stamina.SpeedZones.Value, .001f);
            Assert.AreEqual(3.25f, kit.UltimateCharge, .001f);
            Assert.False(victim.IsStunned, "Resumed preparation resolved a hit on its client.");
            caster.AbilitySystem.ResetKit();
            yield return null;
            Assert.IsNull(GameObject.Find("GrandCovenEclipseEffect"));
            Assert.AreEqual(movement, caster.Stamina.SpeedZones.Value, .001f);
        }

        [UnityTest]
        public IEnumerator SkySnapshotResumesTheLatestLookAndItsExistingAge()
        {
            SkyEvent.Play(SkyEvent.Look.Eclipse, 10);
            SkyEvent.Play(SkyEvent.Look.Stormfront, 7);
            var live = Object.FindFirstObjectByType<SkyEvent>();
            live.StepTo(2);
            var ambient = RenderSettings.ambientSkyColor;
            Assert.True(SkyEvent.CaptureTimeline(out var look, out float age, out float lifetime));
            Assert.AreEqual(SkyEvent.Look.Stormfront, look);
            SkyEvent.StopAll();
            yield return null;
            SkyEvent.RestoreTimeline(look, age, lifetime);
            Assert.AreEqual(1, Object.FindObjectsByType<SkyEvent>(FindObjectsSortMode.None).Length);
            Assert.True(SkyEvent.CaptureTimeline(out var restoredLook, out float restoredAge, out float restoredLife));
            Assert.AreEqual(look, restoredLook);
            Assert.AreEqual(age, restoredAge, .001f);
            Assert.AreEqual(lifetime, restoredLife, .001f);
            Assert.Less(Vector4.Distance(ambient, RenderSettings.ambientSkyColor), .001f);
            Object.FindFirstObjectByType<SkyEvent>().StepTo(lifetime + .01f);
            yield return null;
            Assert.False(SkyEvent.CaptureTimeline(out _, out _, out _));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RecordRitualConstructionAndFirstFrameCosts()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay, GameMode.HeroStrike);
            var output = new System.Text.StringBuilder("sample,construction_ms,first_frame_delta,renderers,vertices,projection_ms,mesh_sha256,materials_ms,script_arcs_ms,tick_arcs_ms,phase_material_sha256\n");
            for (int sample = 0; sample < 3; sample++)
            {
                using var projection = Unity.Profiling.ProfilerRecorder.StartNew(
                    Unity.Profiling.ProfilerCategory.Scripts, "TUMP.DrapeGround", 4);
                using var materials = Unity.Profiling.ProfilerRecorder.StartNew(
                    Unity.Profiling.ProfilerCategory.Scripts, "TUMP.VfxGhostMaterial", 4);
                using var scripts = Unity.Profiling.ProfilerRecorder.StartNew(
                    Unity.Profiling.ProfilerCategory.Scripts, "TUMP.CovenScriptArcs", 4);
                using var ticks = Unity.Profiling.ProfilerRecorder.StartNew(
                    Unity.Profiling.ProfilerCategory.Scripts, "TUMP.CovenTickArcs", 4);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                var effect = HeroHazards.SpawnGrandCovenEclipse(Vector3.zero, 10.5f, 7,
                    PhaisterHeroKit.RitualBuildSeconds);
                clock.Stop();
                int renderers = effect.GetComponentsInChildren<Renderer>().Length;
                int vertices = 0;
                foreach (var filter in effect.GetComponentsInChildren<MeshFilter>())
                    if (filter.sharedMesh != null) vertices += filter.sharedMesh.vertexCount;
                yield return null;
                Assert.True(projection.Valid && projection.Count > 0, "The projection timer did not record.");
                var geometry = new System.Text.StringBuilder();
                foreach (var filter in effect.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == null) continue;
                    geometry.Append(filter.name);
                    foreach (var vertex in filter.sharedMesh.vertices)
                        geometry.Append(FormattableString.Invariant($"|{vertex.x:R},{vertex.y:R},{vertex.z:R}"));
                    foreach (int index in filter.sharedMesh.triangles) geometry.Append('|').Append(index);
                }
                using var sha = System.Security.Cryptography.SHA256.Create();
                string fingerprint = BitConverter.ToString(sha.ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(geometry.ToString()))).Replace("-", "").ToLowerInvariant();
                var phases = new System.Text.StringBuilder();
                var circle = effect.GetComponentInChildren<HeroHazards.CovenCircleBuild>();
                foreach (float phase in new[] { 0f, .4f, .9f, 1.55f, 2.2f, 8.2f })
                {
                    circle.StepTo(phase);
                    foreach (var renderer in circle.GetComponentsInChildren<Renderer>())
                    {
                        var color = renderer.sharedMaterial.color;
                        var emission = renderer.sharedMaterial.GetColor("_EmissionColor");
                        phases.Append(FormattableString.Invariant(
                            $"|{phase}:{renderer.name}:{color.r:R},{color.g:R},{color.b:R},{color.a:R}:{emission.r:R},{emission.g:R},{emission.b:R}"));
                    }
                }
                string materialFingerprint = BitConverter.ToString(sha.ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(phases.ToString()))).Replace("-", "").ToLowerInvariant();
                output.AppendLine(FormattableString.Invariant(
                    $"{sample},{clock.Elapsed.TotalMilliseconds},{Time.deltaTime},{renderers},{vertices},{projection.LastValue / 1000000.0},{fingerprint},{materials.LastValue / 1000000.0},{scripts.LastValue / 1000000.0},{ticks.LastValue / 1000000.0},{materialFingerprint}"));
                Assert.IsNotNull(effect.transform.Find("EclipseReach"));
                Object.Destroy(effect);
                yield return null;
                Assert.IsNull(GameObject.Find("GrandCovenEclipseEffect"));
                yield return new WaitForSeconds(.2f);
            }
            File.WriteAllText("Logs/phaister-construction-cost.csv", output.ToString());
            Debug.Log(output.ToString());
        }
    }
}
