# Example Mod Structure

This folder contains a sample mod demonstrating how to add custom rooms to Book of Hours.

## Structure

```
ExampleMod/
├── content/
│   └── terrain.json     # Terrain definitions
├── images/
│   └── (sprite files)   # Room sprite images
└── synopsis.json        # (Would be created by mod loader)
```

## terrain.json

The `terrain` root tag defines custom rooms:

```json
{
  "terrain": [
    {
      "id": "example_room",
      "label": "Example Room",
      "preface": "A mysterious chamber",
      "startdescription": "Something interesting happens here.",
      "desc": "Full description of the example room.",
      "posx": 800,
      "posy": 400,
      "sprite": "sprites/rooms/example_room",
      "startsopen": false,
      "startsunsealed": false,
      "connectedto": {
        "gatehouse": "1"
      }
    }
  ]
}
```

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| id | string | Yes | Unique room identifier |
| label | string | Yes | Display name |
| posx | float | Yes | X position in world |
| posy | float | Yes | Y position in world |
| width | float | No | Room width (pixels) |
| height | float | No | Room height (pixels) |
| sprite | string | No | Sprite asset path |
| preface | string | No | Prefix text |
| startdescription | string | No | Initial description |
| desc | string | No | Full description |
| startsopen | bool | No | Room starts unshrouded |
| startsunsealed | bool | No | Room starts unsealed |
| connectedto | dict | No | Map of connected room IDs |

## Notes

- Room IDs must be unique
- Position is relative to the library sphere
- Width and height set the room's size (affects collision/interaction area)
- `connectedto` links this room to other rooms by their IDs
- Sprites must be bundled with the mod in the images folder