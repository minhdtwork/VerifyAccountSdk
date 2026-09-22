using System;
using System.Globalization;
using UnityEngine;

namespace OnDi.VerifyAccount
{
    [DisallowMultipleComponent]
    internal sealed class PlaytimeTracker : MonoBehaviour
    {
        const string PrefDay = "OnDi.VerifyAccount.Playtime.Day";
        const string PrefSeconds = "OnDi.VerifyAccount.Playtime.Seconds";
        const string PrefWarned = "OnDi.VerifyAccount.Playtime.Warned";

        const float MaxFrameSeconds = 5f;

        static PlaytimeTracker _instance;

        static bool _loaded;
        static string _day;
        static float _seconds;
        static bool _warned;

        internal static bool IsRunning => _instance != null && _instance.isActiveAndEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoStart()
        {
            if (VerifyAccountSdk.Settings.autoStartPlaytime) StartTracking();
        }

        internal static void ResetStatics()
        {
            _instance = null;
            _loaded = false;
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
            _instance.enabled = false;
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
