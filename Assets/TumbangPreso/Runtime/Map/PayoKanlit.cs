using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// WHAT CATCHES A FALLEN BODY ON PAYO: KANLIT (owner, 2026-10-18, after the hawk, the bubble spirit, two statues and
    /// three vine drones: "i want the shape of the second but the concept of the first ... i like the 2nd overall but not
    /// the white"; then "1st design", variant A, honey wood). A little carved bulul wood spirit, Kanin's round gumdrop
    /// shape, flying under a gabi leaf like the Arena's drone SAGIP flies under its rotor. Instead of a beam, its flowering
    /// vine grows down from its paws and hugs the body three times round, knees to chest, tied in a leaf bow.
    /// Design and model: `tools/payo_pieces/catcher_cute.py` (sheet `Logs/payo/pieces/catchers/cute/render_v2.png`).
    ///
    /// ⚠️ PRESENTATION ONLY, THE SAME CONTRACT AS `ArenaDrone`: `Attend(who)` for as long as that body's replicated
    /// `EdgeKind` is `Drone`; it then lets go and leaves by itself. The host's `ArenaFallRecovery` still decides the
    /// catch, `CharacterMotor.StepDroneCarry` still owns where the body goes and how long each part takes (0.4 s caught,
    /// 2.5 s lifted and carried, 0.6 s set down) and the tag's freeze follows as before. Nothing is sent; every peer draws
    /// the same thing off the phase and ratio `SyncUnit` already brings. No collider anywhere.
    ///
    /// ⚠️ GPU-DRIVEN (owner's standing rule): two baked meshes (`PayoKanlitMeshes`), two shared materials; the rotor, the
    /// flapping leaf, the faces, the swaying cowlick and hoops, the growing vine, the popping leaves and the flowers flying
    /// off are all moved in the shaders. A frame is two transforms placed and two property-block writes: nothing
    /// allocated, no vertex touched. Two draws, no ink line (owner: "remove it"). Its own cost is kept in `Ticks`.
    ///
    /// THE ACTS, read off the replicated phase (seconds into the carry, `s`):
    ///   DIVE    (0 to 0.32) it swoops down from over the stage to just above the body, worried "o" face, leaf spinning;
    ///   WRAP    (0.1 to 0.55) the vine grows out of its paws, down behind the body and round it; leaves and flowers pop;
    ///   HAUL    (to 3.5) the effort face, the leaf flapping hard, a jiggle as it strains, the body dangling in the hug;
    ///   LET GO  the flowers fly off spinning, the vine runs back up into its paws, a puff and a pop;
    ///   HOP     a happy hop with the ^ ^ face; then it flies off up into the sky.
    /// </summary>
    [DefaultExecutionOrder(710)]
    public sealed class PayoKanlit : MonoBehaviour
    {
        public const string BuddyShader = "TumbangPreso/PayoKanlit", VineShader = "TumbangPreso/PayoKanlitVine";
        /// <summary>The buddy's size (its mesh is in metres, about 0.8 m wide), and where it dives from (over the stage).</summary>
        public const float BuddyScale = 1.25f, DiveUp = 6f, DiveBack = 4f;
        /// <summary>When the vine starts and ends growing (seconds into the carry).</summary>
        private const float GrowFrom = 0.10f, GrowTo = 0.55f;
        /// <summary>After it lets go: the vine runs back, the hop, then the flight away.</summary>
        public const float RetractSeconds = 0.4f, HopSeconds = 0.55f, AwaySeconds = 1.2f;

        public static long Ticks;
        public static int Frames;
        /// <summary>A probe's switch: true draws nothing (everything else runs), so its render cost can be told apart. Never set in play.</summary>
        public static bool HideForMeasure;
        private static int _countedFrame = -1;

        private static Material _buddyMat, _vineMat;
        private static bool _warned;
        private static readonly int PoseId = Shader.PropertyToID("_Pose"), GlowId = Shader.PropertyToID("_Glow"), GrowId = Shader.PropertyToID("_Grow"),
            ReleaseId = Shader.PropertyToID("_Release"), LeadId = Shader.PropertyToID("_Lead");

        private Transform _buddy, _vine;
        private MeshRenderer _buddyR, _vineR;
        private MaterialPropertyBlock _buddyBlock, _vineBlock;
        private CharacterMotor _body;
        // The body's renderers, read once a catch: Kanlit holds the vine clear over the top of whatever it is (a tall
        // form, a hat), the lead taking up the difference.
        private Renderer[] _looks = System.Array.Empty<Renderer>();
        /// <summary>How far over the top of the body Kanlit's paws stay, and the most the lead may reach for it.</summary>
        public const float Clear = 0.35f, MostReach = 1.6f;
        private bool _carried, _leaving, _wrapCued, _hopCued;
        private float _clock, _leave, _yaw, _spin, _hopFace;
        private Vector3 _at, _last, _travel, _outward, _from, _letGoAt;

        public bool Busy => gameObject.activeSelf;

        public static PayoKanlit Build(Transform parent)
        {
            if (!Materials()) return null;
            if (PayoKanlitMeshes.Buddy == null || PayoKanlitMeshes.Vine == null)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[Payo] Kanlit's baked meshes are missing (Resources/Map/PayoCatcher): no catcher is drawn"); }
                return null;
            }
            var root = new GameObject("Payo Kanlit");
            root.transform.SetParent(parent, false);
            var self = root.AddComponent<PayoKanlit>();
            self._buddy = Part(root.transform, "kanlit", PayoKanlitMeshes.Buddy, _buddyMat, out self._buddyR);
            self._vine = Part(root.transform, "vine", PayoKanlitMeshes.Vine, _vineMat, out self._vineR);
            self._buddyBlock = new MaterialPropertyBlock(); self._vineBlock = new MaterialPropertyBlock();
            root.SetActive(false);
            return self;
        }

        private static bool Materials()
        {
            if (_buddyMat != null && _vineMat != null) return true;
            Shader a = Shader.Find(BuddyShader), b = Shader.Find(VineShader);
            if (a == null || b == null)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[Payo] Kanlit's shaders are missing: no catcher is drawn"); }
                return false;
            }
            var atlas = PayoKanlitMeshes.Atlas;
            _buddyMat = new Material(a) { name = "Payo Kanlit (shared)", hideFlags = HideFlags.DontSave, mainTexture = atlas };
            _vineMat = new Material(b) { name = "Payo Kanlit vine (shared)", hideFlags = HideFlags.DontSave, mainTexture = atlas };
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

        /// <summary>Carry `body` while its replicated kind is `Drone`. Called every frame by `PayoKanlitCatch`; a repeat
        /// for the body it already carries does nothing.</summary>
        public void Attend(CharacterMotor body)
        {
            if (body == null) return;
            if (_body == body && !_leaving && gameObject.activeSelf) return;
            _body = body; _carried = false; _leaving = false; _wrapCued = false; _hopCued = false; _leave = 0f; _clock = 0f;
            gameObject.SetActive(true);
            _vineR.enabled = true;
            _outward = body.EdgeOutward.sqrMagnitude > 0.5f ? body.EdgeOutward : Vector3.back;
            _looks = body.GetComponentsInChildren<Renderer>(false);
            PlaceVine(body);
            var spool = Spool();
            _from = spool + Vector3.up * DiveUp - _outward * DiveBack;
            _at = _last = _from; _travel = Vector3.zero;
            _yaw = FaceYaw(_at, body.transform.eulerAngles.y + 180f);
            ArenaFx.Cue("sfx_arena_drone_zip", spool, 1.25f, 1.4f, 0.55f);
        }

        private bool Carrying => _body != null && _body.gameObject.activeInHierarchy && _body.EdgeKind == EdgeRecoveryKind.Drone;

        // The vine stands on the body's feet, facing where the body faces, sized to its capsule.
        private void PlaceVine(CharacterMotor body)
        {
            var t = body.transform;
            float tall = 1.75f, wide = 0.3f;
            var cc = body.GetComponent<CharacterController>();
            if (cc != null)
            {
                tall = cc.height * t.lossyScale.y;
                wide = cc.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);
            }
            float sy = Mathf.Clamp(tall / 1.75f, 0.85f, 1.4f), sx = Mathf.Clamp(wide / 0.3f, sy * 0.9f, 1.6f);
            _vine.SetPositionAndRotation(t.position, Quaternion.Euler(0f, t.eulerAngles.y, 0f));
            _vine.localScale = new Vector3(sx, sy, sx);
        }

        // Where its paws hold the lead: the vine's own lead top, lifted clear of the body's top if the body is taller.
        private Vector3 Spool()
        {
            var spool = _vine.TransformPoint(PayoKanlitMeshes.Lead);
            float top = float.NegativeInfinity;
            for (int i = 0; i < _looks.Length; i++)
            {
                var r = _looks[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                top = Mathf.Max(top, r.bounds.max.y);
            }
            if (!float.IsInfinity(top)) spool.y += Mathf.Clamp(top + Clear - spool.y, 0f, MostReach);
            return spool;
        }

        private void LateUpdate()
        {
            long began = System.Diagnostics.Stopwatch.GetTimestamp();
            float dt = Time.deltaTime;
            _clock += dt;
            float glow = PayoNight.Factor;
            if (_buddyR.forceRenderingOff != HideForMeasure) { _buddyR.forceRenderingOff = HideForMeasure; _vineR.forceRenderingOff = HideForMeasure; }
            if (Carrying && !_leaving) Carry(dt, glow);
            else Leave(dt, glow);
            Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - began;
            if (_countedFrame != Time.frameCount) { _countedFrame = Time.frameCount; Frames++; }
        }

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
            int phase = _body.EdgePhase; float r = _body.EdgePhaseRatio;
            float s = Seconds();
            if (phase > 0) _carried = true;
            PlaceVine(_body);
            var spool = Spool();

            // ---- where it is: the dive, then holding the lead, bobbing with the effort
            Vector3 bob = Vector3.zero;
            Vector3 at;
            if (s < 0.32f)
            {
                float k = s / 0.32f;
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                at = Vector3.Lerp(_from, spool, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.8f);
            }
            else
            {
                float hard = phase == 1 && r < CharacterMotor.DroneLiftShare ? 1f : 0.55f;
                bob = new Vector3(Mathf.Sin(_clock * 3.1f) * 0.06f, Mathf.Sin(_clock * 9.5f) * 0.07f * hard, 0f);
                float settle = Mathf.Clamp01((s - 0.32f) / 0.12f);
                at = spool + bob + Vector3.up * (0.25f * (1f - settle) * Mathf.Cos((s - 0.32f) * 25f));
            }
            if (dt > 1e-5f) _travel = Vector3.Lerp(_travel, (at - _last) / dt, Mathf.Clamp01(dt * 8f));
            _last = at; _at = at;

            // ---- the vine grows from its paws round the body, and follows its bob at the paws
            float grow = Mathf.Clamp01((s - GrowFrom) / (GrowTo - GrowFrom));
            grow = grow * grow * (3f - 2f * grow);
            if (!_wrapCued && s >= 0.3f) { _wrapCued = true; ArenaFx.Cue("sfx_arena_balloon_creak", _vine.position + Vector3.up, 1.2f, 1.35f, 0.5f); }
            _vineBlock.SetFloat(GrowId, grow);
            _vineBlock.SetFloat(ReleaseId, -1f);
            _vineBlock.SetVector(LeadId, _vine.InverseTransformVector(at - _vine.TransformPoint(PayoKanlitMeshes.Lead)));
            _vineBlock.SetFloat(GlowId, glow);
            _vineR.SetPropertyBlock(_vineBlock);

            // ---- its face, its leaf
            int face = s < 0.45f ? 0 : 1;
            float spinRate = s < 0.32f ? 18f : 30f;
            _spin += spinRate * dt;
            Pose(face, s < 0.32f ? 0.3f : 1f, glow);
            float jiggle = s >= 0.32f ? 0.05f * Mathf.Sin(_clock * 19f) : 0f;
            float stretch = s < 0.32f ? 0.12f * Mathf.Sin(s / 0.32f * Mathf.PI) : 0f;
            _yaw = Mathf.LerpAngle(_yaw, FaceYaw(at, _body.transform.eulerAngles.y + 180f), Mathf.Clamp01(dt * 7f));
            PlaceBuddy(at, 1f + jiggle + stretch, 1f - jiggle * 0.6f - stretch * 0.5f);
        }

        private void PlaceBuddy(Vector3 at, float tall, float wide)
        {
            var flat = _travel; flat.y = 0f;
            float bank = Mathf.Min(18f, flat.magnitude * 2.2f);
            var lean = bank > 0.5f ? Quaternion.AngleAxis(bank, Vector3.Cross(Vector3.up, flat.normalized)) : Quaternion.identity;
            _buddy.SetPositionAndRotation(at, lean * Quaternion.Euler(0f, _yaw, 0f));
            _buddy.localScale = new Vector3(wide, tall, wide) * BuddyScale;
        }

        private void Pose(int face, float flap, float glow)
        {
            _buddyBlock.SetVector(PoseId, new Vector4(_spin, face, flap, 0f));
            _buddyBlock.SetFloat(GlowId, glow);
            _buddyR.SetPropertyBlock(_buddyBlock);
        }

        // ------------------------------------------------------------------ let go, the hop, away

        private void Leave(float dt, float glow)
        {
            if (!_leaving)
            {
                _leaving = true; _leave = 0f;
                _letGoAt = _at;
                if (_carried)
                {
                    ArenaFx.Cue("sfx_arena_balloon_pop", _vine.position + Vector3.up, 1.5f, 1.7f, 0.5f);
                    Puff(_vine.position + Vector3.up * 0.9f, 1.4f, 0.55f, 3);
                }
                else _vineR.enabled = false;                            // the round reset under it: just go
            }
            _leave += dt;
            float u = _leave;
            // The vine: flowers fly, the vine runs back up into the paws.
            if (_vineR.enabled)
            {
                float back = Mathf.Clamp01((u - 0.08f) / RetractSeconds);
                _vineBlock.SetFloat(GrowId, 1f - back * back);
                _vineBlock.SetFloat(ReleaseId, u);
                _vineBlock.SetVector(LeadId, _vine.InverseTransformVector(_at - _vine.TransformPoint(PayoKanlitMeshes.Lead)));
                _vineBlock.SetFloat(GlowId, glow);
                _vineR.SetPropertyBlock(_vineBlock);
                if (back >= 1f) _vineR.enabled = false;
            }
            _spin += (u < RetractSeconds ? 20f : 26f) * dt;
            if (!_carried)
            {
                float v0 = Mathf.Clamp01(u / AwaySeconds);
                Pose(0, 0.5f, glow);
                Away(_letGoAt, v0, dt);
                if (v0 >= 1f) Done();
                return;
            }
            float hop0 = RetractSeconds * 0.6f;
            if (u < hop0 + HopSeconds)
            {
                // A happy hop, a little spin of joy, the ^ ^ face.
                float h = Mathf.Clamp01((u - hop0) / HopSeconds);
                if (!_hopCued && h > 0f) { _hopCued = true; ArenaFx.Cue("sfx_arena_balloon_boing", _letGoAt, 1.3f, 1.45f, 0.45f); }
                var at = _letGoAt + Vector3.up * (0.6f * Mathf.Sin(h * Mathf.PI));
                float squash = h > 0.85f ? Mathf.Sin((h - 0.85f) / 0.15f * Mathf.PI) : 0f;
                Pose(h > 0f ? 2 : 1, 0.5f, glow);
                if (h <= 0f) _hopFace = FaceYaw(at, _yaw);
                _yaw = _hopFace + 360f * Mathf.SmoothStep(0f, 1f, h);
                _travel = Vector3.zero;
                _at = at;
                PlaceBuddy(at, 1f + 0.12f * Mathf.Sin(h * Mathf.PI) - 0.15f * squash, 1f + 0.1f * squash);
                return;
            }
            float v = Mathf.Clamp01((u - hop0 - HopSeconds) / AwaySeconds);
            Pose(2, 0.6f, glow);
            Away(_letGoAt, v, dt);
            if (v >= 1f) Done();
        }

        // Off up into the sky, over the stage, waving its leaf, shrinking into the distance.
        private void Away(Vector3 from, float v, float dt)
        {
            var at = from + Vector3.up * (10f * v * v + 1.2f * v) - _outward * (5f * v) + Vector3.Cross(Vector3.up, _outward) * (1.5f * Mathf.Sin(v * Mathf.PI));
            if (dt > 1e-5f) _travel = Vector3.Lerp(_travel, (at - _at) / dt, Mathf.Clamp01(dt * 6f));
            _at = at;
            _yaw = Mathf.LerpAngle(_yaw, FaceYaw(at, _yaw), Mathf.Clamp01(dt * 5f));
            float shrink = 1f - 0.5f * Mathf.SmoothStep(0.6f, 1f, v);
            PlaceBuddy(at, shrink, shrink);
        }

        private void Done()
        {
            _body = null;
            gameObject.SetActive(false);
        }

        // A puff, as the game's toon cloud puffs (`CloudPuffs`): warm white, a soft leaf-green line.
        private static void Puff(Vector3 at, float scale, float life, int count)
        {
            if (Settings.SettingsStore.Current.ReducedEffects) count = 1;
            for (int i = 0; i < count; i++)
            {
                var puff = new Visual.CloudPuffField.Puff
                {
                    At = at + new Vector3(Mathf.Cos(i * 2.1f), 0.3f * i, Mathf.Sin(i * 2.1f)) * (i == 0 ? 0f : 0.35f * scale),
                    Scale = scale * (i == 0 ? 1f : 0.7f), Life = life, Reach = 0.6f, Lift = 0.45f, Turn = i * 1.3f, Share = 1f,
                    Lit = new Color(1.0f, 0.97f, 0.9f), Shade = new Color(0.93f, 0.86f, 0.8f), Line = new Color(0.45f, 0.62f, 0.38f),
                };
                Visual.CloudPuffs.Spawn(puff);
            }
        }

        private void OnDestroy() { _body = null; }
    }
}
