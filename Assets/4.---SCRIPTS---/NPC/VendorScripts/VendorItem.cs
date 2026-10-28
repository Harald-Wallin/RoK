using UnityEngine;

public enum VendorStockType
{
    Infinite,
    Limited
}

[CreateAssetMenu(menuName = "Vendors/Vendor Item")]
public class VendorItem : ScriptableObject
{
    [Header("Item")]
    public ItemData item;

    [Header("Price")]
    public int vendorPrice;

    [Header("Stock")]
    public VendorStockType stockType = VendorStockType.Infinite;

    [Header("Requirements")]
    public bool useLevelRequirement = false;
    public int requiredLevel = 1;

    public bool useReputationRequirement = false;
    public ReputationState requiredReputation = ReputationState.Indifferent;

    public int maxStock = 1;

    public float restockTime = 0f;
}
