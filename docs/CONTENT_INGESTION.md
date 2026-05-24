# Book of Hours Content Mod Ingestion Pipeline

Analysis of how the decompiled game (`BookOfHoursDecompiled`) ingests content mods.

**Key insight:** Terrain/rooms are NOT part of this Fucine entity pipeline. They are Unity scene objects
(`TerrainFeature : AbstractPermanentPayload`) populated at runtime via `PopulateTerrainFeatureCommand`.
Mods like TheChandlery that add custom rooms must use Harmony patching to inject into `TerrainFeature`,
`PopulateTerrainFeatureCommand`, and `GameGateway`. The "terrain JSON" in ExampleMod is a custom format
read by TheChandlery's own patching code, NOT by the game's built-in content loader.

---

## Mod Discovery

| Class | File | Role |
|---|---|---|
| **ModManager** | `SecretHistories.Infrastructure.Modding/ModManager.cs` | Scans `persistentDataPath/mods/` + Steam Workshop, reads `mods.txt`/`mods_order.txt`, catalogues mods via `synopsis.json` manifests |
| **Mod** | `SecretHistories.Infrastructure.Modding/Mod.cs` | Represents one mod; holds `content/`, `loc/`, `images/`, `dll/` folder paths |
| **SubscribedStorefrontMod** | same file | Extends `Mod` for Steam Workshop items |
| **NullMod** | same file | Invalid-mod placeholder |
| **ModInstallType** | same file | Enum: `Local`, `SteamWorkshop`, `Unknown` |

---

## Mod Load Order

Mod load order is defined by **`mods_order.txt`** at `Application.persistentDataPath/mods_order.txt` — one mod ID per line.

| Class / Method | File | Role |
|---|---|---|
| `ModManager._modsOrder` | `ModManager.cs:120` | `List<string>` loaded from `mods_order.txt` during `CatalogueMods()` |
| `CatalogueMods()` | `ModManager.cs:203` | Reads `mods_order.txt` into `_modsOrder`; appends new mods not already in the list (line 261) |
| `GetAllModsInLoadOrder()` | `ModManager.cs:184` | Returns `_cataloguedMods.Values` sorted by `_modsOrder.IndexOf(mod.Id)` — lower index = earlier |
| `GetEnabledModsInLoadOrder()` | `ModManager.cs:191` | Filters `GetAllModsInLoadOrder()` to only enabled mods, preserving order |
| `SetModIndex(modId, index)` | `ModManager.cs:523` | Removes and reinserts a mod at a given position in `_modsOrder`, then persists |

### How load order affects content

1. **`CompendiumLoader.LoadModsToCompendium()`** (line 137) iterates `GetEnabledModsInLoadOrder()`. For each mod in order, it creates and appends `DataFileLoader` instances to `modContentLoaders` / `modLocLoaders`. These lists preserve iteration order.
2. For each entity type, `modContentLoaders` are iterated in order when gathering `LoadedDataFile`s. All files are fed to `EntityTypeDataLoader.SupplyContentFiles()` as a flat list.
3. Inside `LoadEntityDataFromSuppliedFiles()`, all `EntityData` objects are sorted by **`$priority`** (ascending), then processed sequentially. When two mods define the same entity ID, the one **processed later** wins — its data overwrites the earlier one after merge/override/operation logic.
4. Since `$priority` sorting is applied first, a mod's internal `$priority` values override positional load order for individual entities. Load order serves as a secondary tiebreaker for entities with equal `$priority`.
5. **Image loading** (`TryLoadImagesForEnabledMods()`, line 464): iterates mods in load order; later mod images overwrite earlier ones on path collision.
6. **DLL loading** (`LoadModDLLs()`, line 142): mod DLLs are loaded and `TryInitialiseAssembly()` called in load order.

### In-game reordering

The Mod Manager UI calls `ModManager.SetModIndex(modId, index)`, which removes the mod from `_modsOrder` and reinserts it at the target index, then persists to `mods_order.txt`.

---

## Content Loading Orchestration

