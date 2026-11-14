using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Отладочная строка поверх AR-сцены. Показывает тип графики, активный
/// рендер-пайплайн, число полученных кадров камеры и последние события
/// трекинга. Нужна для диагностики на устройстве.
/// </summary>
public class ARDebugHUD : MonoBehaviour
{
    public static ARDebugHUD Instance { get; private set; }

    private TMP_Text label;
    private ARCameraManager cameraManager;
    private int frameCount;
    private string lastEvent = "ожидание маркера…";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        var cam = Camera.main;
        if (cam != null)
            cameraManager = cam.GetComponent<ARCameraManager>();
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<ARCameraManager>(FindObjectsInactive.Include);

        if (cameraManager != null)
            cameraManager.frameReceived += OnFrameReceived;
        else
            lastEvent = "⚠️ ARCameraManager НЕ НАЙДЕН";

        var rp = GraphicsSettings.currentRenderPipeline;
        Debug.Log($"[HUD] GPU: {SystemInfo.graphicsDeviceType}, пайплайн: {(rp ? rp.name : "NONE")}, " +
                  $"ARCameraManager: {(cameraManager != null)}, background: {(cam != null && cam.GetComponent<ARCameraBackground>() != null)}");
    }

    private void OnDestroy()
    {
        if (cameraManager != null)
            cameraManager.frameReceived -= OnFrameReceived;
        if (Instance == this) Instance = null;
    }

    private void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        frameCount++;
    }

    public static void Log(string message)
    {
        if (Instance != null)
            Instance.lastEvent = message;
    }

    private void Update()
    {
        if (label == null) return;
        var rp = GraphicsSettings.currentRenderPipeline;
        label.text =
            $"GPU: {SystemInfo.graphicsDeviceType}\n" +
            $"Пайплайн: {(rp ? rp.name : "NONE")}\n" +
            $"Кадров камеры: {frameCount}\n" +
            lastEvent;
    }

    public static ARDebugHUD Create(Canvas canvas, TMP_FontAsset font)
    {
        var go = new GameObject("ARDebugHUD", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(8, -8);
        rect.sizeDelta = new Vector2(620, 100);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 14;
        tmp.color = new Color(1f, 0.9f, 0.2f);
        tmp.alignment = TextAlignmentOptions.TopLeft;

        var hud = go.AddComponent<ARDebugHUD>();
        hud.label = tmp;
        return hud;
    }
}
