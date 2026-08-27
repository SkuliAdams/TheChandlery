# Verb

**JSON root key:** `"verbs"` — value is an array of verb objects.

Verbs are the game's actions/workstations: the verbs in the verb toolbar, workspaces (beds, library rooms), the terrain unlock verb, and the consider verb. Each verb has a slot where the player places a card, and a recipe list that determines what can be crafted.

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique verb identifier. Used in recipe `actionid` matching. |
| `lever` | string | `""` | Content-gating lever. |

### Display

| Field | Type | Default | Description |
|---|---|---|---|
| `label` | string | `"."` | Human-readable name (localised). |
| `desc` | string | `"."` | Description shown when hovering (localised). |
| `icon` | string | `null` | Icon key for the verb button. |
| `comments` | string | `""` | Internal notes. |

### Slot Configuration

| Field | Type | Description |
|---|---|---|
| `slot` | SphereSpec (inline) | Single card slot definition. If `slots` (plural) is also provided, they are combined into `Thresholds`. |
| `slots` | array of SphereSpec | Multiple card slots. Combined with `slot` into `Thresholds`. |

The verb's card slot(s) define what type of card can be placed to activate a recipe. At minimum, a verb needs at least one slot.

### Behaviour

| Field | Type | Default | Description |
|---|---|---|---|
| `category` | string enum | `Classic` | Visual category: `Classic`, `Workstation`, `Roomwork`, etc. |
| `spontaneous` | bool | `false` | If true, this verb is not placed by the player but is auto-generated (e.g. terrain unlock, incidents). |
| `multiple` | bool | `false` | If true, multiple instances of this verb can exist simultaneously. |
| `maxnotes` | int | `3` | Maximum number of note/description lines shown. |
| `ambits` | bool | `false` | If true, this verb supports ambit-based automation. |
| `hints` | array of strings | `[]` | Hint recipe IDs shown in the verb popup as suggestions. |

### Audio

| Field | Type | Default | Description |
|---|---|---|---|
| `audio` | string | `"Tile"` | Audio category for the verb's interaction sounds. |

### Aspects & Morphing

| Field | Type | Description |
|---|---|---|
| `aspects` | dict | Self-aspects of the verb (used for categorisation, filtering). |
| `xtriggers` | dict of `key → [VerbMorphDetails]` | Morph triggers. Similar to element xtriggers but morphs target verbs. Each key is a trigger condition; value is a list of morph rules. |

---

## Sub-Entities Used

| Sub-Entity | Field(s) | Description |
|---|---|---|
| [SphereSpec](spherespec.md) | `slot`, `slots[ ]` | Card slot definitions. |
| [VerbMorphDetails](verbmorphdetails.md) | `xtriggers{ }[ ]` | Verb-specific morph rules. |

---

## Behaviour Notes

- The verb's `Thresholds` list is populated at import time from `slot` (singular) + `slots` (plural). If `slots` is non-empty, they're appended; otherwise `slot` alone is used.
- A `spontaneous` verb is automatically created by the game (not placed by the player). The terrain unlock verb is spontaneous.
- Recipes target verbs via `actionid` matching, which supports `*` wildcards. E.g. `"library.*"` matches `"library.consider"`, `"library.craft"`, etc.

---

## Example

```json
{
  "verbs": [
    {
      "id": "terrain.unlock",
      "label": "Unlock",
      "spontaneous": true,
      "category": "roomwork",
      "slot": {
        "id": "infoRecipeInput",
        "label": "Key",
        "required": { "key": 1 },
        "forbidden": {}
      },
      "maxnotes": 0,
      "audio": "Work"
    }
  ]
}
```
