using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // Presentation measurements only. Model identity is cached, but moving, crouching,
    // growing and replaced meshes always contribute their current world bounds.
    public static class SpectatorFraming
    {
        private sealed class Body
        {
            public CharacterMotor Actor;
            public GameObject Model;
            public int Version;
            public readonly List<Renderer> Renderers = new();
        }
        private static readonly Body[] Bodies = new Body[Core.Balance.PlayerCount];
        private static RaycastHit[] SightHits = new RaycastHit[32];
        public static Bounds BodyBounds(CharacterMotor actor)
        {
            if (actor == null) return new Bounds(Vector3.zero, Vector3.one);
            var visual = actor.GetComponent<CharacterVisual>();
            int slot = actor.PlayerSlot;
            if (visual?.Model != null && slot >= 0 && slot < Bodies.Length)
            {
                var body = Bodies[slot] ??= new Body();
                if (body.Actor != actor || body.Model != visual.Model || body.Version != visual.ModelVersion)
                {
                    body.Actor = actor; body.Model = visual.Model; body.Version = visual.ModelVersion;
                    body.Renderers.Clear(); visual.Model.GetComponentsInChildren(true, body.Renderers);
                }
                bool found = false; Bounds bounds = default;
                foreach (var renderer in body.Renderers)
                {
                    if (renderer == null || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                        || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    var current = renderer.bounds;
                    if (!Finite(current.center) || !Finite(current.size) || current.size.sqrMagnitude > 10000) continue;
                    if (!found) { bounds = current; found = true; } else bounds.Encapsulate(current);
                }
                if (found && bounds.size.sqrMagnitude > .0001f) return bounds;
            }
            var capsule = actor.GetComponent<CharacterController>();
            if (capsule != null && capsule.enabled && capsule.bounds.size.sqrMagnitude > .0001f) return capsule.bounds;
            float height = actor.IsPerson ? 1.65f : .5f;
            return new Bounds(actor.transform.position + Vector3.up * (height * .5f), new Vector3(.6f, height, .6f));
        }
        public static Vector3 BodyFocus(CharacterMotor actor) => BodyBounds(actor).center;

        public static Vector3 ClearFollowEye(Vector3 focus, Vector3 wanted)
        {
            Vector3 delta = wanted - focus; float length = delta.magnitude;
            if (length < .001f) return wanted;
            int count;
            while (true)
            {
                count = Physics.SphereCastNonAlloc(focus, .22f, delta / length, SightHits, length, ~0, QueryTriggerInteraction.Ignore);
                if (count < SightHits.Length) break;
                System.Array.Resize(ref SightHits, SightHits.Length * 2);
            }
            float clear = length;
            for (int i = 0; i < count; i++)
            {
                var collider = SightHits[i].collider;
                if (collider == null || collider.GetComponentInParent<CharacterMotor>() != null
                    || collider.GetComponentInParent<Lata>() != null || collider.GetComponentInParent<Slipper>() != null) continue;
                clear = Mathf.Min(clear, Mathf.Max(.05f, SightHits[i].distance - .04f));
            }
            return focus + delta / length * clear;
        }

        // Extra distance along the current view ray needed to keep every bounds corner
        // in the inner84% of the actual lens, including narrow windows and tall bodies.
        public static float Pullback(Bounds bounds, Vector3 eye, Vector3 focus, float verticalFov, float aspect)
        {
            Vector3 forward = focus - eye;
            if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
            Quaternion inverse = Quaternion.Inverse(Quaternion.LookRotation(forward, Vector3.up));
            float vertical = Mathf.Tan(Mathf.Clamp(verticalFov, 10, 150) * Mathf.Deg2Rad * .5f) * .84f;
            float horizontal = vertical * Mathf.Max(.2f, aspect);
            float needed = 0;
            for (int i = 0; i < 8; i++)
            {
                var sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                Vector3 point = inverse * (bounds.center + Vector3.Scale(bounds.extents, sign) - eye);
                needed = Mathf.Max(needed, Mathf.Max(.3f - point.z,
                    Mathf.Max(Mathf.Abs(point.x) / horizontal - point.z, Mathf.Abs(point.y) / vertical - point.z)));
            }
            return needed;
        }
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => System.Array.Clear(Bodies, 0, Bodies.Length);
    }
}
