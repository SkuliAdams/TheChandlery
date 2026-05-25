# MutationEffect

**Not a top-level entity.** A sub-entity embedded inline within recipes.

MutationEffect modifies aspects on specific cards (mutations) during recipe execution. It can add, remove, or set aspect levels on matching cards based on a filter.

---

## Where Used

| Parent Entity | Field | Role |
|---|---|---|
| [Recipe](recipe.md) | `mutations[ ]` | Defines aspect modifications on cards in the situation. |

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Identifier for this mutation effect. |
| `lever` | string | `""` | Content-gating lever. |

### Targeting

| Field | Type | Default | Description |
|---|---|---|---|
| `filter` | string | `""` | Aspect filter — only cards with this aspect are affected. Must validate as an Element/aspect ID. |
| `mutate` | string | `""` | Target aspect to mutate (add/remove/modify). Must validate as an Element/aspect ID. |

### Level & Mode

| Field | Type | Default | Description |
|---|---|---|---|
| `level` | string | `"0"` | Level change value. Can be a number or a dynamic expression (e.g. `"mystery.edge"` resolves to the current edge mystery level). |
| `additive` | bool | `false` | If true, `level` is added to the existing value. If false, the aspect level is **set** to `level`. |

---

## Example

```json
{
  "mutations": [
    {
      "id": "mystery.increment",
      "filter": "mystery",
      "mutate": "mystery",
      "level": "1",
      "additive": true
    },
    {
      "id": "aspect.replace",
      "filter": "corrupted",
      "mutate": "corrupted",
      "level": "0",
      "additive": false
    }
  ]
}
```
