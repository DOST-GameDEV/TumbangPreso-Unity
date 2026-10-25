using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ SOULBOUND, NEMU'S PASSIVE (owner 2026-10-21, replacing DEAD WEIGHT's slow): *"when Nemu's ghosts expire they revert
    /// into a dormant ghost from where they were instead of going into cooldown"*. This is that ghost.
    ///
    ///  * IT HAS TO READ AS SOMETHING TO PICK UP (owner on v1: *"its not really like clear or intuitive that you can pick them
    ///    up, see Gekko as reference but dont copy 1:1"*). So the ghost curls up asleep inside a glass soul-globe in its own
    ///    colour (Peeker icy blue, Fetcher green, Keeper lantern orange) with Nemu's violet on the rim (`Shaders/SoulOrb`),
    ///    floating at waist height over a pool of the same light on the court. Gekko's globs are a creature in a bubble;
    ///    hers stay her ghosts, asleep, and the globe is hers. For Nemu the globe quickens and brightens once she is in
    ///    reach, her reclaim ring is drawn round it (`Visual.NemuGhostRings`), the slipper's recall mark rides it with its
    ///    window's clock on the ring (`UI.SlipperRecall.TrackDormant`), and Grab reads "Retrieve Peeker/Fetcher/Keeper".
    ///  * IT WAITS 10 S AND FOLLOWS HER (owner's third pass): it drifts after her at 1.5 m/s from 5 m out, 1.25 m/s by
    ///    3.5 m, slowing as it nears and stopping beside her, inside her reach (`NemuRules.DormantFollowSpeedAt`). Reclaimed in time, the skill takes
    ///    its 15 s cooldown; left, it fades away and the skill takes 25 s. The skill's tile counts the window down.
    ///  * Peeker's is left where it let go of its victim, or on its spot when nobody came; Fetcher's and Keeper's where they
    ///    last stood.
    ///  * While it is here, that skill cannot be cast and its cooldown is held full (`NemuHeroKit`). Nemu reclaims it by
    ///    holding Grab, the RETRIEVE SLIPPER / RESET CAN button, within `NemuRules.ReclaimRadius` of it for
    ///    `NemuRules.ReclaimSeconds`; the globe rises and shrinks toward her as she holds it, then pops.
    ///  * ONLY THE HOST SPAWNS AND RECLAIMS IT. Peers draw the host's copy from the ghost frames (`NemuGhostFrame`, kind 4),
    ///    with its reclaim progress, so every screen sees it lift toward her as she channels.
    ///  * One per skill per Nemu. None outlives the round (`WorldEffectSnapshot.ClearPersistentFields`).
    /// </summary>
    public sealed partial class NemuDormant : MonoBehaviour
    {
        public enum Ghost : byte { Peeker = 1, Fetcher = 2, Keeper = 3 }

        public static readonly List<NemuDormant> Live = new List<NemuDormant>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLive() => Live.Clear();

        public int OwnerSlot { get; private set; } = -1;
        public Ghost Kind { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Facing { get; private set; } = Vector3.forward;
        /// <summary>How far through being reclaimed it is, 0 to 1 (the host's channel, drawn on every peer).</summary>
        public float Progress { get; set; }
        /// <summary>Her own screen's bar while she channels, ahead of the host's (drawn as the larger of the two).</summary>
        public float LocalProgress { get; set; }
        public bool Reclaimed => _reclaimed;
        /// <summary>Its 10 s ran out before she came for it: it is fading away.</summary>
        public bool Expired => _expired;
        /// <summary>Seconds left of its window, and the share of it left (the recall ring's clock).</summary>
        public float TimeLeft => Mathf.Max(0f, NemuRules.DormantSeconds - _age);
        public float TimeLeftRatio => TimeLeft / NemuRules.DormantSeconds;
        /// <summary>The middle of its globe, where the recall mark points.</summary>
        public Vector3 Centre => _centre;
        public Color Colour => _colour;

        /// <summary>The name the reclaim prompt reads: "Retrieve Peeker", "Retrieve Fetcher", "Retrieve Keeper".</summary>
        public static string NameOf(Ghost kind) => kind == Ghost.Peeker ? "Peeker" : kind == Ghost.Fetcher ? "Fetcher" : "Keeper";
        public string Name => NameOf(Kind);

        private const float SettleSeconds = .4f, GoneSeconds = .45f, GhostScale = .4f, OrbRadius = .34f, Float = .6f, PoolRadius = .6f;
        private static readonly Color Violet = new Color(.62f, .26f, 1f, 1f);
        private static readonly int ColorId = Shader.PropertyToID("_Color"), RimId = Shader.PropertyToID("_Rim"),
            FadeId = Shader.PropertyToID("_Fade"), PulseId = Shader.PropertyToID("_Pulse");
        private static Mesh _sphere, _pool;
        private static Material _orbMaterial, _poolMaterial;
        private NemuGhostBody _body;
        private NemuSmokeBatch _fx;
        private MeshRenderer _orb, _poolRenderer;
        private MaterialPropertyBlock _orbBlock, _poolBlock;
        private Color _colour;
        private float _age, _goneAt, _shown, _near, _pulseClock;
        private bool _reclaimed, _expired, _networkDriven;
        private Vector3 _centre, _networkAt;

        /// <summary>The ghost's own colour, which its globe and pool take.</summary>
        public static Color ColourOf(Ghost kind) => kind == Ghost.Peeker ? BlindfoldGhostModel.Aura
            : kind == Ghost.Fetcher ? NemuGhostModels.SkitterBody : NemuGhostModels.LookoutBody;

        /// <summary>Leaves Nemu's dormant <paramref name="kind"/> ghost at <paramref name="at"/> (her last one of that kind goes).</summary>
        public static NemuDormant Spawn(Ghost kind, Vector3 at, Vector3 facing, int ownerSlot)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null && Live[i].OwnerSlot == ownerSlot && Live[i].Kind == kind) Destroy(Live[i].gameObject);
            var go = new GameObject("NemuDormant" + NameOf(kind));
            var dormant = go.AddComponent<NemuDormant>();
            dormant.OwnerSlot = ownerSlot;
            dormant.Kind = kind;
            dormant.Position = VfxShapes.GroundPoint(at);
            facing.y = 0.0f;
            dormant.Facing = facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector3.forward;
            go.transform.position = dormant.Position;
            var mesh = kind == Ghost.Peeker ? BlindfoldGhostModel.Waiting
                : kind == Ghost.Fetcher ? NemuGhostModels.Skitter : NemuGhostModels.Lookout;
            dormant._colour = ColourOf(kind);
            dormant._body = new NemuGhostBody(go.transform, "DormantBody", mesh, Violet);
            dormant._fx = new NemuSmokeBatch(go.transform, "DormantFx", 2, 6, 1);
            dormant.BuildGlobe();
            GameServices.Audio?.PlayAt("sfx_ghost_teleport", dormant.Position);
            return dormant;
        }

        private void BuildGlobe()
        {
            if (_orbMaterial == null)
            {
                var shader = Shader.Find("TumbangPreso/SoulOrb");
                if (shader != null) _orbMaterial = new Material(shader) { name = "SoulOrb" };
                else Debug.LogError("[NemuDormant] Shaders/SoulOrb is missing from the build (GameBuilder).");
            }
            if (_poolMaterial == null) _poolMaterial = new Material(Shader.Find("Sprites/Default")) { name = "SoulPool" };
            if (_sphere == null)
            {
                var probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _sphere = probe.GetComponent<MeshFilter>().sharedMesh;
                Destroy(probe);
            }
            if (_pool == null) _pool = BuildPool();
            _orb = Part("SoulGlobe", _sphere, _orbMaterial);
            _poolRenderer = Part("SoulPool", _pool, _poolMaterial);
            _orbBlock = new MaterialPropertyBlock();
            _poolBlock = new MaterialPropertyBlock();
        }

        private MeshRenderer Part(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.enabled = material != null;
            return r;
        }

        /// <summary>A flat disc of light, bright in the middle and gone at its edge (coloured per vertex, tinted per ghost).</summary>
        private static Mesh BuildPool()
        {
            const int sides = 40;
            var v = new Vector3[sides + 2]; var c = new Color[sides + 2]; var t = new int[sides * 3];
            v[0] = Vector3.zero; c[0] = new Color(1f, 1f, 1f, .55f);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                v[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                c[i + 1] = new Color(1f, 1f, 1f, 0f);
            }
            for (int i = 0; i < sides; i++) { t[i * 3] = 0; t[i * 3 + 1] = i + 1; t[i * 3 + 2] = i + 2; }
            var mesh = new Mesh { name = "SoulPool", hideFlags = HideFlags.DontSave, vertices = v, colors = c, triangles = t };
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>Nemu's dormant ghost of this kind still waiting to be reclaimed, or null.</summary>
        public static NemuDormant For(int ownerSlot, Ghost kind)
        {
            foreach (var d in Live) if (d != null && !d._reclaimed && !d._expired && d.OwnerSlot == ownerSlot && d.Kind == kind) return d;
            return null;
        }

        /// <summary>The nearest of her unreclaimed dormant ghosts within reclaim reach of <paramref name="at"/>, flat.</summary>
        public static NemuDormant InReach(int ownerSlot, Vector3 at)
        {
            NemuDormant best = null;
            float bestDistance = NemuRules.ReclaimRadius;
            foreach (var d in Live)
            {
                if (d == null || d._reclaimed || d._expired || d.OwnerSlot != ownerSlot) continue;
                float distance = new Vector2(d.Position.x - at.x, d.Position.z - at.z).magnitude;
                if (distance <= bestDistance) { bestDistance = distance; best = d; }
            }
            return best;
        }

        /// <summary>Taken back: it rises into her and is gone.</summary>
        public void Reclaim()
        {
            if (_reclaimed || _expired) return;
            _reclaimed = true; _goneAt = _age; Progress = 1f;
            GameServices.Audio?.PlayAt("sfx_possess_enter", Position);
        }

        /// <summary>Her window ran out: it sighs out and fades where it is (the skill takes the longer cooldown).</summary>
        private void Expire()
        {
            if (_reclaimed || _expired) return;
            _expired = true; _goneAt = _age;
            GameServices.Audio?.PlayAt("sfx_nemu_dontlook_fade", Position);
        }

        /// <summary>
        /// ONLY THE HOST MOVES IT: it drifts after Nemu, slowing as it nears her and stopping beside her
        /// (`NemuRules.DormantFollowSpeedAt`); peers ease toward the host's frames.
        /// </summary>
        private void Follow(float dt)
        {
            if (_networkDriven)
            {
                Position = Vector3.Lerp(Position, _networkAt, 1f - Mathf.Exp(-12f * dt));
                return;
            }
            if (_reclaimed || _expired || PresentationClock.Held) return;
            var her = GameServices.Round?.PlayerAt(OwnerSlot);
            if (her == null || !her.gameObject.activeInHierarchy) return;
            Vector3 to = her.transform.position - Position; to.y = 0f;
            float speed = NemuRules.DormantFollowSpeedAt(to.magnitude);
            if (speed <= 0f) return;
            var step = to.normalized * Mathf.Min(speed * dt, to.magnitude - NemuRules.DormantStopDistance);
            Position = VfxShapes.GroundPoint(Position + step);
            Facing = Vector3.Slerp(Facing, to.normalized, 1f - Mathf.Exp(-3f * dt));
        }

        private void OnEnable() { Live.Add(this); NemuGhostRings.Ensure(); }
        private void OnDisable() => Live.Remove(this);
        private void OnDestroy() { _fx?.Dispose(); _fx = null; }

        /// <summary>Nemu herself, on her own screen, standing close enough to take it back.</summary>
        private bool HerReachIsHere()
        {
            int seat = GameLaunch.Spectator ? -1 : NetAuthority.LocalSlot;
            var observer = Camera.main != null ? Camera.main.GetComponent<CameraSystem.SpectatorCamera>() : null;
            if (observer != null && observer.isActiveAndEnabled) seat = observer.PlayerViewTarget?.PlayerSlot ?? -1;
            if (seat != OwnerSlot) return false;
            var her = GameServices.Round?.PlayerAt(OwnerSlot);
            if (her == null) return false;
            Vector3 d = her.transform.position - Position; d.y = 0f;
            return d.magnitude <= NemuRules.ReclaimRadius;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            // ⚠️ THE GATE IS WRITTEN OUT, NOT HOISTED: `tools/audit_ability_authority.py` reads it by brace depth.
            if (NetAuthority.ShouldResolve())
            {
                if (!_networkDriven && !_reclaimed && !_expired && _age >= NemuRules.DormantSeconds) Expire();
            }
            Follow(dt);
            float gone = _reclaimed || _expired ? Mathf.Clamp01((_age - _goneAt) / GoneSeconds) : 0f;
            if (gone >= 1f) { Destroy(gameObject); return; }
            _shown = Mathf.MoveTowards(_shown, Mathf.Clamp01(Mathf.Max(Progress, LocalProgress)), dt * 4f);
            _near = Mathf.MoveTowards(_near, !_reclaimed && !_expired && HerReachIsHere() ? 1f : 0f, dt * 5f);
            // It breathes slowly asleep, and quickens when she is in reach: the "this is yours, take it" beat.
            _pulseClock += dt * Mathf.Lerp(1.4f, 4.2f, _near);
            float pulse = .5f + .5f * Mathf.Sin(_pulseClock);
            // It drops in with a little overshoot, as if set down.
            float settle = Mathf.Clamp01(_age / SettleSeconds);
            float pop = settle >= 1f ? 1f : 1f + Mathf.Sin(settle * Mathf.PI) * .25f - (1f - settle) * (1f - settle);
            // Bobbing at waist height; reclaimed, it rises and shrinks toward her as she holds it, then pops up and away.
            float bob = .05f * Mathf.Sin(_age * 1.6f);
            // Fading unclaimed, it sinks into its pool instead of rising.
            float rise = _expired ? -.45f * gone : 1.1f * gone * gone;
            Vector3 centre = Position + Vector3.up * (Float + bob + .1f * _near + .35f * _shown + rise);
            _centre = centre;
            // The last seconds of its window it flickers, so a glance says it is about to go.
            float flicker = !_reclaimed && !_expired && TimeLeft < 3f ? .75f + .25f * Mathf.Sin(_age * 18f) : 1f;
            float size = Mathf.Max(0f, pop) * Mathf.Lerp(1f, .7f, _shown) * (1f - gone) * (1f + .06f * pulse * (.3f + _near));
            float fade = Mathf.Clamp01(_age / .2f) * (1f - gone * gone) * flicker;

            if (_orb != null)
            {
                _orb.transform.SetPositionAndRotation(centre, Quaternion.identity);
                _orb.transform.localScale = Vector3.one * (OrbRadius * 2f * size);
                _orbBlock.SetColor(ColorId, _colour);
                _orbBlock.SetColor(RimId, Violet);
                _orbBlock.SetFloat(FadeId, fade);
                _orbBlock.SetFloat(PulseId, Mathf.Clamp01(pulse * (.35f + .65f * _near) + _shown));
                _orb.SetPropertyBlock(_orbBlock);
            }
            if (_poolRenderer != null)
            {
                _poolRenderer.transform.SetPositionAndRotation(Position + Vector3.up * .03f, Quaternion.identity);
                _poolRenderer.transform.localScale = Vector3.one * (PoolRadius * Mathf.Max(0f, pop) * (1f - gone) * (1f + .12f * pulse));
                var tint = _colour; tint.a = fade * Mathf.Clamp01(.55f + .3f * pulse * (.4f + _near) + .3f * _shown);
                _poolBlock.SetColor(ColorId, tint);
                _poolRenderer.SetPropertyBlock(_poolBlock);
            }
            // Curled up asleep in the middle of its globe, turning slowly; it wakes and faces up as she takes it back.
            var facing = Quaternion.AngleAxis(_age * 25f, Vector3.up) * Quaternion.LookRotation(Facing)
                * Quaternion.AngleAxis(Mathf.Lerp(25f, 0f, Mathf.Max(_shown, _near * .5f)), Vector3.right);
            Vector3 feet = centre - Vector3.up * (OrbRadius * .55f * size);
            _body?.Pose(feet, facing, GhostScale * size, fade, _age, .03f, .4f * _near + .6f * _shown + .5f * gone);
            if (_fx == null) return;
            _fx.Begin();
            _fx.Light(centre, .9f * size, (.25f + .25f * pulse * (.4f + _near) + .4f * _shown) * fade, _colour);
            _fx.Light(centre + Vector3.up * OrbRadius * .6f, .5f * size, .2f * fade, NemuSmokeBatch.Violet);
            _fx.End(_age);
        }

        /// <summary>Every dormant ghost on the court goes (the round ended).</summary>
        public static void RetireAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }
    }
}
