using System;
using System.Globalization;
using UnityEngine;

namespace OnDi.VerifyAccount
{
    /// <summary>
    /// Cộng dồn thời gian thực trong game theo từng ngày lịch của thiết bị. Số giây nằm trong
    /// PlayerPrefs nên tắt game mở lại vẫn cộng tiếp trong cùng ngày.
    /// </summary>
    /// <remarks>
    /// Chỉ ghi PlayerPrefs ở những mốc có thật: vào nền, thoát game, tắt bộ đếm, sang ngày mới,
    /// chạm mốc cảnh báo. Không ghi định kỳ. Đổi lại, app bị giết mà không kịp gọi
    /// <c>OnApplicationPause</c> (thường chỉ xảy ra khi crash) thì mất phần chưa lưu của phiên đó.
    /// </remarks>
    [DisallowMultipleComponent]
    internal sealed class PlaytimeTracker : MonoBehaviour
    {
        const string PrefDay = "OnDi.VerifyAccount.Playtime.Day";
        const string PrefSeconds = "OnDi.VerifyAccount.Playtime.Seconds";
        const string PrefWarned = "OnDi.VerifyAccount.Playtime.Warned";

        /// <summary>Chặn một frame kéo dài bất thường (loading, vừa resume) làm phồng bộ đếm.</summary>
        const float MaxFrameSeconds = 5f;

        static PlaytimeTracker _instance;

        static bool _loaded;
        static string _day;
        static float _seconds;
        static bool _warned;

        internal static bool IsRunning => _instance != null && _instance.isActiveAndEnabled;

        /// <summary>
        /// Ngoại lệ duy nhất của quy tắc "SDK không tự sinh gì": mốc cảnh báo phải tính từ lúc mở
        /// game, chờ game gọi <see cref="StartTracking"/> thì phần thời gian trước đó mất trắng.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoStart()
        {
            // Đọc thẳng Resources thay vì VerifyAccountSdk.Settings: project chưa cấu hình SDK thì
            // im lặng bỏ qua, không bắn cảnh báo vào Console ngay lúc khởi động.
            var settings = Resources.Load<VerifyAccountSettings>(VerifyAccountSettings.ResourceName);
            if (settings != null && settings.autoStartPlaytime) StartTracking();
        }

        internal static TimeSpan Today
        {
            get
            {
                EnsureLoaded();
                RollOverIfNeeded();
                return TimeSpan.FromSeconds(_seconds);
            }
        }

        internal static bool WarnedToday
        {
            get
            {
                EnsureLoaded();
                RollOverIfNeeded();
                return _warned;
            }
        }

        internal static void StartTracking()
        {
            EnsureLoaded();
            if (_instance != null)
            {
                _instance.enabled = true;
                return;
            }

            var go = new GameObject("[OnDiVerifyPlaytime]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PlaytimeTracker>();
        }

        internal static void StopTracking()
        {
            if (_instance == null) return;
            _instance.enabled = false; // OnDisable chốt sổ luôn
        }

        internal static void ResetToday()
        {
            EnsureLoaded();
            _day = TodayKey();
            _seconds = 0f;
            _warned = false;
            Save();
        }

        void Update()
        {
            if (RollOverIfNeeded()) Save();

            _seconds += Mathf.Min(Time.unscaledDeltaTime, MaxFrameSeconds);

            CheckLimit();
        }

        void OnApplicationPause(bool paused)
        {
            // Vào nền: chốt sổ. Ra khỏi nền: có thể đã sang ngày mới.
            if (paused) Save();
            else if (RollOverIfNeeded()) Save();
        }

        void OnApplicationQuit() => Save();

        void OnDisable() => Save();

        static string TodayKey() => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            _day = PlayerPrefs.GetString(PrefDay, "");
            _seconds = PlayerPrefs.GetFloat(PrefSeconds, 0f);
            _warned = PlayerPrefs.GetInt(PrefWarned, 0) == 1;

            if (RollOverIfNeeded()) Save();
        }

        /// <summary>Sang ngày mới thì bộ đếm về 0. Trả về true nếu vừa đổi ngày.</summary>
        static bool RollOverIfNeeded()
        {
            var today = TodayKey();
            if (_day == today) return false;

            _day = today;
            _seconds = 0f;
            _warned = false;
            return true;
        }

        static void CheckLimit()
        {
            if (_warned) return;

            var limitMinutes = VerifyAccountSdk.Settings.dailyPlayLimitMinutes;
            if (limitMinutes <= 0) return;
            if (_seconds < limitMinutes * 60f) return;

            _warned = true;
            Save();
            VerifyAccountSdk.Playtime.RaiseDailyLimitReached(TimeSpan.FromSeconds(_seconds));
        }

        static void Save()
        {
            if (!_loaded) return;
            PlayerPrefs.SetString(PrefDay, _day);
            PlayerPrefs.SetFloat(PrefSeconds, _seconds);
            PlayerPrefs.SetInt(PrefWarned, _warned ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
