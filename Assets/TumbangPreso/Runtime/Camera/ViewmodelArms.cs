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
        // § CHARACTER SKIN TONES (Transcribed from 3D TPP Roster Palettes)
        // -------------------------------------------------------------------
        // Heroes
        public static readonly Color SkinSean = new Color(0.722f, 0.455f, 0.251f, 1.0f);     // Golden brown tan
        public static readonly Color SkinZack = new Color(0.780f, 0.478f, 0.271f, 1.0f);     // Warm medium caramel
        public static readonly Color SkinDante = new Color(0.659f, 0.376f, 0.173f, 1.0f);    // SKIN #a8602c
        public static readonly Color SkinCheska = new Color(0.961f, 0.722f, 0.580f, 1.0f);   // Fair porcelain skin
        public static readonly Color SkinNemu = new Color(0.878f, 0.686f, 0.518f, 1.0f);     // Pale lavender / ghostly ethereal

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

        private ViewmodelClothPhysics _rightClothPhysics;
        private ViewmodelClothPhysics _leftClothPhysics;
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

            if (_rightClothPhysics != null)
            {
                if (clip == "throw")
                {
                    _rightClothPhysics.AddImpulse(new Vector3(0.15f, 0.40f, -0.65f));
                    _rightClothPhysics.AddAngularImpulse(new Vector3(-45.0f, 15.0f, -30.0f));
                }
                else if (clip == "slam" || clip == "supernova-slam" || clip == "fissure-slam")
                {
                    _rightClothPhysics.AddImpulse(new Vector3(0.0f, -0.75f, -0.40f));
                    _rightClothPhysics.AddAngularImpulse(new Vector3(55.0f, 0.0f, 0.0f));
                }
                else if (clip == "ghost-step" || clip == "sprint-electric")
                {
                    _rightClothPhysics.AddImpulse(new Vector3(-0.35f, 0.20f, -0.45f));
                    _rightClothPhysics.AddAngularImpulse(new Vector3(-25.0f, 35.0f, -40.0f));
                }
                else if (clip == "project-spirit" || clip == "nova-burst")
                {
                    _rightClothPhysics.AddImpulse(new Vector3(0.10f, 0.35f, 0.50f));
                    _rightClothPhysics.AddAngularImpulse(new Vector3(30.0f, -20.0f, 25.0f));
                }
                else if (clip == "seance-channel" || clip == "ignite" || clip == "overcharge")
                {
                    _rightClothPhysics.AddImpulse(new Vector3(0.0f, 0.25f, 0.0f));
                    _rightClothPhysics.AddAngularImpulse(new Vector3(20.0f, 25.0f, -20.0f));
                }
            }

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
            _rightClothPhysics = null;
            _leftClothPhysics = null;

            ClearAccessories(_rightArm);
            ClearAccessories(_leftArm);

            Vector3 armScale = characterId == "sean"
                ? new Vector3(1.18f, 1.0f, 1.18f)
                : Vector3.one;

            if (_rightArm != null) _rightArm.localScale = armScale;
            if (_leftArm != null) _leftArm.localScale = armScale;

            Color skinColor = SkinColorForCharacter(characterId);

            bool isNemu = characterId == "nemu";

            if (_rightArmRenderer != null)
            {
                _rightArmRenderer.enabled = !isNemu;
                if (!isNemu)
                {
                    Visual.MaterialKit.Dress(_rightArmRenderer, skinColor);
                    Visual.ToonSkin.Apply(_rightArmRenderer, Visual.ToonSkin.PersonOutlineWidth);
                }
            }
            if (_leftArmRenderer != null)
            {
                _leftArmRenderer.enabled = !isNemu;
                if (!isNemu)
                {
                    Visual.MaterialKit.Dress(_leftArmRenderer, skinColor);
                    Visual.ToonSkin.Apply(_leftArmRenderer, Visual.ToonSkin.PersonOutlineWidth);
                }
            }

            if (_rightArm != null) BuildArmAccessories(_rightArm, characterId, isRight: true, parent: this);
            if (_leftArm != null) BuildArmAccessories(_leftArm, characterId, isRight: false, parent: this);
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
            var skinHighlight = new Color(0.796f, 0.525f, 0.306f, 1.0f);
            var skinShadow = new Color(0.588f, 0.349f, 0.180f, 1.0f);
            var vestRed = new Color(0.788f, 0.165f, 0.165f, 1.0f);
            var gold = new Color(0.941f, 0.647f, 0.000f, 1.0f);
            var goldShadow = new Color(0.722f, 0.478f, 0.000f, 1.0f);

            // ⚠️ Sean is sleeveless in build_iggy_voxel.py. The old viewmodel invented a red
            // shoulder sleeve and hand wraps. His authored silhouette is bare muscle followed
            // by a red and gold combat bracer, then an uncovered fist.
            AddBoxAccessory(arm, "Deltoid", new Vector3(0.34f, 0.22f, 0.34f),
                new Vector3(0.0f, 0.12f, 0.0f), Quaternion.identity, skinHighlight);
            AddBoxAccessory(arm, "Bicep", new Vector3(0.31f, 0.22f, 0.34f),
                new Vector3(0.0f, 0.31f, 0.02f), Quaternion.identity, SkinSean);
            AddBoxAccessory(arm, "BicepCrease", new Vector3(0.26f, 0.025f, 0.025f),
                new Vector3(0.0f, 0.40f, 0.16f), Quaternion.identity, skinShadow);
            AddBoxAccessory(arm, "BracerGoldInner", new Vector3(0.33f, 0.045f, 0.31f),
                new Vector3(0.0f, 0.45f, 0.0f), Quaternion.identity, gold);
            AddBoxAccessory(arm, "BracerBody", new Vector3(0.34f, 0.18f, 0.32f),
                new Vector3(0.0f, 0.56f, 0.0f), Quaternion.identity, vestRed);
            AddBoxAccessory(arm, "BracerPlate", new Vector3(0.19f, 0.12f, 0.025f),
                new Vector3(0.0f, 0.56f, 0.17f), Quaternion.identity, gold);
            AddBoxAccessory(arm, "BracerPlateShadow", new Vector3(0.09f, 0.07f, 0.028f),
                new Vector3(0.0f, 0.56f, 0.185f), Quaternion.identity, goldShadow);
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
            if (!isRight)
            {
                AddBoxAccessory(arm, "ElectricWristband", new Vector3(0.31f, 0.085f, 0.30f),
                    new Vector3(0.0f, 0.505f, 0.0f), Quaternion.identity, crest);
            }
        }

        private static void BuildDanteAccessories(Transform arm, bool isRight)
        {
            var robeGreen = new Color(0.239f, 0.388f, 0.208f, 1.0f);
            var robeDark = new Color(0.141f, 0.243f, 0.122f, 1.0f);
            var leather = new Color(0.282f, 0.184f, 0.114f, 1.0f);
            var gold = new Color(0.875f, 0.698f, 0.282f, 1.0f);

            // Dante is asymmetric. His left arm carries the broad demonic chevrons.
            // His right arm has a brown sleeve, gold cuff and a slimmer forest-green vine.
            if (isRight)
            {
                AddBoxAccessory(arm, "BrownRightSleeve", new Vector3(0.30f, 0.31f, 0.29f),
                    new Vector3(0.0f, 0.155f, 0.0f), Quaternion.identity, leather);
                AddBoxAccessory(arm, "BrownSleeveShadow", new Vector3(0.30f, 0.04f, 0.025f),
                    new Vector3(0.0f, 0.29f, 0.155f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "GoldRightCuff", new Vector3(0.32f, 0.09f, 0.31f),
                    new Vector3(0.0f, 0.355f, 0.0f), Quaternion.identity, gold);
                AddBoxAccessory(arm, "NaturalVineStem", new Vector3(0.030f, 0.105f, 0.010f),
                    new Vector3(0.075f, 0.475f, 0.146f), Quaternion.identity, robeGreen);
                AddBoxAccessory(arm, "NaturalVineSweep", new Vector3(0.030f, 0.115f, 0.010f),
                    new Vector3(0.035f, 0.535f, 0.146f), Quaternion.Euler(0, 0, 25.0f), robeGreen);
                AddBoxAccessory(arm, "NaturalVineHook", new Vector3(0.125f, 0.030f, 0.010f),
                    new Vector3(-0.035f, 0.595f, 0.146f), Quaternion.identity, robeGreen);
            }
            else
            {
                AddBoxAccessory(arm, "DemonicCarapaceShadow", new Vector3(0.26f, 0.030f, 0.010f),
                    new Vector3(0.0f, 0.145f, 0.146f), Quaternion.identity, robeDark);
                AddBoxAccessory(arm, "DemonicChevronA", new Vector3(0.035f, 0.185f, 0.010f),
                    new Vector3(-0.040f, 0.275f, 0.146f), Quaternion.Euler(0, 0, 40.0f), robeGreen);
                AddBoxAccessory(arm, "DemonicChevronALeg", new Vector3(0.035f, 0.100f, 0.010f),
                    new Vector3(0.038f, 0.365f, 0.146f), Quaternion.identity, robeGreen);
                AddBoxAccessory(arm, "DemonicChevronB", new Vector3(0.035f, 0.185f, 0.010f),
                    new Vector3(-0.025f, 0.445f, 0.146f), Quaternion.Euler(0, 0, 40.0f), robeGreen);
                AddBoxAccessory(arm, "DemonicChevronBLeg", new Vector3(0.035f, 0.100f, 0.010f),
                    new Vector3(0.055f, 0.535f, 0.146f), Quaternion.identity, robeGreen);
                AddBoxAccessory(arm, "DemonicOuterCrest", new Vector3(0.035f, 0.105f, 0.010f),
                    new Vector3(0.105f, 0.615f, 0.146f), Quaternion.identity, robeGreen);
            }
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
            var skinDark = new Color(0.839f, 0.600f, 0.455f, 1.0f);     // SKIN_DARK #d69974

            // 1. Oversized Stepped Boxy Streetwear Hoodie Sleeve Outer Shell
            var sleeveMesh = CreateHoodieDrapedSleeveMesh(isRight);
            var sleeveGo = new GameObject(AccessoryPrefix + "HoodieSleeve");
            sleeveGo.transform.SetParent(arm, false);
            sleeveGo.transform.localPosition = Vector3.zero;
            sleeveGo.transform.localRotation = Quaternion.identity;
            sleeveGo.transform.localScale = Vector3.one;

            var mf = sleeveGo.AddComponent<MeshFilter>();
            mf.sharedMesh = sleeveMesh;
            var mr = sleeveGo.AddComponent<MeshRenderer>();
            Visual.MaterialKit.Dress(mr, hoodieDark);
            Visual.ToonSkin.Apply(mr, Visual.ToonSkin.PersonOutlineWidth);

            // Bind dynamic cloth physics solver to sleeve
            var clothPhys = sleeveGo.AddComponent<ViewmodelClothPhysics>();
            clothPhys.BindMesh(mf, isRight, weightBoost: 1.0f);
            if (parent != null)
            {
                if (isRight) parent._rightClothPhysics = clothPhys;
                else parent._leftClothPhysics = clothPhys;
            }

            // 2. Hollow Interior Sleeve Cavity Lining
            var innerMesh = CreateHoodieInnerLiningMesh(isRight);
            var innerGo = new GameObject(AccessoryPrefix + "HoodieInnerLining");
            innerGo.transform.SetParent(arm, false);
            innerGo.transform.localPosition = Vector3.zero;
            innerGo.transform.localRotation = Quaternion.identity;
            innerGo.transform.localScale = Vector3.one;

            var innerMf = innerGo.AddComponent<MeshFilter>();
            innerMf.sharedMesh = innerMesh;
            var innerMr = innerGo.AddComponent<MeshRenderer>();
            Visual.MaterialKit.Dress(innerMr, hoodieShadow);
            Visual.ToonSkin.Apply(innerMr, Visual.ToonSkin.PersonOutlineWidth);

            // 3. Crisp Lavender Flared Cuff Border Band
            var cuffMesh = CreateHoodieCuffRimMesh(isRight);
            var cuffGo = new GameObject(AccessoryPrefix + "HoodieCuffRim");
            cuffGo.transform.SetParent(arm, false);
            cuffGo.transform.localPosition = Vector3.zero;
            cuffGo.transform.localRotation = Quaternion.identity;
            cuffGo.transform.localScale = Vector3.one;

            var cuffMf = cuffGo.AddComponent<MeshFilter>();
            cuffMf.sharedMesh = cuffMesh;
            var cuffMr = cuffGo.AddComponent<MeshRenderer>();
            Visual.MaterialKit.Dress(cuffMr, lavenderTrim);
            Visual.ToonSkin.Apply(cuffMr, Visual.ToonSkin.PersonOutlineWidth);

            // 4. Cute Tucked Hand in exact Nemu Skin Tone (#e0af84 and #d69974)
            var handGo = new GameObject(AccessoryPrefix + "SpiritHand");
            handGo.transform.SetParent(arm, false);
            handGo.transform.localPosition = Vector3.zero;
            handGo.transform.localRotation = Quaternion.identity;
            handGo.transform.localScale = Vector3.one;

            // Perfectly proportioned palm, thumb, fingers, and shaded tips wrapping the held slipper at Y ~ 0.70-0.82
            AddBoxAccessory(handGo.transform, "Palm", new Vector3(0.082f, 0.085f, 0.038f),
                new Vector3(0.0f, 0.72f, 0.0f), Quaternion.identity, skinTone);
            AddBoxAccessory(handGo.transform, "Thumb", new Vector3(0.028f, 0.055f, 0.026f),
                new Vector3(isRight ? -0.046f : 0.046f, 0.75f, 0.015f), Quaternion.Euler(0, 0, isRight ? 24f : -24f), skinTone);
            AddBoxAccessory(handGo.transform, "Fingers", new Vector3(0.076f, 0.075f, 0.032f),
                new Vector3(0.0f, 0.78f, -0.005f), Quaternion.identity, skinTone);
            AddBoxAccessory(handGo.transform, "FingertipShade", new Vector3(0.072f, 0.032f, 0.024f),
                new Vector3(0.0f, 0.82f, 0.010f), Quaternion.Euler(-14f, 0, 0), skinDark);
        }

        private static Vector3 RoundedBoxOffset(float angle, float rx, float rz, float power = 0.5f)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float x = Mathf.Sign(cos) * Mathf.Pow(Mathf.Abs(cos), power) * rx;
            float z = Mathf.Sign(sin) * Mathf.Pow(Mathf.Abs(sin), power) * rz;
            return new Vector3(x, 0.0f, z);
        }

        private static Mesh CreateHoodieDrapedSleeveMesh(bool isRight)
        {
            var mesh = new Mesh { name = "Nemu_HoodieSleeve" };
            const int radialSegments = 24;
            int ringCount = 8;

            // Stepped streetwear hoodie progression (shoulder -> mid-sleeve drop -> flared cuff)
            float[] ySteps       = { 0.04f, 0.20f, 0.22f, 0.42f, 0.44f, 0.58f, 0.67f, 0.70f };
            float[] rxSteps      = { 0.100f, 0.115f, 0.135f, 0.150f, 0.170f, 0.188f, 0.198f, 0.202f };
            float[] rzSteps      = { 0.080f, 0.090f, 0.105f, 0.115f, 0.130f, 0.142f, 0.152f, 0.155f };
            float[] drapeOffsetZ = { 0.000f, -0.005f, -0.012f, -0.020f, -0.028f, -0.036f, -0.042f, -0.045f };

            var vertices = new Vector3[ringCount * radialSegments];
            var normals = new Vector3[ringCount * radialSegments];
            var uvs = new Vector2[ringCount * radialSegments];

            for (int ring = 0; ring < ringCount; ring++)
            {
                float y = ySteps[ring];
                float rx = rxSteps[ring];
                float rz = rzSteps[ring];
                float dz = drapeOffsetZ[ring];
                float ringProgress = (float)ring / (ringCount - 1);

                for (int i = 0; i < radialSegments; i++)
                {
                    float angle = (float)i / radialSegments * Mathf.PI * 2.0f;
                    Vector3 boxPt = RoundedBoxOffset(angle, rx, rz, 0.50f);

                    int idx = ring * radialSegments + i;
                    vertices[idx] = new Vector3(boxPt.x, y, boxPt.z + dz);
                    normals[idx] = new Vector3(Mathf.Cos(angle), 0.10f, Mathf.Sin(angle)).normalized;
                    uvs[idx] = new Vector2((float)i / radialSegments, ringProgress);
                }
            }

            int triCount = (ringCount - 1) * radialSegments * 6;
            var triangles = new int[triCount];
            int t = 0;

            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                for (int i = 0; i < radialSegments; i++)
                {
                    int next = (i + 1) % radialSegments;
                    int b1 = ring * radialSegments + i;
                    int b2 = ring * radialSegments + next;
                    int t1 = (ring + 1) * radialSegments + i;
                    int t2 = (ring + 1) * radialSegments + next;

                    triangles[t++] = b1;
                    triangles[t++] = t1;
                    triangles[t++] = b2;

                    triangles[t++] = b2;
                    triangles[t++] = t1;
                    triangles[t++] = t2;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateHoodieInnerLiningMesh(bool isRight)
        {
            var mesh = new Mesh { name = "Nemu_HoodieInnerLining" };
            const int radialSegments = 24;
            int ringCount = 3;

            float[] ySteps       = { 0.69f, 0.58f, 0.46f };
            float[] rxSteps      = { 0.192f, 0.175f, 0.150f };
            float[] rzSteps      = { 0.146f, 0.130f, 0.112f };
            float[] drapeOffsetZ = { -0.043f, -0.035f, -0.026f };

            var vertices = new Vector3[ringCount * radialSegments];
            var normals = new Vector3[ringCount * radialSegments];
            var uvs = new Vector2[ringCount * radialSegments];

            for (int ring = 0; ring < ringCount; ring++)
            {
                float y = ySteps[ring];
                float rx = rxSteps[ring];
                float rz = rzSteps[ring];
                float dz = drapeOffsetZ[ring];

                for (int i = 0; i < radialSegments; i++)
                {
                    float angle = (float)i / radialSegments * Mathf.PI * 2.0f;
                    Vector3 boxPt = RoundedBoxOffset(angle, rx, rz, 0.50f);

                    int idx = ring * radialSegments + i;
                    vertices[idx] = new Vector3(boxPt.x, y, boxPt.z + dz);
                    normals[idx] = -new Vector3(Mathf.Cos(angle), -0.10f, Mathf.Sin(angle)).normalized;
                    uvs[idx] = new Vector2((float)i / radialSegments, (float)ring / (ringCount - 1));
                }
            }

            int triCount = (ringCount - 1) * radialSegments * 6;
            var triangles = new int[triCount];
            int t = 0;

            // Inverted winding so normals face inside the sleeve cavity
            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                for (int i = 0; i < radialSegments; i++)
                {
                    int next = (i + 1) % radialSegments;
                    int b1 = ring * radialSegments + i;
                    int b2 = ring * radialSegments + next;
                    int t1 = (ring + 1) * radialSegments + i;
                    int t2 = (ring + 1) * radialSegments + next;

                    triangles[t++] = b1;
                    triangles[t++] = b2;
                    triangles[t++] = t1;

                    triangles[t++] = b2;
                    triangles[t++] = t2;
                    triangles[t++] = t1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateHoodieCuffRimMesh(bool isRight)
        {
            var mesh = new Mesh { name = "Nemu_HoodieCuffRim" };
            const int radialSegments = 24;
            int ringCount = 2;

            float[] ySteps       = { 0.67f, 0.71f };
            float[] rxSteps      = { 0.201f, 0.205f };
            float[] rzSteps      = { 0.155f, 0.159f };
            float[] drapeOffsetZ = { -0.042f, -0.045f };

            var vertices = new Vector3[ringCount * radialSegments];
            var normals = new Vector3[ringCount * radialSegments];
            var uvs = new Vector2[ringCount * radialSegments];

            for (int ring = 0; ring < ringCount; ring++)
            {
                float y = ySteps[ring];
                float rx = rxSteps[ring];
                float rz = rzSteps[ring];
                float dz = drapeOffsetZ[ring];

                for (int i = 0; i < radialSegments; i++)
                {
                    float angle = (float)i / radialSegments * Mathf.PI * 2.0f;
                    Vector3 boxPt = RoundedBoxOffset(angle, rx, rz, 0.50f);

                    int idx = ring * radialSegments + i;
                    vertices[idx] = new Vector3(boxPt.x, y, boxPt.z + dz);
                    normals[idx] = new Vector3(Mathf.Cos(angle), 0.10f, Mathf.Sin(angle)).normalized;
                    uvs[idx] = new Vector2((float)i / radialSegments, (float)ring);
                }
            }

            int triCount = radialSegments * 6;
            var triangles = new int[triCount];
            int t = 0;

            for (int i = 0; i < radialSegments; i++)
            {
                int next = (i + 1) % radialSegments;
                int b1 = i;
                int b2 = next;
                int t1 = radialSegments + i;
                int t2 = radialSegments + next;

                triangles[t++] = b1;
                triangles[t++] = t1;
                triangles[t++] = b2;

                triangles[t++] = b2;
                triangles[t++] = t1;
                triangles[t++] = t2;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        // -------------------------------------------------------------------
        // § CLASSIC ROSTER BESPOKE ACCESSORY BUILDERS
        // -------------------------------------------------------------------

        private static void BuildBayanAccessories(Transform arm, bool isRight)
        {
            var greenShirt = new Color(0.247f, 0.561f, 0.361f, 1.0f);
            var greenFold = new Color(0.20f, 0.46f, 0.30f, 1.0f);
            var wristLeather = new Color(0.447f, 0.271f, 0.173f, 1.0f);

            // 1. Forest green rolled t-shirt sleeve
            AddCylinderAccessory(arm, "GreenSleeve", 0.145f, 0.145f, 0.26f, 12,
                new Vector3(0.0f, 0.14f, 0.0f), Quaternion.identity, greenShirt);
            AddCylinderAccessory(arm, "GreenSleeveFold", 0.152f, 0.152f, 0.05f, 12,
                new Vector3(0.0f, 0.26f, 0.0f), Quaternion.identity, greenFold);

            // 2. Leather athletic wristband on right wrist
            if (isRight)
            {
                AddCylinderAccessory(arm, "LeatherWristband", 0.146f, 0.146f, 0.08f, 12,
                    new Vector3(0.0f, 0.55f, 0.0f), Quaternion.identity, wristLeather);
            }
        }

        private static void BuildMaringAccessories(Transform arm, bool isRight)
        {
            var blouse = new Color(0.192f, 0.141f, 0.114f, 1.0f);
            var maroon = new Color(0.541f, 0.204f, 0.275f, 1.0f);
            var maroonDark = new Color(0.420f, 0.145f, 0.210f, 1.0f);

            // Maring has dark short sleeves on both arms. Only her left forearm has
            // the broad maroon wrap visible on character-female-f.glb.
            AddBoxAccessory(arm, "DarkBlouseSleeve", new Vector3(0.30f, 0.31f, 0.30f),
                new Vector3(0.0f, 0.155f, 0.0f), Quaternion.identity, blouse);
            AddBoxAccessory(arm, "BlouseSleeveHem", new Vector3(0.32f, 0.035f, 0.32f),
                new Vector3(0.0f, 0.315f, 0.0f), Quaternion.identity, maroonDark);

            if (!isRight)
            {
                AddBoxAccessory(arm, "MaroonForearmWrap", new Vector3(0.32f, 0.16f, 0.32f),
                    new Vector3(0.0f, 0.515f, 0.0f), Quaternion.identity, maroon);
                AddBoxAccessory(arm, "MaroonWrapInset", new Vector3(0.19f, 0.075f, 0.018f),
                    new Vector3(0.0f, 0.515f, 0.169f), Quaternion.identity, maroonDark);
            }
        }

        private static void BuildTotoyAccessories(Transform arm, bool isRight)
        {
            var darkGreenShirt = new Color(0.184f, 0.490f, 0.310f, 1.0f);
            var greySweatband = new Color(0.525f, 0.545f, 0.631f, 1.0f);

            // 1. Dark green athletic t-shirt sleeve
            AddCylinderAccessory(arm, "GreenSleeve", 0.144f, 0.144f, 0.26f, 12,
                new Vector3(0.0f, 0.14f, 0.0f), Quaternion.identity, darkGreenShirt);

            // 2. Grey athletic sweatband on right wrist
            if (isRight)
            {
                AddCylinderAccessory(arm, "Sweatband", 0.148f, 0.148f, 0.09f, 12,
                    new Vector3(0.0f, 0.54f, 0.0f), Quaternion.identity, greySweatband);
            }
        }

        private static void BuildIndayAccessories(Transform arm, bool isRight)
        {
            var yellow = new Color(0.878f, 0.706f, 0.235f, 1.0f);
            var coral = new Color(0.761f, 0.329f, 0.247f, 1.0f);
            var coralDark = new Color(0.639f, 0.255f, 0.220f, 1.0f);
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
            AddBoxAccessory(arm, "CoralGuardFace", new Vector3(0.19f, 0.11f, 0.024f),
                new Vector3(0.0f, 0.485f, 0.182f), Quaternion.identity, coralDark);
        }

        private static void BuildKuyaBoyAccessories(Transform arm, bool isRight)
        {
            var navyShirt = new Color(0.165f, 0.290f, 0.478f, 1.0f);
            var navyFold = new Color(0.12f, 0.22f, 0.38f, 1.0f);
            var darkWatch = new Color(0.192f, 0.141f, 0.114f, 1.0f);

            // 1. Navy blue rolled work shirt sleeve
            AddCylinderAccessory(arm, "NavySleeve", 0.146f, 0.146f, 0.28f, 12,
                new Vector3(0.0f, 0.15f, 0.0f), Quaternion.identity, navyShirt);
            AddCylinderAccessory(arm, "NavySleeveFold", 0.154f, 0.154f, 0.06f, 12,
                new Vector3(0.0f, 0.28f, 0.0f), Quaternion.identity, navyFold);

            // 2. Sturdy dark utility watch on left wrist
            if (!isRight)
            {
                AddCylinderAccessory(arm, "UtilityWatch", 0.148f, 0.148f, 0.07f, 12,
                    new Vector3(0.0f, 0.55f, 0.0f), Quaternion.identity, darkWatch);
            }
        }

        private static void BuildAteGirlieAccessories(Transform arm, bool isRight)
        {
            var magentaTop = new Color(0.851f, 0.310f, 0.416f, 1.0f);
            var silverTone = new Color(0.88f, 0.88f, 0.92f, 1.0f);

            // 1. Magenta/pink top short sleeve
            AddCylinderAccessory(arm, "PinkSleeve", 0.144f, 0.144f, 0.22f, 12,
                new Vector3(0.0f, 0.12f, 0.0f), Quaternion.identity, magentaTop);

            // 2. Slender wristband on left wrist
            if (!isRight)
            {
                AddCylinderAccessory(arm, "PinkWristband", 0.145f, 0.145f, 0.04f, 12,
                    new Vector3(0.0f, 0.54f, 0.0f), Quaternion.identity, magentaTop);
                AddBoxAccessory(arm, "SilverRing", new Vector3(0.03f, 0.03f, 0.015f),
                    new Vector3(0.0f, 0.54f, 0.143f), Quaternion.identity, silverTone);
            }
        }

        private static void BuildTikboyAccessories(Transform arm, bool isRight)
        {
            var oliveShirt = new Color(0.416f, 0.620f, 0.290f, 1.0f);
            var digitalWatch = new Color(0.220f, 0.220f, 0.239f, 1.0f);
            var watchScreen = new Color(0.12f, 0.14f, 0.12f, 1.0f);

            // 1. Olive green streetwear sleeve
            AddCylinderAccessory(arm, "OliveSleeve", 0.144f, 0.144f, 0.28f, 12,
                new Vector3(0.0f, 0.15f, 0.0f), Quaternion.identity, oliveShirt);

            // 2. Digital sports watch on left wrist
            if (!isRight)
            {
                AddCylinderAccessory(arm, "DigitalStrap", 0.147f, 0.147f, 0.06f, 12,
                    new Vector3(0.0f, 0.55f, 0.0f), Quaternion.identity, digitalWatch);
                AddBoxAccessory(arm, "DigitalFace", new Vector3(0.045f, 0.045f, 0.02f),
                    new Vector3(0.0f, 0.55f, 0.144f), Quaternion.identity, watchScreen);
            }
        }

        private static void BuildBebangAccessories(Transform arm, bool isRight)
        {
            var burgundyBlouse = new Color(0.541f, 0.227f, 0.227f, 1.0f);

            // 1. Burgundy blouse sleeve
            AddCylinderAccessory(arm, "BurgundySleeve", 0.144f, 0.144f, 0.24f, 12,
                new Vector3(0.0f, 0.13f, 0.0f), Quaternion.identity, burgundyBlouse);

            // 2. Burgundy wristband on right wrist
            if (isRight)
            {
                AddCylinderAccessory(arm, "BurgundyWristband", 0.146f, 0.146f, 0.06f, 12,
                    new Vector3(0.0f, 0.54f, 0.0f), Quaternion.identity, burgundyBlouse);
            }
        }

        private static void BuildJunJunAccessories(Transform arm, bool isRight)
        {
            var blueShirt = new Color(0.133f, 0.157f, 0.227f, 1.0f);
            var whiteTrim = new Color(1.000f, 1.000f, 1.000f, 1.0f);
            var redFriendship = new Color(0.85f, 0.18f, 0.18f, 1.0f);

            // 1. Dark blue shirt sleeve with white cuff band
            AddCylinderAccessory(arm, "BlueSleeve", 0.144f, 0.144f, 0.26f, 12,
                new Vector3(0.0f, 0.14f, 0.0f), Quaternion.identity, blueShirt);
            AddCylinderAccessory(arm, "WhiteCuffBand", 0.148f, 0.148f, 0.04f, 12,
                new Vector3(0.0f, 0.26f, 0.0f), Quaternion.identity, whiteTrim);

            // 2. Braided friendship bracelet on right wrist
            if (isRight)
            {
                AddCylinderAccessory(arm, "FriendshipBand", 0.144f, 0.144f, 0.035f, 12,
                    new Vector3(0.0f, 0.54f, 0.0f), Quaternion.identity, redFriendship);
            }
        }

        private static void BuildLolaPacingAccessories(Transform arm, bool isRight)
        {
            var greyBaro = new Color(0.541f, 0.478f, 0.416f, 1.0f);
            var whiteLace = new Color(0.96f, 0.96f, 0.98f, 1.0f);
            var goldBangle = new Color(0.88f, 0.72f, 0.24f, 1.0f);

            // 1. Traditional baro sleeve with delicate lace trim
            AddCylinderAccessory(arm, "BaroSleeve", 0.146f, 0.146f, 0.32f, 12,
                new Vector3(0.0f, 0.17f, 0.0f), Quaternion.identity, greyBaro);
            AddCylinderAccessory(arm, "LaceTrim", 0.152f, 0.152f, 0.05f, 12,
                new Vector3(0.0f, 0.32f, 0.0f), Quaternion.identity, whiteLace);

            // 2. Classic gold bangle on left wrist
            if (!isRight)
            {
                AddCylinderAccessory(arm, "GoldBangle", 0.146f, 0.146f, 0.04f, 12,
                    new Vector3(0.0f, 0.54f, 0.0f), Quaternion.identity, goldBangle);
            }
        }

        private static void BuildMangKanorAccessories(Transform arm, bool isRight)
        {
            var whiteSando = new Color(1.000f, 1.000f, 1.000f, 1.0f);
            var vintageLeather = new Color(0.192f, 0.141f, 0.114f, 1.0f);
            var brassDial = new Color(0.82f, 0.72f, 0.45f, 1.0f);

            // 1. White sleeveless sando strap at shoulder (full bare muscular arms)
            AddCylinderAccessory(arm, "SandoStrap", 0.144f, 0.144f, 0.14f, 12,
                new Vector3(0.0f, 0.08f, 0.0f), Quaternion.identity, whiteSando);

            // 2. Vintage leather strap wristwatch on left wrist
            if (!isRight)
            {
                AddCylinderAccessory(arm, "VintageStrap", 0.146f, 0.146f, 0.06f, 12,
                    new Vector3(0.0f, 0.55f, 0.0f), Quaternion.identity, vintageLeather);
                AddBoxAccessory(arm, "VintageDial", new Vector3(0.04f, 0.04f, 0.02f),
                    new Vector3(0.0f, 0.55f, 0.143f), Quaternion.identity, brassDial);
            }
        }

        private static void BuildAlingNenaAccessories(Transform arm, bool isRight)
        {
            var whiteBlouse = new Color(1.000f, 1.000f, 1.000f, 1.0f);
            var orangePattern = new Color(0.878f, 0.478f, 0.227f, 1.0f);

            // 1. White blouse sleeve with orange duster trim
            AddCylinderAccessory(arm, "BlouseSleeve", 0.144f, 0.144f, 0.24f, 12,
                new Vector3(0.0f, 0.13f, 0.0f), Quaternion.identity, whiteBlouse);
            AddCylinderAccessory(arm, "OrangePatternTrim", 0.148f, 0.148f, 0.04f, 12,
                new Vector3(0.0f, 0.24f, 0.0f), Quaternion.identity, orangePattern);

            // 2. Beaded orange bracelet on right wrist
            if (isRight)
            {
                AddCylinderAccessory(arm, "OrangeBracelet", 0.146f, 0.146f, 0.045f, 12,
                    new Vector3(0.0f, 0.54f, 0.0f), Quaternion.identity, orangePattern);
            }
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

            // Step dynamic cloth physics for baggy sleeves (Nemu / baggy clothing)
            Vector3 worldVel = _characterMotor != null ? _characterMotor.Velocity : Vector3.zero;
            Vector2 lookDelta = Vector2.zero;
            if (Application.isPlaying)
            {
                lookDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            }
            float vertAccel = 0.0f;
            if (_characterMotor != null && !_characterMotor.IsGrounded)
            {
                vertAccel = -9.81f;
            }

            if (snap)
            {
                if (_rightClothPhysics != null) _rightClothPhysics.ResetPose();
                if (_leftClothPhysics != null) _leftClothPhysics.ResetPose();
            }
            else
            {
                if (_rightClothPhysics != null) _rightClothPhysics.StepSimulation(dt, worldVel, lookDelta, vertAccel);
                if (_leftClothPhysics != null) _leftClothPhysics.StepSimulation(dt, worldVel, lookDelta, vertAccel);
            }

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
