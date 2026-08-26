using System;
using System.Linq;
using System.Reflection;
using SecretHistories;
using SecretHistories.Abstract;
using SecretHistories.Assets.Scripts.Application.Spheres.Dominions;
using SecretHistories.Entities;
using SecretHistories.Spheres;
using SecretHistories.Spheres.Choreographers;
using SecretHistories.Tokens.Payloads;
using SecretHistories.UI;
using TheHouse.Wheel;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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
            PatchSize(tf, def);
            PatchPosition(tf, def);
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
                    var newSprite = TerrainRegistry.FindSprite(def.Sprite);
                    if (newSprite != null)
                        img.sprite = newSprite;
                    else
                        Debug.LogWarning($"Chandlery Lionsmith: Override sprite '{def.Sprite}' not found for room '{def.Id}'");
                    break;

                case "ShroudedImage" when def.ShroudSprite != null:
                    var newShroud = TerrainRegistry.FindSprite(def.ShroudSprite);
                    if (newShroud != null)
                        img.sprite = newShroud;
                    else
                        Debug.LogWarning($"Chandlery Lionsmith: Override shroud sprite '{def.ShroudSprite}' not found for room '{def.Id}'");
                    break;
            }
        }
    }

    private static void PatchPosition(TerrainFeature terrainFeature, CustomTerrainDefinition def)
    {
        if (!def.PosX.HasValue || !def.PosY.HasValue)
            return;

        var rt = terrainFeature.GetComponent<RectTransform>();
        var halfW = rt.sizeDelta.x * 0.5f;
        var halfH = rt.sizeDelta.y * 0.5f;
        rt.anchoredPosition = new Vector2(def.PosX.Value + halfW, def.PosY.Value + halfH);
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
        if (!def.WasPropertySpecified("aspects"))
            return;

        var aspectsField = typeof(AbstractPermanentPayload)
            .GetField("_aspects", BindingFlags.Instance | BindingFlags.NonPublic);
        if (aspectsField == null)
            return;

        aspectsField.SetValue(terrainFeature,
            def.Aspects.Count == 0
                ? Array.Empty<AspectSpec>()
                : def.Aspects.Select(kv => new AspectSpec { name = kv.Key, level = kv.Value }).ToArray());
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

        if (contents.Spheres != null)
            foreach (var sd in contents.Spheres)
                AddOrModifySphere(roomGo, sd, sd.SphereType ?? "normal", def.Id);

        if (contents.Workstations != null)
            foreach (var wd in contents.Workstations)
                AddOrModifyWorkstation(roomGo, wd, def.Id);
    }

    private static void AddOrModifySphere(GameObject roomGo, ISphereOverrideTarget def,
        string sphereType, string roomId)
    {
        var existing = FindSphereBySpecId(roomGo, def.Id);

        if (existing != null)
            ModifyExistingSphere(existing, def, roomGo);
        else
            AddNewSphere(roomGo, def, sphereType, roomId);
    }

    private static void AddOrModifyWorkstation(GameObject roomGo, WorkstationDefinition def, string roomId)
    {
        var existing = FindSphereBySpecId(roomGo, def.Id);

        if (existing != null)
        {
            ModifyExistingSphere(existing, def, roomGo);
            return;
        }

        var archetype = FindArchetype(roomGo, typeof(FitmentWorkstationSphere), null);
        if (archetype == null)
        {
            Debug.LogWarning($"Chandlery Lionsmith: Cannot add workstation '{def.Id}' — no archetype in room '{roomId}'");
            return;
        }

        var dominion = FindDominion(roomGo, false);
        if (dominion == null) return;

        var go = Object.Instantiate(archetype, dominion.transform, false);
        go.SetActive(false);
        go.name = "workstation_" + def.Id;

        var oldSpec = go.GetComponent<PermanentSphereSpec>();
        if (oldSpec != null)
            Object.DestroyImmediate(oldSpec);

        ApplySphereTransform(go, roomGo, def.PosX ?? 0f, def.PosY ?? 0f, def.Width ?? 120f, def.Height ?? 120f);

        RoomInstance.ConfigureCanvasGroup(go);
        RoomInstance.ReplaceChoreographer<FitmentChoreographer>(go);

        var ws = go.GetComponent<FitmentWorkstationSphere>();
        if (ws != null && def.Verb != null)
            typeof(FitmentWorkstationSphere).GetField("seedWithVerbId",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(ws, def.Verb);

        RoomInstance.AddSphereSpec(go, def.Id, def.Label, def.Description,
            def.Required, def.Essential, def.Forbidden);
        RegisterSphereInRoom(go, dominion, roomGo);

        go.SetActive(true);
    }

    private static Type ChoreographerForSphereType(string sphereType)
    {
        switch (sphereType ?? "normal")
        {
            case "bookshelf": return typeof(ShelfChoreographer);
            case "wall":      return typeof(WallChoreographer);
            case "comfort":
            default:          return typeof(ThingChoreographer);
        }
    }

    private static void AddNewSphere(GameObject roomGo, ISphereOverrideTarget def,
        string sphereType, string roomId)
    {
        var archetype = FindArchetypeForOverride(roomGo, sphereType);
        if (archetype == null)
        {
            Debug.LogWarning($"Chandlery Lionsmith: Cannot add sphere '{def.Id}' — no archetype in room '{roomId}'");
            return;
        }

        var choreographerType = ChoreographerForSphereType(sphereType);
        var dominion = FindDominion(roomGo, sphereType == "bookshelf");
        if (dominion == null)
        {
            Debug.LogWarning($"Chandlery Lionsmith: Cannot add sphere '{def.Id}' — no suitable dominion in room '{roomId}'");
            return;
        }

        var go = Object.Instantiate(archetype, dominion.transform, false);
        go.SetActive(false);
        go.name = def.Id + "_override";

        var oldSpec = go.GetComponent<PermanentSphereSpec>();
        if (oldSpec != null)
            Object.DestroyImmediate(oldSpec);

        foreach (var token in go.GetComponentsInChildren<Token>(true))
            if (token.gameObject != go)
                Object.DestroyImmediate(token.gameObject);

        DestroySeeds(go);

        ApplySphereTransform(go, roomGo, def.PosX ?? 0f, def.PosY ?? 0f,
            def.Width ?? 120f, def.Height ?? 120f);

        RoomInstance.ConfigureCanvasGroup(go);
        RoomInstance.ReplaceChoreographerGeneric(choreographerType, go);
        RoomInstance.ConfigurePhysicalSphereFields(go, def.LockDrag ?? false,
            def.ShowGlowOnHover ?? false, def.ShowInteractionGlow ?? false);

        RoomInstance.AddSphereSpec(go, def.Id, def.Label, def.Description,
            def.Required, def.Essential, def.Forbidden);
        RegisterSphereInRoom(go, dominion, roomGo);
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
            var width = def.Width ?? rt?.sizeDelta.x ?? 120f;
            var height = def.Height ?? rt?.sizeDelta.y ?? 120f;
            if (rt != null)
            {
                if (def.PosX != null || def.PosY != null)
                {
                    var posX = def.PosX ?? (rt.anchoredPosition.x + roomW * 0.5f - width * 0.5f);
                    var posY = def.PosY ?? (rt.anchoredPosition.y + roomH * 0.5f - height * 0.5f);
                    var centerX = posX - roomW * 0.5f + width * 0.5f;
                    var centerY = posY - roomH * 0.5f + height * 0.5f;
                    rt.anchoredPosition = new Vector2(centerX, centerY);
                }
                if (def.Width != null || def.Height != null)
                    rt.sizeDelta = new Vector2(width, height);
            }
        }

        if (def.LockDrag != null || def.ShowGlowOnHover != null || def.ShowInteractionGlow != null)
        {
            RoomInstance.ConfigurePhysicalSphereFields(sphereGo,
                def.LockDrag ?? GetPhysicalSphereField(sphereGo, "LockDrag"),
                def.ShowGlowOnHover ?? GetPhysicalSphereField(sphereGo, "ShowGlowOnHover"),
                def.ShowInteractionGlow ?? GetPhysicalSphereField(sphereGo, "ShowInteractionGlow"));
        }

        if (def.WasPropertySpecified("seeds"))
        {
            DestroySeeds(sphereGo);

            if (def.Seeds.Count > 0)
                RoomInstance.AddSeeds(sphereGo, def.Seeds);
        }

        if (def.WasPropertySpecified("required") || def.WasPropertySpecified("essential")
            || def.WasPropertySpecified("forbidden")
            || def.Label != null || def.Description != null)
        {
            var spec = sphereGo.GetComponent<PermanentSphereSpec>();
            if (spec != null)
            {
                if (def.Label != null)
                    spec.Title = def.Label;
                if (def.Description != null)
                    spec.Description = def.Description;
                if (def.WasPropertySpecified("required"))
                    spec.Required = RoomInstance.AspectSpecsFromDict(def.Required);
                if (def.WasPropertySpecified("essential"))
                    spec.Essential = RoomInstance.AspectSpecsFromDict(def.Essential);
                if (def.WasPropertySpecified("forbidden"))
                    spec.Forbidden = RoomInstance.AspectSpecsFromDict(def.Forbidden);

                EnsurePermanentPayloads(spec);
                spec.ApplySpecToSphere(sphereGo.GetComponent<Sphere>());
            }
        }
    }

    private static void RegisterSphereInRoom(GameObject sphereGo, GameObject dominionGo, GameObject roomGo)
    {
        var sphere = sphereGo.GetComponent<Sphere>();
        var spec = sphereGo.GetComponent<PermanentSphereSpec>();
        if (sphere == null || spec == null)
            return;

        spec.ApplySpecToSphere(sphere);

        var dominion = dominionGo?.GetComponent<AbstractDominion>();
        if (dominion != null)
        {
            var spheres = typeof(AbstractDominion)
                .GetField("_spheres", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(dominion) as System.Collections.IList;
            if (spheres != null && !spheres.Contains(sphere))
                spheres.Add(sphere);

            sphere.Subscribe(dominion);
        }

        roomGo.GetComponent<TerrainFeature>()?.AttachSphere(sphere);
    }

    private static bool GetPhysicalSphereField(GameObject go, string fieldName)
    {
        var sphere = go.GetComponent<PhysicalSphere>();
        if (sphere == null)
            return false;

        return (bool)(typeof(PhysicalSphere)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(sphere) ?? false);
    }

    private static void EnsurePermanentPayloads(PermanentSphereSpec spec)
    {
        // Re-applying a spec must not re-run InitialisePermanentPayloads, which
        // would spawn duplicate tokens for already-initialised permanent payloads.
        typeof(PermanentSphereSpec)
            .GetField("_cachedPermanentPayloads", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(spec, Array.Empty<AbstractPermanentPayload>());
    }

    private static void DestroySeeds(GameObject root)
    {
        foreach (var lazy in root.GetComponentsInChildren<ILazyEdenable>(true))
        {
            if (lazy is not MonoBehaviour mb || mb is Sphere || mb.gameObject == root)
                continue;

            Object.DestroyImmediate(mb.gameObject);
        }
    }

    private static void ApplySphereTransform(GameObject sphereGo, GameObject roomGo,
        float posX, float posY, float width, float height)
    {
        var rt = sphereGo.GetComponent<RectTransform>();
        var roomRt = roomGo.GetComponent<RectTransform>();
        var roomW = roomRt?.sizeDelta.x ?? 400f;
        var roomH = roomRt?.sizeDelta.y ?? 200f;
        // JSON: (posX, posY) = item's bottom-left corner, (0,0) = room's bottom-left, Y increases upward
        var centerX = posX - roomW * 0.5f + width * 0.5f;
        var centerY = posY - roomH * 0.5f + height * 0.5f;
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

    private static GameObject FindArchetypeForOverride(GameObject roomGo, string sphereType)
    {
        switch (sphereType ?? "normal")
        {
            case "bookshelf":
                return FindArchetype(roomGo, typeof(ShelfSpaceSphere), null);
            case "comfort":
                return FindArchetype(roomGo, typeof(ComfortSphere), null);
            case "wall":
                return FindArchetype(roomGo, typeof(PhysicalSphere),
                    s => !(s is FitmentWorkstationSphere) && !(s is ComfortSphere));
            default:
                return FindArchetype(roomGo, typeof(PhysicalSphere),
                    s => !(s is FitmentWorkstationSphere) && !(s is ComfortSphere));
        }
    }

    private static GameObject FindArchetype(GameObject roomGo, Type componentType, Func<Component, bool> filter)
    {
        var found = FindInRoot(roomGo, componentType, filter);
        if (found != null)
            return found;

        foreach (var tf in Resources.FindObjectsOfTypeAll<TerrainFeature>())
        {
            if (!tf.gameObject.scene.IsValid() || tf.gameObject == roomGo)
                continue;

            found = FindInRoot(tf.gameObject, componentType, filter);
            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject FindInRoot(GameObject root, Type componentType, Func<Component, bool> filter)
    {
        foreach (var comp in root.GetComponentsInChildren(componentType, true))
        {
            if (filter != null && !filter(comp))
                continue;

            var go = comp.gameObject;
            if (go != root && !go.name.StartsWith("__archetype_") && !go.name.EndsWith("_override"))
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