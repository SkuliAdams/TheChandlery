using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using UnityEngine;

namespace TheHouse.Wheel;

internal static class WheelStore
{
    private readonly struct PropertySlot
    {
        public readonly Type Type;
        public readonly object DefaultValue;
        public readonly bool Localize;
        public readonly Fucine ImporterAttribute;

        public PropertySlot(Type type, object defaultValue, bool localize, Fucine importerAttribute)
        {
            Type = type;
            DefaultValue = defaultValue;
            Localize = localize;
            ImporterAttribute = importerAttribute;
        }
    }

    private static readonly Dictionary<Type, Dictionary<string, PropertySlot>> _claimed = new();
    private static readonly ConditionalWeakTable<IEntityWithId, Dictionary<string, object>> _data = new();
    private static readonly Dictionary<Type, List<Action<EntityData>>> _moldings = new();

    internal static void AddClaim<TEntity>(string propertyName, Type propertyType,
        object defaultValue, bool localize, Fucine importerAttribute = null) where TEntity : AbstractEntity<TEntity>
    {
        var entityType = typeof(TEntity);
        if (!_claimed.TryGetValue(entityType, out var props))
        {
            props = new Dictionary<string, PropertySlot>();
            _claimed[entityType] = props;
        }
        props[propertyName.ToLower()] = new PropertySlot(propertyType, defaultValue, localize, importerAttribute);
    }

    internal static void AddMolding<TEntity>(Action<EntityData> molding) where TEntity : AbstractEntity<TEntity>
    {
        var entityType = typeof(TEntity);
        if (!_moldings.TryGetValue(entityType, out var list))
        {
            list = new List<Action<EntityData>>();
            _moldings[entityType] = list;
        }
        list.Add(molding);
    }

    internal static void ApplyMoldings(Type entityType, EntityData entityData, ContentImportLog log)
    {
        var type = entityType;
        while (type != null)
        {
            if (_moldings.TryGetValue(type, out var list))
            {
                foreach (var molding in list)
                {
                    try
                    {
                        molding(entityData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"WheelStore: Molding for {type.Name} threw: {ex.Message}");
                    }
                }
            }
            type = type.BaseType;
        }
    }

    internal static bool HasClaim(Type entityType, string propertyKey)
    {
        var type = entityType;
        while (type != null)
        {
            if (_claimed.TryGetValue(type, out var props) && props.ContainsKey(propertyKey))
                return true;
            type = type.BaseType;
        }
        return false;
    }

    internal static bool TryConvertAndStore(IEntityWithId entity, Type entityType, string propertyKey, object rawValue)
    {
        var type = entityType;
        while (type != null)
        {
            if (_claimed.TryGetValue(type, out var props) && props.TryGetValue(propertyKey, out var slot))
            {
                var dict = _data.GetOrCreateValue(entity);
                dict[propertyKey] = ConvertWithImporter(rawValue, slot);
                return true;
            }
            type = type.BaseType;
        }
        return false;
    }

    private static object ConvertWithImporter(object rawValue, PropertySlot slot)
    {
        if (rawValue == null)
            return null;
        if (slot.Type.IsInstanceOfType(rawValue))
            return rawValue;

        if (slot.ImporterAttribute != null)
        {
            try
            {
                var importer = slot.ImporterAttribute.CreateImporterInstance();
                return importer.Import(rawValue, slot.Type);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"WheelStore: Custom importer for '{slot.Type.Name}' failed: {ex.Message}");
            }
        }

        try
        {
            return Convert.ChangeType(rawValue, slot.Type);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"WheelStore: Convert.ChangeType failed for value '{rawValue}' -> {slot.Type.Name}: {ex.Message}");
            return rawValue;
        }
    }

    internal static object RetrieveProperty(IEntityWithId entity, string propertyName)
    {
        var key = propertyName.ToLower();
        if (_data.TryGetValue(entity, out var dict) && dict.TryGetValue(key, out var value))
            return value;
        return null;
    }

    internal static bool HasCustomProperty(IEntityWithId entity, string propertyName)
    {
        var key = propertyName.ToLower();
        return _data.TryGetValue(entity, out var dict) && dict.ContainsKey(key);
    }

    internal static void SetCustomProperty(IEntityWithId entity, string propertyName, object value)
    {
        var dict = _data.GetOrCreateValue(entity);
        dict[propertyName.ToLower()] = value;
    }

    internal static void RemoveProperty(IEntityWithId entity, string propertyName)
    {
        if (_data.TryGetValue(entity, out var dict))
            dict.Remove(propertyName.ToLower());
    }

    internal static Dictionary<string, object> GetCustomProperties(IEntityWithId entity)
    {
        if (_data.TryGetValue(entity, out var dict))
            return new Dictionary<string, object>(dict);
        return new Dictionary<string, object>();
    }

    internal static void InheritClaimedProperties(IEntityWithId child, IEntityWithId parent)
    {
        if (!_data.TryGetValue(parent, out var parentDict))
            return;

        var childDict = _data.GetOrCreateValue(child);
        foreach (var kvp in parentDict)
        {
            if (!childDict.ContainsKey(kvp.Key))
                childDict[kvp.Key] = kvp.Value;
        }
    }
}

public static class WheelEntityExtensions
{
    public static T RetrieveProperty<T>(this IEntityWithId entity, string propertyName)
    {
        var value = WheelStore.RetrieveProperty(entity, propertyName);
        if (value is T typed)
            return typed;
        return default;
    }

    public static bool TryRetrieveProperty<T>(this IEntityWithId entity, string propertyName, out T result)
    {
        var value = WheelStore.RetrieveProperty(entity, propertyName);
        if (value != null && value is T typed)
        {
            result = typed;
            return true;
        }
        result = default;
        return false;
    }

    public static void SetCustomProperty(this IEntityWithId entity, string propertyName, object value)
    {
        WheelStore.SetCustomProperty(entity, propertyName, value);
    }

    public static bool HasCustomProperty(this IEntityWithId entity, string propertyName)
    {
        return WheelStore.HasCustomProperty(entity, propertyName);
    }

    public static void RemoveCustomProperty(this IEntityWithId entity, string propertyName)
    {
        WheelStore.RemoveProperty(entity, propertyName);
    }

    public static Dictionary<string, object> GetCustomProperties(this IEntityWithId entity)
    {
        return WheelStore.GetCustomProperties(entity);
    }
}
