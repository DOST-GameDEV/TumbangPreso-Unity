# Asset sourcing: ability VFX, SFX and later map art

Checked 2026-09-03 for Unity 6000.5.8f1 and URP 17.5.0.

This is the licensed source list for the next art pass. **The existing Kenney characters are good
and are not part of this replacement work.** Hero Strike ability VFX and the synthesised SFX are
the priority. Buildings, environment props, ambience and UI sources are kept at the end so they
can be used later without repeating the hunt.

## 1. Rules that decide whether an asset ships

1. Every listed asset costs zero. Paid, subscription, trial, non-commercial, GPL and CC BY-SA
   assets are excluded.
2. The game owns the final look. A downloaded prefab is an ingredient, not an art direction.
3. Keep `Toon.shader`, `ToonTransparent.shader` and `WorldOutline.shader`. Imported PBR, Shader
   Graph, VFX Graph and store shaders do not ship unless separately reviewed and rebuilt for this
   project.
4. Use flat colour, two or three stepped values and one dark ink edge. No photographic smoke,
   glossy ice, realistic fire simulation, screen distortion or bloom-dependent effects.
5. A skill remains inside its existing 1.8 to 2.5 m radius. An ultimate may be larger once. No
   downloaded demo prefab gets to change gameplay geometry.
6. A mid-fight frame must still show the lata, chalk and all players. The existing 12 percent
   white-frame gate still applies.
7. Repack chosen sprites into one or two atlases and use shared materials. This is how six source
   pages become one game's visual language instead of six unrelated packs.
8. CC0 and CC BY source may live in this public repository with the proper licence. Unity Asset
   Store and Sonniss source libraries may ship inside a compiled game but their raw files must not
   be committed publicly.

## 2. The five ability VFX downloads to start with

