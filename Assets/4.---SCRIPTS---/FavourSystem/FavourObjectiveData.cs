using UnityEngine;

public abstract class FavourObjectiveData :
    ScriptableObject
{
    [Header("Presentation")]

    [SerializeField]
    [Tooltip(
        "Texten som visas för spelaren i favourens objective-lista.\n\n" +
        "Exempel:\n" +
        "Report to Hirdman Fanarik\n" +
        "Slay Starving Wolves\n" +
        "Collect Strange Wolf Fangs"
    )]
    private string displayName;

    [TextArea(2, 5)]
    [SerializeField]
    [Tooltip(
        "Egna anteckningar eller en längre beskrivning av objective't. " +
        "Display Name är den primära texten som visas i objective-listan."
    )]
    private string description;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            displayName
        )
            ? name
            : displayName;

    public string Description =>
        description;

    public abstract FavourObjectiveRuntime
        CreateRuntime(
            FavourRuntime favour);
}