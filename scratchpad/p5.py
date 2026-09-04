import io
p = "docs/TODO.md"
s = io.open(p, encoding="utf-8").read()
old = """⚠️ **THE PATTERN ACROSS ALL THREE IS THE SAME AND IT IS WORTH NAMING**: this pass moved
objects and drew new ones, and where it MOVED an object without redrawing it, he can see it
instantly. **Moving a control to a better place does not make it the right control for that
place.**"""
assert old in s
new = old + """

⚠️⚠️ **AND TWO MORE ON THE SAME CROP, WHICH TAKE THE COUNT TO FIVE.** Cropping the whole
bottom row of `Lobby-v91.png`:

| What he said | What it settles |
|---|---|
| **"only start match looks good here u js reused all buttons"** | ⚠️⚠️ **THE PAINTER CHANGED AND THE ROLES DID NOT.** `PaintBrand` now varies its stroke weight, gives every family its own four corner radii and its own edge, and all of that is TRUE of the chips as well as of the primary. What he is seeing is that **START MATCH is the only control on the row with a JOB that shows**: it is Chartreuse, 560 by 132, with a burst behind it, and MATCH SETTINGS, DANTE, Standard Build, JOIN and CHAT are five Honey Quartz rectangles of four similar sizes. **The variation is in the silhouette and the hierarchy is not**, so the eye reads five of one thing and one of another. `Front_End_Design.md` § 1's four atoms exist for exactly this (`Sheet`, `Tray`, `Chip`, `Row`) and this row uses two of them; § 6.5's *"a chamfer means pressable and a round means furniture"* is the shape difference that is still not being spent. |
| **"join a game and chat being there looks ugly af too"** | ⚠️⚠️ **THEY ARE IN THE SPINE'S SLOT AND THEY ARE THE WRONG TWO CONTROLS FOR IT.** § 1 puts secondary chips *"in a row to the LEFT of the primary"*, and that is where they are, so the placement is following the design. What the design did not ask is whether these two belong on the row at all. **CHAT is a drawer, not an action** (it opens a panel; § 1.2 says a thing that opens something is a `Row` with a chevron), and **JOIN is only meaningful when you are NOT already in a room**, which on this screen you always are. § 2.2b's mode table already drops JOIN in ranked for that reason. Candidates: chat becomes a drip-marked drawer on the room's own edge, and JOIN moves to the banner beside the room code, which is the one place on the screen already about codes. |

⚠️ **NEITHER OF THESE IS A PAINT PROBLEM AND BOTH WILL SURVIVE ANOTHER RECOLOUR**, which is
why they are written here rather than left as a note about button styling."""
s = s.replace(old, new)
io.open(p, "w", encoding="utf-8", newline="\n").write(s)
print("133.16 extended to five critiques")
