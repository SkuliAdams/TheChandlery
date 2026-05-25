# Wheel Data Loading System — Implementation Status

Port of The Roost Machine's Beachcomber data loading subsystem to Book of Hours, as part of TheChandlery's Wheel module.

---

## Overview

| Source | Target |
|--------|--------|
| **Roost Machine** — `Roost.Beachcomber` | **TheChandlery** — `TheHouse.Wheel` |
| Namespace `Roost` | Namespace `TheHouse.Wheel` |
| Cultist Simulator (.NET 4.7.2) | Book of Hours (.NET 4.7.2) |
| Harmony 2.2.1 | Harmony 2.4.2 |

The data loading system enables two things mods couldn't do before:
1. **Custom entity types** — define new `[FucineImportable]` classes in a mod DLL that load from JSON on par with native `Element`, `Recipe`, `Verb`, etc.
2. **Custom properties on existing entities** — add new JSON-serializable properties to native entity types at runtime, accessible via extension methods.

---

## File Layout

```
src/Wheel/
├── Wheel.cs              ← entry point (public API + orchestration)
├── WheelTypes.cs          ← entity type injection via transpiler  ✓ Phase 1
├── WheelStore.cs          ← property registry + runtime data store ✓ Phase 2
├── WheelIntercept.cs      ← PushUnknownProperty prefix + FactoryInstantiator prefix ✓ Phase 3
├── WheelIgnore.cs         ← property/entity group suppression     ✓ Phase 4
├── WheelChain.cs          ← $extends/$derives inheritance support ✓ Phase 5
├── WheelFucine.cs         ← custom Fucine attribute types         ✓ Phase 6
└── (WheelChain.cs Phase 7) ← runtime InheritFrom patching         ✓ Phase 7
```

---

## Phase 1 — `WheelTypes.cs` ✓ DONE

### Class

```csharp
namespace TheHouse.Wheel;

internal static class WheelTypes
{
    internal static void Enact(Harmony harmony);
}
```

### Patch

Harmony **transpiler** on `CompendiumLoader.PopulateCompendium()`.

### What It Does

Book of Hours' `CompendiumLoader.PopulateCompendium()` (`CompendiumLoader.cs:31-118`) only scans `Assembly.GetAssembly(GetType())` — the game assembly — for `[FucineImportable]` types. Mod DLLs that define custom entity classes are not discovered.

The transpiler injects a call to `InjectModEntityTypes` AFTER the native `foreach (Type type in types)` loop (lines 58-67) that populates local variables `dictionary` (`Dictionary<string, EntityTypeDataLoader>`) and `list` (`List<Type>`), but BEFORE `compendiumToPopulate.Initialise(list)` (line 68).

### Injected Logic

```csharp
private static void InjectModEntityTypes(
    List<Type> typesToLoad,
    Dictionary<string, EntityTypeDataLoader> fucineLoaders,
    string cultureId,
    ContentImportLog log)
{
    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        if (IsModAssembly(assembly))
            foreach (Type type in assembly.GetTypes())
                if (!typesToLoad.Contains(type))
                {
                    var attr = (FucineImportable)type.GetCustomAttribute(typeof(FucineImportable), false);
                    if (attr != null)
                    {
                        typesToLoad.Add(type);
                        fucineLoaders.Add(attr.TaggedAs.ToLower(),
                            new EntityTypeDataLoader(type, attr.TaggedAs, cultureId, log));
                    }
                }
}
```

### IL Locals Detection

The transpiler uses IL pattern analysis to find the `list` and `dictionary` local variable slots: it searches for the `Callvirt Compendium.Initialise` instruction to locate the list local (the `ldloc` before the call), then searches for `Newobj Dictionary<...>.ctor()` to locate the dictionary local (the `stloc` after construction).

### `IsModAssembly()` Check

Filter assemblies whose `Location` path starts with:
- `Application.persistentDataPath` → `mods/` (local mods) — **normalized from `/` to `\`**
- `steamapps\workshop\content` (Steam Workshop)

### DLL Loading Timing

`ModManager.LoadModDLLs()` is called from `Glory.Awake()` during startup, BEFORE `CompendiumLoader.PopulateCompendium()` runs. All mod assemblies are already in the AppDomain when the transpiler fires.

### Validation

- 3 custom entity types injected (`CustomTerrainDefinition`, `TestItem`, `TestConfig`)
- Types are registered in `Compendium.Initialise(IEnumerable<Type>)` alongside native types
- `OnPostImportForSpecificEntity` fires correctly for loaded entities

---

## Phase 2 — `WheelStore.cs` ✓ DONE

### Data Structures

```csharp
// Registration-time: entity type → property name → type metadata
private static Dictionary<Type, Dictionary<string, PropertySlot>> _claimed = new();

