# OnDi Verify Account

Plugin Unity để xác thực thông tin người chơi (họ tên, số điện thoại + OTP, ngày sinh,
đồng ý điều khoản) kèm badge **18+** nổi neo vào viền màn hình.

- Unity **2022.3** trở lên, chữ chạy bằng **TextMeshPro**.
- Chạy trên **Android và iOS** bằng cùng một đường code — **không có native plugin nào**.
- Màn dọc và màn ngang đều đúng, xoay giữa chừng cũng đúng.
- **Không tự sinh gì cả**: không có GameObject nào tồn tại cho tới khi game gọi API.
- Bộ đếm thời gian chơi trong ngày, vượt 180 phút thì bắn callback cảnh báo một lần.

📖 **Tra cứu API đầy đủ: [API.md](API.md)**

---

## 1. Cài đặt

**Cách A — Git URL** (Window > Package Manager > `+` > Add package from git URL):

```
https://github.com/minhdtwork/VerifyAccountSdk.git?path=Packages/com.ondigames.verifyaccount
```

**Cách B — copy tay**: chép thư mục `Packages/com.ondigames.verifyaccount` vào thư mục
`Packages` của project.

Sau khi cài, nếu project chưa dùng TextMeshPro bao giờ thì vào **Window > TextMeshPro >
Import TMP Essential Resources** một lần. Thiếu bước này TMP không có font mặc định và
chữ trong SDK sẽ không hiện. (Menu **Tools > OnDi Verify > Rebuild UI Prefabs** cũng tự
import hộ nếu thiếu.)

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
| `uiFont`, `uiFontMaterial` | **Font TextMeshPro cho toàn bộ chữ của SDK** — xem mục 3 |
| `badgeSprite`, `badgeTooltipText`, `tooltipAutoHideSeconds` | Badge 18+ |
| `badgeIdleAlpha`, `badgeFadeSeconds` | Độ mờ của badge lúc nằm yên |
| `autoStartPlaytime`, `dailyPlayLimitMinutes`, `showBadgeTooltipOnDailyLimit` | Bộ đếm thời gian chơi |

## 3. Font riêng cho từng project

Prefab đi kèm SDK dùng font mặc định của TextMeshPro (LiberationSans SDF). Font này đủ
dấu tiếng Việt nên form hiện đúng ngay khi mới cài, nhưng nó là font hệ thống chung —
mỗi project thay bằng font của mình:

1. Tạo TMP Font Asset từ file `.ttf` của game: **Window > TextMeshPro > Font Asset Creator**.
   Nhớ để **Atlas Population Mode = Dynamic**, hoặc nếu dùng atlas tĩnh thì **Character Set**
   phải có đủ khối tiếng Việt (`Latin Extended Additional`) — thiếu là chữ ra ô vuông.
2. Kéo font asset đó vào ô `uiFont` trong `VerifyAccountSettings`. Muốn kèm viền/đổ bóng
   thì gán thêm material preset vào `uiFontMaterial`.

SDK tự dán font này xuống **mọi** `TMP_Text` của panel và badge ngay lúc sinh prefab, nên
không phải sửa prefab. Đổi giữa chừng (đa ngôn ngữ, remote config) thì gọi:

```csharp
VerifyAccountSdk.SetUiFont(myFontAsset);            // null là quay lại font trong Settings
VerifyAccountSdk.SetUiFont(myFontAsset, myOutline); // kèm material preset
```

## 4. Dùng

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

        // Cảnh báo chơi quá 180 phút/ngày — phát đúng một lần mỗi ngày.
        VerifyAccountSdk.Playtime.DailyLimitReached += total =>
            Debug.Log($"đã chơi {total.TotalMinutes:0} phút hôm nay");

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

## 5. API

**Tài liệu tra cứu đầy đủ: [API.md](API.md)** — từng hook, từng sự kiện, từng trường trong
Settings, kèm ví dụ và bảng lỗi thường gặp.

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
| `SetUiFont(font, material?)` | Đổi font TextMeshPro của cả SDK lúc chạy |
| `FloatButton.Show() / Hide() / ResetPosition() / IsVisible` | Badge 18+ |
| `Playtime.*` | Bộ đếm thời gian chơi — xem mục 8 |

Hook chưa gán thì SDK log warning và coi như thất bại — không ném exception làm kẹt game.
Hook ném exception cũng vậy.

## 6. Lưu trữ

SDK chỉ ghi `PlayerPrefs` cờ đã xác thực + mốc thời gian, vị trí badge, và bộ đếm thời
gian chơi của ngày hôm nay. **Họ tên, số điện thoại, ngày sinh không được lưu trên thiết
bị** — chúng chỉ đi qua `OnSubmitProfile`.

## 7. Sửa giao diện

`Runtime/Resources/OnDiVerify/VerifyPanel.prefab` và `FloatBadge.prefab` là prefab thật,
mở ra sửa layout, màu, chữ như bình thường.

