using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OnDi.VerifyAccount
{
    [DisallowMultipleComponent]
    internal sealed class SdkRoot : MonoBehaviour
    {
        internal const string PanelResource = "OnDiVerify/VerifyPanel";
        internal const string BadgeResource = "OnDiVerify/FloatBadge";

        static readonly Vector2 PortraitReference = new Vector2(1080f, 1920f);
        static readonly Vector2 LandscapeReference = new Vector2(1920f, 1080f);

        static SdkRoot _instance;

        CanvasScaler _scaler;
        VerifyPanel _panel;
        FloatBadge _badge;
        int _lastWidth;
        int _lastHeight;

        internal event Action ScreenChanged;

        internal static SdkRoot Current => _instance != null ? _instance : null;

        internal static SdkRoot Ensure()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("[OnDiVerifySdk]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SdkRoot>();
            _instance.Build();
            return _instance;
        }

        void Build()
        {
            var settings = VerifyAccountSdk.Settings;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = settings.sortingOrder;
            if (!string.IsNullOrEmpty(settings.sortingLayerName) &&
                SortingLayer.NameToID(settings.sortingLayerName) != 0)
            {
                canvas.sortingLayerName = settings.sortingLayerName;
            }

            _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            gameObject.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();
            ApplyScreen();
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("[OnDiVerifyEventSystem]");
            DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#else
            Debug.LogWarning("[VerifyAccount] The scene has no EventSystem and the project uses the new " +
                             "Input System. Add an EventSystem with InputSystemUIInputModule.");
#endif
        }

        void Update()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;
            ApplyScreen();
        }

        void ApplyScreen()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            var landscape = Screen.width > Screen.height;
            _scaler.referenceResolution = landscape ? LandscapeReference : PortraitReference;
            _scaler.matchWidthOrHeight = landscape ? 1f : 0f;

            ScreenChanged?.Invoke();
        }

        internal bool IsPanelOpen => _panel != null && _panel.gameObject.activeSelf;

        internal void ShowPanel()
        {
            if (_panel == null)
            {
                _panel = Spawn<VerifyPanel>(PanelResource);
                if (_panel == null) return;
            }

            _panel.transform.SetAsFirstSibling();
            _panel.gameObject.SetActive(true);
            _panel.Open();
        }

        internal void HidePanel()
        {
            if (_panel == null) return;
            _panel.gameObject.SetActive(false);
            VerifyAccountSdk.RaiseClosed();
        }

        internal void RefreshPanelSkipButton()
        {
            if (_panel != null) _panel.ApplySkipButtonVisibility();
        }

        internal bool IsBadgeVisible => _badge != null && _badge.gameObject.activeSelf;

        internal void ShowBadge()
        {
            if (_badge == null)
            {
                _badge = Spawn<FloatBadge>(BadgeResource);
                if (_badge == null) return;
            }

            _badge.gameObject.SetActive(true);
            _badge.transform.SetAsLastSibling();
        }

        internal void HideBadge()
        {
            if (_badge != null) _badge.gameObject.SetActive(false);
        }

        internal void ResetBadgePosition()
        {
            if (_badge != null) _badge.ResetPosition();
        }

        internal void ShowBadgeTooltip()
        {
            if (_badge != null && _badge.gameObject.activeInHierarchy) _badge.ShowTooltip();
        }

        internal void ApplyUiFont()
        {
            if (_panel != null) ApplyUiFont(_panel.gameObject);
            if (_badge != null) ApplyUiFont(_badge.gameObject);
        }

        static void ApplyUiFont(GameObject target)
        {
            var font = VerifyAccountSdk.UiFont;
            if (font == null) return;

            var material = VerifyAccountSdk.UiFontMaterial;
            var texts = target.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                text.font = font;
                if (material != null) text.fontSharedMaterial = material;
            }
        }

        T Spawn<T>(string resourcePath) where T : Component
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError("[VerifyAccount] Could not load prefab Resources/" + resourcePath +
                               ". Run Tools > OnDi Verify > Rebuild UI Prefabs to recreate it.");
                return null;
            }

            var instance = Instantiate(prefab, transform, false);
            instance.name = prefab.name;
            ApplyUiFont(instance);

            var component = instance.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError("[VerifyAccount] Prefab " + resourcePath + " is missing component " + typeof(T).Name + ".");
                Destroy(instance);
            }
            return component;
        }
    }
}
