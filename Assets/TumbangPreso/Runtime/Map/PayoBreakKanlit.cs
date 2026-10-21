using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// KANLIT IN A BREAK: the same carved bulul wood spirit that catches a fall on Payo (`PayoKanlit`: its baked meshes,
    /// its two shaders, its painted atlas, its vine that hugs the body knees to chest), posed from outside for one frame
    /// at a time by `PayoBreakLift`, as the Arena's break poses its drones through `ArenaDrone.Hold`. Owner, 2026-10-19:
    /// "in arena, the players are lifted by the drones when the map changes. do the same thing here." On Payo the thing
    /// that already plays the drone's part is Kanlit, so Kanlit lifts them: one visual language for every carry.
    ///
    /// ⚠️ IT DECIDES NOTHING AND HAS NO CLOCK OF ITS OWN WORTH SENDING. `PayoBreakLift` names the act, how far through it
    /// is, where the paws are and how far the vine has grown, all off the break's shared clock; this only draws that.
    /// No collider, nothing sent, nothing allocated per frame: two transforms placed and two property-block writes.
    ///
    /// ⚠️ A BREAK RUNS WITH `Time.timeScale` 0, WHICH STOPS THE SHADERS' `_Time`. The leaf's beat, the cowlick, the hoops
    /// and the vine's swell are moved in the shaders off that clock, so this writes its own real-time clock's lead into
    /// `_Pose.w` and `_Lead.w` (both 0 in a catch, so the catch draws exactly as before) and they keep moving.
    ///
    /// THE ACTS (faces: 0 the worried "o", 1 the effort squint, 2 the happy ^ ^):
    ///   DIVE     it swoops down out of the sky onto the player, stretched with speed, the leaf spinning;
    ///   WRAP     it settles over the head with a bounce while the vine grows down and hugs the body; a creak;
    ///   HAUL     the effort face, the leaf flapping hard; it squashes as it takes the weight, then strains up, jiggling;
    ///   ACROSS   banked into its travel, the leaf still beating, a little lighter now;
    ///   SET-DOWN the worried face again, watching the feet down onto the new stone, a squash as they touch;
    ///   LET GO   the flowers fly off, the vine runs back into its paws, a pop and a happy hop; AWAY it zips off up.
    /// </summary>
    public sealed class PayoBreakKanlit : MonoBehaviour
    {
        public enum Act : byte { Dive, Wrap, Haul, Across, SetDown, LetGo, Away }

        /// <summary>Its own CPU time (stopwatch ticks round `Hold`), for a probe's cost line.</summary>
        public static long Ticks;
        private static Material _buddyMat, _vineMat;
        private static bool _warned;
        private static readonly int PoseId = Shader.PropertyToID("_Pose"), GlowId = Shader.PropertyToID("_Glow"), GrowId = Shader.PropertyToID("_Grow"),
            ReleaseId = Shader.PropertyToID("_Release"), LeadId = Shader.PropertyToID("_Lead");

        private Transform _buddy, _vine;
        private MeshRenderer _buddyR, _vineR;
        private MaterialPropertyBlock _buddyBlock, _vineBlock;
        private float _clock, _spin, _yaw;
        private Vector3 _last, _travel;
        private bool _held, _started;
        private Act _act;

        public static PayoBreakKanlit Build(Transform parent)
        {
            if (!Materials()) return null;
            if (PayoKanlitMeshes.Buddy == null || PayoKanlitMeshes.Vine == null)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[Payo] Kanlit's baked meshes are missing (Resources/Map/PayoCatcher): the break's lift draws no Kanlit"); }
                return null;
            }
            var root = new GameObject("Payo break Kanlit");
            root.transform.SetParent(parent, false);
            var self = root.AddComponent<PayoBreakKanlit>();
            self._buddy = Part(root.transform, "kanlit", PayoKanlitMeshes.Buddy, _buddyMat, out self._buddyR);
            self._vine = Part(root.transform, "vine", PayoKanlitMeshes.Vine, _vineMat, out self._vineR);
            self._buddyBlock = new MaterialPropertyBlock(); self._vineBlock = new MaterialPropertyBlock();
            root.SetActive(false);
            return self;
        }

        private static bool Materials()
        {
            if (_buddyMat != null && _vineMat != null) return true;
            Shader a = Shader.Find(PayoKanlit.BuddyShader), b = Shader.Find(PayoKanlit.VineShader);
            if (a == null || b == null)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[Payo] Kanlit's shaders are missing: the break's lift draws no Kanlit"); }
                return false;
            }
            var atlas = PayoKanlitMeshes.Atlas;
            _buddyMat = new Material(a) { name = "Payo break Kanlit (shared)", hideFlags = HideFlags.DontSave, mainTexture = atlas };
            _vineMat = new Material(b) { name = "Payo break Kanlit vine (shared)", hideFlags = HideFlags.DontSave, mainTexture = atlas };
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

        /// <summary>
        /// One frame of the lift: Kanlit's paws at `at` (its bob, squash and bank are added here) drawn `size` big, in the
        /// middle of `act` (`t`, 0 to 1 through it). The vine stands at `vineAt` turned `vineTurn` and sized `vineScale`
        /// (the drawn body's feet, facing and capsule), grown `grow` (0 to 1) and let go `release` seconds ago (under 0:
        /// not yet). `dt` is the caller's step (a break holds `Time.deltaTime` at 0). Only a `voiced` one sounds: a break
        /// lifts every player at once.
        /// </summary>
        public void Hold(Act act, float t, Vector3 at, float size, Vector3 vineAt, Quaternion vineTurn, Vector3 vineScale,
                         float grow, float release, float dt, bool voiced)
        {
            long began = System.Diagnostics.Stopwatch.GetTimestamp();
            if (!_held || !gameObject.activeSelf)
            {
                _held = true; _started = false; _last = at; _travel = Vector3.zero; _clock = 0f;
                gameObject.SetActive(true);
                _yaw = FaceYaw(at, 0f);
            }
            bool entered = !_started || act != _act;
            _started = true; _act = act;
            _clock += dt;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            float glow = PayoNight.Factor;
            // The shaders' clock is stopped in a break: this is how far ahead of it the parts should be.
            float lead = _clock - Time.timeSinceLevelLoad;

            // ---- its own shape and motion, act by act
            int face = 0;
            float flap = 0.5f, spinRate = 22f, tall = 1f, wide = 1f;
            Vector3 bob = Vector3.zero;
            switch (act)
            {
                case Act.Dive:
                {
                    // Stretched with the speed of the swoop, the leaf spinning fast.
                    float stretch = 0.16f * Mathf.Sin(t * Mathf.PI);
                    tall = 1f + stretch; wide = 1f - stretch * 0.5f; flap = 0.3f; spinRate = 18f;
                    break;
                }
                case Act.Wrap:
                {
                    // It lands over the head with a bounce that rings out while the vine goes round.
                    float ring = (1f - t) * Mathf.Cos(t * 14f);
                    bob.y = 0.22f * ring;
                    tall = 1f - 0.1f * ring; wide = 1f + 0.08f * ring; flap = 0.6f; spinRate = 24f;
                    break;
                }
                case Act.Haul:
                {
                    // Taking the weight: a squash and a dip, then the effort, straining up and jiggling.
                    face = 1; flap = 1f; spinRate = 32f;
                    float take = t < 0.2f ? Mathf.Sin(t / 0.2f * Mathf.PI) : 0f;
                    float shake = reduced ? 0f : 0.06f * Mathf.Sin(_clock * 19f);
                    bob.y = -0.3f * take + 0.07f * Mathf.Sin(_clock * 9.5f);
                    bob.x = Mathf.Sin(_clock * 3.1f) * 0.06f;
                    tall = 1f - 0.2f * take + 0.1f * (1f - take) + shake; wide = 1f + 0.14f * take - shake * 0.6f;
                    break;
                }
                case Act.Across:
                {
                    face = 1; flap = 0.75f; spinRate = 27f;
                    float shake = reduced ? 0f : 0.035f * Mathf.Sin(_clock * 17f);
                    bob = new Vector3(Mathf.Sin(_clock * 3.1f) * 0.06f, 0.05f * Mathf.Sin(_clock * 7f), 0f);
                    tall = 1f + shake; wide = 1f - shake * 0.6f;
                    break;
                }
                case Act.SetDown:
                {
                    // Watching the feet down; a squash as they touch.
                    face = 0; flap = 0.8f; spinRate = 22f;
                    float touch = t > 0.82f ? Mathf.Sin((t - 0.82f) / 0.18f * Mathf.PI) : 0f;
                    bob.y = 0.04f * Mathf.Sin(_clock * 6f) - 0.12f * touch;
                    tall = 1f - 0.14f * touch; wide = 1f + 0.1f * touch;
                    break;
                }
                case Act.LetGo:
                {
                    // The hop (its height is the caller's); a squash on the way up and a stretch at the top.
                    face = 2; flap = 0.5f; spinRate = 20f;
                    float s = Mathf.Sin(t * Mathf.PI);
                    tall = 1f + 0.12f * s - 0.12f * (1f - s) * (t < 0.3f ? 1f : 0f); wide = 1f + 0.08f * (1f - s) * (t < 0.3f ? 1f : 0f);
                    break;
                }
                default:
                    face = 2; flap = 0.6f; spinRate = 30f; tall = 1.12f; wide = 0.92f;
                    break;
            }
            at += bob;
            if (dt > 1e-5f) _travel = Vector3.Lerp(_travel, (at - _last) / dt, Mathf.Clamp01(dt * 8f));
            _last = at;
            _spin += spinRate * dt;

            // ---- the buddy: facing whoever is watching, banked into its travel across the stage
            float spinOfJoy = act == Act.LetGo ? 360f * Mathf.SmoothStep(0f, 1f, t) : 0f;
            _yaw = Mathf.LerpAngle(_yaw, FaceYaw(at, _yaw), Mathf.Clamp01(dt * 7f));
            var flat = _travel; flat.y = 0f;
            float bank = act == Act.Across || act == Act.Away ? Mathf.Min(18f, flat.magnitude * 2.2f) : 0f;
            var lean = bank > 0.5f ? Quaternion.AngleAxis(bank, Vector3.Cross(Vector3.up, flat.normalized)) : Quaternion.identity;
            _buddy.SetPositionAndRotation(at, lean * Quaternion.Euler(0f, _yaw + spinOfJoy, 0f));
            _buddy.localScale = new Vector3(wide, tall, wide) * (PayoKanlit.BuddyScale * size);
            _buddyBlock.SetVector(PoseId, new Vector4(_spin, face, flap, lead));
            _buddyBlock.SetFloat(GlowId, glow);
            _buddyR.SetPropertyBlock(_buddyBlock);

            // ---- the vine: on the drawn body, its lead in the paws
            bool vine = grow > 0.001f;
            if (_vineR.enabled != vine) _vineR.enabled = vine;
            if (vine)
            {
                _vine.SetPositionAndRotation(vineAt, vineTurn);
                _vine.localScale = vineScale;
                Vector3 paws = _vine.InverseTransformVector(at - _vine.TransformPoint(PayoKanlitMeshes.Lead));
                _vineBlock.SetFloat(GrowId, grow);
                _vineBlock.SetFloat(ReleaseId, release);
                _vineBlock.SetVector(LeadId, new Vector4(paws.x, paws.y, paws.z, lead));
                _vineBlock.SetFloat(GlowId, glow);
                _vineR.SetPropertyBlock(_vineBlock);
            }

            if (entered && voiced) Enter(act);
            Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - began;
        }

        /// <summary>Let go of a Kanlit posed through `Hold`.</summary>
        public void Release()
        {
            _held = false; _started = false;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        // The break is watched through its own camera, the same on every peer: it shows that camera its face.
        private static float FaceYaw(Vector3 at, float fallback)
        {
            var view = ArenaFx.View;
            if (view == null) return fallback;
            var to = view.transform.position - at; to.y = 0f;
            return to.sqrMagnitude > 0.01f ? Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg : fallback;
        }

        // Each act's one sound: the catch's own cues, flat, because the break is watched from far off.
        private static void Enter(Act act)
        {
            switch (act)
            {
                case Act.Dive: ArenaFx.CueFlat("sfx_arena_drone_zip", 1.25f, 1.4f, 0.5f); break;
                case Act.Wrap: ArenaFx.CueFlat("sfx_arena_balloon_creak", 1.2f, 1.35f, 0.45f); break;
                case Act.Haul: ArenaFx.CueFlat("sfx_arena_balloon_creak", 0.95f, 1.05f, 0.45f); break;
                case Act.LetGo:
                    ArenaFx.CueFlat("sfx_arena_balloon_pop", 1.5f, 1.7f, 0.45f);
                    ArenaFx.CueFlat("sfx_arena_balloon_boing", 1.3f, 1.45f, 0.4f);
                    break;
                case Act.Away: ArenaFx.CueFlat("sfx_arena_drone_zip", 1.4f, 1.55f, 0.4f); break;
            }
        }
    }
}
