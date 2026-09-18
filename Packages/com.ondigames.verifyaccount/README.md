# OnDi Verify Account

Plugin Unity để xác thực thông tin người chơi (họ tên, số điện thoại + OTP, ngày sinh,
đồng ý điều khoản) kèm badge **18+** nổi neo vào viền màn hình.

- Unity **2022.3** trở lên.
- Chạy trên **Android và iOS** bằng cùng một đường code — **không có native plugin nào**.
- Màn dọc và màn ngang đều đúng, xoay giữa chừng cũng đúng.
- **Không tự sinh gì cả**: không có GameObject nào tồn tại cho tới khi game gọi API.

---

## 1. Cài đặt

**Cách A — Git URL** (Window > Package Manager > `+` > Add package from git URL):

```
https://github.com/minhdtwork/VerifyAccountSdk.git?path=Packages/com.ondigames.verifyaccount
```

**Cách B — copy tay**: chép thư mục `Packages/com.ondigames.verifyaccount` vào thư mục
`Packages` của project.

## 2. Cấu hình

Create > **OnDi** > **Verify Account Settings**, đặt file vào một thư mục `Resources`
bất kỳ và **giữ nguyên tên `VerifyAccountSettings`**.

Thiếu file này SDK vẫn chạy bằng giá trị mặc định, chỉ log một dòng nhắc.

Các mục hay dùng:

| Trường | Ý nghĩa |
|---|---|
| `termsUrl`, `privacyUrl` | Link trang chính sách, mở bằng trình duyệt hệ thống |
| `sortingLayerName`, `sortingOrder` | Vị trí canvas của SDK so với UI game (mặc định order `32000`) |
| `phoneRegex` | Quy tắc số điện thoại, mặc định là số di động Việt Nam |
| `otpLength`, `otpTtlSeconds`, `resendCooldownSeconds` | Độ dài mã và thời gian OTP |
| `minAge` | Tuổi tối thiểu theo ngày sinh, `0` là không kiểm tra |
| `showSkipButton` | Hiện nút "Bỏ qua" |
| `badgeSprite`, `badgeTooltipText`, `tooltipAutoHideSeconds` | Badge 18+ |

## 3. Dùng

```csharp
using System.Threading.Tasks;
using OnDi.VerifyAccount;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    void Start()
    {
        // Cắm backend của bạn vào. SDK không biết gì về domain, header hay JSON của bạn.
        VerifyAccountSdk.OnSendOtp       = req  => MyBackend.SendOtpAsync(req);
        VerifyAccountSdk.OnVerifyOtp     = req  => MyBackend.VerifyOtpAsync(req);
        VerifyAccountSdk.OnSubmitProfile = prof => MyBackend.SaveProfileAsync(prof);

        VerifyAccountSdk.Verified += prof => Debug.Log("xong: " + prof.PhoneNumber);
        VerifyAccountSdk.Skipped  += ()   => Debug.Log("người chơi bỏ qua");

        VerifyAccountSdk.Show();                 // bỏ qua nếu đã xác thực rồi
        VerifyAccountSdk.FloatButton.Show();     // badge 18+
    }
}
```

Một hàm backend trả về `Task<SdkResult>`:

```csharp
static async Task<SdkResult> SendOtpAsync(SendOtpRequest req)
{
    using var www = UnityWebRequest.Post("https://api.example.com/otp/send", form);
    await www.SendWebRequest();
    return www.result == UnityWebRequest.Result.Success
        ? SdkResult.Success()
        : SdkResult.Fail("Không gửi được mã OTP.");   // chuỗi này hiện thẳng lên form
}
```

Còn dùng coroutine thuần? Bọc lại bằng `TaskCompletionSource<SdkResult>`.

## 4. API

| Thành viên | Mô tả |
|---|---|
| `OnSendOtp` | Gọi khi bấm "Gửi OTP" / "Gửi lại" |
| `OnVerifyOtp` | Gọi khi bấm "Hoàn thành", trước `OnSubmitProfile` |
| `OnSubmitProfile` | Gọi sau khi OTP đúng — chỗ game lưu thông tin lên server của mình |
| `Verified`, `Skipped`, `Closed` | Sự kiện |
| `IsVerified`, `VerifiedAtUtc`, `ClearVerified()` | Cờ đã xác thực trên thiết bị |
| `Show()` | Mở form, không làm gì nếu đã xác thực |
| `ShowForced()` | Mở form kể cả khi đã xác thực |
| `Hide()`, `IsPanelOpen` | Đóng form / kiểm tra |
| `SetSkipButtonVisible(bool?)` | Bật tắt nút "Bỏ qua" lúc chạy, `null` là quay về Settings |
| `FloatButton.Show() / Hide() / ResetPosition() / IsVisible` | Badge 18+ |

Hook chưa gán thì SDK log warning và coi như thất bại — không ném exception làm kẹt game.
Hook ném exception cũng vậy.

## 5. Lưu trữ

SDK chỉ ghi `PlayerPrefs` cờ đã xác thực + mốc thời gian, và vị trí badge. **Họ tên, số
điện thoại, ngày sinh không được lưu trên thiết bị** — chúng chỉ đi qua `OnSubmitProfile`.

## 6. Sửa giao diện

`Runtime/Resources/OnDiVerify/VerifyPanel.prefab` và `FloatBadge.prefab` là prefab thật,
mở ra sửa layout, màu, chữ như bình thường.

Menu **Tools > OnDi Verify > Rebuild UI Prefabs** dựng lại cả hai từ code — chỉ dùng khi
muốn vứt hết sửa đổi và làm lại từ đầu.

Icon 18+, dấu tick và bong bóng tooltip là ảnh vẽ tạm theo bản demo. Thay icon badge bằng
`badgeSprite` trong Settings, hoặc thay thẳng file trong `Resources/OnDiVerify/Sprites`.

## 7. Giới hạn đã biết

- Trang chính sách mở bằng trình duyệt ngoài (`Application.OpenURL`), không có webview trong game.
- Ngày sinh nhập tay `dd/MM/yyyy`, không có date picker của hệ điều hành.
- Chuỗi tiếng Việt nằm thẳng trong prefab, chưa có hệ đa ngôn ngữ.
- Chạm ra ngoài để tắt bong bóng cần Input Manager cũ. Project chỉ bật Input System mới
  thì bong bóng vẫn tự tắt theo `tooltipAutoHideSeconds`.
