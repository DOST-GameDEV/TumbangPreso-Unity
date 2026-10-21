# Hero voice lines: research and decisions (VOICE-1, 2026-09-24)

Owner: *"can u give them all their own voice lines too or with actual audio and connect it to their
story and personality"*, *"make sure theres voicelines wherein they interact with each other and
voicelines related to skills ... use valorant as reference for when voiceliens are used or how
theyre constructed, research other games too"*.

## What was read

| Source | What it says | What TUMP takes |
|---|---|---|
| VALORANT, as documented by [GameRiv's ultimate line list](https://gameriv.com/all-the-agent-ultimate-voice-line-in-valorant/) and the [Dot Esports list](https://dotesports.com/valorant/news/all-valorant-ultimate-voice-lines-for-all-agents) (the second refused a direct fetch; its search summary was used) | Each agent's ultimate has a line that differs by listener: allies hear a warning, enemies hear a threat (Raze warns teammates of the rockets). Agents call abilities, react to kills, the Spike and round start, and trade banter in the buy phase when specific agents are together. | Two ultimate readings, `UltimateAlly` and `UltimateOpponent`, chosen per listener. Skill calls. Pre-match banter only between heroes the biographies connect. |
| Overwatch, from the [Blizzard forum thread on ultimate lines](https://us.forums.blizzard.com/en/overwatch/t/ultimate-voice-lines-changed/515696) and general knowledge of shipped play (labelled as such) | Two ultimate lines, one for the caster's side and one for enemies; pre-match hero interactions when two related heroes share a team. | The same two-reading rule, and the "only when both are present" rule for interactions. |
| [Game Developer, "Adding Life To Worlds With Dialogue Barks"](https://www.gamedeveloper.com/design/adding-life-to-worlds-with-dialogue-barks) | "Barks should be as subtle as possible, bearing in mind that they're often repeatable." Relevance to the player's own action makes a bark compelling. | Seven words at most (a test holds it). Lines react to what that player just did. |
| [Game Developer, "Organizing And Formatting Game Dialogue"](https://www.gamedeveloper.com/design/organizing-and-formatting-game-dialogue) and the Valve "dynamic dialog" GDC talk it points to (Elan Ruskin), via search summary | Barks need priority (a higher-priority line is not interrupted by a lower one) and rate limits. | `HeroLines.Priority`, per-hero and per-skill cooldowns, one line in the room at a time, the announcer first. |
| This repository: `docs/HUMAN.md`, `VoiceDirector` | Voice casting is 10 per cent of the competition score; the team records its own Filipino lines; takes cycle rather than repeat at random; the voice ducks the music. | The script is written into HUMAN.md Table E for the team to record; the runtime cycles lines; a recording replaces a generated clip by name. |

## Decisions

1. **The script is the characters' biographies.** Every line is written from `CHARACTER_ORIGINS.md`
   and `BADJAO_EXPANSION.md`: Sean's patience and score-keeping, Zack's showing off for the
   neighbours, Dante's fairness, Cheska's quiet planning (and "who's got the water?"), Nemu's drift
   and Kuro, Phaister's staging, Rafi's warm teasing. The twelve exchanges are the relationships
   those files already wrote, not new ones.
2. **The audio is a stylised babble per hero, not synthetic speech.** A neural text-to-speech voice
   was available on this machine (a Piper voice downloads here), but HUMAN.md treats voice casting
   as scored team work, and putting a machine speaking voice into a competition entry is the
   owner's decision, not a build script's. So each hero speaks in the manner of Animal Crossing or
   Banjo-Kazooie: their own pitch, throat, speed and delivery following the line's syllables and
   punctuation, with the words in a caption. Measured medians: Dante 128 Hz, Sean 183, Rafi 204,
   Zack 217, Nemu 267, Phaister 272, Cheska 288. **A team recording dropped in under the same file
   name replaces it with no code change**, and the generator never overwrites a file it did not
   write.
3. **Spoken per peer, never relayed**, so nothing new goes on the wire.
4. **Captions for hero lines are an accessibility option, off by default**, under their own setting
   beside the announcer's, because `VISION.md` section 3 says the in-match HUD carries no sentences.
   Until the team records the lines, the babble carries each hero's character and the moment's tone
   (a rising exclamation on a knockdown, a hero's own pitch and pace), and the words are one toggle
   away.

## What is not verified here

No Unity licence on the cloud machine: the clips were measured (length, pitch, peak) and drawn as a
waveform sheet, not heard in a match. Owed on Windows: listen to a Hero Strike match with four heroes
(the skill rest, the announcer hand-off, the banter at round one, the ultimate's two readings from
both sides), and the caption panel at 4:3 and on a phone.
