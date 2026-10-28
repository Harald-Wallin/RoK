using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public sealed class CreatureIdentity :
    MonoBehaviour
{
    [SerializeField]
    private CreatureDefinition definition;

    public CreatureDefinition Definition =>
        definition;

    public string CreatureId =>
        definition != null
            ? definition.Id
            : string.Empty;
}
