# Verify Account SDK — Design

Ngày: 2026-09-18
Trạng thái: đã duyệt, sẵn sàng implement

## 1. Mục tiêu

Một plugin Unity dùng chung cho game mobile, cho phép game yêu cầu người chơi xác thực
thông tin cá nhân (họ tên, số điện thoại + OTP, ngày sinh, đồng ý điều khoản), kèm một
badge "18+" nổi neo vào viền màn hình theo quy định hiển thị cảnh báo thời lượng chơi.

Tích hợp phải ở mức: thêm package → gán vài callback → gọi một hàm.

## 2. Ràng buộc

- Unity **2022.3 trở lên** (dev trên 2022.3.62f2). Không dùng API chỉ có ở Unity 6.
- Chạy trên **Android và iOS**. Không viết native code cho nền tảng nào — toàn bộ là
  uGUI + networking phía game, nên hai nền chạy cùng một đường code.
- Chạy đúng ở cả màn dọc và màn ngang, kể cả khi xoay giữa chừng.
- UI luôn nằm trên canvas của game; layer và sorting order phải chỉnh được.
- **Không tự sinh gì cả.** Không `RuntimeInitializeOnLoadMethod`, không bootstrap ngầm.
  Game không gọi API thì SDK không có GameObject nào trong scene.

## 3. Không làm (YAGNI)

Các thứ sau bị loại khỏi phạm vi vì yêu cầu hiện tại không cần:

- Plugin native Android (.aar) / iOS (.framework) — không có tính năng nào cần tới.
- Webview trong game để mở trang policy — dùng `Application.OpenURL` là đủ.
- Date picker native của OS — nhập tay `dd/MM/yyyy` có mask.
- Đọc SMS tự động để điền OTP.
- Hệ thống đa ngôn ngữ — chuỗi tiếng Việt nằm trong prefab, sửa trực tiếp.
- Interface / factory / DI — mỗi thành phần chỉ có một hiện thực.
- Editor Window riêng — `[CreateAssetMenu]` là đủ để tạo file cấu hình.

## 4. Kiến trúc

### 4.1 Bố cục package

```
Packages/com.ondigames.verifyaccount/
  package.json
  README.md
  Runtime/
    OnDi.VerifyAccount.asmdef
    VerifyAccountSdk.cs        facade static: hooks, events, Show/Hide
    VerifyAccountSettings.cs   ScriptableObject cấu hình
    VerifyValidator.cs         hàm static thuần: kiểm tra phone / ngày sinh / tuổi
    SdkRoot.cs                 canvas dùng chung, tạo lazy, theo dõi xoay màn
    VerifyPanel.cs             logic form
    FloatBadge.cs              kéo, snap viền, tooltip
    Resources/OnDiVerify/
      VerifyPanel.prefab
      FloatBadge.prefab
      Sprites/*.png
  Editor/
    OnDi.VerifyAccount.Editor.asmdef
    UiPrefabBuilder.cs         menu "Tools/OnDi Verify/Rebuild UI Prefabs"
  Tests/Editor/
    OnDi.VerifyAccount.Tests.asmdef
    VerifyValidatorTests.cs
  Samples~/Demo/
    DemoScene.unity
    DemoBootstrap.cs           server giả, OTP hợp lệ là 123456
```

Package sống trong `Packages/` của repo này (embedded package), game khác dùng được
qua Git URL hoặc copy thư mục.

### 4.2 Vòng đời

Không có gì được tạo cho tới lần gọi API đầu tiên.

`SdkRoot.Ensure()` là điểm vào duy nhất tạo hạ tầng: sinh một GameObject
`[OnDiVerifySdk]` mang `Canvas` + `CanvasScaler` + `GraphicRaycaster`, đánh dấu
`DontDestroyOnLoad`, rồi trả về. Mọi lần gọi sau dùng lại instance đó.

- `VerifyAccountSdk.Show()` → `SdkRoot.Ensure()` → instantiate `VerifyPanel` nếu chưa có.
- `VerifyAccountSdk.FloatButton.Show()` → `SdkRoot.Ensure()` → instantiate `FloatBadge` nếu chưa có.
- `Hide()` của cả hai chỉ `SetActive(false)`, không hủy, để lần gọi sau không phải dựng lại.

Vì canvas `DontDestroyOnLoad`, gọi `FloatButton.Show()` một lần là badge sống qua các
scene cho tới khi `Hide()`.

