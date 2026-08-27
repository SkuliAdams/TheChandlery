using Newtonsoft.Json;

namespace TheHouse.Colonel;

public class ColonelConfig
{
    [JsonProperty("clearVanilla")]
    public bool ClearVanilla { get; set; }

    public bool IsEmpty => !ClearVanilla;
}
