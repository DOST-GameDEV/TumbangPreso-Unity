using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// ⚠️⚠️ CAN-CARRY-1024: THE RESET IS A REAL CARRY NOW, FOR EVERY HERO.
    ///
    /// 🧑 2026-10-24: *"show can being raised up and put in the right spot by defender ... animate them carrying it and
    /// walking back"*, *"dapat hold nalang nila ung raise can button, kasama na ung movement sa holding"*, *"they drop
    /// it"*, *"siguro normal speed"*, *"apparent that the person carries the can and puts it back in right place in all
    /// aspects (fpp view etc)"*, *"code it so that it works in network ... seen by everyone"*.
    ///
    /// The taya holds Grab beside the downed, not hot can:
    ///  * PICKUP (<see cref="PickupSeconds"/>): a crouch, and the can rises from where it lay into both hands;
    ///  * CARRY: the body walks itself to the mark at normal walking speed while the button is held, the stick ignored
    ///    (`CharacterMotor.CarryWish`, on whichever peer simulates the body, like `FleeWish`);
    ///  * SET-DOWN (<see cref="SetDownSeconds"/>): a crouch-and-press at the mark, then <see cref="HostRestore"/>.
    /// Letting go, any stun (`CanAct`), Whirled, or leaving the taya role drops the can at their feet, still down. A body on
    /// the mark that blocks the walk within the old reset's reach gets the can planted from there; an ultimate's shared
    /// pause holds the carry where it is.
    ///
    /// ⚠️ THE HOST OWNS IT. Carrier slot, phase and phase age live here and travel on `SyncLata` (tag
    /// <c>MatchRpc.CarrySuffix</c>, protocol 179). The host and solo reach the same `Host*` methods from
    /// `Carrier.StepDefender`; a client's own taya sends START and CANCEL (`MatchRpc.HostApplyResetPhase`) and predicts
    /// the pickup locally until the host answers. The host alone decides the set-down, off the carrier's own synced
    /// position, and alone stands the can up.
    ///
    /// ⚠️ THE DRAWING IS EVERY PEER'S OWN. The mesh is put in the carrier's hands from the carrier's synced body
    /// (`ApplyCarryPresentation`), the way the raise already tilts the mesh and never the transform. On the host the
    /// transform follows the carrier too, so the pose stream, the off-screen arrow and a drop all agree where it is.
    /// </summary>
    public sealed partial class Lata
    {
        public enum CarryPhase : byte { None = 0, Pickup = 1, Carry = 2, SetDown = 3 }

        /// <summary>Seconds of the crouch that brings the can up into the hands.</summary>
        public const float PickupSeconds = .35f;
        /// <summary>Seconds of the crouch-and-press that plants it on the mark.</summary>
        public const float SetDownSeconds = .3f;
        /// <summary>How far in front of the carrier's feet the carried can is held, metres (flat).</summary>
        public const float HoldReach = .42f;
        /// <summary>How high the carried can's BASE is held above the carrier's feet in their own first person, metres (see `HeldBaseHeightFor`).</summary>
        public const float HoldBaseHeight = .78f;
        /// <summary>
        /// The host's arrival test: the carrier's feet within <see cref="HoldReach"/> plus this of the mark. The walking
        /// body stops at <see cref="HoldReach"/> plus <see cref="WalkStopSlack"/>, so a synced position a little behind
        /// still arrives.
        /// </summary>
        public const float ArriveSlack = .35f;
        public const float WalkStopSlack = .05f;
        /// <summary>Logs every carry drop and why (probes and tests switch it on).</summary>
        public static bool LogCarry;
        /// <summary>A walk that has made no headway for this long is stuck on something; the host drops the can.</summary>
        public const float StuckSeconds = 2.5f;
        /// <summary>A walk blocked this long within <c>Balance.InteractionRadius</c> of the mark sets the can down from there.</summary>
        public const float BlockedSetDownSeconds = .35f;
        /// <summary>A client's predicted pickup the host has not confirmed in this long is abandoned.</summary>
        private const float PredictionTimeout = .6f;

        /// <summary>The seat carrying the can, or -1.</summary>
        public int CarrierSlot { get; private set; } = -1;
        public CarryPhase Carrying { get; private set; }
        /// <summary>Seconds in the current carry phase.</summary>
        public float CarryAge { get; private set; }
        public bool IsCarried => CarrierSlot >= 0 && Carrying != CarryPhase.None;
        public bool IsCarriedBy(int slot) => IsCarried && CarrierSlot == slot;

        /// <summary>The carrying body itself, kept from the start of the carry (a seat lookup can miss a body not in the round's list).</summary>
        private CharacterMotor _carrierBody;
        private CharacterMotor CarrierBody
            => _carrierBody != null && _carrierBody.PlayerSlot == CarrierSlot ? _carrierBody : GameServices.Round?.PlayerAt(CarrierSlot);

        private float _carryStartDistance = 1.0f;
        private float _carryBestDistance, _carryStuckFor;
        private bool _carryPredicted;
        private float _carryPredictedAt;
        // Where the downed mesh lay when the pickup began, so the rise starts from it on every peer.
        private Vector3 _pickupFromPos;
        private Quaternion _pickupFromRot = Quaternion.identity;
        private bool _pickupFromKnown;
        private float _lastCarryDrawnAt = -100.0f;
        private Collider[] _carryColliders;
        private bool _carryCollidersOff;

        /// <summary>The flat distance from the carrier's feet to the mark, or 0 with no carrier.</summary>
        public float CarryDistanceLeft(CharacterMotor who)
        {
            if (who == null) return 0.0f;
            Vector3 a = who.transform.position, b = _mark; a.y = b.y = 0.0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>The mark the can is carried back to.</summary>
        public Vector3 Mark => _mark;

        /// <summary>
        /// 0 to 1, how far through the whole reset this seat is, for the HUD, the clock and the readouts: the pickup fills
        /// the first fifth, the walk the next two thirds by distance covered, the set-down the rest.
        /// </summary>
        public float CarryProgressFor(int slot)
        {
            if (!IsCarriedBy(slot)) return 0.0f;
            switch (Carrying)
            {
                case CarryPhase.Pickup: return Mathf.Lerp(.02f, .2f, Mathf.Clamp01(CarryAge / PickupSeconds));
                case CarryPhase.Carry:
                {
                    var who = CarrierBody;
                    float walk = Mathf.Max(0.0f, _carryStartDistance - HoldReach);
                    float left = Mathf.Max(0.0f, CarryDistanceLeft(who) - HoldReach);
                    float done = walk > .01f ? 1.0f - Mathf.Clamp01(left / walk) : 1.0f;
                    return Mathf.Lerp(.2f, .85f, done);
                }
                case CarryPhase.SetDown: return Mathf.Lerp(.85f, 1.0f, Mathf.Clamp01(CarryAge / SetDownSeconds));
            }
            return 0.0f;
        }

        /// <summary>
        /// The raise's own shape (`Visual.CanRaiseShape`) for this seat's body and first-person arms: the pickup runs 0 to
        /// 0.84 (crouch, grip, lift), the carry holds 0.84 (standing, can in both hands), the set-down runs 0.85 to 1
        /// (the press). 0 when this seat is not carrying.
        /// </summary>
        public float RaiseShapeFor(int slot)
        {
            if (!IsCarriedBy(slot)) return 0.0f;
            switch (Carrying)
            {
                case CarryPhase.Pickup: return Mathf.Lerp(.01f, .84f, Mathf.Clamp01(CarryAge / PickupSeconds));
                case CarryPhase.Carry: return .84f;
                case CarryPhase.SetDown: return Mathf.Lerp(.85f, 1.0f, Mathf.Clamp01(CarryAge / SetDownSeconds));
            }
            return 0.0f;
        }

        /// <summary>True while this seat's body should walk itself to the mark (the CARRY phase only).</summary>
        public bool CarryWalks(int slot) => IsCarriedBy(slot) && Carrying == CarryPhase.Carry;

        /// <summary>True while this seat's stick is ignored: the whole carry, pickup to set-down.</summary>
        public bool CarryHolds(int slot) => IsCarriedBy(slot);

        /// <summary>
        /// Every condition for STARTING a carry, the same on the host and solo: a taya who can act, beside a downed can
        /// that is neither hot nor being kept by Nemu's Lookout.
        /// </summary>
        public bool MayStartCarry(CharacterMotor who)
        {
            if (who == null || _isUpright) return false;
            if (!who.IsDefender || !who.CanAct() || who.IsWhirled) return false;
            if (Abilities.NemuLookout.Keeping(this)) return false;
            if (IsHot) return false;   // Sean's MOLTEN THROW: too hot to start a reset on.
            if (PracticeRange.CanIsFrozen(this)) return false;
            Vector3 a = who.transform.position, b = transform.position; a.y = b.y = 0.0f;
            return Vector3.Distance(a, b) <= Balance.InteractionRadius;
        }

        /// <summary>Every condition for KEEPING a carry going once it has started (not the distance: it walks).</summary>
        private bool MayKeepCarry(CharacterMotor who)
            => who != null && !_isUpright && who.IsDefender && who.CanAct() && !who.IsWhirled
               && !Abilities.NemuLookout.Keeping(this);

        /// <summary>Host-side. Starts the pickup for this seat if it may; a second START from the carrier is a no-op.</summary>
        public bool HostBeginCarry(int slot) => HostBeginCarry(GameServices.Round?.PlayerAt(slot));

        /// <summary>Host-side, with the body in hand (the solo and host path from `Carrier`).</summary>
        public bool HostBeginCarry(CharacterMotor who)
        {
            if (!NetAuthority.ShouldResolve() || who == null) return false;
            int slot = who.PlayerSlot;
            if (IsCarriedBy(slot)) return true;
            if (IsCarried) return false;
            if (!MayStartCarry(who)) return false;
            _carrierBody = who;
            BeginCarryState(slot, CarryPhase.Pickup, 0.0f);
            _carryStartDistance = Mathf.Max(HoldReach, CarryDistanceLeft(who));
            _carryBestDistance = _carryStartDistance;
            _carryStuckFor = 0.0f;
            NetCue.PlayVaried("pickup", transform.position, 0.92f, 1.0f, 0.85f);
            Net.MatchRpc.Instance?.BroadcastLataState();
            return true;
        }

        /// <summary>Host-side. The carrier let go (or may no longer carry): the can drops at their feet, still down.</summary>
        public void HostDropCarry(int slot)
        {
            if (!NetAuthority.ShouldResolve() || !IsCarriedBy(slot)) return;
            DropCarry();
        }

        private void DropCarry()
        {
            var who = CarrierBody;
            if (LogCarry) Debug.Log($"[Carry] drop seat {CarrierSlot} in {Carrying} at {CarryAge:0.00}s");
            bool lifted = Carrying != CarryPhase.Pickup || CarryAge > PickupSeconds * .5f;
            ClearCarryState();
            if (lifted && who != null)
            {
                // At their feet, in front, on the ground under that spot, lying as it lay.
                Vector3 at = who.transform.position + FlatForward(who) * HoldReach;
                at = AIController.ClampToPlayable(at);
                float ground = Visual.VfxShapes.TryGroundUnder(at, 1.0f, 4.0f, out float g) ? g : _mark.y;
                transform.position = new Vector3(at.x, ground + DownedLift * Mathf.Abs(Mathf.Sin(_toppleAngle * Mathf.Deg2Rad)), at.z);
                _lastRollPosition = transform.position;
                _groundY = ground;
            }
            NetCue.PlayImpact("can_knockdown", "lata_impact", transform.position, .35f);
            OnCarryDropPresented();
            Net.MatchRpc.Instance?.BroadcastLataState();
        }

        private void BeginCarryState(int slot, CarryPhase phase, float age)
        {
            bool fresh = !IsCarriedBy(slot);
            CarrierSlot = slot;
            Carrying = phase;
            CarryAge = age;
            if (!fresh) return;
            // Picked up while still going over: the topple finishes now, so it cannot fight the carry for the transform.
            if (_toppleTimer > 0.0f) { _toppleTimer = 0.0f; _toppleAngle = Balance.DownedTiltDeg; ApplyTilt(_toppleAngle); }
            NotePickupFrom();
        }

        private void ClearCarryState()
        {
            CarrierSlot = -1;
            _carrierBody = null;
            Carrying = CarryPhase.None;
            CarryAge = 0.0f;
            _carryPredicted = false;
            _pickupFromKnown = false;
            SetCarryColliders(false);
        }

        /// <summary>
        /// The owning client's prediction: its own taya pressed Grab beside the can and sent START, so the pickup plays at
        /// once rather than a round trip later. The host's next snapshot confirms or clears it.
        /// </summary>
        public void PredictCarry(CharacterMotor who)
        {
            if (NetAuthority.ShouldResolve() || IsCarried || who == null) return;
            _carrierBody = who;
            BeginCarryState(who.PlayerSlot, CarryPhase.Pickup, 0.0f);
            _carryPredicted = true;
            _carryPredictedAt = Time.time;
            _carryStartDistance = Mathf.Max(HoldReach, CarryDistanceLeft(who));
        }

        /// <summary>The owning client let go of a predicted or confirmed carry: drop the local picture now.</summary>
        public void UnpredictCarry(int slot)
        {
            if (NetAuthority.ShouldResolve() || !IsCarriedBy(slot)) return;
            ClearCarryState();
            OnCarryDropPresented();
        }

        /// <summary>A client takes the host's carry from `SyncLata`.</summary>
        public void ApplyCarrySnapshot(int slot, byte phase, float age)
        {
            if (NetAuthority.ShouldResolve()) return;
            if (!float.IsFinite(age)) age = 0.0f;
            var p = phase <= (byte)CarryPhase.SetDown ? (CarryPhase)phase : CarryPhase.None;
            if (slot < 0 || p == CarryPhase.None || _isUpright)
            {
                // A predicted pickup the host has not heard of yet survives a snapshot sent before it arrived.
                if (_carryPredicted && Time.time - _carryPredictedAt < PredictionTimeout) return;
                if (IsCarried) { ClearCarryState(); if (!_isUpright) OnCarryDropPresented(); }
                return;
            }
            bool wasMine = IsCarriedBy(slot);
            if (!wasMine)
            {
                var who = GameServices.Round?.PlayerAt(slot);
                _carrierBody = who;
                _carryStartDistance = Mathf.Max(HoldReach, CarryDistanceLeft(who) + (p == CarryPhase.Pickup ? 0.0f : HoldReach));
            }
            _carryPredicted = false;
            // A phase that has not changed keeps the local clock (it is already ahead of a packet); a new one takes the host's.
            // Nor does it step back: a replica that walked its own pickup on into the carry ignores a late pickup packet.
            if (wasMine && (byte)p <= (byte)Carrying) return;
            BeginCarryState(slot, p, Mathf.Max(0.0f, age));
        }

        /// <summary>Every peer: ages the phase; the host also checks it may continue and moves it on.</summary>
        private void StepCarry(float dt)
        {
            if (!IsCarried) { SetCarryColliders(false); return; }
            SetCarryColliders(true);
            // An ultimate's shared pause holds the whole world, the walking body included (`CharacterMotor.FixedUpdate`):
            // the carry pauses with it rather than reading the blocked input as a stun and dropping the can.
            if (PresentationClock.BlocksInput) return;
            CarryAge += dt;
            if (!NetAuthority.ShouldResolve())
            {
                if (_carryPredicted && Time.time - _carryPredictedAt > PredictionTimeout) { ClearCarryState(); OnCarryDropPresented(); return; }
                // The replica walks the pickup on into the carry itself; only the host says when it is set down.
                if (Carrying == CarryPhase.Pickup && CarryAge >= PickupSeconds) { Carrying = CarryPhase.Carry; CarryAge = 0.0f; }
                return;
            }

            var who = CarrierBody;
            if (!MayKeepCarry(who))
            {
                if (LogCarry) Debug.Log($"[Carry] may not keep: body {(who != null)} defender {who?.IsDefender} act {who?.CanAct()} whirled {who?.IsWhirled}");
                DropCarry(); return;
            }

            switch (Carrying)
            {
                case CarryPhase.Pickup:
                    if (CarryAge < PickupSeconds) return;
                    Carrying = CarryPhase.Carry; CarryAge = 0.0f;
                    if (CarryDistanceLeft(who) <= HoldReach + ArriveSlack) { Carrying = CarryPhase.SetDown; }
                    Net.MatchRpc.Instance?.BroadcastLataState();
                    return;

                case CarryPhase.Carry:
                {
                    // The transform rides with the carrier, so the pose stream and every arrow point at the can.
                    Vector3 held = who.transform.position + FlatForward(who) * HoldReach;
                    transform.position = new Vector3(held.x, who.transform.position.y + DownedLift * Mathf.Abs(Mathf.Sin(_toppleAngle * Mathf.Deg2Rad)), held.z);
                    _lastRollPosition = transform.position;
                    float left = CarryDistanceLeft(who);
                    if (left <= HoldReach + ArriveSlack)
                    {
                        Carrying = CarryPhase.SetDown; CarryAge = 0.0f;
                        Net.MatchRpc.Instance?.BroadcastLataState();
                        return;
                    }
                    if (left < _carryBestDistance - .05f) { _carryBestDistance = left; _carryStuckFor = 0.0f; }
                    else if ((_carryStuckFor += dt) > BlockedSetDownSeconds && left <= Balance.InteractionRadius)
                    {
                        // Something stands on the mark (a body, most often): within the old reset's own reach the taya
                        // leans in and plants it rather than walk into them for ever.
                        Carrying = CarryPhase.SetDown; CarryAge = 0.0f;
                        Net.MatchRpc.Instance?.BroadcastLataState();
                    }
                    else if (_carryStuckFor > StuckSeconds)
                    {
                        if (LogCarry) Debug.Log($"[Carry] stuck {left:0.00} m from the mark {_mark} at {who.transform.position}: move {who.CanMove()} walks {CarryWalks(who.PlayerSlot)} holds {who.IsCarryingCan} sim {who.IsLocallySimulated()} enabled {who.enabled}; at the mark: "
                            + string.Join(", ", System.Array.ConvertAll(Physics.OverlapSphere(_mark, .6f, ~0, QueryTriggerInteraction.Ignore), c => $"{c.name}/{c.GetType().Name}@{c.bounds.center}")));
                        DropCarry();
                    }
                    return;
                }

                case CarryPhase.SetDown:
                    if (CarryAge < SetDownSeconds) return;
                    ClearCarryState();
                    _lastCarryDrawnAt = Time.time;
                    HostRestore();
                    Net.MatchRpc.Instance?.BroadcastLataState();
                    return;
            }
        }

        private static Vector3 FlatForward(CharacterMotor who)
        {
            Vector3 f = who.transform.forward; f.y = 0.0f;
            return f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
        }

        /// <summary>Where the downed mesh lies at the start of the pickup, so it rises from there.</summary>
        private void NotePickupFrom()
        {
            if (!ResolveRaiseMesh()) { _pickupFromKnown = false; return; }
            // `Update` has put the mesh back at rest by now, so this is the can as it lies.
            _pickupFromPos = _raiseMesh.position;
            _pickupFromRot = _raiseMesh.rotation;
            _pickupFromKnown = true;
        }

        /// <summary>
        /// A carried can is not a body in the road: its colliders are off so the carrier does not walk into the can in
        /// their own hands and nothing else bumps it. Back on the moment the carry ends.
        /// </summary>
        private void SetCarryColliders(bool off)
        {
            if (off == _carryCollidersOff) return;
            _carryCollidersOff = off;
            if (_carryColliders == null || off) _carryColliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in _carryColliders)
                if (c != null && !c.isTrigger) c.enabled = !off;
        }

        /// <summary>A dropped can clatters where it falls, on every peer (the same three hops as a knockdown).</summary>
        private void OnCarryDropPresented()
        {
            _hopLeft = 0.0f;
            _clatterLeft = Settings.SettingsStore.Current.ReducedUiMotion ? 0.0f : ClatterSeconds * .6f;
            _clatterSpinSign = -_clatterSpinSign;
        }

        /// <summary>
        /// The drawn can in the carrier's hands. False when nobody is carrying it (the other presentations run instead).
        /// Pickup: from where it lay up into the hands; carry: in both hands in front of the chest, upright; set-down: from
        /// the hands down onto the mark.
        /// </summary>
        private bool ApplyCarryPresentation()
        {
            if (!IsCarried || _isUpright) return false;
            var who = CarrierBody;
            if (who == null || !ResolveRaiseMesh()) return false;
            if (!_pickupFromKnown) NotePickupFrom();

            // From the rest pose, never on top of a pose already applied (that would save the held pose as the rest).
            RestoreRaisePresentation();
            _raiseMeshPos = _raiseMesh.localPosition; _raiseMeshRot = _raiseMesh.localRotation; _raiseMeshScale = _raiseMesh.localScale;
            _raiseApplied = true;

            // The mesh's pose relative to the can's own origin, so the held can sits on its base like the real one.
            Quaternion rootInv = Quaternion.Inverse(transform.rotation);
            Vector3 relPos = rootInv * (_raiseMesh.position - transform.position);
            Quaternion relRot = rootInv * _raiseMesh.rotation;

            Vector3 forward = FlatForward(who);
            Quaternion upright = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 heldBase = who.transform.position + forward * HoldReach + Vector3.up * HeldBaseHeightFor(who);
            // A little bob with the walk, so it reads as held and not glued.
            float bob = Carrying == CarryPhase.Carry ? .018f * Mathf.Sin(Time.time * 11.0f) : 0.0f;
            Vector3 heldPos = heldBase + Vector3.up * bob + upright * relPos;
            Quaternion heldRot = upright * relRot;

            Vector3 pos; Quaternion rot;
            switch (Carrying)
            {
                case CarryPhase.Pickup:
                {
                    float u = Mathf.Clamp01(CarryAge / PickupSeconds);
                    // It stays on the road while the hands close on it, then comes up with the body's lift.
                    float lift = Visual.CanRaiseShape.Rise(Mathf.Lerp(0.0f, .84f, u));
                    Vector3 from = _pickupFromKnown ? _pickupFromPos : _raiseMesh.position;
                    Quaternion fromRot = _pickupFromKnown ? _pickupFromRot : _raiseMesh.rotation;
                    pos = Vector3.Lerp(from, heldPos, lift) + Vector3.up * (.06f * Mathf.Sin(lift * Mathf.PI));
                    rot = Quaternion.Slerp(fromRot, heldRot, lift);
                    break;
                }
                case CarryPhase.SetDown:
                {
                    float u = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(CarryAge / SetDownSeconds));
                    Vector3 onMark = _mark + upright * relPos;
                    pos = Vector3.Lerp(heldPos, onMark, u);
                    rot = heldRot;
                    break;
                }
                default:
                    pos = heldPos; rot = heldRot;
                    break;
            }
            _raiseMesh.position = pos;
            _raiseMesh.rotation = rot;
            _lastCarryDrawnAt = Time.time;
            return true;
        }

        /// <summary>
        /// How high the carried can's base is drawn above the carrier's feet. ⚠️ TWO ANSWERS, ONE PER KIND OF VIEW. In the
        /// carrier's own first person it is <see cref="HoldBaseHeight"/>, just under the eye, where `ViewmodelArms` grips
        /// it. Every other view draws it in the body's real hands (their palm, less half the can): at that first-person
        /// height the can sat inside the big head and the hands were seen carrying nothing (film, 2026-10-24).
        /// </summary>
        private float HeldBaseHeightFor(CharacterMotor who)
        {
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig != null && rig.IsLocalFpp && rig.IsFollowing(who)) return HoldBaseHeight;
            var palm = who.GetComponent<Visual.CharacterVisual>()?.HandAnchor;
            if (palm == null) return HandsBaseHeight;
            return Mathf.Clamp(palm.position.y - who.transform.position.y - HeldGripAboveBase, .1f, HoldBaseHeight);
        }

        /// <summary>The base height in the hands when the body has no palm to read, metres.</summary>
        public const float HandsBaseHeight = .32f;
        /// <summary>Where the hands hold the can, measured up from its base (about half its height), metres.</summary>
        private const float HeldGripAboveBase = .17f;

        /// <summary>True when the can was in someone's hands a moment ago, so its stand-up is the set-down's own landing.</summary>
        private bool CarriedJustNow => Time.time - _lastCarryDrawnAt < .5f;
    }
}
