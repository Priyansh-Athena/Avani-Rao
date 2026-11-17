using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Two trigger pads for Cardboard phone testing. Enabled only in the touch-test APK.
/// Each half of the stereo display has both pads, so either thumb can reach them.
/// </summary>
public sealed class MobileTriggerSimulator : MonoBehaviour
{
    private static MobileTriggerSimulator instance;
    private GameObject overlay;
    private static int shortPressFrame = -1;
    private static int longPressFrame = -1;
    private const float PadHeightFraction = 0.16f;

    public static bool PressedThisFrame =>
        shortPressFrame == Time.frameCount || longPressFrame == Time.frameCount;

    public static bool ShortPressedThisFrame => shortPressFrame == Time.frameCount;
    public static bool LongPressedThisFrame => longPressFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        shortPressFrame = -1;
        longPressFrame = -1;
#if ENABLE_PHONE_TRIGGER_SIMULATOR
        if (instance != null) return;

        var go = new GameObject("Mobile Trigger Simulator");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MobileTriggerSimulator>();
#endif
    }

    private void Awake()
    {
        CreateOverlay();
    }

    private void Update()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
                RegisterTouch(touch.position);
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            RegisterTouch(Input.mousePosition);
#endif
    }

    private static void RegisterTouch(Vector2 point)
    {
        if (point.y > Screen.height * PadHeightFraction) return;

        // Left and right eyes each occupy half the display.
        // The left quarter of each eye is Button 0; the right quarter is Button 14.
        float positionInEye = (point.x / Screen.width * 2f) % 1f;
        if (positionInEye < 0.5f)
            shortPressFrame = Time.frameCount + 1;
        else
            longPressFrame = Time.frameCount + 1;
    }

    private void CreateOverlay()
    {
        var canvasObject = new GameObject("Development Trigger Pads", typeof(RectTransform), typeof(Canvas));
        // Screen-space Overlay canvases must stay at the hierarchy root.
        overlay = canvasObject;
        DontDestroyOnLoad(canvasObject);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        for (int i = 0; i < 4; i++)
        {
            var pad = new GameObject("Trigger " + (i % 2 == 0 ? "0" : "14"),
                typeof(RectTransform), typeof(Image));
            pad.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = pad.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(i * 0.25f, 0);
            rect.anchorMax = new Vector2((i + 1) * 0.25f, PadHeightFraction);
            rect.offsetMin = new Vector2(8, 6);
            rect.offsetMax = new Vector2(-8, -6);
            pad.GetComponent<Image>().color = i % 2 == 0
                ? new Color(0.06f, 0.42f, 0.5f, 0.82f)
                : new Color(0.45f, 0.22f, 0.12f, 0.82f);

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(pad.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.resizeTextForBestFit = true;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = i % 2 == 0 ? "TRIGGER 0" : "TRIGGER 14";
        }
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
        if (instance == this) instance = null;
    }
}