using System.Collections.Generic;
using SecretHistories.Abstract;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Spheres;
using SecretHistories.Spheres.Choreographers;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse;

internal class ProgrammaticSeed : MonoBehaviour, ILazyEdenable
{
    private enum SeedAnchor
    {
        BottomLeft,
        Center,
        BottomRight,
        TopLeft,
        TopRight,
    }

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

            var anchor = ResolveAnchor(def);
            var pos = ResolvePosition(def, rt);
            var location = new TokenLocation(pos, sphere);

            var element = compendium.GetEntityById<Element>(elementId);
            var isFixed = element?.Aspects != null && element.Aspects.ContainsKey("fixed");

            var token = new TokenCreationCommand()
                .WithElementStack(elementId, 1)
                .WithLocation(location)
                .Execute(new Context(Context.ActionSource.Eden), sphere);
            token.Understate();

            // Tokens are positioned by their center, but posx/posy is the seed's
            // anchor point. Shift based on rendered size so the anchor lands there.
            var tokenRect = token.TokenRectTransform;
            var tokenSize = tokenRect != null ? tokenRect.rect.size : Vector2.zero;
            var offset = AnchorOffset(anchor, tokenSize);

            var isWallArt = sphere.GetComponent<AbstractChoreographer>() is WallChoreographer;
            if (tokenRect != null)
            {
                // Let the sphere's choreographer place the token (clamping it to
                // the sphere and resolving overlaps), then nudge the rendered
                // token so the anchor point lands on the intended posx/posy.
                var placed = tokenRect.localPosition;
                if (isWallArt)
                {
                    // Wall-art spheres: the anchor applies on both axes.
                    tokenRect.localPosition = new Vector3(placed.x + offset.x, placed.y + offset.y, placed.z);
                }
                else if (!string.IsNullOrEmpty(def.Anchor) && string.IsNullOrEmpty(def.Side))
                {
                    // Non-wall spheres: Only apply on x axis, since item forced to bottom edge.
                    tokenRect.localPosition = new Vector3(placed.x + offset.x, placed.y, placed.z);
                }
            }

            if (isFixed)
                token.gameObject.AddComponent<NoDragMarker>();
        }

        return true;
    }

    private static SeedAnchor ResolveAnchor(SeedEntry def)
    {
        if (!string.IsNullOrEmpty(def.Anchor))
        {
            switch (def.Anchor.Trim().ToLowerInvariant())
            {
                case "center":
                case "centre": // appease the brits
                    return SeedAnchor.Center;
                case "bottomright":
                    return SeedAnchor.BottomRight;
                case "topleft":
                    return SeedAnchor.TopLeft;
                case "topright":
                    return SeedAnchor.TopRight;
                case "bottomleft":
                default:
                    return SeedAnchor.BottomLeft;
            }
        }

        if (!string.IsNullOrEmpty(def.Side) || (!def.PosX.HasValue && !def.PosY.HasValue))
            return SeedAnchor.Center;
        return SeedAnchor.BottomLeft;
    }

    private static Vector3 AnchorOffset(SeedAnchor anchor, Vector2 size)
    {
        var halfW = size.x * 0.5f;
        var halfH = size.y * 0.5f;

        return anchor switch
        {
            SeedAnchor.BottomLeft => new Vector3(halfW, halfH, 0f),
            SeedAnchor.BottomRight => new Vector3(-halfW, halfH, 0f),
            SeedAnchor.TopLeft => new Vector3(halfW, -halfH, 0f),
            SeedAnchor.TopRight => new Vector3(-halfW, -halfH, 0f),
            _ => Vector3.zero,
        };
    }

    private static Vector3 ResolvePosition(SeedEntry def, RectTransform rt)
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
        }
        else if (hasX && hasY)
        {
            x = def.PosX.Value;
            y = def.PosY.Value;
        }
        else if (hasX)
        {
            // Only X given: Y defaults to the bottom edge.
            x = def.PosX.Value;
            y = 0f;
        }
        else
        {
            // No position specified — center of the sphere.
            x = width * 0.5f;
            y = height * 0.5f;
        }

        // Convert bottom-left-origin coords to local space (origin at the pivot, the sphere's centre).
        return new Vector3(x - halfW, y - halfH, 0f);
    }

    public void NotFreshSetup() { }

    public string GetLaterEdenId() => null;
}
