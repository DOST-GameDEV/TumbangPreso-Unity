using TumbangPreso.Abilities;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Compact live power seals and a hold-to-read reference, with no gameplay authority.</summary>
    public sealed partial class TumpPowerReadout : MonoBehaviour
    {
        private readonly TumpAbilityDial[] _dials = new TumpAbilityDial[3];
        private readonly TumpAbilitySymbol[] _symbols = new TumpAbilitySymbol[3];
        private readonly Text[] _keys = new Text[3], _states = new Text[3], _names = new Text[3], _bodies = new Text[3], _timings = new Text[3];
        private readonly Image[] _keyGlyphs = new Image[3];
        private readonly TumpSurface[] _keycaps = new TumpSurface[3];
        private readonly TumpAbilitySymbol[] _detailSymbols = new TumpAbilitySymbol[3];
        private RectTransform _deck, _detail;
        private Text _hint;
        private InputAction _inspect;
        private HeroKit _shownKit;
        private string _shownSignature;
        private bool _ultimateReady;
        private bool _captureReference;
        private readonly HeroAbility[] _skills = new HeroAbility[3];
        private static readonly string[] Actions = { "Skill1", "Skill2", "Ultimate" };
        public void Build(Transform root)
        {
            var f = TumpUiTheme.Current;
            _deck = TumpUiFactory.Rect(root, "PowerSeals");
            TumpUiFactory.Anchor(_deck, new Vector2(.5f, 0), new Vector2(0, 112), new Vector2(470, 194));
            for (int i = 0; i < 3; i++)
            {
                float size = i == 2 ? 114 : 96;
                _dials[i] = TumpUiFactory.Rect(_deck, "Power" + i).gameObject.AddComponent<TumpAbilityDial>();
                _dials[i].raycastTarget = false;
                TumpUiFactory.Place(_dials[i].rectTransform, 30 + i * 144, i == 2 ? 0 : 18, size, size);
                _symbols[i] = TumpUiFactory.Rect(_dials[i].transform, "PowerIcon").gameObject.AddComponent<TumpAbilitySymbol>();
                _symbols[i].raycastTarget = false; TumpUiFactory.Stretch(_symbols[i].rectTransform, 19);
                _keys[i] = TumpUiFactory.Text(_deck, "LiveBinding" + i, "", 28, true);
                _keys[i].color = f.Cream; _keys[i].alignment = TextAnchor.MiddleCenter;
                TumpUiFactory.Place(_keys[i].rectTransform, 16 + i * 144, 120, 132, 52);
                _keys[i].font = f.Bold; _keys[i].fontSize = 30;
                _keycaps[i] = TumpUiFactory.Surface(_deck, "KeyboardCap" + i, TumpSurface.Form.Ticket, f.Cream);
                TumpUiFactory.Place(_keycaps[i].rectTransform, 52 + i * 144, 122, 60, 48);
                _keycaps[i].transform.SetSiblingIndex(_keys[i].transform.GetSiblingIndex());
                _keyGlyphs[i] = TumpUiFactory.Art(_keys[i].transform, "BindingGlyph", null);
                TumpUiFactory.Stretch(_keyGlyphs[i].rectTransform, 5);
                _states[i] = TumpUiFactory.Text(_dials[i].transform, "PowerState", "", 26);
                _states[i].alignment = TextAnchor.MiddleCenter; _states[i].color = f.Cream;
                TumpUiFactory.Stretch(_states[i].rectTransform, 8);
            }
            _hint = TumpUiFactory.Text(_deck, "PowerInfoBinding", "", 22);
            _hint.color = f.Cream; _hint.alignment = TextAnchor.MiddleCenter;
            TumpUiFactory.Place(_hint.rectTransform, -70, 169, 590, 42);
            BuildDetails(root);
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            _inspect = asset?.FindActionMap("Player", false)?.FindAction("AbilityInfo", false);
            _inspect?.Enable();
        }
        private void BuildPreviousDetails(Transform root)
        {
            var f = TumpUiTheme.Current;
            _detail = TumpUiFactory.Rect(root, "HeldPowerReference");
            TumpUiFactory.Anchor(_detail, new Vector2(.5f, 0), new Vector2(0, 464), new Vector2(1770, 486));
            // One reading sheet with three editorial columns, not three tiled menu cards.
            TumpUiFactory.Ground(_detail, f.Cream, false);
            for (int i = 0; i < 3; i++)
            {
                float x = 30 + i * 582;
                _detailSymbols[i] = TumpUiFactory.Rect(_detail, "DetailIcon" + i).gameObject.AddComponent<TumpAbilitySymbol>();
                _detailSymbols[i].color = f.Brick; _detailSymbols[i].raycastTarget = false;
                TumpUiFactory.Place(_detailSymbols[i].rectTransform, x, 28, 86, 86);
                _names[i] = TumpUiFactory.Text(_detail, "PowerName" + i, "", 36, true);
                _names[i].color = f.Brick; TumpUiFactory.Place(_names[i].rectTransform, x + 106, 16, 432, 126);
                _timings[i] = TumpUiFactory.Text(_detail, "PowerTiming" + i, "", 24);
                TumpUiFactory.Place(_timings[i].rectTransform, x + 2, 148, 532, 62);
                _bodies[i] = TumpUiFactory.Text(_detail, "PowerDescription" + i, "", 26);
                _bodies[i].alignment = TextAnchor.UpperLeft;
                TumpUiFactory.Place(_bodies[i].rectTransform, x + 2, 224, 530, 236);
                if (i > 0)
                {
                    var line = TumpUiFactory.Rect(_detail, "ColumnRule").gameObject.AddComponent<Image>();
                    line.color = f.OliveSand; line.raycastTarget = false; TumpUiFactory.Place(line.rectTransform, x - 24, 38, 2, 402);
                }
            }
            _detail.gameObject.SetActive(false);
        }
        public void Tick(HeroAbilitySystem system, bool visible)
        {
            var kit = system != null ? system.Kit : null;
            visible &= kit != null;
            _deck.gameObject.SetActive(visible);
            if (!visible) { _detail.gameObject.SetActive(false); return; }
            _skills[0] = kit.Skill1; _skills[1] = kit.Skill2; _skills[2] = kit.Ultimate;
            var skills = _skills;
            var f = TumpUiTheme.Current;
            for (int i = 0; i < 3; i++)
            {
                var skill = skills[i]; if (skill == null) continue;
                if (_symbols[i].Glyph != skill.Glyph) { _symbols[i].Glyph = skill.Glyph; _symbols[i].SetVerticesDirty(); }
                bool ready = !kit.PracticeMode && (i == 2 ? kit.IsUltimateReady : skill.IsReady);
                float ratio = i == 2 ? kit.UltimateRatio : skill.IsActive ? skill.DurationRatio : 1 - skill.CooldownRatio;
                _dials[i].State(ratio, ready, skill.IsActive, i == 2);
                _symbols[i].color = ready ? f.Lime : f.Cream;
                string state = kit.PracticeMode ? "Wait" : skill.IsActive ? skill.CanReactivate ? "Again" : skill.DurationRemaining.ToString("0.0")
                    : i == 2 ? ready ? "" : Mathf.FloorToInt(kit.UltimateRatio * 100) + "%"
                    : skill.UsesCharges ? skill.ChargesRemaining.ToString() : skill.CooldownRemaining > 0 ? AbilityDeckHud.CooldownLabel(skill.CooldownRemaining) : "";
                var slot = i == 0 ? HeroAbilitySystem.Slot.Skill1 : i == 1 ? HeroAbilitySystem.Slot.Skill2 : HeroAbilitySystem.Slot.Ultimate;
                if (system.SecondsSinceAnswer(slot) < .8f)
                {
                    var answer = system.LastAnswer(slot);
                    if (answer == HeroKit.CastOutcome.CannotAct) state = "Wait";
                    else if (answer == HeroKit.CastOutcome.NoCharge) state = "Empty";
                    else if (answer == HeroKit.CastOutcome.NotYet) state = "Not yet";
                }
                _states[i].text = state;
                _symbols[i].canvasRenderer.SetAlpha(string.IsNullOrEmpty(state) ? 1 : .25f);
                string binding = Hud.KeyLabelFor(Actions[i]); _keys[i].text = Hud.OnTouch ? "" : binding;
                bool gamepad = LastInputDevice.Current == InputDeviceKind.Gamepad;
                _keyGlyphs[i].sprite = gamepad ? InputGlyphs.For(binding.ToUpperInvariant(), true) : null;
                _keyGlyphs[i].enabled = _keyGlyphs[i].sprite != null;
                _keycaps[i].gameObject.SetActive(!Hud.OnTouch && !gamepad && binding.Length <= 3);
                _keys[i].color = _keyGlyphs[i].enabled ? Color.clear : _keycaps[i].gameObject.activeSelf ? f.DeepOlive : f.Cream;
            }
            if (kit.IsUltimateReady && !kit.PracticeMode && !_ultimateReady) GameServices.Audio?.PlayUi("sfx_super_ready");
            _ultimateReady = kit.IsUltimateReady && !kit.PracticeMode;
            _hint.text = Hud.OnTouch ? "Hold info for skills" : "Hold " + Hud.KeyLabelFor("AbilityInfo") + " for skills";
            bool held = _captureReference || (_inspect != null && _inspect.IsPressed());
            _detail.gameObject.SetActive(held);
            if (held) Describe(kit, skills);
        }
        private void Describe(HeroKit kit, HeroAbility[] skills)
        {
            string signature = string.Join("|", System.Array.ConvertAll(skills, a => a == null ? ""
                : a.Id + a.EffectiveName + a.EffectiveDescription + "/" + a.Cooldown + "/" + a.Duration + "/" + a.MaxCharges + "/" + (int)a.Glyph))
                + "/" + kit.UltimateCost;
            if (_shownKit == kit && _shownSignature == signature) return;
            _shownKit = kit; _shownSignature = signature;
            for (int i = 0; i < 3; i++)
            {
                var skill = skills[i]; if (skill == null) continue;
                _detailSymbols[i].Glyph = skill.Glyph; _detailSymbols[i].SetVerticesDirty();
                _names[i].text = skill.EffectiveName; _bodies[i].text = skill.EffectiveDescription;
                _timings[i].text = skill.UsesCharges ? skill.MaxCharges + (skill.MaxCharges == 1 ? " use" : " uses")
                    : skill.Cooldown > 0 ? skill.Cooldown.ToString("0.#") + "s cooldown" : "";
                if (skill.Duration > 0) _timings[i].text += " · " + skill.Duration.ToString("0.#") + "s duration";
                if (i == 2) _timings[i].text = kit.UltimateCost.ToString("0.#") + " charge needed";
            }
        }
        public void OpenForCapture(HeroKit kit)
        {
            _captureReference = true;
            Describe(kit, new[] { kit.Skill1, kit.Skill2, kit.Ultimate }); _detail.gameObject.SetActive(true);
        }
        public void CloseCapture() { _captureReference = false; _detail.gameObject.SetActive(false); }
    }
}
