using UnityEngine;

/// <summary>
/// Ett konkret interaction-alternativ för exakt en
/// FavourRuntime hos en specifik FavourGiver.
///
/// Objektet är ett tunt adapterlager mellan det generella
/// interaction-systemet och befintlig Favour-logik.
/// </summary>
public sealed class FavourInteractionOption :
    IInteractionOption
{
    private readonly FavourGiver giver;
    private readonly FavourRuntime runtime;

    public FavourInteractionOption(
        FavourGiver giver,
        FavourRuntime runtime)
    {
        this.giver =
            giver;

        this.runtime =
            runtime;
    }

    public InteractionCategory Category =>
        InteractionCategory.Favour;

    public InteractionPresentation
        GetPresentation()
    {
        string title =
            runtime != null
                ? runtime.DisplayName
                : "Favour";

        return new InteractionPresentation(
            Category,
            title
        );
    }

    public string GetStatusText()
    {
        if (runtime == null)
            return string.Empty;

        switch (runtime.State)
        {
            case FavourState.Available:
                return "Available";

            case FavourState.Active:
                return "Active";

            case FavourState.ReadyToTurnIn:
                return "Complete";

            case FavourState.Failed:
                return "Failed";

            case FavourState.Cooldown:
                return BuildCooldownText(
                    runtime.CooldownRemaining
                );

            /*
             * Completed ska normalt inte existera som
             * ett selectable world-interaction-alternativ.
             *
             * Om ett sådant ändå skulle nå hit visar vi
             * därför ingen historisk "Completed"-status.
             */
            case FavourState.Completed:
                return string.Empty;

            default:
                return string.Empty;
        }
    }

    public bool CanInteract(
        in InteractionContext context)
    {
        if (!context.IsValid ||
            giver == null ||
            runtime == null ||
            runtime.Data == null)
        {
            return false;
        }

        return giver.TryGetVisibleRuntime(
            runtime.Data,
            out FavourRuntime visibleRuntime
        ) &&
        visibleRuntime == runtime;
    }

    public void Interact(
        in InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        giver.OpenFavour(
            runtime,
            context.Target
        );
    }

    private static string BuildCooldownText(
        float secondsRemaining)
    {
        int totalSeconds =
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    secondsRemaining
                )
            );

        int hours =
            totalSeconds / 3600;

        int minutes =
            (totalSeconds % 3600) / 60;

        int seconds =
            totalSeconds % 60;

        if (hours > 0)
        {
            /*
             * Exempel:
             * Retry in: 2h
             * Retry in: 2h 14m
             */
            if (minutes > 0)
            {
                return
                    $"Retry in: {hours}h {minutes}m";
            }

            return
                $"Retry in: {hours}h";
        }

        if (minutes > 0)
        {
            /*
             * Under en timme visar vi även sekunder,
             * eftersom spelaren faktiskt kan se
             * countdownen ticka.
             */
            return
                $"Retry in: {minutes}m {seconds}s";
        }

        return
            $"Retry in: {seconds}s";
    }
}