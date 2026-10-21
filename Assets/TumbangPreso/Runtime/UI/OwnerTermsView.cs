using System;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>A readable account document with persistent actions; opening it never accepts it.</summary>
    public sealed class OwnerTermsView : MonoBehaviour
    {
        private Canvas _canvas;
        private Action<bool> _closed;
        private ScrollRect _scroll;
        public bool IsOpen => _canvas != null && _canvas.gameObject.activeSelf;

        public static OwnerTermsView Open(Transform owner, Action<bool> closed)
        {
            var node = new GameObject("OwnerTermsView"); node.transform.SetParent(owner, false);
            var view = node.AddComponent<OwnerTermsView>(); view.Build(closed); return view;
        }

        private void Build(Action<bool> closed)
        {
            _closed = closed;
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerTermsCanvas", 950);
            ScreenTakeover.Register(this, () => IsOpen);
            var root = (RectTransform)_canvas.transform;
            var scrim = HubKit.Stretch(HubKit.Rect(root, "ModalBlocker")).gameObject.AddComponent<Image>();
            scrim.color = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, .78f);
            scrim.raycastTarget = true;
            var panel = HubKit.Place(HubKit.Rect(root, "ReadingSheet"), HubKit.Centre, Vector2.zero, new Vector2(1480, 940));
            var face = HubKit.Shape(panel, "DocumentPlate", HubStyle.Night, false, 1501, 6, 26);
            HubKit.Stretch(face.rectTransform); face.raycastTarget = true;
            var title = HubKit.Text(panel, "Title", "TERMS & CONDITIONS", 68, true, HubStyle.Honey);
            HubKit.Place(title.rectTransform, HubKit.TopLeft, new Vector2(52, -30), new Vector2(1376, 100));
            var subtitle = HubKit.Text(panel, "DocumentSummary", "Accounts, fair play and your online experience.", HubStyle.Body, false, HubStyle.Honey);
            HubKit.Place(subtitle.rectTransform, HubKit.TopLeft, new Vector2(56, -132), new Vector2(1030, 60));
            var date = HubKit.Text(panel, "Revision", "23 SEP 2026", HubStyle.Floor, false, HubStyle.Golden, TextAnchor.MiddleRight);
            HubKit.Place(date.rectTransform, HubKit.TopRight, new Vector2(-56, -132), new Vector2(300, 60));
            var rule = HubKit.Rect(panel, "HeaderRule").gameObject.AddComponent<Image>();
            HubKit.Place(rule.rectTransform, HubKit.TopLeft, new Vector2(56, -202), new Vector2(1368, 3));
            rule.color = HubStyle.Golden; rule.raycastTarget = false;

            var content = OwnerScrollColumn.Build(panel, "TermsScroll", new Rect(56, 226, 1368, 560), out _scroll);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 18; layout.padding = new RectOffset(4, 36, 2, 28);
            var scrollbar = _scroll.verticalScrollbar;
            var rail = scrollbar.GetComponent<Image>(); rail.color = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, .2f);
            scrollbar.targetGraphic.color = HubStyle.Golden;
            ((RectTransform)scrollbar.transform).sizeDelta = new Vector2(18, 560);
            _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            var document = Resources.Load<TextAsset>("UI/owner-painted/play-terms");
            string words = document != null ? document.text : "TERMS UNAVAILABLE\nThe document could not be loaded. Please go back and reopen it.";
            foreach (var block in words.Replace("\r", "").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                int split = block.IndexOf('\n');
                string heading = split >= 0 ? block.Substring(0, split) : block;
                var label = HubKit.Text(content, "SectionTitle", heading, HubStyle.Label, true, HubStyle.Golden, TextAnchor.MiddleLeft);
                label.gameObject.AddComponent<LayoutElement>().minHeight = 66;
                if (split < 0) continue;
                var body = HubKit.Text(content, "SectionText", block.Substring(split + 1).Trim(), HubStyle.Floor, false, HubStyle.Honey, TextAnchor.UpperLeft);
                body.lineSpacing = 1.08f;
            }
            var back = HubKit.IconButton(panel, "TermsBack", HubGlyph.Mark.Back, HubStyle.Honey, () => Close(false), 1502);
            HubKit.Place((RectTransform)back.transform, HubKit.BottomLeft, new Vector2(56, 30), new Vector2(106, 106));
            var agree = HubKit.Button(panel, "AcceptGuidelines", "I AGREE", HubStyle.Chartreuse, () => Close(true), HubStyle.Title, 1503);
            HubKit.Place((RectTransform)agree.transform, HubKit.BottomRight, new Vector2(-56, 30), new Vector2(430, 106));
            agree.interactable = document != null && !string.IsNullOrWhiteSpace(document.text);
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            _scroll.verticalNormalizedPosition = 1;
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(scrollbar.gameObject);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (InputLayer.MenuNav.CancelPressed) { ScreenTakeover.ConsumeEscape(); Close(false); }
        }

        private void Close(bool accepted)
        {
            if (!IsOpen) return;
            _canvas.gameObject.SetActive(false); ScreenTakeover.ConsumeEscape();
            _closed?.Invoke(accepted); Destroy(gameObject);
        }

        private void OnDestroy()
        {
            ScreenTakeover.Unregister(this);
            if (_canvas != null) Destroy(_canvas.gameObject);
        }
    }
}
