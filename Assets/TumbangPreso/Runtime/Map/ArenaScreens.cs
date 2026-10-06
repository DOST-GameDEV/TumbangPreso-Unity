using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE ARENA'S BIG SCREENS, IN PLAY (owner, 2026-10-06: "the tv screens during real gameplay should
    /// be more reactive to events like when the lata falls, someone is tagged, or falls off"). Between
    /// events the screens show the stadium's own art, as built. When something happens they cut, all
    /// eight of them, to a card for it: a headline, the portrait and the name of whoever it happened to
    /// (or who did it), for three seconds, and back.
    ///
    ///   the can goes down      LATA DOWN!        whoever knocked it      gold
    ///   a tag                  TAGGED!           whoever was caught      magenta
    ///   a near miss            SO CLOSE!         the thrower             cyan
    ///   a block                BLOCKED!          the blocker             cyan
    ///   a fall down the shaft  OVER THE EDGE!    whoever fell            violet
    ///
    /// The card is `ArenaIntroScreen`'s (the opening's: one card drawn to a texture, on eight screens,
    /// through the screen shader), so the two look like one broadcast. A bigger event takes the screens
    /// from a smaller one; a smaller one waits its turn or is dropped.
    ///
    /// ⚠️ PRESENTATION ONLY, EVERY PEER FOR ITSELF. It listens to `MatchFlair.Presented`, which every
    /// peer already raises for these events, and watches the bodies for a fall: nothing is sent.
    /// ⚠️ NOT IN THE OPENING: the opening owns the screens then (`ArenaIntro.HidesUi`), and two cards
    /// would be drawn by each other's cameras.
    /// REPLAYS (same message: "i also wanna make it so it shows replays of events sometimes"). The game
    /// already keeps the round's best moments (`MatchReplayArchive`: a catch, a knocked can) and can draw one
    /// again from its own camera into a texture (`RecordedWorldView`, what halftime's replay is). When a
    /// moment is kept, about half the time and never twice inside 25 s, the screens play it back once at
    /// its own speed under a REPLAY label, then return. An event card takes the screens from a replay.
    /// ⚠️ THREE LIMITS, EACH FOR A REASON. Only a clip of THIS round: the replay stands the stage in its
    /// clip's layout, which would rearrange the live stage for another round's. Never while anything else
    /// holds the picture (the opening, halftime, a break, a held clock): two replays would hide each
    /// other's bodies. And the replay is drawn every other frame: each drawing is the scene rendered again.
    /// Any failure ends the replay and leaves the screens on their art.
    /// </summary>
    [DefaultExecutionOrder(870)]
    public sealed class ArenaScreens : MonoBehaviour
    {
        private const float Seconds = 3.2f, FadeIn = 0.14f, FadeOut = 0.4f, SlamSeconds = 0.2f;

        private readonly ArenaIntroScreen _screens = new ArenaIntroScreen();
        private readonly bool[] _falling = new bool[Core.Balance.PlayerCount];
        private float _until = -1.0f, _began = -1.0f;
        private int _rank;

        // The replay.
        private const float ReplayGap = 25.0f, ReplayChance = 0.5f;
        private CameraSystem.MatchReplayArchive _archive;
        private CameraSystem.RecordedMatchClip _pending, _clip;
        private CameraSystem.RecordedWorldView _view;
        private float _replayBegan, _lastReplay = -100.0f;
        private uint _dice = 0x1234ABCDu;

        private void OnEnable() => MatchFlair.Presented += OnFlair;

        private void OnDisable()
        {
            MatchFlair.Presented -= OnFlair;
            if (_archive != null) _archive.RetainedClip -= OnRetained;
            _archive = null; _pending = null;
            EndReplay();
            _screens.Destroy();
            _until = -1.0f;
        }

        private void OnFlair(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            switch (kind)
            {
                case MatchFlair.Kind.LataDown: Cut(4, "LATA DOWN!", ArenaFx.Gold, actor); break;
                case MatchFlair.Kind.Tag: Cut(3, "TAGGED!", ArenaFx.Magenta, subject >= 0 ? subject : actor); break;
                case MatchFlair.Kind.Block: Cut(1, "BLOCKED!", ArenaFx.Cyan, subject >= 0 ? subject : actor); break;
                case MatchFlair.Kind.NearMiss: Cut(1, "SO CLOSE!", ArenaFx.Cyan, actor); break;
            }
        }

        /// <summary>Cut the screens to a card, unless a bigger one is on them.</summary>
        private void Cut(int rank, string headline, Color colour, int seat)
        {
            var round = GameServices.Round;
            if (round == null || seat < 0 || seat >= Core.Balance.PlayerCount || ArenaIntro.HidesUi) return;
            float now = Time.unscaledTime;
            if (now < _until && rank < _rank) return;
            if (!EnsureBuilt()) return;
            EndReplay();   // an event takes the screens from a replay

            _screens.Headline(headline, colour);
            _screens.Show(seat, true);
            _rank = rank; _began = now; _until = now + Seconds;
        }

        private bool EnsureBuilt()
        {
            if (_screens.Built) return true;
            var stage = ArenaStage.Instance;
            var round = GameServices.Round;
            if (stage == null || round == null) return false;
            var players = new CharacterMotor[Core.Balance.PlayerCount];
            for (int s = 0; s < players.Length; s++) players[s] = round.PlayerAt(s);
            _screens.Build(transform, stage.transform.position, players);
            return _screens.Built;
        }

        // ------------------------------------------------------------------ the replay

        private void OnRetained(CameraSystem.MatchReplayArchive.Retained kept)
        {
            if (kept == null || kept.Clip == null) return;
            _dice ^= _dice << 13; _dice ^= _dice >> 17; _dice ^= _dice << 5;
            if ((_dice & 0xFFFF) / 65536.0f > ReplayChance || Time.unscaledTime - _lastReplay < ReplayGap) return;
            _pending = kept.Clip;
        }

        /// <summary>Whether a replay may have the screens now: nothing else holds the picture or the stage.</summary>
        private static bool Free()
        {
            var hp = HalftimePresentation.Instance;
            var stage = ArenaStage.Instance;
            var round = GameServices.Round;
            return !ArenaIntro.HidesUi && !PresentationClock.Held && (hp == null || !hp.Active) && stage != null
                && !stage.TryBreak(out _) && round != null && round.RoundActive;
        }

        private void BeginReplay()
        {
            var clip = _pending; _pending = null;
            var match = GameServices.Match;
            if (clip == null || match == null || clip.Round != match.RoundNumber || clip.MatchId != match.PresentationMatchId || !EnsureBuilt()) return;
            try
            {
                _view = new CameraSystem.RecordedWorldView(transform, clip);
                _view.ShowOnScreen(false);
                if (!_view.Ready) { EndReplay(); return; }
            }
            catch (System.Exception failure) { Debug.LogWarning("[ArenaScreens] replay unavailable: " + failure.Message); EndReplay(); return; }
            _clip = clip; _replayBegan = _lastReplay = Time.unscaledTime;
            _screens.Feed(_view.Target, "REPLAY");
        }

        private void EndReplay()
        {
            if (_view != null) { try { _view.Dispose(); } catch (System.Exception) { } }
            _view = null; _clip = null;
            if (_screens.Built) _screens.Feed(null, null);
        }

        /// <summary>True while a replay has the screens; draws its next frame.</summary>
        private bool Replaying()
        {
            if (_view == null) return false;
            float age = Time.unscaledTime - _replayBegan;
            if (!Free() || _clip == null || age > _clip.Duration + 0.4f) { EndReplay(); _screens.SetVisible(0.0f); return false; }
            if ((Time.frameCount & 1) == 0)
            {
                try { _view.Draw(_clip.Start + Mathf.Min(age, _clip.Duration), false); _view.ShowOnScreen(false); }
                catch (System.Exception failure) { Debug.LogWarning("[ArenaScreens] replay stopped: " + failure.Message); EndReplay(); _screens.SetVisible(0.0f); return false; }
            }
            _screens.SetVisible(Mathf.Clamp01(age / 0.25f) * Mathf.Clamp01((_clip.Duration + 0.4f - age) / 0.3f));
            _screens.Animate(0.0f, 0.0f, 1.0f, 0.0f);
            return true;
        }

        private void LateUpdate()
        {
            // The opening has the screens: hand them over whole.
            if (ArenaIntro.HidesUi) { EndReplay(); _pending = null; if (_screens.Built) _screens.Destroy(); _until = -1.0f; return; }

            if (_archive == null)
            {
                _archive = FindAnyObjectByType<CameraSystem.MatchReplayArchive>();
                if (_archive != null) _archive.RetainedClip += OnRetained;
            }

            // A fall down the shaft, seen from the bodies themselves.
            var round = GameServices.Round;
            if (round != null && round.RoundActive)
                for (int s = 0; s < _falling.Length; s++)
                {
                    bool falling = ArenaStage.IsShaftFall(round.PlayerAt(s));
                    if (falling && !_falling[s]) Cut(2, "OVER THE EDGE!", ArenaFx.Violet, s);
                    _falling[s] = falling;
                }

            float now = Time.unscaledTime;
            // A kept moment waits for the screens to be free of a card, then plays.
            if (_view == null && _pending != null && now >= _until + FadeOut)
            {
                if (Free()) BeginReplay(); else _pending = null;
            }
            if (Replaying()) return;

            if (!_screens.Built) return;
            if (now >= _until + FadeOut) { _screens.SetVisible(0.0f); return; }

            float age = now - _began;
            float alpha = Mathf.Clamp01(age / FadeIn) * Mathf.Clamp01((_until + FadeOut - now) / FadeOut);
            _screens.SetVisible(alpha);
            // The card arrives with the roll's glitch, and its headline slams as the opening's TAYA does.
            float glitch = Mathf.Clamp01(1.0f - age / 0.35f);
            _screens.Animate(glitch, Mathf.Floor(now * 24.0f), Mathf.Clamp01(age / SlamSeconds), 0.5f * Mathf.Clamp01(1.0f - age / 0.25f)
                                                                                           * Settings.SettingsStore.Current.EffectiveFlashIntensity);
        }
    }
}
