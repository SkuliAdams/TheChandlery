# Expulsion

**Not a top-level entity.** A sub-entity embedded inline within LinkedRecipeDetails.

Expulsion defines rules for ejecting specific elements from their slots when a linked recipe triggers. This is used to clear out cards that shouldn't persist after a recipe chains or alternates.

---

## Where Used

| Parent Entity | Field | Role |
|---|---|---|
| [LinkedRecipeDetails](linkedrecipedetails.md) | `expulsion` | Defines which cards to kick out and where they go. |

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Identifier for this expulsion rule. |
| `lever` | string | `""` | Content-gating lever. |

### Filtering

| Field | Type | Default | Description |
|---|---|---|---|
| `filter` | dict of `aspectId → level` | `{}` | Aspect filter — elements with matching aspects are candidates for expulsion. |

### Behaviour

| Field | Type | Default | Description |
|---|---|---|---|
| `limit` | int | `0` | Maximum number of elements to expulse. `0` = no limit. |
| `topath` | string (path) | empty | FucinePath to send expulsed elements to. |

---

## Example

```json
{
  "expulsion": {
    "filter": {
      "fuel": 1,
      "ingredient": 1
    },
    "limit": 2,
    "topath": "~.portage"
  }
}
```
