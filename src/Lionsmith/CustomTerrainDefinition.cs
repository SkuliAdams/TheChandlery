using System.Collections.Generic;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using UnityEngine;

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

    [FucineValue] public string RoomSize { get; set; }

    [FucineSubEntity]
    public RoomContentsDefinition Contents { get; set; }

    [FucineList] public List<string> ConnectedTo { get; set; }

    private const float BlockWidth = 400f;
    private const float BlockHeight = 200f;
    private const float BlockGap = 20f;

    public void ResolveSize(out float w, out float h)
    {
        if (!string.IsNullOrEmpty(RoomSize))
        {
            var parts = RoomSize.Split('x');
            if (parts.Length == 2
                && int.TryParse(parts[0].Trim(), out var rows)
                && int.TryParse(parts[1].Trim(), out var cols)
                && rows > 0 && cols > 0)
            {
                w = BlockWidth * cols + BlockGap * (cols - 1);
                h = BlockHeight * rows + BlockGap * (rows - 1);
                return;
            }
            Debug.LogWarning($"Chandlery Lionsmith: Invalid RoomSize '{RoomSize}' for room '{Id}', falling back to Width/Height");
        }
        w = Width;
        h = Height;
    }
}
