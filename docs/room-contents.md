# Room Contents: Everything That Can Be Inside a Room

> **Superseded for implementation planning.** The active implementation plan is `plans/from-scratch-room-creation.md`. Sections 8a–8g below are archived as the original design proposal — the plan document overrides them (see "Known Design Decisions" in the plan for delta with this document).

A room (`TerrainFeature`) in Book of Hours can contain interactive elements organised in a three-level hierarchy:

```
TerrainFeature (room GameObject)
  ├── AbstractDominion (groups of spheres; 8 types)
  ├── Sphere (interactive furniture/slots on child GameObjects; 6 room-relevant types)
  ├── ILazyEdenable Seeds (spawned on first unshroud; 5 types)
  ├── PermanentSphereSpec (defines what a slot accepts)
  └── Manifestation (visual component for each sphere type)
```

---

## 1. Sphere Types — Interactive Furniture & Slots

Each sphere is a `MonoBehaviour` component on a child GameObject of the room.

### 1a. Workstations — Crafting Verbs

| Sphere | What It Does | Examples |
|---|---|---|
| `FitmentWorkstationSphere` | Spawns a verb token (crafting station). `seedWithVerbId` specifies which verb. Implements `ILazyEdenable`. Extends `PhysicalSphere` (slot glow, hover effects). Manifestation uses VFab (3D prefab) visuals. | **All interior Hush House furniture:** Desks (Ambrose, Eva, Natan, Nonna, Pale, Reading Room, Vagabond), Altars (Ascite, Calicite, Catacombs, Chancel, Glorious, Knot, Malachite, Solar, Tentreto), Workbenches (Gallery, Governor, Gullscry, Hermit), Instruments (Bells, Double Bass, Drum, Harp, Organ, Piano), Beds (adept, guest, infirmary, librarian, lodge, long, motley, pale, servants, severn, solomon, violet), Foundry, Loom, Telescope, Nocturnary, Condignator, Dispensary, Phonographs, Projector, Glassware, Cage, Barber Chair, Chrysalis, Mirrors, Oubliette, Sarcophagus, Sacred Spring, Necropsy Table, Practice Dummy, Exercise Yard, Deeplight Corals, Garden Plots, Great Clock, Prospects, Fireplaces |
| `BuildingWorkstationSphere` | Spawns a verb token via a sibling `BuildingWorkstationSeed` component. Extends `Sphere` directly (no PhysicalSphere features). Manifestation uses 2D sprite visuals. | **Exterior Brancrug Village buildings:** `village.rectory`, `village.smithy`, `village.killes`, `village.sweetbones`, `village.postoffice` (both `*.closed` and `*.open` variants) |

### 1b. Slots & Surfaces

| Sphere | What It Does |
|---|---|
| `PhysicalSphere` | General-purpose interactive slot for physical objects. `SphereCategory = World`. |
| `ShelfSpaceSphere` | Book shelf slot. Accepts readable aspects. `WillMixBooksAndThings()`. `SphereCategory = World`. Default label: `UI_BOOKSHELF_LABEL`. |
| `TabletopSphere` | Free-form surface. Allows drag, stack merge. Items placed freely. Uses `TabletopChoreographer`. |
| `PortageSphere` | Transfer slot. Sends intangible tokens to linked destinations. |

### 1c. Workstation Recipe Slots (inside verb situations, not room-level)

| Sphere | Where / Role |
|---|---|
| `ThresholdSphere` | Recipe input slot inside a verb — soul/skill/memory. Required/Essential/Forbidden aspects. |
| `AureateThresholdSphere` | BH-styled version of ThresholdSphere. |
| `SimpleThresholdSphere` | Simplified threshold slot variant. |
| `DrydockSphere` | Temporary holding area inside a situation. Persists between scenes. |

---

## 2. Seeds — Things Spawned on First Unshroud

When `TerrainFeature.EnactSeedsInRoom()` runs (called on first unshroud from `Unshroud()`), it finds all `ILazyEdenable` children and calls `EdenSetup()`.

