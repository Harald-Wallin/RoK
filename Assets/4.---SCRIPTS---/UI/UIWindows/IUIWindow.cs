/// <summary>
/// Gemensamt kontrakt för UI-fönster som kan hanteras
/// av GlobalUIManager.
/// </summary>
public interface IUIWindow
{
    /// <summary>
    /// Anger om fönstret för närvarande är öppet.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Stänger fönstret och återställer dess aktiva state.
    /// </summary>
    void Close();
}
