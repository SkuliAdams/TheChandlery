using System;
using System.Linq;
using HarmonyLib;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Infrastructure;
using SecretHistories.Spheres;
using SecretHistories.Tokens.Payloads;
using SecretHistories.UI;
using TheHouse.Colonel;
using ColonelApi = TheHouse.Colonel.Colonel;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse;

internal static class Lionsmith
{
    internal static void Enact(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(TokenCreationCommand), "Execute",
                [typeof(Context), typeof(Sphere)]),
            prefix: new HarmonyMethod(typeof(Lionsmith), nameof(OnTokenCreationCommandExecute))
        );

        harmony.Patch(
            AccessTools.Method(typeof(GameGateway), "PopulateEnvironment"),
            postfix: new HarmonyMethod(typeof(Lionsmith), nameof(OnEnvironmentPopulated))
        );

        harmony.Patch(
            AccessTools.Method(typeof(TerrainFeature), "Unshroud",
                [typeof(bool)]),
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
        
        if (token?.Payload is not TerrainFeature) return;
        
        var fx = new EnviroFxCommand(roomId + ".open", "1");
        Watchman.Get<LocalNexus>().BroadcastFx(fx);
    }

    private static bool OnCanBeDraggedPrefix(Token __instance, ref bool __result)
    {
        if (__instance.GetComponent<NoDragMarker>() == null) return true;
        
        __result = false;
        return false;
    }

    // Most fogs are in the MistsAndSmokes layer, but the cucurbit fog is an exception.
    // Move it up so custom rooms in its vicinity will visually be below the fog.
    private static void RaiseCucurbitFogToFogLayer()
    {
        var fogLayerId = SortingLayer.NameToID("MistsAndSmokes");

        foreach (var img in UnityEngine.Object.FindObjectsOfType<Image>(true))
        {
            if (img.name != "ShroudedObscurerImage1")
                continue;

            if (!HasAncestorNamed(img.transform, "cucurbitbridge_token"))
                continue;

            var container = img.transform.parent;
            if (container == null || (container.name != "Shrouded" && container.name != "Sealed"))
                continue;

            var go = container.gameObject;
            if (go.GetComponent<Canvas>() != null)
                continue;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = fogLayerId;
            go.AddComponent<GraphicRaycaster>();
            Debug.Log($"Chandlery Lionsmith: Raised Cucurbit fog container '{go.name}' to 'MistsAndSmokes'");
        }
    }

    private static bool HasAncestorNamed(Transform t, string name)
    {
        var cur = t.parent;
        while (cur != null)
        {
            if (cur.name == name)
                return true;
            cur = cur.parent;
        }
        return false;
    }

    private static void OnEnvironmentPopulated()
    {
        try
        {
            RaiseCucurbitFogToFogLayer();

            if (!TerrainRegistry.HasAny())
                TerrainRegistry.LoadAll();
            if (!TerrainRegistry.HasAny())
                return;

            var newDefs = TerrainRegistry.GetAllNew().ToList();
            var overrideDefs = TerrainRegistry.GetAllOverrides().ToList();

            RecipeRegistrar.RegisterAll();

            var factory = new TerrainFactory();
            foreach (var def in newDefs)
            {
                factory.Create(def);
                RegisterBackground(def);
            }

            var patcher = new VanillaRoomPatcher();
            foreach (var def in overrideDefs)
            {
                patcher.Patch(def);

                if (def.ConnectedTo is { Count: > 0 })
                    TerrainRegistry.RegisterConnection(def.Id, def.ConnectedTo);
            }

            ColonelApi.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chandlery Lionsmith: Error during terrain injection: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void RegisterBackground(CustomTerrainDefinition def)
    {
        var bg = def.Background;
        if (bg == null || bg.IsEmpty)
            return;

        ColonelApi.AddMapFeature(new MapFeatureDefinition(def.Id + ".background")
        {
            Sprite = bg.Sprite,
            Layer = "background",
            Width = bg.Width,
            Height = bg.Height,
            PosX = (def.PosX ?? 0f) + (bg.OffsetX ?? 0f),
            PosY = (def.PosY ?? 0f) + (bg.OffsetY ?? 0f),
            Scale = bg.Scale,
            ClickBlocking = false
        });
    }
}