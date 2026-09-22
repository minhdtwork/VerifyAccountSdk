# OnDi Verify Account SDK

Plugin Unity để xác thực thông tin người chơi (họ tên, số điện thoại + OTP, ngày sinh,
đồng ý điều khoản) kèm badge **18+** nổi neo vào viền màn hình và bộ đếm thời gian chơi
trong ngày.

- Unity **2022.3** trở lên, chữ chạy bằng **TextMeshPro**.
- **Android và iOS** cùng một đường code — không có native plugin nào.
- Màn dọc, màn ngang, xoay giữa chừng đều đúng; badge luôn nằm trong `Screen.safeArea`.
- **Không tự sinh gì cả**: không GameObject nào tồn tại cho tới khi game gọi API.

## Tài liệu

| Đọc gì | Ở đâu |
|---|---|
| Cài đặt, cấu hình, hướng dẫn dùng | [Packages/com.ondigames.verifyaccount/README.md](Packages/com.ondigames.verifyaccount/README.md) |
| Tra cứu API đầy đủ | [Packages/com.ondigames.verifyaccount/API.md](Packages/com.ondigames.verifyaccount/API.md) |

## Cài vào project của bạn

Window > Package Manager > `+` > **Add package from git URL**:

```
https://github.com/minhdtwork/VerifyAccountSdk.git?path=Packages/com.ondigames.verifyaccount
```

Hoặc chép thẳng thư mục `Packages/com.ondigames.verifyaccount` vào thư mục `Packages` của
project.

## Dùng nhanh

```csharp
using OnDi.VerifyAccount;

// Cắm backend của bạn vào ba hook, mỗi hook trả Task<SdkResult>.
VerifyAccountSdk.OnSendOtp       = req => MyApi.SendOtp(req.PhoneNumber);
VerifyAccountSdk.OnVerifyOtp     = req => MyApi.VerifyOtp(req.PhoneNumber, req.Otp);
VerifyAccountSdk.OnSubmitProfile = profile => MyApi.SaveProfile(profile);

VerifyAccountSdk.Verified += profile => Debug.Log("Xong: " + profile.PhoneNumber);

VerifyAccountSdk.Show();                 // đã xác thực rồi thì không hỏi lại
VerifyAccountSdk.FloatButton.Show();     // badge 18+
```

## Repo này có gì

```
Packages/com.ondigames.verifyaccount/   package UPM — thứ duy nhất cần copy đi
Assets/VerifyAccountDemo/DemoScene      scene demo, mở lên bấm Play là chạy
docs/                                   ghi chú thiết kế
```

Repo là một project Unity 2022.3 mở được luôn: clone về, mở bằng Unity Hub, mở
`Assets/VerifyAccountDemo/DemoScene.unity` rồi bấm Play. Backend được giả lập ngay trong
`DemoBootstrap.cs`, không cần server thật:

- mã OTP đúng là **`123456`**
- số bắt đầu bằng **`0999`** thì gửi OTP thất bại, để thử đường lỗi
