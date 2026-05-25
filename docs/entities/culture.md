# Culture

**JSON root key:** `"cultures"` — value is an array of culture objects.

Cultures define language/localisation configurations. Each culture corresponds to a supported language and specifies its endonym, exonym, font script, and UI label overrides. Culture JSON files live in subdirectories per language (e.g. `cultures/en/`, `cultures/jp/`).

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Culture code (e.g. `"en"`, `"jp"`, `"ru"`, `"zh-hans"`). |
| `lever` | string | `""` | Content-gating lever. |

### Names

| Field | Type | Description |
|---|---|---|---|
| `endonym` | string | The language's name in its own writing system (e.g. `"English"`, `"日本語"`). |
| `exonym` | string | The language's name in English (e.g. `"English"`, `"Japanese"`). |

### Typography

| Field | Type | Default | Description |
|---|---|---|---|
| `fontscript` | string | `"x"` | Font script identifier for rendering (e.g. `"latin"`, `"jp"`, `"ru"`). Determines which font to use. |
| `boldallowed` | bool | `false` | If true, bold text rendering is supported for this culture. |

### Visibility

| Field | Type | Default | Description |
|---|---|---|---|
| `released` | bool | `false` | If true, this culture/localisation has been released and is available to players. |

### UI Labels

| Field | Type | Description |
|---|---|---|
| `uilabels` | dict | Localised UI label overrides. Keys are label IDs; values are the translated text (may include formatting tags). Used for UI elements whose text must adapt to each language. |

---

## Example

```json
{
  "cultures": [
    {
      "id": "en",
      "endonym": "English",
      "exonym": "English",
      "fontscript": "latin",
      "uilabels": {
        "UI_PAUSE": "Pause\n<size=12px><smallcaps>[{SETTING:kbpause}]</smallcaps></size>",
        "UI_MUTE": "Mute",
        "UI_ELEMENTS_DROPPED_OFFSCREEN": "{CULTURE_ARTICLE} element{CULTURE_PLURAL} were dropped offscreen."
      }
    },
    {
      "id": "jp",
      "endonym": "日本語",
      "exonym": "Japanese",
      "fontscript": "jp",
      "boldallowed": false,
      "released": true,
      "uilabels": {
        "UI_PAUSE": "一時停止\n<size=12px><smallcaps>[{SETTING:kbpause}]</smallcaps></size>"
      }
    }
  ]
}
```
