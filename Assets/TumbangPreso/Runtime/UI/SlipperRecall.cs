using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// § THE RECALL MARK. Where the tsinelas you just threw is, and which control fetches it.
    ///
    /// 🧑 2026-09-19, with a reference frame of a ringed button cap sitting on a target:
    /// *"i need you to create a prompt where the image icon will pop up in a player's screen
    /// after their slippers get thrown. this should have a tracking logic based on the tsinelas
    /// location."*
    ///
    /// ⚠️⚠️ THE GAME'S THESIS IS THE RETRIEVAL AND NOTHING ON SCREEN WAS ABOUT IT UNTIL NOW.
    /// `docs/VISION.md` § 0 and `Slipper`'s own class note both say the tension is getting your
    /// tsinelas back, not the throw, and the whole of what the HUD said about that was a screen
    /// EDGE arrow that appeared only once the shoe had already left the frame plus a
    /// `PICK UP` line that appears only once you are standing on it. Between those two, which is
    /// most of every retrieval, the player was told nothing at all.
    ///
    /// ⚠️⚠️ IT IS ONE MARK AND IT REPLACED THE EDGE ARROW RATHER THAN JOINING IT.
    /// `OffscreenIndicators` drew a `▲` for your own slipper on the screen edge and hid it the
    /// moment the shoe came into frame, so the two would have been two answers to one question
    /// with a seam in the middle. `CLAUDE.md` § 6.3: *"NEVER ADD A SECOND DOOR TO FIX A
    /// FINDABILITY PROBLEM."* This mark owns both halves: it sits ON the tsinelas while the
    /// tsinelas is on screen and clamps to the edge with a chevron when it is not, and the
    /// projection both states use is <see cref="ScreenTrack"/>, shared with the lata arrow so
    /// there is one answer to *"where on this canvas does a marker for that world point go"*.
    ///
    /// ⚠️⚠️ THE CAP IS THE LIVE BINDING, NEVER A LITERAL, AND IT IS NOTHING AT ALL ON TOUCH.
    /// `docs/VISION.md` § 3: *"a screen that teaches the wrong key is worse than one that
    /// teaches none"*. The label comes from `Hud.KeyLabelFor`, which resolves the bound control
    /// per device and invalidates on `Rebinding.Revision`, and the picture comes from
    /// `InputGlyphs`, so a rebind and a pad picked up mid-match both follow. On a phone there is
    /// no key to draw, so the ring carries no cap and `TouchHud.Emphasise` pulses the GRAB
    /// button instead, which is `Hud`'s own § A PROMPT ON A PHONE NAMES THE ACTION rule.
    ///
    /// ⚠️ IT IS YOURS AND ONLY YOURS. The mark reads `Slipper.OwnerSlot` against the local seat,
    /// which is the same question the owner glow asks, so a second seat's tsinelas never draws
    /// one. That is deliberate and it is the same call as § THE OWNERSHIP LOCK in `Slipper`:
    /// under that rule somebody else's shoe is not ammunition you can ever use, and a marker
    /// over it would be pointing at a thing the grab is going to refuse.
    ///
    /// ⚠️ IT IS PER-PEER AND NOTHING ABOUT IT CROSSES THE WIRE. Everything it reads (the state,
    /// the owner, the position) is already replicated, so it works identically on a client and
    /// on the host. ⚠️⚠️ **THAT IS NOT TRUE OF THE LANDED RIM AND IT IS WHY THIS EXISTS AS A HUD
    /// OBJECT RATHER THAN AS MORE SHADER WORK**: `Slipper.Land` is the only thing that can light
    /// that rim and it is inside a `NetAuthority.ShouldResolve()` gate, so for the whole life of
    /// the feature a non-host player's own landed tsinelas never lit at all.
    /// </summary>
    public sealed class SlipperRecall : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ THE MARK IS THE SAME SIZE AT EVERY RANGE, AND SCALING IT WITH DISTANCE WAS
        /// REJECTED BEFORE IT WAS WRITTEN. A marker that shrinks as the thing gets further away
        /// is least readable exactly when it is the only thing telling you where your ammunition
        /// went. `OffscreenIndicators` already made this call for the arrows: they are a fixed
        /// 58 x 66 at every resolution.
        /// </summary>
        public const float MarkSize = 132.0f;

        /// <summary>Outer radius of the ring, in canvas units.</summary>
        public const float RingRadius = 44.0f;

        /// <summary>
        /// The cap inside the ring. 46 units square against a 44 unit radius, so the glyph fills
        /// a little over half the ring's diameter and the ring still reads as a ring rather than
        /// as a border on a button.
        /// </summary>
        public const float CapSize = 46.0f;

        private RectTransform _canvasRect;
        private RectTransform _mark;
        private SlipperRecallMark _ring;
        private Image _cap;
        private Text _capText;

        private string _capLabel = "";
        private int _capBindings = -1;
        private int _capDevice = -1;

        /// <summary>
        /// ⚠️ ITS OWN CANVAS, AT THE ARROWS' ORDER, FOR `OffscreenIndicators`' OWN REASON. This
        /// is a world-tracking marker and it has to draw over the match chrome rather than
        /// underneath a status row that happens to be built later. `HudOverflowProbe` already
        /// records that the HUD is not one canvas for exactly this.
        /// </summary>
        public void Build(Transform owner)
        {
            var canvas = TumpUiFactory.Canvas(owner, "TumpRecallCanvas", 111);
            _canvasRect = (RectTransform)canvas.transform;

            // ⚠️ THE FOCUS RIG COMES OFF, AS IT DOES ON THE ARROWS' CANVAS. `MenuKit.BuildCanvas`
            // and `TumpUiFactory.Canvas` install `ScreenFocus` so a screen added later cannot
            // forget a pad path (`CLAUDE.md` § 4a); a marker is not a screen and has nothing to
            // focus, and leaving it armed puts an invisible selectable in the middle of a match.
            var focus = canvas.GetComponent<InputLayer.ScreenFocus>();
            if (focus != null) focus.enabled = false;

            _mark = TumpUiFactory.Rect(_canvasRect, "RecallMark");
            TumpUiFactory.Anchor(_mark, new Vector2(0.5f, 0.5f), Vector2.zero,
                                 new Vector2(MarkSize, MarkSize));

            _ring = _mark.gameObject.AddComponent<SlipperRecallMark>();
            _ring.raycastTarget = false;

            _cap = TumpUiFactory.Art(_mark, "RecallKeyCap", null);
            TumpUiFactory.Anchor(_cap.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                                 new Vector2(CapSize, CapSize));
            _cap.raycastTarget = false;
            _cap.enabled = false;

            // ⚠️ THE TEXT IS THE FALLBACK AND IT IS NEVER REMOVED, which is `InputGlyphs`' own
            // rule: *"a control with no glyph draws exactly what it draws today, so this can only
            // ever improve a prompt and can never blank one."* A control the sheets do not carry
            // still names itself here.
            _capText = TumpUiFactory.Text(_mark, "RecallKeyLabel", "", 26, bold: true);
            TumpUiFactory.Anchor(_capText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                                 new Vector2(CapSize * 2.2f, CapSize));
            _capText.alignment = TextAnchor.MiddleCenter;
            _capText.color = TumpUiTheme.Current.Cream;
            _capText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _capText.enabled = false;

            _mark.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (_canvasRect != null) _canvasRect.gameObject.SetActive(visible);
        }
        private void HideMarker(){if(_mark!=null)_mark.gameObject.SetActive(false);}
        private void OnDisable()=>HideMarker();
        private void LateUpdate()
        {
            if(PresentationClock.Held||ScreenTakeover.AnyOpen)HideMarker();
        }

        /// <summary>
        /// Whether the mark is on screen right now, for anything that has to check.
        ///
        /// ⚠️ IT EXISTS BECAUSE THE CANVAS IS A SCENE ROOT AND NOT A CHILD OF THE HUD.
        /// `TumpUiFactory.Canvas` builds every canvas in this front end unparented and binds its
        /// lifetime with `CanvasLifetime` instead, so a probe walking down from the HUD's own
        /// transform finds nothing and concludes the feature was never built. Asking the
        /// component is the answer that cannot be wrong about where its own objects live.
        /// </summary>
        public bool Drawing => _mark != null && _mark.gameObject.activeInHierarchy
                               && _canvasRect != null && _canvasRect.gameObject.activeInHierarchy;

        /// <summary>
        /// Once a frame from the HUD, with the local unit and the tsinelas that answers to it.
        ///
        /// ⚠️⚠️ THE MARK IS UP FROM THE MOMENT THE SHOE LEAVES THE HAND, NOT FROM THE MOMENT IT
        /// LANDS, AND THE IN-FLIGHT HALF IS NOT DECORATION. `AIController.MySlipper`'s note
        /// measured what leaving it out costs a player who cannot see their own throw land:
        /// *"a version that only considered loose slippers made a bot's slipper invisible to it
        /// the instant it was released: measured throws 27 → 14"*. A person in first person has a
        /// narrower cone than that bot does.
        ///
        /// ⚠️ THE CAP ONLY APPEARS ONCE THE GRAB WOULD ACTUALLY TAKE IT. In flight the ring is
        /// drawn alone: there is nothing to press yet, and a button cap over a shoe still in the
        /// air is the prompt promising something the carrier would refuse. The state clause it
        /// asks is `Slipper.IsGrabbableIgnoringReach`, the same function the grab itself asks,
        /// for the reason `Hud.UpdatePickupPrompt` already records: *"a prompt derived from its
        /// own distance check is a second answer to one question."*
        /// </summary>
        public void Track(CharacterMotor local, Slipper mine)
        {
            if(PresentationClock.Held||ScreenTakeover.AnyOpen){HideMarker();return;}
            if (_mark == null) return;

            var cam = UnityEngine.Camera.main;

            bool show = cam != null && local != null && mine != null
                        && !local.IsDefender
                        && mine.gameObject.activeInHierarchy
                        && mine.OwnerSlot == local.PlayerSlot
                        && !(mine.State == SlipperState.Held && mine.Holder == local);

            // ⚠️⚠️ THE MARK GOES OUT THE MOMENT THE GRAB IS IN REACH, AND THE RENDER IS WHAT
            // FORCED THAT. The first build had an "in range" state, a thicker yellow ring, and
            // the frame of it came back with that ring drawn straight through the middle card of
            // the ability deck. That is `CLAUDE.md` § 6.2b's fourth row exactly: *"WITH EVERY
            // ALWAYS-ON PIECE OF CHROME STILL LIVE. Chrome does not know about a screen added
            // after it."* `SlipperRecallShots` asserts the absence now, so the hand-off is a
            // thing that can fail rather than a thing this comment claims.
            //
            // ⚠️⚠️ AND IT COSTS ALMOST NOTHING, WHICH IS ARITHMETIC RATHER THAN TASTE. The FPP
            // camera sits about 1.55 m above the road at `CameraRig.FppEyeHeight` and its field
            // of view is 95 degrees, so a tsinelas on the ground leaves the bottom of the frame
            // (with this mark's own radius reserved) at about **1.69 m**, and
            // `Balance.PickupRadius` is **1.75 m**. The two are within six centimetres of each
            // other: for the whole time the shoe is out of reach the mark can sit ON it, and the
            // only range at which it would have to clamp downward is the range at which it has
            // nothing left to say.
            //
            // ⚠️ SOMETHING ELSE IS ALREADY SAYING IT THERE. `TumpMatchReadout.Prompts` prints
            // `[X] Pick up` and pulses the touch GRAB button off the same `CanBeGrabbedBy` this
            // asks, under the crosshair, which is where this HUD puts an answer about the thing
            // you are looking at. Two markers for one question is what § 6.3 forbids, and the
            // one that has to go is the one that is in the wrong place.
            if (show && mine.CanBeGrabbedBy(local)) show = false;

            if (!show)
            {
                if (_mark.gameObject.activeSelf) _mark.gameObject.SetActive(false);
                return;
            }

            // ⚠️ THE CAMERA'S OWN PIXELS, NOT `Screen`. `ScreenTrack.Project`'s note has the
            // argument: the two agree in play and disagree under every render probe.
            var viewport = new Vector2(cam.pixelWidth, cam.pixelHeight);
            Vector2 canvas = _canvasRect != null ? _canvasRect.rect.size : viewport;

            // ⚠️ THE MARGIN CARRIES THE RING'S OWN RADIUS, which the arrows' margin does not need
            // to: a glyph half off the edge is ugly, and a RING half off the edge has stopped
            // being a ring. A target inside that band counts as off frame and the mark clamps.
            //
            // ⚠️ IT IS ALSO WHAT MAKES THE HAND-OFF ABOVE LAND WHERE IT DOES, so the two numbers
            // are related rather than independent: reserving the radius is what moves the
            // frame-exit distance to about 1.69 m, which is where `Balance.PickupRadius` already
            // is. Widening this band pushes the mark out of the frame while it still has
            // something to say.
            var track = ScreenTrack.Project(cam,
                                            mine.transform.position + ScreenTrack.ChestHeight,
                                            canvas, viewport,
                                            OffscreenIndicators.EdgeMargin + RingRadius);

            if (!track.Visible)
            {
                if (_mark.gameObject.activeSelf) _mark.gameObject.SetActive(false);
                return;
            }

            if (!_mark.gameObject.activeSelf) _mark.gameObject.SetActive(true);
            _mark.anchoredPosition = track.Clamped ? ClearOfTheDeck(track.Anchored, canvas)
                                                   : track.Anchored;

            _ring.Bearing = track.Clamped ? track.Bearing : (float?)null;
            _ring.SetVerticesDirty();

            PaintCap(mine.IsGrabbableIgnoringReach(local));
        }

        /// <summary>
        /// Lifts a mark clamped to the bottom edge above the ability deck.
        ///
        /// ⚠️⚠️ THE RENDER IS WHY THIS EXISTS AND NOTHING ELSE COULD HAVE FOUND IT.
        /// `recall-4-clamped.png` put the ring through *"Hold TAB for skills"* and between two
        /// ability cards: a tsinelas directly behind you clamps to the BOTTOM CENTRE, which is
        /// the one part of this screen that is already full. `CLAUDE.md` § 6.2b's fourth row is
        /// this exactly, and its own lesson is the general one: *"chrome does not know about a
        /// screen added after it"*, so the screen added after it has to ask.
        ///
        /// ⚠️ IT ASKS `TumpPowerReadout` RATHER THAN CARRYING ITS OWN COPY OF THE DECK'S RECT,
        /// including whether the deck is drawn at all: it is switched off for a seat with no hero
        /// kit, which is every seat in Classic, and dodging a rectangle nobody can see would put
        /// the mark in the wrong place for half the game.
        ///
        /// ⚠️ ONLY THE HEIGHT MOVES. The X stays exactly where the clamp put it, so the chevron
        /// still points along the true bearing to the tsinelas; what changes is the one thing
        /// that was making it unreadable.
        /// </summary>
        private Vector2 ClearOfTheDeck(Vector2 at, Vector2 canvas)
        {
            if (_deck == null) _deck = FindFirstObjectByType<TumpPowerReadout>();
            if (_deck == null || !_deck.DeckVisible) return at;

            // ⚠️ THE CHEVRON'S REACH IS IN BOTH BOUNDS, AND LEAVING IT OUT OF THE HEIGHT WAS
            // VISIBLE IN THE VERY NEXT RENDER: the ring lifted clear and its chevron still poked
            // down between two ability cards. `SlipperRecallMark.ChevronReach` owns that number.
            // ⚠️ AND THE THEME'S OWN GAP ON TOP, rather than landing exactly on the deck's edge.
            // `TumpUiTheme.Gap` is the front end's standard breathing space and is already owned
            // somewhere else, which is the point: a clearance that just touches reads as a near
            // miss, and inventing a private number for the distance between two controls is how
            // this front end ended up with hand-written offsets in the first place (§ 92.1).
            float reach = RingRadius + SlipperRecallMark.ChevronReach + TumpUiTheme.Current.Gap;

            if (Mathf.Abs(at.x) > TumpPowerReadout.DeckHalfWidth + reach) return at;

            float clear = -(canvas.y * 0.5f) + TumpPowerReadout.DeckTop + reach;
            if (at.y > clear) return at;

            at.y = clear;
            return at;
        }

        private TumpPowerReadout _deck;

        /// <summary>
        /// ⚠️ ONE REVISION PAIR FOR THE CAP, WHICH IS THE PATTERN `Hud.RefreshKeyCaps` ALREADY
        /// USES. Resolving the label is a `FindActionMap`, a `FindAction` and a
        /// `ToHumanReadableString`, two of which allocate, and this mark is on screen for most of
        /// every retrieval. It is re-resolved when a binding moves or the player changes device
        /// and never otherwise. `CLAUDE.md` § 7.1 records what per-frame string work costs here:
        /// a HUD string rebuilt every frame took an eighth of the probe's frames.
        /// </summary>
        private void PaintCap(bool grabbable)
        {
            // ⚠️⚠️ NOTHING ON TOUCH, AND THE THUMB CONTROL PULSES INSTEAD. 🧑 2026-09-03, over a
            // screenshot of the Android build carrying `[X]  PICK UP`: *"why the fuck does it
            // have keybinds theres no keys in mobile"*. The ring still tracks, because "where did
            // it go" is a question a phone player has too.
            if (!grabbable || Hud.OnTouch)
            {
                if (_cap.enabled) _cap.enabled = false;
                if (_capText.enabled) _capText.enabled = false;
                if (grabbable && Hud.OnTouch) InputLayer.TouchHud.Emphasise(Verb.Grab);
                return;
            }

            if (_capBindings != Settings.Rebinding.Revision
                || _capDevice != InputLayer.LastInputDevice.Revision)
            {
                _capBindings = Settings.Rebinding.Revision;
                _capDevice = InputLayer.LastInputDevice.Revision;
                _capLabel = Hud.KeyLabelFor("Grab");

                // ⚠️ `onDark: true`. The mark is drawn over the street, which is what that flag
                // means: `InputGlyphs`' own note is that a sprite *"is not correct on its own,
                // only against the thing it is drawn on."*
                var sprite = InputGlyphs.For(_capLabel, onDark: true);

                _cap.sprite = sprite;
                _cap.enabled = sprite != null;
                _capText.enabled = sprite == null;
                _capText.text = sprite == null ? _capLabel : "";
            }
            else
            {
                if (_cap.sprite != null && !_cap.enabled) _cap.enabled = true;
                if (_cap.sprite == null && !_capText.enabled) _capText.enabled = true;
            }
        }
    }
}
