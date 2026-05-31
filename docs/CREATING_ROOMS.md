# Creating Rooms with Chandlery

A guide for modders who want to add custom rooms with shelves, slots, workstations, and comfort areas to Book of Hours using the Chandlery modding library.

---

## Prerequisites

- Book of Hours with the **Chandlery** mod installed
- Basic familiarity with JSON
- A text editor

---

## Quick Start

The simplest room mod needs only two files:

```
YourModName/
  synopsis.json          # mod manifest
  content/
    terrain/
      rooms.json         # room definitions (file name is arbitrary)
```

---

## Mod Folder Structure

A complete mod folder looks like this:

```
YourModName/
  synopsis.json                    # required — mod manifest
  cover.png                        # optional — thumbnail shown in mod manager
  content/
    terrain/
      rooms.json                   # your room definitions
    recipes/                       # optional — custom recipes
      *.json
  images/
    terrain/
      myroom.png                   # optional — unshrouded sprite
      myroom_shrouded.png          # optional — shrouded sprite
  chandlery/
    mainmenu.png                   # optional — main menu background
    wolfdivided.json               # optional — terrain disable rules
```

Place the mod folder in `%APPDATA%\..\LocalLow\Weather Factory\Book of Hours\mods\`.

---

## synopsis.json

```json
{
  "name": "MyRooms",
  "author": "You",
  "version": "1.0.0",
  "description": "Adds custom rooms.",
  "description_long": "Adds custom rooms with shelves and workstations.",
  "tags": ["content"]
}
```

---

## Room Definitions (`rooms.json`)

A single file can contain multiple rooms. Each room is an entry in the top-level `"terrain"` array:

```json
{
  "terrain": [
    {
      "id": "myroom",
      "posx": 3000,
      "posy": 500,
      "width": 400,
      "height": 200,
      "startsopen": false,
      "startsunsealed": true,
      "contents": { ... }
    }
  ]
}
```

### Required fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (used for save/load, must not conflict with existing rooms) |
| `posx` | number | X position on the map grid |
| `posy` | number | Y position on the map grid |

### Optional fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `width` | number | `400` | Room width in world units |
| `height` | number | `200` | Room height in world units |
| `roomsize` | string | *uses width/height* | Shorthand — `"1x1"`, `"1x2"`, `"2x1"`, `"2x2"`, `"3x3"` — overrides width/height. Each block = 400x200, 20px gap. |
| `startsopen` | bool | `false` | Whether the room starts visually open |
| `startsunsealed` | bool | `true` | Whether the room starts unlocked |
| `templateid` | string | `"watchmanstower1"` | Which existing room to clone for structure/sprites |
| `connectedto` | string[] | `null` | Room IDs to unseal when this room is unlocked (chain-unlocking) |
| `contents` | object | `null` | Inner furnishings — see below |

### Sprite images

If you add images named `myroom.png` and `myroom_shrouded.png` under `images/terrain/`, Chandlery will use them as the room's unshrouded and shrouded backgrounds. If no sprites are found, the room gets a solid black placeholder.

---

## Room Contents

The `contents` block defines interactive elements inside the room. Four types are supported:

| Key | Element type | Choreographer |
|-----|-------------|---------------|
| `slots` | `PhysicalSphere` — general item drop target | `ThingChoreographer` (items arrange horizontally) |
| `workstations` | `FitmentWorkstationSphere` — crafting verb station | `FitmentChoreographer` (token fills the slot) |
| `shelves` | `ShelfSpaceSphere` — book shelf | `ShelfChoreographer` (books left/right, things centre) |
| `comforts` | `ComfortSphere` — seating/comfort area (one item at a time) | `ThingChoreographer` (items arrange horizontally) |

### Shared fields (all content types)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `id` | string | *(required)* | Unique identifier within the room (e.g., `"desk"`, `"shelf1"`) |
| `label` | string | *(empty)* | Display name shown on hover |
| `description` | string | *(empty)* | Tooltip text |
| `posx` | number | *(required)* | X position relative to the room's top-left corner, in world units |
| `posy` | number | *(required)* | Y position relative to the room's top-left corner, in world units, **increasing downward** |
| `width` | number | `120` | Width of the drop area |
| `height` | number | `120` | Height of the drop area |
| `required` | dict | `{}` | Aspect requirements — at least one must match (e.g., `{ "physical": 1 }`) |
| `essential` | dict | `{}` | All listed aspects must match |
| `forbidden` | dict | `{}` | These aspects must not be present |
| `greedy` | bool | `false` | Auto-accept matching tokens |
| `lockdrag` | bool | `false` | Prevent items from being dragged out |
| `showglowonhover` | bool | `false` | Show glow effect when hovering |
| `showinteractionglow` | bool | `false` | Show glow on interaction |

### Workstation-specific field

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `verb` | string | *(required for workstations)* | Verb ID for the crafting station (e.g., `"library.desk.natan.consider"`, `"library.altar.knot"`) |

---

## Coordinate System

Positions are in world units relative to the room's **top-left corner**:

```
(0, 0) ────────────── X increases ──►
  │
  │   (34, 180) ┌─────────────┐
  │             │   THINGS    │  (width=90, height=18)
  │             └─────────────┘
  │
  Y
  increases
  downward
  │
  ▼
