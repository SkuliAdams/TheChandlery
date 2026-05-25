using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SecretHistories.Fucine;
using UnityEngine;

namespace TheHouse.Wheel;

internal static class WheelIgnore
{
    private static readonly Dictionary<Type, List<string>> _ignoredProperties = new();
    private static readonly HashSet<string> _ignoredGroups = new();
    private static Dictionary<Type, string> _typeToGroup;
    private static bool _groupMapBuilt;

    internal static void IgnoreProperty(Type entityType, string propertyName)
    {
        var key = propertyName.ToLower();
        if (!_ignoredProperties.TryGetValue(entityType, out var list))
        {
            list = new List<string>();
            _ignoredProperties[entityType] = list;
        }
        if (!list.Contains(key))
            list.Add(key);
    }

    internal static void IgnoreEntityGroup(string groupId)
    {
        _ignoredGroups.Add(groupId.ToLower());
    }

    internal static bool Ignores(Type entityType, string propertyName)
    {
        var key = propertyName.ToLower();

        var type = entityType;
        while (type != null)
        {
            if (_ignoredProperties.TryGetValue(type, out var list) && list.Contains(key))
                return true;
            type = type.BaseType;
        }

        if (_ignoredGroups.Count > 0 && IgnoresGroupForType(entityType))
            return true;

        return false;
    }

    internal static bool IgnoresGroup(string groupId)
    {
        return _ignoredGroups.Contains(groupId.ToLower());
    }

    private static bool IgnoresGroupForType(Type entityType)
    {
        BuildGroupMapIfNeeded();
        return _typeToGroup.TryGetValue(entityType, out var group) && _ignoredGroups.Contains(group);
    }

    private static void BuildGroupMapIfNeeded()
    {
        if (_groupMapBuilt)
            return;
        _groupMapBuilt = true;
        _typeToGroup = new Dictionary<Type, string>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;

            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

            foreach (var type in types)
            {
                var attr = (FucineImportable)type.GetCustomAttribute(typeof(FucineImportable), false);
                if (attr != null)
                    _typeToGroup[type] = attr.TaggedAs.ToLower();
            }
        }
    }
}
