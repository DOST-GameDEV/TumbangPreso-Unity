using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE OVERCLOCK ZAP, drawn (owner, 2026-10-26: *"BOTH hands must visibly participate"*, *"not a one-hand Neon imitation"*).
    ///
    /// Zack's own pose is jumper cables. The LEAD hand (his left) is thrust out at the rival, palm open; the REAR hand (his right)
    /// is drawn back beside his chest with the fingers splayed, like the second clamp. Current visibly runs between the two
    /// hands first (a short, fast-restriking bridge arc), then leaves the lead palm as a jagged gold bolt with an ink edge
    /// that forks once near the rival. So the beam is his two hands closing a circuit, not a finger laser.
    ///
    /// * Searching (key held, nobody in the cone): the bridge arcs, and short sputters spit 1 m ahead of the lead palm.
    /// * Connected: the bolt holds on the rival's chest; a ring on the court around their feet tightens with the host's lock and
    ///   flashes when it Zaps them.
    /// * Grounded (Zapped by it in the last 3 s): the bolt goes thin and copper and the ring turns grey: it cannot Zap them yet.
    ///
    /// Every peer draws it from the kit's channel state (the host's word; the owner's own press on his screen). In his own first
    /// person the hands are the camera arms (`ViewmodelArms.TryZackHandTips`), and the bolt starts from them. Render only.
    /// </summary>
    [DefaultExecutionOrder(2600)]
    public sealed class ZackZapLook : MonoBehaviour
    {
        private const int BoltPoints = 9, RingPoints = 24;
        private CharacterMotor _motor;
        private ZackHeroKit _kit;
        private CharacterAnimator _animator;
        private LineRenderer _bridge, _bridgeB, _bolt, _boltInk, _fork, _ring, _sputter;
        private float _age, _pose, _flash;
        private int _lastTarget = -1;
        private bool _wasGrounded;
        private ViewmodelArms _arms;

        /// <summary>0 to 1 of the two-handed pose on the body this frame (diagnostics and capture traces).</summary>
        public float PoseAmount => _pose;
        public bool BoltDrawn => _bolt != null && _bolt.enabled;
        public bool BridgeDrawn => _bridge != null && _bridge.enabled;
        public Vector3 LeadHand { get; private set; }
        public Vector3 RearHand { get; private set; }

        public static ZackZapLook Attach(CharacterMotor motor, ZackHeroKit kit)
        {
            if (motor == null) return null;
            var go = new GameObject("ZackZap-P" + (motor.PlayerSlot + 1));
            var look = go.AddComponent<ZackZapLook>();
            look._motor = motor; look._kit = kit;
            look._animator = motor.GetComponentInChildren<CharacterAnimator>();
            look._bridge = ZackFx.Line(go.transform, "HandBridge", 6, 0.028f, ZackFx.Core, 1.8f);
            look._bridgeB = ZackFx.Line(go.transform, "HandBridgeB", 5, 0.018f, ZackFx.Body, 1.6f);
            look._boltInk = ZackFx.Line(go.transform, "ZapBoltInk", BoltPoints, 0.085f, ZackFx.Ink, 0.0f);
            look._bolt = ZackFx.Line(go.transform, "ZapBolt", BoltPoints, 0.045f, ZackFx.Body, 1.6f);
            look._fork = ZackFx.Line(go.transform, "ZapFork", 4, 0.022f, ZackFx.Core, 1.7f);
            look._sputter = ZackFx.Line(go.transform, "ZapSputter", 4, 0.02f, ZackFx.Core, 1.6f);
            look._ring = ZackFx.Line(go.transform, "ZapLockRing", RingPoints, 0.035f, ZackFx.Body, 1.4f);
            look._ring.loop = true;
            look.HideAll();
            return look;
        }

        public bool BelongsTo(CharacterMotor motor) => this != null && _motor == motor;

        private void HideAll()
        {
            _bridge.enabled = _bridgeB.enabled = _bolt.enabled = _boltInk.enabled = _fork.enabled = _ring.enabled = _sputter.enabled = false;
        }

        private void LateUpdate()
        {
            if (_motor == null || _kit == null || _motor.AbilitySystem?.Kit != _kit) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            _age += dt;
            bool zapping = _kit.Zapping && _motor.CanAct();
            _pose = Mathf.MoveTowards(_pose, zapping ? 1.0f : 0.0f, dt / (zapping ? 0.09f : 0.16f));
            if (_pose <= 0.0f) { HideAll(); _lastTarget = -1; return; }
            bool eyes = ZackFx.InOwnFirstPerson(_motor);
            var rival = _kit.ZapTarget >= 0 ? GameServices.Round?.BodyAt(_kit.ZapTarget) : null;
            var aimAt = rival != null ? rival.transform.position + Vector3.up * 0.9f
                : _motor.transform.position + Vector3.up * 1.0f + Flat(_motor.transform.forward) * 4.0f;
            if (!eyes) PoseBody(aimAt);
            if (!Hands(eyes, aimAt, out var lead, out var rear)) { HideAll(); return; }
            LeadHand = lead; RearHand = rear;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            int strike = reduced ? 0 : Mathf.FloorToInt(_age / 0.045f);
            float on = _pose;

            // His own view has two equal feeds into a point ahead of both palms, so neither hand reads as a prop holder.
            var emitter = eyes ? (lead + rear) * .5f + Camera.main.transform.forward * .12f : lead;
            // The circuit between his hands: always while the key is down.
            _bridge.enabled = true;
            ZackFx.Jag(_bridge, rear, eyes ? emitter : lead, 0.035f, strike * 1.3f);
            ZackFx.Tint(_bridge, Alpha(ZackFx.Core, on));
            _bridgeB.enabled = eyes || !reduced;
            if (_bridgeB.enabled) { ZackFx.Jag(_bridgeB, eyes ? lead : rear, eyes ? emitter : lead, reduced ? .02f : .05f, strike * 2.1f + 5); ZackFx.Tint(_bridgeB, Alpha(ZackFx.Body, on * (reduced || Mathf.Repeat(_age, .09f) < .05f ? 1 : .35f))); }

            bool grounded = _kit.ZapGrounded;
            if (rival != null && zapping)
            {
                if (_kit.ZapTarget != _lastTarget) ZackFx.Sparks(aimAt, .3f, reduced ? 1 : 3, _kit.ZapTarget * 7 + _age);
                // The moment the lock pays out (grounded turns on), a flash on the rival.
                if (grounded && !_wasGrounded && _lastTarget == _kit.ZapTarget) { _flash = 1.0f; ZackFx.Sparks(aimAt, .5f, reduced ? 2 : 5, _age); }
                _lastTarget = _kit.ZapTarget;
                var boltColour = grounded ? new Color(.55f, .47f, .38f) : ZackFx.Body;
                _bolt.enabled = _boltInk.enabled = true;
                _bolt.widthMultiplier = grounded ? 0.022f : 0.045f;
                _boltInk.widthMultiplier = grounded ? 0.05f : 0.085f;
                float amp = Mathf.Lerp(0.05f, 0.14f, Mathf.Clamp01(Vector3.Distance(lead, aimAt) / ZackRules.ZapRange));
                ZackFx.Jag(_bolt, emitter, aimAt, amp, strike * 1.7f);
                for (int i = 0; i < BoltPoints; i++) _boltInk.SetPosition(i, _bolt.GetPosition(i) + Vector3.down * 0.008f);
                ZackFx.Tint(_bolt, Alpha(boltColour, on * (grounded && Mathf.Repeat(_age, .12f) < .05f ? .3f : 1f)));
                ZackFx.Tint(_boltInk, Alpha(ZackFx.Ink, on * .8f));
                _fork.enabled = !reduced && !grounded;
                if (_fork.enabled)
                {
                    var knee = _bolt.GetPosition(BoltPoints - 3);
                    ZackFx.Jag(_fork, knee, aimAt + Vector3.up * .25f + Flat(Vector3.Cross(aimAt - lead, Vector3.up)).normalized * .2f, .05f, strike * 3.1f);
                    ZackFx.Tint(_fork, Alpha(ZackFx.Core, on * .9f));
                }
                _sputter.enabled = false;
                // The lock ring round the rival's feet: wide at first, tightening to their body as the host's lock builds.
                _ring.enabled = true;
                float lockNow = grounded ? 1.0f : _kit.ZapLock01;
                float radius = Mathf.Lerp(0.95f, 0.38f, lockNow);
                var feet = rival.transform.position + Vector3.up * 0.04f;
                for (int i = 0; i < RingPoints; i++)
                {
                    float a = i / (float)RingPoints * Mathf.PI * 2.0f;
                    float knee = reduced ? 0 : ((i % 2 == 0) ? 1 : -1) * .02f * (1 + lockNow);
                    _ring.SetPosition(i, feet + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (radius + knee));
                }
                _flash = Mathf.MoveTowards(_flash, 0.0f, dt / 0.25f);
                var ringColour = grounded ? new Color(.5f, .48f, .46f) : Color.Lerp(ZackFx.Body, ZackFx.Core, lockNow);
                ZackFx.Tint(_ring, Alpha(Color.Lerp(ringColour, Color.white * .95f, _flash * .4f), on));
            }
            else
            {
                _lastTarget = -1;
                _bolt.enabled = _boltInk.enabled = _fork.enabled = _ring.enabled = false;
                // Searching: short sputters spit from the lead palm toward the crosshair and die.
                _sputter.enabled = Mathf.Repeat(_age, 0.11f) < 0.06f;
                if (_sputter.enabled)
                {
                    var ahead = (aimAt - emitter).normalized;
                    ZackFx.Jag(_sputter, emitter, emitter + ahead * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(_age * 9))), 0.06f, strike * 2.3f);
                    ZackFx.Tint(_sputter, Alpha(ZackFx.Core, on));
                }
            }
            _wasGrounded = grounded;
        }

        private static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
        private static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward; }

        /// <summary>
        /// The two-handed stance on his third-person body, over whatever the gait or clip drew: the lead arm straight at the rival,
        /// the rear arm folded back at the chest, the chest turned a little side-on so both read in silhouette.
        /// </summary>
        private void PoseBody(Vector3 aimAt)
        {
            if (_animator == null) _animator = _motor.GetComponentInChildren<CharacterAnimator>();
            if (_animator == null || _animator.SwingArmLeft == null || _animator.SwingArmRight == null) return;
            var root = _motor.transform;
            Transform armA = _animator.SwingArmLeft, armB = _animator.SwingArmRight;
            Vector3 alongA = _animator.LeftArmAlong, alongB = _animator.RightArmAlong;
            // Sides by position: the importer mirrors X, so the bone named left may sit on his right.
            bool aIsLeft = Vector3.Dot(armA.position - root.position, root.right) < 0;
            Transform lead = aIsLeft ? armA : armB, rear = aIsLeft ? armB : armA;
            Vector3 leadAlong = aIsLeft ? alongA : alongB, rearAlong = aIsLeft ? alongB : alongA;
            var toTarget = (aimAt - lead.position).normalized;
            Aim(lead, leadAlong, toTarget, _pose);
            // The rear hand folds forward and in across the chest toward the lead side, a little down, so its fist sits beside
            // the sternum.
            var rearSide = Vector3.Dot(rear.position - root.position, root.right) >= 0 ? -1f : 1f;
            var rearDir = (Flat(toTarget) * 0.6f + root.right * 0.5f * rearSide + Vector3.down * 0.3f).normalized;
            Aim(rear, rearAlong, rearDir, _pose);
        }

        private static void Aim(Transform arm, Vector3 localAlong, Vector3 worldDirection, float weight)
        {
            var current = arm.rotation * localAlong;
            var turn = Quaternion.FromToRotation(current, worldDirection);
            arm.rotation = Quaternion.Slerp(Quaternion.identity, turn, Mathf.Clamp01(weight)) * arm.rotation;
        }

        /// <summary>Where his two hands are this frame: the camera arms in his own first person, else the body's arm tips.</summary>
        private bool Hands(bool eyes, Vector3 aimAt, out Vector3 lead, out Vector3 rear)
        {
            lead = rear = Vector3.zero;
            if (eyes)
            {
                if (_arms == null) _arms = Object.FindFirstObjectByType<ViewmodelArms>();
                return _arms != null && _arms.TryZackHandTips(out lead, out rear);
            }
            if (_animator == null || _animator.SwingArmLeft == null || _animator.SwingArmRight == null) return false;
            var root = _motor.transform;
            Transform armA = _animator.SwingArmLeft, armB = _animator.SwingArmRight;
            bool aIsLeft = Vector3.Dot(armA.position - root.position, root.right) < 0;
            Transform leadArm = aIsLeft ? armA : armB, rearArm = aIsLeft ? armB : armA;
            Vector3 leadAlong = aIsLeft ? _animator.LeftArmAlong : _animator.RightArmAlong;
            Vector3 rearAlong = aIsLeft ? _animator.RightArmAlong : _animator.LeftArmAlong;
            float reach = ArmReach(root);
            lead = leadArm.position + leadArm.rotation * leadAlong * reach;
            rear = rearArm.position + rearArm.rotation * rearAlong * reach;
            return true;
        }

        private float _reach = -1;
        private float ArmReach(Transform root)
        {
            if (_reach > 0) return _reach;
            var hand = _motor.GetComponent<CharacterVisual>()?.HandAnchor;
            _reach = hand != null && _animator.SwingArmRight != null
                ? Mathf.Clamp(Vector3.Distance(hand.position, _animator.SwingArmRight.position), 0.12f, 0.6f)
                : 0.22f * Mathf.Max(0.5f, root.lossyScale.y);
            return _reach;
        }
    }
}
