using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OnDi.VerifyAccount
{
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

        public static bool TryParseBirthDate(string text, int minAge, out DateTime date)
        {
            return TryParseBirthDate(text, minAge, DateTime.Today, out date);
        }

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

        public static string DescribeBirthDateProblem(string text, int minAge) =>
            DescribeBirthDateProblem(text, minAge, DateTime.Today);

        public static string DescribeBirthDateProblem(string text, int minAge, DateTime today)
        {
            if (text == null || text.Length < DateFormat.Length) return null;
            if (!TryParseBirthDate(text, 0, today, out var birth)) return "Ngày sinh không hợp lệ.";
            if (minAge > 0 && AgeOn(birth, today) < minAge)
                return "Bạn chưa đủ " + minAge + " tuổi để sử dụng dịch vụ.";
            return null;
        }

        public static int AgeOn(DateTime birth, DateTime onDate)
        {
            var age = onDate.Year - birth.Year;
            if (birth.Date > onDate.Date.AddYears(-age)) age--;
            return age;
        }

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
            if (d.Length == 0) return string.Empty;

            var i = 0;
            if (!TakeDatePart(d, ref i, '4', out var day)) return day;
            if (i >= d.Length) return day;
            if (!TakeDatePart(d, ref i, '2', out var month)) return day + "/" + month;
            if (i >= d.Length) return day + "/" + month;

            var year = d.Substring(i);
            if (year.Length > 4) year = year.Substring(0, 4);
            return day + "/" + month + "/" + year;
        }

        static bool TakeDatePart(string digits, ref int i, char padFrom, out string part)
        {
            var first = digits[i];
            if (first >= padFrom)
            {
                part = "0" + first;
                i++;
                return true;
            }

            if (digits.Length - i >= 2)
            {
                part = digits.Substring(i, 2);
                i += 2;
                return true;
            }

            part = first.ToString();
            i++;
            return false;
        }

        public static string FormatCountdown(float secondsLeft)
        {
            var total = Mathf_CeilToInt(secondsLeft);
            if (total < 0) total = 0;
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        static int Mathf_CeilToInt(float v) => (int)Math.Ceiling(v);
    }
}
