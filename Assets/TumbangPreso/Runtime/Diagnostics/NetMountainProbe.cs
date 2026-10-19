using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// MOUNTAIN FORM ACROSS REAL PEERS (opt-in, `-tp-mountaintrace <csv>`; `tools/net_mountain_matrix.py` runs it). Every peer
    /// plays Dante. Seat one's own client presses CONTINENTAL DRIFT at 12 s (the shared cutscene plays), then the host tries to
    /// stun and concuss him inside the form, then seat one throws at seat two (a remote human, beside him, out of the lane and
    /// away from the can) so the rock's stagger and launch travel to the peer that owns that body. A fourth process may join
    /// while the form runs. Every peer writes what it draws: the form's clock, the suit and its age, his clip set, the immunity,
    /// the hit. Presentation and rules are read, never changed, apart from the fixture's placement and the host's test stun.
    /// </summary>
    [DefaultExecutionOrder(-250)]
    public sealed class NetMountainProbe : MonoBehaviour
    {
        private const float CastAt = 12f;
        private static bool _active;
        private StreamWriter _writer;
        private bool _prepared, _immunityTested, _thrown;
        private float _formSeenAt = -1f, _throwPressedAt = -1f;
        private double _next;

        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            _active = Argument("-tp-mountaintrace") != null && !Environment.GetCommandLineArgs().Contains("-tp-tournament");
            if (!_active) return;
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            Settings.SettingsStore.Current.CharacterPick = Roster.GetPeople(GameMode.HeroStrike)
                .Select((person, index) => (person, index)).First(pair => pair.person.Id == "dante").index;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!_active) return;
            var root = new GameObject("~NetMountainProbe"); DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetMountainProbe>();
            string path = Path.GetFullPath(Argument("-tp-mountaintrace")); Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer = new StreamWriter(path) { AutoFlush = true };
            probe._writer.WriteLine("time,elapsed,local,host,hero,model,mountainLeft,immune,casterStun,casterConcussed,armour,armourAge,armourEnd," +
                                    "armourShown,debris,variant,ultcharge,casterX,casterY,casterZ,targetX,targetY,targetZ,targetStun,held,heldStone," +
                                    "immunityTested,thrown,shoeState,shoeX,shoeY,shoeZ");
        }

        private void Update()
        {
            var round = GameServices.Round;
            if (!_active || !NetAuthority.IsNetworked || round == null || !round.RoundActive || GameServices.Match == null || GameServices.Match.RoundNumber < 1) return;
            var ready = FindFirstObjectByType<ReadyGate>(); if (ready != null && ready.CountingDown) return;
            var caster = round.PlayerAt(1); var target = round.PlayerAt(2);
            if (caster == null || target == null || caster.AbilitySystem?.Kit == null) return;
            foreach (var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var player in round.Players) if (player != null && player != caster) { player.Intent.Clear(); player.Intent.Parked = true; }
            float elapsed = UI.SceneFlow.SelectedRoundSeconds - round.TimeLeft;
            var dante = caster.AbilitySystem.Kit as DanteHeroKit;

            if (!_prepared && elapsed < CastAt - 2f)
            {
                _prepared = true;
                caster.AbilitySystem.Kit.AddUltimateCharge(100);
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 1)
                { caster.Teleport(new Vector3(0, .12f, -8)); caster.transform.rotation = Quaternion.identity; caster.ClearStun(); caster.ClearTrip(); }
                // Beside him, out of the cascade's lane: it reaches nobody, so the throw's target is clean.
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 2) target.Teleport(new Vector3(5.5f, .12f, -8));
                if (NetAuthority.IsHost) round.PlayerAt(0).Teleport(new Vector3(-9, .12f, -12));
                Debug.Log("[MountainProbe] prepared local=" + NetAuthority.LocalSlot + " elapsed=" + elapsed);
            }
            if (dante != null && dante.IsMountainForm && _formSeenAt < 0) { _formSeenAt = elapsed; Debug.Log("[MountainProbe] form seen at " + elapsed); }

            if (NetAuthority.LocalSlot == 1)
            {
                caster.Intent.Parked = false; caster.Intent.FaceAimPoint = true; caster.Intent.Move = Vector2.zero;
                bool throwing = _formSeenAt >= 0 && elapsed >= _formSeenAt + 5f;
                caster.Intent.AimPoint = throwing ? target.transform.position + Vector3.up : new Vector3(0, .1f, -3);
                caster.Intent.Set(Verb.Ultimate, elapsed >= CastAt && elapsed < CastAt + .35f && _formSeenAt < 0);
                // The throw: turned to the body beside him, held for 1 s and let go (net-v1: a 0.45 s lob from a body still
                // turning landed 4 m out, short of it).
                bool hold = throwing && !_thrown && caster.GetComponent<Carrier>()?.Held != null;
                if (hold && _throwPressedAt < 0)
                {
                    _throwPressedAt = elapsed;
                    var toward = target.transform.position - caster.transform.position; toward.y = 0f;
                    caster.transform.rotation = Quaternion.LookRotation(toward.normalized, Vector3.up);
                }
                bool pressing = hold && elapsed < _throwPressedAt + 1f;
                if (_throwPressedAt >= 0 && !pressing && !_thrown) { _thrown = true; Debug.Log("[MountainProbe] released throw at " + elapsed); }
                caster.Intent.Set(Verb.SpecialAbility, pressing);
            }
            if (NetAuthority.IsHost && !_immunityTested && dante != null && dante.IsMountainForm && _formSeenAt >= 0 && elapsed >= _formSeenAt + 2.5f)
            {
                _immunityTested = true;
                caster.ApplyStagger(1.2f, StunElement.Stone, 6); caster.ApplyConcussed();
                caster.ApplyResolvedImpact(Vector3.right * 8f + Vector3.up * 3f);
                Debug.Log("[MountainProbe] host tested immunity: stun=" + caster.StunLeft + " concussed=" + caster.IsConcussed);
            }

            double now = NetworkManager.Singleton.ServerTime.Time;
            if (now < _next) return;
            _next = now + .05;
            var armour = caster.GetComponentInChildren<DanteMountainArmour>(true);
            int shown = 0;
            if (armour != null && armour.Renderers != null)
                foreach (var r in armour.Renderers) if (r != null && r.transform.lossyScale.sqrMagnitude > 1e-8f) shown++;
            var debris = GameObject.Find("DanteMountainDebris");
            var animator = caster.GetComponentInChildren<CharacterAnimator>();
            var held = caster.GetComponent<Carrier>()?.Held;
            var shoe = FindObjectsByType<Slipper>(FindObjectsSortMode.None).FirstOrDefault(s => s.SeatOfOrigin == 1);
            var a = caster.transform.position; var b = target.transform.position; var s = shoe != null ? shoe.transform.position : Vector3.zero;
            object[] row =
            {
                now, elapsed, NetAuthority.LocalSlot, NetAuthority.IsHost ? 1 : 0, caster.AbilitySystem.HeroId == "dante" ? 1 : 0,
                caster.GetComponent<CharacterVisual>()?.Model != null ? 1 : 0, dante != null ? dante.MountainLeft : -1f,
                caster.AbilitySystem.IsImmuneToStuns ? 1 : 0, caster.StunLeft, caster.IsConcussed ? 1 : 0,
                armour != null ? 1 : 0, armour != null ? armour.Age : -1f, armour != null ? armour.EndAt : -1f, shown,
                debris != null ? debris.transform.childCount : 0, animator != null && animator.ClipVariant == CharacterAnimator.MountainVariant ? 1 : 0,
                caster.AbilitySystem.Kit.UltimateCharge, a.x, a.y, a.z, b.x, b.y, b.z, target.StunLeft,
                held != null ? 1 : 0, DanteBoulderCoating.MountainHeld(held) ? 1 : 0, _immunityTested ? 1 : 0, _thrown ? 1 : 0,
                shoe != null ? (int)shoe.State : -1, s.x, s.y, s.z,
            };
            _writer.WriteLine(string.Join(",", row.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))));
            if (_formSeenAt >= 0 && elapsed > _formSeenAt + GeoRules.MountainSeconds + 6f) { _writer.Flush(); Application.Quit(); }
            else if (_formSeenAt < 0 && elapsed > CastAt + 20f) { Debug.Log("[MountainProbe] the form never began"); _writer.Flush(); Application.Quit(); }
        }

        private void OnDestroy() => _writer?.Dispose();
    }
}
