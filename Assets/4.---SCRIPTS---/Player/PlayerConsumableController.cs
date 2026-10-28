using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(CharacterChannelController))]
public sealed class PlayerConsumableController :
    MonoBehaviour
{
    private PlayerStats player;
    private Inventory inventory;
    private PlayerMovement movement;

    private CharacterStateController
        stateController;

    private CharacterActionController
        actionController;

    private CharacterChannelController
        channelController;

    private BuffSystem buffSystem;

    private ItemData activeFood;
    private RuntimeDisplayBuff eatingBuff;

    public bool IsConsuming =>
        activeFood != null &&
        channelController != null &&
        channelController.IsOwnedBy(this);

    public ItemData ActiveFood =>
        activeFood;

    public float NormalizedProgress =>
        IsConsuming
            ? channelController
                .NormalizedProgress
            : 0f;

    public event Action<ItemData>
        ConsumptionStarted;

    public event Action<ItemData, int>
        ConsumptionTicked;

    public event Action<ItemData>
        ConsumptionCancelled;

    public event Action<ItemData>
        ConsumptionCompleted;

    private void Awake()
    {
        player =
            GetComponent<PlayerStats>();

        inventory =
            GetComponent<Inventory>();

        if (inventory == null)
        {
            inventory =
                GetComponentInChildren<
                    Inventory>(
                    true);
        }

        movement =
            GetComponent<PlayerMovement>();

        stateController =
            GetComponent<
                CharacterStateController>();

        actionController =
            GetComponent<
                CharacterActionController>();

        channelController =
            GetComponent<
                CharacterChannelController>();

        buffSystem =
            GetComponent<BuffSystem>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        CancelConsumption();

        Unsubscribe();
    }

    public bool TryConsumeFoodFromSlot(
        int slotIndex)
    {
        if (IsConsuming ||
            inventory == null ||
            player == null ||
            channelController == null)
        {
            return false;
        }

        if (slotIndex < 0 ||
            slotIndex >=
            inventory.slots.Count)
        {
            return false;
        }

        InventorySlot slot =
            inventory.slots[
                slotIndex];

        if (slot == null ||
            slot.IsEmpty() ||
            slot.item == null)
        {
            return false;
        }

        ItemData food =
            slot.item;

        if (!CanStartConsuming(
                food))
        {
            return false;
        }

        ChannelRequest request =
    new ChannelRequest(
        this,
        $"{food.DisplayName}",
        food.icon,
        food.FoodChannelDuration,
        food.FoodTickInterval,
        isReversed: true);

        /*
         * Channelingen startas först.
         *
         * Om någon annan channel redan är aktiv förbrukas
         * alltså ingen mat.
         */
        if (!channelController
                .TryStartChannel(
                    request))
        {
            return false;
        }

        activeFood =
            food;

        inventory.RemoveItemAt(
            slotIndex,
            1);

        AddEatingBuff(
            food);

        ConsumptionStarted?.Invoke(
            food);

        return true;
    }

    public bool CancelConsumption()
    {
        if (channelController == null ||
            !channelController.IsOwnedBy(this))
        {
            ClearConsumptionState();
            return false;
        }

        return channelController
            .CancelChannel(
                this);
    }

    private bool CanStartConsuming(
        ItemData food)
    {
        if (food == null ||
            !food.IsFood)
        {
            return false;
        }

        if (stateController != null &&
            !stateController.CanEat)
        {
            return false;
        }

        if (actionController != null &&
            actionController.HasActiveAction)
        {
            return false;
        }

        if (channelController != null &&
            channelController.IsChanneling)
        {
            return false;
        }

        if (player == null ||
            !player.IsAlive)
        {
            return false;
        }

        if (!food.AllowFoodAtFullHealth &&
            player.currentHP >=
            player.GetMaxHP())
        {
            return false;
        }

        return true;
    }

    private void HandleChannelTicked(
        ChannelRuntime channel,
        int tickIndex)
    {
        if (!IsOwnedChannel(channel) ||
            activeFood == null ||
            player == null)
        {
            return;
        }

        /*
         * Mat använder den separata healing-pipelinen och kan
         * uttryckligen inte critta.
         */
        HealingResult result =
            player.ApplyHealing(
                activeFood
                    .FoodHealingPerTick,
                canCrit: false);

        ConsumptionTicked?.Invoke(
            activeFood,
            result.AppliedAmount);
    }

    private void HandleChannelCompleted(
        ChannelRuntime channel)
    {
        if (!IsOwnedChannel(channel))
            return;

        ItemData completedFood =
            activeFood;

        ClearConsumptionState();

        if (completedFood != null)
        {
            ConsumptionCompleted?.Invoke(
                completedFood);
        }
    }

    private void HandleChannelCancelled(
        ChannelRuntime channel)
    {
        if (!IsOwnedChannel(channel))
            return;

        ItemData cancelledFood =
            activeFood;

        ClearConsumptionState();

        if (cancelledFood != null)
        {
            ConsumptionCancelled?.Invoke(
                cancelledFood);
        }
    }

    private bool IsOwnedChannel(
        ChannelRuntime channel)
    {
        return
            channel != null &&
            ReferenceEquals(
                channel.Owner,
                this);
    }

    private void AddEatingBuff(
        ItemData food)
    {
        RemoveEatingBuff();

        if (buffSystem == null ||
            food == null)
        {
            return;
        }

        eatingBuff =
            new RuntimeDisplayBuff(
                $"{food.DisplayName}",
                "Restoring health while eating. ",
                food.icon,
                food.FoodChannelDuration,
                removeOnDeath: true,
                removeOnEncounterReset: true);

        buffSystem.AddRuntimeBuff(
            eatingBuff);
    }

    private void RemoveEatingBuff()
    {
        if (eatingBuff == null)
            return;

        buffSystem?.RemoveBuff(
            eatingBuff);

        eatingBuff = null;
    }

    private void ClearConsumptionState()
    {
        RemoveEatingBuff();

        activeFood = null;
    }

    private void Subscribe()
    {
        Unsubscribe();

        if (movement != null)
        {
            movement.MovementInputStarted +=
                HandleMovementInputStarted;
        }

        if (player != null)
        {
            player.OnDamagedBy +=
                HandlePlayerDamaged;

            player.OnDied +=
                HandlePlayerDied;
        }

        if (stateController != null)
        {
            stateController.OnEnteredCombat +=
                HandleEnteredCombat;
        }

        if (actionController != null)
        {
            actionController.OnActionStarted +=
                HandleActionStarted;
        }

        if (channelController != null)
        {
            channelController.ChannelTicked +=
                HandleChannelTicked;

            channelController.ChannelCompleted +=
                HandleChannelCompleted;

            channelController.ChannelCancelled +=
                HandleChannelCancelled;
        }
    }

    private void Unsubscribe()
    {
        if (movement != null)
        {
            movement.MovementInputStarted -=
                HandleMovementInputStarted;
        }

        if (player != null)
        {
            player.OnDamagedBy -=
                HandlePlayerDamaged;

            player.OnDied -=
                HandlePlayerDied;
        }

        if (stateController != null)
        {
            stateController.OnEnteredCombat -=
                HandleEnteredCombat;
        }

        if (actionController != null)
        {
            actionController.OnActionStarted -=
                HandleActionStarted;
        }

        if (channelController != null)
        {
            channelController.ChannelTicked -=
                HandleChannelTicked;

            channelController.ChannelCompleted -=
                HandleChannelCompleted;

            channelController.ChannelCancelled -=
                HandleChannelCancelled;
        }
    }

    private void HandleMovementInputStarted()
    {
        CancelConsumption();
    }

    private void HandlePlayerDamaged(
        CharacterStats attacker)
    {
        CancelConsumption();
    }

    private void HandlePlayerDied(
        CharacterStats deadCharacter)
    {
        CancelConsumption();
    }

    private void HandleEnteredCombat()
    {
        CancelConsumption();
    }

    private void HandleActionStarted(
        ActionContext context)
    {
        CancelConsumption();
    }
}