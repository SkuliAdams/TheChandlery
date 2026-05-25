# Room Processing in Book of Hours

Analysis of how the game handles rooms (terrain features) — from initialization through unlocking, content population, and save/load.

## Room Data Model

Rooms are `TerrainFeature` objects (`SecretHistories.Tokens.Payloads\TerrainFeature.cs`) — Unity MonoBehaviours placed in the scene with three key boolean states:

| Property | Meaning |
|---|---|
| `IsShrouded` | Room is unexplored/dark (the fog-of-war state) |
| `IsSealed` | Room is locked and cannot be interacted with |
| `IsOpen` | Room is visually open (used for connected room traversal) |

Each room has a **spec seed** (`TerrainFeatureSpecSeed`) set in the Unity editor with two booleans: `StartsOpen` and `StartsUnsealed`.

## Room Initialization (New Game Flow)

The flow for a fresh game:

1. **`GameGateway.PopulateEnvironment()`** (`SecretHistories.Infrastructure\GameGateway.cs:96`) finds all `IEdenable` components (which includes `TerrainFeature`) and calls `EdenSetup()` on each.

2. **`TerrainFeature.EdenSetup()`** (`TerrainFeature.cs:111`) applies the spec seed:
   - If `StartsUnsealed` → `Unseal()`, else `Seal()`
   - If `StartsOpen` → `Unshroud(instant: true)`, else `Shroud(instant: true)`

3. **`TerrainFeature.SetUpAsTokenWithId()`** (`TerrainFeature.cs:104`) is called during token setup — it defaults `IsShrouded = true` and populates the room's info recipe (lookup: `"terrain." + Id`).

## The Unshrouding Chain (Room Unlocking)

When a room is **unshrouded** (either initially or unlocked during play):

```
Unshroud()
  ├── Unseal()                       // mark as not locked
  ├── EnactSeedsInRoom()             // spawn contents
  │     └── Find ILazyEdenable children → EdenSetup() on each
  ├── EnactLateChamberlainInRoom()   // apply late chamberlain effects to dominions
  └── Notify UI + manifestation
```

### ConnectedTerrain (`ConnectedTerrain.cs:23`)
This subclass overrides `Unshroud()` to **unseal all `connectedRooms`** before calling `base.Unshroud()`. This is the mechanic where unlocking one room (e.g., the Lodge) unseals its neighbors (e.g., the Entrance Hall), making them available for unlocking.

## Room Contents: Seeds (ILazyEdenable)

When `EnactSeedsInRoom()` runs, it finds `ILazyEdenable` children and calls `EdenSetup()`. There are **five types of seeds**:

| Seed Type | File | What it does |
|---|---|---|
| **`AbstractSeed`** | `SecretHistories.Tokens.Seeds\AbstractSeed.cs` | Spawns a single element token (item) in its parent sphere at its local position. Disables itself after seeding. |
| **`BookSeed`** | (same pattern) | Spawns a book element. |
| **`QFSeed`** | `SecretHistories.Tokens\QFSeed.cs` | Conditional spawn — checks a legacy event record, and if it matches, spawns a specific element. Also runs optional "suitabiliser catalyst" (aspect trigger). |
| **`FXSeed`** | `SecretHistories.Tokens\FXSeed.cs` | Broadcasts `EnviroFxCommand` (e.g., plays sound effects, particle effects, etc.) |
| **`FitmentWorkstationSphere`** | `SecretHistories.Spheres\FitmentWorkstationSphere.cs` | Creates a **workstation verb** token (e.g., a workbench, desk, or crafting station). The `seedWithVerbId` field specifies the verb to spawn. |

Seeds track whether they've already been enacted via `_laterEdenId` and `EdensEnacted` — on subsequent unshrouds (e.g., saving/reloading), they only enact if they haven't been before.

## Dominions & Spheres (Room Structure)

Each `TerrainFeature` has **`Dominions`** — groups of spheres that define where items, tokens, and workstations sit:

- `PopulateDominionCommand` (`SecretHistories.Commands.SituationCommands\PopulateDominionCommand.cs`) creates/restores spheres within a dominion.
- `SphereCreationCommand` (`SecretHistories.Commands\SphereCreationCommand.cs`) creates individual spheres with optional tokens and shrouding state.

