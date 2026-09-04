# The front end, designed: one theme, five screens that are not each other

**Read `CLAUDE.md` § 6.2, § 6.2b, § 6.2c and § 6.3 first, then `docs/FUTURE.md` § 0.5b.** This
file is those methods applied to the five screens `docs/TODO.md` § 133 covers. It is not a new
set of rules and it does not overrule any of them.

Written 2026-09-03, branch `abilities-rework`, for § 133.

⚠️⚠️ **THE IN-MATCH LAYER IS NOT IN THIS FILE AND MUST NOT BE TOUCHED IN THIS PASS.** § 133.4
draws the line at "is it drawn while a round is live": `Hud`, `AbilityDeckHud`,
`AbilityInspectPanel`, `StatusStack`, `HudDeclutter`, `OffscreenIndicators`, `PlayerNameplate`,
`RoleSwapCard`, `EmoteWheel`, `PausePanel` and `ComicPopup` stay exactly as they are. The HUD
answers to `VISION.md` § 3, which is a different contract from this one.

---

## 0 · The brief, in his own words, because all nine sentences pull at once

| What he asked for | Where it came from |
|---|---|
| *"i want it so that shit isnt overwhelming and that the game is easy to look at"* | § 133.7 |
| *"i want it to feel quirky like the work in progress logo"* | § 133.7 |
| *"a user would be able to find everything on their own bcz these controls are familliar to them already"* | § 133.8 |
| *"i dont want UI to feel repetitive but we can repeat shit ... js figure out where and what will llook good"* | 2026-09-03, this session |
| *"i want our ui for lobby and everything (except for game for now) to all have its own identity"*, and *"i want settings, character select profile to all be under the same theme but feel like their own screens"* | 2026-09-03, this session |
| *"i want it to be quirky and feel filipino-esque ... but dont force the filipino shit, i js want it to be felt from it"* | 2026-09-03, this session |
| *"it should have all the functions of old ui, make sure ntohing in old ui as functions get lost"* | 2026-09-03, this session, and § 133.5 already |
| *"maybe we could use like reoccuring shit to guide ppl"*, relaying Paul Andrei: *"maybe pwede natin iincorporate yung crown thingy sa game"* | 2026-09-03, this session. § 1.1 and § 1.2 |
| *"u can add random shit and designs to the ui too btw to give our screens character, not everything has to be functional"* | 2026-09-03, this session. § 1.3 |
| *"u can also use capital and shit depending on stuff, u figure out where all capital looks best and where it doesnt"* | 2026-09-03, this session. § 3.1 |

