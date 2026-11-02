using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ ONE SWITCH, AND THE ONE WATCHER, FOR ILYAS'S SKIM. Off, SKIM is what it has shipped as: a thin water line round the
    /// held shoe's sole (`RafiSkimCoating`), made by the cast, and nothing in flight or on the ground. On, it is:
    ///
    ///   * THE COAT (`RafiSkimPainted`, the quick pass's water rolled round the sole, in the wall's colours now) while the
    ///     load is in the hand, KEPT ON THE SHOE WHILE IT FLIES, since that throw is the loaded one;
    ///   * THE WAKE (`RafiSkimWake`) from the moment the shoe touches the ground and slides, to the moment it rests.
    ///
    /// ⚠️ WHY A WATCHER, AND NOT THE CAST. The first pass was made by the cast (`Skim.OnActivate`) and by nothing else, and it
    /// did not show to a player who joined while a load was live, nor reliably to anybody but the caster: a joiner's restore
    /// (`RafiHeroKit.RestoreJoiningSkim`) only ever made the old line, and the coat's own test asked for the kit's private
    /// loaded shoe, which only a replayed cast sets. So this looks at what every peer already HAS, every frame:
    ///   * a Rafi whose SKIM clock is running (`AttackingSkill.IsActive`, which the cast replay and the joiner's restore both
    ///     set) and who holds a shoe: that shoe is loaded;
    ///   * a shoe in flight whose affinity is SKIM: that is the slipper's own replicated state (`Slipper.Affinity` rides the
    ///     snapshot with the flight), so every peer and every joiner knows it;
    ///   * that shoe at the ground still in flight: it is sliding. The host knows it exactly (`IsSkimming`); a client sees the
    ///     interpolated shoe on the ground with its flight still open, which is the same moment a frame late.
    /// It never decides anything and never writes anything back: no collider, no rule, nothing on the wire.
    ///
    /// ⚠️ THE TRACK IS WHERE THE SHOE WENT. It is laid from the point the shoe was first seen on the ground, along the way
    /// it is sliding, to where it is now, and never past the rule's 2 m: so a shoe stopped early by a wall leaves a short
    /// track, and the picture cannot claim a slide the rule did not give.
    /// </summary>
    public sealed class RafiSkimWater : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ THE SWITCH. On in this draft so the owner can judge it in a match; false gives today's SKIM exactly (the thin
        /// line, made by the cast). Read every frame: turning it off lets every coat and wake already drawn finish.
        /// </summary>
        public static bool Painted = true;
        /// <summary>A film has no main camera: it says where the lens is, and the drops turn to it.</summary>
        public static Vector3? StagedEye;
        /// <summary>
        /// Where the spray at the touch goes: the match's one field of cloud puffs. A film outside Play has no running
        /// field and hands in its own.
        /// </summary>
        public static System.Action<CloudPuffField.Puff> Spray = p => CloudPuffs.Spawn(p);

        /// <summary>Is this shoe loaded with this kit's SKIM, by what every peer knows?</summary>
        public static bool Loaded(RafiHeroKit kit, Slipper shoe)
        {
            if (kit == null || shoe == null) return false;
            if (kit.IsSkimLoadedFor(shoe)) return true;
            if (!Painted) return false;
            var holder = shoe.Holder;
            return shoe.State == SlipperState.Held && holder != null && !holder.IsDefender
                && holder.AbilitySystem?.Kit == kit && kit.AttackingSkill != null && kit.AttackingSkill.IsActive;
        }

        /// <summary>A loaded throw in the air or sliding: the slipper's own replicated state.</summary>
        public static bool Flying(Slipper shoe) => shoe != null && shoe.State == SlipperState.InFlight && shoe.Affinity == SlipperAffinity.Skim;

        /// <summary>Should the coat on this shoe still be drawn?</summary>
        public static bool Wears(RafiHeroKit kit, Slipper shoe) => Loaded(kit, shoe) || (Painted && Flying(shoe));

        /// <summary>
        /// The spray at the touch: the approved cloud's puffs in the wall's pale water instead of chalk, thrown about knee
        /// high (k2: the k1 film's smaller puff was a few pixels wide from the victim's eyes).
        /// </summary>
        public static CloudPuffField.Puff TouchSpray(Vector3 at, float turn)
        {
            var look = RafiWaterPaint.Day;
            return new CloudPuffField.Puff
            {
                At = at, Scale = 1.0f, Life = 0.40f, Reach = 0.50f, Lift = 0.40f, Turn = turn, Share = 0.9f,
                Lit = look.Foam, Shade = look.Pale, Line = look.Body,
            };
        }

        private sealed class Slide
        {
            public Slipper Shoe;
            public RafiSkimWake Wake;
            public Transform Holder;
            public Vector3 From, Way;
            public float Born, RestedAt = -1f, Slid;
        }

        private static RafiSkimWater _current;
        private readonly List<Slide> _slides = new List<Slide>();
        private Slipper[] _shoes = new Slipper[0];
        private float _nextScan, _clock;
        private int _slideCount;
        private readonly Dictionary<Slipper, float> _rest = new Dictionary<Slipper, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_current != null) return;
            var go = new GameObject("Ilyas skim water") { hideFlags = HideFlags.DontSave };
            DontDestroyOnLoad(go);
            _current = go.AddComponent<RafiSkimWater>();
        }

        private void OnDestroy() { Clear(); if (_current == this) _current = null; }

        private void Clear()
        {
            foreach (var slide in _slides) Drop(slide);
            _slides.Clear();
        }

        private static void Drop(Slide slide)
        {
            slide.Wake?.Dispose();
            if (slide.Holder != null) Destroy(slide.Holder.gameObject);
        }

        private void LateUpdate()
        {
            var round = GameServices.Round;
            if (round == null || !round.RoundActive) { if (_slides.Count > 0) Clear(); return; }
            _clock += Time.deltaTime;
            if (!Painted && _slides.Count == 0) return;
            if (Time.unscaledTime >= _nextScan) { _shoes = FindObjectsByType<Slipper>(FindObjectsInactive.Exclude); _nextScan = Time.unscaledTime + 1f; }

            if (Painted)
            {
                // The coat, on every Rafi whose load is live, on whatever shoe he holds.
                foreach (var player in round.Players)
                {
                    if (player == null || !(player.AbilitySystem?.Kit is RafiHeroKit kit)) continue;
                    foreach (var shoe in _shoes)
                        if (shoe != null && shoe.Holder == player && Loaded(kit, shoe))
                            RafiSkimPainted.Ensure(shoe.GetComponentInChildren<MeshFilter>(), shoe, kit);
                }
                // The wake, on every loaded shoe that is on the ground with its flight still open.
                foreach (var shoe in _shoes)
                {
                    if (shoe == null) continue;
                    var slide = Live(shoe);
                    if (!Flying(shoe)) { if (slide != null && slide.RestedAt < 0f) slide.RestedAt = _clock - slide.Born; continue; }
                    Vector3 at = shoe.transform.position;
                    if (slide == null)
                    {
                        if (!shoe.IsSkimming && !OnGround(shoe, at)) continue;
                        slide = Begin(shoe, at);
                    }
                    // How far along its own line it is now; it cannot run back, and it cannot run past the rule.
                    float along = Vector3.Dot(at - slide.From, slide.Way);
                    slide.Slid = Mathf.Clamp(Mathf.Max(slide.Slid, along), 0f, RafiRules.SkimDistance);
                }
            }

            Vector3 eye = RafiWaterPaint.Eye(StagedEye, transform.position);
            for (int i = _slides.Count - 1; i >= 0; i--)
            {
                var slide = _slides[i];
                float age = _clock - slide.Born;
                // A slide nobody saw end (the shoe went) ends now.
                if (slide.Shoe == null && slide.RestedAt < 0f) slide.RestedAt = age;
                if (slide.Holder == null || (slide.RestedAt >= 0f && age > slide.RestedAt + RafiSkimWake.Tail) || age > 6f)
                { Drop(slide); _slides.RemoveAt(i); continue; }
                slide.Wake.Pose(age, slide.Slid, slide.RestedAt, eye);
            }
        }

        private Slide Live(Slipper shoe)
        {
            foreach (var slide in _slides) if (slide.Shoe == shoe && slide.RestedAt < 0f) return slide;
            return null;
        }

        // A client's shoe is interpolated: on the ground means its sole is within a few centimetres of the floor under it.
        private bool OnGround(Slipper shoe, Vector3 at)
        {
            if (!_rest.TryGetValue(shoe, out float rest)) { rest = shoe.RestHeight; _rest[shoe] = rest; }
            float floor = Slipper.FindGroundY(at, 0.5f);
            return float.IsFinite(floor) && at.y - floor <= rest + 0.06f;
        }

        private Slide Begin(Slipper shoe, Vector3 at)
        {
            var way = shoe.Velocity; way.y = 0f;
            if (way.sqrMagnitude < 1e-4f) { way = shoe.transform.forward; way.y = 0f; }
            if (way.sqrMagnitude < 1e-4f) way = Vector3.forward;
            way.Normalize();
            float floor = Slipper.FindGroundY(at, 0.5f);
            var from = new Vector3(at.x, float.IsFinite(floor) ? floor : at.y, at.z);
            var holder = new GameObject("Ilyas skim slide") { hideFlags = HideFlags.DontSave }.transform;
            holder.SetPositionAndRotation(from, Quaternion.LookRotation(way, Vector3.up));
            var slide = new Slide { Shoe = shoe, Holder = holder, From = from, Way = way, Born = _clock,
                Wake = new RafiSkimWake(holder, Mathf.Repeat((shoe.OwnerSlot + 1) * 0.37f + _slideCount * 0.23f, 1f)) };
            _slides.Add(slide); _slideCount++;
            try { Spray?.Invoke(TouchSpray(from, slide.Born * 3.1f)); } catch (System.Exception) { }
            return slide;
        }
    }
}
