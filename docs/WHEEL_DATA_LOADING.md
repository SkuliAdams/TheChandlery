# Wheel Data Loading System — Implementation Plan

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

All files under `src/Wheel/`, all with `namespace TheHouse.Wheel;` (file-scoped).

```
src/Wheel/
├── Wheel.cs              ← existing (image loading) +
                             public API (ClaimProperty, AddIgnoredProperty, etc.)
├── WheelTypes.cs          ← entity type injection via transpiler
├── WheelStore.cs          ← property registry + runtime data store
├── WheelIntercept.cs      ← constructor interceptor via transpiler
├── WheelIgnore.cs         ← property/entity group suppression
├── WheelChain.cs          ← custom property inheritance
└── WheelFucine.cs         ← custom Fucine attribute types
```

---

## Phase 1 — `WheelTypes.cs`

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

### Rationale

Book of Hours' `CompendiumLoader.PopulateCompendium()` (`CompendiumLoader.cs:31-118`) only scans `Assembly.GetAssembly(GetType())` — the game assembly — for `[FucineImportable]` types. Mod DLLs that define custom entity classes are not discovered.

### Injection Point

After the native `foreach (Type type in types)` loop (lines 58-67) that populates local variables `dictionary` (string → `EntityTypeDataLoader`) and `list` (List<Type>), but before `compendiumToPopulate.Initialise(list)` (line 68).

### Injected Logic

```csharp
// Pseudocode for the injected call
private static void InjectModEntityTypes(
    ref List<Type> typesToLoad,
    ref Dictionary<string, EntityTypeDataLoader> fucineLoaders,
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

### `IsModAssembly()` Check

Filter assemblies whose `Location` path contains:
- `Application.persistentDataPath` → `mods/` (local mods)
- `steamapps\workshop\content` (Steam Workshop)

(Same approach as Roost Machine.)

### DLL Loading Timing

`ModManager.LoadModDLLs()` is called from `Glory.Awake()` during startup, *before* `CompendiumLoader.PopulateCompendium()` runs. All mod assemblies are already in the AppDomain when the transpiler fires.

---

## Phase 2 — `WheelStore.cs`

### Class

```csharp
namespace TheHouse.Wheel;

internal static class WheelStore
{
    // Called by WheelTypes for custom FucineImportable types' localizable keys
    internal static IEnumerable<string> GetLocalizableProperties(Type entityType);

    // Called by WheelIntercept after the native Fucine import loop
    internal static void InterceptClaimedProperties(
        IEntityWithId entity,
        EntityData entityData,
        Type entityType,
        ContentImportLog log);

    // Called by WheelIntercept to import a single value
    internal static void LoadCustomProperty(
        IEntityWithId entity,
        string propertyName,
        object data,
        ContentImportLog log);

    // Runtime access (called by extension methods)
    internal static object RetrieveProperty(IEntityWithId entity, string propertyName);
    internal static bool HasCustomProperty(IEntityWithId entity, string propertyName);
    internal static void SetCustomProperty(IEntityWithId entity, string propertyName, object value);
    internal static void RemoveProperty(IEntityWithId entity, string propertyName);
    internal static Dictionary<string, object> GetCustomProperties(IEntityWithId entity);

    // Registration
    internal static void AddClaim<TEntity>(string propertyName, Type propertyType,
        object defaultValue, bool localize, Fucine attribute)
        where TEntity : AbstractEntity<TEntity>;

    // Molding (pre-processing EntityData before Fucine import)
    internal static void AddMolding<TEntity>(Action<EntityData> molding);
    internal static void ApplyMoldings(Type entityType, EntityData entityData, ContentImportLog log);
}
```

### Data Structures

```csharp
// Registration-time: entity type → property name → metadata
private static Dictionary<Type, Dictionary<string, PropertySlot>> _claimed = new();

// Runtime: entity instance → property name → value
private static Dictionary<IEntityWithId, Dictionary<string, object>> _data = new();

// Molding callbacks keyed by entity type
private static Dictionary<Type, List<Action<EntityData>>> _moldings = new();

private struct PropertySlot
{
    public Type type;
    public object defaultValue;
    public bool localize;
    public Fucine importer;  // custom importer attribute, null = use default
}
```

### `InterceptClaimedProperties` Logic

1. Look up `_claimed[entityType]` — if no claims for this type, return
2. For each claimed property key:
   a. Check if present in `entityData.ValuesTable` (lowercase keys)
   b. If present: import value using the registered `PropertySlot`'s type/importer
   c. Store in `_data[entity, key]`
   d. Remove from `entityData.ValuesTable` (so it won't be pushed as unknown)
   e. If `WheelIgnore.Ignores(entityType, propertyName)`, skip (don't store)

---

## Phase 3 — `WheelIntercept.cs`

### Class

```csharp
namespace TheHouse.Wheel;

