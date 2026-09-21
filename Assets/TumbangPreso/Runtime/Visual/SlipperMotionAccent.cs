using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Public motion of the actual flying shoe. This is separate from the local
    // player's optional landed locator and from hero-specific charged effects.
    [DisallowMultipleComponent, DefaultExecutionOrder(1600)]
    public sealed class SlipperMotionAccent : MonoBehaviour
    {
        private Slipper _shoe;
        private TrailRenderer _trail;
        private Vector3 _lastPosition;
        private bool _flying;
        public bool Emitting => _trail != null && _trail.emitting;
        private void Awake() => _shoe = GetComponent<Slipper>();
        private void Build()
        {
            var go = new GameObject("SlipperMotionStroke"); go.transform.SetParent(transform, false);
            _trail = go.AddComponent<TrailRenderer>(); _trail.emitting = false;
            _trail.minVertexDistance = .06f; _trail.numCapVertices = 2;
            _trail.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(.35f, .65f), new Keyframe(1, 0));
            _trail.shadowCastingMode = ShadowCastingMode.Off; _trail.receiveShadows = false;
            var material = new Material(Shader.Find("Sprites/Default")) { name = "Slipper motion accent" };
            _trail.sharedMaterial = material; VfxRenderTag.Own(go, material);
        }
        public void ClearFlight()
        {
            _flying = false;
            if (_trail == null) return;
            _trail.emitting = false; _trail.Clear();
        }
        private void OnDisable() => ClearFlight();
        private void LateUpdate()
        {
            if (_shoe == null || _shoe.State != SlipperState.InFlight)
            { if (_flying) ClearFlight(); return; }
            if (_trail == null) Build();
            bool low = Settings.SettingsStore.Current.GraphicsQuality == 0;
            _trail.time = low ? .08f : .14f;
            _trail.widthMultiplier = low ? .032f : .05f;
            Color identity = _shoe.OwnerSlot >= 0 ? PlayerIdentity.Colour(_shoe.OwnerSlot) : new Color(.86f, .87f, .78f);
            Color head = Color.Lerp(identity, Color.white, .55f); head.a = _shoe.Affinity == SlipperAffinity.Normal ? .65f : .35f;
            _trail.startColor = head; _trail.endColor = new Color(identity.r, identity.g, identity.b, 0);
            // A network correction/recovery cannot draw a false route across the court.
            float plausibleStep = Mathf.Max(2, _shoe.Velocity.magnitude * Time.deltaTime * 3 + .5f);
            if (!_flying || Vector3.Distance(transform.position, _lastPosition) > plausibleStep) _trail.Clear();
            _lastPosition = transform.position; _flying = true; _trail.emitting = true;
        }
    }
}