// Runtime: entity instance → property name → value
// Uses ConditionalWeakTable for automatic cleanup when entities are GC'd
private static ConditionalWeakTable<IEntityWithId, Dictionary<string, object>> _data = new();

// Molding callbacks keyed by entity type
private static Dictionary<Type, List<Action<EntityData>>> _moldings = new();
```

### Key Methods

| Method | Purpose |
|--------|---------|
| `AddClaim<TEntity>()` | Register a claim: entity type → property name → PropertySlot |
| `HasClaim(Type, string)` | Walk type hierarchy checking if property is claimed |
| `SetCustomProperty(IEntityWithId, string, object)` | Store a value in the runtime data store |
| `RetrieveProperty(IEntityWithId, string)` | Read a value from the runtime data store |
| `ApplyMoldings(Type, EntityData, ContentImportLog)` | Run all registered molding callbacks for the entity type |
| `ConvertValue(object, Type)` | Convert raw import values to claimed property type via `Convert.ChangeType` |

### Property Interception Flow

1. `PushUnknownProperty` prefix checks `HasClaim(entityType, key)` — case-insensitive (key lowercased)
2. On match: `SetCustomProperty` stores the value, prefix returns `false` (suppresses the unknown-property warning)
3. On mismatch: prefix returns `true` (native behavior — logged as UNKNOWN PROPERTY)

---

## Phase 3 — `WheelIntercept.cs` ✓ DONE

**Note**: The original plan called for a transpiler on `AbstractEntity<T>.ctor()`. This was abandoned due to Harmony 2.4.2 limitations.

### Patch Strategy

Two Harmony prefixes, both applied per **closed generic base class** (e.g., `AbstractEntity<Element>`, `AbstractEntity<Recipe>`), not on open generics or leaf classes.

### Patch 1: PushUnknownProperty (claimed property interception)

- **Target**: `AbstractEntity<T>.PushUnknownProperty(object key, object value)` — one patch per closed generic instantiation
- **Type**: Prefix
- **Logic**: If the key matches a claimed property for the entity's type, store it in WheelStore and return `false` (skip the warning). Otherwise return `true` (normal handling).

```csharp
private static bool UnknownPrefix(object __instance, object key, object value)
{
    if (!(__instance is IEntityWithId entity))
        return true;
    var entityType = entity.GetType();
    var propertyKey = key.ToString().ToLower();
    if (WheelStore.HasClaim(entityType, propertyKey))
    {
        WheelStore.SetCustomProperty(entity, propertyKey, value);
        return false;
    }
    return true;
}
```

### Patch 2: FactoryInstantiator.CreateEntity (molding application)

- **Target**: `FactoryInstantiator.CreateEntity(Type, EntityData, ContentImportLog)` — single static method
- **Type**: Prefix
- **Logic**: Apply registered moldings to the EntityData BEFORE the entity is constructed. This allows renaming/migrating legacy JSON keys.

```csharp
private static bool CreateEntityPrefix(Type T, EntityData importDataForEntity, ContentImportLog log)
{
    WheelStore.ApplyMoldings(T, importDataForEntity, log);
    return true;
}
```

### Rejected Approaches

| Approach | Why Rejected |
|----------|-------------|
| Transpiler on `AbstractEntity<T>.ctor` | Harmony 2.4.2 cannot patch open generic types at all (not just transpilers — prefixes and postfixes also fail) |
| Constructor prefix on `AbstractEntity<T>.ctor` per closed generic | Caused NullReferenceException on all entity types during Fucine import. Root cause: Harmony interaction with generic base class constructors — the prefix runs but the subsequent native constructor body fails for unknown reasons on all properties |
| Per-type constructor transpiler | Excessive complexity for per-type IL pattern matching |
| Importer customization via `FucineImportable` | Requires modifying native Fucine pipeline at a deeper level |

---

## Phase 4 — `WheelIgnore.cs` ✓ DONE

```csharp
namespace TheHouse.Wheel;

