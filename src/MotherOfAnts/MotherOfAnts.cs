using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SecretHistories.Manifestations;
using SecretHistories.Spheres;
using SecretHistories.Tokens;
using SecretHistories.Tokens.Payloads;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.Rendering;
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

    public static void LogTerrainDetails()
    {
        var allTerrain = Resources.FindObjectsOfTypeAll<TerrainFeature>();
        Debug.Log($"Chandlery: ===== Found {allTerrain.Length} TerrainFeature objects =====");

        foreach (var terrain in allTerrain)
        {
            var go = terrain.gameObject;
            var active = go.activeInHierarchy ? "active" : "inactive";
            var rootRect = go.GetComponent<RectTransform>();
            var rootPos = rootRect != null ? rootRect.anchoredPosition : Vector2.zero;
            var rootSize = rootRect != null ? rootRect.sizeDelta : Vector2.zero;

            Debug.Log($"Chandlery: ========== Terrain '{terrain.Id}' ({active}) ==========");
            Debug.Log($"Chandlery:   Root: {go.name} pos=({rootPos.x:F1},{rootPos.y:F1}) size=({rootSize.x:F1}x{rootSize.y:F1}) layer={go.layer} tag={go.tag}");

            LogTerrainComponentDetails(terrain);

            var manifestation = go.GetComponentInChildren<RoomManifestation>();
            if (manifestation != null)
                LogManifestationDetails(manifestation);

            LogTerrainHierarchy(go.transform);
        }
    }

    public static void LogRoomSpheres(string roomId)
    {
        var allTerrain = Resources.FindObjectsOfTypeAll<TerrainFeature>();
        var room = allTerrain.FirstOrDefault(t => t.Id == roomId);

        if (room == null)
        {
            Debug.Log($"Chandlery: No TerrainFeature found with id '{roomId}'");
            return;
        }

        var go = room.gameObject;
        Debug.Log($"Chandlery: ===== Spheres in '{roomId}' ({go.name}) =====");

        var spheres = go.GetComponentsInChildren<Sphere>(includeInactive: true);
        Debug.Log($"Chandlery: Found {spheres.Length} Sphere(s)");

        foreach (var sphere in spheres)
        {
            var sphereGo = sphere.gameObject;
            var sphereType = sphere.GetType().Name;
            var active = sphereGo.activeInHierarchy ? "active" : "inactive";

            var spec = sphereGo.GetComponent<PermanentSphereSpec>();
            var specId = spec != null ? spec.ApplyId : "(no spec)";
            var label = spec != null ? spec.Title : "";

            Debug.Log($"Chandlery:   [{sphereType}] id='{specId}' label='{label}' ({active})");

            if (sphere is FitmentWorkstationSphere fws)
            {
                var verbId = typeof(FitmentWorkstationSphere).GetField("seedWithVerbId", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fws) as string;
                Debug.Log($"Chandlery:     VerbId={verbId}");
            }

            if (spec != null)
            {
                var required = spec.Required != null ? string.Join(", ", spec.Required.Select(a => $"{a.name}x{a.level}")) : "";
                var essential = spec.Essential != null ? string.Join(", ", spec.Essential.Select(a => $"{a.name}x{a.level}")) : "";
                var forbidden = spec.Forbidden != null ? string.Join(", ", spec.Forbidden.Select(a => $"{a.name}x{a.level}")) : "";
                Debug.Log($"Chandlery:     Required=[{required}] Essential=[{essential}] Forbidden=[{forbidden}]");
                Debug.Log($"Chandlery:     Category={sphere.SphereCategory} AllowDrag={sphere.AllowDrag}");
            }

            var lockDrag = typeof(PhysicalSphere).GetField("LockDrag", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(sphere);
            var glowHover = typeof(PhysicalSphere).GetField("ShowGlowOnHover", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(sphere);
            var glowInteract = typeof(PhysicalSphere).GetField("ShowInteractionGlow", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(sphere);
            if (lockDrag != null)
                Debug.Log($"Chandlery:     LockDrag={lockDrag} ShowGlowOnHover={glowHover} ShowInteractionGlow={glowInteract}");
        }
    }

    private static void LogTerrainComponentDetails(TerrainFeature terrain)
    {
        var go = terrain.gameObject;
        var comps = go.GetComponents<Component>();
        foreach (var comp in comps)
        {
            if (comp is TerrainFeature tf)
            {
                Debug.Log($"Chandlery:   [TerrainFeature] Id={tf.Id} IsShrouded={tf.IsShrouded} IsSealed={tf.IsSealed} IsOpen={tf.IsOpen} HasPreviouslyUnshrouded={tf.HasPreviouslyUnshrouded} Quantity={tf.Quantity}");
                Debug.Log($"Chandlery:     Label={tf.Label} Description={tf.Description} UniquenessGroup={tf.UniquenessGroup} Unique={tf.Unique} Icon={tf.Icon}");
                Debug.Log($"Chandlery:     EdensEnacted=[{string.Join(", ", tf.EdensEnacted)}]");
                Debug.Log($"Chandlery:     Dominions={tf.Dominions?.Count ?? 0}");
                continue;
            }
            if (comp is TerrainFeatureSpecSeed seed)
            {
                Debug.Log($"Chandlery:   [TerrainFeatureSpecSeed] StartsOpen={seed.StartsOpen} StartsUnsealed={seed.StartsUnsealed}");
                continue;
            }
            if (comp is RectTransform rt)
            {
                Debug.Log($"Chandlery:   [RectTransform] anchoredPosition=({rt.anchoredPosition.x:F1},{rt.anchoredPosition.y:F1}) sizeDelta=({rt.sizeDelta.x:F1}x{rt.sizeDelta.y:F1}) pivot=({rt.pivot.x:F2},{rt.pivot.y:F2}) anchorMin=({rt.anchorMin.x:F2},{rt.anchorMin.y:F2}) anchorMax=({rt.anchorMax.x:F2},{rt.anchorMax.y:F2})");
                continue;
            }
            Debug.Log($"Chandlery:   [{comp.GetType().Name}]");
        }
    }

    private static void LogManifestationDetails(RoomManifestation manifestation)
    {
        Debug.Log($"Chandlery:   [RoomManifestation] (child of TerrainFeature)");

        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        var unshrouded = typeof(RoomManifestation).GetField("_unshrouded", flags)?.GetValue(manifestation) as CanvasGroupFader;
        var shrouded = typeof(RoomManifestation).GetField("_shrouded", flags)?.GetValue(manifestation) as CanvasGroupFader;
        var sealed_ = typeof(RoomManifestation).GetField("_sealed", flags)?.GetValue(manifestation) as CanvasGroupFader;
        var glowImage = typeof(RoomManifestation).GetField("glowImage", flags)?.GetValue(manifestation) as GraphicFader;
        var dontRespond = typeof(RoomManifestation).GetField("dontRespondToClicksWhenClosed", flags)?.GetValue(manifestation);
        var dontZoom = typeof(RoomManifestation).GetField("dontZoomInOnDoubleClicks", flags)?.GetValue(manifestation);
        var shroudedSubs = typeof(RoomManifestation).GetField("shroudedSubFaders", flags)?.GetValue(manifestation) as CanvasGroupFader[];

        var unshroudedGo = unshrouded?.gameObject;
        var shroudedGo = shrouded?.gameObject;
        var sealedGo = sealed_?.gameObject;
        var glowGo = glowImage?.gameObject;

        Debug.Log($"Chandlery:     _unshrouded={(unshroudedGo != null ? unshroudedGo.name : "null")}");
        Debug.Log($"Chandlery:     _shrouded={(shroudedGo != null ? shroudedGo.name : "null")}");
        Debug.Log($"Chandlery:     _sealed={(sealedGo != null ? sealedGo.name : "null")}");
        Debug.Log($"Chandlery:     glowImage={(glowGo != null ? glowGo.name : "null")}");
        Debug.Log($"Chandlery:     dontRespondToClicksWhenClosed={dontRespond} dontZoomInOnDoubleClicks={dontZoom}");
        Debug.Log($"Chandlery:     shroudedSubFaders={shroudedSubs?.Length ?? 0}");

        if (unshrouded != null) LogCanvasGroupFaderDetails(unshrouded, "  ");
        if (shrouded != null) LogCanvasGroupFaderDetails(shrouded, "  ");
        if (sealed_ != null) LogCanvasGroupFaderDetails(sealed_, "  ");
        if (glowImage != null) LogGraphicFaderDetails(glowImage, "  ");
    }

    private static void LogCanvasGroupFaderDetails(CanvasGroupFader fader, string indent)
    {
        var go = fader.gameObject;
        var group = fader.GetComponent<CanvasGroup>();
        var groupAlpha = group != null ? group.alpha : -1f;

        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var maxAlpha = typeof(CanvasGroupFader).GetField("maxAlpha", flags)?.GetValue(fader);

        Debug.Log($"Chandlery: {indent}  [CanvasGroupFader] on '{go.name}' maxAlpha={maxAlpha} groupAlpha={groupAlpha:F2} durationTurnOn={fader.durationTurnOn} durationTurnOff={fader.durationTurnOff} blockRaysDuringFade={fader.blockRaysDuringFade}");
    }

    private static void LogGraphicFaderDetails(GraphicFader fader, string indent)
    {
        var go = fader.gameObject;
        var graphic = go.GetComponent<Graphic>();
        var graphicActive = graphic != null ? graphic.gameObject.activeSelf : false;

        Debug.Log($"Chandlery: {indent}  [GraphicFader] on '{go.name}' color=({fader.currentColor.r:F2},{fader.currentColor.g:F2},{fader.currentColor.b:F2},{fader.currentColor.a:F2}) durationTurnOn={fader.durationTurnOn} durationTurnOff={fader.durationTurnOff} keepEnabled={fader.KeepGraphicEnabledWhenHidden} ignoreTimeScale={fader.ignoreTimeScale} graphicActive={graphicActive} graphicType={(graphic != null ? graphic.GetType().Name : "null")}");
    }

    private static void LogTerrainHierarchy(Transform root)
    {
        var stack = new Stack<(Transform node, int depth)>();
        stack.Push((root, 0));
        var index = 0;

        while (stack.Count > 0)
        {
            var (node, depth) = stack.Pop();
            if (depth == 0)
            {
                for (var i = node.childCount - 1; i >= 0; i--)
                    stack.Push((node.GetChild(i), depth + 1));
                index++;
                continue;
            }

            var indent = new string(' ', depth * 2);
            var rect = node as RectTransform;
            var pos = rect != null ? (Vector3)rect.anchoredPosition : node.localPosition;
            var comps = node.GetComponents<Component>();
            var compNames = new string[comps.Length];
            for (var c = 0; c < comps.Length; c++)
                compNames[c] = comps[c].GetType().Name;

            var compInfo = new List<string>();
            var canvasGroup = node.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                compInfo.Add($"alpha={canvasGroup.alpha:F2} blocksRaycasts={canvasGroup.blocksRaycasts} interactable={canvasGroup.interactable}");

            var image = node.GetComponent<Image>();
            if (image != null)
            {
                var sprite = image.sprite;
                var spriteName = sprite != null ? sprite.name : "null";
                var spriteRect = sprite != null ? $"({sprite.rect.width:F0}x{sprite.rect.height:F0})" : "";
                var color = image.color;
                compInfo.Add($"Image[sprite={spriteName}{spriteRect} color=({color.r:F2},{color.g:F2},{color.b:F2},{color.a:F2}) alpha={image.canvasRenderer.GetAlpha():F2} raycast={image.raycastTarget}]");
            }

            var size = rect != null ? $"size=({rect.sizeDelta.x:F1}x{rect.sizeDelta.y:F1})" : "";
            var posStr = rect != null ? $"anchored=({rect.anchoredPosition.x:F1},{rect.anchoredPosition.y:F1})" : $"localPos=({pos.x:F1},{pos.y:F1})";

            var extra = compInfo.Count > 0 ? $"  [{string.Join("] [", compInfo)}]" : "";
            Debug.Log($"Chandlery: [{index}] {indent}{node.name}  [{string.Join(", ", compNames)}]  {posStr} {size}{extra}");

            for (var i = node.childCount - 1; i >= 0; i--)
                stack.Push((node.GetChild(i), depth + 1));

            index++;
        }
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

    public static void DumpAllLoadedAssets()
    {
        var outputDir = Path.Combine(Application.persistentDataPath, "ChandleryAssetDump");
        Directory.CreateDirectory(outputDir);

        DumpTextures(outputDir);
        DumpSpritesAsImages(outputDir);
        DumpVFabs(outputDir);

        Debug.Log($"Chandlery MotherOfAnts: Asset dump complete at {outputDir}");
    }

    private static void DumpTextures(string outputDir)
    {
        var texDir = Path.Combine(outputDir, "textures");
        Directory.CreateDirectory(texDir);

        var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
        var saved = 0;
        var skipped = 0;

        foreach (var tex in textures)
        {
            if (tex == null) continue;
            var name = SanitizeFileName(tex.name);
            if (string.IsNullOrEmpty(name)) name = "unnamed";

            try
            {
                byte[] bytes = null;

                if (tex.isReadable)
                    bytes = tex.EncodeToPNG();
                else
                    bytes = ReadTextureViaGPU(tex);

                if (bytes is { Length: > 0 })
                {
                    File.WriteAllBytes(Path.Combine(texDir, $"{name}.png"), bytes);
                    saved++;
                    continue;
                }
            }
            catch
            {
            }

            skipped++;
        }

        Debug.Log($"Chandlery MotherOfAnts: Textures: {saved} saved, {skipped} skipped (total {textures.Length})");
    }

    private static byte[] ReadTextureViaGPU(Texture2D tex)
    {
        var readable = CopyTextureReadable(tex);
        if (readable == null) return null;
        var bytes = readable.EncodeToPNG();
        Object.DestroyImmediate(readable);
        return bytes;
    }

    private static Texture2D CopyTextureReadable(Texture2D tex)
    {
        var w = tex.width;
        var h = tex.height;
        if (w <= 0 || h <= 0 || w > 4096 || h > 4096 || tex.dimension != TextureDimension.Tex2D)
            return null;

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
        var prevActive = RenderTexture.active;

        try
        {
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;

            var readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            readable.Apply();
            return readable;
        }
        finally
        {
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    private static void DumpSpritesAsImages(string outputDir)
    {
        var spriteDir = Path.Combine(outputDir, "sprites");
        Directory.CreateDirectory(spriteDir);

        var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        var textureToSprites = new Dictionary<Texture2D, List<Sprite>>();

        foreach (var sprite in allSprites)
        {
            if (sprite == null || sprite.texture == null) continue;
            if (!textureToSprites.TryGetValue(sprite.texture, out var list))
                textureToSprites[sprite.texture] = list = new List<Sprite>();
            list.Add(sprite);
        }

        using (var indexWriter = new StreamWriter(Path.Combine(spriteDir, "sprite_index.tsv")))
        {
            indexWriter.WriteLine("name\ttexture\tx\ty\twidth\theight\tpivotX\tpivotY\tsavedAs");

            var saved = 0;
            var skipped = 0;

            foreach (var kvp in textureToSprites)
            {
                var sourceTex = kvp.Key;
                var spriteList = kvp.Value;

                Texture2D readable = null;
                var createdCopy = false;

                try
                {
                    if (sourceTex.isReadable)
                    {
                        readable = sourceTex;
                    }
                    else
                    {
                        readable = CopyTextureReadable(sourceTex);
                        if (readable == null) continue;
                        createdCopy = true;
                    }

                    foreach (var sprite in spriteList)
                    {
                        try
                        {
                            var rect = sprite.rect;
                            var ix = (int)rect.x;
                            var iy = (int)rect.y;
                            var iw = (int)rect.width;
                            var ih = (int)rect.height;

                            if (iw <= 0 || ih <= 0) { skipped++; continue; }

                            var pixels = readable.GetPixels(ix, iy, iw, ih);

                            var outTex = new Texture2D(iw, ih, TextureFormat.RGBA32, false);
                            outTex.SetPixels(pixels);
                            outTex.Apply();

                            var outBytes = outTex.EncodeToPNG();
                            Object.DestroyImmediate(outTex);

                            if (outBytes is { Length: > 0 })
                            {
                                var spriteName = SanitizeFileName(sprite.name);
                                if (string.IsNullOrEmpty(spriteName)) spriteName = "unnamed";
                                var filePath = Path.Combine(spriteDir, $"{spriteName}.png");
                                File.WriteAllBytes(filePath, outBytes);

                                var pivot = sprite.pivot;
                                indexWriter.WriteLine($"{sprite.name}\t{sourceTex.name}\t{rect.x:F0}\t{rect.y:F0}\t{iw}\t{ih}\t{pivot.x:F4}\t{pivot.y:F4}\t{spriteName}.png");
                                saved++;
                            }
                        }
                        catch
                        {
                            skipped++;
                        }
                    }
                }
                finally
                {
                    if (createdCopy && readable != null)
                        Object.DestroyImmediate(readable);
                }
            }

            Debug.Log($"Chandlery MotherOfAnts: Sprites: {saved} saved, {skipped} skipped (total {allSprites.Length})");
        }
    }

    private static void DumpVFabs(string outputDir)
    {
        var vfabDir = Path.Combine(outputDir, "vfabs");
        Directory.CreateDirectory(vfabDir);

        var vfabs = Resources.FindObjectsOfTypeAll<VFab>();

        using (var writer = new StreamWriter(Path.Combine(vfabDir, "vfab_report.txt")))
        {
            writer.WriteLine($"Total VFabs: {vfabs.Length}");
            writer.WriteLine();

            foreach (var vfab in vfabs)
            {
                if (vfab == null) continue;
                var go = vfab.gameObject;

                writer.WriteLine($"--- {vfab.name} ---");
                writer.WriteLine($"  GameObject: {go.name}");
                writer.WriteLine($"  Active: {go.activeInHierarchy}");
                writer.WriteLine($"  Scene: {go.scene.name}");
                writer.WriteLine($"  Tag: {go.tag}");
                writer.WriteLine($"  Layer: {go.layer}");

                var ws = vfab.GetWorkstationActivity();
                writer.WriteLine($"  WorkstationActivity: {(ws != null ? ws.name : "null")}");

                var components = go.GetComponents<Component>();
                writer.WriteLine($"  Components ({components.Length}):");
                foreach (var comp in components)
                    writer.WriteLine($"    - {comp.GetType().Name}");

                writer.WriteLine();
            }
        }

        Debug.Log($"Chandlery MotherOfAnts: VFabs: {vfabs.Length} logged");
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "unnamed";
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        if (sanitized.Length > 100) sanitized = sanitized.Substring(0, 100);
        return sanitized.Trim();
    }
}