⚠️⚠️ **THE FIRST TWO AND THE MIDDLE TWO ARE THE SAME TENSION TWICE, AND RESOLVING IT IS THE WHOLE
JOB.** A screen that answers "quirky" by adding things gets rejected (§ 92, *"theres liek 20 shits
at once"*). A screen that answers "familiar" by making every screen identical is the *"everything
feels repetitive bcz i think u use the same code to generate them all"* complaint that
`CLAUDE.md` § 6.5 already exists to answer.

**The resolution is that repetition and identity live in different layers, and which layer a thing
belongs to is decided by one question: does the player have to LEARN it?**

- **Anything the player has to learn is repeated exactly.** Where BACK is. What a chevron means.
  What the focus ring looks like. Where the one primary action sits. What Escape does. Learning
  these once and having them hold everywhere is the entire content of *"familiar to them already"*.
- **Anything the player only has to LOOK at is different on every screen.** The composition of the
  middle, the ground, the anchor colour, and what kind of object the screen is built around.

⚠️ **So "we can repeat shit" has a precise answer: repeat the CHROME, never the COMPOSITION.**
Forty identical settings rows are correct. Five screens with the same middle are the fault.

---

## 1 · The spine: what is identical on every screen, on purpose

⚠️⚠️ **THIS HALF IS BORROWED, NOT INVENTED, AND THAT IS § 133.8'S INSTRUCTION.** *"Invention is
the expensive option, and this front end has paid for it twice."* Every row below is a convention
a player already owns from another game, so none of it needs teaching.

| Element | Where it sits | Who the player already knows it from | Why it may never move |
|---|---|---|---|
| **Screen name** | Top left, `Display` step, Darumadrop | Every game since about 2010 | It is the answer to "where am I", and a player who has to hunt for that has already lost the screen. |
| **BACK** | Top left, immediately under the name, and Escape always does the same thing | Console UI convention | `ConvertedScreen.CancelTarget` exists because three screens shipped with a dead Escape. A player who learns Escape is reliable and then meets one screen where it is not has learned that it is **unreliable**. |
| **Identity chip** | Top RIGHT, carrying the player's face and name | Overwatch, Valorant, Fortnite, every live game | ⚠️⚠️ **This is `docs/TODO.md` § 96'S FIX AND IT IS THE MOST IMPORTANT ROW IN THIS TABLE.** He commissioned the player hub and then could not find the way into it, because its one door was a corner chip stating a name and a level, which reads as a **status readout** rather than as a door. Top-right with a FACE on it is where every live game puts the way into your profile, and a face is a thing people press. |
| **The one primary action** | Bottom RIGHT of the screen's own content area | Rocket League, Brawl Stars, every console flow | One per screen, always the largest and loudest object, always in the same corner. § 6.2c question 1. |
| **Secondary actions** | Chips in a row to the LEFT of the primary, never above or below it | Same | A chip is visibly a smaller thing than the primary, so the hierarchy survives a photograph. |
| **The four atoms** | `PaperKit.Sheet`, `Tray`, `Chip`, `Row` | n/a, but internally consistent | § 6.5: a chamfer means pressable and a round means furniture. A shape difference survives a photograph and a colourblind player; a fill difference does not. |
| **The chevron** | On the right of any `Row` that opens something | Every settings list on every phone | `PaperKit.Chevron` is one function so the mark cannot become three sizes. A `Tray` with no chevron is a value; a `Tray` with one is a way through. |
| **The focus ring** | `FocusRing`, identical everywhere | Console UI | `CLAUDE.md` § 4a: a pad and a thumb reach every screen by construction, and a focus indicator that changed per screen would be a focus indicator nobody trusts. |
| **The type scale** | Four steps, two faces, `PaperKit.FaceFor` decides | n/a | See § 3. |

⚠️ **AND THE SPINE IS WHERE THE "REPEAT" PERMISSION IS SPENT IN FULL.** These are not merely
similar between screens. They are the same code, at the same size, in the same place, with the
same behaviour. Anything less and the learning does not transfer, which is the only reason the
spine exists.

### 1.1 ⚠️⚠️ THE MARK: one recurring shape that always means "this one"

🧑 2026-09-03, relaying Paul Andrei: *"maybe pwede natin iincorporate yung crown thingy sa game"*,
and his own framing of why it is worth doing: **"maybe we could use like reoccuring shit to guide
ppl"**.

**That is a wayfinding device and it is the strongest idea in this brief**, because it is the one
thing on this list that teaches by repetition rather than by borrowing. The crown is already an
authored asset: `tsinelas_hit.jpg`, the tsinelas with an impact burst behind it, which is the
game's own subject drawn in the logo's hand.

**The rule is one sentence: the mark means THIS ONE, it appears at most once per screen, and it
never appears as decoration.**

| Screen | What wears the mark |
|---|---|
| **Lobby** | Your own seat, out of the four |
| **Character select** | The fighter currently chosen |
| **Settings** | The group currently open |
| **Profile** | The tab currently open |
| **Login** | Nothing. There is no choice on it yet, so the mark would be a lie |

⚠️⚠️ **IT IS A SHAPE, WHICH IS WHY IT IS WORTH MORE THAN AN ACCENT COLOUR.** `CLAUDE.md` § 6.5:
*"a chamfer means pressable and a round means furniture. A shape difference survives a photograph
and a colourblind player; a fill difference does not."* This project has a measured colourblind
problem (§ 16.1) and `FUTURE.md` § 0.5b puts colour LAST of the four ordering tools. A mark that
says "this one" in greyscale is a hierarchy every player has.

⚠️ **AT MOST ONCE PER SCREEN, AND THAT IS THE HALF THAT WILL BE TEMPTING TO BREAK.** The moment
it appears twice it stops meaning "this one" and becomes a bullet, and a bullet is decoration.
§ 92 is what happens when a good element is used everywhere: *"theres liek 20 shits at once"*.

⚠️ **AND IT IS NOT A SECOND DOOR.** § 6.3 forbids adding a door to fix findability. The mark is
not pressable and never navigates; it only ever says where you already are. A player pressing it
should get the thing under it, because there is nothing else there.

### 1.2 The rest of the recurring vocabulary, and every one means exactly one thing

🧑 2026-09-03: *"u figure out what else we can reuse or make reoccur"*.

⚠️⚠️ **THE VALUE OF A RECURRING MARK IS ENTIRELY IN IT MEANING ONE THING, so the list is short on
purpose and each row owns its meaning outright.** A vocabulary of six signs a player learns once
is wayfinding. The same six used decoratively is § 92 again: *"theres liek 20 shits at once"*.

⚠️ **AND FIVE OF THE SIX ARE SHAPES OR STROKES RATHER THAN COLOURS**, which is deliberate.
`FUTURE.md` § 0.5b puts colour LAST of the four ordering tools, this project has a measured
colourblind problem (§ 16.1), and `CLAUDE.md` § 6.5 is explicit that a shape difference survives
a photograph where a fill difference does not.

| The sign | Taken from | It means, and only this | Where it recurs |
|---|---|---|---|
| **The mark** (§ 1.1) | `tsinelas_hit`, the crown | **THIS ONE.** Where you are, what is selected. | At most once per screen |
| ⚠️⚠️ **The heavy uneven outline** | The wordmark: every shape in the logo is held in a deep-red stroke | **THIS IS AN OBJECT YOU CAN ACT ON.** Pressable things carry the stroke; furniture does not. | Every button, chip, field and row. Never a panel, a heading or a caption |
| **The drip** | The wordmark's bottom-right, where the orange runs off | **THERE IS MORE BELOW.** A list that scrolls drips at its bottom edge; one that does not, does not. | Any scrolling region: the settings list, the roster grid, chat |
| **The chevron `›`** | Already in `PaperKit.Chevron` | **THIS OPENS SOMETHING.** A tray without one is a value; a tray with one is a way through. | Every row that navigates |
| **The hatch** | The grey diagonal strokes across the `1` and the blob | **THIS IS NOT AVAILABLE.** Locked, disabled, not yet earned. | Locked loadout alternates, disabled controls, unearned cosmetics |
| **The lean and the sag** | The letters lean; a hung tarpaulin sags | **THIS IS CHROME, NOT CONTENT.** Anything a degree off square is the frame around the thing rather than the thing. | The lobby banner, the profile card. ⚠️ **Never type** |

⚠️⚠️ **THE OUTLINE IS THE MOST VALUABLE ROW AND IT IS ALSO THE ONE MOST LIKELY TO BE BROKEN**,
because a stroke looks like decoration and gets added to a panel to "tie it together". The moment
a non-pressable surface wears it, the sign stops answering "can I press this" and the front end
is back to the state `CLAUDE.md` § 6.2 calls the INTUITIVE failure: *the player cannot predict
what a control does before pressing it*. § 108 is the receipt, an EQUIP button with no `onClick`
listener that looked perfect.

⚠️ **THE HATCH REPLACES A TINT AND THAT IS AN UPGRADE RATHER THAN A SWAP.** `PaperButton`'s own
note already argues it from the other side: *"the disabled state is a pose, not a tint"*, because
`game-ui-design`'s **Color-Only Information** anti-pattern is explicit that a control
distinguishable only by colour is not distinguishable. A hatched control is legibly unavailable in
greyscale, at a glance, to everybody.

⚠️ **WHAT IS DELIBERATELY NOT ON THIS LIST: a second accent, a glow, a badge, a corner ribbon and
a drop shadow.** Each would be a seventh thing to learn, none answers a question the six do not,
and § 133.7 is explicit that the personality is in the shape and the line rather than in the
count.

⚠️⚠️ **THAT PARAGRAPH IS ABOUT SIGNS AND NOT ABOUT ORNAMENT, AND § 1.3 IS THE DIFFERENCE.** The
six above all MEAN something, so a seventh costs a player a seventh thing to learn. Decoration
means nothing, and that is exactly what makes it cheap.

### 1.5 The avatars, because the identity chip needs a face and a screenshot is not one

🧑 2026-09-03, cropping the first identity chip: **"like tf is that pic doing there"**, and
the instruction that follows: *"maybe give them an option to pick from a bunch of cute
profile pics"*.

⚠️ **HE IS RIGHT AND THE PICTURE WAS A SCREENSHOT.** The chip carried a square cut out of
`docs/Godot_Character_Select_References/`, which are full captures of the OLD Godot character
screen: a panel, a heading and a model. Cropping one to 70 units gives a picture of a user
interface, on a user interface.

**There are fifteen drawn avatars now** (`tools/build_avatar_art.py`), and they are DRAWN
rather than cut out for a reason that is not laziness:

- **Two passes tried to cut the twelve heads out of those sheets.** The knockout is solvable:
  the ground is the only cold thing in the frame, so blue-greater-than-red is exact at every
  height of that navy gradient. **The FRAMING is not.** The model stands at a different height
  and scale in every sheet, so half the set came out cropped at the forehead. **A picker is
  twelve things seen together, and twelve things that disagree about where the eyes sit is not
  a set.**
- **Drawn, they are in the logo's hand by construction**: flat fills, a heavy deep-red stroke,
  four unequal corner radii, no ramp and no bevel, which is the same rule
  `PaperCraft.PaintBrand` now draws every button by. So an avatar belongs to this game rather
  than reading as clip art from a pack.
- **Every fill is a `UiTheme` brand constant**, so `CLAUDE.md` § 6.4's ban cannot be violated by
  an avatar, which is not a claim a downloaded icon set could make.
- ⚠️ **The six skin tones are sampled off the twelve Classic portraits rather than invented.**
  `docs/VISION.md` § 6: *nobody's skin is a dial*. The picker offers the range of people who are
  actually in this game.
- **Three of the fifteen are objects rather than people**: the tsinelas, the lata and a chalk
  star. They are the game's own subject, and they are what somebody picks who does not want a
  face.
- **172 KB in the player** for all fifteen, and no licence line to add to the credits screen.

⚠️ **THE AVATAR IS NOT A SEVENTH SIGN.** § 1.2's list is closed and this is not on it: an
avatar means "you", it is only ever drawn on the identity chip and in the picker, and it
carries no meaning a player has to learn.

### 1.4 How a button is built, because it is not built the way the old ones were

🧑 2026-09-03, after the font pass landed on its own: **"the darumadrop buttons AS TEXT stay, i
wanted u to remake all buttons in a diff style that feels like my logo bruh"**, and before that
*"can u overhaul it like all of it gang, i dont wanna use the old colors anymore"*.

⚠️⚠️ **THE LETTERING WAS NEVER THE COMPLAINT. THE SURFACE WAS.** Every pressable thing in this
front end was a LIT SOLID: a value ramp down its face, a bright keyline outside a dark rim, a
cast shadow under it. That vocabulary is `WoodCraft`'s, it came from sampling his own
`BUTTON LONG.png`, and it is faithful to that art. **His logo is drawn by completely different
rules.** There is no ramp anywhere in the mark, no bevel and no lit edge; every shape is a flat
colour held inside a heavy irregular line, and the only depth in the whole drawing is a darker red
bar tucked inside the bottom of each letter.

**So `PaperCraft.Surface.Brand` is four things and none of them is a gradient:**

| Part | The number | Where the number came from |
|---|---|---|
| **The stroke** | **8.5 per cent** of the button's height, in deep red | The mark's line measured against its letter height. Deep red is **34.3 per cent of the whole drawing**, more area than any fill: the line IS the object and the colour inside it is the hole. |
| **The fill** | One flat value, Chartreuse for the one primary and Honey Quartz for every chip | § 4's role table. No ramp, because there is not one in the mark. |
| **The under-bar** | **5.5 per cent**, in rim red, sitting a unit or two clear of the stroke rather than touching it | It is drawn that way under every letter, and it is the only depth cue the logo has. |
| **The wobble** | **3 units**, sampled at two frequencies | ⚠️ Small on purpose: the mark's line varies by a few per cent of its own thickness. **What reads as hand-drawn is that the variation is irregular, not that it is large.** |

⚠️⚠️ **THREE MORE PARTS LANDED 2026-09-03 AND ALL THREE ARE THE SAME COMPLAINT ANSWERED:
"give the buttons a bit more personality and cuter", "i wanted u to remake all buttons".**
The first version had the right vocabulary and drew it too evenly, so a screen of them still
read as one button stamped nine times.

| Part | The number | Where the number came from |
|---|---|---|
| ⚠️⚠️ **The stroke's weight VARIES along its own length** | ± **26 per cent** of itself, sampled on y at 0.055 | § 133.13 names this in the rejection in as many words. Measuring the deep red perpendicular to its own run around the T and the P, it reads **19 to 33 px** on a mark whose letters are 300 px tall. A constant stroke is a BORDER; a varying one is a line somebody drew, and it is the single most characteristic thing about the mark. |
| ⚠️⚠️ **Four DIFFERENT corner radii** | each **0.74 to 1.32** of the base 0.22 × height | The mark has no two corners alike: the T's shoulders are near square and its foot is round; the P's bowl is round and its stem is cut flat. `PaperCraft.Depth` folds the rect with `Mathf.Abs`, so it *cannot* tell one corner from another; `Depth4` is the new measurement and the old one is untouched for furniture. |
| ⚠️⚠️ **Every control family is drawn by its OWN hand** | a stable seed off surface + accent + height | *"the issue with old UI is everything feels repetitive bcz i think u use the same code to generate them all"* answered at the level it was asked. START MATCH, JOIN, CHAT and BACK get four different silhouettes and four different edges, and each gets the SAME one every time. ⚠️ **The seed excludes the POSE**, or a button would change shape under the pointer. |
| **The bar's ends taper** | full weight **9 units** in | Legal on x where the stroke is not, because only the middle COLUMN of a nine-slice is stretched and the taper lives inside the preserved caps. In the mark the bar is heaviest in the middle and lifts off the page at both ends, because it was one stroke of a marker starting and stopping. |
| **Hover presses the pen harder** | stroke × **1.12** | On a construction with no ramp and no keyline, a fill lift is the smallest signal the front end has. A stroke that thickens survives a photograph and a colourblind player; a tint does not. The line COLOUR is still untouched: a stroke that changes hue reads as a different control rather than as the same one being pointed at. |

⚠️⚠️ **THE WOBBLE VARIES WITH Y AND NEVER WITH X, AND THAT IS A SLICING CONSTRAINT RATHER THAN A
DESIGN ONE.** These sprites are nine-sliced horizontally, so the middle column is STRETCHED to
whatever width the caller asks for: anything that varied along x would smear into streaks. Varying
with y makes the left and right edges hand-drawn and leaves the top and bottom steady, which is
also how the mark itself reads, its horizontals being far steadier than its verticals.

⚠️ **THE PRESS IS THE OBJECT SITTING DOWN ONTO ITS OWN UNDER-BAR.** There is no highlight to move
and no ramp to shift, so the bar going away IS the press. That is also the honest reading of the
mark, where the bar is what holds each letter up off the page.

⚠️ **AND THE LIT-SOLID PAINTER IS KEPT, NOT DELETED.** `PaperCraft.PaintAction` still holds every
number measured off `BUTTON LONG.png` and the three rejections that tuned them (*"i js wanted u to
make it mroe 3d"*, *"i prefer the old sharper edges on it"*, *"ugly shadows and edges"*). Nothing
dispatches to it. **He has reversed a look before**, by name, on the character select screen; if
the slab is ever wanted again it is there intact with its receipts, and rebuilding it from the
comments would lose the measurements.

### 1.3 Decoration: allowed, and here is where it goes

🧑 2026-09-03: **"u can add random shit and designs to the ui too btw to give our screens
character, not everything has to be functional"**.

⚠️⚠️ **THAT PULLS AGAINST § 92 AND THE RESOLUTION IS NOT A COMPROMISE, IT IS A PLACE.** *"Theres
liek 20 shits at once"* was six BUTTONS in six visual languages: every one of them was a thing the
player had to look at, decide about and dismiss. **A drawing that means nothing costs none of
that.** So the rule is not "how much" but "where":

> **Decoration is free where nothing has to be read, and expensive where something does.**

**Where it goes, in order of how much it buys:**

1. ⚠️⚠️ **EMPTY STATES, WHICH ARE THE BEST PLACE IN THE WHOLE FRONT END.** An empty chat, a
   server list with nothing in it, a fresh career: by definition there is nothing to read, so a
   drawing there costs exactly zero legibility. `FUTURE.md` § 0.5b question 3 already says the
   empty state is *"the state most players see first and it is the one that gets designed last"*,
   and § 118.1 row 1 is the receipt: the lobby chat is *"the only surface on the lobby that looks
   unfinished rather than quiet"*. **An empty box with a drawing in it reads as made; an empty box
   reads as broken.**
2. ⚠️⚠️ **THE DEAD GROUND, AND § 118.1 ROW 2 SAYS EXACTLY HOW MUCH OF IT THERE IS.** *"The screen
   is four corners and a hole"*: **680 units of nothing** down the lobby's left side and **475**
   down its right, measured. That is not space that needs protecting, it is space that is already
   doing nothing.
3. **The margins and the corners**, outside every content rect.
4. **The edges of surfaces**: the drip running off a card's bottom, the ties at each end of the
   lobby's banner, a torn corner. These are part of the object rather than things beside it.
5. **Loading and transitions**, where waiting is the only thing happening.

**Where it may never go, and every one of these is a receipt:**

- ⚠️ **Between rows in a list.** § 92.3b: grouping without collapsing does not fix a wall, it
  aligns it, and an ornament between rows makes the wall taller.
- ⚠️ **Beside a value.** § 94.7 is seven readability faults at once with every probe green,
  including a value drawn 1600 px from its own label.
- ⚠️ **Behind or on a control's own lettering.** That is the § 6.4 amber-on-cream problem with a
  picture instead of a colour.
- ⚠️ **Anywhere that raises the count of things a player has to SCAN to find what they came for.**
  That is the actual test, and it is `CLAUDE.md` § 6.2's third claim: *never overwhelming*.

**And one number, so this is checkable rather than a matter of taste:**

⚠️⚠️ **AN ORNAMENT SITS UNDER 1.5:1 AGAINST ITS OWN GROUND, OR IT IS OUTSIDE EVERY CONTENT RECT.**
Below that ratio it cannot compete with anything at `Caption` or larger, which all measure 5:1 or
better; outside the content rects it is not competing at all. **A drawing that satisfies neither
is not decoration, it is a seventh sign**, and it belongs in § 1.2 with a meaning attached or
nowhere.

**What to draw, since the vocabulary already exists:** the tsinelas mark faded into a ground, the
hatch at low contrast, chalk scribbles on the settings ledger (this game is chalk on asphalt), the
drip, a hand-drawn corner flourish on the login card, the banner's ties. ⚠️ **All of it is the
logo's hand**, which is what makes it character rather than clip art, and none of it is a new
element to learn.

---

## 2 · The identity: what is different on every screen, on purpose

Each screen gets four things of its own, and **only four**, because § 133.7 is explicit that
personality is in the shape and the line rather than in the count.

1. **An ANCHOR COLOUR**, one, from the logo's five. It is the screen's ground or its marker, never
   both, and no two screens share one.
2. **A LAYOUT ARCHETYPE**, borrowed whole from a game that already solved this exact job.
3. **A HERO ELEMENT**: the one object the screen is built around, and it is a different KIND of
   object on each of the five.
4. **A MOTIF**: one hand-drawn mark taken from the logo, spent once.

⚠️⚠️ **THE HERO ELEMENT BEING A DIFFERENT KIND OF OBJECT IS WHAT ACTUALLY STOPS THE SCREENS FROM
READING AS ONE SCREEN FIVE TIMES**, and it is worth more than the colour. A stage, a room, a form,
a card and a ledger are five different things before a single pixel is coloured. Five differently
tinted lists are one thing.

### The five, at a glance

| Screen | Hero element | Anchor | Archetype borrowed from | Motif from the logo |
|---|---|---|---|---|
| **Login** | The wordmark itself | Honey Quartz | Riot / Supercell sign-in: narrow form column beside art | **The drip** |
| **Lobby** | The live 3D cast in the street | Chartreuse | Among Us room code + Fall Guys lit room | **The hung banner** |
| **Character select** | The chosen fighter's model, lit, on a dark stage | Army | Fighting-game select: big centre, roster grid under | **The heavy outline** |
| **Settings** | Nothing. It is a ruled list and that is correct | Khaki | Valorant / PUBG rows: label left, control in a FIXED column | **The chalk rule** |
| **Profile** | One large player card | Persimmon | A trading card, with tabs down the side | **The lean** |

⚠️ **THE DEEP RED IS NOT AN ANCHOR AND IS DELIBERATELY LEFT OFF THAT COLUMN.** In the logo it is
the OUTLINE, not a fill, and it is what holds every other colour in. So it is the front end's
outline too: the stroke on the primary action, the selection frame, and the one destructive
control. Making it a screen's ground would spend the thing that ties the five together.

⚠️⚠️ **AND NONE OF THIS RELAXES `CLAUDE.md` § 6.4.** No blue, no navy, no cold grey, in any layer,
in any of the five. Persimmon, Honey Quartz and the deep red are warm; Chartreuse and Army are
yellow-green and olive, which are warm-side greens. Nothing in the new palette tests the ban, and
he has had to state it six separate times.

---

### 2.1 LOGIN, "the shutter"

- **The ONE thing:** get in. Not "manage your account".
- **First press, and can they guess it:** the primary in the bottom right of the form card. At
  boot it reads PLAY AS GUEST and at every other entry it reads SIGN IN.
- **Not needed right now:** everything about accounts. Linking, upgrading and deleting all live on
  the profile screen's ACCOUNT tab, which is where somebody goes on purpose.
- **Out in one press:** ⚠️ At boot there is no BACK, so the escape IS the guest button.
  `FUTURE.md` § 0.5b and § 97: **the escape from any gate is one press and never needs the
  network**, and the nationals in General Santos City are why that has an assertion rather than a
  paragraph.
- **Hero element:** the wordmark, large and off-centre, with its chartreuse blob behind it exactly
  as drawn. ⚠️ **This is the ONLY screen where the logo is the hero.** Putting it on all five is
  how a brand stops being noticed.
- **Motif, the drip.** ⚠️⚠️ **BUILT 2026-09-03, AND IT LANDED ON THE SEAM RATHER THAN ON THE
  FORM CARD.** This row used to read *"the drip becomes the top edge of the form card"*, and
  building it found a bigger fault one object over: **the column and the key art met at a
  perfectly straight vertical line down the middle of the window**
  (`Logs/shots-runtime/SignInBoot-v83.png`). Every rect fitted its box and every colour was in
  the palette, and the one edge the player actually looks at was the one edge in the whole design
  that no hand drew. `BrandMarks.ColumnEdge` is that edge now, torn, and the drip is a **bulge in
  it** rather than an object beside it.
- ⚠️ **AND THE DRIP HAD TO BE PART OF THE EDGE, WHICH TOOK TWO RENDERS TO LEARN.** Drawn as its
  own sprite hung against the seam it came out as a flat-topped bar over a circle: `-v87.png` and
  `-v88.png` both read it as an **exclamation mark**, because two shapes butted against a third
  have two seams of their own and the eye finds all three. **In the wordmark the drip is not a
  blob placed under the letters, it is the OUTLINE running off the corner and gathering at the
  end**, so one continuous boundary is both the more faithful drawing and the one that reads.
- **⚠️ Its whole history is a warning and both halves are in `CLAUDE.md` § 6.2c.** § 100: the
  column was 38 per cent of the window around a 420-unit form, which on his window was 860 units
  of surface around a form that never grew. It is 580 now, which is the form plus one margin
  either side. **The art must be fitted to the region it is SEEN in**, not to the whole canvas,
  because the column covers a third of it.
- **⚠️ Every state gets designed and rendered**, which is § 6.2b's first row and was first written
  about this exact screen: `Open()`, `OpenForUpgrade()`, `OpenAtBoot()` and welcome-back. The
  boot state has been the single most-seen screen in the game since § 114.5, and it is the state
  nobody had ever looked at.

### 2.2 LOBBY, "the room"

- **The ONE thing:** START MATCH. Settled in § 118.2 and unchanged.
- **First press:** for the host, the primary. For everyone else it is READY, in the same slot.
- **Not needed right now:** the match rules, which collapse into the settings drawer with a
  one-line summary on the header. § 6.2 question 3: *a group closed by default with a one-line
  summary on its header beats the same rows always open*, and the summary is what makes it worth
  opening.
- **Out in one press:** BACK, top left, and Escape.
- **Hero element:** the cast, standing in the street, live and in 3D. ⚠️ **This is the only screen
  in the front end with a living middle**, and § 118.1 row 2 is what the room looked like without
  one: *"the screen is four corners and a hole"*, 680 units of nothing down the left and 475 down
  the right.
- **Anchor, Chartreuse:** ⚠️ **this REPLACES `UiTheme.MenuGreen` as the action colour rather than
  joining it.** His authored `JOIN BUTTON.png` and the PLAY pennant are already green, `CLAUDE.md`
  § 6.5 names green as his primary, and Chartreuse is the logo's own yellow-green. So the change
  is a shift of the same role to the settled palette, not a new hue. `MenuGreenFace` is the
  measured peak of his own button and is what any new value is checked against.
- **Motif, the hung banner:** the top band is a tarpaulin strung between two points, so its bottom
  edge sags a little rather than ruling straight across. **This is the whole of "filipino-esque"
  on this screen and it is not labelled as such**: a vinyl tarp over a barangay court is a thing
  the room is made of, not a decoration applied to it. The room code lives on it, which is Among
  Us's mechanism and the answer to § 118.1 row 7 (*`tap to copy` at 15 units on the one control
  the screen exists to produce*).
- **⚠️ § 118.1 lists eight faults and this composition has to answer them, not step around them.**
  Rows 2, 3 and 7 are answered above. Row 1 (the chat is a placeholder) needs a resting state that
  looks finished when empty, which is § 0.5b question 3. Row 4 (two near-twin wooden rows) is a
  shape problem and `PaperKit`'s atoms already solve it. Row 5 (BACK competes with the tab row) is
  answered by the spine: BACK is top left and tabs are not. Row 6 (nothing moves) and row 8 (the
  version stamp sits on nothing) are still open.

#### 2.2b The lobby is THREE screens and the tarpaulin is the slot that changes

🧑 2026-09-03, looking at the first composition: *"thoroughy think abt how shit would look
too for each thing, like lets say u click ranked wtf would show?"*

⚠️⚠️ **THAT QUESTION HAD NO WRITTEN ANSWER AND THE CODE ANSWERED IT BY HIDING THINGS.**
`LobbyChrome.Parts.SetMode` switches nine objects off and on: the room sign, the chip row,
the tier plate, the settings chip, the ranked rule line, the whole mode column in practice.
Nothing said what the screen IS in each mode, so each mode was the custom screen with holes
in it, and a hole is exactly what § 118.1 row 2 measured as *"four corners and a hole"*.

**The composition does not change between modes. One slot does.** The tarpaulin's middle is
a single slot carrying THE ONE FACT THAT MODE EXISTS TO PRODUCE, at the same size, in the
same place, in the display face. That is § 0's rule doing real work: the chrome repeats,
the content is what differs, and a player who has learned where to look has learned it once.

| | **CUSTOM** | **RANKED** | **PRACTICE** |
|---|---|---|---|
| **The tarp's slot** | `ROOM CODE` eyebrow, **GKXB** at Display, `tap to copy` under it | `YOUR TIER` eyebrow, **UNRANKED** at Display, the party rule under it | `PRACTICE` eyebrow, **OFFLINE** at Display, `nothing here is recorded` under it |
| **The primary** | START MATCH | FIND MATCH | START MATCH |
| **Chips left of it** | JOIN, CHAT | CHAT only ⚠️ **JOIN goes, because a code cannot be joined in ranked and a control that is present and refuses is `CLAUDE.md` § 6.2's INTUITIVE failure** | neither |
| **The row above the primary** | MATCH RULES, closed, with its summary | the ranked rule line, which states the party rule BEFORE the press (§ 119.8) | MATCH RULES, closed |
| **The seats** | four tags, yours wearing the mark | four tags, yours wearing the mark | four tags, three of them BOT |
| **The mark** | your seat | your seat | your seat |

⚠️ **THE TIER IS NOT A SECOND PLATE AND THAT IS THE WHOLE POINT.** `BuildTierPlate` draws a
152-unit card in the right-hand column carrying `YOUR TIER / UNRANKED / note`, which is a
third object in a column that already had two, and its own comment records the note being
truncated to nothing twice because the card was too short for it. **On the tarp it is the
same three lines in a slot that is already 170 units tall and already the thing the eye goes
to**, so the card, its height arithmetic and both of its recorded faults stop existing.

⚠️ **AND NOTHING IS LOST, WHICH IS THE ONE THING THAT MAY NOT HAPPEN HERE.**
`PaperPurityProbe.NothingOnTheInventoryDisappeared` holds all 338 controls captured before
the pass; `TierValue` and `TierNote` keep their names and their handlers and move parent.

### 2.3 CHARACTER SELECT, "the stage"

- **The ONE thing:** the fighter you are choosing, as a lit model. Not the grid of the others.
- **First press:** a face in the roster grid.
- **Not needed right now:** the loadout. It is its own board behind its own door, which § 132 just
  rebuilt.
- **Out in one press:** BACK, and Escape.
- **Hero element:** the model, centre, lit, large.
- **⚠️⚠️ IT IS THE ONLY DARK SCREEN AND THAT IS AN INSTRUCTION RATHER THAN A CHOICE.** 🧑 2026-09-02,
  sending a capture of the pre-paper version: **"it used to look really good here, maybe it can
  retain old brownn color"**, and the scope in his own words, *"js the character select"*.
  `ConvertedCharacterSelect.Wire` carries the design argument (a picker is a stage and a stage is
  dark) and `PaperPurityProbe.Walk` carries the exemption by node name. **Army is the olive that
  ground becomes; it is a darkening of a decision already made, not a reopening of it.**
- **Motif, the heavy outline:** the selection frame is the wordmark's own stroke, thick and
  uneven, drawn around the chosen face. ⚠️ **This adds character without adding a single thing to
  read**, which is § 133.7's test, and it is a SHAPE difference, so it survives a photograph and a
  colourblind player where a tint would not.
- **⚠️ Persimmon is the marker here and it is spent once.** § 118.4's rule the right way up: on a
  dark ground the marker is the one warm thing, exactly as amber was on the old board.

### 2.4 SETTINGS, "the ledger"

- **The ONE thing:** the setting the player came to change. ⚠️ **A settings screen has no primary
  action and pretending otherwise is a fault**, which is why APPLY is a chip and not a hero.
- **First press:** the tab for the group they want. The tabs are the screen's own navigation and
  they read as tabs.
- **Not needed right now:** every group except the open one. ⚠️⚠️ **GROUPING WITHOUT COLLAPSING
  DOES NOT FIX A WALL OF NUMBERS, IT ALIGNS IT** (§ 92.3b, and `FUTURE.md` § 0.5b question 5).
- **Out in one press:** BACK, and Escape.
- **Hero element:** deliberately none. ⚠️ **This screen's identity is that it is the calmest thing
  in the game**, and § 133.8 says so in as many words: *"a settings screen that looks like a
  settings screen is not unimaginative, it is finished."*
- **Archetype:** Valorant and PUBG's full-width rows. ⚠️⚠️ **WHAT TRANSFERS IS THE FIXED COLUMN,
  NOT THE ALIGNMENT**, and § 94.7 fault 1 is what copying the alignment without the column looks
  like: a value drawn 1600 px from its own label. `UiRows` is that column, it takes no offsets,
  and § 6.3 says a screen not built out of it is not a settings-shaped screen.
- **Motif, the chalk rule:** the line between two groups is drawn rather than ruled, so it wavers
  by a pixel or two along its length. **One mark, repeated, is the entire decoration budget of
  this screen.**
- **⚠️ The destructive control is not a peer of the safe ones**, which is § 0.5b question 4 and a
  fault this project has already shipped: DELETE ACCOUNT once sat between PLAY AS GUEST and CLOSE
  at identical size, one misclick from a lost career. RESET ALL takes the deep red and sits apart
  from APPLY.
- **⚠️ § 121.8 is still 🧑's open call and it belongs here.** Whether `PaperKit.Caption`'s 16 is
  too small was measured against Darumadrop, and this pass changes the face those captions are
  set in. `Attention.md` § 3 says to answer it against the NEW font, not before it.

### 2.5 PROFILE, "the card"

- **The ONE thing:** who you are. ⚠️ **`FUTURE.md` § 0.5b: "identity as a thing to LOOK at, then
  press", and "it states something before it offers something, which no button labelled ACCOUNT
  can."**
- **First press:** a tab down the side.
- **Not needed right now:** the account machinery, which is one tab rather than the front page.
- **Out in one press:** `HubClose`, and Escape.
- **Hero element:** one large card carrying the face, the name and the level, built as a single
  object rather than as a row of chips. It is the only screen in the game shaped around one big
  card.
- **Motif, the lean:** the card sits about a degree off square, so it reads as placed rather than
  generated. ⚠️⚠️ **NOT THE TEXT.** Rotated type is unreadable type, and `AspectRatioProbes`
  measures what a label needs rather than what it looks like. The CARD leans; every word on it
  stays level.
- **⚠️⚠️ THE DOOR IS THE WHOLE PROBLEM ON THIS SCREEN AND IT IS NOT ON THIS SCREEN.** § 96: the
  hub had exactly one door and the person who commissioned it never found it. The fix is in the
  spine, § 1: the identity chip is top right, it carries the player's FACE, and it is in the place
  every live game puts it. ⚠️ **AND NO SECOND DOOR MAY BE ADDED TO HELP**, which is exactly how
  § 92's six-button panel happened. Fix the door or move it.

---

## 3 · Type: two faces, and which one is deciding

⚠️ **`Assets/TumbangPreso/Art/ui/fonts/SOURCES.txt` carries the licences, the measurements and the
three faces that were rejected.** This is only the design rule.

| Step | Units | Face | What it is for |
|---|---|---|---|
| `Display` | 44 | **Darumadrop** | One per screen. The wordmark's neighbour, the room code. |
| `Title` | 26 | **Darumadrop** | The name of a thing: a heading, a player, a hero. |
| `Body` | 20 | **Darumadrop** | A row, a button's lettering, a list entry. |
| `Caption` | 16 | **Nunito** | The SUB line: a hint, a placeholder, a field's label, an ability description, the quiet second line under a row. |

`PaperKit.FaceFor` is the whole rule and it takes the SIZE, so a caller never chooses and
therefore can never choose wrong. `CLAUDE.md` § 4a: the answer is construction, not discipline.

⚠️⚠️ **THE BOUNDARY WAS AT `Title` FOR ONE AFTERNOON AND 🧑 MOVED IT, AND HIS REASON DEFINES WHAT
THE SECOND FACE IS FOR.** At `Title` the split put `Body` 20 into Nunito, which is most of the
lettering in the game: every settings row, every button, every list entry. He looked at it and
said **"ur over replacing fonts, i lowk js wanted u to replace sub fonts with the new font, not
everything gang"**, and of the login screen, *"i think everything here in darumadrop looked good,
just change your username to the sub font"*.

**So Nunito is the SUB font, not the body font.** Darumadrop keeps everything a player looks AT,
which is most of the front end; Nunito takes the things a player reads underneath it. ⚠️ **The
fault § 133 exists for is still fixed either way**, because that fault was the SMEAR:
`FontStyle.Bold` on a face with one weight, which `MenuKit.Apply` makes unreachable on both sides
of the boundary. And the prose that genuinely needed a reading face, the four-line ability
descriptions, is authored at `Caption`, so it is still Nunito.

⚠️ **A LABEL THAT BYPASSES THE KIT BYPASSES THE RULE.** The login screen's USERNAME and PASSWORD
captions are built through `MenuKit.Label` rather than `PaperKit.Ink`, so `FaceFor` never ran on
them and they stayed in the display face while every other caption moved. He spotted it in a
render. Same class of miss as the converted `InputField` that kept Unity's blue selection
highlight: **the kit is only a guarantee for the callers that go through it.**

⚠️⚠️ **AND ALL-CAPS IS NOW A DISPLAY DEVICE RATHER THAN A DEFAULT, WHICH IS A MEASUREMENT AND NOT
A TASTE.** Darumadrop's caps are unusually narrow against its own lowercase (x-height over cap
height is **0.833**, where a text face is nearer 0.70). Setting the same string in Nunito
therefore costs width, and it costs it **only in capitals**:

