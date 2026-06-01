using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse;

internal static class TerrainRegistry
{
    private static Dictionary<string, CustomTerrainDefinition> _definitions;
    private static Dictionary<string, List<string>> _connections;

    internal static void LoadAll()
    {
        try
        {
            var list = Watchman.Get<Compendium>()?.GetEntitiesAsList<CustomTerrainDefinition>();
            if (list == null || list.Count == 0)
                return;

            _definitions = new Dictionary<string, CustomTerrainDefinition>();
            _connections = new Dictionary<string, List<string>>();
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

                if (def.ConnectedTo != null && def.ConnectedTo.Count > 0)
                    _connections[def.Id] = def.ConnectedTo;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Chandlery Lionsmith: Failed to load terrain definitions: {ex.Message}");
            _definitions = new Dictionary<string, CustomTerrainDefinition>();
            _connections = new Dictionary<string, List<string>>();
        }
    }

    internal static bool HasAny() => _definitions?.Count > 0;

    internal static bool Has(string id) => _definitions?.ContainsKey(id) == true;

    internal static CustomTerrainDefinition Get(string id)
    {
        if (_definitions != null && _definitions.TryGetValue(id, out var def))
            return def;
        return null;
    }

    internal static IEnumerable<CustomTerrainDefinition> GetAll() =>
        _definitions?.Values ?? Enumerable.Empty<CustomTerrainDefinition>();

    internal static IEnumerable<CustomTerrainDefinition> GetAllNew() =>
        _definitions?.Values.Where(d => d.Override != true) ?? Enumerable.Empty<CustomTerrainDefinition>();

    internal static IEnumerable<CustomTerrainDefinition> GetAllOverrides() =>
        _definitions?.Values.Where(d => d.Override == true) ?? Enumerable.Empty<CustomTerrainDefinition>();

    internal static bool IsOverride(string id) =>
        _definitions != null && _definitions.TryGetValue(id, out var def) && def.Override == true;

    internal static void RegisterConnection(string roomId, List<string> connectedIds)
    {
        if (_connections == null)
            _connections = new Dictionary<string, List<string>>();
        _connections[roomId] = connectedIds;
    }

    internal static bool TryGetConnections(string roomId, out List<string> connectedIds)
    {
        if (_connections != null && _connections.TryGetValue(roomId, out connectedIds))
            return true;
        connectedIds = null;
        return false;
    }

    internal static Sprite FindSprite(string searchKey)
    {
        var imagesField = AccessTools.Field(typeof(ModManager), "_images");
        var images = imagesField.GetValue(Watchman.Get<ModManager>()) as Dictionary<string, Sprite>;
        if (images == null)
            return null;

        searchKey = searchKey.ToLowerInvariant();
        foreach (var kv in images)
            if (kv.Key.ToLowerInvariant().EndsWith(searchKey))
                return kv.Value;

        return null;
    }
}
