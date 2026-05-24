# The Roost Machine — Data Loading Capability Analysis

> **Project**: The Roost Machine (v1.0.0, `Roost` namespace)  
> **Target**: Cultist Simulator (.NET Framework 4.7.2)  
> **Dependency**: Harmony 2.x  
> **Module codename**: Beachcomber  
> **Codebase**: `C:\Programming\Modding\RiderProjects\TheRoostMachine\TheRoost`

---

## Overview

The Roost Machine is a Harmony 2.x modding framework for **Cultist Simulator**. Its data loading subsystem, **Beachcomber**, extends the game's native **Fucine** JSON content loading pipeline — the same pipeline used to load all core entity types (Elements, Recipes, Verbs, Slots, etc.).

Entry point: `TheRoostMachine.Initialise()` → `Beachcomber.Enact()` orchestrates three sub-modules:

| Module | File | Role |
|--------|------|------|
| **Cuckoo** | `BeachcomberLoader.cs` | Inject custom entity types into the game's loader |
| **CuckooJr** | `BeachcomberInheritance.cs` | Propagate custom properties through entity inheritance |
| **Usurper** | `BeachcomberUsurper.cs` | Intercept the native import pipeline for custom properties |

---

## Strategy 1 — Custom Entity Types (Cuckoo)

### Goal
Define entirely new entity classes that load from JSON on par with native types like `Element` or `Recipe`.

### How it works
A Harmony **transpiler** on `CompendiumLoader.PopulateCompendium()` injects a call to `InsertCustomTypesForLoading()` just before `Compendium.InitialiseForEntityTypes()`. This method:

1. Scans all loaded **mod assemblies** (identified by file path — either in `Application.persistentDataPath` or `steamapps/workshop/content`)
2. For each assembly, finds all types with the `[FucineImportable("tag")]` attribute
3. Adds them to the game's internal `_typesToLoad` list and creates corresponding `EntityTypeDataLoader` instances

### Requirements for custom entity classes

```csharp
[FucineImportable("beachcomberexample")]           // JSON top-level key
public class ExampleFucineClass : AbstractEntity<ExampleFucineClass>
{
    [FucineValue(DefaultValue = 0)]
    public int Number { get; set; }

    [FucineValue(DefaultValue = "", Localise = true)]
    public string Text { get; set; }

    // Must implement this constructor
    public ExampleFucineClass(EntityData importDataForEntity, ContentImportLog log)
        : base(importDataForEntity, log) { }

    // Optional post-import hook
    protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium) { }
}
```

JSON would then be loaded from files like:
```json
{ "beachcomberexample": [ { "id": "my_entity", "number": 5, "text": "Hello" } ] }
```

---

## Strategy 2 — Custom Properties on Existing Entity Types (Usurper + Hoard)

### Goal
Add new JSON-serializable properties to **native** entity types (e.g., add a custom property to `Element` or `Recipe`) without modifying the game's assemblies.

### API

```csharp
// Register a custom property
Machine.ClaimProperty<Element, string>("myCustomProperty");
Machine.ClaimProperty<Element, int>("myIntProperty", defaultValue: 42);

// Batch register
Machine.ClaimProperties<Element>(new Dictionary<string, Type> {
    { "prop1", typeof(string) },
    { "prop2", typeof(int)   }
});

// Runtime access (extension methods on IEntityWithId)
element.RetrieveProperty<string>("myCustomProperty");
element.SetCustomProperty("myCustomProperty", "value");
element.HasCustomProperty("myCustomProperty");
element.RemoveProperty("myCustomProperty");
element.TryRetrieveProperty<int>("myIntProperty", out int value);
```

### How it works

**Registration** — `Hoard.AddCustomProperty<TEntity, TProperty>()` stores property metadata (`type`, `defaultValue`, `localize`, optional `Fucine` attribute importer) in a static dictionary keyed by entity type.

**The Harmony transpiler** — `Usurper.AbstractEntityConstructorTranspiler()` patches the `AbstractEntity<Element>` constructor. After the native constructor completes, execution falls through to `ImportRootEntity<T>()` which runs this sequence:

1. **Set ID** — extracts `"id"` or `UniqueId` from `EntityData`
2. **Apply moldings** — runs pre-processors registered via `Machine.AddImportMolding<T>()`
3. **Intercept claimed properties** — `Hoard.InterceptClaimedProperties()` matches claimed property keys in the JSON `ValuesTable`, imports them, and removes them so the native pipeline doesn't choke on them
4. **Import declared properties** — runs standard Fucine import for the entity's own `[FucineValue]`-annotated properties
5. **CustomSpec hook** — calls `ICustomSpecEntity.CustomSpec()` if the entity implements it
6. **Push unknowns** — logs any remaining unrecognized keys

### Storage model

Custom properties are NOT stored on the entity object itself. They're held in a separate static dictionary:

```
Hoard.loadedData: IEntityWithId → Dictionary<string, object>
```

This avoids reflection-based field injection and keeps custom data cleanly segregated.

---

