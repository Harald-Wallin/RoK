using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visar spelarens aktiva buffs.
///
/// Komponenten väntar tills spelarens BuffSystem finns och
/// prenumererar därefter på buff-events.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerBuffUI :
    MonoBehaviour
{
    public static PlayerBuffUI Instance
    {
        get;
        private set;
    }

    [Header("UI")]

    [SerializeField]
    private GameObject buffSlotPrefab;

    [SerializeField]
    private Transform container;

    [Header("References")]

    [SerializeField]
    private BuffSystem playerBuffSystem;

    private readonly List<BuffSlotUI>
        activeSlots =
            new();

    private bool isSubscribed;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Flera PlayerBuffUI hittades. " +
                "Den nya komponenten stängs av.",
                this);

            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        TryResolveAndSubscribe();
    }

    private void Update()
    {
        if (playerBuffSystem == null ||
            !isSubscribed)
        {
            TryResolveAndSubscribe();
        }

        RemoveDestroyedSlots();
        SortBuffs();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void TryResolveAndSubscribe()
    {
        if (playerBuffSystem == null)
        {
            PlayerStats player =
                PlayerReference.Player;

            if (player == null)
            {
                player =
                    FindFirstObjectByType<
                        PlayerStats>();
            }

            if (player != null)
            {
                playerBuffSystem =
                    player.GetComponent<
                        BuffSystem>();
            }
        }

        if (playerBuffSystem == null ||
            isSubscribed)
        {
            return;
        }

        playerBuffSystem.OnBuffAdded +=
            HandleBuffAdded;

        playerBuffSystem.OnBuffRemoved +=
            HandleBuffRemoved;

        playerBuffSystem.OnBuffChanged +=
            HandleBuffChanged;

        isSubscribed = true;

        RebuildFromCurrentBuffs();
    }

    private void Unsubscribe()
    {
        if (!isSubscribed ||
            playerBuffSystem == null)
        {
            isSubscribed = false;
            return;
        }

        playerBuffSystem.OnBuffAdded -=
            HandleBuffAdded;

        playerBuffSystem.OnBuffRemoved -=
            HandleBuffRemoved;

        playerBuffSystem.OnBuffChanged -=
            HandleBuffChanged;

        isSubscribed = false;
    }

    private void RebuildFromCurrentBuffs()
    {
        ClearSlots();

        if (playerBuffSystem == null)
            return;

        List<ActiveBuff> buffs =
            playerBuffSystem.GetActiveBuffs();

        foreach (ActiveBuff buff
                 in buffs)
        {
            AddBuffSlot(
                buff,
                playerBuffSystem);
        }
    }

    private void HandleBuffAdded(
        ActiveBuff buff,
        BuffSystem owner)
    {
        AddBuffSlot(
            buff,
            owner);
    }

    private void HandleBuffRemoved(
        ActiveBuff buff,
        BuffSystem owner)
    {
        RemoveBuffSlot(
            buff);
    }

    private void HandleBuffChanged(
        ActiveBuff buff,
        BuffSystem owner)
    {
        /*
         * BuffSlotUI läser runtime-datan kontinuerligt.
         * Vi behöver därför endast säkerställa att slotten finns.
         */
        if (!HasSlotForBuff(buff))
        {
            AddBuffSlot(
                buff,
                owner);
        }
    }

    private void AddBuffSlot(
        ActiveBuff buff,
        BuffSystem owner)
    {
        if (buff == null ||
            buffSlotPrefab == null ||
            container == null)
        {
            return;
        }

        if (HasSlotForBuff(buff))
            return;

        GameObject slotObject =
            Instantiate(
                buffSlotPrefab,
                container);

        BuffSlotUI slot =
            slotObject.GetComponent<
                BuffSlotUI>();

        if (slot == null)
        {
            Debug.LogError(
                $"Buff-prefaben '{buffSlotPrefab.name}' " +
                $"saknar {nameof(BuffSlotUI)}.",
                buffSlotPrefab);

            Destroy(slotObject);
            return;
        }

        slot.Setup(
            buff,
            owner);

        activeSlots.Add(
            slot);

        SortBuffs();
    }

    private bool HasSlotForBuff(
        ActiveBuff buff)
    {
        if (buff == null)
            return false;

        foreach (BuffSlotUI slot
                 in activeSlots)
        {
            if (slot != null &&
                ReferenceEquals(
                    slot.Buff,
                    buff))
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveBuffSlot(
        ActiveBuff buff)
    {
        for (int i = activeSlots.Count - 1;
             i >= 0;
             i--)
        {
            BuffSlotUI slot =
                activeSlots[i];

            if (slot == null)
            {
                activeSlots.RemoveAt(i);
                continue;
            }

            if (!ReferenceEquals(
                    slot.Buff,
                    buff))
            {
                continue;
            }

            activeSlots.RemoveAt(i);

            Destroy(
                slot.gameObject);
        }
    }

    private void RemoveDestroyedSlots()
    {
        activeSlots.RemoveAll(
            slot => slot == null);
    }

    private void SortBuffs()
    {
        activeSlots.Sort(
            (first, second) =>
            {
                if (first == null &&
                    second == null)
                {
                    return 0;
                }

                if (first == null)
                    return 1;

                if (second == null)
                    return -1;

                return second
                    .GetRemainingTime()
                    .CompareTo(
                        first.GetRemainingTime());
            });

        for (int i = 0;
             i < activeSlots.Count;
             i++)
        {
            BuffSlotUI slot =
                activeSlots[i];

            if (slot != null)
            {
                slot.transform.SetSiblingIndex(
                    i);
            }
        }
    }

    private void ClearSlots()
    {
        foreach (BuffSlotUI slot
                 in activeSlots)
        {
            if (slot != null)
            {
                Destroy(
                    slot.gameObject);
            }
        }

        activeSlots.Clear();
    }
}