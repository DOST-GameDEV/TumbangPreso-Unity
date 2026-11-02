using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // A small ember curls around the equipped prop. Body and FPP use the same
    // authored form, scaled from their actual mesh bounds rather than a hand offset.
    //
    // ⚠️ MOLTEN THROW'S HELD COAT (2026-10-29). With `Painted` on, the loaded shoe in his hand wears the held fire of the
    // EMPOWERED THROW boards the owner chose s2 from ("s2 for empowered"): `FxStudySeanThrow.HeldMesh` and `HeldMaterial`,
    // exactly as `SeanThrowFilm` drew it on the held shoe, a glow and five licks in one mesh and one draw. It was filmed on
    // the bench and never wired into a match (`docs/VFX_BENCH_METHOD.md` section 10.3: "the held loaded shoe is unchanged"),
    // so the shoe flew as the s2 fireball out of a hand holding the older three tongues. This component, its lifetime and
    // every caller (the kit, the first-person arms, the replays) are unchanged; only what it draws is.
    public sealed class SeanIgnitionVisual : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ THE ONE SWITCH FOR MOLTEN THROW'S LOOK. On, the held loaded shoe wears the s2 held fire, and the can a coated
        /// throw knocks over is drawn by the s2 burst alone (`SeanImpact.CanContact` no longer lays the Ignition blast on top
        /// of it). Off, both are exactly as the rework left them. The flight is the approved s2 either way
        /// (`SeanThrowFire.Painted`). TO GO BACK, MAKE THE LINE BELOW `false`.
        /// </summary>
        public static bool Painted = true;

        private SeanHeroKit _kit;
        private Carrier _carrier;
        private Slipper _shoe;
        private readonly Transform[] _flames = new Transform[3];
        private readonly Vector3[] _sizes = new Vector3[3];
        private readonly Renderer[] _renderers = new Renderer[3];
        private Renderer _sourceRenderer;
        private bool _painted;
        private Renderer _held;
        private MaterialPropertyBlock _block;
        private static Material _heldLook;
        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity");
        private float _age;
        public float RecordedAge=>_age;
        public Slipper RecordedShoe=>_shoe;
        public MeshFilter RecordedTarget {get;private set;}
        public bool RecordedWorld=>_shoe!=null&&RecordedTarget!=null&&RecordedTarget.transform.IsChildOf(_shoe.transform);

        public static void Ensure(MeshFilter target,Slipper shoe,SeanHeroKit kit)
        {
            if(target==null||target.sharedMesh==null||shoe==null||shoe.Holder==null||kit==null||!kit.IsEmpoweredThrowLoadedFor(shoe))return;
            if(target.GetComponentInChildren<SeanIgnitionVisual>()!=null)return;
            var visual=CreateVisual(target,Painted);visual._kit=kit;visual._shoe=shoe;visual._carrier=shoe.Holder.GetComponent<Carrier>();
        }
        public static SeanIgnitionVisual Recorded(MeshFilter target) => Recorded(target, Painted);
        /// <summary>A replay's or a film's copy, stepped by `StepTo`; `painted` picks the look whatever the switch says.</summary>
        public static SeanIgnitionVisual Recorded(MeshFilter target, bool painted)
        {var visual=CreateVisual(target,painted);visual.enabled=false;return visual;}
        private static SeanIgnitionVisual CreateVisual(MeshFilter target, bool painted)
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
            effect._painted = painted;
            if (painted) effect.BuildHeld();
            else effect.Build();
            return effect;
        }

        /// <summary>
        /// The s2 held fire on this shoe: the bench's one mesh and its material, built once for the process; each shoe's own
        /// clock rides in a property block.
        /// </summary>
        private void BuildHeld()
        {
            if (_heldLook == null) _heldLook = FxStudySeanThrow.HeldMaterial(AmihanWhirlwind.DayLight);
            if (_heldLook == null) { _painted = false; Build(); return; }
            // On this holder as it is: its origin is the middle of the shoe's mesh and its z runs along the shoe's length,
            // which is all the held fire reads (`SeanFireThrow.shader`, piece 6, builds it in WORLD metres from the object's
            // origin and lays its licks along its z), so it is the board's size on the world shoe and the first-person one.
            var go = new GameObject("MoltenCoat") { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = FxStudySeanThrow.HeldMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _heldLook;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            VfxRenderTag.Attach(go);
            _held = renderer;
            _block = new MaterialPropertyBlock();
            StepTo(0f);
        }

        private void Build()
        {
            // Toe flame, side curl and a shorter heel flick. Their offsets and lean
            // differ so the shoe stays readable between the three tongues of heat.
            // Normalize the authored mesh radius: the small world footprint is
            // deliberate, but multiplying it by .32 again made detached needles.
            for (int i = 0; i < 3; i++)
            {
                var flame = VfxShapes.Stand(transform, "KindledFlame_" + i,
                    VfxShapes.Tongue(5, .32f, i == 1 ? -.3f : .35f, .6f, .16f, 610 + i),
                    (i == 0 ? .055f : .038f) / .32f, i == 0 ? .19f : .13f, yaw: i * 115);
                flame.layer = gameObject.layer;
                flame.transform.localPosition = i == 0 ? new Vector3(.025f, .012f, .14f)
                    : i == 1 ? new Vector3(-.065f, .012f, -.02f) : new Vector3(.045f, .012f, -.15f);
                VfxMaterial.Ghost(flame.GetComponent<Renderer>(),
                    i == 0 ? new Color(1, .66f, .075f, .88f) : new Color(1, .36f, .035f, .80f), .35f);
                _flames[i] = flame.transform; _sizes[i] = flame.transform.localScale;
                _renderers[i] = flame.GetComponent<Renderer>();
            }
        }

        private void LateUpdate()
        {
            // CameraRig hides the held body prop before this effect is added. Follow
            // that prop's visibility; its FPP copy has its own visible ember.
            if (_sourceRenderer == null) return;
            if (_painted)
            {
                if (_held != null) _held.enabled = _sourceRenderer.enabled;
                return;
            }
            foreach (var renderer in _renderers)
            {
                renderer.enabled = _sourceRenderer.enabled;
                renderer.shadowCastingMode = _sourceRenderer.shadowCastingMode;
            }
        }

        private void Update()
        {
            if (_kit == null || !_kit.IsEmpoweredThrowLoadedFor(_shoe) || _carrier == null
                || _carrier.Held != _shoe || _carrier.GetComponent<HeroAbilitySystem>()?.Kit != _kit)
            {
                gameObject.SetActive(false); Destroy(gameObject); return;
            }
            StepTo(_age+Time.deltaTime);
        }
        public void StepTo(float seconds)
        {
            _age=seconds;
            if (_painted)
            {
                // The bench's clock and nothing else: the shader draws the glow and the licks from it.
                if (_held == null || _block == null) return;
                _held.GetPropertyBlock(_block);
                _block.SetFloat(AgeId, _age); _block.SetFloat(OpacityId, 1f);
                _held.SetPropertyBlock(_block);
                return;
            }
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
