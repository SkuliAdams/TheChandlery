using System.Collections.Generic;
using HarmonyLib;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse;

// Shared suffix-match lookup over the ModManager sprite cache.
internal static class SpriteLookup
{
    internal static Sprite Find(string searchKey)
    {
        if (string.IsNullOrEmpty(searchKey))
            return null;

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
