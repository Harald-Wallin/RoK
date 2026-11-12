using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class WeaponMasteryFamilyRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image experienceFill;
    [SerializeField] private GameObject selectedVisual;

    private Button button;
    private WeaponFamilyDefinition family;
    private Action<WeaponFamilyDefinition> onSelected;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(
        WeaponFamilyDefinition definition,
        Action<WeaponFamilyDefinition> selectFamily)
    {
        family = definition;
        onSelected = selectFamily;
    }

    public void Refresh(PlayerWeaponMastery.Progress progress, bool selected)
    {
        titleText.text = family.DisplayName;

        if (levelText != null)
            levelText.text = progress.Level.ToString();

        experienceFill.fillAmount = Mathf.Clamp01(progress.Fraction);

        if (selectedVisual != null)
            selectedVisual.SetActive(selected);
    }

    private void HandleClick()
    {
        if (family != null)
            onSelected?.Invoke(family);
    }
}
