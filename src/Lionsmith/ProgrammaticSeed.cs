using System.Collections.Generic;
using SecretHistories.Abstract;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Spheres;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse;

internal class ProgrammaticSeed : MonoBehaviour, ILazyEdenable
{
    public List<SeedEntry> SeedDefs;

    public bool EdenSetup(bool withLogging)
    {
        if (SeedDefs == null || SeedDefs.Count == 0)
            return false;

        var sphere = GetComponentInParent<Sphere>();
        if (sphere == null)
            return false;

        var compendium = Watchman.Get<Compendium>();
        var rt = sphere.GetComponent<RectTransform>();

        foreach (var def in SeedDefs)
        {
            if (string.IsNullOrEmpty(def?.Id))
                continue;

            var elementId = def.Id.Trim();

            if (!compendium.EntityExists<Element>(elementId))
            {
                Debug.LogWarning($"Chandlery ProgrammaticSeed: element '{elementId}' not found");
                continue;
            }

            var pos = ResolvePosition(def, rt);
            var location = new TokenLocation(pos, sphere);

            var element = compendium.GetEntityById<Element>(elementId);
            var isFixed = element?.Aspects != null && element.Aspects.ContainsKey("fixed");

            var token = new TokenCreationCommand()
                .WithElementStack(elementId, 1)
                .WithLocation(location)
                .Execute(new Context(Context.ActionSource.Eden), sphere);
            token.Understate();
            if (isFixed)
                token.gameObject.AddComponent<NoDragMarker>();
        }

        return true;
    }

    private static Vector3 ResolvePosition(SeedEntry def, RectTransform rt)
    {
        var hasX = !float.IsNaN(def.PosX);
        var hasY = !float.IsNaN(def.PosY);

        if (!string.IsNullOrEmpty(def.Side))
        {
            var halfW = rt != null ? rt.rect.width * 0.5f : 30f;
            var x = def.Side == "left" ? -halfW + 10f : halfW - 10f;
            var y = hasY ? def.PosY : 0f;
            return new Vector3(x, y, 0f);
        }

        if (hasX && hasY)
            return new Vector3(def.PosX, def.PosY, 0f);

        if (hasX)
        {
            var y = rt != null ? -rt.rect.height * 0.5f + 10f : 0f;
            return new Vector3(def.PosX, y, 0f);
        }

        return Vector3.zero;
    }

    public void NotFreshSetup() { }

    public string GetLaterEdenId() => null;
}
