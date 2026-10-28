using TMPro;
using UnityEngine;

public sealed class SpellbookUI :
    MonoBehaviour
{
    private enum SpellbookFilter
    {
        All,
        Melee,
        Buff,
        BaseAttack
    }

    [Header("Content")]

    [SerializeField]
    private Transform contentParent;

    [SerializeField]
    private GameObject abilityEntryPrefab;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [Header("Filtering")]

    [SerializeField]
    private TMP_InputField searchInput;

    [SerializeField]
    private SpellbookTabButton showAllTab;

    [SerializeField]
    private SpellbookTabButton meleeTab;

    [SerializeField]
    private SpellbookTabButton buffTab;

    [SerializeField]
    private SpellbookTabButton baseAttackTab;

    private SpellbookFilter activeFilter =
        SpellbookFilter.All;

    private bool isOpen;

    private PlayerAbilityCollection Collection
    {
        get
        {
            PlayerStats player =
                PlayerReference.Player;

            return player != null
                ? player.GetComponent<
                    PlayerAbilityCollection
                >()
                : null;
        }
    }

    private void Start()
    {
        SetVisible(
            false
        );

        if (searchInput != null)
        {
            searchInput.onValueChanged
                .AddListener(
                    HandleSearchChanged
                );
        }

        SelectTab(
            showAllTab
        );

        Refresh();
    }

    private void OnDestroy()
    {
        if (searchInput != null)
        {
            searchInput.onValueChanged
                .RemoveListener(
                    HandleSearchChanged
                );
        }
    }

    private void HandleSearchChanged(
        string value)
    {
        Refresh();
    }

    private void Populate()
    {
        PlayerAbilityCollection collection =
            Collection;

        if (collection == null ||
            contentParent == null ||
            abilityEntryPrefab == null)
        {
            return;
        }

        var abilities =
            collection.GetAllSpellbookEntries();

        string search =
            searchInput != null
                ? searchInput.text.Trim()
                : string.Empty;

        foreach (AbilityData ability in abilities)
        {
            if (ability == null)
                continue;

            if (!MatchesSearch(
                    ability,
                    search))
            {
                continue;
            }

            if (!PassesFilter(
                    ability))
            {
                continue;
            }

            GameObject entryObject =
                Instantiate(
                    abilityEntryPrefab,
                    contentParent
                );

            AbilityEntryUI entry =
                entryObject.GetComponent<
                    AbilityEntryUI
                >();

            if (entry == null)
            {
                Debug.LogError(
                    $"Spellbook-prefaben " +
                    $"'{abilityEntryPrefab.name}' saknar " +
                    $"{nameof(AbilityEntryUI)}.",
                    entryObject
                );

                Destroy(
                    entryObject
                );

                continue;
            }

            entry.Setup(
                ability
            );

            DraggableAbility drag =
                entryObject.GetComponentInChildren<
                    DraggableAbility
                >(true);

            if (drag != null)
            {
                drag.ability =
                    ability;
            }
        }
    }

    private static bool MatchesSearch(
        AbilityData ability,
        string search)
    {
        if (ability == null)
            return false;

        if (string.IsNullOrWhiteSpace(
                search))
        {
            return true;
        }

        string abilityName =
            ability.abilityName ??
            string.Empty;

        return abilityName.IndexOf(
                   search,
                   System.StringComparison
                       .OrdinalIgnoreCase
               ) >= 0;
    }

    private bool PassesFilter(
        AbilityData ability)
    {
        if (ability == null)
            return false;

        switch (activeFilter)
        {
            case SpellbookFilter.All:
                return true;

            case SpellbookFilter.Melee:
                return
                    !ability.IsBaseAttack &&
                    HasTag(
                        ability,
                        AbilityTag.Melee
                    );

            case SpellbookFilter.Buff:
                return
                    !ability.IsBaseAttack &&
                    HasTag(
                        ability,
                        AbilityTag.Buff
                    );

            case SpellbookFilter.BaseAttack:
                return ability.IsBaseAttack;

            default:
                return true;
        }
    }

    private static bool HasTag(
        AbilityData ability,
        AbilityTag tag)
    {
        if (ability == null ||
            ability.tags == null)
        {
            return false;
        }

        for (int i = 0;
             i < ability.tags.Length;
             i++)
        {
            if (ability.tags[i] == tag)
                return true;
        }

        return false;
    }

    private void SelectTab(
        SpellbookTabButton selectedTab)
    {
        showAllTab?.SetSelected(
            showAllTab == selectedTab
        );

        meleeTab?.SetSelected(
            meleeTab == selectedTab
        );

        buffTab?.SetSelected(
            buffTab == selectedTab
        );

        baseAttackTab?.SetSelected(
            baseAttackTab == selectedTab
        );
    }

    public void Refresh()
    {
        if (contentParent == null)
            return;

        for (int i =
                 contentParent.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                contentParent
                    .GetChild(i)
                    .gameObject
            );
        }

        Populate();
    }

    public void ShowAll()
    {
        activeFilter =
            SpellbookFilter.All;

        SelectTab(
            showAllTab
        );

        Refresh();
    }

    public void FilterMelee()
    {
        activeFilter =
            SpellbookFilter.Melee;

        SelectTab(
            meleeTab
        );

        Refresh();
    }

    public void FilterBuff()
    {
        activeFilter =
            SpellbookFilter.Buff;

        SelectTab(
            buffTab
        );

        Refresh();
    }

    public void FilterBaseAttack()
    {
        activeFilter =
            SpellbookFilter.BaseAttack;

        SelectTab(
            baseAttackTab
        );

        Refresh();
    }

    public void Toggle()
    {
        isOpen =
            !isOpen;

        SetVisible(
            isOpen
        );

        if (!isOpen)
            return;

        SpellbookNotificationManager.Instance
            ?.ClearUnread();

        Refresh();
    }

    public void Close()
    {
        isOpen = false;

        SetVisible(
            false
        );
    }

    private void SetVisible(
        bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha =
            visible ? 1f : 0f;

        canvasGroup.interactable =
            visible;

        canvasGroup.blocksRaycasts =
            visible;
    }
}