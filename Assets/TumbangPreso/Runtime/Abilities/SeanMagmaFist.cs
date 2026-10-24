using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Sean's MAGMA FIST in the world: a fist of lava punched from his knuckles that skims the court after its target, turning
    /// at most `SeanRules.FistTurnDegreesPerSecond`. Where it reaches them it sinks into a puddle under their feet and a giant
    /// fist punches up out of it (the owner's Puddle Punch picture), throwing them up and straight away from the lata.
    ///
    /// ⚠️ EVERY PEER FLIES ITS OWN COPY FROM THE SAME CAST, AND ONLY THE HOST DECIDES WHO IS HIT. The cast frame and the target
    /// are the same on every peer; positions differ by a frame or two, so a replica may draw the punch a hair off, never a
    /// different outcome. Nothing here runs while a cutscene holds the world (`PresentationClock.Held`).
    ///
    /// The look is a first pass in the kit's family (liquid lava, gold to cherry red, cooling to black glass;
    /// `docs/reports/sean-kit-2026-10-24/plan.md` § 0). Its model and effects are the art pass still to come.
    /// </summary>
    public sealed class SeanMagmaFist : MonoBehaviour
    {
        public static readonly Color LavaCore = new Color(1f, .78f, .23f), LavaEdge = new Color(.85f, .15f, .11f),
                                     Glass = new Color(.09f, .07f, .1f);

        private CharacterMotor _owner, _target;
        private SeanHeroKit _kit;
        private float _heading, _age, _punchAge = -1;
        private Vector3 _travel, _punchAt;
        private Transform _fist, _giant, _puddle;
        private Renderer[] _glow;

        public static SeanMagmaFist Spawn(CharacterMotor owner, SeanHeroKit kit, Vector3 at, Vector3 way, CharacterMotor target)
        {
            var go = new GameObject("SeanMagmaFist");
            go.transform.position = at;
            var fist = go.AddComponent<SeanMagmaFist>();
            fist._owner = owner; fist._kit = kit; fist._target = target;
            way.y = 0;
            fist._heading = way.sqrMagnitude > .0001f ? Mathf.Atan2(way.x, way.z) * Mathf.Rad2Deg : owner.transform.eulerAngles.y;
            fist.Build();
            NetCue.Play("sfx_cast_sean_rush", at);
            return fist;
        }

        private void Build()
        {
            _fist = Block("Fist", transform, new Vector3(.42f, .38f, .48f), Vector3.zero, LavaEdge);
            Block("Knuckles", _fist, new Vector3(1.05f, .45f, .3f), new Vector3(0, .2f, .45f), LavaCore);
            AbilityVfx.AttachAura(_fist, AbilityVfx.Aura.FireEmber, SeanRules.FistMaxFlightSeconds + 1f);
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

            _age += dt;
            if (_age > SeanRules.FistMaxFlightSeconds || _owner == null) { Fizzle(); return; }

            bool chasing = _target != null && _target.gameObject.activeInHierarchy && !_target.IsTagged;
            if (chasing)
            {
                Vector3 to = _target.transform.position - transform.position; to.y = 0;
                if (to.magnitude <= SeanRules.FistArriveRadius) { Punch(_target.transform.position); return; }
                _heading = SeanRules.FistSteer(_heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, dt);
            }

            _travel = Quaternion.Euler(0, _heading, 0) * Vector3.forward;
            Vector3 step = _travel * SeanRules.FistSpeed * dt;
            if (Physics.Raycast(transform.position, _travel, out var wall, step.magnitude + .25f, ~0, QueryTriggerInteraction.Ignore)
                && wall.collider.GetComponentInParent<CharacterMotor>() == null
                && wall.collider.GetComponentInParent<Slipper>() == null
                && wall.collider.GetComponentInParent<Lata>() == null
                && wall.collider.GetComponentInParent<SeanMagmaFist>() == null)
            { Fizzle(); return; }

            Vector3 next = transform.position + step;
            next.y = Slipper.FindGroundY(next, next.y) + .55f;
            transform.position = next;
            transform.rotation = Quaternion.LookRotation(_travel, Vector3.up);
        }

        /// <summary>It reached them: the fist sinks into a puddle and a giant one punches up out of it.</summary>
        private void Punch(Vector3 at)
        {
            _punchAt = at; _punchAt.y = Slipper.FindGroundY(at, at.y);
            _punchAge = 0;
            if (_fist != null) _fist.gameObject.SetActive(false);
            transform.position = _punchAt;
            transform.rotation = Quaternion.LookRotation(_travel.sqrMagnitude > .0001f ? _travel : Vector3.forward, Vector3.up);

            var puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.name = "Puddle";
            Object.Destroy(puddle.GetComponent<Collider>());
            puddle.transform.SetParent(transform, false);
            puddle.transform.localPosition = new Vector3(0, .02f, 0);
            puddle.transform.localScale = new Vector3(SeanRules.FistPunchRadius * 2.2f, .02f, SeanRules.FistPunchRadius * 2.2f);
            VfxMaterial.Ghost(puddle.GetComponent<Renderer>(), LavaEdge, 1.0f);
            _puddle = puddle.transform;

            var giant = new GameObject("GiantFist").transform;
            giant.SetParent(transform, false);
            Block("Forearm", giant, new Vector3(.9f, 1.3f, .9f), new Vector3(0, .65f, 0), LavaEdge);
            Block("Fist", giant, new Vector3(1.15f, .7f, 1.1f), new Vector3(0, 1.6f, 0), LavaCore);
            giant.localScale = new Vector3(1, 0, 1);
            _giant = giant;
            _glow = GetComponentsInChildren<Renderer>();

            NetCue.Play("sfx_explosion_heavy", _punchAt);
            HeroHazards.CreateExplosionVisual(_punchAt, SeanRules.FistPunchRadius, "", HeroHazards.ExplosionStyle.Fire);
            HostResolvePunch();
        }

        private void HostResolvePunch()
        {
            if (!NetAuthority.ShouldResolve() || GameServices.Round?.RoundActive != true || _owner == null) return;
            var can = GameServices.Round.Lata;
            Vector3 canAt = can != null ? can.transform.position : _punchAt - _travel;
            bool landed = false;
            foreach (var p in SeanHeroKit.Rivals(_owner))
            {
                Vector3 to = p.transform.position - _punchAt; to.y = 0;
                if (to.magnitude > SeanRules.FistPunchRadius) continue;
                SeanRules.FistPushDirection(p.transform.position.x, p.transform.position.z, canAt.x, canAt.z,
                    _travel.x, _travel.z, out float x, out float z);
                p.ApplyResolvedImpact(new Vector3(x, 0, z) * SeanRules.FistPushSpeed + Vector3.up * SeanRules.FistLiftSpeed);
                p.ApplyStagger(.5f);
                ComicPopup.Bonk(p.transform.position);
                landed = true;
            }
            if (landed) _kit?.HostAddHeat(_owner);
        }

        private void StepPunch(float dt)
        {
            _punchAge += dt;
            float rise = SeanRules.FistRiseSeconds, hold = SeanRules.FistHoldSeconds, cool = .45f;
            if (_giant != null)
            {
                float up = _punchAge < rise ? Mathf.SmoothStep(0, 1.12f, _punchAge / rise)
                         : _punchAge < rise + .08f ? Mathf.Lerp(1.12f, 1f, (_punchAge - rise) / .08f)
                         : _punchAge < rise + hold ? 1f
                         : Mathf.Lerp(1f, 0f, (_punchAge - rise - hold) / cool);
                _giant.localScale = new Vector3(1, Mathf.Max(0, up), 1);
            }
            // It cools to black glass as it sinks back into the puddle.
            float k = Mathf.Clamp01((_punchAge - rise - hold) / cool);
            if (k > 0 && _glow != null)
                foreach (var r in _glow) if (r != null && r.sharedMaterial != null) r.sharedMaterial.color = Color.Lerp(LavaEdge, Glass, k);
            float across = SeanRules.FistPunchRadius * 2.2f * (1 - .3f * k);
            if (_puddle != null) _puddle.localScale = new Vector3(across, .02f, across);
            if (_punchAge > rise + hold + cool + .35f) Object.Destroy(gameObject);
        }

        /// <summary>It lost its target or hit a wall: a small splash and it is gone.</summary>
        private void Fizzle()
        {
            HeroHazards.CreateExplosionVisual(transform.position, .6f, "", HeroHazards.ExplosionStyle.Ignition);
            Object.Destroy(gameObject);
        }
    }
}
