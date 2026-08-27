using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using TheHouse.Wheel;

namespace TheHouse.Colonel;

[FucineImportable("mapFeatures")]
public class MapFeatureDefinition : AbstractEntity<MapFeatureDefinition>
{
    public MapFeatureDefinition() { }

    public MapFeatureDefinition(string id)
    {
        SetId(id);
    }

    public MapFeatureDefinition(EntityData importDataForEntity, ContentImportLog log)
        : base(importDataForEntity, log) { }

    protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium) { }

    [FucineValue] public string Sprite { get; set; }

    [FucineValue("background")] public string Layer { get; set; }

    [WheelFucineNullable] public float? PosX { get; set; }

    [WheelFucineNullable] public float? PosY { get; set; }

    [WheelFucineNullable] public float? Width { get; set; }

    [WheelFucineNullable] public float? Height { get; set; }

    [WheelFucineNullable] public int? Order { get; set; }

    [WheelFucineNullable] public bool? ClickBlocking { get; set; }

    public FauxLayerBand ResolveBand()
    {
        FauxLayer.TryParse(Layer, out var band);
        return band;
    }
}