Menu **Tools > OnDi Verify > Rebuild UI Prefabs** dựng lại cả hai từ code — chỉ dùng khi
muốn vứt hết sửa đổi và làm lại từ đầu.

Icon 18+, dấu tick và bong bóng tooltip là ảnh vẽ tạm theo bản demo. Thay icon badge bằng
`badgeSprite` trong Settings, hoặc thay thẳng file trong `Resources/OnDiVerify/Sprites`.

Bong bóng của badge chia hai cột: cột "18+" cố định nằm trong prefab, cột chữ lấy từ
`badgeTooltipText`. Chuỗi mặc định có sẵn ký tự xuống dòng để ngắt câu đúng chỗ như bản
thiết kế — tự đặt chuỗi khác thì tự chọn chỗ xuống dòng, bong bóng cao theo số dòng.

Form có sẵn `ScrollRect` với thanh cuộn dọc để ở chế độ *auto hide*: nội dung vừa khung
thì không thấy thanh nào và form rộng nguyên, chỉ khi bị co — màn ngang, máy màn ngắn,
chữ xuống dòng nhiều — thanh cuộn mới hiện ra và viewport hẹp lại nhường chỗ cho nó.
`PanelAutoHeight` trên node `Panel` quyết định form cao tối đa bao nhiêu phần màn hình
(`maxScreenRatio`, mặc định `0.92`).

## 8. Bộ đếm thời gian chơi

Cộng dồn **thời gian thực** người chơi ở trong game theo từng ngày lịch của thiết bị.
Không bị `Time.timeScale` ảnh hưởng, không tính lúc app chạy nền, và số giây nằm trong
`PlayerPrefs` nên thoát game mở lại vẫn cộng tiếp trong cùng ngày. **Qua ngày mới bộ đếm
và cờ đã cảnh báo cùng về 0.**

Bộ đếm **tự chạy ngay khi game khởi động**, nên chỉ cần gắn callback:

```csharp
VerifyAccountSdk.Playtime.DailyLimitReached += total =>
    MyUi.ShowWarning($"Bạn đã chơi {total.TotalMinutes:0} phút hôm nay.");
```

Đây là **ngoại lệ duy nhất** của quy tắc "SDK không tự sinh gì" — panel và badge vẫn chỉ
xuất hiện khi game gọi. Lý do: mốc cảnh báo phải tính từ lúc mở game, chờ game gọi `Start()`
thì phần thời gian trước đó mất trắng. Tắt `autoStartPlaytime` trong Settings nếu muốn tự
chọn thời điểm bắt đầu.

| Thành viên | Mô tả |
|---|---|
| `DailyLimitReached` | Sự kiện `Action<TimeSpan>`, phát **đúng một lần mỗi ngày** khi vượt mốc |
| `Start()` / `Stop()` / `IsRunning` | Bật, tạm dừng, kiểm tra bộ đếm. `Start()` thừa nếu `autoStartPlaytime` đang bật |
| `Today` | Tổng thời gian đã chơi hôm nay, đọc được cả khi chưa `Start()` |
| `RemainingToday` | Còn bao lâu nữa thì chạm mốc |
| `LimitReachedToday` | Hôm nay đã cảnh báo hay chưa |
| `ResetToday()` | Xoá bộ đếm của hôm nay, chủ yếu để test |

Mốc là `dailyPlayLimitMinutes` trong Settings, mặc định **180 phút**; để `0` là tắt hẳn
cảnh báo. Chạm mốc mà badge 18+ đang hiện thì SDK bật luôn bong bóng cảnh báo của badge —
tắt `showBadgeTooltipOnDailyLimit` nếu game muốn tự dựng popup trong callback.

Callback ném exception cũng không làm chết bộ đếm, SDK log lại rồi chạy tiếp.

PlayerPrefs chỉ được ghi ở những mốc có thật — vào nền, thoát game, `Stop()`, sang ngày mới,
chạm mốc cảnh báo — chứ không ghi định kỳ. Đổi lại, app bị giết mà không kịp gọi
`OnApplicationPause` (thường chỉ khi crash) thì mất phần chưa lưu của phiên đó.

## 9. Giới hạn đã biết

- Trang chính sách mở bằng trình duyệt ngoài (`Application.OpenURL`), không có webview trong game.
- Ngày sinh nhập tay `dd/MM/yyyy`, không có date picker của hệ điều hành.
- Chuỗi tiếng Việt nằm thẳng trong prefab, chưa có hệ đa ngôn ngữ.
- Font game tự tạo mà thiếu khối `Latin Extended Additional` thì dấu tiếng Việt ra ô vuông (mục 3).
- Bộ đếm thời gian chơi đi theo đồng hồ thiết bị; người chơi vặn ngày máy thì đếm lại từ đầu.
- Chạm ra ngoài để tắt bong bóng cần Input Manager cũ. Project chỉ bật Input System mới
  thì bong bóng vẫn tự tắt theo `tooltipAutoHideSeconds`.
