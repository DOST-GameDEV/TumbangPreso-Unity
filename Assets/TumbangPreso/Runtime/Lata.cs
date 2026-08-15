using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The can. It stands on its mark, it goes over, and the taya stands it back up.
    ///
    /// ⚠️⚠️ IsUpright GATES FOUR SEPARATE RULES: the throw, the tag, passive scoring and the
    /// reset channel. It is host-authoritative, and in the Godot build it is replicated by an
    /// EXPLICIT RPC rather than a synchronised property. That was not a style choice: a
    /// `MultiplayerSynchronizer` writes the property directly, so the setter's signal never
    /// fires on the peer that RECEIVED it. One setter, three symptoms, and it cost a whole
    /// session.
    ///
    /// ⚠️ THE SAME TRAP EXISTS IN UNITY. A `NetworkVariable` hook fires on change, but a
    /// value written server-side and read client-side in the same frame can be observed
    /// before the hook runs. When Phase 5 arrives, replicate this with an explicit Rpc and
    /// raise <see cref="UprightChanged"/> from one place, exactly as the original does.
    /// </summary>
    public sealed class Lata : MonoBehaviour
    {
        public event Action<bool> UprightChanged;

        [SerializeField] private int _skinIndex = -1;

        private bool _isUpright = true;
        private float _toppleTimer;
        private Vector3 _mark;

        public int SkinIndex { get => _skinIndex; set => _skinIndex = value; }
        public bool IsUpright => _isUpright;

        /// <summary>The scoring window for the CURRENT can skin, divided by its STANCE.</summary>
        public float HitWindow => ThrowRules.HitWindow(_skinIndex);

        /// <summary>How long the taya's reset channel takes on this can.</summary>
        public float ResetChannelTime => Combat.ResetChannelFor(_skinIndex);

        private void Awake() => _mark = transform.position;

        /// <summary>
        /// Host-side. Did a slipper at this position connect?
        ///
        /// ⚠️ FLAT DISTANCE, TESTED PER PHYSICS FRAME, AND NOT AN OVERLAP VOLUME. The Godot
        /// `Lata.tscn` still carries an `Area3D` authored to a hurtbox shape that nothing
        /// ever read: the rule ran off a bare literal in another file while the balance doc
        /// documented a third shape. Three numbers that were meant to be one. Here there is
        /// exactly one, and it is <see cref="Balance.LataHitMargin"/>.
        /// </summary>
        public bool Connects(Vector3 slipperPosition)
        {
            Vector3 a = new Vector3(slipperPosition.x, 0.0f, slipperPosition.z);
            Vector3 b = new Vector3(transform.position.x, 0.0f, transform.position.z);
            return ThrowRules.Connects(Vector3.Distance(a, b), _skinIndex);
        }

        /// <summary>Host-side. Knock it over and pay the thrower.</summary>
        public void HostKnockDown(int throwerSlot)
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (!_isUpright) return;

            SetUpright(false);
            _toppleTimer = Balance.ToppleTime;

            if (throwerSlot >= 0)
                GameServices.Match.AddScore(throwerSlot, ScoreEvent.LataKnocked);
        }

        /// <summary>
        /// Host-side. The end of a completed reset channel.
        ///
        /// ⚠️ IT GOES BACK ON ITS MARK AND *THEN* STANDS UP, IN THAT ORDER. A lata that
        /// stands up where it was knocked to is a lata the next throw cannot miss, and the
        /// taya would have spent the channel making the attackers' next shot easier.
        /// </summary>
        public void HostRestore()
        {
            transform.position = _mark;
            transform.rotation = Quaternion.identity;
            SetUpright(true);

            GameServices.Round.NotifyLataRestored();
        }

        private void SetUpright(bool value)
        {
            if (_isUpright == value) return;
            _isUpright = value;
            UprightChanged?.Invoke(value);
        }

        /// <summary>
        /// ⚠️ A TOPPLED CAN IS LIFTED BY ITS OWN RADIUS, AND THAT IS LOAD-BEARING. The tilt
        /// rotates the visual about its BASE, so a lying-down cylinder's axis would sit at
        /// floor level and half the can would be underground. That was reported from play as
        /// "the cans are phasing thru the floor". Measure the lift off the mesh bounds so it
        /// follows the skin rather than assuming a radius, because the four cans span 0.108
        /// to 0.143 and the default is the slimmest of them.
        /// </summary>
        private void Update()
        {
            if (_isUpright || _toppleTimer <= 0.0f) return;

            _toppleTimer = Mathf.Max(0.0f, _toppleTimer - Time.deltaTime);
            float t = 1.0f - (_toppleTimer / Balance.ToppleTime);

            float radius = 0.14f;
            var mesh = GetComponentInChildren<Renderer>();
            if (mesh != null) radius = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z);

            transform.rotation = Quaternion.Euler(Mathf.Lerp(0.0f, Balance.DownedTiltDeg, t), 0.0f, 0.0f);
            transform.position = new Vector3(_mark.x, _mark.y + radius * t, _mark.z);
        }
    }
}
