using System.Collections.Generic;
using UnityEngine;
using System;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;
    public event Action OnEquipmentChanged;
    public List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();

    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private HumanoidEquipment humanoidEquipment;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        FindEquipmentSlots();
    }

    private void FindEquipmentSlots()
    {
        equipmentSlots.Clear();

        EquipmentSlotUI[] slots =
        FindObjectsByType<EquipmentSlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);


        foreach (EquipmentSlotUI slot in slots)
        {
            equipmentSlots.Add(slot);
        }
    }

    private EquipmentSlotUI GetSlotForItem(ItemType itemType)
    {
        foreach (EquipmentSlotUI slot in equipmentSlots)
        {
            if (slot.slotType == itemType)
            {
                return slot;
            }
        }

        return null;
    }

    public int GetEquippedItemCount(
    ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;

        foreach (EquipmentSlotUI slot
                 in equipmentSlots)
        {
            if (slot == null)
                continue;

            ItemData equippedItem =
                slot.GetEquippedItem();

            if (Inventory.ItemsMatch(
                    equippedItem,
                    item))
            {
                total++;
            }
        }

        return total;
    }

    public void TryEquipItem(
    ItemData item,
    int fromSlotIndex)
    {
        if (item == null)
            return;

        Debug.Log($"EquipmentManager -> {item.itemName}" );

        if (!item.MeetsRequirements(playerStats))
        {
            Debug.Log("Requirements not met.");

            return;
        }

        if (!item.equippable)
            return;

        EquipmentSlotUI targetSlot = GetSlotForItem(item.itemType);

        if (targetSlot == null)
            return;

        ItemData existingItem = targetSlot.GetEquippedItem();

        if (existingItem != null)
        {
            RemoveStats(targetSlot);

            inventory.slots[fromSlotIndex].item =
                existingItem;

            inventory.slots[fromSlotIndex].amount =
                1;
        }
        else
        {
            InventorySlot sourceSlot =
                inventory.slots[fromSlotIndex];

            sourceSlot.amount -= 1;

            if (sourceSlot.amount <= 0)
            {
                sourceSlot.item = null;
                sourceSlot.amount = 0;
            }
        }

        targetSlot.SetItem(
            item
        );

        ApplyStats(
            item,
            targetSlot
        );

        humanoidEquipment.Equip(
            item
        );

        inventory.NotifyChanged();
        OnEquipmentChanged?.Invoke();
        OnEquipmentChanged?.Invoke();

        WardSystem ward = playerStats.GetComponent<WardSystem>();

        if (ward != null)
        {
            ward.RefreshShieldState();
        }
    }



    private void ApplyStats(
    ItemData item,
    EquipmentSlotUI sourceSlot)
    {
        if (item == null ||
            sourceSlot == null)
        {
            return;
        }

        if (item.statModifiers == null)
            return;

        foreach (ItemStatModifier modifier
                 in item.statModifiers)
        {
            if (modifier == null)
                continue;

            if (Mathf.Approximately(
                    modifier.value,
                    0f))
            {
                continue;
            }

            playerStats.AddModifier(
                new StatModifier(
                    modifier.stat,
                    modifier.value,
                    modifier.modifierType,
                    sourceSlot,
                    ModifierSourceType.Equipment
                )
            );
        }
    }

    public void RemoveStats(
    EquipmentSlotUI sourceSlot)
    {
        if (sourceSlot == null)
            return;

        playerStats.RemoveModifiersFromSource(
            sourceSlot
        );
    }

    public void RemoveEquippedItemFromSlot(
    EquipmentSlotUI slot)
    {
        if (slot == null)
            return;

        ItemData item =
            slot.GetEquippedItem();

        if (item == null)
            return;

        RemoveStats(slot);

        humanoidEquipment.Unequip(item);

        slot.ClearSlot();
        OnEquipmentChanged?.Invoke();

        WardSystem ward =
            playerStats.GetComponent<WardSystem>();

        ward?.RefreshShieldState();
    }

    public void Unequip(
    EquipmentSlotUI slot)
    {
        if (slot == null)
            return;

        ItemData item = slot.GetEquippedItem();

        if (item == null)
            return;

        RemoveStats(slot);

        humanoidEquipment.Unequip(item);

        slot.ClearSlot();
        OnEquipmentChanged?.Invoke();

        WardSystem ward =
            playerStats.GetComponent<WardSystem>();

        if (ward != null)
        {
            ward.RefreshShieldState();
        }

        inventory.AddItem(
            item,
            1
        );
    }
}


