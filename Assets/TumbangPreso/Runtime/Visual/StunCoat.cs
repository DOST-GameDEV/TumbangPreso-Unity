using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// What each <see cref="StunElement"/> looks like on a body and on the victim's screen.
    ///
    /// ⚠️⚠️ ONE TABLE, TWO CONSUMERS, AND THAT IS THE POINT. `CharacterVisual` paints the body
    /// for everybody ELSE to see and `Hud` paints the vignette for the VICTIM, and the two have
    /// to name the same element with the same colour or the player being held sees one thing
    /// while the player holding them sees another. 🧑 2026-08-26 asked for both halves in one
    /// breath: *"i want them to look frozen or have the element cover them when stunned"* and
    /// *"i want their ui to also have the frozen or stunned effect (depending on the element)"*.
    /// The danger vignette's split rule (a screen effect tells nobody anything about anyone
    /// else) is why there are two halves at all; this is what keeps them in step.
    ///
    /// ⚠️ THE COLOURS COME FROM `UiTheme` WHERE ONE EXISTS. His UI art is the design system
    /// (`docs/VISION.md` § 6) and a stun coat invented in a different palette is the thing that
    /// looks broken rather than the thing that looks new. The hero accents are already tuned
    /// against each other and against the street, so reusing them means the coat cannot collide
    /// with a hero's own kit colour.
    /// </summary>
    public static class StunCoat
    {
        /// <summary>
        /// `Body` tints the character, `Rim` edges it, `Screen` fills the victim's vignette.
        ///
        /// ⚠️ RIM IS BRIGHTER THAN BODY IN EVERY ROW, WITHOUT EXCEPTION. The toon ramp is two
        /// flat bands, so a coated body loses its interior detail and the silhouette is all that
        /// is left to read. The frost term this reuses learned the same thing: its own note says
        /// that without the edge "a frozen body loses its silhouette against a pale background".
        /// </summary>
        public readonly struct Coat
        {
            public readonly Color Body;
            public readonly Color Rim;
            public readonly Color Screen;
            public readonly string Verb;

            public Coat(Color body, Color rim, Color screen, string verb)
            {
                Body = body;
                Rim = rim;
                Screen = screen;
                Verb = verb;
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE VERB IS WHAT THE CARD SAYS, AND IT IS PER ELEMENT ON PURPOSE. 🧑: *"to get
        /// unstunned or unfrozen"*, which is two words for two states because they are two
        /// states. A card reading BREAK FREE over a body encased in ice says less than one
        /// reading SHATTER THE ICE, and the whole reason the element is carried through the stun
        /// at all is so the game can be specific about what is holding you.
        /// </summary>
        public static Coat For(StunElement element)
        {
            switch (element)
            {
                // Cheska. The one that literally freezes, and the only row that keeps the
                // shader's authored frost colours: they were tuned for exactly this and there is
                // no reason to restate them slightly differently here.
                case StunElement.Ice:
                    return new Coat(new Color(0.62f, 0.87f, 0.95f),
                                    new Color(0.85f, 0.98f, 1.00f),
                                    new Color(0.55f, 0.82f, 0.94f),
                                    "SHATTER THE ICE");

                // Sean. Charred rather than burning: a body still on fire would read as taking
                // damage every frame, and this game has no damage.
                case StunElement.Fire:
                    return new Coat(new Color(0.42f, 0.16f, 0.10f),
                                    new Color(1.00f, 0.54f, 0.18f),
                                    new Color(0.86f, 0.34f, 0.12f),
                                    "SHAKE OFF THE BURN");

                // Zack. Locked up rather than coated, so the body keeps most of its own colour
                // and the rim does nearly all the work. Electricity is an edge, not a surface.
                case StunElement.Shock:
                    return new Coat(new Color(0.78f, 0.80f, 0.52f),
                                    new Color(1.00f, 0.96f, 0.42f),
                                    new Color(0.94f, 0.88f, 0.30f),
                                    "BREAK THE CURRENT");

                // Dante. Buried. The heaviest coat in the table, because stone is the one
                // element here that is opaque in life.
                case StunElement.Stone:
                    return new Coat(new Color(0.44f, 0.40f, 0.36f),
                                    new Color(0.72f, 0.62f, 0.48f),
                                    new Color(0.38f, 0.33f, 0.28f),
                                    "DIG OUT");

                // Nemu. Held rather than covered: a spirit does not coat you, it takes hold.
                case StunElement.Void:
                    return new Coat(new Color(0.34f, 0.20f, 0.48f),
                                    new Color(0.72f, 0.45f, 1.00f),
                                    new Color(0.42f, 0.22f, 0.62f),
                                    "PULL AWAY");

                // Phaister. Her branch's magenta-into-gold, which `docs/TODO.md` § 21.5 records
                // as the one hero palette with two hues in it. Nemu and Phaister are the only
                // pair sharing an element, so this row and `Void` above are the two that most
                // need to not look alike.
                case StunElement.Hex:
                    return new Coat(new Color(0.52f, 0.14f, 0.44f),
                                    new Color(1.00f, 0.76f, 0.30f),
                                    new Color(0.62f, 0.16f, 0.52f),
                                    "BREAK THE HEX");

                // ⚠️ `None` IS THE TAG AND IT IS NOT DRAWN FROM THIS TABLE AT ALL. It uses
                // § THE CAUGHT MARK in `Toon.shader`, which drains colour instead of adding one,
                // and it has no verb because it cannot be fought. This row exists so a caller
                // that asks anyway gets something inert rather than a black body.
                default:
                    return new Coat(UiTheme.Ink, UiTheme.Defense, UiTheme.Ink, "");
            }
        }
    }
}
