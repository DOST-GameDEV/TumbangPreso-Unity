using System.Collections.Generic;
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
    /// ⚠️ IT MUST READ AS AN ARM (owner 2026-10-24, from a match: "it doenst read as an arm bcz it has no details it js looks
    /// like a red dildo"). Two causes, both fixed here and in `tools/build_sean_props.py` (`giant_fist` v3):
    ///  * the model was a straight column with a round cap; v4 is a comically big fist erupting out of the puddle on a short
    ///    wrist in a surge of lava, no arm (his reference: Seeker's fist, Risk of Rain 2), tipped toward the caster, its detail
    ///    carried in colour (hot fingers on a dark core, gold knuckles) as flat toon shading needs;
    ///  * the cooling wrote `sharedMaterial.color`, and `ToonSkin` hands every Sean prop dressed the same way ONE cached
    ///    material whose `_Color` multiplies the palette: the first fist to cool stamped one flat colour over every fist, Molten
    ///    fist and bomb after it. It now tints each renderer through a property block (<see cref="Cool"/>).
    /// The puddle is a lumpy splat with a hotter heart, as the bomb's pool is, not a flat red disc.
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
        private const float GiantHeight = 1.05f;  // `tools/build_sean_props.py --measure giant_fist` (v4: 1.05 m)
        // Drawn this much bigger than the model: comically big, about 2.4 m tall and as wide as the puddle, so its edge is still
        // what it throws (owner 2026-10-24: "needs to be bigger", "make it more comically large rather than showing the whole arm",
        // after Seeker's fist in Risk of Rain 2). Bigger than the reach would draw players under it who are not thrown.
        private const float GiantScale = 2.3f;
        private Renderer _puddleEdge, _puddleHeart;
        private Renderer[] _giantSurfaces;
        private MaterialPropertyBlock _tint;
        private static readonly int ColourId = Shader.PropertyToID("_Color");
        // What the palette is multiplied by once it has cooled: dark, a little warm, so it reads as glass and not a hole.
        private static readonly Color CooledTint = new Color(.24f, .17f, .19f);

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
            // A lumpy splat of lava (the bomb pool's outline) with a hotter heart inside it, both unit-across under one holder
            // that the warning spreads. (It was a flat cylinder: from a player's eyes, a red plate.)
            var puddle = new GameObject("Puddle").transform;
            puddle.SetParent(transform, false);
            puddle.localPosition = new Vector3(0, .02f, 0);
            puddle.localScale = new Vector3(.01f, 1f, .01f);
            var edge = VfxShapes.Lay(puddle, "PuddleEdge", VfxShapes.Splat(26, .1f, 11), .5f, 0f);
            VfxMaterial.Ghost(edge.GetComponent<Renderer>(), LavaEdge, 1.0f);
            var heart = VfxShapes.Lay(puddle, "PuddleHeart", VfxShapes.Splat(18, .16f, 23), .3f, .008f, 40f);
            VfxMaterial.Ghost(heart.GetComponent<Renderer>(), LavaCore, 1.3f);
            _puddle = puddle;
            _puddleEdge = edge.GetComponent<Renderer>(); _puddleHeart = heart.GetComponent<Renderer>();
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
            if (_puddle != null) _puddle.localScale = new Vector3(across, 1f, across);
            if (_age >= SeanRules.FistWarnSeconds) Punch();
        }

        /// <summary>The warning is over: a giant fist punches up out of the puddle.</summary>
        private void Punch()
        {
            _punchAge = 0;
            var giant = new GameObject("GiantFist").transform;
            giant.SetParent(transform, false);
            // The modelled giant fist (`giant_fist` v4, rising +Y out of the puddle); the first-pass blocks if it is missing.
            if (SeanProp.Spawn("giant_fist", giant) == null)
            {
                Block("Forearm", giant, new Vector3(.9f, 1.3f, .9f), new Vector3(0, .65f, 0), LavaEdge);
                Block("Fist", giant, new Vector3(1.15f, .7f, 1.1f), new Vector3(0, 1.6f, 0), LavaCore);
            }
            // It comes UP OUT OF the puddle, never scaled out of nothing (plan § 0 rule 4): it starts sunk below the court.
            giant.localScale = Vector3.one * GiantScale;
            giant.localPosition = new Vector3(0, -GiantHeight * GiantScale, 0);
            // Its fingers and thumb face back toward HIM: the front of a fist is its most readable face, and his is the screen
            // that placed it (film v13: facing the throw, his view was the back of the hand). The throw is `_travel`'s, unchanged.
            giant.localRotation = Quaternion.Euler(0, 180, 0);
            _giant = giant;
            _giantSurfaces = giant.GetComponentsInChildren<Renderer>();

            NetCue.Play("sfx_explosion_heavy", _punchAt);
            // The burst stays LOW and at the arm's foot, and the ring marks the reach: the arm coming up IS the picture. (It
            // was a fire dome the puddle's full width, a translucent orange blob over the arm as it came out; film v10.)
            SeanGroundMark.Spawn(_punchAt, SeanRules.FistPunchRadius, SeanGroundMark.Mode.Flash, .35f, SeanImpact.Accent);
            HeroHazards.CreateExplosionVisual(_punchAt + Vector3.up * .15f, .55f, "", HeroHazards.ExplosionStyle.Ignition);
            Spray();
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
                    _giant.localScale = new Vector3(pop, 1f / Mathf.Sqrt(pop), pop) * GiantScale;
                }
                return;
            }
            var size = Vector3.one * GiantScale;
            if (_giant != null && _giant.localScale != size) _giant.localScale = Vector3.MoveTowards(_giant.localScale, size, dt * 4f * GiantScale);
            _punchAge += dt;
            StepGobs(dt);
            if (!_resolved && _punchAge >= SeanRules.FistHitDelay) { _resolved = true; HostResolvePunch(); PresentContact(); }
            float rise = SeanRules.FistRiseSeconds, hold = SeanRules.FistHoldSeconds, cool = .45f;
            if (_giant != null)
            {
                float up = _punchAge < rise ? Mathf.SmoothStep(0, 1.12f, _punchAge / rise)
                         : _punchAge < rise + .08f ? Mathf.Lerp(1.12f, 1f, (_punchAge - rise) / .08f)
                         : _punchAge < rise + hold ? 1f
                         : Mathf.Lerp(1f, 0f, (_punchAge - rise - hold) / cool);
                // Risen by `up` out of the puddle and sunk back into it as it cools; the court hides what is below it.
                _giant.localPosition = new Vector3(0, -GiantHeight * GiantScale * (1f - Mathf.Max(0, up)), 0);
            }
            // It cools to black glass as it sinks back into the puddle, and MELTS as it goes: wider and lower, lava settling
            // into the pool it came from rather than a solid sliding down a hole.
            float k = Mathf.Clamp01((_punchAge - rise - hold) / cool);
            if (k > 0)
            {
                Cool(k);
                float melt = Mathf.SmoothStep(0, 1, k);
                if (_giant != null) _giant.localScale = new Vector3(1f + .3f * melt, 1f - .45f * melt, 1f + .3f * melt) * GiantScale;
            }
            float across = SeanRules.FistPunchRadius * 2.2f * (1 - .3f * k);
            if (_puddle != null) _puddle.localScale = new Vector3(across, 1f, across);
            if (_punchAge > rise + hold + cool + .35f) Object.Destroy(gameObject);
        }

        /// <summary>
        /// Cools the fist and the puddle toward glass by <paramref name="k"/> (0 hot, 1 cooled). The fist's surfaces are tinted
        /// one renderer at a time through a property block, NEVER through `sharedMaterial`: that material is `ToonSkin`'s cached
        /// dressing, shared with every other Sean prop. The puddle's two materials are its own (`VfxMaterial.Ghost` makes them).
        /// </summary>
        private void Cool(float k)
        {
            if (_giantSurfaces != null)
            {
                _tint ??= new MaterialPropertyBlock();
                var tint = Color.Lerp(Color.white, CooledTint, k);
                foreach (var r in _giantSurfaces)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(_tint); _tint.SetColor(ColourId, tint); r.SetPropertyBlock(_tint);
                }
            }
            if (_puddleEdge != null && _puddleEdge.sharedMaterial != null) _puddleEdge.sharedMaterial.color = Color.Lerp(LavaEdge, Glass, k);
            if (_puddleHeart != null && _puddleHeart.sharedMaterial != null) _puddleHeart.sharedMaterial.color = Color.Lerp(LavaCore, LavaEdge, Mathf.Clamp01(k * 1.6f));
        }

        // ---- The spray: gobs of lava thrown up and out as it erupts, splatting where they land (the reference's flying blobs).
        private struct Gob { public Transform Body; public Vector3 Velocity; public float Life; public bool Landed; }
        private readonly List<Gob> _gobs = new List<Gob>();
        private const float GobSeconds = .9f;

        private void Spray()
        {
            int count = Settings.SettingsStore.Current.ReducedEffects ? 4 : 10;
            // Presentation only, so its own seeded random: it never moves Unity's shared random state.
            var random = new System.Random(Mathf.RoundToInt(_punchAt.x * 97f) ^ Mathf.RoundToInt(_punchAt.z * 131f));
            for (int i = 0; i < count; i++)
            {
                float around = (i + (float)random.NextDouble() * .7f) / count * Mathf.PI * 2f;
                var out_ = new Vector3(Mathf.Cos(around), 0, Mathf.Sin(around));
                var gob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gob.name = "LavaGob";
                Object.Destroy(gob.GetComponent<Collider>());
                gob.transform.SetParent(transform, true);
                gob.transform.position = _punchAt + out_ * (.45f + .25f * (float)random.NextDouble()) + Vector3.up * .3f;
                gob.transform.localScale = Vector3.one * (.13f + .12f * (float)random.NextDouble());
                VfxMaterial.Ghost(gob.GetComponent<Renderer>(), i % 3 == 0 ? LavaEdge : LavaCore, 1.4f);
                _gobs.Add(new Gob { Body = gob.transform, Life = GobSeconds * (.75f + .25f * (float)random.NextDouble()),
                    Velocity = out_ * (1.6f + 2.2f * (float)random.NextDouble()) + Vector3.up * (4.2f + 3f * (float)random.NextDouble()) });
            }
        }

        private void StepGobs(float dt)
        {
            for (int i = _gobs.Count - 1; i >= 0; i--)
            {
                var g = _gobs[i];
                if (g.Body == null) { _gobs.RemoveAt(i); continue; }
                g.Life -= dt;
                if (g.Life <= 0) { Object.Destroy(g.Body.gameObject); _gobs.RemoveAt(i); continue; }
                if (!g.Landed)
                {
                    g.Velocity += Physics.gravity * dt;
                    var at = g.Body.position + g.Velocity * dt;
                    // A gob that comes down on the court flattens into a splat there.
                    if (g.Velocity.y < 0 && at.y <= _punchAt.y + .03f)
                    {
                        at.y = _punchAt.y + .03f; g.Landed = true;
                        var s = g.Body.localScale; g.Body.localScale = new Vector3(s.x * 1.8f, s.y * .25f, s.z * 1.8f);
                    }
                    g.Body.position = at;
                }
                // The last quarter of its life it shrinks away.
                float fade = Mathf.Clamp01(g.Life / (GobSeconds * .25f));
                if (fade < 1) g.Body.localScale *= Mathf.Lerp(.85f, 1f, fade);
                _gobs[i] = g;
            }
        }

        private void OnDestroy() { if (_mark != null) _mark.Stop(); }
    }
}
