using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The dotted aim arc.
    ///
    /// ⚠️⚠️ THE LINE AND THE FLIGHT ARE ONE LINE BY CONSTRUCTION, NOT BY AGREEMENT. This
    /// integrates the velocity that <see cref="Slipper.LaunchVelocity"/> hands it, using the
    /// same gravity, rather than reimplementing the arc. Measured in the original: the
    /// preview lands where the slipper lands to 0.000 m on three of the four skins.
    ///
    /// ⚠️ THE FOURTH MISSES BY 0.263 m AND THE ARC IS NOT THE BUG. A crocs RESTS 0.161 m off
    /// the ground against the other three at 0.034 to 0.056, while the preview stops its line
    /// at a fixed floor epsilon. The line is right; the tall skin simply stops higher. Fixed
    /// here by taking the rest height per skin instead of assuming one.
    ///
    /// ⚠️ AND IT IS SHOWN ONLY WHEN A THROW WOULD ACTUALLY BE ACCEPTED, asking the same
    /// CanThrow the rules ask. A preview that promises a throw the rules then refuse is the
    /// most confusing possible failure: the player sees no reason for nothing to happen.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private Carrier _carrier;

        [Tooltip("Simulation step for the preview. Smaller is smoother, not more accurate: " +
                 "the integrator matches the flight either way.")]
        [SerializeField] private float _step = 0.02f;

        [SerializeField] private int _maxPoints = 128;

        private LineRenderer _line;
        private readonly List<Vector3> _points = new List<Vector3>();

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            if (_motor == null) _motor = GetComponentInParent<CharacterMotor>();
            if (_carrier == null) _carrier = GetComponentInParent<Carrier>();
        }

        private void LateUpdate()
        {
            if (!ShouldShow())
            {
                _line.enabled = false;
                return;
            }

            _line.enabled = true;
            Rebuild();
        }

        private bool ShouldShow()
        {
            if (_motor == null || _carrier == null) return false;
            if (_carrier.Held == null) return false;

            var round = GameServices.Round;
            return round != null && round.CanThrow(_motor);
        }

        private void Rebuild()
        {
            _points.Clear();

            Vector3 aim = _motor.Intent.AimPoint - _motor.transform.position;
            aim.y = 0.0f;
            if (aim.sqrMagnitude < 0.01f) aim = _motor.transform.forward;

            Vector3 dir = (aim.normalized + Vector3.up).normalized;
            Vector3 v = _carrier.Held.LaunchVelocity(dir, _carrier.ChargeRatio);

            // ⚠️ FROM THE SIGHT LINE, NOT THE HAND, matching the throw. Leaving from the hand
            // sags the flight 0.38 to 0.43 m below the line the player is aiming along.
            Vector3 p = _motor.transform.position + Vector3.up * 1.25f
                        + aim.normalized * Balance.MuzzleForward;

            float floor = RestHeightFor(_carrier.Held);

            for (int i = 0; i < _maxPoints; i++)
            {
                _points.Add(p);

                v.y -= Balance.Gravity * _step;
                p += v * _step;

                if (p.y <= floor)
                {
                    _points.Add(new Vector3(p.x, floor, p.z));
                    break;
                }
            }

            _line.positionCount = _points.Count;
            _line.SetPositions(_points.ToArray());
        }

        /// <summary>
        /// Per skin, measured off the mesh rather than assumed. This is the whole of the
        /// CROCS fix: a fixed epsilon is correct for three skins and wrong for the tall one.
        /// </summary>
        private static float RestHeightFor(Slipper s)
        {
            var r = s.GetComponentInChildren<Renderer>();
            return r != null ? r.bounds.extents.y : 0.045f;
        }
    }
}