| String | Growth, Darumadrop to Nunito |
|---|---|
| `Master volume` | **+1.9%** |
| `MASTER VOLUME` | **+12.1%** |
| `Slams the ground and knocks every attacker back three metres.` | **+2.8%** |
| `PRESS START TO HOST A GAME` | **+14.4%** |
| `BACK TO LOBBY` | **+13.0%** |

`scratchpad/fontsrc/widths.py` re-runs it. **So a body-step label that stays in capitals is a
14 per cent overflow risk, and one that moves to sentence case is a 2 per cent one.** That is
§ 133.3's silent failure with a number on it: `MenuKit.Label` overflows rather than wrapping, so
the row simply draws over its neighbour.

⚠️ **The design answer and the safety answer are the same answer, which is why this is worth
doing rather than working around.** Capitals stay where Darumadrop is drawing them, so no width
changes at all; what moves to sentence case is the reading matter, which is calmer, which is
*"easy to look at"*.

### 3.1 Where ALL CAPS is right, and where it is not

🧑 2026-09-03, having seen the width table: *"u can also use capital and shit depending on stuff, u
figure out where all capital looks best and where it doesnt"*, with a reference showing a heavy
hand-drawn **HARRY** over a small letterspaced **GOMEZ**.

⚠️⚠️ **THAT REFERENCE IS THE RULE IN ONE PICTURE, AND IT IS TWO DIFFERENT USES OF CAPITALS RATHER
THAN ONE.** The name is capitals because it is a name being looked at. The line under it is
capitals because it is a LABEL, and it earns them by being short, small, letterspaced and quiet.
Neither is a sentence, and that is what they have in common.

