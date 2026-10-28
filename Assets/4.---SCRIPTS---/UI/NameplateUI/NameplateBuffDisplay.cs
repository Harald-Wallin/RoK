using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation av aktiva buffar och debuffar i en nameplate.
///
/// Visar högst ett konfigurerbart antal effekter.
/// De effekter som löper ut först prioriteras och den kortaste
/// återstående durationen placeras längst till höger.
/// </summary>
public sealed class NameplateBuffDisplay :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Transform container;

    [SerializeField]
    private BuffSlotUI slotPrefab;

    [Header("Display")]

    [SerializeField]
    [Min(1)]
    private int maximumVisibleSlots = 5;

    [SerializeField]
    [Min(0.05f)]
    private float refreshInterval = 0.25f;

    private readonly Dictionary<
        ActiveBuff,
        BuffSlotUI> slots =
            new();

    private readonly List<ActiveBuff>
        sortedBuffs =
            new();

    private BuffSystem buffSystem;
    private float refreshTimer;

    public void Bind(
        BuffSystem system)
    {
        if (buffSystem == system)
            return;

        Unsubscribe();
        ClearSlots();

        buffSystem =
            system;

        Subscribe();
        Rebuild();
    }

    private void Update()
    {
        if (buffSystem == null)
            return;

        refreshTimer -=
            Time.deltaTime;

        if (refreshTimer > 0f)
            return;

        refreshTimer =
            refreshInterval;

        RefreshVisibleSlots();
    }

    private void Subscribe()
    {
        if (buffSystem == null)
            return;

        buffSystem.OnBuffAdded +=
            HandleBuffAdded;

        buffSystem.OnBuffRemoved +=
            HandleBuffRemoved;

        buffSystem.OnBuffChanged +=
            HandleBuffChanged;
    }

    private void Unsubscribe()
    {
        if (buffSystem == null)
            return;

        buffSystem.OnBuffAdded -=
            HandleBuffAdded;

        buffSystem.OnBuffRemoved -=
            HandleBuffRemoved;

        buffSystem.OnBuffChanged -=
            HandleBuffChanged;
    }

    private void Rebuild()
    {
        if (buffSystem == null)
            return;

        List<ActiveBuff> activeBuffs =
            buffSystem.GetActiveBuffs();

        for (int i = 0;
             i < activeBuffs.Count;
             i++)
        {
            CreateSlot(
                activeBuffs[i]
            );
        }

        RefreshVisibleSlots();
    }

    private void HandleBuffAdded(
        ActiveBuff buff,
        BuffSystem owner)
    {
        CreateSlot(
            buff
        );

        RefreshVisibleSlots();
    }

    private void HandleBuffRemoved(
        ActiveBuff buff,
        BuffSystem owner)
    {
        RemoveSlot(
            buff
        );

        RefreshVisibleSlots();
    }

    private void HandleBuffChanged(
        ActiveBuff buff,
        BuffSystem owner)
    {
        RefreshVisibleSlots();
    }

    private void CreateSlot(
        ActiveBuff buff)
    {
        if (buff == null ||
            container == null ||
            slotPrefab == null ||
            slots.ContainsKey(buff))
        {
            return;
        }

        BuffSlotUI slot =
            Instantiate(
                slotPrefab,
                container
            );

        slot.Setup(
            buff,
            buffSystem
        );

        slots.Add(
            buff,
            slot
        );
    }

    private void RemoveSlot(
        ActiveBuff buff)
    {
        if (buff == null ||
            !slots.TryGetValue(
                buff,
                out BuffSlotUI slot))
        {
            return;
        }

        slots.Remove(
            buff
        );

        if (slot != null)
        {
            Destroy(
                slot.gameObject
            );
        }
    }

    private void RefreshVisibleSlots()
    {
        sortedBuffs.Clear();

        foreach (
            KeyValuePair<ActiveBuff, BuffSlotUI>
                entry in slots)
        {
            if (entry.Key == null ||
                entry.Value == null)
            {
                continue;
            }

            sortedBuffs.Add(
                entry.Key
            );
        }

        /*
         * För urvalet sorterar vi kortast duration först.
         * Då prioriteras de mest tidskritiska effekterna när fler
         * än maximumVisibleSlots är aktiva.
         */
        sortedBuffs.Sort(
            CompareRemainingTimeAscending
        );

        int visibleCount =
            Mathf.Min(
                maximumVisibleSlots,
                sortedBuffs.Count
            );

        /*
         * De valda buffarna placeras därefter i omvänd ordning:
         *
         * längre tid → kortare tid
         *
         * Den buff som löper ut först hamnar därför längst till
         * höger.
         */
        for (int i = 0;
             i < sortedBuffs.Count;
             i++)
        {
            ActiveBuff buff =
                sortedBuffs[i];

            BuffSlotUI slot =
                slots[buff];

            bool visible =
                i < visibleCount;

            slot.gameObject.SetActive(
                visible
            );
        }

        for (int i = visibleCount - 1,
             siblingIndex = 0;
             i >= 0;
             i--,
             siblingIndex++)
        {
            ActiveBuff buff =
                sortedBuffs[i];

            BuffSlotUI slot =
                slots[buff];

            slot.transform.SetSiblingIndex(
                siblingIndex
            );
        }
    }

    private static int
        CompareRemainingTimeAscending(
            ActiveBuff left,
            ActiveBuff right)
    {
        if (left == null &&
            right == null)
        {
            return 0;
        }

        if (left == null)
            return 1;

        if (right == null)
            return -1;

        return left.RemainingTime
            .CompareTo(
                right.RemainingTime
            );
    }

    private void ClearSlots()
    {
        foreach (
            KeyValuePair<ActiveBuff, BuffSlotUI>
                entry in slots)
        {
            if (entry.Value != null)
            {
                Destroy(
                    entry.Value.gameObject
                );
            }
        }

        slots.Clear();
        sortedBuffs.Clear();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
