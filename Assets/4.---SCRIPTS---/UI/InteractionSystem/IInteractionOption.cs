public interface IInteractionOption
{
    InteractionCategory Category { get; }

    /// <summary>
    /// Skapar den presentation som ska frysas när
    /// InteractionSelectionWindow öppnas.
    ///
    /// Exempel:
    /// Favour -> Favour-titeln.
    /// Vendor -> en slumpad dialograd.
    /// </summary>
    InteractionPresentation GetPresentation();

    /// <summary>
    /// Hämtar dynamisk status.
    ///
    /// Kan anropas upprepade gånger medan selection-fönstret
    /// är öppet utan att huvudtexten förändras.
    /// </summary>
    string GetStatusText();

    bool CanInteract(
        in InteractionContext context);

    void Interact(
        in InteractionContext context);
}