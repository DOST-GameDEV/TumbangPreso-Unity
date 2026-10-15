using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ LOOKOUT, THE LITTLE GHOST NEMU LEAVES MINDING THE CAN (owner 2026-10-15: *"spawn siya ghost na tutulungan siya
    /// magdefend (pede ren magtayo lata) but medyo small and not that good but it helps in some cases"*, *"only stays for like
    /// 15 seconds"*; the look from the approved lineup, `Visual.NemuGhostModels.Lookout`).
    ///
    /// The rule lives in `NemuHeroKit.Lookout` (the 15 s clock, the slow stand-up, the scare); this is only the ghost:
    ///  * MINDING: it floats beside the can, holding its lantern up, turning to watch the nearest attacker.
    ///  * LIFTING (<see cref="Lift"/> 0 to 1): the can is down and it is straining to stand it back up, over it, its lantern
    ///    brightening as it gets there.
    ///  * SCARED: an attacker is right on it; it shrinks back behind the can and its lantern gutters.
    ///  * LEAVING: its time is up (or its job done); it sinks away.
    /// </summary>
    public sealed class NemuLookout : MonoBehaviour
    {
        public static readonly List<NemuLookout> Live = new List<NemuLookout>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLive() => Live.Clear();

        public int OwnerSlot { get; private set; } = -1;
        public bool Leaving => _leaving;
        /// <summary>How far through standing the can up it is (0 when it is not trying).</summary>
        public float Lift { get; set; }
        public bool Scared { get; set; }
        /// <summary>The attacker it is watching, if any.</summary>
        public Vector3? Watching { get; set; }

        private const float PopSeconds = .3f, LeaveSeconds = .5f, Scale = .85f;
        private NemuGhostBody _body;
        private NemuSmokeBatch _light;
        private Transform _can;
        private Vector3 _canAt, _side = Vector3.right;
        private Quaternion _facing = Quaternion.identity;
        private float _age, _leftAt, _scare, _lift;
        private bool _leaving;

        /// <summary>Puts Nemu's Lookout by the can (one per Nemu: her last one goes).</summary>
        public static NemuLookout Summon(Transform can, Vector3 fromOwner, int ownerSlot)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null && Live[i].OwnerSlot == ownerSlot) Live[i].Leave();
            var go = new GameObject("NemuLookout");
            var lookout = go.AddComponent<NemuLookout>();
            lookout.OwnerSlot = ownerSlot;
            lookout._can = can;
            lookout._canAt = can != null ? can.position : fromOwner;
            // It floats on the side of the can nearest her.
            Vector3 side = fromOwner - lookout._canAt; side.y = 0f;
            lookout._side = side.sqrMagnitude > 1e-4f ? side.normalized : Vector3.right;
            lookout._facing = Quaternion.LookRotation(-lookout._side);
            lookout._body = new NemuGhostBody(go.transform, "LookoutBody", NemuGhostModels.Lookout, new Color(1f, .8f, .5f, 1f));
            lookout._light = new NemuSmokeBatch(go.transform, "LookoutLight", 1, 2, 1);
            return lookout;
        }

        public static NemuLookout OwnedBy(int ownerSlot)
        {
            foreach (var l in Live) if (l != null && l.OwnerSlot == ownerSlot && !l.Leaving) return l;
            return null;
        }

        public void Leave()
        {
            if (_leaving) return;
            _leaving = true; _leftAt = _age;
        }

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);
        private void OnDestroy() { _light?.Dispose(); _light = null; }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_can != null) _canAt = _can.position;
            _scare = Mathf.MoveTowards(_scare, Scared ? 1f : 0f, dt * 5f);
            _lift = Mathf.MoveTowards(_lift, Lift, dt * 4f);
            float pop = Mathf.SmoothStep(0f, 1f, _age / PopSeconds);
            float fade = 1f, sink = 0f;
            if (_leaving)
            {
                float u = Mathf.Clamp01((_age - _leftAt) / LeaveSeconds);
                fade = 1f - u; sink = .6f * u * u;
                if (u >= 1f) { Destroy(gameObject); return; }
            }
            // Minding: beside the can, bobbing. Lifting: over it, straining up and down. Scared: back behind it, small.
            Vector3 mind = _canAt + _side * .7f + Vector3.up * (.15f + .05f * Mathf.Sin(_age * 2.4f));
            Vector3 over = _canAt + _side * .25f + Vector3.up * (.35f + .08f * Mathf.Abs(Mathf.Sin(_age * 9f)) * _lift);
            Vector3 hide = _canAt - _side * .55f + Vector3.up * .05f;
            Vector3 at = Vector3.Lerp(Vector3.Lerp(mind, over, Mathf.Clamp01(_lift * 3f)), hide, _scare) - Vector3.up * sink;
            Vector3 look = Watching.HasValue ? Watching.Value - at : -_side;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-4f)
                _facing = Quaternion.Slerp(_facing, Quaternion.LookRotation(look.normalized), Mathf.Clamp01(dt * 6f));
            float scale = Scale * pop * Mathf.Lerp(1f, .75f, _scare);
            _body?.Pose(at, _facing, scale, fade, _age, .05f);

            if (_light == null) return;
            // Its lantern: warm, brighter as it lifts, guttering when it is scared.
            float flicker = .85f + .15f * Mathf.Sin(_age * 17f) * Mathf.Sin(_age * 5f + 1f);
            float strength = (.55f + _lift * .9f) * Mathf.Lerp(1f, .3f, _scare) * flicker * fade;
            _light.Begin();
            if (fade > .005f) _light.Light(at + _facing * (NemuGhostModels.LookoutLantern * scale), .5f + _lift * .3f, strength, NemuGhostModels.LookoutLight);
            _light.End(_age);
        }

        /// <summary>Every Lookout on the court goes (the round ended).</summary>
        public static void RetireAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }
    }
}
