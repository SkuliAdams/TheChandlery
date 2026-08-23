# Setting

**JSON root key:** `"settings"` — value is an array of setting objects.

Settings define user-configurable options in the game's UI: graphics, audio, keybindings, and gameplay preferences. Each setting is rendered in the settings screen based on its UI type and linked to the game's config system.

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique setting identifier. Also used as the config key for reading/saving the value. |
| `lever` | string | `""` | Content-gating lever. |

### Placement

| Field | Type | Description |
|---|---|---|
| `tabid` | string | Which settings tab this setting appears in (e.g. `"UI_VISUAL"`, `"UI_AUDIO"`, `"UI_GAMEPLAY"`). |
| `targetconfigarray` | string | If set, this setting targets a specific config array rather than a single config value. |

### UI Configuration

| Field | Type | Default | Description |
|---|---|---|---|
| `hint` | string | `null` | Tooltip/hint text shown for this setting (localised). |
| `hintlocid` | string | `null` | Alternative localization key for the hint. |
| `ui` | string enum | `Default` | UI control type: `Default`, `ToggleGroup`, `Arrows`, `Dropdown`, `TextLine`. |
| `valuelabels` | dict | `{}` | Labels for each possible value, keyed by value string. |
| `valueinnerlabels` | dict | `{}` | Shorter labels shown inside the control for each value. |
| `valuenotifications` | dict | `{}` | Notification text shown when each value is selected. |

### Data Type & Range

| Field | Type | Default | Description |
|---|---|---|---|
| `datatype` | string | `null` | Data type for the value: `"Single"` (float), `"Int32"`, `"String"`, `"Boolean"`. Controls value parsing and UI. |
| `defaultvalue` | string | `null` | Default value when no saved value exists. |
| `minvalue` | int | `0` | Minimum value (for numeric types). |
| `maxvalue` | int | `1` | Maximum value (for numeric types). |

---

## Example

```json
{
  "settings": [
    {
      "id": "ScreenCanvasSizeSlider",
      "tabid": "UI_VISUAL",
      "hint": "Adjust the game canvas size",
      "minvalue": 75,
      "maxvalue": 110,
      "defaultvalue": 100,
      "datatype": "Int32"
    },
    {
      "id": "SkipConfirmDialog",
      "tabid": "UI_GAMEPLAY",
      "hint": "Skip confirmation dialogs",
      "ui": "ToggleGroup",
      "minvalue": 0,
      "maxvalue": 1,
      "defaultvalue": 0,
      "datatype": "Boolean"
    }
  ]
}
```
