using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace OnDi.VerifyAccount
{
    /// <summary>Kết quả một lần gọi server. <see cref="Message"/> hiện thẳng lên form khi lỗi.</summary>
    public struct SdkResult
    {
        public bool Ok;
        public string Message;

        public static SdkResult Success(string message = null) => new SdkResult { Ok = true, Message = message };
        public static SdkResult Fail(string message) => new SdkResult { Ok = false, Message = message };
    }

    public sealed class SendOtpRequest
    {
        public string FullName;
        public string PhoneNumber;
    }

    public sealed class VerifyOtpRequest
    {
        public string PhoneNumber;
        public string Otp;
    }

    public sealed class VerifiedProfile
    {
        public string FullName;
        public string PhoneNumber;
        public DateTime BirthDate;
    }

    /// <summary>
    /// Điểm vào duy nhất của SDK. Không có GameObject nào tồn tại cho tới lần gọi
    /// <see cref="Show"/> hoặc <see cref="FloatButton.Show"/> đầu tiên.
    /// Hướng dẫn đầy đủ: API.md.
    /// </summary>
    public static class VerifyAccountSdk
    {
        const string PrefVerified = "OnDi.VerifyAccount.Verified";
        const string PrefVerifiedAt = "OnDi.VerifyAccount.VerifiedAt";

        /// <summary>Gọi khi người chơi bấm "Gửi OTP" hoặc "Gửi lại".</summary>
        public static Func<SendOtpRequest, Task<SdkResult>> OnSendOtp;

        /// <summary>Gọi khi người chơi bấm "Hoàn thành", trước <see cref="OnSubmitProfile"/>.</summary>
        public static Func<VerifyOtpRequest, Task<SdkResult>> OnVerifyOtp;

        /// <summary>Gọi sau khi OTP đúng. Đây là chỗ game lưu thông tin lên server của mình.</summary>
        public static Func<VerifiedProfile, Task<SdkResult>> OnSubmitProfile;

        /// <summary>Xác thực thành công trọn vẹn.</summary>
        public static event Action<VerifiedProfile> Verified;

        /// <summary>Người chơi bấm "Bỏ qua".</summary>
        public static event Action Skipped;

        /// <summary>Panel đóng lại, vì bất kỳ lý do gì.</summary>
        public static event Action Closed;

        static VerifyAccountSettings _settings;
        static bool? _skipVisibleOverride;
        static TMP_FontAsset _fontOverride;
        static Material _fontMaterialOverride;

        public static VerifyAccountSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;

                // File của game trước, không có thì rơi về bản đóng gói sẵn trong SDK. Thứ tự
                // này là cố định, không phụ thuộc việc Resources.Load chọn cái nào khi trùng tên.
                // Dùng `==` chứ không dùng `??`: toán tử null của C# bỏ qua phép so sánh null
                // riêng của UnityEngine.Object.
                _settings = Resources.Load<VerifyAccountSettings>(VerifyAccountSettings.ResourceName);
                if (_settings == null)
                    _settings = Resources.Load<VerifyAccountSettings>(
                        VerifyAccountSettings.DefaultResourcePath);

                if (_settings == null)
                {
                    Debug.LogWarning(
                        "[VerifyAccount] Không tìm thấy Resources/" + VerifyAccountSettings.ResourceName +
                        " lẫn bản mặc định của SDK. Đang chạy bằng giá trị khởi tạo. Tạo file bằng " +
                        "Create > OnDi > Verify Account Settings rồi đặt vào một thư mục Resources.");
                    _settings = ScriptableObject.CreateInstance<VerifyAccountSettings>();
                }
                return _settings;
            }
        }

        public static bool IsVerified => PlayerPrefs.GetInt(PrefVerified, 0) == 1;

        /// <summary>Thời điểm xác thực (UTC). <c>default</c> nếu chưa xác thực.</summary>
        public static DateTime VerifiedAtUtc
        {
            get
            {
                var raw = PlayerPrefs.GetString(PrefVerifiedAt, "");
                return DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d)
                    ? d
                    : default;
            }
        }

        /// <summary>Xoá cờ đã xác thực để hỏi lại từ đầu.</summary>
        public static void ClearVerified()
        {
            PlayerPrefs.DeleteKey(PrefVerified);
            PlayerPrefs.DeleteKey(PrefVerifiedAt);
            PlayerPrefs.Save();
        }

        /// <summary>Mở form xác thực. Không làm gì nếu <see cref="IsVerified"/> đã true.</summary>
        public static void Show()
        {
            if (IsVerified) return;
            ShowForced();
        }

        /// <summary>Mở form xác thực kể cả khi đã xác thực rồi.</summary>
        public static void ShowForced() => SdkRoot.Ensure().ShowPanel();

        /// <summary>Đóng form. Không phát <see cref="Skipped"/>.</summary>
        public static void Hide() => SdkRoot.Current?.HidePanel();

        public static bool IsPanelOpen => SdkRoot.Current != null && SdkRoot.Current.IsPanelOpen;

        /// <summary>
        /// Bật/tắt nút "Bỏ qua" lúc chạy, đè lên giá trị trong Settings — dùng cho remote config.
        /// Truyền <c>null</c> để quay lại giá trị trong Settings.
        /// </summary>
        public static void SetSkipButtonVisible(bool? visible)
        {
            _skipVisibleOverride = visible;
            SdkRoot.Current?.RefreshPanelSkipButton();
        }

        internal static bool SkipButtonVisible => _skipVisibleOverride ?? Settings.showSkipButton;

        /// <summary>
        /// Đổi font của cả SDK lúc chạy, đè lên <see cref="VerifyAccountSettings.uiFont"/>.
        /// <c>null</c> là quay lại font trong Settings.
        /// </summary>
        public static void SetUiFont(TMP_FontAsset font, Material fontMaterial = null)
        {
            _fontOverride = font;
            _fontMaterialOverride = fontMaterial;
            SdkRoot.Current?.ApplyUiFont();
        }

        internal static TMP_FontAsset UiFont => _fontOverride != null ? _fontOverride : Settings.uiFont;

        internal static Material UiFontMaterial =>
            _fontOverride != null ? _fontMaterialOverride : Settings.uiFontMaterial;

        /// <summary>Badge 18+ nổi neo viền màn hình.</summary>
        public static class FloatButton
        {
            /// <summary>Hiện badge. Lần gọi đầu mới sinh GameObject.</summary>
            public static void Show() => SdkRoot.Ensure().ShowBadge();

            public static void Hide() => SdkRoot.Current?.HideBadge();

            /// <summary>Đưa badge về vị trí mặc định và xoá vị trí đã lưu.</summary>
            public static void ResetPosition() => SdkRoot.Current?.ResetBadgePosition();

            public static bool IsVisible => SdkRoot.Current != null && SdkRoot.Current.IsBadgeVisible;
        }

        /// <summary>
        /// Bộ đếm thời gian chơi trong ngày. Tự chạy từ lúc game khởi động trừ khi tắt
        /// <see cref="VerifyAccountSettings.autoStartPlaytime"/>.
        /// </summary>
        public static class Playtime
        {
            /// <summary>
            /// Vượt <see cref="VerifyAccountSettings.dailyPlayLimitMinutes"/>. Phát đúng một lần
            /// mỗi ngày; qua ngày mới bộ đếm về 0 và cảnh báo được phát lại.
            /// </summary>
            public static event Action<TimeSpan> DailyLimitReached;

            /// <summary>Bắt đầu đếm. Gọi lại khi đang chạy cũng không sao.</summary>
            public static void Start() => PlaytimeTracker.StartTracking();

            /// <summary>Tạm dừng đếm và chốt sổ xuống PlayerPrefs.</summary>
            public static void Stop() => PlaytimeTracker.StopTracking();

            public static bool IsRunning => PlaytimeTracker.IsRunning;

            /// <summary>Tổng thời gian đã chơi hôm nay. Đọc được cả khi chưa <see cref="Start"/>.</summary>
            public static TimeSpan Today => PlaytimeTracker.Today;

            public static bool LimitReachedToday => PlaytimeTracker.WarnedToday;

            /// <summary>Số phút còn lại trước khi chạm mốc. 0 nếu đã vượt hoặc mốc bị tắt.</summary>
            public static TimeSpan RemainingToday
            {
                get
                {
                    var limit = Settings.dailyPlayLimitMinutes;
                    if (limit <= 0) return TimeSpan.Zero;
                    var left = TimeSpan.FromMinutes(limit) - Today;
                    return left > TimeSpan.Zero ? left : TimeSpan.Zero;
                }
            }

            /// <summary>Xoá bộ đếm của hôm nay, kể cả cờ đã cảnh báo — chủ yếu để test.</summary>
            public static void ResetToday() => PlaytimeTracker.ResetToday();

            internal static void RaiseDailyLimitReached(TimeSpan total)
            {
                if (Settings.showBadgeTooltipOnDailyLimit)
                    SdkRoot.Current?.ShowBadgeTooltip();

                try
                {
                    DailyLimitReached?.Invoke(total);
                }
                catch (Exception e)
                {
                    // Callback là code của game — lỗi bên đó không được làm chết bộ đếm.
                    Debug.LogException(e);
                }
            }
        }

        internal static Task<SdkResult> InvokeSendOtp(SendOtpRequest request) =>
            Invoke(OnSendOtp, request, nameof(OnSendOtp));

        internal static Task<SdkResult> InvokeVerifyOtp(VerifyOtpRequest request) =>
            Invoke(OnVerifyOtp, request, nameof(OnVerifyOtp));

        internal static Task<SdkResult> InvokeSubmitProfile(VerifiedProfile profile) =>
            Invoke(OnSubmitProfile, profile, nameof(OnSubmitProfile));

        static async Task<SdkResult> Invoke<T>(Func<T, Task<SdkResult>> hook, T arg, string hookName)
        {
            if (hook == null)
            {
                Debug.LogWarning("[VerifyAccount] Chưa gán VerifyAccountSdk." + hookName + ".");
                return SdkResult.Fail("Chức năng chưa sẵn sàng, vui lòng thử lại sau.");
            }

            try
            {
                var task = hook(arg);
                if (task == null)
                {
                    Debug.LogWarning("[VerifyAccount] " + hookName + " trả về null Task.");
                    return SdkResult.Fail("Có lỗi xảy ra, vui lòng thử lại.");
                }
                return await task;
            }
            catch (Exception e)
            {
                // Hook là code của game — lỗi bên đó không được làm kẹt form.
                Debug.LogException(e);
                return SdkResult.Fail("Không kết nối được máy chủ, vui lòng thử lại.");
            }
        }

        internal static void MarkVerified(VerifiedProfile profile)
        {
            PlayerPrefs.SetInt(PrefVerified, 1);
            PlayerPrefs.SetString(PrefVerifiedAt, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
            Verified?.Invoke(profile);
        }

        internal static void RaiseSkipped() => Skipped?.Invoke();
        internal static void RaiseClosed() => Closed?.Invoke();
    }
}
