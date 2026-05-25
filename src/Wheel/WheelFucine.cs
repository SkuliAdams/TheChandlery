using System;
using HarmonyLib;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;

namespace TheHouse.Wheel;

internal static class WheelFucine
{
    internal static void Enact(HarmonyLib.Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(FucinePathValue), nameof(FucinePathValue.CreateImporterInstance)),
            prefix: AccessTools.Method(typeof(WheelFucine), nameof(ExtendPathImporter)));
    }

    private static bool ExtendPathImporter(ref AbstractImporter __result)
    {
        __result = new WheelExtendedPathImporter();
        return false;
    }
}

public class WheelPanImporter : AbstractImporter
{
    public override object Import(object data, Type type)
    {
        var import = ImportMethods.GetDefaultImportFuncForType(type);
        return import(data, type);
    }

    public override object GetDefaultValue<T>(CachedFucineProperty<T> cachedFucineProperty)
    {
        var attr = cachedFucineProperty.FucineAttribute;
        if (attr.DefaultValue != null)
        {
            var import = ImportMethods.GetDefaultImportFuncForType(cachedFucineProperty.ThisPropInfo.PropertyType);
            return import(attr.DefaultValue, cachedFucineProperty.ThisPropInfo.PropertyType);
        }
        return FactoryInstantiator.CreateObjectWithDefaultConstructor(cachedFucineProperty.ThisPropInfo.PropertyType);
    }
}

public class WheelNullableImporter : AbstractImporter
{
    public override object Import(object data, Type type)
    {
        if (data == null || data.ToString() == "null")
            return null;

        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            var import = ImportMethods.GetDefaultImportFuncForType(underlyingType);
            return import(data, underlyingType);
        }

        var defaultImport = ImportMethods.GetDefaultImportFuncForType(type);
        return defaultImport(data, type);
    }

    public override object GetDefaultValue<T>(CachedFucineProperty<T> cachedFucineProperty)
    {
        return Import(cachedFucineProperty.FucineAttribute.DefaultValue, cachedFucineProperty.ThisPropInfo.PropertyType);
    }
}

public class WheelExtendedPathImporter : AbstractImporter
{
    public override object Import(object data, Type type)
    {
        var fucinePath = new FucinePath(data?.ToString());
        if (fucinePath.IsValid())
            return fucinePath;
        return FucinePath.Current();
    }

    public override object GetDefaultValue<T>(CachedFucineProperty<T> cachedFucineProperty)
    {
        if (cachedFucineProperty.FucineAttribute.DefaultValue != null)
        {
            var fucinePath = new FucinePath(cachedFucineProperty.FucineAttribute.DefaultValue.ToString());
            if (fucinePath.IsValid())
                return fucinePath;
        }
        return FucinePath.Current();
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class WheelFucineEverValue : Fucine
{
    public WheelFucineEverValue() { DefaultValue = new System.Collections.ArrayList(); }
    public WheelFucineEverValue(object defaultValue) { DefaultValue = defaultValue; }
    public WheelFucineEverValue(params object[] defaultValue) { DefaultValue = new System.Collections.ArrayList(defaultValue); }

    public override AbstractImporter CreateImporterInstance() { return new WheelPanImporter(); }
}

[AttributeUsage(AttributeTargets.Property)]
public class WheelFucineNullable : Fucine
{
    public WheelFucineNullable() { }
    public WheelFucineNullable(object defaultValue) { DefaultValue = defaultValue; }

    public override AbstractImporter CreateImporterInstance() { return new WheelNullableImporter(); }
}
