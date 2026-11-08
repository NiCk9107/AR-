using TMPro;
using UnityEngine;

/// <summary>
/// Информационная карточка объекта реконструкции.
/// Показывает название и историческую справку, содержит кнопку
/// сброса позы модели и кнопку закрытия панели.
/// </summary>
public class ARInfoPanel : MonoBehaviour
{
    [Header("Ссылки на UI (Canvas)")]
    public GameObject panelRoot;      // фоновая панель карточки
    public TMP_Text titleText;        // заголовок объекта
    public TMP_Text descriptionText;  // историческая справка

    [Header("Логика")]
    public ModelController modelController;

    private bool manuallyHidden;

    private void Start()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>Показывает карточку с данными объекта (вызывается ImageTracker).</summary>
    public void ShowObjectInfo(string title, string description)
    {
        manuallyHidden = false;

        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>Возвращает панель, если пользователь её не закрывал вручную.</summary>
    public void RestoreVisibility()
    {
        if (panelRoot != null && !manuallyHidden)
            panelRoot.SetActive(true);
    }

    /// <summary>Скрывает панель (потеря маркера).</summary>
    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>Кнопка «Закрыть» — скрывает панель до смены объекта.</summary>
    public void OnCloseButtonPressed()
    {
        manuallyHidden = true;
        HidePanel();
    }

    /// <summary>Кнопка «Сбросить» — возвращает модель в исходную позу.</summary>
    public void OnResetButtonPressed()
    {
        if (modelController != null)
            modelController.ResetModel();
    }
}
