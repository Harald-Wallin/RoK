using UnityEngine;

/// <summary>
/// Interaktionsalternativ som låter spelaren donera ett visst
/// item för reputation och eventuellt experience.
/// </summary>
public sealed class ReputationDonationNPC :
    MonoBehaviour,
    IInteractionOption
{
    [Header("Donation")]

    [SerializeField]
    private ItemData requiredItem;

    [SerializeField]
    private int reputationPerItem = 5;

    [SerializeField]
    private int experiencePerItem;

    [Header("Faction")]

    [SerializeField]
    private Faction faction;

    [Header("Interaction Requirements")]

    [SerializeField]
    private bool useReputationRequirement;

    [SerializeField]
    private ReputationState requiredReputation =
        ReputationState.Indifferent;

    [TextArea]
    [SerializeField]
    private string rejectedMessage =
        "I don't trust you enough.";

    public InteractionCategory Category =>
    InteractionCategory.Tribute;

    public InteractionPresentation
        GetPresentation()
    {
        return new InteractionPresentation(
            Category,
            "Donate"
        );
    }

    public string GetStatusText()
    {
        return string.Empty;
    }

    public ItemData RequiredItem =>
        requiredItem;

    public int ReputationPerItem =>
        reputationPerItem;

    public int ExperiencePerItem =>
        experiencePerItem;

    public Faction Faction =>
        faction;

    public bool CanInteract(
        in InteractionContext context)
    {
        if (!context.IsValid)
            return false;

        NPCReactionController reaction =
            GetComponent<NPCReactionController>();

        if (reaction != null &&
            reaction.BlocksInteraction(
                context.Player))
        {
            return false;
        }

        PlayerReputationManager repManager =
            context.Player.GetComponent<
                PlayerReputationManager>();

        return MeetsReputationRequirement(
            repManager);
    }

    public void Interact(
        in InteractionContext context)
    {
        if (!context.IsValid)
            return;

        if (!CanInteract(context))
        {
            if (!string.IsNullOrWhiteSpace(
                    rejectedMessage))
            {
                Debug.Log(rejectedMessage);
            }

            return;
        }

        OpenDonationUI();
    }

    /// <summary>
    /// Behålls som separat publik kontroll eftersom andra system
    /// kan behöva fråga om reputation-kravet utan ett fullständigt
    /// InteractionContext.
    /// </summary>
    public bool CanInteract(
        PlayerReputationManager repManager)
    {
        return MeetsReputationRequirement(
            repManager);
    }

    public void OpenDonationUI()
    {
        DonationUI donationUI =
            DonationUI.Instance;

        if (donationUI == null)
        {
            Debug.LogWarning(
                $"Donation NPC '{name}' kunde inte öppnas " +
                "eftersom ingen DonationUI finns i scenen.",
                this);

            return;
        }

        donationUI.Open(this);
        InteractionTarget target =
            GetComponentInChildren<
        InteractionTarget>();

        if (target != null)
        {
            GlobalUIManager.Instance?
                .RegisterInteractionWindow(
                    donationUI,
                    target.InteractionTransform,
                    target.WindowCloseDistance);
        }
    }

    public void Donate(int amount)
    {
        if (amount <= 0)
            return;

        if (requiredItem == null)
        {
            Debug.LogWarning(
                $"Donation NPC '{name}' saknar Required Item.",
                this);

            return;
        }

        Inventory inventory =
            Inventory.Instance;

        if (inventory == null)
        {
            Debug.LogWarning(
                "Ingen Inventory.Instance kunde hittas.",
                this);

            return;
        }

        int ownedAmount =
            inventory.GetItemCount(
                requiredItem);

        int donationAmount =
            Mathf.Min(
                amount,
                ownedAmount);

        if (donationAmount <= 0)
            return;

        bool removed =
            inventory.RemoveItemAmount(
                requiredItem,
                donationAmount);

        if (!removed)
            return;

        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
        {
            Debug.LogWarning(
                "Donation genomfördes inte eftersom " +
                "PlayerReference.Player saknas.",
                this);

            /*
             * I normal drift ska detta aldrig inträffa eftersom
             * interaktionen redan kräver en giltig spelare.
             *
             * Vi återför inte items här eftersom Inventory API:t
             * kan ha projektspecifika regler. Senare kan Donate
             * göras helt transaktionell.
             */
            return;
        }

        GrantReputation(
            player,
            donationAmount);

        GrantExperience(
            player,
            donationAmount);

        Debug.Log(
            $"Donated {donationAmount}x " +
            $"{requiredItem.itemName}.");
    }

    private bool MeetsReputationRequirement(
        PlayerReputationManager repManager)
    {
        if (!useReputationRequirement)
            return true;

        if (repManager == null ||
            faction == null)
        {
            return false;
        }

        return repManager
                   .GetReputationState(faction)
               >= requiredReputation;
    }

    private void GrantReputation(
        PlayerStats player,
        int amount)
    {
        if (player == null ||
            amount <= 0 ||
            faction == null ||
            reputationPerItem == 0)
        {
            return;
        }

        PlayerReputationManager repManager =
            player.GetComponent<
                PlayerReputationManager>();

        if (repManager == null)
            return;

        int reputationGain =
            reputationPerItem *
            amount;

        repManager.AddReputation(
            faction,
            reputationGain);
    }

    private void GrantExperience(
        PlayerStats player,
        int amount)
    {
        if (player == null ||
            amount <= 0 ||
            experiencePerItem <= 0)
        {
            return;
        }

        int experienceGain =
            experiencePerItem *
            amount;

        player.GainExp(
            experienceGain);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        reputationPerItem =
            Mathf.Max(
                0,
                reputationPerItem);

        experiencePerItem =
            Mathf.Max(
                0,
                experiencePerItem);
    }
#endif
}