using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>Accepted forward earthquake. Only the host resolves each timed band.</summary>
    public sealed class DanteDriftWave : MonoBehaviour
    {
        public Vector3 Origin { get; private set; }
        public Vector3 Forward { get; private set; }
        public int OwnerSlot { get; private set; }
        public float HalfWidth { get; private set; }
        public float Reach { get; private set; }
        public float Age { get; private set; }
        public float Remaining => Mathf.Max(0, GeoRules.DriftSeconds - Age);
        public int ReleasedBands => _nextBand;
        int _nextBand, _announced;
        DanteDriftVisual _visual;

        public static DanteDriftWave Spawn(Vector3 origin, Vector3 forward, int owner)
        {
            forward.y = 0;
            forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float reach = .1f, width = .1f;
            foreach (var corner in new[] {
                new Vector3(AIController.PlayableMinX, 0, AIController.PlayableMinZ),
                new Vector3(AIController.PlayableMinX, 0, AIController.PlayableMaxZ),
                new Vector3(AIController.PlayableMaxX, 0, AIController.PlayableMinZ),
                new Vector3(AIController.PlayableMaxX, 0, AIController.PlayableMaxZ) })
            {
                Vector3 delta = corner - origin; delta.y = 0;
                reach = Mathf.Max(reach, Vector3.Dot(delta, forward));
                width = Mathf.Max(width, Mathf.Abs(Vector3.Dot(delta, right)));
            }
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
            // A snapshot describes outcomes already resolved. Never replay old blasts.
            wave._nextBand = restored ? Mathf.Min(GeoRules.DriftBlasts,
                Mathf.FloorToInt(age / GeoRules.DriftInterval) + 1) : 0;
            wave._announced = restored ? (1 << Balance.PlayerCount) - 1 : 0;
            wave._visual = DanteDriftVisual.Build(go.transform, origin, forward, width, reach);
            wave.Advance(0);
            return wave;
        }

        void Update() => Advance(Time.deltaTime);

        internal void Advance(float dt)
        {
            Age += Mathf.Max(0, dt);
            while (_nextBand < GeoRules.DriftBlasts && Age >= _nextBand * GeoRules.DriftInterval)
            {
                ResolveBand(_nextBand);
                _nextBand++;
            }
            if (_visual != null) _visual.StepTo(Age);
            if (Age >= GeoRules.DriftSeconds) { gameObject.SetActive(false); Destroy(gameObject); }
        }

        void ResolveBand(int band)
        {
            var round = GameServices.Round;
            if (!NetAuthority.ShouldResolve() || round?.RoundActive != true) return;
            Vector3 right = Vector3.Cross(Vector3.up, Forward);
            float depth = Reach / GeoRules.DriftBlasts;
            float start = band * depth, end = (band + 1) * depth;
            foreach (var actor in round.Players)
            {
                if (actor == null || !actor.gameObject.activeInHierarchy || actor.PlayerSlot == OwnerSlot) continue;
                Vector3 delta = actor.transform.position - Origin;
                float along = Vector3.Dot(delta, Forward), across = Vector3.Dot(delta, right);
                if (along < start || along > end || (band > 0 && along == start) || Mathf.Abs(across) > HalfWidth) continue;
                actor.ApplyConcussed();
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