| Priority | Asset | What it contributes | Format and fit | Licence |
|---:|---|---|---|---|
| 1 | [PVFX Foundry](https://nerijs.itch.io/pvfx-foundry) | Earth Rupture, Frost Nova, Warm Explosion, Ember Jet, Electric Impact, Void Implosion, Spectral Bloom, Rift Portal, Smoke Puff, Landing Dust and other compact combat effects | Transparent 96x96 flipbooks at 20 FPS, packed and grid editions, engine-neutral, no compute | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/), commercial use and redistribution allowed, no credit required |
| 2 | [Kenney Particle Pack](https://kenney.nl/assets/particle-pack) | Sparks, smoke, magic, electricity, impact accents, rings and generic particle shapes | 80 separate 512x512 sprites plus sheets and vector source. Use the raw art with the project's URP materials | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/), commercial use and redistribution allowed, no credit required |
| 3 | [Animated Particle Effects #2](https://opengameart.org/content/animated-particle-effects-2) | Fire, flame and teleport flipbooks for Sean, Nemu and Phaister | 1024x1024 or 512x512 sheets with 128x128 cells. Extract only the needed sheets | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/), commercial use and redistribution allowed, no credit required |
| 4 | [Lightning texture by hdst](https://opengameart.org/content/lightnings) | Zack's branching bolt, sprint ribbon, circuit arcs and vertical strike | One transparent 512x512 texture. Tint from its source colour to amber gold | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/), commercial use and redistribution allowed, no credit required |
| 5 | [Four Summoning Circles](https://opengameart.org/content/4-summoning-circles) | Phaister's Hex and Grand Coven glyph language | Four editable SVG and AI circles. Export only the selected simplified circles to 512 PNG | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/), commercial use and redistribution allowed, no credit required |

Useful supplements:

- [Magic Summoning Circle](https://opengameart.org/content/magic-summoning-circle), CC0,
  transparent 768x512 PNG for Grand Coven.
- [Magic and Smoke Effect](https://opengameart.org/content/magic-and-smoke-effect), CC0, smoke
  and magic strips for Phantom Veil and Shadow Blink.
- [Teleporter Effect](https://opengameart.org/content/teleporter-effect), CC0. Use the no-rings
  variation for Nemu and Phaister so it cannot be confused with Hex.
- [Seamless Magic/Forcefield Effect](https://opengameart.org/content/seamless-looping-magicforcefield-effect),
  CC0, 33 greyscale 512x512 frames for Magnet and shield surfaces.
- [Arcane Magic Effect](https://opengameart.org/content/arcane-magic-effect), CC0, compact dark
  magic projectile and wisp shapes.
- [Low Poly Rocks](https://opengameart.org/content/low-poly-rocks), CC0. Each source mesh is about
  150 polygons. Extract only the OBJ meshes and discard the supplied 4K normal maps.
- [Y2K Ice Texture](https://opengameart.org/content/y2k-ice-texture), CC0, 1024 PNG. Use only as a
  heavily graded surface detail mask, never as a realistic material.

## 3. The eighteen Hero Strike abilities

The current measured geometry remains authoritative. The sourced art replaces primitive-looking
surfaces, particles and transients without changing range, collision, authority or balance.

| Hero and ability | Use from the source list | Shipping composition |
|---|---|---|
| **Dante: Seismic Stomp** | PVFX **Earth Rupture**, Kenney dust and rock specks | One rupture flipbook at Dante's feet, one low outward dust ring and a few opaque chips. Keep the 2.2 m gameplay radius and never scale the flipbook into another floor decal. |
| **Dante: Demonic Carapace** | CC0 low-poly rock meshes, Kenney dust and ember particles | Five to seven small flat-shaded plates around the torso and arms. Keep the face and body silhouette readable. No glowing sphere. |
| **Dante: Titan Fissure** | PVFX **Earth Rupture**, CC0 rocks, Kenney dust streaks | One rupture at the impact centre, then two or three staggered rising plates along the cast direction. Dust joins the pieces. Do not paint the floor a second time. |
| **Cheska: Permafrost Sheet** | PVFX **Frost Nova**, graded CC0 ice texture, Kenney sharp particles | Frost Nova is the formation transient. The existing raised sheet receives a dark cracked rim and restrained ice detail. Keep the footprint at its current 2.3 m scale. |
| **Cheska: Ice Barricade** | PVFX **Frost Nova**, Kenney shard sprites | Keep the current collision. Replace cubes with five to seven tapered in-house shard meshes. The free source art solves formation and splinters, not the wall mesh itself. |
| **Cheska: Glacial Nova** | PVFX **Frost Nova**, Kenney radial shards | One centre burst, a brief outward shard ring and short frost wisps on frozen players. No second white disc below it. |
| **Sean: Flame Rush** | PVFX **Ember Jet**, Animated Particle Effects fire sheets | Ember Jet at the leading edge and short flame tongues along the swept path. Orient them with movement so they read as a streak, not circular puddles. |
| **Sean: Ignition Cannon** | PVFX **Magical Projectile** tinted orange, small fire impact sheet | A compact projectile head, a short ember tail and one small impact burst. Remove the current glowing point-light ball. |
| **Sean: Supernova** | PVFX **Warm Explosion** and **Solar Shrapnel**, Kenney sparks | One main burst with shrapnel and sparse embers. Keep the existing crater geometry and remove competing white flash layers. |
| **Zack: Bolt Sprint** | hdst lightning texture, PVFX **Electric Impact** | Short world-space bolt ribbons behind Zack, plus one small start and end impact. Tint to amber gold. Randomise UV or rotation so the trail does not tile visibly. |
| **Zack: Magnet** | Greyscale forcefield loop, Kenney sparks | A narrow band around the held or affected target with a few inward-moving sparks. Keep it close to the target and off the floor. |
| **Zack: Thunderstrike** | hdst lightning, PVFX **Electric Impact** | One vertical or angled bolt, one ground impact and one brief low shock ring. No full-screen white flash and no additive floor disc. |
| **Nemu: Phantom Veil** | Magic and Smoke strip, PVFX **Spectral Bloom** | Dark smoke hugs the body, with one small bloom at activation and deactivation. Fade with alpha or dither rather than a large transparent sphere. |
| **Nemu: Astral Hijack** | Teleporter no-rings frames, PVFX **Rift Portal** or **Spectral Bloom** | A thin trail between endpoints and a small reverse-played wisp at transfer. Keep ring imagery out so it does not borrow Phaister's language. |
| **Nemu: Devouring Seance** | PVFX **Void Implosion**, Arcane Magic wisps | Place the implosion inside the existing dished funnel, then pull a few wisps inward above it. Keep the centre dark and vertically profiled. |
| **Phaister: Hex** | One of the Four Summoning Circles SVGs | Simplify to one written 512 decal, recolour to warm ink, violet and amber, and leave the middle clear. Static written geometry separates Hex from Blink. |
| **Phaister: Shadow Blink** | Teleporter no-rings frames, Magic and Smoke strip | A torn vertical exit wisp and a tighter reverse-played arrival puff. No floor circle. |
| **Phaister: Grand Coven** | Magic Summoning Circle, Four Summoning Circles, PVFX **Spectral Bloom** | One sparse ground circle and a separate broken overhead corona with an empty middle. Bloom punctuates the cast once. |

### 3.1 Ability VFX gaps

- Ice Barricade still needs a small original shard-wall mesh. No free wall matched the style,
  licence, scale and mobile budget.
- Demonic Carapace needs hand placement of the sourced rocks around Dante. A generic shield prefab
  cannot know which parts of the character must stay readable.
- Astral Hijack's trail and Devouring Seance's funnel choreography remain code-driven because
  their endpoints and pull direction are gameplay facts.
- These are assembly tasks around sourced art. They are not reasons to keep the current primitive
  presentation.

## 4. Optional free Unity VFX packages

These are useful for reference or local integration, but their raw files cannot be pushed to the
public repository.

| Asset | Coverage | Compatibility | Licence and source handling |
|---|---|---|---|
| [Free Quick Effects Vol. 1](https://assetstore.unity.com/packages/vfx/particles/free-quick-effects-vol-1-304424) | 30 effects including electricity, explosions, fire, two flamethrowers, implosion, two lightning effects, portals, projectiles, shield, shockwave, smoke and sparks | Publisher supplies a URP package authored for Unity 2022.3.43. Test in a disposable Unity 6000.5 project before copying selected textures/modules | Free under the [Standard Unity Asset Store EULA](https://unity.com/legal/as-terms). Commercial compiled games allowed. No raw public redistribution |
| [Free Game VFX: Magic Circle URP](https://assetstore.unity.com/packages/vfx/particles/free-game-vfx-magic-circle-urp-344984) | Hex and Grand Coven reference/prefab | Store lists URP compatibility for Unity 2022.3.45. Package is 992.4 KB | Free under the Standard Unity Asset Store EULA. No raw public redistribution |
| [Free Stylized Smoke Effects Pack](https://assetstore.unity.com/packages/vfx/particles/fire-explosions/free-stylized-smoke-effects-pack-226406) | Shadow Blink, sprint dust, knockdown, landing and can impact puffs | Store lists URP compatibility. Package is 120.5 KB and authored for Unity 2020.3.36 | Free under the Standard Unity Asset Store EULA. No raw public redistribution |

Reject [3D Games Effects Pack Free](https://assetstore.unity.com/packages/vfx/particles/3d-games-effects-pack-free-42285)
for this project despite its zero price. It is 642 MB, and its broad sprite library is a worse
mobile/source-control trade than the smaller CC0 set above.

## 5. Ability and gameplay SFX sources

Every current effect is a generated placeholder. Replace it with short, dry source recordings and
designed layers. Positional cues should end as mono 48 kHz WAV files. The game applies its own
spatialisation and headroom, so do not bake in long reverb.

### 5.1 Pack sources

| Source | Best use | Licence |
|---|---|---|
| [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds) | 130 impact and foley files for body hits, bumps, guard blocks, lata impacts, can knockdown, slipper bounce/land and quake transients | CC0, public-repo safe, no credit |
| [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) | 100 files for click, hover, back, error, countdown, score and ability-ready feedback | CC0, public-repo safe, no credit |
| [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio) | 50 footsteps, weapons and foley sounds for movement, pickups, grabs and physical accents | CC0, public-repo safe, no credit |
| [Kenney Digital Audio](https://kenney.nl/assets/digital-audio) | 60 electronic layers for Zack, Nemu and Phaister. Use as supporting transients, not the whole cue | CC0, public-repo safe, no credit |
| [Kenney Music Jingles](https://kenney.nl/assets/music-jingles) | Round win, loss, end, match win, score and boot-sting candidates | CC0, public-repo safe, no credit |
| [Sonniss GameAudioGDC 2026](https://gdc.sonniss.com/) and [archive](https://sonniss.com/gameaudiogdc/) | Professional source recordings for fire, ice, rock, thunder, debris, explosions, whooshes, ghost/void design, crowds and ambience | Free worldwide royalty-free commercial licence, modification allowed, no credit. Compiled use allowed. Raw library redistribution forbidden. Keep outside public Git |

### 5.2 Verified individual CC0 recordings

| Sound | Direct link | Use in this game | Source format |
|---|---|---|---|
| Fire whoosh by hnhnh | [Freesound 244926](https://freesound.org/people/hnhnh/sounds/244926/) | `sfx_fire_whoosh`, Sean cast layers | 48 kHz, 16-bit, stereo WAV, 3.857 s |
| Ice cracking by frigeriose | [Freesound 822369](https://freesound.org/people/frigeriose/sounds/822369/) | `sfx_ice_form`, `sfx_ice_shatter`, barricade raise | 48 kHz, 24-bit, stereo WAV, 33.710 s source take |
| Short ice crack by getwecked | [Freesound 764657](https://freesound.org/people/getwecked/sounds/764657/) | Short `sfx_ice_shatter` transient | 48 kHz, 24-bit, stereo WAV, 1.5 s |
| Freeze effect by antonsoederberg | [Freesound 685253](https://freesound.org/people/antonsoederberg/sounds/685253/) | `sfx_ice_freeze`, Glacial Nova cast layer | 48 kHz, 16-bit, stereo WAV, 3.651 s |
| Thunder Impact by pluralz | [Freesound 475818](https://freesound.org/people/pluralz/sounds/475818/) | `sfx_thunder_impact`, Thunderstrike body | 44.1 kHz, 16-bit, stereo WAV, trim from 29.714 s source |
| Raw close thunder by TSP-Talk | [Freesound 844420](https://freesound.org/people/TSP-Talk/sounds/844420/) | Thunderstrike low end and tail, shortened heavily | 48 kHz, 32-bit float, stereo WAV, 40.575 s source |
| Earthquake objects by RutgerMuller | [Freesound 51123](https://freesound.org/people/RutgerMuller/sounds/51123/) | `sfx_quake_slam`, fissure debris and carapace movement | 44.1 kHz, 24-bit, mono WAV, 9.5 s |
| Raw wood/metal earthquake by tompallant | [Freesound 163494](https://freesound.org/s/163494/) | Rock and street-debris layers for Dante | 48 kHz, 24-bit, mono WAV, 68.350 s source take |
| Electric crackle by ironcross32 | [Freesound 582631](https://freesound.org/s/582631/) | Zack sprint, magnet and strike accents | 44.1 kHz, 24-bit WAV, 1.262 s |
| Dark Magic Loop by qubodup | [Freesound 442825](https://freesound.org/people/qubodup/sounds/442825/) | Nemu void bed, Phaister coven bed, trimmed and made mono where positional | 44.1 kHz, 16-bit, stereo WAV, seamless 1.329 s |
| Dark spell by LilMati | [Freesound 683628](https://freesound.org/people/DneproMan/sounds/683628/) | Nemu/Phaister cast transient source | 44.1 kHz, 16-bit, stereo WAV, trim from 10 s source |
| Magic Spell 02 by LilMati | [Freesound 455205](https://freesound.org/people/LilMati/sounds/455205/) | `sfx_hex_cast`, `sfx_hex_afflict`, blink arrival | 44.1 kHz, 16-bit, mono WAV, 1.347 s |
| Tin Can by jberkuta14 | [Freesound 134903](https://freesound.org/people/jberkuta14/sounds/134903/) | Bright layer for `lata_impact`, `lata_knockdown`, `can_knockdown` | 48 kHz stereo MP3, render selected transient to mono WAV |
| Can rolling by 21100375 | [Freesound 593129](https://freesound.org/people/21100375/sounds/593129/) | Lata settle/reset tail | 48 kHz, 24-bit, mono WAV, 8.202 s |
| Basketball on concrete by Connorisfine | [Freesound 708564](https://freesound.org/people/Connorisfine/sounds/708564/) | Distant Bayan Plaza ambience detail | 48 kHz, 24-bit, stereo WAV, 13.245 s |
| Medium crowd cheer by BeeProductive | [Freesound 430046](https://freesound.org/people/BeeProductive/sounds/430046/) | Tournament crowd bed and win accents | 48 kHz, 24-bit, stereo WAV, 15.953 s |

All individual recordings in this table are [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
Commercial use, modification and redistribution are allowed without attribution.

✅ **ALL SIXTEEN WERE DOWNLOADED ON 2026-09-04** into `scratchpad/asset-src/freesound/`, which is
gitignored, and each was verified against the format and duration stated in its row above to three
decimal places. **Thirteen of the sixteen are wired**; the three that are not are the tin can, the
basketball and the crowd cheer, each with its reason in `tools/build_ability_audio.py`'s `KEPT`.
The tin can is the one worth knowing about: this table names it for `lata_impact` and
`lata_knockdown`, and **§ 5.4 records those exact cues being rejected by ear**. A source table
written before a listening test does not overrule the listening test.

### 5.3 Cue-family assignment

- **Dante:** earthquake/wood/metal recordings for the body, Kenney impacts for the transient, and
  short gravel/debris layers from Sonniss. Carapace movement should be quieter than the stomp.
- **Cheska:** the short ice crack for hits, the long frozen-food take for formation variation, and
  the freeze effect for the magical high layer. Do not reuse one full cue for all three powers.
- **Sean:** fire whoosh as the recorded body, then separate short whoosh/impact layers from
  Sonniss or Kenney for rush, cannon and supernova.
- **Zack:** electric crackle for movement and charge, Thunder Impact for the hit, and only a short
  slice of raw thunder for the ultimate tail.
- **Nemu:** Dark Magic Loop as a quiet bed, with reversed whooshes and short dark-spell transients.
  Hijack needs a directional travel layer that is absent from Veil.
- **Phaister:** Magic Spell 02 for the written cast, a short dry teleport arrival and a separate
  low coven toll. Do not give all three abilities the same witch sound.
- **Hero voices:** record the six grunts and ultimate callouts in Tagalog in-house. Do not replace
  them with generic English voice packs.

### 5.4 Existing cues that the source pass must preserve

The 2026-09-03 replacement pass changed these three cues and 🧑 rejected the new versions after
hearing them in the game. Their pre-pass WAV files are restored and
`tools/build_ability_audio.py` deliberately excludes them:

- `lata_impact`, the can hit.
- `lata_knockdown`, which is also the file reached by the `can_knockdown` alias.
- `ui_hover`, the button hover.

Do not bulk-regenerate those files from the Kenney packs. A source label that says tin or hover is
not better evidence than the sound that was preferred in the game.

### 5.5 Rollback ledger for the remaining sourced cues

🧑 may reject more of the sourced replacements after hearing them in play. Treat the remaining
twenty-four as provisional, not approved as a group. The replacement commit is `ee8bced`; the
exact pre-pass WAV for any cue is the same path at `ee8bced^` (`c5b6ff9`). Restore only the named
cue, then remove its row from `tools/build_ability_audio.py.REPLACEMENTS` and record it in `KEPT`
in the same commit. Otherwise the next generator run will silently put the rejected sound back.

⚠️⚠️ **AND THE 2026-09-04 ELEMENTAL PASS ADDED 42 MORE PROVISIONAL CUES, WHICH IS MOST OF THE
ABILITY LAYER.** All 18 `sfx_cast_*`, all 12 `sfx_var_*`, and `sfx_fire_whoosh`, `sfx_ice_form`,
`sfx_ice_freeze`, `sfx_ice_shatter`, `sfx_barricade_raise`, `sfx_thunder_impact`,
`sfx_lightning_strike`, `sfx_hex_cast`, `sfx_hex_afflict`, `sfx_blink_arrive`, `sfx_quake_slam`
and `lata_seal`. **None of them has been heard in play**, so `CLAUDE.md` § 6's rule applies to
every one: provisional until 🧑 hears them. The pre-pass file for any of them is the same path at
the commit before the elemental pass.

⚠️⚠️ **THE GENERATOR IS IDEMPOTENT NOW AND IT WAS NOT BEFORE, WHICH IS ITS OWN SMALL LEDGER.**
`peak_of` read the peak of the file it was about to OVERWRITE and multiplied the row's `gain`
onto it, so every re-run multiplied the gain again: `tag` at gain 0.9 goes 0.850, 0.765, 0.688,
0.620, and the sixth run is half the level `AudioCues.TrimDb`'s clipping measurement was taken
at. Nothing warned and every row still printed a plausible number. `tools/assets/cue_reference_peaks.json`
holds the final target per cue, written once, and a second run is now a byte-for-byte no-op.

The provisional files are:

- Body/contact: `bump`, `tag`, `downed`, `guard_block`, `sfx_hitmarker`.
- Movement/tsinelas: `land`, `slipper_land`, `slipper_bounce`, `grab`, `throw_whoosh`, `dash`.
- Ability/feedback: `sfx_quake_slam`, `sfx_ice_shatter`, `sfx_stun_break`, `sfx_super_ready`,
  `stamina_empty`.
- UI/match: `ui_click`, `ui_back`, `ui_error`, `countdown_tick`, `countdown_go`, `score_award`,
  `reset_channel_start`, `reset_channel_complete`.

Aliases follow their target file: `hit_body` uses `bump`, `pickup` uses `grab`, `throw_release`
uses `throw_whoosh`, `bump_swing` uses `dash`, and `reset_complete` uses
`reset_channel_complete`. A request to restore an alias means restoring and protecting its target.

## 6. Common non-ability VFX and SFX

Use the same sources instead of adding another art family:

| Cue | Visual source | Audio source |
|---|---|---|
| Hit spark | Kenney sharp spark sprite | Kenney Impact Sounds |
| Stun ring | Kenney ring sprite with dark rim | Electric crackle or short Kenney electronic tick |
| Knockdown puff | PVFX Smoke Puff or Landing Dust | Kenney impact plus short dirt layer |
| Slipper impact | PVFX compact impact or Kenney spark/dust | Record real tsinelas on asphalt; use Kenney impact only as support |
| Can knockdown burst | PVFX Warm Explosion cropped to debris, no flame | Tin Can plus Can Rolling recordings |
| Sprint dust | PVFX Landing Dust, stretched with movement | Short dry foot scrape from Kenney RPG Audio |
| Landing puff | PVFX Landing Dust | Kenney RPG Audio land/footstep layer |
| Taya marker | Keep the existing ring geometry | No loop needed; role-change UI cue only |

## 7. Later map, building and prop sources

These are not part of the first implementation pass. They are retained for later map work.

| Asset | Link | Useful coverage | Recolour and pipeline | Licence |
|---|---|---|---|---|
| Quaternius Ultimate Buildings Pack | [Source](https://quaternius.com/packs/ultimatetexturedbuildings.html) | 76 modular buildings and alternate atlas textures | Replace materials with Toon and collapse to the warm palette | CC0, fully free pack, public-repo safe |
| Quaternius Modular Streets Pack | [Source](https://quaternius.com/packs/modularstreets.html) | 25 modular street pieces | Mesh-only path is URP and WebGL safe | CC0, fully free pack, public-repo safe |
| Quaternius Ultimate House Interior Pack | [Source](https://quaternius.com/packs/ultimatehomeinterior.html) | 120+ doors, windows, kitchen, bathroom and household props for eskinita and sari-sari-store kitbashing | Use only readable silhouettes and shared Toon materials | CC0, fully free pack, public-repo safe |
| Quaternius Furniture Pack | [Source](https://quaternius.com/packs/furniture.html) | 23 beds, chairs and tables | FBX/OBJ/Blend, simple mobile-friendly meshes | CC0, fully free pack, public-repo safe |
| Quaternius Ultimate Nature Pack | [Source](https://quaternius.com/packs/ultimatenature.html) | 150 trees, grass, rocks and bushes | Flat warm greens and browns, minimal alpha foliage | CC0, fully free pack, public-repo safe |
| Plastic Monobloc Chair 01 by Kuutti Siitonen | [Source](https://polyhaven.com/a/plastic_monobloc_chair_01) | Exact Filipino street-chair silhouette | About 3K triangles. Discard PBR textures and use one flat Toon material | CC0, public-repo safe |
| Jeepney by Maclin Macalindong ✅ SHIPPED | [Source](https://sketchfab.com/3d-models/jeepney-0b8bcde5df19458da9fa5606989b1e7d) | Static Philippine background landmark | 74.2K triangles, and it ships at 74.2K. ⚠️ **Do NOT decimate or merge materials**, `CLAUDE.md` § 6.0 and § 7.1 below. Keep out of the playable centre | CC BY, commercial use allowed with credit |
| Manila Street by Kevin Luce | [Source](https://freesound.org/people/kevp888/sounds/464557/) | Authentic traffic, motorcycles, jeepneys, horns and voices | 44.1 kHz stereo WAV, edit around foreground events and make a clean loop | CC BY 4.0, commercial use allowed with credit |

### 7.1 ✅ SHIPPED 2026-09-04: jeepney placement on Ilalim ng Tulay, AS DELIVERED

✅ **IT IS IN THE MAP, UNMODIFIED.** 74,170 triangles, 17 materials, 5 textures, its own colours.
It replaces the north `van` at `(-2.8, RoadTop, 30.0)` exactly as the paragraph below asks.
`tools/build_jeepney.py` copies it and checks the licence; it changes nothing about the mesh.

⚠️⚠️ **THE "DECIMATE, MERGE MATERIALS" SENTENCE BELOW IS REVERSED AND IT IS KEPT SO THE REVERSAL
IS FINDABLE.** It was written before anybody had the model. The first build followed it: 74,170
triangles down to **3,000**, seventeen materials collapsed to **one**, UVs rewritten onto the
kit's nine-swatch palette atlas so `tumbang-warm-c` would recolour it like a van. 🧑, opening the
render: *"ew what is that jeep wtf did u do"*, **"u ate all its colors and design wtf"**, and then
the rule: *"no need to lower triangles wtf"*, **"no need to lower triangles or compress dont worry
it wont lag"**, *"make that a rule in claude md"*. **`CLAUDE.md` § 6.0 is that rule and this prop
is why it exists.**

**What was actually wrong with the optimised version, stated so the lesson survives:** the model
is on that boundary for its **silhouette and its livery**, which is the whole of what "culturally
specific" means in the sentence below, and every one of the three steps removed some of it. The
budget was never the constraint: nothing had measured a frame cost, and a background prop at 74K
triangles is not what will cost one.

⚠️ **PLACEMENT IS STILL OURS AND THAT IS THE LINE.** The model is 24.35 units long as authored
against the van's 2.75, so the vehicle row carries its own scale: a van draws at 1.35 and stands
3.71 units long, a jeepney is about 6.0 m against a van's 4.5, so 4.95 units is proportionate and
`4.95 / 24.3526` is the number. **Nothing about the mesh is touched to get it.**

⚠️⚠️ **AND IT IS PLACED WITH AN EMPTY PALETTE, WHICH IS A REQUEST RATHER THAN A FALLBACK.**
`InstantiateKitProp` swaps a `tumbang-warm-*` atlas onto every other vehicle's material; `""`
means keep what the author shipped. A missing atlas still warns, because "I meant to recolour
this and the file is gone" is a defect and "do not recolour this" is not.

⚠️⚠️ **THE CC BY CREDIT IS ENFORCED RATHER THAN REMEMBERED.** `build_jeepney.py` refuses to copy
the model at all unless `CreditsContent.CcByCredits` already names the author, and it reads that
name out of the .glb's own metadata rather than a constant, so a different author on that
Sketchfab id fails loudly instead of approving a credit for the wrong person. A credit that is
"added next time" is a licence breach in every build between the two commits.

⚠️ **THE ORIGINAL BRIEF, KEPT, because the reversal above is only legible beside it:**

Replace one existing generic boundary vehicle rather than adding more traffic. The first target is
the distant north `van` at `(-2.8, RoadTop, 30.0)` in
`IlalimNgTulayBuilder.BuildBoundaryTraffic`. This keeps the jeepney outside the gameplay walls,
preserves the existing traffic count and puts the culturally specific silhouette where it can be
read without competing with the lata.

The source model is 74.2K triangles, so it does not enter the map as delivered. Make an optimised
copy, merge its materials into the map's warm palette, use the existing Toon and outline shaders,
solve its wheels to `RoadTop` through the same visible-bounds path as the current cars, and keep the
CC BY credit. Run `MapGeometryCheck` after replacement. A second instance at the south end is only
allowed if the optimised mesh and rendered frame show that it does not cost the mobile scene or
turn the background into repeated decoration.

## 8. Later UI source art

- [Kenney UI Pack Adventure](https://kenney.nl/assets/ui-pack-adventure), CC0, 130 buttons,
  panels and controls. Use as construction parts and recolour into wood, cream, amber and warm ink.
- [GUI Graphics Kit by Jamie Cross](https://jamiecross.itch.io/graphical-user-interface-graphics-kit-free),
  CC0, more than 60 SVG/EPS/PNG buttons, windows, bars and icons. The vector originals are useful
  for new shapes. Existing authored UI art remains the design system and is not automatically
  replaced by either pack.

### 8.1 Shipped: the CONTROLLER MAP's pad illustration

⚠️ **This one is IN the game already**, unlike the rest of § 8, which is a shopping list.

| | |
|---|---|
| Asset | [Dualshock 4 Layout](https://commons.wikimedia.org/wiki/File:Dualshock_4_Layout.svg) by Tokyoship, via Wikimedia Commons |
| Licence | **CC BY 3.0**. Rule 1 satisfied: free, commercial use allowed, and **not** share-alike, which is the one Creative Commons flavour rule 1 excludes. Rule 8 satisfied: CC BY may live in this public repository with its licence |
| Where | `tools/assets/ds4_gamepad_ccby.svg`, its 2400 px rasterisation beside it, and `ds4_gamepad_ccby.LICENSE.txt` |
| Ships as | `Assets/TumbangPreso/Resources/UI/input/pad_diagram_v1.png`, regenerated by `tools/build_controller_diagram.py` |
| Attribution | ⚠️⚠️ **REQUIRED**, and the line is in § 9 and in the game's credits. Removing the credit and keeping the picture is a licence breach, not a tidy-up |

⚠️⚠️ **THE PLAYSTATION ROUNDEL IS ERASED ON THE WAY IN, AND THAT IS A TRADEMARK CALL RATHER THAN
AN ARTISTIC ONE.** A licence to reuse somebody's DRAWING is not a licence to the trademark inside
it, and this repository already carries one open item of exactly that shape:
`docs/Port_Plan.md` § 8 lists the IKE slipper first in the replacement queue because it *"carries
the real Nike wordmark as geometry"*. The generator fills that disc with the body colour before
anything else runs, so the mark is in no shipped file. **SHARE and OPTIONS stay**: those are
ordinary English words naming a button.

⚠️ **IT IS ALSO RECOLOURED, LIKE EVERYTHING ELSE SOURCED INTO THIS PROJECT**, which is rule 2
(*"the game owns the final look"*) and `CLAUDE.md` § 6.4 together. The source is neutral grey with
black linework and § 6.4 bans cold grey as flatly as it bans navy. Every pixel is mapped onto the
warm paper ramp by luminance, with the top of the ramp pushed to `UiTheme.Paper` so the pad reads
as an object lying ON the honey page rather than a hole cut in it.

⚠️ **THE SEARCH WAS RESTRICTED TO FREELY LICENSED SOURCES ON PURPOSE.** The ask was to take art
off the web; rule 1 is what decided WHICH art, because an arbitrary image result is almost always
somebody's copyright and this repository is public. ⚠️ **A CC0 DualSense could not be found** —
Commons has no DualSense SVG at all and its one PS5 render is **CC BY-SA**, which rule 1 excludes
outright. The DualShock 4 layout is the closest freely licensed drawing to the reference: same
generation, same touchpad, same slab body.

⚠️ **WHAT THIS REPLACED**: Grumbel's PlayStation 3 gamepad (CC0), used for one afternoon. It was
legally simpler and it was the wrong picture. Recoverable from history if the attribution above
ever becomes inconvenient.

### 8.2 Shipped: the gamepad prompt glyphs

⚠️ **Also IN the game**, and it replaced a bought pack rather than filling a gap.

| | |
|---|---|
| Asset | [Input Prompts 1.5](https://kenney.nl/assets/input-prompts) by Kenney, the **PlayStation Series** and **Xbox Series** Default sets |
| Licence | **CC0 1.0**, verbatim in `tools/assets/kenney_ps4/Kenney_License.txt`. Kenney's own words: *"Support by crediting 'Kenney' or 'www.kenney.nl' (this is not a requirement)"* |
| Where | Nineteen source PNGs each in `tools/assets/kenney_ps4/` and `tools/assets/kenney_xbox/` |
| Ships as | `Assets/TumbangPreso/Resources/UI/input/glyphs_pad_v2.png` plus its index, from `tools/build_pad_prompt_icons.py` |
| Attribution | Not required. Kenney is already credited in `CreditsContent.CourtesyCredits` for the environment kits |

⚠️⚠️ **IT IS THE PS4 SET, AND ONLY TWO OF THE NINETEEN FILES ARE ACTUALLY PS4-SPECIFIC.** Kenney
draws the four shapes, the four triggers, the two stick clicks, the two sticks and the d-pad once
for every PlayStation generation. **SHARE and OPTIONS are the pair that changes**, and those are
the PS4 files, because the pad the CONTROLLER MAP draws is a DualShock 4. Moving the diagram to a
DualSense means swapping those two and nothing else.

⚠️ **WHY IT REPLACED WHAT WAS THERE.** The pad glyphs were the Xbox half of vryell's pack, so the
map drew a DualShock and labelled it `Y`, `B`, `A`, `X`: two vocabularies for one device on one
screen. 🧑 2026-09-04: *"change the control icons to these"*, with a sheet of PlayStation prompts.

⚠️⚠️ **BOTH FAMILIES SHIP AND THE GAME PICKS AT RUNTIME**, in one sheet of four rows built from one
column table, so the two orders cannot drift. `UI.InputGlyphs.FamilyOf` asks whether the attached
pad is a `DualShockGamepad`; **Xbox is the default** because everything else Unity matches, and
every pad `GenericPadBridge` stands in for, is XInput-shaped. Showing a cross to somebody holding
an Xbox pad is the same fault as showing `Y` to somebody holding a DualShock.

⚠️ **THE ART IS TINTED, NOT USED AS SHIPPED.** Kenney's prompts are pure white on transparent, so
the generator bakes two rows — ink for paper screens, cream for the in-match HUD — because
`UI.InputGlyphs.For` promises its callers a sprite that is already the right colour. ⚠️ **The
d-pad's highlighted arm keeps a colour of its own** (Persimmon, § 6.4's "marker"), or all four
directions collapse into the same picture.

## 9. Credits block for the attribution-required options

Only add a line if that asset actually ships.

✅ **THE JEEPNEY LINE IS LIVE.** It is in `CreditsContent.CcByCredits` under the chip `JEEPNEY`,
alongside CROCS, PANTULOG and IKE, and it shipped in the same commit as the model.
⚠️ **The Manila Street line below has NOT shipped**, because that recording is not in the game:
`sfx_sky_*` and `sfx_lrt_pass` are still the placeholders. Do not add its credit until it is.

```text
"LS_34209_PH_ManilaStreet.wav" by Kevin Luce (kevp888), licensed under Creative Commons Attribution 4.0, via Freesound.
https://freesound.org/s/464557/
https://creativecommons.org/licenses/by/4.0/

"Jeepney" by Maclin Macalindong, licensed under Creative Commons Attribution, via Sketchfab.
https://sketchfab.com/3d-models/jeepney-0b8bcde5df19458da9fa5606989b1e7d
https://creativecommons.org/licenses/by/4.0/

"Dualshock 4 Layout" by Tokyoship, licensed under Creative Commons Attribution 3.0, via Wikimedia Commons.
https://commons.wikimedia.org/wiki/File:Dualshock_4_Layout.svg
https://creativecommons.org/licenses/by/3.0/
```

⚠️⚠️ **THE LAST ONE IS NOT A PLAN, IT IS IN THE GAME.** It is in `CreditsContent.CcByCredits`
beside the three slippers, because the CONTROLLER MAP's pad ships (§ 8.1). The other lines in this
block are for assets that have not landed yet. **Deleting the credit while the picture ships is a
licence breach rather than a tidy-up.**

## 10. Rejected sources

- Cartoon FX Remaster: $30.
- Lightning Systems by SineVFX: $46 and 150.9 MB.
- Master Stylized FX: paid.
- RPG Game VFX Collection URP: paid.
- Philippine tricycle by kurtcamarines: CC BY-NC and personal-use language.
- Sari-sari Store on 3D Warehouse: no sufficiently clear commercial licence on the asset page and
  roughly 303K polygons.
- Quaternius packs that expose only a limited free subset under a paid Source/Pro tier: excluded by
  the zero-budget brief even where the visible subset says CC0.
- Any asset whose look depends on PBR, 4K texture sheets, compute shaders, VFX Graph, full-screen
  bloom or distortion.

## 11. Integration order

1. Download the five CC0 VFX sources and store their licence text beside the imported art.
2. Build one shared URP particle material and one sprite atlas before replacing an ability.
3. Replace one representative per family first: Seismic Stomp, Permafrost Sheet, Flame Rush,
   Thunderstrike, Devouring Seance and Hex.
4. Run `AbilityShowcaseProbe`, inspect every new frame and keep the 12 percent white-frame gate.
5. Extend the same family to the other twelve abilities without changing gameplay geometry.
6. Download and audition the CC0 SFX recordings, then replace cues family by family. Run
   `AudioCueCheck` after every batch so no catalogue cue becomes unreachable or fileless.
7. Add attribution lines only for CC BY assets that actually ship.
8. Replace the distant north van in Ilalim ng Tulay boundary traffic with the optimised jeepney,
   then run `MapGeometryCheck` and inspect the gameplay-camera frame.
9. Other map and building replacements wait until the ability VFX and SFX pass is complete.
