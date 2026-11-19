using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Header("Settings")]
    [SerializeField, Min(1)]
    private int size = 9;

    [Header("Slots")]
    public List<InventorySlot> slots = new();

    [Header("Debug")]
    [SerializeField] private bool enableDebugItems;
    [SerializeField] private ItemData debugTestItemOne;
    [SerializeField] private ItemData debugTestItemTwo;
    [SerializeField] private ItemData debugTestItemThree;
    [SerializeField] private ItemData debugTestItemFour;
    [SerializeField] private ItemData debugTestItemFive;
    [SerializeField] private ItemData debugTestItemSix;

    [SerializeField]
    private KeyCode debugAddItemKey = KeyCode.F8;

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
            Instance = null;
    }

    private void Initialize()
    {
        slots ??= new List<InventorySlot>();

        while (slots.Count < size)
            slots.Add(new InventorySlot());

        while (slots.Count > size)
            slots.RemoveAt(slots.Count - 1);

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i] ??= new InventorySlot();
            NormalizeSlot(slots[i]);
        }
    }

    private List<InventorySlot> CloneSlots()
    {
        List<InventorySlot> result = new(slots.Count);

        foreach (InventorySlot source in slots)
        {
            InventorySlot clone = new();
            clone.CopyFrom(source);
            result.Add(clone);
        }

        return result;
    }

    private static bool TryRemoveFromSlots(
        List<InventorySlot> targetSlots,
        IReadOnlyList<InventoryItemAmount> removals)
    {
        if (removals == null)
            return true;

        foreach (InventoryItemAmount removal in removals)
        {
            if (!removal.IsValid)
                continue;

            if (GetItemCountFromSlots(
                    targetSlots,
                    removal.Item) < removal.Amount)
            {
                return false;
            }

            int remaining = removal.Amount;

            foreach (InventorySlot slot in targetSlots)
            {
                if (remaining <= 0)
                    break;

                if (slot == null ||
                    slot.IsEmpty() ||
                    !ItemsMatch(slot.item, removal.Item))
                {
                    continue;
                }

                int removed = Mathf.Min(
                    slot.amount,
                    remaining);

                slot.amount -= removed;
                remaining -= removed;

                NormalizeSlot(slot);
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

        foreach (InventoryItemAmount addition in additions)
        {
            if (!addition.IsValid)
                continue;

            int remaining = addition.Amount;
            ItemData item = addition.Item;

            if (item.stackable)
            {
                int maxStack = GetSafeMaxStack(item);

                foreach (InventorySlot slot in targetSlots)
                {
                    if (remaining <= 0)
                        break;

                    if (slot == null ||
                        slot.IsEmpty() ||
                        !ItemsMatch(slot.item, item))
                    {
                        continue;
                    }

                    int availableSpace =
                        maxStack - slot.amount;

                    if (availableSpace <= 0)
                        continue;

                    int added = Mathf.Min(
                        availableSpace,
                        remaining);

                    slot.amount += added;
                    remaining -= added;
                }

                foreach (InventorySlot slot in targetSlots)
                {
                    if (remaining <= 0)
                        break;

                    if (slot == null || !slot.IsEmpty())
                        continue;

                    int added = Mathf.Min(
                        maxStack,
                        remaining);

                    slot.SetItem(item, added);
                    remaining -= added;
                }
            }
            else
            {
                foreach (InventorySlot slot in targetSlots)
                {
                    if (remaining <= 0)
                        break;

                    if (slot == null || !slot.IsEmpty())
                        continue;

                    slot.SetItem(item, 1);
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
        if (targetSlots == null || item == null)
            return 0;

        int total = 0;

        foreach (InventorySlot slot in targetSlots)
        {
            if (slot == null ||
                slot.IsEmpty() ||
                !ItemsMatch(slot.item, item))
            {
                continue;
            }

            total += Mathf.Max(0, slot.amount);
        }

        return total;
    }

    private void CommitSimulatedSlots(
        IReadOnlyList<InventorySlot> simulatedSlots)
    {
        if (simulatedSlots == null ||
            simulatedSlots.Count != slots.Count)
        {
            throw new InvalidOperationException(
                "Invalid inventory transaction size.");
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i] ??= new InventorySlot();
            slots[i].CopyFrom(simulatedSlots[i]);
        }
    }

    private static bool HasValidEntries(
        IReadOnlyList<InventoryItemAmount> entries)
    {
        if (entries == null)
            return false;

        foreach (InventoryItemAmount entry in entries)
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

    public bool AddItem(
        ItemData item,
        int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (!CanAddItem(item, amount))
        {
            NotificationSpawner.Instance?.Show(
                NotificationSpawner.Instance.Database.inventoryFull);

            return false;
        }

        int remaining = amount;

        if (item.stackable)
        {
            int maxStack = GetSafeMaxStack(item);

            for (int i = 0;
                 i < slots.Count && remaining > 0;
                 i++)
            {
                InventorySlot slot = slots[i];

                if (slot == null ||
                    slot.IsEmpty() ||
                    !ItemsMatch(slot.item, item))
                {
                    continue;
                }

                int space = maxStack - slot.amount;

                if (space <= 0)
                    continue;

                int added = Mathf.Min(space, remaining);

                slot.amount += added;
                remaining -= added;
            }

            for (int i = 0;
                 i < slots.Count && remaining > 0;
                 i++)
            {
                InventorySlot slot = slots[i];

                if (slot == null || !slot.IsEmpty())
                    continue;

                int added = Mathf.Min(maxStack, remaining);

                slot.SetItem(item, added);
                remaining -= added;
            }
        }
        else
        {
            for (int i = 0;
                 i < slots.Count && remaining > 0;
                 i++)
            {
                InventorySlot slot = slots[i];

                if (slot == null || !slot.IsEmpty())
                    continue;

                slot.SetItem(item, 1);
                remaining--;
            }
        }

        if (remaining > 0)
        {
            Debug.LogError(
                $"Inventory could not add all of '{item.DisplayName}'.",
                this);

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
        List<InventorySlot> simulated = CloneSlots();

        if (!TryRemoveFromSlots(simulated, removals))
            return false;

        if (!TryAddToSlots(simulated, additions))
        {
            if (notifyIfInventoryFull)
            {
                NotificationSpawner.Instance?.Show(
                    NotificationSpawner.Instance.Database.inventoryFull);
            }

            return false;
        }

        return true;
    }

    public bool TryApplyTransaction(
        IReadOnlyList<InventoryItemAmount> removals,
        IReadOnlyList<InventoryItemAmount> additions,
        bool notifyIfInventoryFull = true)
    {
        List<InventorySlot> simulated = CloneSlots();

        if (!TryRemoveFromSlots(simulated, removals))
            return false;

        if (!TryAddToSlots(simulated, additions))
        {
            if (notifyIfInventoryFull)
            {
                NotificationSpawner.Instance?.Show(
                    NotificationSpawner.Instance.Database.inventoryFull);
            }

            return false;
        }

        if (!HasValidEntries(removals) &&
            !HasValidEntries(additions))
        {
            return true;
        }

        CommitSimulatedSlots(simulated);
        NotifyChanged();

        return true;
    }

    public bool CanAddItem(
        ItemData item,
        int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int capacity = 0;

        if (item.stackable)
        {
            int maxStack = GetSafeMaxStack(item);

            foreach (InventorySlot slot in slots)
            {
                if (slot == null)
                    continue;

                if (slot.IsEmpty())
                {
                    capacity += maxStack;
                }
                else if (ItemsMatch(slot.item, item))
                {
                    capacity += Mathf.Max(
                        0,
                        maxStack - slot.amount);
                }
            }
        }
        else
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot != null && slot.IsEmpty())
                    capacity++;
            }
        }

        return capacity >= amount;
    }

    public void RemoveItem(
        ItemData item,
        int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        foreach (InventorySlot slot in slots)
        {
            if (slot == null ||
                slot.IsEmpty() ||
                !ItemsMatch(slot.item, item))
            {
                continue;
            }

            int removed = Mathf.Min(slot.amount, amount);
            slot.amount -= removed;

            NormalizeSlot(slot);

            if (removed > 0)
                NotifyChanged();

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

        InventorySlot slot = slots[slotIndex];

        if (slot == null || slot.IsEmpty())
            return;

        int removed = Mathf.Min(slot.amount, amount);
        slot.amount -= removed;

        NormalizeSlot(slot);

        if (removed > 0)
            NotifyChanged();
    }

    public bool TryRemoveItemAt(
        int slotIndex,
        ItemData expectedItem,
        int amount = 1)
    {
        if (expectedItem == null ||
            amount <= 0 ||
            slotIndex < 0 ||
            slotIndex >= slots.Count)
        {
            return false;
        }

        InventorySlot slot = slots[slotIndex];

        if (slot == null ||
            slot.IsEmpty() ||
            !ItemsMatch(slot.item, expectedItem) ||
            slot.amount < amount)
        {
            return false;
        }

        slot.amount -= amount;
        NormalizeSlot(slot);
        NotifyChanged();

        return true;
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count ||
            indexB < 0 || indexB >= slots.Count ||
            indexA == indexB)
        {
            return;
        }

        InventorySlot temporary = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temporary;

        NotifyChanged();
    }

    public int GetItemCount(ItemData item)
    {
        return GetItemCountFromSlots(slots, item);
    }

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return 0;

        int total = 0;

        foreach (InventorySlot slot in slots)
        {
            if (slot == null ||
                slot.IsEmpty() ||
                !string.Equals(
                    slot.item.Id,
                    itemId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            total += Mathf.Max(0, slot.amount);
        }

        return total;
    }

    public bool RemoveItemAmount(
        ItemData item,
        int amount)
    {
        if (item == null ||
            amount <= 0 ||
            GetItemCount(item) < amount)
        {
            return false;
        }

        int remaining = amount;

        foreach (InventorySlot slot in slots)
        {
            if (remaining <= 0)
                break;

            if (slot == null ||
                slot.IsEmpty() ||
                !ItemsMatch(slot.item, item))
            {
                continue;
            }

            int removed = Mathf.Min(
                slot.amount,
                remaining);

            slot.amount -= removed;
            remaining -= removed;

            NormalizeSlot(slot);
        }

        if (remaining > 0)
            return false;

        NotifyChanged();
        return true;
    }

    public bool Contains(
        ItemData item,
        int amount = 1)
    {
        return item != null &&
               amount > 0 &&
               GetItemCount(item) >= amount;
    }

    public static bool ItemsMatch(
        ItemData first,
        ItemData second)
    {
        if (first == null || second == null)
            return false;

        if (first == second)
            return true;

        if (string.IsNullOrWhiteSpace(first.Id) ||
            string.IsNullOrWhiteSpace(second.Id))
        {
            return false;
        }

        return string.Equals(
            first.Id,
            second.Id,
            StringComparison.Ordinal);
    }

    private static int GetSafeMaxStack(ItemData item)
    {
        return item == null ||
               item.IsContainer ||
               !item.stackable
            ? 1
            : Mathf.Max(1, item.maxStack);
    }

    private static void NormalizeSlot(InventorySlot slot)
    {
        if (slot == null)
            return;

        if (slot.item == null || slot.amount <= 0)
        {
            slot.Clear();
            return;
        }

        if (slot.item.IsContainer)
        {
            slot.amount = 1;
            slot.EnsureContainerLoot();
            return;
        }

        slot.amount = !slot.item.stackable
            ? 1
            : Mathf.Clamp(
                slot.amount,
                1,
                GetSafeMaxStack(slot.item));
    }

    public bool OpenContainerAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return false;

        InventorySlot slot = slots[slotIndex];

        if (slot == null ||
            slot.IsEmpty() ||
            !slot.item.IsContainer)
        {
            return false;
        }

        slot.EnsureContainerLoot().Open(this);
        return true;
    }

    internal bool TryLootContainerItem(
        InventoryContainerLoot source,
        ItemData item,
        int quantity)
    {
        if (source == null ||
            item == null ||
            quantity <= 0 ||
            source.GetItemQuantity(item) < quantity)
        {
            return false;
        }

        int index = source.FindSlotIndex();

        if (index < 0 ||
            !ReferenceEquals(
                slots[index].ContainerLoot,
                source))
        {
            return false;
        }

        List<InventorySlot> simulated = CloneSlots();

        if (source.RemovesContainerWhenTaking(item, quantity))
            simulated[index].Clear();

        if (!TryAddToSlots(
                simulated,
                new[] { new InventoryItemAmount(item, quantity) }))
        {
            NotificationSpawner.Instance?.Show(
                NotificationSpawner.Instance.Database.inventoryFull);

            return false;
        }

        if (!source.RemoveContents(item, quantity))
            return false;

        CommitSimulatedSlots(simulated);
        NotifyChanged();

        return true;
    }

    internal void RemoveEmptyContainer(
        InventoryContainerLoot source)
    {
        if (source == null || !source.ContentsEmpty)
            return;

        int index = source.FindSlotIndex();

        if (index < 0 ||
            !ReferenceEquals(
                slots[index].ContainerLoot,
                source))
        {
            return;
        }

        slots[index].Clear();
        NotifyChanged();
    }

    public bool TryRestoreContainer(
        ItemData item,
        InventoryContainerLoot state)
    {
        if (item == null ||
            !item.IsContainer ||
            (state != null &&
                (state.IsOwned ||
                 !ItemsMatch(item, state.Definition))))
        {
            return false;
        }

        foreach (InventorySlot slot in slots)
        {
            if (state != null &&
                ReferenceEquals(slot?.ContainerLoot, state))
            {
                return false;
            }
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot == null || !slot.IsEmpty())
                continue;

            slot.SetItem(item, 1, state);
            NotifyChanged();

            return true;
        }

        return false;
    }

    private void Update()
    {
        if (!enableDebugItems ||
            !Input.GetKeyDown(debugAddItemKey))
        {
            return;
        }

        AddDebugItem(debugTestItemOne);
        AddDebugItem(debugTestItemTwo);
        AddDebugItem(debugTestItemThree);
        AddDebugItem(debugTestItemFour);
        AddDebugItem(debugTestItemFive);
        AddDebugItem(debugTestItemSix);
    }

    private void AddDebugItem(ItemData item)
    {
        if (item != null)
            AddItem(item, 1);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        size = Mathf.Max(1, size);
    }
#endif
}