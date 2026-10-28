
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class DeedCategoryButtonUI :
    MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;

    [Header("Selection")]
    [SerializeField]
    private Color normalColor =
        new Color(0.16f, 0.16f, 0.16f, 0.95f);

    [SerializeField]
    private Color selectedColor =
        new Color(0.37f, 0.30f, 0.20f, 1f);

    private Button button;
    private Action clicked;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClicked);
    }

    public void Bind(
        string displayName,
        int deedCount,
        Sprite icon,
        bool selected,
        Action onClicked)
    {
        clicked = onClicked;

        if (nameText != null)
        {
            nameText.text =
                $"{displayName} ({deedCount})";
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(
                icon != null
            );
        }

        if (backgroundImage != null)
        {
            backgroundImage.color =
                selected
                    ? selectedColor
                    : normalColor;
        }
    }

    private void HandleClicked()
    {
        clicked?.Invoke();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleClicked
            );
        }

        clicked = null;
    }
}
