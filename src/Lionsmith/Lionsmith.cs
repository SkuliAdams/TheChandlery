using HarmonyLib;
using SecretHistories.Infrastructure;
using UnityEngine;

namespace TheHouse;

internal static class Lionsmith
{
    internal static void Enact(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(GameGateway), "PopulateEnvironment"),
            postfix: new HarmonyMethod(typeof(Lionsmith), nameof(OnEnvironmentPopulated))
        );

        Debug.Log("Chandlery Lionsmith: Patches applied");
    }

    private static void OnEnvironmentPopulated()
    {
        Debug.Log("Chandlery: === Terrain hierarchy dump ===");
        MotherOfAnts.LogAllCanvases();
    }
}