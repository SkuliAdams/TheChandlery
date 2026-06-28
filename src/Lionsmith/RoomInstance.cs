using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SecretHistories.Assets.Scripts.Application.Spheres.Dominions;
using SecretHistories.Spheres;
using SecretHistories.Spheres.Choreographers;
using SecretHistories.Abstract;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse;

internal class RoomInstance
{
    private readonly GameObject _root;
    private readonly CustomTerrainDefinition _def;

    private GameObject _slotArchetype;
    private GameObject _workstationArchetype;
    private GameObject _shelfArchetype;
    private GameObject _comfortArchetype;
    private GameObject _wallArtArchetype;

    private GameObject _worldDominion;
    private GameObject _shelfDominion;
    private float _roomW;
    private float _roomH;

    public RoomInstance(GameObject root, CustomTerrainDefinition def)
    {
        _root = root;
        _def = def;
    }

    public void ExtractArchetypes()
    {
        _slotArchetype = FindAndCloneArchetype<PhysicalSphere>("__archetype_slot",
            s => !(s is FitmentWorkstationSphere) && !(s is ComfortSphere));
        _workstationArchetype = FindAndCloneArchetype<FitmentWorkstationSphere>("__archetype_workstation");
        _shelfArchetype = FindAndCloneArchetype<ShelfSpaceSphere>("__archetype_shelf");
        _comfortArchetype = FindAndCloneArchetype<ComfortSphere>("__archetype_comfort");
        _wallArtArchetype = FindAndCloneArchetype<PhysicalSphere>("__archetype_wallart",
            s => !(s is FitmentWorkstationSphere) && !(s is ComfortSphere));
    }

    public void PopulateContents(float roomW, float roomH)
    {
        _roomW = roomW;
        _roomH = roomH;
        var contents = _def.Contents;
        if (contents == null)
            return;

        if ((contents.Spheres == null || contents.Spheres.Count == 0)
            && (contents.Workstations == null || contents.Workstations.Count == 0))
            return;

        BuildDominions();

        if (contents.Spheres != null)
            foreach (var sd in contents.Spheres)
                BuildSphere(sd);

        if (contents.Workstations != null)
            foreach (var wd in contents.Workstations)
                BuildWorkstation(wd);
    }

    private GameObject FindAndCloneArchetype<T>(string name, Func<T, bool> filter = null) where T : MonoBehaviour
    {
        foreach (var comp in _root.GetComponentsInChildren<T>(true))
        {
            if (filter != null && !filter(comp))
                continue;

            var clone = UnityEngine.Object.Instantiate(comp.gameObject, _root.transform);
            clone.name = name;

            foreach (var token in clone.GetComponentsInChildren<Token>(true))
                if (token.gameObject != clone)
                    UnityEngine.Object.DestroyImmediate(token.gameObject);

            foreach (var t in clone.GetComponentsInChildren<Transform>(true))
                if (t != clone.transform && t.name.StartsWith("Seed"))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);

            clone.SetActive(false);
            return clone;
        }

