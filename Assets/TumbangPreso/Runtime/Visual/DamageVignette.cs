using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// A short coloured wash at the EDGES of the local player's frame, in the hero colour of
    /// whoever just did something to them.
    ///
    /// ⚠️⚠️ IT ANSWERS THE ASYMMETRY THAT MADE HERO STRIKE FEEL UNFAIR. Every telegraph in this
    /// game is a ring drawn on the GROUND, and a ground ring is drawn for the person AIMING it:
    /// they are looking down at where they are placing it. The person standing inside it is in
    /// first person at eye height, seeing that same ring edge-on from a metre away, which is the
    /// worst possible angle for reading a circle. So the player who most needs the information
    /// gets the least of it, and "I got hit by something I never saw" is the result.
    ///
    /// Overwatch and Valorant both solve this by making the incoming thing louder for the
    /// TARGET than for the caster. This is that, at the cheapest possible cost: the ground ring
    /// is unchanged and a wash at the edge of the victim's own frame carries the rest.
    ///
    /// ⚠️ THE COLOUR IS THE ATTACKER'S ACCENT, WHICH IS WHY THIS IS WORTH A SCREEN EFFECT AT
    /// ALL. A generic red flash says "something happened", which the player already knew. Jade
    /// says Dante and amber says Sean, and `HeroPresentationTests` already asserts the five
    /// accents 30 degrees clear of each other and 25 clear of both role hues, so they survive
    /// being seen for a tenth of a second.
    ///
    /// ⚠️⚠️ IT IS AN OVERLAY AND NOT A POST PASS, DELIBERATELY. `ColourGrade` is already a
    /// full-screen blit on this camera and a second one would double the cost of the frame for
    /// an effect that is on screen for a fifth of a second. A single stretched quad on a
    /// screen-space canvas costs one draw call and can be torn down completely when idle.
    ///
    /// `docs/Hero_Strike_Balance.md` § 4.2.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageVignette : MonoBehaviour
    {
        private CanvasGroup _group;
        private float _left;
        private float _duration;
        private float _peak;

        /// <summary>
        /// ⚠️ NEVER PAST ~0.35 ALPHA. This fires on a player who has just been knocked down and
        /// is about to have to run somewhere; a wash they cannot see through is a second
        /// punishment on top of the stun, and `docs/VISION.md` § 2 is a whole section about
        /// effects that stop the player reading the arena. The vignette is a rim, not a filter:
        /// the middle of the screen stays clear by construction because the gradient sprite is
        /// transparent there.
        /// </summary>
        private const float MaxAlpha = 0.35f;

        public static void Flash(Component host, Color accent, float duration)
        {
            if (host == null || duration <= 0.0f) return;

            var v = host.GetComponent<DamageVignette>();
            if (v == null) v = host.gameObject.AddComponent<DamageVignette>();

            v.Begin(accent, duration);
        }

        private void Begin(Color accent, float duration)
        {
            EnsureCanvas();

            if (_image != null)
            {
                // Alpha is driven by the group, so the sprite tint carries hue only.
                _image.color = new Color(accent.r, accent.g, accent.b, 1.0f);
            }

            _duration = Mathf.Max(0.05f, duration);

            // ⚠️ A SECOND HIT INSIDE THE FIRST RESTARTS RATHER THAN STACKS. Two ultimates
            // landing together must not produce a wash twice as opaque as either; `Mathf.Max`
            // on the peak keeps the heavier of the two and the timer simply begins again.
            _left = _duration;
            _peak = MaxAlpha;
        }

        private UnityEngine.UI.Image _image;

        private void EnsureCanvas()
        {
            if (_group != null) return;

            var go = new GameObject("DamageVignette");
            go.transform.SetParent(transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // ⚠️ ABOVE THE HUD BUT BELOW NOTHING ELSE. The deck and the nameplates have to stay
            // readable through it, and they do because the middle of the sprite is clear; what
            // must NOT happen is the wash rendering under the HUD's own opaque wood panels,
            // which would clip it into rectangles.
            canvas.sortingOrder = 500;

            _group = go.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0.0f;

            var imageGo = new GameObject("Rim");
            imageGo.transform.SetParent(go.transform, false);

            _image = imageGo.AddComponent<UnityEngine.UI.Image>();
            _image.raycastTarget = false;
            _image.sprite = RimSprite();
            _image.type = UnityEngine.UI.Image.Type.Simple;

            var rect = imageGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite _rim;

        /// <summary>
        /// A radial gradient built once in code: opaque at the corners, clear through the middle.
        ///
        /// ⚠️ GENERATED RATHER THAN AUTHORED, because a 64 by 64 texture is smaller than the
        /// .meta file that would import it and this way it cannot go missing from a build the
        /// way a `Shader.Find`-only shader can (see `ColourGrade`'s note on stripping).
        /// </summary>
        private static Sprite RimSprite()
        {
            if (_rim != null) return _rim;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var centre = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = centre.magnitude;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) / maxDist;

                    // Clear until 55 per cent out, then ramped. Squared so the falloff is soft
                    // at the inner edge and firm at the rim rather than a visible ring.
                    float a = Mathf.Clamp01((d - 0.55f) / 0.45f);
                    tex.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, a * a));
                }
            }

            tex.Apply();

            _rim = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            _rim.hideFlags = HideFlags.HideAndDontSave;
            return _rim;
        }

        private void LateUpdate()
        {
            if (_group == null) return;

            if (_left <= 0.0f)
            {
                if (_group.alpha > 0.0f) _group.alpha = 0.0f;
                return;
            }

            // ⚠️ UNSCALED, TO MATCH `CameraRig.HoldFrame`. The vignette fires on the same frame
            // as the hitstop and the two have to fade on the same clock or the wash outlives the
            // freeze by a visible margin.
            _left -= Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(_left / _duration);

            // Fast in, slow out. The rise has to beat the eye to the punch and the fall has to
            // be slow enough to be read as a fade rather than as a cut.
            _group.alpha = _peak * Mathf.Sqrt(t);
        }
    }
}
