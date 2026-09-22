using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnDi.VerifyAccount
{
    /// <summary>Logic của form xác thực. Các tham chiếu UI do prefab gán sẵn.</summary>
    [DisallowMultipleComponent]
    internal sealed class VerifyPanel : MonoBehaviour
    {
        [Header("Input Fields")]
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] TMP_InputField phoneInput;
        [SerializeField] TMP_InputField otpInput;
        [SerializeField] TMP_InputField dobInput;

        [Header("Buttons")]
        [SerializeField] Button skipButton;
        [SerializeField] Button sendOtpButton;
        [SerializeField] Button resendButton;
        [SerializeField] Button submitButton;
        [SerializeField] Image submitImage;
        [SerializeField] Sprite submitEnabledSprite;
        [SerializeField] Sprite submitDisabledSprite;

        [Header("Terms")]
        [SerializeField] Toggle termsToggle;
        [SerializeField] Toggle privacyToggle;
        [SerializeField] Button termsLinkButton;
        [SerializeField] Button privacyLinkButton;
        [SerializeField] Button agreeHeaderButton;
        [SerializeField] GameObject agreeGroup;
        [SerializeField] RectTransform agreeArrow;

        [Header("Scrolling")]
        [SerializeField] ScrollRect scroll;

        [Header("Messages")]
        [SerializeField] TMP_Text countdownText;
        [SerializeField] TMP_Text nameError;
        [SerializeField] TMP_Text phoneError;
        [SerializeField] TMP_Text otpError;
        [SerializeField] TMP_Text dobError;
        [SerializeField] TMP_Text formError;

        VerifyAccountSettings _settings;
        bool _busy;
        int _scrollToTopIn;
        bool _otpSent;
        bool _maskingDate;
        float _otpExpiresAt;
        float _resendReadyAt;

        void Awake()
        {
            _settings = VerifyAccountSdk.Settings;

            otpInput.characterLimit = _settings.otpLength;

            nameInput.onValueChanged.AddListener(_ => { Clear(nameError); Refresh(); });
            phoneInput.onValueChanged.AddListener(_ => { Clear(phoneError); Refresh(); });
            otpInput.onValueChanged.AddListener(_ => { Clear(otpError); Refresh(); });
            dobInput.onValueChanged.AddListener(OnDobChanged);

            sendOtpButton.onClick.AddListener(() => SendOtp(resend: false));
            resendButton.onClick.AddListener(() => SendOtp(resend: true));
            submitButton.onClick.AddListener(Submit);
            skipButton.onClick.AddListener(OnSkip);

            termsToggle.onValueChanged.AddListener(_ => Refresh());
            privacyToggle.onValueChanged.AddListener(_ => Refresh());
            termsLinkButton.onClick.AddListener(() => OpenUrl(_settings.termsUrl, "Điều khoản sử dụng"));
            privacyLinkButton.onClick.AddListener(() => OpenUrl(_settings.privacyUrl, "Chính sách bảo vệ dữ liệu"));
            agreeHeaderButton.onClick.AddListener(ToggleAgreeGroup);
        }

        internal void Open()
        {
            _busy = false;
            _otpSent = false;
            _otpExpiresAt = 0f;
            _resendReadyAt = 0f;

            nameInput.text = "";
            phoneInput.text = "";
            otpInput.text = "";
            dobInput.text = "";
            termsToggle.isOn = false;
            privacyToggle.isOn = false;

            Clear(nameError);
            Clear(phoneError);
            Clear(otpError);
            Clear(dobError);
            Clear(formError);
            countdownText.gameObject.SetActive(false);

            SetAgreeGroupOpen(true);
            ApplySkipButtonVisibility();
            Refresh();

            _scrollToTopIn = 2;
        }

        /// <summary>
        /// Mỗi lần mở lại phải về đầu form, nhưng chỉ sau khi PanelAutoHeight và layout group
        /// đã chốt kích thước. Thứ tự LateUpdate giữa các component không đảm bảo nên đợi hẳn
        /// vài frame thay vì đặt ngay trong <see cref="Open"/>.
        /// </summary>
        void LateUpdate()
        {
            if (_scrollToTopIn <= 0) return;
            if (--_scrollToTopIn > 0 || scroll == null) return;

            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }

        internal void ApplySkipButtonVisibility()
        {
            skipButton.gameObject.SetActive(VerifyAccountSdk.SkipButtonVisible);
        }

        void Update()
        {
            if (!_otpSent) return;

            var left = _otpExpiresAt - Time.unscaledTime;
            if (left <= 0f)
            {
                if (countdownText.gameObject.activeSelf)
                {
                    countdownText.gameObject.SetActive(false);
                    Show(otpError, "Mã OTP đã hết hạn, vui lòng gửi lại.");
                    Refresh();
                }
            }
            else
            {
                countdownText.text = "OTP hết hạn sau " + VerifyValidator.FormatCountdown(left);
            }

            var resendReady = Time.unscaledTime >= _resendReadyAt;
            if (resendButton.interactable != (resendReady && !_busy))
                Refresh();
        }

        void OnDobChanged(string raw)
        {
            if (_maskingDate) return;

            var masked = VerifyValidator.MaskDate(raw);
            if (masked != raw)
            {
                _maskingDate = true;
                dobInput.text = masked;

                // Phải là stringPosition chứ không phải caretPosition. caretPosition đếm theo
                // ký tự đã dựng hình và bị kẹp vào `characterCount - 1` của textInfo — mà lúc này
                // textInfo vẫn là của chuỗi cũ, ngắn hơn một ký tự. Kết quả: con trỏ tụt về trước
                // ký tự vừa gõ, đúng lúc mask chèn thêm dấu "/" (tức từ phần tháng trở đi).
                // stringPosition kẹp theo text.Length nên không dính, TMP tự quy ra chỗ con trỏ
                // sau khi label cập nhật xong.
                dobInput.stringPosition = masked.Length;
                _maskingDate = false;
            }

            ValidateDobLive();
            Refresh();
        }

        /// <summary>
        /// Báo lỗi ngay khi gõ đủ <c>dd/mm/yyyy</c> thay vì đợi tới lúc bấm "Hoàn thành".
        /// Gõ dở thì im lặng — không ai muốn bị mắng lúc mới bấm được hai chữ số.
        /// </summary>
        void ValidateDobLive() =>
            Show(dobError, VerifyValidator.DescribeBirthDateProblem(dobInput.text, _settings.minAge));

        bool PhoneOk => VerifyValidator.IsPhoneValid(phoneInput.text, _settings.phoneRegex);
        bool NameOk => VerifyValidator.IsNameValid(nameInput.text);
        bool OtpOk => _otpSent && VerifyValidator.IsOtpValid(otpInput.text, _settings.otpLength);
        bool DobOk => VerifyValidator.TryParseBirthDate(dobInput.text, _settings.minAge, out _);
        bool AgreedOk => termsToggle.isOn && privacyToggle.isOn;

        bool CanSubmit => !_busy && NameOk && PhoneOk && OtpOk && DobOk && AgreedOk &&
                          Time.unscaledTime < _otpExpiresAt;

        void Refresh()
        {
            sendOtpButton.interactable = !_busy && !_otpSent && NameOk && PhoneOk;
            SetRowVisible(sendOtpButton, !_otpSent);

            SetRowVisible(resendButton, _otpSent);
            resendButton.interactable = !_busy && Time.unscaledTime >= _resendReadyAt;

            otpInput.interactable = _otpSent && !_busy;

            var can = CanSubmit;
            submitButton.interactable = can;
            if (submitImage != null)
                submitImage.sprite = can ? submitEnabledSprite : submitDisabledSprite;
        }

        /// <summary>
        /// Ẩn cả hàng chứ không chỉ ẩn nút. Nút nằm trong một hàng riêng để canh trái được, mà
        /// hàng mới là mục của layout group — ẩn mỗi nút thì hàng vẫn giữ nguyên chiều cao và
        /// form thủng một mảng trống.
        /// </summary>
        static void SetRowVisible(Component inRow, bool visible)
        {
            var parent = inRow.transform.parent;
            var row = parent != null ? parent.gameObject : inRow.gameObject;
            if (row.activeSelf != visible) row.SetActive(visible);
        }

        async void SendOtp(bool resend)
        {
            if (_busy) return;

            if (!NameOk) { Show(nameError, "Vui lòng nhập họ và tên."); return; }
            if (!PhoneOk) { Show(phoneError, "Số điện thoại không hợp lệ."); return; }

            SetBusy(true);
            Clear(formError);

            var result = await VerifyAccountSdk.InvokeSendOtp(new SendOtpRequest
            {
                FullName = nameInput.text.Trim(),
                PhoneNumber = phoneInput.text.Trim(),
            });

            if (this == null) return; // panel bị huỷ trong lúc chờ mạng
            SetBusy(false);

            if (!result.Ok)
            {
                Show(phoneError, Fallback(result.Message, "Không gửi được mã OTP, vui lòng thử lại."));
                return;
            }

            _otpSent = true;
            _otpExpiresAt = Time.unscaledTime + _settings.otpTtlSeconds;
            _resendReadyAt = Time.unscaledTime + _settings.resendCooldownSeconds;
            if (resend) otpInput.text = "";

            Clear(otpError);
            countdownText.gameObject.SetActive(true);
            countdownText.text = "OTP hết hạn sau " + VerifyValidator.FormatCountdown(_settings.otpTtlSeconds);
            Refresh();
        }

        async void Submit()
        {
            if (!CanSubmit) return;
            if (!VerifyValidator.TryParseBirthDate(dobInput.text, _settings.minAge, out var birthDate))
            {
                Show(dobError, _settings.minAge > 0
                    ? "Ngày sinh không hợp lệ hoặc chưa đủ " + _settings.minAge + " tuổi."
                    : "Ngày sinh không hợp lệ (dd/mm/yyyy).");
                return;
            }

            SetBusy(true);
            Clear(formError);

            var phone = phoneInput.text.Trim();
            var verify = await VerifyAccountSdk.InvokeVerifyOtp(new VerifyOtpRequest
            {
                PhoneNumber = phone,
                Otp = otpInput.text.Trim(),
            });

            if (this == null) return;
            if (!verify.Ok)
            {
                SetBusy(false);
                Show(otpError, Fallback(verify.Message, "Mã OTP không đúng."));
                return;
            }

            var profile = new VerifiedProfile
            {
                FullName = nameInput.text.Trim(),
                PhoneNumber = phone,
                BirthDate = birthDate,
            };

            var submit = await VerifyAccountSdk.InvokeSubmitProfile(profile);

            if (this == null) return;
            SetBusy(false);

            if (!submit.Ok)
            {
                Show(formError, Fallback(submit.Message, "Lưu thông tin thất bại, vui lòng thử lại."));
                return;
            }

            VerifyAccountSdk.MarkVerified(profile);
            SdkRoot.Current?.HidePanel();
        }

        void OnSkip()
        {
            if (_busy) return;
            VerifyAccountSdk.RaiseSkipped();
            SdkRoot.Current?.HidePanel();
        }

        void SetBusy(bool busy)
        {
            _busy = busy;
            Refresh();
        }

        void ToggleAgreeGroup() => SetAgreeGroupOpen(!agreeGroup.activeSelf);

        void SetAgreeGroupOpen(bool open)
        {
            agreeGroup.SetActive(open);
            if (agreeArrow != null)
                agreeArrow.localRotation = Quaternion.Euler(0f, 0f, open ? 0f : 180f);
        }

        static void OpenUrl(string url, string label)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning("[VerifyAccount] Chưa cấu hình link cho \"" + label +
                                 "\" trong VerifyAccountSettings.");
                return;
            }
            Application.OpenURL(url);
        }

        static void Show(TMP_Text target, string message)
        {
            if (target == null) return;
            target.text = message;
            target.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        static void Clear(TMP_Text target) => Show(target, null);

        static string Fallback(string message, string standIn) =>
            string.IsNullOrWhiteSpace(message) ? standIn : message;
    }
}