        Debug.LogError($"Chandlery RoomInstance: No archetype found for '{typeof(T).Name}' in room '{_def.Id}'");
        return null;
    }

    private void BuildDominions()
    {
        var parentTf = _root.transform.Find("RoomManifestation/Unshrouded") ?? _root.transform;

        var worldGo = new GameObject("dominion_world");
        worldGo.transform.SetParent(parentTf, false);
        _worldDominion = worldGo;
        _worldDominion.AddComponent<WorldDominion>();

        if (_def.Contents != null && _def.Contents.HasShelves)
        {
            var shelfGo = new GameObject("dominion_shelves");
            shelfGo.transform.SetParent(parentTf, false);
            var shelfDom = shelfGo.AddComponent<ShelfDominion>();
            typeof(AbstractDominion).GetProperty("Identifier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(shelfDom, "dominion_shelves");
            _shelfDominion = shelfGo;
        }
    }

    private void BuildSphere(SphereDefinition def)
    {
        var type = def.SphereType ?? "normal";
        GameObject archetype;
        GameObject dominion;
        Type choreographerType;
        bool applyFields;
        string prefix;

        switch (type)
        {
            case "bookshelf":
                archetype = _shelfArchetype;
                dominion = _shelfDominion ?? _worldDominion;
                choreographerType = typeof(ShelfChoreographer);
                applyFields = false;
                prefix = "shelf_";
                break;
            case "comfort":
                archetype = _comfortArchetype;
                dominion = _worldDominion;
                choreographerType = typeof(ThingChoreographer);
                applyFields = true;
                prefix = "comfort_";
                break;
            case "wall":
                archetype = _wallArtArchetype;
                dominion = _worldDominion;
                choreographerType = typeof(WallChoreographer);
                applyFields = true;
                prefix = "wallart_";
                break;
            default:
                archetype = _slotArchetype;
                dominion = _worldDominion;
                choreographerType = typeof(ThingChoreographer);
                applyFields = true;
                prefix = "slot_";
                break;
        }

        if (dominion == null)
        {
            Debug.LogError($"Chandlery RoomInstance: No dominion for room '{_def.Id}' — cannot build sphere '{def.Id}'");
            return;
        }

        if (archetype == null)
        {
            Debug.LogError($"Chandlery RoomInstance: No {type} archetype for room '{_def.Id}' — cannot build sphere '{def.Id}'");
            return;
        }

        var go = UnityEngine.Object.Instantiate(archetype, dominion.transform, false);
        go.SetActive(true);
        go.name = prefix + def.Id;

        var oldSpec = go.GetComponent<PermanentSphereSpec>();
        if (oldSpec != null)
            UnityEngine.Object.DestroyImmediate(oldSpec);

        var oldLazy = go.GetComponentsInChildren<ILazyEdenable>(true);
        foreach (var s in oldLazy)
            if (s is MonoBehaviour mb)
                UnityEngine.Object.DestroyImmediate(mb);

        AssignPositionAndSize(go, def.PosX ?? 0f, def.PosY ?? 0f, def.Width ?? 120f, def.Height ?? 120f);
        ConfigureCanvasGroup(go);
        ReplaceChoreographerGeneric(choreographerType, go);

        if (applyFields)
        {
            ConfigurePhysicalSphereFields(go, def.LockDrag ?? false, def.ShowGlowOnHover ?? false, def.ShowInteractionGlow ?? false);
            ConfigureSphereDropCatcher(go);
        }

        AddSphereSpec(go, def.Id, def.Label, def.Description, def.Required, def.Essential, def.Forbidden);
        AddSeeds(go, def.Seeds);
    }

    private void BuildWorkstation(WorkstationDefinition def)
    {
        var dominion = _worldDominion;
        if (dominion == null) return;

        if (_workstationArchetype == null)
        {
            Debug.LogError($"Chandlery RoomInstance: No workstation archetype for room '{_def.Id}' — cannot build workstation '{def.Id}'");
            return;
        }

        var go = UnityEngine.Object.Instantiate(_workstationArchetype, dominion.transform, false);
        go.SetActive(true);
        go.name = "workstation_" + def.Id;

        AssignPositionAndSize(go, def.PosX ?? 0f, def.PosY ?? 0f, def.Width ?? 120f, def.Height ?? 120f);
        ConfigureCanvasGroup(go);
        ReplaceChoreographer<FitmentChoreographer>(go);

        var ws = go.GetComponent<FitmentWorkstationSphere>();
        if (ws != null)
            typeof(FitmentWorkstationSphere).GetField("seedWithVerbId", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(ws, def.Verb);
    }

    internal void AssignPositionAndSize(GameObject go, float posX, float posY, float width, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            // JSON: (posX, posY) = item's bottom-left corner, (0,0) = room's top-left, Y increases downward
            // Unity: anchoredPosition = item's center relative to room's center, Y increases upward
            var centerX = posX - _roomW * 0.5f + width * 0.5f;
            var centerY = _roomH * 0.5f - posY + height * 0.5f;
            rt.anchoredPosition = new Vector2(centerX, centerY);
            rt.sizeDelta = new Vector2(width, height);
        }
    }

    internal static void ConfigureCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
    }

    internal static void ConfigurePhysicalSphereFields(GameObject go, bool lockDrag, bool showGlowOnHover, bool showInteractionGlow)
    {
        var sphere = go.GetComponent<PhysicalSphere>();
        if (sphere == null) return;

        typeof(PhysicalSphere).GetField("LockDrag", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(sphere, lockDrag);
        typeof(PhysicalSphere).GetField("ShowGlowOnHover", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(sphere, showGlowOnHover);
        typeof(PhysicalSphere).GetField("ShowInteractionGlow", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(sphere, showInteractionGlow);
    }

    internal static void ReplaceChoreographer<T>(GameObject go) where T : AbstractChoreographer
    {
        var existing = go.GetComponent<AbstractChoreographer>();
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
        go.AddComponent<T>();
    }

    internal static void ReplaceChoreographerGeneric(Type choreographerType, GameObject go)
    {
        var existing = go.GetComponent<AbstractChoreographer>();
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
        go.AddComponent(choreographerType);
    }

    internal static void AddSeeds(GameObject go, List<SeedEntry> seeds)
    {
        if (seeds == null || seeds.Count == 0)
            return;

        var seedComp = go.AddComponent<ProgrammaticSeed>();
        seedComp.SeedDefs = seeds;
        Debug.Log($"Chandlery RoomInstance: Added {seeds.Count} seed(s) to sphere '{go.name}'");
    }

    internal static void ConfigureSphereDropCatcher(GameObject go)
    {
        var sphere = go.GetComponent<Sphere>();
        if (sphere == null) return;

        var catcher = go.GetComponentInChildren<SphereDropCatcher>(true);
        if (catcher != null)
        {
            catcher.Sphere = sphere;
        }
        else
            Debug.LogWarning($"Chandlery RoomInstance: No SphereDropCatcher found in children of '{go.name}'");
    }

    internal static void AddSphereSpec(GameObject go, string id, string label, string description,
        Dictionary<string, int> required, Dictionary<string, int> essential, Dictionary<string, int> forbidden)
    {
        var spec = go.AddComponent<PermanentSphereSpec>();
        spec.ApplyId = id;
        spec.Title = label;
        spec.Description = description;
        spec.Required = AspectSpecsFromDict(required);
        spec.Essential = AspectSpecsFromDict(essential);
        spec.Forbidden = AspectSpecsFromDict(forbidden);

        typeof(PermanentSphereSpec).GetField("_cachedPermanentPayloads",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(spec, Array.Empty<AbstractPermanentPayload>());
    }

    internal static AspectSpec[] AspectSpecsFromDict(Dictionary<string, int> dict)
    {
        if (dict == null || dict.Count == 0)
            return null;

        return dict.Select(kvp => new AspectSpec { name = kvp.Key, level = kvp.Value }).ToArray();
    }
}
