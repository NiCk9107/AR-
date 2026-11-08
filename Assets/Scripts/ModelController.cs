using UnityEngine;

/// <summary>
/// Управление 3D-моделью жестами.
/// Модель остаётся дочерним объектом отслеживаемого маркера, поэтому
/// она всегда «приклеена» к маркеру, а сброс возвращает её в исходную позу.
///
/// Жесты:
///   1 палец, перетаскивание по горизонтали — вращение вокруг вертикальной оси;
///   щипок (2 пальца сближаются/расходятся) — масштабирование;
///   2 пальца, перетаскивание — перенос модели по плоскости маркера.
/// </summary>
public class ModelController : MonoBehaviour
{
    [Header("Настройки жестов")]
    [SerializeField] private float rotationSpeed = 0.25f;   // градусы на пиксель
    [SerializeField] private float moveSpeed = 0.0015f;    // м/пиксель
    [SerializeField] private float minScale = 0.2f;
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float maxOffset = 0.5f;     // максимальное смещение от маркера (м)

    private GameObject currentModel;

    // Исходная локальная поза модели относительно маркера (для сброса)
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private bool hasModel;

    // Состояние жестов двумя пальцами
    private float previousPinchDistance;
    private Vector2 previousMidPoint;

    /// <summary>Вызывается ImageTracker при появлении модели.</summary>
    public void SetModel(GameObject model)
    {
        currentModel = model;
        originalLocalPosition = model.transform.localPosition;
        originalLocalRotation = model.transform.localRotation;
        originalLocalScale = model.transform.localScale;
        hasModel = true;
    }

    /// <summary>Вызывается ImageTracker при потере маркера.</summary>
    public void ClearModel()
    {
        currentModel = null;
        hasModel = false;
    }

    /// <summary>Возвращает модель в исходную позу (кнопка «Сбросить»).</summary>
    public void ResetModel()
    {
        if (!hasModel || currentModel == null) return;

        currentModel.transform.localPosition = originalLocalPosition;
        currentModel.transform.localRotation = originalLocalRotation;
        currentModel.transform.localScale = originalLocalScale;

        Debug.Log("✅ Модель возвращена в исходное положение");
    }

    private void Update()
    {
        if (!hasModel || currentModel == null) return;

        switch (Input.touchCount)
        {
            case 1:
                RotateByDrag();
                break;
            case 2:
                // Одновременно обрабатываем и щипок, и перенос
                HandlePinch();
                HandleTwoFingerPan();
                break;
        }
    }

    /// <summary>Вращение за один палец — горизонтальное перетаскивание.</summary>
    private void RotateByDrag()
    {
        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Moved) return;

        currentModel.transform.Rotate(
            Vector3.up,
            -touch.deltaPosition.x * rotationSpeed,
            Space.Self // вокруг локальной оси маркера
        );
    }

    /// <summary>Масштабирование щипком.</summary>
    private void HandlePinch()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        float currentDistance = Vector2.Distance(t0.position, t1.position);

        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            previousPinchDistance = currentDistance;
            return;
        }

        if (previousPinchDistance <= 0f) return;

        float ratio = currentDistance / previousPinchDistance;
        Vector3 scale = currentModel.transform.localScale * ratio;
        scale = Vector3.ClampMagnitude(scale, maxScale);
        scale = Vector3.Max(scale, originalLocalScale * minScale);

        currentModel.transform.localScale = scale;
        previousPinchDistance = currentDistance;
    }

    /// <summary>Перенос модели перетаскиванием двумя пальцами.</summary>
    private void HandleTwoFingerPan()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        Vector2 midPoint = (t0.position + t1.position) * 0.5f;

        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            previousMidPoint = midPoint;
            return;
        }

        if (t0.phase != TouchPhase.Moved && t1.phase != TouchPhase.Moved) return;

        Vector2 screenDelta = midPoint - previousMidPoint;

        // Переводим смещение по экрану в смещение по плоскости маркера
        Transform anchor = currentModel.transform.parent;
        Vector3 worldDelta =
            anchor.right * (screenDelta.x * moveSpeed) +
            anchor.up * (screenDelta.y * moveSpeed);

        Vector3 newLocalPos = currentModel.transform.localPosition +
                              anchor.InverseTransformDirection(worldDelta);

        // Не даём утащить модель слишком далеко от маркера
        newLocalPos = Vector3.ClampMagnitude(newLocalPos, maxOffset);

        currentModel.transform.localPosition = newLocalPos;
        previousMidPoint = midPoint;
    }
}
