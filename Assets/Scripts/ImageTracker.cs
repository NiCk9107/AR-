using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Данные об одном отслеживаемом объекте реконструкции.
/// Имя маркера должно совпадать с именем изображения в Reference Image Library.
/// </summary>
[Serializable]
public class TrackedObject
{
    public string imageName;                 // имя изображения в библиотеке маркеров
    public GameObject prefab;                // префаб 3D-модели
    public string title;                     // заголовок карточки
    [TextArea(3, 6)] public string description; // историческая справка
}

/// <summary>
/// Отслеживает маркеры изображений через ARTrackedImageManager,
/// создаёт соответствующую 3D-модель поверх маркера и показывает
/// информационную карточку объекта.
/// Поддерживает несколько маркеров одновременно.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTracker : MonoBehaviour
{
    [Header("Настройки")]
    public List<TrackedObject> trackedObjects = new();
    public ModelController modelController;
    public ARInfoPanel infoPanel;
    public ARTrackedImageManager trackedImageManager;

    // Активные модели: маркер → созданный объект
    private readonly Dictionary<ARTrackedImage, GameObject> spawnedModels = new();

    private void Awake()
    {
        if (trackedImageManager == null)
            trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        // AR Foundation 6: событие изменений трекейблов — UnityEvent
        trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
            OnImageFound(trackedImage);

        foreach (var trackedImage in eventArgs.updated)
            OnImageUpdated(trackedImage);

        // удалённые трекейблы приходят как пары (TrackableId, трекейбл)
        foreach (var removedPair in eventArgs.removed)
            OnImageLost(removedPair.Value);
    }

    private void OnImageFound(ARTrackedImage trackedImage)
    {
        TrackedObject entry = FindEntry(trackedImage);
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"⚠️ Маркер «{trackedImage.referenceImage.name}» не найден в списке trackedObjects");
            return;
        }

        GameObject spawnedModel = Instantiate(entry.prefab, trackedImage.transform, false);
        spawnedModel.SetActive(true);
        spawnedModels[trackedImage] = spawnedModel;

        if (modelController != null)
            modelController.SetModel(spawnedModel);

        if (infoPanel != null)
            infoPanel.ShowObjectInfo(entry.title, entry.description);

        Debug.Log($"✅ Показан объект: {entry.title}");
    }

    private void OnImageUpdated(ARTrackedImage trackedImage)
    {
        if (!spawnedModels.TryGetValue(trackedImage, out GameObject spawnedModel)) return;

        bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
        spawnedModel.SetActive(isTracking);

        if (infoPanel != null)
        {
            if (isTracking) infoPanel.RestoreVisibility();
            else infoPanel.HidePanel();
        }
    }

    private void OnImageLost(ARTrackedImage trackedImage)
    {
        if (spawnedModels.TryGetValue(trackedImage, out GameObject spawnedModel))
        {
            if (modelController != null)
                modelController.ClearModel();

            Destroy(spawnedModel);
            spawnedModels.Remove(trackedImage);
        }

        if (infoPanel != null)
            infoPanel.HidePanel();
    }

    /// <summary>
    /// Ищет данные объекта по имени маркера.
    /// Если в списке ровно один объект — используем его для любого маркера
    /// (удобно на этапе прототипа с одним маркером).
    /// </summary>
    private TrackedObject FindEntry(ARTrackedImage trackedImage)
    {
        if (trackedObjects.Count == 1)
            return trackedObjects[0];

        string name = trackedImage.referenceImage.name;
        return trackedObjects.Find(o => o.imageName == name);
    }
}
