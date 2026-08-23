using TumbangPreso.Abilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The hold-to-read ability panel. Hold the key, the three powers slide in with their full
    /// descriptions; let go and they slide out.
    ///
    /// ⚠️⚠️ THIS EXISTS SO THE HUD CAN STAY QUIET. 🧑 2026-08-23: *"games like valorant
    /// overwatch league etc dont clog their screen with text, to see how abilities work they
    /// usually click a button and then let go when they dont wanna see it anymore"*. Correct,
    /// and it is the standard answer to a real tension: a player needs the full text exactly
    /// twice, when learning the hero and when they forget mid-match, and needs it gone the rest
    /// of the time. The deck at the bottom of the screen therefore carries only what is true
    /// RIGHT NOW (icon, key, name, whether it is up) and every sentence lives here.
    ///
    /// ⚠️ IT IS A HOLD, NOT A TOGGLE, AND THAT IS THE WHOLE INTERACTION. A toggle leaves the
    /// panel up when the player gets jumped, which is the moment it does the most damage. A
    /// hold cannot be left on by accident: the screen is clear the instant the hand moves.
    ///
    /// ⚠️ NOTHING HERE TOUCHES `InputIntent`, AND THAT IS DELIBERATE. `PlayerInputReader`'s note
    /// is that it is the only place that reads hardware, because a bot and a human must press
    /// the same table. This key changes no world state at all: it opens a panel on the local
    /// player's own screen. Routing it through the intent table would mean every AI unit
    /// carries a verb that can never do anything.
    /// </summary>
    public sealed class AbilityInspectPanel : MonoBehaviour
    {
        private const float SlideDistance = 90.0f;
        private const float OpenSpeed = 7.5f;
        private const float CloseSpeed = 11.0f;

        /// <summary>
        /// ⚠️ THE THREE CARDS DO NOT ARRIVE TOGETHER. A stagger is what makes the panel read as
        /// one motion instead of a box appearing; each row is delayed by a fraction of the
        /// whole so the eye is led down the list in the order the keys sit on the deck.
        /// </summary>
        private const float Stagger = 0.18f;

        private CanvasGroup _group;
        private RectTransform _rt;
        private InputAction _hold;

        private readonly Row[] _rows = new Row[3];
        private Text _title;
        private Text _hint;

        private float _open;          // 0 closed, 1 open
        private HeroKit _boundKit;

        private sealed class Row
        {
            public RectTransform Rt;
            public CanvasGroup Group;
            public Image Tile;
            public Image Glyph;
            public Text Key;
            public Text Name;
            public Text Kind;
            public Text Body;
            public Text Meta;
        }

        public static AbilityInspectPanel Create(Transform parent)
        {
            var go = new GameObject("AbilityInspect", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var panel = go.AddComponent<AbilityInspectPanel>();
            panel.Build();
            return panel;
        }

        private void Build()
        {
            _rt = gameObject.GetComponent<RectTransform>();
            if (_rt == null) _rt = gameObject.AddComponent<RectTransform>();

            // Right-hand side, vertically centred. The deck owns the bottom of the screen and
            // the scoreboard owns the top left, so this is the one large area that is free.
            _rt.anchorMin = new Vector2(1.0f, 0.5f);
            _rt.anchorMax = new Vector2(1.0f, 0.5f);
            _rt.pivot = new Vector2(1.0f, 0.5f);
            _rt.anchoredPosition = new Vector2(-28, 0);
            _rt.sizeDelta = new Vector2(470, 396);

            var bg = gameObject.AddComponent<Image>();
            bg.sprite = GodotTheme.Box(UiTheme.WoodDark, UiTheme.WoodEdge,
                                       GodotTheme.WoodBorderWidth, GodotTheme.WoodCornerRadius);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0.0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var column = gameObject.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childForceExpandWidth = true;
            column.spacing = 8.0f;
            column.padding = new RectOffset(14, 14, 12, 12);

            _title = Label(transform, "Title", 26, UiTheme.Amber, TextAnchor.MiddleLeft);
            _title.fontStyle = FontStyle.Bold;
            _title.text = "YOUR POWERS";
            Height(_title.gameObject, 30);

            for (int i = 0; i < _rows.Length; i++) _rows[i] = BuildRow(transform);

            _hint = Label(transform, "Hint", MenuKit.MinReadableUnits, UiTheme.CreamMuted,
                          TextAnchor.MiddleLeft);
            _hint.text = "HOLD TO KEEP THIS OPEN";
            Height(_hint.gameObject, 20);

            gameObject.SetActive(false);
        }

        private Row BuildRow(Transform parent)
        {
            var row = new Row();

            // ⚠️ TYPED AT CONSTRUCTION. See the note in `Hud.BuildAbilityCard`: a plain
            // `new GameObject` is not a RectTransform and parenting does not make it one.
            var go = new GameObject("Ability", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            row.Rt = (RectTransform)go.transform;
            row.Group = go.AddComponent<CanvasGroup>();

            var bg = go.AddComponent<Image>();
            bg.sprite = GodotTheme.Box(UiTheme.WoodDeep, UiTheme.WoodEdge, 3, 6);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            Height(go, 104);

            var group = go.AddComponent<HorizontalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = false;
            group.spacing = 10.0f;
            group.padding = new RectOffset(10, 10, 9, 9);
            group.childAlignment = TextAnchor.UpperLeft;

            // ---- the tile ----------------------------------------------------------
            var tileGo = new GameObject("Tile");
            tileGo.transform.SetParent(go.transform, false);
            row.Tile = tileGo.AddComponent<Image>();
            row.Tile.sprite = GodotTheme.Box(UiTheme.Amber, UiTheme.Ink, 3, 6);
            row.Tile.type = Image.Type.Sliced;
            row.Tile.raycastTarget = false;

            var tileLe = tileGo.AddComponent<LayoutElement>();
            tileLe.minWidth = 68;
            tileLe.preferredWidth = 68;
            tileLe.minHeight = 68;
            tileLe.preferredHeight = 68;
            tileLe.flexibleHeight = 0.0f;

            var glyphGo = new GameObject("Glyph");
            glyphGo.transform.SetParent(tileGo.transform, false);
            row.Glyph = glyphGo.AddComponent<Image>();
            row.Glyph.color = UiTheme.Ink;
            row.Glyph.preserveAspect = true;
            row.Glyph.raycastTarget = false;
            MenuKit.Stretch(row.Glyph.rectTransform);
            row.Glyph.rectTransform.offsetMin = new Vector2(11, 11);
            row.Glyph.rectTransform.offsetMax = new Vector2(-11, -11);

            var chipGo = new GameObject("KeyChip");
            chipGo.transform.SetParent(tileGo.transform, false);
            var chip = chipGo.AddComponent<Image>();
            chip.sprite = GodotTheme.Box(UiTheme.Ink, new Color(0, 0, 0, 0), 0, 4);
            chip.type = Image.Type.Sliced;
            chip.raycastTarget = false;
            var chipRt = chip.rectTransform;
            chipRt.anchorMin = new Vector2(1.0f, 0.0f);
            chipRt.anchorMax = new Vector2(1.0f, 0.0f);
            chipRt.pivot = new Vector2(1.0f, 0.0f);
            chipRt.anchoredPosition = new Vector2(-2, 2);
            chipRt.sizeDelta = new Vector2(30, 20);

            row.Key = Label(chipGo.transform, "Key", 17, UiTheme.Cream, TextAnchor.MiddleCenter);
            row.Key.fontStyle = FontStyle.Bold;
            MenuKit.Stretch(row.Key.rectTransform);

            // ---- the words ---------------------------------------------------------
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textCol = textGo.AddComponent<VerticalLayoutGroup>();
            textCol.childControlHeight = true;
            textCol.childControlWidth = true;
            textCol.childForceExpandHeight = false;
            textCol.childForceExpandWidth = true;
            textCol.spacing = 1.0f;

            var textLe = textGo.AddComponent<LayoutElement>();
            textLe.minWidth = 340;
            textLe.preferredWidth = 340;
            textLe.flexibleWidth = 1.0f;

            row.Name = Label(textGo.transform, "Name", 22, UiTheme.Cream, TextAnchor.UpperLeft);
            row.Name.fontStyle = FontStyle.Bold;
            Height(row.Name.gameObject, 24);

            row.Kind = Label(textGo.transform, "Kind", MenuKit.MinReadableUnits, UiTheme.Amber,
                             TextAnchor.UpperLeft);
            row.Kind.fontStyle = FontStyle.Bold;
            Height(row.Kind.gameObject, 18);

            // ⚠️ THE DESCRIPTION WRAPS AND IS ALLOWED TO OVERFLOW ITS ROW. Truncating the one
            // piece of text in the whole game whose entire job is to explain the power would be
            // the same defect the deck already had, one level up.
            row.Body = Label(textGo.transform, "Body", MenuKit.MinReadableUnits, UiTheme.CreamMuted,
                             TextAnchor.UpperLeft);
            row.Body.horizontalOverflow = HorizontalWrapMode.Wrap;
            row.Body.verticalOverflow = VerticalWrapMode.Overflow;
            Height(row.Body.gameObject, 34);

            row.Meta = Label(textGo.transform, "Meta", MenuKit.MinReadableUnits, UiTheme.Highlight,
                             TextAnchor.UpperLeft);
            row.Meta.fontStyle = FontStyle.Bold;
            Height(row.Meta.gameObject, 18);

            return row;
        }

        // ------------------------------------------------------------------ runtime

        public void Bind(HeroKit kit)
        {
            if (kit == null || kit == _boundKit) return;

            _boundKit = kit;
            _title.text = kit.HeroName + "  ·  POWERS";

            Color hero = UiTheme.ColorForHero(kit.HeroId);

            Fill(_rows[0], kit.Skill1, "Skill1", hero);
            Fill(_rows[1], kit.Skill2, "Skill2", hero);
            Fill(_rows[2], kit.Ultimate, "Ultimate", hero);
        }

        private static void Fill(Row row, HeroAbility ability, string action, Color hero)
        {
            if (row == null) return;

            if (ability == null)
            {
                row.Rt.gameObject.SetActive(false);
                return;
            }

            row.Rt.gameObject.SetActive(true);
            row.Tile.color = hero;
            row.Glyph.sprite = AbilityIcons.For(ability.Glyph);
            row.Key.text = Hud.KeyLabelFor(action);
            row.Name.text = ability.Name;
            row.Kind.text = AbilityIcons.LabelFor(ability.Glyph);
            row.Body.text = ability.Description;

            // ⚠️ THE ULTIMATE HAS NO COOLDOWN AND SAYING "0.0s COOLDOWN" WOULD BE A LIE. It is
            // gated by charge, which is a different economy, so it says so.
            if (ability.Cooldown > 0.0f)
            {
                row.Meta.text = ability.Duration > 0.0f
                    ? $"COOLDOWN {ability.Cooldown:0.#}s   ·   LASTS {ability.Duration:0.#}s"
                    : $"COOLDOWN {ability.Cooldown:0.#}s";
            }
            else
            {
                row.Meta.text = "CHARGES FROM OBJECTIVE PLAY";
            }
        }

        public void Tick(HeroKit kit, float dt)
        {
            if (kit == null)
            {
                if (gameObject.activeSelf && _open <= 0.0f) gameObject.SetActive(false);
                return;
            }

            Bind(kit);

            bool held = HoldPressed();
            float speed = held ? OpenSpeed : CloseSpeed;
            _open = Mathf.MoveTowards(_open, held ? 1.0f : 0.0f, speed * dt);

            if (_open <= 0.0f)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            // ⚠️ EASED, NOT LINEAR, AND THE TWO EASES DIFFER. Opening overshoots slightly and
            // settles, which reads as the panel being pulled out; closing is a straight fall,
            // because a bouncy exit keeps the eye on something the player has just dismissed.
            float eased = held ? EaseOutBack(_open) : EaseInQuad(_open);

            _group.alpha = Mathf.Clamp01(_open * 1.35f);
            _rt.anchoredPosition = new Vector2(-28 + (1.0f - eased) * SlideDistance, 0);

            for (int i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                if (row == null || row.Group == null) continue;

                // Each row runs the same 0..1 through its own slice of the timeline.
                float begin = i * Stagger;
                float local = Mathf.InverseLerp(begin, begin + (1.0f - Stagger * 2.0f), _open);
                float rowEase = held ? EaseOutBack(local) : local;

                row.Group.alpha = Mathf.Clamp01(local * 1.4f);
                row.Rt.anchoredPosition = new Vector2((1.0f - rowEase) * 46.0f,
                                                      row.Rt.anchoredPosition.y);
            }
        }

        private bool HoldPressed()
        {
            if (_hold == null)
            {
                var asset = Resources.Load<InputActionAsset>("TumbangPreso");
                if (asset == null) return false;

                Settings.Rebinding.Load(asset);
                var map = asset.FindActionMap("Player", false);
                _hold = map?.FindAction("AbilityInfo", false);
                _hold?.Enable();
            }

            return _hold != null && _hold.IsPressed();
        }

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1.0f;
            float p = t - 1.0f;
            return 1.0f + c3 * p * p * p + c1 * p * p;
        }

        private static float EaseInQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        // ------------------------------------------------------------------ helpers

        private static void Height(GameObject go, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        private static Text Label(Transform parent, string name, int size, Color colour,
                                  TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<Text>();
            t.font = MenuKit.Font;
            t.fontSize = size;
            t.color = colour;
            t.alignment = align;
            t.alignByGeometry = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var ring = go.AddComponent<GodotOutline>();
            ring.OutlineColour = UiTheme.Ink;
            ring.Radius = 1.5f;

            return t;
        }
    }
}