| Seed Type | Interface | When Seeded | What It Spawns | Purpose |
|---|---|---|---|---|---|
| `AbstractSeed` | ILazyEdenable | On room unshroud | A single element token at its local position | Initial items (books, tools, keys, ingredients). Disables itself after seeding. |
| `BookSeed` | ILazyEdenable | On room unshroud | A book element | Specific books placed on shelves. |
| `QFSeed` | ILazyEdenable | On room unshroud | An element, conditionally (checks legacy event + aspect catalyst) | Quest items, event-triggered spawns. |
| `FXSeed` | ILazyEdenable | On room unshroud | Audio/visual effect via `EnviroFxCommand` | Ambient sounds, particle effects, lighting. |
| `FitmentWorkstationSphere` | ILazyEdenable | On room unshroud | A verb token | Workstation spawning — self-seeds via `EnactSeed()`. |
| `BuildingWorkstationSeed` | IEdenable | At game start | A verb token | Village workstation spawning — sibling component on BuildingWorkstationSphere's parent. Named `"SeedBW_" + verbId`. |

ILazyEdenable seeds track enactment via `_laterEdenId` / `EdensEnacted` — on save/load they only spawn if not already done. IEdenable seeds run once at `EdenSetup()` during `GameGateway.PopulateEnvironment()`.

---

## 3. Dominion Types — Structural Grouping

| Dominion | Role | Can Create Spheres? |
|---|---|---|
| `WorldDominion` | Main room furniture group. Finds pre-existing spheres by ID. | No |
| `ShelfDominion` | Bookshelf grouping. Finds pre-existing shelf spheres. | No |
| `AnnexDominion` | Secondary room area. Finds pre-existing spheres. | No |
| `MinimalDominion` | Generic group. Can create new spheres via PrefabFactory. | Yes |
| `WindowDominion` | Popup/window content. | Yes |
| `OtherworldDominion` | Egress/otherworld content. | Yes |
| `SituationDominion` | Workstation verb internal structure (VerbThresholds, RecipeThresholds, Notes, Storage, Output, Portage). | Yes |
| `DealersTable` | Global draw piles and card decks (not room-specific). | N/A |

---

## 4. Backing Data Components

Each sphere GameObject typically carries:

| Component | What It Defines |
|---|---|
| `PermanentSphereSpec` | Sphere ID, Required / Essential / Forbidden aspects (as `AspectSpec[]`), Label, Description, AvailableFromHouse, Greedy, SphereType, AllowAnyToken, Angels |

SphereSpec (the data backing `PermanentSphereSpec`) is an inline sub-entity used in Elements, Verbs, and Recipes. Its fields:

| Field | Type | Description |
|---|---|---|
| `id` | string | Unique slot identifier |
| `label` | string | Display label |
| `description` | string | Tooltip text |
| `availablefromhouse` | string | Source house label |
| `actionid` | string | Verb ID or `*` wildcard this slot responds to |
| `greedy` | bool | Auto-fill with matching card |
| `frompath` | FucinePath | Auto-pull path |
| `enroutespherepath` | FucinePath | Animation path |
| `windowsspherepath` | FucinePath | View window path |
| `required` | dict | Aspect match requirements (at least one must match) |
| `essential` | dict | Aspect gating (all must match) |
| `forbidden` | dict | Disallowed aspects |
| `ifaspectspresent` | dict | Conditional requirements |
| `angels` | array | Automated card movement rules |

---

## 5. Room Characteristics (TerrainFeature state properties)

| Property | Meaning |
|---|---|
| `IsShrouded` | Fog-of-war (unexplored) |
| `IsSealed` | Locked, requires unlocking |
| `IsOpen` | Visually open for traversal |
| `StartsOpen` / `StartsUnsealed` | Spec seed booleans (prefab or injected) |
| `UnlockAnchor` | Transform where unlock offering appears |
| Info Recipe (`"terrain.<roomId>"`) | Recipe entity with `actionid: "terrain.unlock"` defining unlock requirements, warmup, and FX |

---

## 6. Visual Manifestations

| Manifestation | Applied To | Role |
|---|---|---|---|
| `RoomManifestation` | The room `TerrainFeature` | Unshrouded/Shrouded/Sealed sprite layers, glow, CanvasGroupFader |
| `FitmentWorkstationManifestation` | `FitmentWorkstationSphere` children | VFab-based (3D animated prefab) workstation visuals. ART_SCALING_FACTOR = 0.25. |
| `BuildingWorkstationManifestation` | `BuildingWorkstationSphere` children | Sprite-based (2D) workstation icon. ART_SCALING_FACTOR = 1. |
| `MinimalManifestation` | `PhysicalSphere` children | Basic slot visuals |
| `CardManifestation` | `SimpleThresholdSphere` children | Card-sized slot visuals |

---

## 7. Vanilla Furniture Categories (by verb ID pattern)

