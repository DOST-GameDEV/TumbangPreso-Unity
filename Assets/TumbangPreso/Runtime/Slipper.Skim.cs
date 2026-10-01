using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class Slipper
    {
        public bool IsSkimming => _skimLeft > 0 && Affinity == SlipperAffinity.Skim;
        private float _skimLeft;
        private bool _skimStarted;
        private Vector3 _skimDirection;
        private float _skimGround;
        private static readonly RaycastHit[] SkimHits = new RaycastHit[32];

        private bool BeginSkim(float ground)
        {
            if (Affinity != SlipperAffinity.Skim || _skimStarted) return false;
            _skimStarted = true;
            var flat = new Vector3(_velocity.x, 0, _velocity.z);
            if (flat.sqrMagnitude < .001f) return false;
            _skimDirection = flat.normalized;
            _skimLeft = RafiRules.SkimDistance;
            _skimGround = ground;
            transform.position = new Vector3(transform.position.x, ground + RestHeight, transform.position.z);
            transform.rotation = Quaternion.identity;
            _velocity = _skimDirection * RafiRules.SkimSpeed;
            return true;
        }

        // Keep ordinary host body/can checks after this movement, preserving the
        // original throw chain. Only solid world geometry ends the skim here.
        private bool MoveSkim(float dt)
        {
            if (GameServices.Round?.RoundActive != true)
            { Land(false, _skimGround); return false; }
            float distance = Mathf.Min(_skimLeft, RafiRules.SkimSpeed * dt);
            var from = transform.position;
            var wanted = from + _skimDirection * distance;
            wanted.x = ClampToPlayableAxis(wanted.x, AIController.PlayableMinX, AIController.PlayableMaxX);
            wanted.z = ClampToPlayableAxis(wanted.z, AIController.PlayableMinZ, AIController.PlayableMaxZ);
            float allowed = distance;
            if (Mathf.Abs(_skimDirection.x) > .0001f) allowed = Mathf.Min(allowed, (wanted.x - from.x) / _skimDirection.x);
            if (Mathf.Abs(_skimDirection.z) > .0001f) allowed = Mathf.Min(allowed, (wanted.z - from.z) / _skimDirection.z);
            allowed = Mathf.Max(0, allowed);
            int count = Physics.SphereCastNonAlloc(from + Vector3.up * .11f, .10f,
                _skimDirection, SkimHits, allowed, ~0, QueryTriggerInteraction.Ignore);
            if (count == SkimHits.Length) allowed = 0;
            for (int i = 0; i < count; i++)
            {
                var hit = SkimHits[i];
                if (hit.collider.GetComponentInParent<Slipper>() != null
                    || hit.collider.GetComponentInParent<CharacterMotor>() != null
                    || hit.collider.GetComponentInParent<Lata>() != null) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - .025f));
            }
            var next = from + _skimDirection * allowed;
            count = Physics.RaycastNonAlloc(next + Vector3.up * .25f, Vector3.down,
                SkimHits, .6f, ~0, QueryTriggerInteraction.Ignore);
            float support = float.NegativeInfinity;
            if (count < SkimHits.Length)
                for (int i = 0; i < count; i++)
                {
                    var hit = SkimHits[i];
                    if (hit.normal.y < .7f || hit.collider.GetComponentInParent<Slipper>() != null
                        || hit.collider.GetComponentInParent<CharacterMotor>() != null
                        || hit.collider.GetComponentInParent<Lata>() != null) continue;
                    support = Mathf.Max(support, hit.point.y);
                }
            if (!float.IsFinite(support) || Mathf.Abs(support - _skimGround) > RafiRules.SkimStepHeight)
            { Land(true, _skimGround); return false; }
            _skimGround = support;
            transform.position = new Vector3(next.x, support + RestHeight, next.z);
            transform.rotation = Quaternion.identity;
            _skimLeft = allowed < distance - .001f ? 0 : Mathf.Max(0, _skimLeft - allowed);
            _velocity = _skimDirection * RafiRules.SkimSpeed;
            return true;
        }
    }
}
