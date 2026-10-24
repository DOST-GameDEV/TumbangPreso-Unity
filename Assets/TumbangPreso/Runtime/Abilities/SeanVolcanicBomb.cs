using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// A VOLCANIC BOMB in the world: since MAYON AWAKENS v2 his volcano lobs them (`SeanVolcano`), a ball of lava out of the
    /// crater on a high arc, splashing where it lands and leaving a pool that skins over into black glass and slows anyone in it.
    ///
    /// ⚠️ EVERY PEER RUNS ITS OWN COPY FROM THE SAME CAST; THE HOST ALONE RESOLVES THE SPLASH (`SeanHeroKit.HostBlast`, which
    /// knocks the lata over unless Sean is the taya guarding it). The pool's slow is applied by each peer to the bodies it
    /// simulates (`CharacterMotor.EnterSpeedZone` is gated on that), so nothing about it is sent.
    ///
    /// ⚠️⚠️ HOW IT MEETS THE COURT (`docs/reports/sean-kit-2026-10-24/feel-and-readability.md` § 2):
    ///  * **The ring is drawn the moment it leaves his hands**, at exactly the splash radius, on every screen, filling as it
    ///    falls. It is the one warning a dodgeable blast gets, and the player standing inside it sees theirs brighter.
    ///  * **The arc is swept against the world.** A wall, an awning or a roof edge in its way stops it, and it splashes at the
    ///    foot of whatever it hit, never through it.
    ///  * **The splash and the pool reach their own level only** (`SeanRules.BlastReachHeight`), and the pool is draped onto
    ///    the court, so it lies on a slope and climbs a kerb.
    /// </summary>
    public sealed class SeanVolcanicBomb : MonoBehaviour
    {
        private CharacterMotor _owner;
        private Vector3 _land, _from, _last;
        private float _age, _gather, _flight, _radius, _push, _size, _apex;
        private Transform _ball;
        private bool _splashed;
        private SeanGroundMark _ring;

        /// <summary>
        /// One bomb out of <paramref name="crater"/>, landing on <paramref name="land"/> after `VolcanoBombFlightSeconds`. Every
        /// peer runs its own from the host's announcement (`MatchFlair.Kind.SeanVolcanoBomb`); the host's splash is the hit.
        /// </summary>
        public static SeanVolcanicBomb FromVolcano(CharacterMotor owner, Vector3 crater, Vector3 land)
        {
            var go = new GameObject("SeanVolcanicBomb");
            var bomb = go.AddComponent<SeanVolcanicBomb>();
            bomb._owner = owner; bomb._land = land;
            bomb._gather = .08f;
            bomb._flight = SeanRules.VolcanoBombFlightSeconds;
            bomb._radius = SeanRules.VolcanoBombRadius; bomb._push = SeanRules.VolcanoBombPush;
            bomb._size = .8f;
            // A lob: higher the farther it goes, so a far one is still a falling thing and not a line drive.
            bomb._apex = 2.5f + .25f * Vector3.Distance(crater, land);
            bomb._from = crater;
            bomb._last = bomb._from;
            go.transform.position = bomb._from;
            // The modelled ball of lava (`bomb_ball`, 1.2 m across at scale 1); the first-pass spheres if it is missing.
            var holder = new GameObject("Ball").transform;
            holder.SetParent(go.transform, false);
            var model = SeanProp.Spawn("bomb_ball", holder);
            // The holder's scale is the ball's diameter (as the unit sphere's was); the model is 1.2 m at scale 1.
            if (model != null) model.transform.localScale = Vector3.one / 1.2f;
            else
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.transform.SetParent(holder, false);
                VfxMaterial.Ghost(ball.GetComponent<Renderer>(), SeanMagmaFist.LavaEdge, 1.4f);
                var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                core.transform.SetParent(ball.transform, false);
                core.transform.localScale = Vector3.one * .7f;
                VfxMaterial.Ghost(core.GetComponent<Renderer>(), SeanMagmaFist.LavaCore, 1.8f);
            }
            bomb._ball = holder;
            bomb._ball.localScale = Vector3.zero;
            AbilityVfx.AttachAura(bomb._ball, AbilityVfx.Aura.FireEmber, bomb._gather + bomb._flight + .2f);
            bomb._ring = SeanGroundMark.Spawn(land, bomb._radius, SeanGroundMark.Mode.Fill,
                bomb._gather + bomb._flight, SeanImpact.Accent);
            return bomb;
        }

        private void Update()
        {
            if (PresentationClock.Held || _splashed) return;
            _age += Time.deltaTime;
            MarkForLocalPlayer();
            float size = _size;
            if (_age < _gather)
            {
                // It wells up out of the crater.
                transform.position = _last = _from;
                _ball.localScale = Vector3.one * size * Mathf.SmoothStep(0, 1, _age / _gather);
                return;
            }
            float t = Mathf.Clamp01((_age - _gather) / _flight);
            _ball.localScale = Vector3.one * size * (1 + .06f * Mathf.Sin(_age * 30f));   // it wobbles like liquid
            Vector3 next = Vector3.Lerp(_from, _land, t) + Vector3.up * (_apex * 4 * t * (1 - t));

            // Swept against the world: whatever is in the way is where it lands.
            Vector3 step = next - _last;
            if (t > .15f && step.sqrMagnitude > .0001f
                && Physics.SphereCast(_last, .3f, step.normalized, out var hit, step.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && Blocks(hit.collider))
            {
                Vector3 foot = hit.point + hit.normal * .3f;
                if (VfxShapes.TryGroundUnder(foot, .5f, 8f, out float y)) foot.y = y;
                _land = foot;
                Splash();
                return;
            }
            transform.position = _last = next;
            if (t >= 1) Splash();
        }

        private bool Blocks(Collider c)
            => c != null && c.GetComponentInParent<CharacterMotor>() == null && c.GetComponentInParent<Slipper>() == null
               && c.GetComponentInParent<Lata>() == null && c.GetComponentInParent<SeanVolcanicBomb>() == null
               && c.GetComponentInParent<SeanVolcano>() == null;

        /// <summary>The player it is coming down on sees its ring brighter (the threat to YOU outranks the rest).</summary>
        private void MarkForLocalPlayer()
        {
            if (_ring == null) return;
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraSystem.CameraRig>() : null;
            var me = rig != null ? rig.Following : null;
            if (me == null || me == _owner) { _ring.Brightness = 1f; return; }
            Vector3 to = me.transform.position - _land;
            bool inside = SeanRules.WithinReach(to.y, SeanRules.BlastReachHeight)
                          && new Vector2(to.x, to.z).magnitude <= _radius;
            _ring.Brightness = inside ? 1.7f : 1f;
        }

        private void Splash()
        {
            _splashed = true;
            if (_ball != null) _ball.gameObject.SetActive(false);
            if (_ring != null) _ring.Stop();
            float radius = _radius;
            // Local on every peer: each runs this same flight from the same announcement.
            GameServices.Audio?.PlayAt("sfx_explosion_heavy", _land);
            HeroHazards.CreateExplosionVisual(_land, radius, "", HeroHazards.ExplosionStyle.Fire);
            SeanGroundMark.Spawn(_land, radius, SeanGroundMark.Mode.Flash, .35f, SeanImpact.Accent);
            if (_owner != null)
                SeanHeroKit.HostBlast(_owner, _land, radius, _push, SeanRules.BombStaggerSeconds,
                    knocksCan: true, MatchFlair.Kind.SeanSlam);
            SeanLavaPool.Spawn(_land, radius * SeanRules.PoolRadiusShare, SeanRules.PoolSeconds, _owner);
            Object.Destroy(gameObject, .1f);
        }

        private void OnDestroy() { if (_ring != null) _ring.Stop(); }
    }

    /// <summary>
    /// The pool a VOLCANIC BOMB leaves: glowing, then skinning over into black glass from its edge in, slowing everyone in it
    /// but Sean by `SeanRules.PoolSpeedScale` until it is gone. Draped onto the court, and it slows only bodies on its level.
    /// </summary>
    public sealed class SeanLavaPool : MonoBehaviour
    {
        private float _radius, _life, _age;
        private CharacterMotor _owner;
        private Renderer _disc;
        private readonly HashSet<CharacterMotor> _slowed = new HashSet<CharacterMotor>();

        public static void Spawn(Vector3 at, float radius, float seconds, CharacterMotor owner)
        {
            var go = new GameObject("SeanLavaPool");
            if (VfxShapes.TryGroundUnder(at, 1.5f, 3.0f, out float y)) at.y = y;
            go.transform.position = at;
            var pool = go.AddComponent<SeanLavaPool>();
            pool._radius = radius; pool._life = seconds; pool._owner = owner;
            var disc = VfxShapes.Lay(go.transform, "Pool", VfxShapes.Splat(28, .08f, 7), radius, .02f);
            VfxMaterial.Ghost(disc.GetComponent<Renderer>(), SeanMagmaFist.LavaEdge, 1.1f);
            VfxMaterial.StripCollider(disc);
            VfxShapes.DrapeToGround(disc);
            pool._disc = disc.GetComponent<Renderer>();
        }

        private void Update()
        {
            if (PresentationClock.Held) return;
            _age += Time.deltaTime;
            if (_age >= _life) { Object.Destroy(gameObject); return; }
            if (_disc != null && _disc.sharedMaterial != null)
                _disc.sharedMaterial.color = Color.Lerp(SeanMagmaFist.LavaEdge, SeanMagmaFist.Glass, Mathf.Clamp01(_age / _life));

            var round = GameServices.Round;
            if (round == null) return;
            foreach (var p in round.Players)
            {
                if (p == null || p == _owner) continue;
                Vector3 to = p.transform.position - transform.position;
                bool inside = p.gameObject.activeInHierarchy && SeanRules.WithinReach(to.y, SeanRules.BlastReachHeight)
                              && new Vector2(to.x, to.z).magnitude <= _radius;
                if (inside && _slowed.Add(p)) p.EnterSpeedZone(SeanRules.PoolSpeedScale);
                else if (!inside && _slowed.Remove(p)) p.ExitSpeedZone(SeanRules.PoolSpeedScale);
            }
        }

        private void OnDestroy()
        {
            foreach (var p in _slowed) if (p != null) p.ExitSpeedZone(SeanRules.PoolSpeedScale);
            _slowed.Clear();
        }
    }
}
