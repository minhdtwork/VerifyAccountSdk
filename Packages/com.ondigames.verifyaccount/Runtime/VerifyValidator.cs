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

        /// <summary>
        /// Câu cảnh báo cho ô ngày sinh trong lúc người chơi đang gõ, hoặc <c>null</c> khi chưa
        /// có gì để nói — gõ dở hoặc ngày đã hợp lệ. Tách riêng khỏi
        /// <see cref="TryParseBirthDate(string,int,out DateTime)"/> vì "sai ngày" và "chưa đủ
        /// tuổi" cần hai câu khác nhau.
        /// </summary>
        public static string DescribeBirthDateProblem(string text, int minAge) =>
            DescribeBirthDateProblem(text, minAge, DateTime.Today);

        /// <summary>Bản nhận mốc "hôm nay" từ ngoài để test không phụ thuộc đồng hồ máy.</summary>
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

        /// <summary>
        /// Chèn dấu <c>/</c> trong lúc gõ: <c>"11031991"</c> thành <c>"11/03/1991"</c>.
        /// Mọi ký tự không phải chữ số đều bị bỏ, tối đa 8 chữ số.
        ///
        /// <para>Chữ số đầu của ô ngày mà từ <c>4</c> trở lên, hoặc của ô tháng mà từ <c>2</c>
        /// trở lên, thì tự thêm <c>0</c> đằng trước rồi sang ô kế tiếp luôn — không ngày nào
        /// bắt đầu bằng 4–9 và không tháng nào bắt đầu bằng 2–9, nên đoán được chắc chắn.
        /// Ngược lại (1–3 ở ô ngày, 1 ở ô tháng) phải đợi phím sau mới biết, vì còn ngày
        /// 10–31 và tháng 10–12; muốn chốt sớm thì gõ thẳng <c>"01"</c>.</para>
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

        /// <summary>
        /// Lấy hai chữ số cho một ô của ngày tháng. Chữ số đầu từ <paramref name="padFrom"/>
        /// trở lên thì không thể là hàng chục, tự thêm <c>0</c> và coi như xong ô. Trả
        /// <c>false</c> khi mới có một chữ số còn mập mờ — khi đó <paramref name="part"/> là
        /// phần đang gõ dở.
        /// </summary>
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
