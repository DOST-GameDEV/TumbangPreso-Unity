using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The floor ring and the floating tag over a unit, converted from
    /// `scripts/characters/character_nameplate.gd`.
    ///
    /// §4.2: the ACCENT tracks role — blue for the taya, orange for an attacker. Team
    /// identity is never carried by hue.
    ///
    /// ⚠️ THE SEAT IS STABLE FOR THE MATCH; THE ROLE IS NOT. That split is the whole point of
    /// the tag: "P3" is who you have been chasing all match, "TAYA" is what they happen to be
    /// doing this round. Roles rotate every round, so the refresh cannot be a one-shot.
    /// </summary>
    public sealed class CharacterNameplate : MonoBehaviour
    {
        /// <summary>
        /// §4.5: "Fades out past ~15m." Without this every tag in the match renders at full
        /// opacity forever, and in a 40x40 arena that means four billboarded labels
        /// permanently stacked across the middle of a first-person view.
        /// </summary>
        public const float FadeStart = 12.0f;
        public const float FadeEnd = 18.0f;

        /// <summary>Ring radius as a multiple of the unit's own capsule radius.
        /// 0.55 / 0.4 = 1.375 for the Person, kept as a RATIO rather than a flat number so a
        /// smaller unit's ring reads as a ring around that unit, not a person-sized ring
        /// around a small object.</summary>
        public const float RingRadiusRatio = 1.375f;

        /// <summary>Small and absolute on purpose: it only has to clear the floor mesh, not
        /// scale with the unit.</summary>
        public const float RingFloorMargin = 0.02f;

        /// <summary>0.25 at Person scale is where this was tuned; a shorter unit wants a
        /// proportionally smaller gap, not the same 0.25 floating disconnected above it.</summary>
        public const float LabelMarginAtPersonScale = 0.25f;
        public const float PersonCapsuleHeight = 1.6f;

        /// <summary>`font_size = 32` at `pixel_size = 0.005` on the .tscn's Label3D.</summary>
        public const float LabelWorldHeight = 0.16f;
        public const int LabelFontSize = 96;

        private CharacterMotor _character;
        private Transform _ring;
        private Renderer _ringRenderer;
        private MeshFilter _ringFilter;
        private Mesh _discMesh;
        private MaterialPropertyBlock _ringBlock;
        private TextMesh _label;
        private Transform _labelTransform;
        private Color _roleColor = UiTheme.Defense;

        /// <summary>
        /// How many of THIS mesh's own radii make one unit of `localScale`. See `ApplySizing`:
        /// the cylinder primitive is built at radius 0.5 and the collar at radius 1.0.
        /// </summary>
        private float _ringUnitSpan = 2.0f;

        /// <summary>
        /// The taya's ring, generated once and shared by every unit that ever defends.
        ///
        /// ⚠️⚠️ `HideAndDontSave` AND A NULL RE-CHECK, WHICH IS THE SAME PAIR
        /// `InputLayer.TouchSkin.Alive` RECORDS AND FOR THE SAME REASON: a runtime-generated
        /// asset with no owner is destroyed by a scene load, and Unity reports a destroyed object
        /// as null, so the flag stops it dying and the null check rebuilds it if it does anyway.
        /// A mesh that has been destroyed under a live `MeshFilter` draws nothing at all, so the
        /// taya would simply have no marker for the rest of the match.
        ///
        /// ⚠️ 32 SIDES, AND IT IS A MARKER RATHER THAN AN EFFECT. Every other caller of
        /// `VfxShapes` picks a low count on purpose, because a faceted silhouette is this game's
        /// look; this one has to read as a clean circle at a glance from across a 14 m box while
        /// somebody is running at you, so it is the one place a high count is right.
        ///
        /// ⚠️ `innerRatio` 0.66 IS THE THICKNESS AND IT IS THE SIGNAL. Too thin and it disappears
        /// against the road at distance; too thick and it is a disc with a dot missing, which is
        /// the shape it has to be distinguishable FROM.
        /// </summary>
        private static Mesh _tayaRing;

        private static Mesh TayaRingMesh()
        {
            if (_tayaRing != null) return _tayaRing;

            _tayaRing = VfxShapes.Collar(sides: 32, height: 0.10f, innerRatio: 0.66f);
            _tayaRing.name = "TayaRing";
            _tayaRing.hideFlags = HideFlags.HideAndDontSave;

            return _tayaRing;
        }

        private void Awake()
        {
            _character = GetComponentInParent<CharacterMotor>();
            Build();
        }

        private void Start()
        {
            // ⚠️ SIZING RUNS IN Start, NOT Awake. The capsule this reads is resized by the
            // unit's own role setup, and in Godot this exact ordering was the bug: a child
            // readies before its parent, so sizing in Awake reads the prefab's default
            // capsule on every unit's first frame.
            ApplySizing();
            Refresh();
        }

        private void Build()
        {
            // The ring: a flat disc under the unit. Unlit so it reads as a marker painted on
            // the street rather than as geometry catching the scene light.
            var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringGo.name = "NameplateRing";

            // ⚠️⚠️ DISABLED BEFORE IT IS DESTROYED, NOT JUST DESTROYED. `Destroy()` on a
            // component is deferred to the end of the frame, so an UNSCALED marker (the
            // primitive's default 2 m tall cylinder, before ApplySizing shrinks it to a floor
            // disc in Start) is still solid physics geometry for every raycast that runs
            // between here and then. `Lata.SnapHomeToGround` is one of them: on the very first
            // frame all four seats are still at their pre-teleport default position, stacked
            // with the lata at the origin, and its downward ray picked the tallest of these
            // still-live rings at y=1 over the actual road at y=0.1. Reported as *"cans are
            // floating"*. Disabling first removes it from physics immediately; `Destroy` still
            // runs after to clean up the component.
            var ringCollider = ringGo.GetComponent<Collider>();
            ringCollider.enabled = false;
            Destroy(ringCollider);

            _ring = ringGo.transform;
            _ring.SetParent(transform, false);
            _ringRenderer = ringGo.GetComponent<Renderer>();

            // ⚠️ THE PRIMITIVE'S OWN CYLINDER IS KEPT AS THE ATTACKER'S DISC rather than
            // generated, because it is already here, it is already the shape three of the four
            // bodies want, and a shared built-in mesh costs nothing. `Refresh` swaps between this
            // and the taya's ring.
            _ringFilter = ringGo.GetComponent<MeshFilter>();
            _discMesh = _ringFilter.sharedMesh;

            _ringBlock = new MaterialPropertyBlock();

            var labelGo = new GameObject("NameplateLabel");
            _labelTransform = labelGo.transform;
            _labelTransform.SetParent(transform, false);

            _label = labelGo.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;

            // ⚠️⚠️ THE TAG IS 0.16 m TALL IN THE WORLD AND IT WAS ABOUT FIVE TIMES THAT.
            // `CharacterBase.tscn`'s Label3D is `font_size = 32` at `pixel_size = 0.005`, so one
            // line is exactly 32 × 0.005 = 0.16 m of world. A `TextMesh` expresses the same
            // thing as `characterSize × fontSize / 10`, and the converted values (0.08 × 96)
            // came to 0.77 m — a name nearly as tall as the character wearing it.
            //
            // At range that only looked slightly heavy; at two metres it was a wall of orange
            // lettering across the street, which is what the capture shows and reads as a broken
            // font rather than as a scale.
            //
            // ⚠️ THE FONT SIZE STAYS HIGH AND THE CHARACTER SIZE CARRIES THE SCALE. They trade
            // off exactly, and a large font rendered small is the crisp half of that trade.
            _label.fontSize = LabelFontSize;
            _label.characterSize = LabelWorldHeight * 10.0f / LabelFontSize;

            // ⚠️⚠️ THE GAME'S OWN FACE, AND WITHOUT THIS IT IS ARIAL. A `TextMesh` with no font
            // assigned falls back to Unity's built-in, so the one piece of type that is IN the
            // world — over every character's head, for the whole match — was the only thing in
            // the build not set in Darumadrop. Reported as *"the font for names look ugly"*, and
            // it reads exactly as a placeholder next to the HUD directly above it.
            //
            // ⚠️ THE MATERIAL COMES WITH IT. A TextMesh draws through a MeshRenderer, and the
            // font's own atlas material is what puts the glyphs on it. Assigning the font and
            // leaving the renderer alone draws the new glyph quads with the OLD atlas, which is
            // a nameplate of scrambled letters rather than a wrong typeface.
            var font = UI.MenuKit.Font;

            if (font != null)
            {
                _label.font = font;

                var renderer = labelGo.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = font.material;
            }
        }

        /// <summary>
        /// Sizes and positions the ring and label from THIS unit's own current capsule rather
        /// than a shared constant. Public because the role swap has to call it again: a unit
        /// that changes role can change capsule.
        /// </summary>
        public void ApplySizing()
        {
            if (_character == null) return;

            var cc = _character.GetComponent<CharacterController>();
            float height = cc != null ? cc.height : PersonCapsuleHeight;
            float radius = cc != null ? cc.radius : 0.4f;

            // ⚠️ THE CAPSULE CENTRE IS NOT THE TRANSFORM ORIGIN HERE. Godot's CharacterBody3D
            // is centred on its origin, so the .gd could write -height/2 for the floor. Unity
            // seats are built with `center.y = height/2`, putting the origin at the FEET.
            // Copying the Godot expression across sinks the ring half a body into the street.
            float centreY = cc != null ? cc.center.y : height / 2.0f;
            float feetY = centreY - height / 2.0f;
            float headY = centreY + height / 2.0f;

            float ringRadius = radius * RingRadiusRatio;

            // ⚠️⚠️ THE SCALE DEPENDS ON WHICH MESH IS ON, AND GETTING THIS WRONG DRAWS A MARKER
            // TWICE THE RIGHT SIZE. A Unity cylinder primitive is 1 unit ACROSS (radius 0.5), so
            // it wants `ringRadius * 2`; `VfxShapes.Collar` is built at unit RADIUS, so it wants
            // `ringRadius`. `_ringUnitSpan` carries whichever applies and `Refresh` sets it in the
            // same breath as the mesh, because a role swap changes both.
            //
            // The flat Y is what makes either of them a disc rather than a column.
            _ring.localScale = new Vector3(ringRadius * _ringUnitSpan, 0.005f,
                                           ringRadius * _ringUnitSpan);
            _ring.localPosition = new Vector3(0.0f, feetY + RingFloorMargin, 0.0f);

            float labelMargin = LabelMarginAtPersonScale * (height / PersonCapsuleHeight);
            _labelTransform.localPosition = new Vector3(0.0f, headY + labelMargin, 0.0f);
        }

        /// <summary>Public so a late-joiner sync can force it, matching the HUD's own refresh.</summary>
        public void Refresh()
        {
            if (_character == null) return;

            bool isDefense = _character.IsDefender;
            _roleColor = isDefense ? UiTheme.Defense : UiTheme.Offense;

            // ⚠️⚠️ THE TAYA'S MARKER IS A RING AND AN ATTACKER'S IS A DISC, AND THAT IS THE ONLY
            // PLACE THE ROLE IS CARRIED BY SOMETHING OTHER THAN HUE ON THE FLOOR.
            // `docs/FUTURE.md` § 16.1: *"the roles are currently distinguished by hue alone. A
            // colourblind player, a bad projector at a tournament, or a cheap phone screen all
            // produce the same failure: you cannot tell the taya from the attackers."*
            //
            // ⚠️ RE-VERIFIED BEFORE IT WAS BUILT, AND HALF OF THAT CLAIM WAS ALREADY STALE.
            // The scoreboard spends the role colour on the WORD `DEFENDER` / `ATTACKER` and says
            // so in its own note, and the tag below already writes `· TAYA` on the defender and
            // nothing on the other three. **The floor ring was the one that was still hue-only**,
            // and it is the one that matters most in play: it is where the retrieval happens, and
            // the tag above the head fades out past `FadeStart`, twelve metres.
            //
            // ⚠️⚠️ A SHAPE, NOT A SECOND COLOUR, WHICH IS `CLAUDE.md` § 6.5'S RULE ONE SUBSYSTEM
            // OVER: *"a chamfer means pressable and a round means furniture ... a shape difference
            // survives a photograph and a colourblind player; a fill difference does not."*
            //
            // ⚠️ AND IT SPENDS LESS FLOOR RATHER THAN MORE, WHICH `VfxShapes.Collar` ALREADY
            // ARGUES IN ITS OWN WORDS: *"a ring at 8 per cent thickness costs about a sixth of the
            // painted floor its filled equivalent does, at the same radius, carrying the same
            // information about where the edge is."* `docs/VISION.md` § 2 is a budget on AREA, so
            // the accessible answer is also the cheaper one.
            //
            // ⚠️ IT REUSES `Collar` RATHER THAN GENERATING A NINTH RING. `VISION.md` § 2 rule 3:
            // *"a slab with walls, a field of broken plates, a swept flame ... are five things.
            // Five polygons handed to one builder are one thing."* `Wedges` was the other
            // candidate and is wrong here: it jitters every plate on purpose, and a role marker
            // has to be the SAME shape on every taya in every round or it is not a signal.
            //
            // ⚠️ ONLY THE TAYA CHANGES, WHICH MATCHES THE DECISION THE TAG BELOW ALREADY MADE.
            // There is exactly one taya and everybody else is an attacker by definition, so the
            // ring says the same thing the word does and neither of them is spent on the other
            // three bodies.
            if (isDefense)
            {
                _ringFilter.sharedMesh = TayaRingMesh();
                _ringUnitSpan = 1.0f;
            }
            else
            {
                _ringFilter.sharedMesh = _discMesh;
                _ringUnitSpan = 2.0f;
            }

            // ⚠️ THE SIZE IS RE-APPLIED BECAUSE THE SPAN JUST CHANGED. A role swap runs through
            // here every round and the two meshes are built at different radii; without this the
            // taya's ring would be drawn at the disc's scale for the rest of the round.
            ApplySizing();

            _ringBlock.SetColor("_Color", new Color(_roleColor.r, _roleColor.g, _roleColor.b, 0.8f));
            _ringBlock.SetColor("_BaseColor", new Color(_roleColor.r, _roleColor.g, _roleColor.b, 0.8f));
            _ringRenderer.SetPropertyBlock(_ringBlock);

            // ⚠️⚠️ THE ROLE SUFFIX IS ONLY ON THE TAYA, AND "· ATK" IS DELETED. 🧑 2026-08-27:
            // *"lessen the words showing up on screen, game feels overstimulating"*.
            //
            // ⚠️ THE SUFFIX WAS NEVER CARRYING ANY INFORMATION ON THREE OF THE FOUR PLATES. There
            // is exactly ONE taya per round and everybody else is an attacker by definition, so
            // "· ATK" is a word printed over three of the four bodies on the court to say "not
            // the special one". The colour already says it, on the plate and on the ground ring
            // under the same body, and `docs/VISION.md` § 3 puts the whole teaching load on the
            // colour rule rather than on labels.
            //
            // ⚠️ THE TAYA KEEPS ITS WORD, DELIBERATELY. Which of the four is the taya is the one
            // fact worth naming in the world, it changes every round, and a player who has just
            // rotated needs to find them before the colour rule has re-registered.
            _label.text = isDefense
                ? $"{_character.DisplayName()} · TAYA"
                : _character.DisplayName();
            _label.color = _roleColor;
        }

        /// <summary>
        /// The tag fade (§4.5) and the billboard. Polled rather than signalled because
        /// distance to the viewer is a continuously-varying value, not an event.
        ///
        /// ⚠️ IT READS THE ACTIVE CAMERA, NOT THE RIG. That makes it automatically correct for
        /// whichever unit this peer is looking through — FPP, TPP, or the spectator — with no
        /// reference back to any camera component.
        /// </summary>
        private void LateUpdate()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null || _label == null) return;

            // ⚠️⚠️ YOUR OWN TAG IS NOT DRAWN ON YOUR OWN SCREEN IN FIRST PERSON. The label hangs
            // 0.2 m above the head at y ≈ 1.8 and the FPP eye sits at 1.25, so it is half a
            // metre from the near plane and DIRECTLY over the crosshair: at a 95° vertical FOV
            // that is a name the height of the frame painted across the middle of the street.
            // The report's screenshot shows it as a giant orange "· ATK" over the arena.
            //
            // ⚠️ IT IS THE RIG THAT DECIDES, NOT A DISTANCE THRESHOLD. A spectator in POV sits
            // in the same place and SHOULD still see tags, and a taya standing nose to nose with
            // an attacker must still read theirs. The one case that has to go is the camera
            // looking out THROUGH this body.
            bool mine = false;

            var rig = cam.GetComponent<CameraSystem.CameraRig>();
            if (rig != null && rig.IsLocalFpp && rig.IsFollowing(_character)) mine = true;

            if (_ring != null) _ring.gameObject.SetActive(!mine);
            _label.gameObject.SetActive(!mine);

            if (mine) return;

            float distance = Vector3.Distance(cam.transform.position, _labelTransform.position);

            // InverseLerp(End, Start, d): 1 up close, 0 past FadeEnd.
            float alpha = Mathf.Clamp01(Mathf.InverseLerp(FadeEnd, FadeStart, distance));
            _label.color = new Color(_roleColor.r, _roleColor.g, _roleColor.b, alpha);

            // Face the viewer. Godot's Label3D billboarded itself; a TextMesh does not.
            _labelTransform.rotation = Quaternion.LookRotation(
                _labelTransform.position - cam.transform.position, Vector3.up);
        }
    }
}
