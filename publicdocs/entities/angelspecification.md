# AngelSpecification

**Not a top-level entity.** A sub-entity embedded inline within SphereSpec.

AngelSpecification defines "angel" behaviours for a card slot. Angels are automated processes that manage card movement between slots — automatically moving valid cards from one sphere to another, acting as a sort of robotic assistant within the slot system.

---

## Where Used

| Parent Entity | Field | Role |
|---|---|---|
| [SphereSpec](spherespec.md) | `angels[ ]` | Automated card management rules for a slot. |

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Identifier for this angel specification. |
| `lever` | string | `""` | Content-gating lever. |

### Angel Behaviour

| Field | Type | Default | Description |
|---|---|---|---|
| `choir` | string | `null` | The type/choir of angel behaviour. Determines the automation logic. |
| `livein` | string | `null` | SphereSpec ID where the angel resides (which slot it lives in). Validates as a SphereSpec ID. |
| `watchover` | string | `null` | SphereSpec ID the angel watches over (which slot it manages). Validates as a SphereSpec ID. |

---

## Example

```json
{
  "angels": [
    {
      "choir": "Automation",
      "livein": "slot_storage",
      "watchover": "slot_work"
    }
  ]
}
```