internal static class WheelIgnore
{
    internal static void IgnoreProperty(Type entityType, string propertyName);
    internal static void IgnoreEntityGroup(string groupId);
    internal static bool Ignores(Type entityType, string propertyName);
    internal static bool IgnoresGroup(string groupId);
}
```

Suppresses specific JSON properties from triggering UNKNOWN PROPERTY warnings. Per-property ignores use a type→list map (walks type hierarchy for inherited ignores). Group-level ignores use `[FucineImportable]` TaggedAs and lazily build a type→group reverse map.

### Validation

- `Wheel.IgnoreProperty<Element>("wheelLegacyField")` → `[WheelIntercept] Ignored unknown property 'wheellegacyfield' on Element` ✓ No UNKNOWN PROPERTY warning
- `Wheel.IgnoreEntityGroup("wheel_test_configs")` → `[WheelIntercept] Ignored unknown property 'anotherunknownkey'` and `'extraunknownfield'` on TestConfig ✓ Both suppressed without warnings

---

## Phase 5 — `WheelChain.cs` ✓ DONE

### What It Does

The native BoH inheritance system (`EntityData.ApplyDataToCollection`) resolves `$extends` and `$derives` entirely at the `EntityData` Hashtable level, **before** `AbstractEntity<T>.ctor()` and therefore **before** `PushUnknownProperty`. Custom properties in the ValuesTable (including those for our Wheel claimed-property system) flow through this pipeline naturally — parent values are inherited into child EntityData, then our `PushUnknownProperty` prefix stores them in WheelStore at construction time.

WheelChain provides a Harmony **prefix** on `EntityData.ApplyDataToCollection()` that converts the key `"extends"` (without `$`) to `"$extends"` so the native system processes it identically. This allows mods to use `"extends": "parent_id"` as a shorthand for `"$extends": "parent_id"`.

### Patch

| Target | Type | Purpose |
|--------|------|---------|
| `EntityData.ApplyDataToCollection` | Prefix | Rename `extends` → `$extends` before native inheritance processing |

### Validation

| Entity | JSON Key | `wheelteststring` | `wheeltestint` | Behavior |
|--------|----------|-------------------|----------------|----------|
| `base_entity` | (none) | `inherited value` | `999` | Own values |
| `derived_entity` | `$extends` | `overridden value` | `999` | Child overrides string, inherits int |
| `converted_entity` | `extends` (bare) | `inherited value` | `999` | WheelChain converts bare `extends` to `$extends`, both values inherited |

No `UNKNOWN PROPERTY` warnings for `extends` or `$extends` — both are silently consumed.

---

## Phase 6 — `WheelFucine.cs` ✓ DONE

Custom Fucine attribute types (`WheelFucineNullable`, `WheelFucineEverValue`) and custom importers (`WheelPanImporter`, `WheelNullableImporter`, `WheelExtendedPathImporter`). Extended `PropertySlot` with `Fucine` attribute field. Added `ConvertWithImporter()` pipeline for claimed properties with custom importers. See `src/Wheel/WheelFucine.cs`.

### Validation

- `WheelFucineEverValue<Vector2>(Position)` on TestItem: `Position=(3.00, 4.00)` ✓
- `WheelFucineEverValue<int>(LegacyValue)` on TestConfig: `LegacyValue=42` ✓
- `WheelFucineNullable<int?>(wheelNullableInt)` claimed: value `77` stored as `int?` ✓
- `FucinePathValue(wheelPath)` claimed: path `~/library/shelf!book001` stored as `FucinePath` ✓

---

## Phase 7 — `WheelChain.cs` Runtime Inheritance ✓ DONE

### The Gap

Book of Hours has **two** inheritance systems:

| System | Level | When | Key | Handles Custom Props? |
|--------|-------|------|-----|-----------------------|
| **JSON-time** | EntityData (Hashtable) | Pre-construction, in `ApplyDataToCollection()` | `$extends` / `$derives` | ✓ Yes — WheelChain Phase 5 handles this |
| **Runtime** | Live entity objects | Post-construction, during import via `OnPostImportForProperties` | `Inherits` property → `Element.InheritFrom()` / `Recipe.InheritFrom()` | ❌ No — custom properties are NOT copied |

`Element.InheritFrom()` copies `Aspects`, `XTriggers`, `Ambits`, `Slots`, etc. from the parent element. `Recipe.InheritFrom()` copies `Reqs`, `ExtantReqs`, etc. Neither copies custom claimed properties stored in WheelStore.

The Roost Machine handles this via `CuckooJr` — a **transpiler** on `Element.InheritFrom()` and `Recipe.InheritFrom()` that injects calls to `InheritClaimedProperties(parent, child)` after native copying completes.

### Implementation

Two Harmony **postfixes** (not transpilers — postfix is simpler and sufficient since copying happens at the end of each method):

| Target | Patch | Purpose |
|--------|-------|---------|
| `Element.InheritFrom(Compendium, Element)` | Postfix (`InheritElementPostfix`) | Copies custom props from parent to child after native copying |
| `Recipe.InheritFrom(Recipe)` (private) | Postfix (`InheritRecipePostfix`) | Copies custom props from parent to child after native copying |

```csharp
private static void InheritElementPostfix(Element __instance, Element inheritFromElement)
{
    WheelStore.InheritClaimedProperties(__instance, inheritFromElement);
}

