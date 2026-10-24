using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Sean's VOLCANIC BOMB in the world: the lava pulled off both his arms into one heavy ball over his head (the tell), hurled
    /// on a high arc, splashing where it lands and leaving a pool that skins over into black glass and slows anyone in it.
    /// Unthrown when Molten runs out, it drops at his feet and splashes smaller (owner, 2026-10-24: "Drops at his feet").
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
        private bool _thrown;
        private Vector3 _land, _from, _last;
        private float _age, _gather, _flight;
        private Transform _ball;
        private bool _splashed;
        private SeanGroundMark _ring;

        public static SeanVolcanicBomb Spawn(CharacterMotor owner, Vector3 land, bool thrown)
        {
            var go = new GameObject("SeanVolcanicBomb");
            var bomb = go.AddComponent<SeanVolcanicBomb>();
            bomb._owner = owner; bomb._thrown = thrown; bomb._land = land;
            bomb._gather = thrown ? SeanRules.BombTellSeconds : .15f;
            bomb._flight = thrown ? SeanRules.BombFlightSeconds : .25f;
            bomb._from = owner.transform.position + Vector3.up * 2.3f;
            bomb._last = bomb._from;
            go.transform.position = bomb._from;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            ball.transform.SetParent(go.transform, false);
            VfxMaterial.Ghost(ball.GetComponent<Renderer>(), SeanMagmaFist.LavaEdge, 1.4f);
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(ball.transform, false);
            core.transform.localScale = Vector3.one * .7f;
            VfxMaterial.Ghost(core.GetComponent<Renderer>(), SeanMagmaFist.LavaCore, 1.8f);
            bomb._ball = ball.transform;
            bomb._ball.localScale = Vector3.zero;
            AbilityVfx.AttachAura(bomb._ball, AbilityVfx.Aura.FireEmber, bomb._gather + bomb._flight + .2f);
            bomb._ring = SeanGroundMark.Spawn(land, SeanRules.BombRadiusFor(thrown), SeanGroundMark.Mode.Fill,
                bomb._gather + bomb._flight, SeanImpact.Accent);
            return bomb;
        }

        private void Update()
        {
            if (PresentationClock.Held || _splashed) return;
            _age += Time.deltaTime;
            MarkForLocalPlayer();
            float size = _thrown ? 1.2f : .9f;
            if (_age < _gather)
            {
                // The gather: it swells over his head, riding with him.
                if (_owner != null) _from = _owner.transform.position + Vector3.up * 2.3f;
                transform.position = _last = _from;
                _ball.localScale = Vector3.one * size * Mathf.SmoothStep(0, 1, _age / _gather);
                return;
            }
            float t = Mathf.Clamp01((_age - _gather) / _flight);
            _ball.localScale = Vector3.one * size * (1 + .06f * Mathf.Sin(_age * 30f));   // it wobbles like liquid
            float apex = _thrown ? 4.0f : .2f;
            Vector3 next = Vector3.Lerp(_from, _land, t) + Vector3.up * (apex * 4 * t * (1 - t));

            // Swept against the world: whatever is in the way is where it lands.
            Vector3 step = next - _last;
            if (_thrown && step.sqrMagnitude > .0001f
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
               && c.GetComponentInParent<Lata>() == null && c.GetComponentInParent<SeanVolcanicBomb>() == null;

        /// <summary>The player it is coming down on sees its ring brighter (the threat to YOU outranks the rest).</summary>
        private void MarkForLocalPlayer()
        {
            if (_ring == null) return;
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraSystem.CameraRig>() : null;
            var me = rig != null ? rig.Following : null;
            if (me == null || me == _owner) { _ring.Brightness = 1f; return; }
            Vector3 to = me.transform.position - _land;
            bool inside = SeanRules.WithinReach(to.y, SeanRules.BlastReachHeight)
                          && new Vector2(to.x, to.z).magnitude <= SeanRules.BombRadiusFor(_thrown);
            _ring.Brightness = inside ? 1.7f : 1f;
        }

        private void Splash()
        {
            _splashed = true;
            if (_ball != null) _ball.gameObject.SetActive(false);
            if (_ring != null) _ring.Stop();
            float radius = SeanRules.BombRadiusFor(_thrown);
            NetCue.Play("sfx_explosion_heavy", _land);
            HeroHazards.CreateExplosionVisual(_land, radius, "", HeroHazards.ExplosionStyle.Fire);
            SeanGroundMark.Spawn(_land, radius, SeanGroundMark.Mode.Flash, .35f, SeanImpact.Accent);
            if (_owner != null)
                SeanHeroKit.HostBlast(_owner, _land, radius, SeanRules.BombPushFor(_thrown), SeanRules.BombStaggerSeconds,
                    knocksCan: true, _thrown ? MatchFlair.Kind.SeanBlast : MatchFlair.Kind.SeanSlam);
            SeanLavaPool.Spawn(_land, radius * .6f, SeanRules.PoolSeconds, _owner);
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
