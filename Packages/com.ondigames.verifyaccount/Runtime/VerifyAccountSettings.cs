using TMPro;
using UnityEngine;

namespace OnDi.VerifyAccount
{
    [CreateAssetMenu(fileName = ResourceName, menuName = "OnDi/Verify Account Settings")]
    public sealed class VerifyAccountSettings : ScriptableObject
    {
        public const string ResourceName = "VerifyAccountSettings";

        public const string DefaultResourcePath = "OnDiVerify/DefaultSettings";

        [Header("Policy Links")]
        [Tooltip("Opened when the player taps the \"Terms of use\" line.")]
        public string termsUrl = "";

        [Tooltip("Opened when the player taps the \"Personal data protection policy\" line.")]
        public string privacyUrl = "";

        [Header("Font")]
        [Tooltip("TextMeshPro font used for every piece of text in the SDK - this is where each " +
                 "project plugs in its own font. Leave empty to use the TextMeshPro default " +
                 "(LiberationSans SDF, which covers Vietnamese diacritics).")]
        public TMP_FontAsset uiFont;

        [Tooltip("Material preset that goes with the font (outline, shadow...). Leave empty to " +
                 "use the font's own material.")]
        public Material uiFontMaterial;

        [Header("Canvas")]
        [Tooltip("Sorting layer of the SDK canvas. Falls back to Default when empty or unknown.")]
        public string sortingLayerName = "Default";

        [Tooltip("Sorting order. Keep it high so the SDK always draws above the game UI.")]
        public int sortingOrder = 32000;

        [Header("Validation")]
        [Tooltip("Regular expression for the phone number. Empty accepts any non-empty string.")]
        public string phoneRegex = @"^(0|\+84)(3|5|7|8|9)\d{8}$";

        [Min(1)] public int otpLength = 6;

        [Tooltip("Minimum age computed from the birth date. 0 disables the check.")]
        [Min(0)] public int minAge = 0;

        [Header("OTP Timing")]
        [Tooltip("Countdown for the \"OTP expires in\" line, in seconds.")]
        [Min(1)] public int otpTtlSeconds = 180;

        [Tooltip("How many seconds the \"Resend\" button stays locked after each send.")]
        [Min(0)] public int resendCooldownSeconds = 60;

        [Header("Panel")]
        [Tooltip("Show the \"Skip\" button. Override at runtime with " +
                 "VerifyAccountSdk.SetSkipButtonVisible().")]
        public bool showSkipButton = true;

        [Header("Floating Badge")]
        [Tooltip("Leave empty to use the default icon baked into the prefab.")]
        public Sprite badgeSprite;

        public const string DefaultBadgeTooltipText =
            "Chơi quá\n180 phút một ngày\nsẽ ảnh hưởng xấu\nđến sức khỏe";

        [TextArea(2, 4)]
        public string badgeTooltipText = DefaultBadgeTooltipText;

        [Tooltip("Auto-hide the bubble after this many seconds. 0 keeps it open.")]
        [Min(0f)] public float tooltipAutoHideSeconds = 4f;

        [Tooltip("Badge opacity while no bubble is showing. 1 is fully opaque.")]
        [Range(0.1f, 1f)] public float badgeIdleAlpha = 0.55f;

        [Tooltip("Fade time between dimmed and opaque, in seconds. 0 switches instantly.")]
        [Min(0f)] public float badgeFadeSeconds = 0.15f;

        [Header("Playtime Tracker")]
        [Tooltip("Start counting as soon as the game boots, without calling " +
                 "VerifyAccountSdk.Playtime.Start(). This is the only exception to the \"the SDK " +
                 "spawns nothing on its own\" rule - the panel and the badge still appear only " +
                 "when the game asks. Turn it off to pick the starting point yourself.")]
        public bool autoStartPlaytime = true;

        [Tooltip("Total minutes played in a day before VerifyAccountSdk.Playtime.DailyLimitReached " +
                 "fires. 0 disables the warning.")]
        [Min(0)] public int dailyPlayLimitMinutes = 180;

        [Tooltip("Pop the badge warning bubble automatically when the limit is hit, if the badge " +
                 "is visible. Turn it off to build your own popup inside the callback.")]
        public bool showBadgeTooltipOnDailyLimit = true;
    }
}
