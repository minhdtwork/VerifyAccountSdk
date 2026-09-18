using System.IO;
using OnDi.VerifyAccount;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Chụp prefab UI ra PNG để đối chiếu với ảnh demo. Công cụ nội bộ của project phát triển,
/// không nằm trong package.
/// Chạy: <c>Unity -batchmode -executeMethod DevScreenshot.Capture</c> (không kèm -nographics).
/// </summary>
public static class DevScreenshot
{
    const string OutDir = "DevShots"; // Unity xoá sạch Temp/ khi thoát nên không dùng chỗ đó

    [MenuItem("Tools/OnDi Verify/Dev - Capture Screenshots")]
    public static void Capture()
    {
        Directory.CreateDirectory(OutDir);
        Shoot("panel_portrait", 1080, 1920, "OnDiVerify/VerifyPanel");
        Shoot("panel_landscape", 1920, 1080, "OnDiVerify/VerifyPanel");
        Shoot("badge", 900, 500, "OnDiVerify/FloatBadge");
        Debug.Log("[DevShot] Đã ghi ảnh vào " + Path.GetFullPath(OutDir));
    }

    static void Shoot(string name, int width, int height, string resourcePath)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var rt = new RenderTexture(width, height, 24) { antiAliasing = 1 };
        var cam = new GameObject("Cam").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.30f, 0.18f); // giả nền game cho dễ nhìn
        cam.orthographic = true;
        cam.targetTexture = rt;

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;

        var landscape = width > height;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = landscape ? new Vector2(1920f, 1080f) : new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = landscape ? 1f : 0f;

        var prefab = Resources.Load<GameObject>(resourcePath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform);
        ((RectTransform)instance.transform).anchoredPosition = Vector2.zero;

        PrepareBadge(instance);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvasGo.transform);

        // PanelAutoHeight là ExecuteAlways: bật tắt lại để OnEnable của nó chạy sau khi
        // canvas đã có kích thước thật.
        instance.SetActive(false);
        instance.SetActive(true);
        var autoHeight = instance.GetComponentInChildren<PanelAutoHeight>(true);
        if (autoHeight != null) autoHeight.Apply();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvasGo.transform);
        Canvas.ForceUpdateCanvases();

        cam.Render();

        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        shot.Apply();
        RenderTexture.active = previous;

        File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), shot.EncodeToPNG());
        Object.DestroyImmediate(shot);
        cam.targetTexture = null;
        rt.Release();
    }

    /// <summary>Badge trong edit mode không chạy Awake, nên bật bong bóng bằng tay để xem hình.</summary>
    static void PrepareBadge(GameObject instance)
    {
        var tooltip = instance.transform.Find("Tooltip") as RectTransform;
        if (tooltip == null) return;

        tooltip.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);

        var tail = tooltip.Find("Tail") as RectTransform;
        if (tail != null)
        {
            tail.anchorMin = tail.anchorMax = new Vector2(0f, 0.5f);
            tail.anchoredPosition = new Vector2(-tail.rect.width * 0.5f + 2f, 0f);
        }

        var badgeRect = (RectTransform)instance.transform;
        badgeRect.anchoredPosition = new Vector2(-260f, 0f);
        tooltip.anchoredPosition = new Vector2(
            badgeRect.rect.width * 0.5f + (tail != null ? tail.rect.width : 0f) + tooltip.rect.width * 0.5f, 0f);
    }
}
