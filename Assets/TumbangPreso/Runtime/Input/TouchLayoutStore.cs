using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.InputLayer
{
    /// <summary>One control's departure from the layout the game ships with.</summary>
    [Serializable]
    public struct TouchTweak
    {
        /// <summary>The verb this row is about, by name, so the file survives an enum reorder.</summary>
        public string Verb;

        /// <summary>Offset from the built-in position, in canvas units.</summary>
        public float OffsetX;
        public float OffsetY;

        /// <summary>Multiplier on the built-in size. Clamped to the legal band on read.</summary>
        public float Scale;

        /// <summary>Whether the player has switched this control off entirely.</summary>
        public bool Hidden;
    }

    [Serializable]
    internal sealed class TouchLayoutFile
    {
        public float Opacity = TouchLayoutStore.DefaultOpacity;
        public float Scale = 1.0f;
        public List<TouchTweak> Tweaks = new List<TouchTweak>();
    }

    /// <summary>
    /// The player's own touch layout: opacity, size and where each control sits.
    ///
    /// ⚠️⚠️ 🧑 ASKED FOR THIS BY NAME AND BY EXAMPLE: *"give users the option to configure game
    /// ui and huds too like pubg (tehy can lower opacity, changhe size and position ) etc"*. It is
    /// not decoration. **A touch layout is the one part of a mobile game's UI that cannot be got
    /// right for everybody**, because it is fitted to a hand: thumb length, whether the player
    /// claws or uses two thumbs, phone width, and which hand holds the device all move the right
    /// answer, and none of them is knowable from here. Every competitive mobile game ships this
    /// screen for that reason.
    ///
    /// ⚠️⚠️ THE SHIPPED LAYOUT IS STILL DESIGNED, AND THIS DOES NOT EXCUSE IT. `CLAUDE.md` § 6.2:
    /// *"what is the ONE thing on this screen"*, and a customiser is not an answer to a bad
    /// default. `TouchHud`'s constants are measured against `TouchMetrics` and the probe drives
    /// them at twelve shapes; this lets a player move a good layout, not repair a poor one.
    ///
    /// ⚠️ IT IS KEYED BY VERB NAME, NOT BY ENUM VALUE. A saved layout has to survive a `Verb`
    /// being inserted in the middle of the enum, which reorders every value after it. A player
    /// whose THROW button silently became their EMOTE button after an update would reasonably
    /// call that data loss. An unknown name is dropped on read rather than erroring.
    ///
    /// ⚠️ AND A VERB WITH NO ROW IS THE NORMAL CASE. The file holds only what the player CHANGED,
    /// so a new verb added next month arrives at its designed position for everybody, including
    /// players who customised everything else. A file that stored an absolute position per verb
    /// would freeze the shipped layout at whatever it was the day the player first opened this
    /// screen.
    /// </summary>
    public static class TouchLayoutStore
    {
        private const string Key = "tumbangpreso.touchlayout";

        /// <summary>
        /// ⚠️ 0.55 IS THE DEFAULT, NOT 1.0, AND THAT IS `docs/VISION.md` § 2 RULE 5 APPLIED. *"A
        /// screenshot taken mid-fight must still show the lata, the chalk and every player."* The
        /// controls sit over a live 14 m arena and are the one piece of chrome that never moves,
        /// so at full opacity they are permanently in front of the thing the player is aiming at.
        /// The band below lets somebody who wants them solid have that.
        /// </summary>
        public const float DefaultOpacity = 0.55f;

        public const float MinOpacity = 0.15f;
        public const float MaxOpacity = 1.0f;

        /// <summary>
        /// ⚠️⚠️ THE SIZE BAND'S FLOOR IS NOT A TASTE, IT IS `TouchMetrics.MinTargetUnits`. A
        /// player may shrink a control to 0.75 of its designed size and no further, because the
        /// smallest control in the layout is exactly at the 144-unit accessibility floor and
        /// scaling it below that produces a target that cannot reliably be hit. **A settings
        /// slider that lets somebody break their own game is a defect, not a freedom**, and the
        /// player who would drag it to the bottom is the one who then reports that the controls
        /// do not work. The ceiling is 1.6, which fills a phone without the cluster reaching the
        /// stick.
        /// </summary>
        public const float MinScale = 0.75f;

        public const float MaxScale = 1.6f;

        private static TouchLayoutFile _file;

        private static TouchLayoutFile File
        {
            get
            {
                if (_file != null) return _file;

                string json = PlayerPrefs.GetString(Key, "");

                if (!string.IsNullOrEmpty(json))
                {
                    try { _file = JsonUtility.FromJson<TouchLayoutFile>(json); }
                    catch { _file = null; }
                }

                // ⚠️ A CORRUPT FILE FALLS BACK TO THE DEFAULTS RATHER THAN THROWING. This is read
                // while the match HUD is being built; an exception here would leave a player on a
                // phone with no controls at all, which is a far worse outcome than a lost layout.
                return _file ??= new TouchLayoutFile();
            }
        }

        /// <summary>Bumped on every change, so a live layer knows to re-apply.</summary>
        public static int Revision { get; private set; }

        public static float Opacity
        {
            get => Mathf.Clamp(File.Opacity, MinOpacity, MaxOpacity);
            set { File.Opacity = Mathf.Clamp(value, MinOpacity, MaxOpacity); Touch(); }
        }

        /// <summary>The size multiplier applied to every control at once.</summary>
        public static float Scale
        {
            get => Mathf.Clamp(File.Scale, MinScale, MaxScale);
            set { File.Scale = Mathf.Clamp(value, MinScale, MaxScale); Touch(); }
        }

        public static TouchTweak TweakFor(Verb verb)
        {
            foreach (var tweak in File.Tweaks)
                if (tweak.Verb == verb.ToString()) return Normalised(tweak);

            return new TouchTweak { Verb = verb.ToString(), Scale = 1.0f };
        }

        private static TouchTweak Normalised(TouchTweak tweak)
        {
            // A row written by an older build, or by hand, may carry a zero scale. Zero is not a
            // legal size and would draw an invisible control that still eats presses.
            if (tweak.Scale <= 0.01f) tweak.Scale = 1.0f;

            tweak.Scale = Mathf.Clamp(tweak.Scale, MinScale, MaxScale);
            return tweak;
        }

        public static void SetTweak(TouchTweak tweak)
        {
            tweak.Scale = Mathf.Clamp(tweak.Scale <= 0.01f ? 1.0f : tweak.Scale, MinScale, MaxScale);

            for (int i = 0; i < File.Tweaks.Count; i++)
            {
                if (File.Tweaks[i].Verb != tweak.Verb) continue;

                File.Tweaks[i] = tweak;
                Touch();
                return;
            }

            File.Tweaks.Add(tweak);
            Touch();
        }

        /// <summary>
        /// Everything back to the layout the game ships with.
        ///
        /// ⚠️ THE ESCAPE FROM A LAYOUT SOMEBODY HAS MADE UNUSABLE, and it is the reason this
        /// screen can be shipped at all. A player who drags THROW off the bottom of the screen
        /// has no way to press THROW, so RESET must be reachable from the SETTINGS side rather
        /// than from a control on the layer itself. `CLAUDE.md` § 6.3: a dead end is a bug.
        /// </summary>
        public static void ResetAll()
        {
            _file = new TouchLayoutFile();
            Touch();
        }

        private static void Touch()
        {
            Revision++;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(File));
            PlayerPrefs.Save();
        }
    }
}
