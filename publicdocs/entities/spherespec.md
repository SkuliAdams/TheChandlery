# SphereSpec

**Not a top-level entity.** A sub-entity embedded inline within other entity types. Not declared via `[FucineImportable]`.

SphereSpec defines a card slot: its label, which aspects are required/forbidden/essential, whether it auto-fills, and its path connections. Used across Elements, Verbs, and Recipes.

---

## Where Used

| Parent Entity | Field(s) | Role |
|---|---|---|
| [Element](element.md) | `slots[ ]` | Card slots on an element (e.g. skill effort/memory slots). |
| [Verb](verb.md) | `slot`, `slots[ ]` | The verb's card input threshold(s). |
| [Recipe](recipe.md) | `preslots[ ]`, `slots[ ]` | Recipe card slots for player input. |

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique slot identifier within its parent (e.g. `"infoRecipeInput"`, `"a1"`, `"c"`). |
| `lever` | string | `""` | Content-gating lever. |

### Display

| Field | Type | Default | Description |
|---|---|---|---|
| `label` | string | `""` | Slot label shown to the player (localised). |
| `description` | string | `""` | Tooltip/hover description (localised). |
| `availablefromhouse` | string | `""` | Which verb/house makes this slot available (localised). |

### Slot Behaviour

| Field | Type | Default | Description |
|---|---|---|---|
| `actionid` | string | `""` | Verb ID or wildcard that this slot responds to. Supports `*`. Empty = matches all verbs. |
| `greedy` | bool | `false` | If true, the slot auto-fills with any valid card when it becomes available. |
| `frompath` | string (path) | `""` | FucinePath from which cards are automatically pulled for this slot. |
| `enroutespherepath` | string (path) | `""` | FucinePath for the "en route" sphere (animation/transition). |
| `windowsspherepath` | string (path) | `""` | FucinePath for the slot's viewing window. |

### Aspect Requirements

All three dictionaries map aspect IDs to levels:

| Field | Type | Description |
|---|---|---|
| `required` | dict of `aspectId → level` | Aspects the placed card should have (at least one must match). |
| `essential` | dict of `aspectId → level` | Aspects the placed card **must** have (all must match, gating at the aspect level). |
| `forbidden` | dict of `aspectId → level` | Aspects that are forbidden in this slot. |
| `ifaspectspresent` | dict of `aspectId → condition` | Conditional requirements based on which aspects are already present in the situation. |

### Angels

| Field | Type | Description |
|---|---|---|
| `angels` | array of AngelSpecification | Angel behaviours for this slot. Angels automate card moving and slot management. |

---

## Sub-Entities Used

| Sub-Entity | Field | Description |
|---|---|---|
| [AngelSpecification](angelspecification.md) | `angels[ ]` | Angel behaviour rules for this slot. |

---

## Example

```json
{
  "slots": [
    {
      "id": "infoRecipeInput",
      "label": "Key",
      "description": "A key or tool suitable for this task",
      "required": { "knock": 2, "forge": 1 },
      "essential": { "thing": 1, "key": 1 },
      "forbidden": { "assistance": 1 },
      "actionid": "terrain.unlock",
      "greedy": false,
      "angels": [
        { "choir": "Automation", "livein": "slot1", "watchover": "slot2" }
      ]
    }
  ]
}
```