## Strategy 3 — Custom Importers and Fucine Attribute Extensions

The Roost Machine provides new Fucine attribute types that support richer import behavior than the vanilla game:

| Attribute | Importer Class | Behavior |
|-----------|---------------|----------|
| `[FucineEverValue]` | `PropertyPanImporter` | Auto-detects type, uses default Fucine import. Default value accepts `params object[]`. |
| `[FucineConstruct]` | `ConstructorPanImporter` | Calls `ImportMethods.ImportWithConstructor()` — constructs objects from array arguments (e.g., `[0.5, 100]` → `Vector2`). |
| `[FucineCustomList(typeof(PathImporter))]` | `CustomListPanImporter` | Lists with custom entry-level importer. |
| `[FucineCustomDict(KeyImporter, ValueImporter)]` | `CustomDictPanImporter` | Dictionaries with custom key/value importers. |
| `[FucineNullable]` | `NullableImporter` | Nullable value types; `"null"` in JSON maps to C# `null`. |
| — (replaces vanilla) | `ExtendedPathImporter` | Replaces the vanilla `FucinePathValue.CreateImporterInstance()` via prefix patch. Uses `TwinsParser.ParseSpherePath()` for extended path syntax. |

**File**: `BeachcomberFucineExtensions.cs`

---

## Inheritance of Custom Properties (CuckooJr)

Harmony transpilers on `Element.InheritFrom()` and `Recipe.InheritFrom()` merge custom properties during entity inheritance:

- **Scalar values**: child's value takes precedence; parent's value is ignored if child already has one
- **Lists**: entries from the parent are appended to the child's list
- **Dictionaries**: recursive deep merge — existing keys are merged recursively, new keys are added

**File**: `BeachcomberInheritance.cs`

---

## Ignoring Properties / Entity Groups (Ostrich)

```csharp
Machine.AddIgnoredProperty<Element>("obsoleteProperty");
Machine.AddIgnoredEntityGroup<Element>("dontload");
```

Used to tell the importer to skip certain properties or entire entity groups during loading.

**File**: `BeachcomberIgnore.cs`

---

## Entity Lifecycle Hooks

Entities can implement optional interfaces for fine-grained import control:

| Interface | Method | When Called |
|-----------|--------|-------------|
| `IMalleable` | `Mold(EntityData, ContentImportLog)` | After ID is set, **before** any properties are imported |
| `ICustomSpecEntity` | `CustomSpec(EntityData, ContentImportLog)` | After all properties are imported, **before** unknown keys are logged |
| `IQuickSpecEntity` | `QuickSpec(string)` | When entity is defined as a single string instead of a full JSON object |

Precedence in `ImportRootEntity<T>`:
```
Mold() → InterceptClaimedProperties → Fucine import → CustomSpec() → PushUnknowns
```

---

## Architectural Diagram

```
                   TheRoostMachine.Initialise()
                             │
                   ┌─────────────────────┐
                   │  Beachcomber.Enact() │
                   └─────────┬───────────┘
                             │
            ┌────────────────┼────────────────┐
            │                │                │
       Cuckoo.Enact()   CuckooJr.Enact()  Usurper.Overthrow...
            │                │                │
            ▼                ▼                ▼
   PopulateCompendium   Element.InheritFrom  AbstractEntity<Element>
   (transpiler)         Recipe.InheritFrom   constructor (transpiler)
            │           (transpilers)               │
            ▼                                       ▼
   InsertCustomTypes     MergeCustomProperty   ImportRootEntity<T>
   ForLoading()          (smart merge:         ├─ Mold(EntityData)
            │            scalar/list/dict)     ├─ Hoard.InterceptClaimed
   Custom types added                           ├─ Standard Fucine import
   to _typesToLoad                              ├─ ICustomSpecEntity.CustomSpec
   and fucineLoaders                            └─ Push unknown keys
                                                       │
                                                       ▼
                                               Hoard (static dict)
                                          IEntityWithId → {property→value}
```

---

## Key Files Summary

| File | Component | Lines |
|------|-----------|-------|
| `BeachcomberLoader.cs` | Cuckoo (entity types) + Hoard (property store) + Machine API | 385 |
| `BeachcomberUsurper.cs` | Import pipeline interception, moldings, ImportRootEntity | 164 |
| `BeachcomberFucineExtensions.cs` | Custom importer types, new Fucine attributes | 205 |
| `BeachcomberInheritance.cs` | Custom property inheritance merging | 122 |
| `BeachcomberIgnore.cs` | Property/entity group ignore system | 64 |
| `BeachcomberImporter.cs` | Pantiment helper, Machine.ConvertTo | 33 |
| `BeachcomberExamples.cs` | Example Fucine entity class with all attribute types | 120 |
| `MainHall.cs` | Entry point, enactor orchestration, Machine partial class root | 134 |
| `HarmonyPatchWrapper.cs` | Reflection helpers, HarmonyMask, Machine.Patch/Schedule | 475 |
| `Birdsong.cs` | Logging system, Rooster coroutine scheduler | 136 |
