# Legacy

**JSON root key:** `"legacies"` — value is an array of legacy objects.

Legacies define starting conditions for a new game: which character the player begins as, starting elements, verb/recipe setup, and the initial game state before the player takes control. Each legacy is an entry point into a new playthrough.

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique legacy identifier. |
| `lever` | string | `""` | Content-gating lever. |
| `family` | string | `""` | Legacy family/group for categorisation. |

### Display

| Field | Type | Default | Description |
|---|---|---|---|
| `label` | string | `""` | Title text shown in the legacy selection screen (localised). |
| `desc` | string | `""` | Description text (localised). |
| `startdescription` | string | `""` | Narrative text shown at the start of the game (localised). |
| `image` | string | `""` | Image sprite key for the legacy card/portrait. |
| `comments` | string | `""` | Internal notes. |

### Availability

| Field | Type | Default | Description |
|---|---|---|---|
| `fromending` | string | `""` | Ending ID that unlocks this legacy. If set, this legacy is only available after completing that ending. |
| `availablewithoutendingmatch` | bool | `false` | If true, this legacy is available even without matching the required ending. |
| `excludesonending` | array of legacy IDs | `[]` | Legacy IDs that are excluded/unlocked alongside this one when its fromending is reached. |

### Starting State

| Field | Type | Description |
|---|---|---|
| `effects` | dict of `aspectId → level` | Starting aspects/elements granted to the player. Similar to recipe effects — specifies element IDs with levels/quantities. |
| `startup` | array of LinkedRecipeDetails | Recipes to execute on game start. Each entry specifies a recipe ID and a target path for where to place the resulting situation/verb. Used to set up initial verbs, situations, and interactions. |

---

## Startup Processing

At game start, `startup` entries are processed into `RootPopulationCommand`:

- If the referenced recipe's verb is **spontaneous**, the recipe's output is sent to the specified `topath` as a chamberlain setup command.
- If the verb is **not spontaneous**, a `TokenCreationCommand` is created that places the verb+recipe at the specified path.

---

## Sub-Entities Used

| Sub-Entity | Field | Description |
|---|---|---|
| [LinkedRecipeDetails](linkedrecipedetails.md) | `startup[ ]` | Recipes to execute at game start. |

---

## Example

```json
{
  "legacies": [
    {
      "id": "librarian",
      "label": "Brancrug, March 7th, 1936",
      "startdescription": "The Librarian arrives at Hush House...",
      "desc": "A new Librarian takes up residence at Hush House...",
      "effects": { "journal": 1 },
      "startup": [
        { "id": "setup.librarian.consider", "topath": "~/fixedverbs" },
        { "id": "setup.librarian.bookcase", "topath": "~/library!lodge/?" }
      ]
    }
  ]
}
```
