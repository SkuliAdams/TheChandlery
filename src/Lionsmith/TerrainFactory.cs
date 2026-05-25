using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SecretHistories;
using SecretHistories.Abstract;
using SecretHistories.Entities;
using SecretHistories.Infrastructure.Modding;
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

            var templateId = def.TemplateId ?? DefaultTemplateId;
            var templateToken = Watchman.Get<HornedAxe>().FindSingleOrDefaultTokenById(templateId);

            if (templateToken == null || !templateToken.IsValid())
            {
                Debug.LogWarning($"Chandlery Lionsmith: Template '{templateId}' not found for room '{def.Id}'");
                return;
            }

            var sphere = templateToken.Sphere;
            if (sphere == null)
            {
                Debug.LogWarning($"Chandlery Lionsmith: No parent sphere for template '{templateId}'");
                return;
            }

            var clone = SetupClone(def, templateToken, sphere);
            if (clone == null)
                return;

            var terrainFeature = clone.GetComponent<TerrainFeature>();
            (terrainFeature as IEdenable)?.EdenSetup(withLogging: false);

            Debug.Log($"Chandlery Lionsmith: Created room '{def.Id}' at ({def.PosX}, {def.PosY}) from template '{templateId}'");
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
            specSeed.StartsOpen = def.StartsOpen;
            specSeed.StartsUnsealed = def.StartsUnsealed;
        }

        StripInteractiveChildren(clone);

        var rt = clone.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(def.Width, def.Height);
        rt.anchoredPosition = new Vector2(def.PosX, def.PosY);
        rt.pivot = new Vector2(0.5f, 0.5f);

        TryReplaceSprites(terrainFeature, templatePayload.Id, def);

        terrainFeature.SetUpAsTokenWithId(sphere);

        return clone;
    }

    private static void StripInteractiveChildren(GameObject root)
    {
        var toDestroy = new System.Collections.Generic.HashSet<GameObject>();

        foreach (var lazy in root.GetComponentsInChildren<ILazyEdenable>(true))
        {
            var mb = lazy as MonoBehaviour;
            if (mb != null && mb.gameObject != root)
                toDestroy.Add(mb.gameObject);
        }

        foreach (var dom in root.GetComponentsInChildren<AbstractDominion>(true))
        {
            var mb = dom as MonoBehaviour;
            if (mb != null && mb.gameObject != root)
                toDestroy.Add(mb.gameObject);
        }

        foreach (var go in toDestroy)
            GameObject.DestroyImmediate(go);

        if (toDestroy.Count > 0)
            Debug.Log($"Chandlery Lionsmith: Stripped {toDestroy.Count} interactive children from cloned room");
    }

    private static void TryReplaceSprites(TerrainFeature terrainFeature, string templateId, CustomTerrainDefinition def)
    {
        var templateSpriteName = "room_" + templateId;
        var templateShroudName = templateSpriteName + "_shrouded";
        var modManager = Watchman.Get<ModManager>();

        var manifestationGo = terrainFeature.GetComponentInChildren<IManifestation>() as MonoBehaviour;
        if (manifestationGo == null)
            return;

        foreach (var img in manifestationGo.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null)
                continue;

            var spriteName = img.sprite.name;

            if (spriteName == templateSpriteName && !string.IsNullOrEmpty(def.Sprite))
            {
                var newSprite = modManager.GetSprite("chandlery\\terrain\\" + def.Sprite);
                if (newSprite != null)
                    img.sprite = newSprite;
                else
                    Debug.LogWarning($"Chandlery Lionsmith: Sprite 'chandlery\\terrain\\{def.Sprite}' not found for room '{def.Id}'");
            }
            else if (spriteName == templateShroudName && !string.IsNullOrEmpty(def.ShroudSprite))
            {
                var newSprite = modManager.GetSprite("chandlery\\terrain\\" + def.ShroudSprite);
                if (newSprite != null)
                    img.sprite = newSprite;
                else
                    Debug.LogWarning($"Chandlery Lionsmith: Sprite 'chandlery\\terrain\\{def.ShroudSprite}' not found for room '{def.Id}'");
            }
        }
    }
}
