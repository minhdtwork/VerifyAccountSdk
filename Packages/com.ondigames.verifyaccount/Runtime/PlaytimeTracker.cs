using System;
using System.Globalization;
using UnityEngine;

namespace OnDi.VerifyAccount
{
    /// <summary>
    /// Cộng dồn thời gian thực người chơi ở trong game, theo từng ngày lịch của thiết bị.
    /// Vượt mốc <see cref="VerifyAccountSettings.dailyPlayLimitMinutes"/> thì phát
    /// <see cref="VerifyAccountSdk.Playtime.DailyLimitReached"/> đúng một lần, qua ngày mới
    /// thì bộ đếm và cờ đã cảnh báo cùng về 0.
    ///
    /// <para>Số giây nằm trong PlayerPrefs nên tắt game mở lại vẫn cộng tiếp trong cùng ngày.
    /// Thời gian app chạy nền không được tính: Update ngừng chạy nên không cộng thêm.</para>
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PlaytimeTracker : MonoBehaviour
    {
        const string PrefDay = "OnDi.VerifyAccount.Playtime.Day";
        const string PrefSeconds = "OnDi.VerifyAccount.Playtime.Seconds";
        const string PrefWarned = "OnDi.VerifyAccount.Playtime.Warned";

        /// <summary>Chặn một frame kéo dài bất thường (loading, vừa resume) làm phồng bộ đếm.</summary>
        const float MaxFrameSeconds = 5f;

        /// <summary>Ghi PlayerPrefs thưa ra để không đụng ổ đĩa mỗi frame.</summary>
        const float SaveIntervalSeconds = 20f;

        static PlaytimeTracker _instance;

        static bool _loaded;
        static string _day;
        static float _seconds;
        static bool _warned;

        float _sinceSave;

        internal static bool IsRunning => _instance != null && _instance.isActiveAndEnabled;

        /// <summary>Tổng thời gian đã chơi trong ngày hôm nay, đọc được cả khi chưa Start.</summary>
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
            _instance.enabled = false;
            Save();
        }

        /// <summary>Xoá bộ đếm của hôm nay, kể cả cờ đã cảnh báo.</summary>
        internal static void ResetToday()
        {
            EnsureLoaded();
            _day = TodayKey();
            _seconds = 0f;
            _warned = false;
            Save();
        }

        // ---- Vòng đời ----

        void Update()
        {
            if (RollOverIfNeeded()) Save();

            var delta = Mathf.Min(Time.unscaledDeltaTime, MaxFrameSeconds);
            _seconds += delta;

            CheckLimit();

            _sinceSave += delta;
            if (_sinceSave < SaveIntervalSeconds) return;
            _sinceSave = 0f;
            Save();
        }

        void OnApplicationPause(bool paused)
        {
            // Vào nền: chốt sổ. Ra khỏi nền: có thể đã sang ngày mới.
            if (paused) Save();
            else if (RollOverIfNeeded()) Save();
        }

        void OnApplicationQuit() => Save();

        void OnDisable() => Save();

        // ---- Trạng thái ----

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
