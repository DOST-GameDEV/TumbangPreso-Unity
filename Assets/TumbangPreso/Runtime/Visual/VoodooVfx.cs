using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️ PHAISTER'S VOODOO WORLD OBJECTS (ABILITY-2): the cursed doll, the pin's cone and HIGOP's black
    /// hole. Gameplay is resolved on the host by distance; the bodies here are first-pass shapes in her
    /// magenta and stitched cloth, placeholders for the presentation pass (plan § 5, `ultimates.md` § 3.3),
    /// recorded as such in TODO ABILITY-2.
    /// </summary>
    public sealed class VoodooDoll : MonoBehaviour
    {
        private Vector3 _velocity;
        private int _owner;
        private bool _done;
        private Visual.PhaisterManika _manika;

        public static VoodooDoll Spawn(Vector3 origin, Vector3 target, int ownerSlot)
        {
            var go = new GameObject("VoodooDoll");
            go.transform.position = origin;
            var d = go.AddComponent<VoodooDoll>();
            d._owner = ownerSlot;
            // ⚠️⚠️ FIXED 2026-09-27 (HERO-10, film v1): `Slipper.SolveArc` RETURNS A UNIT DIRECTION, NOT A VELOCITY. The first
            // pass assigned it straight to `_velocity`, so every doll left her hand at 1 m/s instead of `DollSpeed` (12) and
            // dropped half a metre in front of her feet: MANIKA MISCHIEF could never reach anyone. `Slipper` scales it by the
            // throw speed itself; so must this.
            d._velocity = Slipper.SolveArc(origin, target, VoodooRules.DollSpeed) * VoodooRules.DollSpeed;
            // ⚠️ HERO-10: EVERYTHING SEEN IS `Visual.PhaisterManika` (the modelled manika, its trail, the steal, the return to her
            // hand, the hold, the crumble). This object is only the flight and the host's hit test.
            d._manika = Visual.PhaisterManika.Spawn(ownerSlot);
            d._manika.Follow(origin, Quaternion.identity);
            return d;
        }

        private void FixedUpdate()
        {
            if (_done) return;
            float dt = Time.fixedDeltaTime;
            _velocity.y -= Balance.Gravity * dt;
            transform.position += _velocity * dt;
            transform.Rotate(Vector3.right, 540f * dt, Space.Self);
            if (_manika != null) _manika.Follow(transform.position, transform.rotation);
            bool landed = transform.position.y <= Slipper.GroundY(transform.position) + 0.1f && _velocity.y < 0f;
            if (NetAuthority.ShouldResolve())
            {
                var round = GameServices.Round;
                if (round != null)
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == _owner) continue;
                        Vector3 d = p.transform.position + Vector3.up * 0.8f - transform.position;
                        float reach = landed ? VoodooRules.DollHitRadius + 0.8f : VoodooRules.DollHitRadius;
                        if (d.magnitude > reach) continue;
                        p.ApplyDisoriented();
                        Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.HeroCursed, _owner, p.PlayerSlot, p.transform.position, StatusRules.DisorientedSeconds);
                        landed = true;
                        break;
                    }
            }
            if (landed || transform.position.y < -20f)
            {
                _done = true;
                if (_manika != null) _manika.Landed(transform.position);
                // ⚠️ HERO-10: its own landing sound, no longer the blink's arrival (the method: no two verbs share a recipe).
                GameServices.Audio?.PlayAt("sfx_phaister_manika_land", transform.position);
                Destroy(gameObject, 0.25f);
            }
        }
    }

    /// <summary>
    /// OMEN (was HIGOP), the black eye. Her slow cast, then `HeroHazards.SeanceVoidComponent` (the same pull Kuro's
    /// maw uses, on every peer for its own body and the host for all) drags every other body and every
    /// loose slipper that is not hers to the heart. The pull outruns a run, so pushing away gains a few
    /// steps and loses them (owner: *"they can try to move away but they js get sucked back in"*).
    /// </summary>
    public sealed class VoodooBlackHole : MonoBehaviour
    {
        private float _age, _castLeft, _life, _eyeHeight = VoodooRules.HigopMinHeight;
        private bool _pulling;
        private int _owner;

        /// <summary>
        /// ⚠️ HERO-10: THIS IS ONLY THE TIMING AND THE PULL NOW. Everything seen (the sigil ring, the ribbons off her, the
        /// butterflies, the black eye and its corona, the landing, the maelstrom, the burst into the sky) is
        /// `Visual.PhaisterOmen`, a separate object on its own clock, so nothing here scales the presentation.
        /// </summary>
        public static GameObject Spawn(Vector3 at, int ownerSlot, float castSeconds, float pullSeconds)
        {
            var go = new GameObject("Higop");
            go.transform.position = Visual.VfxShapes.GroundPoint(at);
            var h = go.AddComponent<VoodooBlackHole>();
            h._owner = ownerSlot; h._castLeft = castSeconds; h._life = pullSeconds;
            // ⚠️ HERO-10: THE AIM'S y IS THE EYE'S HEIGHT (she looks up to hang it higher); the hole itself sits on the court under it.
            h._eyeHeight = Mathf.Clamp(at.y - go.transform.position.y, VoodooRules.HigopMinHeight, VoodooRules.HigopMaxHeight);
            var owner = GameServices.Round?.PlayerAt(ownerSlot);
            var omen = Visual.PhaisterOmen.Play(go.transform.position, owner != null ? owner.transform : null, castSeconds, pullSeconds, 0f, h._eyeHeight);
            omen.transform.SetParent(go.transform, true);
            return go;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (!_pulling)
            {
                _castLeft -= Time.deltaTime;
                if (_castLeft <= 0.0f) BeginPull();
            }
            else
            {
                _life -= Time.deltaTime;
                if (_life <= 0.0f)
                {
                    GameServices.Audio?.PlayAt("sfx_phaister_higop_close", transform.position);
                    // The presentation plays its end (the butterflies bursting up) after the pull stops: let it go first.
                    foreach (var omen in GetComponentsInChildren<Visual.PhaisterOmen>()) omen.transform.SetParent(null, true);
                    Destroy(gameObject);
                }
            }
        }

        private void BeginPull()
        {
            _pulling = true; _age = 0.0f;
            GameServices.Audio?.PlayAt("sfx_phaister_higop_open", transform.position);
            var pull = gameObject.AddComponent<HeroHazards.SeanceVoidComponent>();
            pull.Radius = VoodooRules.HigopRadius; pull.Duration = _life; pull.OwnerSlot = _owner;
            // HERO-10: caught bodies are held with their chests at the eye, so an eye hung high puts them in the air.
            pull.PullStrength = 50.0f; pull.LiftHeight = Mathf.Max(0.0f, _eyeHeight - VoodooRules.HigopLiftBelowEye); pull.SlipperPull = 10.0f;
            pull.SparedSlipperOwner = _owner; pull.DrowseEvery = 0.0f;
            pull.HoldRadius = VoodooRules.HigopHoldRadius;
            HazardVolume.Attach(gameObject, VoodooRules.HigopRadius, _owner);
        }
    }
}
