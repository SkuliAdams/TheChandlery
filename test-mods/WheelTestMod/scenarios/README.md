# WheelTestMod — Test Scenarios

This mod validates all phases of the Wheel data loading system. Each scenario
documents what it tests, which files are involved, and what to check in
`Player.log` to confirm success or diagnose failure.

---

## Before Running

1. Build TheChandlery: `dotnet build TheChandlery.sln` (from solution root)
2. Build WheelTestMod: `dotnet build test-mods\WheelTestMod\WheelTestMod.csproj`
3. Enable **WheelTestMod** and **GHIBRI** in the Book of Hours mod manager
4. Launch the game and check `Player.log`

> The build deploys `WheelTestMod.dll` to `$(BOHModsPath)/WheelTestMod/dll/`
> and copies content JSON to `$(BOHModsPath)/WheelTestMod/content/`.

---

## Scenario 1 — Custom Entity Types (WheelTypes)

**Validates**: WheelTypes transpiler injects `[FucineImportable]` types from
mod assemblies into the CompendiumLoader's type registry.

**Files**: `TestEntities.cs`, `content/wheel-test-items.json`,
`content/wheel-test-configs.json`

**JSON top-level keys**: `wheel_test_items`, `wheel_test_configs`

**What happens**:
- `TestItem` class is annotated with `[FucineImportable("wheel_test_items")]`
- `TestConfig` class is annotated with `[FucineImportable("wheel_test_configs")]`
- Both classes have `AbstractEntity<T>` constructors and `OnPostImportForSpecificEntity`
- The transpiler scans WheelTestMod's assembly, finds these attributes, and
  registers them with the game's loader

**Success indicators** (in `Player.log`):
- No errors containing `FAILED TO IMPORT` or "unknown entity tag"
- `TestItem.OnPostImportForSpecificEntity` logs:
  ```
  [WheelTestMod] Loaded TestItem 'wheel.test.simple_item': ...
  [WheelTestMod] Loaded TestItem 'wheel.test.magical_gem': ...
  [WheelTestMod] Loaded TestItem 'wheel.test.heavy_rock': ...
  ```

**Manual verification** (via debug console or code):
```csharp
var testItem = Watchman.Get<Compendium>().GetEntityById<TestItem>("wheel.test.magical_gem");
// testItem.Value == 100, testItem.IsMagical == true
```

---

## Scenario 2 — Custom Properties on Existing Entities (WheelStore + WheelIntercept)

**Validates**: WheelIntercept transpiler intercepts claimed property keys from
EntityData and stores them in WheelStore before the constructor pushes unknowns.

**Files**: `Init.cs` (calls `Wheel.ClaimProperty`), `content/wheel-elements.json`

**Claims registered**:
| Entity Type | Property Name | CLR Type | Default |
|---|---|---|---|
| `Element` | `wheelTestString` | `string` | `"default-string"` |
| `Element` | `wheelTestInt` | `int` | `42` |
| `Element` | `wheelTestBool` | `bool` | `false` |
| `Recipe` | `wheelRecipeNote` | `string` | `""` |
| `Verb` | `wheelVerbTag` | `string` | `"untagged"` |

**What happens**:
- For `wheel.test.element.basic`: all three custom properties are set in JSON
  → `RetrieveProperty<string>("wheelTestString")` returns `"hello from wheel"`
  → `RetrieveProperty<int>("wheelTestInt")` returns `100`
  → `RetrieveProperty<bool>("wheelTestBool")` returns `true`
- For `wheel.test.element.defaults`: no custom properties in JSON
  → `RetrieveProperty<string>("wheelTestString")` returns `"default-string"`
  → `RetrieveProperty<int>("wheelTestInt")` returns `42`
  → `RetrieveProperty<bool>("wheelTestBool")` returns `false`

**Success indicators**:
- No "UNKNOWN PROPERTY" warnings for `wheelteststring`, `wheeltestint`,
  `wheeltestbool`, or `wheelrecipenote`
- Unknown property "unknownfield" (if added to test JSON) IS logged as warning

---

## Scenario 3 — Ignored Properties (WheelIgnore)

**Validates**: `Wheel.AddIgnoredProperty<T>()` suppresses a claimed property
so it is silently skipped during import (not stored, not logged as unknown).

