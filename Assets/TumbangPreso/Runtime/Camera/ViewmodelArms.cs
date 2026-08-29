using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// The first-person arms, converted from `scenes/characters/visuals/ViewmodelArms.tscn` and
    /// enhanced with bespoke hero skin tones, sleeves, wristbands/bracers, and element signatures.
    ///
    /// ⚠️⚠️ FIRST PERSON GETS DEDICATED ARMS, NOT THE BODY'S OWN. From playtest: *"don't see
    /// arms of ppl"*. The real rig tops out below the eye line because the chibi head is big
    /// enough that the eye sits above the shoulders, so looking down showed nothing at all.
    /// These are mounted to the camera pivot and inherit its pitch, so they rise and fall
    /// with the view.
    ///
    /// ⚠️ THE PIVOT TRANSFORMS ARE BAKED AND CONVERTED, NOT RE-EYEBALLED. The .tscn carries a
    /// full basis per arm; both are converted below with the handedness flip written out, so
    /// a future reader can check the arithmetic instead of trusting that somebody nudged the
    /// values until they looked right.
    /// </summary>
    public sealed class ViewmodelArms : MonoBehaviour
    {
        /// <summary>
        /// The right arm's baked basis and origin, straight out of the .tscn:
        /// `Transform3D(-0.90016, -0.43556, 0, -0.30109, 0.62224, -0.72261,
        ///              0.31474, -0.65046, -0.69126, 0.58, -1.02, -0.34)`
        /// </summary>
        private static readonly Vector3 RightBasisX = new Vector3(-0.90016f, -0.43556f, 0.00000f);
        private static readonly Vector3 RightBasisY = new Vector3(-0.30109f, 0.62224f, -0.72261f);
        private static readonly Vector3 RightBasisZ = new Vector3(0.31474f, -0.65046f, -0.69126f);
        private static readonly Vector3 RightOrigin = new Vector3(0.5800f, -1.0200f, -0.3400f);

        /// <summary>The left arm, mirrored in X as the .tscn has it.</summary>
        private static readonly Vector3 LeftBasisX = new Vector3(-0.90016f, 0.43556f, 0.00000f);
        private static readonly Vector3 LeftBasisY = new Vector3(0.30109f, 0.62224f, -0.72261f);
        private static readonly Vector3 LeftBasisZ = new Vector3(-0.31474f, -0.65046f, -0.69126f);
        private static readonly Vector3 LeftOrigin = new Vector3(-0.6000f, -1.0400f, -0.3200f);

        /// <summary>
        /// Where the carried tsinelas sits on the right forearm.
        ///
        /// ⚠️⚠️ THIS IS WHERE THE MESH'S **CENTRE** GOES, NOT WHERE ITS ORIGIN GOES, AND THAT
        /// DISTINCTION IS THE WHOLE OF THE "IT IS FLOATING" REPORT. 🧑 2026-08-29, off an FPP
        /// frame: *"the slippers on my arm dont look right"*, *"iits floating a bit and doesnt
        /// look the way a slipper would sit on a hand"*, *"pls fix the fpp view on slippers"*.
        ///
        /// `docs/TODO.md` § 70.2 requires every slipper mesh to be **centred on XY and seated on
        /// Z = 0** — measured, *"every one has `min.y == 0.0000` in glTF space"*. So the authored
        /// origin is on the SOLE, at one end of the shoe, not in the middle of it. Parenting the
        /// object at the hand therefore put the sole's corner in the fist and hung the entire
        /// shoe off into space beside it, which is exactly the shoe floating clear of both hands
        /// in the screenshot. <see cref="NormaliseHeldSize"/> now subtracts the scaled bounds
        /// centre so the middle of the shoe lands here whatever its author chose.
        ///
        /// ⚠️ SOLVED PER SKIN RATHER THAN NUDGED, for the same reason <see cref="SlipperLength"/>
        /// is a presented length rather than a scale multiplier: nine slippers with nine authored
        /// origins would otherwise need nine offsets, and the tenth would ship wrong.
        /// </summary>
        private static readonly Vector3 HeldSlipperLocal = new Vector3(0.045f, 0.930f, -0.165f);

        /// <summary>
        /// How the tsinelas is turned in the fist.
        ///
        /// ⚠️⚠️ THE VIEWMODEL COPY WAS NEVER ROTATED AT ALL, WHILE THE WORLD COPY ALWAYS WAS.
        /// `Carrier` places the real object with `hand.rotation * Slipper.CarryRotation`, and
        /// `Slipper.CarryRotation`'s own note records why that quarter turn exists: § 70.3 fixed
        /// **+X as the length convention** for every slipper mesh in the roster. The viewmodel
        /// set `localPosition` and stopped, so the shoe kept its authored +X and lay ACROSS the
        /// view with its sole to the camera instead of running away from the fist.
        ///
        /// ⚠️⚠️ IT REUSES `Slipper.CarryRotation` RATHER THAN RESTATING THE QUARTER TURN. Two
        /// copies of one convention is how the world shoe and the hand shoe drift apart on the
        /// commit that changes the convention, and § 70.3 is the entry that fixed the convention
        /// once already. The extra roll is the only part that is specific to the hand: a shoe
        /// gripped dead flat reads as a plank, and 14 degrees is enough to show the footbed.
        /// </summary>
        private static readonly Quaternion HeldSlipperGrip =
            Quaternion.LookRotation(Vector3.left, Vector3.back);

        /// <summary>The arm colour from the .tscn's shader material.</summary>
        public static readonly Color ArmColour = new Color(0.784f, 0.529f, 0.353f, 1.0f);

        // -------------------------------------------------------------------
        // § CHARACTER SKIN TONES (Transcribed from 3D TPP Roster Palettes)
        // -------------------------------------------------------------------
        // Heroes
        public static readonly Color SkinSean = new Color(0.722f, 0.455f, 0.251f, 1.0f);     // Golden brown tan
        public static readonly Color SkinZack = new Color(0.780f, 0.478f, 0.271f, 1.0f);     // Warm medium caramel
        public static readonly Color SkinDante = new Color(0.659f, 0.376f, 0.173f, 1.0f);    // SKIN #a8602c
        public static readonly Color SkinCheska = new Color(0.961f, 0.722f, 0.580f, 1.0f);   // Fair porcelain skin
        public static readonly Color SkinNemu = new Color(0.878f, 0.686f, 0.518f, 1.0f);     // Pale lavender / ghostly ethereal
        public static readonly Color SkinPhaister = new Color(0.957f, 0.784f, 0.659f, 1.0f); // Warm porcelain peach #f4c8a8

        // Classic Characters
        public static readonly Color SkinBayan = new Color(0.851f, 0.541f, 0.373f, 1.0f);    // Berto arm slot 13
        public static readonly Color SkinMaring = new Color(0.969f, 0.788f, 0.651f, 1.0f);   // Fair cream
        public static readonly Color SkinTotoy = new Color(0.447f, 0.271f, 0.173f, 1.0f);    // Warm dark tan
        public static readonly Color SkinInday = new Color(0.369f, 0.216f, 0.133f, 1.0f);    // Inday arm slot 14
        public static readonly Color SkinKuyaBoy = new Color(0.690f, 0.443f, 0.290f, 1.0f);  // Deep sun-tan bronze
        public static readonly Color SkinAteGirlie = new Color(0.969f, 0.788f, 0.651f, 1.0f);// Fair porcelain
        public static readonly Color SkinTikboy = new Color(0.851f, 0.604f, 0.424f, 1.0f);   // Warm tan
        public static readonly Color SkinBebang = new Color(0.851f, 0.541f, 0.373f, 1.0f);   // Golden tan
        public static readonly Color SkinJunJun = new Color(0.969f, 0.788f, 0.651f, 1.0f);   // Fair kid tan
        public static readonly Color SkinLolaPacing = new Color(0.969f, 0.788f, 0.651f, 1.0f);// Gentle weathered fair
        public static readonly Color SkinMangKanor = new Color(0.192f, 0.141f, 0.114f, 1.0f); // Arm slot 8
        public static readonly Color SkinAlingNena = new Color(0.878f, 0.478f, 0.227f, 1.0f); // Arm slot 5

        /// <summary>Idle breathing. 2.6 s, looping, a couple of degrees — the original's
        /// keyframes are ±0.045 rad on the right and ±0.038 on the left.</summary>
        public const float IdlePeriod = 2.6f;
        public const float IdleRightSwing = 0.045f;
        public const float IdleLeftSwing = 0.038f;

        // -------------------------------------------------------------------
        // THE CARRY POSE — `camera_rig.gd::_update_viewmodel_carry`.
        //
        // ⚠️⚠️ THE VIEWMODEL CARRIES ITS OWN SLIPPER, IT DOES NOT CHASE THE WORLD ONE, and
        // that inversion is the whole fix for the reported "the slippers just float when you
        // hold it, its completely unattached to person". A carried slipper is parented to the
        // real hand, which in first person is hidden and below the frustum entirely; moving
        // the VISIBLE hand onto the world slipper instead meant the world slipper's position
        // had to be chosen to compose the first-person frame, so every other player saw a
        // tsinelas hovering beside its carrier's head.
        //
        // Two views, two objects. The world slipper sits in the real hand and is correct in
        // third person; `HeldSlipper` under this fist is what the local player sees.
        // -------------------------------------------------------------------

        /// <summary>Length of `viewmodel_arm.obj` from elbow to fingertip. The mesh is authored
        /// along +Y from the elbow at the origin, so the fist sits exactly this far along the
        /// pivot's y-axis.</summary>
        public const float ArmLength = 0.84f;

        /// <summary>The direction the throwing forearm points while carrying, in rig space: up,
        /// forward and slightly inward toward the crosshair. ⚠️ Z IS FLIPPED from the .gd's
        /// (-0.447, 0.745, -0.477), the same flip every other converted vector takes.</summary>
        private static readonly Vector3 CarryDir = new Vector3(-0.447f, 0.745f, 0.477f);

        /// <summary>Size of the throwing arm while it holds something. The empty one does not
        /// shrink: only the arm that comes up into frame needs to get out of the way.</summary>
        public const float CarryScale = 0.55f;

        /// <summary>How fast the hand converges on the carry pose. Instant snapping on pick-up
        /// reads as a teleport; this is quick enough to feel attached, slow enough to see.</summary>
        public const float ReachSpeed = 14.0f;

        /// <summary>Where the held slipper sits in the local player's frame: forward, right and
        /// below the crosshair. ⚠️ Z flipped from the .gd's (0.26, -0.16, -0.48).</summary>
        private static readonly Vector3 CarryAnchor = new Vector3(0.26f, -0.16f, 0.48f);

        /// <summary>
        /// Toe-to-heel length the held slipper presents, in metres, so it reads at arm's length.
        ///
        /// ⚠️ MEASURED, NOT TYPED, AND THE ORIGINAL VALUE WAS 0.171 m. The node is authored at
        /// mesh scale and then inherits TWO nested shrinks, the arms' own 0.72 and the carry
        /// pose's 0.55, so a 0.432 m mesh arrived on screen at 0.396 of its size. It was reported
        /// as "it doesnt get seen in first person" rather than as a size bug, because nothing was
        /// switched off: it was simply too small to notice at the fingertip.
        ///
        /// ⚠️⚠️ 0.46 m, UP FROM 0.34, ON A SECOND REPORT OF THE SAME THING. 🧑 2026-08-26, after
        /// the first correction had shipped and been played: *"pls try to make slipper look
        /// bigger in fpp too bcz it looks so small"*. The first pass fixed the ARITHMETIC (a
        /// number that had been silently multiplied down twice) and left the presented size at
        /// roughly life size, which is the trap: **a first-person prop at its true size reads as
        /// small**, because it sits at arm's length under a 70 degree field of view while the
        /// player's attention is at the centre of the screen. Every shipped shooter oversizes the
        /// thing in the hand for exactly this reason.
        ///
        /// ⚠️ AND IT IS THE PRESENTED LENGTH, NOT A SCALE MULTIPLIER, which is what makes it safe
        /// to raise. `FitHeldSlipper` solves each skin's own mesh to this number, so the IKE, the
        /// Spartan and every future replacement present identically rather than each inheriting
        /// its author's units. Nothing about the WORLD slipper moves: this is the viewmodel copy
        /// only, and third person still shows the real object at its real size.
        /// </summary>
        public const float SlipperLength = 0.46f;

        private const string AccessoryPrefix = "~HeroAccessory_";

        private Transform _rightPivot;
        private Transform _leftPivot;
        private Transform _rightArm;
        private Transform _leftArm;
        private MeshRenderer _rightArmRenderer;
        private MeshRenderer _leftArmRenderer;
        private Transform _heldSlipper;
        private Renderer _heldRenderer;

        // -------------------------------------------------------------------
        // ⚠️⚠️ THE SLEEVES HAVE NO CLOTH SOLVER ANY MORE AND MUST NOT GET ONE BACK.
        // DELETED 2026-08-27 with `ViewmodelClothPhysics`.
        //
        // 🧑, on Nemu in first person: *"the arms of Nemu her sleeves are phasing and looks weird
        // ... maybe js remove the physics on her sleeves bcz it looks so ugly, js show me cute
        // blocky sleeves"*.
        //
        // ⚠️ THE FAULT WAS STRUCTURAL, NOT A TUNING VALUE. The solver moved VERTICES: it
        // instanced the sleeve mesh and pushed every vertex by a weighted rotation plus an
        // offset plus a sine ripple, up to 0.12 m and 35 degrees. The sleeve, the inner lining
        // and the lavender cuff rim are three separate meshes occupying the same volume, and only
        // ONE of them was being deformed, so the outer shell walked through the lining and the
        // cuff on every step and every look. No damping ratio fixes that; the two surfaces are
        // not solved together and cannot be.
        //
        // ⚠️ AND THE CAST IS VOXEL ART. `tools/build_person_voxel.py` emits boxes; a rippling
        // sleeve is fighting the whole look to add motion the silhouette is deliberately without.
        // A blocky sleeve that rides the arm bone is the art, not a limitation of it.
        // -------------------------------------------------------------------

        private CharacterMotor _characterMotor;

        private Quaternion _rightRest;
        private Quaternion _leftRest;
        private Vector3 _rightRestPos;
        private Vector3 _rightRestScale;
        private float _phase;

        private bool _carrying;
        private string _currentHeroId;
        private bool _heroInitialized;
        private bool _built;

        /// <summary>Active normalized hero identity currently styled on these arms.</summary>
        public string CurrentHeroId => _currentHeroId;

        /// <summary>Current skin tone applied to the viewmodel arms.</summary>
        public Color CurrentSkinColor => SkinColorForHero(_currentHeroId);

        /// <summary>Show or hide the slipper in the viewmodel hand.</summary>
        public void SetHolding(bool holding)
        {
            _carrying = holding;
            if (_heldSlipper != null) _heldSlipper.gameObject.SetActive(holding);
        }

        /// <summary>
        /// ⚠️ THE VIEWMODEL SLIPPER WEARS THE PICKED SKIN, AND IT USED NOT TO. The .tscn
        /// hardcodes `tsinelas_classic.obj` on this node, so a player who chose CROCS held a
        /// brown flip-flop in their own hands while every other peer correctly saw their pick.
        ///
        /// ⚠️ COPIED FROM THE WORLD SLIPPER, NOT LOOKED UP IN THE ROSTER. The world object
        /// already resolves index to mesh; asking the roster again here is a second
        /// implementation free to drift from the first, and reading the object that is actually
        /// in the player's hand cannot disagree with it.
        /// </summary>
        public void MatchSkin(Slipper held)
        {
            if (_heldSlipper == null || held == null) return;

            var source = held.GetComponentInChildren<MeshFilter>();
            if (source == null || source.sharedMesh == null) return;

            var filter = _heldSlipper.GetComponent<MeshFilter>();
            if (filter == null) filter = _heldSlipper.gameObject.AddComponent<MeshFilter>();

            // ⚠️⚠️ THE EARLY RETURN USED TO SIT HERE AND IT IS WHY EVERY SLIPPER WAS BROWN IN
            // FIRST PERSON. 🧑 2026-08-29, holding IKE: *"ingame shader messes up the color of
            // slippers"*, *"doesnt look anything like the frigging character select anymore"*,
            // *"pls fix the shaders for slippers ... i dont want them to fuck up the color"*.
            //
            // It read `if (filter.sharedMesh == source.sharedMesh) return;` and returned BEFORE
            // the material copy below. `Build` dresses the viewmodel shoe in
            // `UiTheme.PropFoam` (#7a5741, a flat mid brown) as a placeholder, so any path that
            // reached this method with the mesh already correct kept that placeholder for the
            // whole match: the mesh was right, the colour was a stand-in, and the two were being
            // guarded by one condition. IKE renders as a dark sneaker with a white swoosh
            // everywhere else in the game and as a plain brown slab in the hand.
            //
            // ⚠️ THE MESH ASSIGNMENT IS STILL SKIPPED WHEN IT WOULD BE A NO-OP, because that is
            // what the guard was actually worth: writing `sharedMesh` dirties the renderer. The
            // MATERIALS are copied unconditionally, which is cheap and is the thing that was
            // being missed.
            bool meshChanged = filter.sharedMesh != source.sharedMesh;
            if (meshChanged) filter.sharedMesh = source.sharedMesh;

            // ⚠️⚠️ ONE MATERIAL PER SUBMESH, AND `sharedMaterial` ASSIGNS AN ARRAY OF LENGTH ONE.
            // A renderer draws submesh `i` with `sharedMaterials[i]` and silently DOES NOT DRAW
            // anything past the end of that array, which is the exact fault `MaterialKit.Dress`
            // was written for and this is the one place that never went through it. The viewmodel
            // copy of a multi-surface skin therefore rendered its FIRST surface and nothing else,
            // in the player's own hand, for as long as they carried it.
            //
            // It splits the tsinelas roster rather than breaking all of it, which is why 🧑
            // reported it as *"some slippers have broken shaders"* rather than as a dead shader:
            // the single-`usemtl` skins were always fine, while `tsinelas_classic` is five
            // surfaces and `tsinelas_sike` is two whose first one is the black `m2`. Holding a
            // sike put a solid black slab in frame with the rest of the shoe missing.
            //
            // ⚠️ THE COUNT COMES FROM THE MESH, NOT FROM THE SOURCE ARRAY. The world slipper is a
            // separate object with its own history, and a short array there would carry the same
            // fault across rather than correct it. Padding with the last entry is the honest
            // degrade: a surface drawn in a neighbouring material is visible and wrong, where a
            // surface not drawn at all is invisible and reads as missing geometry.
            var sourceRenderer = source.GetComponent<Renderer>();
            if (_heldRenderer != null && sourceRenderer != null)
                CopySurfaces(sourceRenderer, _heldRenderer, source.sharedMesh.subMeshCount);

            // The mesh changed, so the length-normalising scale has to be recomputed.
            NormaliseHeldSize();

            // ⚠️ AND THE TOON MATERIAL RE-APPLIED. The line above copies the WORLD slipper's
            // material, which is already a toon variant carrying that skin's colour, but its
            // outline width was measured against the world object's scale rather than the
            // fistful-sized copy in the viewmodel. Re-deriving it here is what keeps the border
            // the same thickness in both views.
            Visual.ToonSkin.ApplySlipper(_heldRenderer, Visual.ToonSkin.PropOutlineWidth);
        }

        /// <summary>Give <paramref name="to"/> one material per submesh, taken from
        /// <paramref name="from"/> and padded with its last entry. See the note at the call
        /// site for why a bare `sharedMaterial` assignment loses every surface but the first.
        /// </summary>
        private static void CopySurfaces(Renderer from, Renderer to, int surfaces)
        {
            var sources = from.sharedMaterials;
            if (sources == null || sources.Length == 0) return;

            surfaces = Mathf.Max(1, surfaces);

            var slots = new Material[surfaces];
            for (int i = 0; i < surfaces; i++)
                slots[i] = sources[Mathf.Min(i, sources.Length - 1)];

            to.sharedMaterials = slots;
        }

        /// <summary>
        /// Seats the held mesh in the fist: scaled to <see cref="SlipperLength"/>, turned by
        /// <see cref="HeldSlipperGrip"/>, and moved so its CENTRE lands on
        /// <see cref="HeldSlipperLocal"/> whatever origin the skin authored.
        ///
        /// ⚠️⚠️ ALL THREE ARE SOLVED FROM THE MESH, AND THAT IS WHY THIS IS ONE METHOD. The
        /// roster is nine slippers from five sources with five different ideas of where a shoe's
        /// origin is and how long it is; the two that were already solved here (length, and the
        /// nested parent scale) were solved exactly this way, and the placement was the one that
        /// was left as a hand-typed constant. It is the constant that was wrong.
        ///
        /// ⚠️ THE CENTRE OFFSET IS ROTATED BY THE GRIP BEFORE IT IS SUBTRACTED. The bounds centre
        /// is in the mesh's own axes and the object is turned by the quarter turn above, so
        /// subtracting it unrotated would correct along the wrong axis and move the shoe further
        /// off the hand on skins whose origin is furthest from their middle. Same trap as
        /// `NormaliseHeldSize`'s existing `parent.lossyScale` division: a correction has to be
        /// expressed in the space it is applied in.
        /// </summary>
        private void NormaliseHeldSize()
        {
            if (_heldRenderer == null) return;

            var filter = _heldSlipper.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            var bounds = filter.sharedMesh.bounds;
            var size = bounds.size;
            float longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (longest <= 0.0001f) return;

            // Against the parent's CURRENT world scale, so the slipper keeps this size while
            // the carry pose is still interpolating in rather than growing as the arm settles.
            float parent = Mathf.Max(0.0001f, _heldSlipper.parent.lossyScale.x);

            float k = SlipperLength / longest / parent;

            _heldSlipper.localScale = Vector3.one * k;
            _heldSlipper.localRotation = HeldSlipperGrip;
            _heldSlipper.localPosition = HeldSlipperLocal - HeldSlipperGrip * (bounds.center * k);
        }

        // -------------------------------------------------------------------
        // § THE ACTION CLIPS — `ViewmodelArms.tscn`'s `throw` and `grab`.
        // -------------------------------------------------------------------

        /// <summary>One keyframe: when, and the Godot euler it holds, in radians.</summary>
        private readonly struct Key
        {
            public readonly float T;
            public readonly Vector3 Godot;

            public Key(float t, float x, float y, float z)
            {
                T = t;
                Godot = new Vector3(x, y, z);
            }
        }

        private static readonly Key[] ThrowClip =
        {
            new Key(0.00f,  0.00f,  0.00f, 0.0f),
            new Key(0.14f,  0.52f,  0.10f, 0.0f),
            new Key(0.24f, -0.68f, -0.06f, 0.0f),
            new Key(0.46f,  0.00f,  0.00f, 0.0f),
        };

        private static readonly Key[] SlamClip =
        {
            new Key(0.00f,  0.00f,  0.00f, 0.0f),
            new Key(0.12f,  0.75f,  0.15f, 0.0f),
            new Key(0.22f, -0.85f, -0.10f, 0.0f),
            new Key(0.44f,  0.00f,  0.00f, 0.0f),
        };

        private static readonly Key[] ThrustClip =
        {
            new Key(0.00f,  0.00f,  0.00f, 0.0f),
            new Key(0.10f, -0.65f,  0.20f, 0.0f),
            new Key(0.20f,  0.85f, -0.15f, 0.0f),
            new Key(0.38f,  0.00f,  0.00f, 0.0f),
        };

        // -------------------------------------------------------------------
        // § THE FOUR BASE VERBS, WHICH HAD NO ARM WHILE ALL EIGHTEEN HERO POWERS DID.
        //
        // ⚠️⚠️ `grab`, `punch`, `lunge` AND `shove` REACHED `PlayAction` AND RESOLVED TO null,
        // so `CameraRig.PlayViewmodelAction` fell through to its procedural camera kick and the
        // first-person hand did not move for any of them. 🧑 2026-08-28: *"no animation when
        // tagging / raising lata"*. Both halves of that report are in this list: the taya's tag
        // is `punch` and `lunge`, and righting the can is `grab`.
        //
        // ⚠️ THE GAP IS THE EXACT ONE `HeroPresentationTests` ALREADY GUARDS FOR THE KITS.
        // `EveryHeroAbilityHasBespokeCastAndViewModelActions` went red the moment Phaister
        // shipped without arms and the block below it records why that mattered. Nothing applied
        // the same standard to the verbs every character has in every mode, so the four that are
        // ALWAYS available were the four with nothing authored. `grab` is the sharpest case: the
        // header on this whole section still says the .tscn ships *"`throw` and `grab`"*, and the
        // grab keyframes had been dropped from the table at some point without the header moving.
        //
        // ⚠️ THE SIGN CONVENTION IS THE MEASURED CLIPS', NOT A NEW ONE, AND IT IS THE OPPOSITE OF
        // WHAT IT LOOKS LIKE. These keys are GODOT space and go through `ToUnityLocal`, which
        // negates x, while `SetCharge` writes the cock-back straight into UNITY space as
        // `+WindupRad`. So:
        //
        //     godot +x  ->  unity -x  ->  the hand drives FORWARD and DOWN
        //     godot -x  ->  unity +x  ->  the hand COCKS BACK and UP
        //
        // `ThrustClip` directly above is the check: it cocks to -0.65 and then drives to +0.85,
        // which is a thrust and not a backhand. `WindupRad`'s own note names the bug from getting
        // it backwards, B-131, *"the hand dropping instead of cocking"*.
        //
        // ⚠️ AND EACH IS SHORTER THAN ITS VERB'S COOLDOWN. The arm must be home before the same
        // verb can fire again, or a player on cooldown sees the clip restart from a pose it never
        // left. Punch 0.34 against `PunchCooldown`, lunge 0.62 against `LungeCooldown`, shove
        // 0.40 against `ShoveMissCooldown`, grab 0.40 against a channel that re-fires it.
        // -------------------------------------------------------------------

        /// <summary>
        /// Righting the lata, and picking a tsinelas up. One reach-down gesture serves both
        /// because they are the same movement, which is what the .tscn's own `grab` was.
        /// </summary>
        private static readonly Key[] GrabClip =
        {
            new Key(0.00f, 0.00f,  0.00f, 0.0f),
            new Key(0.18f, 0.46f, -0.14f, 0.0f),
            new Key(0.40f, 0.00f,  0.00f, 0.0f),
        };

        /// <summary>The taya's jab: a short cock back and a hard straight thrust, inward toward
        /// the crosshair. It leads with the ARM, which is what tells it apart from the lunge at a
        /// glance.</summary>
        private static readonly Key[] PunchClip =
        {
            new Key(0.00f,  0.00f,  0.00f, 0.0f),
            new Key(0.06f, -0.18f,  0.04f, 0.0f),
            new Key(0.16f,  0.62f, -0.10f, 0.0f),
            new Key(0.34f,  0.00f,  0.00f, 0.0f),
        };

        /// <summary>
        /// The taya's dash tag. ⚠️ IT REACHES AND HOLDS WHERE THE PUNCH SNAPS BACK, and that is a
        /// gameplay read rather than a flourish: the sweep stays live for
        /// <c>Balance.LungeActiveTime</c> after the dash starts, so an arm that is still out is
        /// telling the truth about a tag that can still land. No cock-back either, because the
        /// commitment was already paid during the charge that `SetCharge` posed.
        /// </summary>
        private static readonly Key[] LungeClip =
        {
            new Key(0.00f, 0.00f,  0.00f, 0.0f),
            new Key(0.10f, 0.70f, -0.18f, 0.0f),
            new Key(0.38f, 0.58f, -0.16f, 0.0f),
            new Key(0.62f, 0.00f,  0.00f, 0.0f),
        };

        /// <summary>The attacker's shove. Pushes OUTWARD as well as forward, where the punch and
        /// the lunge go inward, so it does not read as a weak jab: a shove moves a body sideways
        /// and the arm should say so.</summary>
        private static readonly Key[] ShoveClip =
        {
            new Key(0.00f,  0.00f, 0.00f, 0.0f),
            new Key(0.08f, -0.22f, 0.10f, 0.0f),
            new Key(0.20f,  0.50f, 0.16f, 0.0f),
            new Key(0.40f,  0.00f, 0.00f, 0.0f),
        };

        /// <summary>How long <see cref="GrabClip"/> runs. Read by the reset channel, which is
        /// held far longer than the gesture and has to re-fire it rather than dip once and stand
        /// still. See <c>Carrier.StepDefender</c>.</summary>
        public const float GrabSeconds = 0.40f;

        // § BESPOKE HERO ACTION CLIPS
        private static readonly Key[] ThrustFireClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.08f,  0.45f,  0.20f, -0.10f),
            new Key(0.20f, -0.95f, -0.15f,  0.15f),
            new Key(0.42f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] IgniteClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.12f,  0.60f, -0.25f,  0.20f),
            new Key(0.24f,  0.48f, -0.20f,  0.15f),
            new Key(0.40f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] SupernovaSlamClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.15f,  0.90f,  0.25f,  0.10f),
            new Key(0.30f,  0.85f,  0.20f,  0.10f),
            new Key(0.45f, -1.10f, -0.20f, -0.15f),
            new Key(0.70f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] SprintElectricClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.08f, -0.50f,  0.30f,  0.20f),
            new Key(0.18f,  0.60f, -0.25f, -0.15f),
            new Key(0.28f, -0.55f,  0.25f,  0.15f),
            new Key(0.45f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] OverchargeClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.08f,  0.35f, -0.15f,  0.25f),
            new Key(0.16f,  0.25f, -0.10f,  0.15f),
            new Key(0.24f,  0.35f, -0.15f,  0.25f),
            new Key(0.38f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] SummonLightningClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.14f,  1.10f,  0.30f,  0.00f),
            new Key(0.28f,  1.05f,  0.28f,  0.00f),
            new Key(0.42f, -0.95f, -0.15f,  0.10f),
            new Key(0.65f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] StompHeavyClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.12f,  0.80f,  0.20f, -0.10f),
            new Key(0.24f, -1.05f, -0.15f,  0.05f),
            new Key(0.48f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] CarapaceGuardClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.12f,  0.45f, -0.45f,  0.35f),
            new Key(0.32f,  0.40f, -0.40f,  0.30f),
            new Key(0.55f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] FissureSlamClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.16f,  1.15f,  0.25f, -0.10f),
            new Key(0.35f, -1.20f, -0.20f,  0.00f),
            new Key(0.70f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] FrostSweepClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.10f,  0.35f,  0.40f, -0.20f),
            new Key(0.24f, -0.45f, -0.55f,  0.30f),
            new Key(0.45f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] RaiseBarricadeClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.10f, -0.55f,  0.10f, -0.15f),
            new Key(0.24f,  0.75f, -0.20f,  0.20f),
            new Key(0.48f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] NovaBurstClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.12f,  0.50f, -0.30f,  0.20f),
            new Key(0.25f, -0.90f,  0.15f, -0.25f),
            new Key(0.55f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] GhostStepClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.14f, -0.30f,  0.35f,  0.25f),
            new Key(0.30f,  0.25f, -0.20f, -0.15f),
            new Key(0.48f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] ProjectSpiritClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.10f,  0.45f, -0.15f,  0.10f),
            new Key(0.22f, -0.85f,  0.05f, -0.10f),
            new Key(0.44f,  0.00f,  0.00f,  0.00f),
        };

        private static readonly Key[] SeanceChannelClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.15f,  0.60f,  0.30f, -0.25f),
            new Key(0.32f,  0.35f, -0.35f,  0.30f),
            new Key(0.50f, -0.60f, -0.10f,  0.15f),
            new Key(0.75f,  0.00f,  0.00f,  0.00f),
        };

        // -------------------------------------------------------------------
        // § PHAISTER, and she arrived with NONE of these.
        //
        // ⚠️⚠️ ALL THREE OF HER POWERS NAMED A VIEWMODEL ACTION THAT DID NOT EXIST.
        // `PhaisterHeroKit` asks for `cast-hex`, `blink` and `coven-eclipse`; `PlayAction`'s
        // chain had no arm for any of them, so it returned null and the first-person arms did
        // NOTHING for the entire sixth kit.
        // `HeroPresentationTests.EveryHeroAbilityHasBespokeCastAndViewModelActions` is the test
        // that exists for exactly this and it went red the moment the branch merged:
        // *"phaister: HEX ViewmodelAction 'cast-hex' is not supported by ViewmodelArms"*.
        //
        // ⚠️ THE HAND IS THE WHOLE CHARACTER IN FIRST PERSON. Every other hero got three bespoke
        // arm clips because the caster's own screen shows nothing else: the sigil she draws is on
        // the floor and out of frame at the moment she casts it.
        // -------------------------------------------------------------------

        /// <summary>
        /// Drawing the hex: a circle traced in the air, then pushed down to set it.
        ///
        /// ⚠️ IT ORBITS BEFORE IT STRIKES, which is what separates her from every other caster in
        /// the game. Sean punches, Dante slams, Cheska sweeps; Phaister DRAWS, so the hand has to
        /// go around before it goes down or the gesture is just another jab.
        /// </summary>
        private static readonly Key[] CastHexClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.12f,  0.35f,  0.45f, -0.30f),
            new Key(0.24f,  0.50f, -0.10f, -0.55f),
            new Key(0.36f,  0.20f, -0.50f, -0.20f),
            new Key(0.50f, -0.70f, -0.15f,  0.35f),
            new Key(0.72f,  0.00f,  0.00f,  0.00f),
        };

        /// <summary>
        /// The blink: the hand collapses inward and snaps out on the far side.
        ///
        /// ⚠️ IT IS THE SHORTEST CLIP IN THE FILE ON PURPOSE. A teleport that takes as long as a
        /// slam is not a teleport. The hand is gone from the frame at 0.14 and back by 0.40.
        /// </summary>
        private static readonly Key[] BlinkClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.09f, -0.55f,  0.50f,  0.40f),
            new Key(0.18f,  0.75f, -0.45f, -0.50f),
            new Key(0.40f,  0.00f,  0.00f,  0.00f),
        };

        /// <summary>
        /// The ultimate: both the reach up and the long hold before the eclipse is thrown down.
        ///
        /// ⚠️ THE HOLD IS THE POINT. `Hero_Strike_Balance.md` § 4.3 asks an ultimate for a wind-up
        /// so the payoff has a moment, and this is the longest clip here: the arm is up and still
        /// from 0.30 to 0.58, which is most of a second doing nothing, which is what makes the
        /// throw land.
        /// </summary>
        private static readonly Key[] CovenEclipseClip =
        {
            new Key(0.00f,  0.00f,  0.00f,  0.00f),
            new Key(0.18f, -0.30f,  0.20f,  0.15f),
            new Key(0.30f, -0.95f,  0.10f, -0.10f),
            new Key(0.58f, -0.90f, -0.05f, -0.05f),
            new Key(0.70f,  0.65f, -0.20f,  0.30f),
            new Key(0.95f,  0.00f,  0.00f,  0.00f),
        };

        private Key[] _clip;
        private float _clipTime;

        /// <summary>
        /// § THE WIND-UP. How far the throwing arm cocks back at full charge, radians.
        ///
        /// ⚠️⚠️ 1.02 (~58°), UP FROM 0.62 (~36°), AND THIS IS `docs/TODO.md` § 75 ANSWERED.
        /// 🧑 2026-08-29 reported *"no wind up charger for slipper throw"*. § 75 traced every
        /// mechanism — the charge accumulator, this pose, the `arm-right` bone on all 25 rigs,
        /// the YOU card meter, the trajectory preview — and found all of them present and wired,
        /// which is why that entry asked which of three things he meant rather than guessing.
        /// **He picked this one: the arm moves, and 0.62 rad is not enough to see it.**
        ///
        /// ⚠️⚠️ SO THIS IS A PRESENTATION CHANGE AND NOT A BALANCE ONE, WHICH IS THE WHOLE
        /// REASON § 75 REFUSED TO ACT WITHOUT AN ANSWER. The other two readings were a minimum
        /// wind-up time before a throw may leave the hand — a number `docs/Design.md` owns — and
        /// a second charge meter somewhere central. **Nothing in `Balance` moves here.**
        /// `ThrowRules.PowerFor`, `Balance.ChargeFullTime` and `Balance.ChargeMinPower` are all
        /// untouched, so a tap still throws instantly at the same power it always did and no
        /// tournament number changes. Only how far the arm swings while it happens.
        ///
        /// ⚠️ THE .gd's ORIGINAL REASONING IS KEPT BECAUSE IT IS STILL THE BOUND, and it is what
        /// stops this going further: *"the HUD charge meter is on the YOU card at the bottom
        /// corner, which nobody looks at while aiming ... enough to be unmistakable in peripheral
        /// vision without the fist leaving the frame."* The arm IS the charge meter in first
        /// person, and the constraint that matters is the second half. At 58° the fist is still
        /// inside the frame at the top of the swing; that is what caps this rather than taste.
        ///
        /// ⚠️ ONE CONSTANT, BOTH VIEWS. `CharacterAnimator.ChargePoseRad` is defined as this
        /// field, so the third-person body and the first-person arm cannot drift apart. Raising
        /// it here raises the read for the four other players too, which is the half of the
        /// report a first-person-only fix would have missed.
        /// </summary>
        public const float WindupRad = 1.02f;

        /// <summary>
        /// Which way <see cref="WindupRad"/> turns the VIEWMODEL arm. -1 cocks back and up.
        ///
        /// ⚠️⚠️ IT IS THE OPPOSITE OF THE THIRD-PERSON SIGN AND THAT IS NOT AN INCONSISTENCY,
        /// IT IS TWO DIFFERENT LOCAL BASES. 🧑 2026-08-29, off the built player: *"is wind up even
        /// in the irght direction? Usually when u wind up btw u pull BACK not put arm forward"*.
        /// He was right.
        ///
        /// `CharacterAnimator.ChargePoseAxis` is `+X` and its note says so with a measurement
        /// behind it: *"`character_visual.gd` records that the first build of this used -X and
        /// the hand DROPPED instead of cocking"*. That is correct **for the rig's `arm-right`
        /// bone**, whose local frame comes from the .glb. This arm is not that bone. It is the
        /// viewmodel arm, whose basis is baked out of `ViewmodelArms.tscn` into
        /// <see cref="RightBasisX"/>/`Y`/`Z`, and in that frame local **+Y runs toward the hand**
        /// while local **-Z is toward the camera and up** — the same rotated frame that cost two
        /// of the three attempts at the held-slipper placement in `docs/TODO.md` § 79.8.
        ///
        /// Turning +58° about THIS local X carries local +Y (-0.301, **+0.622**, -0.723) toward
        /// local +Z (0.315, **-0.650**, -0.691). The hand's Y goes from +0.62 to -0.65: it swings
        /// DOWN and slightly forward. Sharing one sign across two unrelated bases reproduced
        /// exactly the B-131 failure the third-person note is warning about, in the other view.
        ///
        /// ⚠️ MEASURED FROM A RENDER, NOT FROM THE ARITHMETIC ALONE. `FppArmsSnapshotTool` now
        /// shoots `fpp_windup_side_000/050/100` from a PROFILE camera, because a straight-down-
        /// the-barrel FPP shot flattens the one axis this question lives on: an arm rotating
        /// about its local X moves mostly toward and away from that lens. At +1 the hand was
        /// forward and low in frame; at -1 it cocks up and back behind the shoulder line.
        ///
        /// ⚠️ THE THIRD-PERSON SIGN IS UNTOUCHED. `ChargePoseRad` is still this file's
        /// `WindupRad` so the two views agree on HOW FAR, which is the thing that must not drift.
        /// Only the direction is per-basis, which is the half that was never per-basis.
        /// </summary>
        public const float WindupCarry = -1.0f;

        /// <summary>-1 when nothing is charging, 0..1 while something is.</summary>
        private float _charge = -1.0f;

        public void SetCharge(float power)
        {
            _charge = power < 0.0f ? -1.0f : Mathf.Clamp01(power);

            if (_charge >= 0.0f) _clip = null;
        }

        /// <summary>
        /// Play `throw`, `grab`, `slam`, `cast`, or bespoke hero actions on the viewmodel arm.
        /// </summary>
        public bool PlayAction(string clip)
        {
            _clip = clip == "throw" ? ThrowClip
                  : clip == "grab" ? GrabClip
                  : clip == "punch" ? PunchClip
                  : clip == "lunge" ? LungeClip
                  : clip == "shove" ? ShoveClip
                  : clip == "slam" ? SlamClip
                  : clip == "cast" || clip == "thrust" || clip == "dash" ? ThrustClip
                  : clip == "thrust-fire" ? ThrustFireClip
                  : clip == "ignite" ? IgniteClip
                  : clip == "supernova-slam" ? SupernovaSlamClip
                  : clip == "sprint-electric" ? SprintElectricClip
                  : clip == "overcharge" ? OverchargeClip
                  : clip == "summon-lightning" ? SummonLightningClip
                  : clip == "stomp-heavy" || clip == "stomp" ? StompHeavyClip
                  : clip == "carapace-guard" ? CarapaceGuardClip
                  : clip == "fissure-slam" ? FissureSlamClip
                  : clip == "frost-sweep" ? FrostSweepClip
                  : clip == "raise-barricade" ? RaiseBarricadeClip
                  : clip == "nova-burst" ? NovaBurstClip
                  : clip == "ghost-step" ? GhostStepClip
                  : clip == "project-spirit" ? ProjectSpiritClip
                  : clip == "seance-channel" ? SeanceChannelClip
                  : clip == "cast-hex" ? CastHexClip
                  : clip == "blink" ? BlinkClip
                  : clip == "coven-eclipse" ? CovenEclipseClip
                  : null;

            _clipTime = 0.0f;

            // ⚠️ THE PER-CLIP SLEEVE IMPULSES WENT WITH THE SOLVER. See the field block at the
            // top of this class: they fed `ViewmodelClothPhysics`, and the recoil they added was
            // paid for by the outer sleeve walking through its own lining on every cast.

            return _clip != null;
        }

        private static Quaternion ToUnityLocal(Vector3 godotEuler) =>
            Quaternion.Euler(-godotEuler.x * Mathf.Rad2Deg,
                             -godotEuler.y * Mathf.Rad2Deg,
                              godotEuler.z * Mathf.Rad2Deg);

        private void StepAction(float dt)
        {
            if (_rightArm == null) return;

            if (_charge >= 0.0f)
            {
                _rightArm.localRotation = Quaternion.Euler(WindupCarry * WindupRad * _charge * Mathf.Rad2Deg,
                                                           0.0f, 0.0f);
                return;
            }

            if (_clip == null)
            {
                _rightArm.localRotation = Quaternion.identity;
                return;
            }

            _clipTime += dt;

            if (_clipTime >= _clip[_clip.Length - 1].T)
            {
                _clip = null;
                _rightArm.localRotation = Quaternion.identity;
                return;
            }

            for (int i = 1; i < _clip.Length; i++)
            {
                if (_clipTime > _clip[i].T) continue;

                float span = Mathf.Max(0.0001f, _clip[i].T - _clip[i - 1].T);
                float t = (_clipTime - _clip[i - 1].T) / span;

                t = t * t * (3.0f - 2.0f * t);

                _rightArm.localRotation = Quaternion.Slerp(ToUnityLocal(_clip[i - 1].Godot),
                                                           ToUnityLocal(_clip[i].Godot), t);
                return;
            }
        }

        private void Awake() => EnsureBuilt();

        public void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            Build();
        }

        private void Build()
        {
            var armMesh = Resources.Load<Mesh>("Models/viewmodel_arm");

            _rightPivot = BuildArm("RightPivot", RightBasisX, RightBasisY, RightBasisZ,
                RightOrigin, armMesh, out _rightArm, out _rightArmRenderer);
            _leftPivot = BuildArm("LeftPivot", LeftBasisX, LeftBasisY, LeftBasisZ,
                LeftOrigin, armMesh, out _leftArm, out _leftArmRenderer);

            _rightRest = _rightPivot.localRotation;
            _leftRest = _leftPivot.localRotation;
            _rightRestPos = _rightPivot.localPosition;
            _rightRestScale = _rightPivot.localScale;

            var slipperGo = new GameObject("HeldSlipper");
            _heldSlipper = slipperGo.transform;
            _heldSlipper.SetParent(_rightArm, false);
            _heldSlipper.localPosition = HeldSlipperLocal;

            var slipperMesh = Resources.Load<Mesh>("Models/tsinelas_classic");
            if (slipperMesh != null)
            {
                var mf = slipperGo.AddComponent<MeshFilter>();
                mf.sharedMesh = slipperMesh;

                _heldRenderer = slipperGo.AddComponent<MeshRenderer>();
                Visual.MaterialKit.Dress(_heldRenderer, UI.UiTheme.PropFoam);

                NormaliseHeldSize();
                Visual.ToonSkin.ApplySlipper(_heldRenderer, Visual.ToonSkin.PropOutlineWidth);
            }

            SetHolding(false);
            SetHero("classic");
        }

        private Transform BuildArm(string name, Vector3 bx, Vector3 by, Vector3 bz,
            Vector3 origin, Mesh mesh, out Transform armTransform, out MeshRenderer armRenderer)
        {
            var pivotGo = new GameObject(name);
            var pivot = pivotGo.transform;
            pivot.SetParent(transform, false);

            pivot.localPosition = ToUnityPosition(origin);
            pivot.localRotation = ToUnityRotation(bx, by, bz);

            var armGo = new GameObject("Arm");
            armTransform = armGo.transform;
            armTransform.SetParent(pivot, false);

            armRenderer = null;
            if (mesh != null)
            {
                var mf = armGo.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                armRenderer = armGo.AddComponent<MeshRenderer>();
                Visual.MaterialKit.Dress(armRenderer, ArmColour);
                Visual.ToonSkin.Apply(armRenderer, Visual.ToonSkin.PersonOutlineWidth);
            }

            return pivot;
        }

        /// <summary>
        /// Resolves skin tone for any hero or classic character identifier.
        /// </summary>
        public static Color SkinColorForHero(string heroId) => SkinColorForCharacter(heroId);

        public static Color SkinColorForCharacter(string characterId)
        {
            switch (NormalizeCharacterId(characterId))
            {
                // Heroes
                case "sean": return SkinSean;
                case "zack": return SkinZack;
                case "dante": return SkinDante;
                case "cheska": return SkinCheska;
                case "nemu": return SkinNemu;
                case "phaister": return SkinPhaister;

                // Classic Characters
                case "bayan": return SkinBayan;
                case "maring": return SkinMaring;
                case "totoy": return SkinTotoy;
                case "inday": return SkinInday;
                case "kuya_boy": return SkinKuyaBoy;
                case "ate_girlie": return SkinAteGirlie;
                case "tikboy": return SkinTikboy;
                case "bebang": return SkinBebang;
                case "jun_jun": return SkinJunJun;
                case "lola_pacing": return SkinLolaPacing;
                case "mang_kanor": return SkinMangKanor;
                case "aling_nena": return SkinAlingNena;

                default: return ArmColour;
            }
        }

        /// <summary>
        /// Normalizes raw character or alias string to canonical character id.
        /// </summary>
        public static string NormalizeHeroId(string heroId) => NormalizeCharacterId(heroId);

        public static string NormalizeCharacterId(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return "classic";
            switch (characterId.ToLowerInvariant())
            {
                case "sean":
                case "iggy":
                    return "sean";
                case "zack":
                    return "zack";
                case "dante":
                    return "dante";
                case "cheska":
                    return "cheska";
                case "nemu":
                    return "nemu";
                case "phaister":
                case "witch":
                    return "phaister";

                case "bayan":
                case "berto":
                    return "bayan";
                case "maring":
                    return "maring";
                case "totoy":
                    return "totoy";
                case "inday":
                    return "inday";
                case "kuya_boy":
                case "kuya-boy":
                    return "kuya_boy";
                case "ate_girlie":
                case "ate-girlie":
                    return "ate_girlie";
                case "tikboy":
                    return "tikboy";
                case "bebang":
                    return "bebang";
                case "jun_jun":
                case "jun-jun":
                    return "jun_jun";
                case "lola_pacing":
                case "lola-pacing":
                    return "lola_pacing";
                case "mang_kanor":
                case "mang-kanor":
                    return "mang_kanor";
                case "aling_nena":
                case "aling-nena":
                    return "aling_nena";

                default:
                    return "classic";
            }
        }

        /// <summary>
        /// Read active character from motor and style arms appropriately in FPP.
        /// </summary>
        public void MatchHero(CharacterMotor character) => MatchCharacter(character);

        public void MatchCharacter(CharacterMotor character)
        {
            EnsureBuilt();
            if (character == null) return;

            _characterMotor = character;

            string charId = null;
            if (character.Mode == Core.GameMode.HeroStrike)
            {
                var abilitySystem = character.AbilitySystem;
                if (abilitySystem != null && abilitySystem.Kit != null)
                {
                    charId = abilitySystem.Kit.HeroId;
                }
                else
                {
                    var heroPeople = Core.Roster.GetPeople(Core.GameMode.HeroStrike);
                    if (character.CharacterIndex >= 0 && character.CharacterIndex < heroPeople.Count)
                        charId = heroPeople[character.CharacterIndex].Id;
                }
            }
            else
            {
                var classicPeople = Core.Roster.GetPeople(Core.GameMode.Classic);
                if (character.CharacterIndex >= 0 && character.CharacterIndex < classicPeople.Count)
                    charId = classicPeople[character.CharacterIndex].Id;
            }

            SetCharacter(charId);
        }

        /// <summary>
        /// Customize viewmodel arms with bespoke skin tone, sleeves, wristbands/bracers,
        /// markings/tattoos, and accessories matching the character's TPP model.
        /// </summary>
        public void SetHero(string heroId) => SetCharacter(heroId);

        public void SetCharacter(string characterId)
        {
            EnsureBuilt();
            characterId = NormalizeCharacterId(characterId);
            if (_heroInitialized && _currentHeroId == characterId) return;

            _currentHeroId = characterId;
            _heroInitialized = true;

            ApplyCharacterStyle(characterId);
        }

        private void ApplyCharacterStyle(string characterId)
        {
            ClearAccessories(_rightArm);
            ClearAccessories(_leftArm);

            float thickness = characterId switch
            {
                "bayan" => 1.08f,
                "mang_kanor" => 1.06f,
                "maring" or "ate_girlie" or "bebang" or "lola_pacing" or "aling_nena" => 0.90f,
                "inday" => 0.94f,
                _ => 1.0f,
            };
            Vector3 armScale = new Vector3(thickness, 1.0f, thickness);

            if (_rightArm != null) _rightArm.localScale = armScale;
            if (_leftArm != null) _leftArm.localScale = armScale;

            Color skinColor = SkinColorForCharacter(characterId);

            // ⚠️⚠️ NEMU USES THE SHARED ARM MESH AGAIN, AND HIDING IT IS WHY HER HAND FLOATED.
            // 🧑 2026-08-27: *"make it look liek her arms are a bit thhicker and come from inside
            // her sleeves bcz it floating in ur pic"*. This read `isNemu || sean`, which switched
            // `Models/viewmodel_arm` OFF for her, and it was correct for exactly as long as her
            // sleeve was a hollow lofted tube with a visible interior lining: the shared arm
            // would have poked through it. Her sleeve is boxes now, so the limb underneath is
            // simply missing, and the sleeve and the hand were two objects with a gap between
            // them and nothing joining them.
            //
            // ⚠️ SEAN IS THE ONLY REAL ENTRY. `CreateSeanMuscularArmMesh` replaces the shared
            // mesh with a different SHAPE, which is what this flag is for. Nemu was on the list
            // to hide a limb rather than to replace one.
            //
            // ⚠️ EVERY OTHER HERO ALREADY WORKS THIS WAY, and Cheska is the reference frame:
            // shared arm at full section, tinted skin, with sleeve boxes stacked over its upper
            // half. That is where "arms a bit thicker" comes from, because the shared mesh is
            // the width the voxel cast's arms actually are.
            bool hasCustomArmMesh = characterId == "sean";

            if (_rightArmRenderer != null)
            {
                _rightArmRenderer.enabled = !hasCustomArmMesh;
                if (!hasCustomArmMesh)
                {
                    Visual.MaterialKit.Dress(_rightArmRenderer, skinColor);
                    Visual.ToonSkin.Apply(_rightArmRenderer, Visual.ToonSkin.PersonOutlineWidth);
                }
            }
            if (_leftArmRenderer != null)
            {
                _leftArmRenderer.enabled = !hasCustomArmMesh;
                if (!hasCustomArmMesh)
                {
                    Visual.MaterialKit.Dress(_leftArmRenderer, skinColor);
                    Visual.ToonSkin.Apply(_leftArmRenderer, Visual.ToonSkin.PersonOutlineWidth);
                }
            }

            if (_rightArm != null) BuildArmAccessories(_rightArm, characterId, isRight: true, parent: this);
            if (_leftArm != null) BuildArmAccessories(_leftArm, characterId, isRight: false, parent: this);
        }

        /// <summary>
        /// ⚠️⚠️ THE MESH GOES WITH THE GameObject, AND IT USED NOT TO. Every accessory under an
        /// arm carries a mesh this file BUILT. `CreateBoxMesh`, `CreateCylinderMesh` and
        /// `CreateSeanMuscularArmMesh` all return `new Mesh`, and a mesh assigned to
        /// `sharedMesh` is not owned by the renderer holding it, so destroying the object left
        /// the mesh alive with nothing pointing at it. Nothing ever collected them: `Mesh` is a
        /// native object and the managed wrapper going out of scope does not free it.
        ///
        /// It leaked on a path the player walks constantly. `ApplyCharacterStyle` clears and
        /// rebuilds BOTH arms on every character change. Counted from this file: 89 accessory
        /// calls spread over the 20 builders, so an average pick builds about four and a half
        /// meshes per arm and strands nine of them, and the roster is eighteen deep.
        ///
        /// ⚠️ THE WHOLE SUBTREE, NOT THE DIRECT CHILD. Nemu's `SpiritHand` is an accessory whose
        /// own hand box is a child of it, so walking one level would have missed exactly the
        /// nested cases.
        ///
        /// ⚠️ AND THE WELD IS FORGOTTEN FIRST. `OutlineNormals` keys its "already done" set on
        /// the entity id, which stops being unique the moment the object behind it dies. See
        /// `OutlineNormals.Forget`.
        ///
        /// ⚠️ NOTHING ELSE UNDER AN ARM IS TOUCHED. `Arm` and `HeldSlipper` carry meshes loaded
        /// from `Resources` and are not accessories, so the prefix test is what keeps this from
        /// destroying an imported asset. Do not widen it.
        /// </summary>
        private static void ClearAccessories(Transform arm)
        {
            if (arm == null) return;
            for (int i = arm.childCount - 1; i >= 0; i--)
            {
                var child = arm.GetChild(i);
                if (child == null || !child.name.StartsWith(AccessoryPrefix)) continue;

                foreach (var filter in child.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null) continue;

                    var mesh = filter.sharedMesh;
                    if (mesh == null) continue;

                    filter.sharedMesh = null;
                    Visual.OutlineNormals.Forget(mesh);

                    if (Application.isPlaying) Object.Destroy(mesh);
                    else Object.DestroyImmediate(mesh);
                }

                if (Application.isPlaying) Object.Destroy(child.gameObject);
                else Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void BuildArmAccessories(Transform arm, string characterId, bool isRight, ViewmodelArms parent = null)
        {
            switch (characterId)
            {
                // -----------------------------------------------------------
                // § HEROES
                // -----------------------------------------------------------
                case "sean":
                    BuildSeanAccessories(arm, isRight);
                    break;
                case "zack":
                    BuildZackAccessories(arm, isRight);
                    break;
                case "dante":
                    BuildDanteAccessories(arm, isRight);
                    break;
                case "cheska":
                    BuildCheskaAccessories(arm, isRight);
                    break;
                case "nemu":
                    BuildNemuAccessories(arm, isRight, parent);
                    break;
                case "phaister":
                    BuildPhaisterAccessories(arm, isRight);
                    break;

                // -----------------------------------------------------------
                // § CLASSIC ROSTER
                // -----------------------------------------------------------
                case "bayan":
                    BuildBayanAccessories(arm, isRight);
                    break;
                case "maring":
                    BuildMaringAccessories(arm, isRight);
                    break;
                case "totoy":
                    BuildTotoyAccessories(arm, isRight);
                    break;
                case "inday":
                    BuildIndayAccessories(arm, isRight);
                    break;
                case "kuya_boy":
                    BuildKuyaBoyAccessories(arm, isRight);
                    break;
                case "ate_girlie":
                    BuildAteGirlieAccessories(arm, isRight);
                    break;
                case "tikboy":
                    BuildTikboyAccessories(arm, isRight);
                    break;
                case "bebang":
                    BuildBebangAccessories(arm, isRight);
                    break;
                case "jun_jun":
                    BuildJunJunAccessories(arm, isRight);
                    break;
                case "lola_pacing":
                    BuildLolaPacingAccessories(arm, isRight);
                    break;
                case "mang_kanor":
                    BuildMangKanorAccessories(arm, isRight);
                    break;
                case "aling_nena":
                    BuildAlingNenaAccessories(arm, isRight);
                    break;

                default:
                    BuildClassicAccessories(arm, isRight);
                    break;
            }
        }

        // -------------------------------------------------------------------
        // § HERO BESPOKE ACCESSORY BUILDERS
        // -------------------------------------------------------------------

        private static void BuildSeanAccessories(Transform arm, bool isRight)
        {
            var vestRed = new Color(0.788f, 0.165f, 0.165f, 1.0f);
            var gold = new Color(0.941f, 0.647f, 0.000f, 1.0f);
            var goldShadow = new Color(0.722f, 0.478f, 0.000f, 1.0f);

            // team-sean.glb is sleeveless and uses a continuous broad deltoid, bicep,
            // narrow elbow and thick forearm. Build that silhouette as one mesh instead
            // of stacking rectangular skin blocks.
            var muscleGo = new GameObject(AccessoryPrefix + "MuscularArm");
            muscleGo.transform.SetParent(arm, false);
            var muscleFilter = muscleGo.AddComponent<MeshFilter>();
            muscleFilter.sharedMesh = CreateSeanMuscularArmMesh();
            var muscleRenderer = muscleGo.AddComponent<MeshRenderer>();
            Visual.MaterialKit.Dress(muscleRenderer, SkinSean);
            Visual.ToonSkin.Apply(muscleRenderer, Visual.ToonSkin.PersonOutlineWidth);

            // The source model wraps the same red and gold combat bracer around both
            // forearms, followed by an uncovered fist.
            AddCylinderAccessory(arm, "BracerGoldInner", 0.205f, 0.205f, 0.045f, 8,
                new Vector3(0.0f, 0.385f, 0.0f), Quaternion.identity, gold);
            AddCylinderAccessory(arm, "BracerBody", 0.205f, 0.195f, 0.18f, 8,
                new Vector3(0.0f, 0.49f, 0.0f), Quaternion.identity, vestRed);
            AddCylinderAccessory(arm, "BracerGoldOuter", 0.205f, 0.205f, 0.045f, 8,
                new Vector3(0.0f, 0.595f, 0.0f), Quaternion.identity, gold);
            AddBoxAccessory(arm, "BracerPlate", new Vector3(0.20f, 0.12f, 0.025f),
                new Vector3(0.0f, 0.49f, 0.205f), Quaternion.identity, gold);
            AddBoxAccessory(arm, "BracerPlateInset", new Vector3(0.09f, 0.065f, 0.028f),
                new Vector3(0.0f, 0.49f, 0.222f), Quaternion.identity, goldShadow);
        }

        private static Mesh CreateSeanMuscularArmMesh()
        {
            const int sides = 8;
            var sections = new[]
            {
                // x is half-width, y is distance from shoulder, z is half-depth.
                new Vector3(0.185f, 0.000f, 0.175f),
                new Vector3(0.225f, 0.105f, 0.210f),
                new Vector3(0.215f, 0.220f, 0.200f),
                new Vector3(0.185f, 0.330f, 0.175f),
                new Vector3(0.145f, 0.405f, 0.135f),
                new Vector3(0.180f, 0.505f, 0.165f),
                new Vector3(0.165f, 0.620f, 0.155f),
                new Vector3(0.155f, 0.720f, 0.145f),
            };

            int ringVertices = sections.Length * sides;
            var vertices = new Vector3[ringVertices + 2];
            var uvs = new Vector2[vertices.Length];

            for (int ring = 0; ring < sections.Length; ring++)
            {
                Vector3 section = sections[ring];
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2.0f / sides;
                    int index = ring * sides + side;
                    vertices[index] = new Vector3(
                        Mathf.Cos(angle) * section.x,
                        section.y,
                        Mathf.Sin(angle) * section.z);
                    uvs[index] = new Vector2(side / (float)sides, ring / (float)(sections.Length - 1));
                }
            }

            int shoulderCentre = ringVertices;
            int handCentre = ringVertices + 1;
            vertices[shoulderCentre] = new Vector3(0.0f, sections[0].y, 0.0f);
            vertices[handCentre] = new Vector3(0.0f, sections[sections.Length - 1].y, 0.0f);
            uvs[shoulderCentre] = new Vector2(0.5f, 0.0f);
            uvs[handCentre] = new Vector2(0.5f, 1.0f);

            int sideTriangleCount = (sections.Length - 1) * sides * 2;
            var triangles = new int[(sideTriangleCount + sides * 2) * 3];
            int cursor = 0;

            for (int ring = 0; ring < sections.Length - 1; ring++)
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                int a = ring * sides + side;
                int b = ring * sides + next;
                int c = (ring + 1) * sides + side;
                int d = (ring + 1) * sides + next;

                triangles[cursor++] = a;
                triangles[cursor++] = c;
                triangles[cursor++] = b;
                triangles[cursor++] = b;
                triangles[cursor++] = c;
                triangles[cursor++] = d;
            }

            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                triangles[cursor++] = shoulderCentre;
                triangles[cursor++] = next;
                triangles[cursor++] = side;

                int last = (sections.Length - 1) * sides;
                triangles[cursor++] = handCentre;
                triangles[cursor++] = last + side;
                triangles[cursor++] = last + next;
            }

            var mesh = new Mesh { name = "Sean_MuscularViewmodelArm" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void BuildZackAccessories(Transform arm, bool isRight)
        {
            var jacket = new Color(0.910f, 0.820f, 0.120f, 1.0f);
            var jacketDark = new Color(0.650f, 0.500f, 0.080f, 1.0f);
            var crest = new Color(0.980f, 0.950f, 0.220f, 1.0f);

            // Zack's roster palette uses electric yellow for the jacket, cuff and crest.
            AddBoxAccessory(arm, "ElectricJacketSleeve", new Vector3(0.30f, 0.37f, 0.30f),
                new Vector3(0.0f, 0.185f, 0.0f), Quaternion.identity, jacket);
            AddBoxAccessory(arm, "ElectricJacketCuff", new Vector3(0.32f, 0.065f, 0.32f),
                new Vector3(0.0f, 0.405f, 0.0f), Quaternion.identity, jacketDark);
            AddBoxAccessory(arm, "ElectricSleeveStripe", new Vector3(0.24f, 0.025f, 0.025f),
                new Vector3(0.0f, 0.29f, 0.16f), Quaternion.identity, jacketDark);
            AddBoxAccessory(arm, "ElectricWristband", new Vector3(0.31f, 0.085f, 0.30f),
                new Vector3(0.0f, 0.505f, 0.0f), Quaternion.identity, crest);
        }

        private static void BuildDanteAccessories(Transform arm, bool isRight)
        {
            var robeGreen   = new Color(0.239f, 0.388f, 0.208f, 1.0f); // #3d6335 - Primary Marking Green (Matte earthen)
            var robeDark    = new Color(0.141f, 0.243f, 0.122f, 1.0f); // #243e1f - Shadow / Border Green
            var leather     = new Color(0.282f, 0.184f, 0.114f, 1.0f); // #482f1d - Brown Warrior Tunic
            var leatherDark = new Color(0.180f, 0.110f, 0.060f, 1.0f); // #2e1c0f - Dark Leather Crease
            var gold        = new Color(0.875f, 0.698f, 0.282f, 1.0f); // #dfb248 - Canonical Gold Trim
            var goldDark    = new Color(0.680f, 0.520f, 0.180f, 1.0f); // #ad842e - Shaded Gold Rim
            var skinTone    = SkinDante;                                // #a8602c - Canonical Dante Skin
            var skinDark    = new Color(0.549f, 0.282f, 0.102f, 1.0f); // #8c481a - Shaded Skin Tone

            if (isRight)
            {
                // ===================================================================
                // § DANTE RIGHT ARM - Warrior Leather Sleeve, Gold Cuff & Runic Glyph
                // ===================================================================
                
                // 1. Heavy Warrior Leather Sleeve (Y: 0.04 - 0.32)
                AddBoxAccessory(arm, "LeatherSleeve", new Vector3(0.315f, 0.30f, 0.305f),
                    new Vector3(0.0f, 0.16f, 0.0f), Quaternion.identity, leather);
                AddBoxAccessory(arm, "LeatherSleeveCrease", new Vector3(0.325f, 0.04f, 0.315f),
                    new Vector3(0.0f, 0.06f, 0.0f), Quaternion.identity, leatherDark);

                // 2. Diagonal Harness Strap across Shoulder Armor
                AddBoxAccessory(arm, "HarnessStrap", new Vector3(0.075f, 0.28f, 0.025f),
                    new Vector3(0.02f, 0.16f, 0.155f), Quaternion.Euler(0, 0, -28.0f), gold);
                AddBoxAccessory(arm, "HarnessBuckle", new Vector3(0.09f, 0.06f, 0.035f),
                    new Vector3(-0.02f, 0.19f, 0.162f), Quaternion.identity, goldDark);

                // 3. Volumetric 3D Beveled Gold Cuff Ring (Y: 0.32 - 0.40)
                AddBoxAccessory(arm, "GoldCuffLining", new Vector3(0.325f, 0.09f, 0.315f),
                    new Vector3(0.0f, 0.355f, 0.0f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "GoldCuffBody", new Vector3(0.345f, 0.080f, 0.335f),
                    new Vector3(0.0f, 0.355f, 0.0f), Quaternion.identity, gold);
                AddBoxAccessory(arm, "GoldCuffRimTop", new Vector3(0.352f, 0.020f, 0.342f),
                    new Vector3(0.0f, 0.385f, 0.0f), Quaternion.identity, gold);
                AddBoxAccessory(arm, "GoldCuffRimBot", new Vector3(0.352f, 0.020f, 0.342f),
                    new Vector3(0.0f, 0.325f, 0.0f), Quaternion.identity, goldDark);

                // 4. FOREARM MARKING: a lengthwise GREEN BAND, stepped, with a gold strip
                //    outboard of it.
                //
                // ⚠️⚠️ THIS REPLACED A THREE-PIECE "RUNIC GLYPH" HE DOES NOT HAVE. 🧑 2026-08-29,
                // on the arms: *"he has diff arm markings"*, *"that is not how dante's arms look
                // like at all"*, and, on the scope of the fix: *"all i needed u to change in old
                // one was the green markings"*. Everything else on this arm - the leather
                // sleeve, the harness strap and buckle, the beveled gold cuff - is his and is
                // untouched. A first pass at this deleted all of it and was rightly rejected:
                // *"infact old one was better"*.
                //
                // **Measured off `Logs/cast-sheet.png`, Dante at r4c1, cropped per arm.** His
                // right forearm carries ONE green band running ALONG the arm, stepping narrower
                // in the middle and wider at both ends, with a gold strip outboard of it. The
                // old conduit/crossbar/hook assembly was a symbol laid ACROSS the arm and is not
                // on the model in any form.
                //
                // ⚠️ READ THE CAST SHEET, NOT THE MODEL SHEET. `ModelSheet` renders with NO palette
                // ("[no palette, stock atlas colours]" in its own index) and shows Dante in the
                // source asset's blue and orange, which is a different character. `RunCast`
                // applies the roster palette and is the only one that shows what the game draws.
                //
                // ⚠️ BOTH FACES, for the reason the glyph's own names already gave: local +Z does
                // not face the same way on the two arms, because `RightBasisX/Y/Z` and
                // `LeftBasisX/Y/Z` are rotated frames rather than mirrored scales. A band on one
                // face only is invisible on this arm through most of the swing.

                // 4a. Front Face (+Z)
                AddBoxAccessory(arm, "MarkFrontWide1Base", new Vector3(0.100f, 0.128f, 0.015f),
                    new Vector3(-0.045f, 0.360f, 0.126f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "MarkFrontWide1Body", new Vector3(0.092f, 0.120f, 0.018f),
                    new Vector3(-0.045f, 0.360f, 0.128f), Quaternion.identity, robeGreen);
                AddBoxAccessory(arm, "MarkFrontNarrowBase", new Vector3(0.070f, 0.128f, 0.015f),
                    new Vector3(-0.060f, 0.482f, 0.126f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "MarkFrontNarrowBody", new Vector3(0.062f, 0.120f, 0.018f),
                    new Vector3(-0.060f, 0.482f, 0.128f), Quaternion.identity, robeGreen);
                AddBoxAccessory(arm, "MarkFrontWide2Base", new Vector3(0.100f, 0.128f, 0.015f),
                    new Vector3(-0.045f, 0.604f, 0.126f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "MarkFrontWide2Body", new Vector3(0.092f, 0.120f, 0.018f),
                    new Vector3(-0.045f, 0.604f, 0.128f), Quaternion.identity, robeGreen);

                // 4b. Back/Dorsal Face (-Z)
                AddBoxAccessory(arm, "MarkBackWide1Base", new Vector3(0.100f, 0.128f, 0.015f),
                    new Vector3(-0.045f, 0.360f, -0.126f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "MarkBackWide1Body", new Vector3(0.092f, 0.120f, 0.018f),
                    new Vector3(-0.045f, 0.360f, -0.128f), Quaternion.identity, robeGreen);
                AddBoxAccessory(arm, "MarkBackNarrowBase", new Vector3(0.070f, 0.128f, 0.015f),
                    new Vector3(-0.060f, 0.482f, -0.126f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "MarkBackNarrowBody", new Vector3(0.062f, 0.120f, 0.018f),
                    new Vector3(-0.060f, 0.482f, -0.128f), Quaternion.identity, robeGreen);
                AddBoxAccessory(arm, "MarkBackWide2Base", new Vector3(0.100f, 0.128f, 0.015f),
                    new Vector3(-0.045f, 0.604f, -0.126f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "MarkBackWide2Body", new Vector3(0.092f, 0.120f, 0.018f),
                    new Vector3(-0.045f, 0.604f, -0.128f), Quaternion.identity, robeGreen);

                // 4c. The gold strip outboard of the green, and the outer edge wrap.
                AddBoxAccessory(arm, "MarkGoldStripFront", new Vector3(0.024f, 0.330f, 0.018f),
                    new Vector3(-0.108f, 0.482f, 0.128f), Quaternion.identity, gold);
                AddBoxAccessory(arm, "MarkGoldStripBack", new Vector3(0.024f, 0.330f, 0.018f),
                    new Vector3(-0.108f, 0.482f, -0.128f), Quaternion.identity, gold);
                AddBoxAccessory(arm, "MarkOuterWrapBase", new Vector3(0.015f, 0.330f, 0.120f),
                    new Vector3(-0.130f, 0.482f, 0.00f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "MarkOuterWrapBody", new Vector3(0.018f, 0.320f, 0.110f),
                    new Vector3(-0.132f, 0.482f, 0.00f), Quaternion.identity, robeGreen);

                // 5. Modeled Hand & Knuckle Anatomy
                AddHandKnuckles(arm, isRight, 0.280f, skinTone, skinDark);
            }
            else
            {
                // ===================================================================
                // § DANTE LEFT ARM - Bare Skin with 2 Bold Full-Width Chevrons
                // ===================================================================

                // 1. Dark Green Shoulder Collar / Sleeve Trim (Y: 0.04 - 0.16)
                AddBoxAccessory(arm, "ShoulderCap", new Vector3(0.295f, 0.12f, 0.275f),
                    new Vector3(0.0f, 0.07f, 0.0f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "ShoulderGoldTrim", new Vector3(0.305f, 0.035f, 0.285f),
                    new Vector3(0.0f, 0.13f, 0.0f), Quaternion.identity, gold);
                AddBoxAccessory(arm, "ShoulderGreenLining", new Vector3(0.300f, 0.03f, 0.280f),
                    new Vector3(0.0f, 0.155f, 0.0f), Quaternion.identity, robeGreen);

                // 2. TWO GREEN STRIPES RUNNING LENGTHWISE DOWN THE FOREARM, each kinked once.
                //
                // ⚠️⚠️ THESE WERE CHEVRONS AND HIS MODEL HAS NO CHEVRONS. 🧑 2026-08-29, cropping
                // this exact arm out of a render: *"this specifically bcz it doesnt matcht eh arm
                // of the model"*, after *"he has diff arm markings"*.
                //
                // The old geometry drew two "^" arrows ACROSS the arm: two legs angled at
                // ±28 degrees meeting under a flat apex block, stacked at two heights, like
                // sergeant's stripes. **Measured off `Logs/cast-sheet.png` (Dante r4c1, left
                // forearm, cropped and magnified 10x), what he actually wears is two green bands
                // running ALONG the arm**, parallel to its long axis, separated by a strip of
                // bare skin, each bending once near the shoulder end. The direction is the whole
                // difference: lengthwise stripes read as a sleeve pattern, cross-arm arrows read
                // as rank insignia, and the arm is mostly vertical on screen in first person so
                // the two orientations could not look less alike.
                //
                // ⚠️ THE KINK IS THE SHAPE, NOT A BEVEL. Each stripe steps sideways once about two
                // thirds of the way up and continues; drawing them dead straight loses the only
                // feature the pattern has. It is built as lower run, angled bridge, upper run.
                //
                // ⚠️ BOTH FACES PLUS THE OUTER EDGE, for the reason the right arm's marking carries:
                // `RightBasisX/Y/Z` and `LeftBasisX/Y/Z` are rotated frames rather than mirrored
                // scales, so a stripe on one face alone disappears through most of the swing.
                AddDanteLengthStripe(arm, "StripeInner", -0.070f, 0.062f, robeGreen, robeDark);
                AddDanteLengthStripe(arm, "StripeOuter", 0.030f, 0.062f, robeGreen, robeDark);

                // 3. Modeled Hand & Knuckle Anatomy
                AddHandKnuckles(arm, isRight, 0.280f, skinTone, skinDark);
            }
        }

        /// <summary>
        /// One of Dante's left-arm stripes: a run up the forearm, a sideways kink, and a shorter
        /// run above it, mirrored onto the back face and wrapped onto the outer edge.
        ///
        /// ⚠️ THE DARK PLATE UNDER EACH GREEN ONE IS THE MODEL'S OWN SHADOW GREEN and is not a
        /// substitute for an outline. `AccessoryOutlineWidth` sizes the ink border to each piece's
        /// own thinness (`docs/TODO.md` § 78.10), which is what stopped these markings being
        /// swallowed by a hull wider than themselves.
        /// </summary>
        private static void AddDanteLengthStripe(Transform arm, string name, float x,
                                                 float capReach, Color green, Color dark)
        {
            // ⚠️⚠️ A SQUARE CORNER, NOT A ROTATED BAR. The first cut of the bend used an angled
            // bridge between two offset runs, and at 52 degrees it stopped reading as one band:
            // the render came back as four disconnected diagonal blocks per arm. A rotated box
            // meets an axis-aligned box along a wedge, so the join is only as wide as the overlap
            // and the eye loses the line. An L made of two axis-aligned pieces shares a whole
            // face and reads as one continuous marking, which is what the model's does.
            //
            // ⚠️ AND IT IS THE SHAPE ITSELF. 🧑: *"still not how his arms look in model its too
            // straight"* about the near-straight version, after *"that is not how dante's arms
            // look like at all"* about the chevrons. The marking runs DOWN the forearm and then
            // turns ACROSS it once, near the elbow. Straight misses the turn; a shallow kink is
            // invisible; a steep diagonal is not a turn at all.
            foreach (float z in new[] { 1.0f, -1.0f })
            {
                string face = z > 0.0f ? "Front" : "Back";
                float baseZ = 0.126f * z;
                float bodyZ = 0.129f * z;

                // The long run, down the arm.
                AddBoxAccessory(arm, name + face + "RunBase", new Vector3(0.054f, 0.320f, 0.015f),
                    new Vector3(x, 0.430f, baseZ), Quaternion.identity, dark);
                AddBoxAccessory(arm, name + face + "RunBody", new Vector3(0.046f, 0.312f, 0.018f),
                    new Vector3(x, 0.430f, bodyZ), Quaternion.identity, green);

                // The corner, turning across the arm at the top of the run.
                AddBoxAccessory(arm, name + face + "CapBase",
                    new Vector3(Mathf.Abs(capReach) + 0.054f, 0.054f, 0.015f),
                    new Vector3(x + capReach * 0.5f, 0.603f, baseZ), Quaternion.identity, dark);
                AddBoxAccessory(arm, name + face + "CapBody",
                    new Vector3(Mathf.Abs(capReach) + 0.046f, 0.046f, 0.018f),
                    new Vector3(x + capReach * 0.5f, 0.603f, bodyZ), Quaternion.identity, green);
            }

            // The outer edge, so the pattern survives the arm turning away from the camera.
            AddBoxAccessory(arm, name + "OuterWrap", new Vector3(0.018f, 0.300f, 0.055f),
                new Vector3(-0.132f, 0.470f, 0.030f), Quaternion.identity, green);
        }

        private static void BuildCheskaAccessories(Transform arm, bool isRight)
        {
            var white = new Color(0.957f, 0.980f, 1.000f, 1.0f);
            var cyanCuff = new Color(0.149f, 0.588f, 0.659f, 1.0f);
            var cyanBand = new Color(0.392f, 0.886f, 0.965f, 1.0f);

            // build_person_voxel.py gives Cheska a white short sleeve, stepped cyan cuff,
            // bare forearm and hand, with a striped sports band on the left wrist only.
            AddBoxAccessory(arm, "WhiteShortSleeve", new Vector3(0.30f, 0.32f, 0.30f),
                new Vector3(0.0f, 0.16f, 0.0f), Quaternion.identity, white);
            AddBoxAccessory(arm, "CyanSleeveCuff", new Vector3(0.32f, 0.08f, 0.32f),
                new Vector3(0.0f, 0.36f, 0.0f), Quaternion.identity, cyanCuff);
            if (!isRight)
            {
                AddBoxAccessory(arm, "CyanSportsBand", new Vector3(0.31f, 0.10f, 0.30f),
                    new Vector3(0.0f, 0.50f, 0.0f), Quaternion.identity, cyanBand);
                AddBoxAccessory(arm, "WhiteBandStripe", new Vector3(0.315f, 0.025f, 0.305f),
                    new Vector3(0.0f, 0.50f, 0.0f), Quaternion.identity, white);
            }
        }

        private static void BuildNemuAccessories(Transform arm, bool isRight, ViewmodelArms parent = null)
        {
            // Exact color transcription from Nemu's 3D roster palette (tools/build_nemu_voxel.py & person_team-nemu.tres)
            var hoodieDark = new Color(0.137f, 0.110f, 0.204f, 1.0f);   // HOODIE_DARK #231c34
            var hoodieShadow = new Color(0.094f, 0.071f, 0.141f, 1.0f); // HOODIE_SHADOW #181224
            var lavenderTrim = new Color(0.667f, 0.361f, 0.941f, 1.0f); // LAVENDER_GLOW #aa5cf0
            var skinTone = SkinNemu;                                     // SKIN #e0af84

            // -------------------------------------------------------------------
            // ⚠️⚠️ THESE ARE BOXES NOW, AND THEY ARE BOXES BECAUSE HER MODEL IS.
            //
            // 🧑 2026-08-27, after the cloth solver came out: *"did u replace nemu's sleeves with
            // something that looks like sleeves of her 3d model?"*, having asked for *"cute
            // blocky sleeves"*. Deleting the solver stopped the phasing and left the wrong SHAPE
            // standing: three lofted 24-segment tubes that flared toward the cuff with a lavender
            // RIM around the opening.
            //
            // ⚠️⚠️ `Logs/model-ref-nemu.png` IS THE ANSWER AND IT DISAGREES ON EVERY POINT.
            // Her arms are straight plum boxes, they do not flare, and the lavender is a VERTICAL
            // BAR DOWN THE OUTER EDGE rather than a band around the wrist. The viewmodel was a
            // different garment in the same two colours: correct palette, invented silhouette.
            // In first person the sleeve is most of the screen, so it is the piece of her that a
            // player looks at longest and the one that most has to be her.
            //
            // ⚠️ SAME CONSTRUCTION AS EVERY OTHER HERO'S ARM NOW. Cheska and Phaister are stacked
            // `AddBoxAccessory` calls; Nemu was the only one carrying bespoke lofted geometry, a
            // vertex solver and three mesh builders to be an outlier. `tools/build_person_voxel.py`
            // emits boxes, `docs/Voxel_Person_Guide.md` is about boxes, and the toon shader's ink
            // outline is at its best on a hard edge.
            //
            // ⚠️ THE STRIPE IS ON BOTH SIDE FACES ON PURPOSE. `RightBasisX` and `LeftBasisX` are
            // rotated frames rather than mirrored scales, so "outer" is not the same local sign
            // on the two arms and a single stripe would be correct on one and inside the other.
            // Two thin bars cost four triangles and cannot be handed wrong.
            // -------------------------------------------------------------------

            // -------------------------------------------------------------------
            // ⚠️⚠️ AND THE ARM HAS TO COME OUT OF THE SLEEVE. 🧑 2026-08-27, at the box version:
            // *"give some volume ot her sleeves and make it look liek her arms are a bit thhicker
            // and come from inside her sleeves bcz it floating in ur pic"*.
            //
            // ⚠️⚠️ IT WAS FLOATING BECAUSE THERE WAS NO FOREARM AT ALL. `ApplyCharacterStyle`
            // switches `_rightArmRenderer.enabled` OFF for Nemu (`hasCustomArmMesh`), on the
            // assumption that her accessories draw the whole limb. They did not: the sleeve
            // stopped at 0.65 and the hand block started at 0.70, so a small pale chip hung in
            // the air five centimetres above a big plum box with nothing between them. No amount
            // of resizing the two ends fixes a missing middle.
            //
            // ⚠️ THE FOREARM STARTS **INSIDE** THE CUFF, at 0.49 against a cuff that ends at
            // 0.63. Butting it against the opening would leave a seam that reads as two objects
            // touching; starting it 0.14 m up inside is what makes the arm read as EMERGING.
            //
            // ⚠️ AND THE OPENING IS THREE CONCENTRIC BOXES, WHICH IS HOW A VOXEL SLEEVE SHOWS A
            // HOLE. Hem at 0.384 wide, the shadow lining at 0.330, the forearm at 0.150: the
            // shadow reads as a dark ring around the wrist rather than as a cap over it. A single
            // dark box at the mouth would just be a lid.
            //
            // ⚠️ THE VOLUME IS A STEP, NOT A LOFT. `docs/Voxel_Person_Guide.md` and
            // `tools/build_person_voxel.py` are boxes, and the whole reason the previous version
            // was thrown out is that it was a smooth 24-segment tube. An oversized hoodie is a
            // narrow box with a wider box on the end of it.
            // -------------------------------------------------------------------

            // ⚠️⚠️ THE SLEEVE STARTS AT y = 0, NOT ABOVE IT, AND THE FIRST PASS AT 0.035 LEFT A
            // CREAM SLIVER OF BARE ARM AT THE SHOULDER END OF THE FRAME. Cheska's sleeve is
            // authored 0.00 to 0.32 for exactly this reason: `Models/viewmodel_arm` runs from the
            // pivot, so anything that does not begin at the pivot shows the limb behind it.

            // 1. The upper sleeve, 0.345 across so it clears the shared arm mesh underneath
            //    rather than sitting flush on it.
            AddBoxAccessory(arm, "HoodieSleeve", new Vector3(0.345f, 0.395f, 0.335f),
                new Vector3(0.0f, 0.1975f, 0.0f), Quaternion.identity, hoodieDark);

            // 2. The oversized drop at the wrist. This is the volume: one step out to 0.386.
            AddBoxAccessory(arm, "HoodieCuff", new Vector3(0.386f, 0.170f, 0.374f),
                new Vector3(0.0f, 0.480f, 0.0f), Quaternion.identity, hoodieDark);

            // 3. The hem ring at the very end of the cuff, a shade darker and a shade wider.
            AddBoxAccessory(arm, "HoodieHem", new Vector3(0.398f, 0.058f, 0.386f),
                new Vector3(0.0f, 0.594f, 0.0f), Quaternion.identity, hoodieShadow);

            // 4. The inside of the sleeve, seen as a dark ring around the wrist. It sits just
            //    under the hem's top face and is WIDER than the arm and NARROWER than the hem, so
            //    what reads is an opening with a limb coming out of it. A single dark box across
            //    the whole mouth would be a lid.
            AddBoxAccessory(arm, "HoodieMouth", new Vector3(0.340f, 0.040f, 0.328f),
                new Vector3(0.0f, 0.601f, 0.0f), Quaternion.identity, hoodieShadow);

            // 5. The lavender bar down each side face of the upper sleeve. It stands 0.018 proud,
            //    which is enough to never z-fight and little enough to read as paint on cloth.
            AddBoxAccessory(arm, "LavenderStripeA", new Vector3(0.036f, 0.355f, 0.175f),
                new Vector3(0.172f, 0.1975f, 0.0f), Quaternion.identity, lavenderTrim);

            AddBoxAccessory(arm, "LavenderStripeB", new Vector3(0.036f, 0.355f, 0.175f),
                new Vector3(-0.172f, 0.1975f, 0.0f), Quaternion.identity, lavenderTrim);

            // 6. ⚠️ NO FOREARM BOX. `Models/viewmodel_arm` is the forearm now and it is switched
            //    back on for her in `ApplyCharacterStyle`. A hand-rolled box was tried first and
            //    came out 0.150 across against a 0.372 cuff, which is a tab rather than an arm;
            //    the shared mesh is the section the whole cast's arms are, which is what
            //    *"a bit thhicker"* actually asks for. The sleeve ends at 0.605 and the hand
            //    starts at 0.695, so there are nine centimetres of bare wrist between them.

            // 4. Cute Tucked Hand in exact Nemu Skin Tone (#e0af84 and #d69974)
            var handGo = new GameObject(AccessoryPrefix + "SpiritHand");
            handGo.transform.SetParent(arm, false);
            handGo.transform.localPosition = Vector3.zero;
            handGo.transform.localRotation = Quaternion.identity;
            handGo.transform.localScale = Vector3.one;

            // ⚠️⚠️ ONE BOX, FOR THE SAME REASON `AddHandKnuckles` IS ONE BOX. This was a palm, a
            // thumb, a finger block and a shaded fingertip: four pieces of anatomy the voxel cast
            // does not have. *"i dont want finger geometry at all on any of the fkn FPP shit /
            // bcz our characters dont ahve fingers !"*, and `tools/build_person_voxel.py` agrees,
            // emitting a single `hand-left` / `hand-right` box in `SKIN_LIT` and nothing else.
            //
            // ⚠️ IT KEEPS THE OLD BLOCK'S REACH, 0.70 to 0.82, so the hand still wraps the held
            // tsinelas exactly where it did. The anatomy is gone; the placement is not.
            //
            // ⚠️⚠️ THE REACH IS KEPT AND THE SECTION IS NOT, because 0.086 by 0.042 beside a
            // 0.372 cuff is not a hand, it is a chip. That thinness is most of why the whole
            // limb read as floating: the eye had a big box, a gap, and something too small to be
            // attached to it. 0.170 by 0.160 is the section of the forearm it sits on, which is
            // what `tools/build_person_voxel.py` emits: one `hand-right` box, no taper.
            AddBoxAccessory(handGo.transform, "Hand", new Vector3(0.170f, 0.130f, 0.160f),
                new Vector3(0.0f, 0.760f, 0.0f), Quaternion.identity, skinTone);
        }

        private static void BuildPhaisterAccessories(Transform arm, bool isRight)
        {
            var blackSleeve = new Color(0.094f, 0.086f, 0.133f, 1.0f); // COAT_DARK #181622
            var purpleBand  = new Color(0.290f, 0.118f, 0.471f, 1.0f); // CLOTH_PURPLE #4a1e78
            var crimsonBand = new Color(0.549f, 0.078f, 0.141f, 1.0f); // CRIMSON #8c1424
            var goldStripe  = new Color(0.973f, 0.722f, 0.141f, 1.0f); // GOLD #f8b824
            var whiteCuff   = Color.white;                              // WHITE #ffffff
            var skinTone    = SkinPhaister;                             // SKIN #f4c098
            var skinDark    = new Color(0.878f, 0.627f, 0.471f, 1.0f); // SKIN_DARK #e0a078

            // 1. Black coat upper sleeve (Y ~ 0.04 to 0.22)
            AddBoxAccessory(arm, "BlackUpperSleeve", new Vector3(0.295f, 0.220f, 0.295f),
                new Vector3(0.0f, 0.110f, 0.0f), Quaternion.identity, blackSleeve);

            // 2. Royal Purple Forearm Band (Y ~ 0.22 to 0.48)
            AddBoxAccessory(arm, "PurpleSleeveBand", new Vector3(0.305f, 0.260f, 0.305f),
                new Vector3(0.0f, 0.350f, 0.0f), Quaternion.identity, purpleBand);

            // Gold Cross Emblem on outer forearm
            float crossSign = isRight ? 1.0f : -1.0f;
            AddBoxAccessory(arm, "GoldSleeveCrossV", new Vector3(0.025f, 0.090f, 0.025f),
                new Vector3(crossSign * 0.156f, 0.350f, 0.0f), Quaternion.identity, goldStripe);
            AddBoxAccessory(arm, "GoldSleeveCrossH", new Vector3(0.025f, 0.025f, 0.080f),
                new Vector3(crossSign * 0.156f, 0.350f, 0.0f), Quaternion.identity, goldStripe);

            // 3. Crimson Red Sleeve Stripe (Y ~ 0.48 to 0.53)
            AddBoxAccessory(arm, "CrimsonSleeveStripe", new Vector3(0.310f, 0.050f, 0.310f),
                new Vector3(0.0f, 0.505f, 0.0f), Quaternion.identity, crimsonBand);

            // 4. Crisp White Flared Cuff Rim (Y ~ 0.53 to 0.62, right at wrist)
            AddBoxAccessory(arm, "WhiteCuffRim", new Vector3(0.330f, 0.085f, 0.330f),
                new Vector3(0.0f, 0.570f, 0.0f), Quaternion.identity, whiteCuff);

            // 5. Porcelain Skin Hand & Knuckle details (Y ~ 0.62 to 0.82)
            if (isRight)
            {
                AddHandKnuckles(arm, isRight, 0.260f, skinTone, skinDark);
            }
            else
            {
                AddHandKnuckles(arm, isRight, 0.260f, skinTone, skinDark);
            }
        }

        // -------------------------------------------------------------------
        // § CLASSIC ROSTER BESPOKE ACCESSORY BUILDERS
        // -------------------------------------------------------------------

        private static void BuildBayanAccessories(Transform arm, bool isRight)
        {
            var greenShirt = new Color(0.247f, 0.561f, 0.361f, 1.0f);
            var lightWrap = new Color(0.941f, 0.694f, 0.518f, 1.0f);

            // character-male-f.glb uses the same green sleeve and pale forearm wrap
            // on both arms. There is no watch or one-sided accessory.
            AddBoxAccessory(arm, "GreenShortSleeve", new Vector3(0.31f, 0.36f, 0.31f),
                new Vector3(0.0f, 0.18f, 0.0f), Quaternion.identity, greenShirt);
            AddBoxAccessory(arm, "PaleForearmWrap", new Vector3(0.32f, 0.14f, 0.32f),
                new Vector3(0.0f, 0.435f, 0.0f), Quaternion.identity, lightWrap);
        }

        private static void BuildMaringAccessories(Transform arm, bool isRight)
        {
            var blouse = new Color(0.192f, 0.141f, 0.114f, 1.0f);
            var maroon = new Color(0.541f, 0.204f, 0.275f, 1.0f);

            // character-female-f.glb has dark sleeves on both arms and one maroon
            // left forearm wrap. Those are the only non-skin arm palette slots.
            AddBoxAccessory(arm, "DarkBlouseSleeve", new Vector3(0.30f, 0.35f, 0.30f),
                new Vector3(0.0f, 0.175f, 0.0f), Quaternion.identity, blouse);

            if (!isRight)
            {
                AddBoxAccessory(arm, "MaroonForearmWrap", new Vector3(0.32f, 0.16f, 0.32f),
                    new Vector3(0.0f, 0.515f, 0.0f), Quaternion.identity, maroon);
            }
        }

        private static void BuildTotoyAccessories(Transform arm, bool isRight)
        {
            var darkGreenShirt = new Color(0.184f, 0.490f, 0.310f, 1.0f);
            // character-male-a.glb has matching green short sleeves and bare arms.
            AddBoxAccessory(arm, "GreenShortSleeve", new Vector3(0.30f, 0.35f, 0.30f),
                new Vector3(0.0f, 0.175f, 0.0f), Quaternion.identity, darkGreenShirt);
        }

        private static void BuildIndayAccessories(Transform arm, bool isRight)
        {
            var yellow = new Color(0.878f, 0.706f, 0.235f, 1.0f);
            var coral = new Color(0.761f, 0.329f, 0.247f, 1.0f);
            var plum = new Color(0.478f, 0.247f, 0.369f, 1.0f);

            // character-female-a.glb uses a yellow short sleeve and the same chunky
            // coral gauntlet with a plum centre strap on both arms.
            AddBoxAccessory(arm, "YellowShortSleeve", new Vector3(0.30f, 0.30f, 0.30f),
                new Vector3(0.0f, 0.15f, 0.0f), Quaternion.identity, yellow);
            AddBoxAccessory(arm, "CoralSleeveCap", new Vector3(0.32f, 0.055f, 0.32f),
                new Vector3(0.0f, 0.325f, 0.0f), Quaternion.identity, coral);
            AddBoxAccessory(arm, "CoralForearmGuard", new Vector3(0.34f, 0.20f, 0.34f),
                new Vector3(0.0f, 0.485f, 0.0f), Quaternion.identity, coral);
            AddBoxAccessory(arm, "PlumGuardStrap", new Vector3(0.35f, 0.065f, 0.35f),
                new Vector3(0.0f, 0.485f, 0.0f), Quaternion.identity, plum);
        }

        private static void BuildKuyaBoyAccessories(Transform arm, bool isRight)
        {
            var navyShirt = new Color(0.165f, 0.290f, 0.478f, 1.0f);
            // character-male-b.glb has plain navy sleeves and bare forearms.
            AddBoxAccessory(arm, "NavyShortSleeve", new Vector3(0.30f, 0.40f, 0.30f),
                new Vector3(0.0f, 0.20f, 0.0f), Quaternion.identity, navyShirt);
        }

        private static void BuildAteGirlieAccessories(Transform arm, bool isRight)
        {
            var magentaTop = new Color(0.851f, 0.310f, 0.416f, 1.0f);
            // character-female-b.glb has plain pink sleeves and bare forearms.
            AddBoxAccessory(arm, "PinkShortSleeve", new Vector3(0.30f, 0.40f, 0.30f),
                new Vector3(0.0f, 0.20f, 0.0f), Quaternion.identity, magentaTop);
        }

        private static void BuildTikboyAccessories(Transform arm, bool isRight)
        {
            var oliveShirt = new Color(0.416f, 0.620f, 0.290f, 1.0f);
            var darkGuard = new Color(0.220f, 0.220f, 0.239f, 1.0f);

            // character-male-c.glb has olive sleeves and one solid dark left guard.
            AddBoxAccessory(arm, "OliveShortSleeve", new Vector3(0.30f, 0.35f, 0.30f),
                new Vector3(0.0f, 0.175f, 0.0f), Quaternion.identity, oliveShirt);
            if (!isRight)
            {
                AddBoxAccessory(arm, "DarkLeftForearmGuard", new Vector3(0.32f, 0.15f, 0.32f),
                    new Vector3(0.0f, 0.51f, 0.0f), Quaternion.identity, darkGuard);
            }
        }

        private static void BuildBebangAccessories(Transform arm, bool isRight)
        {
            var burgundyBlouse = new Color(0.541f, 0.227f, 0.227f, 1.0f);

            // character-female-c.glb has matching burgundy sleeves and bare arms.
            AddBoxAccessory(arm, "BurgundyShortSleeve", new Vector3(0.30f, 0.35f, 0.30f),
                new Vector3(0.0f, 0.175f, 0.0f), Quaternion.identity, burgundyBlouse);
        }

        private static void BuildJunJunAccessories(Transform arm, bool isRight)
        {
            var blueShirt = new Color(0.133f, 0.157f, 0.227f, 1.0f);
            var whiteTrim = new Color(1.000f, 1.000f, 1.000f, 1.0f);
            // character-male-d.glb has navy sleeves with white hems on both arms.
            AddBoxAccessory(arm, "NavyJacketSleeve", new Vector3(0.30f, 0.38f, 0.30f),
                new Vector3(0.0f, 0.19f, 0.0f), Quaternion.identity, blueShirt);
            AddBoxAccessory(arm, "WhiteSleeveHem", new Vector3(0.32f, 0.075f, 0.32f),
                new Vector3(0.0f, 0.405f, 0.0f), Quaternion.identity, whiteTrim);
        }

        private static void BuildLolaPacingAccessories(Transform arm, bool isRight)
        {
            var greyBaro = new Color(0.541f, 0.478f, 0.416f, 1.0f);
            var whiteLace = new Color(0.96f, 0.96f, 0.98f, 1.0f);
            // character-female-d.glb has taupe sleeves with white hems on both arms.
            AddBoxAccessory(arm, "TaupeBaroSleeve", new Vector3(0.30f, 0.38f, 0.30f),
                new Vector3(0.0f, 0.19f, 0.0f), Quaternion.identity, greyBaro);
            AddBoxAccessory(arm, "WhiteBaroHem", new Vector3(0.32f, 0.075f, 0.32f),
                new Vector3(0.0f, 0.405f, 0.0f), Quaternion.identity, whiteLace);
        }

        private static void BuildMangKanorAccessories(Transform arm, bool isRight)
        {
            var whiteSando = new Color(1.000f, 1.000f, 1.000f, 1.0f);
            // character-male-e.glb has white short sleeves and bare dark arms.
            AddBoxAccessory(arm, "WhiteShortSleeve", new Vector3(0.30f, 0.40f, 0.30f),
                new Vector3(0.0f, 0.20f, 0.0f), Quaternion.identity, whiteSando);
        }

        private static void BuildAlingNenaAccessories(Transform arm, bool isRight)
        {
            var whiteBlouse = new Color(1.000f, 1.000f, 1.000f, 1.0f);
            // character-female-e.glb has white short sleeves and bare orange-tan arms.
            AddBoxAccessory(arm, "WhiteBlouseSleeve", new Vector3(0.30f, 0.40f, 0.30f),
                new Vector3(0.0f, 0.20f, 0.0f), Quaternion.identity, whiteBlouse);
        }

        private static void BuildClassicAccessories(Transform arm, bool isRight)
        {
            var shirtWhite = new Color(0.85f, 0.85f, 0.85f, 1.0f);
            var foldGrey = new Color(0.78f, 0.78f, 0.78f, 1.0f);
            var bandDark = new Color(0.18f, 0.18f, 0.20f, 1.0f);

            // 1. Rolled streetwear t-shirt sleeve
            AddCylinderAccessory(arm, "ClassicSleeve", 0.144f, 0.144f, 0.28f, 12,
                new Vector3(0.0f, 0.15f, 0.0f), Quaternion.identity, shirtWhite);
            AddCylinderAccessory(arm, "ClassicSleeveFold", 0.152f, 0.152f, 0.05f, 12,
                new Vector3(0.0f, 0.28f, 0.0f), Quaternion.identity, foldGrey);

            // 2. Neutral athletic wrist sweatband
            AddCylinderAccessory(arm, "ClassicWristband", 0.146f, 0.146f, 0.08f, 12,
                new Vector3(0.0f, 0.55f, 0.0f), Quaternion.identity, bandDark);
        }

        // -------------------------------------------------------------------
        // § PROCEDURAL GEOMETRY & ACCESSORY HELPERS
        // -------------------------------------------------------------------

        /// <summary>
        /// The hand: ONE box, in skin tone. No knuckles, no fingers, no thumb.
        ///
        /// ⚠️⚠️ THE CAST HAS NO FINGERS, SO THE VIEWMODEL MUST NOT EITHER. Stated plainly
        /// after a first attempt added them: *"i dont want finger geometry at all on any of the
        /// fkn FPP shit / bcz our characters dont ahve fingers !"*. He is right, and it is
        /// checkable rather than a matter of taste: `tools/build_person_voxel.py` emits exactly
        /// one box per hand, `hand-left` and `hand-right`, in `SKIN_LIT`. There is no finger,
        /// knuckle or thumb geometry anywhere in the voxel cast.
        ///
        /// ⚠️⚠️ AND THAT MAKES DETAILED FPP HANDS A CONTINUITY BUG, NOT AN UPGRADE. These
        /// arms are the SAME CHARACTER the other three players are looking at in third person.
        /// Modelling fingers here means the person you are is built to a different standard from
        /// the person everyone else sees, and the moment an emote swings the camera to third
        /// (`CLAUDE.md` section 4: emotes swing to TPP and back) the hands change shape.
        ///
        /// ⚠️ WHAT WAS THERE BEFORE WAS WORSE THAN EITHER. A flat plate with three `KnuckleIndent`
        /// boxes on it, each DEEPER than the plate (0.034 against 0.030) and 0.002 further
        /// forward, so the grooves stood proud of the surface they were grooving. Reported as
        /// *"wtf are those rectangles on his hand"*, which is exactly what they were.
        ///
        /// ⚠️ THE PROPORTIONS COME FROM THE VOXEL HAND RATHER THAN BEING PICKED.
        /// `build_person_voxel.py` authors it 0.1496 wide, 0.1234 tall and 0.058 deep, so the
        /// ratios are 1 : 0.825 : 0.388, and that is what is applied to whatever width the hero's
        /// arm asks for. A hand invented at this end would drift from the models the first time
        /// anybody retuned either.
        /// </summary>
        private static void AddHandKnuckles(Transform arm, bool isRight, float handWidth,
                                            Color skinTone, Color skinDark)
        {
            // ⚠️ `skinDark` IS DELIBERATELY UNUSED AND THE PARAMETER STAYS. It shaded the three
            // stripes; there is nothing left to shade, and the toon ramp gives the box its own
            // banding for free. Keeping the signature means the four call sites did not each need
            // editing again, and a hero that later wants a cuff colour has it to hand.
            AddBoxAccessory(arm, (isRight ? "Right" : "Left") + "Hand",
                new Vector3(handWidth, handWidth * 0.825f, handWidth * 0.388f),
                new Vector3(0.0f, 0.735f, 0.040f), Quaternion.identity, skinTone);
        }

        private static GameObject AddBoxAccessory(Transform parent, string name, Vector3 size,
            Vector3 pos, Quaternion rot, Color color, float emission = 0.0f, bool toon = true)
        {
            var mesh = CreateBoxMesh(size);
            return AddMeshAccessory(parent, name, mesh, pos, rot, color, emission, toon);
        }

        private static GameObject AddCylinderAccessory(Transform parent, string name, float radiusBottom,
            float radiusTop, float height, int segments, Vector3 pos, Quaternion rot, Color color,
            float emission = 0.0f, bool toon = true)
        {
            var mesh = CreateCylinderMesh(radiusBottom, radiusTop, height, segments);
            return AddMeshAccessory(parent, name, mesh, pos, rot, color, emission, toon);
        }

        /// <summary>
        /// How thick an ink border a viewmodel accessory may wear, solved from its own geometry.
        ///
        /// ⚠️⚠️ EVERY ACCESSORY USED `PersonOutlineWidth`, AND THAT IS WHY DANTE'S ARM MARKINGS
        /// DID NOT LOOK LIKE HIS MARKINGS. 🧑 2026-08-29, off an FPP frame: *"fix the markings and
        /// toon shader lines or wtv lines thes are for dante's fpp bcz it doesnt look like his
        /// character's real markings"*.
        ///
        /// `PersonOutlineWidth` is **0.019 m** and it is derived for a whole Person — the 2.38
        /// rig scale and the voxel face's feature size (`ModelPreview` works it out). Dante's
        /// runic glyph and his chevrons are boxes **0.015 to 0.018 m thick**. The ink hull is an
        /// inverted shell pushed out along every normal, so a 19 mm shell on a 15 mm plate is
        /// **wider than the plate it is outlining**: the border swallows the marking, meets
        /// itself around the edges, and what is left on screen is the sprawl of thin dark lines
        /// he is pointing at rather than a green glyph with a dark edge.
        ///
        /// ⚠️⚠️ THIS IS `docs/TODO.md` § 43 ON A DIFFERENT SURFACE, AND § 43 ALREADY WROTE THE
        /// RULE: *"Inflating that swoosh by 12 mm in every direction produces a hull far larger
        /// than the shape it is supposed to outline, so the ink covers the decal and only
        /// fragments show through."* That was the IKE decal and the answer there was to drop the
        /// hull on `slot > 0`, because a submesh sits on a base surface that is already outlined.
        /// **An accessory is the same thing built out of separate objects instead of submeshes**,
        /// so the same reasoning applies and the slot rule cannot reach it.
        ///
        /// ⚠️ SOLVED, NOT ZEROED, BECAUSE THE PIECES ARE NOT ALL SMALL. Dante's leather sleeve is
        /// 0.30 m and Cheska's cuffs are chunky; those are forms in their own right and want the
        /// full border. Only the thin ones must come down, so the bound is the accessory's own
        /// THINNEST axis. A quarter of it keeps the shell comfortably inside the shape: a 0.015 m
        /// glyph plate gets 0.00375 m and reads as a crisp edge, while anything thicker than
        /// 0.076 m is unchanged at `PersonOutlineWidth`.
        ///
        /// ⚠️ MEASURED FROM THE MESH RATHER THAN PASSED IN, so it covers `AddCylinderAccessory`
        /// and every future shape without a second call site to keep in step.
        /// </summary>
        private static float AccessoryOutlineWidth(Mesh mesh)
        {
            if (mesh == null) return Visual.ToonSkin.PersonOutlineWidth;

            var size = mesh.bounds.size;
            float thinnest = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            if (thinnest <= 0.0001f) return Visual.ToonSkin.PersonOutlineWidth;

            return Mathf.Min(Visual.ToonSkin.PersonOutlineWidth, thinnest * 0.25f);
        }

        private static GameObject AddMeshAccessory(Transform parent, string name, Mesh mesh,
            Vector3 pos, Quaternion rot, Color color, float emission, bool toon)
        {
            var go = new GameObject(AccessoryPrefix + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            if (emission > 0.001f)
            {
                Visual.VfxMaterial.Solid(mr, color, emission);
            }
            else
            {
                Visual.MaterialKit.Dress(mr, color);
                if (toon) Visual.ToonSkin.Apply(mr, AccessoryOutlineWidth(mesh));
            }

            return go;
        }

        private static Mesh CreateBoxMesh(Vector3 size)
        {
            var mesh = new Mesh { name = "HeroArm_Box" };
            float hx = size.x * 0.5f;
            float hy = size.y * 0.5f;
            float hz = size.z * 0.5f;

            var vertices = new Vector3[]
            {
                // Front (+Z)
                new Vector3(-hx, -hy,  hz), new Vector3( hx, -hy,  hz), new Vector3( hx,  hy,  hz), new Vector3(-hx,  hy,  hz),
                // Back (-Z)
                new Vector3( hx, -hy, -hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx,  hy, -hz), new Vector3( hx,  hy, -hz),
                // Top (+Y)
                new Vector3(-hx,  hy,  hz), new Vector3( hx,  hy,  hz), new Vector3( hx,  hy, -hz), new Vector3(-hx,  hy, -hz),
                // Bottom (-Y)
                new Vector3(-hx, -hy, -hz), new Vector3( hx, -hy, -hz), new Vector3( hx, -hy,  hz), new Vector3(-hx, -hy,  hz),
                // Right (+X)
                new Vector3( hx, -hy,  hz), new Vector3( hx, -hy, -hz), new Vector3( hx,  hy, -hz), new Vector3( hx,  hy,  hz),
                // Left (-X)
                new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy,  hz), new Vector3(-hx,  hy,  hz), new Vector3(-hx,  hy, -hz),
            };

            var normals = new Vector3[]
            {
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right,
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
            };

            var triangles = new int[36];
            for (int face = 0; face < 6; face++)
            {
                int v = face * 4;
                int t = face * 6;
                triangles[t + 0] = v + 0;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 0;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCylinderMesh(float radiusBottom, float radiusTop, float height, int segments)
        {
            var mesh = new Mesh { name = "HeroArm_Cylinder" };
            int vCount = (segments + 1) * 2;
            var vertices = new Vector3[vCount];
            var normals = new Vector3[vCount];
            var triangles = new int[segments * 6];

            float halfH = height * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2.0f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[i] = new Vector3(cos * radiusBottom, -halfH, sin * radiusBottom);
                vertices[i + segments + 1] = new Vector3(cos * radiusTop, halfH, sin * radiusTop);

                Vector3 n = new Vector3(cos, 0.0f, sin).normalized;
                normals[i] = n;
                normals[i + segments + 1] = n;
            }

            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int b1 = i;
                int b2 = i + 1;
                int t1 = i + segments + 1;
                int t2 = i + segments + 2;

                triangles[t++] = b1;
                triangles[t++] = t1;
                triangles[t++] = b2;

                triangles[t++] = b2;
                triangles[t++] = t1;
                triangles[t++] = t2;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Godot is right-handed with -Z forward; Unity is left-handed with +Z forward. For a
        /// POSITION that is a single sign flip on Z — the same flip the map conversion makes.
        /// </summary>
        private static Vector3 ToUnityPosition(Vector3 godot)
            => new Vector3(godot.x, godot.y, -godot.z);

        /// <summary>
        /// The same flip for a BASIS, which is not a single sign change.
        ///
        /// ⚠️ MIRRORING A ROTATION IS NOT MIRRORING THREE VECTORS INDEPENDENTLY. Reflecting
        /// through the XY plane negates the Z COMPONENT of the X and Y axes, and negates the
        /// X and Y COMPONENTS of the Z axis — which together preserve handedness. Flipping
        /// every z the same way instead yields a mirrored basis with a negative determinant,
        /// and Unity renders that as an arm turned inside out.
        /// </summary>
        private static Quaternion ToUnityRotation(Vector3 bx, Vector3 by, Vector3 bz)
        {
            Vector3 x = new Vector3(bx.x, bx.y, -bx.z);
            Vector3 y = new Vector3(by.x, by.y, -by.z);
            Vector3 z = new Vector3(-bz.x, -bz.y, bz.z);

            // LookRotation wants forward and up; a basis's Z is its forward and Y its up.
            if (z.sqrMagnitude < 0.0001f || y.sqrMagnitude < 0.0001f) return Quaternion.identity;

            return Quaternion.LookRotation(z.normalized, y.normalized);
        }

        /// <summary>
        /// The idle breathe. Two arms, slightly different swings and the same period, so they
        /// move together without moving identically.
        /// </summary>
        /// <summary>
        /// The idle breathe. Two arms, slightly different swings and the same period, so they
        /// move together without moving identically.
        /// </summary>
        private void LateUpdate() => StepVisuals(Time.deltaTime);

        public void StepVisuals(float dt, bool snap = false)
        {
            _phase += dt;

            // § THE ACTION CLIPS, stepped before the pose below so a throw reads over whatever
            // the pivot is doing rather than under it.
            StepAction(dt);

            float t = Mathf.Sin(_phase / IdlePeriod * Mathf.PI * 2.0f);

            // ⚠️ THE EMPTY ARM ALWAYS BREATHES; THE CARRYING ONE HOLDS ITS POSE. An idle
            // swing folded on top of the carry pose makes the held tsinelas wobble in the
            // hand, which reads as the attachment being loose again.
            if (_leftPivot != null)
            {
                _leftPivot.localRotation =
                    _leftRest * Quaternion.Euler(-t * IdleLeftSwing * Mathf.Rad2Deg, 0.0f,
                                                 -t * 0.02f * Mathf.Rad2Deg);
            }

            if (_rightPivot == null) return;

            if (!_carrying)
            {
                if (snap)
                {
                    _rightPivot.localPosition = _rightRestPos;
                    _rightPivot.localRotation = _rightRest * Quaternion.Euler(-t * IdleRightSwing * Mathf.Rad2Deg, 0.0f, t * 0.02f * Mathf.Rad2Deg);
                    _rightPivot.localScale = _rightRestScale;
                }
                else
                {
                    StepToward(_rightRestPos,
                               _rightRest * Quaternion.Euler(-t * IdleRightSwing * Mathf.Rad2Deg, 0.0f,
                                                             t * 0.02f * Mathf.Rad2Deg),
                               _rightRestScale, dt);
                }
                return;
            }

            // A FIXED carry pose, not a chase. Nothing here reads the world slipper's position,
            // so the two can never drag each other around, which is what produced the reported
            // "my arms float during windup".
            Vector3 dir = CarryDir.normalized;
            float reach = ArmLength * CarryScale;
            Vector3 elbow = CarryAnchor - dir * reach;

            // The arm mesh runs along +Y from the elbow, so the pose is "point the pivot's up
            // axis along dir". Any roll about that axis is equally correct; a stable reference
            // keeps the hand from spinning as the view turns.
            Vector3 reference = Mathf.Abs(Vector3.Dot(dir, Vector3.forward)) > 0.99f
                ? Vector3.right
                : Vector3.forward;

            Vector3 right = Vector3.Cross(dir, reference).normalized;
            Vector3 forward = Vector3.Cross(right, dir).normalized;

            if (snap)
            {
                _rightPivot.localPosition = elbow;
                _rightPivot.localRotation = Quaternion.LookRotation(forward, dir);
                _rightPivot.localScale = Vector3.one * CarryScale;
            }
            else
            {
                StepToward(elbow, Quaternion.LookRotation(forward, dir), Vector3.one * CarryScale, dt);
            }
        }

        private void StepToward(Vector3 position, Quaternion rotation, Vector3 scale, float dt)
        {
            if (_rightPivot == null) return;
            float k = Mathf.Clamp01(ReachSpeed * dt);

            _rightPivot.localPosition = Vector3.Lerp(_rightPivot.localPosition, position, k);
            _rightPivot.localRotation = Quaternion.Slerp(_rightPivot.localRotation, rotation, k);
            _rightPivot.localScale = Vector3.Lerp(_rightPivot.localScale, scale, k);
        }
    }
}