**So the test is not the size, it is whether the string is a LABEL or a SENTENCE**, with a width
threshold underneath it so the rule cannot be argued with.

| Use | Case | Face and step | Why |
|---|---|---|---|
| A screen heading, a hero or player name, the room code | **ALL CAPS** | Darumadrop, `Title` or `Display` | Darumadrop draws these, so capitals cost nothing in width, and a display face set in lowercase is a display face doing nothing. This is **HARRY**. |
| An eyebrow: a short label directly above or below a name | **ALL CAPS, letterspaced** | Nunito Bold, `Caption` | This is **GOMEZ**. Two words at most. Letterspacing is what makes short capitals read as a label rather than as shouting, and at that length the width cost is a handful of units. |
| A chip or button's lettering | **ALL CAPS** | Darumadrop, `Body` | BACK, JOIN, READY, APPLY, START MATCH. ⚠️ **The width table does not apply here any more**: the button step stayed in Darumadrop, so nothing grew. 🧑 confirmed these by eye: *"i think everything here in darumadrop looked good"*. |
| A settings row label | **Sentence case** | Darumadrop, `Body` | ⚠️ **A settings screen is a long list that is SCANNED**, and lowercase word-shapes are most of what makes a list scannable. `Master volume` reads faster than `MASTER VOLUME`, and this is a case-only change with no face change under it. |
| Any sentence: a hint, a blurb, an ability description, chat, an empty state | **Sentence case** | Nunito, `Caption` | Capitals destroy word-shape reading, which is the entire job of the sub face. These are also the longest strings in the game, so they carry the whole width risk, and they are the step that actually changed face. |
| Lettering inside an already-loud container | **Sentence case** | Nunito | ⚠️ Capitals on a chartreuse primary with a deep-red stroke is three emphases spent on one thing. § 6.2c question 1: if two things are competing, one of them is decoration. |

