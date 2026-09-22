using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OnDi.VerifyAccount
{
    [DisallowMultipleComponent]
    internal sealed class FloatBadge : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler
    {
        const string PrefOnRight = "OnDi.VerifyAccount.Badge.OnRight";
        const string PrefYRatio = "OnDi.VerifyAccount.Badge.YRatio";
        const float DefaultYRatio = 0.5f;
        const int FallbackMinAge = 18;

        [SerializeField] Image icon;
        [SerializeField] TMP_Text ageLabel;
        [SerializeField] RectTransform tooltip;
        [SerializeField] TMP_Text tooltipText;
        [SerializeField] TMP_Text tooltipMark;
        [SerializeField] RectTransform tooltipTail;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float edgeMargin = 12f;

        RectTransform _rect;
        RectTransform _canvasRect;
        Vector2 _dragOffset;
        bool _onRight;
        float _yRatio = DefaultYRatio;
        float _hideTooltipAt;
        bool _dragging;
        bool _draggedThisPress;

        void Awake()
        {
            _rect = (RectTransform)transform;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            var settings = VerifyAccountSdk.Settings;
            if (settings.badgeSprite != null && icon != null) icon.sprite = settings.badgeSprite;
            if (tooltipText != null) tooltipText.text = settings.badgeTooltipText;

            var age = AgeMark(settings.minAge);
            if (ageLabel != null) ageLabel.text = age;
            if (tooltipMark != null) tooltipMark.text = age;

            _onRight = PlayerPrefs.GetInt(PrefOnRight, 0) == 1;
            _yRatio = PlayerPrefs.GetFloat(PrefYRatio, DefaultYRatio);

            if (tooltip != null) tooltip.gameObject.SetActive(false);
            SnapAlpha();
        }

        void OnEnable()
        {
            _canvasRect = transform.parent as RectTransform;
            var root = SdkRoot.Current;
            if (root != null) root.ScreenChanged += ApplySavedPosition;
            ApplySavedPosition();
            SnapAlpha();
        }

        void OnDisable()
        {
            var root = SdkRoot.Current;
            if (root != null) root.ScreenChanged -= ApplySavedPosition;
            _dragging = false;
            _draggedThisPress = false;
            HideTooltip();
        }

        void Update()
        {
            FadeAlpha();

            if (tooltip == null || !tooltip.gameObject.activeSelf) return;

            if (_hideTooltipAt > 0f && Time.unscaledTime >= _hideTooltipAt)
            {
                HideTooltip();
                return;
            }

#if ENABLE_LEGACY_INPUT_MANAGER

            if (Input.GetMouseButtonDown(0) && !IsPointerOverSelf(Input.mousePosition))
                HideTooltip();
#endif
        }

        internal static string AgeMark(int minAge) =>
            (minAge > 0 ? minAge : FallbackMinAge) + "<sup>+</sup>";

        internal void ResetPosition()
        {
            _onRight = false;
            _yRatio = DefaultYRatio;
            PlayerPrefs.DeleteKey(PrefOnRight);
            PlayerPrefs.DeleteKey(PrefYRatio);
            ApplySavedPosition();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _draggedThisPress = true;
            HideTooltip();
            if (TryGetLocalPoint(eventData, out var local))
                _dragOffset = _rect.anchoredPosition - local;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!TryGetLocalPoint(eventData, out var local)) return;

            GetBounds(out var minX, out var maxX, out var minY, out var maxY);
            var target = local + _dragOffset;
            _rect.anchoredPosition = new Vector2(
                Mathf.Clamp(target.x, minX, maxX),
                Mathf.Clamp(target.y, minY, maxY));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            GetBounds(out var minX, out var maxX, out var minY, out var maxY);

            var pos = _rect.anchoredPosition;
            _onRight = Mathf.Abs(pos.x - maxX) <= Mathf.Abs(pos.x - minX);
            _yRatio = Mathf.Approximately(maxY, minY) ? 0.5f : Mathf.InverseLerp(minY, maxY, pos.y);

            PlayerPrefs.SetInt(PrefOnRight, _onRight ? 1 : 0);
            PlayerPrefs.SetFloat(PrefYRatio, _yRatio);
            PlayerPrefs.Save();

            ApplySavedPosition();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _draggedThisPress = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_draggedThisPress) return;

            if (tooltip == null) return;
            if (tooltip.gameObject.activeSelf) HideTooltip();
            else ShowTooltip();
        }

        void ApplySavedPosition()
        {
            _canvasRect = transform.parent as RectTransform;
            if (_canvasRect == null) return;

            GetBounds(out var minX, out var maxX, out var minY, out var maxY);
            _rect.anchoredPosition = new Vector2(
                _onRight ? maxX : minX,
                Mathf.Lerp(minY, maxY, _yRatio));

            if (tooltip != null && tooltip.gameObject.activeSelf) LayoutTooltip();
        }

        void GetBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            var canvasW = _canvasRect.rect.width;
            var canvasH = _canvasRect.rect.height;
            var scale = Screen.width > 0 ? canvasW / Screen.width : 1f;

            var safe = Screen.safeArea;
            var halfW = _rect.rect.width * 0.5f;
            var halfH = _rect.rect.height * 0.5f;

            minX = -canvasW * 0.5f + safe.xMin * scale + halfW + edgeMargin;
            maxX = -canvasW * 0.5f + safe.xMax * scale - halfW - edgeMargin;
            minY = -canvasH * 0.5f + safe.yMin * scale + halfH + edgeMargin;
            maxY = -canvasH * 0.5f + safe.yMax * scale - halfH - edgeMargin;

            if (minX > maxX) minX = maxX = 0f;
            if (minY > maxY) minY = maxY = 0f;
        }

        bool TryGetLocalPoint(PointerEventData eventData, out Vector2 local)
        {
            local = default;
            return _canvasRect != null &&
                   RectTransformUtility.ScreenPointToLocalPointInRectangle(
                       _canvasRect, eventData.position, eventData.pressEventCamera, out local);
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        bool IsPointerOverSelf(Vector2 screenPoint)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPoint, null) ||
                   (tooltip != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(tooltip, screenPoint, null));
        }
