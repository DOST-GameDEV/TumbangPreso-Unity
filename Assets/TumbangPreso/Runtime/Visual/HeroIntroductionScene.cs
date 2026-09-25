using System;
using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Private scene authoring, driven explicitly by presentation time. No ability,
    // physics, score, input or global-clock callbacks. Activation in a live match
    // still requires the accepted shared phase; only opt-in art probes use this now.
    //
    // ⚠️⚠️ REFINE-2.11 (owner 2026-09-24): "give every hero a memorable moment in which the stage
    // briefly feels like theirs", authored one hero at a time, "dont js spam copy paste stuff bcz
    // it will be boring". THIS FILE IS ONLY THE PLUMBING every hero needs: pieces, the stage wall,
    // the authored shots, the voice clock and capture visibility. Each hero's stage, props and
    // timing live in their own partial file (`HeroIntroductionScene.<Hero>.cs`), written from
    // their own section of `docs/reports/ultimate-performances-2026-09-24/plan.md`, and share no
    // shapes, colours or beats. The body keys and shots come from `UltimatePerformance`.
    //
    // ⚠️ EVERYTHING HERE EXISTS ONLY IN THE OVERLAY'S RENDER COPY. The stage is forced off except
    // inside `UltimatePhaseView`'s own camera render, so the live court never changes and nobody's
    // aim or information is affected by it.
    public sealed partial class HeroIntroductionScene : IDisposable
    {
        private sealed class Piece
        {
            public Transform Transform;
            public Renderer Renderer;
            public Color Color;
            public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        }
        private readonly GameObject _root;
        private readonly string _hero;
        private readonly Vector3 _ground;
        private readonly Quaternion _facing;
        private readonly List<Piece> _pieces = new List<Piece>();
        private readonly List<LineRenderer> _lines = new List<LineRenderer>();
        private readonly Transform _rightHand, _leftArm, _head;
        private readonly Vector3 _leftPalm;
        private Renderer[] _renderers;
        private readonly Renderer[] _bodyRenderers;
        private GameObject _heldItem;
        private Renderer[] _heldRenderers;
        private AudioSource _sound, _voice;
        private readonly UltimatePerformance _performance;
        private readonly bool _reducedEffects;
        public GameObject Root => _root;
        public UltimatePerformance Performance => _performance;
        public float Seconds => _performance?.Seconds ?? UltimatePerformance.DefaultSeconds;
        /// <summary>True once this introduction has spoken the hero's own line inside the gesture.</summary>
        public bool VoicePlayed { get; private set; }

        public HeroIntroductionScene(Transform parent, string hero, CharacterMotor source, MatchPoseHistory.Copy body)
        {
            _bodyRenderers = body.Root.GetComponentsInChildren<Renderer>(true);
            foreach (var bone in body.Bones)
            {
                if (bone.name == "HandAnchor") _rightHand = bone;
                if (bone.name == "arm-left") _leftArm = bone;
                if (bone.name == "head") _head = bone;
            }
            if (_rightHand != null)
            { _leftPalm = _rightHand.localPosition; _leftPalm.x = -_leftPalm.x; }
            // Outfits can be asymmetric. Use this rig's real free-hand surface,
            // with the mirrored point only as the same missing-rig fallback.
            foreach (var skin in body.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int left = Array.IndexOf(skin.bones, _leftArm);
                if (left >= 0 && CharacterVisual.PalmCentre(skin, left, out var palm))
                { _leftPalm = palm; break; }
            }
            _hero = hero; _ground = VfxShapes.GroundPoint(source.transform.position);
            _facing = Quaternion.Euler(0, source.transform.eulerAngles.y, 0);
            _performance = UltimatePerformance.For(hero, source.GetComponent<Carrier>()?.Held != null);
            _reducedEffects = Settings.SettingsStore.Current.ReducedEffects;
            _root = new GameObject("IntroductionScene-" + hero);
            _root.transform.SetParent(parent, false); _root.transform.SetPositionAndRotation(_ground, _facing);
            try
            {
                CopyHeldItem(source);
                switch (hero)
                {
                    case "sean": BuildSean(); break;
                    case "phaister": BuildPhaister(); break;
                    case "zack": BuildZack(); break;
                    case "nemu": BuildNemu(source); break;
                    case "dante": BuildDante(); break;
                    case "cheska": BuildCheska(); break;
                    case "rafi": BuildRafi(); break;
                    // ⚠️ No stage yet: her ultimate is a placeholder, and an introduction is
                    // staged around the ultimate. Listed so a request for her never throws.
                    case "amihan": break;
                    default: throw new ArgumentOutOfRangeException(nameof(hero));
                }
                _renderers = _root.GetComponentsInChildren<Renderer>(true);
                SetVisibleForCapture(false);
                Sample(0);
            }
            catch { Dispose(); throw; }
        }

        // ------------------------------------------------------------------ shared plumbing

        private Vector3 RightPalm => _rightHand != null ? _root.transform.InverseTransformPoint(_rightHand.position) : new Vector3(-.4f, 1.2f, .3f);
        private Vector3 BothPalms => _rightHand != null && _leftArm != null
            ? _root.transform.InverseTransformPoint((_rightHand.position + _leftArm.TransformPoint(_leftPalm)) * .5f)
            : new Vector3(0, .7f, .5f);
        private Vector3 HeadPoint => _head != null ? _root.transform.InverseTransformPoint(_head.position) + Vector3.up * .35f : new Vector3(0, 1.6f, 0);
        private float LiftAt(float t) => _performance?.LiftAt(t) ?? 0;
        private Vector3 FreePalm => _leftArm != null ? _root.transform.InverseTransformPoint(_leftArm.TransformPoint(_leftPalm)) : BothPalms;

        private void CopyHeldItem(CharacterMotor actor)
        {
            var held = actor.GetComponent<Carrier>()?.Held;
            var liveHand = actor.GetComponent<CharacterVisual>()?.HandAnchor;
            if (held == null || liveHand == null || _rightHand == null) return;
            // Preserve the actual selected shoe and fitted grip from the live rig.
            // Only mesh/material data is copied, never Slipper/physics/ownership.
            _heldItem = new GameObject("IntroductionHeldSlipper");
            _heldItem.transform.SetParent(_rightHand, false);
            var surfaces = new List<Renderer>();
            foreach (var source in held.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!source.enabled || source.GetComponent<VfxRenderTag>() != null) continue;
                bool activePart = true;
                for (var part = source.transform; part != held.transform; part = part.parent)
                    if (!part.gameObject.activeSelf) { activePart = false; break; }
                if (!activePart) continue;
                var mesh = source.GetComponent<MeshFilter>();
                if (mesh == null || mesh.sharedMesh == null) continue;
                var go = new GameObject("HeldShoeSurface"); go.transform.SetParent(_heldItem.transform, false);
                go.transform.localPosition = liveHand.InverseTransformPoint(source.transform.position);
                go.transform.localRotation = Quaternion.Inverse(liveHand.rotation) * source.transform.rotation;
                Vector3 scale = source.transform.lossyScale, parentScale = liveHand.lossyScale;
                go.transform.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
                go.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var copy = go.AddComponent<MeshRenderer>(); copy.sharedMaterials = source.sharedMaterials;
                var properties = new MaterialPropertyBlock(); source.GetPropertyBlock(properties); copy.SetPropertyBlock(properties);
                copy.shadowCastingMode = ShadowCastingMode.On; copy.forceRenderingOff = true;
                surfaces.Add(copy);
            }
            _heldRenderers = surfaces.ToArray();
        }

        /// <summary>A translucent, glowing piece. Returns its index for <see cref="Place"/>.</summary>
        private int Add(string name, Mesh mesh, Color color, float emission = .32f, bool plain = false)
        {
            var go = VfxShapes.Stand(_root.transform, name, mesh, 1);
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            // Rafi's own water uses his water shader; his sky and land (`plain`) do not.
            if (_hero == "rafi" && !plain) RafiWaterVisual.Paint(renderer, color);
            else VfxMaterial.Ghost(renderer, color, emission);
            _pieces.Add(new Piece { Transform = go.transform, Renderer = renderer, Color = color });
            return _pieces.Count - 1;
        }

        /// <summary>An opaque, lit piece: stone and ice that must read as objects, not light.</summary>
        private int AddSolid(string name, Mesh mesh, Color color)
        {
            var go = VfxShapes.Stand(_root.transform, name, mesh, 1);
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.On;
            VfxMaterial.Solid(renderer, color);
            _pieces.Add(new Piece { Transform = go.transform, Renderer = renderer, Color = color });
            return _pieces.Count - 1;
        }

        /// <summary>
        /// ⚠️ THE STAGE WALL. A cylinder round the hero, open at the top, seen from inside, so every
        /// authored shot (front, side, over the shoulder, from below) has THEIR world behind them
        /// instead of the court. Radius 8 m keeps every intro camera (3 to 6 m out) inside it.
        /// Translucent on purpose: the court recedes behind it rather than vanishing, which is
        /// Sepak U's rule that the summoned world sits BEHIND the action (research.md § 1).
        /// Each hero builds its own bands from this, in its own colours; nobody shares a wall.
        /// </summary>
        private int Wall(string name, float bottom, float top, Color color, float radius = 8f, int sides = 28, float emission = .25f, bool cap = false)
        {
            int index = Add(name, WallMesh(sides, cap), color, emission, plain: true);
            var p = _pieces[index];
            p.Transform.localPosition = new Vector3(0, bottom, 0);
            p.Transform.localScale = new Vector3(radius, top - bottom, radius);
            return index;
        }

        /// <summary>
        /// A cylinder wall of unit radius and height. `cap` closes the top: the stage sketch showed
        /// every low shot looking up through the open top into the bright real sky, which reads
        /// as a hole in the night (`previews/phaister_v7_stage.png`).
        /// </summary>
        private static Mesh WallMesh(int sides, bool cap = false)
        {
            var mesh = new Mesh { name = "Introduction stage wall" };
            var vertices = new List<Vector3>((sides + 1) * 2 + 1);
            var triangles = new List<int>(sides * 18);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                var rim = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                vertices.Add(rim); vertices.Add(rim + Vector3.up);
                if (i == sides) continue;
                int n = i * 2;
                triangles.AddRange(new[] { n, n + 1, n + 2, n + 2, n + 1, n + 3 });
            }
            if (cap)
            {
                int centre = vertices.Count; vertices.Add(Vector3.up);
                for (int i = 0; i < sides; i++)
                {
                    int a = i * 2 + 1, b = i * 2 + 3;
                    triangles.AddRange(new[] { centre, a, b });
                }
            }
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            // Opposite faces must not share normals. Their cancellation made
            // these large lit walls black even after TwoSided's other users
            // were fixed. Reuse its separated front/back vertices here too.
            return VfxShapes.TwoSided(mesh);
        }

        private void Place(int index, Vector3 position, Vector3 scale, Quaternion rotation, float opacity)
        {
            var p = _pieces[index]; p.Transform.localPosition = position;
            p.Transform.localScale = scale; p.Transform.localRotation = rotation;
            Tint(index, opacity);
        }

        private void Tint(int index, float opacity, Color? colour = null)
        {
            var p = _pieces[index];
            Color color = colour ?? p.Color; color.a *= Mathf.Clamp01(opacity);
            p.Block.SetColor("_Color", color); p.Block.SetColor("_BaseColor", color);
            p.Renderer.SetPropertyBlock(p.Block);
            p.Renderer.enabled = opacity > .002f;
        }

        private LineRenderer Line(string name, int points, float width, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(_root.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false;
            line.positionCount = points; line.widthMultiplier = width; line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            VfxMaterial.Ghost(line, color, .65f);
            _lines.Add(line);
            return line;
        }

        private static float Ease(float from, float to, float time)
        { float u = Mathf.InverseLerp(from, to, time); return u * u * (3 - 2 * u); }

        /// <summary>0 before the stage arrives, 1 while it holds, 0 again as play returns.</summary>
        private float StagePresence(float t, float arrive = .35f)
            => Ease(0, arrive, t) * (1 - Ease(Seconds - .45f, Seconds - .05f, t));

        /// <summary>A flash is one bright moment. Reduced effects keeps the shape and drops the flash.</summary>
        private float Flash(float t, float at, float width = .08f)
            => _reducedEffects ? 0 : Mathf.Clamp01(1 - Mathf.Abs(t - at) / width);

        public bool StartSound()
        {
            if (_sound != null) return true;
            string cue = Abilities.HeroAbilitySystem.ThemeFor(_hero);
            var clip = !string.IsNullOrEmpty(cue) ? Resources.Load<AudioClip>("Sfx/" + Audio.AudioCues.FileStemFor(cue)) : null;
            if (clip == null) return false;
            _sound = _root.AddComponent<AudioSource>(); _sound.playOnAwake = false;
            _sound.spatialBlend = 0; _sound.loop = false; _sound.clip = clip;
            _sound.volume = 0; _sound.Play();
            return true;
        }

        /// <summary>
        /// ⚠️ THE HERO'S OWN LINE PLAYS INSIDE THE GESTURE IT DESCRIBES (Phaister's laugh while she
        /// laughs), at the authored `voice` time, once. Overwatch's ultimate lines are the model
        /// (research.md § 1): the voice is part of the performance, not a stinger afterwards. The
        /// live ability skips its copy when this one played (`HeroAbility.IntroductionVoiced`).
        /// </summary>
        private void SampleVoice(float t)
        {
            if (VoicePlayed || _performance == null || _performance.VoiceAt < 0 || t < _performance.VoiceAt) return;
            VoicePlayed = true;
            var clip = Resources.Load<AudioClip>("Sfx/" + Audio.AudioCues.FileStemFor(_performance.VoiceCue));
            if (clip == null) { VoicePlayed = false; return; }
            _voice = _root.AddComponent<AudioSource>(); _voice.playOnAwake = false; _voice.spatialBlend = 0;
            _voice.clip = clip; _voice.volume = Settings.SettingsStore.Current.SfxGain * .85f; _voice.Play();
        }

        /// <summary>
        /// The shared boundary when this hero is one of a cohort. A shorter performance holds its
        /// last full moment and only plays its exit as the LONGEST caster's ends, so no stage
        /// vanishes while the overlay is still up.
        /// </summary>
        public float Boundary { get; set; } = -1;

        private float Local(float seconds)
        {
            float t = Mathf.Max(0, seconds);
            if (Boundary <= Seconds + .001f) return Mathf.Min(t, Seconds);
            float holdFrom = Seconds - .45f;
            if (t <= holdFrom) return t;
            if (t >= Boundary - .45f) return Mathf.Min(Seconds, Seconds - (Boundary - t));
            return holdFrom;
        }

        /// <summary>
        /// Pose the stage at this moment. `audible` false is for judging shots ahead of time: it
        /// must never fire the voice line early.
        /// </summary>
        public void Sample(float seconds, bool audible = true)
        {
            float t = Local(seconds);
            float leave = 1 - Ease(Seconds - .42f, Seconds, t);
            if (_sound != null) _sound.volume = Settings.SettingsStore.Current.SfxGain * .60f * Ease(0, .15f, t) * leave;
            if (audible) SampleVoice(t);
            switch (_hero)
            {
                case "sean": SampleSean(t); break;
                case "phaister": SamplePhaister(t); break;
                case "zack": SampleZack(t); break;
                case "nemu": SampleNemu(t); break;
                case "dante": SampleDante(t); break;
                case "cheska": SampleCheska(t); break;
                case "rafi": SampleRafi(t); break;
            }
        }

        // ------------------------------------------------------------------ shots

        public int ShotCount => _performance?.Shots.Count ?? 0;
        public int ShotIndexAt(float seconds) => ShotCount > 0 ? _performance.ShotIndexAt(seconds) : -1;
        public bool IsCloseShot(float seconds) { int i = ShotIndexAt(seconds); return i >= 0 && _performance.Shots[i].Close; }
        public float ShotStart(int index) => _performance.Shots[index].Start;
        public float ShotEnd(int index) => _performance.Shots[index].End;

        /// <summary>The authored shot covering this moment, in world space. Convenience for probes.</summary>
        public void Shot(float seconds, out Vector3 position, out Vector3 focus, out float fov, float aspect = 16f / 9)
            => ShotAt(ShotIndexAt(seconds), seconds, out position, out focus, out fov, aspect);

        public void ShotAt(int index, float seconds, out Vector3 position, out Vector3 focus, out float fov, float aspect = 16f / 9)
        {
            if (index < 0)
            {
                position = _ground + _facing * new Vector3(2.2f, 1.1f, 4.5f); focus = _ground + _facing * (Vector3.up * 1.05f); fov = 46; return;
            }
            _performance.Shot(index, seconds, out var eye, out var look, out fov);
            position = _ground + _facing * eye; focus = _ground + _facing * look;
            if (_performance.Shots[index].Fit) FitBodies(ref position, ref focus, fov, aspect, seconds);
        }

        /// <summary>The single locked shot for reduced motion: no cut and no camera move.</summary>
        public void StillShot(out Vector3 position, out Vector3 focus, out float fov)
        {
            if (_performance != null && _performance.HasStill)
            { position = _ground + _facing * _performance.StillEye; focus = _ground + _facing * _performance.StillLook; fov = _performance.StillFov; return; }
            ShotAt(ShotCount - 1, Seconds, out position, out focus, out fov);
        }

        /// <summary>
        /// Keep every body in frame (Nemu with the growing Kuro), keeping the lens constant and
        /// backing the camera out along its own line. Retained from the previous Nemu reveal.
        /// </summary>
        private void FitBodies(ref Vector3 position, ref Vector3 focus, float fov, float aspect, float seconds)
        {
            if (!TryCharacterBounds(out var bounds)) return;
            Vector3 backward = (position - focus).normalized;
            float distance = Vector3.Distance(position, focus);
            focus = Vector3.Lerp(focus, bounds.center, Ease(ShotStart(ShotIndexAt(seconds)), ShotEnd(ShotIndexAt(seconds)), seconds));
            var inverse = Quaternion.Inverse(Quaternion.LookRotation(-backward, Vector3.up));
            // .78, not .82: the 4:3 framing check missed by 0.003 of the frame (NemuKeeps...Aspect).
            float vertical = Mathf.Tan(fov * Mathf.Deg2Rad * .5f) * .78f;
            float horizontal = vertical * Mathf.Max(.5f, aspect);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 at = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                Vector3 view = inverse * (at - focus);
                distance = Mathf.Max(distance, Mathf.Max(Mathf.Abs(view.x) / horizontal - view.z + .1f,
                    Mathf.Abs(view.y) / vertical - view.z + .1f));
            }
            position = focus + backward * distance;
        }

        public bool TryCharacterBounds(out Bounds bounds)
        {
            bounds = default; bool found = false; Bounds result = default;
            void Include(Renderer[] renderers)
            {
                if (renderers == null) return;
                foreach (var renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.name == "KuroEyeWisp") continue;
                    if (!found) { result = renderer.bounds; found = true; } else result.Encapsulate(renderer.bounds);
                }
            }
            Include(_bodyRenderers); Include(_kuroRenderers); Include(_heldRenderers); bounds = result;
            return found;
        }

        public void SetVisibleForCapture(bool visible)
        {
            if (_renderers != null) foreach (var r in _renderers) if (r != null) r.forceRenderingOff = !visible;
            if (_heldRenderers != null) foreach (var r in _heldRenderers) if (r != null) r.forceRenderingOff = !visible;
        }

        public void Dispose()
        {
            if (_sound != null) _sound.Stop();
            if (_voice != null) _voice.Stop();
            _rage?.Dispose(); _rage = null;
            if (_heldItem != null) { _heldItem.SetActive(false); ObjectDestroy(_heldItem); }
            if (_root != null) { _root.SetActive(false); ObjectDestroy(_root); }
        }
        private static void ObjectDestroy(UnityEngine.Object value)
        { if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
    }
}
