using System.IO;
using HarmonyLib;
using SecretHistories.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse
{
    // Replaces the main menu background and disables overlapping visual elements.
    internal static class Flowermaker
    {
        public static void Enact(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(BHMenuScreenController), "Start"),
                postfix: AccessTools.Method(typeof(Flowermaker), nameof(OnMenuScreenStart)));
        }

        private static void OnMenuScreenStart(BHMenuScreenController __instance)
        {
            ApplyCustomMenuBackground(__instance);
        }

        private static void ApplyCustomMenuBackground(BHMenuScreenController controller)
        {
            var bgPath = @"C:\Users\jonat\AppData\LocalLow\Weather Factory\Book of Hours\mods\Chandlery\images\mainmenu.png";

            if (!File.Exists(bgPath))
                return;

            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(File.ReadAllBytes(bgPath)))
                return;

            var bgHolder = GameObject.Find("CanvasBG/BGHolder");
            if (bgHolder != null)
            {
                var rawImage = bgHolder.GetComponent<RawImage>();
                if (rawImage != null)
                    rawImage.texture = texture;
            }

            var floatingGlyphs = GameObject.Find("CanvasBG/BGHolder/floatingGlyphs - left to right");
            if (floatingGlyphs != null) floatingGlyphs.SetActive(false);

            var fuchsia = GameObject.Find("CanvasMenu/Fuchsia");
            if (fuchsia != null) fuchsia.SetActive(false);
            
            var proem = GameObject.Find("CanvasMenu/ProemHolder");
            if (proem != null) proem.SetActive(false);
        }
    }
}