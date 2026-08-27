using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using SecretHistories.Infrastructure;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Services;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse.Colonel;

// Places world-space images ("map features") onto the tabletop at a
// given faux layer. Features are defined via the "mapFeatures" Fucine entity in
// content JSON, or programmatically via AddMapFeature. The vanilla background is
// hidden when the global clearVanilla flag is set in chandlery/colonel.json.
public static class Colonel
{
    private static bool _contentListenerAdded;

    public static void Enact(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(GameGateway), "PopulateEnvironment"),
            postfix: new HarmonyMethod(typeof(Colonel), nameof(OnEnvironmentPopulated))
        );

        Debug.Log("Chandlery Colonel: Patches applied");
    }

    public static void AddMapFeature(MapFeatureDefinition feature)
    {
        MapFeatureStore.AddProgrammatic(feature);
    }

    public static void AddMapFeature(string id, string sprite, FauxLayerBand layer,
        float posX, float posY, float width, float height, int? order = null)
    {
        var feature = new MapFeatureDefinition(id)
        {
            Sprite = sprite,
            Layer = layer.ToString().ToLowerInvariant(),
            PosX = posX,
            PosY = posY,
            Width = width,
            Height = height,
            Order = order
        };
        MapFeatureStore.AddProgrammatic(feature);
    }

    private static void OnEnvironmentPopulated()
    {
        if (!_contentListenerAdded)
        {
            Watchman.Get<Concursum>().ContentUpdatedEvent.AddListener(_ => Rebuild());
            _contentListenerAdded = true;
        }

        Rebuild();
    }

    private static void Rebuild()
    {
        try
        {
            var config = LoadMergedConfig();
            MapFeatureStore.LoadJsonFeatures();
            var features = MapFeatureStore.GetAll();

            Debug.Log($"Chandlery Colonel: Placing {features.Count} map feature(s), clearVanilla={config.ClearVanilla}");
            MapFeatureRenderer.Rebuild(features);

            if (config.ClearVanilla)
                HideVanillaBackgrounds();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chandlery Colonel: Error during background injection: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static ColonelConfig LoadMergedConfig()
    {
        var clearVanilla = Watchman.Get<ModManager>()
            .GetEnabledModsInLoadOrder()
            .Select(mod => (Mod: mod, Path: Path.Combine(mod.ModRootFolder, "chandlery", "colonel.json")))
            .Where(x => File.Exists(x.Path))
            .Select(x => TryLoadClearVanilla(x.Mod, x.Path))
            .Any(clear => clear);

        return new ColonelConfig { ClearVanilla = clearVanilla };
    }

    private static bool TryLoadClearVanilla(Mod mod, string configPath)
    {
        try
        {
            return JsonConvert.DeserializeObject<ColonelConfig>(File.ReadAllText(configPath))?.ClearVanilla == true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Chandlery Colonel: Failed to load config from {mod.Id}: {ex.Message}");
            return false;
        }
    }

    private static void HideVanillaBackgrounds()
    {
        var backgrounds = GameObject.Find("CanvasWorld/CameraDragRect/Backgrounds");
        if (backgrounds != null)
        {
            foreach (Transform child in backgrounds.transform)
                child.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Chandlery Colonel: CanvasWorld/CameraDragRect/Backgrounds not found");
        }

        var bgCanvas = GameObject.Find("BuildingBackgrounds");
        if (bgCanvas != null)
            bgCanvas.SetActive(false);
    }
}
