using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On-screen camera view buttons for mobile devices.
/// Shows 3 buttons in the bottom-left corner (White/Black/Top views).
/// Only visible when PlatformDetector.IsMobile is true.
/// </summary>
public class MobileCameraControls : MonoBehaviour
{
    private GameObject container;
    private CameraMove cameraMove;
    private Font uiFont;
    private bool isInitialized;

    // Button size and positioning - smaller in portrait mode
    private float ButtonSize => IsPortrait ? 40f : 50f;
    private const float BUTTON_SPACING = 6f;
    private const float MARGIN = 10f;

    private bool IsPortrait => Screen.height > Screen.width;

    void Start()
    {
        // Only show on mobile platforms
        if (!PlatformDetector.IsMobile)
        {
            enabled = false;
            return;
        }

        // Don't show in online matches (camera is locked to player perspective)
        if (MatchConfig.isOnlineMatch)
        {
            enabled = false;
            return;
        }

        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        cameraMove = FindFirstObjectByType<CameraMove>();
        if (cameraMove == null)
        {
            Debug.LogWarning("[MobileCameraControls] CameraMove not found, disabling controls");
            enabled = false;
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[MobileCameraControls] Canvas not found, disabling controls");
            enabled = false;
            return;
        }

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateUI(canvas);
        isInitialized = true;
    }

    private void CreateUI(Canvas canvas)
    {
        float btnSize = ButtonSize;

        // Container anchored to bottom-left
        container = new GameObject("MobileCameraControls");
        container.transform.SetParent(canvas.transform, false);

        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0f, 0f);
        containerRt.anchorMax = new Vector2(0f, 0f);
        containerRt.pivot = new Vector2(0f, 0f);
        containerRt.anchoredPosition = new Vector2(MARGIN, MARGIN);
        float totalWidth = btnSize * 3 + BUTTON_SPACING * 2;
        containerRt.sizeDelta = new Vector2(totalWidth, btnSize);

        // Semi-transparent background
        Image containerBg = container.AddComponent<Image>();
        containerBg.color = new Color(0f, 0f, 0f, 0.3f);

        // Create three buttons: White (1), Black (2), Top (3)
        CreateCameraButton(container.transform, "WhiteViewBtn", "W", 0, btnSize, () => OnCameraViewClicked(1));
        CreateCameraButton(container.transform, "BlackViewBtn", "B", 1, btnSize, () => OnCameraViewClicked(2));
        CreateCameraButton(container.transform, "TopViewBtn", "T", 2, btnSize, () => OnCameraViewClicked(3));
    }

    private void CreateCameraButton(Transform parent, string name, string label, int index, float btnSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        float xPos = index * (btnSize + BUTTON_SPACING);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(xPos, 0f);
        rt.sizeDelta = new Vector2(btnSize, btnSize);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.25f, 0.35f, 0.85f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(onClick);

        // Label - smaller font in portrait
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = IsPortrait ? 18 : 22;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.text = label;
    }

    private void OnCameraViewClicked(int viewIndex)
    {
        // Trigger haptic feedback
        HapticFeedback.SelectionVibrate();

        // Call camera move directly (since we have a reference)
        switch (viewIndex)
        {
            case 1:
                cameraMove.Player1Move();
                break;
            case 2:
                cameraMove.Player2Move();
                break;
            case 3:
                cameraMove.TopMove();
                break;
        }
    }

    /// <summary>
    /// Show or hide the camera controls.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (container != null)
        {
            container.SetActive(visible);
        }
    }

    void OnDestroy()
    {
        if (container != null)
        {
            Destroy(container);
        }
    }
}
