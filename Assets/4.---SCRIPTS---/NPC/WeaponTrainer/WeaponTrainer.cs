using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponTrainer : MonoBehaviour, IInteractionOption
{
    public enum TrainingState
    {
        Available,
        RequiresDeed,
        NotEnoughCoins,
        Learned
    }

    [Serializable]
    public sealed class TrainingOffer
    {
        [SerializeField] private AbilityData ability;
        [SerializeField, Min(0)] private int price = 10;

        public AbilityData Ability => ability;
        public int Price => Mathf.Max(0, price);

        public TrainingOffer(AbilityData ability, int price)
        {
            this.ability = ability;
            this.price = price;
        }
    }

    [SerializeField]
    private TrainingOffer[] trainingOffers =
        Array.Empty<TrainingOffer>();

    [SerializeField, HideInInspector]
    private AbilityData[] teachableAbilities =
        Array.Empty<AbilityData>();

    [SerializeField, Min(0)] private int defaultTrainingPrice = 10;

    private bool purchaseInProgress;

    public InteractionCategory Category => InteractionCategory.Trainer;

    public InteractionPresentation GetPresentation() =>
        new(Category, "Train");

    public string GetStatusText() => string.Empty;

    public IEnumerable<TrainingOffer> GetOffers()
    {
        if (trainingOffers != null && trainingOffers.Length > 0)
        {
            foreach (TrainingOffer offer in trainingOffers)
            {
                if (offer != null && offer.Ability != null)
                    yield return offer;
            }

            yield break;
        }

        foreach (AbilityData ability in teachableAbilities ?? Array.Empty<AbilityData>())
        {
            if (ability != null)
                yield return new TrainingOffer(ability, defaultTrainingPrice);
        }
    }

    public bool CanInteract(in InteractionContext context)
    {
        if (!isActiveAndEnabled ||
            !context.IsValid ||
            context.Target.InteractionOwner != gameObject ||
            WeaponTrainerWindowUI.Instance == null)
        {
            return false;
        }

        NPCReactionController reaction = GetComponent<NPCReactionController>();

        if (reaction != null && reaction.BlocksInteraction(context.Player))
            return false;

        foreach (TrainingOffer offer in GetOffers())
            return true;

        return false;
    }

    public void Interact(in InteractionContext context)
    {
        if (CanInteract(context))
            WeaponTrainerWindowUI.Instance.Open(this, context);
    }

    public TrainingState GetTrainingState(PlayerStats player, TrainingOffer offer)
    {
        PlayerAbilityCollection abilities = player.GetComponent<PlayerAbilityCollection>();

        if (abilities != null && abilities.HasLearned(offer.Ability))
            return TrainingState.Learned;

        PlayerDeedManager deeds = player.GetComponent<PlayerDeedManager>();

        if (abilities == null || !DeedUnlockCatalog.CanTrainAbility(deeds, offer.Ability))
            return TrainingState.RequiresDeed;

        PlayerCurrency currency = PlayerCurrency.Instance;

        if (offer.Price > 0 && (currency == null || !currency.HasCoins(offer.Price)))
            return TrainingState.NotEnoughCoins;

        return TrainingState.Available;
    }

    public bool TryTrain(in InteractionContext context, AbilityData ability)
    {
        if (purchaseInProgress || ability == null || !CanInteract(context))
            return false;

        Vector2 offset = context.Player.transform.position -
                         context.Target.InteractionTransform.position;

        float allowedDistance = context.Target.WindowCloseDistance;

        if (offset.sqrMagnitude > allowedDistance * allowedDistance)
            return false;

        foreach (TrainingOffer offer in GetOffers())
        {
            if (offer.Ability != ability)
                continue;

            if (GetTrainingState(context.Player, offer) != TrainingState.Available)
                return false;

            return Purchase(context.Player, offer);
        }

        return false;
    }

    private bool Purchase(PlayerStats player, TrainingOffer offer)
    {
        PlayerCurrency currency = PlayerCurrency.Instance;
        PlayerAbilityCollection abilities = player.GetComponent<PlayerAbilityCollection>();
        int price = offer.Price;

        purchaseInProgress = true;

        try
        {
            if (price > 0 && (currency == null || !currency.TrySpendCoins(price)))
                return false;

            if (abilities.LearnAbility(offer.Ability))
                return true;

            if (price > 0)
                currency.AddCoins(price);

            return false;
        }
        finally
        {
            purchaseInProgress = false;
        }
    }
}