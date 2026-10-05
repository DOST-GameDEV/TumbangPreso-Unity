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
    /// ⚠️ REPLAYS ARE NOT HERE YET. The owner also asked for replays of events on these screens; that is
    /// `RecordedWorldView`'s machinery pointed at this texture and is its own piece of work.
    /// </summary>
    [DefaultExecutionOrder(870)]
    public sealed class ArenaScreens : MonoBehaviour
    {
        private const float Seconds = 3.2f, FadeIn = 0.14f, FadeOut = 0.4f, SlamSeconds = 0.2f;

        private readonly ArenaIntroScreen _screens = new ArenaIntroScreen();
        private readonly bool[] _falling = new bool[Core.Balance.PlayerCount];
        private float _until = -1.0f, _began = -1.0f;
        private int _rank;

        private void OnEnable() => MatchFlair.Presented += OnFlair;

        private void OnDisable()
        {
            MatchFlair.Presented -= OnFlair;
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

            if (!_screens.Built)
            {
                var stage = ArenaStage.Instance;
                if (stage == null) return;
                var players = new CharacterMotor[Core.Balance.PlayerCount];
                for (int s = 0; s < players.Length; s++) players[s] = round.PlayerAt(s);
                _screens.Build(transform, stage.transform.position, players);
            }

            _screens.Headline(headline, colour);
            _screens.Show(seat, true);
            _rank = rank; _began = now; _until = now + Seconds;
        }

        private void LateUpdate()
        {
            // The opening has the screens: hand them over whole.
            if (ArenaIntro.HidesUi) { if (_screens.Built) _screens.Destroy(); _until = -1.0f; return; }

            // A fall down the shaft, seen from the bodies themselves.
            var round = GameServices.Round;
            if (round != null && round.RoundActive)
                for (int s = 0; s < _falling.Length; s++)
                {
                    bool falling = ArenaStage.IsShaftFall(round.PlayerAt(s));
                    if (falling && !_falling[s]) Cut(2, "OVER THE EDGE!", ArenaFx.Violet, s);
                    _falling[s] = falling;
                }

            if (!_screens.Built) return;
            float now = Time.unscaledTime;
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
