using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Map
{
    /// <summary>
    /// A PAYO GEYSER, SEEN (owner, 2026-10-12: geysers get "water and steam of their own"). It sits on the same
    /// object as the `JumpPad` that does the throwing (`JumpPad.NoLook` set, so the amber pad of the other maps is
    /// not drawn here) and draws, in shaped pieces and not sprite fuzz:
    ///
    ///   * A STANDING POOL in the stone bowl, breathing, with a slow bubble that swells and pops off centre.
    ///   * THE BUILD: when a body comes near, the pool domes and a short fat column gurgles up and down, taller
    ///     the nearer the body is: the geyser is getting ready, and a player can read that it is live.
    ///   * THE BURST: the moment the pad throws (`JumpPad.LaunchedAt`, which every peer has for every body), a
    ///     column goes up as the body does, fat at the foot and whipping thin at the top, under a crown of eight
    ///     petals that opens and falls; faceted drops are thrown out on their own arcs and land round the bowl.
    ///   * STEAM: soft three-lobed clouds leave the bowl, lean with the mountain wind, swell and thin out. A wisp
    ///     now and then while it idles, a bank of it after a burst.
    ///   * WET STONE: a dark ring round the bowl after a burst that dries from the outside in.
    ///   * Now and then, with nobody near, it burps: a small spurt and a wisp (its own private random).
    ///
    /// ⚠️ PRESENTATION ONLY: no collider, nothing sent, nothing gameplay reads; the throw is `JumpPad`'s, unchanged.
    /// Hidden in the menus' map preview. Its pieces are made at run time and are not saved in the scene.
    /// </summary>
    [DefaultExecutionOrder(700)]
    public sealed class PayoGeyser : MonoBehaviour
    {
        /// <summary>The bowl's inner radius and how far under the pad's floor point the water stands, metres.</summary>
        public float BowlRadius = 0.62f, PoolDepth = -0.02f;
        /// <summary>How high the burst's column stands at its tallest, and how long the burst lasts.</summary>
        public float BurstHeight = 5.2f, BurstSeconds = 1.25f;
        /// <summary>The wind the steam leans with (the map's: from the valley).</summary>
        public Vector3 Wind = new Vector3(-0.7f, 0.0f, -0.55f);

        private const int Drops = 12, Puffs = 7, Petals = 8;
        private static readonly Color Water = new Color(0.62f, 0.90f, 0.93f, 0.82f), Foam = new Color(0.93f, 0.98f, 1.0f, 0.9f);
        private static readonly Color Steam = new Color(0.96f, 0.98f, 1.0f, 0.34f), Wet = new Color(0.06f, 0.08f, 0.1f, 0.42f);
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private JumpPad _pad;
        private Transform _root, _pool, _bubble, _column, _crown, _wet;
        private readonly Transform[] _drop = new Transform[Drops], _puff = new Transform[Puffs], _petal = new Transform[Petals];
        private readonly Vector3[] _dropVelocity = new Vector3[Drops], _dropAt = new Vector3[Drops], _puffAt = new Vector3[Puffs];
        private readonly float[] _dropAge = new float[Drops], _puffAge = new float[Puffs], _puffLife = new float[Puffs], _puffSize = new float[Puffs];
        private Renderer _poolR, _columnR, _wetR, _bubbleR;
        private readonly Renderer[] _puffR = new Renderer[Puffs], _petalR = new Renderer[Petals], _dropR = new Renderer[Drops];
        private MaterialPropertyBlock _block;
        private Material _waterMaterial, _steamMaterial, _wetMaterial;
        private static Mesh _disc, _spout, _lobe, _gem, _petalMesh;
        private System.Random _random;
        private float _lastLaunch = -99.0f, _burst = 99.0f, _wetness, _near, _nextBurp, _nextWisp, _burp = 99.0f, _clock, _rescan;
        private CharacterMotor[] _motors = System.Array.Empty<CharacterMotor>();

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;

        private float Rand() => (float)_random.NextDouble();
        private float Rand(float a, float b) => a + (b - a) * Rand();

        private void OnEnable()
        {
            _pad = GetComponent<JumpPad>();
            _random = new System.Random(Mathf.RoundToInt(transform.position.x * 131.0f + transform.position.z * 977.0f) + 7);
            _nextBurp = Rand(6.0f, 16.0f); _nextWisp = Rand(1.0f, 4.0f);
            _burst = 99.0f; _burp = 99.0f; _wetness = 0.0f;
            if (_pad != null) _lastLaunch = _pad.LaunchedAt;
        }

        private void OnDisable() { if (_root != null) _root.gameObject.SetActive(false); }

        private void OnDestroy()
        {
            if (_waterMaterial != null) Destroy(_waterMaterial);
            if (_steamMaterial != null) Destroy(_steamMaterial);
            if (_wetMaterial != null) Destroy(_wetMaterial);
        }

        // ------------------------------------------------------------------ made once

        private static Material Make(string name, bool additive)
        {
            var shader = Shader.Find("TumbangPreso/ArenaGlow");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = name, hideFlags = HideFlags.HideAndDontSave };
            if (m.HasProperty("_VertexTint"))
            {
                m.SetFloat("_Rim", 0.0f); m.SetVector("_Near", new Vector4(0.15f, 0.6f, 0.0f, 0.0f));
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                m.SetFloat("_Cull", (float)CullMode.Off); m.SetFloat("_Fog", 1.0f); m.SetFloat("_FogToColour", 1.0f); m.SetFloat("_VertexTint", 0.0f);
            }
            return m;
        }

        /// <summary>A shape turned about the upright from a profile of (radius, height) pairs, `sides` round, flat shaded.</summary>
        private static Mesh Lathe(string name, int sides, params float[] profile)
        {
            int rings = profile.Length / 2;
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            for (int r = 0; r + 1 < rings; r++)
                for (int k = 0; k < sides; k++)
                {
                    float a0 = k * Mathf.PI * 2.0f / sides, a1 = (k + 1) * Mathf.PI * 2.0f / sides;
                    Vector3 P(float a, int ring) => new Vector3(Mathf.Sin(a) * profile[ring * 2], profile[ring * 2 + 1], Mathf.Cos(a) * profile[ring * 2]);
                    int n = v.Count;
                    v.Add(P(a0, r)); v.Add(P(a1, r)); v.Add(P(a1, r + 1)); v.Add(P(a0, r + 1));
                    t.Add(n); t.Add(n + 1); t.Add(n + 2); t.Add(n); t.Add(n + 2); t.Add(n + 3);
                }
            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static void Meshes()
        {
            if (_disc != null) return;
            // A unit disc (radius 1) with a low dome, a unit spout (height 1, fat foot, thin whipping top), a three-lobed
            // cloud (three squashed balls as one lathe each is not possible: one ball, used three times), a drop, a petal.
            _disc = Lathe("Payo geyser disc", 14, 0.0f, 0.06f, 0.55f, 0.045f, 0.9f, 0.015f, 1.0f, 0.0f);
            _spout = Lathe("Payo geyser spout", 9, 0.0f, 0.0f, 1.0f, 0.0f, 0.82f, 0.12f, 0.9f, 0.3f, 0.6f, 0.52f, 0.66f, 0.7f, 0.36f, 0.86f, 0.42f, 0.94f, 0.0f, 1.0f);
            _lobe = Lathe("Payo geyser lobe", 8, 0.0f, -0.42f, 0.36f, -0.3f, 0.5f, 0.0f, 0.36f, 0.3f, 0.0f, 0.42f);
            _gem = Lathe("Payo geyser drop", 5, 0.0f, -0.5f, 0.32f, -0.1f, 0.0f, 0.6f);
            _petalMesh = Lathe("Payo geyser petal", 4, 0.0f, 0.0f, 0.2f, 0.25f, 0.12f, 0.75f, 0.0f, 1.0f);
        }

        private Transform Piece(string name, Mesh mesh, Material material, out Renderer renderer)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave, layer = gameObject.layer };
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go.transform;
        }

        private void Ensure()
        {
            if (_root != null) return;
            Meshes();
            _block = new MaterialPropertyBlock();
            _waterMaterial = Make("Payo geyser water", false);
            _steamMaterial = Make("Payo geyser steam", false);
            _wetMaterial = Make("Payo geyser wet stone", false);
            _wetMaterial.renderQueue = 2990; _waterMaterial.renderQueue = 3010; _steamMaterial.renderQueue = 3020;
            var holder = new GameObject("Geyser (made at run time)") { hideFlags = HideFlags.HideAndDontSave, layer = gameObject.layer };
            _root = holder.transform;
            _root.SetParent(transform, false);
            _wet = Piece("wet stone", _disc, _wetMaterial, out _wetR);
            _pool = Piece("pool", _disc, _waterMaterial, out _poolR);
            _bubble = Piece("bubble", _lobe, _waterMaterial, out _bubbleR);
            _column = Piece("column", _spout, _waterMaterial, out _columnR);
            _crown = new GameObject("crown") { hideFlags = HideFlags.HideAndDontSave }.transform;
            _crown.SetParent(_root, false);
            for (int i = 0; i < Petals; i++) { _petal[i] = Piece("petal " + i, _petalMesh, _waterMaterial, out _petalR[i]); _petal[i].SetParent(_crown, false); }
            for (int i = 0; i < Drops; i++) { _drop[i] = Piece("drop " + i, _gem, _waterMaterial, out _dropR[i]); _dropAge[i] = 99.0f; _drop[i].gameObject.SetActive(false); }
            for (int i = 0; i < Puffs; i++)
            {
                // One cloud is three lobes: the first carries the other two as children, set once.
                _puff[i] = Piece("steam " + i, _lobe, _steamMaterial, out _puffR[i]);
                for (int k = 0; k < 2; k++)
                {
                    var side = Piece("lobe", _lobe, _steamMaterial, out _);
                    side.SetParent(_puff[i], false);
                    side.localPosition = new Vector3(k == 0 ? 0.42f : -0.36f, k == 0 ? -0.08f : 0.1f, k == 0 ? 0.12f : -0.2f);
                    side.localScale = Vector3.one * (k == 0 ? 0.78f : 0.66f);
                }
                _puffAge[i] = 99.0f; _puffLife[i] = 1.0f; _puff[i].gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ every frame

        private void LateUpdate()
        {
            if (IsPreview()) { if (_root != null) _root.gameObject.SetActive(false); return; }
            Ensure();
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);
            float dt = Time.deltaTime;
            _clock += dt;

            // A new throw: the burst.
            if (_pad != null && _pad.LaunchedAt > _lastLaunch + 0.01f) { _lastLaunch = _pad.LaunchedAt; Burst(1.0f); PayoSound.GeyserBurst(transform.position); }

            // Who is near: the build.
            _rescan -= dt;
            if (_rescan <= 0.0f) { _rescan = 1.0f; _motors = FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None); }
            float nearest = 99.0f;
            Vector3 here = transform.position;
            foreach (var m in _motors)
            {
                if (m == null || !m.isActiveAndEnabled) continue;
                Vector3 d = m.transform.position - here;
                if (Mathf.Abs(d.y) > 2.0f) continue;
                nearest = Mathf.Min(nearest, new Vector2(d.x, d.z).magnitude);
            }
            float want = Mathf.Clamp01((3.4f - nearest) / 2.6f);
            _near = Mathf.MoveTowards(_near, want, dt * (want > _near ? 2.2f : 0.9f));

            // Left alone, it burps now and then.
            _nextBurp -= dt;
            if (_nextBurp <= 0.0f) { _nextBurp = Rand(9.0f, 22.0f); if (_burst > BurstSeconds + 1.0f && _near < 0.1f) { _burp = 0.0f; Puff(0.6f, 0.5f); } }
            _nextWisp -= dt;
            if (_nextWisp <= 0.0f) { _nextWisp = Rand(1.6f, 4.2f) * (_near > 0.3f ? 0.45f : 1.0f); Puff(0.38f + 0.3f * _near, 0.25f); }

            _burst += dt; _burp += dt;
            float night = Mathf.Lerp(1.0f, 0.42f, PayoNight.Factor);
            Color water = new Color(Water.r * night, Water.g * night, Water.b * night, Water.a);
            Color foam = new Color(Foam.r * night, Foam.g * night, Foam.b * night, Foam.a);

            // ---- the pool: breathing, doming as it builds
            float breathe = Mathf.Sin(_clock * 1.7f) * 0.5f + Mathf.Sin(_clock * 2.9f + 1.0f) * 0.5f;
            float dome = 1.0f + 2.4f * _near + 0.5f * breathe * (0.3f + _near);
            _pool.localPosition = new Vector3(0.0f, PoolDepth, 0.0f);
            _pool.localScale = new Vector3(BowlRadius, dome, BowlRadius);
            Tint(_poolR, water);

            // A bubble that swells off centre and pops.
            float cycle = Mathf.Repeat(_clock * (0.42f + 0.9f * _near), 1.0f);
            float swell = cycle < 0.8f ? Mathf.SmoothStep(0.0f, 1.0f, cycle / 0.8f) : 0.0f;
            float turn = Mathf.Floor(_clock * (0.42f + 0.9f * _near)) * 2.4f;
            _bubble.localPosition = new Vector3(Mathf.Sin(turn) * BowlRadius * 0.4f, PoolDepth + 0.03f + 0.04f * swell, Mathf.Cos(turn) * BowlRadius * 0.4f);
            _bubble.localScale = new Vector3(0.22f, 0.16f, 0.22f) * swell * (0.7f + 0.6f * _near);
            Tint(_bubbleR, foam);

            // ---- the column: the build's gurgle, a burp's spurt, the burst
            float height, girth;
            if (_burst < BurstSeconds)
            {
                float u = _burst / BurstSeconds;
                // Up fast with an overshoot, held, then it collapses on itself.
                float rise = u < 0.22f ? OutBack(u / 0.22f) : u < 0.55f ? 1.0f + 0.05f * Mathf.Sin((u - 0.22f) * 40.0f) : 1.0f - InCubic((u - 0.55f) / 0.45f);
                height = BurstHeight * Mathf.Max(0.0f, rise);
                girth = BowlRadius * Mathf.Lerp(0.95f, 0.5f, u) * (1.0f + 0.08f * Mathf.Sin(_clock * 31.0f));
            }
            else if (_burp < 0.7f)
            {
                float u = _burp / 0.7f;
                height = 1.1f * Mathf.Sin(u * Mathf.PI);
                girth = BowlRadius * 0.45f;
            }
            else
            {
                // Gurgling: two beats out of step, taller the nearer a body stands.
                float gurgle = Mathf.Abs(Mathf.Sin(_clock * 5.3f)) * 0.6f + Mathf.Abs(Mathf.Sin(_clock * 8.1f + 0.7f)) * 0.4f;
                height = _near * (0.25f + 0.55f * gurgle);
                girth = BowlRadius * (0.4f + 0.25f * _near);
            }
            bool up = height > 0.02f;
            if (_column.gameObject.activeSelf != up) _column.gameObject.SetActive(up);
            if (up)
            {
                _column.localPosition = new Vector3(0.0f, PoolDepth, 0.0f);
                _column.localScale = new Vector3(girth, height, girth);
                _column.localRotation = Quaternion.Euler(0.0f, _clock * 140.0f, 0.0f);
                Tint(_columnR, Color.Lerp(water, foam, 0.55f));
            }

            // ---- the crown: eight petals that open as the column tops out and fall away
            bool crowned = _burst < BurstSeconds * 0.8f && _burst > 0.12f;
            if (_crown.gameObject.activeSelf != crowned) _crown.gameObject.SetActive(crowned);
            if (crowned)
            {
                float u = Mathf.Clamp01((_burst - 0.12f) / (BurstSeconds * 0.68f));
                _crown.localPosition = new Vector3(0.0f, PoolDepth + height * 0.96f, 0.0f);
                for (int i = 0; i < Petals; i++)
                {
                    float a = i * 360.0f / Petals + 14.0f * Mathf.Sin(_clock * 3.0f + i);
                    float open = Mathf.Lerp(18.0f, 128.0f, OutCubic(u)) + (i % 2 == 0 ? 0.0f : 12.0f);
                    _petal[i].localRotation = Quaternion.Euler(0.0f, a, 0.0f) * Quaternion.Euler(open, 0.0f, 0.0f);
                    float size = (i % 2 == 0 ? 1.15f : 0.8f) * Mathf.Sin(Mathf.Clamp01(u * 1.15f) * Mathf.PI);
                    _petal[i].localScale = new Vector3(1.4f, 1.5f, 1.4f) * size;
                    Tint(_petalR[i], foam);
                }
            }

            // ---- drops, steam, wet stone
            for (int i = 0; i < Drops; i++)
            {
                if (_dropAge[i] > 1.6f) { if (_drop[i].gameObject.activeSelf) _drop[i].gameObject.SetActive(false); continue; }
                _dropAge[i] += dt;
                _dropVelocity[i] += Vector3.down * 16.0f * dt;
                _dropAt[i] += _dropVelocity[i] * dt;
                if (_dropAt[i].y < 0.0f) { _dropAge[i] = 99.0f; _drop[i].gameObject.SetActive(false); continue; }
                _drop[i].localPosition = _dropAt[i];
                _drop[i].localRotation = Quaternion.FromToRotation(Vector3.up, _dropVelocity[i].sqrMagnitude > 0.01f ? _dropVelocity[i].normalized : Vector3.up);
                Tint(_dropR[i], foam);
            }

            Vector3 wind = transform.InverseTransformDirection(Wind);
            for (int i = 0; i < Puffs; i++)
            {
                if (_puffAge[i] > _puffLife[i]) { if (_puff[i].gameObject.activeSelf) _puff[i].gameObject.SetActive(false); continue; }
                _puffAge[i] += dt;
                float u = _puffAge[i] / _puffLife[i];
                // It leaves fast and slows, leans more with the wind the higher it is, swells all the way.
                _puffAt[i] += (Vector3.up * Mathf.Lerp(1.5f, 0.35f, u) + wind * (0.25f + 1.3f * u)) * dt;
                _puff[i].localPosition = _puffAt[i];
                _puff[i].localScale = Vector3.one * _puffSize[i] * Mathf.Lerp(0.5f, 2.3f, OutCubic(u));
                _puff[i].localRotation = Quaternion.Euler(0.0f, i * 51.0f + _clock * 9.0f, 8.0f * Mathf.Sin(_clock + i));
                float alpha = Steam.a * Mathf.Sin(Mathf.Clamp01(u * 1.2f) * Mathf.PI * 0.5f) * (1.0f - u * u);
                var colour = new Color(Steam.r * night, Steam.g * night, Steam.b * night, alpha);
                foreach (var r in _puff[i].GetComponentsInChildren<Renderer>()) Tint(r, colour);
            }

            _wetness = Mathf.MoveTowards(_wetness, 0.0f, dt / 9.0f);
            bool wet = _wetness > 0.01f;
            if (_wet.gameObject.activeSelf != wet) _wet.gameObject.SetActive(wet);
            if (wet)
            {
                // It dries from the outside in.
                float reach = BowlRadius * Mathf.Lerp(1.6f, 3.6f, Mathf.Sqrt(_wetness));
                _wet.localPosition = new Vector3(0.0f, 0.012f, 0.0f);
                _wet.localScale = new Vector3(reach, 0.02f, reach);
                Tint(_wetR, new Color(Wet.r, Wet.g, Wet.b, Wet.a * Mathf.Clamp01(_wetness * 1.6f)));
            }
        }

        private void Tint(Renderer r, Color colour)
        {
            r.GetPropertyBlock(_block);
            _block.SetColor(ColorId, colour);
            r.SetPropertyBlock(_block);
        }

        /// <summary>The burst's start: the column's clock, a ring of drops, a bank of steam, the stone wetted.</summary>
        private void Burst(float strength)
        {
            _burst = 0.0f; _wetness = 1.0f;
            for (int i = 0; i < Drops; i++)
            {
                float a = (i + Rand(0.0f, 0.8f)) * Mathf.PI * 2.0f / Drops, outward = Rand(1.2f, 3.4f) * strength;
                _dropAt[i] = new Vector3(0.0f, BurstHeight * Rand(0.25f, 0.6f), 0.0f);
                _dropVelocity[i] = new Vector3(Mathf.Sin(a) * outward, Rand(4.0f, 8.5f) * strength, Mathf.Cos(a) * outward);
                _dropAge[i] = Rand(-0.25f, 0.0f);
                float size = Rand(0.09f, 0.18f);
                _drop[i].localScale = new Vector3(size, size * 1.7f, size);
                _drop[i].gameObject.SetActive(true);
            }
            for (int k = 0; k < 4; k++) Puff(Rand(0.7f, 1.1f), Rand(0.6f, 3.0f));
        }

        private void Puff(float size, float height)
        {
            int oldest = 0;
            for (int i = 0; i < Puffs; i++)
            {
                if (_puffAge[i] > _puffLife[i]) { oldest = i; break; }
                if (_puffAge[i] / _puffLife[i] > _puffAge[oldest] / _puffLife[oldest]) oldest = i;
            }
            _puffAge[oldest] = 0.0f; _puffLife[oldest] = Rand(2.6f, 4.4f); _puffSize[oldest] = size;
            _puffAt[oldest] = new Vector3(Rand(-0.2f, 0.2f), PoolDepth + height, Rand(-0.2f, 0.2f));
            _puff[oldest].gameObject.SetActive(true);
        }

        private static float OutBack(float t)
        {
            t = Mathf.Clamp01(t) - 1.0f;
            return 1.0f + 2.70158f * t * t * t + 1.70158f * t * t;
        }

        private static float OutCubic(float t)
        {
            t = 1.0f - Mathf.Clamp01(t);
            return 1.0f - t * t * t;
        }

        private static float InCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }
    }
}
