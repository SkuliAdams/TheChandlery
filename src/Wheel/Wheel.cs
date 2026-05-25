using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using SecretHistories.Abstract;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Services;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse.Wheel;

public static class Wheel
{
    private static readonly Dictionary<string, Type> EntityTypes = new();
    private static readonly Dictionary<string, Dictionary<string, object>> Entities = new();
    private static readonly Dictionary<string, bool> ModsWithChandleryFolder = new();

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

    private static void OnLoadImages(ModManager __instance)
    {
        var imagesField = AccessTools.Field(typeof(ModManager), "_images");
        var images = imagesField.GetValue(__instance) as Dictionary<string, Sprite>;
        if (images == null)
            return;

        ModsWithChandleryFolder.Clear();

        foreach (var mod in __instance.GetEnabledModsInLoadOrder())
        {
            var chandleryDir = Path.Combine(mod.ModRootFolder, "chandlery");
            if (!Directory.Exists(chandleryDir))
                continue;

            ModsWithChandleryFolder[mod.Id] = true;

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
                        continue;

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