| Category | Verb ID Pattern | Sphere Type |
|---|---|---|
| Altars / Shrines | `library.altar.*` | FitmentWorkstationSphere |
| Desks | `library.desk.*` | FitmentWorkstationSphere |
| Workbenches | `library.workbench.*` | FitmentWorkstationSphere |
| Instruments | `library.instrument.*` | FitmentWorkstationSphere |
| Beds | `library.bed.*` | FitmentWorkstationSphere |
| Fireplaces | `library.fireplace.*` | FitmentWorkstationSphere |
| Unique interior | `library.cage`, `.condignator`, `.dispensary`, `.foundry`, `.loom`, `.mirrors`, `.nocturnary`, `.oubliette`, `.telescope`, `.spring`, `.sarcophagus.*`, `.chrysalis.*`, `.phonograph.*`, `.projector.*`, `.glassware.*`, `.duelhall`, `.practic.*`, `.rowenarium.*`, `.table.necropsy`, `.yard.exercise`, `.corals.*`, `.chair.barber`, `.clock.great`, `.prospect.*` | FitmentWorkstationSphere |
| Garden plots | `garden.plot.*` | FitmentWorkstationSphere |
| Bookshelves | (no verb — native sphere child) | ShelfSpaceSphere in ShelfDominion |
| Village buildings | `village.*` (rectory, smithy, killes, sweetbones, postoffice) | **BuildingWorkstationSphere** |

---

---

## 8. Proposed Phase 4 — `BuildFromDefinition`

The current JSON schema (`CustomTerrainDefinition`) defines only position, size, and sprites. The `TerrainFactory.Create()` method clones a template room then **strips all interactive children** (`ILazyEdenable`, `AbstractDominion`), leaving a bare room. Phase 4 extends the JSON schema so `BuildFromDefinition` can construct spheres and seeds from data rather than cloning.

Split into 12 incremental, independently implementable and testable chunks.

### 8a. JSON reference schema

```jsonc
{
  "id": "thechandlery",
  "posx": 3030,
  "posy": 460,
  "startsopen": false,
  "startsunsealed": true,
  "contents": {
    // Slot-level PermanentSphereSpec properties
    "aspectDefaults": {
      "required": { "light": 1 },
      "forbidden": { "readable": 1 }
    },
    // Array of PhysicalSphere definitions
    "slots": [
      {
        "id": "things",
        "label": "THINGS",
        "position": { "x": 0, "y": 0 },
        "required": { "physical": 1 },
        "greedy": true
      }
    ],
    // Array of ComfortSphere definitions
    "comforts": [
      {
        "id": "comfort",
        "label": "COMFORT",
        "position": { "x": 100, "y": 0 }
      }
    ],
    // Array of ShelfSpaceSphere definitions
    "shelves": [
      {
        "id": "shelf1",
        "label": "Shelf",
        "description": "A dusty bookshelf.",
        "position": { "x": -100, "y": 0 },
        "required": { "readable": 1 },
        "seeds": [
          { "type": "book", "elementId": "t.thegeminiadii", "position": { "x": 0, "y": 0 } },
          { "type": "item", "elementId": "ink.erudition", "position": { "x": 60, "y": 0 } }
        ]
      }
    ],
    // Array of FitmentWorkstationSphere definitions
    "workstations": [
      {
        "id": "desk",
        "verb": "library.desk.reading",
        "label": "Reading Desk",
        "position": { "x": 200, "y": 50 }
      }
    ]
  }
}
```

### 8b. Common fields (all sphere types)

| Field | Type | Default | Maps to |
|---|---|---|---|
| `id` | string | (required) | `PermanentSphereSpec.ApplyId`, sphere `Id` |
| `label` | string | `"things"` | `PermanentSphereSpec.Title` → also tries `pspherespec.<Title>` pseudo-element lookup |
| `description` | string | auto from pseudo-element or empty | `PermanentSphereSpec.Description` |
| `position` | `{ x, y }` | `{0,0}` | child GameObject's local `RectTransform.anchoredPosition` |
| `required` | dict | `{}` | `PermanentSphereSpec.Required` — at least one must match |
| `essential` | dict | `{}` | `PermanentSphereSpec.Essential` — all must match |
| `forbidden` | dict | `{}` | `PermanentSphereSpec.Forbidden` — none may match |
| `availableFromHouse` | string | `null` | `PermanentSphereSpec.AvailableFromHouse` |
| `greedy` | bool | `false` | `SphereSpec.Greedy` — auto-accept |
| `lockDrag` | bool | `false` | `PhysicalSphere.LockDrag` (inverted to `AllowDrag`) |

