using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ SPOTLIGHT PIN, THE CAST (HERO-10, plan 4.3 and 4.5): her sigils sweep across the 60 degree cone on the court from
    /// HER LEFT TO HER RIGHT in 0.2 s, each writing itself at its own spot and burning out. Nine typed marks: angle in the
    /// cone, distance, size, which of her runes. The victims' moonlight is `PhaisterMoonlight`, driven by their status.
    /// </summary>
    public sealed class PhaisterPinSweep : MonoBehaviour, IVfxTimeline
    {
        private static readonly (float Angle, float Range, float Size, int Rune)[] Marks =
        {
            // v2 (film v1: invisible at court distance at 0.5 m): about twice the size.
            (-28f, 2.2f, 0.95f, 11), (-21f, 4.0f, 1.15f, 12), (-14f, 5.8f, 1.05f, 13), (-7f, 3.1f, 0.90f, 14), (0f, 4.9f, 1.25f, 15),
            (  7f, 2.6f, 1.00f, 16), ( 14f, 4.3f, 1.10f, 17), (21f, 6.2f, 0.95f, 18), (28f, 3.4f, 1.05f, 19),
        };
        public const float Sweep = 0.20f, Burn = 0.55f;
        public float LifeSeconds => Sweep + Burn;

        /// <summary>
        /// ⚠️ v3 (film v7: at 7 m the sigils alone did not show at all): a CRESCENT STROKE wipes across the cone from her left to her
        /// right with them, Seele's curved stroke laid on the court. Paete's brush-stroke lesson (film r16): light alone vanishes on a
        /// pale court, so it is a dark translucent body with her light down its middle. Radius, width at its fattest, lift.
        /// </summary>
        public const float StrokeRadius = 3.6f, StrokeWidth = 1.05f, StrokeLinger = 0.30f;
        private const int StrokeSegments = 28;

        private readonly List<(Transform T, Material M, float At, float Size)> _marks = new List<(Transform, Material, float, float)>();
        private Mesh _strokeBody, _strokeLight;
        private Material _strokeBodyInk, _strokeLightInk;
        private float _age;

        public static PhaisterPinSweep Play(Vector3 at, Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            var go = new GameObject("PhaisterPinSweep");
            go.transform.SetPositionAndRotation(PhaisterProp.OnCourt(at), Quaternion.LookRotation(forward.normalized));
            var fx = go.AddComponent<PhaisterPinSweep>();
            float half = VoodooRules.VulnerableConeDegrees * 0.5f;
            foreach (var m in Marks)
            {
                // v3: each sigil over its own dark ink, a little wider, so it reads on a pale court from across it.
                var ink = VfxShapes.Lay(go.transform, "SigilInk", PhaisterSpellGeometry.FlatRune(m.Rune, 0.20f), m.Size * 1.12f, 0.037f, m.Angle);
                ink.transform.localPosition = Quaternion.Euler(0f, m.Angle, 0f) * new Vector3(0f, 0.037f, m.Range);
                VfxMaterial.Ghost(ink.GetComponent<Renderer>(), new Color(0.12f, 0.03f, 0.16f, 0f), 0f);
                var mark = VfxShapes.Lay(go.transform, "Sigil", PhaisterSpellGeometry.FlatRune(m.Rune, 0.13f), m.Size, 0.04f, m.Angle);
                mark.transform.localPosition = Quaternion.Euler(0f, m.Angle, 0f) * new Vector3(0f, 0.04f, m.Range);
                VfxMaterial.Ghost(mark.GetComponent<Renderer>(), new Color(0.74f, 0.34f, 1.0f, 0f), 2.6f);
                // Left to right across the cone: the first mark is at her left edge.
                float writeAt = (m.Angle + half) / (2f * half) * Sweep;
                fx._marks.Add((mark.transform, mark.GetComponent<Renderer>().sharedMaterial, writeAt, m.Size));
                fx._marks.Add((ink.transform, ink.GetComponent<Renderer>().sharedMaterial, writeAt, m.Size * 1.12f));
            }
            // The stroke: a dark body and her light down its middle, rebuilt each frame as it wipes across.
            fx._strokeBody = new Mesh { name = "PinStrokeBody" }; fx._strokeBody.MarkDynamic();
            fx._strokeLight = new Mesh { name = "PinStrokeLight" }; fx._strokeLight.MarkDynamic();
            var body = new GameObject("PinStrokeBody"); body.transform.SetParent(go.transform, false);
            body.transform.localPosition = Vector3.up * 0.045f;
            body.AddComponent<MeshFilter>().sharedMesh = fx._strokeBody; var br = body.AddComponent<MeshRenderer>();
            VfxShapes.Own(body, fx._strokeBody);
            VfxMaterial.Ghost(br, new Color(0.30f, 0.08f, 0.40f, 0.62f), 0.35f);
            br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; br.receiveShadows = false;
            fx._strokeBodyInk = br.sharedMaterial;
            var light = new GameObject("PinStrokeLight"); light.transform.SetParent(go.transform, false);
            light.transform.localPosition = Vector3.up * 0.06f;
            light.AddComponent<MeshFilter>().sharedMesh = fx._strokeLight; var lr = light.AddComponent<MeshRenderer>();
            VfxShapes.Own(light, fx._strokeLight);
            var glow = Resources.Load<Shader>("Shaders/SpiritGlow");
            if (glow != null)
            {
                var gm = new Material(glow) { name = "PinStrokeLight" };
                gm.SetFloat("_Billboard", 0f); gm.SetFloat("_Band", 1f); gm.SetFloat("_Falloff", 1.6f); gm.SetFloat("_Core", 0.6f);
                gm.SetColor("_Color", new Color(0.78f, 0.40f, 1.0f, 1f));
                lr.sharedMaterial = gm; VfxRenderTag.Own(light, gm); fx._strokeLightInk = gm;
            }
            else { VfxMaterial.Ghost(lr, new Color(0.78f, 0.40f, 1.0f, 0.8f), 1.6f); fx._strokeLightInk = lr.sharedMaterial; }
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
            fx.StepTo(0f);
            return fx;
        }

        /// <summary>
        /// The crescent from the cone's left edge to <paramref name="toDeg"/>, <paramref name="width"/> at its fattest, tapering to
        /// points at both ends; uv.x along it, uv.y across (the glow's band).
        /// </summary>
        private static void Arc(Mesh mesh, float fromDeg, float toDeg, float radius, float width)
        {
            var v = new Vector3[(StrokeSegments + 1) * 2];
            var uv = new Vector2[v.Length];
            var tri = new int[StrokeSegments * 6];
            for (int i = 0; i <= StrokeSegments; i++)
            {
                float u = i / (float)StrokeSegments;
                float a = Mathf.Lerp(fromDeg, toDeg, u) * Mathf.Deg2Rad;
                // Fattest a third of the way from the leading end, the way a brush lands and lifts.
                float w = width * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.8f))), 0.7f) * 0.5f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                // The inner edge sits a little further out at the ends: a crescent, not a band.
                v[i * 2] = dir * (radius - w * 0.6f);
                v[i * 2 + 1] = dir * (radius + w * 1.4f);
                uv[i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = new Vector2(u, 1f);
                if (i == StrokeSegments) continue;
                int k = i * 6, n = i * 2;
                tri[k] = n; tri[k + 1] = n + 1; tri[k + 2] = n + 2; tri[k + 3] = n + 2; tri[k + 4] = n + 1; tri[k + 5] = n + 3;
            }
            mesh.Clear(); mesh.vertices = v; mesh.uv = uv; mesh.triangles = tri;
            var normals = new Vector3[v.Length]; for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals; mesh.RecalculateBounds();
        }

        private void Update() { StepTo(_age + Time.deltaTime); if (_age >= LifeSeconds) Destroy(gameObject); }

        public void StepTo(float seconds)
        {
            _age = seconds;
            // The stroke wipes left to right with the sigils, holds, then thins away from its tail.
            float half = VoodooRules.VulnerableConeDegrees * 0.5f;
            float wipe = Mathf.Clamp01(seconds / Sweep);
            float linger = Mathf.Clamp01((seconds - Sweep) / StrokeLinger);
            float thin = 1f - linger * linger;
            if (_strokeBody != null)
            {
                float to = Mathf.Lerp(-half, half, 1f - (1f - wipe) * (1f - wipe));
                float from = Mathf.Lerp(-half, to, linger * 0.9f);
                float width = StrokeWidth * thin;
                if (to - from < 0.5f || width < 0.02f) { _strokeBody.Clear(); _strokeLight.Clear(); }
                else
                {
                    Arc(_strokeBody, from, to, StrokeRadius, width);
                    Arc(_strokeLight, from, to, StrokeRadius + width * 0.25f, width * 0.55f);
                }
                if (_strokeLightInk != null && _strokeLightInk.HasProperty("_Color"))
                    _strokeLightInk.SetColor("_Color", new Color(0.78f, 0.40f, 1.0f, 1.3f * thin));
                PhaisterProp.SetAlpha(_strokeBodyInk, 0.62f * thin);
            }
            foreach (var (t, m, at, size) in _marks)
            {
                float u = seconds - at;
                bool on = u >= 0f;
                t.gameObject.SetActive(on);
                if (!on || m == null) continue;
                // Writes itself in (a quick grow and turn), holds, then burns out.
                float write = Mathf.Clamp01(u / 0.08f);
                float k = size * Mathf.Lerp(0.2f, 1f, write);
                t.localScale = new Vector3(k, 1f, k);
                PhaisterProp.SetAlpha(m, (t.name == "SigilInk" ? 0.55f : 1.0f) * write * Mathf.Clamp01(1f - (u - 0.15f) / Burn));
            }
        }
    }

    /// <summary>
    /// ⚠️⚠️ THE MOONLIGHT ON A VULNERABLE PLAYER (plan 4.3, 4.5; owner, of the light following them: *"its good"*). A pale violet
    /// column drops on them out of the sky and FOLLOWS them for the whole Vulnerable time, so everyone can read who is exposed;
    /// a pin of light drives into the court at their feet; motes drift DOWN inside the light; at the end the column thins to
    /// one beam and snaps off and the pin crumbles.
    ///
    /// ⚠️ DRIVEN BY THE STATUS (`CharacterMotor.IsVulnerable`, synced to every peer), so it is the same on every screen with no
    /// message of its own, and a rejoiner sees it too. `PhaisterStatusPresenter` spawns and ends it.
    /// </summary>
    public sealed class PhaisterMoonlight : MonoBehaviour
    {
        public const float Drop = 0.12f, Height = 7.0f, Radius = 0.62f, End = 0.22f;
        // v2 (film v1): the first colour (0.78, 0.66, 1.0) at 0.55 alpha read as a pale GLASS TUBE, nearly white. Deeper violet, fainter.
        private static readonly Color Moon = new Color(0.60f, 0.38f, 0.98f);
        private static readonly Color Warm = new Color(1.0f, 0.58f, 0.80f);

        // ⚠️ v3 (film v7: still a glass tube, the slipper beam's shader is brighter at its edges): the shaft is `Shaders/MoonShaft`
        // on an OPEN cylinder (no caps), soft at its sides and brightest at the court; at their feet a ring of her runes turns
        // CLOCKWISE (angle deg, size, seed), so the court as well as the air says who is pinned.
        private static readonly (float A, float Size, int Seed)[] FootRunes =
        {
            (0f, 0.26f, 81), (-72f, 0.22f, 82), (-140f, 0.28f, 83), (-205f, 0.23f, 84), (-285f, 0.25f, 85),
        };
        private Transform _runes;
        private readonly List<Material> _runeInks = new List<Material>();

        // Motes: typed (angle, radius, speed, phase, size).
        private static readonly (float A, float R, float Speed, float Phase, float Size)[] MoteRows =
        {
            (20f, 0.30f, 0.9f, 0.1f, 0.05f), (95f, 0.42f, 0.7f, 0.5f, 0.04f), (160f, 0.22f, 1.1f, 0.8f, 0.06f),
            (230f, 0.46f, 0.8f, 0.3f, 0.04f), (290f, 0.34f, 1.0f, 0.65f, 0.05f), (340f, 0.18f, 0.6f, 0.95f, 0.04f),
        };

        private CharacterMotor _who;
        private Transform _column, _pool, _pin;
        private readonly List<Transform> _motes = new List<Transform>();
        private float _age, _endAge = -1f;

        public bool Ending => _endAge >= 0f;

        public static PhaisterMoonlight On(CharacterMotor who)
        {
            var go = new GameObject("PhaisterMoonlight");
            var fx = go.AddComponent<PhaisterMoonlight>();
            fx._who = who;
            var column = new GameObject("Moonbeam"); column.transform.SetParent(go.transform, false);
            var shaftMesh = Shaft();
            column.AddComponent<MeshFilter>().sharedMesh = shaftMesh; var cr = column.AddComponent<MeshRenderer>();
            VfxShapes.Own(column, shaftMesh);
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; cr.receiveShadows = false;
            var shaft = Resources.Load<Shader>("Shaders/MoonShaft");
            if (shaft != null)
            {
                var sm = new Material(shaft) { name = "MoonShaft" };
                // v3b (film v9: at 0.55 the column painted its victim violet head to foot): the light around them, their own colours
                // still readable through it.
                sm.SetColor("_Color", new Color(Moon.r, Moon.g, Moon.b, 0.40f)); sm.SetColor("_Rim", Warm);
                cr.sharedMaterial = sm; VfxRenderTag.Own(column, sm);
            }
            else VfxMaterial.Beam(cr, Moon, 0.34f, false);
            fx._column = column.transform;
            // The pool of it on the court: soft, additive, no edge (`SpiritGlow` laid flat).
            var glow = Resources.Load<Shader>("Shaders/SpiritGlow");
            var pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pool.name = "MoonPool"; VfxMaterial.StripCollider(pool); pool.transform.SetParent(go.transform, false);
            pool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var pr = pool.GetComponent<Renderer>();
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pr.receiveShadows = false;
            if (glow != null)
            {
                var pm = new Material(glow) { name = "MoonPool" };
                pm.SetFloat("_Billboard", 0f); pm.SetFloat("_Falloff", 1.4f); pm.SetFloat("_Core", 0.4f);
                pm.SetColor("_Color", new Color(Moon.r, Moon.g, Moon.b, 0.9f));
                pr.sharedMaterial = pm; VfxRenderTag.Own(pool, pm);
            }
            else VfxMaterial.Beam(pr, Moon, 0.6f, true);
            fx._pool = pool.transform;
            fx._runes = new GameObject("MoonRunes").transform; fx._runes.SetParent(go.transform, false);
            foreach (var r in FootRunes)
            {
                var ink = VfxShapes.Lay(fx._runes, "MoonRuneInk", PhaisterSpellGeometry.FlatRune(r.Seed, 0.2f), r.Size * 1.12f, 0.035f, -r.A);
                ink.transform.localPosition = Quaternion.Euler(0f, r.A, 0f) * new Vector3(0f, 0.035f, Radius + 0.22f);
                VfxMaterial.Ghost(ink.GetComponent<Renderer>(), new Color(0.12f, 0.03f, 0.16f, 0.5f), 0f);
                var rune = VfxShapes.Lay(fx._runes, "MoonRune", PhaisterSpellGeometry.FlatRune(r.Seed, 0.13f), r.Size, 0.04f, -r.A);
                rune.transform.localPosition = Quaternion.Euler(0f, r.A, 0f) * new Vector3(0f, 0.04f, Radius + 0.22f);
                VfxMaterial.Ghost(rune.GetComponent<Renderer>(), new Color(0.80f, 0.46f, 1.0f, 0.95f), 2.0f);
                fx._runeInks.Add(ink.GetComponent<Renderer>().sharedMaterial); fx._runeInks.Add(rune.GetComponent<Renderer>().sharedMaterial);
            }
            var pin = PhaisterProp.Spawn("hatpin", go.transform, null, PhaisterProp.InsectOutlineWidth);
            if (pin != null) { pin.transform.localScale = Vector3.one * 2.2f; fx._pin = pin.transform; }
            foreach (var m in MoteRows)
            {
                var mote = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mote.name = "Mote"; VfxMaterial.StripCollider(mote); mote.transform.SetParent(go.transform, false);
                var mr = mote.GetComponent<Renderer>();
                if (glow != null)
                {
                    var mm = new Material(glow) { name = "MoonMote" };
                    mm.SetFloat("_Billboard", 1f); mm.SetFloat("_Falloff", 2.4f); mm.SetFloat("_Core", 0.9f);
                    mm.SetColor("_Color", new Color(0.86f, 0.70f, 1.0f, 1.2f));
                    mr.sharedMaterial = mm; VfxRenderTag.Own(mote, mm);
                }
                else VfxMaterial.Ghost(mr, new Color(0.90f, 0.82f, 1.0f, 0.85f), 1.2f);
                mote.transform.localScale = Vector3.one * m.Size * 2.2f;
                fx._motes.Add(mote.transform);
            }
            GameServices.Audio?.PlayAt("sfx_phaister_moonlight_on", who.transform.position);
            fx.Update();
            return fx;
        }

        /// <summary>An open cylinder, unit radius and height, uv.x round it and uv.y up it: no caps (a cap is a disc of light in the air).</summary>
        private static Mesh Shaft()
        {
            const int Sides = 24;
            var v = new Vector3[(Sides + 1) * 2]; var n = new Vector3[v.Length]; var uv = new Vector2[v.Length];
            var tri = new int[Sides * 6];
            for (int i = 0; i <= Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides;
                var rim = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v[i * 2] = rim; v[i * 2 + 1] = rim + Vector3.up;
                n[i * 2] = rim; n[i * 2 + 1] = rim;
                uv[i * 2] = new Vector2(i / (float)Sides, 0f); uv[i * 2 + 1] = new Vector2(i / (float)Sides, 1f);
                if (i == Sides) continue;
                int k = i * 6, j = i * 2;
                tri[k] = j; tri[k + 1] = j + 1; tri[k + 2] = j + 2; tri[k + 3] = j + 2; tri[k + 4] = j + 1; tri[k + 5] = j + 3;
            }
            var mesh = new Mesh { name = "MoonShaftMesh", vertices = v, normals = n, uv = uv, triangles = tri };
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Finish()
        {
            if (Ending) return;
            _endAge = 0f;
            if (_who != null) GameServices.Audio?.PlayAt("sfx_phaister_moonlight_off", _who.transform.position);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_endAge >= 0f) _endAge += dt;
            if (_who == null) { Destroy(gameObject); return; }
            Vector3 feet = _who.transform.position;
            feet.y = Slipper.GroundY(feet);
            transform.position = feet;

            // The drop: the column comes down out of the sky, its foot reaching the court at `Drop`.
            float drop = Mathf.Clamp01(_age / Drop);
            float thin = _endAge >= 0f ? Mathf.Clamp01(1f - _endAge / End) : 1f;
            float r = Radius * (0.25f + 0.75f * thin) * (thin > 0.02f ? 1f : 0f);
            float h = Height * drop;
            // The open cylinder is a unit tall from its foot: it drops from the sky, its foot reaching the court at `Drop`.
            _column.localScale = new Vector3(r, h, r);
            _column.localPosition = new Vector3(0f, Height - h, 0f);
            _column.gameObject.SetActive(thin > 0.02f);
            _pool.gameObject.SetActive(drop >= 1f && thin > 0.02f);
            _pool.localPosition = Vector3.up * 0.04f;
            _pool.localScale = Vector3.one * r * 3.2f;
            // Her runes round their feet, written in as the light lands, turning clockwise, gone with it.
            if (_runes != null)
            {
                float write = Mathf.Clamp01((_age - Drop) / 0.2f) * thin;
                _runes.localRotation = Quaternion.Euler(0f, -30f * _age, 0f);
                _runes.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, write);
                _runes.gameObject.SetActive(write > 0.01f);
                for (int i = 0; i < _runeInks.Count; i++) PhaisterProp.SetAlpha(_runeInks[i], (i % 2 == 0 ? 0.5f : 0.95f) * write);
            }

            // The pin drives straight DOWN into the court at their feet as the light lands, and crumbles at the end.
            if (_pin != null)
            {
                float fall = Mathf.Clamp01((_age - Drop * 0.5f) / 0.1f);
                float crumble = _endAge >= 0f ? Mathf.Clamp01(_endAge / End) : 0f;
                // v3: it stands in the court just outside the light, on the rune ring, where the court camera sees it.
                _pin.localPosition = new Vector3(Radius + 0.1f, Mathf.Lerp(3.0f, 0.62f, fall * fall), 0.12f);
                _pin.localRotation = Quaternion.Euler(0f, 30f, 8f);
                _pin.localScale = new Vector3(2.2f, 2.2f * (1f - crumble), 2.2f);
                _pin.gameObject.SetActive(fall > 0f && crumble < 1f);
            }

            // Motes drift DOWN inside the light, each on its own loop.
            for (int i = 0; i < _motes.Count; i++)
            {
                var m = MoteRows[i];
                float y = Height * 0.45f * (1f - Mathf.Repeat(_age * m.Speed * 0.35f + m.Phase, 1f));
                float a = (m.A - 20f * _age) * Mathf.Deg2Rad;
                _motes[i].localPosition = new Vector3(Mathf.Cos(a) * m.R * thin, y, Mathf.Sin(a) * m.R * thin);
                _motes[i].gameObject.SetActive(drop >= 1f && thin > 0.02f);
            }
            if (_endAge >= End) Destroy(gameObject);
        }
    }

    /// <summary>
    /// ⚠️ PHAISTER'S STATUSES, PRESENTED FROM THE STATUS ITSELF, ON EVERY PEER (HERO-10). Each frame: a Vulnerable player gets
    /// `PhaisterMoonlight` until it ends; a player who has just turned Disoriented hands the nearest waiting manika to them
    /// (`PhaisterManika.Steal`) and wears a small sigil spinning CLOCKWISE over their head while it lasts. Nothing here sends
    /// or decides anything; the host's `SyncUnit` timers are the whole input.
    /// </summary>
    public sealed class PhaisterStatusPresenter : MonoBehaviour
    {
        private static PhaisterStatusPresenter _instance;
        private readonly Dictionary<CharacterMotor, PhaisterMoonlight> _moon = new Dictionary<CharacterMotor, PhaisterMoonlight>();
        private readonly Dictionary<CharacterMotor, Transform> _mark = new Dictionary<CharacterMotor, Transform>();
        private readonly HashSet<CharacterMotor> _wasDisoriented = new HashSet<CharacterMotor>();

        public static void Ensure()
        {
            if (_instance != null) return;
            _instance = new GameObject("PhaisterStatusPresenter").AddComponent<PhaisterStatusPresenter>();
        }

        private void OnDestroy() { if (_instance == this) _instance = null; }

        private void Update()
        {
            var round = GameServices.Round;
            if (round == null) return;
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                // Moonlight on the Vulnerable.
                _moon.TryGetValue(p, out var moon);
                if (p.IsVulnerable && (moon == null || moon.Ending)) _moon[p] = PhaisterMoonlight.On(p);
                else if (!p.IsVulnerable && moon != null && !moon.Ending) moon.Finish();

                // The doll's steal on the rising edge; the victim's mark while it lasts.
                bool dis = p.IsDisoriented;
                if (dis && !_wasDisoriented.Contains(p))
                {
                    PhaisterManika.Nearest(p.transform.position + Vector3.up, 4.5f)?.Steal(p);
                    _wasDisoriented.Add(p);
                }
                else if (!dis) _wasDisoriented.Remove(p);
                _mark.TryGetValue(p, out var mark);
                if (dis && mark == null)
                {
                    var go = VfxShapes.Lay(null, "HexMark", VfxShapes.TwoSided(VfxShapes.Rune(21, 0.12f)), 0.32f, 0f);
                    VfxMaterial.Ghost(go.GetComponent<Renderer>(), new Color(0.80f, 0.50f, 1.0f, 0.85f), 1.1f);
                    go.transform.localScale = Vector3.one * 0.42f; // an upright glyph spinning over their head: scaled in all three axes (Lay scales x and z only)
                    _mark[p] = mark = go.transform;
                }
                if (mark != null)
                {
                    if (!dis) { Destroy(mark.gameObject); _mark.Remove(p); continue; }
                    mark.position = p.transform.position + Vector3.up * 2.25f;
                    mark.rotation = Quaternion.Euler(0f, -140f * Time.time, 0f);
                }
            }
        }
    }
}
