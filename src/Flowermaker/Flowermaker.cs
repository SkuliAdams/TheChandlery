using HarmonyLib;
using SecretHistories.Infrastructure;
using SecretHistories.Infrastructure.Modding;
using SecretHistories.Services;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheHouse.Flowermaker;

// Replaces the main menu background with chandlery\mainmenu from any mod's
// chandlery/ folder. Hides decorative overlays (floating glyphs, Fuchsia,
// Proem) while a custom background is active. Suppresses the bug-report
// button since custom backgrounds imply a non-vanilla setup.
internal static class Flowermaker
{
    // Cached once to avoid repeated GameObject.Find scans on content reloads
    private static Texture _originalTexture;
    private static GameObject _floatingGlyphs;
    private static GameObject _fuchsia;
    private static GameObject _proem;
    private static GameObject _bugsButton;
    private static GameObject _versionAndDlcInfo;
    private static GameObject _dataPrivacyButton;
    private static Vector2 _originalVersionAndDlcInfoPosition;
    private static bool _positionSaved;

    public static void Enact(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(BHMenuScreenController), "Start"),
            postfix: AccessTools.Method(typeof(Flowermaker), nameof(OnMenuScreenStart)));
    }

    private static void OnMenuScreenStart(BHMenuScreenController __instance)
    {
        ApplyCustomMenuBackground();
        (Watchman.Get<Concursum>().ContentUpdatedEvent).AddListener(_ => ApplyCustomMenuBackground());
    }

    private static void ApplyCustomMenuBackground()
    {
        var bgHolder = GameObject.Find("CanvasBG/BGHolder");
        if (bgHolder == null)
        {
            Debug.LogWarning("Chandlery Flowermaker: CanvasBG/BGHolder not found — cannot apply menu background");
            return;
        }

        var rawImage = bgHolder.GetComponent<RawImage>();
        if (rawImage == null)
        {
            Debug.LogWarning("Chandlery Flowermaker: No RawImage on CanvasBG/BGHolder");
            return;
        }

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
        if (_versionAndDlcInfo == null)
        {
            _versionAndDlcInfo = GameObject.Find("CanvasMenu/VersionAndDlcInfo");
            if (_versionAndDlcInfo != null)
            {
                _originalVersionAndDlcInfoPosition = _versionAndDlcInfo.GetComponent<RectTransform>().anchoredPosition;
                _positionSaved = true;
            }
        }
        if (_dataPrivacyButton == null)
            _dataPrivacyButton = GameObject.Find("CanvasMenu/VersionAndDlcInfo/DataPrivacyButton");
    }

    // Toggle decorative overlays. When hiding, shift version info downward to fill
    // the gap left by the removed bug-report button, and save the original position
    // for restoration.
    private static void SetOverlaysVisible(bool visible)
    {
        if (_floatingGlyphs != null) _floatingGlyphs.SetActive(visible);
        if (_fuchsia != null) _fuchsia.SetActive(visible);
        if (_proem != null) _proem.SetActive(visible);

        if (visible)
        {
            if (_versionAndDlcInfo != null && _positionSaved)
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

                viRect.anchoredPosition = new Vector2(
                    viRect.anchoredPosition.x,
                    viRect.anchoredPosition.y - bugsRect.rect.height);

                _bugsButton.SetActive(false);
            }
        }
    }
}