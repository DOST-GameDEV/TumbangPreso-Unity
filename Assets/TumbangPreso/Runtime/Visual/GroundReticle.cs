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
        private Renderer _rimRenderer;
        private Renderer _fillRenderer;
        private Renderer _centreRenderer;

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

        private void Awake()
        {
            _rimGo = Disc("ReticleRim", 0.030f);
            _fillGo = Disc("ReticleFill", 0.022f);
            _centreGo = Disc("ReticleCentre", 0.038f);

            _rimRenderer = _rimGo.GetComponent<Renderer>();
            _fillRenderer = _fillGo.GetComponent<Renderer>();
            _centreRenderer = _centreGo.GetComponent<Renderer>();

            _centreGo.transform.localScale = new Vector3(0.34f, 0.03f, 0.34f);

            gameObject.SetActive(false);
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

            // Both discs carry the SAME outer measurement, so a caller reading the ring learns
            // the real radius rather than the radius of whichever disc they happened to look at.
            float diameter = _radius * 2.0f;
            if (_rimGo != null) _rimGo.transform.localScale = new Vector3(diameter, 0.02f, diameter);
            if (_fillGo != null)
                _fillGo.transform.localScale = new Vector3(diameter * FillRatio, 0.02f, diameter * FillRatio);

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

            Tint(_rimRenderer, 0.85f * alpha * breathe);
            Tint(_fillRenderer, 0.22f * alpha * breathe);
            Tint(_centreRenderer, 0.90f * alpha);
        }

        private void Tint(Renderer target, float alpha)
        {
            if (target == null) return;

            var colour = new Color(_colour.r, _colour.g, _colour.b, Mathf.Clamp01(alpha));
            var material = target.material;
            material.color = colour;

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", new Color(_colour.r, _colour.g, _colour.b, 1.0f) * (0.5f * alpha));
        }
    }
}
