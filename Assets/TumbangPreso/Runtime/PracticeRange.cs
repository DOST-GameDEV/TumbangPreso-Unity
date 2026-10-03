using System.Collections;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    // An explicit local training session, never inferred from transport teardown.
    public sealed class PracticeRange : MonoBehaviour
    {
        public enum Cheat { NoCooldowns, InfiniteSkills, FullUltimate, InfiniteStamina, FreezeCan }
        public static PracticeRange Instance { get; private set; }
        public static bool Requested => PracticeRangeRules.Allowed(GameLaunch.TrainingRange,
            NetAuthority.IsNetworked, UI.SceneFlow.Networked, MatchAbandon.AuthorityRevoked,
            GameLaunch.GuidedTutorial, GameLaunch.Spectator || GameLaunch.AllBots);
        public static bool Active => Requested && Instance != null && Instance._ready && Instance.isActiveAndEnabled;
        // Menu configuration remains available while offline pause blocks world input.
        // Shared presentation phases still own actor/model state until their release.
        public bool CanEdit => Active && Instance == this
            && !(PresentationClock.Held && !SharedUltimatePhase.Collecting)
            && !SharedUltimatePhase.BlocksActions;
        public CharacterMotor Local { get; private set; }
        public bool InfiniteSkills { get; private set; }
        public bool FullUltimate { get; private set; }
        public bool InfiniteStamina { get; private set; }
        public bool FreezeCan { get; private set; }
        private bool _ready, _resetting;
        private Lata _lata;
        private CharacterMotor[] _seats;
        private Slipper[] _slippers;
        private SliceRunner _runner;
        private readonly bool[] _idle = new bool[Balance.PlayerCount];

        public void Configure(CharacterMotor local, Lata lata, CharacterMotor[] seats, Slipper[] slippers, SliceRunner runner)
        {
            if (!Requested || local == null || lata == null || seats == null || slippers == null || runner == null
                || seats.Length != Balance.PlayerCount || slippers.Length != Balance.PlayerCount)
            { enabled = false; return; }
            Instance = this; Local = local; _lata = lata; _seats = seats; _slippers = slippers; _runner = runner;
            // Build once behind loading; absent targets are not registered or simulated.
            _runner.Seats = (CharacterMotor[])seats.Clone();
            for (int slot = 0; slot < seats.Length; slot++)
            {
                _idle[slot] = true;
                if (seats[slot] == null || seats[slot] == local) continue;
                var brain = seats[slot].GetComponent<AIController>(); if (brain != null) brain.enabled = false;
                seats[slot].Intent.Parked = true; seats[slot].RoundActive = false;
                seats[slot].gameObject.SetActive(false); _runner.Seats[slot] = null;
                GameServices.Round?.Unregister(seats[slot]);
            }
            UI.PausePanel.PrepareTraining(FindFirstObjectByType<PauseWatcher>());
            StartCoroutine(Begin());
        }

        private IEnumerator Begin()
        {
            yield return null;
            if (!Requested || Local == null) yield break;
            _ready = true; _runner.Begin();
            SetDefender((Local.PlayerSlot + 1) % Balance.PlayerCount);
        }

        public bool Value(Cheat cheat) => cheat switch
        {
            Cheat.NoCooldowns => PracticeSandbox.Wanted,
            Cheat.InfiniteSkills => InfiniteSkills,
            Cheat.FullUltimate => FullUltimate,
            Cheat.InfiniteStamina => InfiniteStamina,
            Cheat.FreezeCan => FreezeCan,
            _ => false
        };

        public bool Set(Cheat cheat, bool value)
        {
            if (!CanEdit) return false;
            switch (cheat)
            {
                case Cheat.NoCooldowns: PracticeSandbox.Wanted = value; break;
                case Cheat.InfiniteSkills: InfiniteSkills = value; break;
                case Cheat.FullUltimate: FullUltimate = value; break;
                case Cheat.InfiniteStamina: InfiniteStamina = value; break;
                case Cheat.FreezeCan: FreezeCan = value; break;
                default: return false;
            }
            return true;
        }

        public static bool RefillsAbilities(CharacterMotor actor) => Active && actor == Instance.Local
            && (PracticeSandbox.Active || Instance.InfiniteSkills || Instance.FullUltimate);
        public static bool CanIsFrozen(Lata can) => Active && Instance._lata == can && Instance.FreezeCan && !Instance._resetting;

        public bool ChangeCharacter(int index)
        {
            if (!CanEdit || Local == null) return false;
            var roster = Roster.GetPeople(Local.Mode);
            if (index < 0 || index >= roster.Count) return false;
            var art = RosterBook.Load()?.PersonArt(index, Local.Mode);
            if (art?.Model == null) return false;
            Local.AbilitySystem?.ResetKitForMatch();
            Local.ClearStun(); Local.ClearTrip(); Local.ClearStatuses();
            Local.CharacterIndex = index;
            if (Local.AbilitySystem != null)
            { Local.AbilitySystem.SpeaksAsHero = true; Local.AbilitySystem.BindHero(roster[index].Id); }
            Local.GetComponent<Visual.CharacterVisual>()?.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            return true;
        }

        public bool BotPresent(int seat) => IsBotSeat(seat) && _seats[seat].gameObject.activeSelf;
        public bool BotIdle(int seat) => IsBotSeat(seat) && _idle[seat];
        private bool IsBotSeat(int seat) => _seats != null && seat >= 0 && seat < _seats.Length
            && _seats[seat] != null && _seats[seat] != Local;

        public bool SetBot(int seat, bool present, bool idle)
        {
            if (!CanEdit || !IsBotSeat(seat)) return false;
            var body = _seats[seat];
            if (present && idle && !_idle[seat] && body.gameObject.activeSelf)
            {
                // Retire windups before parking publishes a release to their consumers.
                body.GetComponent<Carrier>()?.CancelPendingInput();
                body.GetComponent<CombatVerbs>()?.CancelPendingInput();
                body.AbilitySystem?.ClearPresentationInput();
            }
            _idle[seat] = idle;
            var brain = body.GetComponent<AIController>();
            if (brain != null) brain.enabled = present && !idle;
            body.Intent.Parked = idle || !present;
            if (present && !body.gameObject.activeSelf)
            {
                body.gameObject.SetActive(true);
                body.ClearStun(); body.ClearTrip(); body.ClearStatuses(); body.AbilitySystem?.ResetKitForMatch();
                body.IsDefender = seat == GameServices.Match.DefenderSlot; body.RoundActive = true;
                body.SpawnPosition = SliceRunner.SpawnPointFor(seat, GameServices.Match.DefenderSlot);
                body.Teleport(body.SpawnPosition); body.Stamina.RefillAndClearFatigue();
                Vector3 facing = -new Vector3(body.SpawnPosition.x, 0, body.SpawnPosition.z);
                body.transform.rotation = facing.sqrMagnitude > .0001f
                    ? Quaternion.LookRotation(facing, Vector3.up) : Quaternion.identity;
                MatchHost.SeatOnFloor(body);
                _runner.Seats[seat] = body; GameServices.Round.Register(body);
                EquipSeat(seat);
            }
            else if (!present)
            {
                _slippers[seat]?.HostDisarm();
                if (_slippers[seat] != null) _slippers[seat].gameObject.SetActive(false);
                body.AbilitySystem?.ResetKitForMatch(); body.RoundActive = false;
                GameServices.Round.Unregister(body); _runner.Seats[seat] = null;
                body.gameObject.SetActive(false);
            }
            return true;
        }

        public bool SetDefender(int seat)
        {
            if (!CanEdit || seat < 0 || seat >= Balance.PlayerCount) return false;
            int[] scores = new int[Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = GameServices.Match.ScoreFor(i);
            // Like guided training, change the round identity so taya stays derived.
            GameServices.Match.ApplySnapshot(scores, seat + 1, true);
            GameServices.Round.ApplySnapshot(UI.SceneFlow.SelectedRoundSeconds, true, seat);
            return ResetRange();
        }

        public bool ResetRange()
        {
            if (!CanEdit) return false;
            _resetting = true;
            try
            {
                foreach (var body in _runner.Seats)
                {
                    if (body == null) continue;
                    // Reset teleports a live body, so its old contact origin is no longer valid.
                    body.GetComponent<Carrier>()?.CancelPendingInput();
                    body.GetComponent<CombatVerbs>()?.RetireActions();
                }
                _runner.ResetWorld(GameServices.Match.DefenderSlot);
                foreach (var body in _runner.Seats)
                {
                    if (body == null) continue;
                    body.ClearStun(); body.ClearTrip(); body.ClearStatuses(); body.AbilitySystem?.ResetKitForMatch();
                    body.RoundActive = true;
                    body.Intent.Parked = body == Local ? UI.Panel.AnyOpen : _idle[body.PlayerSlot];
                }
                EquipActiveSeats();
                return true;
            }
            finally { _resetting = false; }
        }

        private void EquipActiveSeats()
        { for (int seat = 0; seat < _slippers.Length; seat++) EquipSeat(seat); }

        private void EquipSeat(int seat)
        {
            var slipper = _slippers[seat]; if (slipper == null) return;
            var body = _runner.Seats[seat];
            slipper.HostDisarm();
            bool attacker = body != null && !body.IsDefender;
            slipper.OwnerSlot = attacker ? seat : -1; slipper.gameObject.SetActive(attacker);
            if (attacker) slipper.HostForceEquip(body);
        }

        private void Update()
        {
            if (!Active || PresentationClock.Held) return;
            if (InfiniteStamina && Local != null) Local.Stamina.RefillAndClearFatigue();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null; GameLaunch.TrainingRange = false; PracticeSandbox.Clear();
        }
    }
}