private static void InheritRecipePostfix(Recipe __instance, Recipe inheritFromRecipe)
{
    WheelStore.InheritClaimedProperties(__instance, inheritFromRecipe);
}
```

`WheelStore.InheritClaimedProperties()` iterates the parent's stored custom properties and copies each to the child if the child doesn't already have it (override semantics matching native `InheritFrom` behavior).

### Validation

| Entity | Inherits From | Custom Props After Phase 7 | Status |
|--------|---------------|----------------------------|--------|
| `wheel.test.element.runtime_inherit` | `wheel.test.element.base_entity` | `wheeltestint=999`, `wheelteststring="inherited value"` | ✓ Inherited |
| `wheel.test.recipe.runtime_inherit` | `wheel.test.recipe.base_inherit` | `wheelrecipenote="inherited recipe note"` | ✓ Inherited |

---

## Critical Discoveries

### Harmony 2.4.2 Cannot Patch Open Generic Types

This was the single most impactful discovery. `AccessTools.Constructor(typeof(AbstractEntity<>), ...)` and similar lookups for open generic types will resolve to a `MethodBase`, but calling `harmony.Patch()` with that `MethodBase` does NOT work — the patch silently fails or is never applied. All patches must target closed generic instantiations (e.g., `typeof(AbstractEntity<Element>)`).

### Path Normalization

`Application.persistentDataPath` returns forward slashes (`C:/Users/...`) on Windows, while `Assembly.Location` uses backslashes (`C:\Users\...`). All path comparisons must normalize with `.Replace('/', '\\')`.

### Hashtable, Not Dictionary

`EntityData.ValuesTable` is `System.Collections.Hashtable` (not `Dictionary<string, object>`). Use `ContainsKey()`, `this[key]`, `.Remove()`.

### Fucine Namespace

`ContentImportLog`, `IEntityWithId`, and `AbstractEntity<T>` are all in `SecretHistories.Fucine`.

### ValuesTable Keys Are Lowercase

`UnpackAndLocaliseEntityData` (in `EntityTypeDataLoader.cs:112`) lowercases all JSON property keys with `item.Name.ToLower()`. Molding callbacks must use lowercase keys when checking `ValuesTable.ContainsKey()`.

### AbstractEntity<T>.Lever Property

`AbstractEntity<T>` (in `AbstractEntity.cs:35`) has:
```csharp
[FucineValue("")]
public string Lever { get; set; }
```

This means EVERY entity type inherits a `Lever` Fucine property. It's part of the native import loop and shows up if the EntityData has a `lever` key.

---

## Public API Surface (`Wheel.cs`)

### Registration Methods

```csharp
public static void ClaimProperty<TEntity, TProperty>(string propertyName,
    bool localize = false, TProperty defaultValue = default(TProperty))
    where TEntity : AbstractEntity<TEntity>;

