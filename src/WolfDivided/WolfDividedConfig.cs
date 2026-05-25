using System.Collections.Generic;
using Newtonsoft.Json;

namespace TheHouse.WolfDivided;

public class WolfDividedConfig
{
    [JsonProperty("cleanSlate")]
    public bool CleanSlate { get; set; }

    [JsonProperty("preserve")]
    public List<string> Preserve { get; set; } = new();

    [JsonProperty("disableTerrain")]
    public List<string> DisableTerrain { get; set; } = new();

    public bool IsEmpty =>
        !CleanSlate && Preserve.Count == 0 && DisableTerrain.Count == 0;
}
