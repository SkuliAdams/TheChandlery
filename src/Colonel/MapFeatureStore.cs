using System.Collections.Generic;
using System.Linq;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse.Colonel;

// Holds map features loaded from the compendium plus programmatically
// registered features. Programmatic features override JSON features by Id.
internal static class MapFeatureStore
{
    private static readonly List<MapFeatureDefinition> Programmatic = new();
    private static List<MapFeatureDefinition> _jsonFeatures;

    internal static void LoadJsonFeatures()
    {
        try
        {
            _jsonFeatures = Watchman.Get<Compendium>()?.GetEntitiesAsList<MapFeatureDefinition>()
                            ?? new List<MapFeatureDefinition>();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Chandlery Colonel: Failed to load map features from compendium: {ex.Message}");
            _jsonFeatures = new List<MapFeatureDefinition>();
        }
    }

    internal static void AddProgrammatic(MapFeatureDefinition feature)
    {
        if (feature == null || string.IsNullOrEmpty(feature.Id))
        {
            Debug.LogWarning("Chandlery Colonel: Ignoring programmatic map feature with null/empty id");
            return;
        }

        Programmatic.RemoveAll(f => f.Id == feature.Id);
        Programmatic.Add(feature);
    }

    internal static IReadOnlyList<MapFeatureDefinition> GetAll()
    {
        return (_jsonFeatures ?? Enumerable.Empty<MapFeatureDefinition>())
            .Concat(Programmatic)
            .Where(f => !string.IsNullOrEmpty(f.Id))
            .GroupBy(f => f.Id)
            .Select(g => g.Last())
            .ToList();
    }
}