#endif

        internal void ShowTooltip()
        {
            if (tooltip == null) return;

            tooltip.gameObject.SetActive(true);
            LayoutTooltip();

            var seconds = VerifyAccountSdk.Settings.tooltipAutoHideSeconds;
            _hideTooltipAt = seconds > 0f ? Time.unscaledTime + seconds : 0f;
        }

        void HideTooltip()
        {
            if (tooltip != null) tooltip.gameObject.SetActive(false);
            _hideTooltipAt = 0f;
        }

        float TargetAlpha
        {
            get
            {
                if (_dragging) return 1f;
                if (tooltip != null && tooltip.gameObject.activeSelf) return 1f;
                return Mathf.Clamp01(VerifyAccountSdk.Settings.badgeIdleAlpha);
            }
        }

        void SnapAlpha()
        {
            if (canvasGroup != null) canvasGroup.alpha = TargetAlpha;
        }

        void FadeAlpha()
        {
            if (canvasGroup == null) return;

            var target = TargetAlpha;
            var seconds = VerifyAccountSdk.Settings.badgeFadeSeconds;
            if (seconds <= 0f)
            {
                canvasGroup.alpha = target;
                return;
            }

            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha, target, Time.unscaledDeltaTime / seconds);
        }

        void LayoutTooltip()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);

            var tailW = tooltipTail != null ? tooltipTail.rect.width : 0f;
            var offsetX = _rect.rect.width * 0.5f + tailW + tooltip.rect.width * 0.5f;

            if (tooltipTail != null)
            {
                var anchor = new Vector2(_onRight ? 1f : 0f, 0.5f);
                tooltipTail.anchorMin = anchor;
                tooltipTail.anchorMax = anchor;
                tooltipTail.pivot = new Vector2(0.5f, 0.5f);
                tooltipTail.anchoredPosition = new Vector2((_onRight ? 1f : -1f) * (tailW * 0.5f - 2f), 0f);
                tooltipTail.localScale = new Vector3(_onRight ? -1f : 1f, 1f, 1f);
            }

            var canvasH = _canvasRect.rect.height;
            var halfBubble = tooltip.rect.height * 0.5f;
            var badgeY = _rect.anchoredPosition.y;
            var wantedY = Mathf.Clamp(badgeY, -canvasH * 0.5f + halfBubble, canvasH * 0.5f - halfBubble);

            tooltip.anchoredPosition = new Vector2((_onRight ? -1f : 1f) * offsetX, wantedY - badgeY);
        }
    }
}
