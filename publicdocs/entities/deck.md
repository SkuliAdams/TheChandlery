# Deck

**JSON root key:** `"decks"` — value is an array of deck objects.

Decks define collections of elements that can be drawn from. They are used for card draws, random selection, and procedural content generation. Decks can appear as standalone JSON entities or be embedded inline within a recipe as an `internaldeck`.

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique deck identifier. Referenced in recipe `deckeffects` and by other game systems. |
| `lever` | string | `""` | Content-gating lever. |

### Display

| Field | Type | Default | Description |
|---|---|---|---|
| `label` | string | `""` | Human-readable name (localised). |
| `desc` | string | `""` | Description (localised). |
| `comments` | string | `""` | Internal notes. |
| `ishidden` | bool | `false` | If true, the deck is hidden from UI listings. |

### Card Pool

| Field | Type | Default | Description |
|---|---|---|---|
| `spec` | array of element IDs | `[]` | The list of element IDs that form the deck's card pool. Elements are drawn in order from this list. |
| `defaultcard` | string | `""` | Fallback element ID to draw if the deck is exhausted. |
| `draws` | int | `1` | Number of cards drawn each time the deck is used. |
| `resetonexhaustion` | bool | `false` | If true, the deck resets to its initial spec when exhausted. |

### Visual

| Field | Type | Default | Description |
|---|---|---|---|
| `cover` | string | `"books"` | Visual cover style for the deck display. |
| `drawmessages` | dict | `{}` | Localised messages shown for specific draw results. Keys match element IDs or categories. |
| `defaultdrawmessages` | dict | `{}` | Default draw messages when no specific message is set. |

---

## Uniqueness Groups

During import, the deck registers uniqueness groups for elements in its `spec` that have a `uniquenessgroup` set. This ensures uniqueness rules are respected when drawing from the deck.

---

## Example (standalone)

```json
{
  "decks": [
    {
      "id": "d.chat.generic",
      "label": "Chance Conversation",
      "spec": [
        "precursor.mem.dreamt",
        "precursor.mem.storm",
        "chit.cat"
      ],
      "resetonexhaustion": true
    }
  ]
}
```

## Example (inline in recipe)

```json
{
  "recipes": [
    {
      "id": "dream.resonance",
      "actionid": "dream",
      "internaldeck": {
        "spec": ["memory.stray", "memory.regret", "memory.gossip"],
        "draws": 1,
        "resetonexhaustion": false
      },
      "effects": { "dream": 1 }
    }
  ]
}
```
