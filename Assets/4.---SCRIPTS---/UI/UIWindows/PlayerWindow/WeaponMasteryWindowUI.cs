using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WeaponMasteryWindowUI : MonoBehaviour
{
    [Header("Character Page")]
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject statsPanel;

    [Header("Tabs")]
    [SerializeField] private Button characterTab;
    [SerializeField] private Button masteryTab;
    [SerializeField] private GameObject characterTabSelected;
    [SerializeField] private GameObject masteryTabSelected;

    [Header("Weapon Families")]
    [SerializeField] private GameObject masteryPage;
    [SerializeField] private RectTransform familyContent;
    [SerializeField] private WeaponMasteryFamilyRowUI familyRowPrefab;
    [SerializeField] private WeaponFamilyDefinition[] weaponFamilies;

    [Header("Weapon Details")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TMP_Text detailsTitle;
    [SerializeField] private TMP_Text detailsDamageBonus;
    [SerializeField] private TMP_Text detailsLevel;
    [SerializeField] private TMP_Text detailsExperience;
    [SerializeField] private Image detailsFill;
    [SerializeField] private Toggle trackToggle;

    private readonly Dictionary<WeaponFamilyDefinition, WeaponMasteryFamilyRowUI>
        rows = new();

    private PlayerWeaponMastery mastery;
    private WeaponFamilyDefinition selectedFamily;
    private bool showingMastery;
    private bool equipmentWasActive;
    private bool statsWereActive;

    private void Awake()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError(
                "Assign the Weapon Mastery window references in the Inspector.",
                this);

            enabled = false;
            return;
        }

        characterTab.onClick.AddListener(ShowCharacter);
        masteryTab.onClick.AddListener(ShowMastery);
        trackToggle.onValueChanged.AddListener(OnTrackChanged);

        BuildRows();

        detailsPanel.SetActive(false);
        masteryPage.SetActive(false);
        RefreshTabVisuals();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        PlayerStats player = PlayerReference.Player;

        PlayerWeaponMastery next = player != null
            ? player.GetComponent<PlayerWeaponMastery>()
            : null;

        if (ReferenceEquals(next, mastery))
            return;

        UnbindMastery();
        mastery = next;

        if (mastery != null)
            mastery.Changed += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        UnbindMastery();
    }

    private void OnDestroy()
    {
        if (characterTab != null)
            characterTab.onClick.RemoveListener(ShowCharacter);

        if (masteryTab != null)
            masteryTab.onClick.RemoveListener(ShowMastery);

        if (trackToggle != null)
            trackToggle.onValueChanged.RemoveListener(OnTrackChanged);
    }

    private bool HasRequiredReferences()
    {
        return equipmentPanel != null &&
               statsPanel != null &&
               characterTab != null &&
               masteryTab != null &&
               masteryPage != null &&
               familyContent != null &&
               familyRowPrefab != null &&
               detailsPanel != null &&
               detailsTitle != null &&
               detailsDamageBonus != null &&
               detailsLevel != null &&
               detailsExperience != null &&
               detailsFill != null &&
               trackToggle != null;
    }

    private void UnbindMastery()
    {
        if (mastery != null)
            mastery.Changed -= Refresh;

        mastery = null;
    }

    private void BuildRows()
    {
        if (weaponFamilies == null)
            return;

        foreach (WeaponFamilyDefinition family in weaponFamilies)
        {
            if (family == null || rows.ContainsKey(family))
                continue;

            WeaponMasteryFamilyRowUI row =
                Instantiate(familyRowPrefab, familyContent, false);

            row.Bind(family, SelectFamily);
            rows.Add(family, row);
        }
    }

    private void ShowCharacter()
    {
        if (!showingMastery)
            return;

        showingMastery = false;
        masteryPage.SetActive(false);
        equipmentPanel.SetActive(equipmentWasActive);
        statsPanel.SetActive(statsWereActive);
        RefreshTabVisuals();
    }

    private void ShowMastery()
    {
        if (showingMastery)
            return;

        equipmentWasActive = equipmentPanel.activeSelf;
        statsWereActive = statsPanel.activeSelf;

        showingMastery = true;
        equipmentPanel.SetActive(false);
        statsPanel.SetActive(false);
        masteryPage.SetActive(true);

        RefreshTabVisuals();
        Refresh();
    }

    private void RefreshTabVisuals()
    {
        if (characterTabSelected != null)
            characterTabSelected.SetActive(!showingMastery);

        if (masteryTabSelected != null)
            masteryTabSelected.SetActive(showingMastery);
    }

    private void SelectFamily(WeaponFamilyDefinition family)
    {
        selectedFamily = family;
        Refresh();
    }

    private void OnTrackChanged(bool isTracked)
    {
        if (mastery == null || selectedFamily == null)
            return;

        if (isTracked)
            mastery.SetTrackedFamily(selectedFamily);
        else if (mastery.TrackedFamily == selectedFamily)
            mastery.SetTrackedFamily(null);
    }

    private void Refresh()
    {
        if (!showingMastery)
            return;

        foreach (var entry in rows)
        {
            entry.Value.Refresh(
                GetProgress(entry.Key),
                entry.Key == selectedFamily);
        }

        detailsPanel.SetActive(selectedFamily != null);

        if (selectedFamily == null)
            return;

        PlayerWeaponMastery.Progress progress = GetProgress(selectedFamily);

        detailsTitle.text = selectedFamily.DisplayName;
        detailsLevel.text = progress.Level.ToString();
        detailsDamageBonus.text =
            $"+{progress.WeaponDamageBonus * 100f:0}%";

        detailsExperience.text = progress.IsMaxLevel
            ? "MAX"
            : $"{progress.ExperienceInLevel}/{progress.ExperienceForNextLevel} XP";

        detailsFill.fillAmount = Mathf.Clamp01(progress.Fraction);

        trackToggle.interactable = mastery != null;
        trackToggle.SetIsOnWithoutNotify(
            mastery != null && mastery.TrackedFamily == selectedFamily);
    }

    private PlayerWeaponMastery.Progress GetProgress(WeaponFamilyDefinition family)
    {
        return mastery != null
            ? mastery.GetProgress(family)
            : new PlayerWeaponMastery.Progress(
                1, 0, family.GetExperienceRequiredForNextLevel(1));
    }
}