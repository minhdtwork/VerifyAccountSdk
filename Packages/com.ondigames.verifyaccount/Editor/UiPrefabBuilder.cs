using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OnDi.VerifyAccount.Editor
{
    /// <summary>
    /// Dựng lại hai prefab UI từ đầu. Chạy một lần để sinh asset; sau đó prefab là nguồn
    /// sự thật và designer sửa trực tiếp trong Inspector.
    ///
    /// ponytail: builder và prefab trùng thông tin. Nếu hai bên lệch nhau thì bỏ builder đi,
    /// đừng cố đồng bộ ngược.
    ///
    /// Toàn bộ chữ là TextMeshPro và cố tình **không gán font** — prefab dùng font mặc định
    /// của TMP, mỗi project cắm font riêng qua <see cref="VerifyAccountSettings.uiFont"/>.
    /// </summary>
    public static class UiPrefabBuilder
    {
        const string PackageRoot = "Packages/com.ondigames.verifyaccount";
        const string ResourcesDir = PackageRoot + "/Runtime/Resources/OnDiVerify";
        const string SpritesDir = ResourcesDir + "/Sprites";

        // Bảng màu lấy từ ảnh demo.
        static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color PanelTint = Color.white;
        static readonly Color FieldBg = new Color32(0xEC, 0xEC, 0xEC, 0xFF);
        static readonly Color TextDark = new Color32(0x1E, 0x1E, 0x1E, 0xFF);
        static readonly Color TextBody = new Color32(0x2B, 0x2B, 0x2B, 0xFF);
        static readonly Color TextPlaceholder = new Color32(0x9A, 0x9A, 0x9A, 0xFF);
        static readonly Color Orange = new Color32(0xF4, 0x67, 0x1F, 0xFF);
        static readonly Color ErrorRed = new Color32(0xD3, 0x2F, 0x2F, 0xFF);
        static readonly Color ScrollTrack = new Color32(0x00, 0x00, 0x00, 0x14);
        static readonly Color ScrollHandle = new Color32(0xB5, 0xB5, 0xB5, 0xFF);

        const float PanelWidth = 888f;
        const float SidePadding = 68f;
        const float FieldHeight = 66f;
        const float ScrollTopInset = 130f;
        const float ScrollBottomInset = 40f;
        const float ScrollbarWidth = 16f;
        const float ScrollbarInset = 10f;   // cách mép phải panel
        const float ScrollbarVInset = 8f;   // tránh góc bo của khung

        // Bong bóng badge: cột "18+" bên trái, gạch dọc, rồi chữ cảnh báo.
        const float TooltipWidth = 560f;
        const float TooltipPadX = 20f;
        const float TooltipMarkWidth = 140f;
        const float TooltipDividerX = TooltipPadX + TooltipMarkWidth;
        const float TooltipTextInset = 22f; // chữ cách gạch dọc

        const float FontTitle = 40f;
        const float FontBody = 31f;
        const float FontField = 30f;
        const float FontError = 26f;
        const float FontButton = 30f;
        const float FontSubmit = 35f;
        const float FontAgree = 29f;

        [MenuItem("Tools/OnDi Verify/Rebuild UI Prefabs")]
        public static void RebuildAll()
        {
            if (!EnsureTmpResources()) return;

            ApplySpriteImportSettings();
            BuildPanelPrefab();
            BuildBadgePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VerifyAccount] Đã dựng lại prefab vào " + ResourcesDir);
        }

        /// <summary>
        /// Không có TMP Essential Resources thì TextMeshPro không có font mặc định và prefab
        /// dựng ra sẽ trống trơn. Import luôn hộ, đằng nào project cũng cần — nhưng
        /// <c>ImportPackage</c> chạy bất đồng bộ nên phải đợi nó xong rồi mới dựng lại.
        /// </summary>
        static bool EnsureTmpResources()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;

            Debug.Log("[VerifyAccount] Chưa có TMP Essential Resources — import xong sẽ tự dựng " +
                      "lại prefab.");
            AssetDatabase.importPackageCompleted += OnTmpImportCompleted;
            AssetDatabase.importPackageFailed += OnTmpImportFailed;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            return false;
        }

        static void OnTmpImportCompleted(string packageName)
        {
            UnsubscribeTmpImport();
            EditorApplication.delayCall += RebuildAll;
        }

        static void OnTmpImportFailed(string packageName, string error)
        {
            UnsubscribeTmpImport();
            Debug.LogError("[VerifyAccount] Import TMP Essential Resources thất bại (" + error +
                           "). Vào Window > TextMeshPro > Import TMP Essential Resources rồi chạy lại.");
        }

        static void UnsubscribeTmpImport()
        {
            AssetDatabase.importPackageCompleted -= OnTmpImportCompleted;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
        }

        // ---- Import settings cho sprite ----

        static void ApplySpriteImportSettings()
        {
            // border 9-slice đo từ bán kính bo góc của từng ảnh
            Configure("BG.png", new Vector4(24, 24, 24, 24));
            Configure("btn_cam.png", new Vector4(10, 10, 10, 10));
            Configure("btn_xam.png", new Vector4(10, 10, 10, 10));
            Configure("f_tick.png", new Vector4(10, 10, 10, 10));
            Configure("round_white.png", new Vector4(16, 16, 16, 16));
            Configure("bubble.png", new Vector4(28, 28, 28, 28));
            Configure("bubble_tail.png", Vector4.zero);
            Configure("tick.png", Vector4.zero);
            Configure("badge18.png", Vector4.zero);
        }

        static void Configure(string fileName, Vector4 border)
        {
            var path = SpritesDir + "/" + fileName;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[VerifyAccount] Thiếu sprite " + path);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static Sprite Sprite(string fileName) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/" + fileName);

        // ---- Panel ----

        static void BuildPanelPrefab()
        {
            var root = NewUi("VerifyPanel", null);
            Stretch(root);
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = Backdrop;
            backdrop.raycastTarget = true; // chặn click xuống UI của game bên dưới

            var panel = NewUi("Panel", root);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, 1400f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = Sprite("BG.png");
            panelImage.type = Image.Type.Sliced;
            panelImage.color = PanelTint;

            // "Bỏ qua" nằm ngoài vùng cuộn để luôn nhìn thấy
            var skip = NewButton("BtnSkip", panel, "round_white.png", "Bỏ qua", TextDark, FontBody);
            var skipRect = (RectTransform)skip.transform;
            skipRect.anchorMin = skipRect.anchorMax = skipRect.pivot = new Vector2(1f, 1f);
            skipRect.anchoredPosition = new Vector2(-40f, -40f);
            skipRect.sizeDelta = new Vector2(150f, 62f);

            var scroll = NewUi("Scroll", panel);
            Stretch(scroll, 0f, ScrollBottomInset, 0f, ScrollTopInset);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;

            var viewport = NewUi("Viewport", scroll);
            Stretch(viewport);
            // Thanh cuộn ăn bớt bề ngang viewport từ mép phải, nên pivot phải nằm ở mép trái.
            viewport.pivot = new Vector2(0f, 1f);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Ảnh trong suốt để kéo vào chỗ trống trong form cũng cuộn được.
            AddDragCatcher(viewport);
            scrollRect.viewport = viewport;

            var content = NewUi("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);
            AddDragCatcher(content);
            scrollRect.content = content;

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset((int)SidePadding, (int)SidePadding, 24, 24);
            vlg.spacing = 18f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildVerticalScrollbar(scroll, scrollRect);

            var autoHeight = panel.gameObject.AddComponent<PanelAutoHeight>();

            // --- nội dung form, đúng thứ tự trong demo ---
            NewLabel("Title", content, "Xác thực thông tin", TextDark, FontTitle,
                     TextAlignmentOptions.Center, bold: true, height: 62f);
            NewLabel("Subtitle", content, "Vui lòng xác thực để tiếp tục dịch vụ.", TextBody, FontBody,
                     TextAlignmentOptions.Left, bold: true, height: 50f);

            var nameInput = NewField("NameField", content, "Nhập họ và tên",
                                     TMP_InputField.ContentType.Standard, TouchScreenKeyboardType.Default, 50);
            var nameError = NewError("NameError", content);

            var phoneInput = NewField("PhoneField", content, "Nhập số điện thoại",
                                      TMP_InputField.ContentType.Custom, TouchScreenKeyboardType.PhonePad, 15);
            var phoneError = NewError("PhoneError", content);

            var sendOtp = NewButton("BtnSendOtp", NewRow("RowSendOtp", content, 86f),
                                    "btn_cam.png", "Gửi OTP", Color.white, FontButton);
            SetLayoutSize(sendOtp.transform, 215f, 80f);

            var otpInput = NewField("OtpField", content, "Nhập mã OTP",
                                    TMP_InputField.ContentType.IntegerNumber, TouchScreenKeyboardType.NumberPad, 6);

            var resend = NewButton("BtnResend", NewRow("RowResend", content, 68f),
                                   "btn_cam.png", "Gửi lại", Color.white, FontButton);
            SetLayoutSize(resend.transform, 168f, 62f);

            var countdown = NewLabel("Countdown", content, "OTP hết hạn sau 3:00", TextBody, FontBody,
                                     TextAlignmentOptions.Left, bold: true, height: 48f);
            var otpError = NewError("OtpError", content);

            var dobInput = NewField("DobField", content, "dd/mm/yyyy",
                                    TMP_InputField.ContentType.Custom, TouchScreenKeyboardType.NumberPad, 10);
            var dobError = NewError("DobError", content);

            // hàng tiêu đề nhóm điều khoản + mũi tên gập
            var agreeHeader = NewRow("RowAgreeHeader", content, 0f);
            var agreeHlg = agreeHeader.gameObject.GetComponent<HorizontalLayoutGroup>();
            agreeHlg.childForceExpandWidth = false;
            agreeHlg.spacing = 12f;
            var agreeLabel = NewLabel("Label", agreeHeader,
                                      "Tôi đồng ý và chấp thuận với các chính sách và điều khoản sau đây",
                                      TextBody, FontBody, TextAlignmentOptions.Left, bold: true, height: 0f);
            SetFlexibleWidth(agreeLabel.transform, 1f);
            var arrow = NewLabel("Arrow", agreeHeader, "^", TextBody, 40f, TextAlignmentOptions.Center,
                                 bold: true, height: 0f);
            SetLayoutSize(arrow.transform, 48f, 48f);
            var agreeHeaderButton = agreeHeader.gameObject.AddComponent<Button>();
            agreeHeaderButton.targetGraphic = agreeLabel;
            agreeHeaderButton.transition = Selectable.Transition.None;

            var agreeGroup = NewUi("AgreeGroup", content);
            var agreeVlg = agreeGroup.gameObject.AddComponent<VerticalLayoutGroup>();
            agreeVlg.padding = new RectOffset(48, 0, 12, 12);
            agreeVlg.spacing = 24f;
            agreeVlg.childControlWidth = true;
            agreeVlg.childControlHeight = true;
            agreeVlg.childForceExpandWidth = true;
            agreeVlg.childForceExpandHeight = false;

            BuildAgreeRow(agreeGroup, "RowTerms",
                          "<color=#F4671F>Điều khoản sử dụng</color> (các điều khoản và điều kiện sử dụng dịch vụ)",
                          out var termsToggle, out var termsLink);
            BuildAgreeRow(agreeGroup, "RowPrivacy",
                          "<color=#F4671F>Chính sách bảo vệ và xử lý dữ liệu cá nhân</color> " +
                          "(điều khoản và điều kiện liên quan đến thu thập và xử lý dữ liệu cá nhân)",
                          out var privacyToggle, out var privacyLink);

            var formError = NewError("FormError", content);

            var submit = NewButton("BtnSubmit", content, "btn_xam.png", "Hoàn thành", Color.white, FontSubmit);
            SetLayoutSize(submit.transform, 0f, 88f, preferHeightOnly: true);

            WirePanel(root, autoHeight, content, scrollRect,
                      nameInput, phoneInput, otpInput, dobInput,
                      skip, sendOtp, resend, submit,
                      termsToggle, privacyToggle, termsLink, privacyLink,
                      agreeHeaderButton, agreeGroup, arrow,
                      countdown, nameError, phoneError, otpError, dobError, formError);

            SavePrefab(root.gameObject, ResourcesDir + "/VerifyPanel.prefab");
        }

        /// <summary>
        /// Thanh cuộn dọc bám mép phải vùng cuộn. <c>AutoHideAndExpandViewport</c> nên nội dung
        /// vừa khung thì thanh biến mất hẳn và form rộng lại như cũ; chỉ khi UI bị co — màn ngang,
        /// máy màn ngắn, chữ xuống dòng nhiều — thanh mới hiện ra.
        /// </summary>
        static void BuildVerticalScrollbar(RectTransform scroll, ScrollRect scrollRect)
        {
            var bar = NewUi("ScrollbarV", scroll);
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 1f);
            bar.offsetMin = new Vector2(-(ScrollbarWidth + ScrollbarInset), ScrollbarVInset);
            bar.offsetMax = new Vector2(-ScrollbarInset, -ScrollbarVInset);

            var track = bar.gameObject.AddComponent<Image>();
            track.sprite = Sprite("round_white.png");
            track.type = Image.Type.Sliced;
            // Bo góc của round_white là 16px, rộng hơn cả thanh — thu nhỏ border lại cho vừa.
            track.pixelsPerUnitMultiplier = 4f;
            track.color = ScrollTrack;

            var slidingArea = NewUi("Sliding Area", bar);
            Stretch(slidingArea);

            var handle = NewUi("Handle", slidingArea);
            Stretch(handle);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = Sprite("round_white.png");
            handleImage.type = Image.Type.Sliced;
            handleImage.pixelsPerUnitMultiplier = 4f;
            handleImage.color = ScrollHandle;

            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.transition = Selectable.Transition.None;
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handle;
            scrollbar.value = 1f;
            scrollbar.size = 1f;

            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            // Viewport hẹp lại đúng bằng bề ngang thanh cộng khoảng cách tới mép, nên mép
            // phải của nội dung dừng ngay sát mép trái thanh cuộn, không chồng lên nhau.
            scrollRect.verticalScrollbarSpacing = ScrollbarInset;
        }

        /// <summary>Graphic trong suốt: ScrollRect cần một thứ nhận raycast thì mới kéo được.</summary>
        static void AddDragCatcher(RectTransform target)
        {
            var image = target.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }

        static void WirePanel(RectTransform root, PanelAutoHeight autoHeight, RectTransform content,
                              ScrollRect scrollRect,
                              TMP_InputField nameInput, TMP_InputField phoneInput,
                              TMP_InputField otpInput, TMP_InputField dobInput,
                              Button skip, Button sendOtp, Button resend, Button submit,
                              Toggle termsToggle, Toggle privacyToggle, Button termsLink, Button privacyLink,
                              Button agreeHeaderButton, RectTransform agreeGroup, TMP_Text arrow,
                              TMP_Text countdown, TMP_Text nameError, TMP_Text phoneError, TMP_Text otpError,
                              TMP_Text dobError, TMP_Text formError)
        {
            var panel = root.GetComponent<VerifyPanel>() != null
                ? root.GetComponent<VerifyPanel>()
                : root.gameObject.AddComponent<VerifyPanel>();

            var so = new SerializedObject(panel);
            so.FindProperty("nameInput").objectReferenceValue = nameInput;
            so.FindProperty("phoneInput").objectReferenceValue = phoneInput;
            so.FindProperty("otpInput").objectReferenceValue = otpInput;
            so.FindProperty("dobInput").objectReferenceValue = dobInput;
            so.FindProperty("skipButton").objectReferenceValue = skip;
            so.FindProperty("sendOtpButton").objectReferenceValue = sendOtp;
            so.FindProperty("resendButton").objectReferenceValue = resend;
            so.FindProperty("submitButton").objectReferenceValue = submit;
            so.FindProperty("submitImage").objectReferenceValue = submit.GetComponent<Image>();
            so.FindProperty("submitEnabledSprite").objectReferenceValue = Sprite("btn_cam.png");
            so.FindProperty("submitDisabledSprite").objectReferenceValue = Sprite("btn_xam.png");
            so.FindProperty("termsToggle").objectReferenceValue = termsToggle;
            so.FindProperty("privacyToggle").objectReferenceValue = privacyToggle;
            so.FindProperty("termsLinkButton").objectReferenceValue = termsLink;
            so.FindProperty("privacyLinkButton").objectReferenceValue = privacyLink;
            so.FindProperty("agreeHeaderButton").objectReferenceValue = agreeHeaderButton;
            so.FindProperty("agreeGroup").objectReferenceValue = agreeGroup.gameObject;
            so.FindProperty("agreeArrow").objectReferenceValue = arrow.rectTransform;
            so.FindProperty("scroll").objectReferenceValue = scrollRect;
            so.FindProperty("countdownText").objectReferenceValue = countdown;
            so.FindProperty("nameError").objectReferenceValue = nameError;
            so.FindProperty("phoneError").objectReferenceValue = phoneError;
            so.FindProperty("otpError").objectReferenceValue = otpError;
            so.FindProperty("dobError").objectReferenceValue = dobError;
            so.FindProperty("formError").objectReferenceValue = formError;
            so.ApplyModifiedPropertiesWithoutUndo();

            var ah = new SerializedObject(autoHeight);
            ah.FindProperty("content").objectReferenceValue = content;
            ah.FindProperty("extraHeight").floatValue = ScrollTopInset + ScrollBottomInset;
            ah.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildAgreeRow(RectTransform parent, string name, string richText,
                                  out Toggle toggle, out Button linkButton)
        {
            var row = NewUi(name, parent);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var box = NewUi("Toggle", row);
            var boxImage = box.gameObject.AddComponent<Image>();
            boxImage.sprite = Sprite("f_tick.png");
            boxImage.type = Image.Type.Sliced;
            SetLayoutSize(box, 44f, 44f);

            var check = NewUi("Checkmark", box);
            Stretch(check, 6f, 6f, 6f, 6f);
            var checkImage = check.gameObject.AddComponent<Image>();
            checkImage.sprite = Sprite("tick.png");
            checkImage.color = Orange;
            checkImage.raycastTarget = false;

            toggle = box.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            var label = NewLabel("Label", row, richText, TextBody, FontAgree, TextAlignmentOptions.TopLeft,
                                 bold: true, height: 0f);
            SetFlexibleWidth(label.transform, 1f);
            linkButton = label.gameObject.AddComponent<Button>();
            linkButton.targetGraphic = label;
            linkButton.transition = Selectable.Transition.None;
        }

        // ---- Badge ----

        static void BuildBadgePrefab()
        {
            var root = NewUi("FloatBadge", null);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(120f, 120f);

            var icon = root.gameObject.AddComponent<Image>();
            icon.sprite = Sprite("badge18.png");
            icon.raycastTarget = true;

            // Mờ/rõ chạy qua CanvasGroup để một lần đổi là cả badge lẫn bong bóng cùng theo.
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            var label = NewLabel("Label", root, "18<sup>+</sup>", new Color32(0x22, 0x33, 0x55, 0xFF), 44f,
                                 TextAlignmentOptions.Center, bold: true, height: 0f);
            Stretch(label.rectTransform);
            label.raycastTarget = false;

            var tooltip = NewUi("Tooltip", root);
            tooltip.anchorMin = tooltip.anchorMax = tooltip.pivot = new Vector2(0.5f, 0.5f);
            tooltip.sizeDelta = new Vector2(TooltipWidth, 220f);
            var bubble = tooltip.gameObject.AddComponent<Image>();
            bubble.sprite = Sprite("bubble.png");
            bubble.type = Image.Type.Sliced;
            bubble.raycastTarget = true;

            // Chỉ chữ nằm trong layout; cột "18+" và gạch dọc neo tuyệt đối nên chúng cao
            // bằng bong bóng dù bong bóng co giãn theo số dòng chữ.
            var bubbleVlg = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();
            bubbleVlg.padding = new RectOffset(
                (int)(TooltipDividerX + TooltipTextInset), (int)TooltipPadX + 4, 28, 28);
            bubbleVlg.childControlWidth = true;
            bubbleVlg.childControlHeight = true;
            bubbleVlg.childForceExpandWidth = true;
            bubbleVlg.childForceExpandHeight = false;
            var bubbleFitter = tooltip.gameObject.AddComponent<ContentSizeFitter>();
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var mark = NewLabel("Mark", tooltip, "18<sup>+</sup>", TextDark, 82f,
                                TextAlignmentOptions.Center, bold: true, height: 0f);
            mark.enableWordWrapping = false; // "+" không được rơi xuống dòng dưới
            mark.raycastTarget = false;
            StretchColumn(mark.rectTransform, TooltipPadX + TooltipMarkWidth * 0.5f,
                          TooltipMarkWidth, 0f);

            var divider = NewUi("Divider", tooltip);
            var dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = TextDark;
            dividerImage.raycastTarget = false;
            StretchColumn(divider, TooltipDividerX, 2f, 6f);

            var text = NewLabel("Text", tooltip,
                                VerifyAccountSettings.DefaultBadgeTooltipText,
                                TextDark, 36f, TextAlignmentOptions.Center, bold: true, height: 0f);
            text.raycastTarget = false;

            var tail = NewUi("Tail", tooltip);
            tail.sizeDelta = new Vector2(30f, 46f);
            var tailImage = tail.gameObject.AddComponent<Image>();
            tailImage.sprite = Sprite("bubble_tail.png");
            tailImage.raycastTarget = false;
            tail.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var badge = root.gameObject.AddComponent<FloatBadge>();
            var so = new SerializedObject(badge);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("tooltip").objectReferenceValue = tooltip;
            so.FindProperty("tooltipText").objectReferenceValue = text;
            so.FindProperty("tooltipTail").objectReferenceValue = tail;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            tooltip.gameObject.SetActive(false);

            SavePrefab(root.gameObject, ResourcesDir + "/FloatBadge.prefab");
        }

        // ---- Tiện ích dựng UI ----

        static RectTransform NewUi(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f,
                            float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>
        /// Cột cao bằng cha, rộng cố định, đo từ mép trái cha — và đứng ngoài layout group để
        /// cha co giãn bao nhiêu cột cũng theo.
        /// </summary>
        static void StretchColumn(RectTransform rect, float centerX, float width, float vInset)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, -vInset * 2f);
            rect.anchoredPosition = new Vector2(centerX, 0f);
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        static TextMeshProUGUI NewLabel(string name, RectTransform parent, string content, Color color,
                                        float size, TextAlignmentOptions alignment, bool bold, float height)
        {
            var rect = NewUi(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.color = color;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alignment;
            text.richText = true;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = 8f;
            text.margin = Vector4.zero;
            if (height > 0f) SetLayoutSize(rect, 0f, height, preferHeightOnly: true);
            return text;
        }

        static TextMeshProUGUI NewError(string name, RectTransform parent)
        {
            var text = NewLabel(name, parent, "", ErrorRed, FontError, TextAlignmentOptions.Left,
                                bold: false, height: 0f);
            text.gameObject.SetActive(false);
            return text;
        }

        static RectTransform NewRow(string name, RectTransform parent, float height)
        {
            var row = NewUi(name, parent);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            if (height > 0f) SetLayoutSize(row, 0f, height, preferHeightOnly: true);
            return row;
        }

        static Button NewButton(string name, RectTransform parent, string sprite, string caption,
                                Color captionColor, float fontSize)
        {
            var rect = NewUi(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(sprite);
            image.type = Image.Type.Sliced;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = NewLabel("Label", rect, caption, captionColor, fontSize,
                                 TextAlignmentOptions.Center, bold: true, height: 0f);
            Stretch(label.rectTransform, 10f, 4f, 10f, 4f);
            label.raycastTarget = false;
            return button;
        }

        static TMP_InputField NewField(string name, RectTransform parent, string placeholder,
                                       TMP_InputField.ContentType contentType,
                                       TouchScreenKeyboardType keyboard, int characterLimit)
        {
            var rect = NewUi(name, parent);
            var bg = rect.gameObject.AddComponent<Image>();
            bg.sprite = Sprite("round_white.png");
            bg.type = Image.Type.Sliced;
            bg.color = FieldBg;
            SetLayoutSize(rect, 0f, FieldHeight, preferHeightOnly: true);

            // TMP_InputField cắt chữ tràn bằng RectMask2D trên "Text Area" chứ không bằng offset
            // như InputField cũ, nên phải có đúng node này ở giữa.
            var area = NewUi("Text Area", rect);
            Stretch(area, 26f, 6f, 26f, 6f);
            area.gameObject.AddComponent<RectMask2D>();

            var placeholderText = NewLabel("Placeholder", area, placeholder, TextPlaceholder, FontField,
                                           TextAlignmentOptions.Left, bold: false, height: 0f);
            Stretch(placeholderText.rectTransform);
            placeholderText.enableWordWrapping = false;
            placeholderText.raycastTarget = false;

            var text = NewLabel("Text", area, "", TextDark, FontField, TextAlignmentOptions.Left,
                                bold: true, height: 0f);
            Stretch(text.rectTransform);
            text.enableWordWrapping = false;
            text.richText = false;
            text.raycastTarget = false;

            var input = rect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.contentType = contentType;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.keyboardType = keyboard;
            input.characterLimit = characterLimit;
            input.customCaretColor = true;
            input.caretColor = TextDark;
            input.caretWidth = 2;
            input.selectionColor = new Color32(0xF4, 0x67, 0x1F, 0x55);
            input.restoreOriginalTextOnEscape = false;
            return input;
        }

        static void SetLayoutSize(Transform target, float width, float height,
                                  bool preferHeightOnly = false)
        {
            var element = target.gameObject.GetComponent<LayoutElement>() ??
                          target.gameObject.AddComponent<LayoutElement>();
            if (!preferHeightOnly && width > 0f)
            {
                element.preferredWidth = width;
                element.minWidth = width;
            }
            element.preferredHeight = height;
            element.minHeight = height;
        }

        static void SetFlexibleWidth(Transform target, float value)
        {
            var element = target.gameObject.GetComponent<LayoutElement>() ??
                          target.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = value;
        }

        static void SavePrefab(GameObject go, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
    }
}
