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
        private const float SlideDistance = 45.0f;
        private const float OpenSpeed = 8.5f;
        private const float CloseSpeed = 12.0f;
        private const float Stagger = 0.12f;

        private CanvasGroup _group;
        private RectTransform _rt;
        private InputAction _hold;

        private readonly Card[] _cards = new Card[3];
        private Text _title;
        private Text _hint;

        private float _open;          // 0 closed, 1 open
        private HeroKit _boundKit;

        private sealed class Card
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

            // Bottom-center horizontal tray inspired by modern hero shooters (Valorant style)
            _rt.anchorMin = new Vector2(0.5f, 0.0f);
            _rt.anchorMax = new Vector2(0.5f, 0.0f);
            _rt.pivot = new Vector2(0.5f, 0.0f);
            _rt.anchoredPosition = new Vector2(0, 16);
            _rt.sizeDelta = new Vector2(1060, 236);

            var bg = gameObject.AddComponent<Image>();
            bg.sprite = GodotTheme.Box(new Color(0.18f, 0.24f, 0.35f, 0.90f), new Color(0.05f, 0.07f, 0.11f, 0.95f), 2, 8);
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
            column.spacing = 6.0f;
            column.padding = new RectOffset(14, 14, 10, 10);

            // ---- Top Header Row (Title on left, Hold Hint on right) ----
            var headerRow = new GameObject("HeaderRow", typeof(RectTransform));
            headerRow.transform.SetParent(transform, false);
            var headerHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerHlg.childControlHeight = true;
            headerHlg.childControlWidth = true;
            headerHlg.childForceExpandHeight = true;
            headerHlg.childForceExpandWidth = false;
            Height(headerRow, 26);

            _title = Label(headerRow.transform, "Title", 22, UiTheme.Amber, TextAnchor.MiddleLeft);
            _title.fontStyle = FontStyle.Bold;
            _title.text = "HERO POWERS";
            var titleLe = _title.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1.0f;

            _hint = Label(headerRow.transform, "Hint", 15, UiTheme.CreamMuted, TextAnchor.MiddleRight);
            _hint.text = "HOLD [TAB] TO INSPECT";
            var hintLe = _hint.gameObject.AddComponent<LayoutElement>();
            hintLe.minWidth = 220;

            // ---- Cards Row (3 side-by-side columns) ----
            var cardsRow = new GameObject("CardsRow", typeof(RectTransform));
            cardsRow.transform.SetParent(transform, false);
            var cardsHlg = cardsRow.AddComponent<HorizontalLayoutGroup>();
            cardsHlg.childControlHeight = true;
            cardsHlg.childControlWidth = true;
            cardsHlg.childForceExpandHeight = true;
            cardsHlg.childForceExpandWidth = true;
            cardsHlg.spacing = 10.0f;
            Height(cardsRow, 178);

            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i] = BuildCard(cardsRow.transform, i);
            }

            gameObject.SetActive(false);
        }

        private Card BuildCard(Transform parent, int slotIndex)
        {
            var card = new Card();

            var go = new GameObject($"AbilityCard_{slotIndex}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            card.Rt = (RectTransform)go.transform;
            card.Group = go.AddComponent<CanvasGroup>();

            var bg = go.AddComponent<Image>();
            bg.sprite = GodotTheme.Box(new Color(0.16f, 0.22f, 0.32f, 0.85f), new Color(0.07f, 0.10f, 0.16f, 0.95f), 2, 6);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            var cardCol = go.AddComponent<VerticalLayoutGroup>();
            cardCol.childControlHeight = true;
            cardCol.childControlWidth = true;
            cardCol.childForceExpandHeight = false;
            cardCol.childForceExpandWidth = true;
            cardCol.spacing = 6.0f;
            cardCol.padding = new RectOffset(10, 10, 8, 8);

            // Top section: Icon Tile + Name + Role + Cooldown
            var topSection = new GameObject("TopSection", typeof(RectTransform));
            topSection.transform.SetParent(go.transform, false);
            var topHlg = topSection.AddComponent<HorizontalLayoutGroup>();
            topHlg.childControlHeight = true;
            topHlg.childControlWidth = true;
            topHlg.childForceExpandHeight = true;
            topHlg.childForceExpandWidth = false;
            topHlg.spacing = 8.0f;
            Height(topSection, 52);

            // Icon tile with key badge
            var tileGo = new GameObject("Tile");
            tileGo.transform.SetParent(topSection.transform, false);
            card.Tile = tileGo.AddComponent<Image>();
            card.Tile.sprite = GodotTheme.Box(new Color(0.24f, 0.32f, 0.44f, 0.75f), new Color(0.06f, 0.08f, 0.13f, 0.95f), 2, 6);
            card.Tile.type = Image.Type.Sliced;
            card.Tile.raycastTarget = false;

            var tileLe = tileGo.AddComponent<LayoutElement>();
            tileLe.minWidth = 50;
            tileLe.preferredWidth = 50;
            tileLe.minHeight = 50;
            tileLe.preferredHeight = 50;
            tileLe.flexibleHeight = 0.0f;

            var glyphGo = new GameObject("Glyph");
            glyphGo.transform.SetParent(tileGo.transform, false);
            card.Glyph = glyphGo.AddComponent<Image>();
            card.Glyph.color = Color.white;
            card.Glyph.preserveAspect = true;
            card.Glyph.raycastTarget = false;
            MenuKit.Stretch(card.Glyph.rectTransform);
            card.Glyph.rectTransform.offsetMin = new Vector2(6, 6);
            card.Glyph.rectTransform.offsetMax = new Vector2(-6, -6);

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
            chipRt.anchoredPosition = new Vector2(-1, 1);
            chipRt.sizeDelta = new Vector2(24, 18);

            card.Key = Label(chipGo.transform, "Key", 15, UiTheme.Cream, TextAnchor.MiddleCenter);
            card.Key.fontStyle = FontStyle.Bold;
            MenuKit.Stretch(card.Key.rectTransform);

            // Name + Role header stack
            var nameStack = new GameObject("NameStack", typeof(RectTransform));
            nameStack.transform.SetParent(topSection.transform, false);
            var nameCol = nameStack.AddComponent<VerticalLayoutGroup>();
            nameCol.childControlHeight = true;
            nameCol.childControlWidth = true;
            nameCol.childForceExpandHeight = false;
            nameCol.childForceExpandWidth = true;
            nameCol.spacing = 1.0f;
            var nameStackLe = nameStack.AddComponent<LayoutElement>();
            nameStackLe.flexibleWidth = 1.0f;

            card.Name = Label(nameStack.transform, "Name", 19, UiTheme.Cream, TextAnchor.MiddleLeft);
            card.Name.fontStyle = FontStyle.Bold;
            Height(card.Name.gameObject, 22);

            var metaRow = new GameObject("MetaRow", typeof(RectTransform));
            metaRow.transform.SetParent(nameStack.transform, false);
            var metaHlg = metaRow.AddComponent<HorizontalLayoutGroup>();
            metaHlg.childControlHeight = true;
            metaHlg.childControlWidth = true;
            metaHlg.childForceExpandHeight = true;
            metaHlg.childForceExpandWidth = false;
            metaHlg.spacing = 6.0f;
            Height(metaRow, 18);

            card.Kind = Label(metaRow.transform, "Kind", 14, UiTheme.Amber, TextAnchor.MiddleLeft);
            card.Kind.fontStyle = FontStyle.Bold;

            card.Meta = Label(metaRow.transform, "Meta", 14, UiTheme.Highlight, TextAnchor.MiddleLeft);
            card.Meta.fontStyle = FontStyle.Bold;

            // Description body with generous padding and clean font
            card.Body = Label(go.transform, "Body", 15, UiTheme.CreamMuted, TextAnchor.UpperLeft);
            card.Body.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.Body.verticalOverflow = VerticalWrapMode.Truncate;
            var bodyLe = card.Body.gameObject.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1.0f;
            bodyLe.minHeight = 90;

            return card;
        }

        // ------------------------------------------------------------------ runtime

        public void Bind(HeroKit kit)
        {
            if (kit == null || kit == _boundKit) return;

            _boundKit = kit;
            Color hero = UiTheme.ColorForHero(kit.HeroId);
            _title.text = (kit.HeroName + "  ·  HERO POWERS").ToUpperInvariant();
            _title.color = hero;

            Fill(_cards[0], kit.Skill1, "Skill1", hero);
            Fill(_cards[1], kit.Skill2, "Skill2", hero);
            Fill(_cards[2], kit.Ultimate, "Ultimate", hero);
        }

        private static void Fill(Card card, HeroAbility ability, string action, Color hero)
        {
            if (card == null) return;

            if (ability == null)
            {
                card.Rt.gameObject.SetActive(false);
                return;
            }

            card.Rt.gameObject.SetActive(true);
            card.Tile.color = Color.white;
            card.Glyph.sprite = AbilityIcons.For(ability.Glyph);
            card.Glyph.color = Color.white;
            card.Key.text = Hud.KeyLabelFor(action);
            card.Name.text = ability.Name;
            card.Name.color = hero;
            card.Kind.text = $"[{AbilityIcons.LabelFor(ability.Glyph)}]";
            card.Kind.color = hero;
            card.Body.text = ability.Description;

            if (ability.Cooldown > 0.0f)
            {
                card.Meta.text = ability.Duration > 0.0f
                    ? $"· {ability.Cooldown:0.#}s CD ({ability.Duration:0.#}s DURATION)"
                    : $"· {ability.Cooldown:0.#}s CD";
            }
            else
            {
                card.Meta.text = "· OBJECTIVE CHARGE";
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

            float eased = held ? EaseOutBack(_open) : EaseInQuad(_open);

            _group.alpha = Mathf.Clamp01(_open * 1.35f);
            _rt.anchoredPosition = new Vector2(0, 16 + (1.0f - eased) * -SlideDistance);

            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                if (card == null || card.Group == null) continue;

                float begin = i * Stagger;
                float local = Mathf.InverseLerp(begin, begin + (1.0f - Stagger * 2.0f), _open);
                float cardEase = held ? EaseOutBack(local) : local;

                card.Group.alpha = Mathf.Clamp01(local * 1.4f);
                card.Rt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.0f, cardEase);
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
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            return t;
        }
    }
}
