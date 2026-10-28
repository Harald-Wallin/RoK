using System.Collections.Generic;

/// <summary>
/// Implementeras av komponenter som dynamiskt kan producera
/// noll, ett eller flera konkreta interaktionsalternativ.
///
/// Exempel:
/// FavourGiver kan producera ett separat alternativ
/// för varje relevant FavourRuntime.
/// </summary>
public interface IInteractionOptionProvider
{
    void GetInteractionOptions(
        in InteractionContext context,
        List<IInteractionOption> results);
}
