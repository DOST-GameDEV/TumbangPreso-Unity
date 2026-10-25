using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // Four bounded camera slots, separate from authority and input. Local captures
    // come from actual active rigs; remote captures come only through the host relay.
    public static class ObservedPlayerView
    {
        public const float FreshSeconds = .6f;
        private struct LocalView { public CameraRig Rig; }
        private struct RemoteView { public SpectatorViewState State; public float At; public bool Present; }
        private struct Gesture { public CharacterMotor Actor; public string Name; public float At; public uint Sequence; }
        private static readonly LocalView[] Local = new LocalView[Core.Balance.PlayerCount];
        private static readonly RemoteView[] Remote = new RemoteView[Core.Balance.PlayerCount];
        private static readonly Gesture[] Gestures = new Gesture[Core.Balance.PlayerCount];
        public static CharacterMotor Target { get; private set; }
        public static long MatchIdentity => MatchRpc.Instance?.PresentationMatchId ?? 0;
        public static int RequestedSlot => Target != null ? Target.PlayerSlot : -1;

        public static void SetTarget(CharacterMotor target)
        {
            if (Target == target) return;
            Target = target;
            if (target != null && ValidSlot(target.PlayerSlot))
            {
                var previous = Remote[target.PlayerSlot]; previous.Present = false;
                Remote[target.PlayerSlot] = previous; // Keep its serial cursor across target cycling.
            }
        }
        public static void Capture(CameraRig rig, CharacterMotor actor)
        {
            if (rig == null || actor == null || !ValidSlot(actor.PlayerSlot) || !rig.IsFollowing(actor)
                || rig.Camera == null || !rig.Camera.enabled) return;
            var players = GameServices.Round?.Players;
            if (players != null && players.Count > 0)
            {
                bool live = false; foreach (var player in players) if (player == actor) { live = true; break; }
                if (!live) return; // A lobby/thumbnail rig must not replace the live seat's source.
            }
            Local[actor.PlayerSlot] = new LocalView { Rig = rig };
        }
        public static bool TryLocal(CharacterMotor actor, out SpectatorViewState state)
        {
            state = default;
            if (actor == null || !ValidSlot(actor.PlayerSlot)) return false;
            var entry = Local[actor.PlayerSlot];
            return entry.Rig != null && entry.Rig.IsFollowing(actor) && entry.Rig.TryObservedView(out state);
        }
        public static bool InstallRemote(SpectatorViewState state, CharacterMotor actor)
        {
            if (!state.IsValid || !state.Matches(MatchIdentity, GameServices.Match?.RoundNumber ?? 0, actor)) return false;
            var previous = Remote[state.Slot];
            bool sameScope = previous.State.Match == state.Match && previous.State.Round == state.Round && previous.State.Epoch == state.Epoch;
            if (sameScope && previous.State.Sequence != 0 && !SpectatorViewState.Newer(state.Sequence, previous.State.Sequence)) return false;
            Remote[state.Slot] = new RemoteView { State = state, At = Time.unscaledTime, Present = true };
            return true;
        }
        public static bool TryGet(CharacterMotor actor, out SpectatorViewState state, out bool local)
        {
            local = TryLocal(actor, out state);
            if (local) return true;
            if (actor == null) return false;
            if (ValidSlot(actor.PlayerSlot))
            {
                var entry = Remote[actor.PlayerSlot];
                if (entry.Present && Time.unscaledTime - entry.At <= FreshSeconds
                    && entry.State.Matches(MatchIdentity, GameServices.Match?.RoundNumber ?? 0, actor))
                { state = entry.State; return true; }
            }
            if (!NetAuthority.IsNetworked) { state = BotPose(actor); local = true; return state.PoseIsValid; }
            return false;
        }
        public static SpectatorViewState BotPose(CharacterMotor actor)
        {
            var carrier = actor.GetComponent<Carrier>();
            float squat = carrier != null && !carrier.IsCarryWalking ? Visual.CanRaiseShape.FppBend(carrier.RaiseShape) : 0;
            float kneel = Visual.PaeteGroundCall.KneelWeight(actor);
            float drop = (actor.IsPerson ? actor.ReworkEyeDropTarget : 0) + Visual.CanRaiseShape.FppEyeDrop * squat
                + Visual.PaeteGroundCall.FppEyeDrop * kneel;
            Vector3 eye = actor.transform.position + Vector3.up * ((actor.IsPerson ? SpectatorCamera.PovEyeHeightPerson : SpectatorCamera.PovEyeHeightProp) - drop);
            Vector3 look = actor.Intent != null && actor.Intent.HasAimPoint ? actor.Intent.AimPoint - eye : actor.transform.forward;
            if (look.sqrMagnitude < .0001f) look = actor.transform.forward;
            var gesture = ValidSlot(actor.PlayerSlot) ? Gestures[actor.PlayerSlot] : default;
            bool action = gesture.Actor == actor && !string.IsNullOrEmpty(gesture.Name) && Time.time - gesture.At < 4;
            var hero = actor.AbilitySystem; string aim = "";
            if (hero?.Kit != null && actor.CanAct())
            {
                if (hero.IsAiming(Abilities.HeroAbilitySystem.Slot.Skill1)) aim = hero.Kit.Skill1.ViewmodelAction;
                else if (hero.IsAiming(Abilities.HeroAbilitySystem.Slot.Skill2)) aim = hero.Kit.Skill2.ViewmodelAction;
                else if (hero.IsAiming(Abilities.HeroAbilitySystem.Slot.Ultimate)) aim = hero.Kit.Ultimate.ViewmodelAction;
                if (string.IsNullOrEmpty(aim) && actor.IsVoodooReaching) aim = actor.VoodooReachKind == VoodooMarkKind.Drain ? "reach-drain" : "reach-hex";
            }
            return new SpectatorViewState {
                Match = MatchIdentity, Round = GameServices.Match?.RoundNumber ?? 0,
                Slot = actor.PlayerSlot, Epoch = actor.MovementEpoch, Sequence = (uint)Mathf.Max(1, Time.frameCount),
                Eye = eye, Rotation = Quaternion.LookRotation(look, Vector3.up), Fov = SpectatorCamera.SpectatorFov,
                EyeDrop = Mathf.Clamp(drop, 0, 10),
                HandsSeat = CameraRig.ViewmodelSeat, HandsRotation = Quaternion.identity,
                Action = new Unity.Collections.FixedString64Bytes(action ? gesture.Name : ""),
                Aim = new Unity.Collections.FixedString64Bytes(aim ?? ""),
                ActionTime = action ? Mathf.Clamp(Time.time - gesture.At, 0, 30) : 0,
                ActionSequence = action ? gesture.Sequence : 0, VisualPhase = Time.time,
                Flags = SpectatorViewState.FirstPerson | SpectatorViewState.HandsVisible | SpectatorViewState.BotView
            };
        }
        public static void NoteAction(CharacterMotor actor, string action)
        {
            if (actor == null || !ValidSlot(actor.PlayerSlot) || string.IsNullOrEmpty(action) || action.Length > 61) return;
            var previous = Gestures[actor.PlayerSlot]; uint serial = previous.Sequence + 1; if (serial == 0) serial = 1;
            Gestures[actor.PlayerSlot] = new Gesture { Actor = actor, Name = action, At = Time.time, Sequence = serial };
        }
        public static void NoteCancel(CharacterMotor actor, string expected)
        {
            if (actor == null || !ValidSlot(actor.PlayerSlot)) return;
            var previous = Gestures[actor.PlayerSlot];
            if (previous.Actor == actor && (expected == null || expected == previous.Name))
            { previous.Name = null; Gestures[actor.PlayerSlot] = previous; }
        }
        public static void ClearRemote()
        { System.Array.Clear(Remote, 0, Remote.Length); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { System.Array.Clear(Local, 0, Local.Length); System.Array.Clear(Gestures, 0, Gestures.Length); ClearRemote(); Target = null; }
        private static bool ValidSlot(int slot) => slot >= 0 && slot < Core.Balance.PlayerCount;
    }
}