public static void AddImportMolding<TEntity>(Action<EntityData> molding);
```

### Extension Methods (on `IEntityWithId`)

```csharp
public static class WheelEntityExtensions
{
    public static T RetrieveProperty<T>(this IEntityWithId entity, string propertyName);
    public static bool TryRetrieveProperty<T>(this IEntityWithId entity, string propertyName, out T result);
    public static void SetCustomProperty(this IEntityWithId entity, string propertyName, object value);
    public static bool HasCustomProperty(this IEntityWithId entity, string propertyName);
    public static void RemoveCustomProperty(this IEntityWithId entity, string propertyName);
    public static Dictionary<string, object> GetCustomProperties(this IEntityWithId entity);
}
```

---

## Verification Results

| Step | Test | Outcome |
|------|------|---------|
| 1 | `dotnet build src/TheChandlery.csproj` | ✓ Build succeeds (0 errors) |
| 2 | Custom entity: `TestItem` with `[FucineImportable("wheel_test_items")]` + JSON content | ✓ Entity loads, `OnPostImportForSpecificEntity` fires with correct values |
| 3 | Custom entity: `TestConfig` with `[FucineImportable("wheel_test_configs")]` + JSON content | ✓ Entity loads (silent — `OnPostImportForSpecificEntity` is empty) |
| 4 | Claimed property: `Wheel.ClaimProperty<Element, string>("wheelTestString")` + JSON `"wheelteststring": "hello"` | ✓ `element.RetrieveProperty<string>("wheelTestString")` returns `"hello"`, no UNKNOWN PROPERTY warning |
| 5 | Claimed property: omitted from JSON | ✓ Default value returned, no warning |
| 6 | Unclaimed key in JSON | ✓ Logged as UNKNOWN PROPERTY warning (not silently swallowed) |
| 7 | Molding: `Wheel.AddImportMolding<Element>(PreProcessElementData)` converts `wheellegacyformat` → `wheelteststring` | ✓ Legacy format converted silently, no warning |
| 8 | Molding: `PreProcessElementData` removes `wheellegacyformat` from ValuesTable | ✓ Key removed — no UNKNOWN PROPERTY warning for it |
| 9 | Ignore: `Wheel.IgnoreProperty<Element>("wheelLegacyField")` | ✓ `wheellegacyfield` silently suppressed with debug log, no UNKNOWN PROPERTY warning |
| 10 | Group-ignore: `Wheel.IgnoreEntityGroup("wheel_test_configs")` | ✓ `extraunknownfield` and `anotherunknownkey` on TestConfig both suppressed without warnings |
| 11 | Inheritance: `$extends` on derived entity | ✓ Child overrides `wheelteststring`, inherits `wheeltestint=999` from base |
| 12 | Inheritance: bare `extends` (no `$`) on converted entity | ✓ WheelChain converts to `$extends`, both custom props inherited |
| 13 | Runtime inheritance: `inherits` on element | ✓ `wheeltestint=999` and `wheelteststring="inherited value"` inherited from parent |
| 14 | Runtime inheritance: `inherits` on recipe | ✓ `wheelrecipenote="inherited recipe note"` inherited from parent |

### Known Issues (Minor)

- `WheelTestMod` mod listing says "has no content directory" despite `content/` existing on disk — this appears to be cosmetic (maybe a specific subdirectory pattern check); content files ARE loaded correctly
- Constructor prefix on `AbstractEntity<T>.ctor` causes NRE — replaced by `FactoryInstantiator.CreateEntity` prefix (documented under Phase 3 Rejected Approaches)

---

## Test Mod

`test-mods/WheelTestMod/` is a standalone mod (separate solution entry) that validates the Wheel data loading system:

| File | Purpose |
|------|---------|
| `Init.cs` | Entry point — registers claims, moldings |
| `TestEntities.cs` | `TestItem` + `TestConfig` custom FucineImportable entity classes |
| `content/wheel-elements.json` | Element test data with custom properties |
| `content/wheel-test-items.json` | TestItem custom entity data |
| `content/wheel-test-configs.json` | TestConfig custom entity data |
| `content/wheel-recipes.json` | Recipe test data with custom properties |
| `content/wheel-inheritance.json` | Element data testing custom property inheritance |

Build: `dotnet build test-mods/WheelTestMod/WheelTestMod.csproj /p:BOHGamePath=... /p:BOHModsPath=...`

### Key Test Mod Code Patterns

**Register claims and moldings** (in `Initialise()`):
```csharp
Wheel.ClaimProperty<Element, string>("wheelTestString", defaultValue: "default-string");
Wheel.AddImportMolding<Element>(PreProcessElementData);
```

**Molding callback** (note: all keys must be lowercase):
```csharp
private static void PreProcessElementData(EntityData entityData)
{
    if (entityData.ValuesTable.ContainsKey("wheellegacyformat"))
    {
        entityData.ValuesTable["wheelteststring"] = entityData.ValuesTable["wheellegacyformat"];
        entityData.ValuesTable.Remove("wheellegacyformat");
    }
}
```

---

## Dependencies

- `Lib.Harmony` 2.4.2 (NuGet)
- `SecretHistories.Main` (for `AbstractEntity<T>`, `CompendiumLoader`, `EntityTypeDataLoader`)
- `SecretHistories.Fucine` (for `FucineImportable`, `Fucine`, `FactoryInstantiator`)
- `SecretHistories.Interfaces` (for `IEntityWithId`)
- `UnityEngine.CoreModule` (for `Application.persistentDataPath`)
