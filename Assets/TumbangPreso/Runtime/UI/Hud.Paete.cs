using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class Hud
    {
        // -------------------------------------------------------------------
        // § THE INTERACT CARD (HERO-9, 2026-09-26)
        //
        // ⚠️⚠️ THE ONE PROMPT FOR THE GENERAL INTERACT VERB, AND BEFORE IT NOTHING ON SCREEN SAID THE
        // VERB EXISTED. The owner asked for *"a general keybind for interact and remove and shit"*
        // (plan.md § 7), and the kit shipped with `CharacterMotor.BreakFreeProgress` and
        // `PullingPlantProgress` read by nothing: a rooted player had seven seconds of hold to find
        // on a key no prompt named. Three states, most urgent first:
        //   * ROOTED: HOLD [key] TO BREAK FREE, the ring the 7 s hold (kept when they let go);
        //   * PULLING: the ring the 1.2 s pull on Paete's seedling;
        //   * NEAR A SEEDLING: HOLD [key] TO PULL IT OUT once it is pullable, or how long it stays
        //     rooted before that (the first 15 s it is untouchable, owner: *"invincible for the first
        //     15 seconds"*), so the player learns the rule by reading it, not by failing.
        //
        // ⚠️ THE RING IS `HudRing`, the family's meter (VISUAL-1): a timer lives on the thing it
        // times, and this one times the player's own hold. The key comes from the live binding
        // (`KeyLabel`), and on a phone the words name the action, never a key (`OnTouch`).
        // ⚠️ CENTRE, LOW, ABOVE THE GET-UP CARD, because a rooted body can still be knocked down and
        // both must be readable at once. The string is rebuilt only when it changes.
        // -------------------------------------------------------------------

        private Image _interactCard;
        private Text _interactLabel;
        private HudRing _interactRing;
        private string _interactShown = "";

        private void BuildInteractCard()
        {
            var group = WoodCard("InteractCard", new Vector2(0.5f, 0.0f), new Vector2(0.0f, 250.0f),
                                 520.0f, out _interactCard, sink: false, border: UiTheme.Offense);
            group.childAlignment = TextAnchor.MiddleCenter;

            var row = new GameObject("InteractRow");
            row.transform.SetParent(group.transform, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.AddComponent<LayoutElement>().minHeight = 56;

            var ringGo = new GameObject("InteractRing");
            ringGo.transform.SetParent(row.transform, false);
            _interactRing = ringGo.AddComponent<HudRing>();
            _interactRing.Thickness = 8; _interactRing.raycastTarget = false;
            _interactRing.color = UiTheme.Offense; _interactRing.Fill = 0;
            var ringBox = ringGo.AddComponent<LayoutElement>();
            ringBox.minWidth = ringBox.preferredWidth = 52; ringBox.minHeight = ringBox.preferredHeight = 52;

            _interactLabel = HudLabel(row.transform, "InteractLabel", 30, UiTheme.Cream, TextAnchor.MiddleLeft);
            _interactLabel.gameObject.AddComponent<LayoutElement>().minHeight = 40.0f;

            _interactCard.gameObject.SetActive(false);
        }

        private string HoldInteract(string what) =>
            (OnTouch ? "HOLD INTERACT" : "HOLD [" + KeyLabel("Interact") + "]") + " " + what;

        private void UpdateInteractPrompt()
        {
            if (_interactCard == null) return;
            string text = null;
            float fill = 0;
            bool live = false;

            if (_local != null && _local.IsRooted)
            {
                fill = _local.BreakFreeProgress;
                live = _local.IsStruggling;
                text = live ? "BREAKING FREE" : HoldInteract("TO BREAK FREE");
                if (OnTouch) InputLayer.TouchHud.Emphasise(Verb.Interact);
            }
            else if (_local != null && _local.PullingPlantProgress > 0)
            {
                fill = _local.PullingPlantProgress;
                live = true;
                text = "PULLING IT OUT";
            }
            else if (_local != null && _local.CanAct())
            {
                PaetePlant near = null; float best = float.MaxValue;
                foreach (var p in PaetePlant.Live)
                {
                    if (p == null || p.OwnerSlot == _local.PlayerSlot || !p.Landed) continue;
                    Vector3 d = p.transform.position - _local.transform.position; d.y = 0;
                    if (d.magnitude <= PaeteRules.PlantPullReach && d.magnitude < best) { best = d.magnitude; near = p; }
                }
                if (near != null)
                {
                    if (near.Pullable)
                    {
                        text = HoldInteract("TO PULL IT OUT");
                        if (OnTouch) InputLayer.TouchHud.Emphasise(Verb.Interact);
                    }
                    else
                    {
                        text = $"ROOTED DEEP  {Mathf.CeilToInt(PaeteRules.PlantRootedSeconds - near.Age)}s";
                        fill = Mathf.Clamp01(near.Age / PaeteRules.PlantRootedSeconds);
                    }
                }
            }

            if (text == null)
            {
                if (_interactCard.gameObject.activeSelf) { _interactCard.gameObject.SetActive(false); _interactShown = ""; }
                return;
            }
            if (!_interactCard.gameObject.activeSelf) _interactCard.gameObject.SetActive(true);
            if (text != _interactShown)
            {
                _interactShown = text;
                _interactLabel.text = text;
            }
            _interactRing.Set(fill);
            _interactRing.Paint(live ? UiTheme.Offense : UiTheme.Amber, new Color(0, 0, 0, .38f));
        }
    }
}
