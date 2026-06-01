using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SecretHistories;
using SecretHistories.Abstract;
using SecretHistories.Entities;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Manifestations;
using SecretHistories.Services;
using SecretHistories.Spheres;
using SecretHistories.Tokens;
using SecretHistories.Tokens.Payloads;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse;

internal class TerrainFactory
{
    private const string DefaultTemplateId = "watchmanstower1";

    internal void Create(CustomTerrainDefinition def)
    {
        try
        {
            var existing = Watchman.Get<HornedAxe>().FindSingleOrDefaultTokenById(def.Id);
            if (existing != null && existing.IsValid())
            {
                Debug.Log($"Chandlery Lionsmith: Room '{def.Id}' already exists — skipping");
                return;
            }

            var parentSphere = FindParentSphere();
            if (parentSphere == null)
            {
                Debug.LogError($"Chandlery Lionsmith: Cannot find parent sphere for room '{def.Id}'");
                return;
            }

            var root = SetupClone(def, parentSphere);
            if (root == null)
            {
                Debug.LogError($"Chandlery Lionsmith: Failed to setup clone for room '{def.Id}'");
                return;
            }

            var terrainFeature = root.GetComponent<TerrainFeature>();
            (terrainFeature as IEdenable)?.EdenSetup(withLogging: false);

            Debug.Log($"Chandlery Lionsmith: Created room '{def.Id}' at ({def.PosX}, {def.PosY})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chandlery Lionsmith: Failed to create room '{def.Id}': {ex.Message}\n{ex.StackTrace}");
        }
    }

    internal void CreateForLoad(CustomTerrainDefinition def, Sphere sphere)
    {
        try
        {
            var templateId = def.TemplateId ?? DefaultTemplateId;
            var templateToken = Watchman.Get<HornedAxe>().FindSingleOrDefaultTokenById(templateId);

            if (templateToken == null || !templateToken.IsValid())
            {
                Debug.LogError($"Chandlery Lionsmith: Template '{templateId}' not found for room '{def.Id}' — cannot restore from save");
                return;
            }

            if (sphere == null)
            {
                Debug.LogError($"Chandlery Lionsmith: No parent sphere for room '{def.Id}' — cannot restore from save");
                return;
            }

            var clone = SetupClone(def, templateToken, sphere);
            if (clone == null)
            {
                Debug.LogError($"Chandlery Lionsmith: Failed to setup clone for room '{def.Id}' — cannot restore from save");
                return;
            }

            Debug.Log($"Chandlery Lionsmith: Restored room '{def.Id}' from save at ({def.PosX}, {def.PosY})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chandlery Lionsmith: Failed to restore room '{def.Id}' from save: {ex.Message}");
        }
    }

    private static Sphere FindParentSphere()
    {
        var ha = Watchman.Get<HornedAxe>();
        foreach (var sphere in ha.GetSpheres())
        {
            var match = sphere.GetTokens().FirstOrDefault(t => t.Payload is TerrainFeature && t.IsValid());
            if (match != null)
            {
                Debug.Log($"Chandlery Lionsmith: Found parent sphere '{sphere.Id}' via token '{match.PayloadId}'");
                return sphere;
            }
        }
        Debug.LogError("Chandlery Lionsmith: No sphere found containing any TerrainFeature token");
        return null;
    }

    private static GameObject SetupClone(CustomTerrainDefinition def, Sphere sphere)
    {
        var templateToken = Watchman.Get<HornedAxe>().FindSingleOrDefaultTokenById(DefaultTemplateId);
        if (templateToken == null || !templateToken.IsValid())
        {
            Debug.LogWarning($"Chandlery Lionsmith: Template '{DefaultTemplateId}' not found for creating room '{def.Id}'");
            return null;
        }
        return SetupClone(def, templateToken, sphere);
    }

