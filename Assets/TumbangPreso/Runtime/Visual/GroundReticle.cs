using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The ground telegraph: where an area power is about to land, and where it just did.
    ///
    /// ⚠️⚠️ IT DRAWS TWO DIFFERENT THINGS AND THEY ARE NOT THE SAME FEATURE. `Show` is the ring
    /// under a held key, redrawn every frame while the player is deciding. `Flash` is the
    /// confirmation left behind after a cast, which fades on its own. The second one exists
    /// because the first was UNREACHABLE for every power in the game: all of them fire on the
    /// press edge and resolve instantly, so a ring drawn only while the key is down appears on
    /// the frame the ability already went off and vanishes when the finger lifts. On a tap that
    /// is one or two frames. Players never saw it, which is most of why area powers felt like
    /// they went off somewhere vague.
    ///
    /// ⚠️ THE RADIUS AND THE OFFSET ARE THE ABILITY'S, NOT THIS CLASS'S. See
    /// `HeroAbility.TelegraphRadius`: nine of the twelve numbers this used to be handed
    /// disagreed with what the ability actually spawned.
    /// </summary>
    public sealed class GroundReticle : MonoBehaviour
    {
        // ⚠️ A DISC AND A RIM, NOT A DISC AND A SLIGHTLY BIGGER DISC. The old pair were both
        // solid cylinders 10% apart, which draws as one flat blob with no boundary at all, and
        // the boundary is the entire information: a player needs to know whether they are IN
        // it. The rim is the outer radius at high alpha, the fill is 84% of it at low, and the
        // 16% gap between them is what reads as an edge.
        private const float FillRatio = 0.84f;

        private GameObject _rimGo;
        private GameObject _fillGo;
        private GameObject _centreGo;
        private GameObject _crownGo;
        private GameObject _beaconGo;
        private Renderer _rimRenderer;
        private Renderer _fillRenderer;
        private Renderer _centreRenderer;
        private Renderer _crownRenderer;
        private Renderer _beaconRenderer;

        /// <summary>
        /// Whether this ability wants a standing mark at the destination as well as a ring.
        ///
        /// ⚠️⚠️ IT EXISTS FOR THE ONE ABILITY YOU AIM AT A PLACE YOU ARE GOING TO BE. 🧑
        /// 2026-08-27, on Phaister's blink: *"to teleport u have to hold her E skill and all it
        /// shows is a frigging shadow, it's very easy to miss and not in her theme at all"*. A
        /// flat ring on the road is the right telegraph for a power that LANDS somewhere and the
        /// wrong one for a power that puts YOU somewhere: the player is looking at the street
        /// ahead, not at their own feet, and a decal seen at a glancing angle from three metres
        /// up is a smudge. Something with height is visible from where the decision is made.
        ///
        /// ⚠️ IT IS A `Rift`, WHICH IS HER OWN SHAPE. `HeroHazards.SpawnShadowRift` tears the
        /// same sheet at the place she LEAVES, so the aim mark and the departure are recognisably
        /// one power rather than a generic ring plus an effect. That answers the second half of
        /// the report, which is about theme rather than legibility.
        /// </summary>
        private bool _wantsBeacon;

        private float _radius = 3.0f;
        private Color _colour = UiTheme.HeroEarth;

        /// <summary>Set by `Show` every frame the key is held, cleared by `Hide`.</summary>
        private bool _held;

        /// <summary>Seconds of post-cast confirmation left, and what it started at.</summary>
        private float _flashLeft;
        private float _flashTotal;

        private CharacterMotor _owner;

        public static GroundReticle Create(Transform parent)
        {
            var go = new GameObject("~GroundReticle");
            if (parent != null) go.transform.SetParent(parent, false);

            var reticle = go.AddComponent<GroundReticle>();
            reticle._owner = parent != null ? parent.GetComponentInParent<CharacterMotor>() : null;
            return reticle;
        }

        // -------------------------------------------------------------------
        // § THE RETICLE WAS THREE FLAT DISCS AND IT READ AS A SHADOW
        //
        // ⚠️⚠️ 🧑 2026-08-27, on Phaister's blink: *"all it shows is a frigging shadow, it's very
        // easy to miss"*. Three `PrimitiveType.Cylinder` plates, ghosted, at emission 0.5, is a
        // grey smear on asphalt and a genuinely dark one on Ilalim ng Tulay, where the whole
        // street is under a viaduct. It is also the exact construction `docs/TODO.md` § 19 named
        // as the game's default mistake: *"a flat coloured plane"*, made three times.
        //
        // ⚠️⚠️ WHAT CARRIES A TELEGRAPH IS ITS EDGE, NOT ITS AREA. A player needs to know whether
        // they are IN it, which is a boundary question, so the ring is now a real annulus at high
        // emission and the interior is a wash at alpha 0.12 rather than a lid over the court. The
        // tick crown gives the eye something to catch at a glancing angle, which is the angle a
        // first-person player actually sees the ground at.
        //
        // ⚠️ THE COLOUR IS THE HERO'S ACCENT AND ALWAYS WAS. It never reached the screen because
        // the emission did not: 0.5 of a colour under a dark map is that colour's shadow.
        // -------------------------------------------------------------------

        private void Awake()
        {
            // ⚠️ THE RIM AND THE CROWN ARE UNIT-RADIUS MESHES SCALED BY `Place`, and the fill is
            // still a primitive because a solid disc is genuinely what a wash wants. Three
            // different jobs, three different constructions.
            _rimGo = Ring("ReticleRim", VfxShapes.Collar(64, 0.05f, 0.955f), 0.032f);
            _crownGo = Ring("ReticleCrown", VfxShapes.Wedges(16, 0.86f, 12.0f), 0.030f);
            _fillGo = Disc("ReticleFill", 0.022f);
            _centreGo = Ring("ReticleCentre", VfxShapes.Collar(24, 0.05f, 0.0f), 0.038f);

            _rimRenderer = _rimGo.GetComponent<Renderer>();
            _crownRenderer = _crownGo.GetComponent<Renderer>();
            _fillRenderer = _fillGo.GetComponent<Renderer>();
            _centreRenderer = _centreGo.GetComponent<Renderer>();

            _beaconGo = VfxShapes.Stand(transform, "ReticleBeacon",
                                        VfxShapes.TwoSided(VfxShapes.Rift(11, 0.30f, 0.44f, 0.05f, 91)),
                                        0.62f, heightScale: 2.10f);
            _beaconRenderer = _beaconGo.GetComponent<Renderer>();
            VfxMaterial.Ghost(_beaconRenderer, _colour, 1.60f);
            _beaconGo.SetActive(false);

            gameObject.SetActive(false);
        }

        private GameObject Ring(string name, Mesh mesh, float lift)
        {
            var go = VfxShapes.Lay(transform, name, mesh, 1.0f, lift);
            VfxMaterial.Ghost(go.GetComponent<Renderer>(), _colour, 1.40f);
            return go;
        }

        /// <summary>Turns the standing mark on or off for the ability currently being aimed.</summary>
        public void SetBeacon(bool wanted)
        {
            _wantsBeacon = wanted;
            if (_beaconGo != null && !wanted) _beaconGo.SetActive(false);
        }

        private GameObject Disc(string name, float lift)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.0f, lift, 0.0f);

            // ⚠️⚠️ `VfxMaterial.Ghost` OR THIS WHOLE THING IS AN OPAQUE PLATE ON THE FLOOR. A
            // primitive comes back on the built-in `Default-Material`, which is the Standard
            // shader in OPAQUE mode, and writing an alpha into `material.color` there does
            // nothing whatsoever. All three of these discs were authored at 0.25 to 0.75 alpha
            // and all three rendered solid, so the "telegraph" was a painted lid over the patch
            // of court the player was trying to look at. It also strips the collider the
            // primitive arrives with, which a decal must never keep.
            VfxMaterial.Ghost(go.GetComponent<Renderer>(), _colour);

            return go;
        }

        /// <summary>Draw the ring under a held key. Called every frame it is held.</summary>
        public void Show(Vector3 worldPos, float radius, Color colour)
        {
            _held = true;
            _radius = Mathf.Max(0.2f, radius);
            _colour = colour;
            Place(worldPos);
        }

        /// <summary>Stop drawing the held ring. A running confirmation flash is unaffected.</summary>
        public void Hide()
        {
            _held = false;
            if (_beaconGo != null) _beaconGo.SetActive(false);
            if (_flashLeft <= 0.0f && gameObject.activeSelf) gameObject.SetActive(false);
        }

        /// <summary>
        /// Leave the ring behind for a moment after a cast landed.
        ///
        /// ⚠️ IT OUTLIVES `Hide`. The caster releases the key on the same frame it fires, so a
        /// flash that any subsequent `Hide` could cancel would be no better than the held ring
        /// it exists to replace.
        /// </summary>
        public void Flash(Vector3 worldPos, float radius, Color colour, float seconds)
        {
            _radius = Mathf.Max(0.2f, radius);
            _colour = colour;
            _flashTotal = Mathf.Max(0.05f, seconds);
            _flashLeft = _flashTotal;
            Place(worldPos);
        }

        /// <summary>
        /// ⚠️ DRIVEN BY THE OWNER, NOT BY `Update`. The reticle is a child of the character, and
        /// `HeroAbilitySystem` already runs one ordered pass per frame that decides whether a
        /// ring is wanted; a second `Update` here would race it and produce a one-frame flicker
        /// every time a key went down.
        /// </summary>
        public void Tick(float dt)
        {
            if (_flashLeft > 0.0f)
            {
                _flashLeft = Mathf.Max(0.0f, _flashLeft - dt);
                if (_flashLeft <= 0.0f && !_held)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }

            if (!gameObject.activeSelf) return;

            Paint();
        }

        private void Place(Vector3 worldPos)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            transform.position = GroundUnder(worldPos);
            transform.rotation = Quaternion.identity;

            // Both the rim and the crown carry the SAME outer measurement, so a caller reading
            // the ring learns the real radius rather than the radius of whichever piece they
            // happened to look at. ⚠️ THE MESH PIECES ARE UNIT-RADIUS AND THE PRIMITIVE IS
            // UNIT-DIAMETER, which is why one gets `_radius` and the other `_radius * 2`.
            float diameter = _radius * 2.0f;
            if (_rimGo != null) _rimGo.transform.localScale = new Vector3(_radius, 1.0f, _radius);
            if (_crownGo != null) _crownGo.transform.localScale = new Vector3(_radius * 1.10f, 1.0f, _radius * 1.10f);
            if (_centreGo != null) _centreGo.transform.localScale = new Vector3(_radius * 0.16f, 1.0f, _radius * 0.16f);
            if (_fillGo != null)
                _fillGo.transform.localScale = new Vector3(diameter * FillRatio, 0.02f, diameter * FillRatio);

            if (_beaconGo != null)
            {
                bool show = _wantsBeacon && _held;
                if (_beaconGo.activeSelf != show) _beaconGo.SetActive(show);

                if (show)
                {
                    // ⚠️ IT STANDS AT THE CENTRE AND TURNS SLOWLY. A still upright plate seen
                    // edge-on is invisible from exactly one angle, and that angle is common in a
                    // game where everybody is looking along the street.
                    _beaconGo.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                    _beaconGo.transform.localRotation = Quaternion.Euler(0.0f, Time.time * 95.0f, 0.0f);
                    float lift = 0.85f + Mathf.Sin(Time.time * 6.5f) * 0.10f;
                    _beaconGo.transform.localScale = new Vector3(_radius * 0.55f, lift, _radius * 0.55f);
                }
            }

            Paint();
        }

        /// <summary>
        /// Drop the decal onto whatever the court is here.
        ///
        /// ⚠️⚠️ IT SKIPS THE CASTER'S OWN CAPSULE, AND WITHOUT THAT THE RING SAT ON THE PLAYER'S
        /// HEAD. Every self-centred power (Dante's stomp, Zack's grind, Sean's supernova) asks
        /// for a decal at the caster's feet, and a ray fired two metres above that point going
        /// down hits the caster's own `CharacterController` capsule LONG before it reaches the
        /// asphalt. The decal then rendered at roughly chest height, moving with them, which
        /// looks like a bug in the effect rather than in the raycast.
        ///
        /// ⚠️ TRIGGERS ARE IGNORED. `HazardVolume` and the tag safe zone are triggers sitting on
        /// the floor of the arena; landing a telegraph on the lid of an earlier hazard would
        /// stack decals a few centimetres apart and z-fight.
        /// </summary>
        private Vector3 GroundUnder(Vector3 worldPos)
        {
            Vector3 from = worldPos + Vector3.up * 2.0f;
            var hits = Physics.RaycastAll(from, Vector3.down, 12.0f, ~0, QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            bool found = false;
            Vector3 best = worldPos;

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (_owner != null && hit.collider.GetComponentInParent<CharacterMotor>() == _owner) continue;
                if (hit.collider.GetComponentInParent<CharacterMotor>() != null) continue;
                if (hit.distance >= bestDistance) continue;

                bestDistance = hit.distance;
                best = hit.point;
                found = true;
            }

            if (!found)
            {
                best = worldPos;
                best.y = 0.0f;
            }

            best.y += 0.02f;
            return best;
        }

        private void Paint()
        {
            // A slow breath while the player is deciding; a hard bright edge on the confirm that
            // decays, so the two states are never mistaken for one another.
            float alpha = 1.0f;
            float breathe = 1.0f;

            if (_flashLeft > 0.0f)
            {
                alpha = _flashTotal > 0.0f ? _flashLeft / _flashTotal : 0.0f;
                alpha = alpha * alpha; // late fade, so it reads as a hit rather than a dissolve
            }
            else
            {
                breathe = 0.82f + Mathf.Sin(Time.time * 7.0f) * 0.18f;
            }

            // ⚠️ THE WASH IS 0.12, DOWN FROM 0.22, AND THE EDGE IS WHAT WENT UP. A telegraph that
            // tints the court is information; one that covers it is a lid, and the interior is
            // exactly the patch of ground the player is trying to read a body on.
            Tint(_rimRenderer, 0.95f * alpha * breathe);
            Tint(_crownRenderer, 0.70f * alpha * breathe);
            Tint(_fillRenderer, 0.12f * alpha * breathe);
            Tint(_centreRenderer, 0.90f * alpha);

            if (_beaconGo != null && _beaconGo.activeSelf) Tint(_beaconRenderer, 0.88f * breathe);
        }

        private void Tint(Renderer target, float alpha)
        {
            if (target == null) return;

            var colour = new Color(_colour.r, _colour.g, _colour.b, Mathf.Clamp01(alpha));
            var material = target.material;
            material.color = colour;

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            if (material.HasProperty("_EmissionColor"))
                // ⚠️⚠️ 1.60, UP FROM 0.50, AND THIS ONE NUMBER IS WHY THE TELEGRAPH READ AS A
                // SHADOW. Ghosted geometry is lit by its emission and almost nothing else; half a
                // colour on a street that is itself under a viaduct is that colour's shadow. 🧑
                // 2026-08-27, on Phaister's blink: *"all it shows is a frigging shadow"*.
                material.SetColor("_EmissionColor", new Color(_colour.r, _colour.g, _colour.b, 1.0f) * (1.60f * alpha));
        }
    }
}
