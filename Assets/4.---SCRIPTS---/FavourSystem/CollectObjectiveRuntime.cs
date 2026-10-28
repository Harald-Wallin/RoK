using System.Collections.Generic;
using UnityEngine;

public sealed class CollectObjectiveRuntime :
    FavourObjectiveRuntime
{
    private readonly CollectObjectiveData
        collectData;

    private Inventory subscribedInventory;

    private int currentAmount;

    public CollectObjectiveRuntime(
        CollectObjectiveData data,
        FavourRuntime favour)
        : base(
            data,
            favour)
    {
        collectData = data;
    }

    public ItemData RequiredItem =>
        collectData != null
            ? collectData.Item
            : null;

    public override int CurrentProgress =>
        currentAmount;

    public override int RequiredProgress =>
        collectData != null
            ? collectData.RequiredAmount
            : 1;

    public override bool IsComplete =>
        currentAmount >=
        RequiredProgress;

    public override bool IsRelevantToItem(
        ItemData item)
    {
        if (collectData == null ||
            collectData.Item == null ||
            item == null)
        {
            return false;
        }

        return Inventory.ItemsMatch(
            collectData.Item,
            item
        );
    }

    protected override void OnActivated()
    {
        SubscribeToInventory();

        RefreshProgress();
    }

    protected override void OnDeactivated()
    {
        UnsubscribeFromInventory();
    }

    protected override void OnCollectTurnInCosts(
        List<FavourItemCost> costs)
    {
        if (collectData == null ||
            !collectData.ConsumeItemsOnCompletion ||
            collectData.Item == null)
        {
            return;
        }

        costs.Add(
            new FavourItemCost(
                collectData.Item,
                collectData.RequiredAmount
            )
        );
    }

    public override void ResetProgress()
    {
        RefreshProgress();
    }

    private void SubscribeToInventory()
    {
        Inventory inventory =
            Favour?.Manager?.PlayerInventory;

        if (inventory == null)
        {
            inventory =
                Inventory.Instance;
        }

        if (subscribedInventory ==
            inventory)
        {
            return;
        }

        UnsubscribeFromInventory();

        subscribedInventory =
            inventory;

        if (subscribedInventory != null)
        {
            subscribedInventory
                .OnInventoryChanged +=
                HandleInventoryChanged;
        }
    }

    private void UnsubscribeFromInventory()
    {
        if (subscribedInventory != null)
        {
            subscribedInventory
                .OnInventoryChanged -=
                HandleInventoryChanged;
        }

        subscribedInventory =
            null;
    }

    private void HandleInventoryChanged()
    {
        RefreshProgress();
    }

    private void RefreshProgress()
    {
        Inventory inventory =
            subscribedInventory;

        if (inventory == null)
        {
            inventory =
                Favour?.Manager
                    ?.PlayerInventory;
        }

        if (inventory == null)
        {
            inventory =
                Inventory.Instance;
        }

        int newAmount =
            inventory != null &&
            collectData != null &&
            collectData.Item != null
                ? inventory.GetItemCount(
                    collectData.Item
                )
                : 0;

        newAmount =
            Mathf.Max(
                0,
                newAmount
            );

        if (currentAmount ==
            newAmount)
        {
            return;
        }

        currentAmount =
            newAmount;

        RaiseProgressChanged();
    }
}