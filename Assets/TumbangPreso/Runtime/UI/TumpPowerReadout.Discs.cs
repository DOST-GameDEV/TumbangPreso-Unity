using TumbangPreso.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The round abilities (owner, 2026-11-07; `boot_match.disc_powers`): each power is a disc in the kill feed's
    /// plate material, a grey stroke, a woven grey band and a dark centre. The stroke itself carries the state: gold all
    /// round when the power is ready, lime filling clockwise while the ultimate charges, your own colour while a power
    /// runs. A skill cooling down keeps a grey stroke and a light grey fill over its inside that drains from the top.
    /// The seal (`OwnerAbilitySeal`) still holds the state for tests and readers; it no longer draws its ring.
    /// </summary>
    public sealed partial class TumpPowerReadout
    {
        private readonly Image[] _discEdges = new Image[3], _discCools = new Image[3];
        private static readonly Color CoolGrey = new Color32(0xD8, 0xD6, 0xDE, 77);

        private void BuildDisc(int i)
        {
            string kind = i == 2 ? "ult" : "skill";
            var dial = _ownerDials[i];
            dial.Painted = true;
            // The disc picture carries its slab below the circle: 5 units at board size, so the rect reaches past the dial.
            var disc = Hub.HubKit.Stretch(OwnerUiLayout.Rect(dial.transform, "Disc")).gameObject.AddComponent<Image>();
            disc.sprite = LiveHud.Picture("disc-" + kind); disc.raycastTarget = false;
            float d = i == 2 ? 148 : 112;
            disc.rectTransform.offsetMin = new Vector2(0, -dial.rectTransform.rect.height * 5 / d);
            _discEdges[i] = Fill(dial.transform, "DiscEdge", "disc-edge-" + kind, Image.FillMethod.Radial360, (int)Image.Origin360.Top);
            _discCools[i] = Fill(dial.transform, "DiscCooldown", "disc-inner-" + kind, Image.FillMethod.Vertical, (int)Image.OriginVertical.Bottom);
            _discCools[i].color = CoolGrey;
            // Built before the icon and the state, so they draw over the disc.
            disc.transform.SetSiblingIndex(0); _discEdges[i].transform.SetSiblingIndex(1); _discCools[i].transform.SetSiblingIndex(2);
        }

        private static Image Fill(Transform parent, string name, string picture, Image.FillMethod method, int origin)
        {
            var image = Hub.HubKit.Stretch(OwnerUiLayout.Rect(parent, name)).gameObject.AddComponent<Image>();
            image.sprite = LiveHud.Picture(picture); image.raycastTarget = false;
            image.type = Image.Type.Filled; image.fillMethod = method; image.fillOrigin = origin; image.fillClockwise = true;
            image.fillAmount = 0;
            return image;
        }

        private void PaintDisc(int i, HeroAbility skill, HeroKit kit, bool ready, bool running, float runLeft)
        {
            var edge = _discEdges[i]; var cool = _discCools[i];
            if (edge == null) return;
            float shown = _ownerDials[i].ShownFill;
            if (running) { edge.color = _ownerDials[i].Accent; edge.fillAmount = Mathf.Clamp01(runLeft); }
            else if (ready) { edge.color = LiveHud.Yellow; edge.fillAmount = 1; }
            else if (i == 2) { edge.color = LiveHud.Lime; edge.fillAmount = shown; }
            else edge.fillAmount = 0;
            bool cooling = i != 2 && !ready && !running && skill.CooldownRemaining > 0;
            cool.fillAmount = cooling ? Mathf.Clamp01(skill.CooldownRatio) : 0;
        }
    }
}
