# What 🧑 reported on 2026-08-29, with his own screenshots

⚠️⚠️ **DELETE THIS WHOLE FOLDER WHEN THE BATCH IS FINISHED.** 🧑 asked for it to be committed only
so the next session, on a different machine, can see what he was pointing at. It is evidence for
one batch of work, not documentation. `docs/TODO.md` §§ 78 and 79 carry everything that must
survive; this folder must not.

⚠️ **THE IMAGES ARE HIS, NOT RENDERS.** They were pulled out of the session transcript, which is
where the chat stores what he pasted. Anything this session generated has been left out
deliberately: he asked for *"the shit i actually sent"*, and a probe render can always be made
again while a screenshot of a live build cannot.

---

## The index

Numbered in the order he sent them. The quote is his, verbatim.

| # | what it shows | what he said | entry | state |
|---|---|---|---|---|
| `01` | the lata card, its two lines colliding, the plate wider than the text | *"fix this hud, it should only extend when it has to, not all the tim"* | § 78.3 | ✅ |
| `02` | spectator on Eskinita, an orange **warning triangle over the taya**, mid screen | *"WHY IS THERE ! FOR SPECTATOR"* | § 79.4 | ✅ |
| `03` | spectator frame: flat, pale, **no ink outline anywhere** | *"is it js me or the shaders are very dif for spectator and actual"* | § 78.2 | ✅ |
| `04` | first-person frame of the same street: saturated, **heavy black ink** | *"spectatator might not be getting shaders"* | § 78.2 | ✅ |
| `05` | first person, the tsinelas floating clear of both hands | *"iits floating a bit and doesnt look the way a slipper would sit on a hand"* | § 78.4 | ✅ |
| `06` | Dante's first-person arms, green markings broken into **stray dark lines** | *"fix the markings and toon shader lines or wtv lines thes are for dante's fpp bcz it doesnt look like his character's real markings"* | § 78.10 | ✅ |
| `07` | first person, the shoe merged into the hand as one brown mass | *"dude this sucks"* | § 79.8 | ⚠️ **open** |
| `08` | Dante's left forearm cropped, the two **chevrons** circled | *"this specifically bcz it doesnt matcht eh arm of the model"* | § 78.11 | ✅ |
| `09` | the same arm after the first fix, the stripes nearly straight | *"still not how his arms look in model its too straight"* | § 78.11 | ✅ |
| `10` | **character select**, Cheska: pale, low contrast, washed out | *"fix shader on chara select too look at pic 1 vs pic 2, it should look more like pic 2"* | § 79.1 | ✅ |
| `11` | **the lobby cast**, same characters: rich colour, strong outlines. **This is the target.** | (the "pic 2" above) | § 79.1 | ✅ |
| `12` | Ilalim ng Tulay lobby: hazy, pale, low contrast next to Eskinita | *"ilalim ng tulay as well should look more like the other map's shaders"* | § 79.2 | ✅ |
| `13` | character select, Dante: the **ultimate's plate drawn past the bottom** of the wood panel | *"fix hud here it overflows"* | § 79.6 | ✅ |
| `14` | character select, IKE: renders **mid grey** where the material is near black | *"this is what im saying wtf is this its so light"* | § 79.1 / § 78.8 | ✅ |
| `15` | the player name card with `EDIT` in amber at the end of the field | *"remove edit here bcz it lowk does nothing"*, *"tap already works"* | § 79.6 | ✅ |
| `16` | the lobby, `WAITING FOR 4 PLAYERS` drawn out of both ends of its plate | *"fix this overflow"* | § 79.6 | ✅ |
| `17` | a corned beef lata, close up | *"this is supposed to be like rotating and shit, both slippers and tsinelas and hero"*, *"they stop rotating if we move"*, *"and go back to rotating in character select after"* | § 79.5 | ✅ |
| `18` | lobby chat open: the log lists four lines, the **bottom strip shows none** | *"lobby chat ui overflow, also wtf happens if thers more than 100 messages, can i scroll thru it?"*, *"u dont see most recent chats in say something"* | § 79.3 | ⚠️ **open** |
| `19` | the practice lobby's start button, `START MATCH` small in a large empty plate | *"start match text too small in practice"* | § 79.6 | ✅ |
| `20` | first person holding a tsinelas that is **flat maroon/brown**, nothing like the picker | *"ingame shader messes up the color of slippers"*, *"doesnt look anything like the frigging character select anymore"* | § 79.7 | ✅ |
| `21` | the same in close up, holding **IKE** | *"also this is ike, do u see hwat i mean now, the shaders fuck up the color of slipper"* | § 79.7 | ✅ |
| `22` | the player name card again | *"like shit like this wont make sense for next chat dude if u dont attach the image"* | § 79.6 | ✅ |

---

## Two things the next session should read before touching any of this

⚠️⚠️ **READ THE CAST SHEET FOR A CHARACTER, NEVER THE MODEL SHEET.** `ModelSheet.Run` renders the
full sheet with **no palette** and says so in its own index (`[no palette, stock atlas colours]`),
so Dante appears there in the source asset's blue and orange and looks like a different character
entirely. That render was shown to 🧑 as a reference and the reply was *"thats not our dante"*,
*"wtf"*, which was correct. `ModelSheet.RunCast` applies `RosterEntryAsset.Palette` and is the
only one of the two that shows what the game draws. **The palettes are not corrupt.**

⚠️⚠️ **THE SCOPE OF A FIX IS THE REPORTED THING.** Image `08` asked for Dante's green markings to
change. The first attempt also deleted his leather sleeve, harness strap and gold cuff, on the
reasoning that the model shows bare skin there, and was rejected outright: *"infact old one was
better"*, *"all i needed u to change in old one was the green markings"*. Everything else nearby
that also looks arguable is not part of the ask.
