using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ SKITTER, THE GHOST NEMU SENDS FOR HER SLIPPER (owner 2026-10-15: *"Spawn siya ghost na kukunin slipper for her"*;
    /// the look from the approved lineup, `Visual.NemuGhostModels.Skitter`). Kuro is no longer sent: each skill has its own
    /// ghost, Gekko's pets the model. It pops out beside her, scrambles to the slipper, grabs it and hurries it back.
    ///
    /// Only the ability moves it (<see cref="Step"/>, from `NemuHeroKit.Skitter.OnTick`), so every peer's copy runs the same
    /// path from the accepted cast; only the host moves the slipper (the ability's gate). This draws it and, at the end,
    /// lets it go (<see cref="Leave"/>: up and gone).
    /// </summary>
    public sealed class NemuSkitter : MonoBehaviour
    {
        public static readonly List<NemuSkitter> Live = new List<NemuSkitter>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLive() => Live.Clear();

        public int OwnerSlot { get; private set; } = -1;
        public Vector3 Position { get; private set; }
        /// <summary>Within reach of the last place it was sent.</summary>
        public bool Arrived { get; private set; }
        /// <summary>Where the slipper hangs while it carries (between its hands).</summary>
        public Vector3 HoldPoint => Position + _facing * NemuGhostModels.SkitterHands;
        public bool Leaving => _leaving;

        private const float PopSeconds = .25f, LeaveSeconds = .4f, Reach = .3f, Scale = 1f;
        private NemuGhostBody _body;
        private Quaternion _facing = Quaternion.identity;
        private float _age, _leftAt = -1f;
        private bool _leaving;
        private Vector3 _velocity;

        /// <summary>Pops a Skitter out beside Nemu (one per Nemu: her last one is let go).</summary>
        public static NemuSkitter Summon(Vector3 at, Vector3 facing, int ownerSlot)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null && Live[i].OwnerSlot == ownerSlot) Live[i].Leave();
            var go = new GameObject("NemuSkitter");
            var skitter = go.AddComponent<NemuSkitter>();
            skitter.OwnerSlot = ownerSlot;
            skitter.Position = at;
            facing.y = 0f;
            skitter._facing = Quaternion.LookRotation(facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector3.forward);
            skitter._body = new NemuGhostBody(go.transform, "SkitterBody", NemuGhostModels.Skitter, new Color(.55f, 1f, .5f, 1f));
            return skitter;
        }

        public static NemuSkitter OwnedBy(int ownerSlot)
        {
            foreach (var s in Live) if (s != null && s.OwnerSlot == ownerSlot && !s.Leaving) return s;
            return null;
        }

        /// <summary>Tests put it straight somewhere (as they used to put Kuro, `GhostPetCompanion.ApplyCastAnchor`).</summary>
        public void Teleport(Vector3 at) { Position = at; _velocity = Vector3.zero; }

        /// <summary>Tests put its hands straight on <paramref name="hold"/>.</summary>
        public void TeleportHands(Vector3 hold) => Teleport(hold - (HoldPoint - Position));

        /// <summary>One step toward <paramref name="goal"/> at <paramref name="speed"/> m/s, turning to face where it goes.</summary>
        public void Step(Vector3 goal, float speed, float dt)
        {
            if (_leaving) return;
            Vector3 to = goal - Position;
            float d = to.magnitude;
            if (d > 1e-4f && dt > 0f)
            {
                Vector3 move = to * Mathf.Min(1f, speed * dt / d);
                _velocity = move / dt;
                Position += move;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                if (flat.sqrMagnitude > 1e-4f)
                    _facing = Quaternion.Slerp(_facing, Quaternion.LookRotation(flat.normalized), Mathf.Clamp01(dt * 12f));
            }
            Arrived = (goal - Position).sqrMagnitude <= Reach * Reach;
        }

        /// <summary>Done (delivered, dropped, cancelled): it zips up and fades.</summary>
        public void Leave()
        {
            if (_leaving) return;
            _leaving = true; _leftAt = _age;
        }

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);

        private void Update()
        {
            _age += Time.deltaTime;
            float pop = Mathf.SmoothStep(0f, 1f, _age / PopSeconds);
            float fade = 1f, rise = 0f;
            if (_leaving)
            {
                float u = Mathf.Clamp01((_age - _leftAt) / LeaveSeconds);
                fade = 1f - u; rise = 1.2f * u * u;
                if (u >= 1f) { Destroy(gameObject); return; }
            }
            // A scurrying bob, quicker while it moves; it leans into where it is going.
            float speed = _velocity.magnitude;
            float bob = .04f * Mathf.Sin(_age * (6f + speed));
            Quaternion lean = Quaternion.AngleAxis(Mathf.Clamp(speed * 2.5f, 0f, 18f), Vector3.right);
            _body?.Pose(Position + Vector3.up * (bob + rise), _facing * lean, Scale * pop, fade, _age, .08f + speed * .01f);
        }

        /// <summary>Every Skitter on the court goes (the round ended).</summary>
        public static void RetireAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }
    }
}
