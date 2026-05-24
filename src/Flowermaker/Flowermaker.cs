using HarmonyLib;
using SecretHistories.Infrastructure;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Services;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse
{
    // Replaces the main menu background and disables specific visuals.
    internal static class Flowermaker
    {
        private static Texture _originalTexture;
        private static GameObject _floatingGlyphs;
        private static GameObject _fuchsia;
        private static GameObject _proem;
        private static GameObject _bugsButton;
        private static GameObject _versionAndDlcInfo;
        private static GameObject _dataPrivacyButton;
        private static Vector2 _originalVersionAndDlcInfoPosition;

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

        // Find the required game objects for future reference
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
            if (_versionAndDlcInfo == null)
                _versionAndDlcInfo = GameObject.Find("CanvasMenu/VersionAndDlcInfo");
            if (_dataPrivacyButton == null)
                _dataPrivacyButton = GameObject.Find("CanvasMenu/VersionAndDlcInfo/DataPrivacyButton");
        }

        // Sets the main menu visibilities depending on presence of custom main menu
        private static void SetOverlaysVisible(bool visible)
        {
            // Set vanilla background and advert objects
            if (_floatingGlyphs != null) _floatingGlyphs.SetActive(visible);
            if (_fuchsia != null) _fuchsia.SetActive(visible);
            if (_proem != null) _proem.SetActive(visible);

            // Disable bug reporting button and move above buttons down a bit into its space
            // If there's a mod complex enough to demand its own main menu image, users shouldn't report bugs to WF
            if (visible)
            {
                if (_versionAndDlcInfo != null)
                    _versionAndDlcInfo.GetComponent<RectTransform>().anchoredPosition = _originalVersionAndDlcInfoPosition;
                if (_bugsButton != null)
                    _bugsButton.SetActive(true);
            }
            else
            {
                if (_versionAndDlcInfo != null && _bugsButton != null && _dataPrivacyButton != null)
                {
                    var viRect = _versionAndDlcInfo.GetComponent<RectTransform>();
                    var bugsRect = _bugsButton.GetComponent<RectTransform>();

                    _originalVersionAndDlcInfoPosition = viRect.anchoredPosition;
                    var offset = bugsRect.rect.height;
                    viRect.anchoredPosition = new Vector2(viRect.anchoredPosition.x, viRect.anchoredPosition.y - offset);

                    _bugsButton.SetActive(false);
                }
            }
        }
    }
}