# Paete, the ninth hero: brief, 2026-09-25

Owner, 2026-09-25, in order:

- The task: a new Hero Strike hero, PLANT-based, on the signature plus role ability system.
- Three references: a painted tree colossus holding a dragonfly on one finger, a tree-golem
  hero skin with antler branches, and a voxel concept sheet labelled GUARDY (front,
  three-quarter, side, back). Kept here: `owner-concept-guardy-sheet.webp`. The other two are
  third-party game art and stay in chat only.
- On GUARDY: *"only thing i dont like abt this reference is facial expression and eyes"*, *"i
  want it to have engraved sunked green eyes and a nonchalant calm expresison like thhe other
  references"*.
- A second concept, shared in chat only: *"here's a really cool concept i want u to try to
  copy"*. A blocky treant in four views with three face states (NEUTRAL, FOCUSED, ANGRY), and an
  ARM EXTENSION panel: *"arms can extend into branches / vines"*.
- A face close-up, shared in chat only: a head built of carved wooden planks, a heavy brow plank,
  green glowing slit eyes set deep under it, leaves and moss on the crown, branch antlers.
- *"thoroughly plan how to do it first and research"*.
- *"pls draw his 3d model or do it one by one dont try to mass generate it bcz it will loo like
  shit"*.
- Answers: from **Mount Makiling, Laguna**; named **Paete**; *"its fine dude u go fgigure out hhis
  color"*; the vine arm is **the signature**.

Method: [docs/CHARACTER_MODEL_METHOD.md](../../../docs/CHARACTER_MODEL_METHOD.md). Kit and
animation research: [docs/reports/paete-kit-2026-09-25/](../../../docs/reports/paete-kit-2026-09-25/).

## 1. The concepts, read

**Second concept (the one to copy).** A short, broad treant, all blocks. Head: a tall block of
bark planks with two branch antlers rising from the crown, each forking twice, a few leaves.
Eyes: two deep sockets under a brow ledge, each holding a bright yellow-green light, no pupil.
Body: a barrel trunk of stacked bark planks with moss in the seams; square shoulders; long
heavy arms whose forearms are bundled planks ending in three blunt twig fingers; short thick
root legs with splayed root toes. Colour: warm mid-brown bark, darker brown seams, moss green
at the joints, the eye light the only bright thing. The arm-extension panel: the forearm unravels
into a long bundle of branches and vines that reaches across the frame, leaves along it.

**Face states.** NEUTRAL: level eyes, calm. FOCUSED: the eye light narrows to a slit, the brow
plank lowers. ANGRY: the eyes tilt in at the inner corners, brighter.

**GUARDY sheet (kept for its body, not its face).** Moss and vines wound round the limbs, small
white five-petal flowers at the shoulders and arms, a woven green tapis panel with a cream
diamond hanging from a rope belt, root feet. Its ":3" face is the part the owner rejected.

**Face close-up.** The head is built from separate planks, each with carved grain, stacked like
a totem: brow plank, a vertical nose plank, cheek planks. The eyes are narrow slits sunk under
the brow, the light spilling onto the plank below. Nonchalant: no mouth line at all, or one
short horizontal cut.

## 2. Research

**Mount Makiling, Laguna.** An inactive stratovolcano, 1,090 m, rising from Laguna de Bay
between Los Baños, Calamba and Bay. The country's first national park (1933), now the Makiling
Forest Reserve run by the University of the Philippines Los Baños for forestry teaching, an
ASEAN Heritage Park since 2013. Hot springs and a mud spring on its flanks. The name is read as
*makiling*, "leaning, uneven", for the mountain's tilted profile, or from *kawayang kiling*, a
bamboo that grows thick there.

**Mariang Makiling.** The best known diwata in Philippine folklore: the guardian of the
mountain and its bounty, associated with the white mist that hangs on it, living in a humble hut
that can only be found if she allows it. Two stories matter here. **Ginger into gold**: she gives a
poor villager ginger, which is gold when he gets home. **The fruit rule**: you may pick and eat
any fruit in her forest, but never carry it home; whoever does gets lost among thorns and stings
until he throws the fruit away and turns his clothes inside out.

**Paete, Laguna** (his name). On the east shore of the same lake, "the Carving Capital of the
Philippines". The name comes from *paet*, chisel. Its carvers work wood and make *taka*, papier
maché laid over a carved wooden mould, and the town is said to be where the modern yo-yo was
first made.

