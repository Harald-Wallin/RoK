using UnityEngine;

[DisallowMultipleComponent]
public sealed class EntityIdentity :
    MonoBehaviour
{
    [Header("Identity")]

    [SerializeField]
    [Tooltip(
        "Valfritt explicit namn för denna entity.\n\n" +
        "Om tomt används CharacterStats.DisplayName, " +
        "därefter CreatureDefinition och sist GameObject-namnet."
    )]
    private string entityName;

    public string Id =>
        PersistentIdUtility
            .FromDisplayName(
                DisplayName
            );

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(
                    entityName))
            {
                return entityName;
            }

            CharacterStats character =
                GetComponent<CharacterStats>();

            if (character != null &&
                !string.IsNullOrWhiteSpace(
                    character.DisplayName))
            {
                return character.DisplayName;
            }

            CreatureIdentity creature =
                GetComponent<CreatureIdentity>();

            if (creature != null &&
                creature.Definition != null)
            {
                return creature
                    .Definition
                    .DisplayName;
            }

            return RemoveCloneSuffix(
                gameObject.name
            );
        }
    }

    private static string RemoveCloneSuffix(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        const string cloneSuffix =
            "(Clone)";

        if (value.EndsWith(
                cloneSuffix,
                System.StringComparison.Ordinal))
        {
            return value
                .Substring(
                    0,
                    value.Length -
                    cloneSuffix.Length
                )
                .Trim();
        }

        return value;
    }
}