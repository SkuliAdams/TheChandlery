# VerbMorphDetails

**Not a top-level entity.** A sub-entity embedded inline within verb definitions.

VerbMorphDetails is identical to MorphDetails in structure but is used specifically in verb morph triggers. The difference is that its `id` field validates against Verb IDs rather than Element IDs.

---

## Where Used

| Parent Entity | Field | Role |
|---|---|---|
| [Verb](verb.md) | `xtriggers{ }[ ]` | Morph rules triggered on verbs. |

---

## Fields

All fields are identical to [MorphDetails](morphdetails.md):

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Identifier — validates as a **Verb** ID. |
| `lever` | string | `""` | Content-gating lever. |
| `chance` | int | `100` | Percentage chance (0-100). |
| `level` | string | `"1"` | Level value (numeric or dynamic expression). |
| `additive` | bool | `false` | Add vs set mode. |
| `morpheffect` | string enum | `Transform` | Morph effect type. |

---

## Example

```json
{
  "xtriggers": {
    "advancing": [
      {
        "id": "consider",
        "morpheffect": "Transform",
        "level": "1",
        "chance": 100
      }
    ]
  }
}
```
