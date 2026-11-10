using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // Four bounded camera slots, separate from authority and input. Local captures
    // come from actual active rigs; remote captures come only through the host relay.
    public static class ObservedPlayerView
    {
        public const float FreshSeconds = .6f;
        private const float PlaybackDelay = .14f;
        private const int BufferedFrames = 5;
        private struct LocalView { public CameraRig Rig; }
        private struct RemoteView { public SpectatorViewState State; public float At; public int Index, Count; }
        private struct RemoteFrame { public SpectatorViewState State; public float At; }
        private struct Gesture { public CharacterMotor Actor; public string Name; public float At; public uint Sequence; }
        private static readonly LocalView[] Local = new LocalView[Core.Balance.PlayerCount];
        private static readonly RemoteView[] Remote = new RemoteView[Core.Balance.PlayerCount];
        private static readonly RemoteFrame[,] Buffer = new RemoteFrame[Core.Balance.PlayerCount, BufferedFrames];
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
                var previous = Remote[target.PlayerSlot]; previous.Count = 0;
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
            => InstallRemoteAt(state, actor, Time.unscaledTime);

        internal static bool InstallRemoteAt(SpectatorViewState state, CharacterMotor actor, float now)
        {
            if (!state.IsValid || !state.Matches(MatchIdentity, GameServices.Match?.RoundNumber ?? 0, actor)) return false;
            var previous = Remote[state.Slot];
            bool sameScope = previous.State.Match == state.Match && previous.State.Round == state.Round && previous.State.Epoch == state.Epoch;
            if (sameScope && previous.State.Sequence != 0 && !SpectatorViewState.Newer(state.Sequence, previous.State.Sequence)) return false;
            int count = sameScope && now - previous.At <= FreshSeconds ? previous.Count : 0;
            int index = count == 0 ? 0 : (previous.Index + 1) % BufferedFrames;
            Buffer[state.Slot, index] = new RemoteFrame { State = state, At = now };
            Remote[state.Slot] = new RemoteView { State = state, At = now, Index = index,
                Count = Mathf.Min(BufferedFrames, count + 1) };
            return true;
        }
        public static bool TryGet(CharacterMotor actor, out SpectatorViewState state, out bool local)
        {
            local = TryLocal(actor, out state);
            if (local) return true;
            if (actor == null) return false;
            if (actor.IsBot && NetAuthority.ShouldResolve())
            { state = BotPose(actor); local = true; return state.PoseIsValid; }
            if (ValidSlot(actor.PlayerSlot))
            {
                var entry = Remote[actor.PlayerSlot];
                if (entry.Count > 0 && Time.unscaledTime - entry.At <= FreshSeconds
                    && entry.State.Matches(MatchIdentity, GameServices.Match?.RoundNumber ?? 0, actor))
                { state = entry.State; return true; }
            }
            if (!NetAuthority.IsNetworked) { state = BotPose(actor); local = true; return state.PoseIsValid; }
            return false;
        }

        // Play between arrivals instead of easing to one held pose and stopping every packet.
        // The small buffer is viewer-only and never blends across a body epoch or a camera cut.
        internal static SpectatorViewState SampleRemote(SpectatorViewState latest, float now)
        {
            var entry = Remote[latest.Slot];
            float at = now - PlaybackDelay;
            if (entry.Count < 2 || at >= entry.At) return latest;
            var newer = Buffer[latest.Slot, entry.Index];
            for (int i = 1; i < entry.Count; i++)
            {
                var older = Buffer[latest.Slot, (entry.Index + BufferedFrames - i) % BufferedFrames];
                if (at >= older.At)
                {
                    var from = older.State;
                    var to = newer.State;
                    const byte viewKind = SpectatorViewState.FirstPerson | SpectatorViewState.BotView;
                    if (((from.Flags ^ to.Flags) & viewKind) != 0 || (from.Eye - to.Eye).sqrMagnitude > 64)
                        return from;
                    float t = Mathf.InverseLerp(older.At, newer.At, at);
                    from.Eye = Vector3.Lerp(from.Eye, to.Eye, t);
                    from.Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t);
                    from.Fov = Mathf.Lerp(from.Fov, to.Fov, t);
                    from.EyeDrop = Mathf.Lerp(from.EyeDrop, to.EyeDrop, t);
                    from.HandsSeat = Vector3.Lerp(from.HandsSeat, to.HandsSeat, t);
                    from.HandsRotation = Quaternion.Slerp(from.HandsRotation, to.HandsRotation, t);
                    return from;
                }
                newer = older;
            }
            return newer.State;
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
        { System.Array.Clear(Remote, 0, Remote.Length); System.Array.Clear(Buffer, 0, Buffer.Length); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { System.Array.Clear(Local, 0, Local.Length); System.Array.Clear(Gestures, 0, Gestures.Length); ClearRemote(); Target = null; }
        private static bool ValidSlot(int slot) => slot >= 0 && slot < Core.Balance.PlayerCount;
    }
}