⚠️⚠️ **THE WIDTH TABLE ABOVE IS NOW A REASON RATHER THAN A CONSTRAINT, AND SAYING SO MATTERS.**
It was measured when `Body` was going to be Nunito, and it is what made the caps question worth
asking at all. Since the boundary moved to `Body`, **the only step that changed face is
`Caption`**, and captions are short. So no button and no settings row grew by a single unit, and
the rule above is kept because it is still the right typography, not because anything would
overflow without it.

⚠️⚠️ **AND CAPITALS ARE NOW A DEVICE RATHER THAN A DEFAULT, WHICH IS THE POINT.** The old front
end shouted everything, so nothing was louder than anything else. Capitals used on names, on
eyebrows and on short buttons and nowhere else means a capitalised word actually marks something,
which is the same argument `CLAUDE.md` § 6.4 makes about spending the accent once.

---

## 4 · Colour: five slots, and where each is spent

✅ **THE HEXES ARE IN, AND THEY WERE READ RATHER THAN TYPED.** § 133.1: *"READ THE HEXES OFF THE
COMMITTED FILE. DO NOT TYPE THEM IN BY EYE AND DO NOT SAMPLE A CHAT THUMBNAIL."*
`tools/read_brand_palette.py` clusters the committed artwork's flat fills, and it **agreed with
itself across two independently drawn files**, which is what makes them a palette rather than a
sample. `UiTheme`'s brand block is the one place they live and `CLAUDE.md` § 6.4's list moved in
the same commit.

