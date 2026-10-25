using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// What an AFTERIMAGE looks like (plan section 3): HIS OWN BODY, not a ghost (look rule 5), running his own sprint with a
    /// gold Shock rim, a quick flicker and a thin trail at its heels. It pops into flecks when the taya touches it, and at the
    /// end of its run it stumbles a step and fizzles from the feet up. Render only: posed from the field and its age alone.
    ///
    /// The body is a render copy from the match's pose history (`ZackFx.Echo`). Where there is none (a replay stage, a test
    /// scene) a gold runner silhouette stands in, so the copy is never simply missing.
    /// </summary>
    public sealed class ZackAfterimageBody : MonoBehaviour, IVfxTimeline
    {
        private const string SprintClip = "sprint";
        private WorldEffectSnapshot.Field _state;
        private ZackFx.Echo _echo;
        private AnimationClip _sprint;
        private Transform _stand;
        private GameObject _heldSlipper;
        private LineRenderer _trail, _trailInk;
        private float _age;
        private bool _popped;

        public float LifeSeconds => _state.Duration;

        public static ZackAfterimageBody Build(Transform parent, WorldEffectSnapshot.Field state, bool renderOnly = false)
        {
            var go = new GameObject("AfterimageBody");
            go.transform.SetParent(parent, false);
            // The pose copy writes its root in world space, so its parent sits at the origin.
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var body = go.AddComponent<ZackAfterimageBody>();
            body._state = state;
            body._renderOnly = renderOnly;
            if (!renderOnly)
            {
                body._echo = ZackFx.Echo.Of(state.Owner, go.transform);
                var owner = GameServices.Round?.PlayerAt(state.Owner);
                var animator = owner != null ? owner.GetComponentInChildren<CharacterAnimator>() : null;
                body._sprint = animator != null ? animator.ClipNamed(SprintClip) : null;
                if (body._echo != null) { body._echo.PoseAt(body._echo.Track.Newest); body._echo.Visible(true); }
            }
            if (body._echo == null) body._stand = BuildStandIn(go.transform);
            if (state.SecondScale > 0.5f && body._echo != null) body.BuildHeldSlipper();
            body._trailInk = ZackFx.Line(go.transform, "HeelTrailInk", 8, 0.09f, ZackFx.Ink, 0.0f);
            body._trail = ZackFx.Line(go.transform, "HeelTrail", 8, 0.045f, ZackFx.Core, 1.3f);
            return body;
        }

        // Copy meshes only. This shoe cannot be fetched, tagged or picked up and never enters the replay's live world.
        private void BuildHeldSlipper()
        {
            var owner = GameServices.Round?.PlayerAt(_state.Owner);
            var visual = owner != null ? owner.GetComponent<CharacterVisual>() : null;
            var source = owner != null ? owner.GetComponent<Carrier>()?.Held : null;
            if (source == null)
                foreach (var candidate in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                    if (candidate.SeatOfOrigin == _state.Owner) { source = candidate; break; }
            if (source == null || visual?.HandAnchor == null) return;
            var names = new System.Collections.Generic.List<string>();
            var bone = visual.HandAnchor;
            while (bone != null && bone != _echo.Track.Source.transform)
            { names.Add(bone.name); bone = bone.parent; }
            if (bone == null) return;
            names.Reverse();
            var grip = _echo.Copy.Root.transform.Find(string.Join("/", names));
            if (grip == null) return;
            _heldSlipper = new GameObject("AfterimageHeldSlipper");
            var shoe = _heldSlipper.transform;
            shoe.SetParent(grip, false);
            Vector3 scale = source.transform.lossyScale, parentScale = grip.lossyScale;
            shoe.localScale = new Vector3(scale.x / Mathf.Max(.0001f, Mathf.Abs(parentScale.x)),
                scale.y / Mathf.Max(.0001f, Mathf.Abs(parentScale.y)), scale.z / Mathf.Max(.0001f, Mathf.Abs(parentScale.z)));
            shoe.SetPositionAndRotation(grip.position, grip.rotation * Slipper.CarryRotation);
            foreach (var surface in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (surface.GetComponent<VfxRenderTag>() != null || !surface.TryGetComponent<MeshFilter>(out var mesh)
                    || mesh.sharedMesh == null) continue;
                var part = new GameObject(surface.name);
                part.transform.SetParent(shoe, false);
                part.transform.localPosition = source.transform.InverseTransformPoint(surface.transform.position);
                part.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * surface.transform.rotation;
                Vector3 size = surface.transform.lossyScale;
                part.transform.localScale = new Vector3(size.x / Mathf.Max(.0001f, Mathf.Abs(scale.x)),
                    size.y / Mathf.Max(.0001f, Mathf.Abs(scale.y)), size.z / Mathf.Max(.0001f, Mathf.Abs(scale.z)));
                part.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var drawn = part.AddComponent<MeshRenderer>();
                drawn.sharedMaterials = surface.sharedMaterials;
                drawn.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var surfaces = shoe.GetComponentsInChildren<Renderer>();
            if (surfaces.Length == 0) { Destroy(_heldSlipper); _heldSlipper = null; return; }
            var bounds = surfaces[0].bounds;
            foreach (var surface in surfaces) bounds.Encapsulate(surface.bounds);
            Vector3 up = grip.up, extent = bounds.extents;
            float support = Mathf.Abs(up.x) * extent.x + Mathf.Abs(up.y) * extent.y + Mathf.Abs(up.z) * extent.z;
            shoe.position += grip.position + up * support - bounds.center;
        }

        /// <summary>A gold runner made of four primitives, for a replay or a scene with no pose history.</summary>
        private static Transform BuildStandIn(Transform parent)
        {
            var root = new GameObject("AfterimageStandIn").transform;
            root.SetParent(parent, false);
            void Part(PrimitiveType type, Vector3 at, Vector3 scale)
            {
                var part = GameObject.CreatePrimitive(type);
                part.name = "StandIn" + type;
                part.transform.SetParent(root, false);
                part.transform.localPosition = at; part.transform.localScale = scale;
                VfxMaterial.Ghost(part.GetComponent<Renderer>(), new Color(ZackFx.Body.r, ZackFx.Body.g, ZackFx.Body.b, 0.75f), 1.0f);
            }
            Part(PrimitiveType.Capsule, new Vector3(0, 0.75f, 0), new Vector3(0.42f, 0.45f, 0.32f));
            Part(PrimitiveType.Sphere, new Vector3(0, 1.38f, 0.04f), new Vector3(0.36f, 0.36f, 0.36f));
            Part(PrimitiveType.Capsule, new Vector3(0.12f, 0.28f, 0.12f), new Vector3(0.14f, 0.28f, 0.14f));
            Part(PrimitiveType.Capsule, new Vector3(-0.12f, 0.28f, -0.12f), new Vector3(0.14f, 0.28f, 0.14f));
            return root;
        }

        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0.0f, seconds);
            var feet = PositionAt(_age);
            feet.y = VfxShapes.GroundAt(feet, _state.Position.y);
            var facing = Quaternion.LookRotation(_state.Forward, Vector3.up);
            float run = Mathf.Min(ZackRules.AfterimageSeconds, _state.Radius / ZackRules.AfterimageSpeed);
            float left = _state.Duration - _age;
            // Fizzle from the feet up over the last quarter second; the stumble is a dip in the last run step.
            float fizzle = Mathf.Clamp01(1.0f - left / 0.25f);
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            bool flicker = !reduced && Mathf.Repeat(_age, 0.23f) < 0.035f;
            bool visible = !_popped && left > 0.0f && !flicker;
            if (_echo != null)
            {
                var root = _echo.Copy.Bones[0];
                if (_sprint != null) _sprint.SampleAnimation(_echo.Copy.Root, Mathf.Repeat(_age, Mathf.Max(0.05f, _sprint.length)));
                root.SetPositionAndRotation(feet + Vector3.down * 0.06f * fizzle, facing);
                _echo.Coat(1.0f);
                _echo.Visible(visible && fizzle < 0.95f);
                if (_heldSlipper != null) _heldSlipper.SetActive(visible && fizzle < 0.95f);
                root.localScale = new Vector3(root.localScale.x, Mathf.Max(0.05f, 1.0f - fizzle * 0.9f) * Mathf.Abs(root.localScale.x), root.localScale.z);
            }
            else if (_stand != null)
            {
                _stand.SetPositionAndRotation(feet, facing);
                _stand.localScale = new Vector3(1.0f, Mathf.Max(0.05f, 1.0f - fizzle), 1.0f);
                _stand.gameObject.SetActive(visible);
            }
            // The heel trail: the last 1.6 m of its run, low on the road, gone once it stops.
            float travel = ZackRules.AfterimageTravel(_age, _state.Radius);
            float from = Mathf.Max(0.0f, travel - 1.6f);
            float trailAlpha = _popped || _age > run + 0.15f ? 0.0f : 0.9f;
            for (int i = 0; i < _trail.positionCount; i++)
            {
                float t = i / (float)(_trail.positionCount - 1);
                var p = _state.Position + _state.Forward * Mathf.Lerp(from, travel, t);
                p.y = VfxShapes.GroundAt(p, _state.Position.y) + 0.05f + 0.03f * Mathf.Sin(i * 2.3f + _age * 40.0f);
                _trail.SetPosition(i, p);
                _trailInk.SetPosition(i, p + Vector3.down * 0.01f);
            }
            ZackFx.Tint(_trail, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, trailAlpha));
            ZackFx.Tint(_trailInk, new Color(ZackFx.Ink.r, ZackFx.Ink.g, ZackFx.Ink.b, trailAlpha * 0.7f));
            if (fizzle > 0.0f && !_fizzleSparked && !_popped && !_renderOnly)
            {
                _fizzleSparked = true;
                ZackFx.Sparks(feet + Vector3.up * 0.4f, 0.3f, 4, _state.EventId);
            }
        }

        private bool _fizzleSparked, _renderOnly;

        private Vector3 PositionAt(float age) => _state.Position + _state.Forward * ZackRules.AfterimageTravel(age, _state.Radius);

        /// <summary>The taya touched it: a dry pop into flecks and it is gone.</summary>
        public void Pop()
        {
            if (_popped) return;
            _popped = true;
            if (_renderOnly) { StepTo(_age); return; }
            var at = PositionAt(_age) + Vector3.up * 0.9f;
            ZackFx.Sparks(at, 0.55f, 4, _state.EventId);
            ZackFx.Sparks(at + Vector3.down * 0.5f, 0.4f, 3, _state.EventId + 1);
            StepTo(_age);
        }

        private void OnDestroy() => _echo?.Destroy();
    }
}
