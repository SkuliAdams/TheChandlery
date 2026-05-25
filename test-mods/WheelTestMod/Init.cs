using SecretHistories.Entities;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using UnityEngine;
using TheHouse.Wheel;

public static class WheelTestMod
{
    private static bool _initialised;

    public static void Initialise()
    {
        if (_initialised)
            return;

        Debug.Log("[WheelTestMod] Initialising — registering Wheel test configurations...");

        Wheel.ClaimProperty<Element, string>("wheelTestString", defaultValue: "default-string");
        Wheel.ClaimProperty<Element, int>("wheelTestInt", defaultValue: 42);
        Wheel.ClaimProperty<Element, bool>("wheelTestBool", defaultValue: false);
        Wheel.ClaimProperty<Recipe, string>("wheelRecipeNote", defaultValue: "");
        Wheel.ClaimProperty<Verb, string>("wheelVerbTag", defaultValue: "untagged");
        Wheel.ClaimProperty<Element, UnityEngine.Vector2>("wheelFucineVector",
            false, default(UnityEngine.Vector2), new WheelFucineEverValue());
        Wheel.ClaimProperty<Element, int?>("wheelNullableInt",
            false, null, new WheelFucineNullable());
        Wheel.ClaimProperty<Element, FucinePath>("wheelPath",
            false, FucinePath.Current(), new FucinePathValue());
        Wheel.IgnoreProperty<Element>("wheelLegacyField");
        Wheel.IgnoreEntityGroup("wheel_test_configs");
        Wheel.AddImportMolding<Element>(PreProcessElementData);
        Wheel.AddImportMolding<Element>(LogInheritanceData);
        Wheel.AddImportMolding<Recipe>(LogInheritanceData);

        Debug.Log("[WheelTestMod] Registration complete. Claimed: Element(wheelTestString, wheelTestInt, wheelTestBool), Recipe(wheelRecipeNote), Verb(wheelVerbTag). Ignored: Element(wheelLegacyField). Group-ignored: wheel_test_configs.");

        _initialised = true;
    }

    private static void PreProcessElementData(EntityData entityData)
    {
        if (entityData.ValuesTable.ContainsKey("wheellegacyformat"))
        {
            entityData.ValuesTable["wheelteststring"] = entityData.ValuesTable["wheellegacyformat"];
            entityData.ValuesTable.Remove("wheellegacyformat");
        }
    }

    private static string InheritMode(EntityData ed)
    {
        if (ed.ValuesTable.ContainsKey("inherits"))
            return "RUNTIME(inherits)";
        var id = ed.ValuesTable["id"]?.ToString();
        if (id == "wheel.test.element.derived_entity" || id == "wheel.test.recipe.runtime_inherit")
            return "JSON($extends)";
        if (id == "wheel.test.element.converted_entity")
            return "JSON(extends→$extends)";
        return "OWN";
    }

    private static void LogInheritanceData(EntityData entityData)
    {
        var idObj = entityData.ValuesTable["id"];
        if (idObj == null)
            return;
        var id = idObj.ToString();
        if (id.StartsWith("wheel.test."))
        {
            var wtstr = entityData.ValuesTable.ContainsKey("wheelteststring")
                ? entityData.ValuesTable["wheelteststring"].ToString() : "(missing)";
            var wtint = entityData.ValuesTable.ContainsKey("wheeltestint")
                ? entityData.ValuesTable["wheeltestint"].ToString() : "(missing)";
            var wrn = entityData.ValuesTable.ContainsKey("wheelrecipenote")
                ? entityData.ValuesTable["wheelrecipenote"].ToString() : "(missing)";
            var wfv = entityData.ValuesTable.ContainsKey("wheelfucinevector")
                ? entityData.ValuesTable["wheelfucinevector"].ToString() : "(missing)";
            var wni = entityData.ValuesTable.ContainsKey("wheelnullableint")
                ? entityData.ValuesTable["wheelnullableint"].ToString() : "(missing)";
            var wp = entityData.ValuesTable.ContainsKey("wheelpath")
                ? entityData.ValuesTable["wheelpath"].ToString() : "(missing)";
            var mode = InheritMode(entityData);
            Debug.Log($"[WheelTestMod] Inheritance [{mode}] {id} -> " +
                $"wheelteststring='{wtstr}', wheeltestint={wtint}, " +
                $"wheelrecipenote='{wrn}', wheelfucinevector='{wfv}', " +
                $"wheelnullableint={wni}, wheelpath='{wp}'");
        }
    }
}
