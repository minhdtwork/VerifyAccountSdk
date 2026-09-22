using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace OnDi.VerifyAccount
{
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

    public static class VerifyAccountSdk
    {
        const string PrefVerified = "OnDi.VerifyAccount.Verified";
        const string PrefVerifiedAt = "OnDi.VerifyAccount.VerifiedAt";

        public static Func<SendOtpRequest, Task<SdkResult>> OnSendOtp;

        public static Func<VerifyOtpRequest, Task<SdkResult>> OnVerifyOtp;

        public static Func<VerifiedProfile, Task<SdkResult>> OnSubmitProfile;

        public static event Action<VerifiedProfile> Verified;

        public static event Action Skipped;

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

                _settings = Resources.Load<VerifyAccountSettings>(VerifyAccountSettings.ResourceName);
                if (_settings == null)
                    _settings = Resources.Load<VerifyAccountSettings>(
                        VerifyAccountSettings.DefaultResourcePath);

                if (_settings == null)
                {
                    Debug.LogWarning(
                        "[VerifyAccount] Found neither Resources/" + VerifyAccountSettings.ResourceName +
                        " nor the SDK default. Running on freshly constructed values. Create the " +
                        "asset via Create > OnDi > Verify Account Settings and put it in a " +
                        "Resources folder.");
                    _settings = ScriptableObject.CreateInstance<VerifyAccountSettings>();
                }
                return _settings;
            }
        }

        public static bool IsVerified => PlayerPrefs.GetInt(PrefVerified, 0) == 1;

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

        public static void ClearVerified()
        {
            PlayerPrefs.DeleteKey(PrefVerified);
            PlayerPrefs.DeleteKey(PrefVerifiedAt);
            PlayerPrefs.Save();
        }

        public static void Show()
        {
            if (IsVerified) return;
            ShowForced();
        }

        public static void ShowForced() => SdkRoot.Ensure().ShowPanel();

        public static void Hide() => SdkRoot.Current?.HidePanel();

        public static bool IsPanelOpen => SdkRoot.Current != null && SdkRoot.Current.IsPanelOpen;

        public static void SetSkipButtonVisible(bool? visible)
        {
            _skipVisibleOverride = visible;
            SdkRoot.Current?.RefreshPanelSkipButton();
        }

        internal static bool SkipButtonVisible => _skipVisibleOverride ?? Settings.showSkipButton;

        public static void SetUiFont(TMP_FontAsset font, Material fontMaterial = null)
        {
            _fontOverride = font;
            _fontMaterialOverride = fontMaterial;
            SdkRoot.Current?.ApplyUiFont();
        }

        internal static TMP_FontAsset UiFont => _fontOverride != null ? _fontOverride : Settings.uiFont;

        internal static Material UiFontMaterial =>
            _fontOverride != null ? _fontMaterialOverride : Settings.uiFontMaterial;

        public static class FloatButton
        {
            public static void Show() => SdkRoot.Ensure().ShowBadge();

            public static void Hide() => SdkRoot.Current?.HideBadge();

            public static void ResetPosition() => SdkRoot.Current?.ResetBadgePosition();

            public static bool IsVisible => SdkRoot.Current != null && SdkRoot.Current.IsBadgeVisible;
        }

        public static class Playtime
        {
            public static event Action<TimeSpan> DailyLimitReached;

            public static void Start() => PlaytimeTracker.StartTracking();

            public static void Stop() => PlaytimeTracker.StopTracking();

            public static bool IsRunning => PlaytimeTracker.IsRunning;

            public static TimeSpan Today => PlaytimeTracker.Today;

            public static bool LimitReachedToday => PlaytimeTracker.WarnedToday;

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
                Debug.LogWarning("[VerifyAccount] VerifyAccountSdk." + hookName + " has not been assigned.");
                return SdkResult.Fail("Chức năng chưa sẵn sàng, vui lòng thử lại sau.");
            }

            try
            {
                var task = hook(arg);
                if (task == null)
                {
                    Debug.LogWarning("[VerifyAccount] " + hookName + " returned a null Task.");
                    return SdkResult.Fail("Có lỗi xảy ra, vui lòng thử lại.");
                }
                return await task;
            }
            catch (Exception e)
            {
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
