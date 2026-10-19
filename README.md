# Stardew Voices

Stardew Voices adds support for adding voice over to the many wonderful characters in [Stardew Valley]([url](https://www.stardewvalley.net/))! The base mod includes voices for all of the love interests as of the 1.6 update. Since releasing, Stardew Voices has pivoted from being solely a dub to becoming a framework for anyone to develop and release their own dub mod in any language. 

The full version of the mod can be downloaded from [Nexus Mods]([url](https://www.nexusmods.com/stardewvalley/mods/22550?tab=files)). This will include the audio files for all recorded voice lines for the vanilla romanceable npcs (not included in this repo).

## Support for Other Languages

If your game is set to a language other than English audio files will not play unless your Stardew Voices mod is configured to the correct language code:
*This will result in the English voice over lines being played even when the game is configured for a different language*

Instructions
Without Generic Mod Config Menu installed:
﻿1. Open the Config.json file in your Stardew Voices folder
﻿2. You will see "Language": "en" replace "en" with the EXACT string in the list below that corresponds to your language
  ﻿Note: You will have to restart the game for changes to take effect.

Deutsch: "de"
Español: "es"
Français: "fr"
Italiano: "it"
日本語: "ja"
한국인: "ko"
Magyar: "hu"
Português: "pt"
Русский: "ru"
Türkçe: "tr"
中国人: "zh"

With [Generic Mod Config]([url](https://www.nexusmods.com/stardewvalley/mods/5098))!
- Open the generic mod config (either through keyboard shortcut or the gear icon at the bottom left of your menu screen)
- Click the dropdown at the top labeled "Language" and select your desired language
  ﻿Note: You will have to restart the game for changes to take effect.
- ANOTHER NOTE: The language chosen MUST match the language Stardew Valley is configured in


## AI DISCLAIMER
AI is not used in generating images or voices. I DO use AI in my coding workflow. This includes asking coding questions, generating code, creating translations, and searching for and fixing bugs. I currently use Claude by Anthropic as I feel it is the most ethical of the companies as of right now. You can check out Claude’s [Constituation]([url](https://www.anthropic.com/constitution)) and its [Responsible Scaling Policy]([url](https://www.anthropic.com/responsible-scaling-policy)) for more info.

I plan to explain my workflow in greater detail in the code walkthrough! While I won’t be able to fully label everything that was AI generated in the code, it’s kind of easy to spot because fully AI generated methods have REALLY long comments. 

AI is a complicated and nuanced topic right now. I believe it is a wonderful tool for automating away tedious tasks to leave more room in our workday for creative problem solving and playing with cats. I also feel the concern over societal and environmental impact is warranted and important to voice. I plan to always be transparent about my AI usage.


## Commands for Mod Authors and Dub Makers

**sv_validation**
Runs dialogue voice validation for the language code specified in config.json. 

**sv_generation**
Generates Dialogue JSONs and/or Script files in .md/.txt format. Will be exported to your Stardew Voices Mod folder in a “Generated Files” folder.

Usage: sv_generation [filesToGenerate] [scriptFormat] [generateGossipDialogue]
              - filesToGenerate: "json", "script", "both" (default: both)
              - scriptFormat: "txt", "md", "both" (default: both)
              - generateGossipDialogue: "yes", "no" (default: no)
		- (Gossip dialogue is every possible line for a character for explaining gift tastes of another character “Did you know [character] loves [this]?”)


**sv_play_line**
Looks up and plays a voiced dialogue line for a character.

Usage: sv_play_line <characterName> <dialogueString>
Example: sv_play_line Alex “Wow I’m so buff and cool”.
