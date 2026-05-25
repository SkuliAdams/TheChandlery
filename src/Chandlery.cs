using HarmonyLib;
using TheHouse;
using TheHouse.Wheel;
using TheHouse.WolfDivided;
using UnityEngine;

// ReSharper disable once UnusedType.Global
// ReSharper disable once CheckNamespace
public static class Chandlery
{
    private static Harmony _harmony;

    public static void Initialise()
    {
        _harmony = new Harmony("com.chandlery.patch");
        Debug.Log("Chandlery: Initialising...");

        Wheel.Enact(_harmony);
        Flowermaker.Enact(_harmony);
        WolfDivided.Enact(_harmony);
        Lionsmith.Enact(_harmony);

        Debug.Log("Chandlery: Ready");
    }
}