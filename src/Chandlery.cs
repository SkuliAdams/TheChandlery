using HarmonyLib;
using TheHouse;
using TheHouse.Colonel;
using TheHouse.Flowermaker;
using TheHouse.Wheel;
using TheHouse.WolfDivided;
using UnityEngine;

// Main entry point for the mod
public static class TheChandlery
{
    private static Harmony _harmony;

    public static void Initialise()
    {
        _harmony = new Harmony("com.chandlery.patch");
        Debug.Log("Chandlery: Initialising...");

        // General data loading module, including port of some Roost functionality
        Wheel.Enact(_harmony);
        // Main menu manipulation nodule
        Flowermaker.Enact(_harmony);
        // Terrain feature disabling module
        WolfDivided.Enact(_harmony);
        // Terrain feature creation module
        Lionsmith.Enact(_harmony);
        // In-game background manipulation module, in progress
        // Colonel.Enact(_harmony);

        Debug.Log("Chandlery: Ready");
    }
}