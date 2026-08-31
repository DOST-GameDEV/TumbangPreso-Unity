using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// In-game Hero Mastery and Ability Loadout Screen (Phase 10).
    /// Built with authentic Tumbang Preso carved wood and amber gold UI theme.
    ///
    /// Exposes:
    /// - 6 Canonical Heroes (Berto, Sean, Dante, Cheska, Zack, Nemu)
    /// - Ability Slot 1 & Slot 2 sidegrade selectors with stat tradeoffs (+25% Area, -20% Duration)
    /// - Mastery progression track (Levels 1 to 25) with unlocked reward badges
    /// </summary>
    public sealed class HeroLoadoutScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _root;
        private RectTransform _contentList;
        private int _selectedHeroIndex = 0;

        private static readonly string[] Heroes = { "berto", "sean", "dante", "cheska", "zack", "nemu" };
        private static readonly string[] HeroDisplayNames = { "BERTO", "SEAN", "DANTE", "CHESKA", "ZACK", "NEMU" };
        private static readonly string[] HeroRoles = { "TANK / DEFENDER", "SCOUT / RUSHER", "SHOOTER / SNIPER", "PLAYMAKER / TRICKSTER", "BRAWLER / SMASHER", "TACTICIAN / AREA CONTROL" };

        public void Open(int heroIndex = 0)
        {
            _selectedHeroIndex = Mathf.Clamp(heroIndex, 0, Heroes.Length - 1);
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

            _root = new GameObject("HeroLoadoutRoot", typeof(RectTransform), typeof(Image));
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
            pRt.sizeDelta = new Vector2(1100, 720);
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
            tText.text = "HERO MASTERY & ABILITY LOADOUTS";
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

            // Hero Select Tabs Row
            var tabsGo = new GameObject("HeroTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsGo.transform.SetParent(panelGo.transform, false);
            var tbRt = tabsGo.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0, 1);
            tbRt.anchorMax = new Vector2(1, 1);
            tbRt.pivot = new Vector2(0.5f, 1);
            tbRt.anchoredPosition = new Vector2(0, -78);
            tbRt.sizeDelta = new Vector2(-32, 48);
            var hlg = tabsGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            for (int i = 0; i < Heroes.Length; i++)
            {
                int index = i;
                var tabBtnGo = new GameObject($"Tab_{Heroes[i]}", typeof(RectTransform), typeof(Image), typeof(Button));
                tabBtnGo.transform.SetParent(tabsGo.transform, false);
                tabBtnGo.GetComponent<Image>().color = UiTheme.WoodMid;
                var btn = tabBtnGo.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    _selectedHeroIndex = index;
                    Refresh();
                });

                var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                lblGo.transform.SetParent(tabBtnGo.transform, false);
                var lTxt = lblGo.GetComponent<Text>();
                lTxt.font = UiTheme.Font;
                lTxt.fontSize = 16;
                lTxt.color = UiTheme.Cream;
                lTxt.text = HeroDisplayNames[i];
                lTxt.alignment = TextAnchor.MiddleCenter;
                var lRt = lblGo.GetComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.sizeDelta = Vector2.zero;
            }

            // Scrollable Content View
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(panelGo.transform, false);
            var sRt = scrollGo.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(16, 16);
            sRt.offsetMax = new Vector2(-16, -136);
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

            string heroId = Heroes[_selectedHeroIndex];
            string heroName = HeroDisplayNames[_selectedHeroIndex];
            string heroRole = HeroRoles[_selectedHeroIndex];

            // 1. Mastery Status Card
            var masteryCard = CreateCard(_contentList, $"{heroName} — {heroRole}");
            var mDesc = AddText(masteryCard.transform, "Hero Mastery Track (Levels 1 to 25)", 14, UiTheme.Amber);
            var mRewards = AddText(masteryCard.transform, "• Level 3: Title KATUWANG\n• Level 5: Alternate Colors (ALT 1)\n• Level 10: Mastery Crest Badge\n• Level 15: Second Colors (ALT 2)\n• Level 25: Title DALUBHASA", 13, UiTheme.Cream);

            // 2. Ability 1 Loadout Selector
            var a1Card = CreateCard(_contentList, "ABILITY 1: SKILL VARIANT");
            var a1Variants = HeroLoadoutRules.VariantsFor(heroId, 1);
            foreach (var v in a1Variants)
            {
                var row = CreateAbilityRow(a1Card, v);
            }

            // 3. Ability 2 Loadout Selector
            var a2Card = CreateCard(_contentList, "ABILITY 2: SKILL VARIANT");
            var a2Variants = HeroLoadoutRules.VariantsFor(heroId, 2);
            foreach (var v in a2Variants)
            {
                var row = CreateAbilityRow(a2Card, v);
            }
        }

        private GameObject CreateCard(Transform parent, string title)
        {
            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            cardGo.transform.SetParent(parent, false);
            cardGo.GetComponent<Image>().color = UiTheme.WoodMid;

            var vlg = cardGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(16, 16, 12, 12);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            AddText(cardGo.transform, title, 16, UiTheme.Amber, FontStyle.Bold);
            return cardGo;
        }

        private GameObject CreateAbilityRow(GameObject parent, AbilityVariant variant)
        {
            var rowGo = new GameObject($"Ability_{variant.Id}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent.transform, false);
            rowGo.GetComponent<Image>().color = UiTheme.WoodDeep;

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14;
            hlg.padding = new RectOffset(12, 12, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Glyph Icon
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rowGo.transform, false);
            var iImg = iconGo.GetComponent<Image>();
            iImg.sprite = AbilityIcons.For(AbilityGlyph.Zone);
            iImg.color = UiTheme.Amber;
            var iLe = iconGo.AddComponent<LayoutElement>();
            iLe.preferredWidth = 48;
            iLe.preferredHeight = 48;

            // Details Column
            var detailsGo = new GameObject("Details", typeof(RectTransform), typeof(VerticalLayoutGroup));
            detailsGo.transform.SetParent(rowGo.transform, false);
            var dVlg = detailsGo.GetComponent<VerticalLayoutGroup>();
            dVlg.spacing = 4;
            dVlg.childForceExpandWidth = true;
            var dLe = detailsGo.AddComponent<LayoutElement>();
            dLe.flexibleWidth = 1.0f;

            AddText(detailsGo.transform, variant.Name, 15, UiTheme.Amber, FontStyle.Bold);
            AddText(detailsGo.transform, variant.Description, 12, UiTheme.Cream);
            AddText(detailsGo.transform, $"[BUFF] {variant.StatBuff}   ·   [TRADE] {variant.StatDebuff}", 11, UiTheme.Highlight);
            AddText(detailsGo.transform, $"Unlock Challenge: {variant.UnlockChallenge}", 11, UiTheme.CreamMuted);

            // Equip Button
            var equipBtnGo = new GameObject("EquipBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            equipBtnGo.transform.SetParent(rowGo.transform, false);
            equipBtnGo.GetComponent<Image>().color = UiTheme.Amber;
            var eqLe = equipBtnGo.AddComponent<LayoutElement>();
            eqLe.preferredWidth = 90;
            eqLe.preferredHeight = 38;

            var eqTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            eqTxtGo.transform.SetParent(equipBtnGo.transform, false);
            var eqTxt = eqTxtGo.GetComponent<Text>();
            eqTxt.font = UiTheme.Font;
            eqTxt.fontSize = 14;
            eqTxt.color = UiTheme.Ink;
            eqTxt.text = variant.UnlockedByDefault ? "EQUIPPED" : "SELECT";
            eqTxt.alignment = TextAnchor.MiddleCenter;
            var eqRt = eqTxtGo.GetComponent<RectTransform>();
            eqRt.anchorMin = Vector2.zero;
            eqRt.anchorMax = Vector2.one;
            eqRt.sizeDelta = Vector2.zero;

            return rowGo;
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
