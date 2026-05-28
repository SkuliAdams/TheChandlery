using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SecretHistories.Assets.Scripts.Application.Spheres.Dominions;
using SecretHistories.Spheres;
using SecretHistories.Abstract;
using SecretHistories.Tokens;
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

    private GameObject _worldDominion;
    private GameObject _shelfDominion;

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
    }

    public void PopulateContents()
    {
        var contents = _def.Contents;
        if (contents == null)
            return;

        var hasContent = (contents.Slots != null && contents.Slots.Count > 0)
                      || (contents.Workstations != null && contents.Workstations.Count > 0)
                      || (contents.Shelves != null && contents.Shelves.Count > 0)
                      || (contents.Comforts != null && contents.Comforts.Count > 0);

        if (!hasContent)
            return;

        BuildDominions();

        if (contents.Slots != null)
            foreach (var sd in contents.Slots)
                BuildSphere(sd);

        if (contents.Workstations != null)
            foreach (var wd in contents.Workstations)
                BuildWorkstation(wd);

        if (contents.Shelves != null)
            foreach (var sd in contents.Shelves)
                BuildShelfSphere(sd);

        if (contents.Comforts != null)
            foreach (var cd in contents.Comforts)
                BuildComfort(cd);
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

            clone.SetActive(false);
            Debug.Log($"Chandlery RoomInstance: Cloned archetype '{name}' from {comp.GetType().Name} '{comp.name}'");
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

    private void BuildSphere(SlotDefinition def)
    {
        var dominion = _worldDominion;
        if (dominion == null) return;

        if (_slotArchetype == null)
        {
            Debug.LogError($"Chandlery RoomInstance: No slot archetype for room '{_def.Id}' — cannot build sphere '{def.Id}'");
            return;
        }

        var go = UnityEngine.Object.Instantiate(_slotArchetype, dominion.transform, false);
        go.SetActive(true);
        go.name = "slot_" + def.Id;

        var oldSpec = go.GetComponent<PermanentSphereSpec>();
        if (oldSpec != null)
            UnityEngine.Object.DestroyImmediate(oldSpec);

        AssignPositionAndSize(go, def.PosX, def.PosY, def.Width, def.Height);
        ConfigureCanvasGroup(go);
        ConfigurePhysicalSphereFields(go, def.LockDrag, def.ShowGlowOnHover, def.ShowInteractionGlow);
        ConfigureSphereDropCatcher(go);
        AddSphereSpec(go, def.Id, def.Label, def.Description, def.Required, def.Essential, def.Forbidden);
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

        AssignPositionAndSize(go, def.PosX, def.PosY, def.Width, def.Height);
        ConfigureCanvasGroup(go);

        var ws = go.GetComponent<FitmentWorkstationSphere>();
        if (ws != null)
            typeof(FitmentWorkstationSphere).GetField("seedWithVerbId", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(ws, def.Verb);
    }

    private void BuildShelfSphere(ShelfDefinition def)
    {
        var dominion = _shelfDominion ?? _worldDominion;
        if (dominion == null) return;

        if (_shelfArchetype == null)
        {
            Debug.LogError($"Chandlery RoomInstance: No shelf archetype for room '{_def.Id}' — cannot build shelf '{def.Id}'");
            return;
        }

        var go = UnityEngine.Object.Instantiate(_shelfArchetype, dominion.transform, false);
        go.SetActive(true);
        go.name = "shelf_" + def.Id;

        var oldSpec = go.GetComponent<PermanentSphereSpec>();
        if (oldSpec != null)
            UnityEngine.Object.DestroyImmediate(oldSpec);

        AssignPositionAndSize(go, def.PosX, def.PosY, def.Width, def.Height);
        ConfigureCanvasGroup(go);
        AddSphereSpec(go, def.Id, def.Label, def.Description, def.Required, def.Essential, def.Forbidden);
    }

    private void BuildComfort(ComfortDefinition def)
    {
        var dominion = _worldDominion;
        if (dominion == null) return;

        if (_comfortArchetype == null)
        {
            Debug.LogError($"Chandlery RoomInstance: No comfort archetype for room '{_def.Id}' — cannot build comfort '{def.Id}'");
            return;
        }

        var go = UnityEngine.Object.Instantiate(_comfortArchetype, dominion.transform, false);
        go.SetActive(true);
        go.name = "comfort_" + def.Id;

        var oldSpec = go.GetComponent<PermanentSphereSpec>();
        if (oldSpec != null)
            UnityEngine.Object.DestroyImmediate(oldSpec);

        AssignPositionAndSize(go, def.PosX, def.PosY, def.Width, def.Height);
        ConfigureCanvasGroup(go);
        ConfigurePhysicalSphereFields(go, def.LockDrag, def.ShowGlowOnHover, def.ShowInteractionGlow);
        ConfigureSphereDropCatcher(go);
        AddSphereSpec(go, def.Id, def.Label, def.Description, def.Required, def.Essential, def.Forbidden);
    }

    /*
    // From-scratch sphere construction — kept for reference but unused.
    // Archetypes are always expected to be present in the template room.
    private static GameObject BuildSphereFromScratch(GameObject dominion, SlotDefinition def)
    {
        var go = new GameObject("slot_" + def.Id);
        go.transform.SetParent(dominion.transform, false);

        go.AddComponent<RectTransform>();
        go.AddComponent<CanvasGroup>();
        go.AddComponent<CanvasGroupFader>();
        go.AddComponent<ThingChoreographer>();
        go.AddComponent<PhysicalSphere>();
        go.AddComponent<TokenMovementReactionDecorator>();

        var dropCatcherGo = new GameObject("DropCatcher");
        dropCatcherGo.transform.SetParent(go.transform, false);
        var drt = dropCatcherGo.AddComponent<RectTransform>();
        drt.anchorMin = Vector2.zero;
        drt.anchorMax = Vector2.one;
        drt.sizeDelta = Vector2.zero;
        var dimg = dropCatcherGo.AddComponent<Image>();
        dimg.color = new Color(1f, 1f, 1f, 0f);
        dimg.raycastTarget = true;
        dropCatcherGo.AddComponent<SphereDropCatcher>();

        var tokenContainer = new GameObject("TokenContainer");
        tokenContainer.transform.SetParent(go.transform, false);

        var slotImage = new GameObject("SlotImage");
        slotImage.transform.SetParent(go.transform, false);
        var srt = slotImage.AddComponent<RectTransform>();
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.sizeDelta = new Vector2(-2f, -2f);
        var simg = slotImage.AddComponent<Image>();
        simg.color = new Color(1f, 1f, 1f, 0.15f);
        simg.raycastTarget = false;

        return go;
    }
    */

    private static void AssignPositionAndSize(GameObject go, float posX, float posY, float width, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = new Vector2(posX, posY);
            rt.sizeDelta = new Vector2(width, height);
        }
    }

    private static void ConfigureCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
    }

    private static void ConfigurePhysicalSphereFields(GameObject go, bool lockDrag, bool showGlowOnHover, bool showInteractionGlow)
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

    private static void ConfigureSphereDropCatcher(GameObject go)
    {
        var sphere = go.GetComponent<Sphere>();
        if (sphere == null) return;

        var catcher = go.GetComponentInChildren<SphereDropCatcher>();
        if (catcher != null)
            catcher.Sphere = sphere;
    }

    private static void AddSphereSpec(GameObject go, string id, string label, string description,
        Dictionary<string, int> required, Dictionary<string, int> essential, Dictionary<string, int> forbidden)
    {
        var spec = go.AddComponent<PermanentSphereSpec>();
        spec.ApplyId = id;
        spec.Title = label;
        spec.Description = description;
        spec.Required = AspectSpecsFromDict(required);
        spec.Essential = AspectSpecsFromDict(essential);
        spec.Forbidden = AspectSpecsFromDict(forbidden);

        // PermanentSpecIdUpdater.Start() calls CachePermanentPayloads() on the next frame,
        // but SetUpAsTokenWithId → RegisterFor → ApplySpecToSphere runs synchronously
        // and enumerates _cachedPermanentPayloads, which would NPE if null.
        typeof(PermanentSphereSpec).GetField("_cachedPermanentPayloads",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(spec, Array.Empty<AbstractPermanentPayload>());
    }

    private static AspectSpec[] AspectSpecsFromDict(Dictionary<string, int> dict)
    {
        if (dict == null || dict.Count == 0)
            return null;

        return dict.Select(kvp => new AspectSpec { name = kvp.Key, level = kvp.Value }).ToArray();
    }
}
