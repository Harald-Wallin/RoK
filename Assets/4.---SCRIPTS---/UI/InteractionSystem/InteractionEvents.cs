using System;

public static class InteractionEvents
{
    /// <summary>
    /// Interaktionen har validerats och kommer nu att utföras.
    ///
    /// Favour objectives använder denna signal så att deras
    /// state hinner uppdateras innan exempelvis FavourWindow
    /// öppnas av själva interaction-optionen.
    /// </summary>
    public static event Action<
        InteractionContext>
        InteractionCommitted;

    /// <summary>
    /// Interaction-optionen har körts.
    /// </summary>
    public static event Action<
        InteractionContext>
        InteractionCompleted;

    public static void RaiseInteractionCommitted(
        in InteractionContext context)
    {
        if (!context.IsValid)
            return;

        InteractionCommitted?.Invoke(
            context
        );
    }

    public static void RaiseInteractionCompleted(
        in InteractionContext context)
    {
        if (!context.IsValid)
            return;

        InteractionCompleted?.Invoke(
            context
        );
    }
}
