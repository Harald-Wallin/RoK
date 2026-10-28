using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Header("Settings")]

    [SerializeField]
    [Min(1)]
    private int size = 9;

    [Header("Slots")]

    public List<InventorySlot> slots =
        new List<InventorySlot>();

    [Header("Debug")]

    [SerializeField]
    private bool enableDebugItems;

    [SerializeField]
    private ItemData debugTestItemOne;

    [SerializeField]
    private ItemData debugTestItemTwo;

    [SerializeField]
    private ItemData debugTestItemThree;

    [SerializeField]
    private ItemData debugTestItemFour;

    [SerializeField]
    private ItemData debugTestItemFive;

    [SerializeField]
    private ItemData debugTestItemSix;

    [SerializeField]
    private KeyCode debugAddItemKey =
        KeyCode.F8;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Initialize()
    {
        /*
         * Bevarar serialiserade slots om de redan har rätt
         * storlek. Detta blir även bättre för framtida save/load.
         */
        if (slots == null)
        {
            slots =
                new List<InventorySlot>();
        }

        while (slots.Count < size)
        {
            slots.Add(
                new InventorySlot()
            );
        }

        while (slots.Count > size)
        {
            slots.RemoveAt(
                slots.Count - 1
            );
        }

        for (int i = 0;
             i < slots.Count;
             i++)
        {
            if (slots[i] == null)
            {
                slots[i] =
                    new InventorySlot();
            }

            NormalizeSlot(
                slots[i]
            );
        }
    }

    private List<InventorySlot> CloneSlots()
    {
        List<InventorySlot> clonedSlots =
            new List<InventorySlot>(
                slots.Count
            );

        foreach (InventorySlot source
                 in slots)
        {
            InventorySlot clone =
                new InventorySlot();

            if (source != null &&
                !source.IsEmpty())
            {
                clone.SetItem(
                    source.item,
                    source.amount
                );
            }

            clonedSlots.Add(
                clone
            );
        }

        return clonedSlots;
    }

    private static bool TryRemoveFromSlots(
        List<InventorySlot> targetSlots,
        IReadOnlyList<InventoryItemAmount> removals)
    {
        if (removals == null)
            return true;

        foreach (InventoryItemAmount removal
                 in removals)
        {
            if (!removal.IsValid)
                continue;

            int available =
                GetItemCountFromSlots(
                    targetSlots,
                    removal.Item
                );

            if (available <
                removal.Amount)
            {
                return false;
            }

            int remaining =
                removal.Amount;

            foreach (InventorySlot slot
                     in targetSlots)
            {
                if (remaining <= 0)
                    break;

                if (slot == null ||
                    slot.IsEmpty() ||
                    !ItemsMatch(
                        slot.item,
                        removal.Item))
                {
                    continue;
                }

                int removed =
                    Mathf.Min(
                        slot.amount,
                        remaining
                    );

                slot.amount -=
                    removed;

                remaining -=
                    removed;

                NormalizeSlot(
                    slot
                );
            }

            if (remaining > 0)
                return false;
        }

        return true;
    }

    private static bool TryAddToSlots(
        List<InventorySlot> targetSlots,
        IReadOnlyList<InventoryItemAmount> additions)
    {
        if (additions == null)
            return true;

        foreach (InventoryItemAmount addition
                 in additions)
        {
            if (!addition.IsValid)
                continue;

            int remaining =
                addition.Amount;

            ItemData item =
                addition.Item;

            if (item.stackable)
            {
                int maxStack =
                    GetSafeMaxStack(
                        item
                    );

                /*
                 * Fyll befintliga stackar först.
                 */
                foreach (InventorySlot slot
                         in targetSlots)
                {
                    if (remaining <= 0)
                        break;

                    if (slot == null ||
                        slot.IsEmpty() ||
                        !ItemsMatch(
                            slot.item,
                            item))
                    {
                        continue;
                    }

                    int availableSpace =
                        maxStack -
                        slot.amount;

                    if (availableSpace <= 0)
                        continue;

                    int added =
                        Mathf.Min(
                            availableSpace,
                            remaining
                        );

                    slot.amount +=
                        added;

                    remaining -=
                        added;
                }

                /*
                 * Skapa därefter nya stackar.
                 */
                foreach (InventorySlot slot
                         in targetSlots)
                {
                    if (remaining <= 0)
                        break;

                    if (slot == null ||
                        !slot.IsEmpty())
                    {
                        continue;
                    }

                    int added =
                        Mathf.Min(
                            maxStack,
                            remaining
                        );

                    slot.SetItem(
                        item,
                        added
                    );

                    remaining -=
                        added;
                }
            }
            else
            {
                foreach (InventorySlot slot
                         in targetSlots)
                {
                    if (remaining <= 0)
                        break;

                    if (slot == null ||
                        !slot.IsEmpty())
                    {
                        continue;
                    }

                    slot.SetItem(
                        item,
                        1
                    );

                    remaining--;
                }
            }

            if (remaining > 0)
                return false;
        }

        return true;
    }

    private static int GetItemCountFromSlots(
        IReadOnlyList<InventorySlot> targetSlots,
        ItemData item)
    {
        if (targetSlots == null ||
            item == null)
        {
            return 0;
        }

        int total = 0;

        foreach (InventorySlot slot
                 in targetSlots)
        {
            if (slot == null ||
                slot.IsEmpty() ||
                !ItemsMatch(
                    slot.item,
                    item))
            {
                continue;
            }

            total +=
                Mathf.Max(
                    0,
                    slot.amount
                );
        }

        return total;
    }

    private void CommitSimulatedSlots(
        IReadOnlyList<InventorySlot> simulatedSlots)
    {
        if (simulatedSlots == null ||
            simulatedSlots.Count !=
            slots.Count)
        {
            Debug.LogError(
                "Inventory-transaktionen skapade ett ogiltigt antal slots.",
                this
            );

            return;
        }

        for (int i = 0;
             i < slots.Count;
             i++)
        {
            if (slots[i] == null)
            {
                slots[i] =
                    new InventorySlot();
            }

            InventorySlot simulated =
                simulatedSlots[i];

            if (simulated == null ||
                simulated.IsEmpty())
            {
                slots[i].Clear();
                continue;
            }

            slots[i].SetItem(
                simulated.item,
                simulated.amount
            );
        }
    }

    private static bool HasValidEntries(
        IReadOnlyList<InventoryItemAmount> entries)
    {
        if (entries == null)
            return false;

        foreach (InventoryItemAmount entry
                 in entries)
        {
            if (entry.IsValid)
                return true;
        }

        return false;
    }

    public void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Lägger till hela mängden eller ingenting alls.
    /// Metoden skapar aldrig stackar över item.maxStack.
    /// </summary>
    public bool AddItem(
        ItemData item,
        int amount = 1)
    {
        if (item == null ||
            amount <= 0)
        {
            return false;
        }

        if (!CanAddItem(
                item,
                amount))
        {
            NotificationSpawner.Instance
                ?.Show(
                    NotificationSpawner.Instance
                        .Database
                        .inventoryFull
                );

            return false;
        }

        int remaining =
            amount;

        if (item.stackable)
        {
            int maxStack =
                GetSafeMaxStack(
                    item
                );

            /*
             * Fyll först befintliga stackar.
             */
            for (int i = 0;
                 i < slots.Count &&
                 remaining > 0;
                 i++)
            {
                InventorySlot slot =
                    slots[i];

                if (slot == null ||
                    slot.IsEmpty() ||
                    !ItemsMatch(
                        slot.item,
                        item))
                {
                    continue;
                }

                int availableSpace =
                    maxStack -
                    slot.amount;

                if (availableSpace <= 0)
                    continue;

                int added =
                    Mathf.Min(
                        availableSpace,
                        remaining
                    );

                slot.amount +=
                    added;

                remaining -=
                    added;
            }

            /*
             * Skapa därefter nya stackar.
             */
            for (int i = 0;
                 i < slots.Count &&
                 remaining > 0;
                 i++)
            {
                InventorySlot slot =
                    slots[i];

                if (slot == null ||
                    !slot.IsEmpty())
                {
                    continue;
                }

                int added =
                    Mathf.Min(
                        maxStack,
                        remaining
                    );

                slot.SetItem(
                    item,
                    added
                );

                remaining -=
                    added;
            }
        }
        else
        {
            /*
             * Icke-stackbara items behöver en separat slot
             * per exemplar.
             */
            for (int i = 0;
                 i < slots.Count &&
                 remaining > 0;
                 i++)
            {
                InventorySlot slot =
                    slots[i];

                if (slot == null ||
                    !slot.IsEmpty())
                {
                    continue;
                }

                slot.SetItem(
                    item,
                    1
                );

                remaining--;
            }
        }

        if (remaining > 0)
        {
            /*
             * Ska inte kunna inträffa eftersom CanAddItem
             * validerade hela operationen.
             */
            Debug.LogError(
                $"Inventory kunde inte lägga till hela mängden av " +
                $"'{item.DisplayName}' trots godkänd kapacitetskontroll.",
                this
            );

            return false;
        }

        NotifyChanged();

        return true;
    }

    public bool CanApplyTransaction(
    IReadOnlyList<InventoryItemAmount> removals,
    IReadOnlyList<InventoryItemAmount> additions,
    bool notifyIfInventoryFull = false)
    {
        List<InventorySlot> simulatedSlots =
            CloneSlots();

        if (!TryRemoveFromSlots(
                simulatedSlots,
                removals))
        {
            return false;
        }

        if (!TryAddToSlots(
                simulatedSlots,
                additions))
        {
            if (notifyIfInventoryFull)
            {
                NotificationSpawner.Instance
                    ?.Show(
                        NotificationSpawner.Instance
                            .Database
                            .inventoryFull
                    );
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Utför borttagningar och tillägg som en enda atomär
    /// inventory-transaktion.
    ///
    /// Operationen simuleras först på en kopia av inventoryt.
    /// Om något inte går att genomföra ändras ingenting.
    ///
    /// Borttagningar utförs före tillägg, vilket innebär att
    /// completion-items kan frigöra plats för rewards.
    /// </summary>
    public bool TryApplyTransaction(
        IReadOnlyList<InventoryItemAmount> removals,
        IReadOnlyList<InventoryItemAmount> additions,
        bool notifyIfInventoryFull = true)
    {
        List<InventorySlot> simulatedSlots =
            CloneSlots();

        if (!TryRemoveFromSlots(
                simulatedSlots,
                removals))
        {
            return false;
        }

        if (!TryAddToSlots(
                simulatedSlots,
                additions))
        {
            if (notifyIfInventoryFull)
            {
                NotificationSpawner.Instance
                    ?.Show(
                        NotificationSpawner.Instance
                            .Database
                            .inventoryFull
                    );
            }

            return false;
        }

        bool hasChanges =
            HasValidEntries(removals) ||
            HasValidEntries(additions);

        if (!hasChanges)
            return true;

        CommitSimulatedSlots(
            simulatedSlots
        );

        NotifyChanged();

        return true;
    }

    /// <summary>
    /// Kontrollerar om hela mängden får plats utan att modifiera
    /// inventoryt.
    /// </summary>
    public bool CanAddItem(
        ItemData item,
        int amount = 1)
    {
        if (item == null ||
            amount <= 0)
        {
            return false;
        }

        int capacity = 0;

        if (item.stackable)
        {
            int maxStack =
                GetSafeMaxStack(
                    item
                );

            foreach (InventorySlot slot
                     in slots)
            {
                if (slot == null)
                    continue;

                if (slot.IsEmpty())
                {
                    capacity +=
                        maxStack;

                    continue;
                }

                if (!ItemsMatch(
                        slot.item,
                        item))
                {
                    continue;
                }

                capacity +=
                    Mathf.Max(
                        0,
                        maxStack -
                        slot.amount
                    );
            }
        }
        else
        {
            foreach (InventorySlot slot
                     in slots)
            {
                if (slot != null &&
                    slot.IsEmpty())
                {
                    capacity++;
                }
            }
        }

        return capacity >=
               amount;
    }

    /// <summary>
    /// Tar bort upp till amount från den första matchande stacken.
    /// Behålls för kompatibilitet med befintliga anrop.
    /// </summary>
    public void RemoveItem(
        ItemData item,
        int amount = 1)
    {
        if (item == null ||
            amount <= 0)
        {
            return;
        }

        foreach (InventorySlot slot
                 in slots)
        {
            if (slot == null ||
                slot.IsEmpty() ||
                !ItemsMatch(
                    slot.item,
                    item))
            {
                continue;
            }

            int removed =
                Mathf.Min(
                    slot.amount,
                    amount
                );

            slot.amount -=
                removed;

            NormalizeSlot(
                slot
            );

            if (removed > 0)
            {
                NotifyChanged();
            }

            return;
        }
    }

    public void RemoveItemAt(
        int slotIndex,
        int amount = 1)
    {
        if (slotIndex < 0 ||
            slotIndex >= slots.Count ||
            amount <= 0)
        {
            return;
        }

        InventorySlot slot =
            slots[slotIndex];

        if (slot == null ||
            slot.IsEmpty())
        {
            return;
        }

        int removed =
            Mathf.Min(
                slot.amount,
                amount
            );

        slot.amount -=
            removed;

        NormalizeSlot(
            slot
        );

        if (removed > 0)
        {
            NotifyChanged();
        }
    }

    /// <summary>
    /// Försöker ta bort exakt den begärda mängden av ett
    /// förväntat item från en specifik inventory-slot.
    ///
    /// Operationen är all-or-nothing.
    /// Om slot, item eller mängd inte stämmer ändras ingenting.
    /// </summary>
    public bool TryRemoveItemAt(
        int slotIndex,
        ItemData expectedItem,
        int amount = 1)
    {
        if (expectedItem == null ||
            amount <= 0)
        {
            return false;
        }

        if (slotIndex < 0 ||
            slotIndex >= slots.Count)
        {
            return false;
        }

        InventorySlot slot =
            slots[slotIndex];

        if (slot == null ||
            slot.IsEmpty())
        {
            return false;
        }

        if (!ItemsMatch(
                slot.item,
                expectedItem))
        {
            return false;
        }

        if (slot.amount < amount)
        {
            return false;
        }

        slot.amount -=
            amount;

        NormalizeSlot(
            slot
        );

        NotifyChanged();

        return true;
    }

    public void SwapSlots(
        int indexA,
        int indexB)
    {
        if (indexA < 0 ||
            indexA >= slots.Count)
        {
            return;
        }

        if (indexB < 0 ||
            indexB >= slots.Count)
        {
            return;
        }

        if (indexA == indexB)
            return;

        InventorySlot temp =
            slots[indexA];

        slots[indexA] =
            slots[indexB];

        slots[indexB] =
            temp;

        NotifyChanged();
    }

    public int GetItemCount(
        ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;

        foreach (InventorySlot slot
                 in slots)
        {
            if (slot == null ||
                slot.IsEmpty())
            {
                continue;
            }

            if (!ItemsMatch(
                    slot.item,
                    item))
            {
                continue;
            }

            total +=
                Mathf.Max(
                    0,
                    slot.amount
                );
        }

        return total;
    }

    public int GetItemCount(
        string itemId)
    {
        if (string.IsNullOrWhiteSpace(
                itemId))
        {
            return 0;
        }

        int total = 0;

        foreach (InventorySlot slot
                 in slots)
        {
            if (slot == null ||
                slot.IsEmpty() ||
                slot.item == null)
            {
                continue;
            }

            if (!string.Equals(
                    slot.item.Id,
                    itemId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            total +=
                Mathf.Max(
                    0,
                    slot.amount
                );
        }

        return total;
    }

    /// <summary>
    /// Tar bort hela den begärda mängden eller ingenting alls.
    /// </summary>
    public bool RemoveItemAmount(
        ItemData item,
        int amount)
    {
        if (item == null ||
            amount <= 0)
        {
            return false;
        }

        if (GetItemCount(item) <
            amount)
        {
            return false;
        }

        int remaining =
            amount;

        foreach (InventorySlot slot
                 in slots)
        {
            if (remaining <= 0)
                break;

            if (slot == null ||
                slot.IsEmpty() ||
                !ItemsMatch(
                    slot.item,
                    item))
            {
                continue;
            }

            int removed =
                Mathf.Min(
                    slot.amount,
                    remaining
                );

            slot.amount -=
                removed;

            remaining -=
                removed;

            NormalizeSlot(
                slot
            );
        }

        if (remaining > 0)
        {
            Debug.LogError(
                $"Inventory misslyckades med att ta bort hela mängden " +
                $"av '{item.DisplayName}' efter godkänd antal-kontroll.",
                this
            );

            return false;
        }

        NotifyChanged();

        return true;
    }

    public bool Contains(
        ItemData item,
        int amount = 1)
    {
        if (item == null ||
            amount <= 0)
        {
            return false;
        }

        return GetItemCount(item) >=
               amount;
    }

    public static bool ItemsMatch(
        ItemData first,
        ItemData second)
    {
        if (first == null ||
            second == null)
        {
            return false;
        }

        if (first == second)
            return true;

        if (string.IsNullOrWhiteSpace(
                first.Id) ||
            string.IsNullOrWhiteSpace(
                second.Id))
        {
            return false;
        }

        return string.Equals(
            first.Id,
            second.Id,
            StringComparison.Ordinal
        );
    }

    private static int GetSafeMaxStack(
        ItemData item)
    {
        if (item == null ||
            !item.stackable)
        {
            return 1;
        }

        return Mathf.Max(
            1,
            item.maxStack
        );
    }

    private static void NormalizeSlot(
        InventorySlot slot)
    {
        if (slot == null)
            return;

        if (slot.item == null ||
            slot.amount <= 0)
        {
            slot.Clear();
            return;
        }

        if (!slot.item.stackable)
        {
            slot.amount = 1;
            return;
        }

        slot.amount =
            Mathf.Clamp(
                slot.amount,
                1,
                GetSafeMaxStack(
                    slot.item
                )
            );
    }

    private void Update()
    {
        if (!enableDebugItems ||
            !Input.GetKeyDown(
                debugAddItemKey))
        {
            return;
        }

        AddDebugItem(
            debugTestItemOne
        );

        AddDebugItem(
            debugTestItemTwo
        );

        AddDebugItem(
            debugTestItemThree
        );

        AddDebugItem(
            debugTestItemFour
        );

        AddDebugItem(
            debugTestItemFive
        );

        AddDebugItem(
            debugTestItemSix
        );
    }

    private void AddDebugItem(
        ItemData item)
    {
        if (item != null)
        {
            AddItem(
                item,
                1
            );
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        size =
            Mathf.Max(
                1,
                size
            );
    }
#endif
}