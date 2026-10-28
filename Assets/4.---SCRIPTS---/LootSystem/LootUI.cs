using System.Collections.Generic;
using UnityEngine;

public sealed class LootUI :
    MonoBehaviour
{
    public static LootUI Instance
    {
        get;
        private set;
    }

    [Header("Content")]

    [SerializeField]
    private GameObject lootItemRowPrefab;

    [SerializeField]
    private GameObject lootWindow;

    [SerializeField]
    private Transform contentParent;

    [SerializeField]
    private TMPro.TMP_Text titleText;

    [Header("Layout")]

    [SerializeField]
    private RectTransform lootWindowRect;

    [SerializeField]
    private RectTransform contentRect;

    [SerializeField]
    private int paddingTop = 6;

    [SerializeField]
    private int paddingBottom = 6;

    [SerializeField]
    private float maxHeight;

    [SerializeField]
    private float titleHeight = 40f;

    private ILootSource currentSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(
                gameObject
            );

            return;
        }

        gameObject.SetActive(
            false
        );
    }

    public void Show(
        ILootSource source)
    {
        if (source == null)
        {
            Close();
            return;
        }

        currentSource =
            source;

        if (titleText != null)
        {
            titleText.text =
                source.LootTitle;
        }

        ClearRows();

        BuildCoinRow(
            source
        );

        BuildItemRows(
            source
        );

        gameObject.SetActive(
            true
        );
    }

    private void BuildCoinRow(
        ILootSource source)
    {
        if (source == null ||
            source.CoinAmount <= 0 ||
            lootItemRowPrefab == null ||
            contentParent == null)
        {
            return;
        }

        PlayerCurrency playerCurrency =
            PlayerCurrency.Instance;

        CurrencyData currency =
            playerCurrency != null
                ? playerCurrency
                    .CurrencyDefinition
                : null;

        if (currency == null)
        {
            Debug.LogWarning(
                "Loot innehåller coins men PlayerCurrency " +
                "saknar CurrencyData.",
                this
            );

            return;
        }

        GameObject row =
            Instantiate(
                lootItemRowPrefab,
                contentParent
            );

        LootItemRow lootRow =
            row.GetComponent<
                LootItemRow>();

        if (lootRow == null)
        {
            Debug.LogError(
                "Loot row-prefabben saknar LootItemRow.",
                row
            );

            Destroy(
                row
            );

            return;
        }

        lootRow.SetupCoins(
            currency,
            source,
            this
        );
    }

    private void BuildItemRows(
        ILootSource source)
    {
        if (source == null ||
            source.LootItems == null ||
            lootItemRowPrefab == null ||
            contentParent == null)
        {
            return;
        }

        List<ItemData> shownItems =
            new();

        foreach (ItemData item
                 in source.LootItems)
        {
            if (item == null ||
                ContainsMatchingItem(
                    shownItems,
                    item))
            {
                continue;
            }

            shownItems.Add(
                item
            );

            GameObject row =
                Instantiate(
                    lootItemRowPrefab,
                    contentParent
                );

            LootItemRow lootRow =
                row.GetComponent<
                    LootItemRow>();

            if (lootRow == null)
            {
                Debug.LogError(
                    "Loot row-prefabben saknar LootItemRow.",
                    row
                );

                Destroy(
                    row
                );

                continue;
            }

            lootRow.SetupItem(
                item,
                source,
                this
            );
        }
    }

    private static bool ContainsMatchingItem(
        IReadOnlyList<ItemData> items,
        ItemData candidate)
    {
        if (items == null ||
            candidate == null)
        {
            return false;
        }

        foreach (ItemData item
                 in items)
        {
            if (Inventory.ItemsMatch(
                    item,
                    candidate))
            {
                return true;
            }
        }

        return false;
    }

    private void ClearRows()
    {
        if (contentParent == null)
            return;

        foreach (Transform child
                 in contentParent)
        {
            Destroy(
                child.gameObject
            );
        }
    }

    public void Refresh()
    {
        if (currentSource == null ||
            !currentSource.HasLoot)
        {
            Close();
            return;
        }

        Show(
            currentSource
        );
    }

    public void Close()
    {
        currentSource =
            null;

        ItemTooltip.Instance?.Hide();

        gameObject.SetActive(
            false
        );
    }
}