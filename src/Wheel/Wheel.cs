using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using SecretHistories.Infrastructure.Modding;
using UnityEngine;

namespace TheHouse.Wheel;

public static class Wheel
{

    public static void Enact(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(ModManager), "TryLoadImagesForEnabledMods"),
            postfix: AccessTools.Method(typeof(Wheel), nameof(OnLoadImages)));

        WheelTypes.Enact(harmony);
        WheelIntercept.Enact(harmony);
        WheelChain.Enact(harmony);
        WheelFucine.Enact(harmony);
    }

    public static void ClaimProperty<TEntity, TProperty>(string propertyName,
        bool localize = false, TProperty defaultValue = default(TProperty))
        where TEntity : AbstractEntity<TEntity>
    {
        WheelStore.AddClaim<TEntity>(propertyName, typeof(TProperty), defaultValue, localize);
    }

    public static void ClaimProperty<TEntity, TProperty>(string propertyName,
        bool localize, TProperty defaultValue, Fucine importer)
        where TEntity : AbstractEntity<TEntity>
    {
        WheelStore.AddClaim<TEntity>(propertyName, typeof(TProperty), defaultValue, localize, importer);
    }

    public static void AddImportMolding<TEntity>(Action<EntityData> molding)
        where TEntity : AbstractEntity<TEntity>
    {
        WheelStore.AddMolding<TEntity>(molding);
    }

    public static void IgnoreProperty<TEntity>(string propertyName)
        where TEntity : AbstractEntity<TEntity>
    {
        WheelIgnore.IgnoreProperty(typeof(TEntity), propertyName);
    }

    public static void IgnoreEntityGroup(string groupId)
    {
        WheelIgnore.IgnoreEntityGroup(groupId);
    }

    // Loads PNGs from each mod's chandlery/ folder into the ModManager sprite cache.
    // Sprites are addressable by key "chandlery\\{relative_path_without_ext}" so other
    // modules (Flowermaker, Lionsmith, Colonel) can look them up via ModManager.GetSprite().
    private static void OnLoadImages(ModManager __instance)
    {
        var imagesField = AccessTools.Field(typeof(ModManager), "_images");
        var images = imagesField.GetValue(__instance) as Dictionary<string, Sprite>;
        if (images == null)
        {
            Debug.LogWarning("Chandlery Wheel: Could not access ModManager._images — game structure may have changed");
            return;
        }

        foreach (var mod in __instance.GetEnabledModsInLoadOrder())
        {
            var chandleryDir = Path.Combine(mod.ModRootFolder, "chandlery");
            if (!Directory.Exists(chandleryDir))
                continue;

            foreach (var filePath in Directory.GetFiles(chandleryDir, "*.png", SearchOption.AllDirectories))
            {
                try
                {
                    var relativePath = filePath
                        .Substring(chandleryDir.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    var key = "chandlery\\" + Path.ChangeExtension(relativePath, null).Replace('/', '\\');

                    var texture = new Texture2D(2, 2);
                    if (!texture.LoadImage(File.ReadAllBytes(filePath)))
                    {
                        Debug.LogWarning($"Chandlery Wheel: Failed to decode PNG '{filePath}'");
                        continue;
                    }

                    texture.filterMode = FilterMode.Bilinear;
                    texture.anisoLevel = 1;
                    texture.Apply();

                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));

                    images[key] = sprite;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Chandlery Wheel: Failed to load image '{filePath}': {ex.Message}");
                }
            }
        }
    }
}
