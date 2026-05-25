using HarmonyLib;
using SecretHistories.Entities;
using SecretHistories.Infrastructure;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Services;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse.Colonel;

internal static class Colonel
{
    internal static void Enact(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(GameGateway), "PopulateEnvironment"),
            postfix: new HarmonyMethod(typeof(Colonel), nameof(OnEnvironmentPopulated))
        );

        Debug.Log("Chandlery Colonel: Patches applied");
    }

    private static void OnEnvironmentPopulated()
    {
        var sprite = Watchman.Get<ModManager>().GetSprite("chandlery\\gamebg");
        if (sprite == null)
        {
            Debug.Log("Chandlery Colonel: No gamebg.png provided by any mod — using vanilla backgrounds");
            return;
        }

        Debug.Log("Chandlery Colonel: Applying custom game background...");

        var backgrounds = GameObject.Find("CanvasWorld/CameraDragRect/Backgrounds");
        if (backgrounds != null)
        {
            var image = backgrounds.GetComponent<Image>();
            if (image == null)
                image = backgrounds.AddComponent<Image>();

            image.sprite = sprite;
            image.preserveAspect = false;

            var rt = backgrounds.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            foreach (Transform child in backgrounds.transform)
                child.gameObject.SetActive(false);
        }
        else
            Debug.LogWarning("Chandlery Colonel: CanvasWorld/CameraDragRect/Backgrounds not found");

        var bgCanvas = GameObject.Find("BuildingBackgrounds");
        if (bgCanvas != null)
        {
            bgCanvas.SetActive(false);
            Debug.Log("Chandlery Colonel: Disabled BuildingBackgrounds");
        }
    }
}