### 8c. Sphere-specific fields

| Sphere | Additional fields |
|---|---|
| `PhysicalSphere` | `lockDrag`, `showGlowOnHover`, `showInteractionGlow`, `seeds` (array) |
| `ComfortSphere` | `lockDrag`, `showGlowOnHover`, `showInteractionGlow`, `seeds` (array) |
| `ShelfSpaceSphere` | `seeds` (array), `plaqueDescription`, niche `label` defaults to `UI_BOOKSHELF_LABEL` loc string |
| `FitmentWorkstationSphere` | **`verb`** (string, required) — the `seedWithVerbId` for the workstation verb token; position only (no aspect filters, no seeds — it self-seeds) |

### 8d. Seed types (for `seeds[]` on slots, shelves, and comforts)

| `type` | C# Component | Parent Restriction | What It Spawns | Additional Fields |
|---|---|---|---|---|
| `"item"` | `ThingSeed` | `PhysicalSphere` or `ShelfSpaceSphere` only | 1× Element stack (tools, keys, ingredients). Object name `"SeedT_" + id`. Scale 0.25. | `elementId` (string, required) |
| `"wallart"` | `WallArtSeed` | `PhysicalSphere` only | 1× Element stack (paintings, tapestries). Object name `"SeedWA_" + id`. Scale 0.25. | `elementId` (string, required) |
| `"comfort"` | `ComfortSeed` | `ComfortSphere` only | 1× Element stack (rugs, standing items). Object name `"SeedC_" + id`. Scale 0.25. | `elementId` (string, required) |
| `"book"` | `BookSeed` | Any `Sphere` (typically `ShelfSpaceSphere`) | 1× Element stack, calls `.Understate()` on the token to fade it in. Object name `"SeedT_" + id`. | `elementId` (string, required) |
| `"quest"` | `QFSeed` | Any `Sphere` | 1× Element from past legacy event by `qfRecord` key, optional aspect catalyst applied after spawn | `record` (string, required), `catalyst` (string, optional), `catalystLevel` (int, optional, default 1) |
| `"fx"` | `FXSeed` | None (global broadcast) | Environmental FX broadcast (`EnviroFxCommand`) — no token spawned. Placed on room root or any child. | `effects` (array of `{ key, value }`) |

**Common seed fields** (all types):

| Field | Type | Default | Maps to |
|---|---|---|---|
| `position` | `{ x, y }` | `{0, 0}` | Seed GameObject's local `RectTransform.anchoredPosition` within the parent sphere |
| `laterEdenId` | string | `null` | `_laterEdenId` — if set, seed only fires on first unshroud. If null, fires every unshroud. |

**Example seed configurations:**

```jsonc
// "item" seed inside a shelf
{ "type": "item", "elementId": "t.thegeminiadii", "position": { "x": 0, "y": 0 } }

// "book" seed inside a shelf
{ "type": "book", "elementId": "book.degrassi.suspiria", "position": { "x": 60, "y": 0 } }

// "quest" seed with catalyst
{ "type": "quest", "record": "perspicacity", "catalyst": "rose", "catalystLevel": 5, "position": { "x": 30, "y": 0 } }

// "fx" seed on the room root
{ "type": "fx", "effects": [ { "key": "enviro_atmos", "value": "dripping" } ] }

// "wallart" seed inside a PhysicalSphere
{ "type": "wallart", "elementId": "painting.knowledge", "position": { "x": 0, "y": 0 } }
```

**Note:** `FitmentWorkstationSphere` does NOT use child seeds — it implements `ILazyEdenable` itself and self-seeds a verb token via `EnactSeed()` → `TokenCreationCommand.WithUnstartedVerb(verbId)`.

### 8e. Implementation plan — 12 incremental phases

```
Dependency graph:

4.1 Data Model ──┬── 4.2 Slots ──┬── 4.6 Item Seeds
                  │               ├── 4.8 WallArt Seeds
                  ├── 4.3 Workstations
                  ├── 4.4 Shelves ─── 4.7 Book Seeds
                  ├── 4.5 Comforts ── 4.9 Comfort Seeds
                  ├── 4.10 Quest Seeds
                  ├── 4.11 FX Seeds
                  └── 4.12 Save/Load
```

