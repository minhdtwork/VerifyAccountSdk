using System;
using NUnit.Framework;

namespace OnDi.VerifyAccount.Tests
{
    public class VerifyValidatorTests
    {
        const string VnPhone = @"^(0|\+84)(3|5|7|8|9)\d{8}$";
        static readonly DateTime Today = new DateTime(2026, 9, 18);

        [TestCase("0912345678", true)]
        [TestCase("+84912345678", true)]
        [TestCase("0312345678", true)]
        [TestCase("0212345678", false)]  // đầu số cố định
        [TestCase("091234567", false)]   // thiếu một chữ số
        [TestCase("09123456789", false)] // thừa một chữ số
        [TestCase("abcdefghij", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void Phone(string input, bool expected)
        {
            Assert.AreEqual(expected, VerifyValidator.IsPhoneValid(input, VnPhone));
        }

        [Test]
        public void Phone_EmptyPatternAcceptsAnyNonEmpty()
        {
            Assert.IsTrue(VerifyValidator.IsPhoneValid("bất kỳ", ""));
            Assert.IsFalse(VerifyValidator.IsPhoneValid("   ", ""));
        }

        [Test]
        public void Phone_BrokenPatternDoesNotThrow()
        {
            Assert.IsFalse(VerifyValidator.IsPhoneValid("0912345678", "([unclosed"));
        }

        [TestCase("123456", 6, true)]
        [TestCase("12345", 6, false)]
        [TestCase("1234567", 6, false)]
        [TestCase("12a456", 6, false)]
        [TestCase("", 6, false)]
        [TestCase(null, 6, false)]
        public void Otp(string input, int length, bool expected)
        {
            Assert.AreEqual(expected, VerifyValidator.IsOtpValid(input, length));
        }

        [TestCase("Nguyễn Văn A", true)]
        [TestCase("Lê", true)]
        [TestCase("A", false)]
        [TestCase("   ", false)]
        [TestCase(null, false)]
        public void Name(string input, bool expected)
        {
            Assert.AreEqual(expected, VerifyValidator.IsNameValid(input));
        }

        [Test]
        public void Name_RejectsOverLimit()
        {
            Assert.IsFalse(VerifyValidator.IsNameValid(new string('a', VerifyValidator.MaxNameLength + 1)));
            Assert.IsTrue(VerifyValidator.IsNameValid(new string('a', VerifyValidator.MaxNameLength)));
        }

        [Test]
        public void BirthDate_ParsesDemoValue()
        {
            Assert.IsTrue(VerifyValidator.TryParseBirthDate("11/03/1991", 0, Today, out var date));
            Assert.AreEqual(new DateTime(1991, 3, 11), date);
        }

        [TestCase("1991-03-11")]
        [TestCase("03/11/1991/")]
        [TestCase("32/01/1991")]
        [TestCase("11/13/1991")]
        [TestCase("11/03/91")]
        [TestCase("")]
        [TestCase(null)]
        public void BirthDate_RejectsBadFormat(string input)
        {
            Assert.IsFalse(VerifyValidator.TryParseBirthDate(input, 0, Today, out _));
        }

        [Test]
        public void BirthDate_RejectsFuture()
        {
            Assert.IsFalse(VerifyValidator.TryParseBirthDate("19/09/2026", 0, Today, out _));
            Assert.IsTrue(VerifyValidator.TryParseBirthDate("18/09/2026", 0, Today, out _));
        }

        [Test]
        public void BirthDate_EnforcesMinAge()
        {
            // Sinh nhật đúng hôm nay: vừa tròn 18.
            Assert.IsTrue(VerifyValidator.TryParseBirthDate("18/09/2008", 18, Today, out _));
            // Sinh nhật ngày mai: còn thiếu một ngày.
            Assert.IsFalse(VerifyValidator.TryParseBirthDate("19/09/2008", 18, Today, out _));
            // minAge = 0 thì không chặn.
            Assert.IsTrue(VerifyValidator.TryParseBirthDate("19/09/2008", 0, Today, out _));
        }

        [TestCase("", "")]
        [TestCase("1", "1")]
        [TestCase("11", "11")]
        [TestCase("113", "11/3")]
        [TestCase("1103", "11/03")]
        [TestCase("110319", "11/03/19")]
        [TestCase("11031991", "11/03/1991")]
        [TestCase("11/03/1991", "11/03/1991")]
        [TestCase("11-03-1991", "11/03/1991")]
        [TestCase("1103199144", "11/03/1991")] // cắt phần thừa
        [TestCase("abc", "")]
        public void MaskDate(string input, string expected)
        {
            Assert.AreEqual(expected, VerifyValidator.MaskDate(input));
        }

        [TestCase(180f, "3:00")]
        [TestCase(179.4f, "3:00")]
        [TestCase(61f, "1:01")]
        [TestCase(59.2f, "1:00")]
        [TestCase(9f, "0:09")]
        [TestCase(0f, "0:00")]
        [TestCase(-5f, "0:00")]
        public void Countdown(float secondsLeft, string expected)
        {
            Assert.AreEqual(expected, VerifyValidator.FormatCountdown(secondsLeft));
        }
    }
}
