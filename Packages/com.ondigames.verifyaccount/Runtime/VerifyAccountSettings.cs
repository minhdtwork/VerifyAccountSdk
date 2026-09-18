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

        [Header("Link chính sách")]
        [Tooltip("Mở khi người chơi bấm vào dòng \"Điều khoản sử dụng\".")]
        public string termsUrl = "";

        [Tooltip("Mở khi người chơi bấm vào dòng \"Chính sách bảo vệ và xử lý dữ liệu cá nhân\".")]
        public string privacyUrl = "";

        [Header("Canvas")]
        [Tooltip("Sorting layer của canvas SDK. Để trống hoặc tên không tồn tại thì dùng Default.")]
        public string sortingLayerName = "Default";

        [Tooltip("Sorting order. Để cao để luôn nằm trên UI của game.")]
        public int sortingOrder = 32000;

        [Header("Kiểm tra dữ liệu")]
        [Tooltip("Biểu thức chính quy cho số điện thoại. Để trống là chấp nhận mọi chuỗi khác rỗng.")]
        public string phoneRegex = @"^(0|\+84)(3|5|7|8|9)\d{8}$";

        [Min(1)] public int otpLength = 6;

        [Tooltip("Tuổi tối thiểu tính theo ngày sinh. 0 là không kiểm tra.")]
        [Min(0)] public int minAge = 0;

        [Header("Thời gian OTP")]
        [Tooltip("Đồng hồ đếm ngược \"OTP hết hạn sau\", tính bằng giây.")]
        [Min(1)] public int otpTtlSeconds = 180;

        [Tooltip("Khoá nút \"Gửi lại\" bao nhiêu giây sau mỗi lần gửi.")]
        [Min(0)] public int resendCooldownSeconds = 60;

        [Header("Panel")]
        [Tooltip("Hiện nút \"Bỏ qua\". Đổi lúc chạy bằng VerifyAccountSdk.SetSkipButtonVisible().")]
        public bool showSkipButton = true;

        [Header("Badge nổi")]
        [Tooltip("Để trống thì dùng icon mặc định trong prefab.")]
        public Sprite badgeSprite;

        [TextArea(2, 4)]
        public string badgeTooltipText = "Chơi quá 180 phút một ngày sẽ ảnh hưởng xấu đến sức khỏe";

        [Tooltip("Tự tắt bong bóng sau bao nhiêu giây. 0 là không tự tắt.")]
        [Min(0f)] public float tooltipAutoHideSeconds = 4f;
    }
}
