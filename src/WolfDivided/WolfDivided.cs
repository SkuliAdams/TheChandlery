using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using SecretHistories.Entities;
using SecretHistories.Infrastructure;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Tokens.Payloads;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse.WolfDivided;

internal static class WolfDivided
{
    internal static void Enact(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(GameGateway), "PopulateEnvironment"),
            postfix: new HarmonyMethod(typeof(WolfDivided), nameof(OnEnvironmentPopulated))
        );

        Debug.Log("Chandlery WolfDivided: Patches applied");
    }

    private static void OnEnvironmentPopulated()
    {
        var config = LoadMergedConfig();
        if (config.IsEmpty)
            return;

        Debug.Log("Chandlery WolfDivided: Applying terrain disable rules...");

        if (config.CleanSlate)
            ApplyCleanSlate(config);

        ApplySelectiveDisable(config);
    }

    private static WolfDividedConfig LoadMergedConfig()
    {
        var result = new WolfDividedConfig();
        var manager = Watchman.Get<ModManager>();

        foreach (var mod in manager.GetEnabledModsInLoadOrder())
        {
            var configPath = Path.Combine(mod.ModRootFolder, "chandlery", "wolfdivided.json");
            if (!File.Exists(configPath))
                continue;

            try
            {
                var text = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<WolfDividedConfig>(text);
                if (config == null)
                    continue;

                if (config.CleanSlate)
                    result.CleanSlate = true;

                if (config.Preserve?.Count > 0)
                    result.Preserve = result.Preserve.Union(config.Preserve).ToList();

                if (config.DisableTerrain?.Count > 0)
                    result.DisableTerrain = result.DisableTerrain.Union(config.DisableTerrain).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Chandlery WolfDivided: Failed to load config from {mod.Id}: {ex.Message}");
            }
        }

        return result;
    }

    private static void ApplyCleanSlate(WolfDividedConfig config)
    {
        var terrainFeatures = UnityEngine.Object.FindObjectsOfType<TerrainFeature>();
        var preserveSet = new HashSet<string>(config.Preserve);

        foreach (var terrain in terrainFeatures)
        {
            if (!preserveSet.Contains(terrain.Id) && terrain.gameObject.activeSelf)
            {
                terrain.gameObject.SetActive(false);
                Debug.Log($"Chandlery WolfDivided: Disabled terrain '{terrain.Id}' (clean slate)");
            }
        }
    }

    private static void ApplySelectiveDisable(WolfDividedConfig config)
    {
        var axe = Watchman.Get<HornedAxe>();
        foreach (var id in config.DisableTerrain)
        {
            var token = axe.FindSingleOrDefaultTokenById(id);
            if (token != null && token.gameObject.activeSelf)
            {
                token.gameObject.SetActive(false);
                Debug.Log($"Chandlery WolfDivided: Disabled terrain '{id}' (selective)");
            }
        }
    }
}
