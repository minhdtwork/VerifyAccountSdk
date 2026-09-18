using UnityEngine;
using UnityEngine.UI;

namespace OnDi.VerifyAccount
{
    /// <summary>
    /// Ôm chiều cao panel theo nội dung, nhưng không vượt quá một tỉ lệ chiều cao màn hình.
    /// Nhờ vậy màn dọc nhìn đúng như bản demo còn màn ngang thì panel bị giới hạn lại và
    /// phần nội dung dư được cuộn.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PanelAutoHeight : MonoBehaviour
    {
        [Tooltip("Node có ContentSizeFitter dọc, thường là Content của ScrollRect.")]
        [SerializeField] RectTransform content;

        [Tooltip("Phần chiều cao nằm ngoài vùng cuộn (padding trên/dưới của panel).")]
        [SerializeField] float extraHeight = 190f;

        [Range(0.3f, 1f)]
        [SerializeField] float maxScreenRatio = 0.92f;

        RectTransform _self;
        float _lastApplied = -1f;

        void OnEnable()
        {
            _self = (RectTransform)transform;
            _lastApplied = -1f;
            Apply();
        }

        void LateUpdate() => Apply();

        /// <summary>Tính lại chiều cao ngay lập tức. Bình thường không cần gọi tay.</summary>
        public void Apply()
        {
            // OnEnable của panel chạy trước khi các node con được kích hoạt, lúc đó layout
            // group còn tắt và preferredHeight là 0 — đợi lượt LateUpdate đầu tiên.
            if (content == null || !content.gameObject.activeInHierarchy) return;
            if (_self == null) _self = (RectTransform)transform;
            if (!(_self.parent is RectTransform parent)) return;

            // Lúc dựng prefab, parent chưa nằm dưới canvas nào nên rect còn rỗng —
            // đụng vào sẽ bake chiều cao 0 vào asset.
            if (parent.rect.height < 1f) return;

            // Ngay sau OnEnable, layout group chưa chạy nên preferredHeight còn là 0 và panel
            // sẽ co lại mất một frame. Ép tính một lần ở lượt đầu.
            if (_lastApplied < 0f) LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            var max = parent.rect.height * maxScreenRatio;
            var wanted = Mathf.Min(LayoutUtility.GetPreferredHeight(content) + extraHeight, max);

            if (Mathf.Abs(wanted - _lastApplied) < 0.5f) return;
            _lastApplied = wanted;
            _self.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, wanted);
        }
    }
}
