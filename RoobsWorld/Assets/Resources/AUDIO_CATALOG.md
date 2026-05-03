# Audio catalog - RoobsWorld

Place audio files in **Music/** or **SFX/** with **exact filenames** (no extension in this list; Unity imports .mp3, .wav, etc.). `AudioClipLoader` loads by these IDs at startup.

**Cell-stage minimum (scope):** Music: `cell_exploration`, `cell_combat`, `cell_tense`, `cell_victory`. Ambient: `ambient_underwater`. SFX: `ui_click`, `food_eaten_plant`, `food_eaten_meat`, `food_eaten_toxin`, `evolution_complete`, `mutation_choice`, `creature_death`.

## Music (Assets/Resources/Music/)

| ID | Stage / use |
|----|-------------|
| cell_exploration | Cell stage |
| cell_combat | Cell stage |
| cell_tense | Cell stage |
| cell_victory | Cell stage |

## Ambient (Assets/Resources/Music/)

| ID | Stage |
|----|-------|
| ambient_underwater | Cell |

## SFX (Assets/Resources/SFX/)

| ID | Use |
|----|-----|
| ui_click | UI buttons |
| ui_hover | UI hover |
| ui_back | Back / cancel |
| attack_hit | Melee hit |
| attack_miss | Miss |
| creature_death | Creature death |
| creature_spawn | Spawn |
| food_eaten | Eating (generic fallback) |
| food_eaten_plant | Eating algae / plant food |
| food_eaten_meat | Eating dead cells / meat |
| food_eaten_toxin | Eating toxin |
| evolution_complete | Evolution threshold reached |
| mutation_choice | Mutation choice panel opened |
| dna_gained | DNA pickup |
| level_up | Level up |
| stage_transition | Stage change |
| building_placed | Building placed |
| diplomacy_success | Diplomacy success |
| diplomacy_fail | Diplomacy fail |
| shield_hit | Shield hit |
| explosion_small | Small explosion |
| explosion_large | Large explosion |
| terraform_pulse | Terraforming |
| colony_established | Colony founded |
| victory_fanfare | Victory |
