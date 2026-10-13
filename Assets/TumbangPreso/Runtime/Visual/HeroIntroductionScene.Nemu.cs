using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU, LET THEM IN: THEIR OWN SHADOWS, 5.9 s (owner 2026-10-13; `docs/reports/nemu-ultimate-2026-10-11/direction.md`
        // section 14). On preview v5 the owner: *"direction still ssucks, i dont get wtf is happening"*, *"what are those purple
        // eyes"*. Of three new stories offered he picked THEIR OWN SHADOWS.
        //
        // One sentence: Nemu whispers, and every player's own shadow peels up off the ground behind them, stands, puts its
        // hands over their eyes and pours into them. The travelling thing is a ring of dark from her feet: it reaches each
        // player and their shadow wakes.
        //
        //   S1 HER (0.00-0.90): close on her face under her fringe; her eyes ignite violet (0.32); a whisper.
        //   S2 THE COURT (0.90-2.85), from beside her, in daylight: a ring of dark goes out from her feet across the court; as it
        //     reaches each player their shadow turns ink-black and twitches, then peels up off the ground, hinged at their feet,
        //     and stands behind them, taller than them, its eyes opening violet.
        //   S3 TAKEN (2.85-5.90), one eased path: in front of one player, their shadow behind them; its arms stretch round their
        //     head and its hands close over their eyes (3.00-3.60); they flinch and fight it; it pours into them (3.95-4.55);
        //     their eyes open violet (4.60); they turn to the lens head first; the lens pulls back to hold every player staring,
        //     the aura on them and no shadow under any of them.
        //
        // Readability first (v3 to v5 the owner could not follow them): daylight, one idea per shot, the effect in the same
        // frame as its cause, nothing on screen that the story does not explain.
        // Rules that still hold: black and one violet (no white, no amber); no Kuro; after the entry no ghost is drawn on a
        // player, only the aura (`NemuAura`); below 6 s; nothing chonky; nothing flies. Nothing here runs on Update and nothing
        // is random: every piece is posed from the scene clock.
        // ⚠️ THE PLAYERS ARE THE REAL ONES (`StageOthers`), acted by turning their own bones (`NmAct`); each one's shadow is a
        // second copy of THEIR body in `Shaders/NemuDead` (black smoke, a broken violet rim), flattened onto the ground along
        // the sun and stood up from it (`NmShade`).
        // =========================================================================================
        private const float NmIgnite = .32f, NmWide = .90f, NmRingOut = 1.0f, NmRingIn = 1.45f;
        private const float NmPeel = 1.55f, NmStood = 2.5f, NmClose = 2.85f;
        private const float NmHandsIn = 3.0f, NmHandsShut = 3.6f, NmStruggled = 4.1f, NmPour = 3.95f, NmPoured = 4.55f;
        private const float NmEyesOpen = 4.60f, NmTurn = 4.65f, NmPullBack = 4.75f;
        /// <summary>How long the impact frame as the hands shut lasts: two film frames (the first inverted).</summary>
        private const float NmImpact = .067f;
        /// <summary>How much bigger than its player a shadow stands, how far behind them and how far off to one side (metres), so
        /// it looms over a shoulder where the lens in front can see it (film r22: straight behind them it was hidden).</summary>
        private const float NmShadeTall = 1.35f, NmShadeBehind = .45f, NmShadeAside = .4f;
        /// <summary>The ring of dark from her feet: how much further than the furthest player it goes (metres).</summary>
        private const float NmRingPast = 1.5f;

        /// <summary>A FILM OUTSIDE PLAY: the editor's cutscene film still names a companion here (no longer drawn by Nemu's).</summary>
        public static GhostPetCompanion FilmCompanion;

        /// <summary>One of their bones and how it was when the cutscene began.</summary>
        private struct NmBone
        {
            public Transform T;
            public Quaternion Rot;
            public Vector3 Pos, Scale;
            public void Reset() { if (T != null) { T.localRotation = Rot; T.localPosition = Pos; T.localScale = Scale; } }
        }

        /// <summary>A player's shadow, stood up: a copy of their body in black smoke, hinged at the feet.</summary>
        private sealed class NmShade
        {
            public Transform Hinge, Flat;
            public Renderer[] Renderers;
            public Material Skin;
            public NmBone Root, Torso, Head, ArmL, ArmR;
            /// <summary>Each arm's palm in its bone's space, and which local axis runs down the arm.</summary>
            public Vector3 PalmL, PalmR;
            public int AxisL = 1, AxisR = 1;
            public bool HasPalmL, HasPalmR;
            /// <summary>Its eyes, in its head's space.</summary>
            public Vector3 EyesLocal;
            /// <summary>Which way their shadow lies on the court (scene space, flat), and how long it is (times their height).</summary>
            public Vector3 Lies;
            public float Length = 1f;
        }

        private sealed class NmVictim
        {
            public GameObject Holder;
            public Renderer[] Renderers;
            public Transform Head;
            public NmBone Root, Torso, HeadBone, ArmL, ArmR;
            public NmShade Shade;
            public Vector3 Home, FaceLocal, FaceAt;
            public float Yaw, Reach, Top = 1.7f, Width = .7f, FaceDepth = .3f, FaceTop = .5f;
            /// <summary>When the ring of dark reaches them: their shadow wakes.</summary>
            public float Woken;
            public int Seat;
            public bool ShadowsOff;
        }

        private readonly List<NmVictim> _nmVictims = new List<NmVictim>(4);
        private NemuSmokeBatch _nmSmoke;
        private Vector3 _nmWay = Vector3.forward, _nmCentre = new Vector3(0f, 0f, 6f);
        private Vector3 _nmEye = new Vector3(-.72f, 1.08f, 1.75f), _nmLook = new Vector3(0f, .98f, 0f);
        private Vector3 _nmFaceLocal = Vector3.forward, _nmEyesLocal;
        private float _nmFaceDepth = .3f, _nmHeadTop = 1.2f, _nmRingReach = 8f;
        private Material _nmInk;

        // ------------------------------------------------------------------ build

        private void BuildNemu(CharacterMotor source)
        {
            _nmSmoke = new NemuSmokeBatch(_root.transform, "NmShadows", 600, 900);
            if (_head != null)
            {
                NemuAura.MeasureFace(_head, _bodyRenderers, _root.transform.forward, out _nmFaceLocal, out _nmFaceDepth, out _nmHeadTop);
                // HER EYES, in the head's own space, so they ride its pitch. They are the two black blocks on her face just under
                // her fringe (owner on v6, the glows sitting on the fringe above them: *"what even is this"*).
                Vector3 face = _head.rotation * _nmFaceLocal;
                _nmEyesLocal = _head.InverseTransformPoint(_head.position + _head.up * (_nmHeadTop * .1f - .17f) + face * (_nmFaceDepth - .01f));
            }

            StageOthers(3.0f, 8.5f);
            Vector3 centre = Vector3.zero;
            float far = 0f;
            for (int i = 0; i < _others.Count; i++)
            {
                _nmVictims.Add(BuildVictim(_others[i], i));
                centre += _others[i].Home; far = Mathf.Max(far, _others[i].Reach);
            }
            if (_nmVictims.Count > 0)
            {
                centre /= _nmVictims.Count; centre.y = 0f;
                _nmCentre = centre;
                if (centre.sqrMagnitude > .04f) _nmWay = centre.normalized;
                _nmRingReach = far + NmRingPast;
                // The ring reaches each player in turn, nearest first.
                foreach (var v in _nmVictims) v.Woken = Mathf.Lerp(NmRingOut, NmRingIn, Mathf.Sqrt(Mathf.Clamp01(v.Reach / _nmRingReach)));
            }
        }

        private NmVictim BuildVictim(Other other, int index)
        {
            var v = new NmVictim { Holder = other.Holder, Renderers = other.Renderers, Home = other.Home, Yaw = other.Yaw, Reach = other.Reach, Seat = index };
            var round = GameServices.Round;
            if (round != null)
                foreach (var p in round.Players)
                    if (p != null && other.Holder.name == "IntroductionOther-P" + (p.PlayerSlot + 1)) { v.Seat = p.PlayerSlot; break; }
            ReadBones(other.Holder.transform, out v.Root, out v.Torso, out v.HeadBone, out v.ArmL, out v.ArmR);
            v.Head = v.HeadBone.T;
            bool any = false; Bounds b = default;
            foreach (var r in other.Renderers) { if (r == null) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (any)
            {
                v.Top = Mathf.Clamp(b.max.y - other.Holder.transform.position.y, .9f, 2.6f);
                v.Width = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z), .4f, 1.2f);
            }
            if (v.Head != null) NemuAura.MeasureFace(v.Head, other.Renderers, other.Holder.transform.forward, out v.FaceLocal, out v.FaceDepth, out v.FaceTop);
            Vector3 faceWorld = v.Head != null
                ? v.Head.position + Vector3.up * (v.FaceTop * .28f) + other.Holder.transform.forward * v.FaceDepth
                : other.Holder.transform.position + Vector3.up * (v.Top * .7f);
            v.FaceAt = _root.transform.InverseTransformPoint(faceWorld);
            v.Shade = BuildShade(v, faceWorld);
            return v;
        }

        private static void ReadBones(Transform holder, out NmBone root, out NmBone torso, out NmBone head, out NmBone armL, out NmBone armR)
        {
            root = torso = head = armL = armR = default;
            foreach (var t in holder.GetComponentsInChildren<Transform>(true))
            {
                var bone = new NmBone { T = t, Rot = t.localRotation, Pos = t.localPosition, Scale = t.localScale };
                switch (t.name)
                {
                    case "root": if (root.T == null) root = bone; break;
                    case "torso": if (torso.T == null) torso = bone; break;
                    case "head": if (head.T == null) head = bone; break;
                    case "arm-left": if (armL.T == null) armL = bone; break;
                    case "arm-right": if (armR.T == null) armR = bone; break;
                }
            }
        }

        /// <summary>
        /// Their shadow: a second copy of their body, in black smoke, under a hinge at their feet whose local z runs the way the
        /// sun throws their shadow. Flattened along that z and laid down by 90 degrees, it is the shadow on the court; stood up,
        /// it is them in black, taller.
        /// </summary>
        private NmShade BuildShade(NmVictim v, Vector3 faceWorld)
        {
            var shader = Resources.Load<Shader>("Shaders/NemuDead");
            if (shader == null) return null;
            var shade = new NmShade();
            // Which way their shadow lies: out to one side and back, long, toward where it will stand over their shoulder. Not
            // the sun's way (films r22 to r24: the phase lens shows no cast shadows, and the sun's way often hid it behind them).
            Vector3 fwd = Quaternion.Euler(0f, v.Yaw, 0f) * Vector3.forward;
            Vector3 aside = Vector3.Cross(Vector3.up, fwd) * ((v.Seat & 1) == 0 ? 1f : -1f);
            shade.Lies = (aside - fwd * .5f).normalized;
            shade.Length = 1.6f;

            var hinge = new GameObject("NmShadow" + v.Seat).transform;
            hinge.SetParent(_root.transform, false);
            hinge.localPosition = v.Home + Vector3.up * .02f;
            hinge.localRotation = Quaternion.LookRotation(shade.Lies, Vector3.up);
            var flat = new GameObject("Flat").transform;
            flat.SetParent(hinge, false);
            var copy = Object.Instantiate(v.Holder, _root.transform, false);
            copy.name = "Body";
            copy.transform.SetParent(flat, true);
            shade.Hinge = hinge; shade.Flat = flat;
            shade.Skin = new Material(shader) { name = "NemuShadow" + v.Seat };
            shade.Skin.SetFloat("_Seed", v.Seat * 3.1f + .7f);
            VfxRenderTag.Own(hinge.gameObject, shade.Skin);
            shade.Renderers = copy.GetComponentsInChildren<Renderer>(true);
            foreach (var r in shade.Renderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = shade.Skin;
                r.sharedMaterials = mats;
                r.SetPropertyBlock(null);
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                VfxRenderTag.Attach(r.gameObject);
            }
            ReadBones(copy.transform, out shade.Root, out shade.Torso, out shade.Head, out shade.ArmL, out shade.ArmR);
            shade.HasPalmL = Palm(shade.ArmL.T, shade.Renderers, out shade.PalmL, out shade.AxisL);
            shade.HasPalmR = Palm(shade.ArmR.T, shade.Renderers, out shade.PalmR, out shade.AxisR);
            if (shade.Head.T != null) shade.EyesLocal = shade.Head.T.InverseTransformPoint(faceWorld);
            return shade;
        }

        /// <summary>Where an arm's palm is in its bone's space, and the bone's local axis that runs down the arm.</summary>
        private static bool Palm(Transform arm, Renderer[] renderers, out Vector3 palm, out int axis)
        {
            palm = Vector3.down * .3f; axis = 1;
            if (arm == null) return false;
            foreach (var r in renderers)
            {
                if (!(r is SkinnedMeshRenderer skin) || skin.sharedMesh == null) continue;
                int bone = System.Array.IndexOf(skin.bones, arm);
                if (bone < 0 || !CharacterVisual.PalmCentre(skin, bone, out palm)) continue;
                Vector3 a = new Vector3(Mathf.Abs(palm.x), Mathf.Abs(palm.y), Mathf.Abs(palm.z));
                axis = a.x >= a.y && a.x >= a.z ? 0 : (a.y >= a.z ? 1 : 2);
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ the clock's shape

        private Vector3 NmW(Vector3 local) => _root.transform.TransformPoint(local);
        private Vector3 NmWDir(Vector3 local) => _root.transform.TransformDirection(local);
        private static Vector3 NmForward(NmVictim v) => Quaternion.Euler(0f, v.Yaw, 0f) * Vector3.forward;

        // ------------------------------------------------------------------ sample

        private void SampleNemu(float t)
        {
            _nmSmoke.Begin();
            _nmSmoke.SetView(NmW(_nmEye), NmW(_nmLook));
            Vector3 up = Vector3.up;

            // ------------------------------------------------------------- S1: her eyes
            if (_head != null)
            {
                Vector3 eyes = _head.TransformPoint(_nmEyesLocal);
                Vector3 look = _head.rotation * _nmFaceLocal;
                float lit = Ease(NmIgnite, NmIgnite + .06f, t) * (1f + 1.4f * Flash(t, NmIgnite + .05f, .1f)) * (1f - Ease(Seconds - .5f, Seconds, t));
                // On her two eyes, as wide apart as they are (v6: at a quarter of the head's height they sat outside them).
                NemuAura.Eyes(_nmSmoke, eyes, look, .2f, .03f, lit * 2.3f, NmW(_nmEye));
                float glint = Flash(t, NmIgnite + .04f, .1f);
                if (glint > 0f && t < NmWide) Glint(eyes + look * .05f, .06f, 1.6f * glint, Vector3.Cross(Vector3.up, look), .4f * glint);
            }

            // ------------------------------------------------------------- S2: the ring of dark from her feet to the players
            // A band of black smoke rolling out over the court, its strands laid along the ring (films r22 to r26: drawn as a
            // flat ring piece it never showed on the court at all).
            if (t >= NmRingOut)
            {
                float u = Mathf.Clamp01((t - NmRingOut) / (NmRingIn - NmRingOut + .25f));
                float on = u < 1f ? 1f - u * u : 0f;
                float r = Mathf.Lerp(.4f, _nmRingReach, Mathf.Sqrt(u));
                const int strands = 64;
                for (int j = 0; j < strands && on > .01f; j++)
                {
                    float a = (j + .5f * (j & 1)) / strands * Mathf.PI * 2f;
                    Vector3 round = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    Vector3 along = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
                    // Three rows: the leading edge and the smoke dragging behind it (film r27: two thin rows were faint grey
                    // scratches on a sunlit court).
                    for (int row = 0; row < 3; row++)
                    {
                        float back = row * Mathf.Min(.6f, r * .12f);
                        Vector3 at = round * Mathf.Max(.3f, r - back) + up * (.15f + .1f * row);
                        _nmSmoke.Wisp(NmW(at), NmWDir(along), .42f - .1f * row, Mathf.Max(.45f, r * Mathf.PI * 2f / strands * 1.7f),
                                      on * (1f - .25f * row), j * .037f + row * .33f, u * .5f, .9f);
                    }
                }
            }

            for (int i = 0; i < _nmVictims.Count; i++) SampleVictim(_nmVictims[i], i, t);
            _nmSmoke.End(t);
        }

        /// <summary>A four-pointed star of light (world): a long cross on up and <paramref name="side"/>, and a soft core.</summary>
        private void Glint(Vector3 at, float size, float strength, Vector3 side, float reach)
        {
            _nmSmoke.Light(at, size * 1.6f, strength, NemuSmokeBatch.Violet);
            _nmSmoke.Light(at, size * .5f, strength, NemuSmokeBatch.Violet, Vector3.up * reach);
            _nmSmoke.Light(at, size * .5f, strength, NemuSmokeBatch.Violet, side.normalized * reach * 1.3f);
        }

        private void SampleVictim(NmVictim v, int index, float t)
        {
            Vector3 up = Vector3.up;
            Vector3 fwd = NmForward(v);
            float lag = .05f * index;

            // ---- THEY ACT FIRST: their shadow copies them, and its hands follow their head.
            NmAct(v, index, t);

            // Their real shadow leaves them the moment the ring reaches them (and they never get it back in the cutscene).
            bool off = t >= v.Woken;
            if (off != v.ShadowsOff)
            {
                v.ShadowsOff = off;
                foreach (var r in v.Renderers) if (r != null) r.shadowCastingMode = off ? ShadowCastingMode.Off : ShadowCastingMode.On;
            }

            // Their head, as it is now (it shakes and turns).
            Vector3 look = NmWDir(fwd), face, headMid;
            float headR;
            if (v.Head != null)
            {
                look = v.Head.rotation * v.FaceLocal; look.y = 0f;
                look = look.sqrMagnitude > 1e-4f ? look.normalized : NmWDir(fwd);
                face = v.Head.position + Vector3.up * (v.FaceTop * .28f) + look * (v.FaceDepth + .015f);
                headMid = face - look * v.FaceDepth;
                headR = Mathf.Max(v.FaceDepth, v.Width * .42f) + .03f;
            }
            else
            {
                face = NmW(v.Home + up * (v.Top * .78f) + fwd * (v.Width * .5f));
                headMid = face - look * .25f; headR = .3f;
            }

            float pour = Ease(NmPour + lag, NmPoured + lag, t);
            if (v.Shade != null) SampleShade(v, index, t, face, headMid, look, headR, pour);

            Vector3 chest = v.Home + up * (v.Top * .55f);
            // Smoke pulled into them as it pours in.
            if (pour > 0f && pour < 1f)
            {
                Vector3 side = Vector3.Cross(up, fwd);
                for (int j = 0; j < 9; j++)
                {
                    float a = (j * 40f + index * 17f) * Mathf.Deg2Rad;
                    Vector3 outDir = (side * Mathf.Cos(a) + up * Mathf.Sin(a) * .8f - fwd * .5f).normalized;
                    float reach = (1.2f - 1.0f * pour) * (.7f + .3f * Mathf.Repeat(j * .41f, 1f));
                    _nmSmoke.Wisp(NmW(chest + outDir * reach * .5f), NmWDir(outDir), .045f, reach, .7f * Mathf.Sin(pour * Mathf.PI),
                                  j * .07f + index * .2f, pour * .5f, 1f);
                }
            }
            // A flash at the chest as it is in.
            float flash = Flash(t, NmPoured + lag, .1f);
            if (flash > 0f) _nmSmoke.Light(NmW(chest), v.Width * 1.4f, 1.3f * flash, NemuSmokeBatch.Violet);

            // ---- WHAT IS LEFT: the aura (`NemuAura`, the same drawing play keeps), from the moment it is in.
            float auraOn = Ease(NmPoured + lag - .05f, NmPoured + lag + .3f, t);
            if (auraOn > 0f)
            {
                var state = new NemuAura.State
                {
                    Feet = NmW(v.Home), Forward = v.Holder.transform.forward, Stream = Vector3.zero, Top = v.Top, Width = v.Width, Clock = t,
                    Amount = auraOn, Swell = Flash(t, NmPoured + lag + .05f, .25f), Drag = 0f, Thin = 0f, Seat = v.Seat, Reduced = _reducedEffects,
                };
                NemuAura.Draw(_nmSmoke, state);
            }

            // ---- THEIR EYES open violet, with a glint.
            float eyes = Ease(NmEyesOpen + lag, NmEyesOpen + lag + .06f, t) * (1f + 1.3f * Flash(t, NmEyesOpen + lag + .05f, .12f));
            if (eyes > 0f)
            {
                NemuAura.Eyes(_nmSmoke, face, look, v.FaceTop * .32f, .1f, eyes * 1.9f, NmW(_nmEye));
                float glint = Flash(t, NmEyesOpen + lag + .04f, .1f);
                if (glint > 0f) Glint(face + look * .06f, .08f, 1.4f * glint, Vector3.Cross(Vector3.up, look), .5f * glint);
            }
        }

        /// <summary>
        /// Their shadow: on the court, copying them, until the ring reaches them; ink-black and twitching; peeling up, hinged at
        /// their feet, to stand behind them taller than them; its eyes opening; its arms stretching round their head and its hands
        /// over their eyes; then pouring into them.
        /// </summary>
        private void SampleShade(NmVictim v, int index, float t, Vector3 face, Vector3 headMid, Vector3 look, float headR, float pour)
        {
            var s = v.Shade;
            Vector3 up = Vector3.up;
            Vector3 fwd = NmForward(v);
            float lag = .05f * index;
            bool woken = t >= v.Woken;
            // Before the ring reaches them it is their real shadow that shows; this copy waits, hidden.
            bool shown = woken && pour < .999f;
            foreach (var r in s.Renderers) if (r != null) r.enabled = shown;
            if (!shown) return;

            // It moves the way they move (it is their shadow); its arms are its own once they reach.
            CopyPose(v.Root, s.Root); CopyPose(v.Torso, s.Torso); CopyPose(v.HeadBone, s.Head);
            CopyPose(v.ArmL, s.ArmL); CopyPose(v.ArmR, s.ArmR);

            // The twitch: stepped, like a frame skipping, from the ring's touch to the peel.
            float twitch = 0f;
            if (t < NmPeel + lag)
            {
                float beat = Mathf.Floor(t * 14f + index * 3f);
                twitch = (Mathf.Repeat(Mathf.Sin(beat * 12.9898f + index) * 43758.5f, 1f) - .5f) * 9f;
            }
            // The peel: hinged at their feet, slow at first, the head coming up last.
            float rise = Ease(NmPeel + lag, NmStood + lag, t);
            float angle = 90f * (1f - rise) + twitch * (1f - rise);
            // It stands behind them, over one shoulder, and pours in toward them.
            Vector3 aside = Vector3.Cross(up, fwd) * ((v.Seat & 1) == 0 ? 1f : -1f);
            Vector3 behind = v.Home + (-fwd * NmShadeBehind + aside * NmShadeAside) * (rise * (1f - pour));
            // (films r24, r25: at 2 and 6 cm up the shadow lying flat was under the court.)
            s.Hinge.localPosition = behind + up * Mathf.Lerp(.12f, .02f, rise);
            s.Hinge.localRotation = Quaternion.LookRotation(s.Lies, up) * Quaternion.Euler(angle, 0f, 0f);
            // Flat and as long as the sun makes it on the court; standing, a whole body, evenly bigger than them (film r22: left
            // flattened and stretched, the skew threw its arms wide of their eyes).
            float tall = Mathf.Lerp(s.Length, NmShadeTall, rise) * Mathf.Lerp(1f, .9f, pour);
            float wide = Mathf.Lerp(1f, NmShadeTall, rise) * Mathf.Lerp(1f, .9f, pour);
            float thick = Mathf.Lerp(.03f, NmShadeTall, rise * rise) * Mathf.Lerp(1f, .9f, pour);
            s.Flat.localScale = new Vector3(wide, tall, thick);

            // The skin: smoke flowing, and burning away as it pours in.
            s.Skin.SetFloat("_Clock", t);
            s.Skin.SetFloat("_Dissolve", pour);
            s.Skin.SetFloat("_Base", NmW(v.Home).y);
            s.Skin.SetFloat("_Span", v.Top * tall);
            // While it lies on the court it is a shadow: no rim. The rim comes as it rises.
            // Its edge only a little torn, so it keeps their outline: their hair, their shape (film r23: torn harder it was a
            // smoky lump, not them).
            s.Skin.SetFloat("_RimGain", Mathf.Lerp(0f, 1.4f, rise));
            s.Skin.SetFloat("_Tear", Mathf.Lerp(.15f, .38f, rise));

            // ---- ITS ARMS: round their head from behind, the hands over their eyes.
            if (t >= NmHandsIn + lag - .2f && v.Head != null)
            {
                float reach = Ease(NmHandsIn + lag, NmHandsShut + lag, t);
                Vector3 wSide = Vector3.Cross(up, look).normalized;
                Vector3 eyeLevel = new Vector3(headMid.x, face.y, headMid.z);
                for (int k = 0; k < 2; k++)
                {
                    float e = k == 0 ? -1f : 1f;
                    var arm = k == 0 ? s.ArmL : s.ArmR;
                    bool has = k == 0 ? s.HasPalmL : s.HasPalmR;
                    if (arm.T == null || !has) continue;
                    // From beside their ear (110 degrees off the face) round to over the eye (24).
                    float phi = Mathf.Lerp(110f, 24f, reach) * Mathf.Deg2Rad;
                    Vector3 radial = look * Mathf.Cos(phi) + wSide * (e * Mathf.Sin(phi));
                    Vector3 target = eyeLevel + radial * (headR + .02f);
                    // Lifted into place over a fifth of a second before it reaches.
                    float lift = Ease(NmHandsIn + lag - .2f, NmHandsIn + lag, t);
                    Reach(arm.T, k == 0 ? s.PalmL : s.PalmR, k == 0 ? s.AxisL : s.AxisR, target, lift);
                }
            }

            // ---- ITS EYES, violet slits, as it stands.
            if (s.Head.T != null)
            {
                float lit = Ease(NmStood + lag - .25f, NmStood + lag, t) * (1f - pour) * (1f + 1.2f * Flash(t, NmStood + lag - .1f, .12f));
                if (lit > .01f)
                {
                    Vector3 eyes = s.Head.T.TransformPoint(s.EyesLocal);
                    NemuAura.Eyes(_nmSmoke, eyes, NmWDir(fwd), v.FaceTop * .3f, .06f, lit * 1.6f, NmW(_nmEye));
                }
            }
        }

        private static void CopyPose(NmBone from, NmBone to)
        {
            if (from.T == null || to.T == null) return;
            to.T.localRotation = from.T.localRotation; to.T.localPosition = from.T.localPosition; to.T.localScale = to.Scale;
        }

        /// <summary>Turn an arm so its palm points at <paramref name="target"/> (world) and stretch it along its own length to
        /// reach it; <paramref name="weight"/> 0 leaves it as it was.</summary>
        private static void Reach(Transform arm, Vector3 palmLocal, int axis, Vector3 target, float weight)
        {
            if (weight <= 0f) return;
            for (int pass = 0; pass < 2; pass++)
            {
                Vector3 from = arm.position;
                Vector3 palm = arm.TransformPoint(palmLocal);
                Vector3 now = palm - from, want = target - from;
                if (now.sqrMagnitude < 1e-6f || want.sqrMagnitude < 1e-6f) return;
                var turn = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(now, want), weight);
                arm.rotation = turn * arm.rotation;
                float stretch = Mathf.Lerp(1f, want.magnitude / Mathf.Max(1e-3f, now.magnitude), weight);
                var scale = arm.localScale;
                scale[axis] *= Mathf.Clamp(stretch, .5f, 4f);
                // Thinner as it stretches: a shadow's arm, drawn out (film r22: at 0.7 they were slabs).
                float thin = Mathf.Lerp(1f, .4f, Mathf.Clamp01((stretch - 1f) * .8f));
                scale[(axis + 1) % 3] *= thin; scale[(axis + 2) % 3] *= thin;
                arm.localScale = scale;
                weight = 1f;
            }
        }

        // ------------------------------------------------------------------ their acting

        /// <summary>
        /// A player who does not know, then does. Their own bones, turned in the world from the pose they had when the
        /// cutscene began: breathing and a slow shift of weight; a shiver once their shadow stands; frozen as its hands come; a
        /// flinch as they shut, hands up at the face and the head shaking; limp as it pours in; then the possessed turn to the
        /// lens, head first, then the chest, then the body.
        /// </summary>
        private void NmAct(NmVictim v, int index, float t)
        {
            v.Root.Reset(); v.Torso.Reset(); v.HeadBone.Reset(); v.ArmL.Reset(); v.ArmR.Reset();
            float lag = .05f * index;
            float seat = v.Seat * 1.7f;

            // The turn to the lens: how far round, and how much of it the body, the chest and the head have taken.
            Vector3 fwd0 = NmWDir(NmForward(v));
            Vector3 toLens = NmW(_nmEye) - v.Holder.transform.position; toLens.y = 0f;
            float turn = toLens.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(fwd0, toLens, Vector3.up) : 0f;
            float headTurn = Ease(NmTurn + lag, NmTurn + lag + .25f, t);
            float chestTurn = Ease(NmTurn + lag + .1f, NmTurn + lag + .4f, t);
            float bodyTurn = Ease(NmTurn + lag + .2f, NmTurn + lag + .5f, t);
            float bodyYaw = turn * .5f * bodyTurn;
            v.Holder.transform.localRotation = Quaternion.Euler(0f, v.Yaw + bodyYaw, 0f);
            Vector3 up = Vector3.up;
            Vector3 fwd = v.Holder.transform.forward, right = v.Holder.transform.right;

            float handsComing = Ease(NmHandsIn + lag, NmHandsIn + lag + .2f, t);
            float shut = Ease(NmHandsShut + lag, NmHandsShut + lag + .08f, t);
            float limp = Ease(NmPour + lag + .1f, NmPoured + lag, t);
            float woken = Ease(NmEyesOpen + lag, NmEyesOpen + lag + .15f, t);
            float struggle = shut * (1f - Ease(NmStruggled + lag - .1f, NmStruggled + lag + .15f, t));

            // Unaware: breathing, a slow shift of weight. It stops as the hands come (they freeze).
            float calm = 1f - handsComing;
            float breathe = Mathf.Sin(t * 2.1f + seat) * 1.8f * calm;
            float shift = Mathf.Sin(t * 1.3f + seat * .7f) * 1.6f * calm;
            if (v.Root.T != null) v.Root.T.position += right * (Mathf.Sin(t * 1.3f + seat * .7f) * .02f * calm);
            // A shiver once their shadow stands behind them.
            float shiver = t >= NmStood + lag ? (1f - shut) * Mathf.Sin(t * 57f + seat) * .9f : 0f;
            float flinch = Mathf.Clamp01(1f - Mathf.Abs(t - (NmHandsShut + lag + .05f)) / .18f);
            float torsoPitch = breathe - flinch * 12f - struggle * 4f + limp * (1f - woken) * 8f;
            float torsoTwist = struggle * Mathf.Sin(t * 19f + seat) * 6f + (turn * .8f * chestTurn - bodyYaw);
            if (v.Torso.T != null)
            {
                Turn(v.Torso.T, up, torsoTwist);
                Turn(v.Torso.T, right, torsoPitch);
                Turn(v.Torso.T, fwd, shift + shiver);
            }

            float headYaw = struggle * Mathf.Sin(t * 25f + seat) * 16f + Mathf.Sin(t * .9f + seat) * 4f * calm
                          + (turn * headTurn - turn * .8f * chestTurn);
            // (film r20: drooping 22 degrees, the top of their hair filled the close lens.)
            float headPitch = -flinch * 16f + limp * (1f - woken) * 6f - woken * 4f;
            if (v.HeadBone.T != null)
            {
                Turn(v.HeadBone.T, up, headYaw);
                Turn(v.HeadBone.T, v.HeadBone.T.parent != null ? Vector3.Cross(up, v.HeadBone.T.parent.forward).normalized : right, headPitch);
                // The possessed tilt as they stare.
                Turn(v.HeadBone.T, fwd, woken * 10f * ((v.Seat & 1) == 0 ? 1f : -1f));
            }

            // Hands up at their face while they fight it, falling as they go limp.
            float armsUp = Mathf.Max(flinch, struggle) * (1f - limp) * 105f;
            float armsOut = calm * Mathf.Sin(t * 1.7f + seat) * 2f;
            if (v.ArmL.T != null) { Turn(v.ArmL.T, right, -armsUp); Turn(v.ArmL.T, fwd, armsOut + struggle * 10f); }
            if (v.ArmR.T != null) { Turn(v.ArmR.T, right, -armsUp * .92f); Turn(v.ArmR.T, fwd, -armsOut - struggle * 10f); }
        }

        private static void Turn(Transform bone, Vector3 axis, float degrees)
        {
            if (Mathf.Abs(degrees) < .01f) return;
            bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;
        }

        // ------------------------------------------------------------------ camera and grade

        private struct NmKey
        {
            public float T, Fov;
            public Vector3 Eye, Look;
            public NmKey(float t, Vector3 eye, Vector3 look, float fov) { T = t; Eye = eye; Look = look; Fov = fov; }
        }

        /// <summary>
        /// One continuous move through keys: a cubic through each (Hermite on their own times), still at its first and last
        /// key and wherever two keys hold the same place, so speed eases in and out instead of v4's straight lines between
        /// corners.
        /// </summary>
        private static void NmPath(NmKey[] keys, float t, out Vector3 eye, out Vector3 look, out float fov)
        {
            int n = keys.Length;
            if (t <= keys[0].T) { eye = keys[0].Eye; look = keys[0].Look; fov = keys[0].Fov; return; }
            if (t >= keys[n - 1].T) { eye = keys[n - 1].Eye; look = keys[n - 1].Look; fov = keys[n - 1].Fov; return; }
            int i = 0;
            while (i < n - 2 && t > keys[i + 1].T) i++;
            var a = keys[i]; var b = keys[i + 1];
            float span = Mathf.Max(1e-4f, b.T - a.T);
            float u = (t - a.T) / span;
            Vector3 Tan(int k, System.Func<NmKey, Vector3> f)
            {
                if (k <= 0 || k >= n - 1) return Vector3.zero;
                if ((f(keys[k + 1]) - f(keys[k])).sqrMagnitude < 1e-6f || (f(keys[k]) - f(keys[k - 1])).sqrMagnitude < 1e-6f) return Vector3.zero;
                return (f(keys[k + 1]) - f(keys[k - 1])) / Mathf.Max(1e-4f, keys[k + 1].T - keys[k - 1].T) * span;
            }
            Vector3 Herm(Vector3 p0, Vector3 m0, Vector3 p1, Vector3 m1)
            {
                float u2 = u * u, u3 = u2 * u;
                return (2 * u3 - 3 * u2 + 1) * p0 + (u3 - 2 * u2 + u) * m0 + (-2 * u3 + 3 * u2) * p1 + (u3 - u2) * m1;
            }
            eye = Herm(a.Eye, Tan(i, k => k.Eye), b.Eye, Tan(i + 1, k => k.Eye));
            look = Herm(a.Look, Tan(i, k => k.Look), b.Look, Tan(i + 1, k => k.Look));
            fov = Mathf.Lerp(a.Fov, b.Fov, u * u * (3f - 2f * u));
        }

        /// <summary>
        /// Three shots, computed from her face and from where the players really are. S1 her eyes; S2 from beside her, the
        /// court in daylight, her in the near corner and every player and their shadow in frame (the effect in the same frame
        /// as its cause); S3 one eased path in front of one player, in to their face for the hands, back out to hold everyone.
        /// </summary>
        private void NemuFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            Vector3 up = Vector3.up;
            if (t < NmWide && _head != null)
            {
                // S1 HER: low and close under her fringe, a slow push in as her eyes ignite.
                Vector3 eyes = _root.transform.InverseTransformPoint(_head.TransformPoint(_nmEyesLocal));
                Vector3 face = _root.transform.InverseTransformDirection(_head.rotation * _nmFaceLocal); face.y = 0f;
                face = face.sqrMagnitude > 1e-4f ? face.normalized : Vector3.forward;
                Vector3 across = Vector3.Cross(up, face);
                float u = Ease(0f, NmWide, t);
                // Her whole head and shoulders, so she reads as a face (owner on v6: at half a metre it was a wall of blocks).
                eye = eyes + face * Mathf.Lerp(1.6f, 1.3f, u) - up * Mathf.Lerp(.12f, .08f, u) + across * .15f;
                look = eyes + up * .06f;
                fov = Mathf.Lerp(32f, 29f, u);
            }
            else if (_nmVictims.Count > 0)
            {
                Vector3 way = _nmWay, right = Vector3.Cross(up, way);
                var first = _nmVictims[0];
                Vector3 f0 = NmForward(first), s0 = Vector3.Cross(up, f0);
                Vector3 face = first.FaceAt;
                // Backed off until every player (and the shadow standing behind them) fits across the frame at their own depth.
                float back0 = 4.3f;
                foreach (var v in _nmVictims)
                {
                    Vector3 off = v.Home - _nmCentre;
                    back0 = Mathf.Max(back0, (Mathf.Abs(Vector3.Dot(off, right)) + 1.2f) / .66f - Vector3.Dot(off, way));
                }
                if (t < NmClose)
                {
                    // S2 THE COURT, over her shoulder and above: she is in the near corner as the ring leaves her feet (the
                    // cause in the same frame as the effect); the shadows on the court read from up here, and as they stand they
                    // loom over the players' shoulders toward us (films r22, r23: from further off they were specks, and she
                    // was out of the frame).
                    float u = Ease(NmWide, NmClose, t);
                    Vector3 from = -way * 1.7f + right * 1.1f + up * 2.9f;
                    eye = Vector3.Lerp(from, from + way * .9f - up * .5f, u);
                    // (film r27: centred on the players' middle, the left one was cut by the frame's edge.)
                    look = _nmCentre - right * .7f + up * Mathf.Lerp(.3f, 1.1f, u);
                    fov = Mathf.Lerp(60f, 54f, u);
                }
                else
                {
                    // S3 TAKEN: one path.
                    float faceUp = face.y - first.Home.y;
                    Vector3 wide = _nmCentre - way * back0 + right * 2.6f + up * 3.1f;
                    Vector3 lookWide = _nmCentre + up * .9f;
                    Vector3 lookNear = first.Home - f0 * .25f + up * (first.Top * 1.05f);
                    Vector3 lookFace = face + up * .1f;
                    var keys = new[]
                    {
                        new NmKey(NmClose, first.Home + f0 * 2.9f + s0 * .5f + up * (faceUp + .25f), lookNear, 50f),
                        new NmKey(NmHandsIn, first.Home + f0 * 2.6f + s0 * .45f + up * (faceUp + .2f), lookNear, 48f),
                        new NmKey(NmHandsShut, face + f0 * 1.5f + s0 * .16f + up * .06f, lookFace, 40f),
                        new NmKey(NmEyesOpen, face + f0 * 1.35f + s0 * .1f + up * .04f, face + up * .08f, 39f),
                        new NmKey(NmPullBack, face + f0 * 1.35f + s0 * .1f + up * .04f, face + up * .08f, 39f),
                        new NmKey(Seconds, wide, lookWide, 50f),
                    };
                    NmPath(keys, t, out eye, out look, out fov);
                    // One low push of the lens as the hands shut.
                    if (!_reducedEffects)
                    {
                        float push = Mathf.Clamp01((t - NmHandsShut) / .06f) * (1f - Ease(NmHandsShut + .06f, NmHandsShut + .4f, t));
                        eye += (lookFace - eye).normalized * (.14f * push);
                    }
                }
                eye = KeepLensOffOthers(eye, t >= NmHandsIn && t < NmPullBack + .2f ? 1.2f : 1.5f);
                // Never inside her.
                Vector3 flat = new Vector3(eye.x, 0f, eye.z);
                if (flat.magnitude < .9f) eye += (flat.sqrMagnitude > 1e-4f ? flat.normalized : -way) * (.9f - flat.magnitude);
            }
            _nmEye = eye; _nmLook = look;
        }

        /// <summary>
        /// The ink (`Shaders/NemuInk`): the frame's edges sink a little into near-black from her eyes on; one impact frame as the
        /// hands shut; focus lines as her eyes ignite, as the hands shut and as they stare. Lets go as play returns.
        /// </summary>
        private void NemuPostProcess(RenderTexture frame, Camera camera, float t)
        {
            float ink = Ease(NmIgnite, NmIgnite + .3f, t) * .6f * (1f - Ease(Seconds - .6f, Seconds, t));
            float impact = !_reducedEffects && t >= NmHandsShut && t < NmHandsShut + NmImpact ? 1f : 0f;
            bool invert = t < NmHandsShut + NmImpact * .5f;
            float lines = _reducedEffects ? 0f : Mathf.Max(Mathf.Max(
                Flash(t, NmIgnite + .1f, .22f),
                Flash(t, NmHandsShut + .1f, .3f)),
                .6f * Flash(t, NmTurn + .2f, .3f));
            if (ink <= .01f && impact <= 0f && lines <= .01f) return;
            if (_nmInk == null)
            {
                var shader = Resources.Load<Shader>("Shaders/NemuInk");
                if (shader == null) return;
                _nmInk = new Material(shader) { name = "NemuInk" };
            }
            _nmInk.SetFloat("_Ink", ink);
            _nmInk.SetFloat("_Black", 0f);
            _nmInk.SetFloat("_Night", 0f);
            _nmInk.SetFloat("_Impact", impact);
            _nmInk.SetFloat("_Invert", impact > 0f && invert ? 1f : 0f);
            _nmInk.SetFloat("_Lines", lines);
            _nmInk.SetVector("_Focus", new Vector4(.5f, .55f, 0f, 0f));
            _nmInk.SetFloat("_Clock", t);
            _nmInk.SetFloat("_Aspect", frame.height > 0 ? frame.width / (float)frame.height : 16f / 9f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _nmInk);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>Daylight stays (v5's black night hid everything); the world only steps back a little so the violet reads.</summary>
        private void NemuGrade(float t, out float brightness, out float saturation)
        {
            float dim = Ease(NmIgnite, NmWide + .3f, t) * (1f - Ease(Seconds - .6f, Seconds, t));
            brightness = Mathf.Lerp(1f, .78f, dim);
            saturation = Mathf.Lerp(1f, .85f, dim);
        }

        private void NemuDispose()
        {
            _nmSmoke?.Dispose(); _nmSmoke = null;
            if (_nmInk != null) { ObjectDestroy(_nmInk); _nmInk = null; }
        }
    }
}
