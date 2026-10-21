using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// How a hand-made HOME sticker answers the hand (owner, 2026-10-21: "make it amazing to press").
    ///
    /// The board's stickers are a knit FACE on a thicker SLAB (`tools/build_lobby_art.py` draws them as
    /// two sprites on one canvas), so the press is physical: the face slides down and right onto its
    /// slab, squashes, and springs back past rest when let go. <see cref="HubButton"/> still owns the
    /// four states (hover, pad focus, press, disabled) and their sounds; this only draws them.
    ///
    ///   hover / focus  the face lifts off the slab, grows 3 per cent and gives one wobble; the
    ///                  picture on it hops up and leans in
    ///   press          the face sits down onto the slab (80 per cent of the slab's depth) and squashes
    ///   release        damped springs carry it back past rest, so it bounces rather than snaps
    ///   click          the picture pops; PLAY also throws a handful of knit sparks
    ///   idle           the pictures bob on their own phase; PLAY breathes
    ///   pad focus      a honey rim pulses around the face, ONLY while a pad is the live device.
    ///                  ⚠️ HOME selects PLAY on open for every player; a rim drawn for that on a
    ///                  mouse read as a pale halo round the sticker (2026-10-21 capture), the one
    ///                  thing the owner banned. A pointer player never sees it.
    ///
    /// ⚠️ REDUCED UI MOTION keeps every state's resting pose and drops every spring, wobble, bob and
    /// spark (the rule <see cref="HubButton"/> and <see cref="HubSlap"/> already keep).
    /// </summary>
    public sealed class HubStickerFeel : MonoBehaviour
    {
        public HubButton Button;
        public RectTransform Face;
        public RectTransform[] Pictures = new RectTransform[0];
        /// <summary>The slab's offset from the face in Unity units (x right, y up), e.g. (26, -20).</summary>
        public Vector2 Depth = new Vector2(4, -6);
        public bool Primary;
        public float Phase;

        private Vector2 _faceRest, _slabRest;
        private RectTransform _slab;
        private Vector2[] _pictureRest = new Vector2[0];
        private float _lift, _liftV, _press, _pressV, _wobble, _wobbleV, _pop, _popV;
        private float _lastLiftTarget;
        private Outline _rim;
        private bool _ready;

        private const float LiftK = 260f, LiftC = 17f, PressK = 900f, PressC = 34f, WobbleK = 320f, WobbleC = 9f, PopK = 420f, PopC = 14f;

        private void Start() => Capture();

        public void Capture()
        {
            if (Face == null) return;
            _faceRest = Face.anchoredPosition;
            _slab = Button != null ? Button.transform.Find("Slab") as RectTransform : null;
            if (_slab != null) _slabRest = _slab.anchoredPosition;
            _pictureRest = new Vector2[Pictures.Length];
            for (int i = 0; i < Pictures.Length; i++) if (Pictures[i] != null) _pictureRest[i] = Pictures[i].anchoredPosition;
            var faceImage = Face.GetComponent<Image>();
            if (faceImage != null)
            {
                _rim = HubKit.Ensure<Outline>(Face.gameObject);
                _rim.effectColor = new Color(0.961f, 0.71f, 0.129f, 0f);
                _rim.useGraphicAlpha = true;
            }
            if (Button != null) Button.onClick.AddListener(Clicked);
            _ready = true;
        }

        private void OnDisable()
        {
            if (!_ready) return;
            _lift = _liftV = _press = _pressV = _wobble = _wobbleV = _pop = _popV = 0;
            Apply(0);
        }

        private void Clicked()
        {
            if (HubStyle.ReducedMotion) return;
            _popV += 9f;
            _wobbleV += 260f * (Random.value < 0.5f ? -1 : 1);
            if (Primary) HubSparks.Burst(transform, Face != null ? Face : (RectTransform)transform);
        }

        private static void Spring(ref float x, ref float v, float target, float k, float c, float dt)
        {
            // semi-implicit Euler in small steps: stable for the stiff press spring at any frame rate
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / 0.004f), 1, 12);
            float h = dt / steps;
            for (int i = 0; i < steps; i++) { v += (k * (target - x) - c * v) * h; x += v * h; }
        }

        private void Update()
        {
            if (!_ready || Button == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float liftTarget = Button.IsInteractable() ? Button.LiftTarget : 0f;
            float pressTarget = Button.IsInteractable() ? Button.PressTarget : 0f;

            if (HubStyle.ReducedMotion)
            {
                _lift = liftTarget; _press = pressTarget; _wobble = _pop = 0;
                Apply(0);
                return;
            }

            if (liftTarget > 0.5f && _lastLiftTarget <= 0.5f) _wobbleV += 140f * (Phase > 0.5f ? 1 : -1);
            _lastLiftTarget = liftTarget;

            Spring(ref _lift, ref _liftV, liftTarget, LiftK, LiftC, dt);
            Spring(ref _press, ref _pressV, pressTarget, PressK, PressC, dt);
            Spring(ref _wobble, ref _wobbleV, 0f, WobbleK, WobbleC, dt);
            Spring(ref _pop, ref _popV, 0f, PopK, PopC, dt);
            Apply(Time.unscaledTime);
        }

        private void Apply(float t)
        {
            if (Face == null) return;
            bool still = HubStyle.ReducedMotion;
            // ⚠️ HOVER MOVES THE WHOLE STICKER, FACE AND SLAB AS ONE (owner, 2026-10-22: PLAY "separated
            // from itself whenever u hover over it ... the shadow seemed separate"). Lifting the face alone
            // opened a gap over her slab. Hover now grows and tilts both about the face's centre; only a
            // PRESS moves the face, down into its slab, which is the button being pushed.
            float breathe = Primary && !still ? Mathf.Sin(t * 2.4f + Phase * 6.28f) * 0.012f * (1f - _lift) : 0f;
            float grow = 1f + 0.035f * _lift + breathe;
            float tilt = _wobble * 0.04f;
            Face.anchoredPosition = _faceRest + Depth * (0.8f * _press);
            Face.localScale = new Vector3(grow + 0.04f * _press, grow - 0.055f * _press, 1);
            Face.localRotation = Quaternion.Euler(0, 0, tilt);
            if (_slab != null)
            {
                Vector2 off = (Vector2)(Quaternion.Euler(0, 0, tilt) * (Vector3)(_slabRest - _faceRest)) * grow;
                _slab.anchoredPosition = _faceRest + off;
                _slab.localScale = new Vector3(grow, grow, 1);
                _slab.localRotation = Quaternion.Euler(0, 0, tilt);
            }

            for (int i = 0; i < Pictures.Length; i++)
            {
                var p = Pictures[i]; if (p == null) continue;
                float bob = still ? 0f : Mathf.Sin(t * 1.7f + (Phase + i * 0.37f) * 6.28f) * 2.2f;
                p.anchoredPosition = _pictureRest[i] + new Vector2(0, bob + 9f * _lift) + Depth * (0.8f * _press);
                float s = 1f + 0.09f * _lift + 0.16f * _pop - 0.05f * _press;
                p.localScale = new Vector3(s, s, 1);
                p.localRotation = Quaternion.Euler(0, 0, _wobble * 0.09f + 4f * _lift * (Phase > 0.5f ? 1 : -1));
            }

            if (_rim != null)
            {
                bool focus = Button != null && Button.HasPadFocus && LastInputDevice.Current == InputDeviceKind.Gamepad;
                float pulse = focus ? (still ? 1f : 0.65f + 0.35f * Mathf.Sin(t * 6.9f)) : 0f;
                var c = _rim.effectColor; c.a = pulse; _rim.effectColor = c;
                float d = 4f + (still ? 0f : 1.5f * Mathf.Sin(t * 6.9f));
                _rim.effectDistance = new Vector2(d, -d);
                _rim.enabled = focus;
            }
        }
    }

    /// <summary>PLAY's knit sparks: a dozen dots and stars thrown out of the face, falling and fading in 0.7 s.</summary>
    public sealed class HubSparks : MonoBehaviour
    {
        private Vector2 _v; private float _spin, _age, _life;
        private Image _image; private RectTransform _rect;

        public static void Burst(Transform owner, RectTransform from)
        {
            var canvas = owner.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var layer = (RectTransform)canvas.transform;
            Vector3 centre = from.TransformPoint(from.rect.center);
            Vector2 local = layer.InverseTransformPoint(centre);
            string[] art = { "spark_dot_peach", "spark_dot_lime", "spark_star_honey", "spark_star_peach" };
            for (int i = 0; i < 14; i++)
            {
                var rect = HubKit.Rect(layer, "Spark");
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = local + Random.insideUnitCircle * 30f;
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = HubBoard.Art(art[i % art.Length]);
                image.raycastTarget = false;
                float size = Random.Range(14f, 30f);
                rect.sizeDelta = new Vector2(size, size);
                var s = rect.gameObject.AddComponent<HubSparks>();
                float a = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float speed = Random.Range(420f, 820f);
                s._v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed;
                s._spin = Random.Range(-540f, 540f);
                s._life = Random.Range(0.55f, 0.8f);
                s._image = image; s._rect = rect;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _age += dt;
            if (_age >= _life) { Destroy(gameObject); return; }
            _v += new Vector2(0, -1500f) * dt;
            _v *= 1f - 1.8f * dt;
            _rect.anchoredPosition += _v * dt;
            _rect.localRotation = Quaternion.Euler(0, 0, _rect.localEulerAngles.z + _spin * dt);
            float k = _age / _life;
            var c = _image.color; c.a = 1f - k * k; _image.color = c;
            float s = 1f + 0.4f * Mathf.Sin(k * Mathf.PI);
            _rect.localScale = new Vector3(s, s, 1);
        }
    }
}
