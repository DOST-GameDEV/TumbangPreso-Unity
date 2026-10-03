using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN v4: THE CARD, 2.50 to 2.95 s (`docs/reports/amihan-presentation-2026-10-02/cutscene-v4-plan.md`). The research
        // (Rappa, ZZZ, Persona 5, Varesa, Bridget): a character's graphic language belongs IN FRONT OF THE LENS, a flat panel
        // behind them with their name in big letters, under a second; the v3 glyphs floating in the scene read as "shapes".
        //
        // Hers: letterbox bars slide in; a flat warm teal backdrop square to the lens behind her, two stripes in her cream and
        // gold racing across it, AIRBURST in her display face (Darumadrop One) behind her shoulders; she points down the lane and
        // grins. Then her impatience: at 2.86 the wind tears the whole card off sideways before it has finished, and she goes.
        // The v3 density layer (diamond glints, kasikus rings, floor flashes, the glory, the veil) is gone.
        // =========================================================================================

        private int _abBack = -1, _abBarTop = -1, _abBarBottom = -1, _abStripe = -1, _abStripeThin = -1;
        private TextMesh _abWord, _abWordShadow;
        private float _abWordWidth;

        private void BuildAmihanBurst()
        {
            var quad = GlowQuad();
            _abBack = Add("AmihanCardBack", quad, new Color(0.13f, 0.42f, 0.40f, 1f), .95f, plain: true);
            _abBarTop = Add("AmihanCardBarTop", quad, new Color(0.05f, 0.07f, 0.06f, 1f), 0f, plain: true);
            _abBarBottom = Add("AmihanCardBarBottom", quad, new Color(0.05f, 0.07f, 0.06f, 1f), 0f, plain: true);
            // Two flat stripes in her cream and brooch gold racing across the backdrop (v4 r5: no woven cloth anywhere).
            _abStripe = Add("AmihanCardStripe", quad, new Color(1.0f, 0.95f, 0.84f, 1f), .95f, plain: true);
            _abStripeThin = Add("AmihanCardStripeThin", quad, new Color(0.95f, 0.76f, 0.31f, 1f), .95f, plain: true);
            var font = Resources.Load<Font>("UI/fonts/DarumadropOne-Regular");
            if (font != null)
            {
                _abWordShadow = AbWord("AmihanCardWordShadow", font, new Color(0.05f, 0.20f, 0.18f, 1f));
                _abWord = AbWord("AmihanCardWord", font, WindVfx.Cotton);
            }
        }

        private TextMesh AbWord(string name, Font font, Color colour)
        {
            var go = new GameObject(name); go.transform.SetParent(_root.transform, false);
            var word = go.AddComponent<TextMesh>();
            word.font = font; word.text = "AIRBURST"; word.fontSize = 120; word.characterSize = .1f;
            word.anchor = TextAnchor.MiddleCenter; word.alignment = TextAlignment.Center; word.color = colour;
            var renderer = go.GetComponent<MeshRenderer>();
            // Depth tested, so the word sits BEHIND her (the stock font shader drew it across her face, film v4 r1).
            var shader = Resources.Load<Shader>("Shaders/AmihanCardText");
            if (shader != null)
            {
                var m = new Material(shader) { name = name, mainTexture = font.material.mainTexture };
                renderer.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
            }
            else renderer.sharedMaterial = font.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.enabled = false;
            return word;
        }

        private void SampleAmihanBurst(float t, float leave)
        {
            bool on = t >= AmCardAt - .02f && t < AmWarpAt;
            if (!on || !AmLens(t, out _, out var eye, out var look, out float fov))
            {
                PvHide(_abBack); PvHide(_abBarTop); PvHide(_abBarBottom); PvHide(_abStripe); PvHide(_abStripeThin);
                if (_abWord != null) { _abWord.GetComponent<MeshRenderer>().enabled = false; _abWordShadow.GetComponent<MeshRenderer>().enabled = false; }
                return;
            }
            var view = (look - eye).normalized;
            var right = Vector3.Cross(Vector3.up, view).normalized;
            var up = Vector3.Cross(view, right);
            var facing = Quaternion.LookRotation(view, Vector3.up);
            float tan = Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
            // Slid in from the right in 0.08 s; torn off to the left by the wind at 2.86, tumbling.
            float arrive = 1f - Ease(AmCardAt - .02f, AmCardAt + .08f, t);
            float gone = Ease(AmCardGoneAt, AmWarpAt - .01f, t);
            float shove = arrive * 1.2f - gone * 1.6f;

            // THE BACKDROP: square to the lens 2.4 m behind her, a little larger than the frame.
            var herAt = new Vector3(0f, 1.1f + _amCourt, 0f);
            float depth = Vector3.Dot(herAt - eye, view) + 2.4f;
            var centre = eye + view * depth;
            float h = 2f * depth * tan * 1.12f, w = h * 16f / 9f * 1.15f;
            var tilt = Quaternion.AngleAxis(-gone * 25f, view);
            Place(_abBack, centre + right * (shove * w), new Vector3(w, h, 1f), tilt * facing, 1f);

            // THE STRIPES: racing across the backdrop on a diagonal, the way her wind crosses a street.
            var slant = tilt * facing * Quaternion.Euler(0f, 0f, 18f);
            float race = Mathf.Lerp(-.6f, .25f, Ease(AmCardAt - .02f, AmCardAt + .2f, t)) * w;
            Place(_abStripe, centre - view * .05f + right * (shove * w + race) - up * (h * .14f), new Vector3(w * 1.6f, h * .07f, 1f), slant, 1f);
            Place(_abStripeThin, centre - view * .06f + right * (shove * w + race * 1.3f) - up * (h * .24f), new Vector3(w * 1.6f, h * .02f, 1f), slant, 1f);

            // THE WORD behind her shoulders, a hair in front of the band, sized to most of the backdrop's width.
            if (_abWord != null)
            {
                var wordAt = centre - view * .12f + right * (shove * w * 1.15f) + up * (h * .12f);
                AbPlaceWord(_abWordShadow, wordAt + (right * .06f - up * .06f) * (h * .05f) + view * .01f, facing, tilt, w * .46f);
                AbPlaceWord(_abWord, wordAt, facing, tilt, w * .46f);
            }

            // THE LETTERBOX: two bars at the lens, sliding in with the card and out with it.
            float near = .5f, nh = 2f * near * tan, nw = nh * 16f / 9f * 1.3f, bar = nh * .13f;
            var nc = eye + view * near;
            float bars = Ease(AmCardAt - .02f, AmCardAt + .06f, t) * (1f - gone);
            Place(_abBarTop, nc + up * (nh * .5f + bar * (.5f - bars)), new Vector3(nw, bar, 1f), facing, 1f);
            Place(_abBarBottom, nc - up * (nh * .5f + bar * (.5f - bars)), new Vector3(nw, bar, 1f), facing, 1f);
        }

        private void AbPlaceWord(TextMesh word, Vector3 at, Quaternion facing, Quaternion tilt, float width)
        {
            var renderer = word.GetComponent<MeshRenderer>();
            word.transform.localPosition = at;
            if (_abWordWidth <= 0f)
            {
                // The word's own width at unit scale, laid flat in the scene's frame (any yaw of the root kept out), measured once.
                float local = renderer.localBounds.size.x;
                if (local > 1e-3f) _abWordWidth = local;
            }
            word.transform.localRotation = tilt * facing;
            word.transform.localScale = Vector3.one * (_abWordWidth > 0f ? width / _abWordWidth : .5f);
            renderer.enabled = true;
        }
    }
}
