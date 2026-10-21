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
    ///  * It sits where its ghost ended, slumped low and greyed, breathing slowly. Peeker's is left where it let go of its
    ///    victim, or on its spot when nobody came; Fetcher's and Keeper's where they last stood.
    ///  * While it is here, that skill cannot be cast and its cooldown is held full (`NemuHeroKit`). Nemu reclaims it by
    ///    holding Grab, the RETRIEVE SLIPPER / RESET CAN button, within `NemuRules.ReclaimRadius` of it for
    ///    `NemuRules.ReclaimSeconds`; then it rises into her and the 20 s cooldown starts.
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

        /// <summary>The name the reclaim prompt reads: "Retrieve Peeker", "Retrieve Fetcher", "Retrieve Keeper".</summary>
        public static string NameOf(Ghost kind) => kind == Ghost.Peeker ? "Peeker" : kind == Ghost.Fetcher ? "Fetcher" : "Keeper";
        public string Name => NameOf(Kind);

        private const float SettleSeconds = .5f, GoneSeconds = .45f, Scale = .8f, Seen = .55f;
        private static readonly Color Rim = new Color(.55f, .45f, .75f, 1f);
        private NemuGhostBody _body;
        private NemuSmokeBatch _fx;
        private float _age, _goneAt, _shown;
        private bool _reclaimed;

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
            dormant._body = new NemuGhostBody(go.transform, "DormantBody", mesh, Rim);
            dormant._fx = new NemuSmokeBatch(go.transform, "DormantFx", 2, 4, 1);
            return dormant;
        }

        /// <summary>Nemu's dormant ghost of this kind still waiting to be reclaimed, or null.</summary>
        public static NemuDormant For(int ownerSlot, Ghost kind)
        {
            foreach (var d in Live) if (d != null && !d._reclaimed && d.OwnerSlot == ownerSlot && d.Kind == kind) return d;
            return null;
        }

        /// <summary>The nearest of her unreclaimed dormant ghosts within reclaim reach of <paramref name="at"/>, flat.</summary>
        public static NemuDormant InReach(int ownerSlot, Vector3 at)
        {
            NemuDormant best = null;
            float bestDistance = NemuRules.ReclaimRadius;
            foreach (var d in Live)
            {
                if (d == null || d._reclaimed || d.OwnerSlot != ownerSlot) continue;
                float distance = new Vector2(d.Position.x - at.x, d.Position.z - at.z).magnitude;
                if (distance <= bestDistance) { bestDistance = distance; best = d; }
            }
            return best;
        }

        /// <summary>Taken back: it rises into her and is gone.</summary>
        public void Reclaim()
        {
            if (_reclaimed) return;
            _reclaimed = true; _goneAt = _age; Progress = 1f;
            GameServices.Audio?.PlayAt("sfx_possess_enter", Position);
        }

        private void OnEnable() { Live.Add(this); NemuGhostRings.Ensure(); }
        private void OnDisable() => Live.Remove(this);
        private void OnDestroy() { _fx?.Dispose(); _fx = null; }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            float gone = _reclaimed ? Mathf.Clamp01((_age - _goneAt) / GoneSeconds) : 0f;
            if (gone >= 1f) { Destroy(gameObject); return; }
            _shown = Mathf.MoveTowards(_shown, Mathf.Clamp01(Mathf.Max(Progress, LocalProgress)), dt * 4f);
            float settle = Mathf.SmoothStep(0f, 1f, _age / SettleSeconds);
            // Slumped low on the court and breathing slowly; as she reclaims it, it lifts and brightens toward her.
            float bob = .03f * Mathf.Sin(_age * 1.3f);
            Vector3 at = Position + Vector3.up * (.02f + bob + .35f * _shown + 1.2f * gone * gone);
            var facing = Quaternion.LookRotation(Facing) * Quaternion.AngleAxis(18f * (1f - _shown), Vector3.right);
            float fade = settle * Mathf.Lerp(Seen, 1f, _shown) * (1f - gone * gone);
            float squash = .06f * Mathf.Sin(_age * 1.3f);
            var scale = new Vector3(1f + squash * .5f, 1f - squash, 1f + squash * .5f) * Scale * Mathf.Lerp(1f, .4f, gone);
            _body?.Pose(at, facing, scale, fade, _age, .03f, .6f * _shown + .5f * gone);
            if (_fx == null) return;
            _fx.Begin();
            _fx.Light(at + Vector3.up * .3f, .7f, (.18f + .4f * _shown) * fade, NemuSmokeBatch.Violet);
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