### 4.3 Canvas, layer, sorting

`Canvas` ở chế độ `ScreenSpaceOverlay`. `sortingLayerName` và `sortingOrder` lấy từ
Settings (mặc định layer `Default`, order `32000`) nên luôn vẽ trên UI của game và
vẫn chỉnh được nếu game có canvas order cao hơn.

`CanvasScaler` ở `ScaleWithScreenSize`, reference 1080×1920.

Badge là **child cuối cùng** của canvas → luôn nằm trên panel xác thực. Không cần
canvas thứ hai.

### 4.4 Xử lý xoay màn

`SdkRoot` theo dõi `Screen.width`/`Screen.height` mỗi frame (so sánh hai số nguyên, rẻ
hơn đăng ký sự kiện xoay của từng nền tảng). Khi tỉ lệ đổi:

- `CanvasScaler.matchWidthOrHeight` lật `0` (màn dọc, khớp chiều rộng) ↔ `1`
  (màn ngang, khớp chiều cao) để panel không bị tràn ra ngoài.
- Gọi lại `FloatBadge` để neo vào viền theo `Screen.safeArea` mới.

## 5. Bề mặt API

```csharp
public static class VerifyAccountSdk
{
    // Hook — game cắm hàm gọi server của mình vào.
    public static Func<SendOtpRequest,   Task<SdkResult>> OnSendOtp;
    public static Func<VerifyOtpRequest, Task<SdkResult>> OnVerifyOtp;
    public static Func<VerifiedProfile,  Task<SdkResult>> OnSubmitProfile;

    // Sự kiện
    public static event Action<VerifiedProfile> Verified;
    public static event Action Skipped;
    public static event Action Closed;

    // Trạng thái
    public static VerifyAccountSettings Settings { get; }
    public static bool IsVerified { get; }
    public static void ClearVerified();

    // Điều khiển
    public static void Show();         // no-op nếu IsVerified
    public static void ShowForced();   // luôn mở, kể cả đã verify
    public static void Hide();
    public static void SetSkipButtonVisible(bool visible);  // cho remote config

    public static class FloatButton
    {
        public static void Show();
        public static void Hide();
        public static void ResetPosition();
    }
}

public struct SdkResult
{
    public bool Ok;
    public string Message;   // lỗi hiện thẳng thành dòng đỏ dưới ô liên quan
    public static SdkResult Success();
    public static SdkResult Fail(string message);
}

public sealed class SendOtpRequest   { public string FullName, PhoneNumber; }
public sealed class VerifyOtpRequest { public string PhoneNumber, Otp; }
public sealed class VerifiedProfile  { public string FullName, PhoneNumber; public DateTime BirthDate; }
```

SDK không biết gì về domain, header xác thực hay định dạng JSON của server — game tự lo.

Hook chưa gán → SDK log warning và coi như `SdkResult.Fail`, không ném exception làm
crash game.

## 6. Cấu hình

`VerifyAccountSettings : ScriptableObject`, `[CreateAssetMenu]`, nạp bằng
`Resources.Load<VerifyAccountSettings>("VerifyAccountSettings")`. Thiếu file thì dùng
giá trị mặc định và log một dòng hướng dẫn, không chặn game chạy.

| Trường | Mặc định | Ý nghĩa |
|---|---|---|
| `termsUrl` | `""` | Link "Điều khoản sử dụng" |
| `privacyUrl` | `""` | Link "Chính sách bảo vệ và xử lý dữ liệu cá nhân" |
| `sortingLayerName` | `Default` | Sorting layer của canvas SDK |
| `sortingOrder` | `32000` | Sorting order của canvas SDK |
| `phoneRegex` | số di động VN 10 số | Quy tắc số điện thoại |
| `otpLength` | `6` | Độ dài mã OTP |
| `otpTtlSeconds` | `180` | Đồng hồ đếm ngược "OTP hết hạn sau" |
| `resendCooldownSeconds` | `60` | Khoá nút "Gửi lại" |
| `minAge` | `0` | Tuổi tối thiểu, `0` = không kiểm tra |
| `showSkipButton` | `true` | Hiện nút "Bỏ qua" |
| `badgeSprite` | 18+ | Icon badge nổi |
| `badgeTooltipText` | "Chơi quá 180 phút một ngày sẽ ảnh hưởng xấu đến sức khỏe" | Nội dung bong bóng |
| `tooltipAutoHideSeconds` | `4` | Tự tắt bong bóng |