| Class | File | Role |
|---|---|---|
| **Glory** | `SecretHistories.Services/Glory.cs` | Game init: creates `Compendium`, `ModManager`, sets `contentdir`, triggers `LoadCompendium()` |
| **Config** | `SecretHistories.Services/Config.cs` | Sets `contentdir` = `"bhcontent"`, provides culture code |
| **Compendium** | `Compendium.cs` | Central dictionary of all loaded entity instances, keyed by type → `EntityStore` |
| **CompendiumLoader** | `SecretHistories.Services/CompendiumLoader.cs` | Orchestrates full pipeline: core JSON → core loc → mod JSON → mod loc → images |
| **DataFileLoader** | `DataFileLoader.cs` | Recursively scans folder for `.json` files, parses each with Newtonsoft.Json, wraps in `LoadedDataFile` |
| **LoadedDataFile** | `LoadedDataFile.cs` | Holds path, entity tag (e.g. `"elements"`), and the `JProperty` |

---

## Entity Type Discovery & Binding

| Class | File | Role |
|---|---|---|
| **FucineImportable** | `SecretHistories.Fucine/FucineImportable.cs` | `[FucineImportable("tagname")]` attribute marking a class as loadable from JSON |
| **EntityTypeDataLoader** | `SecretHistories.Fucine/EntityTypeDataLoader.cs` | Per-tag pipeline: collects localizable keys, localizes, unpacks JSON → `EntityData`, applies `$depends`/`$extends`/`$derives`/`$mute`/operations, instantiates entities |

### Fucine Entity Types

| Entity Class | Tag | File |
|---|---|---|
| `Element` | `"elements"` | `SecretHistories.Entities/Element.cs` |
| `Recipe` | `"recipes"` | `SecretHistories.Entities/Recipe.cs` |
| `Verb` | `"verbs"` | `SecretHistories.Entities/Verb.cs` |
| `Legacy` | `"legacies"` | `SecretHistories.Entities/Legacy.cs` |
| `DeckSpec` | `"decks"` | `SecretHistories.Entities/DeckSpec.cs` |
| `Culture` | `"cultures"` | `SecretHistories.Entities/Culture.cs` |
| `Achievement` | `"achievements"` | `SecretHistories.Entities/Achievement.cs` |
| `Ending` | `"endings"` | `SecretHistories.Entities/Ending.cs` |
| `Lever` | `"levers"` | `SecretHistories.Entities/Lever.cs` |
| `Setting` | `"settings"` | `SecretHistories.Entities/Setting.cs` |
| `Dictum` | `"dicta"` | `SecretHistories.Entities/Dictum.cs` |

---

## JSON → EntityData Pipeline

| Class | File | Role |
|---|---|---|
| **EntityData** | `SecretHistories.Fucine.DataImport/EntityData.cs` | Hashtable of property → value, applies all merge/override/operation logic |
| **EntityDataImportExtensions** | same file | `ShouldLoad()` (`$depends`/`$incompatible`), `InheritMerge()` (`$derives`), `InheritOverride()` (`$extends`), `FlushMuteFromEntityData()`, `ApplyPropertyOperationsOn()` (`$append`, `$prepend`, `$plus`, `$minus`, `$add`, `$remove`, `$prefix`, `$postfix`, `$replace`, `$replaceLast`, `$listedit`, `$dictedit`, `$clear`) |
| **FucineUniqueIdBuilder** | same file | Builds unique path-based IDs for localization matching |

---

## EntityData → C# Object Instantiation

| Class | File | Role |
|---|---|---|
| **AbstractEntity\<T\>** | `SecretHistories.Fucine/AbstractEntity.cs` | Base: constructor iterates `[FucineX]` properties and imports them via attribute-driven importers |
| **FactoryInstantiator** | `SecretHistories.Fucine/FactoryInstantiator.cs` | Static factory caching compiled constructors, calls `ImportedEntityFactory<T>.ConstructorFastInvoke()` |
| **ImportedEntityFactory\<T\>** | `SecretHistories.Fucine/ImportedEntityFactory.cs` | Generic; uses `PrecompiledInvoke` (Expression trees) for fast construction |
| **PrecompiledInvoke** | `SecretHistories.Fucine/PrecompiledInvoke.cs` | Compiles `Func<EntityData, ContentImportLog, T>` via `System.Linq.Expressions` |
| **TypeInfoCache\<T\>** | `SecretHistories.Fucine/TypeInfoCache.cs` | Static-per-type: caches all `[Fucine]` attributed properties as `CachedFucineProperty<T>` |
| **CachedFucineProperty\<T\>** | `SecretHistories.Fucine/CachedFucineProperty.cs` | Caches property info, compiled getter/setter, and the `Fucine` attribute; `GetImporterForProperty()` delegates to attribute |

