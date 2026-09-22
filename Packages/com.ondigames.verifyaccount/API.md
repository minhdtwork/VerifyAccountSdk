# API — OnDi Verify Account

Tài liệu tra cứu đầy đủ. Cần cài đặt và cấu hình thì xem [README](README.md).

Mọi thứ nằm trong namespace `OnDi.VerifyAccount`, vào bằng một lớp static duy nhất:
`VerifyAccountSdk`. Không có prefab nào phải kéo vào scene, không có component nào phải
gắn tay.

```csharp
using OnDi.VerifyAccount;
```

---

## Mục lục

1. [Luồng chạy](#1-luồng-chạy)
2. [Cắm backend: ba hook](#2-cắm-backend-ba-hook)
3. [Kiểu dữ liệu](#3-kiểu-dữ-liệu)
4. [Mở và đóng form](#4-mở-và-đóng-form)
5. [Sự kiện](#5-sự-kiện)
6. [Cờ đã xác thực](#6-cờ-đã-xác-thực)
7. [Badge 18+](#7-badge-18)
8. [Bộ đếm thời gian chơi](#8-bộ-đếm-thời-gian-chơi)
9. [Đổi font lúc chạy](#9-đổi-font-lúc-chạy)
10. [Nút "Bỏ qua"](#10-nút-bỏ-qua)
11. [Luật kiểm tra dữ liệu](#11-luật-kiểm-tra-dữ-liệu)
12. [VerifyAccountSettings](#12-verifyaccountsettings)
13. [VerifyValidator](#13-verifyvalidator)
14. [PanelAutoHeight](#14-panelautoheight)
15. [PlayerPrefs mà SDK ghi](#15-playerprefs-mà-sdk-ghi)
16. [Bảng tra nhanh](#16-bảng-tra-nhanh)
17. [Lỗi thường gặp](#17-lỗi-thường-gặp)

---

## 1. Luồng chạy

```
        game gọi VerifyAccountSdk.Show()
                    │
                    ▼
        ┌───────────────────────────────────┐
        │  form: họ tên + ngày sinh + SĐT   │
        └───────────┬───────────────────────┘
                    │ bấm "Gửi OTP"
                    ▼
            OnSendOtp(SendOtpRequest)        ──fail──▶ Message hiện dưới ô SĐT
                    │ ok
                    ▼
        ┌───────────────────────────────────────────┐
        │  nhập OTP + tick 2 điều khoản             │
        │  (đồng hồ otpTtlSeconds đang chạy)        │
        └───────────┬───────────────────────────────┘
                    │ bấm "Hoàn thành"
                    ▼
            OnVerifyOtp(VerifyOtpRequest)    ──fail──▶ Message hiện dưới ô OTP
                    │ ok
                    ▼
            OnSubmitProfile(VerifiedProfile) ──fail──▶ Message hiện cuối form
                    │ ok
                    ▼
        PlayerPrefs ghi cờ đã xác thực
                    │
                    ▼
            Verified(profile) ──▶ form đóng ──▶ Closed()
```

Nhánh "Bỏ qua": phát `Skipped()` rồi đóng form, sau đó `Closed()`. Không ghi cờ gì cả.

**SDK không tự sinh gì cả.** Canvas, panel và badge chỉ ra đời ở lần gọi `Show()`,
`ShowForced()` hoặc `FloatButton.Show()` đầu tiên. Ngoại lệ duy nhất là
[bộ đếm thời gian chơi](#8-bộ-đếm-thời-gian-chơi) — nó tự chạy từ lúc khởi động, và tắt
được bằng một cờ trong Settings.

---

## 2. Cắm backend: ba hook

SDK không biết gì về domain, header hay JSON của bạn. Nó chỉ gọi ba `Func` mà bạn gán,
rồi đợi `Task<SdkResult>`.

```csharp
public static Func<SendOtpRequest,   Task<SdkResult>> OnSendOtp;
public static Func<VerifyOtpRequest, Task<SdkResult>> OnVerifyOtp;
public static Func<VerifiedProfile,  Task<SdkResult>> OnSubmitProfile;
```

| Hook | Gọi khi | Gán ở đâu thì hợp lý |
|---|---|---|
| `OnSendOtp` | Bấm "Gửi OTP" hoặc "Gửi lại" | Một lần lúc game khởi động |
| `OnVerifyOtp` | Bấm "Hoàn thành", chạy trước `OnSubmitProfile` | 〃 |
| `OnSubmitProfile` | Sau khi OTP đúng — chỗ game lưu hồ sơ lên server của mình | 〃 |

Gán một lần trong `Awake`/`Start` của một object `DontDestroyOnLoad` là đủ. Đây là field
static nên gán đè lần sau sẽ thay thế lần trước, không cộng dồn như `event`.

### Ví dụ đầy đủ

```csharp
using System.Threading.Tasks;
using OnDi.VerifyAccount;
using UnityEngine;
using UnityEngine.Networking;

public sealed class VerifyBootstrap : MonoBehaviour
{
    const string Api = "https://api.example.com";

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        VerifyAccountSdk.OnSendOtp       = SendOtpAsync;
        VerifyAccountSdk.OnVerifyOtp     = VerifyOtpAsync;
        VerifyAccountSdk.OnSubmitProfile = SubmitProfileAsync;

        VerifyAccountSdk.Verified += profile =>
            Debug.Log("Đã xác thực: " + profile.PhoneNumber);

        VerifyAccountSdk.Show();
        VerifyAccountSdk.FloatButton.Show();
    }

    static async Task<SdkResult> SendOtpAsync(SendOtpRequest req)
    {
        var form = new WWWForm();
        form.AddField("name", req.FullName);
        form.AddField("phone", req.PhoneNumber);

        using var www = UnityWebRequest.Post(Api + "/otp/send", form);
        await www.SendWebRequest();

        return www.result == UnityWebRequest.Result.Success
            ? SdkResult.Success()
            : SdkResult.Fail("Không gửi được mã OTP, vui lòng thử lại.");
    }

    static async Task<SdkResult> VerifyOtpAsync(VerifyOtpRequest req)
    {
        var form = new WWWForm();
        form.AddField("phone", req.PhoneNumber);
        form.AddField("otp", req.Otp);

        using var www = UnityWebRequest.Post(Api + "/otp/verify", form);
        await www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            return SdkResult.Fail("Không kết nối được máy chủ.");

        // Server trả 200 kèm lý do khi mã sai — đẩy nguyên văn lên form.
        var body = JsonUtility.FromJson<OtpResponse>(www.downloadHandler.text);
        return body.ok ? SdkResult.Success() : SdkResult.Fail(body.message);
    }

    static async Task<SdkResult> SubmitProfileAsync(VerifiedProfile profile)
    {
        var form = new WWWForm();
        form.AddField("name", profile.FullName);
        form.AddField("phone", profile.PhoneNumber);
        form.AddField("dob", profile.BirthDate.ToString("yyyy-MM-dd"));

        using var www = UnityWebRequest.Post(Api + "/profile", form);
        await www.SendWebRequest();

        return www.result == UnityWebRequest.Result.Success
            ? SdkResult.Success()
            : SdkResult.Fail("Lưu thông tin thất bại, vui lòng thử lại.");
    }

    [System.Serializable]
    class OtpResponse { public bool ok; public string message; }
}
```

> `await www.SendWebRequest()` cần một extension awaiter cho `UnityWebRequestAsyncOperation`.
> Không muốn thêm thì bọc bằng `TaskCompletionSource` như mục dưới.

### Nếu backend của bạn chạy bằng coroutine

```csharp
static Task<SdkResult> SendOtpAsync(SendOtpRequest req)
{
    var tcs = new TaskCompletionSource<SdkResult>();
    MyRunner.Instance.StartCoroutine(SendOtpRoutine(req, tcs));
    return tcs.Task;
}

static IEnumerator SendOtpRoutine(SendOtpRequest req, TaskCompletionSource<SdkResult> tcs)
{
    using var www = UnityWebRequest.Post(Api + "/otp/send", form);
    yield return www.SendWebRequest();

    tcs.SetResult(www.result == UnityWebRequest.Result.Success
        ? SdkResult.Success()
        : SdkResult.Fail("Không gửi được mã OTP."));
}
```

### Hook chạy ở đâu

Hook được `await` từ luồng chính của Unity, nên phần code sau `await` cũng quay lại luồng
chính. Gọi API Unity trong hook là an toàn.

### Hook lỗi thì sao

SDK **không bao giờ ném exception ra ngoài** vì hook là code của game. Mọi đường lỗi đều
quy về một `SdkResult.Fail` và một dòng log:

| Tình huống | Console | Chữ hiện lên form |
|---|---|---|
| Chưa gán hook | `Chưa gán VerifyAccountSdk.<tên hook>.` | "Chức năng chưa sẵn sàng, vui lòng thử lại sau." |
| Hook trả `null` thay vì `Task` | `<tên hook> trả về null Task.` | "Có lỗi xảy ra, vui lòng thử lại." |
| Hook ném exception | Stack trace đầy đủ | "Không kết nối được máy chủ, vui lòng thử lại." |

Khi bạn trả `SdkResult.Fail(message)` mà `message` rỗng, SDK dùng câu mặc định theo bước:

| Bước | Câu mặc định |
|---|---|
| `OnSendOtp` | "Không gửi được mã OTP, vui lòng thử lại." |
| `OnVerifyOtp` | "Mã OTP không đúng." |
| `OnSubmitProfile` | "Lưu thông tin thất bại, vui lòng thử lại." |

Form không tự khoá vĩnh viễn sau lỗi — người chơi sửa rồi bấm lại được ngay.

---

## 3. Kiểu dữ liệu

### `SdkResult`

```csharp
public struct SdkResult
{
    public bool   Ok;
    public string Message;   // hiện thẳng lên form khi Ok == false

    public static SdkResult Success(string message = null);
    public static SdkResult Fail(string message);
}
```

`Message` là chuỗi **hiện thẳng cho người chơi đọc**, nên viết bằng tiếng Việt và nói được
việc cần làm tiếp. Đừng nhét mã lỗi HTTP hay stack trace vào đây.

### `SendOtpRequest`

```csharp
public sealed class SendOtpRequest
{
    public string FullName;      // đã Trim()
    public string PhoneNumber;   // đã Trim()
}
```

### `VerifyOtpRequest`

```csharp
public sealed class VerifyOtpRequest
{
    public string PhoneNumber;   // đã Trim()
    public string Otp;           // đã Trim(), đúng otpLength chữ số
}
```

### `VerifiedProfile`

```csharp
public sealed class VerifiedProfile
{
    public string   FullName;
    public string   PhoneNumber;
    public DateTime BirthDate;   // đã parse từ dd/MM/yyyy, phần giờ là 00:00
}
```

Cùng một instance `VerifiedProfile` được truyền cho `OnSubmitProfile` rồi cho sự kiện
`Verified`. Đừng sửa nó trong hook.

---

## 4. Mở và đóng form

```csharp
public static void Show();          // bỏ qua nếu IsVerified == true
public static void ShowForced();    // mở kể cả khi đã xác thực
public static void Hide();          // đóng form, KHÔNG phát Skipped
public static bool IsPanelOpen { get; }
```

`Show()` là hàm dùng hằng ngày: gọi ở đầu mỗi phiên, người đã xác thực rồi thì không bị
hỏi lại. `ShowForced()` dành cho nút "Xác thực lại" trong phần cài đặt của game, hoặc để
test.

Mỗi lần mở, form được **reset sạch**: xoá hết ô nhập, bỏ tick điều khoản, xoá dòng lỗi,
tắt đồng hồ OTP, cuộn về đầu. Không có state nào sót lại từ lần mở trước.

`Hide()` đóng form giữa chừng — dùng khi game cần chen một màn hình khác vào. Nó phát
`Closed` nhưng **không** phát `Skipped`, vì người chơi có bấm "Bỏ qua" đâu.

---

## 5. Sự kiện

```csharp
public static event Action<VerifiedProfile> Verified;
public static event Action                  Skipped;
public static event Action                  Closed;
```

| Sự kiện | Phát khi |
|---|---|
| `Verified` | `OnSubmitProfile` trả `Ok` — xác thực xong trọn vẹn. Cờ trong PlayerPrefs đã được ghi trước khi sự kiện này phát. |
| `Skipped` | Người chơi bấm "Bỏ qua" |
| `Closed` | Form đóng lại, vì bất kỳ lý do gì |

Thứ tự:

```
xác thực xong  →  Verified  →  Closed
bấm "Bỏ qua"   →  Skipped   →  Closed
game gọi Hide()          →  Closed
```

Đây là `event` static nên `+=` cộng dồn và **không** tự gỡ khi đổi scene. Đăng ký một lần
ở object `DontDestroyOnLoad`, hoặc nhớ `-=` trong `OnDestroy`:

```csharp
void OnEnable()  => VerifyAccountSdk.Verified += OnVerified;
void OnDisable() => VerifyAccountSdk.Verified -= OnVerified;
```

---

## 6. Cờ đã xác thực

```csharp
public static bool     IsVerified    { get; }   // đã xác thực trên thiết bị này chưa
public static DateTime VerifiedAtUtc { get; }   // mốc UTC, default nếu chưa xác thực
public static void     ClearVerified();          // xoá cờ để hỏi lại từ đầu
```

Cờ này là **của thiết bị**, không phải của tài khoản. Cài lại game hoặc đổi máy là mất.
Nguồn sự thật vẫn nằm ở server của bạn — `IsVerified` chỉ để khỏi hỏi lại người đã làm
xong trên chính máy này.

Người chơi đăng xuất thì gọi `ClearVerified()`:

```csharp
void OnLogout()
{
    VerifyAccountSdk.ClearVerified();
}
```

Muốn bắt xác thực lại sau một khoảng thời gian:

```csharp
if (VerifyAccountSdk.IsVerified &&
    DateTime.UtcNow - VerifyAccountSdk.VerifiedAtUtc > TimeSpan.FromDays(180))
{
    VerifyAccountSdk.ClearVerified();
}

VerifyAccountSdk.Show();
```

---

## 7. Badge 18+

```csharp
public static class FloatButton
{
    public static void Show();            // lần gọi đầu mới sinh GameObject
    public static void Hide();
    public static void ResetPosition();   // về chỗ mặc định, xoá vị trí đã lưu
    public static bool IsVisible { get; }
}
```

Lần đầu hiện, badge nằm **giữa viền trái** màn hình. Kéo thả được, chạm vào thì hiện bong
bóng cảnh báo. Thả tay ở đâu thì nó tự bám về viền gần nhất. Lúc nằm yên badge mờ bớt
(`badgeIdleAlpha`) cho đỡ che game.

Vị trí lưu dưới dạng **tỉ lệ** chứ không phải pixel, nên xoay màn hay đổi thiết bị vẫn về
đúng chỗ tương đối. Badge cũng luôn nằm trong `Screen.safeArea`, không chui xuống dưới tai
thỏ hay thanh home.

Badge luôn được vẽ **trên** form xác thực, nên mở form vẫn thấy badge.

```csharp
// Hiện badge cả phiên chơi:
VerifyAccountSdk.FloatButton.Show();

// Giấu đi trong cutscene:
VerifyAccountSdk.FloatButton.Hide();
```

Đổi icon bằng `badgeSprite` trong Settings, đổi chữ trong bong bóng bằng `badgeTooltipText`.

Số tuổi trên badge và trong bong bóng tự lấy từ `minAge` trong Settings: đặt `16` thì cả
hai chỗ đều hiện "16+". `minAge` bằng `0` (tắt kiểm tra tuổi) thì vẫn hiện "18+".

Bong bóng chia hai cột: cột số tuổi bên trái, cột chữ bên phải lấy từ
`badgeTooltipText`. Chuỗi mặc định có sẵn ký tự xuống dòng để ngắt đúng bốn dòng như bản
thiết kế — tự đặt chuỗi khác thì tự chọn chỗ xuống dòng, bong bóng cao theo số dòng. Hằng
`VerifyAccountSettings.DefaultBadgeTooltipText` giữ nguyên chuỗi mặc định đó.

---

## 8. Bộ đếm thời gian chơi

```csharp
public static class Playtime
{
    public static event Action<TimeSpan> DailyLimitReached;

    public static void     Start();
    public static void     Stop();
    public static bool     IsRunning         { get; }
    public static TimeSpan Today             { get; }
    public static TimeSpan RemainingToday    { get; }
    public static bool     LimitReachedToday { get; }
    public static void     ResetToday();
}
```

Cộng dồn **thời gian thực** người chơi ở trong game, theo từng ngày lịch của thiết bị:

- Không bị `Time.timeScale` ảnh hưởng (dùng `unscaledDeltaTime`), nên pause game vẫn đếm
  đúng và cheat tốc độ không làm lệch.
- Không tính lúc app chạy nền — `Update` ngừng chạy thì bộ đếm cũng ngừng.
- Số giây nằm trong `PlayerPrefs` nên thoát game mở lại vẫn cộng tiếp trong cùng ngày.
- **Qua ngày mới bộ đếm và cờ đã cảnh báo cùng về 0.**
- Một frame kéo dài bất thường (loading, vừa resume) bị kẹp ở 5 giây để không làm phồng
  bộ đếm.

### Tự chạy

Bộ đếm **tự chạy ngay khi game khởi động** (`RuntimeInitializeOnLoadMethod`), nên thường
chỉ cần gắn callback:

```csharp
VerifyAccountSdk.Playtime.DailyLimitReached += total =>
    MyUi.ShowWarning($"Bạn đã chơi {total.TotalMinutes:0} phút hôm nay.");
```

Đây là **ngoại lệ duy nhất** của quy tắc "SDK không tự sinh gì" — panel và badge vẫn chỉ
xuất hiện khi game gọi. Lý do: mốc cảnh báo phải tính từ lúc mở game; chờ game gọi `Start()`
thì phần thời gian trước đó mất trắng.

Muốn tự quyết định thời điểm thì tắt `autoStartPlaytime` trong Settings rồi gọi tay:

```csharp
VerifyAccountSdk.Playtime.Start();   // gọi lại khi đang chạy cũng không sao
VerifyAccountSdk.Playtime.Stop();    // tạm dừng và chốt sổ xuống PlayerPrefs
```

### `DailyLimitReached`

Phát **đúng một lần mỗi ngày**, khi tổng thời gian trong ngày vượt `dailyPlayLimitMinutes`
(mặc định 180 phút). Đặt mốc về `0` là tắt hẳn cảnh báo.

Chạm mốc mà badge 18+ đang hiện thì SDK bật luôn bong bóng cảnh báo của badge. Tắt
`showBadgeTooltipOnDailyLimit` nếu game muốn tự dựng popup trong callback.

Callback ném exception cũng không làm chết bộ đếm — SDK log lại rồi chạy tiếp.

### Đọc số liệu

```csharp
var played = VerifyAccountSdk.Playtime.Today;            // TimeSpan
var left   = VerifyAccountSdk.Playtime.RemainingToday;   // 0 nếu đã vượt hoặc mốc bị tắt
var warned = VerifyAccountSdk.Playtime.LimitReachedToday;

hudLabel.text = played.ToString(@"hh\:mm\:ss");
```

`Today` đọc được **cả khi chưa `Start()`** — nó nạp thẳng từ PlayerPrefs.

`ResetToday()` xoá bộ đếm của hôm nay kể cả cờ đã cảnh báo. Chủ yếu để test.

### Khi nào PlayerPrefs được ghi

Chỉ ở những mốc có thật: vào nền, thoát game, `Stop()`, sang ngày mới, chạm mốc cảnh báo.
Không ghi định kỳ. Đổi lại, app bị giết mà không kịp gọi `OnApplicationPause` (thường chỉ
xảy ra khi crash) thì mất phần chưa lưu của phiên đó.

---

## 9. Đổi font lúc chạy

```csharp
public static void SetUiFont(TMP_FontAsset font, Material fontMaterial = null);
```

Đè lên `uiFont`/`uiFontMaterial` trong Settings và dán xuống **mọi** `TMP_Text` của panel
lẫn badge. Gọi lúc nào cũng được, kể cả khi form đang mở.

```csharp
VerifyAccountSdk.SetUiFont(japaneseFont);              // ví dụ: đổi ngôn ngữ
VerifyAccountSdk.SetUiFont(myFont, myOutlineMaterial); // kèm material preset
VerifyAccountSdk.SetUiFont(null);                      // quay lại font trong Settings
```

Cách cấu hình font cố định cho project nằm ở [mục 3 của README](README.md#3-font-riêng-cho-từng-project).

---

## 10. Nút "Bỏ qua"

```csharp
public static void SetSkipButtonVisible(bool? visible);
```

Đè lên `showSkipButton` trong Settings, có hiệu lực ngay cả khi form đang mở. Truyền `null`
để quay lại giá trị trong Settings.

Dùng cho remote config — bật/tắt theo vùng, theo phiên bản, theo đợt kiểm duyệt:

```csharp
VerifyAccountSdk.SetSkipButtonVisible(remoteConfig.GetBool("verify_allow_skip"));
```

---

## 11. Luật kiểm tra dữ liệu

Form tự kiểm tra trước khi gọi backend. Nút "Hoàn thành" chỉ sáng khi **tất cả** đều đạt:

| Trường | Luật | Chỉnh ở |
|---|---|---|
| Họ và tên | Sau `Trim()` dài 2–50 ký tự | cố định |
| Số điện thoại | Khớp `phoneRegex`; để trống regex là chấp nhận mọi chuỗi khác rỗng | Settings |
| OTP | Đúng `otpLength` ký tự, toàn chữ số | Settings |
| Ngày sinh | Đúng `dd/MM/yyyy`, không phải ngày tương lai, đủ `minAge` tuổi | Settings |
| Điều khoản | Tick đủ **cả hai** ô | cố định |
| Đồng hồ OTP | Chưa quá `otpTtlSeconds` kể từ lần gửi gần nhất | Settings |

Ô ngày sinh tự chèn dấu `/` trong lúc gõ: `11031991` thành `11/03/1991`. Ký tự không phải
chữ số bị bỏ qua.

Ngày và tháng một chữ số cũng tự được thêm `0`, ngay khi đoán chắc chắn được: không ngày nào
bắt đầu bằng **4–9** và không tháng nào bắt đầu bằng **2–9**, nên gõ `5` `3` `1` `9` `9` `1`
ra thẳng `05/03/1991`. Chữ số còn mập mờ thì đợi phím sau — `1` ở ô tháng có thể là tháng 1
mà cũng có thể là đầu của tháng 10, 11, 12; muốn chốt sớm thì gõ `01`.

Gõ đủ `dd/mm/yyyy` mà sai ngày hoặc **chưa đủ `minAge` tuổi** thì dòng cảnh báo đỏ hiện ngay
dưới ô, không phải đợi bấm "Hoàn thành" — xem
[`DescribeBirthDateProblem`](#13-verifyvalidator).

Regex hỏng trong Settings không làm sập form — nó chỉ khiến mọi số điện thoại bị coi là
không hợp lệ.

Nút "Gửi lại" bị khoá `resendCooldownSeconds` giây sau mỗi lần gửi. OTP hết hạn thì ô OTP
hiện dòng nhắc và nút "Hoàn thành" tắt cho tới khi gửi lại.

---

## 12. VerifyAccountSettings

`ScriptableObject` tạo bằng **Create > OnDi > Verify Account Settings**, đặt ở một thư mục
`Resources` bất kỳ, **giữ nguyên tên file `VerifyAccountSettings`**.

SDK nạp theo đúng thứ tự này, lần đầu ai đó đọc `VerifyAccountSdk.Settings`:

1. `Resources.Load("VerifyAccountSettings")` — file của game, nếu có.
2. `Resources.Load("OnDiVerify/DefaultSettings")` — bản đóng gói sẵn trong package, để cài
   xong là chạy được ngay.
3. Không có cả hai thì `CreateInstance` giá trị khởi tạo của class, kèm một dòng log nhắc.

Hai bước đầu cố tình mang **hai tên khác nhau**: hai asset trùng tên nằm ở hai thư mục
`Resources` thì `Resources.Load` trả về cái nào là không xác định, và bản của package sẽ có
lúc đè mất cấu hình của game. Tên khác nhau nên file của game luôn thắng.

Kết quả được nhớ lại (`static`), nên đổi file lúc chạy không có tác dụng — đổi từng trường
trên `VerifyAccountSdk.Settings` thì được.

| Trường | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `termsUrl` | `string` | `""` | Link "Điều khoản sử dụng", mở bằng trình duyệt hệ thống |
| `privacyUrl` | `string` | `""` | Link "Chính sách bảo vệ và xử lý dữ liệu cá nhân" |
| `uiFont` | `TMP_FontAsset` | `null` | Font cho toàn bộ chữ của SDK. Trống là dùng font mặc định của TMP |
| `uiFontMaterial` | `Material` | `null` | Material preset đi kèm font (viền, đổ bóng) |
| `sortingLayerName` | `string` | `"Default"` | Sorting layer của canvas SDK. Tên không tồn tại thì rơi về Default |
| `sortingOrder` | `int` | `32000` | Sorting order — để cao để luôn nằm trên UI game |
| `phoneRegex` | `string` | `^(0\|\+84)(3\|5\|7\|8\|9)\d{8}$` | Luật số điện thoại. Trống là chấp nhận mọi chuỗi khác rỗng |
| `otpLength` | `int` | `6` | Số ký tự của mã OTP |
| `minAge` | `int` | `0` | Tuổi tối thiểu theo ngày sinh, cũng là số hiện trên badge. `0` là không kiểm tra, badge hiện "18+" |
| `otpTtlSeconds` | `int` | `180` | Đồng hồ đếm ngược "OTP hết hạn sau" |
| `resendCooldownSeconds` | `int` | `60` | Khoá nút "Gửi lại" bao nhiêu giây sau mỗi lần gửi |
| `showSkipButton` | `bool` | `true` | Hiện nút "Bỏ qua". Đè lúc chạy bằng `SetSkipButtonVisible()` |
| `badgeSprite` | `Sprite` | `null` | Icon badge. Trống là dùng icon trong prefab |
| `badgeTooltipText` | `string` | `DefaultBadgeTooltipText` | Chữ trong bong bóng của badge, có sẵn ký tự xuống dòng |
| `tooltipAutoHideSeconds` | `float` | `4` | Tự tắt bong bóng sau bao lâu. `0` là không tự tắt |
| `badgeIdleAlpha` | `float` | `0.55` | Độ mờ của badge lúc nằm yên. `1` là rõ hoàn toàn |
| `badgeFadeSeconds` | `float` | `0.15` | Thời gian chuyển giữa mờ và rõ. `0` là đổi tức thì |
| `autoStartPlaytime` | `bool` | `true` | Tự đếm thời gian chơi ngay từ lúc game khởi động |
| `dailyPlayLimitMinutes` | `int` | `180` | Mốc phát `DailyLimitReached`. `0` là tắt cảnh báo |
| `showBadgeTooltipOnDailyLimit` | `bool` | `true` | Chạm mốc thì tự bật bong bóng của badge |

Đọc Settings lúc chạy:

```csharp
int ttl = VerifyAccountSdk.Settings.otpTtlSeconds;
```

> `Settings` trả về chính asset đã nạp từ `Resources`. Sửa field trên đó lúc chạy là sửa
> asset thật trong Editor và thay đổi sẽ dính lại giữa các lần Play. Cần đổi lúc chạy thì
> dùng `SetSkipButtonVisible()` / `SetUiFont()` — hai thứ hay phải đổi nhất đã có API riêng.

---

## 13. VerifyValidator

Toàn bộ luật kiểm tra là hàm thuần, `public`, không đụng tới scene — dùng lại được nếu game
muốn kiểm tra sớm ở màn hình khác.

```csharp
public static class VerifyValidator
{
    public const string DateFormat    = "dd/MM/yyyy";
    public const int    MaxNameLength = 50;

    public static bool   IsNameValid(string name);
    public static bool   IsPhoneValid(string phone, string pattern);
    public static bool   IsOtpValid(string otp, int length);
    public static bool   TryParseBirthDate(string text, int minAge, out DateTime date);
    public static bool   TryParseBirthDate(string text, int minAge, DateTime today, out DateTime date);
    public static int    AgeOn(DateTime birth, DateTime onDate);
    public static string MaskDate(string raw);              // "531991" → "05/03/1991"
    public static string FormatCountdown(float secondsLeft); // 95f → "1:35"

    // null = chưa có gì để nói (gõ dở, hoặc ngày hợp lệ và đủ tuổi)
    public static string DescribeBirthDateProblem(string text, int minAge);
    public static string DescribeBirthDateProblem(string text, int minAge, DateTime today);
}
```

Bản `TryParseBirthDate` có tham số `today` nhận mốc "hôm nay" từ ngoài, để test không phụ
thuộc đồng hồ máy.

`DescribeBirthDateProblem` là thứ form dùng để báo lỗi **ngay trong lúc gõ** ô ngày sinh:
gõ chưa đủ `dd/mm/yyyy` thì trả `null` (chưa mắng vội), sai ngày thì trả "Ngày sinh không
hợp lệ.", còn đủ ngày nhưng chưa đạt `minAge` thì trả câu chưa đủ tuổi. Nó tách hai lỗi ra
làm hai câu, khác `TryParseBirthDate` chỉ trả `true`/`false` cho cả hai.

```csharp
if (!VerifyValidator.IsPhoneValid(input, VerifyAccountSdk.Settings.phoneRegex))
    ShowError("Số điện thoại không hợp lệ.");
```

---

## 14. PanelAutoHeight

Component `public` nằm trên node `Panel` của prefab. Nó ôm chiều cao panel theo nội dung
nhưng không vượt quá một tỉ lệ chiều cao màn hình.

| Field | Mặc định | Ý nghĩa |
|---|---|---|
| `content` | — | Node có `ContentSizeFitter` dọc, thường là `Content` của `ScrollRect` |
| `extraHeight` | `190` | Phần chiều cao nằm ngoài vùng cuộn (padding trên/dưới) |
| `maxScreenRatio` | `0.92` | Panel cao tối đa bao nhiêu phần màn hình |

```csharp
public void Apply();   // tính lại ngay lập tức; bình thường không cần gọi tay
```

Màn dọc thì panel ôm sát nội dung. Màn ngang hoặc máy màn ngắn thì panel bị kẹp lại và
phần dư được cuộn — thanh cuộn ở chế độ *auto hide* nên chỉ hiện khi thật sự cần.

---

## 15. PlayerPrefs mà SDK ghi

| Key | Kiểu | Nội dung |
|---|---|---|
| `OnDi.VerifyAccount.Verified` | `int` | `1` là đã xác thực |
| `OnDi.VerifyAccount.VerifiedAt` | `string` | Mốc UTC, định dạng round-trip (`"o"`) |
| `OnDi.VerifyAccount.Badge.OnRight` | `int` | Badge đang bám viền phải hay trái. Chưa kéo lần nào là trái |
| `OnDi.VerifyAccount.Badge.YRatio` | `float` | Vị trí dọc của badge, `0`–`1` |
| `OnDi.VerifyAccount.Playtime.Day` | `string` | Ngày đang đếm, `yyyy-MM-dd` |
| `OnDi.VerifyAccount.Playtime.Seconds` | `float` | Số giây đã chơi trong ngày đó |
| `OnDi.VerifyAccount.Playtime.Warned` | `int` | Hôm nay đã phát cảnh báo hay chưa |

**Họ tên, số điện thoại và ngày sinh không được lưu trên thiết bị.** Chúng chỉ đi qua các
hook rồi biến mất cùng form.

---

## 16. Bảng tra nhanh

```csharp
// Backend
VerifyAccountSdk.OnSendOtp       = req  => Task<SdkResult>;
VerifyAccountSdk.OnVerifyOtp     = req  => Task<SdkResult>;
VerifyAccountSdk.OnSubmitProfile = prof => Task<SdkResult>;

// Sự kiện
VerifyAccountSdk.Verified += profile => { };
VerifyAccountSdk.Skipped  += ()      => { };
VerifyAccountSdk.Closed   += ()      => { };

// Form
VerifyAccountSdk.Show();
VerifyAccountSdk.ShowForced();
VerifyAccountSdk.Hide();
bool open = VerifyAccountSdk.IsPanelOpen;

// Cờ đã xác thực
bool     done = VerifyAccountSdk.IsVerified;
DateTime when = VerifyAccountSdk.VerifiedAtUtc;
VerifyAccountSdk.ClearVerified();

// Badge 18+
VerifyAccountSdk.FloatButton.Show();
VerifyAccountSdk.FloatButton.Hide();
VerifyAccountSdk.FloatButton.ResetPosition();
bool badge = VerifyAccountSdk.FloatButton.IsVisible;

// Thời gian chơi
VerifyAccountSdk.Playtime.DailyLimitReached += total => { };
VerifyAccountSdk.Playtime.Start();
VerifyAccountSdk.Playtime.Stop();
TimeSpan played  = VerifyAccountSdk.Playtime.Today;
TimeSpan left    = VerifyAccountSdk.Playtime.RemainingToday;
bool     running = VerifyAccountSdk.Playtime.IsRunning;
bool     warned  = VerifyAccountSdk.Playtime.LimitReachedToday;
VerifyAccountSdk.Playtime.ResetToday();

// Tuỳ biến lúc chạy
VerifyAccountSdk.SetSkipButtonVisible(false);
VerifyAccountSdk.SetUiFont(myFont, myMaterial);
VerifyAccountSettings settings = VerifyAccountSdk.Settings;
```

---

## 17. Lỗi thường gặp

**Bấm "Gửi OTP" thì hiện "Chức năng chưa sẵn sàng, vui lòng thử lại sau."**
Chưa gán `VerifyAccountSdk.OnSendOtp`. Console có dòng warning chỉ đúng tên hook còn thiếu.
Nhớ gán trước khi gọi `Show()`.

**Gọi `Show()` mà không thấy gì**
`IsVerified` đang là `true`. Dùng `ShowForced()` để mở bằng được, hoặc `ClearVerified()`
để xoá cờ.

**Chữ trong form không hiện, hoặc ra ô vuông**
Project chưa import TMP Essential Resources (**Window > TextMeshPro > Import TMP Essential
Resources**), hoặc font của bạn thiếu khối `Latin Extended Additional` nên không có dấu
tiếng Việt.

**Console báo không nạp được prefab `Resources/OnDiVerify/...`**
Prefab bị xoá hoặc chưa được sinh. Chạy **Tools > OnDi Verify > Rebuild UI Prefabs**.

**Form hiện nhưng bấm không ăn**
Scene thiếu `EventSystem`. SDK tự dựng một cái nếu project dùng Input Manager cũ; project
chỉ bật Input System mới thì phải tự thêm `EventSystem` kèm `InputSystemUIInputModule` —
Console có warning nhắc.

**UI game che mất form**
Tăng `sortingOrder` trong Settings (mặc định đã là `32000`), hoặc chỉnh `sortingLayerName`.

**Chạm ra ngoài không tắt được bong bóng của badge**
Cần Input Manager cũ. Project chỉ bật Input System mới thì bong bóng vẫn tự tắt theo
`tooltipAutoHideSeconds`.

**Bộ đếm thời gian chơi về 0 giữa chừng**
Nó đi theo đồng hồ thiết bị. Người chơi vặn ngày máy thì SDK coi như sang ngày mới và đếm
lại từ đầu. Cần chống gian lận thì đối chiếu với giờ server trong callback của bạn.