## 7. Luồng form

Trình tự theo bản demo:

1. **Họ và tên** — bắt buộc, cắt khoảng trắng thừa, tối đa 50 ký tự.
2. **Số điện thoại** — khớp `phoneRegex` thì nút **"Gửi OTP"** mới bật.
3. **Gửi OTP** — gọi `OnSendOtp`. Thành công: mở ô OTP, chạy đồng hồ `otpTtlSeconds`
   ("OTP hết hạn sau 3:00"), khoá **"Gửi lại"** trong `resendCooldownSeconds`.
   Thất bại: hiện `SdkResult.Message`.
4. **Mã OTP** — đúng `otpLength` chữ số.
5. **Ngày sinh** — `dd/MM/yyyy`, tự chèn dấu `/` khi gõ, `DateTime.TryParseExact`,
   chặn ngày trong tương lai, kiểm tra `minAge` nếu bật.
6. **Hai checkbox điều khoản** — cả hai phải tick. Phần chữ cam là nút, bấm vào gọi
   `Application.OpenURL` tới `termsUrl` / `privacyUrl`.
7. **"Hoàn thành"** — sprite `btn_xam` khi chưa đủ điều kiện, `btn_cam` khi đủ.
   Bấm → `OnVerifyOtp` → nếu `Ok` thì `OnSubmitProfile` → nếu `Ok` thì lưu cờ,
   phát `Verified`, đóng panel.
8. **"Bỏ qua"** — đóng panel, phát `Skipped`. Ẩn được qua `SetSkipButtonVisible(false)`.

Trong lúc chờ mạng, nút đang chờ bị khoá để chống bấm đúp. Mọi lỗi trả về hiện ở dòng
text đỏ ngay dưới ô liên quan, không dùng popup.

## 8. Float badge

- Kéo bằng `IBeginDragHandler` / `IDragHandler` / `IEndDragHandler`.
- Thả tay → snap về viền trái hoặc phải gần nhất, kẹp Y trong `Screen.safeArea`.
- Vị trí lưu **dạng chuẩn hoá** (`edge` là trái/phải, `yRatio` là tỉ lệ 0..1) vào
  `PlayerPrefs`, nên xoay màn hay đổi thiết bị vẫn về đúng chỗ tương đối.
- Tap (di chuyển dưới ngưỡng kéo) → bật bong bóng. Bong bóng tự lật sang trái nếu badge
  đang ở viền phải. Tự tắt sau `tooltipAutoHideSeconds` hoặc khi chạm chỗ khác.

## 9. Lưu trữ

Chỉ ghi `PlayerPrefs` hai khoá: cờ đã xác thực và mốc thời gian. Họ tên, số điện thoại,
ngày sinh **không** được lưu trên thiết bị — đẩy hết qua `OnSubmitProfile` rồi quên.

## 10. Prefab

Prefab được sinh một lần bằng `UiPrefabBuilder` (menu Editor), sau đó là asset thật để
designer sửa layout/màu trực tiếp trong Inspector. Viết YAML prefab ~20 node bằng tay là
nguồn lỗi không đáng.

> `ponytail:` builder và prefab trùng thông tin. Prefab là nguồn sự thật; builder chỉ
> dùng khi cần dựng lại từ đầu. Nếu prefab và builder lệch nhau thì bỏ builder đi.

Asset gốc lấy từ bản demo: `BG.png` (nền panel, 9-slice), `btn_cam.png`, `btn_xam.png`,
`f_tick.png` (khung checkbox). Icon 18+, dấu tick và bong bóng tooltip chưa có trong bộ
asset nên được vẽ tạm theo demo; thay bằng sprite thật qua Settings.

## 11. Text

Dùng uGUI `Text` với font hệ thống thay vì TextMeshPro: tiếng Việt có dấu hiển thị đúng
ngay mà không phải ship kèm TMP Font Asset đã bake, và package nhẹ hơn.

## 12. Kiểm chứng

- `VerifyValidatorTests.cs` — NUnit EditMode, phủ số điện thoại, parse ngày sinh, ngày
  tương lai, tuổi tối thiểu, độ dài OTP. Đây là chỗ duy nhất có logic dễ sai thầm lặng.
- `Samples~/Demo` — scene bấm tay được với server giả (OTP hợp lệ `123456`, số
  `0999999999` trả lỗi để thử đường lỗi).
- Compile và test chạy bằng `Unity.exe -batchmode -runTests`.
