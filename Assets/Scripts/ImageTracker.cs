using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTracker : MonoBehaviour
{
    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private GameObject infoCanvas; // ← добавили ссылку на Canvas
    
    private GameObject spawnedModel;

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void Start()
    {
        // Скрываем UI при старте
        if (infoCanvas != null)
            infoCanvas.SetActive(false);
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
            OnImageFound(trackedImage);
        
        foreach (var trackedImage in eventArgs.updated)
            OnImageUpdated(trackedImage);
        
        foreach (var trackedImage in eventArgs.removed)
            OnImageLost(trackedImage);
    }

    private void OnImageFound(ARTrackedImage trackedImage)
    {
        if (trackedImage.trackingState == TrackingState.Tracking && spawnedModel == null)
        {
            spawnedModel = Instantiate(modelPrefab);
            spawnedModel.transform.SetParent(trackedImage.transform, false);
            spawnedModel.SetActive(true);
            
            // Показываем UI
            if (infoCanvas != null)
                infoCanvas.SetActive(true);
                
            Debug.Log("✅ Модель появилась!");
        }
    }

    private void OnImageUpdated(ARTrackedImage trackedImage)
    {
        if (spawnedModel != null)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                spawnedModel.SetActive(true);
                if (infoCanvas != null)
                    infoCanvas.SetActive(true);
            }
            else if (trackedImage.trackingState == TrackingState.Limited)
            {
                spawnedModel.SetActive(false);
                if (infoCanvas != null)
                    infoCanvas.SetActive(false);
            }
        }
    }

    private void OnImageLost(ARTrackedImage trackedImage)
    {
        if (spawnedModel != null)
        {
            Destroy(spawnedModel);
            spawnedModel = null;
        }
        // Скрываем UI
        if (infoCanvas != null)
            infoCanvas.SetActive(false);
    }
}