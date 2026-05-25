using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using SecretHistories.Entities;
using SecretHistories.Fucine.DataImport;

namespace TheHouse.Wheel;

internal static class WheelChain
{
    private const string ExtendsKey = "extends";
    private const string DollarExtendsKey = "$extends";

    internal static void Enact(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(EntityData), nameof(EntityData.ApplyDataToCollection)),
            prefix: AccessTools.Method(typeof(WheelChain), nameof(ApplyDataToCollectionPrefix)));

        harmony.Patch(
            original: AccessTools.Method(typeof(Element), "InheritFrom", new[] { typeof(Compendium), typeof(Element) }),
            postfix: AccessTools.Method(typeof(WheelChain), nameof(InheritElementPostfix)));

        harmony.Patch(
            original: AccessTools.Method(typeof(Recipe), "InheritFrom", new[] { typeof(Recipe) }),
            postfix: AccessTools.Method(typeof(WheelChain), nameof(InheritRecipePostfix)));
    }

    private static void ApplyDataToCollectionPrefix(EntityData __instance)
    {
        if (__instance.ValuesTable.ContainsKey(ExtendsKey) && !__instance.ValuesTable.ContainsKey(DollarExtendsKey))
        {
            __instance.ValuesTable[DollarExtendsKey] = __instance.ValuesTable[ExtendsKey];
            __instance.ValuesTable.Remove(ExtendsKey);
        }
    }

    private static void InheritElementPostfix(Element __instance, Element inheritFromElement)
    {
        WheelStore.InheritClaimedProperties(__instance, inheritFromElement);
    }

    private static void InheritRecipePostfix(Recipe __instance, Recipe inheritFromRecipe)
    {
        WheelStore.InheritClaimedProperties(__instance, inheritFromRecipe);
    }
}
