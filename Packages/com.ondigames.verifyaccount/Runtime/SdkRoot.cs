using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OnDi.VerifyAccount
{
    /// <summary>
    /// Canvas dùng chung của SDK. Được tạo lazy ở lần gọi API đầu tiên — không có
    /// bootstrap tự động nào.
    /// </summary>
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

        /// <summary>Phát khi màn hình đổi kích thước hoặc đổi chiều.</summary>
        internal event Action ScreenChanged;

        /// <summary>Instance hiện có, hoặc null. Dùng Unity-null nên an toàn với <c>?.</c>.</summary>
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

        /// <summary>Game nào cũng nên có sẵn EventSystem; nếu không thì SDK tự dựng một cái.</summary>
        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("[OnDiVerifyEventSystem]");
            DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#else
            Debug.LogWarning("[VerifyAccount] Scene không có EventSystem và project đang dùng " +
                             "Input System mới. Hãy thêm EventSystem với InputSystemUIInputModule.");
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

            // Màn ngang khớp theo chiều cao, màn dọc khớp theo chiều rộng, để tỉ lệ pixel
            // của UI giữ nguyên ở cả hai chiều thay vì co lại còn một nửa.
            var landscape = Screen.width > Screen.height;
            _scaler.referenceResolution = landscape ? LandscapeReference : PortraitReference;
            _scaler.matchWidthOrHeight = landscape ? 1f : 0f;

            ScreenChanged?.Invoke();
        }

        // ---- Panel ----

        internal bool IsPanelOpen => _panel != null && _panel.gameObject.activeSelf;

        internal void ShowPanel()
        {
            if (_panel == null)
            {
                _panel = Spawn<VerifyPanel>(PanelResource);
                if (_panel == null) return;
            }

            _panel.transform.SetAsFirstSibling(); // badge luôn nằm trên panel
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

        // ---- Badge ----

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

        T Spawn<T>(string resourcePath) where T : Component
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError("[VerifyAccount] Không nạp được prefab Resources/" + resourcePath +
                               ". Chạy menu Tools > OnDi Verify > Rebuild UI Prefabs để dựng lại.");
                return null;
            }

            var instance = Instantiate(prefab, transform, false);
            instance.name = prefab.name;

            var component = instance.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError("[VerifyAccount] Prefab " + resourcePath + " thiếu component " + typeof(T).Name + ".");
                Destroy(instance);
            }
            return component;
        }
    }
}
