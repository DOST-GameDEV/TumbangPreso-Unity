using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ RAGO'S RED SKY STAYS UNTIL HIS VOLCANO IS GONE (owner, 2026-11-23, of cutscene v4: *"keep the sky until the end of
    /// the effect in live too outside of cutscene"*). The cutscene turns the sky (`HeroIntroductionScene.SeanSky.cs`); this is
    /// the same sky in play, worn by the live `SeanVolcano` for its whole life and gone as it sinks:
    ///
    ///   THE SKY     `SeanSky.shader`, the cutscene's own far sky, drawn only where the map shows sky, so nothing of the street
    ///               is hidden from anybody.
    ///   THE LIGHT   `SeanSkyGrade.shader`, the cutscene's multiply grade, in clip space, so it covers whatever camera draws it.
    ///   ASH         `SeanAshFall.shader`, flakes round whichever lens draws them.
    ///   THE CRATER  one point light in the crater, as the cutscene's.
    ///
    /// ⚠️ ONE CLOCK, CARRIED ACROSS THE CUT. Everything here is posed from the cutscene's own scene clock, which play carries on
    /// as `SeanRules.CutsceneSeconds + age` (a fresh cast with no cutscene is the same clock from the rise). The curves both
    /// sides use are the static functions below, so the cutscene's last frame and play's first are the same sky by
    /// construction, not by two tables kept in step.
    ///
    /// ⚠️ PLAY'S GRADE IS LIGHTER THAN THE CUTSCENE'S (`PlayGrade`), and the two are blended across the hand-back (from the home
    /// move, `BlendFrom`, to `BlendTo`, two seconds into play): under the full cutscene red the can, the chalk and the other
    /// players lose the value contrast the fight needs (`docs/VISION.md` § 2 rule 5). The blend runs on the shared clock, so
    /// it starts in the cutscene and nothing jumps at the cut.
    ///
    /// ⚠️ COSMETIC AND DERIVED ON EVERY PEER. Every peer already builds its own volcano from the accepted cast, on its own clock
    /// (`SeanVolcano`), so every peer builds its own sky from that volcano's age: nothing goes on the wire, no rule changes,
    /// and everyone in the match sees it, not only the caster. It reads the camera only in its shaders
    /// (`_WorldSpaceCameraPos`, clip space), so it works from first person, third person and a spectator's free camera
    /// alike. Preview and UI cameras cull by their own layers and never draw it.
    ///
    /// ⚠️ IT IS NOT GLOBAL STATE, SO IT CANNOT LEAK. No `RenderSettings`, no map light, no `ColourGrade`: its renderers and its
    /// light live under the volcano and die with it, on every exit (a round reset retires the volcano, `SeanVolcano.RetireFor`).
    /// That is also why play's Emberfall weather is folded into it rather than layered under it: `SkyEvent` writes the
    /// street's ambient, sun and fog, rising 0.45 s after the hand-back, which would both jump at the cut and darken the street
    /// a second time under this grade. `SkyEvent` keeps Emberfall's sound, its place in "the last ultimate owns the sky" and
    /// its rejoin record; its lighting is this now.
    ///
    /// ⚠️ ONE SKY AT A TIME. Two Ragos raising two mountains would multiply the street by two grades; only the newest volcano
    /// draws its sky (`SeanVolcano` decides). And if another hero's ultimate turns the weather after this one rose, that one
    /// owns the sky (`SkyEvent`'s rule: the last cast wins) and this one fades out and stays out.
    /// </summary>
    public sealed class SeanSkyLive
    {
        // ------------------------------------------------------------------ the shared curves (the cutscene's clock)

        /// <summary>The sky turns over this beat of the cutscene (inside A2, the lens looking up past him).</summary>
        public const float TurnFrom = .75f, TurnTo = 1.30f;

        /// <summary>
        /// The cutscene's grade eases toward play's from the home move to two seconds into play: it starts while the lens is
        /// still coming home, so the cut itself carries no step, and ends once the fight has resumed.
        /// </summary>
        public const float BlendFrom = 6.35f, BlendTo = 9.0f;

        /// <summary>
        /// Play's share of the cutscene's grade, ash and crater light. The grade at 0.62: the top of the frame (roofs, sky line)
        /// is multiplied by about 0.66, the court at the bottom keeps 0.92 of its red and lifts it, so the can, the chalk and
        /// every body stay readable while the street is plainly under a red sky.
        /// </summary>
        public const float PlayGrade = .62f, PlayAsh = .75f, PlayCraterLight = .70f;

        private static float Rise => HeroIntroductionScene.SeanRiseAt;

        public static float Ease(float from, float to, float time) { float u = Mathf.InverseLerp(from, to, time); return u * u * (3 - 2 * u); }

        private static float Blend(float t) => Ease(BlendFrom, BlendTo, t);

        /// <summary>How far the sky has turned. Held from the turn to the end of the cutscene and on into play.</summary>
        public static float Turn(float t) => Ease(TurnFrom, TurnTo, t);

        /// <summary>The street's grade, before reduced effects: the cutscene's full red, eased to play's lighter one.</summary>
        public static float Grade(float t) => Turn(t) * Mathf.Lerp(1f, PlayGrade, Blend(t));

        /// <summary>How much ash falls.</summary>
        public static float Ash(float t) => Ease(.95f, 1.7f, t) * Mathf.Lerp(1f, PlayAsh, Blend(t));

        /// <summary>The far Mayon's crater kindled, its plume climbed, and the fire's light on the cloud undersides.</summary>
        public static float Kindle(float t) => Ease(1.15f, 1.6f, t);
        public static float Erupt(float t) => Ease(Rise, Rise + 1.8f, t);
        public static float Glow(float t) => .35f * Ease(.8f, 1.5f, t) + .9f * Ease(Rise - .05f, Rise + .2f, t) * (1f - .4f * Ease(Rise + .4f, Rise + 1.6f, t));

        /// <summary>The crater light's steady part (the cutscene adds its flashes on top), eased to play's share.</summary>
        public static float CraterLight(float t) => (3.4f + .5f * Mathf.Sin(t * 9f)) * Mathf.Lerp(1f, PlayCraterLight, Blend(t));

        // ------------------------------------------------------------------ the hand-off from the cutscene

        private static Vector3 _handOffToward;
        private static float _handOffAt = -100f;

        /// <summary>
        /// The cutscene says, every frame it draws its sky, which way its Mayon stands (world, toward the spot). Play takes it
        /// when the volcano is spawned at the hand-back, so the painted mountain does not move by a degree across the cut.
        /// </summary>
        public static void HandOff(Vector3 towardWorld)
        {
            _handOffToward = towardWorld;
            _handOffAt = Time.realtimeSinceStartup;
        }

        private static bool TakeHandOff(out Vector3 toward)
        {
            toward = _handOffToward;
            bool fresh = Time.realtimeSinceStartup - _handOffAt < 3f && toward.sqrMagnitude > .01f;
            _handOffAt = -100f;
            return fresh;
        }

        // ------------------------------------------------------------------ one live sky

        private static readonly int TurnId = Shader.PropertyToID("_Turn"), ClockId = Shader.PropertyToID("_Clock"),
            KindleId = Shader.PropertyToID("_Kindle"), EruptId = Shader.PropertyToID("_Erupt"), GlowId = Shader.PropertyToID("_Glow"),
            FlashId = Shader.PropertyToID("_Flash"), StrengthId = Shader.PropertyToID("_Strength"), AmountId = Shader.PropertyToID("_Amount"),
            MayonDirId = Shader.PropertyToID("_MayonDir");

        private static Mesh _dome, _quad, _ash;

        private readonly GameObject _root;
        private readonly Renderer _skyR, _gradeR, _ashR;
        private readonly Material _skyMat, _gradeMat, _ashMat;
        private readonly Light _crater;
        private readonly bool _fromCutscene;
        private float _firstClock = float.NaN, _yieldFrom = float.NaN;

        /// <summary>
        /// The sky for one live volcano, under <paramref name="volcano"/> (at its foot). <paramref name="handBack"/>: it follows
        /// the cutscene, which handed over its Mayon's bearing; without a hand-off it turns in over 0.6 s instead of appearing.
        /// <paramref name="casterAt"/> gives the bearing when nothing was handed over (his feet toward the foot).
        /// </summary>
        public static SeanSkyLive Build(Transform volcano, Vector3 foot, Vector3? casterAt, bool handBack)
        {
            var sky = Resources.Load<Shader>("Shaders/SeanSky");
            var grade = Resources.Load<Shader>("Shaders/SeanSkyGrade");
            var ash = Resources.Load<Shader>("Shaders/SeanAshFall");
            if (sky == null || grade == null) return null;
            return new SeanSkyLive(volcano, foot, casterAt, handBack, sky, grade, ash);
        }

        private SeanSkyLive(Transform volcano, Vector3 foot, Vector3? casterAt, bool handBack, Shader sky, Shader grade, Shader ash)
        {
            bool handed = TakeHandOff(out var toward);
            _fromCutscene = handBack && handed;
            if (!handed)
            {
                toward = casterAt.HasValue ? foot - casterAt.Value : volcano.forward;
                toward.y = 0f;
                if (toward.sqrMagnitude < .01f) toward = Vector3.forward;
            }
            toward.y = 0f; toward.Normalize();

            _root = new GameObject("SeanSkyLive") { hideFlags = HideFlags.DontSave };
            _root.transform.SetParent(volcano, false);
            _skyMat = new Material(sky) { name = "SeanSkyLive", hideFlags = HideFlags.DontSave };
            _skyMat.SetVector(MayonDirId, new Vector4(toward.x, 0f, toward.z, 0f));
            _skyR = Piece("Sky", Dome(), _skyMat);
            _gradeMat = new Material(grade) { name = "SeanSkyLiveGrade", hideFlags = HideFlags.DontSave };
            _gradeR = Piece("Grade", Quad(), _gradeMat);
            if (ash != null)
            {
                _ashMat = new Material(ash) { name = "SeanSkyLiveAsh", hideFlags = HideFlags.DontSave };
                _ashR = Piece("Ash", AshMesh(), _ashMat);
            }
            // The crater's light, where the cutscene's stands (3.6 m over the foot), the same colour and reach.
            var lightGo = new GameObject("CraterLight") { hideFlags = HideFlags.DontSave };
            lightGo.transform.SetParent(_root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 3.6f, 0f);
            _crater = lightGo.AddComponent<Light>();
            _crater.type = LightType.Point;
            _crater.shadows = LightShadows.None;
            _crater.range = 15f;
            _crater.color = new Color(1f, .46f, .16f);
            _crater.intensity = 0f;
            _crater.enabled = false;
        }

        private Renderer Piece(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            r.enabled = false;
            VfxRenderTag.Attach(go);
            return r;
        }

        /// <summary>
        /// The sky at scene clock <paramref name="t"/> (the cutscene's clock, carried on). <paramref name="alive"/>: seconds since
        /// the volcano was spawned (for whose weather is newer). <paramref name="gone"/>: 0 while it stands, 1 once it has sunk.
        /// <paramref name="lead"/>: false hides it (a newer volcano draws the one sky).
        /// </summary>
        public void Pose(float t, float alive, float gone, bool lead)
        {
            if (float.IsNaN(_firstClock)) _firstClock = t;
            // Turned in over 0.6 s when no cutscene came first (a fresh cast, or a peer that did not draw it).
            float arrive = _fromCutscene ? 1f : Ease(_firstClock, _firstClock + .6f, t);
            // Another hero's weather that began after this volcano owns the sky now: this one fades out and stays out.
            if (float.IsNaN(_yieldFrom) && SkyEvent.CaptureTimeline(out var look, out float age, out _)
                && look != SkyEvent.Look.Emberfall && age < alive)
                _yieldFrom = t;
            float yieldK = float.IsNaN(_yieldFrom) ? 1f : 1f - Ease(_yieldFrom, _yieldFrom + .45f, t);
            float k = lead ? arrive * yieldK * (1f - Mathf.Clamp01(gone)) : 0f;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            float light = reduced ? .5f : 1f;

            float turn = Turn(t) * k;
            _skyR.enabled = turn > .002f;
            if (_skyR.enabled)
            {
                _skyMat.SetFloat(TurnId, turn);
                _skyMat.SetFloat(ClockId, t);
                _skyMat.SetFloat(KindleId, Kindle(t));
                _skyMat.SetFloat(EruptId, Erupt(t));
                _skyMat.SetFloat(GlowId, Glow(t) * light);
                _skyMat.SetFloat(FlashId, 0f);
            }
            float strength = Grade(t) * k * (reduced ? .6f : 1f);
            _gradeR.enabled = strength > .002f;
            if (_gradeR.enabled) _gradeMat.SetFloat(StrengthId, strength);
            if (_ashR != null)
            {
                float amount = Ash(t) * k;
                _ashR.enabled = amount > .002f;
                if (_ashR.enabled) { _ashMat.SetFloat(AmountId, amount); _ashMat.SetFloat(ClockId, t); }
            }
            float crater = Ease(Rise - .05f, Rise + .15f, t) * k;
            _crater.enabled = crater > .002f;
            _crater.intensity = crater * light * CraterLight(t);
        }

        /// <summary>A fixed scatter in 0 to 1 (the cutscene's own, kept so the flakes fall where they always fell).</summary>
        private static float Scatter(int i, int salt)
        {
            uint h = (uint)(i * 73856093) ^ (uint)(salt * 19349663);
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            return (h & 0xffff) / 65535f;
        }

        public void Dispose()
        {
            Kill(_skyMat); Kill(_gradeMat); Kill(_ashMat);
            Kill(_root);
        }

        private static void Kill(Object o) { if (o == null) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }

        // ------------------------------------------------------------------ meshes, built once and shared by every cast

        /// <summary>A unit sphere whose points are directions (the sky shader puts it round the lens on the far plane). Shared with
        /// the cutscene; never freed.</summary>
        public static Mesh Dome()
        {
            if (_dome != null) return _dome;
            const int around = 40, rows = 20;
            var p = new List<Vector3>(); var tris = new List<int>();
            for (int r = 0; r <= rows; r++)
                for (int c = 0; c <= around; c++)
                {
                    float lat = Mathf.Lerp(-90f, 90f, r / (float)rows) * Mathf.Deg2Rad, lon = c / (float)around * Mathf.PI * 2f;
                    p.Add(new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon)));
                    if (r == 0 || c == around) continue;
                    int i = r * (around + 1) + c, pr = i - (around + 1);
                    tris.Add(pr); tris.Add(i); tris.Add(pr + 1);
                    tris.Add(pr + 1); tris.Add(i); tris.Add(i + 1);
                }
            _dome = new Mesh { name = "SeanSkyLiveDome", hideFlags = HideFlags.HideAndDontSave };
            _dome.SetVertices(p); _dome.SetTriangles(tris, 0);
            // Never culled: the shader places it round whichever lens draws it.
            _dome.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            return _dome;
        }

        /// <summary>The screen's corners in clip space (`SeanSkyGrade` ignores every transform), never culled.</summary>
        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            _quad = new Mesh { name = "SeanSkyLiveGrade", hideFlags = HideFlags.HideAndDontSave };
            _quad.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) };
            _quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            return _quad;
        }

        /// <summary>
        /// 700 flakes, four corners each sharing a seed (`SeanAshFall.shader` places them). ⚠️ THE CUTSCENE DRAWS THIS SAME MESH, so
        /// every flake falling round the lens on its last frame is still there, in the same place, on play's first. Never freed.
        /// </summary>
        public static Mesh AshMesh()
        {
            if (_ash != null) return _ash;
            const int count = 700;
            var p = new List<Vector3>(count * 4); var corner = new List<Vector2>(count * 4); var seed = new List<Vector3>(count * 4);
            var kind = new List<Vector2>(count * 4); var tris = new List<int>(count * 6);
            for (int i = 0; i < count; i++)
            {
                var s = new Vector3(Scatter(i, 101), Scatter(i, 102), Scatter(i, 103));
                var k = new Vector2(Scatter(i, 104), Scatter(i, 105) < .15f ? 1f : 0f);
                int b = p.Count;
                for (int c = 0; c < 4; c++)
                {
                    p.Add(Vector3.zero);
                    corner.Add(new Vector2(c == 1 || c == 2 ? 1f : -1f, c >= 2 ? 1f : -1f));
                    seed.Add(s); kind.Add(k);
                }
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }
            _ash = new Mesh { name = "SeanSkyLiveAsh", hideFlags = HideFlags.HideAndDontSave };
            _ash.SetVertices(p); _ash.SetUVs(0, corner); _ash.SetUVs(1, seed); _ash.SetUVs(2, kind); _ash.SetTriangles(tris, 0);
            _ash.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            return _ash;
        }
    }
}
