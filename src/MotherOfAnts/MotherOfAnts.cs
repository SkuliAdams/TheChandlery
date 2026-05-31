using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SecretHistories.Abstract;
using SecretHistories.Manifestations;
using SecretHistories.Spheres;
using SecretHistories.Spheres.Choreographers;
using SecretHistories.Tokens;
using SecretHistories.Tokens.Payloads;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TheHouse;

public static class MotherOfAnts
{
    public static void LogChoreographers(string roomId)
    {
        var allTerrain = Resources.FindObjectsOfTypeAll<TerrainFeature>();
        var room = allTerrain.FirstOrDefault(t => t.Id == roomId);
        if (room == null)
        {
            Debug.Log($"Chandlery MotherOfAnts: No room '{roomId}' found");
            return;
        }

        var spheres = room.gameObject.GetComponentsInChildren<Sphere>(true);
        Debug.Log($"Chandlery MotherOfAnts: === Choreographers in '{roomId}' ({spheres.Length} spheres) ===");

        foreach (var sphere in spheres)
        {
            try
            {
                var spec = sphere.GetComponent<PermanentSphereSpec>();
                var specId = spec != null ? spec.ApplyId : "(no spec)";
                var choreo = sphere.Choreographer;
                var choreoType = choreo != null ? choreo.GetType().Name : "null";
                var active = sphere.gameObject.activeInHierarchy;

                var catcher = sphere.gameObject.GetComponentInChildren<SphereDropCatcher>(true);
                string catcherInfo;
                if (catcher == null)
                    catcherInfo = "no catcher";
                else
                {
                    var disableWhenND = typeof(SphereDropCatcher).GetField("_disableWhenNotDragging", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(catcher);
                    var demandGhost = typeof(SphereDropCatcher).GetField("_demandGhost", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(catcher);
                    var catcherSphereId = catcher.Sphere != null ? catcher.Sphere.Id : "null";
                    catcherInfo = $"catcher->Sphere(id='{catcherSphereId}') _disND={disableWhenND} _demandGhost={demandGhost}";
                }

                Debug.Log($"  [{sphere.GetType().Name}] spec='{specId}' active={active} choreographer={choreoType} catcher={catcherInfo}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"  [ERROR] while logging sphere: {ex.Message}");
            }
        }
    }

    public static void LogChoreographersAll()
    {
        var allTerrain = Resources.FindObjectsOfTypeAll<TerrainFeature>();
        Debug.Log($"Chandlery MotherOfAnts: === Choreographers across all {allTerrain.Length} rooms ===");
        foreach (var room in allTerrain)
            LogChoreographers(room.Id);
    }

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

    public static void DescribeRoomFull(string roomId)
    {
        var allTerrain = Resources.FindObjectsOfTypeAll<TerrainFeature>();
        var room = allTerrain.FirstOrDefault(t => t.Id == roomId);
        if (room == null)
        {
            Debug.Log($"Chandlery: No room '{roomId}' found");
            return;
        }
        Debug.Log($"Chandlery MotherOfAnts: === Full hierarchy for room '{roomId}' ===");
        DescribeHierarchyFull(room.gameObject, 0);
    }

    public static void DescribeSphere(GameObject go)
    {
        Debug.Log($"Chandlery MotherOfAnts: === Full hierarchy for '{go.name}' ===");
        DescribeHierarchyFull(go, 0);
    }

    public static void LogSphereState(GameObject go)
    {
        var sphere = go.GetComponent<Sphere>();
        var rt = go.GetComponent<RectTransform>();
        var cg = go.GetComponentInParent<CanvasGroup>();
        var catcher = go.GetComponentInChildren<SphereDropCatcher>(true);
        var catcherImg = catcher?.GetComponent<Image>();
        string governingSpecStr;
        try { governingSpecStr = sphere?.GoverningSphereSpec != null ? "SET" : "null"; }
        catch { governingSpecStr = "EXCEPTION"; }

        Debug.Log($"Chandlery MotherOfAnts: Sphere '{go.name}' state:\n" +
          $"  RectTransform: size=({rt?.sizeDelta.x:F1},{rt?.sizeDelta.y:F1}) " +
          $"pos=({rt?.anchoredPosition.x:F1},{rt?.anchoredPosition.y:F1})\n" +
          $"  CanvasGroup.blocksRaycasts={cg?.blocksRaycasts}\n" +
          $"  Sphere.AllowDrag={sphere?.AllowDrag}\n" +
          $"  Sphere.GoverningSphereSpec={governingSpecStr}\n" +
          $"  Sphere.dropCatcher GO={(catcher != null ? catcher.gameObject.name : "MISSING")}\n" +
          $"  DropCatcher.Sphere={(catcher?.Sphere != null ? catcher.Sphere.gameObject.name : "null")}\n" +
          $"  DropCatcher Image raycastTarget={catcherImg?.raycastTarget}\n" +
          $"  DropCatcher Image enabled={catcherImg?.enabled}\n" +
          $"  DropCatcher go active={catcher?.gameObject.activeSelf}");
    }

    public static void DescribeSphereInRoom(string roomId, string sphereSpecId)
    {
        var allTerrain = Resources.FindObjectsOfTypeAll<TerrainFeature>();
        var room = allTerrain.FirstOrDefault(t => t.Id == roomId);
        if (room == null)
        {
            Debug.Log($"Chandlery: No room '{roomId}' found");
            return;
        }

        var spheres = room.gameObject.GetComponentsInChildren<Sphere>(true);
        foreach (var sphere in spheres)
        {
            var spec = sphere.GetComponent<PermanentSphereSpec>();
            var matchId = spec != null ? spec.ApplyId : sphere.gameObject.name;
            if (matchId == sphereSpecId)
            {
                DescribeHierarchyFull(sphere.gameObject, 0);
                return;
            }
        }

        Debug.Log($"Chandlery: No sphere with spec id '{sphereSpecId}' found in room '{roomId}'");
    }

    private static void DescribeHierarchyFull(GameObject go, int depth)
    {
        try
        {
            var indent = new string(' ', depth * 2);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var comps = go.GetComponents<Component>();
            var compNames = comps.Select(c => c.GetType().Name);
            Debug.Log($"{indent}{go.name}  [{string.Join(", ", compNames)}]  activeInHierarchy={go.activeInHierarchy} activeSelf={go.activeSelf} layer={go.layer}");

            DescribeComponent_RectTransform(go, indent);
            DescribeComponent_CanvasGroup(go, indent);
            DescribeComponent_Image(go, indent);
            DescribeComponent_Sphere(go, indent, flags);
            DescribeComponent_PhysicalSphere(go, indent, flags);
            DescribeComponent_FitmentWorkstationSphere(go, indent, flags);
            DescribeComponent_ShelfSpaceSphere(go, indent, flags);
            DescribeComponent_PermanentSphereSpec(go, indent, flags);
            DescribeComponent_SphereDropCatcher(go, indent, flags);
            DescribeComponent_GraphicFader(go, indent);
            DescribeComponent_Choreographer(go, indent, flags);
            DescribeComponent_PseudoAspectPreviewTooltip(go, indent);
            DescribeComponent_ShaderGlow(go, indent);
            DescribeComponent_ParticleSystem(go, indent);

            for (var i = 0; i < go.transform.childCount; i++)
                DescribeHierarchyFull(go.transform.GetChild(i).gameObject, depth + 1);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Chandlery DescribeHierarchyFull: Error at '{go.name}': {ex}");
        }
    }

    private static void DescribeComponent_RectTransform(GameObject go, string indent)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
            Debug.Log($"{indent}  RectTransform: anchoredPos=({rt.anchoredPosition.x:F1},{rt.anchoredPosition.y:F1}) size=({rt.sizeDelta.x:F1}x{rt.sizeDelta.y:F1}) pivot=({rt.pivot.x:F2},{rt.pivot.y:F2}) anchorMin=({rt.anchorMin.x:F2},{rt.anchorMin.y:F2}) anchorMax=({rt.anchorMax.x:F2},{rt.anchorMax.y:F2}) localPos=({rt.localPosition.x:F1},{rt.localPosition.y:F1},{rt.localPosition.z:F1})");
    }

    private static void DescribeComponent_CanvasGroup(GameObject go, string indent)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
            Debug.Log($"{indent}  CanvasGroup: alpha={cg.alpha:F2} blocksRaycasts={cg.blocksRaycasts} interactable={cg.interactable} ignoreParentGroups={cg.ignoreParentGroups}");
    }

    private static void DescribeComponent_Image(GameObject go, string indent)
    {
        var img = go.GetComponent<Image>();
        if (img != null)
        {
            var sprite = img.sprite;
            var spriteName = sprite != null ? $"{sprite.name}({sprite.rect.width:F0}x{sprite.rect.height:F0})" : "null";
            var canvasAlpha = img.canvasRenderer != null ? img.canvasRenderer.GetAlpha().ToString("F2") : "null";
            Debug.Log($"{indent}  Image: sprite={spriteName} color=({img.color.r:F2},{img.color.g:F2},{img.color.b:F2},{img.color.a:F2}) raycastTarget={img.raycastTarget} alpha={canvasAlpha} fillCenter={img.fillCenter} type={img.type} preserveAspect={img.preserveAspect}");
        }
    }

    private static void DescribeComponent_Sphere(GameObject go, string indent, BindingFlags flags)
    {
        var sphere = go.GetComponent<Sphere>();
        if (sphere == null) return;

        Debug.Log($"{indent}  Sphere: type={sphere.GetType().Name} id={sphere.Id} category={sphere.SphereCategory} allowDrag={sphere.AllowDrag} allowStackMerge={sphere.AllowStackMerge} isExterior={sphere.IsExteriorSphere} understateContents={sphere.UnderstateContents} enforceUniqueStacks={sphere.EnforceUniqueStacksInThisSphere} heartbeatMultiplier={sphere.TokenHeartbeatIntervalMultiplier:F2}");

        var choreo = sphere.Choreographer;
        Debug.Log($"{indent}  Sphere: choreographer={(choreo != null ? choreo.GetType().Name : "null")}");

        var tokenContainer = typeof(Sphere).GetField("tokenContainer", flags)?.GetValue(sphere);
        var tokenContainerName = tokenContainer is Transform tc ? tc.name : "null";
        Debug.Log($"{indent}  Sphere: tokenContainer={tokenContainerName}");

        var gs = sphere.GoverningSphereSpec;
        if (gs != null)
            Debug.Log($"{indent}  Sphere.GoverningSphereSpec: id={gs.Id} label={gs.Label} description={gs.Description} greedy={gs.Greedy}");

        var blockedInward = typeof(Sphere).GetField("_blockedInward", flags)?.GetValue(sphere);
        Debug.Log($"{indent}  Sphere: blockedInward={blockedInward}");
    }

    private static void DescribeComponent_PhysicalSphere(GameObject go, string indent, BindingFlags flags)
    {
        var ps = go.GetComponent<PhysicalSphere>();
        if (ps == null) return;

        Debug.Log($"{indent}  PhysicalSphere: slotGlow={(ps.slotGlow != null ? ps.slotGlow.name : "null")}");
        Debug.Log($"{indent}    LockDrag={typeof(PhysicalSphere).GetField("LockDrag", flags)?.GetValue(ps)} ShowGlowOnHover={typeof(PhysicalSphere).GetField("ShowGlowOnHover", flags)?.GetValue(ps)} ShowInteractionGlow={typeof(PhysicalSphere).GetField("ShowInteractionGlow", flags)?.GetValue(ps)} NeverShroud={typeof(PhysicalSphere).GetField("NeverShroud", flags)?.GetValue(ps)}");
    }

    private static void DescribeComponent_FitmentWorkstationSphere(GameObject go, string indent, BindingFlags flags)
    {
        var fws = go.GetComponent<FitmentWorkstationSphere>();
        if (fws == null) return;

        var verbId = typeof(FitmentWorkstationSphere).GetField("seedWithVerbId", flags)?.GetValue(fws) as string;
        var laterEden = typeof(FitmentWorkstationSphere).GetField("_laterEdenId", flags)?.GetValue(fws) as string;
        Debug.Log($"{indent}  FitmentWorkstationSphere: seedWithVerbId={verbId} _laterEdenId={laterEden}");
    }

    private static void DescribeComponent_ShelfSpaceSphere(GameObject go, string indent, BindingFlags flags)
    {
        var sss = go.GetComponent<ShelfSpaceSphere>();
        if (sss == null) return;

        Debug.Log($"{indent}  ShelfSpaceSphere: slotGlow={(sss.slotGlow != null ? sss.slotGlow.name : "null")}");
        var plaqueImg = typeof(ShelfSpaceSphere).GetField("plaqueImage", flags)?.GetValue(sss) as Image;
        var plaqueTooltip = typeof(ShelfSpaceSphere).GetField("plaqueImageTooltipComponent", flags)?.GetValue(sss) as PseudoAspectPreviewTooltip;
        Debug.Log($"{indent}    plaqueImage={(plaqueImg != null ? plaqueImg.name : "null")} plaqueImageTooltip={(plaqueTooltip != null ? "present" : "null")}");
    }

    private static void DescribeComponent_PermanentSphereSpec(GameObject go, string indent, BindingFlags flags)
    {
        var permSpec = go.GetComponent<PermanentSphereSpec>();
        if (permSpec == null) return;

        var inferredId = typeof(PermanentSphereSpec).GetMethod("GetId", flags)?.Invoke(permSpec, null) as string;
        Debug.Log($"{indent}  PermanentSphereSpec: ApplyId={permSpec.ApplyId} Title={permSpec.Title} Description={permSpec.Description} InferredId={inferredId} AvailableFromHouse={permSpec.AvailableFromHouse}");
        Debug.Log($"{indent}    Required=[{string.Join(", ", (permSpec.Required ?? System.Array.Empty<AspectSpec>()).Select(a => $"{a.name}x{a.level}"))}]");
        Debug.Log($"{indent}    Essential=[{string.Join(", ", (permSpec.Essential ?? System.Array.Empty<AspectSpec>()).Select(a => $"{a.name}x{a.level}"))}]");
        Debug.Log($"{indent}    Forbidden=[{string.Join(", ", (permSpec.Forbidden ?? System.Array.Empty<AspectSpec>()).Select(a => $"{a.name}x{a.level}"))}]");
        var cachedPayloads = typeof(PermanentSphereSpec).GetField("_cachedPermanentPayloads", flags)?.GetValue(permSpec) as AbstractPermanentPayload[];
        Debug.Log($"{indent}    _cachedPermanentPayloads={(cachedPayloads != null ? cachedPayloads.Length.ToString() : "null")}");
    }

    private static void DescribeComponent_SphereDropCatcher(GameObject go, string indent, BindingFlags flags)
    {
        var catcher = go.GetComponent<SphereDropCatcher>();
        if (catcher == null) catcher = go.GetComponentInChildren<SphereDropCatcher>();
        if (catcher == null) return;

        var catcherSphereField = typeof(SphereDropCatcher).GetField("Sphere", flags)?.GetValue(catcher) as Sphere;
        var catcherSphereId = catcherSphereField != null ? $"{catcherSphereField.GetType().Name} id={catcherSphereField.Id}" : "null";
        var disableWhenNotDragging = typeof(SphereDropCatcher).GetField("_disableWhenNotDragging", flags)?.GetValue(catcher);
        var demandGhost = typeof(SphereDropCatcher).GetField("_demandGhost", flags)?.GetValue(catcher);
        Debug.Log($"{indent}  SphereDropCatcher: Sphere=({catcherSphereId}) _disableWhenNotDragging={disableWhenNotDragging} _demandGhost={demandGhost}");
    }

    private static void DescribeComponent_GraphicFader(GameObject go, string indent)
    {
        var fader = go.GetComponent<GraphicFader>();
        if (fader == null) return;

        Debug.Log($"{indent}  GraphicFader: color=({fader.currentColor.r:F2},{fader.currentColor.g:F2},{fader.currentColor.b:F2},{fader.currentColor.a:F2}) durationTurnOn={fader.durationTurnOn} durationTurnOff={fader.durationTurnOff} keepEnabled={fader.KeepGraphicEnabledWhenHidden} ignoreTimeScale={fader.ignoreTimeScale}");
    }

    private static void DescribeComponent_Choreographer(GameObject go, string indent, BindingFlags flags)
    {
        var choreo = go.GetComponent<AbstractChoreographer>();
        if (choreo == null) return;

        Debug.Log($"{indent}  AbstractChoreographer: type={choreo.GetType().Name} enabled={choreo.enabled}");
        if (choreo is ShelfChoreographer sc)
        {
            var bookSpacing = typeof(ShelfChoreographer).GetField("bookSpacing", flags)?.GetValue(sc);
            var edgePadding = typeof(ShelfChoreographer).GetField("edgePadding", flags)?.GetValue(sc);
            Debug.Log($"{indent}    ShelfChoreographer: bookSpacing={bookSpacing} edgePadding={edgePadding}");
        }
    }

    private static void DescribeComponent_PseudoAspectPreviewTooltip(GameObject go, string indent)
    {
        if (go.GetComponent<PseudoAspectPreviewTooltip>() != null)
            Debug.Log($"{indent}  PseudoAspectPreviewTooltip: present");
    }

    private static void DescribeComponent_ShaderGlow(GameObject go, string indent)
    {
        var sg = go.GetComponent<ShaderGlow>();
        if (sg != null)
        {
            var minVal = typeof(ShaderGlow).GetField("minGlowValue", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(sg) ?? "?";
            var maxVal = typeof(ShaderGlow).GetField("maxGlowValue", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(sg) ?? "?";
            Debug.Log($"{indent}  ShaderGlow: minGlow={minVal} maxGlow={maxVal}");
        }
    }

    private static void DescribeComponent_ParticleSystem(GameObject go, string indent)
    {
        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            Debug.Log($"{indent}  ParticleSystem: startLifetime={main.startLifetime.constant} startSpeed={main.startSpeed.constant} maxParticles={main.maxParticles} startColor={main.startColor.color}");
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