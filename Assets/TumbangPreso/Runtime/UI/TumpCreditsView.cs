using System;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>New credits composition, consuming unchanged team and licence content.</summary>
    public sealed class TumpCreditsView : MonoBehaviour
    {
        private Canvas _canvas;
        private Action _back;
        public void Open(Transform owner, Action back)
        {
            _back = back;
            if (_canvas == null) Build(owner);
            _canvas.gameObject.SetActive(true); _canvas.GetComponent<ScreenFocus>().Rebuild();
        }
        private void Build(Transform owner)
        {
            var f = TumpUiTheme.Current;
            _canvas = TumpUiFactory.Canvas(owner, "TumpCreditsCanvas", 800);
            var root = (RectTransform)_canvas.transform; TumpUiFactory.Ground(root, f.Cream);
            var back = TumpUiFactory.BackButton(root, "CreditsBack", Close);
            TumpUiFactory.Place((RectTransform)back.transform, 56, 26, 170, 76);
            var logo = TumpUiFactory.Art(root, "OriginalLogo", f.Logo != null ? f.Logo : TumpUiFactory.Sprite("UI/brand/tump_logo"));
            TumpUiFactory.Place(logo.rectTransform, 80, 186, 460, 318);
            var title = TumpUiFactory.Text(root, "Heading", "Made with\na lot of heart", 58, true);
            title.color = f.Brick; TumpUiFactory.Place(title.rectTransform, 96, 552, 438, 216);
            var mark = TumpUiFactory.Art(root, "SlipperMark", f.SlipperMark != null ? f.SlipperMark : TumpUiFactory.Sprite("UI/brand/tsinelas_hit"));
            TumpUiFactory.Place(mark.rectTransform, 220, 796, 242, 206);
            var list = TumpUiFactory.Scroll(root, "Credits", out var scroll);
            list.GetComponent<VerticalLayoutGroup>().padding.bottom = 72;
            var viewport = (RectTransform)scroll.transform;
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = new Vector2(0, 1);
            viewport.offsetMin = new Vector2(650, 66); viewport.offsetMax = new Vector2(1814, -104);
            Heading(list, "The team");
            foreach (var member in CreditsContent.TeamCredits)
            {
                var name = TumpUiFactory.Text(list, "TeamName", member.Name, 38, true);
                name.color = f.Brick; TumpUiFactory.Height(name, 70);
                var role = TumpUiFactory.Text(list, "TeamRole", member.Role, 28);
                TumpUiFactory.Height(role, 92);
            }
            Heading(list, "Artwork & sound");
            foreach (var credit in CreditsContent.CourtesyCredits) Credit(list, credit);
            Heading(list, "Licensed assets");
            foreach (var credit in CreditsContent.CcByCredits) Credit(list, credit);
        }
        private static void Heading(Transform list, string words)
        {
            var text = TumpUiFactory.Text(list, "Section", words, 46, true);
            text.color = TumpUiTheme.Current.Brick; TumpUiFactory.Height(text, 112);
        }
        private static void Credit(Transform list, CreditsContent.Credit item)
        {
            var title = TumpUiFactory.Text(list, "CreditName", item.Chip, 32, true);
            TumpUiFactory.Height(title, 68);
            var body = TumpUiFactory.Text(list, "CreditBody", item.Body, 26);
            body.gameObject.AddComponent<TumpParagraph>();
        }
        private void Close() { _canvas.gameObject.SetActive(false); _back?.Invoke(); }
        private void OnDisable() { if (_canvas != null) _canvas.gameObject.SetActive(false); }
        private void Update()
        {
            if (_canvas == null || !_canvas.gameObject.activeSelf || !MenuNav.CancelPressed || ScreenTakeover.EscapeIsSpoken) return;
            ScreenTakeover.ConsumeEscape(); Close();
        }
    }
}
