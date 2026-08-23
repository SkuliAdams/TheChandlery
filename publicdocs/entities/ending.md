# Ending

**JSON root key:** `"endings"` — value is an array of ending objects.

Endings define the game-over/victory states for the game's Histories. Each ending is triggered by a recipe with a matching `ending` field. Endings display a concluding image, flavour text, and may unlock achievements.

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique ending identifier. Referenced by recipe `ending` fields. |
| `lever` | string | `""` | Content-gating lever. |

### Display

| Field | Type | Default | Description |
|---|---|---|---|
| `label` | string | `""` | Title text for the ending screen (localised). |
| `desc` | string | `""` | Description/conclusion text (localised). |
| `image` | string | `""` | Image sprite key displayed on the ending screen. |
| `comments` | string | `""` | Internal notes. |

### Visual & Audio

| Field | Type | Default | Description |
|---|---|---|---|
| `flavour` | string enum | `Melancholy` | Visual/audio flavour category: `None`, `Grand`, `Melancholy`, `Pale`, `Vile`, `Enigmatic`, `Positive`. |
| `anim` | string | `""` | Animation effect key (e.g. `"DramaticLight"`). |

### Achievements

| Field | Type | Description |
|---|---|---|
| `achievements` | array of achievement IDs | Achievements unlocked when this ending is reached. |

---

## Example

```json
{
  "endings": [
    {
      "id": "end.h.sky.oldl.symurgist",
      "image": "wheeloffortune",
      "label": "Symurgist Victory: Songs at Noon",
      "desc": "The road rises...",
      "flavour": "Grand",
      "achievements": [
        "A_V_HISTORY_WHEELOFFORTUNE",
        "A_V_ORIGIN_SYMURGIST",
        "A_V_NUMEN_OLDL"
      ]
    }
  ]
}
```
