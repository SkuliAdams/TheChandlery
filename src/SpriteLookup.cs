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

        searchKey = searchKey.Replace('/', '\\').ToLowerInvariant();
        var lastSep = searchKey.LastIndexOf('\\');
        var searchName = lastSep >= 0 ? searchKey.Substring(lastSep + 1) : searchKey;

        foreach (var kv in images)
        {
            var keyName = kv.Key.Replace('/', '\\').ToLowerInvariant();
            var sep = keyName.LastIndexOf('\\');
            keyName = sep >= 0 ? keyName.Substring(sep + 1) : keyName;

            if (keyName == searchName)
                return kv.Value;
        }

        return null;
    }
}
