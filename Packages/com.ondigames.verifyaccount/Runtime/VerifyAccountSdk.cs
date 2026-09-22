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
    /// Điểm vào duy nhất của SDK.
    ///
    /// <para>SDK không tự sinh gì cả — không có GameObject nào tồn tại cho tới lần gọi
    /// <see cref="Show"/> hoặc <see cref="FloatButton.Show"/> đầu tiên.</para>
    ///
    /// <code>
    /// VerifyAccountSdk.OnSendOtp       = req  => MyBackend.SendOtpAsync(req.PhoneNumber);
    /// VerifyAccountSdk.OnVerifyOtp     = req  => MyBackend.VerifyOtpAsync(req.PhoneNumber, req.Otp);
    /// VerifyAccountSdk.OnSubmitProfile = prof => MyBackend.SaveProfileAsync(prof);
    /// VerifyAccountSdk.Verified       += prof => Debug.Log("xong " + prof.PhoneNumber);
    /// VerifyAccountSdk.Show();
    /// </code>
    /// </summary>
    public static class VerifyAccountSdk
    {
        const string PrefVerified = "OnDi.VerifyAccount.Verified";
        const string PrefVerifiedAt = "OnDi.VerifyAccount.VerifiedAt";

        // ---- Hook: game cắm hàm gọi server của mình vào đây ----

        /// <summary>Gọi khi người chơi bấm "Gửi OTP" hoặc "Gửi lại".</summary>
        public static Func<SendOtpRequest, Task<SdkResult>> OnSendOtp;

        /// <summary>Gọi khi người chơi bấm "Hoàn thành", trước <see cref="OnSubmitProfile"/>.</summary>
        public static Func<VerifyOtpRequest, Task<SdkResult>> OnVerifyOtp;

        /// <summary>Gọi sau khi OTP đúng. Đây là chỗ game lưu thông tin lên server của mình.</summary>
        public static Func<VerifiedProfile, Task<SdkResult>> OnSubmitProfile;

        // ---- Sự kiện ----

        /// <summary>Xác thực thành công trọn vẹn.</summary>
        public static event Action<VerifiedProfile> Verified;

        /// <summary>Người chơi bấm "Bỏ qua".</summary>
        public static event Action Skipped;

        /// <summary>Panel đóng lại, vì bất kỳ lý do gì.</summary>
        public static event Action Closed;

        // ---- Trạng thái ----

        static VerifyAccountSettings _settings;
        static bool? _skipVisibleOverride;
        static TMP_FontAsset _fontOverride;
        static Material _fontMaterialOverride;

        public static VerifyAccountSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;

                _settings = Resources.Load<VerifyAccountSettings>(VerifyAccountSettings.ResourceName);
                if (_settings == null)
                {
                    Debug.LogWarning(
                        "[VerifyAccount] Không tìm thấy Resources/" + VerifyAccountSettings.ResourceName +
                        ". Đang chạy bằng giá trị mặc định. Tạo file bằng Create > OnDi > Verify Account Settings " +
                        "rồi đặt vào một thư mục Resources.");
                    _settings = ScriptableObject.CreateInstance<VerifyAccountSettings>();
                }
                return _settings;
            }
        }

        /// <summary>Đã xác thực xong trên thiết bị này hay chưa.</summary>
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

        // ---- Điều khiển ----

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

        /// <summary>Panel đang mở hay không.</summary>
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
        /// Đổi font TextMeshPro của toàn bộ chữ trong SDK lúc chạy, đè lên
        /// <see cref="VerifyAccountSettings.uiFont"/>. Truyền <c>null</c> để quay lại font trong
        /// Settings. Gọi lúc nào cũng được, kể cả khi form đang mở.
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

            /// <summary>Ẩn badge.</summary>
            public static void Hide() => SdkRoot.Current?.HideBadge();

            /// <summary>Đưa badge về vị trí mặc định và xoá vị trí đã lưu.</summary>
            public static void ResetPosition() => SdkRoot.Current?.ResetBadgePosition();

            public static bool IsVisible => SdkRoot.Current != null && SdkRoot.Current.IsBadgeVisible;
        }

        /// <summary>
        /// Bộ đếm thời gian chơi trong ngày. Cộng dồn thời gian thực người chơi ở trong game
        /// (không tính lúc app chạy nền, không bị <c>Time.timeScale</c> ảnh hưởng), lưu vào
        /// PlayerPrefs nên thoát game mở lại vẫn cộng tiếp trong cùng ngày.
        ///
        /// <para>Mặc định bộ đếm <b>tự chạy</b> ngay khi game khởi động, nên chỉ cần gắn callback:</para>
        ///
        /// <code>
        /// VerifyAccountSdk.Playtime.DailyLimitReached += total =>
        ///     MyUi.ShowWarning($"Bạn đã chơi {total.TotalMinutes:0} phút hôm nay.");
        /// </code>
        ///
        /// <para>Tắt <see cref="VerifyAccountSettings.autoStartPlaytime"/> nếu muốn tự chọn thời
        /// điểm bắt đầu bằng <see cref="Start"/>.</para>
        /// </summary>
        public static class Playtime
        {
            /// <summary>
            /// Tổng thời gian chơi trong ngày vượt <see cref="VerifyAccountSettings.dailyPlayLimitMinutes"/>
            /// (mặc định 180 phút). Phát **đúng một lần mỗi ngày**; qua ngày mới bộ đếm về 0 và
            /// cảnh báo được phát lại.
            /// </summary>
            public static event Action<TimeSpan> DailyLimitReached;

            /// <summary>
            /// Bắt đầu đếm. Không cần gọi nếu <see cref="VerifyAccountSettings.autoStartPlaytime"/>
            /// đang bật. Gọi lại khi đang chạy cũng không sao.
            /// </summary>
            public static void Start() => PlaytimeTracker.StartTracking();

            /// <summary>Tạm dừng đếm và chốt sổ xuống PlayerPrefs.</summary>
            public static void Stop() => PlaytimeTracker.StopTracking();

            /// <summary>Đang đếm hay không.</summary>
            public static bool IsRunning => PlaytimeTracker.IsRunning;

            /// <summary>Tổng thời gian đã chơi hôm nay. Đọc được cả khi chưa <see cref="Start"/>.</summary>
            public static TimeSpan Today => PlaytimeTracker.Today;

            /// <summary>Hôm nay đã phát cảnh báo hay chưa.</summary>
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

        // ---- Nội bộ: gọi hook an toàn ----

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
