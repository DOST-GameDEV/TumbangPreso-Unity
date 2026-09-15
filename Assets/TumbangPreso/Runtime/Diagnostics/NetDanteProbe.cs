using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in evidence from separate players. Only seat one's client presses the skill.
    [DefaultExecutionOrder(-250)]
    public sealed class NetDanteProbe : MonoBehaviour
    {
        private static bool _active;
        private string _case;
        private string _pendingHero="dante";
        private Vector3 _lastImpact;
        private float _impactHeld;
        private StreamWriter _writer;
        private bool _prepared, _testedGuard, _testedAfter;
        private double _next;
        private float _guardStarted = -1;
        private Slipper _loose;
        private bool _pendingReview,_pendingRefreshed,_expiredRefreshed;
        private bool _pendingPickSent,_pendingPickSeeded;
        private bool _latePreparationReview,_lateSnapshotSent;
        private bool _movementReview;
        private bool _lateMovementReview;
        private int _refreshSeat=2;
        private float _pendingSeenAt=-1;
        private readonly System.Collections.Generic.HashSet<UnityEngine.Object> _seenImpacts=new System.Collections.Generic.HashSet<UnityEngine.Object>();
        private static readonly System.Reflection.FieldInfo WarningField=typeof(DanteSeismicVisual).GetField("_warning",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.MethodInfo HostSnapshot=typeof(MatchRpc).GetMethod("HostSyncPeer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            _active = Argument("-tp-dantetrace") != null && !Environment.GetCommandLineArgs().Contains("-tp-tournament");
            if (!_active) return;
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            string hero=Argument("-tp-pendinghero")??"dante";
            Settings.SettingsStore.Current.CharacterPick = Roster.GetPeople(GameMode.HeroStrike)
                .Select((person, index) => (person, index)).First(pair => pair.person.Id == hero).index;
            // The named fixture profile advertises the same selected build through
            // normal pick replication. A later join must not overwrite the case.
            string scenario = Argument("-tp-dantecase") ?? "stomp";
            var build = Settings.SettingsStore.HeroBuildFor(hero);
            build.Slot1VariantId = scenario == "tremor" ? "dante.1.tremor" : "";
            build.Slot2VariantId = scenario == "plating" ? "dante.2.plating" : "";
            foreach (var id in new[] { build.Slot1VariantId, build.Slot2VariantId })
                if (!string.IsNullOrEmpty(id))
                {
                    var counters = Settings.SettingsStore.Current.AbilityChallenges;
                    counters.RemoveAll(row => row.VariantId == id);
                    counters.Add(new AbilityChallengeProgress { VariantId = id, Count = 999 });
                }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!_active) return;
            var root = new GameObject("~NetDanteProbe"); DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetDanteProbe>(); probe._case = Argument("-tp-dantecase") ?? "stomp";
            probe._pendingReview=Environment.GetCommandLineArgs().Contains("-tp-dantepending-review");
            probe._latePreparationReview=Environment.GetCommandLineArgs().Contains("-tp-pendinglate-review");
            probe._movementReview=Environment.GetCommandLineArgs().Contains("-tp-movement-review");
            probe._lateMovementReview=Environment.GetCommandLineArgs().Contains("-tp-movementlate-review");
            if(int.TryParse(Argument("-tp-refresh-seat"),out int refreshSeat) && refreshSeat>=1 && refreshSeat<=2)probe._refreshSeat=refreshSeat;
            probe._pendingHero=Argument("-tp-pendinghero")??"dante";
            string path = Path.GetFullPath(Argument("-tp-dantetrace")); Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer = new StreamWriter(path) { AutoFlush = true };
            probe._writer.WriteLine("time,elapsed,local,host,s1charges,s2active,s2cooldown,ultcharge,casterX,casterY,casterZ,frontX,frontY,frontZ,rearX,rearY,rearZ,frontTrip,frontStun,casterStun,ward,pillars,shoeX,shoeY,shoeZ,guardTested,afterTested,tremor,plating,orbitStones,rumble,pendingReview,pendingRefreshed,expiredRefreshed,windup,impactBirths,heroIndex,casterPick,casterMode,ultActive,ultRemaining,impactX,impactY,impactZ,impactHeld,movementReview,movementActive,movementRemaining,movementSpeed,s1cooldown,fireFields,shockFields");
        }

        private void Update()
        {
            var round = GameServices.Round;
            int dantePick=Roster.IndexIn(Roster.HeroPeople,_pendingHero);
            if(_pendingReview && NetAuthority.IsNetworked && MatchRpc.Instance!=null)
            {
                if(!_pendingPickSent && NetAuthority.LocalSlot==1)
                {
                    var settings=Settings.SettingsStore.Current;
                    MatchRpc.Instance.SelectLobbyPickServerRpc(dantePick,settings.CanPick,settings.SlipperPick);
                    _pendingPickSent=true;
                }
                if(!_pendingPickSeeded && NetAuthority.IsHost && round?.PlayerAt(1)!=null
                    && MatchRpc.Instance.GetSeatInfo(1)?.CharacterPick==dantePick)
                {
                    _pendingPickSeeded=true;MatchRpc.Instance.SyncPicksClientRpc(new[]{1,dantePick,-1,-1});
                    MatchRpc.Instance.BroadcastPicks();
                }
            }
            if (!_active || !NetAuthority.IsNetworked || round == null || !round.RoundActive || GameServices.Match == null || GameServices.Match.RoundNumber < 1) return;
            var ready = FindFirstObjectByType<ReadyGate>(); if (ready != null && ready.CountingDown) return;
            var caster = round.PlayerAt(1); var front = round.PlayerAt(2); var rear = round.PlayerAt(3);
            if (caster == null || front == null || rear == null || caster.AbilitySystem?.Kit == null) return;
            if(_pendingReview && !_prepared && (caster.CharacterIndex!=dantePick || caster.AbilitySystem.HeroId!=_pendingHero))return;
            foreach (var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var player in round.Players) if (player != null && player != caster) { player.Intent.Clear(); player.Intent.Parked = true; }
            float elapsed = UI.SceneFlow.SelectedRoundSeconds - round.TimeLeft;
            if (!_prepared)
            {
                if (NetAuthority.IsHost)
                {
                    _loose = round.PlayerAt(3).GetComponent<Carrier>().Held;
                    if (_loose == null) return;
                }
                _prepared = true;
                var build = new HeroBuild { HeroId = "dante", Slot1VariantId = _case == "tremor" ? "dante.1.tremor" : null,
                    Slot2VariantId = _case == "plating" ? "dante.2.plating" : null };
                if(!_pendingReview)caster.AbilitySystem.BindHero("dante", build);
                caster.AbilitySystem.Kit.AddUltimateCharge(100);
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 1)
                {
                    caster.Teleport(new Vector3(0, .12f, -8)); caster.transform.rotation = Quaternion.identity;
                    caster.ClearStun(); caster.ClearTrip();
                }
                // Seat two is an actual owning observer, so initialize its own
                // motor too. Its late spawn otherwise overwrites the host fixture pose.
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 2)
                    front.Teleport(_movementReview?new Vector3(8,.12f,-6):_case == "fissure" ? new Vector3(0, .12f, -5.5f) : new Vector3(1.3f, .12f, -8));
                if (NetAuthority.IsHost)
                {
                    rear.Teleport(_movementReview?new Vector3(-8,.12f,-6):new Vector3(0, .12f, -9.3f)); round.PlayerAt(0).Teleport(new Vector3(-9, .12f, -12));
                    _loose.HostDisarm();
                    var at = new Vector3(-1, 0, -8); at.y = Slipper.GroundY(at) + _loose.RestHeight;
                    _loose.transform.position = at;
                }
                Debug.Log("[DanteProbe] prepared case=" + _case + " local=" + NetAuthority.LocalSlot + " elapsed=" + elapsed);
            }
            bool guard = _case == "ward" || _case == "plating";
            if (NetAuthority.LocalSlot == 1)
            {
                caster.Intent.Parked = false;
                caster.Intent.AimPoint = _pendingHero=="zack" && elapsed>=12.75f?new Vector3(4,.1f,-7):new Vector3(0, .1f, -3); caster.Intent.FaceAimPoint = true;
                caster.Intent.Set(guard ? Verb.Skill2 : _case == "fissure" ? Verb.Ultimate : Verb.Skill1, elapsed >= 12 && elapsed < (_pendingHero=="zack"?12.7f:12.35f));
                caster.Intent.Move = guard && elapsed >= 12.5f && elapsed < 15.2f ? Vector2.up * .35f : Vector2.zero;
                if(_movementReview && elapsed>=12 && elapsed<15)caster.Intent.Move=Vector2.up*.4f;
            }
            if (guard && NetAuthority.IsHost)
            {
                if (caster.AbilitySystem.Kit.Skill2.IsActive && _guardStarted < 0) _guardStarted = elapsed;
                if (_guardStarted >= 0 && !_testedGuard && elapsed > _guardStarted + .65f)
                {
                    _testedGuard = true; caster.ApplyResolvedImpact(Vector3.right * 8 + Vector3.up * 4);
                    caster.ApplyStagger(1, StunElement.Stone, 6);
                }
                if (_testedGuard && !caster.AbilitySystem.Kit.Skill2.IsActive && !_testedAfter)
                { _testedAfter = true; caster.ApplyStagger(.8f, StunElement.Stone, 6); }
            }
            var pendingSkill=_case=="fissure"?caster.AbilitySystem.Kit.Ultimate:caster.AbilitySystem.Kit.Skill1;
            if(_pendingReview)
            {
                // A normal round trip can reach the host after this short cast
                // finishes. This opt-in boundary case captures the actual host
                // snapshot near contact, then lets the real transport delay it.
                bool ending=_lateMovementReview?pendingSkill.IsActive && pendingSkill.DurationRemaining<=.09f
                    :pendingSkill.IsWindingUp && pendingSkill.WindupRemaining<=.09f;
                if((_latePreparationReview || _lateMovementReview) && NetAuthority.IsHost && !_lateSnapshotSent && ending)
                {
                    foreach(ulong peer in NetworkManager.Singleton.ConnectedClientsIds)
                        if(NetSession.Instance.Lobby.PeerById((int)peer)?.Seat==_refreshSeat)
                        {
                            float remaining=_lateMovementReview?pendingSkill.DurationRemaining:pendingSkill.WindupRemaining;
                            Debug.Log($"[PendingCastProbe] captured near deadline remaining={remaining:F4}");
                            HostSnapshot.Invoke(MatchRpc.Instance,new object[]{(int)peer});
                            _lateSnapshotSent=true;break;
                        }
                }
                if(_pendingHero=="dante")
                {
                    foreach(var impact in FindObjectsByType<DanteSeismicVisual>())
                        if(WarningField!=null && !(bool)WarningField.GetValue(impact))
                        { _seenImpacts.Add(impact);_lastImpact=impact.transform.position;_impactHeld=pendingSkill.HeldSecondsOnCast; }
                }
                else
                {
                    var impact=GameObject.Find(_pendingHero=="cheska"?"GlacialNovaWave":"ThunderShockRing");
                    if(impact!=null){_seenImpacts.Add(impact);_lastImpact=impact.transform.position;_impactHeld=pendingSkill.HeldSecondsOnCast;}
                }
                if(NetAuthority.LocalSlot==_refreshSeat)
                {
                    bool live=_movementReview?pendingSkill.IsActive:pendingSkill.IsWindingUp;
                    if(!_pendingRefreshed && live && _pendingSeenAt<0)_pendingSeenAt=Time.realtimeSinceStartup;
                    bool during=!_pendingRefreshed && live && _pendingSeenAt>=0 && Time.realtimeSinceStartup-_pendingSeenAt>=.035f;
                    bool expired=_pendingRefreshed && !_expiredRefreshed && elapsed>=20;
                    if(during || expired)
                    {
                        if(during)_pendingRefreshed=true;else _expiredRefreshed=true;
                        // Reconstruct the observer kit and request the actual host snapshot.
                        // This is a live snapshot fixture, not a process reconnect claim.
                        caster.AbilitySystem.BindHero(_pendingHero,new HeroBuild{HeroId=_pendingHero});
                        caster.GetComponentInChildren<CharacterAnimator>()?.CancelHeroAction();
                        if(!during || (!_latePreparationReview && !_lateMovementReview))MatchRpc.Instance.RequestWorldSnapshot();
                        pendingSkill=_case=="fissure"?caster.AbilitySystem.Kit.Ultimate:caster.AbilitySystem.Kit.Skill1;
                        Debug.Log("[PendingCastProbe] refreshed "+(during?(_movementReview?"during movement":"during preparation"):"after expiry")+" at="+elapsed);
                    }
                }
            }
            double now = NetworkManager.Singleton.ServerTime.Time;
            if (now >= _next)
            {
                _next = now + .05;
                if (_loose == null) _loose = FindObjectsByType<Slipper>(FindObjectsSortMode.None).FirstOrDefault(s => s.SeatOfOrigin == 3);
                var a = caster.transform.position; var b = front.transform.position; var c = rear.transform.position;
                var shoe = _loose != null ? _loose.transform.position : Vector3.zero;
                var orbit=caster.transform.Find("DanteOrbitingWard");
                var cameraRig=Camera.main!=null?Camera.main.GetComponent<CameraSystem.CameraRig>():null;
                object[] row = { now, elapsed, NetAuthority.LocalSlot, NetAuthority.IsHost ? 1 : 0,
                    caster.AbilitySystem.Kit.Skill1.ChargesRemaining, caster.AbilitySystem.Kit.Skill2.IsActive ? 1 : 0,
                    caster.AbilitySystem.Kit.Skill2.CooldownRemaining, caster.AbilitySystem.Kit.UltimateCharge,
                    a.x, a.y, a.z, b.x, b.y, b.z, c.x, c.y, c.z, front.IsTripped ? 1 : 0, front.StunLeft, caster.StunLeft,
                    caster.GetComponentsInChildren<DanteCarapaceVisual>().Length,
                    FindObjectsByType<DanteFissurePillar>(FindObjectsSortMode.None).Length,
                    shoe.x, shoe.y, shoe.z, _testedGuard ? 1 : 0, _testedAfter ? 1 : 0,
                    caster.AbilitySystem.HasVariant("dante.1.tremor") ? 1 : 0,
                    caster.AbilitySystem.HasVariant("dante.2.plating") ? 1 : 0,
                    orbit!=null?orbit.childCount:0,cameraRig!=null?cameraRig.GroundRumbleOffset.magnitude:0,
                    _pendingReview?1:0,_pendingRefreshed?1:0,_expiredRefreshed?1:0,pendingSkill.WindupRemaining,_seenImpacts.Count,
                    Roster.IndexIn(Roster.HeroPeople,caster.AbilitySystem.HeroId),caster.CharacterIndex,(int)caster.Mode,caster.AbilitySystem.Kit.Ultimate.IsActive?1:0,
                    caster.AbilitySystem.Kit.Ultimate.DurationRemaining,_lastImpact.x,_lastImpact.y,_lastImpact.z,_impactHeld,
                    _movementReview?1:0,caster.AbilitySystem.Kit.Skill1.IsActive?1:0,caster.AbilitySystem.Kit.Skill1.DurationRemaining,
                    caster.AbilitySystem.Kit.MovementSpeedScale,caster.AbilitySystem.Kit.Skill1.CooldownRemaining,
                    _movementReview?FindObjectsByType<HeroHazards.FireTrailComponent>(FindObjectsSortMode.None).Count(f=>f.OwnerSlot==1):0,
                    _movementReview?FindObjectsByType<HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None).Count(f=>f.OwnerSlot==1):0 };
                _writer.WriteLine(string.Join(",", row.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))));
            }
            if (elapsed > 24) { _writer.Flush(); Application.Quit(); }
        }

        private void OnDestroy() => _writer?.Dispose();
    }
}