| Slot | Hex | Share of the logo | Role in the front end | Spent on |
|---|---|---|---|---|
| **Honey Quartz** | `#FCD39F` | 23.1% | The paper. The ground of every light screen, and the quiet button fill. | Login, lobby chrome, settings, profile, every secondary chip |
| **Chartreuse** | `#D6CE01` | 17.0% | The ACTION. One per screen, the primary only. | Every screen's one primary |
| **Persimmon** | `#FD8041` | 5.7% | The MARKER. The one value or selection that matters. | Character select's selection, profile's accent |
| **Golden** | `#F5B521` | 4.2% | The front end's gold, replacing amber. ⚠️ `UiTheme.Amber` stays `ffba00` because the HUD reads it 15 times. | A value, a highlight |
| **Rim red** | `#C32E0D` | 3.8% | The under-bar on every button, and the lit state of the outline. | Every brand surface |
| **Khaki** | `#E8C77E` | derived | The quiet ground: a tray, a sunk row, a disabled face | Settings' ruled ground |
| **Army** | `#B3A828` | 1.4% | The dark ground, and the only one | Character select's stage |
| **Deep red** | `#980715` | 34.3% | The OUTLINE, everywhere, and the destructive control | Every button's stroke, the selection frame, RESET ALL |

⚠️⚠️ **THE OUTLINE IS THE BIGGEST AREA IN THE LOGO AND IT IS NOW THE BIGGEST AREA IN THE FRONT
END, WHICH IS WHY THE BUTTONS HAD TO BE REDRAWN RATHER THAN RETINTED.** Deep red is **34.3 per
cent of the mark**: more of it than any fill. That is the single measurement that says the logo is
a DRAWING rather than a set of shapes, and a button repainted in the palette but still built as a
lit slab with a ramp and a keyline reads as a different object beside it. `PaperCraft.Surface.Brand`
is the answer, and 🧑 asked for it in as many words: *"i wanted u to remake all buttons in a diff
style that feels like my logo bruh"*.

