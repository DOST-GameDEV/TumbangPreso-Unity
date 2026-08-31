using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// In-game Street Achievements Screen (Phase 10).
    /// Displays Bronze, Silver, and Gold achievement shelves, real-time progress bars,
    /// and reward unlocks in hand-carved wood and amber aesthetic.
    /// </summary>
    public sealed class AchievementsScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _root;
        private RectTransform _contentList;

        public void Open()
        {
            if (_root == null) BuildScreen();
            _root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void BuildScreen()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 95;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            _root = new GameObject("AchievementsRoot", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(transform, false);
            var rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;
            _root.GetComponent<Image>().color = new Color(0.08f, 0.04f, 0.02f, 0.94f); // Scrim

            // Main Wood Container Panel
            var panelGo = new GameObject("MainPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panelGo.transform.SetParent(_root.transform, false);
            var pRt = panelGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(1100, 750);
            panelGo.GetComponent<Image>().color = UiTheme.WoodDeep;
            var outline = panelGo.GetComponent<Outline>();
            outline.effectColor = UiTheme.WoodEdge;
            outline.effectDistance = new Vector2(3, -3);

            // Header Banner
            var headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerGo.transform.SetParent(panelGo.transform, false);
            var hRt = headerGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1);
            hRt.sizeDelta = new Vector2(0, 70);
            headerGo.GetComponent<Image>().color = UiTheme.WoodMid;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(headerGo.transform, false);
            var tText = titleGo.GetComponent<Text>();
            tText.font = UiTheme.Font;
            tText.fontSize = 28;
            tText.color = UiTheme.Amber;
            tText.text = "STREET ACHIEVEMENTS & MILESTONES";
            tText.alignment = TextAnchor.MiddleLeft;
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.offsetMin = new Vector2(24, 0);
            tRt.offsetMax = new Vector2(-120, 0);

            // Close Button
            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(headerGo.transform, false);
            var cRt = closeGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1, 0.5f);
            cRt.anchorMax = new Vector2(1, 0.5f);
            cRt.pivot = new Vector2(1, 0.5f);
            cRt.sizeDelta = new Vector2(90, 44);
            cRt.anchoredPosition = new Vector2(-16, 0);
            closeGo.GetComponent<Image>().color = UiTheme.WoodEdge;
            closeGo.GetComponent<Button>().onClick.AddListener(Close);

            var closeLbl = new GameObject("Label", typeof(RectTransform), typeof(Text));
            closeLbl.transform.SetParent(closeGo.transform, false);
            var cTxt = closeLbl.GetComponent<Text>();
            cTxt.font = UiTheme.Font;
            cTxt.fontSize = 18;
            cTxt.color = UiTheme.Cream;
            cTxt.text = "BACK";
            cTxt.alignment = TextAnchor.MiddleCenter;
            var clRt = closeLbl.GetComponent<RectTransform>();
            clRt.anchorMin = Vector2.zero;
            clRt.anchorMax = Vector2.one;
            clRt.sizeDelta = Vector2.zero;

            // Scrollable Content View
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(panelGo.transform, false);
            var sRt = scrollGo.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(16, 16);
            sRt.offsetMax = new Vector2(-16, -80);
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);
            var scroll = scrollGo.GetComponent<ScrollRect>();

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            _contentList = contentGo.GetComponent<RectTransform>();
            _contentList.anchorMin = new Vector2(0, 1);
            _contentList.anchorMax = new Vector2(1, 1);
            _contentList.pivot = new Vector2(0.5f, 1);
            _contentList.sizeDelta = new Vector2(0, 0);

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = _contentList;
            scroll.horizontal = false;
            scroll.vertical = true;
        }

        private void Refresh()
        {
            if (_contentList == null) return;

            foreach (Transform child in _contentList)
                Destroy(child.gameObject);

            var profile = new PlayerProfile(); // Profile lookup fallback

            RenderShelf("GOLD TIER — BARANGAY LEGEND", AchievementTier.Gold, UiTheme.Amber, profile);
            RenderShelf("SILVER TIER — DISTRICT COMPETITOR", AchievementTier.Silver, new Color(0.9f, 0.9f, 0.95f), profile);
            RenderShelf("BRONZE TIER — STREET BASICS", AchievementTier.Bronze, new Color(0.85f, 0.55f, 0.35f), profile);
        }

        private void RenderShelf(string shelfTitle, AchievementTier tier, Color tierColor, PlayerProfile profile)
        {
            var shelfGo = new GameObject($"Shelf_{tier}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            shelfGo.transform.SetParent(_contentList, false);
            shelfGo.GetComponent<Image>().color = UiTheme.WoodMid;

            var vlg = shelfGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(16, 16, 12, 12);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var title = AddText(shelfGo.transform, shelfTitle, 16, tierColor, FontStyle.Bold);

            foreach (var ach in AchievementRules.Tier(tier))
            {
                int progress = AchievementRules.ProgressFor(ach, profile);
                bool unlocked = progress >= ach.TargetCount;

                var rowGo = new GameObject($"Ach_{ach.Id}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                rowGo.transform.SetParent(shelfGo.transform, false);
                rowGo.GetComponent<Image>().color = UiTheme.WoodDeep;

                var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 14;
                hlg.padding = new RectOffset(12, 12, 10, 10);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                // Details
                var detailsGo = new GameObject("Details", typeof(RectTransform), typeof(VerticalLayoutGroup));
                detailsGo.transform.SetParent(rowGo.transform, false);
                var dVlg = detailsGo.GetComponent<VerticalLayoutGroup>();
                dVlg.spacing = 4;
                dVlg.childForceExpandWidth = true;
                var dLe = detailsGo.AddComponent<LayoutElement>();
                dLe.flexibleWidth = 1.0f;

                AddText(detailsGo.transform, ach.Title, 15, tierColor, FontStyle.Bold);
                AddText(detailsGo.transform, ach.Description, 12, UiTheme.Cream);
                AddText(detailsGo.transform, $"Progress: {progress} / {ach.TargetCount}   ·   Reward: {ach.RewardLabel}", 11, unlocked ? UiTheme.MenuGreenLit : UiTheme.CreamMuted);

                // Status Badge
                var statusGo = new GameObject("Status", typeof(RectTransform), typeof(Image));
                statusGo.transform.SetParent(rowGo.transform, false);
                statusGo.GetComponent<Image>().color = unlocked ? UiTheme.MenuGreen : UiTheme.WoodMid;
                var stLe = statusGo.AddComponent<LayoutElement>();
                stLe.preferredWidth = 90;
                stLe.preferredHeight = 36;

                var stTxt = AddText(statusGo.transform, unlocked ? "EARNED" : $"{progress}/{ach.TargetCount}", 13, UiTheme.Cream, FontStyle.Bold);
                stTxt.alignment = TextAnchor.MiddleCenter;
                var stRt = stTxt.GetComponent<RectTransform>();
                stRt.anchorMin = Vector2.zero;
                stRt.anchorMax = Vector2.one;
                stRt.sizeDelta = Vector2.zero;
            }
        }

        private Text AddText(Transform parent, string content, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(parent, false);
            var t = txtGo.GetComponent<Text>();
            t.font = UiTheme.Font;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.text = content;
            return t;
        }
    }
}
