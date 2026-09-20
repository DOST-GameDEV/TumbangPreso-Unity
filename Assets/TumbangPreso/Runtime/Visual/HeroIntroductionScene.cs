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
    public sealed class HeroIntroductionScene : IDisposable
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
        private MatchPoseHistory.Copy _kuro;
        private KuroRagePresentation _rage;
        private Vector3 _kuroScale;
        private Renderer[] _renderers;
        public GameObject Root => _root;

        public HeroIntroductionScene(Transform parent, string hero, CharacterMotor source)
        {
            _hero = hero; _ground = VfxShapes.GroundPoint(source.transform.position);
            _facing = Quaternion.Euler(0, source.transform.eulerAngles.y, 0);
            _root = new GameObject("IntroductionScene-" + hero);
            _root.transform.SetParent(parent, false); _root.transform.SetPositionAndRotation(_ground, _facing);
            try
            {
                switch (hero)
                {
                    case "sean":
                        for (int i = 0; i < 6; i++)
                            Add("InwardHeat" + i, VfxShapes.Tongue(5, .24f, .15f, .35f, .08f, 240 + i),
                                new Color(1, i % 2 == 0 ? .32f : .58f, .04f, .68f));
                        break;
                    case "phaister":
                        Add("EclipseBody", VfxShapes.TwoSided(VfxShapes.Splat(40, 0, 7)), new Color(.035f, .012f, .075f, .96f));
                        Add("EclipseRim", VfxShapes.Collar(40, .025f, .92f), new Color(.63f, .23f, .86f, .8f));
                        break;
                    case "zack":
                        for (int i = 0; i < 3; i++)
                        {
                            var go = new GameObject("FineCharge" + i); go.transform.SetParent(_root.transform, false);
                            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false;
                            line.positionCount = 7; line.widthMultiplier = .018f; line.numCapVertices = 2;
                            line.shadowCastingMode = ShadowCastingMode.Off;
                            VfxMaterial.Ghost(line, new Color(1, .78f, .17f, .85f), .65f);
                            _lines.Add(line);
                        }
                        break;
                    case "nemu":
                        var companion = source.GetComponent<CharacterVisual>()?.Companion;
                        if (companion == null) throw new InvalidOperationException("Nemu's introduction requires retained Kuro.");
                        var track = new MatchPoseHistory.Track(source, companion.gameObject);
                        track.Record(0); track.Record(.05f); _kuro = track.Clone(_root.transform);
                        if (_kuro == null) throw new InvalidOperationException("Kuro exceeded the render-copy contract.");
                        track.Apply(_kuro, .05f); _kuro.Root.SetActive(true);
                        _kuro.Root.transform.localPosition = new Vector3(-.95f, .65f, .15f);
                        _kuro.Root.transform.localRotation = Quaternion.Euler(0, -18, 0);
                        _kuroScale = _kuro.Root.transform.localScale;
                        var calm = GhostPetCompanion.FindForm(_kuro.Root.transform, "CalmForm");
                        var rage = GhostPetCompanion.FindForm(_kuro.Root.transform, "RageForm");
                        if (calm == null || rage == null) throw new InvalidOperationException("Retained Kuro calm/rage forms are missing.");
                        _rage = new KuroRagePresentation(_kuro.Root, calm, rage);
                        break;
                    case "dante":
                        for (int i = 0; i < 3; i++)
                            Add("LoadedGround" + i, VfxShapes.Prism(5, .10f, .86f), new Color(.26f, .22f, .15f, .92f));
                        break;
                    case "cheska":
                        for (int i = 0; i < 3; i++)
                            Add("GatheredIce" + i, VfxShapes.Crystal(6, i * .12f), new Color(.46f, .86f, .94f, .62f));
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(hero));
                }
                _renderers = _root.GetComponentsInChildren<Renderer>(true);
                SetVisibleForCapture(false);
                Sample(0);
            }
            catch { Dispose(); throw; }
        }

        private void Add(string name, Mesh mesh, Color color)
        {
            var go = VfxShapes.Stand(_root.transform, name, mesh, 1);
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            VfxMaterial.Ghost(renderer, color, .32f);
            _pieces.Add(new Piece { Transform = go.transform, Renderer = renderer, Color = color });
        }
        private void Place(int index, Vector3 position, Vector3 scale, Quaternion rotation, float opacity)
        {
            var p = _pieces[index]; p.Transform.localPosition = position;
            p.Transform.localScale = scale; p.Transform.localRotation = rotation;
            Color color = p.Color; color.a *= Mathf.Clamp01(opacity);
            p.Block.SetColor("_Color", color); p.Block.SetColor("_BaseColor", color);
            p.Renderer.SetPropertyBlock(p.Block);
        }
        private static float Ease(float from, float to, float time)
        { float u = Mathf.InverseLerp(from, to, time); return u * u * (3 - 2 * u); }

        public void Sample(float seconds)
        {
            float t = Mathf.Clamp(seconds, 0, HeroAbilityClips.IntroductionSeconds);
            float enter = Ease(.15f, .65f, t), leave = 1 - Ease(2.38f, 2.8f, t);
            switch (_hero)
            {
                case "sean":
                    float gather = Ease(.65f, 2.15f, t);
                    for (int i = 0; i < _pieces.Count; i++)
                    {
                        float a = i * Mathf.PI / 3 + t * .65f;
                        float radius = Mathf.Lerp(.85f, .27f, gather);
                        Place(i, new Vector3(Mathf.Cos(a) * radius, .08f + gather * .55f, Mathf.Sin(a) * radius),
                            new Vector3(.33f, Mathf.Lerp(.5f, .25f, gather), .33f) * enter,
                            Quaternion.Euler(-18, -a * Mathf.Rad2Deg, 0), enter * leave);
                    }
                    break;
                case "phaister":
                    float moon = Mathf.Lerp(.08f, .55f, Ease(.35f, 1.75f, t));
                    for (int i = 0; i < 2; i++)
                        Place(i, new Vector3(0, 2.7f, -.15f - i * .025f),
                            Vector3.one * moon * (i == 0 ? .97f : 1), Quaternion.Euler(90, 0, 0), enter * leave);
                    break;
                case "zack":
                    for (int i = 0; i < _lines.Count; i++)
                    {
                        var line = _lines[i]; line.widthMultiplier = .018f * enter * leave;
                        for (int k = 0; k < 7; k++)
                        {
                            float u = k / 6f;
                            float jag = k == 0 || k == 6 ? 0 : Mathf.Sin(k * 8.2f + i * 2.7f + t * 13) * .055f;
                            line.SetPosition(k, new Vector3(-.40f + (i - 1) * .12f + jag, 1.25f + u * .85f * enter, .28f + jag));
                        }
                    }
                    break;
                case "nemu":
                    float amount = Ease(1.05f, 2.28f, t);
                    _kuro.Root.transform.localPosition = new Vector3(-.95f - amount * .18f, .65f + .06f * Mathf.Sin(t * 2), .15f);
                    _kuro.Root.transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(-18, 8, amount), 0);
                    _kuro.Root.transform.localScale = _kuroScale;
                    _rage.Sample(amount, t, .12f);
                    break;
                case "dante":
                    float weight = Ease(.3f, 1.9f, t);
                    for (int i = 0; i < 3; i++)
                        Place(i, new Vector3((i - 1) * .55f, .015f + weight * .035f, .25f + i * .1f),
                            new Vector3(.30f, .3f, .40f), Quaternion.Euler(weight * (i - 1) * 8, 20 + i * 38, 0), enter * leave);
                    break;
                case "cheska":
                    float form = Ease(.4f, 1.85f, t);
                    for (int i = 0; i < 3; i++)
                        Place(i, new Vector3((i - 1) * Mathf.Lerp(.2f, .09f, form), 1.05f + Mathf.Abs(i - 1) * .05f, .50f),
                            new Vector3(.08f, .26f, .07f) * form, Quaternion.Euler(18, i * 65, (i - 1) * 28), enter * leave);
                    break;
            }
        }

        public void Shot(float seconds, out Vector3 position, out Vector3 focus, out float fov)
        {
            Vector3 offset; Vector3 look = Vector3.up * 1.05f; fov = 46;
            switch (_hero)
            {
                case "phaister": offset = new Vector3(1.8f, 1.5f, 5.2f); look.y = 1.45f; fov = 48; break;
                case "zack": offset = new Vector3(-2.8f, 1.35f, 4.6f); fov = 44; break;
                case "nemu": offset = new Vector3(2.8f, 1.5f, 5.6f); look.x = -.55f; look.y = 1.15f; fov = 50; break;
                case "dante": offset = new Vector3(-3, 1, 4.3f); look.y = .8f; fov = 50; break;
                case "cheska": offset = new Vector3(1.8f, 1.3f, 4); look.y = 1.1f; fov = 43; break;
                default: offset = new Vector3(2.2f, 1.1f, 4.5f); break;
            }
            offset = Vector3.Lerp(offset, offset * .94f, Ease(.35f, 2.25f, seconds));
            position = _ground + _facing * offset; focus = _ground + _facing * look;
        }
        public void SetVisibleForCapture(bool visible)
        { if (_renderers != null) foreach (var r in _renderers) if (r != null) r.forceRenderingOff = !visible; }
        public void Dispose()
        {
            _rage?.Dispose(); _rage = null;
            if (_root != null) { _root.SetActive(false); ObjectDestroy(_root); }
        }
        private static void ObjectDestroy(UnityEngine.Object value)
        { if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
    }
}