⚠️ **ONE ROLE, ONE COLOUR, AND THE ROLES ARE THE SAME ON ALL FIVE SCREENS.** That is what makes
five different-looking screens read as one game: a player learns that chartreuse means "this is
the button" once, and it holds. It is `CLAUDE.md` § 6.5's *"pick a role, not a fill"* applied to
the palette rather than to the surface.

⚠️⚠️ **`UiTheme.Defense` (`0080e8`) IS UNTOUCHED AND IS STILL THE ONLY BLUE THAT MAY BE DRAWN.**
It means "the taya", it is a gameplay fact rather than a style, and it may never appear as menu
chrome. `ChatAndLobbyChromeTests` asserts that for the lobby nameplates.

---

## 5 · The journeys, walked out loud

⚠️⚠️ **§ 133.8: MORE THAN THREE PRESSES, OR ONE PRESS THAT HAS TO BE DISCOVERED RATHER THAN READ,
AND THE FLOW IS THE BUG.** He has asked for this three times and § 96 is the receipt.

| "I want to..." | The presses | Count | The door |
|---|---|---|---|
| **change my character** | fighter card, bottom left of the lobby, showing your current fighter's face and name → a face in the grid → BACK | **3** | A card with your own fighter on it. It is about your fighter, so it is self-evidently the way to change your fighter. |
| **change my loadout** | the build row under the fighter card → an alternate, on the slot you want | **2** | The board opens with all three slots and their alternates already visible, so there is no "open a slot" press. |
| **host a game with my rules** | the SETTINGS drawer chip → (change rows) → START MATCH | **2 + edits** | A chip that says what the rules currently are on its header, so it is worth opening. |
| **join my friend's code** | the JOIN chip → type the code → GO | **2** | A chip on the lobby's action row, beside the primary. |
| **find my profile** | the identity chip, top right, with my face on it | **1** | ⚠️⚠️ **§ 96'S FIX.** A face, top right, where every live game puts it. |
| **change my keybinds** | SETTINGS → the CONTROLS tab → the binding row → press the key | **3** | A tab, on a settings screen, labelled CONTROLS. |
| **sign out** | the identity chip → the ACCOUNT tab → SIGN OUT | **3** | Nothing about signing out is on the lobby, which is correct: it is not a thing anybody does often. |

