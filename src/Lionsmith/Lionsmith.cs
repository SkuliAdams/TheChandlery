using System;
using System.Linq;
using HarmonyLib;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Infrastructure;
using SecretHistories.Services;
using SecretHistories.Spheres;
using SecretHistories.Tokens.Payloads;
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

        harmony.Patch(
            AccessTools.Method(typeof(TerrainFeature), "Unshroud",
                new[] { typeof(bool) }),
            postfix: new HarmonyMethod(typeof(Lionsmith), nameof(OnUnshroudPostfix))
        );

        harmony.Patch(
            AccessTools.Method(typeof(Situation), "ExecuteCurrentRecipe"),
            postfix: new HarmonyMethod(typeof(Lionsmith), nameof(OnRecipeExecuted))
        );

        harmony.Patch(
            AccessTools.Method(typeof(Token), "CanBeDragged"),
            prefix: new HarmonyMethod(typeof(Lionsmith), nameof(OnCanBeDraggedPrefix))
        );
    }

    private static void OnTokenCreationCommandExecute(TokenCreationCommand __instance, Sphere sphere)
    {
        if (__instance.Payload is PopulateTerrainFeatureCommand ptfc)
        {
            if (!TerrainRegistry.HasAny())
            {
                TerrainRegistry.LoadAll();
                RecipeRegistrar.RegisterAll();
            }

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

    private static void OnUnshroudPostfix(TerrainFeature __instance)
    {
        if (__instance == null || string.IsNullOrEmpty(__instance.Id))
            return;

        if (!TerrainRegistry.TryGetConnections(__instance.Id, out var connectedIds))
            return;

        var ha = Watchman.Get<HornedAxe>();
        foreach (var connectedId in connectedIds)
        {
            var token = ha.FindSingleOrDefaultTokenById(connectedId);
            if (token == null || !token.IsValid())
            {
                Debug.LogWarning($"Chandlery Lionsmith: Connected room '{connectedId}' not found from '{__instance.Id}'");
                continue;
            }

            if (token.Payload is TerrainFeature connectedRoom)
            {
                connectedRoom.Unseal();
                Debug.Log($"Chandlery Lionsmith: Unsealed connected room '{connectedId}' from '{__instance.Id}'");
            }
        }
    }

    private static void OnRecipeExecuted(Situation __instance)
    {
        var recipe = __instance.GetCurrentRecipe();
        if (recipe == null || recipe.ActionId != "terrain.unlock")
            return;

        var roomId = recipe.Id;
        if (roomId.StartsWith("terrain."))
            roomId = roomId.Substring("terrain.".Length);

        if (!TerrainRegistry.Has(roomId))
            return;

        var token = Watchman.Get<HornedAxe>().FindSingleOrDefaultTokenById(roomId);
        if (token?.Payload is TerrainFeature tf)
        {
            var fx = new EnviroFxCommand(roomId + ".open", "1");
            Watchman.Get<LocalNexus>().BroadcastFx(fx);
        }
    }

    private static bool OnCanBeDraggedPrefix(Token __instance, ref bool __result)
    {
        if (__instance.GetComponent<NoDragMarker>() != null)
        {
            __result = false;
            return false;
        }
        return true;
    }

    private static void OnEnvironmentPopulated()
    {
        try
        {
            if (!TerrainRegistry.HasAny())
                TerrainRegistry.LoadAll();
            if (!TerrainRegistry.HasAny())
                return;

            var newDefs = TerrainRegistry.GetAllNew().ToList();
            var overrideDefs = TerrainRegistry.GetAllOverrides().ToList();

            RecipeRegistrar.RegisterAll();

            var factory = new TerrainFactory();
            foreach (var def in newDefs)
                factory.Create(def);

            var patcher = new VanillaRoomPatcher();
            foreach (var def in overrideDefs)
            {
                patcher.Patch(def);

                if (def.ConnectedTo != null && def.ConnectedTo.Count > 0)
                    TerrainRegistry.RegisterConnection(def.Id, def.ConnectedTo);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chandlery Lionsmith: Error during terrain injection: {ex.Message}\n{ex.StackTrace}");
        }
    }
}