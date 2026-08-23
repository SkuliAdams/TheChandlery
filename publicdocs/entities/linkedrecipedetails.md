# LinkedRecipeDetails

**Not a top-level entity.** A sub-entity embedded inline within other entity types. Declared with `[FucineLocalizableAs(typeof(Recipe))]`.

LinkedRecipeDetails references another recipe and defines how it should be chained, triggered as an alternative, or induced. Used for recipe sequencing, random outcomes, and legacy startup actions.

---

## Where Used

| Parent Entity | Field(s) | Role |
|---|---|---|
| [Recipe](recipe.md) | `linked[ ]`, `alt[ ]`, `lalt[ ]`, `inductions[ ]` | Recipe chaining, alternatives, and inductions. |
| [Legacy](legacy.md) | `startup[ ]` | Startup recipes executed on new game. |

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Recipe ID to reference. Supports `*` wildcards (e.g. `"setup.*"` matches all setup recipes). Localised like a recipe. |
| `lever` | string | `""` | Content-gating lever. |

### Chance & Selection

| Field | Type | Default | Description |
|---|---|---|---|
| `chance` | int | `100` | Percentage chance (0-100) of this link triggering. |
| `additional` | bool | `false` | If true, this link fires in **addition** to the default outcome rather than replacing it. |
| `shuffle` | bool | `false` | If true, shuffle the pool of matching recipes before picking. |
| `challenges` | dict of `aspectId → level` | Challenge aspects that modify the chance roll (like recipe reqs for the linked result). |

### Output Routing

| Field | Type | Description |
|---|---|---|
| `topath` | string (path) | FucinePath to the sphere where the linked recipe's result output should be sent. |
| `outputpath` | string (path) | Alternative output path override for results. |

### Ambits

| Field | Type | Description |
|---|---|---|
| `ambits` | dict of `aspectId → value` | Ambit values passed into the linked recipe's context. These override or supplement the parent's ambits. |

### Expulsion

| Field | Type | Description |
|---|---|---|
| `expulsion` | Expulsion (inline sub-entity) | Defines expulsion rules triggered by this link — which elements get kicked out of slots and where they go. |

---

## Sub-Entities Used

| Sub-Entity | Field | Description |
|---|---|---|
| [Expulsion](expulsion.md) | `expulsion` | Element expulsion rules for this link. |

---

## Example

```json
{
  "linked": [
    {
      "id": "chandlery.aftermath",
      "chance": 100,
      "topath": "~.tabletop",
      "additional": false,
      "expulsion": {
        "filter": { "fuel": 1 },
        "limit": 1,
        "topath": "~.shelf"
      }
    }
  ]
}
```
