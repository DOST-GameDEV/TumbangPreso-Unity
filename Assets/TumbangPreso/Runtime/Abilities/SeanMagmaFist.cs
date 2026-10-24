using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Sean's MAGMA FIST in the world (owner, 2026-10-24: "his fist thing sucks", "why is he throwing something?", "he js does a
    /// prayer pose tapos he long presses or smth to decide where it gets cast"). Nothing flies any more: he holds the prayer and
    /// places the ring where he looks (`HeroAbility.AimByHolding`, where-looking), and on the release a puddle of lava opens on
    /// that spot. For `SeanRules.FistWarnSeconds` it bubbles, the warning anyone standing on it can step off; then a giant fist
    /// punches up out of it (the owner's Puddle Punch picture) and throws everyone on it up and straight away from the lata.
    ///
    /// ⚠️ EVERY PEER OPENS ITS OWN COPY FROM THE SAME CAST, AND ONLY THE HOST DECIDES WHO IS HIT. The aimed spot is the cast's
    /// own (it rides the cast, so every peer opens it in the same place); positions of the bodies on it differ by a frame or two,
    /// so a replica may draw the punch a hair off, never a different outcome. Nothing here runs while a cutscene holds the world
    /// (`PresentationClock.Held`).
    ///
    /// ⚠️⚠️ HOW IT READS AND MEETS THE COURT (`docs/reports/sean-kit-2026-10-24/feel-and-readability.md` § 2):
    ///  * **The warning is on every screen**: the puddle and a filling ring at the spot, so the player standing there reads it
    ///    and everyone else sees where it will land.
    ///  * **It drapes over the court where it is placed** (`Slipper.FindGroundY`): a kerb or a terrace, it opens on the floor
    ///    that is there.
    ///  * **It reaches its own level only.** The giant fist throws only players within `SeanRules.FistReachHeight` of the puddle.
    ///  * **It hits as the knuckles reach the waist** (`SeanRules.FistHitDelay` after the fist starts up), so what the player
    ///    sees touch them is what hits them.
    ///
    /// The look is a first pass in the kit's family (liquid lava, gold to cherry red, cooling to black glass;
    /// `docs/reports/sean-kit-2026-10-24/plan.md` § 0). Its model and effects are the art pass still to come.
    /// </summary>
    public sealed class SeanMagmaFist : MonoBehaviour
    {
        public static readonly Color LavaCore = new Color(1f, .78f, .23f), LavaEdge = new Color(.85f, .15f, .11f),
                                     Glass = new Color(.09f, .07f, .1f);

        private CharacterMotor _owner;
        private SeanHeroKit _kit;
        private float _age, _punchAge = -1;
        private bool _resolved;
        private SeanGroundMark _mark;
        private Vector3 _travel, _punchAt;
        private Transform _giant, _puddle;
        private const float GiantHeight = 1.84f;   // `tools/build_sean_props.py --measure giant_fist`
        private Renderer[] _glow;

        /// <summary>Opens the puddle at <paramref name="spot"/>; <paramref name="from"/> is where he cast it from, the way the
        /// throw goes for anyone standing on the can itself.</summary>
        public static SeanMagmaFist Erupt(CharacterMotor owner, SeanHeroKit kit, Vector3 spot, Vector3 from)
        {
            var go = new GameObject("SeanMagmaFist");
            var fist = go.AddComponent<SeanMagmaFist>();
            fist._owner = owner; fist._kit = kit;
            Vector3 way = spot - from; way.y = 0;
            fist._travel = way.sqrMagnitude > .0001f ? way.normalized : (owner != null ? owner.transform.forward : Vector3.forward);
            fist._punchAt = spot; fist._punchAt.y = Slipper.FindGroundY(spot + Vector3.up, spot.y + 1f);
            go.transform.SetPositionAndRotation(fist._punchAt, Quaternion.LookRotation(fist._travel, Vector3.up));
            fist.BuildPuddle();
            fist._mark = SeanGroundMark.Spawn(fist._punchAt, SeanRules.FistPunchRadius, SeanGroundMark.Mode.Fill,
                SeanRules.FistWarnSeconds, SeanImpact.Accent);
            NetCue.Play("sfx_cast_sean_rush", fist._punchAt);
            return fist;
        }

        private void BuildPuddle()
        {
            var puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.name = "Puddle";
            Object.Destroy(puddle.GetComponent<Collider>());
            puddle.transform.SetParent(transform, false);
            puddle.transform.localPosition = new Vector3(0, .02f, 0);
            puddle.transform.localScale = new Vector3(.01f, .02f, .01f);
            VfxMaterial.Ghost(puddle.GetComponent<Renderer>(), LavaEdge, 1.0f);
            _puddle = puddle.transform;
            _glow = GetComponentsInChildren<Renderer>();
        }

        private static Transform Block(string name, Transform parent, Vector3 size, Vector3 at, Color colour)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = size;
            VfxMaterial.Ghost(go.GetComponent<Renderer>(), colour, 1.2f);
            return go.transform;
        }

        private void Update()
        {
            if (PresentationClock.Held) return;
            float dt = Time.deltaTime;
            if (_punchAge >= 0) { StepPunch(dt); return; }
            if (_owner == null) { Object.Destroy(gameObject); return; }

            // The warning: the puddle spreads to its full width and bubbles, then the fist comes.
            _age += dt;
            float open = Mathf.SmoothStep(0, 1, _age / (SeanRules.FistWarnSeconds * .6f));
            float across = SeanRules.FistPunchRadius * 2.2f * open * (1 + .05f * Mathf.Sin(_age * 30f));
            if (_puddle != null) _puddle.localScale = new Vector3(across, .02f, across);
            if (_age >= SeanRules.FistWarnSeconds) Punch();
        }

        /// <summary>The warning is over: a giant fist punches up out of the puddle.</summary>
        private void Punch()
        {
            _punchAge = 0;
            var giant = new GameObject("GiantFist").transform;
            giant.SetParent(transform, false);
            // The modelled giant fist (`giant_fist`, 1.84 m, rising +Y out of its splash); the first-pass blocks if it is missing.
            if (SeanProp.Spawn("giant_fist", giant) == null)
            {
                Block("Forearm", giant, new Vector3(.9f, 1.3f, .9f), new Vector3(0, .65f, 0), LavaEdge);
                Block("Fist", giant, new Vector3(1.15f, .7f, 1.1f), new Vector3(0, 1.6f, 0), LavaCore);
            }
            // It comes UP OUT OF the puddle, never scaled out of nothing (plan § 0 rule 4): it starts sunk below the court.
            giant.localPosition = new Vector3(0, -GiantHeight, 0);
            _giant = giant;
            _glow = GetComponentsInChildren<Renderer>();

            NetCue.Play("sfx_explosion_heavy", _punchAt);
            HeroHazards.CreateExplosionVisual(_punchAt, SeanRules.FistPunchRadius, "", HeroHazards.ExplosionStyle.Fire);
            if (_mark != null) { _mark.Stop(); _mark = null; }
            _resolved = false;
        }

        private void HostResolvePunch()
        {
            if (!NetAuthority.ShouldResolve() || GameServices.Round?.RoundActive != true || _owner == null) return;
            var can = GameServices.Round.Lata;
            Vector3 canAt = can != null ? can.transform.position : _punchAt - _travel;
            bool landed = false;
            foreach (var p in SeanHeroKit.Rivals(_owner))
            {
                Vector3 to = p.transform.position - _punchAt;
                if (!SeanRules.WithinReach(to.y, SeanRules.FistReachHeight)) continue;
                to.y = 0;
                if (to.magnitude > SeanRules.FistPunchRadius) continue;
                SeanRules.FistPushDirection(p.transform.position.x, p.transform.position.z, canAt.x, canAt.z,
                    _travel.x, _travel.z, out float x, out float z);
                var away = new Vector3(x, 0, z);
                p.ApplyResolvedImpact(away * SeanRules.FistPushSpeed + Vector3.up * SeanRules.FistLiftSpeed);
                p.ApplyStagger(.5f);
                // From behind them along the throw, so their camera is punched the way they fly.
                MatchFlair.Announce(MatchFlair.Kind.SeanSlam, _owner.PlayerSlot, p.PlayerSlot, p.transform.position - away, .5f);
                landed = true;
            }
            if (landed) _kit?.HostAddHeat(_owner);
        }

        /// <summary>
        /// ⚠️ CONTACT-1024: THE KNUCKLES MEET THE BODIES ON IT, ON EVERY PEER. Owner: the contact "doesn't look animated well".
        /// Each peer looks at who is standing on the puddle as the knuckles reach the waist (presentation; the host alone
        /// decides who is thrown, in <see cref="HostResolvePunch"/>), bursts lava off the knuckles at each one, and holds the
        /// fist on the touch for a beat with a squash, so the hit reads as an impact before the body flies.
        /// </summary>
        private void PresentContact()
        {
            if (_owner == null) return;
            bool touched = false;
            foreach (var p in SeanHeroKit.Rivals(_owner))
            {
                Vector3 to = p.transform.position - _punchAt;
                if (!SeanRules.WithinReach(to.y, SeanRules.FistReachHeight)) continue;
                to.y = 0;
                if (to.magnitude > SeanRules.FistPunchRadius + .2f) continue;
                SeanImpact.FistContact(p, _punchAt + Vector3.up * .9f);
                touched = true;
            }
            if (!touched) return;
            _contactHold = ContactHoldSeconds;
            NetCue.PlayVaried("sfx_explosion_heavy", _punchAt, .82f, .9f, .6f);
        }

        private float _contactHold;
        private const float ContactHoldSeconds = .07f;

        private void StepPunch(float dt)
        {
            // The impact frame: the fist stops dead on the touch for a beat and swells, then carries on up.
            if (_contactHold > 0)
            {
                _contactHold = Mathf.Max(0, _contactHold - dt);
                if (_giant != null)
                {
                    float pop = 1f + .12f * Mathf.Sin((1f - _contactHold / ContactHoldSeconds) * Mathf.PI);
                    _giant.localScale = new Vector3(pop, 1f / Mathf.Sqrt(pop), pop);
                }
                return;
            }
            if (_giant != null && _giant.localScale != Vector3.one) _giant.localScale = Vector3.MoveTowards(_giant.localScale, Vector3.one, dt * 4f);
            _punchAge += dt;
            if (!_resolved && _punchAge >= SeanRules.FistHitDelay) { _resolved = true; HostResolvePunch(); PresentContact(); }
            float rise = SeanRules.FistRiseSeconds, hold = SeanRules.FistHoldSeconds, cool = .45f;
            if (_giant != null)
            {
                float up = _punchAge < rise ? Mathf.SmoothStep(0, 1.12f, _punchAge / rise)
                         : _punchAge < rise + .08f ? Mathf.Lerp(1.12f, 1f, (_punchAge - rise) / .08f)
                         : _punchAge < rise + hold ? 1f
                         : Mathf.Lerp(1f, 0f, (_punchAge - rise - hold) / cool);
                // Risen by `up` out of the puddle and sunk back into it as it cools; the court hides what is below it.
                _giant.localPosition = new Vector3(0, -GiantHeight * (1f - Mathf.Max(0, up)), 0);
            }
            // It cools to black glass as it sinks back into the puddle.
            float k = Mathf.Clamp01((_punchAge - rise - hold) / cool);
            if (k > 0 && _glow != null)
                foreach (var r in _glow) if (r != null && r.sharedMaterial != null) r.sharedMaterial.color = Color.Lerp(LavaEdge, Glass, k);
            float across = SeanRules.FistPunchRadius * 2.2f * (1 - .3f * k);
            if (_puddle != null) _puddle.localScale = new Vector3(across, .02f, across);
            if (_punchAge > rise + hold + cool + .35f) Object.Destroy(gameObject);
        }

        private void OnDestroy() { if (_mark != null) _mark.Stop(); }
    }
}
