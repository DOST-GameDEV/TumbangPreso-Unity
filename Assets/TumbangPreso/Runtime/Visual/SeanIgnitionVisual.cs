using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // A small ember curls around the equipped prop. Body and FPP use the same
    // authored form, scaled from their actual mesh bounds rather than a hand offset.
    public sealed class SeanIgnitionVisual : MonoBehaviour
    {
        private SeanHeroKit _kit;
        private Carrier _carrier;
        private Slipper _shoe;
        private readonly Transform[] _flames = new Transform[3];
        private readonly Vector3[] _sizes = new Vector3[3];
        private readonly Renderer[] _renderers = new Renderer[3];
        private Renderer _sourceRenderer;
        private float _age;
        public float RecordedAge=>_age;
        public Slipper RecordedShoe=>_shoe;
        public MeshFilter RecordedTarget {get;private set;}
        public bool RecordedWorld=>_shoe!=null&&RecordedTarget!=null&&RecordedTarget.transform.IsChildOf(_shoe.transform);

        public static void Ensure(MeshFilter target,Slipper shoe,SeanHeroKit kit)
        {
            if(target==null||target.sharedMesh==null||shoe==null||shoe.Holder==null||kit==null||!kit.IsIgnitionCannonActive)return;
            if(target.GetComponentInChildren<SeanIgnitionVisual>()!=null)return;
            var visual=CreateVisual(target);visual._kit=kit;visual._shoe=shoe;visual._carrier=shoe.Holder.GetComponent<Carrier>();
        }
        public static SeanIgnitionVisual Recorded(MeshFilter target)
        {var visual=CreateVisual(target);visual.enabled=false;return visual;}
        private static SeanIgnitionVisual CreateVisual(MeshFilter target)
        {
            var go = new GameObject("IgnitionEmber");
            go.layer = target.gameObject.layer;
            go.transform.SetParent(target.transform, false);
            var bounds = target.sharedMesh.bounds;
            var size = bounds.size;
            Vector3 along = size.x >= size.y && size.x >= size.z ? Vector3.right
                : size.y >= size.z ? Vector3.up : Vector3.forward;
            Vector3 normal = size.y <= size.x && size.y <= size.z ? Vector3.up
                : size.x <= size.z ? Vector3.right : Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(along, normal)) > .5f) normal = Vector3.up;
            go.transform.localPosition = bounds.center;
            go.transform.localRotation = Quaternion.LookRotation(along, normal);
            go.transform.localScale = Vector3.one * Mathf.Max(size.x, size.y, size.z) / .46f;
            var effect = go.AddComponent<SeanIgnitionVisual>();
            effect.RecordedTarget=target;
            effect._sourceRenderer = target.GetComponent<Renderer>();
            effect.Build();
            return effect;
        }

        private void Build()
        {
            // Toe flame, side curl and a shorter heel flick. Their offsets and lean
            // differ so the shoe stays readable between the three tongues of heat.
            for (int i = 0; i < 3; i++)
            {
                var flame = VfxShapes.Stand(transform, "KindledFlame_" + i,
                    VfxShapes.Tongue(5, .32f, i == 1 ? -.3f : .35f, .6f, .16f, 610 + i),
                    i == 0 ? .095f : .065f, i == 0 ? .19f : .13f, yaw: i * 115);
                flame.layer = gameObject.layer;
                flame.transform.localPosition = i == 0 ? new Vector3(.045f, .025f, .14f)
                    : i == 1 ? new Vector3(-.08f, .015f, -.02f) : new Vector3(.06f, .02f, -.15f);
                VfxMaterial.Ghost(flame.GetComponent<Renderer>(),
                    i == 0 ? new Color(1, .55f, .06f, .88f) : new Color(1, .29f, .025f, .76f), .35f);
                _flames[i] = flame.transform; _sizes[i] = flame.transform.localScale;
                _renderers[i] = flame.GetComponent<Renderer>();
            }
        }

        private void LateUpdate()
        {
            // CameraRig hides the held body prop before this effect is added. Follow
            // that prop's visibility; its FPP copy has its own visible ember.
            if (_sourceRenderer == null) return;
            foreach (var renderer in _renderers)
            {
                renderer.enabled = _sourceRenderer.enabled;
                renderer.shadowCastingMode = _sourceRenderer.shadowCastingMode;
            }
        }

        private void Update()
        {
            if (_kit == null || !_kit.IsIgnitionCannonActive || _carrier == null
                || _carrier.Held != _shoe || _carrier.GetComponent<HeroAbilitySystem>()?.Kit != _kit)
            {
                gameObject.SetActive(false); Destroy(gameObject); return;
            }
            StepTo(_age+Time.deltaTime);
        }
        public void StepTo(float seconds)
        {
            _age=seconds;
            float kindle = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_age / .16f));
            for (int i = 0; i < _flames.Length; i++)
            {
                float flicker = 1 + .13f * Mathf.Sin(_age * (8.7f + i) + i * 2.1f);
                _flames[i].localScale = Vector3.Scale(_sizes[i], new Vector3(1, flicker, 1)) * kindle;
                _flames[i].localRotation = Quaternion.Euler(0, i * 115, 7 * Mathf.Sin(_age * 3.2f + i));
            }
        }
    }
}