**Files**: `Init.cs` (calls `Wheel.AddIgnoredProperty<Element>("wheelLegacyField")`),
`content/wheel-elements.json` (element `wheel.test.element.ignored_field`)

**What happens**:
- `wheelLegacyField` is registered as an ignored property for `Element`
- The element JSON has `"wheelLegacyField": "this should be silently skipped"`
- The interceptor sees it, checks `WheelIgnore.Ignores()`, and skips it

**Success indicators**:
- No "UNKNOWN PROPERTY" warning for `wheellecagyfield` (with any casing)
- `HasCustomProperty("wheelLegacyField")` returns `false`
- `RetrieveProperty<string>("wheelLegacyField")` returns `null`

---

## Scenario 4 — Import Molding (WheelStore molding)

**Validates**: `Wheel.AddImportMolding<T>()` pre-processes EntityData before
the Fucine import runs, enabling legacy syntax conversion.

**Files**: `Init.cs` (calls `Wheel.AddImportMolding<Element>(PreProcessElementData)`),
`content/wheel-elements.json` (element `wheel.test.element.legacy_format`)

**Molding logic**:
```csharp
private static void PreProcessElementData(EntityData entityData)
{
    if (entityData.ValuesTable.ContainsKey("wheelLegacyFormat"))
    {
        entityData.ValuesTable["wheelTestString"] = entityData.ValuesTable["wheelLegacyFormat"];
        entityData.ValuesTable.Remove("wheelLegacyFormat");
    }
}
```

**What happens**:
- The legacy element has `"wheellegacyformat": "converted from legacy"` (no `wheelteststring`)
- The molding copies the value to `wheelTestString`
- `RetrieveProperty<string>("wheelTestString")` returns `"converted from legacy"`

**Success indicators**:
- `wheel.test.element.legacy_format`'s `wheelteststring` equals `"converted from legacy"`
- No "UNKNOWN PROPERTY" warning for `wheellegacyformat`
- The key `wheelLegacyFormat` is not in `ValuesTable` after import

---

## Scenario 5 — Custom Property Inheritance (WheelChain)

**Validates**: Custom properties survive BoH's native `$extends`/`$derives`
EntityData operations and are available on derived entities.

**Files**: `content/wheel-inheritance.json`

**JSON structure**:
```
wheel.test.element.base_entity:
  wheelteststring: "inherited value"
  wheeltestint: 999

wheel.test.element.derived_entity:
  extends: "wheel.test.element.base_entity"
  wheelteststring: "overridden value"
  // wheeltestint NOT set — should inherit 999 from base
```

**What happens**:
- `derived_entity` extends `base_entity` via BoH's native `$extends`
- The EntityData pipeline copies all properties from base to derived
- Then `derived_entity`'s `wheelteststring` overrides the inherited value
- `derived_entity`'s `wheeltestint` is inherited from base (not overridden)

**Success indicators**:
- `derived_entity.RetrieveProperty<string>("wheelTestString")` returns `"overridden value"`
- `derived_entity.RetrieveProperty<int>("wheelTestInt")` returns `999` (inherited)

---

## Scenario 6 — Custom Importers (WheelFucine)

**Validates**: The `WheelFucineNullable` and custom list/dict importers work
correctly in custom entity classes.

**Requires**: A second test entity class that uses `[WheelFucineNullable]` etc.

**Not yet implemented** — add a test entity after WheelFucine is built:
```csharp
public class NullableTestItem : AbstractEntity<NullableTestItem>
{
    [WheelFucineNullable]
    public int? NullableInt { get; set; }

    [WheelFucineNullable]
    public float? NullableFloat { get; set; }
}
```

---

## Quick Reference: Expected Log Lines

| Phase | Log pattern | Meaning |
|-------|-------------|---------|
| 1 | `[WheelTestMod] Loaded TestItem '...'` | Custom entity loaded |
| 1 | `UNKNOWN ENTITY TAG` | WheelTypes transpiler not working |
| 2 | `UNKNOWN PROPERTY 'wheelteststring'` | WheelIntercept not intercepting |
| 2 | `[WheelTestMod] Registration complete.` | Init ran successfully |
| 3 | `UNKNOWN PROPERTY 'wheellecagyfield'` | WheelIgnore not suppressing |
| 4 | `wheel.test.element.legacy_format` has correct prop | Molding applied |
| 5 | Inherited custom property values correct | Chain working |
