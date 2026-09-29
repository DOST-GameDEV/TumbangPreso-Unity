using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PHAISTER, VOODOO DOLL, 5.8 s (HERO-10 v3, 2026-09-29; plan.md 9.8b; her body and the storyboard shots are
        // `tools/author_ultimate_intros.py` `phaister()`). It replaces OMEN's cutscene, of which the owner said *"ult cutscene doesnt
        // amke sense why does she thhrow some random shit and it doesnt touch anythhing thhoroughly rethink direction of it"*; the
        // direction is his: *"i want her to cast like a really scary magic circle in teh sky for her cutscene and then this monster comes
        // out of it and looks like its controlled by strings and scary"*.
        //
        //   0.00-1.10  THE OFFERING  her night replaces the world; she raises the small doll out to her side in her left fist; its eye
        //                            lights; a thread of light rises out of it into the dark
        //   1.10-2.40  THE CIRCLE    the thread reaches the sky and THE CIRCLE tears open over her right (`SkyCircle`: rims sewn,
        //                            teeth, pins, runes, the eye opening); at 2.10 the doll is yanked up out of her hands into the eye
        //   2.40-4.10  THE DESCENT   the MONSTER (the doll at its fighting size) is lowered out of the eye on strings (crown, both
        //                            hands, back) in three jerks, limp, arms hauled up by the hand strings, turning to face the court
        //   4.10-4.70  THE DROP      it lands beside her, knees buckling like a puppet's: the impact frame, a ring of dust and light
        //                            racing out; the REAL opponents flinch (`HeroIntroductionScene.PhaisterMark.cs`)
        //   4.70-5.80  THE PUPPET    its head snaps up, its arms jerk out, it lurches one step at them on its strings, eyes flaring;
        //                            she smirks behind it, a hand to her hat brim
        //
        // ⚠️ THE STRINGS COME FROM THE CIRCLE, NEVER FROM HER (plan 9.2, the standing rule: the doll is its own character; she never
        // dies, faints or controls it). ⚠️ NEVER SHOWN TWICE: play picks up from the last frame, the doll where it lurched to
        // (`VoodooDollBody.BesideHer`, `LurchForward`), the circle fully open over it with the strings down (`VoodooSkyCircle` starts
        // open). ⚠️ No sound: every hero skill sound is deleted. Nothing here runs on Update: every piece is posed from `t`.
        // =========================================================================================

        private const float PhCircleAt = 1.10f, PhSwallowAt = 2.10f, PhDescentAt = 2.40f, PhLandAt = 4.10f, PhPuppetAt = 4.70f;
        /// <summary>When the circle starts to open, and its age runs 1:1 from there (fully open at 2.6 s, `SkyCircle.FullyOpenAge`).</summary>
        private const float PhCircleOpens = 1.20f;
        private const float PhStageRadius = 17f, PhDomeHeight = 16f, PhSmallDoll = 0.55f;
        private static Vector3 PhDollSpot => new Vector3(Abilities.VoodooDollBody.BesideHer, 0f, 0f);
        private static Vector3 PhDollEnd => new Vector3(Abilities.VoodooDollBody.BesideHer, 0f, Abilities.VoodooDollBody.LurchForward);
        private static Vector3 PhCircleCentre => PhDollSpot + Vector3.up * VoodooSkyCircle.Height;

        private int _nightDome, _nightFloor, _phDollGlow, _phDust, _phDustInner, _phEyeFlare;
        private readonly List<int> _stars = new List<int>(8), _phEmbers = new List<int>(24);
        /// <summary>Embers rising through the whole scene, some right at the lens (research: never an empty frame, something near the
        /// lens; Castorice's butterflies, Nahida's leaves). Typed: (angle deg, radius m, start height m, rise m/s, size, phase).</summary>
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
        private readonly List<LineRenderer> _phStrings = new List<LineRenderer>(4);
        private Transform _phSmall, _phMonster;
        private float _phCourtY, _phSmallFoot, _phMonsterFoot, _phMonsterHeight = 2.2f;
        private Transform _phMTorso, _phMHead, _phMArmL, _phMArmR, _phMLegL, _phMLegR;
        private Vector3 _phMPalmL, _phMPalmR;
        private readonly Vector3[] _phStringPoints = new Vector3[10];

        private void BuildPhaister()
        {
            // ⚠️ The stage's root is on the map floor (`VfxShapes.GroundPoint`), and Bayan Plaza's paving stands above it: flat pieces
            // sit on the court's own surface (`Slipper.GroundY`, the method's section 8 trap), or v5's floor z-fights under it.
            var rootAt = _root.transform.position;
            _phCourtY = Mathf.Max(0f, Slipper.GroundY(rootAt + Vector3.up * .3f) - rootAt.y);
            // ⚠️ HER NIGHT REPLACES THE WORLD (v5; film v4's lit translucent wall took the day's blue ambient and fog and read grey-blue,
            // with the court still in daylight under it): an unlit dome and floor of her own (`Shaders/VoodooNight`), PhStageRadius out,
            // which hides the plaza beyond the court and still holds every opponent who can be staged (within PmReach of the doll).
            _nightDome = PhNight("NightDome", WallMesh(40, cap: true), ground: false);
            _nightFloor = PhNight("NightFloor", PhFloorQuad(), ground: true);
            for (int i = 0; i < 7; i++)
                _stars.Add(Add("NightStar" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .38f, 60 + i)), new Color(.95f, .62f, .78f, .9f), .6f));
            _phDollGlow = AddGlow("PhDollGlow", new Color(.86f, .22f, .62f, 1f), falloff: 2.4f, core: .8f, lift: .1f);
            for (int i = 0; i < PhEmberRows.Length; i++)
                _phEmbers.Add(AddGlow("PhEmber" + i, i % 3 == 0 ? new Color(.74f, .40f, 1f, 1f) : new Color(1f, .35f, .30f, 1f), falloff: 3f, core: .9f));
            _phEyeFlare = AddGlow("PhEyeFlare", new Color(1f, .30f, .36f, 1f), falloff: 2.8f, core: .9f);
            _phDust = Add("PhDropDust", VfxShapes.Hollow(48, .86f, .1f, 4), new Color(.62f, .30f, .70f, .85f), 1.4f);
            _phDustInner = Add("PhDropRing", VfxShapes.Hollow(64, .94f, 0f, 9), new Color(1f, .30f, .40f, .8f), 1.2f);

            _phCircle = new SkyCircle(_root.transform, world: false, layer: _root.layer);
            _phRise = PhThread("PhRisingThread", 16);
            for (int i = 0; i < 4; i++) _phStrings.Add(PhThread("PhString" + i, _phStringPoints.Length));

            var art = PhaisterDollArt.LoadArt();
            if (art != null && art.Model != null)
            {
                _phSmall = PhDoll(art, "PhSmallDoll", scale: 0f, height: PhSmallDoll, out _phSmallFoot, out _).transform;
                _phSmallBaseScale = _phSmall.localScale.x;
                var monster = PhDoll(art, "PhMonster", scale: CharacterVisual.PersonScale * CharacterVisual.BodyScaleFor(art.Model.name),
                    height: 0f, out _phMonsterFoot, out _phMonsterHeight);
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

        /// <summary>The circle's light on her night: where it hangs over the dome's cap and the floor (object space), and how bright.</summary>
        private void PhNightLight(float t)
        {
            float glow = Ease(PhCircleOpens, PhCircleOpens + .7f, t);
            var pool = new Vector4(PhDollSpot.x / PhStageRadius, PhDollSpot.z / PhStageRadius, .38f, 0f);
            foreach (int index in new[] { _nightDome, _nightFloor })
            {
                var p = _pieces[index];
                p.Renderer.GetPropertyBlock(p.Block);
                p.Block.SetVector("_Pool", pool);
                p.Block.SetFloat("_Glow", glow);
                p.Block.SetFloat("_Phase", t);
                p.Renderer.SetPropertyBlock(p.Block);
            }
        }

        /// <summary>A thread of her light in the circle's own material (`Shaders/VoodooThread`), in the stage's space.</summary>
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

        /// <summary>
        /// The doll's art, limp (no animator), its light painted in its openings. Either at <paramref name="scale"/> (the monster: the
        /// size its body has in play) or fitted to <paramref name="height"/> metres (the small doll in her hands). <paramref name="foot"/>
        /// is how far its pivot sits above its feet; <paramref name="tall"/> its height.
        /// </summary>
        private GameObject PhDoll(RosterEntryAsset art, string name, float scale, float height, out float foot, out float tall)
        {
            var go = Object.Instantiate(art.Model, _root.transform, false);
            go.name = name;
            foreach (var animator in go.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            ToonSkin.Apply(go, scale > 0f ? ToonSkin.PersonOutlineWidth : ToonSkin.PropOutlineWidth * .6f, art.Palette);
            PhaisterDollArt.ApplyGlow(go);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = scale > 0f ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
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
            float k = scale > 0f ? scale : height / raw;
            go.transform.localScale = Vector3.one * k;
            foot = any ? (go.transform.position.y - bounds.min.y) * k : 0f;
            tall = raw * k;
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

        /// <summary>The small doll gripped in her left fist, raised at her side (it rides her hand as the body keys move it); its middle
        /// sits above the fist, which holds it by the legs.</summary>
        private Vector3 PhOffered => FreePalm + new Vector3(0f, .2f, 0f);

        /// <summary>The small doll at <paramref name="t"/>: in her hands, then yanked up the thread into the eye (gone at 2.40).</summary>
        private Vector3 PhSmallAt(float t, out float size)
        {
            size = 1f;
            if (t < PhSwallowAt) return PhOffered;
            float u = Mathf.Clamp01((t - PhSwallowAt) / (PhDescentAt - PhSwallowAt));
            size = 1f - u * .7f;
            return Vector3.Lerp(PhOffered, PhCircleCentre, u * u);
        }

        /// <summary>
        /// The monster's feet at <paramref name="t"/>: lowered out of the eye in three jerks (each a fast drop that bounces on its
        /// strings), landing at 4.10 with the knees buckling, then the lurch.
        /// </summary>
        private Vector3 PhMonsterFeet(float t)
        {
            float top = PhCircleCentre.y - _phMonsterHeight - .2f;
            if (t < PhDescentAt) return PhDollSpot + Vector3.up * top;
            if (t < PhLandAt)
            {
                float u = (t - PhDescentAt) / (PhLandAt - PhDescentAt);
                // Three drops: 0.00-0.22, 0.33-0.55, 0.66-0.92 of the descent, each followed by a bounce on the strings.
                float y = 0f;
                float[] starts = { 0f, .33f, .66f }, ends = { .22f, .55f, .92f };
                float[] levels = { top, top * .62f, top * .28f, .25f };
                int step = 0;
                for (int i = 0; i < 3; i++) if (u >= starts[i]) step = i;
                float local = Mathf.Clamp01((u - starts[step]) / (ends[step] - starts[step]));
                y = Mathf.Lerp(levels[step], levels[step + 1], local * local);
                if (u > ends[step]) y = levels[step + 1] + Mathf.Sin((u - ends[step]) * 40f) * .12f * Mathf.Exp(-(u - ends[step]) * 20f);
                return PhDollSpot + Vector3.up * y;
            }
            float land = t - PhLandAt;
            // A dip at the landing, not a sink: v4 dropped it 0.28 m through the court.
            float buckle = land < .08f ? -.1f * land / .08f : -.1f * Mathf.Exp(-(land - .08f) * 7f);
            float lurch = Ease(PhPuppetAt + .28f, PhPuppetAt + .52f, t);
            return Vector3.Lerp(PhDollSpot, PhDollEnd, lurch) + Vector3.up * buckle;
        }

        // ------------------------------------------------------------------ the sample

        private void SamplePhaister(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            // THE NIGHT, at once, blood-dark: the change of world is the first thing that happens.
            float night = Ease(0f, .3f, t);
            Tint(_nightDome, night * leave); Tint(_nightFloor, night * leave);
            PhNightLight(t);
            Quaternion facingCentre(Vector3 at) => Quaternion.LookRotation(-new Vector3(at.x, 0, at.z).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < _stars.Count; i++)
            {
                float angle = (-150 + i * 43) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 14f, 5.4f + (i * 37 % 5) * .9f, Mathf.Cos(angle) * 14f - .2f);
                float twinkle = _reducedEffects ? .85f : .7f + .3f * Mathf.Sin(t * 5 + i * 1.7f);
                Place(_stars[i], at, Vector3.one * (.2f + (i % 3) * .07f), facingCentre(at), night * leave * twinkle * Ease(.2f + i * .05f, .6f + i * .05f, t));
            }

            // THE OFFERING: the small doll in her hands, its eye lighting; then yanked up the thread into the eye.
            if (_phSmall != null)
            {
                Vector3 small = PhSmallAt(t, out float size);
                bool on = t < PhDescentAt;
                if (_phSmall.gameObject.activeSelf != on) _phSmall.gameObject.SetActive(on);
                if (on)
                {
                    _phSmall.localPosition = small - Vector3.up * (PhSmallDoll * .5f - _phSmallFoot) * size;
                    float jolt = Flash(t, .55f, .1f) * 14f;
                    // Its face out to the lens and the court (v4 turned it to her, so the lit eye was on the far side).
                    _phSmall.localRotation = Quaternion.Euler(-8f + jolt, -14f + 8f * Mathf.Sin(t * 3f), 0f);
                    _phSmall.localScale = Vector3.one * _phSmallBaseScale * size;
                }
                float glow = Ease(.4f, .9f, t) * (t < PhDescentAt ? 1f : 0f);
                PlaceGlow(_phDollGlow, small, Vector3.one * (.5f + .4f * Ease(1f, 2f, t)), Quaternion.identity, glow * .8f * leave);
            }

            // THE THREAD rises out of the doll to the sky (1.10 to 1.50) and, from 2.10, draws the doll up it.
            {
                bool on = t > .95f && t < PhDescentAt;
                _phRise.enabled = on;
                if (on)
                {
                    Vector3 from = PhSmallAt(t, out _);
                    float reach = Ease(.95f, 1.45f, t);
                    Vector3 to = Vector3.Lerp(from, PhCircleCentre, reach);
                    for (int i = 0; i < _phRise.positionCount; i++)
                    {
                        float u = i / (float)(_phRise.positionCount - 1);
                        Vector3 p = Vector3.Lerp(from, to, u);
                        p += new Vector3(Mathf.Sin(t * 6f + u * 9f), 0f, Mathf.Cos(t * 5f + u * 7f)) * .12f * Mathf.Sin(Mathf.PI * u) * (1f - Ease(PhSwallowAt, PhSwallowAt + .1f, t));
                        _phRise.SetPosition(i, p);
                    }
                    _phRise.widthMultiplier = .07f;
                    _phCircle.Paint(_phRise, SkyCircle.Violet, leave, 1f, 1f);
                }
            }

            // THE EMBERS: rising through every shot, some right at the lens, drawn up toward the circle once it is open.
            for (int i = 0; i < _phEmbers.Count; i++)
            {
                var row = PhEmberRows[i];
                float cycle = 5.5f / row.Rise;
                float local = Mathf.Repeat(t * row.Rise + row.Phase * 5.5f, 5.5f);
                float a = (row.A + 20f * t) * Mathf.Deg2Rad;
                Vector3 at = new Vector3(Mathf.Sin(a) * row.R, row.Y + local, Mathf.Cos(a) * row.R);
                at = Vector3.Lerp(at, PhCircleCentre, Ease(PhCircleAt, PhDescentAt + 1f, t) * Mathf.Clamp01(local / 5.5f) * .5f);
                float twinkle = .6f + .4f * Mathf.Sin(t * 9f + i * 1.3f);
                PlaceGlow(_phEmbers[i], at, Vector3.one * row.Size * (1.2f + .6f * twinkle), Quaternion.identity,
                    night * leave * twinkle * Mathf.Clamp01(local * 2f) * Mathf.Clamp01((5.5f - local) * 2f) * (cycle > 0f ? 1f : 0f));
            }

            // THE CIRCLE: torn open from 1.20, fully open by 2.60, turning so it arrives at the hand-back unturned.
            float age = t - PhCircleOpens;
            if (age > 0f) _phCircle.Pose(age, PhCircleCentre, 1f, (t - Seconds) * 8f * Mathf.Deg2Rad, leave > .02f ? 1f : 0f);
            else _phCircle.Hide();

            SamplePhaisterMonster(t, leave);
            SamplePhaisterStage(t, leave);
        }

        private float _phSmallBaseScale = 1f;

        private void SamplePhaisterMonster(float t, float leave)
        {
            if (_phMonster == null) return;
            bool on = t >= PhDescentAt;
            if (_phMonster.gameObject.activeSelf != on) _phMonster.gameObject.SetActive(on);
            if (!on)
            {
                foreach (var s in _phStrings) s.enabled = false;
                PlaceGlow(_phEyeFlare, Vector3.zero, Vector3.one, Quaternion.identity, 0f);
                Tint(_phDust, 0f); Tint(_phDustInner, 0f);
                return;
            }
            Vector3 feet = PhMonsterFeet(t);
            // Hanging, it turns on its strings from facing her to facing the court, in jerks with the drops.
            float turn = t < PhLandAt ? Mathf.Lerp(150f, 0f, Mathf.Pow(Mathf.Clamp01((t - PhDescentAt) / (PhLandAt - PhDescentAt - .2f)), .7f)) : 0f;
            turn += t < PhLandAt ? 6f * Mathf.Sin(t * 5f) : 0f;
            _phMonster.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw + turn, 0f);
            _phMonster.localPosition = feet + Vector3.up * _phMonsterFoot;

            // Its pose: a marionette. Hanging, the hand strings haul its arms up and its head lolls; each jerk flicks it; landed, the
            // arms drop, the legs splay and the head flops to one side; then the head snaps up, the arms jerk out, one leg lurches.
            // ⚠️ Raw euler on the cast's rig (`author_ultimate_intros.py`): +x on the torso and head pitches FORWARD. v4 landed with
            // the torso at 22 and the head at 38 on top of it, 60 degrees face-down: on a body that is mostly head that read as
            // toppled flat. A dropped puppet slumps a little and its head falls SIDEWAYS (z), and its legs splay (spread).
            const float LandTorso = 8f, LandHeadX = 12f, LandHeadY = 12f, LandHeadZ = 26f;
            float jerk = 0f;
            foreach (float at in new[] { PhDescentAt + .02f, PhDescentAt + .58f, PhDescentAt + 1.14f }) jerk = Mathf.Max(jerk, Flash(t, at + .05f, .12f));
            Vector3 torso, head; float lRaise, lSpread, rRaise, rSpread, lLeg, rLeg, legSpread = 3f, torsoTurn = 0f;
            if (t < PhLandAt)
            {
                torso = new Vector3(10f - 10f * jerk, 0f, 4f * Mathf.Sin(t * 3f));
                head = new Vector3(20f - 26f * jerk, 10f * Mathf.Sin(t * 2.3f), 14f);
                lRaise = 150f + 20f * jerk; lSpread = 42f; rRaise = 146f + 22f * jerk; rSpread = 46f;
                lLeg = 6f * Mathf.Sin(t * 4f); rLeg = -6f * Mathf.Sin(t * 4f + .6f);
            }
            else if (t < PhPuppetAt)
            {
                float drop = Ease(PhLandAt, PhLandAt + .12f, t);
                torso = new Vector3(Mathf.Lerp(10f, LandTorso, drop), 0f, Mathf.Lerp(4f, 6f, drop));
                head = new Vector3(Mathf.Lerp(20f, LandHeadX, drop), LandHeadY, Mathf.Lerp(14f, LandHeadZ, drop));
                lRaise = Mathf.Lerp(150f, 18f, drop); lSpread = 30f; rRaise = Mathf.Lerp(146f, 24f, drop); rSpread = 34f;
                lLeg = 4f; rLeg = -4f; legSpread = Mathf.Lerp(3f, 12f, drop);
            }
            else
            {
                float snap = Ease(PhPuppetAt, PhPuppetAt + .07f, t), arms = Ease(PhPuppetAt + .14f, PhPuppetAt + .24f, t);
                float step = Ease(PhPuppetAt + .28f, PhPuppetAt + .5f, t), settle = Ease(PhPuppetAt + .55f, PhPuppetAt + .9f, t);
                float twitch = Mathf.Sin(t * 17f) * (1f - settle) * 3f;
                torso = new Vector3(Mathf.Lerp(LandTorso, 4f, snap) + 8f * step - 4f * settle, 0f, Mathf.Lerp(6f, -8f, settle));
                // The head jerks round to the real opponents (as far as a neck goes), the chest a little after it.
                float look = PmLookYaw();
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

            // THE STRINGS, from the eye to its crown, both hands and its back: taut while it hangs, slack on the landing, snapping taut
            // with the puppet's jerks.
            Vector3 crown = _phMHead != null ? _root.transform.InverseTransformPoint(_phMHead.position) + Vector3.up * .55f : feet + Vector3.up * _phMonsterHeight;
            Vector3 left = _phMArmL != null ? _root.transform.InverseTransformPoint(_phMArmL.TransformPoint(_phMPalmL)) : feet + new Vector3(-.6f, 1.4f, 0f);
            Vector3 right = _phMArmR != null ? _root.transform.InverseTransformPoint(_phMArmR.TransformPoint(_phMPalmR)) : feet + new Vector3(.6f, 1.4f, 0f);
            Vector3 back = _phMTorso != null ? _root.transform.InverseTransformPoint(_phMTorso.position) + Vector3.up * .3f : feet + Vector3.up * 1.1f;
            var ends = new[] { crown, left, right, back };
            float slack = t < PhLandAt ? 0f : (1f - Ease(PhPuppetAt, PhPuppetAt + .1f, t)) * Ease(PhLandAt, PhLandAt + .1f, t);
            for (int s = 0; s < _phStrings.Count; s++)
            {
                var line = _phStrings[s];
                Vector3 top = SkyCircle.StringAnchor(PhCircleCentre, 1f, s, _phStrings.Count);
                for (int i = 0; i < _phStringPoints.Length; i++)
                {
                    float u = i / (float)(_phStringPoints.Length - 1);
                    Vector3 p = Vector3.Lerp(ends[s], top, u);
                    float envelope = Mathf.Sin(Mathf.PI * u);
                    p += Vector3.down * slack * .6f * envelope;
                    p += new Vector3(Mathf.Sin(t * 1.7f + u * 5f + s), 0f, Mathf.Cos(t * 1.3f + u * 4f + s * 2f)) * .1f * envelope;
                    _phStringPoints[i] = p;
                }
                line.SetPositions(_phStringPoints);
                line.widthMultiplier = s == 0 ? .07f : .05f;
                _phCircle.Paint(line, SkyCircle.Violet, leave, 1f, 1f);
                line.enabled = leave > .02f;
            }

            // THE DROP: a ring of dust and light racing out from its feet; its eyes flare as its head snaps up.
            Vector3 ground = PhDollSpot + Vector3.up * (_phCourtY + .04f);
            float u2 = Mathf.InverseLerp(PhLandAt, PhLandAt + .35f, t);
            Place(_phDust, ground, Vector3.one * Mathf.Lerp(.6f, 7f, 1 - (1 - u2) * (1 - u2)), Quaternion.identity, (t >= PhLandAt ? 1f - u2 : 0f) * leave);
            float ringU = Mathf.InverseLerp(PhLandAt, PhLandAt + .25f, t);
            Place(_phDustInner, ground, Vector3.one * Mathf.Lerp(.4f, 3.2f, ringU), Quaternion.Euler(0f, 20f * t, 0f), (t >= PhLandAt ? (1f - ringU) * .9f : 0f) * leave);
            float flare = Flash(t, PhPuppetAt + .04f, .22f) + .35f * Ease(PhPuppetAt, PhPuppetAt + .2f, t);
            Vector3 face = _phMHead != null ? _root.transform.InverseTransformPoint(_phMHead.position) + _phMonster.localRotation * Vector3.forward * .45f + Vector3.up * .25f : crown;
            PlaceGlow(_phEyeFlare, face, Vector3.one * (.6f + .9f * Flash(t, PhPuppetAt + .04f, .22f)), Quaternion.identity, flare * leave);
        }

        /// <summary>A bone's local turn as the authoring table writes it (raw Unity local euler angles, `author_ultimate_intros.py`).</summary>
        private static void PoseBone(Transform bone, Vector3 raw)
        {
            if (bone != null) bone.localRotation = Quaternion.Euler(raw);
        }

        // ------------------------------------------------------------------ the grade and the impact frame

        /// <summary>Her night steps the world back: deep through the circle and the descent, eased a little for the puppet so the
        /// opponents read, released at the hand-back.</summary>
        private void PhaisterGrade(float t, out float brightness, out float saturation)
        {
            float away = Ease(0f, .3f, t) * (1f - .3f * Ease(PhPuppetAt, PhPuppetAt + .4f, t)) * (1f - Ease(Seconds - .16f, Seconds, t));
            // v5: her night is dark in itself now (`Shaders/VoodooNight`), so the grade only has to settle the lit bodies into it.
            brightness = 1f - .24f * away;
            saturation = 1f - .14f * away;
        }

        private Material _phImpact;

        /// <summary>The two frames as it lands: the picture turned inside out round its feet (`Shaders/PhaisterImpact`), the camera
        /// flinching at its weight. Reduced effects keeps the landing and drops the frames.</summary>
        private void PhaisterPostProcess(RenderTexture frame, Camera camera, float t)
        {
            if (_reducedEffects || t < PhLandAt || t > PhLandAt + .085f) return;
            if (_phImpact == null)
            {
                var shader = Resources.Load<Shader>("Shaders/PhaisterImpact");
                if (shader == null) return;
                _phImpact = new Material(shader) { name = "PhaisterImpact" };
            }
            var vp = camera.WorldToViewportPoint(_root.transform.TransformPoint(PhDollSpot + Vector3.up * .5f));
            _phImpact.SetVector("_Focus", new Vector4(vp.x, vp.y, 0f, 0f));
            _phImpact.SetFloat("_Amount", t < PhLandAt + .045f ? 1f : .75f);
            _phImpact.SetFloat("_Seed", t < PhLandAt + .045f ? 0f : 1f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _phImpact);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }
    }
}
