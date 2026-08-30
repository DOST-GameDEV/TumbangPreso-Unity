using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Who you are, in the corner of the title screen. One chip, and it is the way in.
    ///
    /// ⚠️⚠️ IT REPLACES TWO FLOATING WOOD BUTTONS AND 🧑 ASKED WHY THEY WERE THERE BY NAME:
    /// *"look wtf why are these buttons here"*, over a screenshot of CAREER and ACCOUNT sitting
    /// in the middle of the street in a completely different visual language from the big green,
    /// yellow and blue arrow buttons beside them. They were there because `AccountOverlay` and
    /// `ProfileOverlay` each installed their own button from their own canvas at their own hard
    /// coded offset, so nothing on the menu knew about either of them and they landed wherever
    /// the two numbers said. **A screen that grows a button per feature ends up looking like a
    /// debug bar**, and that is exactly what it looked like.
    ///
    /// ⚠️⚠️ AND IT IS A NAMEPLATE RATHER THAN A BUTTON BECAUSE THAT IS WHAT THE REFERENCE DOES.
    /// Every game 🧑 pointed at puts identity in a corner as a thing to LOOK at, with the name,
    /// the level and the bar on it, and makes that the click target. It states something before
    /// it offers something, which no button labelled ACCOUNT can do.
    ///
    /// ⚠️ THE LEVEL AND THE BAR ARE HIDDEN UNTIL THERE IS XP. Every account is level 1 the moment
    /// it exists, so drawing "LEVEL 1" and an empty bar on a fresh install advertises a system
    /// the player has not touched yet. Same argument the rank badge is still absent for.
    /// </summary>
    public sealed class PlayerNameplate : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _name;
        private Text _level;
        private Image _xpFill;
        private RectTransform _bar;
        private PlayerHub _hub;

        /// <summary>
        /// The one line that appears when this machine's progress is not attached to anything.
        ///
        /// ⚠️⚠️ IT IS THE UPGRADE OFFER, AND THE OFFER USED TO FORCE THE WHOLE ACCOUNT PANEL
        /// OPEN ON THE TITLE SCREEN. `AccountOverlay.Install` ended with
        /// `if (account.ShouldOfferUpgrade) OpenOffer()`, so a player who had just earned
        /// something arrived at the menu and was handed a form with six fields and a password box
        /// on it, unasked. That is exactly the thing 🧑 named: *"usually u dont open up login in
        /// the actual game screen yet"*.
        ///
        /// ⚠️⚠️ AND DELETING THE PANEL WOULD HAVE DELETED THE OFFER WITH IT, which is the
        /// quiet way a rebuild loses a feature: `ShouldOfferUpgrade` had no other reader. Phase 1
        /// built it for a real reason and the reason has not changed. **Anonymous credentials live
        /// in the UGS cache and a player who clears them is gone forever**, so somebody has to be
        /// told, once, at the moment they have something worth losing.
        ///
        /// ⚠️ A LINE ON THE NAMEPLATE IS THE WHOLE OF IT. It interrupts nothing, it is where the
        /// player already looks to see who they are, and pressing the plate they were going to
        /// press anyway lands on the tab that fixes it.
        /// </summary>
        private Text _offer;

        /// <summary>
        /// Every overlay panel on this screen, found once, watched every frame.
        ///
        /// ⚠️⚠️ THE PLATE HIDES ITSELF RATHER THAN WAITING TO BE TOLD, AND TWO TESTS FORCED
        /// THAT. The first fix hooked `ConvertedMainMenu.Overlay`'s click handler, which works
        /// when a player presses SETTINGS and does nothing at all when anything else opens the
        /// panel. `SettingsWheelProbe` and the reachability probe both do exactly that: they find
        /// the panel and activate it directly, and both stayed red.
        ///
        /// ⚠️⚠️ **A CHROME ELEMENT THAT DEPENDS ON EVERY OPENER REMEMBERING TO NOTIFY IT IS
        /// AN ELEMENT THAT WILL BLOCK SOMETHING EVENTUALLY.** There are two overlays today and
        /// the next one added would have re-broken this silently, because the failure is a
        /// swallowed click in one corner rather than an error. Watching the panels is three
        /// bools a frame and cannot be forgotten.
        ///
        /// ⚠️ THE LIST IS BUILT ONCE. `FindObjectsByType` per frame is the shape `Hud`'s
        /// per-frame string rebuild took an eighth of a probe's frames with.
        /// </summary>
        private ConvertedOverlay[] _overlays = System.Array.Empty<ConvertedOverlay>();

        private bool _hubOpen;

        public void Install()
        {
            if (_canvas != null) return;

            _canvas = MenuKit.BuildCanvas(transform, "NameplateCanvas");

            // ⚠️ BELOW THE HUB AND ABOVE THE MENU. It is chrome on the title screen, not a
            // takeover, and `Update` is what stops it covering a panel. See `SetVisible`.
            _canvas.sortingOrder = 480;

            _hub = gameObject.GetComponent<PlayerHub>();
            if (_hub == null) _hub = gameObject.AddComponent<PlayerHub>();
            _hub.Install();

            // ⚠️ THE PLATE HIDES WHILE THE HUB IS UP. See `PlayerHub.VisibleChanged`: the hub
            // header draws the same handle in the same corner at twice the size, and the first
            // render of these screens showed both at once.
            _hub.VisibleChanged += visible => _hubOpen = visible;

            _overlays = UnityEngine.Object.FindObjectsByType<ConvertedOverlay>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            OfferTheAccountChoiceOnce();

            // ⚠️ TOP LEFT, WHICH IS WHERE THE MENU HAS ROOM. The title art and the pennant column
            // own the left-centre and the bottom, and the version stamp owns the bottom right.
            // This is measured against the shipped menu rather than picked: the two buttons it
            // replaces were at (-118, -42) and (-320, -42) from the top RIGHT, which is where the
            // street's brightest building is and why they read as floating.
            var go = new GameObject("Nameplate", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_canvas.transform, false);
            MenuKit.Place((RectTransform)go.transform, new Vector2(0.0f, 1.0f),
                new Vector2(232.0f, -64.0f), new Vector2(412.0f, 92.0f));

            go.GetComponent<Image>().color = UiTheme.WoodDeep;

            var skin = go.AddComponent<GodotPanel>();
            skin.Variation = "WoodPanel";
            skin.ApplyContentMargins = false;
            skin.Apply();

            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(Press);

            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(go.transform, false);
            MenuKit.Place((RectTransform)portrait.transform, new Vector2(0.0f, 0.5f),
                new Vector2(46.0f, 0.0f), new Vector2(56.0f, 56.0f));
            portrait.GetComponent<Image>().color = UiTheme.WoodMid;
            portrait.GetComponent<Image>().raycastTarget = false;

            var slot = portrait.AddComponent<GodotPanel>();
            slot.Variation = "WoodSlot";
            slot.ApplyContentMargins = false;
            slot.Apply();

            _name = MenuKit.Label(go.transform, "", 22, UiTheme.Cream, new Vector2(0.0f, 0.5f),
                new Vector2(238.0f, 16.0f), new Vector2(272.0f, 30.0f), TextAnchor.MiddleLeft);
            _name.raycastTarget = false;

            _offer = MenuKit.Label(go.transform, "", MenuKit.MinReadableUnits, UiTheme.Amber,
                new Vector2(0.0f, 0.5f), new Vector2(250.0f, -18.0f), new Vector2(296.0f, 24.0f),
                TextAnchor.MiddleLeft);
            _offer.raycastTarget = false;

            _level = MenuKit.Label(go.transform, "", MenuKit.MinReadableUnits, UiTheme.Amber,
                new Vector2(0.0f, 0.5f), new Vector2(140.0f, -18.0f), new Vector2(92.0f, 24.0f),
                TextAnchor.MiddleLeft);
            _level.raycastTarget = false;

            var track = new GameObject("XpTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(go.transform, false);
            _bar = (RectTransform)track.transform;
            MenuKit.Place(_bar, new Vector2(0.0f, 0.5f), new Vector2(288.0f, -18.0f),
                new Vector2(180.0f, 6.0f));
            track.GetComponent<Image>().color = UiTheme.WoodDark;
            track.GetComponent<Image>().raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(track.transform, false);
            _xpFill = fillGo.GetComponent<Image>();
            _xpFill.color = UiTheme.Amber;
            _xpFill.raycastTarget = false;

            var fill = _xpFill.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.0f, 1.0f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            if (GameServices.Account != null) GameServices.Account.Changed += Refresh;
            if (GameServices.Career != null) GameServices.Career.Changed += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (GameServices.Account != null) GameServices.Account.Changed -= Refresh;
            if (GameServices.Career != null) GameServices.Career.Changed -= Refresh;
        }

        /// <summary>
        /// ⚠️ THE PLATE IS VISIBLE ONLY WHEN NOTHING ELSE IS. See `_overlays`: it answers the
        /// question itself every frame rather than trusting whoever opened a panel to say so.
        /// </summary>
        private void Update()
        {
            if (_canvas == null) return;

            bool covered = _hubOpen;

            for (int i = 0; !covered && i < _overlays.Length; i++)
                if (_overlays[i] != null && _overlays[i].gameObject.activeInHierarchy) covered = true;

            if (_canvas.enabled == covered) _canvas.enabled = !covered;
        }

        /// <summary>
        /// ⚠️ THE OFFER DECIDES WHICH TAB OPENS, AND IT IS MARKED AS SHOWN HERE RATHER THAN ON
        /// SIGHT. `MarkUpgradeOfferShown` retires the prompt; calling it from `Refresh` would
        /// retire it the first frame the menu drew, whether or not anybody looked. It is spent
        /// when the player acts on it.
        /// </summary>
        /// <summary>
        /// Shows the account screen the first time this machine reaches the menu, and never again.
        ///
        /// ⚠️⚠️ IT LIVES HERE BECAUSE THIS IS ALREADY THE ONE ENTRY POINT. `ConvertedMainMenu`
        /// installs the nameplate, the nameplate installs the hub and the hub installs the
        /// sign-in screen, and § 92.4's whole argument is that a screen must never again grow a
        /// door nothing knows about. Opening it from the menu instead would be a second opener
        /// for a screen that already has an owner.
        ///
        /// ⚠️⚠️ AND IT IS GATED ON A SETTING RATHER THAN ON "HAS NO PASSWORD", WHICH IS THE TRAP
        /// IN THIS FEATURE. Keying it off the account state would show the screen on every launch
        /// to every player who chose to stay a guest, which is the same nag PUBG's is not, and it
        /// would make CONTINUE AS GUEST a button that does nothing lasting.
        /// `GameSettings.AccountChoiceMade` records the ANSWER, not the outcome.
        ///
        /// ⚠️ IT IS SILENT IF THE SCREEN IS NOT THERE. A boot path that throws because a screen
        /// failed to build is worse than a boot path that skips a question.
        /// </summary>
        private void OfferTheAccountChoiceOnce()
        {
            // ⚠️⚠️ A SCENE LOAD IS NOT A BOOT, AND GATING ON THE NAMEPLATE ALONE BLOCKED THE
            // WHOLE SETTINGS PANEL. `SceneFlow.BootedThroughSplash` carries the full note: the
            // menu is reached from the splash, from `LeaveMatchToMainMenu` and from any probe
            // that loads it by name, and only the first has a first-time player behind it.
            if (!SceneFlow.BootedThroughSplash) return;

            var settings = Settings.SettingsStore.Current;
            if (settings == null || settings.AccountChoiceMade) return;
            if (_hub == null) return;

            var signIn = _hub.GetComponent<SignInScreen>();
            if (signIn == null) return;

            signIn.OpenAtBoot();
        }

        private void Press()
        {
            var account = GameServices.Account;
            bool offering = account != null && account.ShouldOfferUpgrade;

            _hub.Open(offering);

            if (offering)
            {
                account.MarkUpgradeOfferShown();
                Refresh();
            }
        }

        /// <summary>
        /// Shows or hides the plate.
        ///
        /// ⚠️⚠️ IT IS PUBLIC BECAUSE THE PLATE BLOCKED THE SETTINGS PANEL, AND TWO TESTS
        /// CAUGHT IT. `EveryButtonIsReachable` reported eight settings controls *"blocked by
        /// MainMenuCanvas/NameplateCanvas/Nameplate"* and `TheWheelScrollsTheSettingsListFromEveryPartOfIt`
        /// found the wheel swallowed at one of its forty-five sample points, over the same
        /// object. **The plate is a clickable rect on its own canvas at sorting order 60**, and
        /// every converted panel on this screen sits below that, so it covered the top left
        /// corner of all of them.
        ///
        /// ⚠️⚠️ THE FIX IS NOT A LOWER SORTING ORDER. Put it under the panels and it is
        /// covered by the menu backdrop instead, which is the same bug pointing the other way.
        /// **A persistent chrome element has to be told when a screen takes over**, which is
        /// exactly what the hub already does through `PlayerHub.VisibleChanged`;
        /// `ConvertedMainMenu` now does the same for SETTINGS and CREDITS.
        ///
        /// ⚠️ `Canvas.enabled` RATHER THAN `SetActive`, so the built hierarchy survives and
        /// nothing is rebuilt when it comes back.
        ///
        /// ⚠️ IT IS STILL PUBLIC, BUT `Update` IS THE AUTHORITY. A caller can force it down for
        /// a frame; the next frame the plate decides for itself. That is deliberate: one owner of
        /// the answer, and a caller that forgets to switch it back cannot leave it hidden.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.enabled = visible;
        }

        private void Refresh()
        {
            var account = GameServices.Account;
            var profile = GameServices.Career?.Profile;
            if (_name == null) return;

            _name.text = account != null ? account.LobbyName : "PLAYER";

            int xp = profile?.Xp ?? 0;
            bool earned = xp > 0;
            bool offering = account != null && account.ShouldOfferUpgrade;

            // ⚠️ THE OFFER AND THE LEVEL SHARE ONE ROW, and the offer wins it. They cannot both
            // be true for long: the offer only fires once a player has earned something, and the
            // whole point of the line is that it is the more urgent of the two facts. Stacking
            // them would need a taller plate on a menu that has no room for one.
            _offer.text = offering ? "SECURE YOUR PROGRESS" : "";
            _level.text = earned && !offering ? $"LV {ProgressionRules.LevelForXp(xp)}" : "";
            if (_bar != null) _bar.gameObject.SetActive(earned && !offering);

            if (!earned) return;

            float into = ProgressionRules.XpIntoLevel(xp) / (float)ProgressionRules.XpPerLevel;
            _xpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(into), 1.0f);
        }
    }
}