Sphere phases (4.2–4.5) are mutually independent. Seed phases depend on at least one compatible sphere type existing.

---

#### 4.1 — Data Model + `BuildFromDefinition` Hook

**What**: New file `RoomContentsDefinition.cs` with the full nested data hierarchy using `[FucineSubEntity]`. Add `Contents` property to `CustomTerrainDefinition`. Add `BuildFromDefinition()` stub to `TerrainFactory` called after `StripInteractiveChildren` when `def.Contents != null`.

**Key classes**:
```
RoomContentsDefinition       ← [FucineSubEntity]
  ├── SlotDefinition[]       ← PhysicalSphere defs
  │     └── SeedDefinition[]
  ├── WorkstationDefinition[]← FitmentWorkstationSphere defs
  ├── ShelfDefinition[]      ← ShelfSpaceSphere defs
  │     └── SeedDefinition[]
  ├── ComfortDefinition[]    ← ComfortSphere defs
  │     └── SeedDefinition[]
  └── AspectDefaults
```

**Files touched**: New file, `CustomTerrainDefinition.cs` (add property), `TerrainFactory.cs` (add stub call).

**Verification**: JSON with `"contents": {}` deserializes; rooms still create as bare shells.

---

#### 4.2 — Slots (PhysicalSphere)

**What**: For each `SlotDefinition` in `contents.slots[]`:
1. `new GameObject("slot_" + id)` child of room clone
2. Add `RectTransform`, set `anchoredPosition` from JSON
3. Add `PhysicalSphere` + `PermanentSphereSpec` + `MinimalManifestation`
4. `WorldDominion` auto-discovers `PermanentSphereSpec` children via `RegisterFor()`

**Files touched**: `TerrainFactory.cs`.

**Verification**: Slots visible in-game, accept dragged items, respect aspect filters.

---

#### 4.3 — Workstations (FitmentWorkstationSphere)

