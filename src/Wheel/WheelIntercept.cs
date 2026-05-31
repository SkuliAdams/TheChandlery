using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using UnityEngine;

namespace TheHouse.Wheel;

internal static class WheelIntercept
{
    internal static void Enact(Harmony harmony)
    {
        try
        {
            var unknownPrefix = typeof(WheelIntercept).GetMethod(nameof(UnknownPrefix),
                BindingFlags.NonPublic | BindingFlags.Static);
            var factoryPrefix = typeof(WheelIntercept).GetMethod(nameof(CreateEntityPrefix),
                BindingFlags.NonPublic | BindingFlags.Static);

            if (unknownPrefix == null || factoryPrefix == null)
            {
                Debug.LogError("WheelIntercept: Could not find prefix methods");
                return;
            }

            var patchedUnknown = new HashSet<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

                foreach (var type in types)
                {
                    if (type.IsAbstract)
                        continue;

                    var baseType = type.BaseType;
                    if (baseType == null || !baseType.IsGenericType ||
                        baseType.GetGenericTypeDefinition() != typeof(AbstractEntity<>))
                        continue;

                    if (patchedUnknown.Add(baseType))
                    {
                        var method = baseType.GetMethod("PushUnknownProperty",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                            null, new[] { typeof(object), typeof(object) }, null);

                        if (method != null)
                            harmony.Patch(original: method, prefix: new HarmonyMethod(unknownPrefix));
                    }
                }
            }

            var createEntity = AccessTools.Method(typeof(FactoryInstantiator), "CreateEntity",
                new[] { typeof(Type), typeof(EntityData), typeof(ContentImportLog) });
            if (createEntity != null)
                harmony.Patch(original: createEntity, prefix: new HarmonyMethod(factoryPrefix));
            else
                Debug.LogError("WheelIntercept: Could not find FactoryInstantiator.CreateEntity");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WheelIntercept] Failed: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static bool UnknownPrefix(object __instance, object key, object value)
    {
        if (__instance == null || key == null)
            return true;

        if (!(__instance is IEntityWithId entity))
            return true;

        var entityType = entity.GetType();
        var propertyKey = key.ToString().ToLower();

        if (WheelIgnore.Ignores(entityType, propertyKey))
            return false;

        if (WheelStore.TryConvertAndStore(entity, entityType, propertyKey, value))
            return false;

        return true;
    }

    private static bool CreateEntityPrefix(Type T, EntityData importDataForEntity, ContentImportLog log)
    {
        if (T == null || importDataForEntity == null)
            return true;

        WheelStore.ApplyMoldings(T, importDataForEntity, log);
        return true;
    }
}