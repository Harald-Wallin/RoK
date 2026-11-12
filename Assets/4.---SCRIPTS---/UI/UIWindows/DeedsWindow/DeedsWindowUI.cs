
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DeedsWindowUI :
    MonoBehaviour,
    IUIWindow
{
    private enum CategoryFilter
    {
        All,
        Tracked,
        Completed,
        Category
    }

    private readonly struct CardEntry
    {
        public readonly DeedRuntime Deed;

        // Null = current rank.
        // Non-null = specific completed rank.
        public readonly DeedRankDefinition Rank;

        public CardEntry(
            DeedRuntime deed,
            DeedRankDefinition rank)
        {
            Deed = deed;
            Rank = rank;
        }
    }

    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("Window")]
    [SerializeField]
    private GameObject mainWindowRoot;

    [SerializeField]
    private Button closeButton;

    [Header("Categories")]
    [SerializeField]
    private Transform categoryContent;

    [SerializeField]
    private DeedCategoryButtonUI categoryButtonPrefab;

    [Header("Deed Cards")]
    [SerializeField]
    private Transform deedContent;

    [SerializeField]
    private DeedCardUI deedCardPrefab;

    [SerializeField]
    private TMP_Text emptyText;

    [Header("Details")]
    [SerializeField]
    private DeedDetailsWindowUI detailsWindow;

    // =====================================================
    // RUNTIME
    // =====================================================

    private readonly List<DeedCategoryButtonUI>
        categoryButtons = new();

    private readonly List<DeedCardUI>
        deedCards = new();

    private PlayerDeedManager manager;

    private CategoryFilter selectedFilter =
        CategoryFilter.All;

    private DeedCategoryDefinition selectedCategory;

    private bool subscribed;

    // =====================================================
    // PUBLIC API
    // =====================================================

    public bool IsOpen =>
        mainWindowRoot != null &&
        mainWindowRoot.activeSelf;

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (IsOpen)
            return;

        ResolveManager();

        if (manager == null)
        {
            Debug.LogWarning(
                "DeedsWindowUI: PlayerDeedManager missing.",
                this
            );

            return;
        }

        if (!ValidateReferences())
            return;

        Subscribe();

        mainWindowRoot.SetActive(true);

        detailsWindow.Close();

        RebuildCategories();
        RebuildCards();
    }

    public void OpenDeed(DeedRuntime deed, DeedRankDefinition rank)
    {
        if (deed == null || !deed.ShouldShowCard || !deed.IsRevealed)
            return;

        Open();

        if (!IsOpen)
            return;

        if (deed.Definition.Category != null)
            SelectCategory(deed.Definition.Category);
        else
            SelectAll();

        detailsWindow.Show(deed, manager, rank);

        foreach (DeedCardUI card in deedCards)
        {
            if (card != null && card.Represents(deed))
            {
                CenterDeedCard(card);
                break;
            }
        }
    }

    private void CenterDeedCard(DeedCardUI card)
    {
        ScrollRect scroll = deedContent.GetComponentInParent<ScrollRect>();

        if (scroll == null || !scroll.vertical || scroll.content == null ||
            card.transform is not RectTransform cardRect)
        {
            return;
        }

        RectTransform viewport = scroll.viewport != null
            ? scroll.viewport
            : scroll.transform as RectTransform;

        if (viewport == null)
            return;

        Canvas.ForceUpdateCanvases();

        Bounds contentBounds = RectTransformUtility
            .CalculateRelativeRectTransformBounds(viewport, scroll.content);

        float scrollableHeight = contentBounds.size.y - viewport.rect.height;

        if (scrollableHeight <= 0f)
            return;

        Vector3 cardCenter = viewport.InverseTransformPoint(
            cardRect.TransformPoint(cardRect.rect.center));

        float distanceFromCenter = cardCenter.y - viewport.rect.center.y;

        scroll.StopMovement();
        scroll.verticalNormalizedPosition = Mathf.Clamp01(
            scroll.verticalNormalizedPosition + distanceFromCenter / scrollableHeight);
    }

    public void Close()
    {
        detailsWindow?.Close();

        if (mainWindowRoot != null)
            mainWindowRoot.SetActive(false);

        Unsubscribe();
    }

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

        if (mainWindowRoot != null)
            mainWindowRoot.SetActive(false);

        detailsWindow?.Close();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );
        }

        ClearCategoryButtons();
        ClearDeedCards();
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private bool ValidateReferences()
    {
        if (mainWindowRoot != null &&
            categoryContent != null &&
            categoryButtonPrefab != null &&
            deedContent != null &&
            deedCardPrefab != null &&
            detailsWindow != null)
        {
            return true;
        }

        Debug.LogError(
            "DeedsWindowUI: Missing Inspector references.",
            this
        );

        return false;
    }

    private void ResolveManager()
    {
        if (manager != null)
            return;

        PlayerStats player =
            PlayerReference.Player;

        if (player != null)
        {
            manager =
                player.GetComponent<PlayerDeedManager>();
        }

        if (manager == null)
        {
            manager =
                FindFirstObjectByType<PlayerDeedManager>();
        }
    }

    // =====================================================
    // EVENTS
    // =====================================================

    private void Subscribe()
    {
        if (subscribed || manager == null)
            return;

        manager.DeedProgressChanged +=
            HandleProgressChanged;

        manager.DeedDiscovered +=
            HandleDiscoveryChanged;

        manager.DeedTrackingChanged +=
            HandleTrackingChanged;

        manager.DeedRankCompleted +=
            HandleRankCompleted;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || manager == null)
            return;

        manager.DeedProgressChanged -=
            HandleProgressChanged;

        manager.DeedDiscovered -=
            HandleDiscoveryChanged;

        manager.DeedTrackingChanged -=
            HandleTrackingChanged;

        manager.DeedRankCompleted -=
            HandleRankCompleted;

        subscribed = false;
    }

    private void HandleProgressChanged(
        DeedRuntime deed)
    {
        if (!IsOpen)
            return;

        foreach (DeedCardUI card in deedCards)
        {
            card?.Refresh();
        }

        detailsWindow?.Refresh();
    }

    private void HandleDiscoveryChanged(
        DeedRuntime deed)
    {
        if (!IsOpen)
            return;

        RebuildCategories();
        RebuildCards();

        detailsWindow?.Refresh();
    }

    private void HandleTrackingChanged(
        DeedRuntime deed)
    {
        if (!IsOpen)
            return;

        RebuildCategories();

        if (selectedFilter ==
            CategoryFilter.Tracked)
        {
            RebuildCards();
        }

        detailsWindow?.Refresh();
    }

    private void HandleRankCompleted(
        DeedRuntime deed,
        DeedRankDefinition rank)
    {
        if (!IsOpen)
            return;

        // Completed gains one new card
        // for every newly completed rank.
        RebuildCategories();
        RebuildCards();

        // Existing details remain open.
        // Current-rank views advance automatically;
        // historical views keep their selected rank.
        detailsWindow?.Refresh();
    }

    // =====================================================
    // FILTER SELECTION
    // =====================================================

    private void SelectAll()
    {
        selectedFilter = CategoryFilter.All;
        selectedCategory = null;

        RefreshSelection();
    }

    private void SelectTracked()
    {
        selectedFilter = CategoryFilter.Tracked;
        selectedCategory = null;

        RefreshSelection();
    }

    private void SelectCompleted()
    {
        selectedFilter = CategoryFilter.Completed;
        selectedCategory = null;

        RefreshSelection();
    }

    private void SelectCategory(
        DeedCategoryDefinition category)
    {
        selectedFilter = CategoryFilter.Category;
        selectedCategory = category;

        RefreshSelection();
    }

    private void RefreshSelection()
    {
        detailsWindow?.Close();

        RebuildCategories();
        RebuildCards();
    }

    // =====================================================
    // FILTER MATCHING
    // =====================================================

    private bool MatchesCurrentFilter(
        DeedRuntime deed)
    {
        if (deed == null ||
            !deed.ShouldShowCard)
        {
            return false;
        }

        switch (selectedFilter)
        {
            case CategoryFilter.All:
                return true;

            case CategoryFilter.Tracked:
                return deed.IsTracked;

            case CategoryFilter.Category:
                return BelongsToCategory(
                    deed.Definition.Category,
                    selectedCategory
                );

            default:
                return false;
        }
    }

    private static bool BelongsToCategory(
        DeedCategoryDefinition actual,
        DeedCategoryDefinition selected)
    {
        if (selected == null)
            return actual == null;

        DeedCategoryDefinition current = actual;

        for (int depth = 0;
             depth < 32 && current != null;
             depth++)
        {
            if (current == selected)
                return true;

            current = current.ParentCategory;
        }

        return false;
    }

    // =====================================================
    // CATEGORY LIST
    // =====================================================

    private void RebuildCategories()
    {
        ClearCategoryButtons();

        if (manager == null ||
            categoryContent == null ||
            categoryButtonPrefab == null)
        {
            return;
        }

        int visibleCount = 0;
        int trackedCount = 0;
        int completedCount = 0;

        HashSet<DeedCategoryDefinition>
            categorySet = new();

        foreach (DeedRuntime deed in manager.Deeds)
        {
            if (deed == null)
                continue;

            // Count every completed rank,
            // including earlier ranks of
            // unfinished deed chains.
            foreach (DeedRankDefinition rank
                     in deed.Definition.Ranks)
            {
                if (rank != null &&
                    deed.HasCompleted(rank))
                {
                    completedCount++;
                }
            }

            if (!deed.ShouldShowCard)
                continue;

            visibleCount++;

            if (deed.IsTracked)
                trackedCount++;

            DeedCategoryDefinition category =
                deed.Definition.Category;

            for (int depth = 0;
                 depth < 32 && category != null;
                 depth++)
            {
                if (!categorySet.Add(category))
                    break;

                category = category.ParentCategory;
            }
        }

        // =================================================
        // VIRTUAL CATEGORIES
        // =================================================

        AddCategoryButton(
            "All",
            visibleCount,
            null,
            selectedFilter == CategoryFilter.All,
            SelectAll
        );

        AddCategoryButton(
            "Tracked",
            trackedCount,
            null,
            selectedFilter == CategoryFilter.Tracked,
            SelectTracked
        );

        AddCategoryButton(
            "Completed",
            completedCount,
            null,
            selectedFilter == CategoryFilter.Completed,
            SelectCompleted
        );

        // =================================================
        // AUTHORED CATEGORIES
        // =================================================

        List<DeedCategoryDefinition> categories =
            new(categorySet);

        categories.Sort(CompareCategories);

        foreach (DeedCategoryDefinition category
                 in categories)
        {
            if (category == null)
                continue;

            DeedCategoryDefinition captured =
                category;

            AddCategoryButton(
                GetIndentedCategoryName(category),
                CountDeedsInCategory(category),
                category.Icon,
                selectedFilter ==
                    CategoryFilter.Category &&
                selectedCategory == category,
                () => SelectCategory(captured)
            );
        }
    }

    private static int CompareCategories(
        DeedCategoryDefinition a,
        DeedCategoryDefinition b)
    {
        if (a == null)
            return b == null ? 0 : -1;

        if (b == null)
            return 1;

        int order =
            a.SortOrder.CompareTo(b.SortOrder);

        if (order != 0)
            return order;

        return string.Compare(
            GetCategoryPath(a),
            GetCategoryPath(b),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static string GetCategoryPath(
        DeedCategoryDefinition category)
    {
        string path = string.Empty;

        DeedCategoryDefinition current =
            category;

        for (int i = 0;
             i < 32 && current != null;
             i++)
        {
            path =
                current.DisplayName + "/" + path;

            current = current.ParentCategory;
        }

        return path;
    }

    private static string GetIndentedCategoryName(
        DeedCategoryDefinition category)
    {
        int depth = 0;

        DeedCategoryDefinition current =
            category.ParentCategory;

        while (current != null &&
               depth < 8)
        {
            depth++;
            current = current.ParentCategory;
        }

        return new string(' ', depth * 2) +
               category.DisplayName;
    }

    private int CountDeedsInCategory(
        DeedCategoryDefinition category)
    {
        int count = 0;

        foreach (DeedRuntime deed in manager.Deeds)
        {
            if (deed == null ||
                !deed.ShouldShowCard)
            {
                continue;
            }

            if (BelongsToCategory(
                    deed.Definition.Category,
                    category))
            {
                count++;
            }
        }

        return count;
    }

    private void AddCategoryButton(
        string displayName,
        int count,
        Sprite icon,
        bool selected,
        Action onClicked)
    {
        DeedCategoryButtonUI button =
            Instantiate(
                categoryButtonPrefab,
                categoryContent
            );

        button.Bind(
            displayName,
            count,
            icon,
            selected,
            onClicked
        );

        categoryButtons.Add(button);
    }

    private void ClearCategoryButtons()
    {
        foreach (DeedCategoryButtonUI button
                 in categoryButtons)
        {
            if (button == null)
                continue;

            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }

        categoryButtons.Clear();
    }

    // =====================================================
    // BUILD DEED CARDS
    // =====================================================

    private void RebuildCards()
    {
        ClearDeedCards();

        if (manager == null ||
            deedContent == null ||
            deedCardPrefab == null)
        {
            return;
        }

        List<CardEntry> entries = new();

        foreach (DeedRuntime deed in manager.Deeds)
        {
            if (deed == null ||
                deed.Definition == null)
            {
                continue;
            }

            // =============================================
            // COMPLETED FILTER
            // =============================================

            if (selectedFilter ==
                CategoryFilter.Completed)
            {
                foreach (DeedRankDefinition rank
                         in deed.Definition.Ranks)
                {
                    if (rank == null ||
                        !deed.HasCompleted(rank))
                    {
                        continue;
                    }

                    // Separate card for each rank.
                    entries.Add(
                        new CardEntry(deed, rank)
                    );
                }

                continue;
            }

            // =============================================
            // NORMAL FILTERS
            // =============================================

            if (MatchesCurrentFilter(deed))
            {
                // One card per deed chain.
                entries.Add(
                    new CardEntry(deed, null)
                );
            }
        }

        // =================================================
        // SORT
        // =================================================

        entries.Sort((a, b) =>
        {
            int nameResult = string.Compare(
                a.Deed.Definition.DisplayName,
                b.Deed.Definition.DisplayName,
                StringComparison.OrdinalIgnoreCase
            );

            if (nameResult != 0)
                return nameResult;

            int aThreshold =
                a.Rank != null
                    ? a.Rank.RequiredTotalProgress
                    : 0;

            int bThreshold =
                b.Rank != null
                    ? b.Rank.RequiredTotalProgress
                    : 0;

            return aThreshold.CompareTo(
                bThreshold
            );
        });

        // =================================================
        // CREATE CARDS
        // =================================================

        foreach (CardEntry entry in entries)
        {
            DeedCardUI card =
                Instantiate(
                    deedCardPrefab,
                    deedContent
                );

            card.Bind(
                entry.Deed,
                entry.Rank,
                HandleDeedClicked
            );

            deedCards.Add(card);
        }

        // =================================================
        // EMPTY STATE
        // =================================================

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(
                entries.Count == 0
            );

            emptyText.text =
                selectedFilter switch
                {
                    CategoryFilter.Tracked =>
                        "No tracked deeds.",

                    CategoryFilter.Completed =>
                        "No completed deeds yet.",

                    _ =>
                        "No deeds in this category."
                };
        }
    }

    private void ClearDeedCards()
    {
        foreach (DeedCardUI card in deedCards)
        {
            if (card == null)
                continue;

            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }

        deedCards.Clear();
    }

    // =====================================================
    // DETAILS WINDOW
    // =====================================================

    private void HandleDeedClicked(
        DeedRuntime deed,
        DeedRankDefinition rank)
    {
        if (deed == null ||
            detailsWindow == null ||
            manager == null)
        {
            return;
        }

        if (detailsWindow.IsShowing(
                deed,
                rank))
        {
            detailsWindow.Close();
            return;
        }

        detailsWindow.Show(
            deed,
            manager,
            rank
        );
    }
}