**Narra** (*Pterocarpus indicus*): the national tree since 1934, the carvers' prized hardwood, a
tall forest tree with yellow flowers and flat, disc-shaped winged seed pods.

Sources: Wikipedia "Mount Makiling", "Maria Makiling", "Paete", "Pterocarpus indicus" (read
2026-09-25).

## 3. Who he is (goes to docs/CHARACTER_ORIGINS.md and LORE.md with the model)

Origin: **Mount Makiling, Laguna**. Home court: **Paanan**, a fictional flat clearing where the
forest reserve meets the town above Los Baños, where students play after class.

Character-select line: **Never hurries. Always arrives.**

Long introduction:

Paete grew up as part of the forest on Mount Makiling, older than the trail markers and younger
than the narra around him. Hikers there learn one rule: eat what the forest gives you, never carry
it home. Paete learned the same rule from the other side. One wet season a woodcarver from Paete,
across the lake, lost the trail in the mist, and Paete walked him down to the road. The carver had
nothing to give but his chisel, so he cut the tree a face: two deep eyes and a mouth that never
quite decided to smile. Paete has worn the town's name ever since. At the forest's edge he found
students playing tumbang preso after class, and stood so still watching that they used him as
the lata twice before he moved. Now he plays, unhurried and a little amused by everyone else's
panic. He lets you take a run at anything. He just does not let you leave with it.

His calm is the flaw inside the strength: so sure of his reach that he lets a moment pass.
Amihan cannot stand him (she hates a stalled game and he is one). Dante, the other mountain in the
cast, is the only one who waits as long as he does. Zack still thinks he is a prop.

**Creative boundaries.** Mariang Makiling stays a legend he grew up among: never shown, never his
mother, never a power source. He is not a diwata, an anito or a god, the line the whole cast holds
("the element is a talent, not a deity"). The fruit rule is a folk superstition used as his
character theme, not a ritual. The chisel-cut face is a carver's gift, not a santo; Paete's Holy
Week carving tradition is not referenced anywhere.

## 4. The model plan

Base: his own builder, `tools/build_paete_voxel.py`, a copy of `tools/build_amihan_voxel.py`
(itself a copy of `build_person_voxel.py`), so it keeps the base rig, the seven bone names, the
family proportions, the height check, the chamfered boxes, the tapers and tilts, the projected
decals and the per-mesh slide solve. Nothing else's builder is touched.

**Owner's rule for this model: one part at a time, by hand.** No loop that stamps bark planks,
leaves, moss or roots. Every plank, leaf, moss patch, root toe and carved mark is its own table
row with its own numbers, and no two are the same size. Built in this order, each step rendered
beside the cast before the next:

1. **Blockout**: trunk, head block, arms, legs as plain bark boxes at cast height. Silhouette
   check at lineup distance.
2. **The head, plank by plank** (the face close-up): brow plank, nose plank, two cheek planks,
   crown planks, the two sunken eye sockets. Replaces the donated human skull, see section 5.
3. **Antlers**: two branches, each forked by hand, asymmetric, blocky. They count against the
   cast height, so the body is built to leave them room.
4. **Torso planks**: the barrel trunk as separate overlapping planks, each tilted and sized on
   its own; dark seams between them.
