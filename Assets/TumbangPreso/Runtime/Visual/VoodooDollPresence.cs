using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ WHAT THE DOLL SHOWS OVER ITS HEAD (HERO-10 v3, plan 9.7 and 9.9 rows 8 and 9), on every peer (`Abilities.VoodooDollBody`
    /// adds it, the replica included):
    ///
    /// | When | What | Why |
    /// |---|---|---|
    /// | it scores (its throw or its tag) | +100 rises over it in HER colour | the owner: *"The doll gives points gained to Phaister"*; the scoreboard pays her, this says it was the doll |
    /// | it is tagged | a GREY STITCHED X pops over it for the stun | the owner: *"The doll does not give points when tagged/sabotaged"*: the one who tagged it sees it paid nothing |
    ///
    /// The score reaches every peer through `MatchDirector.CompanionScored` (the host raises it in `AddScore`; a client hears it in
    /// the `Score` message, which names the scoring body since protocol 91). The tag needs no message: the stun replicates with the body
    /// and `CharacterMotor.IsTagged` reads it.
    /// </summary>
    public sealed class VoodooDollPresence : MonoBehaviour
    {
        private const float PopSeconds = 1.3f, HeadRoom = 0.55f;
        private static readonly Color Grey = new Color(0.62f, 0.62f, 0.66f, 1f);

        private CharacterMotor _body;
        private MatchDirector _match;
        private Transform _x, _pop, _head;
        private TextMesh _popText;
        private float _popAge = -1f, _xAge = -1f;
        private bool _wasTagged;

        private void Awake()
        {
            _body = GetComponent<CharacterMotor>();
            _x = new GameObject("DollTaggedX").transform;
            _x.SetParent(transform, false);
            // Two grey cloth bars crossed, with dark stitches across each: a stitched X.
            foreach (float turn in new[] { 45f, -45f })
            {
                var bar = new GameObject("Bar").transform;
                bar.SetParent(_x, false);
                bar.localRotation = Quaternion.Euler(0f, 0f, turn);
                MarionetteControl.Block(bar, "Cloth", Vector3.zero, new Vector3(0.62f, 0.13f, 0.06f), Grey, 0, 0.25f);
                for (int i = 0; i < 4; i++)
                    MarionetteControl.Block(bar, "Stitch", new Vector3(-0.21f + i * 0.14f, 0f, 0.035f), new Vector3(0.025f, 0.17f, 0.02f),
                        new Color(0.2f, 0.2f, 0.24f, 1f), 0);
            }
            foreach (var r in _x.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _x.gameObject.SetActive(false);

            var pop = new GameObject("DollScorePop");
            _pop = pop.transform;
            _pop.SetParent(transform, false);
            _popText = pop.AddComponent<TextMesh>();
            _popText.anchor = TextAnchor.MiddleCenter;
            _popText.alignment = TextAlignment.Center;
            _popText.fontSize = CharacterNameplate.LabelFontSize;
            _popText.characterSize = 0.34f * 10f / CharacterNameplate.LabelFontSize;
            var font = UI.MenuKit.Font;
            if (font != null)
            {
                _popText.font = font;
                var r = pop.GetComponent<MeshRenderer>();
                if (r != null) r.sharedMaterial = font.material;
            }
            pop.SetActive(false);
        }

        private void OnEnable() => Bind();

        private void OnDisable()
        {
            if (_match != null) _match.CompanionScored -= OnScored;
            _match = null;
        }

        private void Bind()
        {
            var match = GameServices.Match;
            if (match == _match) return;
            if (_match != null) _match.CompanionScored -= OnScored;
            _match = match;
            if (_match != null) _match.CompanionScored += OnScored;
        }

        private void OnScored(int body, ScoreEvent e)
        {
            if (_body == null || body != _body.PlayerSlot) return;
            int points = MatchRules.PointsFor(e);
            if (points <= 0) return;
            _popText.text = "+" + points;
            _popAge = 0f;
        }

        private void LateUpdate()
        {
            Bind();
            if (_body == null) return;
            if (_head == null)
            {
                var visual = GetComponent<CharacterVisual>();
                if (visual != null && visual.Model != null)
                    foreach (var t in visual.Model.GetComponentsInChildren<Transform>(true)) if (t.name == "head") { _head = t; break; }
            }
            Vector3 over = (_head != null ? _head.position : transform.position + Vector3.up * 1.6f) + Vector3.up * HeadRoom;
            var cam = Camera.main;
            Quaternion facing = cam != null ? Quaternion.LookRotation(over - cam.transform.position, Vector3.up) : Quaternion.identity;

            // The grey stitched X: pops in when it is tagged, holds for the stun, shrinks away when it gets back up.
            bool tagged = _body.IsTagged;
            if (tagged && !_wasTagged) _xAge = 0f;
            _wasTagged = tagged;
            if (_xAge >= 0f)
            {
                _xAge += Time.deltaTime;
                float grow = Mathf.Clamp01(_xAge / 0.18f);
                float scale = tagged ? (grow < 1f ? Mathf.Sin(grow * Mathf.PI * 0.5f) * 1.25f : 1f + 0.05f * Mathf.Sin(_xAge * 6f))
                                     : Mathf.MoveTowards(_x.localScale.x, 0f, Time.deltaTime * 6f);
                _x.gameObject.SetActive(scale > 0.01f);
                _x.SetPositionAndRotation(over + Vector3.up * 0.25f, facing);
                _x.localScale = Vector3.one * scale;
                if (!tagged && scale <= 0.01f) _xAge = -1f;
            }

            // The score: rises and fades in her colour.
            if (_popAge >= 0f)
            {
                _popAge += Time.deltaTime;
                float u = _popAge / PopSeconds;
                if (u >= 1f) { _popAge = -1f; _pop.gameObject.SetActive(false); return; }
                _pop.gameObject.SetActive(true);
                var colour = UI.PlayerIdentity.Colour(CompanionSeats.OwnerOf(_body.PlayerSlot));
                colour.a = 1f - u * u;
                _popText.color = colour;
                float pop = u < 0.12f ? Mathf.Lerp(0.4f, 1.2f, u / 0.12f) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((u - 0.12f) / 0.15f));
                _pop.SetPositionAndRotation(over + Vector3.up * (0.35f + 0.9f * u), facing);
                _pop.localScale = Vector3.one * pop;
            }
        }
    }
}