⚠️ **AND THE ONE THING NO PROBE IN THIS REPOSITORY CAN ANSWER IS WHETHER HE FINDS IT.**
`UiClickProbe.EveryButtonIsReachable` has caught new chrome covering a screen three times and it
cannot tell anybody that a door nobody looks at is a door nobody finds. `Attention.md` § 5.1 is
the standing ask and this pass ships with it **queued rather than answered**.

---

## 6 · What this pass owes before any of it is called done

- **⚠️⚠️ Nothing on the control inventory disappeared.**
  `PaperPurityProbe.NothingOnTheInventoryDisappeared` captures every button, toggle, slider, field
  and dropdown on all five screens, in every tab and every drawer and every login state, and
  compares against a committed baseline taken BEFORE the rebuild. 🧑: *"it should have all the
  functions of old ui, make sure ntohing in old ui as functions get lost"*. § 133.5 asked for this
  list to be written before the screens are rebuilt, and a captured list cannot be wrong the way a
  typed one can.
- **⚠️⚠️ No leftover old UI.** `PaperPurityProbe.NoWoodenSurfaceSurvivesOnTheLobby`, extended, not
  replaced. § 119.6's header lists the four things a render cannot see: a surface inside a shut
  drawer, a surface under another surface, a `GodotButton` that only goes wood on hover, and a
  state the shot pass never opened.
- **⚠️ No label fakes its weight.** `PaperPurityProbe.NoLabelFakesItsWeight`.
- **Every screen rendered either side of the font change**, and `AspectRatioProbes` at all nine
  shapes. § 133.3 names this as the trap of the whole pass.
- **⚠️ At his window shape, not only at 16:9.** `Fullscreen` is **false** in his `settings.json`
  and all nine probe resolutions are taller than the window he actually plays in. § 6.2b.
- **⚠️ Three devices.** `CLAUDE.md` § 4a. Build through `MenuKit` or `ConvertedScreen` and the
  focus path and the thumb targets come for free; `InputSurfaceCheck` refuses the build either
  way.
- **A person looks at the picture.** § 6.2a: a green layout probe is not a good screen, and
  § 117.7 is seven faults that every probe in this repository was green through.
