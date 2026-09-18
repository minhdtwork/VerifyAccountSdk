using OnDi.VerifyAccount;
using OnDi.VerifyAccount.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Thiết lập cho chính project phát triển SDK này: dựng prefab, tạo file cấu hình mẫu và
/// scene demo. Không phải một phần của package.
/// Chạy bằng menu hoặc <c>Unity -batchmode -executeMethod DevSetup.Run</c>.
/// </summary>
public static class DevSetup
{
    const string SettingsPath = "Assets/Resources/VerifyAccountSettings.asset";
    const string DemoScenePath = "Assets/VerifyAccountDemo/DemoScene.unity";

    [MenuItem("Tools/OnDi Verify/Dev - Rebuild Everything")]
    public static void Run()
    {
        UiPrefabBuilder.RebuildAll();
        CreateSettings();
        CreateDemoScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DevSetup] Xong.");
    }

    static void CreateSettings()
    {
        if (AssetDatabase.LoadAssetAtPath<VerifyAccountSettings>(SettingsPath) != null) return;

        var settings = ScriptableObject.CreateInstance<VerifyAccountSettings>();
        settings.termsUrl = "https://example.com/terms";
        settings.privacyUrl = "https://example.com/privacy";
        AssetDatabase.CreateAsset(settings, SettingsPath);
        Debug.Log("[DevSetup] Đã tạo " + SettingsPath);
    }

    static void CreateDemoScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var go = new GameObject("DemoBootstrap");
        go.AddComponent<DemoBootstrap>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, DemoScenePath);
        Debug.Log("[DevSetup] Đã tạo " + DemoScenePath);
    }
}
