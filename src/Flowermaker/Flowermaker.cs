using HarmonyLib;
using SecretHistories.Infrastructure;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Services;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse
{
    // Replaces the main menu background and disables overlapping visual elements.
    internal static class Flowermaker
    {
        private static Texture _originalTexture;
    private static GameObject _floatingGlyphs;
    private static GameObject _fuchsia;
    private static GameObject _proem;
    private static GameObject _bugsButton;

        public static void Enact(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(BHMenuScreenController), "Start"),
                postfix: AccessTools.Method(typeof(Flowermaker), nameof(OnMenuScreenStart)));
        }

        private static void OnMenuScreenStart(BHMenuScreenController __instance)
        {
            ApplyCustomMenuBackground();

            (Watchman.Get<Concursum>().ContentUpdatedEvent)
                .AddListener(_ => ApplyCustomMenuBackground());
        }

        private static void ApplyCustomMenuBackground()
        {
            var bgHolder = GameObject.Find("CanvasBG/BGHolder");
            if (bgHolder == null)
                return;

            var rawImage = bgHolder.GetComponent<RawImage>();
            if (rawImage == null)
                return;

            if (_originalTexture == null)
                _originalTexture = rawImage.texture;

            CacheOverlayReferences();

            var sprite = Watchman.Get<ModManager>()?.GetSprite("chandlery\\mainmenu");
            if (sprite != null)
            {
                rawImage.texture = sprite.texture;
                SetOverlaysVisible(false);
            }
            else
            {
                rawImage.texture = _originalTexture;
                SetOverlaysVisible(true);
            }
        }

        private static void CacheOverlayReferences()
        {
            if (_floatingGlyphs == null)
                _floatingGlyphs = GameObject.Find("CanvasBG/BGHolder/floatingGlyphs - left to right");
            if (_fuchsia == null)
                _fuchsia = GameObject.Find("CanvasMenu/Fuchsia");
            if (_proem == null)
                _proem = GameObject.Find("CanvasMenu/ProemHolder");
            if (_bugsButton == null)
                _bugsButton = GameObject.Find("CanvasMenu/VersionAndDlcInfo/Button_Bugs");
        }

        private static void SetOverlaysVisible(bool visible)
        {
            if (_floatingGlyphs != null) _floatingGlyphs.SetActive(visible);
            if (_fuchsia != null) _fuchsia.SetActive(visible);
            if (_proem != null) _proem.SetActive(visible);
            if (_bugsButton != null) _bugsButton.SetActive(visible);
        }
    }
}