using System.Collections.Generic;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;

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

    public bool HasShelves => Shelves != null && Shelves.Count > 0;
}

public abstract class SphereDefinitionBase<T> : AbstractEntity<T> where T : AbstractEntity<T>
{
    protected SphereDefinitionBase() { }
    protected SphereDefinitionBase(EntityData d, ContentImportLog l) : base(d, l) { }
    protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium) { }

    [FucineValue] public float PosX { get; set; }
    [FucineValue] public float PosY { get; set; }
    [FucineValue(120f)] public float Width { get; set; }
    [FucineValue(120f)] public float Height { get; set; }
    [FucineValue] public string Label { get; set; }
    [FucineValue] public string Description { get; set; }
    [FucineDict] public Dictionary<string, int> Required { get; set; }
    [FucineDict] public Dictionary<string, int> Essential { get; set; }
    [FucineDict] public Dictionary<string, int> Forbidden { get; set; }
    [FucineValue(false)] public bool Greedy { get; set; }
    [FucineValue(false)] public bool LockDrag { get; set; }
    [FucineValue(false)] public bool ShowGlowOnHover { get; set; }
    [FucineValue(false)] public bool ShowInteractionGlow { get; set; }
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
