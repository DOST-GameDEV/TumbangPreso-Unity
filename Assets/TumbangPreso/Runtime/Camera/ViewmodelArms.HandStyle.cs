using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ EACH HERO'S HANDS FALL THEIR OWN WAY.
    ///
    /// Owner, 2026-10-06, of the shared jump, fall and landing motion (`AirMotion`): *"some characters should have their
    /// arms flailing around as theyre falling"*, *"most arms falling animation just swings front and back, not flailing
    /// in circles"*, and *"nemu's sleeves arent separated from her arms so it looks like her arms are just swinging
    /// really fast"*. So a `HandStyle` says how one hero's arms move in a fall: THE FLAIL is a CIRCLE (each fist carried
    /// round a loop, pitch and yaw a quarter turn apart; `Circle` 0 is a paddle), and Nemu does not swing at all, her
    /// sleeves BILLOW.
    ///
    /// ⚠️ THE EFFECTS THAT WERE HERE ARE GONE, TWICE REJECTED. First flat polygons thrown off the hands, then drawn
    /// stickers on quads (a flame, a bolt, a Kuro face): *"these are bland vfx. all you did was add 2d particles"*, and
    /// of the stickers, *"i don't like the sticker effects"*. What a hero has on their hands is now a THING with a body
    /// and a temper of its own, alive in every state and not only in a fall: `ViewmodelArms.HandLife.cs` and one file
    /// a hero beside it (`NemuKuroHand`, `SeanEmberHand` and the rest). The lines below say only how the ARMS move; what
    /// rides on them is in those classes. Do not bring quads back.
    ///
    /// ⚠️ ONE CHARACTER'S ENTRY IS NEVER COPIED TO ANOTHER (AGENTS.md: no cast-wide stamped looks).
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        public sealed class HandStyle
        {
            /// <summary>Degrees each fist is carried round its loop, loops a second, and the second arm's delay in loops.</summary>
            public float Flail, FlailHz = 2f, FlailOffset = .5f;
            /// <summary>1 is a full circle, 0 a straight paddle up and down.</summary>
            public float Circle = 1f;
            /// <summary>Nemu's: how far the sleeves swell across as they billow (no swing at all).</summary>
            public float Billow;
            /// <summary>The base motion's own lift and spread, scaled for this hero.</summary>
            public float Lift = 1f, Spread = 1f;
        }

        private static readonly HandStyle PlainHands = new HandStyle();

        private static readonly Dictionary<string, HandStyle> HandStyles = new Dictionary<string, HandStyle>
        {
            // SEAN. Waits for one opening. He does not flail: both fists stay set, shaking with it.
            ["sean"] = new HandStyle
            {
                Flail = 2.5f, FlailHz = 11f, FlailOffset = .25f, Circle = 1f, Lift = .9f,
            },
            // ZACK. Makes casual plays look difficult. The biggest flail in the cast: both arms round in full circles, one
            // after the other.
            ["zack"] = new HandStyle
            {
                Flail = 26f, FlailHz = 2.6f, FlailOffset = .5f, Circle = 1f, Spread = 1.15f,
            },
            // DANTE. Holds the difficult space. Stone does not flail and barely lifts; he drops like one.
            ["dante"] = new HandStyle
            {
                Flail = 0f, Lift = .6f, Spread = .8f,
            },
            // CHESKA. Reads the space, tidy. A small neat circle, both arms together.
            ["cheska"] = new HandStyle
            {
                Flail = 7f, FlailHz = 1.8f, FlailOffset = 0f, Circle = .6f,
            },
            // NEMU. Hands lost in bell sleeves, which BILLOW (no swing).
            ["nemu"] = new HandStyle
            {
                Flail = 0f, Billow = .16f, FlailHz = 2.2f, Lift = 1.15f,
            },
            // PHAISTER. Sets the stage. She falls like it is part of the act: wide slow circles, one arm then the other.
            ["phaister"] = new HandStyle
            {
                Flail = 16f, FlailHz = 1.1f, FlailOffset = .5f, Circle = 1f, Spread = 1.25f,
            },
            // RAFI. Grew up on a boat deck. He paddles the air like a swimmer who has lost the water (a flatter loop than
            // a windmill).
            ["rafi"] = new HandStyle
            {
                Flail = 22f, FlailHz = 2.0f, FlailOffset = .5f, Circle = .45f, Spread = 1.1f,
            },
            // AMIHAN. Reads the wind. She does not flail, she FLIES: arms out wide and steady.
            ["amihan"] = new HandStyle
            {
                Flail = 4f, FlailHz = .9f, FlailOffset = 0f, Circle = 1f, Lift = .85f, Spread = 1.7f,
            },
            // PAETE. Never hurries. The long braids sway in slow loops, late, like boughs.
            ["paete"] = new HandStyle
            {
                Flail = 9f, FlailHz = .8f, FlailOffset = .3f, Circle = .8f, Lift = .8f,
            },
        };

        /// <summary>The style of the hero whose hands these are; the plain one for anybody without an entry.</summary>
        public HandStyle CurrentHandStyle =>
            !string.IsNullOrEmpty(_currentHeroId) && HandStyles.TryGetValue(_currentHeroId, out var style) ? style : PlainHands;
    }
}
