using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE TAYA RAISING THE CAN, SHOWN AS A RAISE WITH PROGRESS.
    ///
    /// 🧑 2026-09-24: *"refine other gameplay animations too for both FPP and TPP view for example the raising
    /// of can"*, *"analyze how it should look to communicate that that action is happening"*
    /// (`docs/reports/gameplay-animation-2026-09-24/research-and-analysis.md` § 2.3). Before this, the 1.5 s
    /// reset channel was the 0.33 s `pick-up` one-shot re-fired every `ViewmodelArms.GrabSeconds`: a body
    /// bobbing up and down beside a can that lay flat until it popped upright at the end. Nothing said how far
    /// along the reset was, which is the one thing an attacker deciding whether to run needs to see.
    ///
    /// Now, while a raise is in progress: bowed to the can at the waist (the rig's own `pick-up` vocabulary:
    /// the legs have no knees and the torso does the bending), both hands down on it, rising as the channel
    /// fills while the can itself tilts upright under them (`Lata`), then a short press down to plant it.
    ///
    /// ⚠️ PROGRESS ON EVERY SCREEN, WITH NO WIRE CHANGE. The taya's own peer knows the real
    /// `Carrier.ChannelRatio`. The others only receive the relayed `grab` gesture every 0.4 s
    /// (`MatchRpc.HostStepResetChannels`), so they start a local clock at the first one and keep it alive while
    /// they keep arriving. It is presentation only: the host alone decides when the can is upright, and the
    /// can snaps to its real state the moment that arrives. A protocol change was not worth it (`CLAUDE.md`
    /// § 4a, crossplay: both builds would have to ship together).
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        /// <summary>A relayed reach older than this means the channel has stopped.</summary>
        public const float ResetRelayGap = .6f;

        private float _raiseStart = -100, _raiseSeen = -100, _raiseWeight;
        private Transform _rrTorso, _rrHead, _rrArmR, _rrArmL;
        private Vector3 _rrAlongR = Vector3.right, _rrAlongL = Vector3.left;
        private bool _rrResolved, _rrApplied;
        private Quaternion _rrTorsoRest, _rrHeadRest, _rrArmRRest, _rrArmLRest;

        /// <summary>0 to 1: how far this body is through raising the can, 0 when it is not raising.</summary>
        public float ResetRaiseProgress { get; private set; }

        /// <summary>Called from `PlayAction("grab")`: on a taya beside a downed can, a reach is a raise.</summary>
        private void NoteResetGesture()
        {
            if (_motor == null || !_motor.IsDefender) return;
            var lata = GameServices.Round?.Lata;
            if (lata == null || lata.IsUpright) return;
            Vector3 a = _motor.transform.position, b = lata.transform.position; a.y = b.y = 0;
            if (Vector3.Distance(a, b) > Core.Balance.InteractionRadius + .5f) return;
            if (Time.time - _raiseSeen > ResetRelayGap) _raiseStart = Time.time;
            _raiseSeen = Time.time;
        }

        private float ResetProgressNow()
        {
            if (_motor == null || !_motor.IsDefender) return 0;
            var lata = GameServices.Round?.Lata;
            if (lata == null || lata.IsUpright) { _raiseSeen = -100; return 0; }
            if (_carrier != null && _carrier.ChannelRatio > 0) return _carrier.ChannelRatio;
            if (Time.time - _raiseSeen > ResetRelayGap) return 0;
            return Mathf.Clamp01((Time.time - _raiseStart) / lata.ResetChannelTime);
        }

        private void RestoreResetRaise()
        {
            if (!_rrApplied) return;
            _rrTorso.localRotation = _rrTorsoRest; _rrArmR.localRotation = _rrArmRRest; _rrArmL.localRotation = _rrArmLRest;
            if (_rrHead != null) _rrHead.localRotation = _rrHeadRest;
            _rrApplied = false;
        }

        private void ClearResetRaise() { RestoreResetRaise(); _rrTorso = _rrHead = _rrArmR = _rrArmL = null; _rrResolved = false; _raiseWeight = 0; ResetRaiseProgress = 0; }

        private void ApplyResetRaise()
        {
            if (_motor == null || _animator == null) return;
            float progress = ResetProgressNow();
            ResetRaiseProgress = progress;
            _raiseWeight = Mathf.MoveTowards(_raiseWeight, progress > 0 ? 1 : 0, Time.deltaTime / (progress > 0 ? .12f : .15f));
            if (_raiseWeight <= .001f) return;
            if (!_rrResolved)
            {
                foreach (var skin in _animator.GetComponentsInChildren<SkinnedMeshRenderer>(false))
                {
                    if (!skin.enabled || skin.bones == null) continue;
                    var binds = skin.sharedMesh != null ? skin.sharedMesh.bindposes : null;
                    for (int i = 0; i < skin.bones.Length; i++)
                    {
                        var b = skin.bones[i];
                        if (b == null) continue;
                        if (b.name == "torso" && _rrTorso == null) _rrTorso = b;
                        else if (b.name == "head" && _rrHead == null) _rrHead = b;
                        else if (b.name == "arm-right" && _rrArmR == null) { _rrArmR = b; _rrAlongR = AlongArm(binds, i, _rrAlongR); }
                        else if (b.name == "arm-left" && _rrArmL == null) { _rrArmL = b; _rrAlongL = AlongArm(binds, i, _rrAlongL); }
                    }
                }
                _rrResolved = true;
            }
            if (_rrTorso == null || _rrArmR == null || _rrArmL == null) return;

            // The shape of the raise against progress: bow in, rise with the can, then the press.
            float p = progress > 0 ? progress : 1;
            float rise = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .85f, p));
            float press = p > .85f ? Mathf.Sin(Mathf.InverseLerp(.85f, 1f, p) * Mathf.PI) : 0;
            float bow = Mathf.Lerp(50, 28, rise) + 10 * press;
            // Hands on the can: low and forward, lifting as it comes up, pushed back down in the press.
            float handY = Mathf.Lerp(-.78f, -.38f, rise) - .25f * press;
            var hand = new Vector3(.12f, handY, Mathf.Lerp(.62f, .9f, rise));

            float w = _raiseWeight;
            _rrTorsoRest = _rrTorso.localRotation; _rrArmRRest = _rrArmR.localRotation; _rrArmLRest = _rrArmL.localRotation;
            if (_rrHead != null) _rrHeadRest = _rrHead.localRotation;
            _rrApplied = true;
            _rrTorso.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.AngleAxis(bow, transform.right), w) * _rrTorso.rotation;
            // The head looks at the can rather than following the chest all the way down.
            if (_rrHead != null) _rrHead.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.AngleAxis(-bow * .35f, transform.right), w) * _rrHead.rotation;
            float sideR = transform.InverseTransformPoint(_rrArmR.position).x >= 0 ? 1f : -1f;
            PointArm(_rrArmR, _rrAlongR, new Vector3(hand.x * sideR, hand.y, hand.z), w, 0);
            PointArm(_rrArmL, _rrAlongL, new Vector3(-hand.x * sideR, hand.y, hand.z), w, 0);
        }
    }
}
