using System;
using UnityEngine;

public class WardSystem : MonoBehaviour
{
    public event Action OnWardChanged;

    [SerializeField]
    private int maxWard = 5;

    [SerializeField]
    private float decayDelay = 15f;

    private int currentWard;
    private bool unlocked;
    private bool hasShieldEquipped;
    private float decayTimer;

    public int CurrentWard => currentWard;
    public int MaxWard => maxWard;
    public bool IsUnlocked => unlocked;

    public int VisibleWardSlots => unlocked ? maxWard : 0;

    void Update()
    {
        if (currentWard <= 0)
            return;

        decayTimer -= Time.deltaTime;

        if (decayTimer <= 0f)
        {
            currentWard--;

            decayTimer = decayDelay;

            OnWardChanged?.Invoke();
        }
    }

    public void EnableWardGeneration()
    {
        unlocked = true;
    }

    public void DisableWardGeneration()
    {
        unlocked = false;
        ClearWard();
    }

    public void SetWardGeneration(bool enabled)
    {
        unlocked = enabled;

        if (!enabled)
            ClearWard();
    }

    public void AddWard(int amount)
    {
        //Debug.Log("AddWard called");

        if (!unlocked)
        {
            //Debug.Log("Ward generation locked");
            return;
        }

        if (!hasShieldEquipped)
        {
            //Debug.Log("No shield equipped");
            return;
        }

        currentWard =
            Mathf.Clamp(
                currentWard + amount,
                0,
                maxWard
            );

        //Debug.Log($"Ward added. Current wards: {currentWard}");

        decayTimer = decayDelay;

        OnWardChanged?.Invoke();
    }

    public bool TrySpendWard(int amount)
    {
        if (currentWard < amount)
            return false;

        currentWard -= amount;

        OnWardChanged?.Invoke();

        return true;
    }

    public void ClearWard()
    {
        currentWard = 0;

        OnWardChanged?.Invoke();
    }

    public void RefreshShieldState()
    {
        EquipmentManager equipment = EquipmentManager.Instance;

        if (equipment == null)
            return;

        hasShieldEquipped = false;

        foreach (var slot in equipment.equipmentSlots)
        {
            ItemData item = slot.GetEquippedItem();

            if (item == null)
                continue;

            if (item.itemType == ItemType.Shield)
            {
                hasShieldEquipped = true;
                break;
            }
        }

        if (!hasShieldEquipped)
        {
            //Debug.Log("Amount of wards: " + currentWard);
            ClearWard();
        }
    }

    public void SetMaxWard(int amount)
    {
        maxWard = amount;

        currentWard =
            Mathf.Clamp(
                currentWard,
                0,
                maxWard
            );

        OnWardChanged?.Invoke();
    }
}
