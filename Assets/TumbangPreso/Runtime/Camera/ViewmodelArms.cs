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

        /// <summary>Where the carried tsinelas sits on the right forearm.</summary>
        private static readonly Vector3 HeldSlipperLocal = new Vector3(0.0f, 0.86f, 0.0f);

        /// <summary>The arm colour from the .tscn's shader material.</summary>
        public static readonly Color ArmColour = new Color(0.784f, 0.529f, 0.353f, 1.0f);

        // -------------------------------------------------------------------
        // § HERO SKIN TONES
        // -------------------------------------------------------------------
        public static readonly Color SkinSean = new Color(0.78f, 0.52f, 0.32f, 1.0f);     // Golden brown / warm tan
        public static readonly Color SkinZack = new Color(0.72f, 0.48f, 0.32f, 1.0f);     // Warm athletic tan
        public static readonly Color SkinDante = new Color(0.38f, 0.24f, 0.16f, 1.0f);    // Deep dark bronze / volcanic skin
        public static readonly Color SkinCheska = new Color(0.95f, 0.84f, 0.78f, 1.0f);   // Fair porcelain skin
        public static readonly Color SkinNemu = new Color(0.80f, 0.75f, 0.90f, 1.0f);     // Pale lavender / ghostly ethereal

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
        /// ⚠️ MEASURED, NOT TYPED, AND THE OLD VALUE WAS 0.171 m. The node is authored at mesh
        /// scale and then inherits TWO nested shrinks — the arms' own 0.72 and the carry pose's
        /// 0.55 — so a 0.432 m mesh arrived on screen at 0.396 of its size. It was reported as
        /// "it doesnt get seen in first person" rather than as a size bug, because nothing was
        /// switched off: it was simply too small to notice at the fingertip.
        /// </summary>
        public const float SlipperLength = 0.34f;

        private const string AccessoryPrefix = "~HeroAccessory_";

        private Transform _rightPivot;
        private Transform _leftPivot;
        private Transform _rightArm;
        private Transform _leftArm;
        private MeshRenderer _rightArmRenderer;
        private MeshRenderer _leftArmRenderer;
        private Transform _heldSlipper;
        private Renderer _heldRenderer;

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

            if (filter.sharedMesh == source.sharedMesh) return;

            filter.sharedMesh = source.sharedMesh;

            var sourceRenderer = source.GetComponent<Renderer>();
            if (_heldRenderer != null && sourceRenderer != null)
                _heldRenderer.sharedMaterial = sourceRenderer.sharedMaterial;

            // The mesh changed, so the length-normalising scale has to be recomputed.
            NormaliseHeldSize();

            // ⚠️ AND THE TOON MATERIAL RE-APPLIED. The line above copies the WORLD slipper's
            // material, which is already a toon variant carrying that skin's colour, but its
            // outline width was measured against the world object's scale rather than the
            // fistful-sized copy in the viewmodel. Re-deriving it here is what keeps the border
            // the same thickness in both views.
            Visual.ToonSkin.Apply(_heldRenderer, Visual.ToonSkin.PropOutlineWidth);
        }

        /// <summary>
        /// Scales the held mesh so it presents at <see cref="SlipperLength"/> whatever the skin
        /// authored, cancelling the two nested shrinks it inherits.
        /// </summary>
        private void NormaliseHeldSize()
        {
            if (_heldRenderer == null) return;

            var filter = _heldSlipper.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            var size = filter.sharedMesh.bounds.size;
            float longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (longest <= 0.0001f) return;

            // Against the parent's CURRENT world scale, so the slipper keeps this size while
            // the carry pose is still interpolating in rather than growing as the arm settles.
            float parent = Mathf.Max(0.0001f, _heldSlipper.parent.lossyScale.x);

            _heldSlipper.localScale = Vector3.one * (SlipperLength / longest / parent);
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

        private Key[] _clip;
        private float _clipTime;

        /// <summary>
        /// § THE WIND-UP. How far the throwing arm cocks back at full charge, radians.
        ///
        /// ⚠️ 0.62 (~36°) IS THE .gd's OWN NUMBER AND ITS REASONING IS WHY IT IS NOT SMALLER:
        /// *"the HUD charge meter is on the YOU card at the bottom corner, which nobody looks at
        /// while aiming. 0.62 rad is enough to be unmistakable in peripheral vision without the
        /// fist leaving the frame."* The arm IS the charge meter in first person.
        /// </summary>
        public const float WindupRad = 0.62f;

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
                  : null;

            _clipTime = 0.0f;
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
                _rightArm.localRotation = Quaternion.Euler(WindupRad * _charge * Mathf.Rad2Deg,
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
                Visual.ToonSkin.Apply(_heldRenderer, Visual.ToonSkin.PropOutlineWidth);
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
        /// Resolves skin tone for the requested hero identifier.
        /// </summary>
        public static Color SkinColorForHero(string heroId)
        {
            switch (NormalizeHeroId(heroId))
            {
                case "sean": return SkinSean;
                case "zack": return SkinZack;
                case "dante": return SkinDante;
                case "cheska": return SkinCheska;
                case "nemu": return SkinNemu;
                default: return ArmColour;
            }
        }

        /// <summary>
        /// Normalizes raw character or alias string to canonical hero id.
        /// </summary>
        public static string NormalizeHeroId(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return "classic";
            switch (heroId.ToLowerInvariant())
            {
                case "sean":
                case "kuya_boy":
                case "iggy":
                    return "sean";
                case "zack":
                    return "zack";
                case "dante":
                case "bayan":
                    return "dante";
                case "cheska":
                case "inday":
                    return "cheska";
                case "nemu":
                    return "nemu";
                default:
                    return "classic";
            }
        }

        /// <summary>
        /// Read active hero from character and style arms appropriately.
        /// </summary>
        public void MatchHero(CharacterMotor character)
        {
            EnsureBuilt();
            if (character == null) return;

            string heroId = null;
            if (character.Mode == Core.GameMode.HeroStrike)
            {
                var abilitySystem = character.AbilitySystem;
                if (abilitySystem != null && abilitySystem.Kit != null)
                {
                    heroId = abilitySystem.Kit.HeroId;
                }
                else
                {
                    var heroPeople = Core.Roster.GetPeople(Core.GameMode.HeroStrike);
                    if (character.CharacterIndex >= 0 && character.CharacterIndex < heroPeople.Count)
                        heroId = heroPeople[character.CharacterIndex].Id;
                }
            }

            SetHero(heroId);
        }

        /// <summary>
        /// Customize viewmodel arms with bespoke hero skin tone, sleeves, wristbands/bracers,
        /// and element signatures.
        /// </summary>
        public void SetHero(string heroId)
        {
            EnsureBuilt();
            heroId = NormalizeHeroId(heroId);
            if (_heroInitialized && _currentHeroId == heroId) return;

            _currentHeroId = heroId;
            _heroInitialized = true;

            ApplyHeroStyle(heroId);
        }

        private void ApplyHeroStyle(string heroId)
        {
            ClearAccessories(_rightArm);
            ClearAccessories(_leftArm);

            Color skinColor = SkinColorForHero(heroId);

            if (_rightArmRenderer != null)
            {
                Visual.MaterialKit.Dress(_rightArmRenderer, skinColor);
                Visual.ToonSkin.Apply(_rightArmRenderer, Visual.ToonSkin.PersonOutlineWidth);
            }
            if (_leftArmRenderer != null)
            {
                Visual.MaterialKit.Dress(_leftArmRenderer, skinColor);
                Visual.ToonSkin.Apply(_leftArmRenderer, Visual.ToonSkin.PersonOutlineWidth);
            }

            if (_rightArm != null) BuildArmAccessories(_rightArm, heroId, isRight: true);
            if (_leftArm != null) BuildArmAccessories(_leftArm, heroId, isRight: false);
        }

        private static void ClearAccessories(Transform arm)
        {
            if (arm == null) return;
            for (int i = arm.childCount - 1; i >= 0; i--)
            {
                var child = arm.GetChild(i);
                if (child != null && child.name.StartsWith(AccessoryPrefix))
                {
                    if (Application.isPlaying) Object.Destroy(child.gameObject);
                    else Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void BuildArmAccessories(Transform arm, string heroId, bool isRight)
        {
            switch (heroId)
            {
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
                    BuildNemuAccessories(arm, isRight);
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
            // 1. Rolled crimson athletic sleeves (UiTheme.HeroFire)
            AddCylinderAccessory(arm, "Sleeve", 0.146f, 0.146f, 0.38f, 12,
                new Vector3(0.0f, 0.20f, 0.0f), Quaternion.identity, UI.UiTheme.HeroFire);

            // 2. Rolled sleeve cuff fold with gold trim (UiTheme.Amber)
            AddCylinderAccessory(arm, "SleeveCuff", 0.158f, 0.158f, 0.06f, 12,
                new Vector3(0.0f, 0.40f, 0.0f), Quaternion.identity, UI.UiTheme.HeroFire);
            AddCylinderAccessory(arm, "SleeveGoldTrim", 0.160f, 0.160f, 0.02f, 12,
                new Vector3(0.0f, 0.43f, 0.0f), Quaternion.identity, UI.UiTheme.Amber);

            // 3. Fiery orange athletic wristband / cuff (UiTheme.HeroMagmaCore)
            AddCylinderAccessory(arm, "Wristband", 0.148f, 0.148f, 0.10f, 12,
                new Vector3(0.0f, 0.54f, 0.0f), Quaternion.identity, UI.UiTheme.HeroMagmaCore);
            AddCylinderAccessory(arm, "WristbandFlameBand", 0.152f, 0.152f, 0.03f, 12,
                new Vector3(0.0f, 0.54f, 0.0f), Quaternion.identity, UI.UiTheme.HeroFireBright, emission: 0.45f);

            // 4. Crimson athletic hand/palm wrap
            AddBoxAccessory(arm, "PalmWrap", new Vector3(0.33f, 0.08f, 0.31f),
                new Vector3(0.0f, 0.70f, 0.0f), Quaternion.identity, UI.UiTheme.HeroFire);
        }

        private static void BuildZackAccessories(Transform arm, bool isRight)
        {
            var carbonDark = new Color(0.09f, 0.10f, 0.13f, 1.0f);
            var armorDark = new Color(0.12f, 0.14f, 0.18f, 1.0f);

            // 1. High-tech sports compression sleeve (dark carbon)
            AddCylinderAccessory(arm, "TechSleeve", 0.142f, 0.142f, 0.42f, 12,
                new Vector3(0.0f, 0.22f, 0.0f), Quaternion.identity, carbonDark);

            // 2. Lightning-speed energy panels / racing stripes
            AddBoxAccessory(arm, "LightningStripe", new Vector3(0.04f, 0.38f, 0.02f),
                new Vector3(0.0f, 0.22f, 0.135f), Quaternion.identity, UI.UiTheme.HeroElectric);
            AddBoxAccessory(arm, "TealStripe", new Vector3(0.02f, 0.32f, 0.04f),
                new Vector3(isRight ? 0.135f : -0.135f, 0.20f, 0.0f), Quaternion.identity, UI.UiTheme.HeroIce);

            // 3. High-tech angular bracer with lightning bolt conductor plates
            AddCylinderAccessory(arm, "TechBracer", 0.152f, 0.152f, 0.10f, 12,
                new Vector3(0.0f, 0.56f, 0.0f), Quaternion.identity, armorDark);
            AddBoxAccessory(arm, "LightningConductor", new Vector3(0.06f, 0.08f, 0.025f),
                new Vector3(0.0f, 0.56f, 0.145f), Quaternion.identity, UI.UiTheme.HeroElectricBright, emission: 0.85f);
            AddBoxAccessory(arm, "SideConductor", new Vector3(0.025f, 0.06f, 0.05f),
                new Vector3(isRight ? 0.145f : -0.145f, 0.56f, 0.0f), Quaternion.identity, UI.UiTheme.HeroElectric, emission: 0.70f);

            // 4. Tech grip fingerless glove
            AddBoxAccessory(arm, "TechGrip", new Vector3(0.33f, 0.07f, 0.31f),
                new Vector3(0.0f, 0.70f, 0.0f), Quaternion.identity, carbonDark);
        }

        private static void BuildDanteAccessories(Transform arm, bool isRight)
        {
            var basaltRock = new Color(0.16f, 0.14f, 0.12f, 1.0f);
            var basaltDarkSlab = new Color(0.13f, 0.11f, 0.10f, 1.0f);

            // 1. Heavy basalt rock forearm guard plates
            AddBoxAccessory(arm, "BasaltArmGuard", new Vector3(0.30f, 0.44f, 0.28f),
                new Vector3(0.0f, 0.24f, 0.0f), Quaternion.identity, basaltRock);
            AddBoxAccessory(arm, "BasaltTopSlab", new Vector3(0.24f, 0.36f, 0.04f),
                new Vector3(0.0f, 0.26f, 0.135f), Quaternion.identity, basaltDarkSlab);

            // 2. Basalt jade crust studs and edge trims (UiTheme.HeroEarth)
            AddBoxAccessory(arm, "JadeCrustStud1", new Vector3(0.04f, 0.04f, 0.02f),
                new Vector3(-0.11f, 0.40f, 0.14f), Quaternion.identity, UI.UiTheme.HeroEarth);
            AddBoxAccessory(arm, "JadeCrustStud2", new Vector3(0.04f, 0.04f, 0.02f),
                new Vector3(0.11f, 0.40f, 0.14f), Quaternion.identity, UI.UiTheme.HeroEarth);

            // 3. Molten glowing magma fissure veins (UiTheme.HeroMagmaCore)
            AddBoxAccessory(arm, "MagmaVeinMain", new Vector3(0.04f, 0.32f, 0.02f),
                new Vector3(0.0f, 0.26f, 0.145f), Quaternion.identity, UI.UiTheme.HeroMagmaCore, emission: 1.20f);
            AddBoxAccessory(arm, "MagmaVeinBranch", new Vector3(0.10f, 0.03f, 0.02f),
                new Vector3(0.05f, 0.34f, 0.145f), Quaternion.identity, UI.UiTheme.HeroMagmaCore, emission: 1.20f);

            // 4. Heavy stone wrist guard with magma core
            AddCylinderAccessory(arm, "BasaltWristRing", 0.158f, 0.158f, 0.11f, 10,
                new Vector3(0.0f, 0.56f, 0.0f), Quaternion.identity, basaltRock);
            AddCylinderAccessory(arm, "MagmaWristCore", 0.160f, 0.160f, 0.03f, 10,
                new Vector3(0.0f, 0.56f, 0.0f), Quaternion.identity, UI.UiTheme.HeroMagmaCore, emission: 1.10f);

            // 5. Volcanic rock knuckle carapace
            AddBoxAccessory(arm, "RockKnuckles", new Vector3(0.33f, 0.07f, 0.31f),
                new Vector3(0.0f, 0.72f, 0.0f), Quaternion.identity, basaltRock);
        }

        private static void BuildCheskaAccessories(Transform arm, bool isRight)
        {
            var deepGlacier = new Color(0.13f, 0.29f, 0.36f, 1.0f);
            var frostWhite = new Color(0.95f, 0.98f, 1.00f, 1.0f);

            // 1. Pastel cyan frost-blue winter coat sleeve
            AddCylinderAccessory(arm, "FrostSleeve", 0.145f, 0.145f, 0.38f, 12,
                new Vector3(0.0f, 0.20f, 0.0f), Quaternion.identity, UI.UiTheme.HeroIce);
            AddBoxAccessory(arm, "GlacierUnderPanel", new Vector3(0.22f, 0.36f, 0.02f),
                new Vector3(0.0f, 0.20f, -0.13f), Quaternion.identity, deepGlacier);

            // 2. Insulated soft fluffy frost-white cuff trim
            AddCylinderAccessory(arm, "FluffyWhiteCuff", 0.162f, 0.162f, 0.08f, 12,
                new Vector3(0.0f, 0.41f, 0.0f), Quaternion.identity, frostWhite);

            // 3. Crystalline ice bracer with delicate snowflake/crystal trim
            AddCylinderAccessory(arm, "IceBracer", 0.150f, 0.150f, 0.09f, 12,
                new Vector3(0.0f, 0.54f, 0.0f), Quaternion.identity, UI.UiTheme.HeroIce);
            AddBoxAccessory(arm, "SnowflakeCrystal", new Vector3(0.08f, 0.08f, 0.02f),
                new Vector3(0.0f, 0.54f, 0.142f), Quaternion.Euler(0, 0, 45.0f), UI.UiTheme.HeroIceBright, emission: 0.50f);
            AddBoxAccessory(arm, "SideCrystal", new Vector3(0.02f, 0.06f, 0.06f),
                new Vector3(isRight ? 0.142f : -0.142f, 0.54f, 0.0f), Quaternion.Euler(45.0f, 0, 0), UI.UiTheme.HeroIceBright, emission: 0.50f);

            // 4. Deep glacier fingerless frost winter glove
            AddBoxAccessory(arm, "FrostGlove", new Vector3(0.33f, 0.11f, 0.31f),
                new Vector3(0.0f, 0.68f, 0.0f), Quaternion.identity, deepGlacier);
        }

        private static void BuildNemuAccessories(Transform arm, bool isRight)
        {
            var voidPurple = new Color(0.17f, 0.07f, 0.26f, 1.0f);
            var voidDarkBand = new Color(0.10f, 0.05f, 0.16f, 1.0f);

            // 1. Loose dark-purple ghostly spirit wraps / sleeves
            AddCylinderAccessory(arm, "SpiritSleeve", 0.144f, 0.144f, 0.42f, 12,
                new Vector3(0.0f, 0.22f, 0.0f), Quaternion.identity, voidPurple);
            AddBoxAccessory(arm, "SpiritWrapStripe", new Vector3(0.18f, 0.04f, 0.02f),
                new Vector3(0.0f, 0.26f, 0.138f), Quaternion.identity, UI.UiTheme.HeroSpirit);

            // 2. Flowing ethereal spirit ribbons / wisps along the forearm
            AddBoxAccessory(arm, "SpiritRibbonOuter", new Vector3(0.02f, 0.36f, 0.08f),
                new Vector3(isRight ? 0.140f : -0.140f, 0.28f, 0.0f), Quaternion.identity, UI.UiTheme.HeroSpiritBright, emission: 0.65f);
            AddBoxAccessory(arm, "SpiritRibbonInner", new Vector3(0.02f, 0.28f, 0.06f),
                new Vector3(isRight ? -0.140f : 0.140f, 0.20f, 0.0f), Quaternion.identity, UI.UiTheme.HeroSpirit, emission: 0.65f);

            // 3. Void energy wrist cuff with glowing ethereal runes
            AddCylinderAccessory(arm, "VoidWristCuff", 0.150f, 0.150f, 0.10f, 12,
                new Vector3(0.0f, 0.55f, 0.0f), Quaternion.identity, voidDarkBand);
            AddBoxAccessory(arm, "SpectralRune", new Vector3(0.06f, 0.06f, 0.02f),
                new Vector3(0.0f, 0.55f, 0.142f), Quaternion.Euler(0, 0, 45.0f), UI.UiTheme.HeroSpiritBright, emission: 0.95f);

            // 4. Spectral palm & finger wraps
            AddBoxAccessory(arm, "SpectralPalmWrap", new Vector3(0.33f, 0.08f, 0.31f),
                new Vector3(0.0f, 0.70f, 0.0f), Quaternion.identity, voidPurple);
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
                if (toon) Visual.ToonSkin.Apply(mr, Visual.ToonSkin.PersonOutlineWidth);
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
        private void LateUpdate()
        {
            _phase += Time.deltaTime;

            // § THE ACTION CLIPS, stepped before the pose below so a throw reads over whatever
            // the pivot is doing rather than under it.
            StepAction(Time.deltaTime);

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
                StepToward(_rightRestPos,
                           _rightRest * Quaternion.Euler(-t * IdleRightSwing * Mathf.Rad2Deg, 0.0f,
                                                         t * 0.02f * Mathf.Rad2Deg),
                           _rightRestScale);
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

            StepToward(elbow, Quaternion.LookRotation(forward, dir), Vector3.one * CarryScale);
        }

        private void StepToward(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (_rightPivot == null) return;
            float k = Mathf.Clamp01(ReachSpeed * Time.deltaTime);

            _rightPivot.localPosition = Vector3.Lerp(_rightPivot.localPosition, position, k);
            _rightPivot.localRotation = Quaternion.Slerp(_rightPivot.localRotation, rotation, k);
            _rightPivot.localScale = Vector3.Lerp(_rightPivot.localScale, scale, k);
        }
    }
}
