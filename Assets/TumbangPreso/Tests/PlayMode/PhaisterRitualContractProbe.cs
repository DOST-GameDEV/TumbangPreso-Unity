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
    }
}
