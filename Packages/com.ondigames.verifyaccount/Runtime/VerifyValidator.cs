using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OnDi.VerifyAccount
{
    /// <summary>Luật kiểm tra dữ liệu của form, tách thành hàm thuần để test không cần scene.</summary>
    public static class VerifyValidator
    {
        public const string DateFormat = "dd/MM/yyyy";
        public const int MaxNameLength = 50;

        public static bool IsNameValid(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var trimmed = name.Trim();
            return trimmed.Length >= 2 && trimmed.Length <= MaxNameLength;
        }

        public static bool IsPhoneValid(string phone, string pattern)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            if (string.IsNullOrEmpty(pattern)) return true;

            // Regex hỏng trong Settings là lỗi cấu hình, không được làm sập form.
            try { return Regex.IsMatch(phone.Trim(), pattern); }
            catch (ArgumentException) { return false; }
        }

        public static bool IsOtpValid(string otp, int length)
        {
            if (string.IsNullOrEmpty(otp) || otp.Length != length) return false;
            foreach (var c in otp)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }

        /// <summary>
        /// Parse <c>dd/MM/yyyy</c>, chặn ngày trong tương lai và kiểm tra tuổi tối thiểu
        /// (<paramref name="minAge"/> bằng 0 là bỏ qua).
        /// </summary>
        public static bool TryParseBirthDate(string text, int minAge, out DateTime date)
        {
            return TryParseBirthDate(text, minAge, DateTime.Today, out date);
        }

        /// <summary>Bản nhận mốc "hôm nay" từ ngoài để test không phụ thuộc đồng hồ máy.</summary>
        public static bool TryParseBirthDate(string text, int minAge, DateTime today, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (!DateTime.TryParseExact(text.Trim(), DateFormat, CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out date))
                return false;

            if (date.Date > today.Date) return false;
            return minAge <= 0 || AgeOn(date, today) >= minAge;
        }

        public static int AgeOn(DateTime birth, DateTime onDate)
        {
            var age = onDate.Year - birth.Year;
            if (birth.Date > onDate.Date.AddYears(-age)) age--;
            return age;
        }

        /// <summary>
        /// Chèn dấu <c>/</c> trong lúc gõ: <c>"11031991"</c> thành <c>"11/03/1991"</c>.
        /// Mọi ký tự không phải chữ số đều bị bỏ, tối đa 8 chữ số.
        /// </summary>
        public static string MaskDate(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            var digits = new StringBuilder(8);
            foreach (var c in raw)
            {
                if (c < '0' || c > '9') continue;
                digits.Append(c);
                if (digits.Length == 8) break;
            }

            var d = digits.ToString();
            if (d.Length <= 2) return d;
            if (d.Length <= 4) return d.Substring(0, 2) + "/" + d.Substring(2);
            return d.Substring(0, 2) + "/" + d.Substring(2, 2) + "/" + d.Substring(4);
        }

        /// <summary>Định dạng giây còn lại thành <c>m:ss</c> cho dòng "OTP hết hạn sau".</summary>
        public static string FormatCountdown(float secondsLeft)
        {
            var total = Mathf_CeilToInt(secondsLeft);
            if (total < 0) total = 0;
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        // Tránh kéo UnityEngine vào file thuần logic.
        static int Mathf_CeilToInt(float v) => (int)Math.Ceiling(v);
    }
}