internal static class WheelIntercept
{
    internal static void Enact(Harmony harmony);
}
```

### Patch

Harmony **transpiler** on `AbstractEntity<T>.ctor(EntityData, ContentImportLog)` — targeting the **open generic** `AbstractEntity<>` so the patch applies to all closed instantiations (Element, Recipe, Verb, Lever, Setting, etc.) simultaneously.

### Why a Transpiler

The user chose Option A: **inject a call after the native Fucine import loop, before the unknown-key push**. This is less invasive than replacing the entire constructor body, but still robust since the constructor IL is stable game code unlikely to change.

### Native Constructor (AbstractEntity.cs:181-212)

```
1. if ValuesTable has "id" → SetId(), remove "id"
2. else if UniqueId is set → SetId(UniqueId)
3. else → SetId(GetType().Name)
4. for each cached Fucine property:
     a. TryImportProperty(this as T, item, importDataForEntity)
     b. importDataForEntity.ValuesTable.Remove(item.LowerCaseName)
   ───────────────────────────────────── ← transpiler injection point
5. for each remaining key in ValuesTable.Keys:
     PushUnknownProperty(key, ValuesTable[key])
```

### Transpiler Logic

Find the start of step 5 (the `foreach (object key in importDataForEntity.ValuesTable.Keys)` loop) by identifying the IL pattern. Insert a call to `WheelStore.InterceptClaimedProperties(entity, entityData, typeof(T), log)` right before that loop.

`InterceptClaimedProperties` will:
1. Apply any moldings registered for `typeof(T)`
2. Remove all claimed property keys from `ValuesTable`, storing their properly-typed values

After this, step 5 sees only truly unknown keys and logs them as warnings.

### Open Generic Fallback

If `AccessTools.Constructor(typeof(AbstractEntity<>), new[] { typeof(EntityData), typeof(ContentImportLog) })` fails at runtime, fall back to iterating all `[FucineImportable]` entity types discovered by `WheelTypes` and patching each closed generic individually.

---

## Phase 4 — `WheelIgnore.cs`

### Class

```csharp
namespace TheHouse.Wheel;

internal static class WheelIgnore
{
    internal static void AddIgnoredProperty(Type entityType, string propertyName);
    internal static void AddIgnoredEntityGroup(string groupId);
    internal static bool Ignores(Type entityType, string propertyName);
    internal static bool Ignores(string groupId);
}
```

### Data

```csharp
private static Dictionary<Type, List<string>> _ignoredProperties = new();
private static HashSet<string> _ignoredGroups = new() { "dontload" };
```

### Usage

- Individual properties: `Wheel.AddIgnoredProperty<Element>("legacyField")`
- Entity groups: `Wheel.AddIgnoredEntityGroup("experimental")`

Checked by `WheelStore.InterceptClaimedProperties()` — if `WheelIgnore.Ignores(entityType, propertyName)`, the property is silently skipped during import. Entity group checking works at the `EntityData.ShouldLoad()` level (complementing BoH's native `$depends` system).

---

## Phase 5 — `WheelChain.cs`

### Class

```csharp
namespace TheHouse.Wheel;

internal static class WheelChain
{
    // Called by WheelStore when a derived entity inherits from a base
    internal static void InheritClaimedProperties(IEntityWithId inheritor, IEntityWithId inheritFrom);
}
```

### Strategy

Phase 1 — **data-level inheritance via BoH's native pipeline**. BoH's `EntityDataImportExtensions.ApplyDataToCollection()` already handles `$derives` and `$extends` at the EntityData level. Custom property keys present in the base entity's data will propagate to the derived entity's `ValuesTable`. `WheelIntercept` naturally picks them up.

Phase 2 (if needed) — **runtime inheritance**. If `Element.InheritFrom()` or similar runtime inheritance doesn't propagate custom properties, add transpilers on those methods. Smart merge rules:
- Scalars: child wins if present, otherwise parent's value
- Lists: parent's entries are appended to child's list
- Dictionaries: recursive deep merge (existing keys merged, new keys added)

---

## Phase 6 — `WheelFucine.cs`

### Purpose

Provide custom Fucine attribute types not present in Book of Hours' native set.

BoH already has: `FucineValue`/`ValueImporter`, `FucineList`/`ListImporter`, `FucineDict`/`DictImporter`, `FucineSubEntity`/`SubEntityImporter`, `FucineAutoValue`/`OmniImporter`, `FucineType`/`TypeImporter`, `FucinePathValue`/`PathImporter`, `StructImporter`.

### New Attributes

| Attribute | Importer | Purpose |
|-----------|----------|---------|
| `WheelFucineNullable` | `NullableImporter` | Maps `"null"` JSON value → C# `null` for `Nullable<T>` properties |
| `WheelFucineCustomList(typeof(EntryImporter))` | `CustomListPanImporter` | Lists with explicit entry-level importer |
| `WheelFucineCustomDict(typeof(KeyImporter), typeof(ValueImporter))` | `CustomDictPanImporter` | Dicts with explicit key/value importers |

All extend `SecretHistories.Fucine.Fucine` (so the Fucine pipeline recognizes them via `Attribute.GetCustomAttribute(propertyInfo, typeof(Fucine))`).

Defined in `namespace TheHouse.Wheel;` with full qualification back to `SecretHistories.Fucine`.

---

## Public API Surface (added to existing `Wheel.cs`)

All exposed via the existing `Wheel` class in `namespace TheHouse.Wheel;`.

### Registration Methods

```csharp
public static void ClaimProperty<TEntity, TProperty>(string propertyName,
    bool localize = false, TProperty defaultValue = default(TProperty))
    where TEntity : AbstractEntity<TEntity>;

