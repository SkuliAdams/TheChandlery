# Lever

**JSON root key:** `"levers"` — value is an array of lever objects.

Levers track player choices and achievements across playthroughs, enabling content gating and legacy transition logic. A lever stores a record of a significant decision or outcome, weighted by aspects, which influences which legacies are available in future games.

Levers have no standalone JSON files in the core game; they are defined in code. Modders can define new levers via JSON if needed.

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique lever identifier. |
| `lever` | string | `""` | (Inherited from base class; not typically used on Lever entities themselves.) |

### Storage

| Field | Type | Description |
|---|---|---|
| `recordkey` | string | Key used to store the recorded value in the player's future legacy event record. Defaults to the lever's `id` if not set. |
| `defaultvalue` | string | Default value if no record exists. |

### Scoring

| Field | Type | Description |
|---|---|---|
| `weights` | dict of `aspectId → level` | Aspect weights used for scoring. Each aspect contributes to the lever's score calculation. |
| `requiredscore` | int | Minimum score required for the lever to be considered "active." |

### Redirection

| Field | Type | Description |
|---|---|---|
| `redirects` | dict of `elementId → elementId` | Maps recorded element IDs to alternate element IDs. Used when a specific recorded element should be counted as a different element for legacy purposes. |

### Behaviour

| Field | Type | Default | Description |
|---|---|---|---|
| `ongameend` | bool | `false` | If true, this lever is recorded when the game ends (rather than during gameplay). |
| `comments` | string | `null` | Internal notes. |

---

## How Levers Work

1. During gameplay, when a player makes a significant choice or achieves something, a lever records the choice via `RecordForFutureCharacter()`.
2. The lever stores a `recordkey` → `elementId` mapping in the player's legacy record.
3. On a future playthrough, legacy availability checks the recorded values against the lever's `weights` and `requiredscore`.
4. `redirects` allows mapping specific recorded elements to different elements for scoring purposes.

---

## Example

```json
{
  "levers": [
    {
      "id": "en.chirurgy",
      "recordkey": "en.chirurgy",
      "weights": {
        "chirurgic": 1
      },
      "requiredscore": 1,
      "redirects": {
        "physician": "chirurgic",
        "surgeon": "chirurgic"
      }
    }
  ]
}
```
