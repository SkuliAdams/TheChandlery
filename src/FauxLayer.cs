namespace TheHouse;

// Named bands for the MostProminentPhysicalWorldObjects sorting layer.
// Custom objects are placed on this layer and sorted within it by order.
public enum FauxLayerBand
{
    Background = 0,
    Room = 1000,
    Foreground = 2000
}

internal static class FauxLayer
{
    public const string BaseLayer = "MostProminentPhysicalWorldObjects";

    public static int ResolveOrder(FauxLayerBand band, int? overrideOrder) =>
        overrideOrder ?? (int)band;

    public static bool TryParse(string value, out FauxLayerBand band)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "background":
                band = FauxLayerBand.Background;
                return true;
            case "room":
                band = FauxLayerBand.Room;
                return true;
            case "foreground":
                band = FauxLayerBand.Foreground;
                return true;
            default:
                band = FauxLayerBand.Background;
                return false;
        }
    }
}
