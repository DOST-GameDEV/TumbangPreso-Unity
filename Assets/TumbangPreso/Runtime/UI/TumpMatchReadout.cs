using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>New match presentation. Reads the existing referee, actors and resource state only.</summary>
    [DefaultExecutionOrder(1200)]
    public sealed partial class TumpMatchReadout : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        private RectTransform _root, _scoreRoot, _clockRoot, _canRoot, _personalRoot, _promptRoot;
        private Text _clock, _round, _canState, _canHint, _role, _stock, _prompt, _context, _toast, _countdown, _spectator, _sandbox;
        private Text _crosshair, _hit, _staminaCaption;
        private float _staminaCaptionWidth;
        private CharacterMotor _aimOwner;
        private Carrier _aimCarrier;
        private Image _stamina, _progress;
        private readonly Text[] _names = new Text[4], _scores = new Text[4], _roles = new Text[4];
        private readonly Image[] _portraits = new Image[4];
        private readonly RectTransform[] _scoreRows = new RectTransform[4];
        private readonly Text[] _status = new Text[4];
        private readonly List<StatusRow> _statusRows = new List<StatusRow>();
        private TumpPowerReadout _powers;
        private TumpHudEffects _effects;
        private float _toastLeft, _hitLeft;
        private Slipper[] _slippers;
        private float _scanAt;
        private float _scoreAt;
        private CameraSystem.SpectatorCamera _spectatorCamera;
        public bool ReadyWindow;

        public void BuildPrevious(Transform owner)
        {
            Canvas = TumpUiFactory.Canvas(owner, "TumpMatchCanvas", 100);
            // The game owns mouse/controller navigation. This is a readout, not menu focus.
            var focus = Canvas.GetComponent<ScreenFocus>(); if (focus != null) focus.enabled = false;
            _root = (RectTransform)Canvas.transform;
            _effects = gameObject.AddComponent<TumpHudEffects>(); _effects.Build(_root);
            BuildScores(); BuildClock(); BuildCan(); BuildPersonal(); BuildPrompts();
            _powers = gameObject.AddComponent<TumpPowerReadout>(); _powers.Build(_root);
            _toast = Ink(_root, "MatchToast", "", 38, true);
            TumpUiFactory.Anchor(_toast.rectTransform, new Vector2(.5f, 1), new Vector2(0, -212), new Vector2(1040, 78));
            _toast.enabled = false;
            _countdown = Ink(_root, "Countdown", "", 112, true);
            TumpUiFactory.Anchor(_countdown.rectTransform, new Vector2(.5f, .58f), Vector2.zero, new Vector2(740, 180));
            _countdown.enabled = false;
            _crosshair = Ink(_root, "Reticle", "+", 34, false);
            TumpUiFactory.Anchor(_crosshair.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(76, 76));
            _hit = Ink(_root, "HitConfirmation", "×", 72, true);
            TumpUiFactory.Anchor(_hit.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(120, 120)); _hit.enabled = false;
            _spectator = Ink(_root, "SpectatorReadout", "", 26, false);
            TumpUiFactory.Anchor(_spectator.rectTransform, new Vector2(.5f, 0), new Vector2(0, 100), new Vector2(1420, 136));
            _sandbox = Ink(_root, "SandboxState", "", 24, false);
            TumpUiFactory.Anchor(_sandbox.rectTransform, new Vector2(1, 0), new Vector2(-250, 38), new Vector2(456, 54));
            var version = Ink(_root, "GameVersion", "", 20, false);
            TumpUiFactory.Anchor(version.rectTransform, new Vector2(1, 0), new Vector2(-230, 14), new Vector2(420, 28));
            GameVersion.ApplyTo(version);
        }
        private static Text InkPrevious(Transform root, string name, string words, int size, bool main)
        {
            var text = TumpUiFactory.Text(root, name, words, size, main);
            text.color = TumpUiTheme.Current.Cream; text.alignment = TextAnchor.MiddleCenter;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, .95f); outline.effectDistance = new Vector2(2, -2);
            return text;
        }
        private void BuildScoresPrevious()
        {
            _scoreRoot = TumpUiFactory.Rect(_root, "FourPlayerScoreboard");
            TumpUiFactory.Place(_scoreRoot, 30, 28, 468, 280);
            for (int i = 0; i < 4; i++)
            {
                var row = _scoreRows[i] = TumpUiFactory.Rect(_scoreRoot, "ScoreRow" + i);
                TumpUiFactory.Place(row, 0, i * 70, 468, 66);
                var strip = row.gameObject.AddComponent<TumpScoreStrip>(); strip.raycastTarget = false;
                _portraits[i] = TumpUiFactory.Art(row, "PlayerPortrait", null);
                TumpUiFactory.Place(_portraits[i].rectTransform, 0, 2, 62, 62);
                _names[i] = Ink(row, "PlayerName", "", 32, true); _names[i].alignment = TextAnchor.MiddleLeft;
                TumpUiFactory.Place(_names[i].rectTransform, 76, 0, 270, 52);
                _scores[i] = Ink(row, "Score", "", 34, true); _scores[i].alignment = TextAnchor.MiddleRight;
                TumpUiFactory.Place(_scores[i].rectTransform, 358, 0, 104, 58);
                _roles[i] = Ink(row, "RoleState", "", 21, false); _roles[i].alignment = TextAnchor.MiddleLeft;
                TumpUiFactory.Place(_roles[i].rectTransform, 78, 44, 366, 28);
            }
        }
        private void BuildClockPrevious()
        {
            _clockRoot = TumpUiFactory.Rect(_root, "RoundClock");
            TumpUiFactory.Anchor(_clockRoot, new Vector2(.5f, 1), new Vector2(0, -94), new Vector2(660, 170));
            var face = TumpUiFactory.Surface(_clockRoot, "ClockFace", TumpSurface.Form.Disc, TumpUiTheme.Current.DeepOlive, false);
            TumpUiFactory.Place(face.rectTransform, 154, -4, 352, 106);
            _clock = Ink(_clockRoot, "TimeLeft", "", 62, true); TumpUiFactory.Place(_clock.rectTransform, 174, 0, 312, 94);
            _round = Ink(_clockRoot, "RoundLabel", "", 28, true); TumpUiFactory.Place(_round.rectTransform, 0, 96, 660, 60);
        }
        private void BuildCanPrevious()
        {
            _canRoot = TumpUiFactory.Rect(_root, "CanReadout");
            TumpUiFactory.Anchor(_canRoot, new Vector2(1, 1), new Vector2(-252, -112), new Vector2(464, 176));
            var can = TumpUiFactory.Rect(_canRoot, "CanStateIcon").gameObject.AddComponent<TumpSymbol>();
            can.Kind = TumpSymbol.Icon.Can; can.color = TumpUiTheme.Current.Cream; can.raycastTarget = false;
            var canOutline = can.gameObject.AddComponent<Outline>(); canOutline.effectColor = UiTheme.InGameOutline;
            canOutline.effectDistance = new Vector2(2, -2);
            TumpUiFactory.Place(can.rectTransform, 0, 0, 92, 106);
            _canState = Ink(_canRoot, "CanState", "", 36, true); _canState.alignment = TextAnchor.MiddleLeft;
            TumpUiFactory.Place(_canState.rectTransform, 110, 12, 344, 74);
            _canHint = Ink(_canRoot, "CanHint", "", 26, false); _canHint.alignment = TextAnchor.UpperRight;
            TumpUiFactory.Place(_canHint.rectTransform, 0, 112, 454, 64);
        }
        private void BuildPersonalPrevious()
        {
            _personalRoot = TumpUiFactory.Rect(_root, "LocalState");
            TumpUiFactory.Anchor(_personalRoot, new Vector2(0, 0), new Vector2(264, 110), new Vector2(464, 174));
            _role = Ink(_personalRoot, "LocalRole", "", 36, true); _role.alignment = TextAnchor.MiddleLeft;
            TumpUiFactory.Place(_role.rectTransform, 0, 0, 464, 64);
            _stock = Ink(_personalRoot, "SlipperState", "", 26, false); _stock.alignment = TextAnchor.MiddleLeft;
            TumpUiFactory.Place(_stock.rectTransform, 0, 64, 464, 54);
            var track = TumpUiFactory.Rect(_personalRoot, "StaminaTrack").gameObject.AddComponent<Image>();
            track.color = TumpUiTheme.Current.DeepOlive; track.raycastTarget = false;
            TumpUiFactory.Place(track.rectTransform, 0, 134, 326, 12);
            _stamina = TumpUiFactory.Rect(track.transform, "StaminaFill").gameObject.AddComponent<Image>();
            _stamina.color = TumpUiTheme.Current.Lime; _stamina.raycastTarget = false; TumpUiFactory.Stretch(_stamina.rectTransform, 2);
            var label = Ink(_personalRoot, "StaminaLabel", "Stamina", 22, false);
            TumpUiFactory.Place(label.rectTransform, 338, 116, 118, 42);
            for (int i = 0; i < 4; i++)
            {
                _status[i] = Ink(_root, "TimedStatus" + i, "", 24, false); _status[i].alignment = TextAnchor.MiddleLeft;
                TumpUiFactory.Place(_status[i].rectTransform, 38, 332 + i * 46, 450, 44);
            }
        }
        private void BuildPromptsPrevious()
        {
            _promptRoot = TumpUiFactory.Rect(_root, "ContextualAction");
            TumpUiFactory.Anchor(_promptRoot, new Vector2(.5f, .32f), Vector2.zero, new Vector2(1100, 176));
            _prompt = Ink(_promptRoot, "ActionPrompt", "", 36, true);
            TumpUiFactory.Place(_prompt.rectTransform, 0, 0, 1100, 74);
            _context = Ink(_promptRoot, "ActionDetail", "", 26, false);
            TumpUiFactory.Place(_context.rectTransform, 0, 78, 1100, 64);
            var track = TumpUiFactory.Rect(_promptRoot, "RecoveryProgress").gameObject.AddComponent<Image>();
            track.color = TumpUiTheme.Current.DeepOlive; track.raycastTarget = false;
            TumpUiFactory.Place(track.rectTransform, 320, 148, 460, 14);
            _progress = TumpUiFactory.Rect(track.transform, "ProgressFill").gameObject.AddComponent<Image>();
            _progress.color = TumpUiTheme.Current.Lime; _progress.raycastTarget = false; TumpUiFactory.Stretch(_progress.rectTransform, 2);
            track.gameObject.SetActive(false);
        }
        public void Toast(string words, float duration)
        { _toast.text = words; _toastLeft = duration; _toast.enabled = true; }
        public void Countdown(string words) { _countdown.text = words; _countdown.enabled = !string.IsNullOrEmpty(words); }
        /// <summary>
        /// The two things `RoundLabel` can ever say, and the widest form of each.
        ///
        /// ⚠️⚠️ THIS EXISTS SO A LAYOUT PROBE READS THE SHIPPING FORMATTER RATHER THAN A
        /// STRING TYPED INTO A TEST. `HudOverflowProbe` was fed `Hud.TopCentreLines()`, which is
        /// the LEGACY hud's worst case: `ROUND n / N   ·   DEFENDER: &lt;14 chars&gt;`, about 704
        /// units of text. **The painted readout never draws that line.** It writes the round and
        /// the total, and the defender is named on its own row, so the probe reported nine
        /// overflows across nine resolutions about a sentence this HUD cannot produce. A probe
        /// fed a guess measures the guess, which is the same warning `Hud.TopCentreLines`'s own
        /// header carries one HUD earlier.
        /// </summary>
        public static IEnumerable<string> RoundLabelLines()
        {
            yield return WarmupRoundLine;

            // ⚠️ EIGHT OF EIGHT IS THE WIDEST LEGAL ROUND LINE, not a round number anybody
            // plays: `CustomGameRules` caps the count at eight, and one digit either side is
            // the longest this string gets.
            yield return RoundLine(8, 8);
        }

        internal const string WarmupRoundLine = "Warm up · Scores paused";

        internal static string RoundLine(int round, int total)
            => $"Round {Mathf.Max(1, round)} / {total}";

        public void Hit(Color color) { _hit.color = color; _hit.enabled = true; _hitLeft = .25f; }
        public void Flash(bool active) => _effects.Flash(active);
        public void Tick(CharacterMotor local, bool spectating, bool training, bool hidePowers, bool spectatorControls)
        {
            Canvas.enabled=!HalftimePresentation.Playing;
            float dt = Time.unscaledDeltaTime;
            if (_toastLeft > 0) { _toastLeft -= dt; if (_toastLeft <= 0) _toast.enabled = false; }
            if (_hitLeft > 0) { _hitLeft -= dt; if (_hitLeft <= 0) _hit.enabled = false; }
            var match = GameServices.Match; var round = GameServices.Round;
            _effects.Tick(local, spectating);
            if (match == null || round == null) return;
            _clockRoot.gameObject.SetActive(!training); _scoreRoot.gameObject.SetActive(!training);
            int time = Mathf.CeilToInt(Mathf.Max(0, round.TimeLeft));
            _clock.text = $"{time / 60:00}:{time % 60:00}";
            _round.text = match.IsWarmupBuffer ? WarmupRoundLine : RoundLine(match.RoundNumber, match.TotalRounds);
            MatchBarClock(match, round, time);
            if (round.RoundActive && match.MatchInProgress) GameServices.Voice?.TickClock(round.TimeLeft);
            if (Time.unscaledTime >= _scoreAt) { _scoreAt = Time.unscaledTime + .1f; Scores(local, spectating); }
            Can(local, training, spectating); Personal(local, spectating);
            _crosshair.enabled = !spectating && local != null && round.RoundActive;
            if(_aimOwner!=local){_aimOwner=local;_aimCarrier=local!=null?local.GetComponent<Carrier>():null;}
            Prompts(local, spectating);
            _powers.Tick(local != null ? local.GetComponent<Abilities.HeroAbilitySystem>() : null,
                !spectating && !hidePowers && SceneFlow.SelectedMode == GameMode.HeroStrike);
            _spectator.enabled = spectating && spectatorControls;
            if (_spectator.enabled)
            {
                if (_spectatorCamera == null) _spectatorCamera = FindFirstObjectByType<CameraSystem.SpectatorCamera>();
                _spectator.text = (_spectatorCamera != null ? _spectatorCamera.StatusText() : "Spectating")
                    + "\n" + Hud.KeyLabelFor("SpectatorControls") + " hide controls · " + Hud.KeyLabelFor("CleanFeed") + " clean feed";
            }
            Sandbox();
        }

        private void LateUpdate()
        {
            PaintScoreMoments();
            SizePromptPlate();
            if(_crosshair==null || !_crosshair.enabled)return;
            var anchor=new Vector2(.5f,.5f);
            var view=UnityEngine.Camera.main;
            if(_aimCarrier!=null && _aimCarrier.IsCharging && view!=null)
            {
                var point=view.WorldToViewportPoint(_aimCarrier.AimGuidePoint());
                if(point.z>0)anchor=view.rect.min+Vector2.Scale(new Vector2(point.x,point.y),view.rect.size);
            }
            _crosshair.rectTransform.anchorMin=_crosshair.rectTransform.anchorMax=anchor;
        }
        private void Scores(CharacterMotor local, bool spectating)
        {
            var match = GameServices.Match;
            // VISUAL-1.4: fixed seat order and a crown for a unique leader, instead of rows
            // that re-sort on every score. See `TumpMatchReadout.MatchBar`.
            int leader = -1, best = 0; bool tied = false;
            for (int seat = 0; seat < 4; seat++)
            {
                if (GameServices.Round.PlayerAt(seat) == null) continue;
                int value = match.ScoreFor(seat);
                if (value > best) { best = value; leader = seat; tied = false; }
                else if (value == best && value > 0) tied = true;
            }
            if (tied) leader = -1;
            for (int i = 0; i < 4; i++)
            {
                int slot = i; _scoreRowSeats[i] = slot; var actor = GameServices.Round.PlayerAt(slot);
                _scoreRows[i].gameObject.SetActive(actor != null); if (actor == null) continue;
                _names[i].text = SeatLabel.WithIdentity(slot);
                _names[i].color = PlayerIdentity.Colour(slot); _scores[i].text = match.ScoreFor(slot).ToString();
                bool defender = slot == match.DefenderSlot;
                string state = defender ? "Defender" : "";
                if (!spectating && local != null && slot == local.PlayerSlot) state = string.IsNullOrEmpty(state) ? "You" : "You · Defender";
                if (spectating)
                {
                    string activity = actor.IsSwimming ? "Swimming" : actor.IsTripped ? "Down" : actor.IsStunned ? "Stunned" :
                        actor.IsDefender ? (actor.GetComponent<Carrier>()?.ChannelRatio > 0 ? "Resetting can" : "") :
                        actor.HoldingSlipper ? "Holding" : "Retrieving";
                    if (!string.IsNullOrEmpty(activity)) state += (state.Length > 0 ? " · " : "") + activity;
                }
                _roles[i].text = state;
                _roles[i].color = defender ? CourtPresentationPalette.Gold : OwnerUiTheme.Current.Pale;
                var people = Roster.GetPeople(actor.Mode);
                var portrait = actor.CharacterIndex >= 0 && actor.CharacterIndex < people.Count
                    ? OwnerPortraitArt.Get("UI/portraits/" + people[actor.CharacterIndex].Id) : null;
                if (_portraits[i].sprite != portrait) _portraits[i].sprite = portrait;
                _portraits[i].enabled = portrait != null;
                bool mine = !spectating && local != null && slot == local.PlayerSlot;
                MatchBarChip(i, slot, actor, defender, mine, slot == leader, spectating);
            }
        }
        private void Can(CharacterMotor local, bool training, bool spectating)
        {
            // VISUAL-1.4: the can's state is one glyph in the clock plate for players and
            // spectators alike, so the corner readout that said it in words stays built (the
            // reading layout and probes address it by name) and is never shown.
            var lata = GameServices.Round.Lata;
            MatchBarCan(lata);
            _canRoot.gameObject.SetActive(false);
            if (lata == null || !spectating) return;
            _canState.text = lata.IsUpright ? "Can upright" : "Can down";
            _canHint.text = lata.IsProtected ? "Can protected · Defender may tag" :
                lata.IsUpright ? "Defender may tag" : "Retrieve or reset";
            _canState.color = lata.IsUpright ? OwnerUiTheme.Current.Pale : CourtPresentationPalette.Gold;
        }
        private void Personal(CharacterMotor local, bool spectating)
        {
            bool show = local != null && !spectating;
            _personalRoot.gameObject.SetActive(show);
            foreach (var text in _status) text.enabled = false;
            StaminaArc(show ? local : null);
            if (!show) return;
            _role.text = local.IsDefender ? "Defender" : "Attacker";
            _role.color = local.IsDefender ? CourtPresentationPalette.Gold : OwnerUiTheme.Current.Pale;
            _stock.text = local.HoldingSlipper ? "Slipper in hand" : local.IsDefender ? "Guard the can" : "Slipper away";
            bool piloting=PilotingFamiliar(local);
            if(piloting)
            {
                _role.text="Controlling Kuro";_role.color=CourtPresentationPalette.Paper;
                bool danger=local.IsTaggable()&&GameServices.Round?.Lata!=null&&GameServices.Round.Lata.IsUpright;
                _stock.text=local.IsStunned?"Nemu's body is recovering":danger?"Nemu's body can be tagged":
                    local.IsDefender?"Nemu's body is the defender":local.HoldingSlipper?"Nemu's body has the slipper":"Nemu's body is unarmed";
            }
            if(_staminaCaption==null)
            {
                var caption=_personalRoot.Find("StaminaLabel");
                if(caption!=null){_staminaCaption=caption.GetComponent<Text>();_staminaCaptionWidth=_staminaCaption.rectTransform.sizeDelta.x;}
            }
            string staminaName=piloting?"Nemu stamina":"Stamina";
            if(_staminaCaption!=null&&_staminaCaption.text!=staminaName)
            {
                _staminaCaption.text=staminaName;var size=_staminaCaption.rectTransform.sizeDelta;
                size.x=piloting?Mathf.Max(_staminaCaptionWidth,190):_staminaCaptionWidth;_staminaCaption.rectTransform.sizeDelta=size;
            }
            var stock = GameServices.Tsinelas;
            bool stockLive = stock != null && stock.Live && !local.IsDefender;
            if (stockLive) _stock.text += " · " + stock.StockFor(local.PlayerSlot) + " left";
            // ⚠️ VISUAL-1.4: THESE LINES ONLY SPEAK WHEN THEY SAY SOMETHING NOTHING ELSE DOES.
            // The role is on the chip, the slipper is in the viewmodel's hand and stamina is the
            // arc beside the reticle, so "Attacker / Slipper in hand / Stamina" repeated the
            // screen (and broke VISION § 3's no-sentences rule) for the whole match. They stay
            // computed, because tests and the familiar readout read them, and are drawn only
            // while piloting Kuro or while a limited slipper stock is counting down.
            _role.enabled = piloting; _stock.enabled = piloting || stockLive;
            if (_staminaCaption != null) _staminaCaption.enabled = piloting;
            // Piloting Kuro keeps the labelled bar: whose stamina it is IS the information there.
            if (_stamina != null && _stamina.transform.parent.gameObject.activeSelf != piloting) _stamina.transform.parent.gameObject.SetActive(piloting);
            _stamina.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(local.Stamina.Ratio), 1);
            _stamina.enabled = local.Stamina.Ratio > .001f;
            _stamina.color = local.Stamina.IsFatigued ? OwnerUiTheme.Current.Orange : CourtPresentationPalette.Gold;
            StatusStack.Collect(local, local.GetComponent<Carrier>(), local.GetComponent<CombatVerbs>(), _statusRows);
            int index = 0;
            foreach (var row in _statusRows)
            {
                if (row.Label == "VULNERABLE" || index >= _status.Length) continue;
                _status[index].enabled = true; _status[index].text = row.Label + (row.Timed ? $" · {row.Remaining:0.0}s" : ""); index++;
            }
        }
        private void Prompts(CharacterMotor local, bool spectating)
        {
            _promptRoot.gameObject.SetActive(local != null && !spectating);
            if (local == null || spectating) return;
            _prompt.text = ""; _context.text = ""; _progress.transform.parent.gameObject.SetActive(false);
            _prompt.color = OwnerUiTheme.Current.Pale;
            if(HalftimePresentation.Playing){_prompt.text="HALFTIME";_context.text="Next round in "+Mathf.CeilToInt(HalftimePresentation.Instance.Remaining)+"s";return;}
            var carrier = local.GetComponent<Carrier>(); var round = GameServices.Round;
            if (local.IsTripped)
            {
                _prompt.text = local.CanMashUp ? Hud.MashVerb("Jump") + " to get up" : "Getting up";
                float ratio = local.MashRemoved / Mathf.Max(.01f, local.TripTotal - Balance.MinTripDown); Progress(ratio);
                if (local.CanMashUp && Hud.OnTouch) TouchHud.Emphasise(Verb.Jump); return;
            }
            if (local.StunElement != StunElement.None)
            {
                _prompt.text = local.CanMashOutOfStun ? Visual.StunCoat.For(local.StunElement).Verb + " " + Hud.PressCue("Jump") : "Breaking free";
                _context.text = local.StunMashPresses + " / " + local.StunBreakPresses + " presses";
                if (local.CanMashOutOfStun && Hud.OnTouch) TouchHud.Emphasise(Verb.Jump); return;
            }
            if (BufferSkipVote.Showing)
            {
                _prompt.text = Hud.PressCue("ReadyUp") + "Skip warmup";
                _context.text = "Scores paused" + (BufferSkipVote.VotesNeeded > 1 ? $" · {BufferSkipVote.Votes}/{BufferSkipVote.VotesNeeded} ready" : ""); return;
            }
            if (ReadyWindow)
            {
                _prompt.text = Hud.PressCue("ReadyUp") + (round.RoundActive ? "Ready" : "Ready to play");
                _context.text = SceneFlow.SelectedMode == GameMode.HeroStrike ? "Warm up freely. Powers start with the round." : "Warm up freely. Scores are paused."; return;
            }
            if(PilotingFamiliar(local))
            {
                _prompt.text=Hud.PressCue("Skill2")+"Bring Nemu to Kuro";
                _context.text="Move to scout. Recall uses Kuro's last safe landing spot.";
                if(Hud.OnTouch)TouchHud.Emphasise(Verb.Skill2);
                return;
            }
            if (local.IsDefender && round.Lata != null && !round.Lata.IsUpright)
            {
                bool toggle = Settings.SettingsStore.Current.ToggleRestore;
                string key = Hud.KeyLabelFor("Grab");
                _prompt.text = carrier != null && carrier.ChannelRatio > 0
                    ? toggle ? "Resetting can · press " + key + " to cancel" : "Resetting can"
                    : (toggle ? "Press " : "Hold ") + key + " at the can to reset";
                if (carrier != null && carrier.ChannelRatio > 0) Progress(carrier.ChannelRatio); return;
            }
            if (carrier != null && carrier.IsCharging)
            {
                float spin = carrier.CurrentPektusSpin;
                _prompt.text = round.Lata != null && round.Lata.IsProtected ? "Can protected" : "Release to throw";
                _context.text = Mathf.Abs(spin) > .08f ? $"Pektus {(spin < 0 ? "left" : "right")} · {Mathf.RoundToInt(Mathf.Abs(spin) * 100)}%" : "Move the aim sideways for pektus";
                Progress(carrier.ChargeRatio); return;
            }
            if (local.IsTaggable() && round.Lata != null && round.Lata.IsUpright) { _prompt.text = "You can be tagged"; _prompt.color = OwnerUiTheme.Current.Orange; }
            else _prompt.color = OwnerUiTheme.Current.Pale;
            if (!local.IsDefender && !local.HoldingSlipper)
            {
                if (Time.time >= _scanAt) { _scanAt = Time.time + .2f; _slippers = FindObjectsByType<Slipper>(FindObjectsInactive.Include, FindObjectsSortMode.None); }
                float returning = 0;
                if (_slippers != null) foreach (var slipper in _slippers)
                {
                    if (slipper == null) continue;
                    if (slipper.CanBeGrabbedBy(local))
                    {
                        _prompt.text = Hud.PressCue("Grab") + "Pick up";
                        if (Hud.OnTouch) TouchHud.Emphasise(Verb.Grab); return;
                    }
                    if (slipper.OwnerSlot == local.PlayerSlot && RooftopRecovery.Instance != null)
                        returning = Mathf.Max(returning, RooftopRecovery.Instance.SecondsUntilReturn(slipper));
                    if (slipper.OwnerSlot == local.PlayerSlot && LagoonWater.Instance != null)
                        returning = Mathf.Max(returning, LagoonWater.Instance.SecondsUntilReturn(slipper));
                }
                if (returning > 0) _context.text = $"Slipper returning · {returning:0.0}s";
                else
                {
                    float idle = round.AttackerIdleSeconds(local.PlayerSlot);
                    if (TournamentRules.IsSlipperWarning(idle)) _context.text = idle < Balance.SlipperUnretrievedGracePeriod
                        ? $"Fetch your slipper · {Balance.SlipperUnretrievedGracePeriod - idle:0.0}s" : "Fetch your slipper · -5 / second";
                }
            }
            if (local.IsDefender && round.IsTayaCampWarningActive)
                _context.text = "Leave the can ring";
        }
        private static bool PilotingFamiliar(CharacterMotor local)
        {
            var visual=local!=null?local.GetComponent<Visual.CharacterVisual>():null;
            return visual!=null&&visual.Companion!=null&&visual.Companion.IsPossessed;
        }
        private void Progress(float ratio)
        { _progress.transform.parent.gameObject.SetActive(true); _progress.enabled = ratio > .001f; _progress.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1); }
        private void Sandbox()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f7Key.wasPressedThisFrame && PracticeSandbox.Allowed) PracticeSandbox.Toggle();
            _sandbox.enabled = PracticeSandbox.Allowed && !Hud.OnTouch && (PracticeSandbox.Active || ReadyWindow);
            _sandbox.text = "F7 · No cooldowns " + (PracticeSandbox.Active ? "on" : "off");
        }
    }
}
