using System;
using System.Linq;
using HarmonyLib;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Events;
using SecretHistories.Infrastructure;
using SecretHistories.Services;
using SecretHistories.Spheres;
using SecretHistories.Tokens.Payloads;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.EventSystems;

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
            AccessTools.Method(typeof(PhysicalSphere), "TryDisplayDropInteraction",
                new[] { typeof(Token) }),
            prefix: new HarmonyMethod(typeof(Lionsmith), nameof(OnTryDisplayDropInteractionPrefix)),
            postfix: new HarmonyMethod(typeof(Lionsmith), nameof(OnTryDisplayDropInteractionPostfix))
        );

        harmony.Patch(
            AccessTools.Method(typeof(SphereDropCatcher), "OnDrop",
                new[] { typeof(PointerEventData) }),
            prefix: new HarmonyMethod(typeof(Lionsmith), nameof(OnDropCatcherOnDropPrefix))
        );

        harmony.Patch(
            AccessTools.Method(typeof(SphereDropCatcher), "TryDisplayDropInteraction",
                new[] { typeof(Token) }),
            prefix: new HarmonyMethod(typeof(Lionsmith), nameof(OnDropCatcherTryDisplayPrefix)),
            postfix: new HarmonyMethod(typeof(Lionsmith), nameof(OnDropCatcherTryDisplayPostfix))
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

    private static void OnTryDisplayDropInteractionPrefix(PhysicalSphere __instance, Token forToken)
    {
        if (__instance.name.StartsWith("slot_") || __instance.name.StartsWith("shelf_")
            || __instance.name.StartsWith("comfort_") || __instance.name.StartsWith("workstation_"))
            Debug.Log($"Chandlery DIAG: PhysicalSphere.TryDisplayDropInteraction enter '{__instance.name}' token='{forToken.PayloadId}'");
    }

    private static void OnTryDisplayDropInteractionPostfix(PhysicalSphere __instance, bool __result)
    {
        if (__instance.name.StartsWith("slot_") || __instance.name.StartsWith("shelf_")
            || __instance.name.StartsWith("comfort_") || __instance.name.StartsWith("workstation_"))
            Debug.Log($"Chandlery DIAG: PhysicalSphere.TryDisplayDropInteraction -> {__result} for '{__instance.name}'");
    }

    private static void OnDropCatcherTryDisplayPrefix(SphereDropCatcher __instance, Token forToken)
    {
        if (__instance.Sphere != null && (__instance.Sphere.name.StartsWith("slot_")
            || __instance.Sphere.name.StartsWith("shelf_") || __instance.Sphere.name.StartsWith("comfort_")
            || __instance.Sphere.name.StartsWith("workstation_")))
            Debug.Log($"Chandlery DIAG: SphereDropCatcher.TryDisplayDropInteraction enter sphere='{__instance.Sphere.name}' token='{forToken.PayloadId}'");
    }

    private static void OnDropCatcherTryDisplayPostfix(SphereDropCatcher __instance, bool __result)
    {
        if (__instance.Sphere != null && (__instance.Sphere.name.StartsWith("slot_")
            || __instance.Sphere.name.StartsWith("shelf_") || __instance.Sphere.name.StartsWith("comfort_")
            || __instance.Sphere.name.StartsWith("workstation_")))
            Debug.Log($"Chandlery DIAG: SphereDropCatcher.TryDisplayDropInteraction -> {__result} for '{__instance.Sphere.name}'");
    }

    private static void OnDropCatcherOnDropPrefix(SphereDropCatcher __instance, PointerEventData eventData)
    {
        if (__instance.Sphere != null && (__instance.Sphere.name.StartsWith("slot_")
            || __instance.Sphere.name.StartsWith("shelf_") || __instance.Sphere.name.StartsWith("comfort_")
            || __instance.Sphere.name.StartsWith("workstation_")))
            Debug.Log($"Chandlery DIAG: SphereDropCatcher.OnDrop enter sphere='{__instance.Sphere.name}'");
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

    private static void OnEnvironmentPopulated()
    {
        MotherOfAnts.LogTerrainDetails();
        MotherOfAnts.LogRoomSpheres("terrain.pantry");

        try
        {
            TerrainRegistry.LoadAll();
            if (!TerrainRegistry.HasAny())
                return;

            Debug.Log($"Chandlery Lionsmith: Creating {TerrainRegistry.GetAll().Count()} custom rooms...");

            var factory = new TerrainFactory();
            foreach (var def in TerrainRegistry.GetAll())
                factory.Create(def);

            MotherOfAnts.LogChoreographers("watchmanstower1");
            MotherOfAnts.LogChoreographers("thechandlery");
            MotherOfAnts.LogChoreographers("thechandlery_gallery");
            MotherOfAnts.LogChoreographers("thechandlery_spire");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chandlery Lionsmith: Error during terrain injection: {ex.Message}\n{ex.StackTrace}");
        }
    }
}