using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class PlayerStatListPanelUI :
    MonoBehaviour
{
    private enum FilterOption
    {
        All,
        Offensive,
        Defensive,
        Primary,
        Secondary,
        Utility
    }

    [Header("Filter")]

    [SerializeField]
    private TMP_Dropdown categoryDropdown;

    [Header("Content")]

    [SerializeField]
    private Transform content;

    [SerializeField]
    private PlayerStatRowUI rowPrefab;

    [Header("Display")]

    [SerializeField]
    [Tooltip(
        "Primary visar endast primary stats. " +
        "Stäng av detta senare om panelen även ska kunna " +
        "visa derived stats."
    )]
    private bool primaryStatsOnly =
        true;

    private readonly List<PlayerStatRowUI>
        rows =
            new();

    private CharacterStats stats;

    private FilterOption selectedFilter =
        FilterOption.All;

    private void Awake()
    {
        ConfigureDropdown();
    }

    private void OnDestroy()
    {
        if (categoryDropdown != null)
        {
            categoryDropdown.onValueChanged
                .RemoveListener(
                    HandleDropdownChanged);
        }
    }

    public void Bind(
        CharacterStats characterStats)
    {
        stats =
            characterStats;

        Rebuild();
    }

    public void Refresh()
    {
        foreach (PlayerStatRowUI row
                 in rows)
        {
            row?.Refresh();
        }
    }

    private void ConfigureDropdown()
    {
        if (categoryDropdown == null)
            return;

        categoryDropdown.onValueChanged
            .RemoveListener(
                HandleDropdownChanged);

        categoryDropdown.ClearOptions();

        List<string> options =
            new();

        foreach (FilterOption option
                 in Enum.GetValues(
                     typeof(FilterOption)))
        {
            options.Add(
                option.ToString());
        }

        categoryDropdown.AddOptions(
            options);

        categoryDropdown.value =
            (int)selectedFilter;

        categoryDropdown.RefreshShownValue();

        categoryDropdown.onValueChanged
            .AddListener(
                HandleDropdownChanged);
    }

    private void HandleDropdownChanged(
        int value)
    {
        selectedFilter =
            Enum.IsDefined(
                typeof(FilterOption),
                value)
                ? (FilterOption)value
                : FilterOption.All;

        Rebuild();
    }

    private void Rebuild()
    {
        ClearRows();

        if (stats == null ||
            content == null ||
            rowPrefab == null)
        {
            return;
        }

        StatDatabase database =
            StatDatabase.Instance;

        if (database == null)
            return;

        List<StatDefinition> definitions =
            new();

        foreach (StatDefinition definition
                 in database.Stats)
        {
            if (!ShouldShow(
                    definition))
            {
                continue;
            }

            definitions.Add(
                definition);
        }

        definitions.Sort(
            CompareDefinitions);

        foreach (StatDefinition definition
                 in definitions)
        {
            PlayerStatRowUI row =
                Instantiate(
                    rowPrefab,
                    content);

            row.Bind(
                stats,
                definition);

            rows.Add(
                row);
        }
    }

    private bool ShouldShow(
        StatDefinition definition)
    {
        if (definition == null ||
            !definition.visible ||
            !definition.ShowInPlayerWindow)
        {
            return false;
        }

        if (primaryStatsOnly &&
            definition.kind !=
                StatKind.Primary)
        {
            return false;
        }

        if (selectedFilter ==
            FilterOption.All)
        {
            return true;
        }

        StatCategory category =
            ConvertFilterToCategory(
                selectedFilter);

        return definition.HasCategory(
            category);
    }

    private static StatCategory
        ConvertFilterToCategory(
            FilterOption filter)
    {
        return filter switch
        {
            FilterOption.Offensive =>
                StatCategory.Offensive,

            FilterOption.Defensive =>
                StatCategory.Defensive,

            FilterOption.Primary =>
                StatCategory.Primary,

            FilterOption.Utility =>
                StatCategory.Utility,

            FilterOption.Secondary =>
                StatCategory.Secondary,

            _ =>
                StatCategory.Offensive
        };
    }

    private static int CompareDefinitions(
        StatDefinition first,
        StatDefinition second)
    {
        if (ReferenceEquals(
                first,
                second))
        {
            return 0;
        }

        if (first == null)
            return 1;

        if (second == null)
            return -1;

        int orderComparison =
            first.DisplayOrder.CompareTo(
                second.DisplayOrder);

        if (orderComparison != 0)
            return orderComparison;

        return string.Compare(
            first.DisplayName,
            second.DisplayName,
            StringComparison
                .OrdinalIgnoreCase);
    }

    private void ClearRows()
    {
        foreach (PlayerStatRowUI row
                 in rows)
        {
            if (row != null)
            {
                Destroy(
                    row.gameObject);
            }
        }

        rows.Clear();
    }
}
