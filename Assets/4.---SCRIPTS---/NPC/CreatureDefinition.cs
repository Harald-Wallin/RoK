using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Characters/Creature Definition"
)]
public sealed class CreatureDefinition :
    ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    private string displayName;

    [Header("Presentation")]

    [SerializeField]
    private Sprite icon;

    public string Id =>
    PersistentIdUtility
        .FromDisplayName(
            DisplayName
        );

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? name
            : displayName;

    public Sprite Icon => icon;
}
