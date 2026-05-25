using System;
using System.Linq;
using HarmonyLib;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Infrastructure;
using SecretHistories.Services;
using SecretHistories.Spheres;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse;

internal static class Lionsmith
{
    internal static void Enact(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(TokenCreationCommand), "Execute",
                new[] { typeof(Context), typeof(Sphere) }),
            prefix: new HarmonyMethod(typeof(Lionsmith), nameof(OnTokenCreationCommandExecute))
        );

        harmony.Patch(
            AccessTools.Method(typeof(GameGateway), "PopulateEnvironment"),
            postfix: new HarmonyMethod(typeof(Lionsmith), nameof(OnEnvironmentPopulated))
        );

        Debug.Log("Chandlery Lionsmith: Patches applied");
    }

    private static void OnTokenCreationCommandExecute(TokenCreationCommand __instance, Sphere sphere)
    {
        if (__instance.Payload is PopulateTerrainFeatureCommand ptfc)
        {
            if (!TerrainRegistry.HasAny())
                TerrainRegistry.LoadAll();

            var def = TerrainRegistry.Get(ptfc.Id);
            if (def != null)
            {
                var existing = Watchman.Get<HornedAxe>().FindSingleOrDefaultTokenById(ptfc.Id);
                if (existing == null || !existing.IsValid())
                {
                    try
                    {
                        new TerrainFactory().CreateForLoad(def, sphere);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Chandlery Lionsmith ERROR] Failed to restore room '{ptfc.Id}' from save: {ex.Message}");
                    }
                }
            }
        }
    }

    private static void OnEnvironmentPopulated()
    {
        MotherOfAnts.LogCanvasesSkipTerrain();

        try
        {
            TerrainRegistry.LoadAll();
            if (!TerrainRegistry.HasAny())
                return;

            Debug.Log($"Chandlery Lionsmith: Creating {TerrainRegistry.GetAll().Count()} custom rooms...");

            var factory = new TerrainFactory();
            foreach (var def in TerrainRegistry.GetAll())
                factory.Create(def);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chandlery Lionsmith: Error during terrain injection: {ex.Message}\n{ex.StackTrace}");
        }
    }
}