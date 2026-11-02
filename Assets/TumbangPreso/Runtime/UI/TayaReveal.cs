using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// TAYA, revealed as each round goes live (owner, 2026-10-22). Live HUD, 2026-11-01 (`boot_match.taya_reveal`):
    /// one big lime word of painted lettering on a soft lime band and, under it, who on a thin tag: the seat's name, or
    /// YOU on a yellow tag when it is this screen's player. Played by `Visual.TayaIdentity` from state every peer has,
    /// so every screen reveals the same player.
    ///   * the letters stamp in from 1.3 times their size with a slight stagger, the band wipes out from the middle;
    ///   * a beat to read, then it fades and rises away.
    /// </summary>
    public sealed class TayaReveal : MonoBehaviour
    {
        private const float Hold = 1.2f, Exit = .3f;
        private static TayaReveal _live;
        /// <summary>How many reveals have played this session. For tests.</summary>
        public static int Played { get; private set; }

        private LiveNotice _notice;
        private RectTransform _tag;
        private float _born, _life;

        public static void Play(int slot)
        {
            var round = GameServices.Round;
            var owner = round != null && round.Lata != null ? round.Lata.transform : null;
            if (owner == null) return;
            if (_live != null) Destroy(_live.gameObject);
            var canvas = OwnerUiLayout.Canvas(owner, "~TayaReveal", 165);
            var focus = canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            var raycaster = canvas.GetComponent<GraphicRaycaster>(); if (raycaster != null) raycaster.enabled = false;
            _live = canvas.gameObject.AddComponent<TayaReveal>(); Played++;
            _live.Build((RectTransform)canvas.transform, slot);
        }

        private void Build(RectTransform root, int slot)
        {
            _born = Time.unscaledTime;
            _notice = LiveNotice.Create(root, "TayaRevealGroup", 104, LiveHud.Lime, LiveHud.TayaInk, LiveHud.TayaInk, 12, 6, 6, LiveHud.Lime, null);
            _notice.MarkLeft.gameObject.SetActive(false); _notice.MarkRight.gameObject.SetActive(false);
            _notice.Title.name = "TayaRevealWord";
            _notice.SetTitle("TAYA");
            _notice.Rest = new Vector2(0, -(130 + 60));
            bool mine = slot == NetAuthority.LocalSlot && !GameLaunch.Spectator;
            _tag = mine ? LiveHud.Tag(_notice.Root, "TayaRevealWho", "YOU", Hub.Knit.ThinYellow, LiveHud.YellowInk, 34)
                        : LiveHud.Tag(_notice.Root, "TayaRevealWho", SeatLabel.Raw(slot), Hub.Knit.ThinYellow, LiveHud.YellowInk, 34);
            _tag.GetComponentInChildren<Text>().name = "TayaRevealName";
            _tag.anchorMin = _tag.anchorMax = _tag.pivot = new Vector2(.5f, .5f);
            _tag.anchoredPosition = new Vector2(0, -102);
            _life = _notice.Duration + Hold + Exit;
            Animate();
        }

        private void Update()
        {
            if (Time.unscaledTime - _born >= _life || GameServices.Round == null) { Destroy(gameObject); return; }
            Animate();
        }

        private void OnDestroy() { if (_live == this) _live = null; }

        private void Animate()
        {
            float t = Time.unscaledTime - _born;
            float exitAt = _life - Exit;
            _notice.Pose(t, t > exitAt ? Mathf.Clamp01((t - exitAt) / Exit) : 0, false);
            // The name pops in once the word has landed.
            float pop = LiveHud.Still ? 1 : LiveHud.Pop((t - _notice.Duration * .7f) / .3f);
            _tag.localScale = Vector3.one * pop;
        }
    }
}
