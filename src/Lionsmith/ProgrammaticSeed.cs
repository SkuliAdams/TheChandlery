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

            var pos = ResolvePosition(def, rt, out var applyCentreOffset);
            var location = new TokenLocation(pos, sphere);

            var element = compendium.GetEntityById<Element>(elementId);
            var isFixed = element?.Aspects != null && element.Aspects.ContainsKey("fixed");

            var token = new TokenCreationCommand()
                .WithElementStack(elementId, 1)
                .WithLocation(location)
                .Execute(new Context(Context.ActionSource.Eden), sphere);
            token.Understate();

            // Tokens are positioned by their center, but an explicit posx/posy
            // is the seed's bottom-left corner. Shift based on rendered size
            if (applyCentreOffset)
            {
                var tokenRect = token.TokenRectTransform;
                if (tokenRect != null)
                {
                    var size = tokenRect.rect.size;
                    tokenRect.localPosition = new Vector3(
                        pos.x + size.x * 0.5f,
                        pos.y + size.y * 0.5f,
                        0f);
                }
            }

            if (isFixed)
                token.gameObject.AddComponent<NoDragMarker>();
        }

        return true;
    }

    private static Vector3 ResolvePosition(SeedEntry def, RectTransform rt, out bool applyCentreOffset)
    {
        var hasX = def.PosX.HasValue;
        var hasY = def.PosY.HasValue;

        var width = rt != null ? rt.rect.width : 60f;
        var height = rt != null ? rt.rect.height : 60f;
        var halfW = width * 0.5f;
        var halfH = height * 0.5f;

        // All coordinates are bottom-left origin, Y up: (0,0) is the sphere's
        // bottom-left corner, X increases right, Y increases up.
        float x, y;
        if (!string.IsNullOrEmpty(def.Side))
        {
            // "side" anchors a seed to the left/right edge of the sphere, centred
            // vertically unless an explicit (bottom-left origin) Y is supplied.
            x = def.Side == "left" ? 10f : width - 10f;
            y = hasY ? def.PosY.Value : height * 0.5f;
            applyCentreOffset = false;
        }
        else if (hasX && hasY)
        {
            x = def.PosX.Value;
            y = def.PosY.Value;
            applyCentreOffset = true;
        }
        else if (hasX)
        {
            // Only X given: Y defaults to the bottom edge.
            x = def.PosX.Value;
            y = 0f;
            applyCentreOffset = true;
        }
        else
        {
            // No position specified — center of the sphere.
            x = width * 0.5f;
            y = height * 0.5f;
            applyCentreOffset = false;
        }

        // Convert bottom-left-origin coords to local space (origin at the pivot, the sphere's centre).
        return new Vector3(x - halfW, y - halfH, 0f);
    }

    public void NotFreshSetup() { }

    public string GetLaterEdenId() => null;
}
