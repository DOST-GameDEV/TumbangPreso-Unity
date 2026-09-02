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
        private GameObject _motifGo;
        private Renderer _rimRenderer;
        private Renderer _fillRenderer;
        private Renderer _centreRenderer;
        private Renderer _crownRenderer;
        private Renderer _beaconRenderer;
        private Renderer _motifRenderer;

        // -------------------------------------------------------------------
        // § THE PREVIEW IS DRAWN IN THE HERO'S OWN LANGUAGE, IN CHALK
        //
        // ⚠️⚠️ 🧑 2026-09-02, having been given a hero-coloured ring: *"show like a proper visual
        // indiicator of how skill will land or how it will look like IN THEIR THEME OR SMTH"*,
        // *"I dont wannt just a fkn shadow like old phaister Q hold"*. The 2026-08-27 pass fixed
        // the EMISSION, which is why the ring stopped reading as a literal shadow, and left the
        // SHAPE alone: one annulus, one tick crown and one wash for all six heroes and all
        // eighteen powers. A telegraph that is the same object whichever hero you are playing
        // tells the player nothing about the power except where it is, and "where" was never the
        // interesting half of a hex, a fissure or a barricade.
        //
        // ⚠️⚠️ SO THE STYLE IS THE HERO'S MOTIF AND THE COLOUR IS CHALK, AND THE SPLIT IS THE
        // SECOND HALF OF THE SAME REPORT: *"make sure its diff from the actual skill cast to"*,
        // *"make it a diff color, bcz it might be confusing if its the same skill already"*.
        // Before this the held ring took the hero's BRIGHT accent and the ward the hex leaves on
        // the road takes the hero's accent, so a held preview and a live hazard two metres apart
        // were the same shape in the same colour and the player had to guess which one would
        // trip them.
        //
        //   * The **shape** says which power this is. It is built from the same `VfxShapes`
        //     generator the effect itself uses, so the preview is recognisably the thing.
        //   * The **colour** says whether it is real yet. Chalk cream, breathing, for a plan;
        //     the hero's own accent, solid, for anything that has landed.
        //
        // ⚠️ CHALK IS NOT AN ARBITRARY UI COLOUR HERE. `docs/VISION.md` § 2 names the chalk and
        // the road, and the arena's own box is chalked on asphalt: a plan drawn in chalk over a
        // street game is the game's own vocabulary for "this is where it is going to go", and it
        // is the one thing on the ground that no ability ever leaves behind.
        //
        // ⚠️ THE WASH KEEPS THE HERO ACCENT AT 0.10. It is the "am I inside it" surface rather
        // than the identity, and stripping the colour out of it too would make every preview in
        // the game one object again, one layer down.
        // -------------------------------------------------------------------

        /// <summary>
        /// Which motif the ground preview wears. One per hero, not one per ability.
        ///
        /// ⚠️ PER HERO RATHER THAN PER ABILITY, DELIBERATELY. A motif per power is eighteen
        /// shapes to author and eighteen to keep in step with what `OnActivate` spawns, which is
        /// exactly the drift `HeroAbility.TelegraphRadius` exists to stop. A motif per hero is
        /// six, it is the thing the player is actually learning to recognise across a match, and
        /// a kit that forgets to set one falls back to <see cref="Ring"/> and looks like the game
        /// did before.
        /// </summary>
        public enum Style
        {
            /// <summary>Rim, tick crown, wash. The fallback, and Classic's.</summary>
            Ring,

            /// <summary>An inscribed ward, turning slowly. Phaister.</summary>
            Ward,

            /// <summary>A frost rosette with a hard toothed edge. Cheska.</summary>
            Frost,

            /// <summary>Split ground: a few thick cracks from the centre. Dante.</summary>
            Fissure,

            /// <summary>Many thin arcs, crackling. Zack.</summary>
            Storm,

            /// <summary>Scattered embers thickening toward the rim. Sean.</summary>
            Ember,

            /// <summary>A torn hollow ring, the mouth of something. Nemu.</summary>
            Maw,
        }

        private Style _style = Style.Ring;

        /// <summary>Which motif the current mesh was built for, so it is rebuilt only on a change.</summary>
        private Style _motifBuiltFor = Style.Ring;

        /// <summary>
        /// The preview's stroke colour: chalk on asphalt.
        ///
        /// ⚠️ IT IS `UiTheme.Cream`, THE SAME CREAM AS THE HUD'S TYPE, rather than white. White
        /// on a sunlit court is a blown highlight and on Ilalim ng Tulay it is the only pure
        /// value in the frame; the game's cream is what every other readable thing on screen is
        /// already drawn in.
        /// </summary>
        private static Color Chalk => UiTheme.Cream;

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

        private bool _built;

        /// <summary>
        /// Build the geometry now rather than at <c>Awake</c>.
        ///
        /// ⚠️⚠️ AN EDITOR PROBE NEVER GETS AN `Awake`, AND WITHOUT THIS THE TELEGRAPH CANNOT BE
        /// PHOTOGRAPHED AT ALL. Unity only runs `Awake` in play mode for a component without
        /// `[ExecuteAlways]`, so `AddComponent<GroundReticle>()` in an edit-mode capture returns
        /// an object with no rim, no crown, no fill and no beacon: the probe would frame an empty
        /// patch of road and report success. `ViewmodelArms.EnsureBuilt` exists for the same
        /// reason and this is the same shape.
        ///
        /// ⚠️ IDEMPOTENT, so `Awake` calling it in the game and a probe calling it first cannot
        /// produce two sets of meshes.
        /// </summary>
        public void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            Build();
        }

        private void Awake() => EnsureBuilt();

        private void Build()
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

            // ⚠️ THE MOTIF IS ONE OBJECT WHOSE MESH IS SWAPPED, NOT SIX OBJECTS SWITCHED ON AND
            // OFF. A reticle lives on every character in the match, and six generated meshes per
            // character that five-sixths of the time nobody draws is memory and build time spent
            // on a hero this seat is not playing. `RebuildMotif` runs when the style actually
            // changes, which is once per ability the player aims.
            _motifGo = VfxShapes.Lay(transform, "ReticleMotif", MotifMesh(Style.Ring),
                                     1.0f, 0.034f);
            _motifRenderer = _motifGo.GetComponent<Renderer>();
            VfxMaterial.Ghost(_motifRenderer, Chalk, 1.70f);
            _motifGo.SetActive(false);

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

        /// <summary>
        /// Which motif to draw for the ability currently being telegraphed.
        ///
        /// ⚠️ THE RIM AND THE CROWN GO AWAY UNDER A MOTIF RATHER THAN SITTING BEHIND IT. Two
        /// concentric edge treatments 10 per cent apart is the flat-blob read this class's own
        /// note opens with, and every motif carries an edge of its own. The wash and the centre
        /// pip stay: the wash is the "am I inside it" surface and the pip is the aim point, and
        /// no motif has either.
        /// </summary>
        public void SetStyle(Style style) => _style = style;

        /// <summary>
        /// The mesh for one hero's motif.
        ///
        /// ⚠️⚠️ EVERY ONE IS THE SAME GENERATOR THE HERO'S OWN EFFECTS USE, AT A THINNER BAR.
        /// That is what makes a preview read as *that power about to happen* rather than as a
        /// second unrelated decal: `HeroHazards.SpawnHexSigil` draws `WardCircle`, Dante's
        /// fissure draws `Fracture`, Sean's burning ground draws `Cinder`. The bar is thinner
        /// and the colour is chalk, so it is unmistakably the plan and not the thing.
        ///
        /// ⚠️ THE SEEDS ARE FIXED CONSTANTS, NOT THE OWNER'S SLOT. This mesh is drawn every
        /// frame the key is held; a seed that varied per cast would reshuffle the inscription
        /// under the player's eye while they were trying to aim with it.
        /// </summary>
        private static Mesh MotifMesh(Style style)
        {
            switch (style)
            {
                // Twelve inscribed cells and four medallions: the hex's own ward, one bar-width
                // lighter than `SpawnHexSigil` draws it.
                case Style.Ward:
                    return VfxShapes.WardCircle(12, 4, 0.022f, 3);

                // ⚠️ A HARD TOOTHED EDGE AND A DEEP INNER RATIO. Frost is read at its boundary,
                // which is the one thing a player sliding toward it needs, and 0.66 leaves the
                // interior open enough to see a body standing in it.
                case Style.Frost:
                    return VfxShapes.Corona(20, 0.66f, 0.30f, 11);

                // ⚠️ SIX THICK ARMS FROM THE CENTRE, WHICH IS WHAT A SPLIT LOOKS LIKE FROM
                // ABOVE. `from: 0.10` keeps them off the very centre so the aim pip stays
                // readable inside them.
                case Style.Fissure:
                    return VfxShapes.Fracture(6, 3, 0.048f, 23, 0.10f);

                // ⚠️ THE SAME GENERATOR AS THE FISSURE AT NINE THIN ARMS AND FOUR LEVELS OF
                // BRANCHING, AND THE DIFFERENCE IS LEGIBLE AT A GLANCE. Dante's is a few heavy
                // splits; this is a crackle. One shape family, two readings, no third mesh to
                // keep in step with anything.
                case Style.Storm:
                    return VfxShapes.Fracture(9, 4, 0.016f, 47, 0.04f);

                // Embers thickening toward the rim, which is where Sean's burning ground is
                // densest.
                case Style.Ember:
                    return VfxShapes.Cinder(4, 11, 0.46f, 59);

                // A torn ring with the middle gone: the mouth of the seance rather than a disc.
                case Style.Maw:
                    return VfxShapes.Hollow(44, 0.58f, 0.22f, 71);

                default:
                    return VfxShapes.Collar(64, 0.05f, 0.955f);
            }
        }

        /// <summary>
        /// Swap the motif mesh when the aimed ability changes hero.
        ///
        /// ⚠️ THE OLD MESH IS DESTROYED WITH THE SWAP. `VfxShapes.Own` ties a generated mesh to
        /// the object that draws it, and that only fires when the OBJECT dies; this object
        /// outlives the match, so replacing `sharedMesh` without freeing the old one leaks a
        /// mesh per style change for the whole session. `VfxShapes.Own`'s own note has the
        /// arithmetic for why that class of leak is not a tidiness point here.
        /// </summary>
        private void RebuildMotif()
        {
            if (_motifGo == null || _motifBuiltFor == _style) return;

            _motifBuiltFor = _style;

            var filter = _motifGo.GetComponent<MeshFilter>();
            if (filter == null) return;

            var old = filter.sharedMesh;
            filter.sharedMesh = MotifMesh(_style);
            if (old != null) Destroy(old);
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

        // -------------------------------------------------------------------
        // § THE HELD RING IS PRIVATE TO WHOEVER IS AIMING IT
        //
        // ⚠️⚠️ 🧑 2026-08-27, on Phaister's blink: *"make sure only she can see it"*. An AIM is a
        // decision that has not been made yet, and painting it on the road tells the other three
        // players where somebody is about to teleport BEFORE they commit to it. That is strictly
        // worse than no telegraph: it hands away the one thing a hold-to-aim power buys, which is
        // that you can change your mind.
        //
        // ⚠️⚠️ AND IT IS ONLY THE HELD RING. `Flash` is the post-cast confirmation and stays
        // visible to everybody, deliberately: by then the power has landed, and "where did that
        // go off" is a question all four players need answered. The two halves of this class were
        // always different features (see the class note); this is the first thing that treats
        // them differently.
        //
        // ⚠️ ASKED OF THE CAMERA RIG RATHER THAN OF A NETWORK ROLE. `CameraRig.IsFollowing` is
        // true for exactly the body being looked through, which is the correct answer offline,
        // online, and after the debug switcher hands the player a different seat. A
        // `NetAuthority.LocalSlot` test would be wrong in all three of the last cases.
        // -------------------------------------------------------------------

        private CameraSystem.CameraRig _rig;
        private float _rigSearchAt = -100.0f;

        /// <summary>Is the player looking through this reticle's owner right now?</summary>
        private bool OwnerIsBeingDriven()
        {
            // No owner means a probe or a preview stage built this: draw, or a render pipeline
            // test photographs an empty road.
            if (_owner == null) return true;

            // ⚠️ THE RIG IS RE-FOUND ON A CLOCK, NOT CACHED ONCE AND NOT SEARCHED EVERY FRAME.
            // `MatchInstaller` can destroy and rebuild it at a round boundary, so a permanent
            // cache goes stale; a `FindFirstObjectByType` per frame per character is the exact
            // per-frame cost `CLAUDE.md` § 7.1 records the HUD being caught for.
            if (_rig == null && Time.unscaledTime - _rigSearchAt > 0.5f)
            {
                _rigSearchAt = Time.unscaledTime;
                _rig = FindFirstObjectByType<CameraSystem.CameraRig>();
            }

            // No rig at all is the spectator and the headless case. A spectator drives nobody,
            // so nobody's aim is theirs to see.
            return _rig != null && _rig.IsFollowing(_owner);
        }

        /// <summary>Draw the ring under a held key. Called every frame it is held.</summary>
        public void Show(Vector3 worldPos, float radius, Color colour)
        {
            if (!OwnerIsBeingDriven())
            {
                // ⚠️ THE FLASH IS LEFT ALONE. `Hide` already refuses to switch the object off
                // while a confirmation is running, which is what keeps a bot's landed cast
                // visible to the human watching it.
                Hide();
                return;
            }

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

            // ⚠️ THE TWO FACES ARE SWITCHED, NOT LAYERED. See `SetStyle`.
            RebuildMotif();

            bool motif = _style != Style.Ring;
            if (_rimGo != null && _rimGo.activeSelf == motif) _rimGo.SetActive(!motif);
            if (_crownGo != null && _crownGo.activeSelf == motif) _crownGo.SetActive(!motif);
            if (_motifGo != null && _motifGo.activeSelf != motif) _motifGo.SetActive(motif);

            if (_rimGo != null) _rimGo.transform.localScale = new Vector3(_radius, 1.0f, _radius);
            if (_crownGo != null) _crownGo.transform.localScale = new Vector3(_radius * 1.10f, 1.0f, _radius * 1.10f);
            if (_centreGo != null) _centreGo.transform.localScale = new Vector3(_radius * 0.16f, 1.0f, _radius * 0.16f);
            if (_fillGo != null)
                _fillGo.transform.localScale = new Vector3(diameter * FillRatio, 0.02f, diameter * FillRatio);

            if (_motifGo != null && motif)
            {
                _motifGo.transform.localScale = new Vector3(_radius, 1.0f, _radius);

                // ⚠️ IT TURNS, AND SLOWLY, BECAUSE A STATIC INSCRIPTION ON ASPHALT IS A STAIN.
                // 18 degrees a second is a third of a turn while the reach is still ramping, so
                // it reads as alive without ever spinning fast enough to smear the glyphs.
                //
                // ⚠️⚠️ AND THE TURN IS THE THIRD THING SEPARATING A PREVIEW FROM A LANDED
                // EFFECT, after the shape family and the chalk. Every hazard this game puts on
                // the road is deliberately STATIC (`HeroHazards`: *"rectilinear, dense, WRITTEN,
                // and STATIC"*), because a hazard that moves invites the player to read the
                // movement as its edge advancing. Nothing that is really there rotates, so
                // anything rotating is not there yet. 🧑 2026-09-02: *"make sure its diff from
                // the actual skill cast to"*.
                _motifGo.transform.localRotation = Quaternion.Euler(0.0f, Time.time * 18.0f, 0.0f);
            }

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
            Color stroke = _flashLeft > 0.0f ? _colour : Chalk;

            Tint(_rimRenderer, stroke, 0.95f * alpha * breathe);
            Tint(_crownRenderer, stroke, 0.70f * alpha * breathe);
            Tint(_fillRenderer, _colour, 0.12f * alpha * breathe);
            Tint(_centreRenderer, stroke, 0.90f * alpha);

            // ⚠️ THE MOTIF IS TINTED LIKE THE RIM AND NOT LIKE THE CROWN. It IS the edge under
            // this style, so anything dimmer would be the shadow read all over again, which is
            // the whole fault this file was rewritten for.
            if (_motifGo != null && _motifGo.activeSelf)
                Tint(_motifRenderer, stroke, 0.95f * alpha * breathe);

            if (_beaconGo != null && _beaconGo.activeSelf)
                Tint(_beaconRenderer, stroke, 0.88f * breathe);
        }

        private void Tint(Renderer target, Color rgb, float alpha)
        {
            if (target == null) return;

            var colour = new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(alpha));
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
