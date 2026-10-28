using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemRarity
{
    Trash,
    Common,
    Uncommon,
    Strange,
    Extraordinary,
    FromTheSagas
}

public enum ItemType
{
    None,
    Weapon,
    Helmet,
    Chest,
    Back,
    Shoulders,
    Legs,
    Feet,
    Neck,
    Ring,
    Reagent,
    Reputation,
    Trash,
    Offhand,
    Shield,
    FavourItem,
    Food
}

[CreateAssetMenu(
    menuName = "Items/Item"
)]
public class ItemData :
    ScriptableObject,
    ITooltipProvider
{


    [Header("Basic Info")]

    public string itemName;

    public Sprite worldSprite;

    [Header("Rarity")]

    public ItemRarity rarity =
        ItemRarity.Common;

    [Header("Directional Sprites")]

    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Appearance")]

    public bool hideHair;
    public bool hideBeard;
    public bool hideHead;
    public bool hideArms;
    public bool hideTorso;
    public bool hideLegs;
    public bool hideFeet;

    [TextArea(2, 3)]
    public string description;

    public Sprite icon;

    public ItemType itemType;

    [Header("Favour Item")]

    [SerializeField]
    [Tooltip(
    "Den favour som måste ha accepterats innan detta item " +
    "får förekomma som loot. Används endast för FavourItem.")]
    private FavourData requiredFavour;

    [Header("Favour Interaction")]

    [SerializeField]
    private ItemFavourUseSettings favourUse =
    new();

    [Header("Food")]

    [SerializeField]
    [Tooltip(
    "Healing som delas ut vid varje lyckad tick.")]
    [Min(1)]
    private int foodHealingPerTick = 10;

    [SerializeField]
    [Tooltip(
        "Sekunder mellan varje healing-tick."
    )]
    [Min(0.1f)]
    private float foodTickInterval = 3f;

    [SerializeField]
    [Tooltip(
        "Matens totala channel-tid."
    )]
    [Min(0.1f)]
    private float foodChannelDuration = 9f;

    [SerializeField]
    [Tooltip(
        "Tillåter användning när spelaren redan har full HP."
    )]
    private bool allowFoodAtFullHealth;

    [Header("Flags")]

    public bool equippable;
    public bool stackable;

    [Min(1)]
    public int maxStack = 1;

    [Header("Stat Modifiers")]

    public ItemStatModifier[] statModifiers;

    [Header("Requirements")]

    public bool useLevelRequirement;
    public int requiredLevel;
    public Faction reputationFaction;

    public bool useReputationRequirement;
    public ReputationState requiredReputation;

    [Header("Ownership")]

    [SerializeField]
    [Min(0)]
    [Tooltip(
    "Maximalt antal exemplar av detta item som spelaren får äga " +
    "samtidigt över Inventory och Equipment.\n\n" +
    "0 = Unlimited.\n" +
    "1 = Unique.\n" +
    "Högre värden tillåter motsvarande antal exemplar."
)]
    private int maximumOwned;

    [Header("Economy")]

    [SerializeField]
    private int buyPrice = 10;

    [Range(0f, 1f)]
    [SerializeField]
    private float sellMultiplier = 0.6f;

    public string Id =>
    PersistentIdUtility
        .FromDisplayName(
            DisplayName
        );

    public FavourData RequiredFavour =>
    requiredFavour;

    public ItemFavourUseSettings FavourUse =>
    favourUse;

    public bool HasFavourInteraction =>
        favourUse != null &&
        favourUse.HasFavours;

    public IReadOnlyList<FavourData>
        LinkedFavours =>
            favourUse != null
                ? favourUse.Favours
                : Array.Empty<FavourData>();

    public string ItemTypeDisplayName =>
        GetItemTypeDisplayName(
            itemType);

    public int MaximumOwned =>
    Mathf.Max(
        0,
        maximumOwned
    );

    public bool HasOwnershipLimit =>
        MaximumOwned > 0;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            itemName
        )
            ? name
            : itemName;

    public int BuyPrice =>
        buyPrice;

    public int SellPrice =>
        Mathf.RoundToInt(
            buyPrice *
            sellMultiplier
        );

    public bool IsFood =>
    itemType ==
    ItemType.Food;

    public int FoodHealingPerTick =>
        Mathf.Max(
            1,
            foodHealingPerTick
        );

    public float FoodTickInterval =>
        Mathf.Max(
            0.1f,
            foodTickInterval
        );

    public float FoodChannelDuration =>
        Mathf.Max(
            FoodTickInterval,
            foodChannelDuration
        );

    public bool AllowFoodAtFullHealth =>
        allowFoodAtFullHealth;

    [Serializable]
    public class VendorItem
    {
        public ItemData item;
        public int price;
        public bool unique;
        public float cooldownSeconds;

        [HideInInspector]
        public float lastSoldTime;

        public string warningDialog;
    }

    public TooltipData GetTooltipData(
        CharacterStats caster)
    {
        TooltipData data =
            new TooltipData();

        data.title =
            DisplayName;

        data.titleColor =
            ItemRarityColors.GetColor(
                rarity
            );

        data.subtitle =
            ItemTypeDisplayName;

        data.description =
            description;

        if (statModifiers != null)
        {
            foreach (ItemStatModifier modifier
                     in statModifiers)
            {
                if (modifier == null)
                    continue;

                if (Mathf.Approximately(
                        modifier.value,
                        0f))
                {
                    continue;
                }

                data.stats.Add(
                    StatFormatting.FormatModifier(
                        modifier.stat,
                        modifier.modifierType,
                        modifier.value
                    )
                );
            }
        }

        data.footer =
            $"Sellprice: {SellPrice}";

        data.showFooter =
            true;

        PlayerStats player =
            PlayerReference.Player;

        AddRequirementLines(
            data,
            player
        );

        if (IsFood)
        {
            data.stats.Add(
                $"Restores {FoodHealingPerTick} health " +
                $"every {FoodTickInterval:0.#} seconds."
            );

            data.stats.Add(
                $"Eating time: " +
                $"{FoodChannelDuration:0.#} seconds."
            );
        }

        FavourPresentationUtility
            .AppendForItem(
                data,
                this
            );

        return data;
    }

    public bool MeetsRequirements(
        PlayerStats player)
    {
        if (player == null)
            return false;

        if (useLevelRequirement &&
            player.level < requiredLevel)
        {
            return false;
        }

        if (useReputationRequirement)
        {
            if (reputationFaction == null)
                return false;

            PlayerReputationManager rep =
                player.GetComponent<
                    PlayerReputationManager
                >();

            if (rep == null)
                return false;

            ReputationState state =
                rep.GetReputationState(
                    reputationFaction
                );

            if (state <
                requiredReputation)
            {
                return false;
            }
        }

        return true;
    }

    public void AddRequirementLines(
        TooltipData data,
        PlayerStats player)
    {
        if (data == null ||
            player == null)
        {
            return;
        }

        if (useLevelRequirement)
        {
            bool meets =
                player.level >=
                requiredLevel;

            string color =
                meets
                    ? "white"
                    : "#FF5555";

            data.requirements.Add(
                $"<color={color}>Requires Level {requiredLevel}</color>"
            );
        }

        if (useReputationRequirement &&
            reputationFaction != null)
        {
            PlayerReputationManager rep =
                player.GetComponent<
                    PlayerReputationManager
                >();

            if (rep != null)
            {
                ReputationState state =
                    rep.GetReputationState(
                        reputationFaction
                    );

                bool meets =
                    state >=
                    requiredReputation;

                string color =
                    meets
                        ? "white"
                        : "#FF5555";

                data.requirements.Add(
                    $"<color={color}>Requires {requiredReputation} " +
                    $"with {reputationFaction.factionName}</color>"
                );
            }
        }
    }

    private static string GetItemTypeDisplayName(
    ItemType type)
    {
        return type switch
        {
            ItemType.FavourItem =>
                "Favour item",

            _ =>
                type.ToString()
        };
    }

    public bool CanDropForPlayer(
    PlayerFavourManager favourManager)
    {
        /*
         * =====================================================
         * OWNERSHIP LIMIT
         * =====================================================
         *
         * Loot får inte generera ytterligare exemplar om
         * spelarens generella ownership-limit skulle överskridas.
         *
         * PlayerItemOwnership räknar Inventory + Equipment.
         */

        if (HasOwnershipLimit)
        {
            PlayerItemOwnership ownership =
                favourManager?.PlayerItems;

            if (ownership != null &&
                !ownership.CanReceive(
                    this,
                    1
                ))
            {
                return false;
            }
        }

        /*
         * =====================================================
         * NORMAL ITEMS
         * =====================================================
         */

        if (itemType !=
            ItemType.FavourItem)
        {
            return true;
        }

        /*
         * =====================================================
         * FAVOUR ITEMS
         * =====================================================
         *
         * ItemType.FavourItem behåller sin gamla betydelse:
         * itemet måste behövas av den angivna aktiva Favourn.
         */

        if (requiredFavour == null)
            return false;

        if (favourManager == null)
            return false;

        return favourManager
            .CanDropFavourItem(
                requiredFavour,
                this
            );
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(
        typeof(ItemData)
    )]
    public class ItemDataEditor :
        UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ItemData item =
                (ItemData)target;

            UnityEditor.EditorGUILayout
                .Space();

            UnityEditor.EditorGUILayout
                .LabelField(
                    "Calculated Sell Price",
                    item.SellPrice.ToString()
                );
        }
    }

    protected virtual void OnValidate()
    {
        favourUse ??=
        new ItemFavourUseSettings();

        maxStack = stackable
                ? Mathf.Max(
                    1,
                    maxStack
                )
                : 1;
        if (itemType != ItemType.FavourItem)
        {
            requiredFavour = null;
        }
        else if (requiredFavour == null)
        {
            Debug.LogWarning(
                $"FavourItem '{name}' saknar Required Favour.",
                this);
        }

        foodHealingPerTick =
    Mathf.Max(
        1,
        foodHealingPerTick
    );

        foodTickInterval =
            Mathf.Max(
                0.1f,
                foodTickInterval
            );

        foodChannelDuration =
            Mathf.Max(
                foodTickInterval,
                foodChannelDuration
            );

        maximumOwned =
            Mathf.Max(
                0,
                maximumOwned
            );
    }
#endif
}