public static void ClaimProperties<TEntity>(Dictionary<string, Type> properties,
    bool localize = false)
    where TEntity : AbstractEntity<TEntity>;

public static void AddIgnoredProperty<TEntity>(string propertyName);

public static void AddIgnoredEntityGroup(string groupId);

public static void AddImportMolding<TEntity>(Action<EntityData> molding);
```

### Extension Methods (on `IEntityWithId`)

Defined in `Wheel.cs` or a separate `WheelExtensions.cs`:

```csharp
public static class WheelEntityExtensions
{
    public static T RetrieveProperty<T>(this IEntityWithId entity, string propertyName);
    public static bool TryRetrieveProperty<T>(this IEntityWithId entity, string propertyName, out T result);
    public static void SetCustomProperty(this IEntityWithId entity, string propertyName, object value);
    public static bool HasCustomProperty(this IEntityWithId entity, string propertyName);
    public static void RemoveProperty(this IEntityWithId entity, string propertyName);
    public static Dictionary<string, object> GetCustomProperties(this IEntityWithId entity);
}
```

---

## Orchestration (`Wheel.cs`)

Updated `Wheel.Enact()`:

```csharp
namespace TheHouse.Wheel;

internal static class Wheel
{
    internal static void Enact(Harmony harmony)
    {
        // Existing: load chandlery/ images
        harmony.Patch(
            original: AccessTools.Method(typeof(ModManager), "TryLoadImagesForEnabledMods"),
            postfix: AccessTools.Method(typeof(Wheel), nameof(OnLoadImages)));

        // New: data loading
        WheelTypes.Enact(harmony);
        WheelIntercept.Enact(harmony);

        // WheelStore, WheelIgnore, WheelChain, WheelFucine are passive —
        // they register data and are invoked by the interceptor.
    }
}
```

---

## Verification

| Step | Test | Expected Outcome |
|------|------|-----------------|
| 1 | `dotnet build TheChandlery.sln` | Build succeeds |
| 2 | Open generic transpiler smoke test: patch `AbstractEntity<T>.ctor` with logging | Log fires for every entity type (Element, Recipe, Verb, etc.) |
| 3 | Custom entity: mod DLL with `[FucineImportable("testitems")] class TestItem : AbstractEntity<TestItem>` + `{ "testitems": [ { "id": "test_1" } ] }` JSON | Entity loads into Compendium, accessible via `Watchman.Get<Compendium>().GetEntityById<TestItem>("test_1")` |
| 4 | Custom property: `Wheel.ClaimProperty<Element, string>("myProp")` + set `"myProp": "hello"` in element JSON | `element.RetrieveProperty<string>("myProp")` returns `"hello"` |
| 5 | Default value: custom property omitted from JSON | `element.RetrieveProperty<int>("myProp")` returns `0` (the default) |
| 6 | Unknown property: unclaimed key in JSON | Logged as warning (not silently swallowed) |
| 7 | Ignored property: `Wheel.AddIgnoredProperty<Element>("legacyField")` + `"legacyField": "x"` in JSON | Silently skipped, no warning |
| 8 | Molding: `Wheel.AddImportMolding<Element>(ed => ed.ValuesTable["computed"] = "val")` | Entity has `computed` property post-import |

---

## Dependencies

- `Lib.Harmony` 2.4.2 (already in project)
- `SecretHistories.Main` (for `AbstractEntity<T>`, `CompendiumLoader`, `EntityTypeDataLoader`)
- `SecretHistories.Fucine` (for `FucineImportable`, `Fucine` attribute base, `ImportMethods`, `AbstractImporter`)
- `SecretHistories.Interfaces` (for `IEntityWithId`)
- `UnityEngine.CoreModule` (for `Application.persistentDataPath`)
