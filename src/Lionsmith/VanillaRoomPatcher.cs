using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SecretHistories;
using SecretHistories.Abstract;
using SecretHistories.Assets.Scripts.Application.Spheres.Dominions;
using SecretHistories.Entities;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Manifestations;
using SecretHistories.Services;
using SecretHistories.Spheres;
using SecretHistories.Spheres.Choreographers;
using SecretHistories.Tokens;
using SecretHistories.Tokens.Payloads;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse;

internal class VanillaRoomPatcher
{
    internal void Patch(CustomTerrainDefinition def)
    {
        try
        {
            var token = Watchman.Get<HornedAxe>().FindSingleOrDefaultTokenById(def.Id);
            if (token?.Payload is not TerrainFeature tf)
            {
                Debug.LogWarning($"Chandlery Lionsmith: Vanilla room '{def.Id}' not found — cannot apply overrides");
                return;
            }

            PatchSprites(tf, def);
            PatchPosition(tf, def);
            PatchSize(tf, def);
            PatchAspects(tf, def);
            PatchContents(tf.gameObject, def);

            Debug.Log($"Chandlery Lionsmith: Patched vanilla room '{def.Id}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chandlery Lionsmith: Failed to patch vanilla room '{def.Id}': {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void PatchSprites(TerrainFeature terrainFeature, CustomTerrainDefinition def)
    {
        if (def.Sprite == null && def.ShroudSprite == null)
            return;

        var modManager = Watchman.Get<ModManager>();
        var manifestationGo = terrainFeature.GetComponentInChildren<IManifestation>() as MonoBehaviour;
        if (manifestationGo == null)
            return;

        foreach (var img in manifestationGo.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null)
                continue;

            switch (img.name)
            {
                case "RoomImage" when def.Sprite != null:
                    var newSprite = modManager.GetSprite("images\\terrain\\" + def.Sprite);
                    if (newSprite != null)
                        img.sprite = newSprite;
                    else
                        Debug.LogWarning($"Chandlery Lionsmith: Override sprite 'images\\terrain\\{def.Sprite}' not found for room '{def.Id}'");
                    break;

                case "ShroudedImage" when def.ShroudSprite != null:
                    var newShroud = modManager.GetSprite("images\\terrain\\" + def.ShroudSprite);
                    if (newShroud != null)
                        img.sprite = newShroud;
                    else
                        Debug.LogWarning($"Chandlery Lionsmith: Override shroud sprite 'images\\terrain\\{def.ShroudSprite}' not found for room '{def.Id}'");
                    break;
            }
        }
    }

    private static void PatchPosition(TerrainFeature terrainFeature, CustomTerrainDefinition def)
    {
        if (!def.PosX.HasValue || !def.PosY.HasValue)
            return;

        var rt = terrainFeature.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(def.PosX.Value, def.PosY.Value);
    }

    private static void PatchSize(TerrainFeature terrainFeature, CustomTerrainDefinition def)
    {
        if (!def.Width.HasValue && !def.Height.HasValue && string.IsNullOrEmpty(def.RoomSize))
            return;

        var rt = terrainFeature.GetComponent<RectTransform>();

        if (!string.IsNullOrEmpty(def.RoomSize))
        {
            def.ResolveSize(out var w, out var h);
            rt.sizeDelta = new Vector2(w, h);
        }
        else
        {
            var currentSize = rt.sizeDelta;
            rt.sizeDelta = new Vector2(def.Width ?? currentSize.x, def.Height ?? currentSize.y);
        }
    }

    private static void PatchAspects(TerrainFeature terrainFeature, CustomTerrainDefinition def)
    {
        if (def.Aspects == null)
            return;

        var aspectsField = typeof(AbstractPermanentPayload)
            .GetField("_aspects", BindingFlags.Instance | BindingFlags.NonPublic);
        if (aspectsField == null)
            return;

        if (def.Aspects.Count == 0)
            aspectsField.SetValue(terrainFeature, Array.Empty<AspectSpec>());
        else
            aspectsField.SetValue(terrainFeature,
                def.Aspects.Select(kv => new AspectSpec { name = kv.Key, level = kv.Value }).ToArray());
    }

    private static void PatchContents(GameObject roomGo, CustomTerrainDefinition def)
    {
        var contents = def.Contents;
        if (contents == null)
            return;

        if (contents.remove_spheres != null)
            foreach (var specId in contents.remove_spheres)
            {
                var found = FindSphereBySpecId(roomGo, specId);
                if (found != null)
                {
                    found.SetActive(false);
                    Debug.Log($"Chandlery Lionsmith: Removed sphere '{specId}' from room '{def.Id}'");
                }
                else
                    Debug.LogWarning($"Chandlery Lionsmith: Sphere '{specId}' not found for removal in room '{def.Id}'");
            }

        if (contents.Slots != null)
            foreach (var sd in contents.Slots)
                AddOrModifySphere(roomGo, sd, typeof(ThingChoreographer), def.Id);

        if (contents.Workstations != null)
            foreach (var wd in contents.Workstations)
                AddOrModifyWorkstation(roomGo, wd, def.Id);

        if (contents.Shelves != null)
            foreach (var sd in contents.Shelves)
                AddOrModifySphere(roomGo, sd, typeof(ShelfChoreographer), def.Id);

        if (contents.Comforts != null)
            foreach (var cd in contents.Comforts)
                AddOrModifySphere(roomGo, cd, typeof(ThingChoreographer), def.Id);

        if (contents.WallArts != null)
            foreach (var wad in contents.WallArts)
                AddOrModifySphere(roomGo, wad, typeof(WallChoreographer), def.Id);
    }

    private static void AddOrModifySphere(GameObject roomGo, ISphereOverrideTarget def,
        Type choreographerType, string roomId)
    {
        var existing = FindSphereBySpecId(roomGo, def.Id);

        if (existing != null)
            ModifyExistingSphere(existing, def, roomGo);
        else
            AddNewSphere(roomGo, def, choreographerType, roomId);
    }

    private static void AddOrModifyWorkstation(GameObject roomGo, WorkstationDefinition def, string roomId)
    {
        var existing = FindSphereBySpecId(roomGo, def.Id);

        if (existing != null)
        {
            ModifyExistingSphere(existing, def, roomGo);
            return;
        }

        var archetype = FindArchetypeByComponent(roomGo, typeof(FitmentWorkstationSphere));
        if (archetype == null)
        {
            Debug.LogWarning($"Chandlery Lionsmith: Cannot add workstation '{def.Id}' — no archetype in room '{roomId}'");
            return;
        }

        var dominion = FindDominion(roomGo, false);
        if (dominion == null) return;

        var go = UnityEngine.Object.Instantiate(archetype, dominion.transform, false);
        go.SetActive(false);
        go.name = "workstation_" + def.Id;

        var oldSpec = go.GetComponent<PermanentSphereSpec>();
        if (oldSpec != null)
            UnityEngine.Object.DestroyImmediate(oldSpec);

        ApplySphereTransform(go, roomGo, def.PosX ?? 0f, def.PosY ?? 0f, def.Width ?? 120f, def.Height ?? 120f);

        RoomInstance.ConfigureCanvasGroup(go);
        RoomInstance.ReplaceChoreographer<FitmentChoreographer>(go);

        var ws = go.GetComponent<FitmentWorkstationSphere>();
        if (ws != null && def.Verb != null)
            typeof(FitmentWorkstationSphere).GetField("seedWithVerbId",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(ws, def.Verb);

        go.SetActive(true);
    }

    private static void AddNewSphere(GameObject roomGo, ISphereOverrideTarget def,
        Type choreographerType, string roomId)
    {
        var archetype = FindArchetypeForOverride(roomGo, def, choreographerType);
        if (archetype == null)
        {
            Debug.LogWarning($"Chandlery Lionsmith: Cannot add sphere '{def.Id}' — no archetype in room '{roomId}'");
            return;
        }

        var dominion = FindDominion(roomGo, choreographerType == typeof(ShelfChoreographer));
        if (dominion == null)
        {
            Debug.LogWarning($"Chandlery Lionsmith: Cannot add sphere '{def.Id}' — no suitable dominion in room '{roomId}'");
            return;
        }

        var go = UnityEngine.Object.Instantiate(archetype, dominion.transform, false);
        go.SetActive(false);
        go.name = def.Id + "_override";

        var oldSpec = go.GetComponent<PermanentSphereSpec>();
        if (oldSpec != null)
            UnityEngine.Object.DestroyImmediate(oldSpec);

        ApplySphereTransform(go, roomGo, def.PosX ?? 0f, def.PosY ?? 0f,
            def.Width ?? 120f, def.Height ?? 120f);

        RoomInstance.ConfigureCanvasGroup(go);
        RoomInstance.ReplaceChoreographerGeneric(choreographerType, go);
        RoomInstance.ConfigurePhysicalSphereFields(go, def.LockDrag ?? false,
            def.ShowGlowOnHover ?? false, def.ShowInteractionGlow ?? false);

        RoomInstance.AddSphereSpec(go, def.Id, def.Label, def.Description,
            def.Required, def.Essential, def.Forbidden);
        RoomInstance.AddSeeds(go, def.Seeds);

        RoomInstance.ConfigureSphereDropCatcher(go);

        go.SetActive(true);
    }

    private static void ModifyExistingSphere(GameObject sphereGo, ISphereOverrideTarget def, GameObject roomGo)
    {
        var rt = sphereGo.GetComponent<RectTransform>();
        var roomRt = roomGo.GetComponent<RectTransform>();
        var roomW = roomRt?.sizeDelta.x ?? 400f;
        var roomH = roomRt?.sizeDelta.y ?? 200f;

        if (def.PosX != null || def.PosY != null || def.Width != null || def.Height != null)
        {
            var posX = def.PosX ?? 0f;
            var posY = def.PosY ?? 0f;
            var width = def.Width ?? rt?.sizeDelta.x ?? 120f;
            var height = def.Height ?? rt?.sizeDelta.y ?? 120f;
            var centerX = posX - roomW * 0.5f + width * 0.5f;
            var centerY = roomH * 0.5f - posY + height * 0.5f;
            if (rt != null)
            {
                if (def.PosX != null || def.PosY != null)
                    rt.anchoredPosition = new Vector2(centerX, centerY);
                if (def.Width != null || def.Height != null)
                    rt.sizeDelta = new Vector2(width, height);
            }
        }

        if (def.Greedy != null)
        {
            var sphere = sphereGo.GetComponent<PhysicalSphere>();
            if (sphere != null)
                typeof(PhysicalSphere).GetField("Greedy", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(sphere, def.Greedy.Value);
        }

        if (def.LockDrag != null || def.ShowGlowOnHover != null || def.ShowInteractionGlow != null)
        {
            RoomInstance.ConfigurePhysicalSphereFields(sphereGo,
                def.LockDrag ?? false,
                def.ShowGlowOnHover ?? false,
                def.ShowInteractionGlow ?? false);
        }

        if (def.Seeds != null)
        {
            var oldSeeds = sphereGo.GetComponentsInChildren<ILazyEdenable>(true);
            foreach (var s in oldSeeds)
                if (s is MonoBehaviour mb)
                    GameObject.DestroyImmediate(mb);

            if (def.Seeds.Count > 0)
                RoomInstance.AddSeeds(sphereGo, def.Seeds);
        }

        if (def.Required != null || def.Essential != null || def.Forbidden != null)
        {
            var oldSpec = sphereGo.GetComponent<PermanentSphereSpec>();
            if (oldSpec != null)
            {
                GameObject.DestroyImmediate(oldSpec);
                RoomInstance.AddSphereSpec(sphereGo, def.Id,
                    def.Label ?? oldSpec.Title,
                    def.Description ?? oldSpec.Description,
                    def.Required, def.Essential, def.Forbidden);
            }
        }
    }

    private static void ApplySphereTransform(GameObject sphereGo, GameObject roomGo,
        float posX, float posY, float width, float height)
    {
        var rt = sphereGo.GetComponent<RectTransform>();
        var roomRt = roomGo.GetComponent<RectTransform>();
        var roomW = roomRt?.sizeDelta.x ?? 400f;
        var roomH = roomRt?.sizeDelta.y ?? 200f;
        var centerX = posX - roomW * 0.5f + width * 0.5f;
        var centerY = roomH * 0.5f - posY + height * 0.5f;
        rt.anchoredPosition = new Vector2(centerX, centerY);
        rt.sizeDelta = new Vector2(width, height);
    }

    private static GameObject FindSphereBySpecId(GameObject root, string specId)
    {
        foreach (var pspec in root.GetComponentsInChildren<PermanentSphereSpec>(true))
            if (pspec.ApplyId == specId)
                return pspec.gameObject;
        foreach (var sphere in root.GetComponentsInChildren<Sphere>(true))
            if (sphere.GoverningSphereSpec?.Id == specId)
                return sphere.gameObject;
        return null;
    }

    private static GameObject FindArchetypeForOverride(GameObject roomGo, ISphereOverrideTarget def, Type choreographerType)
    {
        if (choreographerType == typeof(FitmentChoreographer))
            return FindArchetypeByComponent(roomGo, typeof(FitmentWorkstationSphere));
        if (choreographerType == typeof(ShelfChoreographer))
            return FindArchetypeByComponent(roomGo, typeof(ShelfSpaceSphere));
        if (choreographerType == typeof(ThingChoreographer))
            return FindArchetypeByComponentWithFilter(roomGo, typeof(PhysicalSphere),
                s => !(s is FitmentWorkstationSphere) && !(s is ComfortSphere));
        if (choreographerType == typeof(WallChoreographer))
            return FindArchetypeByComponent(roomGo, typeof(PhysicalSphere)); // wall arts don't have a unique component

        return FindArchetypeByComponent(roomGo, typeof(PhysicalSphere));
    }

    private static GameObject FindArchetypeByComponent(GameObject roomGo, Type componentType)
    {
        foreach (var comp in roomGo.GetComponentsInChildren(componentType, true))
        {
            var go = comp.gameObject;
            if (go != roomGo && !go.name.StartsWith("__archetype_") && !go.name.EndsWith("_override"))
                return go;
        }
        return null;
    }

    private static GameObject FindArchetypeByComponentWithFilter(GameObject roomGo, Type componentType, Func<Component, bool> filter)
    {
        foreach (var comp in roomGo.GetComponentsInChildren(componentType, true))
        {
            if (!filter(comp)) continue;
            var go = comp.gameObject;
            if (go != roomGo && !go.name.StartsWith("__archetype_") && !go.name.EndsWith("_override"))
                return go;
        }
        return null;
    }

    private static GameObject FindDominion(GameObject roomGo, bool preferShelf)
    {
        if (preferShelf)
        {
            var shelfDom = roomGo.GetComponentInChildren<ShelfDominion>();
            if (shelfDom != null)
                return shelfDom.gameObject;
        }

        var worldDom = roomGo.GetComponentInChildren<WorldDominion>();
        if (worldDom != null)
            return worldDom.gameObject;

        var manifestation = roomGo.GetComponentInChildren<IManifestation>() as MonoBehaviour;
        if (manifestation != null)
            return manifestation.gameObject;

        return null;
    }
}