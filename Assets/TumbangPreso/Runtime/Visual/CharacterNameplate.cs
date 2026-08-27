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
        private MaterialPropertyBlock _ringBlock;
        private TextMesh _label;
        private Transform _labelTransform;
        private Color _roleColor = UiTheme.Defense;

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

            // A Unity cylinder is 2 units tall at scale 1, hence the flat Y — this is a disc,
            // not a column.
            _ring.localScale = new Vector3(ringRadius * 2.0f, 0.005f, ringRadius * 2.0f);
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
