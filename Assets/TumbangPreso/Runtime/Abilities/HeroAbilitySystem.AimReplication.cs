using System;
using TumbangPreso.Net;
using Unity.Collections;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class HeroAbilitySystem
    {
        private const float AimLeaseSeconds = .75f;
        private AbilityAimSnapshot _networkAim;
        private HeroAbility _networkAimAbility;
        private float _networkAimAt, _networkAimUntil;
        private int _networkAimEpoch = -1;
        private readonly long[] _closedAimTokens = new long[3];
        private readonly long[] _seenAimTokens = new long[3];
        private uint _aimSequence;
        private readonly long[] _aimTokens = new long[3];
        private HeroAbility _bodyAim;

        private bool UsesNetworkAim => NetAuthority.IsNetworked && _motor != null && !_motor.IsLocallySimulated();

        private void BeginAimToken(Slot slot)
        {
            if (++_aimSequence == 0) _aimSequence = 1;
            _aimTokens[(int)slot] = AbilityAimSnapshot.MakeToken(_motor.MovementEpoch, _aimSequence);
        }

        private long AimTokenFor(Slot slot)
        {
            if (_motor == null || AbilityFor(slot)?.HoldToAim != true) return 0;
            if (_heldSince[(int)slot] >= 0 && !AbilityAimSnapshot.MatchesEpoch(_aimTokens[(int)slot], _motor.MovementEpoch))
                BeginAimToken(slot);
            return _aimTokens[(int)slot];
        }

        public AbilityAimSnapshot CaptureAimPresentation()
        {
            if (!isActiveAndEnabled || Kit == null || _motor == null || !_motor.CanAct() || PresentationClock.BlocksInput) return default;
            if (UsesNetworkAim)
            {
                if (!NetworkAimActive()) return default;
                var snapshot = _networkAim;
                snapshot.Held = Mathf.Clamp(snapshot.Held + Time.time - _networkAimAt, 0, 30);
                return snapshot;
            }
            for (int i = 0; i < 3; i++)
            {
                var slot = (Slot)i;
                if (!IsAiming(slot)) continue;
                var ability = AbilityFor(slot);
                return new AbilityAimSnapshot
                {
                    Slot = (byte)(i + 1), AbilityId = new FixedString64Bytes(ability.Id),
                    Held = Mathf.Clamp(HeldSeconds(slot), 0, 30), Token = AimTokenFor(slot)
                };
            }
            return default;
        }

        public void ApplyNetworkAim(AbilityAimSnapshot aim)
        {
            if (!UsesNetworkAim || !aim.IsValid) return;
            RefreshAimEpoch();
            HeroAbility ability = null;
            if (aim.Slot != 0)
            {
                ability = AbilityFor((Slot)(aim.Slot - 1));
                if (!isActiveAndEnabled || ability?.HoldToAim != true
                    || !aim.AbilityId.Equals(new FixedString64Bytes(ability.Id))
                    || !AbilityAimSnapshot.MatchesEpoch(aim.Token, _motor.MovementEpoch)
                    || aim.Token <= _closedAimTokens[aim.Slot - 1] || aim.Token < _seenAimTokens[aim.Slot - 1]) return;
                if (!_motor.CanAct() || PresentationClock.BlocksInput)
                { CloseNetworkAim(aim.Slot - 1, aim.Token); return; }
                _seenAimTokens[aim.Slot - 1] = aim.Token;
            }
            _networkAim = aim;
            _networkAimAbility = ability;
            _networkAimAt = Time.time;
            _networkAimUntil = Time.unscaledTime + AimLeaseSeconds;
            if (aim.Slot == 0) EndBodyAim();
        }

        public void CloseNetworkAim(int slot, long token)
        {
            if (slot < 0 || slot > 2 || !UsesNetworkAim || !AbilityAimSnapshot.MatchesEpoch(token, _motor.MovementEpoch)) return;
            RefreshAimEpoch();
            // Cast delivery and pose delivery can arrive out of order. Close only
            // this slot's hold through the consumed token, never a newer hold.
            _closedAimTokens[slot] = Math.Max(_closedAimTokens[slot], token);
            if (_networkAim.Slot == slot + 1 && _networkAim.Token <= _closedAimTokens[slot])
            { _networkAim = default; _networkAimAbility = null; EndBodyAim(); }
        }

        private void RefreshAimEpoch()
        {
            if (_motor == null || _networkAimEpoch == _motor.MovementEpoch) return;
            _networkAimEpoch = _motor.MovementEpoch;
            Array.Clear(_closedAimTokens, 0, _closedAimTokens.Length);
            Array.Clear(_seenAimTokens, 0, _seenAimTokens.Length);
            _networkAim = default;
            _networkAimAbility = null;
            EndBodyAim();
        }

        private bool NetworkAimActive()
        {
            RefreshAimEpoch();
            if (_networkAim.Slot == 0 || Time.unscaledTime > _networkAimUntil || !_motor.CanAct() || PresentationClock.BlocksInput) return false;
            var ability = AbilityFor((Slot)(_networkAim.Slot - 1));
            return ability != null && ability == _networkAimAbility;
        }

        private void UpdateBodyAim()
        {
            if (UsesNetworkAim && _motor != null && !_motor.CanAct() && _networkAim.Slot != 0)
                CloseNetworkAim(_networkAim.Slot - 1, _networkAim.Token);
            HeroAbility current = null;
            float held = 0;
            if (_motor != null && _motor.CanAct())
                for (int i = 0; i < 3; i++)
                    if (IsAiming((Slot)i)) { current = AbilityFor((Slot)i); held = HeldSeconds((Slot)i); break; }
            if (_bodyAim != current) EndBodyAim();
            _bodyAim = current;
            _bodyAim?.PresentAimBody(_motor, held);
        }

        private void EndBodyAim() { _bodyAim?.EndAimBody(); _bodyAim = null; }

        private void ClearAimPresentation()
        {
            if (_networkAim.Slot != 0)
                _closedAimTokens[_networkAim.Slot - 1] = Math.Max(_closedAimTokens[_networkAim.Slot - 1], _networkAim.Token);
            _networkAim = default;
            _networkAimAbility = null;
            for (int i = 0; i < _heldSince.Length; i++) _heldSince[i] = -1;
            EndOwnAim();
            EndBodyAim();
        }

        private void OnDisable() => ClearAimPresentation();
        private void OnDestroy() => ClearAimPresentation();

        private void ResetNetworkAimTransport()
        {
            ClearAimPresentation();
            _networkAimEpoch = -1;
            _aimSequence = 0;
            Array.Clear(_closedAimTokens, 0, _closedAimTokens.Length);
            Array.Clear(_seenAimTokens, 0, _seenAimTokens.Length);
            Array.Clear(_aimTokens, 0, _aimTokens.Length);
        }
    }
}
