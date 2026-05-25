using System.Collections.Generic;
using SecretHistories.Tokens.Payloads;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse;

public static class MotherOfAnts
{
    public static void LogAllCanvases()
    {
        var allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (var canvas in allCanvases)
        {
            var active = canvas.gameObject.activeInHierarchy ? "active" : "inactive";
            Debug.Log($"Chandlery: ===== Canvas '{canvas.name}' ({active}) =====");
            LogHierarchyDFS(canvas.transform);
        }
    }

    public static void LogHierarchyDFS(Transform root)
    {
        LogHierarchyDFSInternal(root, includeTerrain: true);
    }

    public static void LogCanvasesSkipTerrain()
    {
        var allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (var canvas in allCanvases)
        {
            var active = canvas.gameObject.activeInHierarchy ? "active" : "inactive";
            Debug.Log($"Chandlery: ===== Canvas '{canvas.name}' ({active}) =====");
            LogHierarchyDFSSkipTerrain(canvas.transform);
        }
    }

    public static void LogHierarchyDFSSkipTerrain(Transform root)
    {
        LogHierarchyDFSInternal(root, includeTerrain: false);
    }

    private static void LogHierarchyDFSInternal(Transform root, bool includeTerrain)
    {
        var stack = new Stack<(Transform node, int depth)>();
        stack.Push((root, 0));
        var index = 0;

        while (stack.Count > 0)
        {
            var (node, depth) = stack.Pop();
            var indent = new string(' ', depth * 2);

            // When skipping terrain, omit children of TerrainFeature nodes
            if (!includeTerrain && depth > 0 && node.GetComponent<TerrainFeature>() != null)
            {
                Debug.Log($"Chandlery: [{index}] {indent}[SKIP] {node.name}  (TerrainFeature - children omitted)");
                index++;
                continue;
            }

            var comps = node.GetComponents<Component>();
            var compNames = new string[comps.Length];
            for (var c = 0; c < comps.Length; c++)
                compNames[c] = comps[c].GetType().Name;

            var pos = node.localPosition;
            var rect = node as RectTransform;
            var size = rect != null ? $" ({rect.sizeDelta.x:F1}x{rect.sizeDelta.y:F1})" : "";

            var image = node.GetComponent<Image>();
            var imageInfo = "";
            if (image != null)
            {
                var sprite = image.sprite;
                var spriteName = sprite != null ? sprite.name : "null";
                var color = image.color;
                imageInfo = $"  Image[sprite={spriteName} color=({color.r:F2},{color.g:F2},{color.b:F2},{color.a:F2}) alpha={image.canvasRenderer.GetAlpha():F2}]";
            }

            Debug.Log($"Chandlery: [{index}] {indent}{node.name}  [{string.Join(", ", compNames)}]  pos=({pos.x:F1}, {pos.y:F1}){size}{imageInfo}");

            for (var i = node.childCount - 1; i >= 0; i--)
                stack.Push((node.GetChild(i), depth + 1));

            index++;
        }
    }
}