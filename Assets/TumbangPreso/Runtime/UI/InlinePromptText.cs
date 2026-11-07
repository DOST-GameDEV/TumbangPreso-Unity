using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A sentence with the player's key icons set in its line, wrapped to a width (owner, 2026-11-05: "if you have
    /// something like Press [Q] do not just put it into text use the prompts that we have, we have assets for keybind
    /// logos"). `{KEY}` is the given action's own button and `{Grab}`, `{Jump}` or any action name in braces is that
    /// action's: each is drawn as its `InputGlyphs` cap, which follows rebinds and the device in hand because it is
    /// looked up by `Hud.KeyLabelFor`. A control with no cap keeps its label in brackets, and a phone, which is never
    /// told a key, reads the button's name (`AbilityKeys`).
    ///
    /// A legacy `Text` cannot hold a picture, so every word is its own small label, laid out here line by line.
    /// </summary>
    public sealed class InlinePromptText : MonoBehaviour
    {
        private static readonly Regex Word = new Regex(@"\S+");
        private static readonly Regex Mark = new Regex(@"^\{(\w+)\}(.*)$");

        public Font Font;
        public int Size = 28;
        public Color Colour = Color.white;
        /// <summary>The colour of a key drawn as bracketed text, when it has no cap.</summary>
        public Color KeyColour = Color.white;
        public float Width = 500;
        public float LineSpacing = 1.32f;
        /// <summary>Shrink the type until the sentence fits one line, down to this size (0: wrap instead).</summary>
        public int SingleLineFloor;
        /// <summary>Called after the words were laid out again, for a parent that sizes itself on them.</summary>
        public System.Action Rebuilt;

        public float Height { get; private set; }
        public float UsedWidth { get; private set; }
        public int Lines { get; private set; }

        private string _source = "", _action = "", _shown;
        private readonly List<GameObject> _pieces = new List<GameObject>();
        private readonly Stack<Text> _spareWords = new Stack<Text>();
        private readonly Stack<Image> _spareKeys = new Stack<Image>();

        public void Show(string source, string ownAction)
        {
            _source = source ?? ""; _action = ownAction ?? "";
            Refresh();
        }

        private void LateUpdate() => Refresh();

        /// <summary>Lays the words out again when the sentence, the size, the width or a key's label changed.</summary>
        public void Refresh()
        {
            string signature = _source + "|" + Size + "|" + Width + "|" + Labels();
            if (signature == _shown) return;
            _shown = signature;
            int size = Size;
            Build(size);
            while (SingleLineFloor > 0 && Lines > 1 && size > SingleLineFloor) Build(--size);
            Rebuilt?.Invoke();
        }

        private bool Touch => InputLayer.LastInputDevice.Current == InputLayer.InputDeviceKind.Touch;

        private string Labels()
        {
            if (Touch) return "touch";
            var labels = "";
            foreach (Match m in Regex.Matches(_source, @"\{(\w+)\}"))
                labels += Hud.KeyLabelFor(Action(m.Groups[1].Value)) + ",";
            return labels;
        }

        private string Action(string mark) => mark == "KEY" ? _action : mark;

        private void Build(int size)
        {
            foreach (var piece in _pieces)
            {
                piece.SetActive(false);
                if (piece.TryGetComponent<Text>(out var t)) _spareWords.Push(t); else _spareKeys.Push(piece.GetComponent<Image>());
            }
            _pieces.Clear();
            float line = Mathf.Ceil(size * LineSpacing), space = SpaceWidth(size), x = 0, y = 0, widest = 0;
            Lines = 1;
            foreach (Match word in Word.Matches(_source))
            {
                var parts = new List<(string text, Sprite key, bool isKey)>();
                var mark = Mark.Match(word.Value);
                if (mark.Success)
                {
                    string action = Action(mark.Groups[1].Value);
                    if (Touch) parts.Add((mark.Groups[1].Value == "KEY" ? "the ability button" : action.ToUpperInvariant(), null, false));
                    else
                    {
                        string label = Hud.KeyLabelFor(action);
                        var cap = InputGlyphs.For(label, true);
                        parts.Add(cap != null ? ("", cap, true) : ("[" + label + "]", null, true));
                    }
                    if (mark.Groups[2].Value.Length > 0) parts.Add((mark.Groups[2].Value, null, false));
                }
                else parts.Add((word.Value, null, false));

                float wordWidth = 0;
                var placed = new List<(RectTransform rect, float width)>();
                foreach (var (text, key, isKey) in parts)
                {
                    RectTransform rect; float width;
                    if (key != null)
                    {
                        var image = KeyPiece();
                        image.sprite = key;
                        float h = line * .92f;
                        width = h * key.rect.width / Mathf.Max(1, key.rect.height);
                        rect = image.rectTransform; rect.sizeDelta = new Vector2(width, h);
                    }
                    else
                    {
                        var label = WordPiece(size);
                        label.text = text;
                        label.color = isKey ? KeyColour : Colour;
                        width = Mathf.Ceil(label.preferredWidth);
                        rect = label.rectTransform; rect.sizeDelta = new Vector2(width, line);
                    }
                    placed.Add((rect, width));
                    wordWidth += width;
                }
                if (x > 0 && x + wordWidth > Width) { x = 0; y += line; Lines++; }
                foreach (var (rect, width) in placed)
                {
                    rect.anchoredPosition = new Vector2(x, -y - (line - rect.sizeDelta.y) * .5f);
                    x += width;
                }
                widest = Mathf.Max(widest, x);
                x += space;
            }
            Height = _source.Length == 0 ? 0 : y + line;
            UsedWidth = widest;
        }

        private Text WordPiece(int size)
        {
            var text = _spareWords.Count > 0 ? _spareWords.Pop() : null;
            if (text == null)
            {
                var go = new GameObject("Word", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                text = go.AddComponent<Text>();
                text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.alignment = TextAnchor.MiddleLeft;
                TopLeft(text.rectTransform);
            }
            text.font = Font; text.fontSize = size;
            text.gameObject.SetActive(true);
            _pieces.Add(text.gameObject);
            return text;
        }

        private Image KeyPiece()
        {
            var image = _spareKeys.Count > 0 ? _spareKeys.Pop() : null;
            if (image == null)
            {
                var go = new GameObject("Key", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                image = go.AddComponent<Image>();
                image.raycastTarget = false; image.preserveAspect = true;
                TopLeft(image.rectTransform);
            }
            image.gameObject.SetActive(true);
            _pieces.Add(image.gameObject);
            return image;
        }

        private static void TopLeft(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
        }

        private float SpaceWidth(int size)
        {
            var probe = WordPiece(size);
            probe.text = "a a"; float with = probe.preferredWidth;
            probe.text = "aa"; float without = probe.preferredWidth;
            probe.gameObject.SetActive(false);
            _pieces.Remove(probe.gameObject); _spareWords.Push(probe);
            return Mathf.Max(4, with - without);
        }
    }
}
