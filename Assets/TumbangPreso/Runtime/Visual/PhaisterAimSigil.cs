using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ WHERE SHE IS ABOUT TO GO, ON HER OWN SCREEN (HERO-10, plan 4.1 and 4.5 row 1 and 2; plan 4.4 for OMEN). Film v7: holding
    /// VANISHING ACT for 0.6 s showed NOTHING 5 m ahead of her, on her screen or the court's; the shared ward did not appear on Bayan
    /// Plaza's paving at all. This replaces it for her (`HeroAbility.DrawsOwnAim`):
    ///
    /// | Kind | What is drawn | Moves |
    /// |---|---|---|
    /// | ARRIVAL (VANISHING ACT) | her lunar sigil on the court where she will land: a thin ring, the crescent, her binding writing, six runes | the runes write themselves CLOCKWISE in 0.3 s; the whole mark turns clockwise 18 degrees a second; it follows the aim |
    /// | | three moths | fly out of her cuffs to the sigil, then circle it CLOCKWISE at 1.5 turns a second, wings slow |
    /// | OMEN | the 7.5 m ring on the court with twelve runes | writes itself clockwise as the hold ramps |
    /// | | a ghost of the eye at the HEIGHT it will hang (the owner must see how high before he lets go), glitching, a soft halo | pulses |
    /// | | a line of lights from the court straight up to it, and a small ring on the court under it | lights climb the line |
    ///
    /// On the release it flares for a tenth of a second and burns out as embers over half a second where it lies (plan 4.5 row 1).
    /// ⚠️ PRIVATE TO WHOEVER IS AIMING (owner, 2026-08-27, on her blink: *"make sure only she can see it"*): drawn only while the
    /// local camera rig follows the caster, the rule `GroundReticle` keeps.
    /// ⚠️ PLACED ON THE COURT (`PhaisterProp.OnCourt`, one ray at the middle) EVERY FRAME, a few centimetres up: the plaza's paving
    /// stands above the map's floor, which is why the shared ring never showed there (and v8's own rings did not either).
    ///
    /// ⚠️⚠️ v2 (film v8): SHE SEES IT ALONG THE COURT, NOT FROM ABOVE. From her eye (about 1.5 m up) a mark 5 m away lies at 16
    /// degrees, so its depth is squashed to a quarter: v8's 8 cm rings were hairlines and its crescent a smudge; only the runes,
    /// which happened to stand up, and the moths read at all. So the flat parts are BOLD (a 19 cm band over its ink, a filled
    /// crescent 1.2 m across, a pool of her light under it), and the runes STAND on the ring on purpose, turned to her lens every
    /// frame so none goes edge-on, growing up out of the court as they write. OMEN's ghost eye is drawn near the size the real one
    /// opens to, so she reads how big as well as how high, with a thin line of light from the court up to it.
    /// </summary>
    public sealed class PhaisterAimSigil : MonoBehaviour
    {
        public enum Kind { Arrival, Omen }

        private static readonly Color Violet = new Color(0.66f, 0.34f, 1.00f), Magenta = new Color(0.90f, 0.20f, 0.56f);
        private static readonly Color Ink = new Color(0.10f, 0.03f, 0.14f);

        // ARRIVAL, typed: the six runes round the ring (angle deg, radius m, size m, rune seed); written clockwise in this order.
        private static readonly (float A, float R, float Size, int Seed)[] ArrivalRunes =
        {
            (  0f, 1.02f, 0.30f, 51), (-62f, 1.06f, 0.26f, 52), (-118f, 1.00f, 0.32f, 53),
            (-180f, 1.05f, 0.27f, 54), (-238f, 1.01f, 0.30f, 55), (-297f, 1.04f, 0.25f, 56),
        };
        // The three moths: (start angle deg, orbit radius m, height m, wing Hz, size), each on its own path.
        private static readonly (float A, float R, float Y, float Hz, float Size)[] Moths =
        {
            (0f, 0.70f, 0.55f, 4.4f, 2.3f), (125f, 0.86f, 0.38f, 5.1f, 2.0f), (245f, 0.62f, 0.72f, 3.8f, 2.5f),
        };
        // OMEN, typed: twelve runes round the 7.5 m ring (seed, size), the same writing order as the live ring (`PhaisterOmen`).
        private static readonly (int Seed, float Size)[] OmenRunes =
        {
            (61, 1.10f), (62, 0.95f), (63, 1.20f), (64, 1.00f), (65, 1.15f), (66, 0.90f),
            (67, 1.05f), (68, 1.20f), (69, 0.95f), (70, 1.10f), (71, 1.00f), (72, 1.15f),
        };
        private const int Beads = 11;

        private Kind _kind;
        private CharacterMotor _caster;
        private CameraSystem.CameraRig _rig;
        private float _rigSearchAt = -100f;
        private float _age, _shownAt = -1f, _releasedAt = -1f;
        private Vector3 _at, _ground;
        private Transform _mark;
        private readonly List<(Transform T, Material M, float Base)> _parts = new List<(Transform, Material, float)>();
        private readonly List<(Transform T, Material M, float At, float Size)> _runes = new List<(Transform, Material, float, float)>();
        private readonly List<(Transform T, Transform[] W)> _moths = new List<(Transform, Transform[])>();
        private readonly List<Vector3> _mothFrom = new List<Vector3>();
        private readonly List<(Transform T, Material M)> _beads = new List<(Transform, Material)>();
        private readonly List<(Transform T, Material M, Vector3 Drift)> _embers = new List<(Transform, Material, Vector3)>();
        private Transform _eye, _halo, _foot, _line;
        private Material _eyeMat, _haloMat, _lineMat;

        public static PhaisterAimSigil Create(Kind kind)
        {
            var go = new GameObject(kind == Kind.Arrival ? "PhaisterAimSigil" : "PhaisterOmenAim");
            var s = go.AddComponent<PhaisterAimSigil>();
            s._kind = kind;
            s.Build();
            go.SetActive(false);
            return s;
        }

        private static Material Glow(Color c)
        {
            var shader = Resources.Load<Shader>("Shaders/SpiritGlow");
            if (shader == null) return null;
            var m = new Material(shader);
            m.SetColor("_Color", c); m.SetFloat("_Billboard", 1f); m.SetFloat("_Falloff", 2.2f); m.SetFloat("_Core", 0.7f);
            return m;
        }

        private Transform Flat(Transform parent, string name, Mesh mesh, float size, Color c, float emission, float lift)
        {
            var go = VfxShapes.Lay(parent, name, mesh, size, lift);
            var r = go.GetComponent<Renderer>();
            VfxMaterial.Ghost(r, c, emission);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            _parts.Add((go.transform, r.sharedMaterial, c.a));
            return go.transform;
        }

        private void Build()
        {
            _mark = new GameObject("Mark").transform;
            _mark.SetParent(transform, false);
            if (_kind == Kind.Arrival)
            {
                // The ink under the light, a little wider, so it reads on a pale court (Paete's brush-stroke lesson: light alone
                // disappears there).
                Pool(_mark, 1.25f, new Color(Violet.r, Violet.g, Violet.b, 0.75f));
                Flat(_mark, "SigilInkRing", VfxShapes.Hollow(48, 0.70f, 0f, 3), 0.94f, new Color(Ink.r, Ink.g, Ink.b, 0.6f), 0f, 0.035f);
                Flat(_mark, "SigilRing", VfxShapes.Hollow(48, 0.78f, 0f, 3), 0.88f, new Color(Violet.r, Violet.g, Violet.b, 0.95f), 1.8f, 0.04f);
                Flat(_mark, "SigilCrescentInk", PhaisterSpellGeometry.Crescent(), 0.66f, new Color(Ink.r, Ink.g, Ink.b, 0.55f), 0f, 0.036f);
                Flat(_mark, "SigilCrescent", PhaisterSpellGeometry.Crescent(), 0.60f, new Color(Magenta.r, Magenta.g, Magenta.b, 0.95f), 1.8f, 0.042f);
                Flat(_mark, "SigilBinding", PhaisterSpellGeometry.Binding(false), 0.70f, new Color(Violet.r, Violet.g, Violet.b, 0.9f), 1.5f, 0.041f);
                for (int i = 0; i < ArrivalRunes.Length; i++)
                {
                    var row = ArrivalRunes[i];
                    Standing(_mark, "SigilRune", row.Seed, Quaternion.Euler(0f, row.A, 0f) * new Vector3(0f, 0.02f, row.R), i * 0.05f, row.Size * 1.25f);
                }
                foreach (var row in Moths)
                {
                    var m = PhaisterProp.Spawn("moth", transform, null, PhaisterProp.InsectOutlineWidth);
                    if (m == null) continue;
                    _moths.Add((m.transform, new[] { PhaisterProp.Find(m, "wing-l"), PhaisterProp.Find(m, "wing-r") }));
                    _mothFrom.Add(Vector3.zero);
                }
            }
            else
            {
                float R = VoodooRules.HigopRadius;
                Flat(_mark, "OmenAimInk", VfxShapes.Hollow(96, 0.955f, 0f, 9), R + 0.05f, new Color(Ink.r, Ink.g, Ink.b, 0.5f), 0f, 0.035f);
                Flat(_mark, "OmenAimRing", VfxShapes.Hollow(96, 0.965f, 0f, 9), R, new Color(Violet.r, Violet.g, Violet.b, 0.9f), 1.6f, 0.04f);
                for (int i = 0; i < OmenRunes.Length; i++)
                {
                    float a = -i * 30f;
                    Standing(_mark, "OmenAimRune", OmenRunes[i].Seed, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.02f, R + 0.25f), i / 12f, OmenRunes[i].Size * 0.9f);
                }
                // The foot of the line: a ring and her light on the court under the eye.
                _foot = new GameObject("OmenAimFoot").transform; _foot.SetParent(transform, false);
                Pool(_foot, 0.9f, new Color(Magenta.r, Magenta.g, Magenta.b, 0.8f));
                Flat(_foot, "OmenAimFootInk", VfxShapes.Hollow(32, 0.56f, 0f, 5), 0.74f, new Color(Ink.r, Ink.g, Ink.b, 0.55f), 0f, 0.04f);
                Flat(_foot, "OmenAimFootRing", VfxShapes.Hollow(32, 0.64f, 0f, 5), 0.70f, new Color(Magenta.r, Magenta.g, Magenta.b, 0.95f), 1.8f, 0.045f);
                // The line: a thin stripe of her light standing from the court to the eye, turned to her lens.
                var glowShader = Resources.Load<Shader>("Shaders/SpiritGlow");
                if (glowShader != null)
                {
                    _lineMat = new Material(glowShader) { name = "OmenAimLine" };
                    _lineMat.SetFloat("_Billboard", 0f); _lineMat.SetFloat("_Band", 1f); _lineMat.SetFloat("_Falloff", 1.4f);
                    _lineMat.SetFloat("_Core", 0.8f); _lineMat.SetFloat("_Tips", 0.35f);
                    _lineMat.SetColor("_Color", new Color(Violet.r, Violet.g, Violet.b, 1.1f));
                    var line = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    line.name = "OmenAimLine"; VfxMaterial.StripCollider(line); line.transform.SetParent(transform, false);
                    var lr = line.GetComponent<Renderer>(); lr.sharedMaterial = _lineMat; VfxRenderTag.Own(line, _lineMat);
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
                    _line = line.transform;
                }
                // The ghost eye: the real eye's window, small and unstable, not yet thrown.
                var eye = GameObject.CreatePrimitive(PrimitiveType.Quad);
                eye.name = "OmenAimEye"; VfxMaterial.StripCollider(eye);
                eye.transform.SetParent(transform, false);
                var shader = Resources.Load<Shader>("Shaders/CosmosEye");
                var er = eye.GetComponent<Renderer>();
                er.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; er.receiveShadows = false;
                if (shader != null) { _eyeMat = new Material(shader); er.sharedMaterial = _eyeMat; VfxRenderTag.Own(eye, _eyeMat); }
                else VfxMaterial.Ghost(er, new Color(Ink.r, Ink.g, Ink.b, 0.8f), 0f);
                _eye = eye.transform;
                _haloMat = Glow(new Color(Magenta.r, Magenta.g, Magenta.b, 0.5f));
                if (_haloMat != null)
                {
                    var halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    halo.name = "OmenAimHalo"; VfxMaterial.StripCollider(halo);
                    halo.transform.SetParent(transform, false);
                    halo.GetComponent<Renderer>().sharedMaterial = _haloMat; VfxRenderTag.Own(halo, _haloMat);
                    _halo = halo.transform;
                }
                for (int i = 0; i < Beads; i++)
                {
                    var bm = Glow(i % 2 == 0 ? new Color(Violet.r, Violet.g, Violet.b, 0.9f) : new Color(Magenta.r, Magenta.g, Magenta.b, 0.9f));
                    if (bm == null) break;
                    var bead = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    bead.name = "OmenAimBead"; VfxMaterial.StripCollider(bead);
                    bead.transform.SetParent(transform, false);
                    bead.GetComponent<Renderer>().sharedMaterial = bm; VfxRenderTag.Own(bead, bm);
                    _beads.Add((bead.transform, bm));
                }
            }
            // The embers it burns out into: eight, typed drifts (right, up, forward), rising and spreading.
            var drifts = new[]
            {
                new Vector3(0.20f, 0.9f, 0.05f), new Vector3(-0.25f, 1.1f, 0.10f), new Vector3(0.05f, 0.8f, -0.30f), new Vector3(0.30f, 1.2f, -0.12f),
                new Vector3(-0.10f, 0.7f, 0.28f), new Vector3(-0.32f, 1.0f, -0.20f), new Vector3(0.14f, 1.3f, 0.24f), new Vector3(-0.05f, 0.95f, 0.02f),
            };
            for (int i = 0; i < drifts.Length; i++)
            {
                var em = Glow(i % 3 == 0 ? new Color(Magenta.r, Magenta.g, Magenta.b, 1f) : new Color(Violet.r, Violet.g, Violet.b, 1f));
                if (em == null) break;
                var ember = GameObject.CreatePrimitive(PrimitiveType.Quad);
                ember.name = "SigilEmber"; VfxMaterial.StripCollider(ember);
                ember.transform.SetParent(transform, false);
                ember.GetComponent<Renderer>().sharedMaterial = em; VfxRenderTag.Own(ember, em);
                ember.SetActive(false);
                _embers.Add((ember.transform, em, drifts[i]));
            }
        }

        /// <summary>A pool of her light laid on the court (additive, soft, no edge), under a mark.</summary>
        private void Pool(Transform parent, float radius, Color c)
        {
            var shader = Resources.Load<Shader>("Shaders/SpiritGlow");
            if (shader == null) return;
            var m = new Material(shader) { name = "AimPool" };
            m.SetFloat("_Billboard", 0f); m.SetFloat("_Falloff", 1.5f); m.SetFloat("_Core", 0.3f); m.SetColor("_Color", c);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "AimPool"; VfxMaterial.StripCollider(q); q.transform.SetParent(parent, false);
            q.transform.localPosition = Vector3.up * 0.03f; q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = Vector3.one * radius * 2f;
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; VfxRenderTag.Own(q, m);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            _pools.Add((m, c.a));
        }
        private readonly List<(Material M, float A)> _pools = new List<(Material, float)>();

        /// <summary>One of her runes STANDING on the ring (upright, both sides), written in at <paramref name="at"/> of the write.</summary>
        private void Standing(Transform parent, string name, int seed, Vector3 local, float at, float size)
        {
            // The glyph sits on a holder at its foot, so scaling the holder up grows it out of the court rather than from its middle.
            var holder = new GameObject(name).transform;
            holder.SetParent(parent, false); holder.localPosition = local;
            var mesh = VfxShapes.TwoSided(VfxShapes.Rune(seed, 0.14f));
            var go = VfxShapes.Stand(holder, name + "Glyph", mesh, 1f);
            go.transform.localPosition = new Vector3(0f, -mesh.bounds.min.y, 0f);
            var r = go.GetComponent<Renderer>();
            VfxMaterial.Ghost(r, new Color(Violet.r, Violet.g, Violet.b, 0f), 1.8f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            _runes.Add((holder, r.sharedMaterial, at, size));
        }

        private bool OwnersView()
        {
            if (_caster == null) return false;
            if (_rig == null && Time.unscaledTime - _rigSearchAt > 0.5f)
            {
                _rigSearchAt = Time.unscaledTime;
                _rig = FindFirstObjectByType<CameraSystem.CameraRig>();
            }
            return _rig != null && _rig.IsFollowing(_caster);
        }

        /// <summary>Every frame of the hold: <paramref name="at"/> is where it would land (with its height, for OMEN).</summary>
        public void Show(CharacterMotor caster, Vector3 at)
        {
            _caster = caster;
            if (_releasedAt >= 0f) return;
            bool mine = OwnersView();
            if (!mine) { if (gameObject.activeSelf) gameObject.SetActive(false); _shownAt = -1f; return; }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (_shownAt < 0f)
            {
                _shownAt = _age;
                // The moths start in her cuffs.
                for (int i = 0; i < _mothFrom.Count; i++)
                    _mothFrom[i] = caster.transform.position + caster.transform.right * (i % 2 == 0 ? 0.32f : -0.32f) + Vector3.up * 0.85f;
            }
            _at = at;
            _ground = PhaisterProp.OnCourt(at);
        }

        /// <summary>The hold is over (cast, cancelled or refused): flare, then burn out as embers where it lies.</summary>
        public void Release()
        {
            if (_releasedAt >= 0f) return;
            // ⚠️ OMEN's release starts its cutscene at once, and the cutscene pauses the world (`Time.timeScale` 0), so a burn-out
            // driven by `Time.deltaTime` froze mid-way and the cutscene's camera filmed the ghost eye, its line and the ring for all
            // five seconds (film v10). It goes on the release.
            if (!gameObject.activeSelf || _kind == Kind.Omen) { Destroy(gameObject); return; }
            _releasedAt = _age;
            for (int i = 0; i < _embers.Count; i++)
            {
                var e = _embers[i];
                e.T.gameObject.SetActive(true);
                float a = i * 45f * Mathf.Deg2Rad;
                float r = _kind == Kind.Arrival ? 0.8f : 1.2f;
                e.T.position = (_kind == Kind.Arrival ? _ground : _at) + new Vector3(Mathf.Sin(a) * r, 0.1f, Mathf.Cos(a) * r);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_shownAt < 0f && _releasedAt < 0f) return;
            float held = _releasedAt >= 0f ? _releasedAt - _shownAt : _age - _shownAt;
            float after = _releasedAt >= 0f ? _age - _releasedAt : -1f;
            // The flare (0 to 0.1 s after the release), then the burn (to 0.6 s): everything thins and sinks as embers rise.
            float flare = after >= 0f ? Mathf.Clamp01(1f - Mathf.Abs(after - 0.05f) / 0.05f) : 0f;
            float burn = after >= 0f ? Mathf.Clamp01(1f - (after - 0.1f) / 0.5f) : 1f;
            if (after > 0.75f) { Destroy(gameObject); return; }

            transform.position = _ground;
            _mark.localRotation = Quaternion.Euler(0f, -18f * held, 0f);
            float grow = Mathf.Clamp01(held / 0.18f);
            _mark.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, grow * (2f - grow)) * (1f + 0.12f * flare);
            foreach (var (t, m, a) in _parts) PhaisterProp.SetAlpha(m, a * Mathf.Clamp01(held / 0.12f) * burn * (1f + 0.3f * flare));
            float writeSpan = _kind == Kind.Arrival ? 0.30f : 0.55f;
            var lens = Camera.main != null ? Camera.main.transform.position : transform.position + Vector3.back;
            foreach (var (t, m, at, size) in _runes)
            {
                float u = Mathf.Clamp01((held / writeSpan - at) / 0.18f);
                t.gameObject.SetActive(u > 0f);
                PhaisterProp.SetAlpha(m, 0.95f * u * burn);
                // Standing, growing up out of the court as it is written, and turned square to her lens (yaw only).
                float k = size * (1f + 0.15f * flare);
                t.localScale = new Vector3(k, k * Mathf.Lerp(0.05f, 1f, u * (2f - u)), k);
                var toLens = lens - t.position; toLens.y = 0f;
                if (toLens.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(-toLens.normalized, Vector3.up);
            }
            foreach (var (pm, pa) in _pools)
                pm.SetColor("_Color", new Color(pm.GetColor("_Color").r, pm.GetColor("_Color").g, pm.GetColor("_Color").b, pa * Mathf.Clamp01(held / 0.2f) * burn * (1f + 0.6f * flare)));

            if (_kind == Kind.Arrival)
            {
                for (int i = 0; i < _moths.Count; i++)
                {
                    var (t, w) = _moths[i];
                    var row = Moths[i];
                    // Out of her cuffs to their orbit (0.3 s), then round and round, clockwise seen from above.
                    float fly = Mathf.Clamp01(held / 0.3f);
                    float a = (row.A - 360f * 1.5f * held) * Mathf.Deg2Rad;
                    Vector3 orbit = _ground + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * row.R + Vector3.up * (row.Y + 0.06f * Mathf.Sin(held * 5f + i));
                    Vector3 p = Vector3.Lerp(_mothFrom[i], orbit, fly * fly * (3f - 2f * fly)) + Vector3.up * Mathf.Sin(fly * Mathf.PI) * 0.5f;
                    // On the release they dive into the middle and are gone (they join the swarm that carries her there).
                    if (after >= 0f) p = Vector3.Lerp(p, _ground + Vector3.up * 0.3f, Mathf.Clamp01(after / 0.15f));
                    Vector3 heading = new Vector3(-Mathf.Cos(a), 0f, Mathf.Sin(a));
                    t.position = p;
                    t.rotation = Quaternion.LookRotation(heading, Vector3.up);
                    float scale = row.Size * (after >= 0f ? Mathf.Clamp01(1f - after / 0.15f) : 1f);
                    t.localScale = Vector3.one * Mathf.Max(0.0001f, scale);
                    t.gameObject.SetActive(scale > 0.01f);
                    float open = 10f + 55f * (0.5f + 0.5f * Mathf.Sin(_age * row.Hz * Mathf.PI * 2f + i));
                    if (w[0] != null) w[0].localRotation = Quaternion.AngleAxis(open, Vector3.forward);
                    if (w[1] != null) w[1].localRotation = Quaternion.AngleAxis(-open, Vector3.forward);
                }
            }
            else
            {
                // The eye at its height, unstable (it has not been thrown yet), a halo round it, and a line of lights up to it.
                float h = Mathf.Max(0.5f, _at.y - _ground.y);
                Vector3 eyeAt = _ground + Vector3.up * h;
                float pulse = 1f + 0.12f * Mathf.Sin(_age * 9f) + 0.06f * Mathf.Sin(_age * 23f);
                float live = Mathf.Clamp01(held / 0.2f) * burn;
                if (_eye != null)
                {
                    // Near the size it will open to (a 1.45 m hole, the real one is 2.4), so she reads how big as well as how high.
                    _eye.position = eyeAt;
                    _eye.localScale = Vector3.one * (1.45f / PhaisterOmen.CosmosOpen) * pulse * live * (1f + 0.3f * flare);
                    _eye.gameObject.SetActive(live > 0.02f);
                    if (_eyeMat != null) { _eyeMat.SetFloat("_Open", 0.78f); _eyeMat.SetFloat("_Time0", _age); _eyeMat.SetFloat("_Glitch", 0.75f); }
                }
                if (_halo != null)
                {
                    _halo.position = eyeAt;
                    _halo.localScale = Vector3.one * 2.4f * pulse * live;
                    _haloMat.SetColor("_Color", new Color(Magenta.r, Magenta.g, Magenta.b, 0.45f * live));
                }
                if (_foot != null) _foot.position = _ground;
                if (_line != null)
                {
                    // The quad's u runs up the line (turned a quarter round z), its face turned to her lens.
                    Vector3 mid = _ground + Vector3.up * (h * 0.5f);
                    var toLens = lens - mid; toLens.y = 0f;
                    _line.position = mid;
                    _line.rotation = (toLens.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-toLens.normalized, Vector3.up) : Quaternion.identity) * Quaternion.Euler(0f, 0f, 90f);
                    _line.localScale = new Vector3(h, 0.16f, 1f);
                    _lineMat.SetColor("_Color", new Color(Violet.r, Violet.g, Violet.b, 1.1f * live));
                }
                for (int i = 0; i < _beads.Count; i++)
                {
                    var (t, m) = _beads[i];
                    float u = (i + 0.5f) / _beads.Count;
                    t.position = _ground + Vector3.up * (h * u);
                    // A light climbs the line, bead to bead, once every 0.8 s.
                    float climb = Mathf.Repeat(_age / 0.8f, 1f);
                    float hot = Mathf.Clamp01(1f - Mathf.Abs(climb - u) * 6f);
                    t.localScale = Vector3.one * (0.18f + 0.16f * hot) * live;
                    var c = i % 2 == 0 ? Violet : Magenta;
                    m.SetColor("_Color", new Color(c.r, c.g, c.b, (0.55f + 0.8f * hot) * live));
                }
            }

            for (int i = 0; i < _embers.Count; i++)
            {
                var (t, m, drift) = _embers[i];
                if (after < 0f) continue;
                float u = Mathf.Clamp01((after - 0.05f) / 0.6f);
                t.position += drift * dt * 1.2f;
                t.localScale = Vector3.one * 0.14f * (1f - u);
                var c = m.GetColor("_Color"); c.a = 1.4f * (1f - u); m.SetColor("_Color", c);
            }
        }
    }
}
