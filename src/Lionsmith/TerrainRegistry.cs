using System;
using System.Collections.Generic;
using System.Linq;
using SecretHistories.Entities;
using SecretHistories.Services;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse;

internal static class TerrainRegistry
{
    private static Dictionary<string, CustomTerrainDefinition> _definitions;

    internal static void LoadAll()
    {
        try
        {
            var list = Watchman.Get<Compendium>()?.GetEntitiesAsList<CustomTerrainDefinition>();
            if (list == null || list.Count == 0)
                return;

            _definitions = new Dictionary<string, CustomTerrainDefinition>();
            foreach (var def in list)
            {
                if (string.IsNullOrEmpty(def.Id))
                {
                    Debug.LogWarning("Chandlery Lionsmith: Skipping CustomTerrainDefinition with null/empty id");
                    continue;
                }

                if (_definitions.ContainsKey(def.Id))
                {
                    Debug.LogWarning($"Chandlery Lionsmith: Duplicate CustomTerrainDefinition id '{def.Id}' — keeping first");
                    continue;
                }

                _definitions[def.Id] = def;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Chandlery Lionsmith: Failed to load terrain definitions: {ex.Message}");
            _definitions = new Dictionary<string, CustomTerrainDefinition>();
        }
    }

    internal static bool HasAny() => _definitions?.Count > 0;

    internal static CustomTerrainDefinition Get(string id)
    {
        if (_definitions != null && _definitions.TryGetValue(id, out var def))
            return def;
        return null;
    }

    internal static IEnumerable<CustomTerrainDefinition> GetAll() =>
        _definitions?.Values ?? Enumerable.Empty<CustomTerrainDefinition>();
}