This is how rooms contain multiple slots (e.g., shelves, tables, display areas) — each is a sphere within a dominion on the terrain feature.

## Room Unlock Mechanic (Player Interaction)

The player unlocks rooms via the `TerrainDetailWindow` (`SecretHistories.UI\TerrainDetailWindow.cs`):

1. Player drags a card onto a sealed/shrouded connected terrain → `ConnectedTerrain.CanInteractWith()` checks if sealed, if already unlocking, and runs the `preSlots` match.
2. `InteractWithIncoming()` shows the `TerrainDetailWindow` with the room's info recipe (preface, start description, and required aspects from `preslots[0]`).
3. Player places the required offering → `TryOpenTerrain()` creates a **verb situation** with `actionId: "terrain.unlock"` and the room's recipe.
4. When the unlock recipe completes, its effects (e.g., `FX` commands) fire, which triggers `TerrainFeature.ReceiveFxBroadcast()` → the `"open"` effect calls `Unshroud()`.

## Room Definitions (Content Data)

Each room is defined as a **Recipe** entity (NOT a separate entity type):
- JSON in `recipes/terrain.json` with `id: "terrain.<roomId>"`
- Has `actionid: "terrain.unlock"`, `warmup` (duration), `preslots` (unlock requirements), `fx` (effects on unlock)
- Discovered at runtime: `TerrainFeature.DetermineInfoRecipeId()` → `"terrain." + Id`
- The `Prefaces`, `StartDescription`, `Desc`, `Label` all come from this recipe via `GetInfoRecipe()`

## WisdomNodeTerrain (Wisdoms Tree Special Case)

`WisdomNodeTerrain` (`SecretHistories.Tokens.Payloads\WisdomNodeTerrain.cs`) extends `ConnectedTerrain` and adds an **input/output/commitment sphere system** for the Wisdoms skill tree — where you commit elements to unlock aspects. It bypasses the normal info recipe system and uses its own `InitialLabel`/`InitialDescription`.

## Save/Load (Encausting)

Room state is saved via the **Encausting** system:
- `TerrainFeature` has `[IsEncaustableClass(typeof(PopulateTerrainFeatureCommand))]` — the command serializes `IsShrouded`, `IsSealed`, `HasPreviouslyUnshrouded`, `EdensEnacted`, `Mutations`, `Dominions`.
- On load, `PopulateTerrainFeatureCommand.Execute()` finds the token by ID, restores shrouded/sealed state, mutations, and dominions.

## Room Size (RectTransform)

Room size is not determined by code or content JSON — it comes from the **Unity prefab** placed in the scene:

1. Each terrain token is a pre-placed `GameObject` with a `RectTransform` whose `sizeDelta` (e.g. `400×200` for single rooms, `820×200` for double-wide) is set in the **Unity Editor** on that prefab/scene object
2. `Token.UpdateRectTransformSizeFromManifestation()` (`Token.cs:1177`) copies `sizeDelta` from the manifestation's `RectTransform` (a child of the token) to the token's `RectTransform`
3. `WallChoreographer` (`WallChoreographer.cs:138,152,167,181`) reads `roomRT.sizeDelta` to compute wall boundaries for element placement
4. The terrain JSON (`terrain.json`) only has `posx`/`posy` for position — **no width/height/size fields exist**
5. `PopulateTerrainFeatureCommand` does not touch sizeDelta

For custom rooms, set `RectTransform.sizeDelta` on the new terrain token to the desired dimensions.

## Summary: The Complete Room Lifecycle

```
1. Placed in Unity scene as TerrainFeature/ConnectedTerrain/WisdomNodeTerrain
2. TerrainFeatureSpecSeed sets StartsOpen, StartsUnsealed
3. On game start: EdenSetup() applies spec seed → Shrouded/Sealed by default
4. Player interaction: Drag card to sealed room → TerrainDetailWindow → "terrain.unlock" verb
5. Recipe completes → FX "open" effect → ReceiveFxBroadcast → Unshroud()
6. Unshroud: Unseal + EnactSeedsInRoom (spawn items, workstations, FX)
7. ConnectedTerrain also unseals all connectedRooms (chained unlocking)
8. Content persists via Encausting save/load
```
