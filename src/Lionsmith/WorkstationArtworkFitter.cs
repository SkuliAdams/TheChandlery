using System;
using HarmonyLib;
using SecretHistories.Manifestations;
using SecretHistories.Spheres;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse;

internal class WorkstationArtworkMarker : MonoBehaviour { }

internal static class WorkstationArtworkFitter
{
    internal static void Patch(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(FitmentWorkstationManifestation), "Initialise"),
            postfix: new HarmonyMethod(typeof(WorkstationArtworkFitter), nameof(OnWorkstationInitialised))
        );
    }

    // Chandlery-created workstations clone a vanilla archetype, so their artwork
    // keeps the archetype's fixed prefab rect. Resize the artwork to the sphere
    // rect and pin it to the sphere's bottom-left corner.
    // Runs as a postfix so it also applies after Dappled Mask's modification
    private static void OnWorkstationInitialised(FitmentWorkstationManifestation __instance, VFab ___vFab)
    {
        try
        {
            var sphere = __instance.GetComponentInParent<FitmentWorkstationSphere>();
            if (sphere == null || sphere.GetComponent<WorkstationArtworkMarker>() == null)
                return;

            var sphereRt = sphere.GetComponent<RectTransform>();
            if (sphereRt == null || sphereRt.sizeDelta == Vector2.zero)
                return;

            if (___vFab == null)
                return;
            
            var art = ___vFab.GetComponentInChildren<Image>(true);
            if (art == null)
                return;

            var artRt = art.rectTransform;
            artRt.pivot = new Vector2(0f, 0f);
            artRt.anchorMin = new Vector2(0f, 0f);
            artRt.anchorMax = new Vector2(0f, 0f);
            artRt.anchoredPosition = Vector2.zero;
            artRt.sizeDelta = sphereRt.sizeDelta;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Chandlery Lionsmith: Failed to fit workstation artwork: {ex.Message}");
        }
    }
}