using System.Collections.Generic;
using System.Linq;
using SecretHistories.Entities;
using SecretHistories.Spheres;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse.Colonel;

// Instantiates world-space Canvas+Image pairs for map features, parented to
// the Library sphere so coordinates match room placement.
internal static class MapFeatureRenderer
{
    private const string ContainerName = "ChandleryColonelFeatures";

    private static readonly Dictionary<string, Sprite> PlaceholderCache = new();

    internal static void Rebuild(IReadOnlyList<MapFeatureDefinition> features)
    {
        var librarySphere = FindLibrarySphere();
        if (librarySphere == null)
            return;

        DestroyExistingContainer(librarySphere);

        var container = new GameObject(ContainerName, typeof(RectTransform));
        container.transform.SetParent(librarySphere.transform, false);

        foreach (var feature in features)
            Render(feature, container.transform);
    }

    private static void Render(MapFeatureDefinition feature, Transform parent)
    {
        var posX = feature.PosX ?? 0f;
        var posY = feature.PosY ?? 0f;

        var band = feature.ResolveBand();
        var order = FauxLayer.ResolveOrder(band, feature.Order);

        var sprite = ResolveSprite(feature);

        var scale = feature.Scale.HasValue && feature.Scale.Value > 0f ? feature.Scale.Value : 4.2f;
        var w = feature.Width ?? (sprite != null ? sprite.rect.width / scale : 400f);
        var h = feature.Height ?? (sprite != null ? sprite.rect.height / scale : 200f);

        if (sprite == null)
        {
            sprite = CreatePlaceholder(feature.Id, Mathf.RoundToInt(w * scale), Mathf.RoundToInt(h * scale));
            Debug.LogWarning($"Chandlery Colonel: No sprite found for map feature '{feature.Id}' — using placeholder");
        }

        var root = new GameObject(feature.Id, typeof(RectTransform));
        root.transform.SetParent(parent, false);

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(posX + w * 0.5f, posY + h * 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerID = SortingLayer.NameToID(FauxLayer.BaseLayer);
        canvas.sortingOrder = order;

        var imageGo = new GameObject("Image", typeof(RectTransform));
        imageGo.transform.SetParent(root.transform, false);

        var imageRt = imageGo.GetComponent<RectTransform>();
        imageRt.anchorMin = Vector2.zero;
        imageRt.anchorMax = Vector2.one;
        imageRt.offsetMin = Vector2.zero;
        imageRt.offsetMax = Vector2.zero;

        var image = imageGo.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = feature.ClickBlocking ?? true;

        if (image.raycastTarget)
            root.AddComponent<GraphicRaycaster>();

        Debug.Log($"Chandlery Colonel: Placed map feature '{feature.Id}' at ({posX}, {posY}) on {band} (order {order})");
    }

    private static Sprite ResolveSprite(MapFeatureDefinition feature)
    {
        var key = feature.Sprite ?? feature.Id;
        return SpriteLookup.Find(key);
    }

    private static Sphere FindLibrarySphere()
    {
        var ha = Watchman.Get<HornedAxe>();
        var sphere = ha.GetSpheres().FirstOrDefault(s => s.Id == "Library");
        if (sphere == null)
            Debug.LogError("Chandlery Colonel: No sphere named 'Library' found");

        return sphere;
    }

    private static void DestroyExistingContainer(Sphere librarySphere)
    {
        var existingContainer = librarySphere.transform.Find(ContainerName);
        if (existingContainer != null)
            Object.Destroy(existingContainer.gameObject);
    }

    private static Sprite CreatePlaceholder(string key, int width, int height)
    {
        if (PlaceholderCache.TryGetValue(key, out var cached))
            return cached;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        var magenta = new Color32(255, 0, 255, byte.MaxValue);
        var black = new Color32(0, 0, 0, byte.MaxValue);
        var pixels = new Color32[width * height];
        var block = Mathf.Max(8, width / 16);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var cx = x / block;
            var cy = y / block;
            pixels[y * width + x] = (cx + cy) % 2 == 0 ? magenta : black;
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        PlaceholderCache[key] = sprite;

        return sprite;
    }
}
