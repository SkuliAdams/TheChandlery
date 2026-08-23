# MorphDetails

**Not a top-level entity.** A sub-entity embedded inline within element definitions.

MorphDetails defines a morphing/transformation rule triggered when an element's aspects reach a certain threshold. It can transform the element into another, add/remove aspects, change quantities, or trigger other morph effects.

---

## Where Used

| Parent Entity | Field | Role |
|---|---|---|
| [Element](element.md) | `xtriggers{ }[ ]` | Morph rules triggered by aspect thresholds. |

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Identifier for this morph effect. Validates as an Element ID. |
| `lever` | string | `""` | Content-gating lever. |

### Level & Mode

| Field | Type | Default | Description |
|---|---|---|---|
| `chance` | int | `100` | Percentage chance (0-100) of this morph triggering. |
| `level` | string | `"1"` | The morph level or target value. Can be numeric or a dynamic expression. |
| `additive` | bool | `false` | If true, level is added to the existing value. If false, the value is set. |
| `morpheffect` | string enum | `Transform` | The type of morph effect: `Transform`, `Upgrade`, `Quantity`, `Degrade`, `Downgrade`, etc. `Transform` replaces the element; `Quantity` changes the aspect level. |

---

## Example

```json
{
  "xtriggers": {
    "skillingup": [
      {
        "id": "skill.chandlery",
        "morpheffect": "Upgrade",
        "level": "1",
        "chance": 100,
        "additive": false
      }
    ]
  }
}
```
