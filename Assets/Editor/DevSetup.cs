using OnDi.VerifyAccount;
using OnDi.VerifyAccount.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        Debug.Log("[DevSetup] Done.");
    }

    static void CreateSettings()
    {
        if (AssetDatabase.LoadAssetAtPath<VerifyAccountSettings>(SettingsPath) != null) return;

        var settings = ScriptableObject.CreateInstance<VerifyAccountSettings>();
        settings.termsUrl = "https://example.com/terms";
        settings.privacyUrl = "https://example.com/privacy";
        AssetDatabase.CreateAsset(settings, SettingsPath);
        Debug.Log("[DevSetup] Created " + SettingsPath);
    }

    static void CreateDemoScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var go = new GameObject("DemoBootstrap");
        go.AddComponent<DemoBootstrap>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, DemoScenePath);
        Debug.Log("[DevSetup] Created " + DemoScenePath);
    }
}
