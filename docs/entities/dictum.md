# Dictum

**JSON root key:** `"dicta"` — value is an array of dictum objects.

Dictum defines global game configuration: which Unity scenes to load, the root world sphere type, default sphere paths, manifestation types, and timing parameters. There is typically a single dictum entry for the game.

---

## Fields

### Identity

| Field | Type | Default | Description |
|---|---|---|---|
| `id` | string | **required** | Unique dictum identifier. |
| `lever` | string | `""` | Content-gating lever. |

### Scene Configuration

| Field | Type | Description |
|---|---|---|
| `logoscene` | string | Unity scene name for the logo splash screen. |
| `quotesscene` | string | Unity scene name for the quotation/title screen. |
| `menuscene` | string | Unity scene name for the main menu. |
| `playfieldscene` | string | Unity scene name for the main game playfield. |
| `gameoverscene` | string | Unity scene name for the game over screen. |
| `newgamescene` | string | Unity scene name for the new game setup screen. |
| `loadingscene` | string | Unity scene name for the loading screen. |

### World & Spheres

| Field | Type | Default | Description |
|---|---|---|---|
| `worldspheretype` | string | `TabletopSphere` | Fully-qualified C# type name for the root world sphere (e.g. `"SecretHistories.Spheres.SkySphere"`). |
| `defaultworldspherepath` | string (path) | empty | Default FucinePath for the world sphere. |
| `alternativedefaultworldspherepaths` | array of paths | `[]` | Alternative default paths used as fallbacks. |
| `defaultcardback` | string | `null` | Default card back sprite key. |
| `noteelementid` | string | `null` | Element ID used for note/journal entries. Must validate as an Element. |

### Timing

| Field | Type | Default | Description |
|---|---|---|---|
| `defaultgamespeed` | int | `0` | Default game speed multiplier. |
| `maxsuitabilitypulsefrequency` | float | `0` | Maximum frequency for suitability pulse checks. |
| `suitabilitypulsespeed` | float | `0` | Speed of the suitability pulse animation. |

### Manifestation Types

| Field | Type | Default | Description |
|---|---|---|---|
| `storedmanifestation` | string | `StoredManifestation` | C# type for the stored manifestation (card-in-sphere display). |
| `storedphysicalmanifestation` | string | `StoredPhysicalManifestation` | C# type for the physical stored manifestation (item-in-room display). |

---

## Example

```json
{
  "dicta": [
    {
      "id": "dictum.bookofhours",
      "worldspheretype": "SecretHistories.Spheres.SkySphere",
      "defaultworldspherepath": "~/lastresort",
      "alternativedefaultworldspherepaths": [
        "~/portage1",
        "~/portage2",
        "~/hand.memories",
        "~/library!cucurbitbridge/outsidepile"
      ],
      "logoscene": "S1Logo",
      "quotesscene": "S2Quote",
      "menuscene": "S3MenuUmber",
      "playfieldscene": "S4Library",
      "newgamescene": "S5NewGame",
      "loadingscene": "LoadingWithTip",
      "gameoverscene": "S6GameOver",
      "defaultcardback": "cardback",
      "noteelementid": "journal",
      "defaultgamespeed": 1,
      "maxsuitabilitypulsefrequency": 2,
      "suitabilitypulsespeed": 0.5,
      "storedmanifestation": "SecretHistories.Manifestations.StoredManifestation",
      "storedphysicalmanifestation": "SecretHistories.Manifestations.StoredPhysicalManifestation"
    }
  ]
}
```
