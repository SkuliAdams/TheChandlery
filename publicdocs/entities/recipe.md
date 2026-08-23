# Recipe

**JSON root key:** `"recipes"` — value is an array of recipe objects.

Recipes define all player actions in Book of Hours: crafting, terrain unlocking, consider interactions, visitor talks, and event completions. A recipe is matched to a verb via its `actionid` and executed when requirements are satisfied.

---

## Fields

### Identity & Routing

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique recipe identifier. Prefix `_` = platonic template (not directly executable). |
| `actionid` | string | `"x"` | Verb ID or wildcard this recipe appears under. `"x"` = not listed in any verb. Supports `*` wildcards (e.g. `"library.*"`). |
| `inherits` | string | `""` | ID of a platonic recipe to inherit properties from. Merges dict/list fields. |
| `lever` | string | `""` | Content-gating lever that must be active. |

### Display Text

All display text fields default to `"."` (meaning unset, may trigger inheritance). Localised.

| Field | Type | Description |
|---|---|---|
| `label` | string | Recipe name shown in verb popup. |
| `startlabel` | string | Alternate label shown when recipe starts. |
| `preface` | string | Short heading text on the recipe card. |
| `startdescription` | string | Description shown when recipe is first started. |
| `desc` | string | Description shown on completion. |
| `comments` | string | Internal developer notes (not localised). |

### Requirements

Requirements are dictionaries mapping aspect IDs to levels. Negative levels mean "must NOT have this aspect."

| Field | Type | Description |
|---|---|---|
| `reqs` | dict | Standard requirements — aspects summed across all slotted cards must meet these levels. |
| `extantreqs` | dict | Extant requirements — aspects on the verb/workstation itself (not the slotted cards). |
| `greq` | dict | Group requirement — at least one single card must satisfy all specified aspects. |
| `ngreq` | dict | Negative group requirement — no single card may satisfy all specified aspects. |
| `fxreqs` | dict | Environment FX state requirements. Prefix key with `-` to require absence. |
| `seeking` | dict | **Obsolete.** Previously used for advanced aspect requirements. |

### Effects & Output

| Field | Type | Description |
|---|---|---|
| `effects` | dict of `elementId → level` | Elements to add to the output. Level can be numeric or a dynamic expression (e.g. `"mystery.edge"`). Use negative levels to consume/destroy slotted elements. |
| `xpans` | dict of `aspectId → int` | Aspects to apply to the output element after creation. |
| `fx` | dict of `fxKey → value` | Environment FX commands. Keys are fx identifiers (e.g. `"vignette.none"`, `"thechandlery.open"`, `"sky.day"`). Values depend on the fx type. |
| `mutations` | array of MutationEffect | Modify aspects on specific cards in the situation. |
| `purge` | dict of `aspectId → int` | Remove cards matching these aspects from the situation. |
| `deckeffects` | dict of `deckId → draws` | Draw from named decks and add results to output. |
| `internaldeck` | DeckSpec (inline) | Defines an inline deck that is drawn from upon recipe completion. |

### Linked / Alternative Recipes

| Field | Type | Description |
|---|---|---|
| `linked` | array of LinkedRecipeDetails | Recipes chained to execute after this one completes. |
| `alt` | array of LinkedRecipeDetails | Mutually exclusive alternative outcomes, resolved by chance. |
| `lalt` | array of LinkedRecipeDetails | Like `alt` but each entry has an independent chance check. |
| `inductions` | array of LinkedRecipeDetails | Recipes that can be induced from this one (alternative flow triggered by specific conditions). |

### Slots

| Field | Type | Description |
|---|---|---|
| `preslots` | array of SphereSpec | Pre-visible slots on the verb popup before the recipe starts. Used for terrain unlock keys, etc. |
| `slots` | array of SphereSpec | Card slots on the recipe card for player input. |

### Crafting & Visibility

| Field | Type | Default | Description |
|---|---|---|---|
| `craftable` | bool | `false` | If true, recipe appears in the verb's selectable recipe list. |
| `hintonly` | bool | `false` | If true, recipe only shows as a hinted-possible recipe (grayed out). |
| `notable` | bool | `false` | If true, recipe appears under the Notable heading. |
| `ambittable` | bool | `true` | If false, automation/ambit matching won't suggest this recipe. |
| `warmup` | int | `0` | Duration in seconds for the recipe to complete. |
| `blocks` | bool | `true` | If true, this recipe's requirement aspects block further application for matching `imms` on elements. |

### Ending & Achievements

| Field | Type | Default | Description |
|---|---|---|---|
| `ending` | string | `""` | Ending ID triggered when this recipe completes (game victory). |
| `signalendingflavour` | string enum | `None` | Flavour for the ending display: `None`, `Grand`, `Melancholy`, `Pale`, `Vile`, `Enigmatic`, `Positive`. |
| `signalimportantloop` | bool | `false` | If true, this recipe represents a significant game loop milestone. |
| `achievements` | array of strings | `[]` | Achievement IDs to unlock on completion. |

### Audio & Visual

| Field | Type | Default | Description |
|---|---|---|---|
| `icon` | string | `"x"` | Icon key for the recipe. |
| `burnimage` | string | `null` | Image shown when the recipe card is burning/active. |
| `audiooneshot` | string | `null` | One-shot audio clip played when recipe starts. |

### Utility

| Field | Type | Default | Description |
|---|---|---|---|
| `aspects` | dict | `{}` | Self-aspects of the recipe (for categorisation, e.g. `fatiguing.ability: 1`). |
| `run` | string | `""` | Run cycle key (`"day"`, `"night"`). |
| `haltverb` | dict of `verbId → int` | Stop/disable specific verbs while this recipe is active. |
| `deleteverb` | dict of `verbId → int` | Delete/remove specific verbs when this recipe completes. |

---

## Sub-Entities Used

| Sub-Entity | Field(s) | Description |
|---|---|---|
| [SphereSpec](spherespec.md) | `preslots[ ]`, `slots[ ]` | Card slot definitions for player input. |
| [MutationEffect](mutationeffect.md) | `mutations[ ]` | Aspect modification rules on specific cards. |
| [LinkedRecipeDetails](linkedrecipedetails.md) | `linked[ ]`, `alt[ ]`, `lalt[ ]`, `inductions[ ]` | Linked/chained recipe references. |
| [Deck](deck.md) | `internaldeck` | Inline deck definition drawn on completion. |

---

## Inheritance

When `inherits` is set, the parent's dictionary fields (`reqs`, `effects`, `fx`, etc.) and list fields (`slots`, `mutations`, `linked`, etc.) are merged onto the child. Scalar fields (`label`, `desc`, `warmup`, etc.) are only inherited if the child hasn't set them.

---

## Example

```json
{
  "recipes": [
    {
      "id": "terrain.thechandlery",
      "actionid": "terrain.unlock",
      "label": "The Chandlery",
      "preface": "The Chandlery",
      "startdescription": "You're pretty sure this wasn't here yesterday.",
      "desc": "It's filled with candles.",
      "preslots": [
        {
          "id": "infoRecipeInput",
          "label": "#UI_ROOMINPUT_LABEL#",
          "description": "#UI_ROOMINPUT_DESCRIPTION#",
          "required": { "forge": 1 },
          "forbidden": {},
          "essential": { "assistance": 1 }
        }
      ],
      "fx": { "thechandlery.open": 1 },
      "warmup": 30,
      "craftable": true
    }
  ]
}
```