---

## Attribute-Driven Property Importers

| Importer | Role |
|---|---|
| **FucineValue** → **ValueImporter** | Simple values (string, int, float, bool, enum) |
| **FucineList** → **ListImporter** | `List<T>` |
| **FucineDict** → **DictImporter** | `Dictionary<K,V>` |
| **FucineSubEntity** → **SubEntityImporter** | Nested entities |
| **FucineAutoValue** → **OmniImporter** | Auto-dispatch based on runtime type |
| **StructImporter** | Complex struct/class types |
| **TypeImporter** | `System.Type` via `Type.GetType()` |
| **PathImporter** | `FucinePath` |
| **ImportMethods** | (`SecretHistories.Fucine/ImportMethods.cs`) Central dispatch: `GetDefaultImportFuncForType()` routes to correct importer |

All importers live under `SecretHistories.Fucine/`.

---

## Post-Import

| Class | File | Role |
|---|---|---|
| **OnPostImport()** | `AbstractEntity.cs` | Calls `OnPostImportForSpecificEntity()`, registers cross-refs, logs unknown keys |
| **Compendium.OnPostImport()** | `Compendium.cs` | Iterates all entities, calls `OnPostImport()`, then `ValidateEntityReferences()` |
| **Compendium.OnPostImportFinal()** | `Compendium.cs` | Second pass: `MutationEffect`/`MorphDetails` finalization |
| **ContentImportLog** | `SecretHistories.Fucine/ContentImportLog.cs` | Singleton log collecting warnings/errors during import |
| **DebugLoadCompendium** | `DebugLoadCompendium.cs` | Editor-only debug component for testing content loading |

---

## Image Loading

| Class | File | Role |
|---|---|---|
| **ModManager.TryLoadImagesForEnabledMods()** | `SecretHistories.Infrastructure.Modding/ModManager.cs:456` | Loads `.png` from mod `images/` folders into `_images` dict |

---

## End-to-End Flow

```
Glory.Awake()
 → CompendiumLoader.PopulateCompendium("en")
   → DataFileLoader scans StreamingAssets/bhcontent/core/ for *.json → LoadedDataFiles
   → DataFileLoader scans StreamingAssets/bhcontent/loc_en/ for *.json → LoadedDataFiles
   → LoadModsToCompendium("en")
     → ModManager.GetEnabledModsInLoadOrder() (sorted by _modsOrder)
     → for each enabled mod, in load order:
       DataFileLoader(mod/content/) → appended to modContentLoaders (preserves order)
       DataFileLoader(mod/loc/en/) → appended to modLocLoaders (preserves order)
     → TryLoadImagesForEnabledMods() (iterates mods in load order)
   → Reflect all [FucineImportable("tag")] types → create EntityTypeDataLoader per tag
   → For each EntityTypeDataLoader:
     SupplyContentFiles(core, coreLoc, mod, modLoc)
     LoadEntityDataFromSuppliedFiles()
       → collect localizable keys → register loc emendations
        → unpack JSON JArray → EntityData[] (mod files processed in load order)
        → filter by $depends/$incompatible → sort by $priority (ascending)
        → for each: ApplyDataToCollection() → merge/override/mute/ops
          (same-ID entities: last processed wins; $priority overrides load order)
     → AddImportedEntityInstancesToCompendium()
       → FactoryInstantiator.CreateEntity(type, entityData, log)
         → AbstractEntity<T> constructor
           → for each [FucineX] property: importer.TryImportProperty() → set compiled setter
   → compendium.OnPostImport() → validate refs
   → compendium.OnPostImportFinal()
   → Fire ContentUpdatedEvent
```
