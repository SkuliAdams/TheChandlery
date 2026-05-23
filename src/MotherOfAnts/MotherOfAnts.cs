using System.Collections.Generic;
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
        var stack = new Stack<(Transform node, int depth)>();
        stack.Push((root, 0));
        var index = 0;

        while (stack.Count > 0)
        {
            var (node, depth) = stack.Pop();
            var indent = new string(' ', depth * 2);
            var comps = node.GetComponents<Component>();
            var compNames = new string[comps.Length];
            for (var c = 0; c < comps.Length; c++)
                compNames[c] = comps[c].GetType().Name;
            Debug.Log($"Chandlery: [{index}] {indent}{node.name}  [{string.Join(", ", compNames)}]");

            for (var i = node.childCount - 1; i >= 0; i--)
                stack.Push((node.GetChild(i), depth + 1));

            index++;
        }
    }
}