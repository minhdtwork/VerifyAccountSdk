using System.Threading.Tasks;
using OnDi.VerifyAccount;
using UnityEngine;

/// <summary>
/// Demo tích hợp SDK với một "server" giả.
/// Đặt script này lên một GameObject rỗng rồi bấm Play.
///
/// Quy ước của server giả:
///  - số bắt đầu bằng 0999 thì gửi OTP thất bại, để thử đường lỗi
///  - mã OTP đúng là 123456
/// </summary>
public sealed class DemoBootstrap : MonoBehaviour
{
    const string ValidOtp = "123456";

    string _log = "Chưa có gì.";

    void Awake()
    {
        VerifyAccountSdk.OnSendOtp = FakeSendOtp;
        VerifyAccountSdk.OnVerifyOtp = FakeVerifyOtp;
        VerifyAccountSdk.OnSubmitProfile = FakeSubmitProfile;

        VerifyAccountSdk.Verified += profile =>
            SetLog("Verified: " + profile.FullName + " / " + profile.PhoneNumber + " / " +
                   profile.BirthDate.ToString("dd/MM/yyyy"));
        VerifyAccountSdk.Skipped += () => SetLog("Người chơi bấm Bỏ qua.");
        VerifyAccountSdk.Closed += () => SetLog("Panel đã đóng.");
    }

    static async Task<SdkResult> FakeSendOtp(SendOtpRequest request)
    {
        await Task.Delay(700);
        if (request.PhoneNumber.StartsWith("0999"))
            return SdkResult.Fail("Số điện thoại này đang bị chặn.");

        Debug.Log("[Demo] OTP cho " + request.PhoneNumber + " là " + ValidOtp);
        return SdkResult.Success();
    }

    static async Task<SdkResult> FakeVerifyOtp(VerifyOtpRequest request)
    {
        await Task.Delay(700);
        return request.Otp == ValidOtp
            ? SdkResult.Success()
            : SdkResult.Fail("Mã OTP không đúng, thử " + ValidOtp + ".");
    }

    static async Task<SdkResult> FakeSubmitProfile(VerifiedProfile profile)
    {
        await Task.Delay(500);
        Debug.Log("[Demo] Đã gửi hồ sơ lên server: " + JsonUtility.ToJson(profile));
        return SdkResult.Success();
    }

    void SetLog(string message)
    {
        _log = message;
        Debug.Log("[Demo] " + message);
    }

    // OnGUI cho nhanh — đây là bảng điều khiển của demo, không phải UI của SDK.
    void OnGUI()
    {
        var scale = Screen.height / 1280f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

        GUILayout.BeginArea(new Rect(20, 20, 420, 520));
        GUILayout.Label("OnDi Verify Account — demo");
        GUILayout.Label("Đã xác thực: " + VerifyAccountSdk.IsVerified);
        GUILayout.Space(8);

        if (GUILayout.Button("Mở form xác thực", GUILayout.Height(44)))
            VerifyAccountSdk.ShowForced();

        if (GUILayout.Button("Show() — bỏ qua nếu đã xác thực", GUILayout.Height(44)))
            VerifyAccountSdk.Show();

        if (GUILayout.Button("Hiện badge 18+", GUILayout.Height(44)))
            VerifyAccountSdk.FloatButton.Show();

        if (GUILayout.Button("Ẩn badge 18+", GUILayout.Height(44)))
            VerifyAccountSdk.FloatButton.Hide();

        if (GUILayout.Button("Đưa badge về chỗ cũ", GUILayout.Height(44)))
            VerifyAccountSdk.FloatButton.ResetPosition();

        if (GUILayout.Button("Ẩn nút Bỏ qua", GUILayout.Height(44)))
            VerifyAccountSdk.SetSkipButtonVisible(false);

        if (GUILayout.Button("Hiện lại nút Bỏ qua", GUILayout.Height(44)))
            VerifyAccountSdk.SetSkipButtonVisible(true);

        if (GUILayout.Button("Xoá cờ đã xác thực", GUILayout.Height(44)))
            VerifyAccountSdk.ClearVerified();

        GUILayout.Space(12);
        GUILayout.Label(_log);
        GUILayout.EndArea();
    }
}
