using System;
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
        EnactModule("Wheel", () => Wheel.Enact(_harmony));
        // Main menu manipulation nodule
        EnactModule("Flowermaker", () => Flowermaker.Enact(_harmony));
        // Terrain feature disabling module
        EnactModule("WolfDivided", () => WolfDivided.Enact(_harmony));
        // Terrain feature creation module
        EnactModule("Lionsmith", () => Lionsmith.Enact(_harmony));
        // In-game background manipulation module, in progress
        // EnactModule("Colonel", () => Colonel.Enact(_harmony));

        Debug.Log("Chandlery: Ready");
    }

    private static void EnactModule(string name, Action action)
    {
        try
        {
            action();
            Debug.Log($"Chandlery: {name} ready");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chandlery: {name} failed to initialise:\n{ex}");
        }
    }
}