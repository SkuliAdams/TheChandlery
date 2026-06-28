using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SecretHistories.Abstract;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Manifestations;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse;

internal static class WorkstationSpriteFallback
{
    internal static void Enact(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(FitmentWorkstationManifestation), "Initialise"),
            postfix: new HarmonyMethod(typeof(WorkstationSpriteFallback), nameof(OnWorkstationInitialised))
        );
    }

    private static void OnWorkstationInitialised(FitmentWorkstationManifestation __instance, IManifestable manifestable)
    {
        var vFabField = typeof(FitmentWorkstationManifestation)
            .GetField("vFab", BindingFlags.Instance | BindingFlags.NonPublic);
        var vFab = vFabField?.GetValue(__instance) as Component;
        if (vFab == null)
            return;

        var rootGo = vFab.gameObject;
        var image = rootGo.GetComponent<Image>();
        if (image == null)
            return;

        var verbId = manifestable.EntityId;
        if (string.IsNullOrEmpty(verbId))
            return;

        var modManager = Watchman.Get<ModManager>();
        if (modManager == null)
            return;

        var imagesField = typeof(ModManager).GetField("_images",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var images = imagesField?.GetValue(modManager) as Dictionary<string, Sprite>;
        if (images == null)
            return;

        var searchKey = verbId.ToLowerInvariant();
        foreach (var kv in images)
        {
            if (kv.Key.ToLowerInvariant().EndsWith(searchKey))
            {
                image.sprite = kv.Value;
                image.preserveAspect = true;
                return;
            }
        }
    }
}
