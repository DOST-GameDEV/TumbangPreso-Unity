using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// WHAT CATCHES A FALLEN BODY ON PAYO: THE BUBBLE SPIRIT (owner, 2026-10-18, of the hawk before it: "does not fit the
    /// cutesy look of the game at all. i need something more creative and with effects"; he chose this). A chubby water
    /// spirit with a carved stone mask rises out of the misty pit under the falling body, gulps it into a wobbly glowing
    /// bubble, carries it up and back to the edge on its head, sets it down, and the bubble pops in a splash of drops and
    /// a little rainbow ring before the spirit waves and sinks back into the mist.
    ///
    /// ⚠️ PRESENTATION ONLY, THE SAME CONTRACT AS `ArenaDrone`: `Attend(who)` for as long as that body's replicated
    /// `EdgeKind` is `Drone`; it then pops, bows and goes by itself. The host's `ArenaFallRecovery` still decides the
    /// catch, `CharacterMotor.StepDroneCarry` still owns where the body goes and how long each part takes (0.4 s caught,
    /// 2.5 s lifted and carried, 0.6 s set down) and the tag's five-second freeze follows as before. Nothing is sent;
    /// every peer draws the same thing off the phase and ratio `SyncUnit` already brings. No collider anywhere.
    ///
    /// ⚠️ GPU-DRIVEN (owner's standing rule): three meshes built once (`PayoSpiritMeshes`), three shared materials, and
    /// every wobble, sway, drip, glint, droplet and ring is moved in the shaders. A frame of a carry is two transforms
    /// placed and two property-block writes: nothing allocated, no vertex touched. Five draws while it carries (the
    /// spirit and its ink line, the bubble's back and front, and the splash when it bursts). Its own cost is kept in
    /// `Ticks` and `Frames` for the probe to read.
    ///
    /// THE ACTS, read off the replicated phase (seconds into the carry, `s`):
    ///   RISE    (0 to 0.4) out of a puff of mist far under the body, stretched tall, ears swept down, mouth opening;
    ///   GULP    (0.2 to 0.62) the bubble closes round the body from the bottom up, a white wavy lip running up it, then
    ///           wobbles with a squash and stretch; the spirit squashes as it takes the weight; a small splash;
    ///   CARRY   (to 2.9) the bubble rides on its head, wobbling, glinting and dripping; the spirit bobs, arms up, ears
    ///           trailing, tail swishing, leaning into its travel;
    ///   SET-DOWN(2.9 to 3.5) it hops out from under to beside the body and the bubble settles on the deck;
    ///   POP     the bubble bursts: drops thrown from its skin, a rainbow ring, a water ring on the deck, a pop;
    ///   BOW, SINK the spirit hops with its ears up, then drops back toward the pit, dissolving into mist.
    /// </summary>
    [DefaultExecutionOrder(710)]
    public sealed class PayoBubbleSpirit : MonoBehaviour
    {
        /// <summary>The bubble's radius, how high its middle rides over the body's feet, and how far down the spirit starts.</summary>
        public const float BubbleRadius = 1.15f, BubbleLift = 0.85f, RiseFrom = 7.0f;
        /// <summary>How long the bow and the sink after the pop take.</summary>
        public const float BowSeconds = 0.6f, SinkSeconds = 1.3f;
        /// <summary>When the bubble has closed round the body (seconds into the carry).</summary>
        private const float Formed = 0.62f, FormBegins = 0.18f;
        public const string SpiritShader = "TumbangPreso/PayoSpirit", BubbleShader = "TumbangPreso/PayoBubble", SplashShader = "TumbangPreso/PayoSplash";

        /// <summary>Stopwatch ticks spent in every spirit's frame since the counters were last cleared, and how many frames.</summary>
        public static long Ticks;
        public static int Frames;
        /// <summary>A probe's switch: true draws no spirit (everything else runs), so its render cost can be told apart. Never set in play.</summary>
        public static bool HideForMeasure;
        private static int _countedFrame = -1;

        private static Material _spiritMat, _bubbleMat, _splashMat;
        private static bool _warned;
        private static readonly int PoseId = Shader.PropertyToID("_Pose"), GlowId = Shader.PropertyToID("_Glow"), BubbleId = Shader.PropertyToID("_Bubble"),
            AgeId = Shader.PropertyToID("_Age"), PartsId = Shader.PropertyToID("_Parts"), ShapeId = Shader.PropertyToID("_Shape");

        private Transform _spirit, _bubble, _splash;
        private MeshRenderer _spiritR, _bubbleR, _splashR;
        private MaterialPropertyBlock _spiritBlock, _bubbleBlock, _splashBlock;
        private CharacterMotor _body;
        private bool _carried, _gulped, _popped, _leaving, _sinking;
        private float _clock, _leave, _yaw, _burstAge = 100f;
        private Vector3 _at, _last, _travel, _beside, _outward, _centre, _feet;

        /// <summary>True while it is on screen (carrying, popping, bowing or sinking).</summary>
        public bool Busy => gameObject.activeSelf;

        /// <summary>Makes one spirit (hidden) under `parent`. Null if the shaders are not in the build.</summary>
        public static PayoBubbleSpirit Build(Transform parent)
        {
            if (!Materials()) return null;
            var root = new GameObject("Payo bubble spirit");
            root.transform.SetParent(parent, false);
            var self = root.AddComponent<PayoBubbleSpirit>();
            self._spirit = Part(root.transform, "spirit", PayoSpiritMeshes.Spirit, _spiritMat, out self._spiritR);
            self._bubble = Part(root.transform, "bubble", PayoSpiritMeshes.Bubble, _bubbleMat, out self._bubbleR);
            // The splash stays where a burst happened while the spirit moves on: it hangs off the root, placed in the world.
            self._splash = Part(root.transform, "splash", PayoSpiritMeshes.Splash, _splashMat, out self._splashR);
            self._spiritBlock = new MaterialPropertyBlock(); self._bubbleBlock = new MaterialPropertyBlock(); self._splashBlock = new MaterialPropertyBlock();
            self._splashBlock.SetFloat(AgeId, 100f);
            self._splashR.SetPropertyBlock(self._splashBlock);
            root.SetActive(false);
            return self;
        }

        private static bool Materials()
        {
            if (_spiritMat != null && _bubbleMat != null && _splashMat != null) return true;
            Shader a = Shader.Find(SpiritShader), b = Shader.Find(BubbleShader), c = Shader.Find(SplashShader);
            if (a == null || b == null || c == null)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[Payo] the bubble spirit's shaders are missing: no catcher is drawn"); }
                return false;
            }
            _spiritMat = new Material(a) { name = "Payo spirit (shared)", hideFlags = HideFlags.DontSave, enableInstancing = false };
            _bubbleMat = new Material(b) { name = "Payo spirit bubble (shared)", hideFlags = HideFlags.DontSave };
            _splashMat = new Material(c) { name = "Payo spirit splash (shared)", hideFlags = HideFlags.DontSave };
            return true;
        }

        private static Transform Part(Transform parent, string name, Mesh mesh, Material material, out MeshRenderer renderer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            return go.transform;
        }

        /// <summary>Carry `body` while its replicated kind is `Drone`. Called every frame by `PayoBubbleSpiritCatch`; a
        /// repeat for the body it already carries does nothing.</summary>
        public void Attend(CharacterMotor body)
        {
            if (body == null) return;
            if (_body == body && !_leaving && gameObject.activeSelf) return;
            _body = body; _carried = false; _gulped = false; _popped = false; _leaving = false; _sinking = false; _leave = 0f; _clock = 0f;
            gameObject.SetActive(true);
            _bubbleR.enabled = true;
            _feet = body.transform.position;
            _centre = _feet + Vector3.up * BubbleLift;
            _at = _centre - Vector3.up * (BubbleRadius * 0.93f + RiseFrom);
            _last = _at; _travel = Vector3.zero;
            _outward = body.EdgeOutward.sqrMagnitude > 0.5f ? body.EdgeOutward : Vector3.back;
            _yaw = FaceYaw(_at, body.transform.eulerAngles.y + 180f);
            // Out of a puff of mist far down in the pit.
            Mist(_at + Vector3.down * 1.2f, 3.2f, 0.95f, 3);
        }

        private bool Carrying => _body != null && _body.gameObject.activeInHierarchy && _body.EdgeKind == EdgeRecoveryKind.Drone;

        private void LateUpdate()
        {
            long began = System.Diagnostics.Stopwatch.GetTimestamp();
            float dt = Time.deltaTime;
            _clock += dt;
            float glow = PayoNight.Factor;
            if (_spiritR.forceRenderingOff != HideForMeasure) { _spiritR.forceRenderingOff = HideForMeasure; _bubbleR.forceRenderingOff = HideForMeasure; _splashR.forceRenderingOff = HideForMeasure; }
            if (Carrying && !_leaving) Carry(dt, glow);
            else Leave(dt, glow);
            // A live burst: its one number a frame (the shader does all the moving).
            if (_burstAge < 1.3f && _splashR != null)
            {
                _burstAge += dt;
                _splashBlock.SetFloat(AgeId, _burstAge);
                _splashR.SetPropertyBlock(_splashBlock);
            }
            Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - began;
            if (_countedFrame != Time.frameCount) { _countedFrame = Time.frameCount; Frames++; }
        }

        // The camera's side of the spirit: it shows its face to whoever is watching.
        private static float FaceYaw(Vector3 at, float fallback)
        {
            var cam = Camera.main;
            if (cam == null) return fallback;
            var to = cam.transform.position - at; to.y = 0f;
            return to.sqrMagnitude > 0.01f ? Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg : fallback;
        }

        private float Seconds()
        {
            int phase = _body.EdgePhase; float r = _body.EdgePhaseRatio;
            if (phase == 0) return r * CharacterMotor.DroneCatchSeconds;
            if (phase == 1) return CharacterMotor.DroneCatchSeconds + r * CharacterMotor.DroneCarrySeconds;
            return CharacterMotor.DroneCatchSeconds + CharacterMotor.DroneCarrySeconds + r * CharacterMotor.DroneSetDownSeconds;
        }

        // ------------------------------------------------------------------ the carry

        private void Carry(float dt, float glow)
        {
            _feet = _body.transform.position;
            _centre = _feet + Vector3.up * BubbleLift;
            int phase = _body.EdgePhase; float r = _body.EdgePhaseRatio;
            float s = Seconds();
            if (phase > 0) _carried = true;
            var under = _centre - Vector3.up * (BubbleRadius * 0.93f);

            // ---- where the spirit is
            Vector3 at;
            if (phase == 0)
            {
                float e = 1f - (1f - r) * (1f - r) * (1f - r);
                at = under - Vector3.up * (RiseFrom * (1f - e));
            }
            else if (phase == 1) at = under + Vector3.up * (Mathf.Sin(_clock * 5.2f) * 0.07f);
            else
            {
                // It hops out from under to stand beside the body, facing it, as the bubble settles.
                _beside = _feet + _outward * 1.9f + Vector3.up * 1.35f;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(r / 0.75f));
                at = Vector3.Lerp(under, _beside, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.7f);
            }
            if (dt > 1e-5f) _travel = Vector3.Lerp(_travel, (at - _last) / dt, Mathf.Clamp01(dt * 8f));
            _last = at; _at = at;

            // ---- its shape: stretched as it shoots up, a squash as it takes the weight, a little push while it lifts
            float stretch = phase == 0 ? 0.26f : (phase == 1 && r < CharacterMotor.DroneLiftShare ? 0.10f : 0f);
            float k0 = s - 0.40f;
            float impact = k0 > 0f ? -0.30f * Mathf.Exp(-k0 * 6f) * Mathf.Cos(k0 * 17f) : 0f;
            float tall = 1f + stretch + impact;
            float wide = 1f / Mathf.Sqrt(Mathf.Max(0.4f, tall));

            // ---- its pose: arms up once it has the bubble, ears trailing on the way up, mouth wide as it gulps
            float arms = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((s - 0.3f) / 0.4f)) * (phase == 2 ? Mathf.Lerp(1f, 0.25f, r) : 1f);
            float ears = phase == 0 ? -0.9f : phase == 1 ? (r < CharacterMotor.DroneLiftShare ? -0.45f : Mathf.Sin(_clock * 6f) * 0.35f) : 0.3f;
            float mouth = s > 0.12f && s < 0.75f ? 1f : 0.18f;
            float mist = phase == 0 ? 0.7f * (1f - r) * (1f - r) : 0f;
            Pose(arms, ears, mouth, mist, glow);

            // ---- the bubble: closes, then a jelly wobble with a squash that rings down, and a bump on the deck
            float form = Mathf.Clamp01((s - FormBegins) / (Formed - FormBegins));
            float kb = s - Formed;
            float ring = kb > 0f ? 0.20f * Mathf.Exp(-kb * 4.5f) * Mathf.Sin(kb * 15f) : 0f;
            if (phase == 2 && r > 0.72f) ring -= 0.12f * Mathf.Sin((r - 0.72f) / 0.28f * Mathf.PI);
            float grow = Mathf.Lerp(0.82f, 1f, Mathf.SmoothStep(0f, 1f, form));
            var bubbleScale = new Vector3(1f - ring * 0.5f, 1f + ring, 1f - ring * 0.5f) * (BubbleRadius * grow);
            // A faster carry wobbles more.
            float wobble = 0.45f + 0.75f * Mathf.Exp(-Mathf.Max(0f, kb) * 1.3f) + Mathf.Min(0.6f, _travel.magnitude * 0.03f);
            _bubble.SetPositionAndRotation(_centre, Quaternion.identity);
            _bubble.localScale = bubbleScale;
            _bubbleBlock.SetVector(BubbleId, new Vector4(form, wobble, phase == 0 ? 0f : 1f, glow));
            _bubbleR.SetPropertyBlock(_bubbleBlock);

            if (!_gulped && s >= Formed)
            {
                _gulped = true;
                Burst(_centre + Vector3.down * (BubbleRadius * 0.7f), 0.55f, false, glow);
                ArenaFx.Cue("sfx_arena_balloon_pop", _centre, 0.55f, 0.62f, 0.45f);
            }

            // ---- it faces the watcher, leaning into its travel
            // Always its face to the watcher (the set-down too: it is showing off what it caught).
            float faceTo = FaceYaw(at, _body.transform.eulerAngles.y + 180f);
            _yaw = Mathf.LerpAngle(_yaw, faceTo, Mathf.Clamp01(dt * 7f));
            PlaceSpirit(at, tall, wide);
        }

        private void PlaceSpirit(Vector3 at, float tall, float wide)
        {
            var flat = _travel; flat.y = 0f;
            float bank = Mathf.Min(16f, flat.magnitude * 2.4f);
            var lean = bank > 0.5f ? Quaternion.AngleAxis(bank, Vector3.Cross(Vector3.up, flat.normalized)) : Quaternion.identity;
            _spirit.SetPositionAndRotation(at, lean * Quaternion.Euler(0f, _yaw, 0f));
            _spirit.localScale = new Vector3(wide, tall, wide);
        }

        private void Pose(float arms, float ears, float mouth, float mist, float glow)
        {
            _spiritBlock.SetVector(PoseId, new Vector4(arms, ears, mouth, mist));
            _spiritBlock.SetFloat(GlowId, glow);
            _spiritR.SetPropertyBlock(_spiritBlock);
        }

        // ------------------------------------------------------------------ the pop, the bow and the sink

        private void Leave(float dt, float glow)
        {
            if (!_leaving)
            {
                _leaving = true; _leave = 0f;
                // Set down (or the round reset under it): the bubble pops where it is.
                bool landed = _carried && _body != null && _body.gameObject.activeInHierarchy && !_body.IsEdgeRecovering;
                if (landed) { _feet = _body.transform.position; _centre = _feet + Vector3.up * BubbleLift; }
                if (!_popped && _carried)
                {
                    _popped = true;
                    Burst(_centre, 1f, landed, glow);
                    ArenaFx.Cue("sfx_arena_balloon_pop", _centre, 1.30f, 1.45f, 0.8f);
                    if (landed) Mist(_feet, 1.6f, 0.5f, 1);
                }
                _bubbleR.enabled = false;
                if (!_carried) _beside = _at;
                else if ((_beside - _at).sqrMagnitude > 9f) _beside = _at;
            }
            _leave += dt;
            if (_leave < BowSeconds && _carried)
            {
                // A happy hop with its ears up and its arms waving, facing the body it saved.
                float u = _leave / BowSeconds;
                var at = _at + Vector3.up * (0.35f * Mathf.Sin(u * Mathf.PI));
                float squash = u > 0.8f ? (u - 0.8f) / 0.2f : 0f;
                Pose(0.55f + 0.35f * Mathf.Sin(_clock * 18f), 1f, 0.8f, 0f, glow);
                _yaw = Mathf.LerpAngle(_yaw, FaceYaw(at, _yaw), Mathf.Clamp01(dt * 7f));
                PlaceSpirit(at, 1f - 0.18f * squash + 0.1f * Mathf.Sin(u * Mathf.PI), 1f + 0.12f * squash);
                return;
            }
            float v = Mathf.Clamp01((_leave - (_carried ? BowSeconds : 0f)) / SinkSeconds);
            if (!_sinking) { _sinking = true; Mist(_at + Vector3.down * 1.5f, 1.8f, 0.6f, 1); }
            // Out over the pit and down into the mist, drawn thin, the mist eating it from the tail up.
            var sink = _at + _outward * (1.8f * Mathf.SmoothStep(0f, 1f, v)) + Vector3.down * (9f * v * v) + Vector3.up * (0.4f * Mathf.Sin(Mathf.Min(1f, v * 3f) * Mathf.PI));
            Pose(0.15f, -0.7f, 0.25f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - 0.25f) / 0.75f)), glow);
            PlaceSpirit(sink, 1f + 0.3f * v, 1f - 0.15f * v);
            if (v >= 1f && _burstAge > 1.2f) { _body = null; gameObject.SetActive(false); }
        }

        // A splash at `at`: droplets (and, for the pop, the rainbow and the floor ring). One property-block write.
        private void Burst(Vector3 at, float scale, bool full, float glow)
        {
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            _splash.SetPositionAndRotation(at, Quaternion.identity);
            _splash.localScale = Vector3.one;
            _burstAge = 0f;
            _splashBlock.SetFloat(AgeId, 0f);
            _splashBlock.SetVector(PartsId, new Vector4(1f, full || scale >= 1f ? 1f : 0f, full ? 1f : 0f, reduced ? 0.5f : 1f));
            _splashBlock.SetVector(ShapeId, new Vector4(BubbleRadius * (scale >= 1f ? 0.95f : 0.4f), _feet.y - at.y, scale, 0f));
            _splashBlock.SetFloat(GlowId, glow);
            _splashR.SetPropertyBlock(_splashBlock);
        }

        // Mist, as the game's own toon cloud puffs (`CloudPuffs`): pale blue-white, a darker line of its own colour.
        private static void Mist(Vector3 at, float scale, float life, int count)
        {
            if (Settings.SettingsStore.Current.ReducedEffects) count = 1;
            for (int i = 0; i < count; i++)
            {
                var puff = new Visual.CloudPuffField.Puff
                {
                    At = at + new Vector3(Mathf.Cos(i * 2.1f), 0f, Mathf.Sin(i * 2.1f)) * (i == 0 ? 0f : 0.9f * scale * 0.3f),
                    Scale = scale * (i == 0 ? 1f : 0.75f), Life = life, Reach = 0.55f, Lift = 0.35f, Turn = i * 1.3f, Share = 1f,
                    Lit = new Color(0.93f, 0.97f, 0.98f), Shade = new Color(0.74f, 0.85f, 0.89f), Line = new Color(0.50f, 0.64f, 0.70f),
                };
                Visual.CloudPuffs.Spawn(puff);
            }
        }

        private void OnDestroy() { _body = null; }
    }
}
