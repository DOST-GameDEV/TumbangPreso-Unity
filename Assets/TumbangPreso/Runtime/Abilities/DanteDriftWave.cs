using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Accepted forward earthquake. It starts at the cutscene's cut: the cutscene already showed the five bands erupting and the
    /// whole lane throwing everyone on it (`Visual.DanteDriftLive`), so play does not erupt them again (owner, 2026-10-17:
    /// *"dapat cutscene transitions into game well, hindi na dapat uulit"*). Only the host resolves: at once, everyone in the
    /// lane is thrown down it (a carry), and is Concussed as they land (`GeoRules.DriftLandSeconds`).
    /// </summary>
    public sealed class DanteDriftWave : MonoBehaviour
    {
        public Vector3 Origin { get; private set; }
        public Vector3 Forward { get; private set; }
        public int OwnerSlot { get; private set; }
        public float HalfWidth { get; private set; }
        public float Reach { get; private set; }
        public float Age { get; private set; }
        public float Remaining => Mathf.Max(0, GeoRules.DriftSeconds - Age);
        public int ReleasedBands => _released ? GeoRules.DriftBlasts : 0;
        bool _released, _landed;
        int _announced;
        readonly System.Collections.Generic.List<CharacterMotor> _thrown = new System.Collections.Generic.List<CharacterMotor>(4);
        DanteDriftVisual _visual;

        /// <summary>The lane: from his feet forward to the court's far edge, as wide as the court reaches either side of him.
        /// The cutscene stages exactly this lane, and the AI reads the same.</summary>
        public static void Lane(Vector3 origin, Vector3 forward, out float halfWidth, out float reach)
        {
            forward.y = 0;
            forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            reach = .1f; halfWidth = .1f;
            foreach (var corner in new[] {
                new Vector3(AIController.PlayableMinX, 0, AIController.PlayableMinZ),
                new Vector3(AIController.PlayableMinX, 0, AIController.PlayableMaxZ),
                new Vector3(AIController.PlayableMaxX, 0, AIController.PlayableMinZ),
                new Vector3(AIController.PlayableMaxX, 0, AIController.PlayableMaxZ) })
            {
                Vector3 delta = corner - origin; delta.y = 0;
                reach = Mathf.Max(reach, Vector3.Dot(delta, forward));
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(Vector3.Dot(delta, right)));
            }
        }

        /// <summary>Whether a point stands in the lane (the five bands together): the rule the host resolves and the cutscene stages.</summary>
        public static bool InLane(Vector3 origin, Vector3 forward, float halfWidth, float reach, Vector3 point)
        {
            forward.y = 0; forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            Vector3 delta = point - origin, right = Vector3.Cross(Vector3.up, forward);
            float along = Vector3.Dot(delta, forward), across = Vector3.Dot(delta, right);
            return along >= 0 && along <= reach && Mathf.Abs(across) <= halfWidth;
        }

        public static DanteDriftWave Spawn(Vector3 origin, Vector3 forward, int owner)
        {
            forward.y = 0;
            forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            Lane(origin, forward, out float width, out float reach);
            return Create(origin, forward, owner, width, reach, 0, false);
        }

        public static DanteDriftWave Restore(WorldEffectSnapshot.Field field, float elapsed)
            => Create(field.Position, field.Forward, field.Owner, field.Radius, field.FirstScale,
                field.Duration - Mathf.Max(0, field.Remaining - elapsed), true);

        static DanteDriftWave Create(Vector3 origin, Vector3 forward, int owner,
            float width, float reach, float age, bool restored)
        {
            var go = new GameObject("DanteContinentalDrift"); go.transform.position = origin;
            var wave = go.AddComponent<DanteDriftWave>();
            wave.Origin = origin; wave.Forward = forward; wave.OwnerSlot = owner;
            wave.HalfWidth = width; wave.Reach = reach; wave.Age = age;
            // A snapshot describes outcomes already resolved. Never replay the throw.
            wave._released = restored; wave._landed = restored;
            wave._announced = restored ? (1 << Balance.PlayerCount) - 1 : 0;
            wave._visual = DanteDriftVisual.Build(go.transform, origin, forward, width, reach);
            wave.Advance(0);
            return wave;
        }

        void Update() => Advance(Time.deltaTime);

        internal void Advance(float dt)
        {
            Age += Mathf.Max(0, dt);
            if (!_released) { _released = true; Throw(); }
            if (!_landed && Age >= GeoRules.DriftLandSeconds)
            {
                _landed = true;
                if (GameServices.Round?.RoundActive == true)
                foreach (var actor in _thrown)
                    if (actor != null && actor.gameObject.activeInHierarchy) actor.ApplyConcussed();
                _thrown.Clear();
            }
            if (_visual != null) _visual.StepTo(Age);
            if (Age >= GeoRules.DriftSeconds) { gameObject.SetActive(false); Destroy(gameObject); }
        }

        void Throw()
        {
            var round = GameServices.Round;
            if (!NetAuthority.ShouldResolve() || round?.RoundActive != true) return;
            foreach (var actor in round.Players)
            {
                if (actor == null || !actor.gameObject.activeInHierarchy || actor.PlayerSlot == OwnerSlot) continue;
                if (!InLane(Origin, Forward, HalfWidth, Reach, actor.transform.position)) continue;
                // Thrown down the lane, away from him, as the cutscene threw them; Concussed as they land.
                actor.ApplyResolvedCarry(Forward * GeoRules.DriftThrowAway + Vector3.up * GeoRules.DriftThrowUp, GeoRules.DriftThrowHold);
                _thrown.Add(actor);
                int bit = 1 << actor.PlayerSlot;
                if ((_announced & bit) != 0) continue;
                _announced |= bit;
                MatchFlair.Announce(MatchFlair.Kind.UltimateImpact, OwnerSlot, actor.PlayerSlot, actor.transform.position);
            }
        }

        public WorldEffectSnapshot.Field Capture() => new WorldEffectSnapshot.Field
        {
            Type = WorldEffectSnapshot.Kind.Drift, Source = gameObject,
            Position = Origin, Forward = Forward, Owner = OwnerSlot,
            Duration = GeoRules.DriftSeconds, Remaining = Remaining,
            Radius = HalfWidth, FirstScale = Reach
        };
    }
}
