using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Accepted forward earthquake. It starts at the cutscene's cut (the cutscene is his charge-up and ends on the slam): five
    /// bands erupt from his fists down the lane, `GeoRules.DriftInterval` apart. Only the host resolves: each band throws
    /// everyone standing in it down the lane (a carry), and they are Concussed as they land (`GeoRules.DriftLandSeconds`).
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
        public int ReleasedBands => _nextBand;
        int _nextBand, _announced;
        readonly System.Collections.Generic.List<(CharacterMotor actor, float lands)> _thrown = new System.Collections.Generic.List<(CharacterMotor, float)>(4);
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

        /// <summary>Whether a point stands in the lane (the five bands together).</summary>
        public static bool InLane(Vector3 origin, Vector3 forward, float halfWidth, float reach, Vector3 point)
        {
            forward.y = 0; forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            Vector3 delta = point - origin, right = Vector3.Cross(Vector3.up, forward);
            float along = Vector3.Dot(delta, forward), across = Vector3.Dot(delta, right);
            return along >= 0 && along <= reach && Mathf.Abs(across) <= halfWidth;
        }

        /// <param name="age">Where the cascade already is: `GeoRules.DriftCutsceneTail` after his cutscene (its last frame drew
        /// the first band rising), 0 for a cast without one.</param>
        public static DanteDriftWave Spawn(Vector3 origin, Vector3 forward, int owner, float age = 0f)
        {
            forward.y = 0;
            forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            Lane(origin, forward, out float width, out float reach);
            return Create(origin, forward, owner, width, reach, Mathf.Max(0f, age), false);
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
            // Concussed as they land.
            for (int i = _thrown.Count - 1; i >= 0; i--)
            {
                if (Age < _thrown[i].lands) continue;
                var actor = _thrown[i].actor;
                _thrown.RemoveAt(i);
                if (GameServices.Round?.RoundActive == true && actor != null && actor.gameObject.activeInHierarchy) actor.ApplyConcussed();
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
                bool already = false;
                foreach (var t in _thrown) if (t.actor == actor) already = true;
                if (already) continue;   // thrown on into the next band: one throw per wave
                // Knocked away down the lane, away from him; Concussed as they land.
                actor.ApplyResolvedCarry(Forward * GeoRules.DriftThrowAway + Vector3.up * GeoRules.DriftThrowUp, GeoRules.DriftThrowHold);
                _thrown.Add((actor, band * GeoRules.DriftInterval + GeoRules.DriftLandSeconds));   // from its own blast
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
