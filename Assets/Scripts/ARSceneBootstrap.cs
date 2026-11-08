using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Автоматическая настройка сцены при запуске: подключает ARInfoPanel,
/// очищает сцену от объектов шаблона, создаёт UI-карточку объекта и
/// настраивает ImageTracker. Не требует ручной настройки сцены в редакторе.
/// </summary>
public static class ARSceneBootstrap
{
    private static bool _done;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSetup()
    {
        if (_done) return;
        _done = true;

        // ---------- 1. Чистим сцену от остатков шаблона ----------
        var spawner = FindByName("Object Spawner");
        if (spawner != null) Object.Destroy(spawner);

        // Отладочные тексты: все Text (TMP), не являющиеся подписью кнопки
        var texts = Object.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in texts)
        {
            if (tmp.name == "Text (TMP)" && tmp.GetComponentInParent<Button>() == null)
                Object.Destroy(tmp.gameObject);
        }

        var raycastManager = Object.FindFirstObjectByType<ARRaycastManager>(FindObjectsInactive.Include);
        if (raycastManager != null) Object.Destroy(raycastManager);

        // ---------- 2. Находим ключевые компоненты ----------
        var tracker = Object.FindFirstObjectByType<ImageTracker>(FindObjectsInactive.Include);
        var modelCtrl = Object.FindFirstObjectByType<ModelController>(FindObjectsInactive.Include);
        if (tracker == null)
        {
            Debug.LogError("ARSceneBootstrap: ImageTracker не найден в сцене");
            return;
        }
        if (modelCtrl == null)
        {
            Debug.LogError("ARSceneBootstrap: ModelController не найден в сцене");
            return;
        }

        // ---------- 3. Отключаем авто-спавн префаба на менеджере ----------
        // (иначе менеджер сам создаст модель и получится дубликат)
        var imgManager = tracker.GetComponent<ARTrackedImageManager>();
        if (imgManager == null)
            imgManager = Object.FindFirstObjectByType<ARTrackedImageManager>(FindObjectsInactive.Include);
        if (imgManager != null)
        {
            imgManager.trackedImagePrefab = null;
            tracker.trackedImageManager = imgManager;
        }

        // ---------- 4. Создаём карточку объекта на BackgroundPanel ----------
        var canvasGO = FindByName("Canvas");
        var bg = canvasGO != null
            ? canvasGO.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "BackgroundPanel")?.gameObject
            : null;
        if (bg == null)
        {
            Debug.LogError("ARSceneBootstrap: BackgroundPanel не найден");
            return;
        }

        // Для работы кнопок Canvas должен иметь GraphicRaycaster
        if (canvasGO.GetComponent<GraphicRaycaster>() == null)
            canvasGO.AddComponent<GraphicRaycaster>();
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
        {
            var es = new GameObject("EventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(canvasGO.transform, false);
        }

        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        var title = CreateText(bg.transform, "Title", font, 28, FontStyles.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -16);
        titleRect.sizeDelta = new Vector2(-72, 40); // место справа под кнопку «✕»

        var desc = CreateText(bg.transform, "Description", font, 18, FontStyles.Normal);
        desc.textWrappingMode = TextWrappingModes.Normal;
        var descRect = desc.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(16, 16);
        descRect.offsetMax = new Vector2(-16, -56);
        descRect.sizeDelta = Vector2.zero;

        // ---------- 5. Настраиваем ARInfoPanel ----------
        var panel = bg.GetComponent<ARInfoPanel>();
        if (panel == null) panel = bg.AddComponent<ARInfoPanel>();
        panel.panelRoot = bg;
        panel.titleText = title;
        panel.descriptionText = desc;
        panel.modelController = modelCtrl;

        // Кнопка «Закрыть» в правом верхнем углу панели
        var closeBtn = CreateCloseButton(bg.transform, font);
        closeBtn.onClick.AddListener(panel.OnCloseButtonPressed);

        // ---------- 6. Настраиваем ImageTracker ----------
        tracker.modelController = modelCtrl;
        tracker.infoPanel = panel;
        tracker.trackedObjects = new List<TrackedObject>
        {
            new TrackedObject
            {
                imageName = "Wright Flyer 1903",
                prefab = Resources.Load<GameObject>("WrightFlyer_Anchor"),
                title = "Флайер Райт (1903)",
                description = "Первый в истории управляемый самолёт с двигателем. " +
                              "17 декабря 1903 года братья Орвилл и Уилбур Райт совершили " +
                              "на нём полёт длиной 37 метров. Размах крыльев — 12,3 м, " +
                              "масса — 274 кг, двигатель — 12 л.с."
            }
        };
        if (tracker.trackedObjects[0].prefab == null)
            Debug.LogError("ARSceneBootstrap: префаб WrightFlyer_Anchor не найден в Resources");

        // ---------- 7. Переподключаем ResetButton на панель ----------
        var resetGO = FindByName("ResetButton");
        if (resetGO != null && resetGO.TryGetComponent(out Button resetBtn))
        {
            resetBtn.onClick.RemoveAllListeners();
            resetBtn.onClick.AddListener(panel.OnResetButtonPressed);
        }

        Debug.Log("✅ ARSceneBootstrap: сцена настроена");
    }

    private static GameObject FindByName(string name)
    {
        return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == name)?.gameObject;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name,
        TMP_FontAsset font, float size, FontStyles style)
    {
        var old = parent.Find(name);
        if (old != null) Object.Destroy(old.gameObject);

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        return tmp;
    }

    private static Button CreateCloseButton(Transform parent, TMP_FontAsset font)
    {
        var go = new GameObject("CloseButton", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-8, -8);
        rect.sizeDelta = new Vector2(44, 44);

        var image = go.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.35f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = image;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        var label = labelGO.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = "✕";
        label.fontSize = 22;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        return btn;
    }
}