    private static GameObject SetupClone(CustomTerrainDefinition def, Token templateToken, Sphere sphere)
    {
        var templatePayload = templateToken.Payload as TerrainFeature;
        if (templatePayload == null)
        {
            Debug.LogWarning($"Chandlery Lionsmith: Template '{templateToken.PayloadId}' has no TerrainFeature payload");
            return null;
        }

        var clone = GameObject.Instantiate(templatePayload.gameObject, sphere.transform);
        clone.name = def.Id + "_token";

        var terrainFeature = clone.GetComponent<TerrainFeature>();
        if (terrainFeature == null)
        {
            Debug.LogWarning($"Chandlery Lionsmith: Clone has no TerrainFeature");
            GameObject.DestroyImmediate(clone);
            return null;
        }

        var existingToken = clone.GetComponent<Token>();
        if (existingToken != null)
            GameObject.DestroyImmediate(existingToken);

        var initialiseField = typeof(AbstractPermanentPayload)
            .GetField("InitialiseWithIdentifier", BindingFlags.Instance | BindingFlags.NonPublic);
        initialiseField?.SetValue(terrainFeature, def.Id);

        var specSeed = clone.GetComponent<TerrainFeatureSpecSeed>();
        if (specSeed != null)
        {
            specSeed.StartsOpen = def.StartsOpen ?? false;
            specSeed.StartsUnsealed = def.StartsUnsealed ?? true;
        }

        var roomInstance = new RoomInstance(clone, def);
        roomInstance.ExtractArchetypes();
        StripInteractiveChildren(clone);

        def.ResolveSize(out var resolvedW, out var resolvedH, 400f, 200f);
        roomInstance.PopulateContents(resolvedW, resolvedH);

        var rt = clone.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(resolvedW, resolvedH);
        rt.anchoredPosition = new Vector2(
            (def.PosX ?? 0f) + resolvedW * 0.5f,
            (def.PosY ?? 0f) + resolvedH * 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        ApplySprites(terrainFeature, resolvedW, resolvedH, def);

        terrainFeature.SetUpAsTokenWithId(sphere);

        ApplyAspects(terrainFeature, def);

        return clone;
    }

    private static void StripInteractiveChildren(GameObject root)
    {
        var toDestroy = new HashSet<GameObject>();

        var archetypes = new HashSet<GameObject>();
        var archetypeDescendants = new HashSet<GameObject>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("__archetype_"))
            {
                archetypes.Add(t.gameObject);
                foreach (var childT in t.GetComponentsInChildren<Transform>(true))
                    archetypeDescendants.Add(childT.gameObject);
            }

        bool IsProtected(GameObject go) => go == root || archetypes.Contains(go) || archetypeDescendants.Contains(go);

        foreach (var lazy in root.GetComponentsInChildren<ILazyEdenable>(true))
        {
            var mb = lazy as MonoBehaviour;
            if (mb != null && !IsProtected(mb.gameObject))
                toDestroy.Add(mb.gameObject);
        }

        foreach (var dom in root.GetComponentsInChildren<AbstractDominion>(true))
        {
            var mb = dom as MonoBehaviour;
            if (mb != null && !IsProtected(mb.gameObject))
                toDestroy.Add(mb.gameObject);
        }

        foreach (var sphere in root.GetComponentsInChildren<Sphere>(true))
        {
            if (!IsProtected(sphere.gameObject))
                toDestroy.Add(sphere.gameObject);
        }

        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!IsProtected(ps.gameObject))
                toDestroy.Add(ps.gameObject);
        }

        var knownVisualEffectNames = new HashSet<string> { "StationaryFires" };
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (IsProtected(t.gameObject)) continue;
            if (knownVisualEffectNames.Contains(t.name))
                toDestroy.Add(t.gameObject);
        }

        foreach (var go in toDestroy)
            GameObject.DestroyImmediate(go);
    }

    private static void ApplySprites(TerrainFeature terrainFeature, float resolvedW, float resolvedH, CustomTerrainDefinition def)
    {
        var manifestationGo = terrainFeature.GetComponentInChildren<IManifestation>() as MonoBehaviour;
        if (manifestationGo == null)
            return;

        var spriteWidth = Mathf.RoundToInt(resolvedW * 4.2f);
        var spriteHeight = Mathf.RoundToInt(resolvedH * 4.2f);

        foreach (var img in manifestationGo.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null)
                continue;

            switch (img.name)
            {
                case "RoomImage":
                    var spriteKey = def.Sprite ?? def.Id;
                    img.sprite = TerrainRegistry.FindSprite(spriteKey)
                                 ?? CreatePlaceholder(def.Id + "_unshrouded", spriteWidth, spriteHeight);
                    break;

                case "ShroudedImage":
                    var shroudKey = (def.ShroudSprite ?? def.Id) + "_shrouded";
                    img.sprite = TerrainRegistry.FindSprite(shroudKey)
                                 ?? CreatePlaceholder(def.Id + "_shrouded", spriteWidth, spriteHeight);
                    break;

                case "SealedImage":
                    var sealedSprite = GetSealedSprite(resolvedW, resolvedH);
                    if (sealedSprite != null)
                        img.sprite = sealedSprite;
                    break;
            }
        }
    }

    private static void ApplyAspects(TerrainFeature terrainFeature, CustomTerrainDefinition def)
    {
        var aspectsField = typeof(AbstractPermanentPayload)
            .GetField("_aspects", BindingFlags.Instance | BindingFlags.NonPublic);
        if (aspectsField == null)
            return;

        if (def.Aspects == null || def.Aspects.Count == 0)
        {
            aspectsField.SetValue(terrainFeature, Array.Empty<AspectSpec>());
        }
        else
        {
            aspectsField.SetValue(terrainFeature,
                def.Aspects.Select(kv => new AspectSpec { name = kv.Key, level = kv.Value }).ToArray());
        }
    }

    private static Sprite LoadModSprite(ModManager modManager, string path)
    {
        var sprite = modManager.GetSprite(path);
        return sprite;
    }

    private static readonly Dictionary<string, Sprite> _placeholderCache = new();

    private static Sprite CreatePlaceholder(string key, int width, int height)
    {
        if (_placeholderCache.TryGetValue(key, out var cached))
            return cached;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        var pixels = new Color32[width * height];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, byte.MaxValue);
        texture.SetPixels32(pixels);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        _placeholderCache[key] = sprite;

        Debug.Log($"Chandlery Lionsmith: Created white placeholder for '{key}' ({width}x{height})");
        return sprite;
    }

    private static Sprite GetSealedSprite(float w, float h)
    {
        string sealedName;
        if (h > w)
            sealedName = "sealed_tall";
        else if (w >= h * 2f)
            sealedName = "sealed";
        else
            sealedName = "sealed_smol";

        return Resources.Load<Sprite>(sealedName) ?? Watchman.Get<ModManager>().GetSprite(sealedName);
    }
}
