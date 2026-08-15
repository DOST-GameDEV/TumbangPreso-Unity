using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Overlay panels that sit on top of a screen rather than replacing it.
    ///
    /// ⚠️ THEY ARE OVERLAYS, NOT SCENES, AND THAT IS DELIBERATE. Settings opened from the main
    /// menu and settings opened from the pause menu must be the same panel, or the two drift
    /// and a slider exists in one and not the other. Making them scenes would also lose the
    /// match underneath when they are opened mid-game.
    /// </summary>
    public abstract class Panel : MonoBehaviour
    {
        protected Canvas Canvas;

        public static T Open<T>(MonoBehaviour owner) where T : Panel
        {
            var existing = owner.GetComponentInChildren<T>(includeInactive: true);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }

            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(owner.transform, false);
            return go.AddComponent<T>();
        }

        protected virtual void Start()
        {
            Canvas = MenuKit.BuildCanvas(transform, name + "Canvas");

            // ⚠️ ABOVE THE SCREEN UNDERNEATH, and opaque enough to be readable over a running
            // match. A translucent panel over a moving arena is unreadable exactly when it
            // matters, which is mid-match.
            Canvas.sortingOrder = 100;

            var bg = MenuKit.Backdrop(Canvas.transform, new Color(0.11f, 0.05f, 0.02f, 0.94f));
            bg.raycastTarget = true; // swallow clicks meant for the screen below

            Build();

            MenuKit.WoodButton(Canvas.transform, "BACK", new Vector2(0.5f, 0.09f),
                               Vector2.zero, new Vector2(320, 74), Close);
        }

        protected abstract void Build();

        public void Close() => gameObject.SetActive(false);
    }

    /// <summary>Volume, mouse, display, difficulty, and the player's name.</summary>
    public sealed class SettingsPanel : Panel
    {
        private Text _nameLabel;
        private Text _values;

        protected override void Build()
        {
            MenuKit.Label(Canvas.transform, "SETTINGS", 60, UiTheme.Amber,
                          new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(900, 90));

            var s = SettingsStore.Current;

            // ⚠️ THE NAME FIELD ENFORCES THE CAP AS A RULE, not by clipping at draw time. An
            // InputField with a character limit refuses the 15th keystroke, which tells the
            // player the rule; truncating later tells them nothing and looks like a bug.
            MenuKit.Label(Canvas.transform, "NAME", 28, UiTheme.CreamMuted,
                          new Vector2(0.5f, 0.75f), new Vector2(-300, 0), new Vector2(200, 50));

            var fieldGo = new GameObject("NameField");
            fieldGo.transform.SetParent(Canvas.transform, false);

            var fieldImg = fieldGo.AddComponent<Image>();
            fieldImg.color = UiTheme.WoodDark;
            MenuKit.Place(fieldImg.rectTransform, new Vector2(0.5f, 0.75f),
                          new Vector2(120, 0), new Vector2(560, 62));

            var text = MenuKit.Label(fieldGo.transform, s.PlayerName, 30, UiTheme.Cream,
                                     new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 56));

            var field = fieldGo.AddComponent<InputField>();
            field.textComponent = text;
            field.text = s.PlayerName;
            field.characterLimit = Balance.PlayerNameMax;
            field.onValueChanged.AddListener(v =>
            {
                SettingsStore.Current.PlayerName = GameSettings.SanitiseName(v);
                SettingsStore.Save();
            });

            _values = MenuKit.Label(Canvas.transform, "", 30, UiTheme.Cream,
                                    new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(1000, 320));

            Row("MASTER", 0.62f, () => Bump(ref SettingsStore.Current.MasterVolume));
            Row("SFX", 0.55f, () => Bump(ref SettingsStore.Current.SfxVolume));
            Row("MUSIC", 0.48f, () => Bump(ref SettingsStore.Current.MusicVolume));

            MenuKit.WoodButton(Canvas.transform, "FULLSCREEN", new Vector2(0.5f, 0.34f),
                               Vector2.zero, new Vector2(420, 70), () =>
                               {
                                   var cur = SettingsStore.Current;
                                   cur.Fullscreen = !cur.Fullscreen;
                                   Screen.fullScreen = cur.Fullscreen;
                                   SettingsStore.Save();
                                   Refresh();
                               });

            MenuKit.WoodButton(Canvas.transform, "AI DIFFICULTY", new Vector2(0.5f, 0.25f),
                               Vector2.zero, new Vector2(420, 70), () =>
                               {
                                   var cur = SettingsStore.Current;
                                   cur.AiDifficulty = (cur.AiDifficulty + 1) % 3;
                                   SettingsStore.Save();
                                   Refresh();
                               });

            Refresh();
        }

        private void Row(string label, float y, System.Action onPress)
        {
            MenuKit.WoodButton(Canvas.transform, label, new Vector2(0.5f, y),
                               new Vector2(-260, 0), new Vector2(300, 62), () =>
                               {
                                   onPress();
                                   SettingsStore.Save();
                                   Refresh();
                               });
        }

        /// <summary>Steps in tenths and wraps, so one control covers the whole range.</summary>
        private static void Bump(ref float v)
        {
            v = Mathf.Round((v + 0.1f) * 10.0f) / 10.0f;
            if (v > 1.001f) v = 0.0f;
        }

        private void Refresh()
        {
            var s = SettingsStore.Current;
            string[] tiers = { "EASY", "NORMAL", "HARD" };

            _values.text =
                $"\n\nMASTER   {s.MasterVolume:0.0}\n" +
                $"SFX      {s.SfxVolume:0.0}\n" +
                $"MUSIC    {s.MusicVolume:0.0}\n\n" +
                $"FULLSCREEN   {(s.Fullscreen ? "ON" : "OFF")}\n" +
                $"AI           {tiers[Mathf.Clamp(s.AiDifficulty, 0, 2)]}";
        }
    }

    /// <summary>
    /// How to play.
    ///
    /// ⚠️ IT TEACHES THE THESIS, NOT JUST THE KEYS. The tension is the retrieval, not the
    /// throw: throwing is safe and free, and getting your slipper back is what costs you. A
    /// player who learns only the controls plays the game backwards for their first match.
    /// </summary>
    public sealed class TutorialPanel : Panel
    {
        protected override void Build()
        {
            MenuKit.Label(Canvas.transform, "HOW TO PLAY", 60, UiTheme.Amber,
                          new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(900, 90));

            MenuKit.Label(Canvas.transform,
                "One taya guards the lata inside the chalk. Three attackers throw tsinelas at it,\n" +
                "then have to run in and get them back.\n\n" +
                "THE TENSION IS THE RETRIEVAL, NOT THE THROW.\n" +
                "Throwing is safe and free. You can only be tagged while holding a slipper\n" +
                "inside the box, so the trip back in is the whole game.\n\n" +
                "ATTACKER\n" +
                "   WASD move   ·   Shift sprint   ·   Space jump\n" +
                "   Left-click hold to charge a throw, release to throw\n" +
                "   E  pick up a slipper, or shove another attacker if there is nothing to grab\n\n" +
                "TAYA (the defender)\n" +
                "   You cannot leave the box, and you cannot throw\n" +
                "   Left-click  punch, a quick close tag\n" +
                "   E held      lunge, a short dash for somebody running past you\n" +
                "   E held in the ring, can down   stand the lata back up\n\n" +
                "SCORING\n" +
                $"   Knock the lata down  +{Balance.ScoreLataKnocked} to the thrower\n" +
                $"   Tag an attacker      +{Balance.ScoreTag} to the taya\n" +
                $"   Sabotage             +{Balance.ScoreSabotage} to the shover\n" +
                $"   Lata upright         +{Balance.ScoreDefensePerTick} per second to the taya\n\n" +
                $"{Balance.Rounds} rounds of {Balance.RoundTime:0}s. Everyone is taya exactly once. Highest total wins.",
                24, UiTheme.Cream,
                new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(1500, 700),
                TextAnchor.UpperLeft);
        }
    }

    /// <summary>Who made it.</summary>
    public sealed class CreditsPanel : Panel
    {
        protected override void Build()
        {
            MenuKit.Label(Canvas.transform, "CREDITS", 60, UiTheme.Amber,
                          new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(900, 90));

            MenuKit.Label(Canvas.transform,
                "TUMBANG PRESO\n\n" +
                "BH Studios\n\n" +
                "1st place, Gear Up NCR Esports Game Dev Challenge\n" +
                "NCR's entry at the nationals in General Santos City\n\n" +
                "Five students across four cities.\n" +
                "Four people, four cities, same street.",
                30, UiTheme.Cream,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 500));
        }
    }

    /// <summary>
    /// The in-match pause overlay.
    ///
    /// ⚠️ PAUSING PARKS INPUT RATHER THAN ONLY STOPPING TIME. A verb held across the boundary
    /// would stay held in the intent table, and the player walks out of the menu already
    /// sprinting or mid-throw-charge.
    /// </summary>
    public sealed class PausePanel : Panel
    {
        public CharacterMotor Local;

        protected override void Build()
        {
            MenuKit.Label(Canvas.transform, "PAUSED", 72, UiTheme.Amber,
                          new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(900, 100));

            MenuKit.WoodButton(Canvas.transform, "RESUME", new Vector2(0.5f, 0.55f),
                               Vector2.zero, new Vector2(460, 84), Resume);

            MenuKit.WoodButton(Canvas.transform, "SETTINGS", new Vector2(0.5f, 0.44f),
                               Vector2.zero, new Vector2(460, 84), () => Open<SettingsPanel>(this));

            MenuKit.WoodButton(Canvas.transform, "QUIT TO MENU", new Vector2(0.5f, 0.33f),
                               Vector2.zero, new Vector2(460, 84), () =>
                               {
                                   Time.timeScale = 1.0f;
                                   SceneFlow.Go(SceneFlow.MainMenu);
                               });

            Time.timeScale = 0.0f;
            if (Local != null) Local.Intent.Parked = true;
        }

        private void Resume()
        {
            Time.timeScale = 1.0f;
            if (Local != null) Local.Intent.Parked = false;
            Close();
        }
    }
}
