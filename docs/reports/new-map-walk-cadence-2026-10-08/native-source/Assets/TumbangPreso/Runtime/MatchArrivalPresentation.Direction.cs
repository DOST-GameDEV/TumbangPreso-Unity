using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class MatchArrivalPresentation
    {
        private enum Direction { Bridge = 2, Cove = 4, Crossing = 5 }
        private readonly struct OpeningDirection
        {
            public readonly Direction Kind;
            public readonly float Arrival, Portrait, Spotlight, Settle, End;
            public OpeningDirection(Direction kind, float arrival, float portrait, float spotlight)
            {
                Kind = kind; Arrival = arrival; Portrait = portrait;
                Spotlight = arrival + 4 * portrait;
                Settle = Spotlight + spotlight;
                End = Settle + 2.4f;
            }
            public float Handoff => End - 1.4f;
        }
        private bool _directed;
        private OpeningDirection _direction;
        private readonly Transform[] _walkRoots = new Transform[4];
        private readonly Vector3[] _walkRest = new Vector3[4], _walkMarks = new Vector3[4];
        private readonly Quaternion[] _walkTurns = new Quaternion[4];
        private readonly Vector3[][] _walkPath = new Vector3[4][];
        private readonly float[] _walkLengths = new float[4];
        private readonly Vector3[] _walkPreviousAt=new Vector3[4];
        private readonly bool[] _walkHadSample=new bool[4];
        private readonly int[] _portraitOrder = new int[4];
        private float _directionLastAge;
        private float _directionDrawAge;
        private bool _directionDrawReduced;
        private DirectedMapArrivalStage _directionStage;

        private void DrawDirection()
        {
            // Visual smoothing writes the ordinary root in Update. Keep this
            // held presentation after the animator and before Carrier's hand read.
            if (_directed && _camera != null) StageWalk(_directionDrawAge, _directionDrawReduced, false);
        }

        private void PrepareDirection(SceneFlow.MapEntry map, bool reduced)
        {
            _directed = true;
            if (map.Id == SceneFlow.IlalimNgTulay) _direction = new OpeningDirection(Direction.Bridge, 3.8f, .9f, 1.9f);
            else if (map.Id == SceneFlow.LagoonCove) _direction = new OpeningDirection(Direction.Cove, 4.8f, .95f, 2f);
            else if (map.Id == SceneFlow.Kanto) _direction = new OpeningDirection(Direction.Crossing, 3.4f, .85f, 1.8f);
            else { _directed = false; return; }
            _directionStage = gameObject.AddComponent<DirectedMapArrivalStage>();
            _directionStage.Draw = DrawDirection;
            _directionLastAge = 0;
            int defender = Core.MatchRules.DefenderSlotFor(1);
            // The taya closes the cast introductions and gets an uninterrupted beat.
            for (int i = 0; i < 4; i++) _portraitOrder[i] = (defender + 1 + i) % 4;
            for (int i = 0; i < 4; i++)
            {
                var body = _players[i];
                var root = body != null ? body.GetComponent<CharacterVisual>()?.ModelRoot : null;
                if (root == null || root == body.transform) continue;
                _walkRoots[i] = root; _walkRest[i] = root.localPosition; _walkTurns[i] = root.localRotation;
                _walkMarks[i] = body.transform.position;
                _poses[i]?.SetArrivalGesture((int)_direction.Kind);
                if (reduced || !Settings.SettingsStore.Current.CinematicCameraMotion) continue;
                _walkPath[i] = SupportedPath(i, _walkMarks[i]);
                _walkLengths[i] = 0;
                for (int k = 1; k < _walkPath[i].Length; k++)
                    _walkLengths[i] += Vector3.Distance(_walkPath[i][k - 1], _walkPath[i][k]);
            }
        }

        private Vector3 EntryOffset(int seat)
        {
            // Outer attackers approach from outside their own lane. Alternating
            // by parity made the left and centre players exchange places and
            // pass through each other before reaching their real marks.
            float side = seat == 0 || seat == 1 ? -1 : seat == 3 ? 1 : 0;
            switch (_direction.Kind)
            {
                case Direction.Bridge: return new Vector3(side * .6f, 0, seat == 0 ? -3.2f : 3.2f);
                case Direction.Cove: return new Vector3(side * 1.8f, 0, seat == 0 ? -2.5f : 2.5f);
                default: return new Vector3(side * 3.1f, 0, seat == 0 ? -1f : 1f);
            }
        }

        private Vector3[] SupportedPath(int seat, Vector3 mark)
        {
            // Nearby actual support is authoritative. Shorten an obstructed entrance
            // rather than walk a render copy through a wall, roof edge or reef.
            Vector3 offset = EntryOffset(seat);
            for (float scale = 1; scale >= .24f; scale *= .7f)
            {
                var points = new Vector3[13]; bool valid = true;
                float side = seat == 0 || seat == 1 ? -1 : seat == 3 ? 1 : 0;
                Vector3 bend = _direction.Kind == Direction.Cove ? Vector3.right * side * .65f * scale : Vector3.zero;
                for (int k = 0; k < points.Length; k++)
                {
                    float p = (float)k / (points.Length - 1);
                    Vector3 at = mark + offset * scale * (1 - p) + bend * Mathf.Sin(p * Mathf.PI);
                    if (!SupportAt(at, mark.y, out float floor)) { valid = false; break; }
                    at.y += floor - SupportHeight(mark);
                    points[k] = at;
                    if (k > 0 && !WalkSegmentClear(points[k - 1], points[k])) { valid = false; break; }
                }
                if (valid) return points;
            }
            return new[] { mark, mark };
        }

        private float SupportHeight(Vector3 at) => SupportAt(at, at.y, out float floor) ? floor : at.y;
        private bool SupportAt(Vector3 at, float referenceY, out float floor)
        {
            floor = referenceY;
            int count = Physics.RaycastNonAlloc(at + Vector3.up * 1.25f, Vector3.down, _hits, 2.5f,
                ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (!OpeningWorldCollider(hit.collider) || hit.normal.y < .65f) continue;
                float difference = Mathf.Abs(hit.point.y - referenceY);
                if (difference > .45f || difference >= best) continue;
                best = difference; floor = hit.point.y;
            }
            return float.IsFinite(best);
        }
        private static bool OpeningWorldCollider(Collider collider) => collider != null
            && collider.GetComponentInParent<CharacterMotor>() == null
            && collider.GetComponentInParent<Slipper>() == null && collider.GetComponentInParent<Lata>() == null;
        private bool WalkSegmentClear(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .0001f) return true;
            int count = Physics.CapsuleCastNonAlloc(from + Vector3.up * .45f, from + Vector3.up * 1.8f,
                .32f, delta.normalized, _hits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) if (OpeningWorldCollider(_hits[i].collider)) return false;
            return true;
        }

        private Vector3 PathAt(int seat, float share)
        {
            var path = _walkPath[seat];
            if (path == null) return _walkMarks[seat];
            float at = Mathf.Clamp01(share) * (path.Length - 1);
            int index = Mathf.Min(path.Length - 2, (int)at);
            return Vector3.Lerp(path[index], path[index + 1], at - index);
        }
        private void StageWalk(float age, bool reduced, bool advance = true)
        {
            float dt = advance ? Mathf.Clamp(age - _directionLastAge, 0, .1f) : 0;
            if (advance) _directionLastAge = age;
            for (int i = 0; i < 4; i++)
            {
                var root = _walkRoots[i]; var body = _players[i]; var pose = _poses[i];
                if (root == null || body == null) continue;
                float delay = i * .12f;
                float p = reduced ? 1 : Mathf.Clamp01((age - delay) / (_direction.Arrival - .45f - delay));
                float progress = Mathf.SmoothStep(0, 1, p);
                Vector3 at = PathAt(i, progress);
                var parent = root.parent;
                Vector3 offset = at - _walkMarks[i];
                root.localPosition = _walkRest[i] + (parent != null ? parent.InverseTransformVector(offset) : offset);
                root.localRotation = _walkTurns[i];
                bool walking = !reduced && p > 0 && p < .98f && _walkLengths[i] > .1f;
                if (walking)
                {
                    Vector3 toward = PathAt(i, Mathf.Min(1, progress + .04f)) - at; toward.y = 0;
                    if (toward.sqrMagnitude > .0001f)
                    {
                        Quaternion parentTurn = parent != null ? parent.rotation : Quaternion.identity;
                        root.localRotation = Quaternion.Inverse(parentTurn) * Quaternion.LookRotation(toward)
                            * Quaternion.Inverse(body.transform.rotation) * parentTurn * _walkTurns[i];
                    }
                }
                // Ease back to the body's actual start orientation before its portrait.
                if (p > .78f) root.localRotation = Quaternion.Slerp(root.localRotation, _walkTurns[i],
                    Mathf.SmoothStep(0, 1, (p - .78f) / .22f));
                if (advance)
                {
                    float metres=_walkHadSample[i]&&walking
                        ? Vector3.ProjectOnPlane(at-_walkPreviousAt[i],Vector3.up).magnitude : 0;
                    _walkPreviousAt[i]=at;_walkHadSample[i]=true;
                    pose?.SetArrivalGait(walking);
                    pose?.AdvanceHeldTravel(dt,metres);
                }
                pose?.SetArrivalGround(root, SupportHeight(at));
                if (!advance) pose?.RefreshArrivalGround();
            }
        }

        private void SampleDirection(float age, bool reduced, SceneFlow.MapEntry map)
        {
            age = Mathf.Clamp(age, 0, _direction.End);
            bool motion = Settings.SettingsStore.Current.CinematicCameraMotion;
            _directionDrawAge = age; _directionDrawReduced = reduced || !motion;
            StageWalk(age, reduced || !motion);
            int portrait = age < _direction.Arrival ? -1 :
                Mathf.Min(3, (int)((age - _direction.Arrival) / _direction.Portrait));
            int beat = portrait < 0 ? -1 : _portraitOrder[portrait];
            if (age >= _direction.Spotlight) beat = Core.MatchRules.DefenderSlotFor(1);
            float handoff = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(_direction.Handoff, _direction.End, age));
            for (int i = 0; i < 4; i++) _poses[i]?.SetArrivalPose(i,
                reduced || !motion ? .001f : Mathf.Max(.001f, i == beat ? .75f * (1 - handoff) : .001f));

            Vector3 eye, focus; float fov = 55;
            if (reduced) { eye = _wideEnd; focus = _centre; }
            else if (age < _direction.Arrival)
            {
                float p = Mathf.SmoothStep(0, 1, age / _direction.Arrival);
                EstablishDirection(p, map, out eye, out focus);
            }
            else if (age < _direction.Settle)
            {
                focus = _focus[beat];
                float local = age >= _direction.Spotlight ? (age - _direction.Spotlight) / (_direction.Settle - _direction.Spotlight)
                    : Mathf.Repeat((age - _direction.Arrival) / _direction.Portrait, 1);
                eye = Vector3.Lerp(_eyes[beat], focus, .06f * Mathf.SmoothStep(0, 1, local));
                fov = age >= _direction.Spotlight ? 46 : PortraitFov;
            }
            else { focus = _focus[beat]; eye = Vector3.Lerp(_eyes[beat], focus, .06f); fov = 46; }
            eye = ClearEye(focus, eye);
            Quaternion rotation = Quaternion.LookRotation(focus - eye, Vector3.up);
            if (reduced)
            {
                if (handoff >= .5f) { eye = _position; rotation = _rotation; fov = _fov; }
            }
            else
            {
                // Finish the camera path before the existing rig ownership return.
                // Orbit the look target instead of crossing through it while
                // turning away from the cast toward an opposite-facing wide shot.
                float viewProgress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(handoff / .7f));
                ReturnDirectionView(eye, focus, viewProgress, out eye, out rotation);
                fov = Mathf.Lerp(fov, _fov, viewProgress);
            }
            if (handoff >= .7f && MayReturnToGameplay()) { _returningToGameplay = true; _rig.SetActive(true); }
            if (!motion) { eye = _position; rotation = _rotation; fov = _fov; }
            _camera.transform.SetPositionAndRotation(eye, rotation); _camera.fieldOfView = fov;
            float ink = 1 - Mathf.SmoothStep(0, 1, age / .5f);
            if (!reduced && motion)
            {
                for (int cut = 0; cut < 4; cut++)
                    ink = Mathf.Max(ink, 1 - Mathf.Clamp01(Mathf.Abs(age - _direction.Arrival - cut * _direction.Portrait) / .1f));
            }
            if (reduced && age >= _direction.Handoff) ink = 1 - Mathf.Clamp01(Mathf.Abs(handoff - .5f) / .22f);
            if (!motion) ink = 0;
            _ink.color = new Color(UI.Hub.HubStyle.Ink.r, UI.Hub.HubStyle.Ink.g, UI.Hub.HubStyle.Ink.b, ink);
            _captionGroup.alpha = (1 - ink) * (1 - handoff);
            Caption(reduced ? (age >= _direction.Spotlight ? Core.MatchRules.DefenderSlotFor(1) : -1) : beat, map);
        }

        private void ReturnDirectionView(Vector3 portraitEye, Vector3 portraitFocus, float progress,
            out Vector3 eye, out Quaternion rotation)
        {
            if (progress >= 1) { eye = _position; rotation = _rotation; return; }
            Vector3 returningForward = _rotation * Vector3.forward;
            Vector3 returningFocus = _position + returningForward
                * Mathf.Max(2, Vector3.Dot(_centre - _position, returningForward));
            Vector3 from = portraitEye - portraitFocus, to = _position - returningFocus;
            float fromRadius = new Vector2(from.x, from.z).magnitude;
            float toRadius = new Vector2(to.x, to.z).magnitude;
            float fromYaw = Mathf.Atan2(from.x, from.z) * Mathf.Rad2Deg;
            float toYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            if (fromRadius < .001f) fromYaw = toYaw;
            if (toRadius < .001f) toYaw = fromYaw;
            // Both offsets are stable for this shot. DeltaAngle gives a stable
            // shortest arc across the wrap, including its180-degree tie.
            float yaw = (fromYaw + Mathf.DeltaAngle(fromYaw, toYaw) * progress) * Mathf.Deg2Rad;
            float radius = Mathf.Lerp(fromRadius, toRadius, progress);
            Vector3 focus = Vector3.Lerp(portraitFocus, returningFocus, progress);
            eye = focus + new Vector3(Mathf.Sin(yaw) * radius,
                Mathf.Lerp(from.y, to.y, progress), Mathf.Cos(yaw) * radius);
            eye = ClearEye(focus, eye);
            Vector3 look = focus - eye;
            if (new Vector2(look.x, look.z).sqrMagnitude < .0001f)
            {
                Vector3 horizontal = Vector3.ProjectOnPlane(returningForward, Vector3.up);
                if (horizontal.sqrMagnitude < .0001f) horizontal = Vector3.forward;
                look += horizontal.normalized * .01f;
            }
            rotation = Quaternion.LookRotation(look, Vector3.up);
        }

        private void EstablishDirection(float p, SceneFlow.MapEntry map, out Vector3 eye, out Vector3 focus)
        {
            focus = _centre;
            switch (_direction.Kind)
            {
                case Direction.Bridge:
                    eye = _centre + new Vector3(Mathf.Lerp(-6, 3, p), Mathf.Lerp(1.6f, 4.4f, p), -Mathf.Lerp(10, 21, p));
                    break;
                case Direction.Cove:
                    eye = _centre + new Vector3(Mathf.Lerp(-9, 5, p), Mathf.Lerp(3, 15, p), -Mathf.Lerp(30, 36, p));
                    break;
                default:
                    eye = _centre + Quaternion.Euler(0, Mathf.Lerp(58, 24, p), 0)
                        * new Vector3(0, Mathf.Lerp(12, 6, p), -Mathf.Lerp(25, 21, p));
                    break;
            }
        }

        private void RestoreDirection()
        {
            if (_directionStage != null)
            { _directionStage.Draw = null; Destroy(_directionStage); _directionStage = null; }
            for (int i = 0; i < 4; i++)
            {
                if (_walkRoots[i] != null)
                { _walkRoots[i].localPosition = _walkRest[i]; _walkRoots[i].localRotation = _walkTurns[i]; }
                _poses[i]?.ClearArrivalGround(); _poses[i]?.SetArrivalGesture(0);
                _walkRoots[i] = null; _walkPath[i] = null; _walkLengths[i] = 0;
                _walkHadSample[i]=false;_walkPreviousAt[i]=Vector3.zero;
            }
            _directed = false;
        }
    }

    [DefaultExecutionOrder(-25)]
    public sealed class DirectedMapArrivalStage : MonoBehaviour
    {
        public System.Action Draw;
        private void LateUpdate() => Draw?.Invoke();
    }
}
