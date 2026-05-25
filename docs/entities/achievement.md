# Achievement

**JSON root key:** `"achievements"` — value is an array of achievement objects.

Achievements define unlockable goals tracked by the platform's achievement system (Steam, etc.). They have locked/unlocked descriptions, icons, and can be organised into categories.

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique achievement identifier. |
| `lever` | string | `""` | Content-gating lever. |

### Display

| Field | Type | Default | Description |
|---|---|---|---|
| `label` | string | `""` | Achievement title (localised). |
| `descriptionlocked` | string | `""` | Description shown when locked (localised). |
| `descriptionunlocked` | string | `""` | Description shown when unlocked (localised). |
| `singledescription` | bool | `false` | If true, the same description is used for both locked and unlocked states. The non-empty value is copied to the other. |
| `unlockmessage` | string | `null` | Custom message shown when unlocked (localised). Falls back to `descriptionunlocked`. |
| `ishidden` | bool | `false` | If true, hidden from the player until unlocked. |
| `iscategory` | bool | `false` | If true, this entry is a category header rather than a real achievement. |
| `category` | string | `"ACH_CATEGORY_MODS"` | Category grouping ID for UI organisation. |

### Icons

| Field | Type | Default | Description |
|---|---|---|---|
| `iconunlocked` | string | `""` | Element sprite key for the unlocked icon. |
| `iconlocked` | string | `""` | Element sprite key for the locked icon. |

### Validation

| Field | Type | Default | Description |
|---|---|---|---|
| `validateonstorefront` | bool | `false` | If true, the achievement is validated against the platform's storefront (Steam) to check its official status and unlock state. |

---

## Example

```json
{
  "achievements": [
    {
      "id": "A_EVENT_BUSTMASTER",
      "label": "Much Is Lost... But We Abide",
      "descriptionlocked": "???",
      "descriptionunlocked": "I curated a perfect display of every Hush House Librarian.",
      "iconunlocked": "trophy",
      "category": "A_CATEGORY_EVENTS"
    },
    {
      "id": "ACH_CATEGORY_VISITOR",
      "label": "Visitors",
      "iscategory": true,
      "singledescription": true,
      "descriptionunlocked": "Visitors who have made their mark on Hush House."
    }
  ]
}
```