5. **Arms**: upper arm, the plank-bundle forearm, three blunt twig fingers per hand as ONE
   block with grooves (the cast's simple no-thumb hands). The signature needs the forearm to read
   as a bundle that can unravel.
6. **Legs and roots**: stump legs, three to four root toes per foot, each placed separately.
7. **Moss and leaves**: moss patches in the seams (shoulders, crown, knees), a handful of
   leaves, each placed and tilted by hand.
8. **GUARDY's extras, as switches for comparison**: vine wraps, the small white flowers, the
   woven tapis. Rendered with and without beside the cast; the owner picks.
9. **Carved marks**: grain lines and one carved relief per zone (brow, chest, shoulder), planned
   as routes before any coordinates (method section 4), drawn as decals, darker than the bark.

## 5. Departures from the method, and why

- **The face is not ink-only, by the owner's instruction.** Method section 2 says eyes and mouth
  are solid ink. The owner asked for *"engraved sunked green eyes"*. So: the socket is geometry
  (the brow and cheek planks stand proud and the eye sits back in a recess), and the eye is a
  flat slit of eye-light green (an emissive slot if the toon shader has one, otherwise the
  brightest value in the palette). Still no pupil, no glint, no teeth. The mouth, if any, is one
  short carved line in ink. The attitude comes from the brow plank's angle, which is the method's
  "the eye's top edge does a brow's job".
- **No donated human skull.** The native skull is a person's; his head is a carved block of
  planks. The eye placement and head size still follow the family proportions so he reads as the
  same cast at lineup distance.
- **Three eye states** (NEUTRAL, FOCUSED, ANGRY), which no hero has yet: three versions of the eye
  slit baked as separate named sub-meshes, only one visible, switched by `CharacterAnimator`
  (FOCUSED while aiming the signature, ANGRY for the ultimate). Neutral is the default in every
  still: portrait, avatar, select.

## 6. Colour

- **Model**: warm bark brown as the body, dark seam brown, one quiet colour of his own: **moss**,
  a dark desaturated green in the seams and on the crown, never a cloth piece. The eye light is a
  yellow-green; it is small and the only bright value on him. No metal at all (the first hero
  without one).
- **UI accent**: every hue is taken (30 degrees between heroes, 25 clear of the two role hues).
  The owner left it to me. Decision: amend the law so two hero accents must be **30 degrees apart
  in hue OR at least 15 apart in OKLab distance** (x100), keeping the 25-degree clearance from the
  role colours as it is. Every existing pair still passes by hue. Paete takes **`4f6b1f`, a dark
  moss (hue 82)**, 16.9 from Dante's jade, its nearest, and 28 or more from both role colours. For
  scale, the closest legal pair already in the game (Zack and Amihan) is 12.6 apart. Red above
  blue, so it is legal in a menu (CLAUDE.md 6.4). It is checked on the deck tile, nameplate and
  select card before it is final; the world-side bright value is chosen the same way.

## 7. Stats

Hero rows are distinct (`BalanceTests.AllPersonRows_AreDistinct`). A tree is grit: proposed
bilis 3, lakas 3, tatag 5, a row no hero or street character has. A prototype number, not a
balance verdict.

## 8. The model, v1 to v10 (2026-09-25)

Builder: `tools/build_paete_voxel.py`. Review: `PaeteNativeModelReview.Run -out Logs/paete-vNN`
(lineup beside the eight heroes, turnaround, head study). Evidence in `evidence/`.

- **v1 to v4, rejected.** Built on the cast's chibi proportions (head 53 %) with bark planks, then
  vertical ridges and a leaf canopy. Owner: *"what the fuck is this it DOES NOTTT LOOK LIEK
  REFERECNES AT ALL"*, *"its body is js brown with greenshit coming out (leaves and mosss and
  engravings) and it doesnt have hair"*, *"i want u to really analyze all details in references"*.
  The canopy read as hair; the proportions were the cast's, not the concept's.
- **The idea board** (owner, same day: *"ideaboard of chat gpt pls use it well"*), read part by
  part: a tall plank mask split down the centre with a carved spiral forehead and angled glowing
  slits under a heavy brow; square-section horns with right-angle elbows; a V chest round a spiral
  boss; layered slanted shoulder planks; long plank-bundle arms ending in claws that hang to the
  knee; plank legs on root feet; thick vines round the torso, a forearm and a leg; moss in the
  crevices; big leaves sprouting from the seams; five browns and four greens.
- **v5 to v10.** His own skeleton (hips 0.260, shoulders 0.470, neck 0.495, crown 0.668, horn
  tips 0.79, inside the cast's height range); every plank, horn, root, vine, moss tuft, leaf and
  engraving typed as its own row. v6 raised the moss off the bark (flat patches read as
  stickers), v8 thickened the horns and turned the leaves to face out, v9 gave each eye a bright
  core in a green rim, v10 took the long nose and the dark jaw wedge out and calmed the brow to
  4 degrees (the owner asked for a nonchalant face).
- **v11 to v14.** Layered shoulder planks with hanging moss strands, two-segment curling claws,
  a back branch; the owner circled two thin side planks hanging beside the waist (*"can u
  remove those"*) and v12 took them out; leaves gathered into clusters (singles read as
  confetti), moss as seam strips, shins flaring into thicker roots, bark plates over the thighs,
  and the back given shoulder blades, a lower plate, moss seams and grain.
- **Open:** the three face states (NEUTRAL, FOCUSED, ANGRY), the owner's read of v10, the
  palette check on the deck tile.
