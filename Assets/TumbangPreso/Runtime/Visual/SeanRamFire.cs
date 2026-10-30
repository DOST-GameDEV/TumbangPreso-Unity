using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// LAVA RAM'S LOOK IN A MATCH: the approved STOKE STEP wake (c3, owner 2026-10-18: "stoke - c3") fitted to the RAM's own
    /// path, with the approved EMPOWERED THROW burst (s2, "s2 for empowered") where he meets a body and the approved cloud as
    /// steam where he meets a wall. A dash wake is a dash wake and a burst is a burst: nothing here is a new picture.
    ///
    /// ⚠️⚠️ WHY THIS EXISTS AND `SeanStokeFire` WAS NOT ENOUGH. The rework (`c10427458`) kept calling `SeanStokeFire`, and that
    /// effect is the c3 bench object with STOKE STEP's numbers built into it: a 0.18 s brace and a 2 m hop at 10.95 m/s that
    /// friction stops in 0.37 s. The RAM braces 0.25 s and charges 8 m, held at 14 m/s and then slid out, in 0.80 s
    /// (`SeanRules.RamTravelSeconds`). Read off the shader (`SeanFireComet.shader`, `Gone`): the fireball's head is the lesser
    /// of where the bench's rule has him and where he really is, and the bench's rule stops at 2.0 m. So in a match the
    /// fireball stalled 2 m from where he left while his body charged six metres on without it, and `SeanStokeFire` told the
    /// wake he had planted 0.37 s after the launch, so it curled up and was eaten while he was still at full speed.
    ///
    /// ⚠️ THE FIT CHANGES NO PICTURE, ONLY THE INPUTS THE BENCH ALREADY HAS (`docs/VFX_BENCH_METHOD.md` section 4):
    ///   * the fireball and the embers are told a stand-in rule whose stop time and length are the RAM's
    ///     (`ModelLaunch`, `ModelFriction`), solved so it is never BEHIND his real body (checked at every hundredth of a
    ///     second of an 8 m charge: it leads by at most 0.70 m). The head is the lesser of the two, so it is always his body;
    ///     the stand-in only decides when the embers may be born, and they are still never drawn ahead of him;
    ///   * the bench is posed on a clock whose launch is the bench's own (`FxStudySeanStoke.Brace`), so the brace's gather
    ///     is played over his real 0.25 s and everything after the launch runs at 1x;
    ///   * the bench hides the fireball at its own travel window (`Brace + Travel + 0.4`), which a full RAM outlives, so the
    ///     fireball's show and clock are written here after the bench poses, from his real stop instead.
    /// The bench file is not touched: every one of those is a material input or a piece's own on/off.
    ///
    /// IT IS A LOOK ONLY. Every peer that runs the RAM's `Activate` (the press), `OnActivate` (the launch) and its contact
    /// checks draws this from those calls and from his transform; nothing is sent. Pooled per Rago, as `SeanStokeFire` is.
    /// </summary>
    public sealed class SeanRamFire : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ THE ONE SWITCH. On, LAVA RAM is drawn as above. Off, it is drawn exactly as the rework left it: `SeanStokeFire`
        /// (STOKE STEP's c3 with STOKE STEP's numbers) and no contact or wall picture of its own. TO GO BACK, MAKE THE LINE
        /// BELOW `false`.
        /// </summary>
        // KEEP: owner 2026-10-29, "keep the r1, lock it in" (LAVA RAM r1: Stoke c3 refit to the 8 m charge). Approved look; do not change.
        public static bool Painted = true;

        /// <summary>The RAM's brace, travel and length, read from the rule so the picture cannot drift from it.</summary>
        public static float BraceSeconds => SeanRules.RamMinBraceSeconds;
        public static float TravelSeconds => SeanRules.RamTravelSeconds;
        public static float LengthMetres => SeanRules.RamTravel;
        /// <summary>
        /// The stand-in rule the bench's shaders are told: a launch that friction stops in exactly `Travel` over exactly
        /// `Length` (v = 2L / T, a = v / T: 19.9 m/s and 24.7 m/s^2). The real charge is held at 14 m/s for 0.34 s and then
        /// slides; this one starts faster and slows sooner, so it is ahead of him or level with him the whole way.
        /// </summary>
        public static float ModelLaunch => 2f * LengthMetres / TravelSeconds;
        public static float ModelFriction => ModelLaunch / TravelSeconds;

        /// <summary>
        /// The contact burst's size: the s2 burst and ring drawn at the RAM's contact reach (0.9 m) instead of the old
        /// 1.25 m throw push, proportions kept. The RAM throws one body, so a ring at the throw's old area would be a lie.
        /// </summary>
        public static float BurstRadius => SeanRules.RamContactReach;

        // How long after the contact the burst, ring, embers and smoke are still drawing (`SeanThrowFire.AfterImpact`).
        private const float BurstLife = 1.05f;
        private const float SteamLife = 1.0f;
        // A press whose launch has not come this long after it was a cast that was stopped (stunned in the brace).
        private const float LaunchLate = 0.6f;

        private static readonly Dictionary<CharacterMotor, SeanRamFire> Pool = new Dictionary<CharacterMotor, SeanRamFire>();
        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity"),
                                    ErodeId = Shader.PropertyToID("_Erode");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetPool() => Pool.Clear();

        private Transform _body;
        private FxStudySeanStoke _wake;
        private Transform _wakeHolder;
        private GameObject _fireball;
        private Material _comet;
        private FxStudySeanThrow _burst;
        private Transform _burstHolder;
        private CloudPuffField _steam;
        private Vector3 _from, _dir;
        private float _age, _launchAge = -1f, _front, _movedAt, _stoppedAt = -1f, _burstAt = -1f, _steamAt = -1f;
        private bool _live, _staged;

        // ------------------------------------------------------------------ the match's calls (every peer)

        /// <summary>The press: the brace begins where he stands, facing his aim.</summary>
        public static void Brace(AbilityContext ctx)
        {
            if (!Painted || !Application.isPlaying || ctx?.Motor == null) return;
            Vector3 dir = ctx.Forward; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            For(ctx.Motor)?.Begin(dir.normalized);
        }

        /// <summary>The launch, along `dir` (the heading the ability charges him along).</summary>
        public static void Burst(CharacterMotor who, Vector3 dir)
        {
            if (!Painted || !Application.isPlaying || who == null) return;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            var fire = For(who);
            if (fire == null) return;
            // A launch with no brace before it (a peer that joined inside the brace) starts the clock at the launch.
            if (!fire._live) fire.Begin(dir.normalized);
            fire.Launch();
        }

        /// <summary>He stopped dead on a body standing at `victim`: the s2 burst on the court between them.</summary>
        public static void Contact(CharacterMotor who, Vector3 victim)
        {
            if (!Painted || !Application.isPlaying || who == null) return;
            if (Pool.TryGetValue(who, out var fire) && fire != null && fire._live)
                fire.Hit(Vector3.Lerp(who.transform.position, victim, 0.6f));
        }

        /// <summary>He ran into a wall at `point`: he is the one dazed, so steam rises off him, not a burst.</summary>
        public static void Wall(CharacterMotor who, Vector3 point)
        {
            if (!Painted || !Application.isPlaying || who == null) return;
            if (Pool.TryGetValue(who, out var fire) && fire != null && fire._live) fire.Steam();
        }

        private static SeanRamFire For(CharacterMotor who)
        {
            if (Pool.TryGetValue(who, out var fire) && fire != null) return fire;
            fire = Make(null, who.transform);
            Pool[who] = fire;
            return fire;
        }

        // ------------------------------------------------------------------ the film's calls (outside Play)

        /// <summary>A look not in the pool, under `parent`, following `body`. The film steps it with `Advance`.</summary>
        public static SeanRamFire Stage(Transform parent, Transform body)
        {
            var fire = Make(parent, body);
            fire._staged = true;
            return fire;
        }
        public void StagedBrace(Vector3 dir) { dir.y = 0f; Begin(dir.normalized); }
        public void StagedLaunch() => Launch();
        public void StagedContact(Vector3 victim) => Hit(Vector3.Lerp(_body.position, victim, 0.6f));
        public void StagedWall() => Steam();

        // ------------------------------------------------------------------ the look

        private static SeanRamFire Make(Transform parent, Transform body)
        {
            var go = new GameObject("SeanRamFire");
            if (parent != null) go.transform.SetParent(parent, false);
            var fire = go.AddComponent<SeanRamFire>();
            fire._body = body;

            // The wake: the c3 bench object built exactly as `SeanStokeFire` builds it (half of it covers in daylight).
            fire._wakeHolder = new GameObject("wake").transform;
            fire._wakeHolder.SetParent(go.transform, false);
            fire._wake = new FxStudySeanStoke(fire._wakeHolder, AmihanWhirlwind.DayLight);
            var piece = fire._wake.Root.Find("fireball");
            fire._fireball = piece != null ? piece.gameObject : null;
            fire._comet = piece != null ? piece.GetComponent<MeshRenderer>()?.sharedMaterial : null;
            if (fire._comet != null) { fire._comet.SetFloat("_Launch", ModelLaunch); fire._comet.SetFloat("_Friction", ModelFriction); }
            // The embers and the needles share one material: told the RAM's length, so a dozen of them are shed along 8 m
            // rather than all in the first two.
            var embers = fire._wake.Root.Find("embers")?.GetComponent<MeshRenderer>()?.sharedMaterial;
            if (embers != null)
            {
                embers.SetFloat("_Length", LengthMetres); embers.SetFloat("_Launch", ModelLaunch); embers.SetFloat("_Friction", ModelFriction);
            }

            // The contact burst: the s2 look exactly as a match builds it, its burst and ring at the contact reach.
            fire._burstHolder = new GameObject("contact").transform;
            fire._burstHolder.SetParent(go.transform, false);
            fire._burst = SeanThrowFire.NewLook(fire._burstHolder);
            var burstLook = fire._burst.Root.Find("throw-fire")?.GetComponent<MeshRenderer>()?.sharedMaterial;
            if (burstLook != null) burstLook.SetFloat("_Radius", BurstRadius);

            // The steam off a dazed RAM: the approved cloud, pale.
            fire._steam = new CloudPuffField(go.transform, "Sean ram steam");
            fire._steam.Show(false);

            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
            fire._wakeHolder.gameObject.SetActive(false);
            fire._burstHolder.gameObject.SetActive(false);
            return fire;
        }

        private void Begin(Vector3 dir)
        {
            _dir = dir;
            _from = _staged ? _body.position : VfxShapes.GroundPoint(_body.position);
            _wakeHolder.SetPositionAndRotation(_from, Quaternion.LookRotation(dir, Vector3.up));
            _age = 0f; _launchAge = -1f; _front = 0f; _movedAt = 0f; _stoppedAt = -1f; _burstAt = -1f; _steamAt = -1f; _live = true;
            _wake.Restart();
            _burst.Restart();
            _steam?.Clear(); _steam?.Show(false);
            _wakeHolder.gameObject.SetActive(true);
            _burstHolder.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        private void Launch()
        {
            if (_launchAge >= 0f) return;
            _launchAge = _age; _movedAt = _age;
        }

        private void Hit(Vector3 at)
        {
            if (_burstAt >= 0f) return;
            _burstAt = _age;
            // On the court where they met: the s2 burst stands on its ring, as it does where a loaded throw lands.
            Vector3 ground = _staged ? new Vector3(at.x, _from.y, at.z) : VfxShapes.GroundPoint(at);
            _burst.Restart();
            _burst.Impact(_burstHolder.InverseTransformPoint(ground), _age);
            _burstHolder.gameObject.SetActive(true);
        }

        private void Steam()
        {
            if (_steamAt >= 0f || _steam == null || !_steam.Valid) return;
            _steamAt = _age;
            _steam.Clear();
            // A ring of steam over his head (the plan's "he rebounds and sits dazed under a ring of steam"): three soft pale
            // puffs round it, out of step, rising. Typed one by one.
            Vector3 head = _body.position + Vector3.up * 1.45f;
            Vector3 side = Vector3.Cross(Vector3.up, _dir);
            Spawn(head + side * 0.30f - _dir * 0.05f, 1.00f, 0.70f, 0.00f, _age + 0.02f);
            Spawn(head - side * 0.28f + _dir * 0.08f, 0.90f, 0.62f, 2.10f, _age + 0.07f);
            Spawn(head - _dir * 0.30f + Vector3.up * 0.10f, 0.80f, 0.55f, 4.00f, _age + 0.12f);
            _steam.Show(true);
        }

        private void Spawn(Vector3 at, float scale, float life, float turn, float when)
        {
            var p = CloudPuffField.Puff.Landing(at);
            p.Scale = scale; p.Life = life; p.Reach = 0.20f; p.Lift = 1.1f; p.Share = 0.75f; p.Turn = turn;
            // Steam, not ash: warm white over a pale grey shade, a soft grey line.
            p.Lit = new Color(0.94f, 0.92f, 0.89f); p.Shade = new Color(0.72f, 0.70f, 0.69f); p.Line = new Color(0.46f, 0.44f, 0.43f);
            _steam.Spawn(p, when);
        }

        private void Update()
        {
            if (_staged || !_live) return;
            var lens = Camera.main;
            Advance(Time.deltaTime, lens != null ? lens.transform.position : _body.position + new Vector3(0f, 1.6f, -6f));
        }

        /// <summary>Moves the look's clock on by `dt` and poses it for an eye at `eye` (the film calls this frame by frame).</summary>
        public void Advance(float dt, Vector3 eye)
        {
            if (!_live) return;
            if (_body == null) { End(); return; }
            _age += dt;
            if (_launchAge < 0f && _age > BraceSeconds + LaunchLate) { End(); return; }
            float t = BenchClock();
            float since = _launchAge >= 0f ? _age - _launchAge : -1f;

            // ⚠️ THE WAKE'S FRONT IS HIS BODY, as in `SeanStokeFire`: how far he really is along the charge, never let go back,
            // and when he has not moved on for a fifteenth of a second it is over there. A RAM that stops dead on a body is a
            // short wake that dies where he stopped.
            if (since >= 0f)
            {
                float now = Vector3.Dot(_body.position - _from, _dir);
                if (now > _front + 0.004f) { _front = now; _movedAt = _age; }
                else if (_stoppedAt < 0f && since > 0.06f && _age - _movedAt > 0.07f) _stoppedAt = _movedAt;
            }
            // How long ago he stopped, in seconds; below nothing while he is still going. Only his real stop, or the rule's
            // own end of the charge if his body never reported one (never STOKE STEP's 0.37 s).
            float stopped = _stoppedAt >= 0f ? _age - _stoppedAt : (since > TravelSeconds + 0.10f ? since - TravelSeconds : -1f);
            _wake.Pose(t, eye, Mathf.Max(0f, _front), stopped);

            // The fireball, from his real stop: the bench's own window and death (+0.4 s), measured from when he stopped.
            if (_fireball != null && _comet != null)
            {
                float stopT = stopped >= 0f ? t - stopped : FxStudySeanStoke.Brace + TravelSeconds;
                bool shown = t >= FxStudySeanStoke.Brace - 0.06f && t <= stopT + 0.4f;
                if (_fireball.activeSelf != shown) _fireball.SetActive(shown);
                if (shown) { _comet.SetFloat(AgeId, t); _comet.SetFloat(OpacityId, 1f); _comet.SetFloat(ErodeId, 0f); }
            }

            bool burstLive = _burstAt >= 0f && _age - _burstAt < BurstLife;
            if (_burstAt >= 0f)
            {
                if (_burstHolder.gameObject.activeSelf != burstLive) _burstHolder.gameObject.SetActive(burstLive);
                if (burstLive)
                {
                    Vector3 local = _burstHolder.InverseTransformPoint(eye);
                    _burst.Pose(_age, local, _burstHolder.InverseTransformPoint(_body.position), false, Vector3.forward);
                }
            }
            bool steamLive = _steamAt >= 0f && _age - _steamAt < SteamLife;
            if (_steam != null && _steam.Valid)
            {
                _steam.Show(steamLive);
                if (steamLive) _steam.Hold(_age);
            }

            float stopAt = stopped >= 0f ? t - stopped : FxStudySeanStoke.Brace + TravelSeconds;
            if (t > Mathf.Max(FxStudySeanStoke.Life, stopAt + 1.6f) + 0.1f && !burstLive && !steamLive) End();
        }

        /// <summary>
        /// The bench's clock: through the brace, the bench's 0.18 s of gather played over his real brace (held just short of
        /// the launch if his brace runs long); from the launch on, the bench's launch plus real time, so the fire runs at 1x.
        /// </summary>
        private float BenchClock()
        {
            float b = FxStudySeanStoke.Brace;
            if (_launchAge < 0f) return Mathf.Min(_age * (b / BraceSeconds), b - 0.005f);
            return b + (_age - _launchAge);
        }

        private void End()
        {
            _live = false;
            if (_steam != null && _steam.Valid) _steam.Show(false);
            if (_staged) { _wakeHolder.gameObject.SetActive(false); _burstHolder.gameObject.SetActive(false); return; }
            gameObject.SetActive(false);
        }

        public bool Live => _live;

        private void OnDestroy()
        {
            if (_body != null && !_staged)
                foreach (var pair in Pool)
                    if (pair.Value == this) { Pool.Remove(pair.Key); break; }
            _wake?.Release(); _wake = null;
            _burst?.Release(); _burst = null;
            _steam?.Dispose(); _steam = null;
        }
    }
}
