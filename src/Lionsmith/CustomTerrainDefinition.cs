using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;

namespace TheHouse;

[FucineImportable("terrain")]
public class CustomTerrainDefinition : AbstractEntity<CustomTerrainDefinition>
{
    public CustomTerrainDefinition() { }

    public CustomTerrainDefinition(EntityData importDataForEntity, ContentImportLog log)
        : base(importDataForEntity, log) { }

    protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium) { }

    [FucineValue] public float PosX { get; set; }
    [FucineValue] public float PosY { get; set; }
    [FucineValue(400f)] public float Width { get; set; }
    [FucineValue(200f)] public float Height { get; set; }
    [FucineValue] public string Sprite { get; set; }
    [FucineValue] public string ShroudSprite { get; set; }
    [FucineValue] public bool StartsOpen { get; set; }
    [FucineValue(true)] public bool StartsUnsealed { get; set; }
    [FucineValue] public string TemplateId { get; set; }
}
