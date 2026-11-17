using UnityEngine;

[DisallowMultipleComponent]
public sealed class TrialObjectiveInteraction :
    MonoBehaviour,
    IInteractionOption,
    IInteractionTimingProvider
{
    private TrialZone zone;
    private string label;

    public InteractionTimingSettings InteractionTiming { get; private set; }
    public InteractionCategory Category => InteractionCategory.Other;

    internal void Initialize(
        TrialZone owner,
        string displayName,
        InteractionTimingSettings timing)
    {
        zone = owner;
        label = displayName;
        InteractionTiming = timing;

        WorldInteractable original = GetComponent<WorldInteractable>();

        if (original != null)
            original.enabled = false;
    }

    public InteractionPresentation GetPresentation()
    {
        return new InteractionPresentation(Category, label);
    }

    public string GetStatusText()
    {
        return string.Empty;
    }

    public bool CanInteract(in InteractionContext context)
    {
        return zone != null && context.IsValid &&
               context.Target.InteractionOwner == gameObject &&
               zone.CanUseInteraction(gameObject, context.Player);
    }

    public void Interact(in InteractionContext context)
    {
        if (CanInteract(context))
            zone.CompleteInteraction(gameObject, context.Player);
    }
}
