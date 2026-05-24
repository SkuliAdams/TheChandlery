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

namespace TheHouse;

internal static class Wheel
{
    private static readonly Dictionary<string, Type> EntityTypes = new();
    private static readonly Dictionary<string, Dictionary<string, object>> Entities = new();
    private static readonly Dictionary<string, bool> ModsWithChandleryFolder = new();

    public static void Enact(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(ModManager), "TryLoadImagesForEnabledMods"),
            postfix: AccessTools.Method(typeof(Wheel), nameof(OnLoadImages)));

        /*harmony.Patch(
            original: AccessTools.Method(typeof(CompendiumLoader), "LoadModsToCompendium"),
            postfix: AccessTools.Method(typeof(Wheel), nameof(OnLoadMods)));

        harmony.Patch(
            original: AccessTools.Method(typeof(CompendiumLoader), "PopulateCompendium"),
            postfix: AccessTools.Method(typeof(Wheel), nameof(OnPopulateCompendium)));*/
    }

    public static void RegisterEntityType<T>(string tag) where T : class
    {
        EntityTypes[tag] = typeof(T);
    }

    public static bool HasChandleryFolder(string modId)
    {
        return ModsWithChandleryFolder.ContainsKey(modId);
    }

    public static IEnumerable<T> GetEntities<T>(string tag) where T : class
    {
        if (Entities.TryGetValue(tag, out var dict))
            return dict.Values.Cast<T>();
        return Enumerable.Empty<T>();
    }

    public static T GetEntity<T>(string tag, string id) where T : class
    {
        if (Entities.TryGetValue(tag, out var dict) && dict.TryGetValue(id, out var entity))
            return entity as T;
        return null;
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

    private static void OnLoadMods(CompendiumLoader __instance, string locFolderForCurrentCulture)
    {
        var modManager = Watchman.Get<ModManager>();
        if (modManager == null)
            return;

        var loadersField = AccessTools.Field(typeof(CompendiumLoader), "modContentLoaders");
        var logField = AccessTools.Field(typeof(CompendiumLoader), "_log");

        var loaders = loadersField.GetValue(__instance) as List<DataFileLoader>;
        var log = logField.GetValue(__instance) as ContentImportLog;
        if (loaders == null || log == null)
            return;

        foreach (var mod in modManager.GetEnabledModsInLoadOrder())
        {
            var chandleryDir = Path.Combine(mod.ModRootFolder, "chandlery");
            if (!Directory.Exists(chandleryDir))
                continue;

            var jsonFiles = Directory.GetFiles(chandleryDir, "*.json", SearchOption.AllDirectories);
            if (jsonFiles.Length == 0)
                continue;

            var loader = new DataFileLoader(chandleryDir);
            loader.LoadFilesFromAssignedFolder(log);
            loaders.Add(loader);
        }
    }

    private static void OnPopulateCompendium(CompendiumLoader __instance, Compendium compendiumToPopulate, string forCultureId)
    {
        if (EntityTypes.Count == 0)
            return;

        var logField = AccessTools.Field(typeof(CompendiumLoader), "_log");
        var loadersField = AccessTools.Field(typeof(CompendiumLoader), "modContentLoaders");

        var log = logField.GetValue(__instance) as ContentImportLog;
        var modLoaders = loadersField.GetValue(__instance) as List<DataFileLoader>;
        if (log == null || modLoaders == null)
            return;

        foreach (var kvp in EntityTypes)
        {
            var tag = kvp.Key;
            var type = kvp.Value;

            try
            {
                var loader = new EntityTypeDataLoader(type, tag, forCultureId, log);

                var modFiles = modLoaders
                    .SelectMany(ml => ml.GetLoadedContentFilesContainingEntityTag(tag))
                    .ToList();

                loader.SupplyContentFiles(
                    Enumerable.Empty<LoadedDataFile>(),
                    Enumerable.Empty<LoadedDataFile>(),
                    modFiles,
                    Enumerable.Empty<LoadedDataFile>());

                loader.LoadEntityDataFromSuppliedFiles();

                var entityDataList = loader.GetLoadedEntityDataAsList();
                var entityDict = new Dictionary<string, object>();

                foreach (var entityData in entityDataList)
                {
                    try
                    {
                        var entity = FactoryInstantiator.CreateEntity(type, entityData, log);
                        entityDict[entity.Id] = entity;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"Chandlery Wheel: Failed to instantiate {tag} entity '{entityData.Id}': {ex.Message}");
                    }
                }

                Entities[tag] = entityDict;
                Debug.Log($"Chandlery Wheel: Loaded {entityDict.Count} entities for tag '{tag}'");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Chandlery Wheel: Failed to process entity type '{tag}': {ex.Message}");
            }
        }
    }
}
