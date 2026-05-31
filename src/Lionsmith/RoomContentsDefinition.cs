using System.Collections.Generic;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using TheHouse.Wheel;

namespace TheHouse;

public class RoomContentsDefinition : AbstractEntity<RoomContentsDefinition>
{
    public RoomContentsDefinition() { }
    public RoomContentsDefinition(EntityData importDataForEntity, ContentImportLog log)
        : base(importDataForEntity, log) { }

    protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium) { }

    [FucineList] public List<SlotDefinition> Slots { get; set; }
    [FucineList] public List<WorkstationDefinition> Workstations { get; set; }
    [FucineList] public List<ShelfDefinition> Shelves { get; set; }
    [FucineList] public List<ComfortDefinition> Comforts { get; set; }
    [FucineList] public List<WallArtDefinition> WallArts { get; set; }
    [FucineList] public List<string> remove_spheres { get; set; }

    public bool HasShelves => Shelves != null && Shelves.Count > 0;
}

internal interface ISphereOverrideTarget
{
    float? PosX { get; }
    float? PosY { get; }
    float? Width { get; }
    float? Height { get; }
    string Id { get; }
    string Label { get; }
    string Description { get; }
    bool? Greedy { get; }
    bool? LockDrag { get; }
    bool? ShowGlowOnHover { get; }
    bool? ShowInteractionGlow { get; }
    List<SeedEntry> Seeds { get; }
    Dictionary<string, int> Required { get; }
    Dictionary<string, int> Essential { get; }
    Dictionary<string, int> Forbidden { get; }
}

public abstract class SphereDefinitionBase<T> : AbstractEntity<T>, ISphereOverrideTarget where T : AbstractEntity<T>
{
    protected SphereDefinitionBase() { }
    protected SphereDefinitionBase(EntityData d, ContentImportLog l) : base(d, l) { }
    protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium) { }

    [WheelFucineNullable] public float? PosX { get; set; }
    [WheelFucineNullable] public float? PosY { get; set; }
    [WheelFucineNullable] public float? Width { get; set; }
    [WheelFucineNullable] public float? Height { get; set; }
    [FucineValue] public string Label { get; set; }
    [FucineValue] public string Description { get; set; }
    [FucineDict] public Dictionary<string, int> Required { get; set; }
    [FucineDict] public Dictionary<string, int> Essential { get; set; }
    [FucineDict] public Dictionary<string, int> Forbidden { get; set; }
    [WheelFucineNullable] public bool? Greedy { get; set; }
    [FucineList] public List<SeedEntry> Seeds { get; set; }
    [WheelFucineNullable] public bool? LockDrag { get; set; }
    [WheelFucineNullable] public bool? ShowGlowOnHover { get; set; }
    [WheelFucineNullable] public bool? ShowInteractionGlow { get; set; }
}

public class SlotDefinition : SphereDefinitionBase<SlotDefinition>
{
    public SlotDefinition() { }
    public SlotDefinition(EntityData d, ContentImportLog l) : base(d, l) { }
}

public class WorkstationDefinition : SphereDefinitionBase<WorkstationDefinition>
{
    public WorkstationDefinition() { }
    public WorkstationDefinition(EntityData d, ContentImportLog l) : base(d, l) { }

    [FucineValue] public string Verb { get; set; }
}

public class ShelfDefinition : SphereDefinitionBase<ShelfDefinition>
{
    public ShelfDefinition() { }
    public ShelfDefinition(EntityData d, ContentImportLog l) : base(d, l) { }
}

public class ComfortDefinition : SphereDefinitionBase<ComfortDefinition>
{
    public ComfortDefinition() { }
    public ComfortDefinition(EntityData d, ContentImportLog l) : base(d, l) { }
}

public class WallArtDefinition : SphereDefinitionBase<WallArtDefinition>
{
    public WallArtDefinition() { }
    public WallArtDefinition(EntityData d, ContentImportLog l) : base(d, l) { }
}

public class SeedEntry : AbstractEntity<SeedEntry>
{
    public SeedEntry() { }
    public SeedEntry(EntityData d, ContentImportLog l) : base(d, l) { }
    protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium) { }

    [FucineValue] public float PosX { get; set; }
    [FucineValue] public float PosY { get; set; }
    [FucineValue] public string Side { get; set; }
}
