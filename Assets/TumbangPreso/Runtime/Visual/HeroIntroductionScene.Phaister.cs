using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PHAISTER, VOODOO DOLL, v7 THE PUPPETEER, 6.4 s (HERO-10 v3, 2026-09-29; plan.md 9.8c; her body and the storyboard shots are
        // `tools/author_ultimate_intros.py` `phaister()`). The owner on v6: *"phaister's ult does not have a terrifying feeel at all eh"*,
        // with Flins' burst as the reference (darkness first, a storm of effects), a photo of gloved hands working a marionette control
        // (*"I want the portal (eye) to be diff from the thing that controls it too"*) and an ink impact frame.
        //
        //   0.00-1.10  THE DAY DIES     the dark spreads down the sky and out across the ground from her (`Shaders/VoodooNight` reach)
        //   1.10-2.10  SHE RISES        she floats up, limp, then casts low and wide; pins orbit her; light is drawn up out of the
        //                               ground into her palms; a light under her face; at 2.05 a thread of light shoots up to the sky
        //   2.10-2.90  THE SEAM         a stitched split in the sky over the doll's spot, its stitches snapping; the circle sews round
        //                               it; her pins fly up and stab into its rim
        //   2.90-3.60  THE EYE OPENS    the lids peel apart, the pupil darts and LOCKS ON THE LENS (impact frame); THE BURST at 3.30:
        //                               rays, two shockwave rings, shards, lightning, the circle blazing
        //   3.60-4.60  THE PUPPETEER    two huge mitten gloves push out of the pupil working a marionette control; they PULL (impact
        //                               frame) and the doll is dragged out head-first, upside down, and swings through to hang under it
        //   4.60-5.40  THE DESCENT      lowered in three jerks; its head turns round too far to the lens, then its body follows
        //   5.40-5.80  THE DROP         the wires go slack, it lands (impact frame); the real opponents stagger; she floats down
        //   5.80-6.40  THE PUPPET       the gloves jerk the control: its head snaps up (impact frame), it lurches; the gloves draw back
        //                               into the eye and the control stays over its head, as play picks it up (`VoodooSkyCircle`)
        //
        // ⚠️ THREE THINGS, THREE LOOKS: the EYE is only the portal; the GLOVES and the CONTROL (`MarionetteControl`) work the doll; the
        // DOLL hangs on the wires. None of it is hers (plan 9.2: she never controls it). ⚠️ NEVER SHOWN TWICE: play picks up with the
        // doll where it lurched to (`VoodooDollBody.BesideHer`, `LurchForward`), the circle open with its eye, the control over its head.
        // ⚠️ No sound: every hero skill sound is deleted. Nothing here runs on Update: every piece is posed from `t`.
        // =========================================================================================

        // v12 (the owner on v11: *"put more focus as well on the head twist its fine if u speed up other parts a bit"*): everything
        // before THE TWIST is quicker, and THE TWIST has a shot of its own.
        private const float PhRiseAt = 0.90f, PhSeamAt = 1.70f, PhEyeAt = 2.40f, PhLockAt = 2.72f, PhBurstAt = 2.78f;
        private const float PhGlovesAt = 3.00f, PhPullAt = 3.30f, PhDescentAt = 3.85f, PhTwistAt = 4.45f, PhFaceAt = 4.80f, PhBodyTurnAt = 99f;
        /// <summary>
        /// v19, IT ENDS ON THE STARE (the owner on v18: *"just end it here"*, then *"hold that frame and make the ending transition of
        /// this frame to actual gameplay scary"*). The stare holds; from `PhIrisAt` the dark closes in until only its two burning eyes
        /// are left in black; they flare; one last ink frame; play. The body never turns round and it never drops in the cutscene: play
        /// stands it up beside her (a cut, out of the black).
        /// </summary>
        private const float PhIrisAt = 5.1f, PhEndAt = 6.0f;
        /// <summary>When its face clears the pupil, dragged out head-first (an ink frame: the owner, *"add impact frames for him"*).</summary>
        private const float PhEmergeAt = 3.52f;
        // ⚠️ v21: THE DROP, THE LANDING and THE PUPPET no longer happen in the cutscene (it ends on the stare). v20 left them at 5.35,
        // 5.42 and 5.75: the doll fell and its body turned round after the stare, and two ink frames fired on nothing (the owner:
        // *"WHHY DOES he turn againa fter this"*, *"the impact frame shit after the last pic i sent maeks no sense"*). Past the end.
        private const float PhDropAt = 99f, PhLandAt = 99.07f, PhPuppetAt = 99.4f;
        /// <summary>When the circle starts to sew round the seam; its age runs 1:1 from there.</summary>
        private const float PhCircleOpens = 1.70f;
        /// <summary>THE TWIST's three cranks of the control: (start, end, head turn reached, degrees).</summary>
        private static readonly (float From, float To, float Turn)[] PhCranks = { (4.52f, 4.58f, 60f), (4.63f, 4.69f, 120f), (4.74f, 4.80f, 180f) };
        private const float PhStageRadius = 17f, PhDomeHeight = 16f;
        private static Vector3 PhDollSpot => new Vector3(Abilities.VoodooDollBody.BesideHer, 0f, 0f);
        private static Vector3 PhDollEnd => new Vector3(Abilities.VoodooDollBody.BesideHer, 0f, Abilities.VoodooDollBody.LurchForward);
        private static Vector3 PhCircleCentre => PhDollSpot + Vector3.up * VoodooSkyCircle.Height;
        /// <summary>The four hits the picture turns to ink for (the owner's reference frame), in order: the eye locking on, the pull,
        /// the landing, the head snapping up.</summary>
        private static readonly float[] PhImpacts = { PhLockAt, PhPullAt, PhEmergeAt, PhFaceAt, PhLandAt, PhPuppetAt + .02f };

        private int _nightDome, _nightFloor, _phEyeFlare, _phDust, _phDustInner, _phPalmL, _phPalmR, _phDollEyeL, _phDollEyeR, _phDollEyeBloomL, _phDollEyeBloomR;
        private readonly List<int> _stars = new List<int>(8), _phEmbers = new List<int>(24), _phDrawn = new List<int>(12);
        private readonly List<int> _phRays = new List<int>(16), _phRings = new List<int>(2), _phShards = new List<int>(18);
        private readonly List<Transform> _phPins = new List<Transform>(8);
        private readonly List<int> _phPinGlows = new List<int>(8), _phSparks = new List<int>(64);
        private readonly List<LineRenderer> _phPinTrails = new List<LineRenderer>(8);
        private Transform _phSigil;
        private Material _phSigilMat;
        /// <summary>Embers rising through the whole scene, some right at the lens (research: never an empty frame, something near the
        /// lens). Typed: (angle deg, radius m, start height m, rise m/s, size, phase).</summary>
        private static readonly (float A, float R, float Y, float Rise, float Size, float Phase)[] PhEmberRows =
        {
            (10f, 1.1f, .2f, .55f, .10f, .0f), (40f, 1.8f, .6f, .70f, .08f, .3f), (75f, 2.4f, .1f, .60f, .12f, .6f), (110f, 1.4f, .9f, .80f, .07f, .1f),
            (140f, 2.9f, .3f, .50f, .11f, .8f), (170f, 1.9f, .5f, .65f, .09f, .4f), (200f, 3.3f, .2f, .75f, .13f, .7f), (230f, 1.2f, .7f, .55f, .08f, .2f),
            (260f, 2.6f, .4f, .70f, .10f, .9f), (290f, 1.6f, .1f, .60f, .12f, .5f), (320f, 3.0f, .8f, .85f, .07f, .15f), (350f, 2.1f, .3f, .50f, .11f, .65f),
            (25f, .9f, 1.2f, .45f, .16f, .35f), (60f, 1.0f, .4f, .40f, .18f, .85f), (95f, 3.6f, .6f, .70f, .09f, .55f), (125f, 4.2f, .2f, .80f, .10f, .25f),
            (155f, 2.2f, 1.0f, .65f, .08f, .75f), (185f, 4.0f, .5f, .60f, .12f, .05f), (215f, .95f, .9f, .50f, .17f, .45f), (245f, 3.8f, .3f, .75f, .09f, .95f),
        };
        private SkyCircle _phCircle;
        private LineRenderer _phRise;
        private Light _phUnderLight;
        private Transform _phControl, _phGloveL, _phGloveR;
        private readonly List<LineRenderer> _phStrings = new List<LineRenderer>(4);
        private Transform _phMonster;
        private float _phCourtY, _phMonsterFoot, _phMonsterHeight = 2.2f;
        private Transform _phMTorso, _phMHead, _phMArmL, _phMArmR, _phMLegL, _phMLegR;
        private Vector3 _phMPalmL, _phMPalmR;
        private readonly Vector3[] _phStringPoints = new Vector3[10];

        private void BuildPhaister()
        {
            // ⚠️ The stage's root is on the map floor (`VfxShapes.GroundPoint`), and Bayan Plaza's paving stands above it: flat pieces
            // sit on the court's own surface (`Slipper.GroundY`, the method's section 8 trap), or the floor z-fights under it (v5).
            var rootAt = _root.transform.position;
            _phCourtY = Mathf.Max(0f, Slipper.GroundY(rootAt + Vector3.up * .3f) - rootAt.y);

            // ⚠️ HER NIGHT REPLACES THE WORLD, AND YOU WATCH THE DAY DIE (v7): an unlit dome and floor of her own (`Shaders/VoodooNight`)
            // whose dark spreads down from the top of the sky and out from her feet.
            _nightDome = PhNight("NightDome", WallMesh(40, cap: true), ground: false);
            _nightFloor = PhNight("NightFloor", PhFloorQuad(), ground: true);
            for (int i = 0; i < 7; i++)
                _stars.Add(Add("NightStar" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .38f, 60 + i)), new Color(.95f, .62f, .78f, .9f), .6f));
            for (int i = 0; i < PhEmberRows.Length; i++)
                _phEmbers.Add(AddGlow("PhEmber" + i, i % 3 == 0 ? new Color(.74f, .40f, 1f, 1f) : new Color(1f, .35f, .30f, 1f), falloff: 3f, core: .9f));

            // HER CAST: light drawn up out of the ground into her palms, the palms burning, a light under her face, her pins.
            for (int i = 0; i < 12; i++)
                _phDrawn.Add(AddGlow("PhDrawn" + i, i % 2 == 0 ? new Color(.80f, .36f, 1f, 1f) : new Color(1f, .28f, .40f, 1f), falloff: 3f, core: .9f));
            _phPalmL = AddGlow("PhPalmL", new Color(.86f, .30f, 1f, 1f), falloff: 2.6f, core: .9f);
            _phPalmR = AddGlow("PhPalmR", new Color(.86f, .30f, 1f, 1f), falloff: 2.6f, core: .9f);
            var lightGo = new GameObject("PhUnderLight");
            lightGo.transform.SetParent(_root.transform, false);
            _phUnderLight = lightGo.AddComponent<Light>();
            _phUnderLight.type = LightType.Point;
            _phUnderLight.color = new Color(.78f, .34f, 1f, 1f);
            _phUnderLight.range = 2.6f;
            _phUnderLight.intensity = 0f;
            _phUnderLight.shadows = LightShadows.None;
            _phUnderLight.enabled = false;
            // HER PINS (v12; the owner on v11: *"the stuff that floats around her doesnt look thhat great"*, thin sticks, one through her
            // face): real sewing pins, a long bright shaft with a taper to its point and a big glowing bead of a head, flying point-first
            // round her on a wide tilted orbit clear of her head, each trailing a thread of light.
            for (int i = 0; i < 8; i++)
            {
                var pin = new GameObject("PhPin" + i).transform;
                pin.SetParent(_root.transform, false);
                pin.gameObject.layer = _root.layer;
                var steel = new Color(.86f, .86f, .92f, 1f);
                var bead = i % 2 == 0 ? SkyCircle.Crimson : SkyCircle.Violet;
                MarionetteControl.Block(pin, "Shaft", new Vector3(0f, 0f, 0f), new Vector3(.045f, .045f, .56f), steel, _root.layer, .5f);
                MarionetteControl.Block(pin, "Taper", new Vector3(0f, 0f, .32f), new Vector3(.028f, .028f, .1f), steel, _root.layer, .5f);
                MarionetteControl.Block(pin, "Point", new Vector3(0f, 0f, .39f), new Vector3(.014f, .014f, .06f), steel, _root.layer, .8f);
                MarionetteControl.Block(pin, "Head", new Vector3(0f, 0f, -.3f), Vector3.one * .14f, bead, _root.layer, 1.6f);
                MarionetteControl.Block(pin, "HeadCap", new Vector3(0f, 0f, -.3f), new Vector3(.1f, .1f, .17f), bead, _root.layer, 1.6f);
                pin.gameObject.SetActive(false);
                _phPins.Add(pin);
                _phPinGlows.Add(AddGlow("PhPinGlow" + i, bead, falloff: 2.4f, core: .9f));
                _phPinTrails.Add(PhThread("PhPinTrail" + i, 12));
            }
            // HER CASTING SIGIL on the court under her: the circle's own rim, runes and star, no fangs, no void (the portal is the sky's).
            _phSigilMat = SkyCircle.PartsMaterial(new Vector4(1f, 1f, 1f, 0f), 0f);
            if (_phSigilMat != null)
            {
                var sigil = new GameObject("PhCastingSigil");
                sigil.transform.SetParent(_root.transform, false);
                sigil.layer = _root.layer;
                sigil.AddComponent<MeshFilter>().sharedMesh = SkyCircle.DiscMesh;
                var r = sigil.AddComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                r.sharedMaterial = _phSigilMat;
                VfxRenderTag.Own(sigil, _phSigilMat);
                r.enabled = false;
                _phSigil = sigil.transform;
            }
            // SPARKS for the hits: the seam's stitches snapping, the pull, each crank of THE TWIST, the landing.
            for (int i = 0; i < PhSparkBursts.Length * PhSparksEach; i++)
                _phSparks.Add(AddGlow("PhSpark" + i, i % 3 == 0 ? new Color(1f, .78f, .5f, 1f) : i % 3 == 1 ? new Color(1f, .3f, .4f, 1f) : new Color(.8f, .45f, 1f, 1f), falloff: 3f, core: .95f));

            // THE BURST: rays out of the eye, two shockwave rings, shards flung out.
            for (int i = 0; i < 14; i++)
                _phRays.Add(AddGlow("PhRay" + i, i % 2 == 0 ? new Color(1f, .32f, .42f, 1f) : new Color(.80f, .45f, 1f, 1f), billboard: false, falloff: 2.2f, core: .8f));
            for (int i = 0; i < 2; i++)
                _phRings.Add(Add("PhShock" + i, VfxShapes.Hollow(64, .93f, 0f, 9), i == 0 ? new Color(1f, .30f, .42f, .9f) : new Color(.78f, .45f, 1f, .8f), 1.8f));
            for (int i = 0; i < 18; i++)
                _phShards.Add(Add("PhShard" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .28f, 80 + i)), i % 3 == 0 ? new Color(.80f, .45f, 1f, .95f) : new Color(1f, .26f, .36f, .95f), 1.3f));
            _phEyeFlare = AddGlow("PhEyeFlare", new Color(1f, .30f, .36f, 1f), falloff: 2.8f, core: .9f);
            // ITS EYES (the owner: *"make his stare look very scary"*): a pin of light in the button and in the X, lifted off the face.
            // v13b: SHARP pinpoints (a soft glow read as a smudge on a pale face), each with a red bloom behind it.
            _phDollEyeL = AddGlow("PhDollEyeButton", new Color(1f, .22f, .3f, 1f), falloff: 7f, core: 1f, lift: .2f);
            _phDollEyeR = AddGlow("PhDollEyeX", new Color(1f, .2f, .2f, 1f), falloff: 7f, core: 1f, lift: .2f);
            _phDollEyeBloomL = AddGlow("PhDollEyeBloomButton", new Color(.9f, .1f, .25f, 1f), falloff: 2.2f, core: .5f, lift: .1f);
            _phDollEyeBloomR = AddGlow("PhDollEyeBloomX", new Color(.9f, .05f, .12f, 1f), falloff: 2.2f, core: .5f, lift: .1f);
            _phDust = Add("PhDropDust", VfxShapes.Hollow(48, .86f, .1f, 4), new Color(.62f, .30f, .70f, .85f), 1.4f);
            _phDustInner = Add("PhDropRing", VfxShapes.Hollow(64, .94f, 0f, 9), new Color(1f, .30f, .40f, .8f), 1.2f);

            _phCircle = new SkyCircle(_root.transform, world: false, layer: _root.layer);
            _phRise = PhThread("PhRisingThread", 16);
            for (int i = 0; i < 4; i++) _phStrings.Add(PhThread("PhString" + i, _phStringPoints.Length));

            // THE PUPPETEER: the control and the two gloves that work it.
            _phControl = MarionetteControl.BuildControl(_root.transform, _root.layer);
            _phControl.localScale = Vector3.one * MarionetteControl.DollScale;
            _phGloveL = MarionetteControl.BuildGlove(_root.transform, _root.layer, 3.2f);
            _phGloveR = MarionetteControl.BuildGlove(_root.transform, _root.layer, 3.2f);
            _phGloveL.localScale = _phGloveR.localScale = Vector3.one * 2.4f;
            _phControl.gameObject.SetActive(false); _phGloveL.gameObject.SetActive(false); _phGloveR.gameObject.SetActive(false);

            var art = PhaisterDollArt.LoadArt();
            if (art != null && art.Model != null)
            {
                var monster = PhDoll(art, "PhMonster", CharacterVisual.PersonScale * CharacterVisual.BodyScaleFor(art.Model.name),
                    out _phMonsterFoot, out _phMonsterHeight);
                _phMonster = monster.transform;
                _phMTorso = FindDeep(_phMonster, "torso"); _phMHead = FindDeep(_phMonster, "head");
                _phMArmL = FindDeep(_phMonster, "arm-left"); _phMArmR = FindDeep(_phMonster, "arm-right");
                _phMLegL = FindDeep(_phMonster, "leg-left"); _phMLegR = FindDeep(_phMonster, "leg-right");
                var skinned = monster.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinned != null)
                    for (int i = 0; i < skinned.bones.Length; i++)
                    {
                        if (skinned.bones[i] == _phMArmL) CharacterVisual.PalmCentre(skinned, i, out _phMPalmL);
                        if (skinned.bones[i] == _phMArmR) CharacterVisual.PalmCentre(skinned, i, out _phMPalmR);
                    }
            }
            BuildPhaisterStage();
        }

        /// <summary>A piece of her night (`Shaders/VoodooNight`): the dome round the stage or the floor over the court. Faded by `Tint`.</summary>
        private int PhNight(string name, Mesh mesh, bool ground)
        {
            var go = VfxShapes.Stand(_root.transform, name, mesh, 1);
            go.layer = _root.layer;
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            var shader = Resources.Load<Shader>("Shaders/VoodooNight");
            if (shader != null)
            {
                var m = new Material(shader) { name = name };
                m.SetFloat("_Ground", ground ? 1f : 0f);
                if (ground) m.renderQueue = 2950;
                renderer.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
            }
            else VfxMaterial.Ghost(renderer, new Color(.04f, .01f, .06f, 1f), .1f);
            go.transform.localPosition = ground ? Vector3.up * (_phCourtY + .025f) : Vector3.zero;
            go.transform.localScale = ground ? new Vector3(PhStageRadius, 1f, PhStageRadius) : new Vector3(PhStageRadius, PhDomeHeight, PhStageRadius);
            _pieces.Add(new Piece { Transform = go.transform, Renderer = renderer, Color = Color.white });
            return _pieces.Count - 1;
        }

        /// <summary>A flat quad over the court, x and z -1 to 1 (the shader rounds it).</summary>
        private static Mesh PhFloorQuad()
        {
            var mesh = new Mesh { name = "VoodooNightFloor" };
            mesh.vertices = new[] { new Vector3(-1f, 0f, -1f), new Vector3(1f, 0f, -1f), new Vector3(1f, 0f, 1f), new Vector3(-1f, 0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, .1f, 2f));
            return mesh;
        }

        /// <summary>How far her night has spread at <paramref name="t"/> (0 day, 1 all): you watch the day die.</summary>
        private static float PhReach(float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.15f, .85f, t));

        /// <summary>The spread of her night, and the circle's light on it (where it hangs over the dome's cap and the floor).</summary>
        private void PhNightLight(float t)
        {
            float glow = Ease(PhSeamAt, PhSeamAt + .8f, t) + 1.5f * Flash(t, PhBurstAt + .1f, .5f);
            var pool = new Vector4(PhDollSpot.x / PhStageRadius, PhDollSpot.z / PhStageRadius, .38f, 0f);
            foreach (int index in new[] { _nightDome, _nightFloor })
            {
                var p = _pieces[index];
                p.Renderer.GetPropertyBlock(p.Block);
                p.Block.SetVector("_Pool", pool);
                p.Block.SetFloat("_Glow", glow);
                p.Block.SetFloat("_Phase", t);
                p.Block.SetFloat("_Reach", PhReach(t));
                p.Renderer.SetPropertyBlock(p.Block);
            }
        }

        /// <summary>A thread of light in the circle's own material (`Shaders/VoodooThread`), in the stage's space.</summary>
        private LineRenderer PhThread(string name, int points)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.layer = _root.layer;
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = SkyCircle.ThreadMaterial;
            line.enabled = false;
            return line;
        }

        /// <summary>The doll's art at its size in play, limp (no animator), its light painted in its openings. <paramref name="foot"/>
        /// is how far its pivot sits above its feet; <paramref name="tall"/> its height.</summary>
        private GameObject PhDoll(RosterEntryAsset art, string name, float scale, out float foot, out float tall)
        {
            var go = Object.Instantiate(art.Model, _root.transform, false);
            go.name = name;
            foreach (var animator in go.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            ToonSkin.Apply(go, ToonSkin.PersonOutlineWidth, art.Palette);
            PhaisterDollArt.ApplyGlow(go);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = true;
            }
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
            go.transform.localScale = Vector3.one;
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            { if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds); }
            float raw = any ? Mathf.Max(.01f, bounds.size.y) : 1f;
            go.transform.localScale = Vector3.one * scale;
            foot = any ? (go.transform.position.y - bounds.min.y) * scale : 0f;
            tall = raw * scale;
            SetLayer(go, _root.layer);
            go.SetActive(false);
            return go;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        // ------------------------------------------------------------------ where things are at t

        /// <summary>Where the pupil is: the eye's centre, over the doll's spot.</summary>
        private static Vector3 PhEye => PhCircleCentre;

        /// <summary>
        /// The control at <paramref name="t"/> (stage space) and its turn: pushed out of the pupil by the gloves, yanked on THE PULL,
        /// lowered in three jerks, let fall at THE DROP, jerked up at THE PUPPET, then hanging over the doll's crown as play keeps it.
        /// </summary>
        private Vector3 PhControlAt(float t, out Quaternion turn)
        {
            Vector3 hang = PhEye + Vector3.down * 2.4f;
            float wire = MarionetteControl.WireLength;
            Vector3 low = PhDollSpot + Vector3.up * (_phMonsterHeight + wire + .5f);
            Vector3 at;
            float pitch = 0f, roll = 0f;
            if (t < PhPullAt)
            {
                at = Vector3.Lerp(PhEye + Vector3.up * .4f, hang, Ease(PhGlovesAt, PhPullAt - .03f, t));
                roll = 8f * Mathf.Sin(t * 5f);
            }
            else if (t < PhDescentAt)
            {
                // THE PULL: yanked down and toward the lens, then easing back as the doll swings through.
                float yank = Flash(t, PhPullAt + .06f, .12f);
                at = hang + new Vector3(0f, -.6f, .4f) * yank;
                pitch = -26f * yank + 6f * Mathf.Sin(t * 7f) * (1f - Ease(PhPullAt, PhDescentAt, t));
            }
            else if (t >= PhTwistAt && t < PhDropAt)
            {
                // THE TWIST: the gloves crank the control round in three jerks, and with it the doll's head.
                at = new Vector3(PhDollSpot.x, low.y, PhDollSpot.z) + Vector3.up * .05f * Mathf.Sin(t * 3f);
                float crank = 0f;
                foreach (var c in PhCranks) crank = Mathf.Max(crank, Flash(t, c.To, .07f));
                turn = Quaternion.Euler(-10f * crank, PhCrankTurn(t) * .5f, 12f * crank * Mathf.Sin(t * 40f));
                return at;
            }
            else if (t < PhDropAt)
            {
                float span = PhTwistAt - PhDescentAt - .02f;
                float u = Mathf.Clamp01((t - PhDescentAt) / span);
                int step = u >= .68f ? 2 : u >= .34f ? 1 : 0;
                float start = step * .34f;
                float local = Mathf.Clamp01((u - start) / .16f);
                float from = Mathf.Lerp(hang.y, low.y, step / 3f), to = Mathf.Lerp(hang.y, low.y, (step + 1) / 3f);
                float y = Mathf.Lerp(from, to, local * local);
                float since = (u - start - .16f) * span;
                if (since > 0f) y += Mathf.Sin(since * 38f) * .1f * Mathf.Exp(-since * 14f);
                at = new Vector3(PhDollSpot.x, y, PhDollSpot.z);
                roll = 14f * Flash(t, PhDescentAt + start * span + .05f, .1f) * (step % 2 == 0 ? 1f : -1f);
            }
            else
            {
                // THE DROP lets it fall (slack wires), THE PUPPET jerks it up to where it hangs over the crown for play.
                Vector3 rest = PhDollEnd + Vector3.up * (_phMonsterHeight + MarionetteControl.AboveCrown);
                float drop = Ease(PhDropAt, PhDropAt + .08f, t) * (1f - Ease(PhPuppetAt - .02f, PhPuppetAt + .06f, t));
                float over = Flash(t, PhPuppetAt + .08f, .16f);
                at = Vector3.Lerp(low, rest, Ease(PhDropAt + .1f, PhPuppetAt, t)) + Vector3.down * .7f * drop + Vector3.up * .35f * over;
                at = Vector3.Lerp(at, rest, Ease(PhPuppetAt + .3f, Seconds - .2f, t));
                pitch = -18f * over;
                roll = 5f * Mathf.Sin(t * 3f) * (1f - Ease(PhPuppetAt + .3f, Seconds, t));
            }
            turn = Quaternion.Euler(pitch, 0f, roll);
            return at;
        }

        /// <summary>How far THE TWIST has cranked its head round at <paramref name="t"/>, degrees (ratcheting, a hold between cranks).</summary>
        private static float PhCrankTurn(float t)
        {
            float turn = 0f, from = 0f;
            foreach (var c in PhCranks)
            {
                float u = Mathf.Clamp01((t - c.From) / (c.To - c.From));
                // Each crank overshoots a little and settles back: the head is dragged, not turned.
                float over = u >= 1f ? Mathf.Sin(Mathf.Clamp01((t - c.To) / .12f) * Mathf.PI) * 6f * Mathf.Exp(-(t - c.To) * 10f) : 0f;
                if (t >= c.From) turn = Mathf.Lerp(from, c.Turn, u * u * (3f - 2f * u)) + over;
                from = c.Turn;
            }
            return turn;
        }

        /// <summary>
        /// The doll's feet and its body's turn at <paramref name="t"/>: dragged out of the pupil head-first, upside down, swinging
        /// through to hang under the control, lowered, falling at THE DROP, then standing and lurching.
        /// </summary>
        private void PhMonsterAt(float t, Vector3 control, out Vector3 feet, out Quaternion body)
        {
            float wire = MarionetteControl.WireLength;
            float h = _phMonsterHeight;
            if (t < PhDescentAt)
            {
                // The swing: from straight up (in the eye, upside down) through toward the lens to straight down, overshooting.
                float u = Mathf.Clamp01((t - PhPullAt) / (PhDescentAt - PhPullAt));
                float back = 1f + 2.2f * Mathf.Pow(u - 1f, 3f) + 1.2f * Mathf.Pow(u - 1f, 2f);
                float angle = 180f * (1f - back);
                Vector3 v = Quaternion.AngleAxis(-angle, Vector3.right) * Vector3.down;
                Vector3 crown = control + v * wire;
                feet = crown + v * h;
                float spin = 180f + 70f * (1f - u);
                body = Quaternion.FromToRotation(Vector3.up, -v) * Quaternion.Euler(0f, spin, 0f);
                return;
            }
            float yaw = 180f - 180f * Ease(PhBodyTurnAt, PhBodyTurnAt + .12f, t);
            if (t < PhDropAt)
            {
                Vector3 sway = new Vector3(Mathf.Sin(t * 2.3f), 0f, Mathf.Cos(t * 1.9f)) * .06f;
                feet = control + Vector3.down * (wire + h) + sway;
                body = Quaternion.Euler(0f, yaw, 0f);
                return;
            }
            // Free fall to the court, then the buckle and the lurch.
            float fall = Mathf.Clamp01((t - PhDropAt) / (PhLandAt - PhDropAt));
            float y = .5f * (1f - fall * fall);
            float land = Mathf.Max(0f, t - PhLandAt);
            float buckle = t < PhLandAt ? 0f : land < .08f ? -.1f * land / .08f : -.1f * Mathf.Exp(-(land - .08f) * 7f);
            float lurch = Ease(PhPuppetAt + .28f, PhPuppetAt + .52f, t);
            feet = Vector3.Lerp(PhDollSpot, PhDollEnd, lurch) + Vector3.up * (y + buckle);
            body = Quaternion.identity;
        }

        /// <summary>The doll's feet at <paramref name="t"/> (the opponents and the cameras read it).</summary>
        private Vector3 PhMonsterFeet(float t)
        {
            var control = PhControlAt(t, out _);
            PhMonsterAt(t, control, out var feet, out _);
            return feet;
        }

        // ------------------------------------------------------------------ the sample

        private void SamplePhaister(float t)
        {
            // The night holds to the last frame: the picture goes to black round its eyes, never back to day, before play.
            float leave = 1 - Ease(Seconds - .02f, Seconds, t);
            float reach = PhReach(t);
            Tint(_nightDome, leave > .02f ? leave : 0f); Tint(_nightFloor, leave > .02f ? leave : 0f);
            PhNightLight(t);
            float night = reach * leave;
            Quaternion facingCentre(Vector3 at) => Quaternion.LookRotation(-new Vector3(at.x, 0, at.z).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < _stars.Count; i++)
            {
                float angle = (-150 + i * 43) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 14f, 5.4f + (i * 37 % 5) * .9f, Mathf.Cos(angle) * 14f - .2f);
                float twinkle = _reducedEffects ? .85f : .7f + .3f * Mathf.Sin(t * 5 + i * 1.7f);
                Place(_stars[i], at, Vector3.one * (.2f + (i % 3) * .07f), facingCentre(at), night * twinkle * Ease(.6f + i * .05f, 1.0f + i * .05f, t));
            }

            // THE EMBERS: rising through every shot once the dark has come, drawn up toward the eye once it is open.
            for (int i = 0; i < _phEmbers.Count; i++)
            {
                var row = PhEmberRows[i];
                float local = Mathf.Repeat(t * row.Rise + row.Phase * 5.5f, 5.5f);
                float a = (row.A + 20f * t) * Mathf.Deg2Rad;
                Vector3 at = new Vector3(Mathf.Sin(a) * row.R, row.Y + local, Mathf.Cos(a) * row.R);
                at = Vector3.Lerp(at, PhEye, Ease(PhSeamAt, PhBurstAt + 1f, t) * Mathf.Clamp01(local / 5.5f) * .5f);
                float twinkle = .6f + .4f * Mathf.Sin(t * 9f + i * 1.3f);
                PlaceGlow(_phEmbers[i], at, Vector3.one * row.Size * (1.2f + .6f * twinkle), Quaternion.identity,
                    night * twinkle * Mathf.Clamp01(local * 2f) * Mathf.Clamp01((5.5f - local) * 2f));
            }

            // THE STARE is the doll's close-up alone: she is left out of that one shot (she floats beside it; v19 to v22 flew her off
            // into the sky to clear the frame, and the owner: *"why tf is she flying away"*). `enabled`, not `forceRenderingOff`, which
            // the phase view sets for every capture.
            bool inStare = t >= PhFaceAt - .03f;
            if (_bodyRenderers != null) foreach (var r in _bodyRenderers) if (r != null && r.enabled == inStare) r.enabled = !inStare;
            if (_heldRenderers != null) foreach (var r in _heldRenderers) if (r != null && r.enabled == inStare) r.enabled = !inStare;
            SamplePhaisterCast(t, leave);
            SamplePhaisterSparks(t, leave);
            SamplePhaisterSky(t, leave);
            SamplePhaisterMonster(t, leave);
            PhDollEyes(t, leave);
            SamplePhaisterStage(t, leave);
        }

        /// <summary>SHE RISES: her casting sigil turning on the court under her, the light drawn up into her palms, her palms burning,
        /// the light under her face, her pins flying round her (flung up into the circle's rim at THE SEAM), and the thread of light
        /// she sends up to the sky.</summary>
        private void SamplePhaisterCast(float t, float leave)
        {
            float lift = LiftAt(t);
            Vector3 palmL = FreePalm, palmR = RightPalm;
            float casting = Ease(1.15f, 1.4f, t) * (1f - Ease(PhBurstAt + .2f, PhDescentAt, t)) * leave;
            PlaceGlow(_phPalmL, palmL, Vector3.one * (.32f + .1f * Mathf.Sin(t * 11f)), Quaternion.identity, casting * .9f);
            PlaceGlow(_phPalmR, palmR, Vector3.one * (.32f + .1f * Mathf.Sin(t * 11f + 1f)), Quaternion.identity, casting * .9f);

            // THE SIGIL: sewn round under her as she lifts off, turning, gone once the sky has opened.
            if (_phSigil != null)
            {
                float sigil = Ease(.95f, 1.35f, t) * (1f - Ease(PhEyeAt + .1f, PhBurstAt + .1f, t)) * leave;
                var r = _phSigil.GetComponent<Renderer>();
                r.enabled = sigil > .01f;
                if (r.enabled)
                {
                    _phSigil.localPosition = new Vector3(0f, _phCourtY + .05f, 0f);
                    _phSigil.localScale = Vector3.one * 1.9f;
                    float age = t - .95f;
                    _phSigilMat.SetFloat("_Reveal", Mathf.Clamp01(age / .4f));
                    _phSigilMat.SetFloat("_Inner", Mathf.Clamp01((age - .2f) / .4f));
                    _phSigilMat.SetFloat("_Spin", t * 1.2f);
                    _phSigilMat.SetFloat("_Phase", t);
                    _phSigilMat.SetFloat("_Glow", 2.2f + 2f * Flash(t, 1.3f, .15f));
                    _phSigilMat.SetFloat("_Alpha", sigil);
                }
            }

            for (int i = 0; i < _phDrawn.Count; i++)
            {
                float start = 1.0f + i * .05f, life = .5f;
                float u = Mathf.InverseLerp(start, start + life, t);
                bool on = u > 0f && u < 1f && leave > .02f;
                float a = (i * 30f + 15f) * Mathf.Deg2Rad;
                Vector3 from = new Vector3(Mathf.Sin(a) * 1.8f, _phCourtY + .05f, Mathf.Cos(a) * 1.8f);
                Vector3 to = i % 2 == 0 ? palmL : palmR;
                Vector3 at = Vector3.Lerp(from, to, u * u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * .6f;
                PlaceGlow(_phDrawn[i], at, Vector3.one * .22f, Quaternion.identity, on ? Mathf.Sin(u * Mathf.PI) * 1.4f : 0f);
            }
            if (_phUnderLight != null)
            {
                float under = Ease(1.0f, 1.4f, t) * (1f - Ease(PhEyeAt + .1f, PhBurstAt, t)) * leave;
                _phUnderLight.transform.localPosition = new Vector3(0f, lift + .35f, .7f);
                _phUnderLight.intensity = 1.6f * under;
                _phUnderLight.enabled = under > .01f;
            }

            // HER PINS fly point-first round her on a wide orbit tilted toward the lens, clear of her head, each trailing a thread of light;
            // then one after another they break off and fly up into the circle's rim.
            float spin = (t - Seconds) * 8f * Mathf.Deg2Rad;
            for (int i = 0; i < _phPins.Count; i++)
            {
                var pin = _phPins[i];
                float appear = Ease(1.15f + i * .03f, 1.4f + i * .03f, t);
                float flyAt = 1.9f + i * .04f, arrive = 2.2f + i * .06f;
                bool on = appear > 0f && t < arrive && leave > .02f;
                if (pin.gameObject.activeSelf != on) pin.gameObject.SetActive(on);
                var trail = _phPinTrails[i];
                trail.enabled = on;
                if (!on) { PlaceGlow(_phPinGlows[i], Vector3.zero, Vector3.one, Quaternion.identity, 0f); continue; }
                float a = i / 8f * Mathf.PI * 2f + spin;
                Vector3 rim = PhEye + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * SkyCircle.Radius * .86f;
                float fly = Ease(flyAt, arrive, t);
                Vector3 at = Vector3.Lerp(PhPinOrbit(i, t, lift, appear), rim, fly * fly);
                Vector3 outward = at - PhPinHalo(lift);
                Vector3 point = fly > .02f ? (rim - at).normalized : outward.sqrMagnitude > 1e-6f ? outward.normalized : Vector3.up;
                pin.localPosition = at;
                pin.localRotation = Quaternion.LookRotation(point, Vector3.up);
                pin.localScale = Vector3.one * Mathf.Lerp(1f, 2.4f, fly) * appear;
                PlaceGlow(_phPinGlows[i], at - point * .3f * pin.localScale.x, Vector3.one * .3f, Quaternion.identity, appear * leave * 1.1f);
                for (int k = 0; k < trail.positionCount; k++)
                {
                    float back = k * .025f;
                    float f2 = Ease(flyAt, arrive, t - back);
                    trail.SetPosition(k, Vector3.Lerp(PhPinOrbit(i, t - back, LiftAt(t - back), appear), rim, f2 * f2));
                }
                trail.widthMultiplier = .045f;
                _phCircle.Paint(trail, i % 2 == 0 ? SkyCircle.Crimson : SkyCircle.Violet, leave * appear * .9f, 1f, 0f);
            }

            // THE THREAD: at the flare a line of her light shoots from her up to the sky, where the seam opens.
            bool thread = t > 1.6f && t < 2.1f && leave > .02f;
            _phRise.enabled = thread;
            if (thread)
            {
                Vector3 from = new Vector3(0f, lift + 1.2f, .1f);
                Vector3 to = Vector3.Lerp(from, PhEye, Ease(1.62f, 1.72f, t));
                for (int i = 0; i < _phRise.positionCount; i++)
                {
                    float u = i / (float)(_phRise.positionCount - 1);
                    Vector3 p = Vector3.Lerp(from, to, u) + new Vector3(Mathf.Sin(t * 30f + u * 17f), 0f, Mathf.Cos(t * 23f + u * 11f)) * .08f * Mathf.Sin(Mathf.PI * u);
                    _phRise.SetPosition(i, p);
                }
                _phRise.widthMultiplier = .12f * (1f - Ease(1.85f, 2.1f, t)) + .02f;
                _phCircle.Paint(_phRise, SkyCircle.Crimson, leave, 1f, 1f);
            }
        }

        /// <summary>
        /// Where pin <paramref name="i"/> hangs at <paramref name="t"/>: a HALO of needles fanned out behind her head and shoulders,
        /// points outward, breathing (v12b; an orbit round her kept crossing her face at the close lens, and Black Swan's cards frame
        /// her rather than cover her).
        /// </summary>
        private static Vector3 PhPinOrbit(int i, float t, float lift, float appear)
        {
            float a = (-118f + i * (236f / 7f) + 5f * Mathf.Sin(t * 1.7f + i * .9f)) * Mathf.Deg2Rad;
            float radius = (1.05f + .05f * Mathf.Sin(t * 2.3f + i)) * appear;
            return PhPinHalo(lift) + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * radius + Vector3.back * .12f * Mathf.Cos(a);
        }

        private static Vector3 PhPinHalo(float lift) => new Vector3(0f, lift + 1.45f, -.4f);

        /// <summary>THE SPARKS: (time, where, how many of the burst's slots, speed, spread up). Each burst owns its slots.</summary>
        private const int PhSparksEach = 8;
        private static readonly (float At, int Where, float Speed, float Up)[] PhSparkBursts =
        {
            (2.02f, 0, 2.2f, -.6f), (2.14f, 1, 2.2f, -.6f), (2.27f, 2, 2.2f, -.6f), (2.38f, 3, 2.2f, -.6f),   // the seam's stitches snapping
            (PhPullAt, 4, 6.5f, -.3f), (PhPullAt + .03f, 4, 4.5f, -.8f),                                        // the pull out of the pupil
            (4.58f, 5, 1.6f, .3f), (4.69f, 5, 1.6f, .3f), (4.80f, 5, 2.6f, .4f),                               // each crank of THE TWIST, at its neck
            (PhEmergeAt, 5, 4.5f, .2f), (PhEmergeAt + .12f, 5, 3.5f, -.2f), (PhEmergeAt + .24f, 5, 3f, -.4f),   // torn off it as it swings out
            (PhLandAt, 6, 3.4f, 1.2f), (PhLandAt + .02f, 6, 2.2f, 1.8f),                                         // the landing: stuffing and embers
        };

        private Vector3 PhSparkOrigin(int where, float t)
        {
            float eyeW = SkyCircle.EyeHalfWidth;
            switch (where)
            {
                case 0: return PhEye + Vector3.right * -eyeW * .55f;
                case 1: return PhEye + Vector3.right * eyeW * .2f;
                case 2: return PhEye + Vector3.right * -eyeW * .1f;
                case 3: return PhEye + Vector3.right * eyeW * .6f;
                case 4: return PhEye + Vector3.down * .4f;
                case 5: return _phMHead != null ? _root.transform.InverseTransformPoint(_phMHead.position) : PhMonsterFeet(t) + Vector3.up * _phMonsterHeight * .7f;
                default: return PhDollSpot + Vector3.up * (_phCourtY + .2f);
            }
        }

        private void SamplePhaisterSparks(float t, float leave)
        {
            for (int b = 0; b < PhSparkBursts.Length; b++)
            {
                var burst = PhSparkBursts[b];
                float age = t - burst.At;
                bool live = age >= 0f && age < .7f && leave > .02f && !_reducedEffects;
                Vector3 origin = live ? PhSparkOrigin(burst.Where, burst.At) : Vector3.zero;
                for (int i = 0; i < PhSparksEach; i++)
                {
                    int slot = b * PhSparksEach + i;
                    if (!live) { PlaceGlow(_phSparks[slot], Vector3.zero, Vector3.one, Quaternion.identity, 0f); continue; }
                    float yaw = (i * 45f + b * 17f) * Mathf.Deg2Rad;
                    float h = Mathf.Repeat(Mathf.Sin((slot + 1) * 12.9898f) * 43758.5453f, 1f);
                    Vector3 dir = new Vector3(Mathf.Cos(yaw), burst.Up + (h - .5f) * .8f, Mathf.Sin(yaw)).normalized;
                    float speed = burst.Speed * (.6f + .8f * h);
                    Vector3 at = origin + dir * speed * age + Vector3.down * 4.5f * age * age;
                    float fade = 1f - age / .7f;
                    PlaceGlow(_phSparks[slot], at, Vector3.one * (.16f + .1f * h) * fade, Quaternion.identity, fade * fade * 2f * leave);
                }
            }
        }

        /// <summary>THE SEAM, THE EYE and THE BURST: the circle posed by hand (the seam, its stitches, the lids, the pupil's aim, the
        /// burst's glow and lightning), and the rays, rings and shards thrown out of it.</summary>
        private void SamplePhaisterSky(float t, float leave)
        {
            _phCircle.Seam = Ease(PhSeamAt, PhSeamAt + .2f, t);
            _phCircle.Tear = Ease(PhSeamAt + .28f, PhEyeAt, t);
            float open = Ease(PhEyeAt + .04f, PhLockAt, t);
            _phCircle.EyeOverride = Mathf.Clamp01(open * (1f - .25f * Flash(t, PhEyeAt + .13f, .05f)));
            // The pupil darts left, right, then LOCKS on the lens (the camera is straight under it) and never leaves.
            float look = t < PhEyeAt + .1f ? 0f : t < PhEyeAt + .19f ? -.9f : t < PhLockAt - .02f ? .9f : 0f;
            _phCircle.LookX = look;
            _phCircle.LookY = 0f;
            _phCircle.BurstAge = t >= PhBurstAt ? t - PhBurstAt : -1f;
            float age = t - PhCircleOpens;
            if (age > 0f) _phCircle.Pose(age, PhCircleCentre, 1f, (t - Seconds) * 8f * Mathf.Deg2Rad, leave > .02f ? 1f : 0f);
            else _phCircle.Hide();

            // THE BURST: rays out of the eye along the circle's plane, growing and fading; two shockwave rings; shards flung out and down.
            float b = t - PhBurstAt;
            for (int i = 0; i < _phRays.Count; i++)
            {
                float yaw = i * (360f / _phRays.Count) + (i % 2) * 11f;
                float grow = Mathf.Clamp01(b / (.18f + (i % 3) * .05f));
                float fade = b < 0f ? 0f : Mathf.Clamp01(1f - (b - .15f) / .45f);
                float length = Mathf.Lerp(1f, 10f + (i % 4) * 2.5f, grow);
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Vector3 at = PhEye + dir * (SkyCircle.Radius * .3f + length * .5f) + Vector3.down * .05f;
                PlaceGlow(_phRays[i], at, new Vector3(.5f + (i % 3) * .2f, length, 1f), Quaternion.Euler(90f, yaw, 0f), b >= 0f ? fade * 2.2f * leave : 0f);
            }
            for (int i = 0; i < _phRings.Count; i++)
            {
                float u = Mathf.Clamp01((b - i * .1f) / .55f);
                float scale = Mathf.Lerp(SkyCircle.Radius * .9f, SkyCircle.Radius * (3.2f + i), 1f - (1f - u) * (1f - u));
                Place(_phRings[i], PhEye + Vector3.down * (.1f + i * .3f), Vector3.one * scale, Quaternion.Euler(0f, 30f * i + 40f * t, 0f),
                    b >= i * .1f && u < 1f ? (1f - u) * leave : 0f);
            }
            for (int i = 0; i < _phShards.Count; i++)
            {
                float u = Mathf.Clamp01(b / .7f);
                float yaw = i * 20f + (i % 3) * 7f;
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float speed = 9f + (i % 5) * 2.2f;
                float bb = Mathf.Max(0f, b);
                Vector3 at = PhEye + dir * (SkyCircle.Radius * .4f + speed * bb) + Vector3.down * (2.5f * bb + 6f * bb * bb);
                Place(_phShards[i], at, Vector3.one * (.5f + (i % 4) * .18f), Quaternion.Euler(t * 600f + i * 40f, yaw, t * 300f),
                    b >= 0f && u < 1f ? (1f - u * u) * leave : 0f);
            }
            if (t < PhPuppetAt)
                PlaceGlow(_phEyeFlare, PhEye + Vector3.down * .2f, Vector3.one * (3f + 9f * Flash(t, PhBurstAt + .03f, .2f)), Quaternion.identity,
                    Flash(t, PhBurstAt + .03f, .3f) * 2.5f * leave);
        }

        /// <summary>THE PUPPETEER and the doll: the gloves out of the pupil working the control, the wires, the doll on them.</summary>
        private void SamplePhaisterMonster(float t, float leave)
        {
            var control = PhControlAt(t, out var controlTurn);
            bool puppeteer = t >= PhGlovesAt && leave > .02f;
            if (_phControl.gameObject.activeSelf != puppeteer) _phControl.gameObject.SetActive(puppeteer);
            bool gloves = puppeteer && t < Seconds - .02f;
            if (_phGloveL.gameObject.activeSelf != gloves) { _phGloveL.gameObject.SetActive(gloves); _phGloveR.gameObject.SetActive(gloves); }
            if (puppeteer)
            {
                _phControl.localPosition = control;
                _phControl.localRotation = controlTurn;
                // The gloves grip the bar's ends, sleeves running back up into the pupil; they let go and draw back into it at the end.
                // They hold the control through THE STARE and draw back into the eye as the dark closes.
                float withdraw = Ease(PhIrisAt, PhEndAt - .05f, t);
                PlaceGlove(_phGloveL, 1, withdraw);
                PlaceGlove(_phGloveR, 2, withdraw);
            }

            bool on = t >= PhPullAt && _phMonster != null;
            if (_phMonster != null && _phMonster.gameObject.activeSelf != on) _phMonster.gameObject.SetActive(on);
            if (!on)
            {
                // Before the pull the wires run from the control back up into the pupil, where the doll still is.
                for (int s = 0; s < _phStrings.Count; s++)
                {
                    var line = _phStrings[s];
                    line.enabled = puppeteer;
                    if (!puppeteer) continue;
                    Vector3 top = control + controlTurn * (MarionetteControl.Anchor(s) * MarionetteControl.DollScale);
                    PhWire(line, top, PhEye + Vector3.up * .5f, 0f, t, s, leave);
                }
                Tint(_phDust, 0f); Tint(_phDustInner, 0f);
                return;
            }

            PhMonsterAt(t, control, out var feet, out var body);
            _phMonster.localRotation = body * Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
            _phMonster.localPosition = feet + (body * Vector3.up) * _phMonsterFoot;

            // Its pose: a marionette. Hanging, the hand wires haul its arms up and its head lolls; each jerk flicks it. On the way down
            // its head turns round too far to the lens before its body follows. Landed, the arms drop, the legs splay and the head
            // flops sideways; then the head snaps up at them, the arms jerk out, one leg lurches.
            // ⚠️ Raw euler on the cast's rig (`author_ultimate_intros.py`): +x on the torso and head pitches FORWARD; +y turns the head
            // to its right. A dropped puppet slumps a little and its head falls SIDEWAYS (v4's 60 degrees face-down read as toppled).
            const float LandTorso = 8f, LandHeadX = 12f, LandHeadY = 12f, LandHeadZ = 26f;
            float span = PhTwistAt - PhDescentAt - .02f;
            float jerk = Flash(t, PhPullAt + .06f, .12f);
            for (int k = 0; k < 3; k++) jerk = Mathf.Max(jerk, Flash(t, PhDescentAt + k * .34f * span + .06f, .1f));
            foreach (var c in PhCranks) jerk = Mathf.Max(jerk, .6f * Flash(t, c.To, .06f));
            Vector3 torso, head; float lRaise, lSpread, rRaise, rSpread, lLeg, rLeg, legSpread = 3f, torsoTurn = 0f;
            if (t < PhDropAt)
            {
                // The head turn: while its body still faces away, the head comes round to the lens; then the body swings round under it.
                // THE TWIST: its body hangs facing away while its head is cranked round, 180 degrees in three jerks, to stare into the
                // lens; then the body swings round under the head, which stays on the lens.
                float bodyYaw = t < PhBodyTurnAt ? 180f : 180f - 180f * Ease(PhBodyTurnAt, PhBodyTurnAt + .12f, t);
                float crankTurn = PhCrankTurn(t);
                // As the body swings round under it the head counter-turns by the same amount, so the face never leaves the lens
                // (v17 unwound it the other way and showed the back of its head for a frame).
                float headYaw = crankTurn + (180f - bodyYaw);
                float round = Ease(PhTwistAt, PhFaceAt, t);
                float stare = Ease(PhFaceAt, PhFaceAt + .06f, t) * (1f - Ease(PhBodyTurnAt, PhBodyTurnAt + .1f, t));
                // THE STARE: dead still on the lens, head cocked, then a twitch, another, a smaller one: something alive inside.
                float twitch = 0f;
                foreach (float at in new[] { PhFaceAt + .14f, PhFaceAt + .25f, PhFaceAt + .33f }) twitch += Flash(t, at, .03f) * (at == PhFaceAt + .33f ? .5f : 1f);
                torso = new Vector3(10f - 10f * jerk, 0f, 4f * Mathf.Sin(t * 3f) * (1f - round));
                head = new Vector3(Mathf.Lerp(20f - 26f * jerk, -8f, round) + 5f * twitch * stare, headYaw + 7f * twitch * stare,
                                   Mathf.Lerp(14f, 0f, round) + 18f * stare - 9f * twitch * stare + 12f * Ease(PhFaceAt + .4f, PhEndAt, t));
                lRaise = 150f + 20f * jerk; lSpread = 42f; rRaise = 146f + 22f * jerk; rSpread = 46f;
                lLeg = 6f * Mathf.Sin(t * 4f); rLeg = -6f * Mathf.Sin(t * 4f + .6f);
            }
            else if (t < PhPuppetAt)
            {
                float drop = Ease(PhDropAt, PhLandAt + .1f, t);
                torso = new Vector3(Mathf.Lerp(10f, LandTorso, drop), 0f, Mathf.Lerp(4f, 6f, drop));
                head = new Vector3(Mathf.Lerp(-4f, LandHeadX, drop), Mathf.Lerp(-12f, LandHeadY, drop), Mathf.Lerp(0f, LandHeadZ, drop));
                lRaise = Mathf.Lerp(150f, 18f, drop); lSpread = 30f; rRaise = Mathf.Lerp(146f, 24f, drop); rSpread = 34f;
                lLeg = 4f; rLeg = -4f; legSpread = Mathf.Lerp(3f, 12f, drop);
            }
            else
            {
                float snap = Ease(PhPuppetAt, PhPuppetAt + .07f, t), arms = Ease(PhPuppetAt + .14f, PhPuppetAt + .24f, t);
                float step = Ease(PhPuppetAt + .28f, PhPuppetAt + .5f, t), settle = Ease(PhPuppetAt + .55f, Seconds, t);
                float twitch = Mathf.Sin(t * 17f) * (1f - settle) * 3f;
                float look = PmLookYaw();
                torso = new Vector3(Mathf.Lerp(LandTorso, 4f, snap) + 8f * step - 4f * settle, 0f, Mathf.Lerp(6f, -8f, settle));
                head = new Vector3(Mathf.Lerp(LandHeadX, -8f, snap) + twitch, Mathf.Lerp(LandHeadY, look * .75f, snap) + 6f * settle,
                                   Mathf.Lerp(LandHeadZ, 22f, settle));
                torsoTurn = look * .25f * arms;
                lRaise = Mathf.Lerp(18f, 78f, arms) - 20f * settle; lSpread = Mathf.Lerp(30f, 58f, arms);
                rRaise = Mathf.Lerp(24f, 70f, arms) - 16f * settle; rSpread = Mathf.Lerp(34f, 62f, arms);
                lLeg = 4f + 26f * step * (1f - settle * .6f); rLeg = -4f - 8f * step;
                legSpread = Mathf.Lerp(12f, 5f, snap);
            }
            PoseBone(_phMTorso, torso + new Vector3(0f, torsoTurn, 0f));
            PoseBone(_phMHead, head);
            PoseBone(_phMArmL, new Vector3(-lRaise, 0f, 80f - lSpread));
            PoseBone(_phMArmR, new Vector3(-rRaise, 0f, -(80f - rSpread)));
            PoseBone(_phMLegL, new Vector3(-lLeg, 0f, -legSpread));
            PoseBone(_phMLegR, new Vector3(-rLeg, 0f, legSpread));

            // THE WIRES, from the control's four points to its crown, both mitten hands and its back: taut while it hangs, slack at
            // THE DROP, snapping taut at THE PUPPET.
            Vector3 bodyUp = body * Vector3.up;
            Vector3 crown = _phMHead != null ? _root.transform.InverseTransformPoint(_phMHead.position) + bodyUp * .55f : feet + bodyUp * _phMonsterHeight;
            Vector3 left = _phMArmL != null ? _root.transform.InverseTransformPoint(_phMArmL.TransformPoint(_phMPalmL)) : feet + new Vector3(-.6f, 1.4f, 0f);
            Vector3 right = _phMArmR != null ? _root.transform.InverseTransformPoint(_phMArmR.TransformPoint(_phMPalmR)) : feet + new Vector3(.6f, 1.4f, 0f);
            Vector3 back = _phMTorso != null ? _root.transform.InverseTransformPoint(_phMTorso.position) + bodyUp * .3f : feet + bodyUp * 1.1f;
            var ends = new[] { crown, left, right, back };
            float slack = t < PhDropAt ? 0f : (1f - Ease(PhPuppetAt - .02f, PhPuppetAt + .06f, t)) * Ease(PhDropAt, PhDropAt + .08f, t);
            for (int s = 0; s < _phStrings.Count; s++)
            {
                Vector3 top = control + controlTurn * (MarionetteControl.Anchor(s) * MarionetteControl.DollScale);
                PhWire(_phStrings[s], top, ends[s], slack, t, s, leave);
            }

            // THE DROP: a ring of dust and light racing out from its feet; its eyes flare as its head snaps up.
            Vector3 ground = PhDollSpot + Vector3.up * (_phCourtY + .04f);
            float u2 = Mathf.InverseLerp(PhLandAt, PhLandAt + .35f, t);
            Place(_phDust, ground, Vector3.one * Mathf.Lerp(.6f, 7f, 1 - (1 - u2) * (1 - u2)), Quaternion.identity, (t >= PhLandAt ? 1f - u2 : 0f) * leave);
            float ringU = Mathf.InverseLerp(PhLandAt, PhLandAt + .25f, t);
            Place(_phDustInner, ground, Vector3.one * Mathf.Lerp(.4f, 3.2f, ringU), Quaternion.Euler(0f, 20f * t, 0f), (t >= PhLandAt ? (1f - ringU) * .9f : 0f) * leave);
            if (t >= PhPuppetAt)
            {
                float flare = Flash(t, PhPuppetAt + .04f, .22f) + .35f * Ease(PhPuppetAt, PhPuppetAt + .2f, t);
                Vector3 face = _phMHead != null ? _root.transform.InverseTransformPoint(_phMHead.position) + Vector3.forward * .45f + Vector3.up * .25f : crown;
                PlaceGlow(_phEyeFlare, face, Vector3.one * (.6f + .9f * Flash(t, PhPuppetAt + .04f, .22f)), Quaternion.identity, flare * leave);
            }
        }

        /// <summary>Its face (stage space): in front of its head, where the camera and the ink frames aim.</summary>
        private Vector3 PhDollFace()
        {
            if (_phMHead == null || _phMonster == null || !_phMonster.gameObject.activeInHierarchy)
                return PhMonsterFeet(Mathf.Max(PhPullAt, 0f)) + Vector3.up * (_phMonsterHeight * .8f);
            return _root.transform.InverseTransformPoint(_phMHead.TransformPoint(new Vector3(0f, .15f, .17f)));
        }

        /// <summary>ITS EYES: two pins of light in its button and its X, from the moment it is dragged out; dim while it hangs, BLAZING in
        /// THE STARE and at THE PUPPET, flickering like something alive behind them.</summary>
        private void PhDollEyes(float t, float leave)
        {
            bool on = _phMHead != null && t >= PhPullAt && leave > .02f;
            if (!on)
            {
                foreach (int g in new[] { _phDollEyeL, _phDollEyeR, _phDollEyeBloomL, _phDollEyeBloomR })
                    PlaceGlow(g, Vector3.zero, Vector3.one, Quaternion.identity, 0f);
                return;
            }
            float stare = Ease(PhFaceAt - .02f, PhFaceAt + .05f, t);
            // The flare before the cut, and the eyes growing a little as the dark closes on them.
            float puppet = Flash(t, PhPuppetAt + .04f, .25f) + .5f * Ease(PhPuppetAt, PhPuppetAt + .2f, t);
            // The eyes brighten as the dark closes on them and FLARE for two frames before the cut, staying pinpoints (v19's flare
            // swelled them into two soft blobs).
            float closing = .8f * Ease(PhIrisAt, PhEndAt - .15f, t) + 2.5f * Flash(t, PhEndAt - .05f, .06f);
            float flicker = .75f + .25f * Mathf.Sin(t * 37f) * Mathf.Sin(t * 23f + 1f);
            float power = (.55f + 2.2f * stare + 1.8f * puppet + 2f * closing) * flicker * leave;
            Vector3 facing = _phMHead.TransformDirection(Vector3.forward);
            Vector3 face = _root.transform.InverseTransformDirection(facing);
            foreach (var (glow, bloom, x) in new[] { (_phDollEyeL, _phDollEyeBloomL, .065f), (_phDollEyeR, _phDollEyeBloomR, -.065f) })
            {
                Vector3 at = _root.transform.InverseTransformPoint(_phMHead.TransformPoint(new Vector3(x, .146f, .17f)));
                if (glow == _phDollEyeL) _phEyeAtL = at; else _phEyeAtR = at;
                // As the dark closes the glows shrink back into the eyes, so it is HIS button and X that burn in the black, not two dots.
                float shrink = 1f - .7f * Ease(PhIrisAt, PhEndAt - .2f, t);
                PlaceGlow(glow, at, Vector3.one * (.07f + .03f * stare + .06f * puppet) * shrink, Quaternion.identity, power * 3.5f, face);
                PlaceGlow(bloom, at, Vector3.one * (.35f + .35f * stare + .3f * puppet) * shrink, Quaternion.identity, power * .55f, face);
            }
        }

        /// <summary>A glove on the control's bar end <paramref name="anchor"/>, its sleeve up to the pupil; drawn back into it as
        /// <paramref name="withdraw"/> goes to 1.</summary>
        private void PlaceGlove(Transform glove, int anchor, float withdraw)
        {
            Vector3 grip = _phControl.localPosition + _phControl.localRotation * ((MarionetteControl.Anchor(anchor) + Vector3.up * .16f) * MarionetteControl.DollScale);
            grip = Vector3.Lerp(grip, PhEye + (grip - PhEye).normalized * .5f, withdraw * withdraw);
            // The sleeve always runs UP through the pupil into the dark above it (measured from a point high over the eye): measured
            // from the eye itself, a glove still inside it pointed its sleeve down through the lens (film v8).
            Vector3 away = grip - (PhEye + Vector3.up * 6f);
            glove.localPosition = grip;
            glove.localRotation = Quaternion.LookRotation(away.normalized, anchor == 1 ? Vector3.left : Vector3.right);
        }

        /// <summary>One wire from <paramref name="top"/> to <paramref name="end"/>, sagging by <paramref name="slack"/>, swaying a little.</summary>
        private void PhWire(LineRenderer line, Vector3 top, Vector3 end, float slack, float t, int s, float leave)
        {
            for (int i = 0; i < _phStringPoints.Length; i++)
            {
                float u = i / (float)(_phStringPoints.Length - 1);
                Vector3 p = Vector3.Lerp(end, top, u);
                float envelope = Mathf.Sin(Mathf.PI * u);
                p += Vector3.down * slack * .7f * envelope;
                p += new Vector3(Mathf.Sin(t * 1.7f + u * 5f + s), 0f, Mathf.Cos(t * 1.3f + u * 4f + s * 2f)) * .04f * envelope;
                _phStringPoints[i] = p;
            }
            line.SetPositions(_phStringPoints);
            line.widthMultiplier = s == 0 ? .06f : .045f;
            _phCircle.Paint(line, SkyCircle.Violet, leave, 1f, 1f);
            line.enabled = leave > .02f;
        }

        /// <summary>A bone's local turn as the authoring table writes it (raw Unity local euler angles, `author_ultimate_intros.py`).</summary>
        private static void PoseBone(Transform bone, Vector3 raw)
        {
            if (bone != null) bone.localRotation = Quaternion.Euler(raw);
        }

        // ------------------------------------------------------------------ the grade, the shake and the impact frames

        /// <summary>Her night steps the world back as it spreads, eased a little for the puppet so the opponents read, released at the
        /// hand-back.</summary>
        private void PhaisterGrade(float t, out float brightness, out float saturation)
        {
            float away = PhReach(t) * (1f - .3f * Ease(PhPuppetAt, PhPuppetAt + .4f, t)) * (1f - Ease(Seconds - .16f, Seconds, t));
            // THE STARE drains the world to grey round its burning eyes, and lets it back as its body turns.
            float stare = Ease(PhFaceAt, PhFaceAt + .08f, t) * (1f - Ease(PhBodyTurnAt, PhDropAt, t));
            // (Only a little: the grade takes the whole frame, glows too, and v15's grey turned its burning eyes a pale pink. The
            // vignette in `PhaisterPostProcess` does the dark.)
            brightness = 1f - .26f * away - .12f * stare;
            saturation = 1f - .16f * away - .12f * stare;
        }

        /// <summary>The lens flinches on every hit and on the burst.</summary>
        private Vector3 PhaisterShake(float t)
        {
            float k = 0f;
            foreach (float at in PhImpacts) if (t >= at && t - at < .35f) k = Mathf.Max(k, Mathf.Exp(-(t - at) * 14f));
            if (t >= PhBurstAt) k = Mathf.Max(k, .8f * Mathf.Exp(-(t - PhBurstAt) * 6f));
            return new Vector3(Mathf.Sin(t * 91f), Mathf.Sin(t * 73f + 1f), Mathf.Sin(t * 59f + 2f)) * .07f * k;
        }

        private Material _phImpact;
        private Vector3 _phEyeAtL, _phEyeAtR;

        /// <summary>
        /// THE IMPACT FRAMES (the owner's reference: *"a complete 1-2 frames change everything biriefy to put focus on some moments"*):
        /// on each hit the whole picture turns to black ink on white for two frames, radial speed lines out of the focus, the frame
        /// punched in toward it (`Shaders/PhaisterImpact`). Reduced effects keeps the hits and drops the frames.
        /// </summary>
        private void PhaisterPostProcess(RenderTexture frame, Camera camera, float t)
        {
            if (_reducedEffects) return;
            int hit = -1;
            for (int i = 0; i < PhImpacts.Length; i++) if (t >= PhImpacts[i] && t < PhImpacts[i] + .1f) hit = i;
            float vignette = Ease(PhFaceAt + .06f, PhFaceAt + .16f, t) * (1f - Ease(PhBodyTurnAt, PhBodyTurnAt + .1f, t));
            // v21 (the owner: *"after this js let it fade to darkness with it ending in only his eyes glowing"*): a plain fade to black
            // from 5.1 s, his O and X burning in over his real eyes as it goes; the last quarter second is his eyes alone.
            float iris = Ease(PhIrisAt, PhEndAt - .25f, t);
            if (hit < 0 && vignette <= .01f) return;
            if (_phImpact == null)
            {
                var shader = Resources.Load<Shader>("Shaders/PhaisterImpact");
                if (shader == null) return;
                _phImpact = new Material(shader) { name = "PhaisterImpact" };
                _phImpact.SetColor("_Ink", new Color(.85f, .10f, .22f, 1f));
            }
            // EACH HIT ITS OWN FRAME (the owner: *"all ur impact frames look very similar"*): its style, its two tones, how many frames.
            int style = 0; float frames = 2f; Color light = new Color(.95f, .94f, .96f, 1f), dark = new Color(.03f, .02f, .04f, 1f);
            if (hit == 3) { style = 4; frames = 2f; }                                                                          // the stare: his face in ink
            else if (hit == 0) { style = 1; light = new Color(.95f, .16f, .24f, 1f); dark = new Color(.02f, 0f, .02f, 1f); }       // the eye: a crimson target
            else if (hit == 1) { style = 2; frames = 1f; }                                                                    // the pull: raked, one frame
            else if (hit == 2) { style = 3; frames = 3f; light = new Color(.1f, .06f, .12f, 1f); dark = new Color(.96f, .92f, .88f, 1f); } // the emergence: torn, a negative
            if (hit >= 0 && t >= PhImpacts[hit] + frames / 30f) hit = -1;
            if (hit < 0 && vignette <= .01f) return;
            _phImpact.SetColor("_Light", light);
            _phImpact.SetColor("_Dark", dark);
            _phImpact.SetFloat("_Style", style);
            _phImpact.SetVector("_Dir", new Vector4(0f, -1f, 0f, 0f));
            Vector3 focus = hit < 0 ? PhDollFace() : hit == 0 ? PhEye : hit == 1 ? PhControlAt(t, out _) : hit == 4 ? PhDollSpot + Vector3.up * .8f : PhDollFace();
            var vp = camera.WorldToViewportPoint(_root.transform.TransformPoint(focus));
            if (vp.z < 0f) vp = new Vector3(.5f, .5f, 1f);
            bool first = hit >= 0 && t < PhImpacts[hit] + .034f;
            bool third = hit >= 0 && t >= PhImpacts[hit] + .067f;
            _phImpact.SetVector("_Focus", new Vector4(Mathf.Clamp01(vp.x), Mathf.Clamp01(vp.y), 0f, 0f));
            _phImpact.SetFloat("_Amount", hit < 0 ? 0f : first ? 1f : third ? .55f : .85f);
            _phImpact.SetFloat("_Seed", Mathf.Max(0, hit) * 2 + (first ? 0f : 1f));
            _phImpact.SetFloat("_Lines", hit < 0 ? 0f : 1f);
            _phImpact.SetFloat("_Zoom", hit < 0 ? 0f : hit == 1 ? .1f : first ? .06f : .03f);
            _phImpact.SetFloat("_Vignette", hit < 0 ? vignette * .85f : 0f);
            // THE IRIS: the dark closes in until only its two eyes burn in black.
            var eyeA = camera.WorldToViewportPoint(_root.transform.TransformPoint(_phEyeAtL));
            var eyeB = camera.WorldToViewportPoint(_root.transform.TransformPoint(_phEyeAtR));
            _phImpact.SetVector("_Eyes", new Vector4(eyeA.x, eyeA.y, eyeB.x, eyeB.y));
            _phImpact.SetFloat("_Iris", hit < 0 ? iris : 0f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _phImpact);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }
    }
}
