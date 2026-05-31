using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using SecretHistories.Services;
using UnityEngine;

namespace TheHouse.Wheel;

internal static class WheelTypes
{
    private static bool IsLdloc(CodeInstruction inst)
    {
        return inst.opcode == OpCodes.Ldloc || inst.opcode == OpCodes.Ldloc_S ||
               inst.opcode == OpCodes.Ldloc_0 || inst.opcode == OpCodes.Ldloc_1 ||
               inst.opcode == OpCodes.Ldloc_2 || inst.opcode == OpCodes.Ldloc_3;
    }

    private static bool IsStloc(CodeInstruction inst)
    {
        return inst.opcode == OpCodes.Stloc || inst.opcode == OpCodes.Stloc_S ||
               inst.opcode == OpCodes.Stloc_0 || inst.opcode == OpCodes.Stloc_1 ||
               inst.opcode == OpCodes.Stloc_2 || inst.opcode == OpCodes.Stloc_3;
    }

    private static int GetLocalIndex(CodeInstruction inst)
    {
        if (inst.operand is int idx)
            return idx;
        if (inst.operand is LocalBuilder lb)
            return lb.LocalIndex;
        return inst.opcode switch
        {
            var op when op == OpCodes.Ldloc_0 || op == OpCodes.Stloc_0 => 0,
            var op when op == OpCodes.Ldloc_1 || op == OpCodes.Stloc_1 => 1,
            var op when op == OpCodes.Ldloc_2 || op == OpCodes.Stloc_2 => 2,
            var op when op == OpCodes.Ldloc_3 || op == OpCodes.Stloc_3 => 3,
            _ => throw new InvalidOperationException(
                $"Cannot determine local index from opcode {inst.opcode}")
        };
    }
    internal static void Enact(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(CompendiumLoader), "PopulateCompendium"),
            transpiler: AccessTools.Method(typeof(WheelTypes), nameof(Transpile)));
    }

    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();

        var initialiseMethod = AccessTools.Method(typeof(Compendium), "Initialise",
            new[] { typeof(IEnumerable<Type>) });
        var helperMethod = AccessTools.Method(typeof(WheelTypes), nameof(InjectModEntityTypes));
        var logField = AccessTools.Field(typeof(CompendiumLoader), "_log");

        if (initialiseMethod == null || helperMethod == null || logField == null)
        {
            Debug.LogError("WheelTypes: Failed to resolve method/field references for transpiler");
            return code;
        }

        var listLocal = FindListLocal(code, initialiseMethod);
        if (listLocal == null)
        {
            Debug.LogError("WheelTypes: Could not find list local for injection");
            return code;
        }

        var dictLocal = FindDictLocal(code, listLocal.Value);
        if (dictLocal == null)
        {
            Debug.LogError("WheelTypes: Could not find dictionary local for injection");
            return code;
        }

        return InjectHelperCall(code, initialiseMethod, helperMethod, logField,
            listLocal.Value, dictLocal.Value);
    }

    private static int? FindListLocal(List<CodeInstruction> code, MethodInfo initialiseMethod)
    {
        for (int i = 0; i < code.Count; i++)
            if (code[i].opcode == OpCodes.Callvirt &&
                Equals(code[i].operand, initialiseMethod) &&
                i >= 1 && IsLdloc(code[i - 1]))
                return GetLocalIndex(code[i - 1]);

        return null;
    }

    private static int? FindDictLocal(List<CodeInstruction> code, int notListLocal)
    {
        var dictCtor = typeof(Dictionary<string, EntityTypeDataLoader>)
            .GetConstructor(Type.EmptyTypes);

        if (dictCtor == null)
            return null;

        for (int i = 0; i < code.Count; i++)
        {
            if (code[i].opcode == OpCodes.Newobj &&
                Equals(code[i].operand, dictCtor) &&
                i + 1 < code.Count && IsStloc(code[i + 1]))
            {
                var idx = GetLocalIndex(code[i + 1]);
                if (idx != notListLocal)
                    return idx;
            }
        }

        return null;
    }

    private static IEnumerable<CodeInstruction> InjectHelperCall(
        List<CodeInstruction> code,
        MethodInfo initialiseMethod,
        MethodInfo helperMethod,
        FieldInfo logField,
        int listLocal,
        int dictLocal)
    {
        for (int i = 0; i < code.Count; i++)
        {
            if (code[i].opcode == OpCodes.Callvirt &&
                Equals(code[i].operand, initialiseMethod))
            {
                yield return new CodeInstruction(OpCodes.Ldloc, listLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, dictLocal);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, logField);
                yield return new CodeInstruction(OpCodes.Call, helperMethod);
            }

            yield return code[i];
        }
    }

    private static void InjectModEntityTypes(
        List<Type> typesToLoad,
        Dictionary<string, EntityTypeDataLoader> fucineLoaders,
        string cultureId,
        ContentImportLog log)
    {
        var modsDir = System.IO.Path.Combine(Application.persistentDataPath, "mods");

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!IsModAssembly(assembly, modsDir))
                continue;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            foreach (Type type in types)
            {
                if (typesToLoad.Contains(type))
                    continue;

                var attr = (FucineImportable)type.GetCustomAttribute(typeof(FucineImportable), false);
                if (attr != null)
                {
                    var tag = attr.TaggedAs.ToLower();
                    if (!fucineLoaders.ContainsKey(tag))
                    {
                        typesToLoad.Add(type);
                        fucineLoaders.Add(tag,
                            new EntityTypeDataLoader(type, attr.TaggedAs, cultureId, log));
                    }
                    else
                    {
                        log.LogWarning($"WheelTypes: Tag '{tag}' on '{type.FullName}' already registered — skipping");
                    }
                }
            }
        }
    }

    private static bool IsModAssembly(Assembly assembly, string modsDir)
    {
        if (assembly.IsDynamic)
            return false;

        try
        {
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location))
                return false;

            var nLocation = location.Replace('/', '\\');
            var nModsDir = modsDir.Replace('/', '\\');

            return nLocation.StartsWith(nModsDir, StringComparison.OrdinalIgnoreCase) ||
                   nLocation.IndexOf("steamapps\\workshop\\content",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }


}