**What**: For each `WorkstationDefinition` in `contents.workstations[]`:
1. `new GameObject("workstation_" + id)` child of room clone
2. Add `RectTransform`, position from JSON
3. Add `FitmentWorkstationSphere`, set `seedWithVerbId` from `verb`
4. No `PermanentSphereSpec` (workstations aren't drop targets)

**Files touched**: `TerrainFactory.cs`.

**Verification**: Verb token spawns on first unshroud; workstation functions as a crafting station.

---

#### 4.4 — Shelves (ShelfSpaceSphere + ShelfDominion)

**What**: For each `ShelfDefinition` in `contents.shelves[]`:
1. `new GameObject("shelf_" + id)` child of room clone
2. Add `RectTransform` + `ShelfSpaceSphere` + `PermanentSphereSpec`
3. Create child `GameObject("shelf_dominion")` with `ShelfDominion` component
4. `ShelfDominion` discovers `ShelfSpaceSphere` children automatically

**No `ShelfChoreographer`** — functional drop targets only.

**Files touched**: `TerrainFactory.cs`.

**Verification**: Shelves accept readable tokens, reject non-readable via `WillMixBooksAndThings()`.

---

#### 4.5 — Comforts (ComfortSphere)

**What**: For each `ComfortDefinition` in `contents.comforts[]`:
1. `new GameObject("comfort_" + id)` child of room clone
2. Add `RectTransform` + `ComfortSphere` + `PermanentSphereSpec` + `MinimalManifestation`
3. `ComfortSphere` natively enforces single-token constraint

**Files touched**: `TerrainFactory.cs`.

**Verification**: Comfort sphere accepts exactly one token, rejects additional tokens.

---

#### 4.6 — Item Seeds (ThingSeed)

**What**: For `seeds[]` entries with `"type": "item"` on `SlotDefinition` or `ShelfDefinition` parents:
1. `new GameObject("SeedT_" + elementId)` child of the parent sphere
2. Add `ThingSeed`, set `seedWithElementId`
3. Set local `RectTransform.anchoredPosition` from seed JSON
4. `EnactSeedsInRoom()` → `ILazyEdenable.EdenSetup()` triggers spawn

**Files touched**: `TerrainFactory.cs`.

**Verification**: Item element tokens appear at correct positions on first unshroud.

---

#### 4.7 — Book Seeds (BookSeed)

**What**: For `seeds[]` entries with `"type": "book"`:
1. `new GameObject("SeedT_" + elementId)` child of parent sphere
2. Add `BookSeed`, set `seedWithElementId`
3. `BookSeed.EdenSetup()` calls `.Understate()` for fade-in

**Files touched**: `TerrainFactory.cs`.

**Verification**: Books appear on shelves/slots on unshroud, fade in via Understate.

---

#### 4.8 — WallArt Seeds (WallArtSeed)

**What**: For `seeds[]` with `"type": "wallart"` on `PhysicalSphere` parents:
1. `new GameObject("SeedWA_" + elementId)` child of sphere
2. Add `WallArtSeed`, set `seedWithElementId`
3. `WallArtSeed.IsAcceptableContainerForSeed()` checks parent is `PhysicalSphere`

**Files touched**: `TerrainFactory.cs`.

**Verification**: Paintings/tapestries spawn on slot unshroud.

---

#### 4.9 — Comfort Seeds (ComfortSeed)

**What**: For `seeds[]` with `"type": "comfort"` on `ComfortSphere` parents:
1. `new GameObject("SeedC_" + elementId)` child of the `ComfortSphere`
2. Add `ComfortSeed`, set `seedWithElementId`

**Files touched**: `TerrainFactory.cs`.

**Verification**: Rugs/standing items appear on comfort sphere on unshroud.

---

#### 4.10 — Quest Seeds (QFSeed)

**What**: For `seeds[]` with `"type": "quest"`:
1. `new GameObject("SeedQF_" + id)` child of parent sphere
2. Add `QFSeed`, set `qfRecord`, `qfCatalyst`, `qfCatalystLevel`
3. `QFSeed.EnactSeed()` checks past legacy event record before spawning

**Files touched**: `TerrainFactory.cs`.

**Verification**: Element spawns conditionally based on legacy event match.

---

#### 4.11 — FX Seeds (FXSeed)

**What**: For `seeds[]` with `"type": "fx"`:
1. `new GameObject("SeedFX_" + id)` child of room root (no sphere parent needed)
2. Add `FXSeed`, set `effects` array (key-value `FXSpec` pairs)
3. `FXSeed.EdenSetup()` broadcasts `EnviroFxCommand` for each effect

**Files touched**: `TerrainFactory.cs`.

**Verification**: Ambient sounds/particles/lighting play on room unshroud.

---

#### 4.12 — Save/Load Durability

**What**: End-to-end persistence:
1. Add `BuildFromDefinition` call to `CreateForLoad()` path (currently only `Create()` has it)
2. Verify `PopulateTerrainFeatureCommand` correctly restores custom spheres and dominions
3. Test: create room → spawn seeds → save → quit → reload → verify all contents present

**Files touched**: `TerrainFactory.cs`.

**Verification**: Full save/load round-trip — spheres, seeds, items, workstations persist correctly.

---

### 8f. Summary

| # | Phase | Files | Est. lines | Test |
|---|-------|-------|-----------|------|
| 4.1 | Data model + hook | `RoomContentsDefinition.cs` (new), `CustomTerrainDefinition.cs`, `TerrainFactory.cs` | ~150 | JSON loads, no crash |
| 4.2 | Slots | `TerrainFactory.cs` | ~80 | Slots visible, accept items |
| 4.3 | Workstations | `TerrainFactory.cs` | ~60 | Verb spawns on unshroud |
| 4.4 | Shelves | `TerrainFactory.cs` | ~70 | Shelves accept books |
| 4.5 | Comforts | `TerrainFactory.cs` | ~50 | Single-token enforced |
| 4.6 | Item seeds | `TerrainFactory.cs` | ~40 | Items on unshroud |
| 4.7 | Book seeds | `TerrainFactory.cs` | ~30 | Books fade in |
| 4.8 | WallArt seeds | `TerrainFactory.cs` | ~25 | Paintings on slots |
| 4.9 | Comfort seeds | `TerrainFactory.cs` | ~25 | Rugs on comforts |
| 4.10 | Quest seeds | `TerrainFactory.cs` | ~30 | Conditional spawn |
| 4.11 | FX seeds | `TerrainFactory.cs` | ~35 | Ambient FX on unshroud |
| 4.12 | Save/Load | `TerrainFactory.cs` | ~20 | Contents persist |

### 8g. Future extensions (post-Phase 4)

- `portages` — `PortageSphere` for multi-room token transfers
- `tabletops` — `TabletopSphere` for free-form surfaces
- `buildingWorkstations` — `BuildingWorkstationSphere` + `BuildingWorkstationSeed` for village buildings
