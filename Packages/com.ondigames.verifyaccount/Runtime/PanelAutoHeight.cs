using UnityEngine;
using UnityEngine.UI;

namespace OnDi.VerifyAccount
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PanelAutoHeight : MonoBehaviour
    {
        [Tooltip("The node carrying a vertical ContentSizeFitter, usually the ScrollRect Content.")]
        [SerializeField] RectTransform content;

        [Tooltip("Height that sits outside the scroll view (the panel top and bottom padding).")]
        [SerializeField] float extraHeight = 190f;

        [Range(0.3f, 1f)]
        [SerializeField] float maxScreenRatio = 0.92f;

        const float ScrollSlack = 4f;

        RectTransform _self;
        float _lastApplied = -1f;

        void OnEnable()
        {
            _self = (RectTransform)transform;
            _lastApplied = -1f;
            Apply();
        }

        void LateUpdate() => Apply();

        public void Apply()
        {
            if (content == null || !content.gameObject.activeInHierarchy) return;
            if (_self == null) _self = (RectTransform)transform;
            if (!(_self.parent is RectTransform parent)) return;

            if (parent.rect.height < 1f) return;

            if (_lastApplied < 0f) LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            var max = parent.rect.height * maxScreenRatio;
            var wanted = Mathf.Min(
                LayoutUtility.GetPreferredHeight(content) + extraHeight + ScrollSlack, max);

            if (Mathf.Abs(wanted - _lastApplied) < 0.5f) return;
            _lastApplied = wanted;
            _self.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, wanted);
        }
    }
}
