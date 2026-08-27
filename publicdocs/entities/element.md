# Element

**JSON root key:** `"elements"` — value is an array of element objects.

Elements are the most fundamental entity in Book of Hours. They represent everything that can exist as a token in the game: cards, aspects, skills, memories, visitors, books, resources, abilities, journal entries, etc.

Each element can have `aspects` (its traits), `slots` (card slots it provides when placed), `xtriggers` (morphing rules), and various display/behaviour flags.

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique identifier. Used to reference this element in aspects, recipes, deck specs, etc. |
| `lever` | string | `""` | Content-gating lever that must be active for this element to be available. |
| `inherits` | string | `""` | ID of another element to inherit aspects, slots, xtriggers, and imms from. |

### Display

| Field | Type | Default | Description |
|---|---|---|---|
| `label` | string | `""` | Human-readable name (localised). |
| `alphalabeloverride` | string | `""` | Alternate label used for alphabetical sorting (localised). Strips `+` characters from `Label` by default. |
| `desc` | string | `""` | Description text shown on the element (localised). |
| `icon` | string | `""` | Sprite key for the element's icon. Defaults to the element's `id` if empty. |
| `verbicon` | string | `""` | Alternate icon used when the element appears as a verb icon. |
| `sort` | string | `"groupzzz.zzz"` | Sort key in format `"group.order"`. Controls ordering in collections. |
| `comments` | string | `""` | Internal developer notes (not localised, not shown in game). |

### Behaviour Flags

| Field | Type | Default | Description |
|---|---|---|---|
| `isaspect` | bool | `false` | If true, this element is a pure aspect (trait) with no card body. |
| `ishidden` | bool | `false` | If true, hidden from the player in most UI contexts. |
| `noartneeded` | bool | `false` | If true, no art asset is required; procedurally generated. |
| `metafictional` | bool | `false` | If true, this element is meta/diagetic (like journal entries). |
| `unique` | bool | `false` | If true, only one instance of this element can exist globally. |
| `resaturate` | bool | `false` | If true, recolour the icon on each draw (used for random-colour elements). |
| `manifestationtype` | string | `"Card"` | Visual manifestation style: `"Card"`, `"Book"`, `"Comfort"`, `"WallArt"`, `"Candle"`, `"Thing"`, etc. Controls icon rendering and determines `LivingInTheMaterialWorld()`. |
| `audio` | string | `"Card"` | Audio category for card interaction sounds. |

### Lifetime & Decay

| Field | Type | Default | Description |
|---|---|---|---|
| `lifetime` | float | `0` | Duration in seconds before this element decays. `0` = no decay. If > 0, decay triggers automatically. |
| `decayto` | string | `""` | Element ID to decay into when lifetime expires. |
| `uniquenessgroup` | string | `""` | Aspect ID used as a uniqueness group. Elements in the same group are mutually exclusive (only one can exist). Automatically adds this aspect to the element. |

### Aspects & Traits

| Field | Type | Description |
|---|---|---|
| `aspects` | dict | Element's own aspects (traits), keyed by aspect element IDs with numeric levels. E.g. `{ "forge": 2, "lantern": 1, "skill": 1 }`. |

### Slots

| Field | Type | Description |
|---|---|---|
| `slots` | array of SphereSpec | Card slots this element provides when placed in a verb or another element. Each slot defines what cards can be placed in it. Used on skills (effort/memory slots), visitors (considerations), workstations, etc. |

See [SphereSpec](spherespec.md) for slot field details.

### Morphing & Triggers

| Field | Type | Description |
|---|---|---|
| `xtriggers` | dict of `aspectId → [MorphDetails...]` | Morph triggers keyed by aspect. When the element's aspects reach the specified level, the listed morph effects fire (transform, add aspect, remove aspect, etc.). The key is the triggering aspect ID; the value is a list of morph rules. |
| `xexts` | dict of `aspectId → string` | Extant triggers keyed by element aspect IDs. Values are aspect IDs to extend onto the parent sphere. |
| `ambits` | dict of `aspectId → value` | Ambit values provided by this element for matching. Ambits expand the element's usability in broader contexts. |
| `reverseambittablesdisplay` | bool | `true` | If true, reverse the display of ambittable elements in UI listings. |

### Commute (Value Conversion)

| Field | Type | Description |
|---|---|---|
| `commute` | array of element IDs | Elements that can be exchanged for this one. Used for value conversion chains. Each listed element must have an aspect matching this element's ID to determine worth. |

### Immunities

| Field | Type | Description |
|---|---|---|
| `imms` | array of recipe objects | Recipes that this element is immune to. When the `blocks` field is true on a recipe, matching elements block aspect application. |

### Achievements

| Field | Type | Description |
|---|---|---|
| `achievements` | array of achievement IDs | Achievements unlocked when this element is first obtained. |

---

## Sub-Entities Used

| Sub-Entity | Field | Description |
|---|---|---|
| [SphereSpec](spherespec.md) | `slots[ ]` | Defines each card slot's requirements, label, and behaviour. |
| [MorphDetails](morphdetails.md) | `xtriggers{ }[ ]` | Defines morph rules triggered by aspect thresholds. |

---

## Inheritance

When `inherits` is set, the parent element's `aspects`, `xtriggers`, `ambits`, `slots`, and `imms` are merged into the child. Scalar fields (`label`, `desc`, `manifestationtype`, `uniquenessgroup`, `lifetime`) are only inherited if the child hasn't set them. Boolean flags (`isaspect`, `ishidden`, `metafictional`, `resaturate`) are OR-combined (if parent has it, child gets it).

---

## Example

```json
{
  "elements": [
    {
      "id": "skill.chandlery",
      "label": "Chandlery",
      "desc": "The art of making candles, lamps, and lights.",
      "aspects": { "forge": 2, "lantern": 1, "w.illumination": 1, "skill": 1 },
      "slots": [
        {
          "id": "a1",
          "label": "Effort",
          "actionid": "consider",
          "required": { "ability": 1 },
          "essential": { "chandlery": 1 }
        },
        {
          "id": "b1",
          "label": "Lesson",
          "actionid": "consider",
          "essential": { "chandlery.lesson": 1 }
        }
      ],
      "xtriggers": {
        "skillingup": [
          { "id": "skill.chandlery", "morpheffect": "Upgrade", "level": "1", "chance": 100 }
        ]
      },
      "commute": ["s.standard.chandlery"],
      "sort": "skill.i.chandlery"
    }
  ]
}
```