```

- `posx` = distance from room's left edge
- `posy` = distance from room's top edge
- The item is positioned by its **bottom-left** corner (so a larger Y value places it lower in the room)

---

## Full Example

```json
{
  "terrain": [
    {
      "id": "my_library",
      "posx": 2850,
      "posy": 250,
      "width": 420,
      "height": 220,
      "startsopen": false,
      "startsunsealed": true,
      "contents": {
        "slots": [
          {
            "id": "odds",
            "label": "Odds and Ends",
            "posx": 15,
            "posy": 175,
            "width": 120,
            "height": 30,
            "required": { "physical": 1 }
          }
        ],
        "shelves": [
          {
            "id": "main_shelf",
            "label": "Reference Books",
            "description": "Dusty folios and crumbling manuscripts.",
            "posx": 40,
            "posy": 40,
            "width": 160,
            "height": 70
          },
          {
            "id": "overflow",
            "label": "Overflow Shelf",
            "posx": 220,
            "posy": 60,
            "width": 100,
            "height": 60,
            "required": { "readable": 1 }
          }
        ],
        "workstations": [
          {
            "id": "reading_desk",
            "label": "Reading Desk",
            "verb": "library.desk.reading",
            "posx": 270,
            "posy": 170,
            "width": 120,
            "height": 40
          }
        ],
        "comforts": [
          {
            "id": "armchair",
            "label": "Armchair",
            "description": "A worn leather armchair.",
            "posx": 155,
            "posy": 180,
            "width": 90,
            "height": 30
          }
        ]
      }
    }
  ]
}
```

---

## Common Pitfalls

- **Ids must be unique** across all rooms in all mods. Prefixed like `"myauthor_myroom"` to avoid conflicts.
- **Aspect names are case-sensitive** and match the game's element IDs (e.g., `"physical"`, `"readable"`, `"light"`, `"fabric"`).
- **Save/load**: Rooms are restored from save automatically. The `id` field is the key — if you change it mid-game, the old save data won't find the room.
- **No sprites = black placeholder**: Without a matching PNG in `images/terrain/`, the room displays as a black rectangle. This is fine for testing.
- **Shelves use `ShelfDominion`**: The mod automatically creates the shelf dominion when `shelves` are present in `contents`. No extra setup needed.
- **Delete `AUTOSAVE.json`** after changing room definitions to force a fresh environment population.

---

## Template Rooms

By default, new rooms are cloned from `watchmanstower1` (the Hush House entrance). To use a different template, set `templateid`:

```json
{
  "id": "my_cave",
  "templateid": "watchmanstower1",
  "posx": 4000,
  "posy": 800
}
```

The template provides the base room structure, sprite layers, and manifestation components. The cloned room then has its `contents` elements injected and its existing interactive children stripped.
