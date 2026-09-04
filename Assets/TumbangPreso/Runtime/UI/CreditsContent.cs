using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The CREDITS screen, converted from `scripts/ui/credits_panel.gd`.
    ///
    /// ⚠️⚠️ THIS IS LICENCE COMPLIANCE, NOT POLISH. Three CC-BY-4.0 models ship (CROCS,
    /// PANTULOG, IKE — `Art_Direction.md` §4b) and their one requirement is that the author
    /// be reachable from somewhere the game actually ships, not a line in a design doc nobody
    /// plays. This screen is that somewhere. **Do not trim it for layout.**
    ///
    /// ⚠️ EVERY CC-BY LINE BELOW IS THE MODEL'S OWN LICENSE.txt, COPIED VERBATIM, NOT
    /// PARAPHRASED. Each `*_LICENSE.txt` beside the `.glb` spells out the exact credit string
    /// the author asked redistributors to use ("copy paste this credit wherever you share
    /// it"). Reusing their words is what makes this the actual requirement being met rather
    /// than a good-faith summary of it. Do not reword, reflow or abbreviate these strings.
    ///
    /// ⚠️ THE CHIP READS "IKE", NOT "SIKE" — it matches the in-game roster name (renamed
    /// 2026-08-01: the model's wordmark only reads "IKE" legibly in play). The licence body
    /// beside it is untouched, because it quotes the model's real title, which the licence
    /// fixes and this game's roster name does not.
    /// </summary>
    public static class CreditsContent
    {
        private static readonly Vector2 TopCentre = new Vector2(0.5f, 1.0f);
        public const float LogoHeight = 104.0f;

        public readonly struct Credit
        {
            public readonly string Chip;
            public readonly string Body;
            public Credit(string chip, string body) { Chip = chip; Body = body; }
        }

        /// <summary>Verbatim. See the class note before touching a character of these.</summary>
        public static readonly Credit[] CcByCredits =
        {
            new Credit("CROCS",
                "This work is based on \"crocs\" (sketchfab.com/3d-models/crocs-fbede59e03394e928ed0eccf27e8fc23) by fnk (sketchfab.com/fnk), licensed under CC-BY-4.0 (creativecommons.org/licenses/by/4.0)."),
            new Credit("PANTULOG",
                "This work is based on \"Pink Slipper\" (sketchfab.com/3d-models/pink-slipper-af5b6388d4f240389591a4ac09fedf06) by The Withered Rose (sketchfab.com/TheWitheredRose), licensed under CC-BY-4.0 (creativecommons.org/licenses/by/4.0)."),
            new Credit("IKE",
                "This work is based on \"Low Poly Nike Sandals\" (sketchfab.com/3d-models/low-poly-nike-sandals-8e77c949319148afb9134ba13f64046f) by les03 (sketchfab.com/les03official), licensed under CC-BY-4.0 (creativecommons.org/licenses/by/4.0)."),

            // ⚠️⚠️ THIS ONE IS THE PRICE OF THE CONTROLLER MAP'S PICTURE AND MUST NOT BE REMOVED
            // WHILE THAT PICTURE SHIPS. `Asset_Sourcing.md` § 8.1 and
            // `tools/assets/ds4_gamepad_ccby.LICENSE.txt`: the pad on SETTINGS, CONTROLS,
            // CONTROLLER MAP is Tokyoship's drawing under CC BY 3.0, and CC BY's single condition
            // is the credit. Deleting this line and keeping the art is a licence breach rather
            // than a tidy-up. The three above are the same deal for the three slippers.
            new Credit("CONTROLLER MAP",
                "This work is based on \"Dualshock 4 Layout\" (commons.wikimedia.org/wiki/File:Dualshock_4_Layout.svg) by Tokyoship, licensed under CC-BY-3.0 (creativecommons.org/licenses/by/3.0)."),
        };

        /// <summary>Courtesy credits — none of these REQUIRE attribution (CC0 / in-house),
        /// but they sit on the same screen as the CC-BY table rather than in a docs file.</summary>
        public static readonly Credit[] CourtesyCredits =
        {
            new Credit("ENVIRONMENT & KITS",
                "Kenney kits (Mini Characters, City, Suburban, Fantasy Town, Mini Forest, Food, Furniture, Car) — CC0, kenney.nl. Attribution is courtesy, not required."),
            // ⚠️ TWO FACES SINCE 2026-09-03, AND BOTH ARE NAMED HERE. `docs/TODO.md` § 133:
            // Darumadrop ships one weight, so it was setting four-line ability descriptions and
            // faking every bold. It is the DISPLAY face now and Work Sans carries the reading.
            // `Assets/TumbangPreso/Art/ui/fonts/SOURCES.txt` has the licences and the
            // measurements; both are SIL OFL and neither is modified.
            new Credit("TYPEFACES",
                "Darumadrop One (display) — Copyright 2020 The Darumadrop One Project Authors (github.com/ManiackersDesign/darumadrop). Work Sans (text) — Copyright 2019 The Work Sans Project Authors (github.com/weiweihuanghuang/Work-Sans). Both licensed under the SIL Open Font License 1.1."),
            new Credit("AUDIO",
                "All music and sound effects are original. The OST is written by the team; the SFX and ambience beds are synthesised in-house by this project's own tools. No third-party audio ships in this build."),
            new Credit("TSINELAS",
                "This project's own mesh, generated procedurally — not a sourced asset."),
            new Credit("DEVELOPMENT TOOLS",
                "Claude Code (Anthropic) was used as a coding assistant during development — programming, debugging, testing and documentation. It helped write the bot AI that drives the computer-controlled players, wrote the procedural code that generates the lata meshes, this project's own tsinelas mesh and the map geometry, repurposed sourced assets into this game's formats, and wrote the tools that synthesise the SFX and ambience beds. Every skin, texture and drawing is the team's own, and the team wrote the soundtrack. All game logic, mechanics and design decisions are the team's own, and the team takes full responsibility for the code submitted. No generative-AI image, music or video service was used."),
        };

        public readonly struct TeamMember
        {
            public readonly string Name;
            public readonly string Role;
            public TeamMember(string name, string role) { Name = name; Role = role; }
        }

        public static readonly TeamMember[] TeamCredits =
        {
            new TeamMember("MATTHEW LABRADOR",
                "Lead Developer  ·  UI Designer  ·  3D Asset Editor  ·  Poster Design"),
            new TeamMember("PAUL RECIO", "Developer  ·  Video Editor"),
            new TeamMember("HARRY GOMEZ",
                "Composer, Original Soundtrack  ·  Logo & UI Design  ·  Game Voice Over"),
            new TeamMember("CLARENCE PAGADUAN",
                "UI Designer  ·  Game Asset Artist  ·  Audio Composer"),
            new TeamMember("HANS LAO",
                "QA Tester & Validation  ·  Administration  ·  Cinematics Director"),
        };

        public const float ChipWidth = 330.0f;

        /// <summary>
        /// Renders the whole credits body into <paramref name="parent"/>, top-down.
        ///
        /// ⚠️ IT SCROLLS, because the licence bodies are long by requirement and a credit
        /// that cannot be reached is the same as a credit that was never given.
        /// </summary>
        public static void Render(Transform parent)
        {
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewport = viewportGo.GetComponent<RectTransform>();
            viewport.SetParent(parent, false);
            MenuKit.Place(viewport, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1560, 780));
            viewportGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var content = contentGo.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0.5f, 1.0f);
            content.anchorMax = new Vector2(0.5f, 1.0f);
            content.pivot = new Vector2(0.5f, 1.0f);
            content.anchoredPosition = Vector2.zero;

            var scroll = viewportGo.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40.0f;

            float y = -10.0f;

            y = Heading(content, "MADE BY", y);
            foreach (var member in TeamCredits)
            {
                var name = MenuKit.Label(content, member.Name, 28, UiTheme.Cream,
                    TopCentre, new Vector2(0, y), new Vector2(1400, 36));
                name.alignment = TextAnchor.UpperCenter;
                y -= 36.0f;

                var role = MenuKit.Label(content, member.Role, 19, UiTheme.Amber,
                    TopCentre, new Vector2(0, y), new Vector2(1400, 28));
                role.alignment = TextAnchor.UpperCenter;
                y -= 42.0f;
            }

            y = Heading(content, "ASSET LICENCES  ·  CC-BY-4.0", y);
            foreach (var c in CcByCredits) y = Row(content, c, y);

            y = Heading(content, "COURTESY CREDITS", y);
            foreach (var c in CourtesyCredits) y = Row(content, c, y);

            content.sizeDelta = new Vector2(1560, Mathf.Abs(y) + 40.0f);
        }

        private static float Heading(Transform parent, string text, float y)
        {
            y -= 26.0f;

            var heading = MenuKit.Label(parent, text, 26, UiTheme.Amber,
                TopCentre, new Vector2(-160, y), new Vector2(1100, 34));
            heading.alignment = TextAnchor.UpperLeft;

            return y - 46.0f;
        }

        /// <summary>One credit: a chip naming the thing, and the licence body beside it.
        /// The body WRAPS rather than overflowing — a body running off the panel is a credit
        /// that was not actually given.</summary>
        private static float Row(Transform parent, Credit credit, float y)
        {
            var chip = MenuKit.Label(parent, credit.Chip, 21, UiTheme.Cream,
                TopCentre, new Vector2(-600, y), new Vector2(ChipWidth, 32));
            chip.alignment = TextAnchor.UpperLeft;

            var body = MenuKit.Label(parent, credit.Body, 17, UiTheme.Cream,
                TopCentre, new Vector2(240, y), new Vector2(1040, 220));
            body.alignment = TextAnchor.UpperLeft;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            // Height from the wrapped text, so a long licence pushes the next row down
            // instead of printing on top of it.
            return y - Mathf.Max(46.0f, body.preferredHeight + 18.0f);
        }
    }
}
