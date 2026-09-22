using TMPro;
using UnityEngine;

namespace OnDi.VerifyAccount
{
    /// <summary>
    /// Cấu hình của SDK. Tạo bằng Create > OnDi > Verify Account Settings rồi đặt vào
    /// một thư mục <c>Resources</c> bất kỳ của game, tên file giữ nguyên
    /// <c>VerifyAccountSettings</c>. Thiếu file thì SDK chạy bằng giá trị mặc định.
    /// </summary>
    [CreateAssetMenu(fileName = ResourceName, menuName = "OnDi/Verify Account Settings")]
    public sealed class VerifyAccountSettings : ScriptableObject
    {
        public const string ResourceName = "VerifyAccountSettings";

        /// <summary>
        /// Bản mặc định đóng gói sẵn trong SDK, dùng khi game chưa tạo file riêng — cài xong là
        /// chạy được ngay. Cố tình đặt tên khác <see cref="ResourceName"/>: hai asset trùng tên
        /// trong hai thư mục <c>Resources</c> thì <c>Resources.Load</c> trả về cái nào là không
        /// xác định, và bản của package sẽ có lúc đè mất cấu hình của game.
        /// </summary>
        public const string DefaultResourcePath = "OnDiVerify/DefaultSettings";

        [Header("Policy Links")]
        [Tooltip("Mở khi người chơi bấm vào dòng \"Điều khoản sử dụng\".")]
        public string termsUrl = "";

        [Tooltip("Mở khi người chơi bấm vào dòng \"Chính sách bảo vệ và xử lý dữ liệu cá nhân\".")]
        public string privacyUrl = "";

        [Header("Font")]
        [Tooltip("Font TextMeshPro dùng cho toàn bộ chữ của SDK — đây là chỗ mỗi project cắm " +
                 "font riêng của mình vào. Để trống thì dùng font mặc định của TextMeshPro " +
                 "(LiberationSans SDF, có đủ dấu tiếng Việt).")]
        public TMP_FontAsset uiFont;

        [Tooltip("Material preset đi kèm font (viền, đổ bóng...). Để trống thì dùng material " +
                 "gốc của font.")]
        public Material uiFontMaterial;

        [Header("Canvas")]
        [Tooltip("Sorting layer của canvas SDK. Để trống hoặc tên không tồn tại thì dùng Default.")]
        public string sortingLayerName = "Default";

        [Tooltip("Sorting order. Để cao để luôn nằm trên UI của game.")]
        public int sortingOrder = 32000;

        [Header("Validation")]
        [Tooltip("Biểu thức chính quy cho số điện thoại. Để trống là chấp nhận mọi chuỗi khác rỗng.")]
        public string phoneRegex = @"^(0|\+84)(3|5|7|8|9)\d{8}$";

        [Min(1)] public int otpLength = 6;

        [Tooltip("Tuổi tối thiểu tính theo ngày sinh. 0 là không kiểm tra.")]
        [Min(0)] public int minAge = 0;

        [Header("OTP Timing")]
        [Tooltip("Đồng hồ đếm ngược \"OTP hết hạn sau\", tính bằng giây.")]
        [Min(1)] public int otpTtlSeconds = 180;

        [Tooltip("Khoá nút \"Gửi lại\" bao nhiêu giây sau mỗi lần gửi.")]
        [Min(0)] public int resendCooldownSeconds = 60;

        [Header("Panel")]
        [Tooltip("Hiện nút \"Bỏ qua\". Đổi lúc chạy bằng VerifyAccountSdk.SetSkipButtonVisible().")]
        public bool showSkipButton = true;

        [Header("Floating Badge")]
        [Tooltip("Để trống thì dùng icon mặc định trong prefab.")]
        public Sprite badgeSprite;

        /// <summary>Xuống dòng cố định để bong bóng ngắt câu đúng chỗ như bản thiết kế.</summary>
        public const string DefaultBadgeTooltipText =
            "Chơi quá\n180 phút một ngày\nsẽ ảnh hưởng xấu\nđến sức khỏe";

        [TextArea(2, 4)]
        public string badgeTooltipText = DefaultBadgeTooltipText;

        [Tooltip("Tự tắt bong bóng sau bao nhiêu giây. 0 là không tự tắt.")]
        [Min(0f)] public float tooltipAutoHideSeconds = 4f;

        [Tooltip("Độ mờ của badge khi đang không hiện bong bóng. 1 là rõ hoàn toàn.")]
        [Range(0.1f, 1f)] public float badgeIdleAlpha = 0.55f;

        [Tooltip("Thời gian chuyển giữa mờ và rõ, tính bằng giây. 0 là đổi tức thì.")]
        [Min(0f)] public float badgeFadeSeconds = 0.15f;

        [Header("Playtime Tracker")]
        [Tooltip("Tự đếm ngay từ lúc game khởi động, không cần gọi VerifyAccountSdk.Playtime.Start(). " +
                 "Đây là ngoại lệ duy nhất của quy tắc \"SDK không tự sinh gì\" — panel và badge vẫn " +
                 "chỉ xuất hiện khi game gọi. Tắt đi nếu muốn tự chọn thời điểm bắt đầu đếm.")]
        public bool autoStartPlaytime = true;

        [Tooltip("Tổng số phút chơi trong một ngày trước khi phát cảnh báo " +
                 "VerifyAccountSdk.Playtime.DailyLimitReached. 0 là tắt cảnh báo.")]
        [Min(0)] public int dailyPlayLimitMinutes = 180;

        [Tooltip("Chạm mốc thì tự bật luôn bong bóng cảnh báo của badge, nếu badge đang hiện. " +
                 "Tắt đi nếu game muốn tự dựng popup cảnh báo trong callback.")]
        public bool showBadgeTooltipOnDailyLimit = true;
    }
}
