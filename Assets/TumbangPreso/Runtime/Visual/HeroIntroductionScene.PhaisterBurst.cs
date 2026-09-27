using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PHAISTER, OMEN: THE AIR AROUND HER (HERO-10 v8, the method's density pass, `HERO_KIT_METHOD.md` section 6; the references
        // re-read for it, `direction.md` section 0). Each layer answers one frame of the footage; each is a typed table:
        //
        //  * THE DOMAIN (Castorice 5 to 7 s: the ground becomes a field of small lights): as her night falls, points of her light
        //    come on over the court, out from her feet, and twinkle; more come on round the spot the eye lands on.
        //  * THE COLUMN (Castorice 2.5 s: tall wavy ribbons climb round her): five ribbons of her light spiral UP round her through
        //    SURGE, turning CLOCKWISE, and let go as she gathers the butterflies.
        //  * THE STROKES (Seele 8.3 s: curved streaks follow the camera's orbit): three arcs whip round her at the height of her
        //    hands, clockwise, faster than anything else in the shot (speed lives in streaks, wings stay slow).
        //  * THE NEAR LAYER (Castorice 25 s: butterflies cross the lens on diagonals): four of hers cross close to the lens, LEFT TO
        //    RIGHT, in SURGE.
        //  * HER EYES (Seele 7.9 s, a glint on the eye; Castorice 23 s, a streak across a calm face): in THE EYE her half-shut ink
        //    eyes light violet from within, a four-point glint on one, one thin streak across both; the light on her face is
        //    the eye's.
        //  * THE THROW'S STROKE (Seele's strike): a stroke of her light follows the eye across the frame, a dark body under it.
        //  * THE VEIL: the frame's edges sink into her plum dark from the first frame to the hand-back (`SpiritVeil`).
        // Her colours only, never white. Reduced effects keeps the shapes and halves the light.
        // =========================================================================================

        private static readonly Color PhViolet = new Color(.66f, .34f, 1f), PhMagenta = new Color(.90f, .20f, .56f), PhLilac = new Color(.80f, .62f, 1f);

        // THE DOMAIN, typed: (angle round her deg, distance m, size m, comes on at s, twinkle Hz). Out from her feet as the night falls.
        private static readonly Vector4[] PhbDomainRows =
        {
            new Vector4(  12f, 1.1f, .10f, .08f), new Vector4(  63f, 1.6f, .08f, .12f), new Vector4( 118f, 1.3f, .11f, .10f),
            new Vector4( 171f, 1.8f, .07f, .16f), new Vector4( 224f, 1.2f, .10f, .11f), new Vector4( 287f, 1.7f, .09f, .15f),
            new Vector4( 331f, 1.5f, .08f, .13f), new Vector4(  38f, 2.4f, .09f, .22f), new Vector4(  94f, 2.9f, .07f, .27f),
            new Vector4( 146f, 2.2f, .10f, .20f), new Vector4( 199f, 2.7f, .08f, .25f), new Vector4( 252f, 2.3f, .09f, .21f),
            new Vector4( 306f, 3.1f, .07f, .29f), new Vector4( 352f, 2.6f, .10f, .24f), new Vector4(  21f, 3.6f, .08f, .33f),
            new Vector4(  77f, 4.2f, .07f, .38f), new Vector4( 133f, 3.9f, .09f, .36f), new Vector4( 184f, 4.6f, .06f, .42f),
            new Vector4( 239f, 3.4f, .08f, .31f), new Vector4( 271f, 4.4f, .07f, .40f), new Vector4( 318f, 3.8f, .09f, .35f),
            new Vector4(  55f, 5.3f, .07f, .48f), new Vector4( 108f, 5.8f, .06f, .53f), new Vector4( 162f, 5.1f, .08f, .46f),
            new Vector4( 214f, 6.0f, .06f, .55f), new Vector4( 296f, 5.5f, .07f, .50f), new Vector4( 341f, 6.3f, .06f, .58f),
            new Vector4(   3f, 4.9f, .08f, .45f),
        };
        // Round the spot as the eye lands: (angle deg, distance m, size m, comes on after the landing s).
        private static readonly Vector4[] PhbLandRows =
        {
            new Vector4(  20f, 1.4f, .09f, .04f), new Vector4(  84f, 2.1f, .08f, .08f), new Vector4( 150f, 1.7f, .10f, .06f),
            new Vector4( 205f, 2.6f, .07f, .12f), new Vector4( 262f, 1.9f, .09f, .07f), new Vector4( 318f, 2.8f, .07f, .14f),
            new Vector4(  50f, 3.4f, .08f, .18f), new Vector4( 118f, 3.9f, .07f, .22f), new Vector4( 176f, 3.2f, .08f, .17f),
            new Vector4( 236f, 4.3f, .06f, .25f), new Vector4( 292f, 3.7f, .07f, .20f), new Vector4( 345f, 4.6f, .06f, .27f),
        };
        // THE COLUMN, typed: (start angle deg, radius m, turns over its height, width m), each its own ribbon.
        // v2 (film v10: at 0.6 to 0.74 m they crossed in front of her face in the orbit): out at 1.0 to 1.2 m, round her, not across her.
        private static readonly Vector4[] PhbColumnRows =
        {
            new Vector4(  0f, 1.05f, .55f, .10f), new Vector4( 72f, 1.18f, .48f, .08f), new Vector4(144f, 1.00f, .62f, .11f),
            new Vector4(216f, 1.22f, .44f, .07f), new Vector4(288f, 1.10f, .58f, .09f),
        };
        // THE STROKES, typed: (start angle deg, radius m, height m, length of arc deg, turns a second).
        private static readonly (float A, float R, float Y, float Arc, float Spin)[] PhbStrokeRows =
        {
            (30f, 1.15f, .95f, 70f, 1.1f), (150f, 1.35f, 1.45f, 60f, .95f), (260f, 1.25f, 1.9f, 80f, 1.2f),
        };
        // THE NEAR LAYER, typed: (start time s, height on the frame -1 to 1 at the start, at the end, share of the way to the focus).
        private static readonly Vector4[] PhbNearRows =
        {
            new Vector4(.00f, -.55f, -.05f, .24f), new Vector4(.22f, .45f, .80f, .30f), new Vector4(.48f, -.20f, .35f, .20f), new Vector4(.74f, .70f, .30f, .27f),
        };
        private const float PhbNearCross = .62f;

        private readonly List<int> _phbDomain = new List<int>(28), _phbLand = new List<int>(12);
        private readonly List<LineRenderer> _phbColumn = new List<LineRenderer>(5), _phbStrokes = new List<LineRenderer>(3);
        private readonly List<(Transform T, Transform[] W)> _phbNear = new List<(Transform, Transform[])>();
        private LineRenderer _phbThrowLight, _phbThrowBody;
        private int _phbEyeLeft = -1, _phbEyeRight = -1, _phbGlint = -1, _phbStreak = -1;
        private Vector3 _phbEyeLocalL, _phbEyeLocalR;
        private bool _phbHaveEyes;
        private Material _phbVeil;
        private readonly Vector3[] _phbPoints = new Vector3[24], _phbThrow = new Vector3[20], _phbColumnPoints = new Vector3[18], _phbStrokePoints = new Vector3[12];

        private LineRenderer PhbLight(string name, int points, float width, Color colour)
        {
            var go = new GameObject(name); go.transform.SetParent(_root.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false;
            line.positionCount = points; line.widthMultiplier = width; line.numCapVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch;
            var shader = Resources.Load<Shader>("Shaders/SpiritGlow");
            if (shader != null)
            {
                var m = new Material(shader) { name = name };
                m.SetFloat("_Billboard", 0f); m.SetFloat("_Band", 1f); m.SetFloat("_Falloff", 1.5f); m.SetFloat("_Core", .7f); m.SetFloat("_Tips", .6f);
                m.SetColor("_Color", colour);
                line.sharedMaterial = m; VfxRenderTag.Own(go, m);
            }
            else VfxMaterial.Ghost(line, colour, 1f);
            _lines.Add(line);
            return line;
        }

        private void BuildPhaisterBurst()
        {
            foreach (var _ in PhbDomainRows) _phbDomain.Add(AddGlow("OmenDomainLight", PhLilac, falloff: 2.2f, core: .9f, lift: .05f));
            foreach (var _ in PhbLandRows) _phbLand.Add(AddGlow("OmenLandLight", PhMagenta, falloff: 2.2f, core: .9f, lift: .05f));
            foreach (var row in PhbColumnRows) _phbColumn.Add(PhbLight("OmenColumn", 18, row.w, PhViolet));
            foreach (var _ in PhbStrokeRows) _phbStrokes.Add(PhbLight("OmenStroke", 12, .07f, PhLilac));
            foreach (var row in PhbNearRows)
            {
                var b = PhaisterProp.Spawn("butterfly", _root.transform, null, PhaisterProp.InsectOutlineWidth);
                if (b == null) continue;
                SetLayer(b, _root.layer);
                _phbNear.Add((b.transform, new[] { PhaisterProp.Find(b, "wing-l"), PhaisterProp.Find(b, "wing-r") }));
            }
            // The throw's stroke: her light down the middle of a dark body, following the eye from her hands.
            _phbThrowBody = Line("OmenThrowBody", 20, .34f, new Color(.18f, .05f, .26f, .7f));
            _phbThrowLight = PhbLight("OmenThrowLight", 20, .16f, PhMagenta);
            // Her eyes: two small lights and a glint, and the streak across them.
            _phbEyeLeft = AddGlow("OmenEyeLightL", PhViolet, falloff: 2.4f, core: 1.2f, lift: .06f);
            _phbEyeRight = AddGlow("OmenEyeLightR", PhViolet, falloff: 2.4f, core: 1.2f, lift: .06f);
            _phbGlint = Add("OmenEyeGlint", VfxShapes.TwoSided(VfxShapes.Star(4, .18f, 3)), new Color(PhLilac.r, PhLilac.g, PhLilac.b, .95f), 2.2f);
            _phbStreak = AddGlow("OmenEyeStreak", PhMagenta, band: true, falloff: 1.6f, core: .8f, lift: .08f);
            _phbHaveEyes = FindHerEyes(out _phbEyeLocalL, out _phbEyeLocalR);
            // The veil: a clip-space quad, the frame's edges in her plum.
            var veilShader = Resources.Load<Shader>("Shaders/SpiritVeil");
            if (veilShader != null)
            {
                var go = new GameObject("OmenVeil"); go.transform.SetParent(_root.transform, false);
                var mesh = new Mesh { name = "OmenVeilQuad" };
                mesh.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) };
                mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
                go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>();
                VfxShapes.Own(go, mesh);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                _phbVeil = new Material(veilShader) { name = "OmenVeil" };
                r.sharedMaterial = _phbVeil; VfxRenderTag.Own(go, _phbVeil);
            }
        }

        /// <summary>
        /// ⚠️ HER EYES, FOUND ON HER OWN MESH (her face is ink on the donor skull, `tools/build_phaister_voxel.py`: the eyes are the
        /// INK cells of the face above the mouth). The copy's skin is baked once, the ink vertices in the top of the model are taken,
        /// split left and right, and each eye is kept in the head bone's space so it follows her head through every key. False, and
        /// no eye light, if it cannot find two eyes where a face should be.
        /// </summary>
        private bool FindHerEyes(out Vector3 left, out Vector3 right)
        {
            left = right = Vector3.zero;
            if (_head == null) return false;
            foreach (var r in _bodyRenderers)
            {
                if (!(r is SkinnedMeshRenderer skin) || skin.sharedMesh == null) continue;
                var baked = new Mesh();
                try
                {
                    skin.BakeMesh(baked, true);
                    var v = baked.vertices; var uv = skin.sharedMesh.uv;
                    if (uv == null || uv.Length != v.Length) continue;
                    float top = float.NegativeInfinity, bottom = float.PositiveInfinity;
                    var world = new Vector3[v.Length];
                    for (int i = 0; i < v.Length; i++) { world[i] = skin.transform.TransformPoint(v[i]); top = Mathf.Max(top, world[i].y); bottom = Mathf.Min(bottom, world[i].y); }
                    float height = top - bottom;
                    // INK is palette slot 8: atlas column 1, row 13 of 16 (v flipped or not by the importer).
                    var ink = new List<Vector3>();
                    for (int i = 0; i < v.Length; i++)
                    {
                        float u = uv[i].x * 16f, w = uv[i].y * 16f;
                        bool cell = u >= 1f && u < 2f && ((w >= 13f && w < 14f) || (w >= 2f && w < 3f));
                        float share = (world[i].y - bottom) / Mathf.Max(.01f, height);
                        if (cell && share > .45f && share < .85f) ink.Add(world[i]);
                    }
                    if (ink.Count < 6) continue;
                    // The eyes are the highest ink on the face: keep what is within 6 cm of the top of it.
                    float inkTop = float.NegativeInfinity; foreach (var p in ink) inkTop = Mathf.Max(inkTop, p.y);
                    Vector3 headAt = _head.position, side = _root.transform.right;
                    Vector3 sumL = Vector3.zero, sumR = Vector3.zero; int nL = 0, nR = 0;
                    foreach (var p in ink)
                    {
                        if (p.y < inkTop - .06f) continue;
                        if (Vector3.Dot(p - headAt, side) < 0f) { sumL += p; nL++; } else { sumR += p; nR++; }
                    }
                    if (nL == 0 || nR == 0) continue;
                    left = _head.InverseTransformPoint(sumL / nL); right = _head.InverseTransformPoint(sumR / nR);
                    return Vector3.Distance(sumL / nL, sumR / nR) is > .04f and < .6f;
                }
                finally { ObjectDestroy(baked); }
            }
            return false;
        }

        private void SamplePhaisterBurst(float t, float leave, float lift)
        {
            float light = _reducedEffects ? .5f : 1f;
            Vector3 up = Vector3.up;

            // THE DOMAIN.
            for (int i = 0; i < _phbDomain.Count; i++)
            {
                var row = PhbDomainRows[i];
                float on = Ease(row.w, row.w + .25f, t) * leave;
                float a = row.x * Mathf.Deg2Rad;
                float twinkle = .6f + .4f * Mathf.Sin(t * (3f + (i % 5) * .7f) + i * 2.1f);
                PlaceGlow(_phbDomain[i], new Vector3(Mathf.Sin(a) * row.y, .06f, Mathf.Cos(a) * row.y), Vector3.one * row.z * 2.2f, Quaternion.identity, on * twinkle * 1.3f * light);
            }
            for (int i = 0; i < _phbLand.Count; i++)
            {
                var row = PhbLandRows[i];
                float on = Ease(PhLandAt + row.w, PhLandAt + row.w + .2f, t) * leave;
                float a = row.x * Mathf.Deg2Rad;
                float twinkle = .6f + .4f * Mathf.Sin(t * (3.4f + (i % 4) * .6f) + i * 1.7f);
                PlaceGlow(_phbLand[i], _phGround + new Vector3(Mathf.Sin(a) * row.y, .06f, Mathf.Cos(a) * row.y), Vector3.one * row.z * 2.2f, Quaternion.identity, on * twinkle * 1.3f * light);
            }

            // THE COLUMN: up round her, clockwise, growing to its full height, letting go as she gathers them in.
            float grow = Ease(.2f, .8f, t), fade = 1f - Ease(1.25f, 1.55f, t);
            for (int i = 0; i < _phbColumn.Count; i++)
            {
                var row = PhbColumnRows[i]; var line = _phbColumn[i];
                float strength = grow * fade * leave;
                if (strength < .01f) { line.enabled = false; continue; }
                line.enabled = true;
                float spin = -120f * t;                     // clockwise seen from above
                for (int k = 0; k < line.positionCount; k++)
                {
                    float u = k / (line.positionCount - 1f);
                    float a = (row.x + spin - 360f * row.z * u) * Mathf.Deg2Rad;
                    float r = row.y * (1f + .15f * Mathf.Sin(u * 7f + t * 5f + i));
                    _phbColumnPoints[k] = new Vector3(Mathf.Sin(a) * r, lift + u * 3.0f * grow, Mathf.Cos(a) * r);
                }
                line.SetPositions(_phbColumnPoints);
                line.sharedMaterial.SetColor("_Color", new Color(PhViolet.r, PhViolet.g, PhViolet.b, 1.2f * strength * light));
            }

            // THE STROKES: arcs whipping round her at her hands' height, clockwise, fast.
            float whip = Ease(.25f, .4f, t) * (1f - Ease(1.1f, 1.3f, t)) * leave;
            for (int i = 0; i < _phbStrokes.Count; i++)
            {
                var row = PhbStrokeRows[i]; var line = _phbStrokes[i];
                if (whip < .01f) { line.enabled = false; continue; }
                line.enabled = true;
                // The arc's head leads, clockwise (the angle falls), its tail trailing behind it.
                float head = row.A - 360f * row.Spin * t;
                for (int k = 0; k < line.positionCount; k++)
                {
                    float a = (head + row.Arc * (k / (line.positionCount - 1f))) * Mathf.Deg2Rad;
                    _phbStrokePoints[k] = new Vector3(Mathf.Sin(a) * row.R, lift + row.Y, Mathf.Cos(a) * row.R);
                }
                line.SetPositions(_phbStrokePoints);
                line.sharedMaterial.SetColor("_Color", new Color(PhLilac.r, PhLilac.g, PhLilac.b, 1.4f * whip * light));
            }

            // THE NEAR LAYER: across the lens, left to right, in SURGE.
            bool lens = PhLens(t, out var eye);
            Vector3 focus = new Vector3(0f, 1.3f + lift, 0f);
            for (int i = 0; i < _phbNear.Count; i++)
            {
                var (b, w) = _phbNear[i]; var row = PhbNearRows[i];
                float u = (t - row.x) / PhbNearCross;
                bool on = lens && u >= 0f && u <= 1f && t < PhEyeAt && !_reducedEffects;
                b.gameObject.SetActive(on);
                if (!on) continue;
                Vector3 fwd = (focus - eye).normalized, right = Vector3.Cross(Vector3.up, fwd).normalized, lensUp = Vector3.Cross(fwd, right);
                float depth = Vector3.Distance(eye, focus) * row.w;
                float halfH = depth * Mathf.Tan(25f * Mathf.Deg2Rad), halfW = halfH * 16f / 9f;
                float x = Mathf.Lerp(-1.3f, 1.3f, u), y = Mathf.Lerp(row.y, row.z, u);
                b.localPosition = eye + fwd * depth + right * x * halfW + lensUp * y * halfH;
                b.localRotation = Quaternion.LookRotation(right * 2f + lensUp * (row.z - row.y), -fwd);
                b.localScale = Vector3.one * .9f * leave;
                float open = 10f + 60f * (.5f + .5f * Mathf.Sin(t * (3.4f + i * .4f) * Mathf.PI * 2f + i));
                if (w[0] != null) w[0].localRotation = Quaternion.AngleAxis(open, Vector3.forward);
                if (w[1] != null) w[1].localRotation = Quaternion.AngleAxis(-open, Vector3.forward);
            }

            // HER EYES: lit from 1.38 s, the glint at 1.42, the streak across them; the left goes dark for her wink at 2.18.
            if (_phbHaveEyes && _head != null)
            {
                Vector3 l = _root.transform.InverseTransformPoint(_head.TransformPoint(_phbEyeLocalL));
                Vector3 r = _root.transform.InverseTransformPoint(_head.TransformPoint(_phbEyeLocalR));
                float lit = Ease(1.34f, 1.42f, t) * (1f - Ease(PhThrowAt + .2f, PhLandAt, t)) * leave;
                float wink = 1f - Mathf.Clamp01(1f - Mathf.Abs(t - 2.2f) / .07f);
                PlaceGlow(_phbEyeLeft, l, Vector3.one * .16f, Quaternion.identity, 1.6f * lit * wink * light, Vector3.forward);
                PlaceGlow(_phbEyeRight, r, Vector3.one * .16f, Quaternion.identity, 1.6f * lit * light, Vector3.forward);
                float glint = Mathf.Clamp01(1f - Mathf.Abs(t - 1.44f) / .1f);
                // The star is drawn flat (its face up): turned to face the lens, spinning a little as it pops.
                Vector3 toLens = lens ? (eye - r).normalized : Vector3.forward;
                Place(_phbGlint, r + toLens * .05f, Vector3.one * .16f * glint, Quaternion.FromToRotation(Vector3.up, toLens) * Quaternion.Euler(0f, 90f * t, 0f), glint * leave);
                float streak = Mathf.Clamp01(1f - Mathf.Abs(t - 1.46f) / .22f);
                PlaceGlow(_phbStreak, (l + r) * .5f + Vector3.forward * .04f, new Vector3(1.4f * streak + .01f, .05f, 1f), Quaternion.identity, 1.3f * streak * light * leave);
            }

            // THE THROW'S STROKE: her light following the eye from her hands to where it is, its tail thinning away.
            float stroke = Ease(PhThrowAt, PhThrowAt + .05f, t) * (1f - Ease(PhLandAt + .05f, PhLandAt + .4f, t)) * leave;
            if (stroke > .01f)
            {
                _phbThrowLight.enabled = _phbThrowBody.enabled = true;
                float head = Mathf.Min(t, PhLandAt), tail = Mathf.Max(PhThrowAt, head - .3f - .5f * Ease(PhLandAt, PhLandAt + .4f, t));
                for (int k = 0; k < 20; k++) _phbPoints[k] = OmenEyeAt(Mathf.Lerp(tail, head, k / 19f));
                System.Array.Copy(_phbPoints, _phbThrow, 20);
                _phbThrowLight.SetPositions(_phbThrow); _phbThrowBody.SetPositions(_phbThrow);
                _phbThrowLight.sharedMaterial.SetColor("_Color", new Color(PhMagenta.r, PhMagenta.g, PhMagenta.b, 1.5f * stroke * light));
            }
            else _phbThrowLight.enabled = _phbThrowBody.enabled = false;

            // THE VEIL: her plum at the frame's edges, deepest through THE EYE, eased for THE MARK, gone at the hand-back.
            if (_phbVeil != null)
            {
                float veil = Ease(0f, .3f, t) * (1f - .4f * Ease(PhMarkAt, PhMarkAt + .4f, t)) * leave;
                _phbVeil.SetColor("_Color", new Color(.16f, .02f, .14f, .62f * veil));
                _phbVeil.SetFloat("_Inner", .40f); _phbVeil.SetFloat("_Outer", 1.2f); _phbVeil.SetFloat("_Tint", .12f);
            }
        }
    }
}
