using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The overlays that are instanced into a screen rather than loaded as one.
    ///
    /// ⚠️⚠️ THEY ARE CONVERTED SCENES NOW, NOT CODE-BUILT REPLACEMENTS. Settings, Tutorial and
    /// Credits were hand-drawn in C# with absolute anchors while the real screens sat unused in
    /// `MapSource/scenes_ui`, which is how the settings overlay ended up as five labels stacked
    /// on the same pixel with no sliders, no keybind rows and no scroll. `main_menu.gd`
    /// instances all three into the title screen and shows them in place, and so does this.
    ///
    /// ⚠️ SHOWN IN PLACE, NOT SWITCHED TO. A scene change would tear down and rebuild the title
    /// screen behind a panel the player is about to close in a few seconds.
    /// </summary>
    public abstract class ConvertedOverlay : ConvertedScreen
    {
        /// <summary>Raised when the panel's own BACK is pressed, so the screen can re-unfurl.</summary>
        public event Action BackPressed;

        protected void RaiseBack() => BackPressed?.Invoke();

        public virtual void Close()
        {
            gameObject.SetActive(false);
            RaiseBack();
        }

        /// <summary>
        /// ⚠️ EVERY PANEL IN THE GAME HAS A WORKING ESC. It is in the Godot build's own
        /// checklist as U-7, and a panel you can only leave with the mouse is the one that
        /// strands a player who opened it with the keyboard.
        /// </summary>
        protected virtual void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            MenuSfx.Back();
            Close();
        }

        /// <summary>
        /// A chip-and-body row: a sunken wood slot holding the term, and the explanation beside
        /// it. Shared by the tutorial and the credits so a player who has seen one reads the
        /// other the same way, which is why `credits_panel.gd` copies Tutorial's CHIP_WIDTH.
        /// </summary>
        protected static GameObject Row(Transform parent, string chip, string body)
        {
            var rowGo = new GameObject("Row");
            rowGo.AddComponent<RectTransform>();
            rowGo.transform.SetParent(parent, false);

            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.childControlHeight = true;
            row.childControlWidth = true;
            row.childForceExpandHeight = false;
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.UpperLeft;
            row.spacing = 16.0f;

            var slotGo = new GameObject("Slot");
            slotGo.AddComponent<RectTransform>();
            slotGo.transform.SetParent(rowGo.transform, false);
            slotGo.AddComponent<Image>();

            var slotGroup = slotGo.AddComponent<VerticalLayoutGroup>();
            slotGroup.childControlHeight = true;
            slotGroup.childControlWidth = true;
            slotGroup.childForceExpandHeight = false;
            slotGroup.childForceExpandWidth = true;

            var skin = slotGo.AddComponent<GodotPanel>();
            skin.Variation = "WoodSlot";

            var slotElement = slotGo.AddComponent<LayoutElement>();
            slotElement.preferredWidth = TutorialContent.ChipWidth;
            slotElement.flexibleWidth = 0.0f;

            var chipText = MenuKit.Styled(slotGo.transform, "MenuValue", chip, TextAnchor.MiddleCenter);
            chipText.raycastTarget = false;

            var bodyText = MenuKit.Styled(rowGo.transform, "MenuBody", body, TextAnchor.UpperLeft);
            bodyText.raycastTarget = false;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var bodyElement = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyElement.flexibleWidth = 1.0f;

            return rowGo;
        }

        protected static Text Heading(Transform parent, string words)
        {
            var text = MenuKit.Styled(parent, "MenuHeading", words, TextAnchor.MiddleLeft);
            text.raycastTarget = false;
            return text;
        }

        protected static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
