using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Objectives/Kill Objective"
)]
public sealed class KillObjectiveData :
    FavourObjectiveData
{
    [Header("Target")]

    [SerializeField]
    [Tooltip(
        "Entity-typen som ska dödas.\n\n" +
        "Dra en prefab eller ett scene object med " +
        "EntityIdentity hit. Endast dess stabila Entity ID sparas."
    )]
    private EntityReference target =
        new();

    [SerializeField]
    [Min(1)]
    private int requiredKills = 1;

    [Header("Credit")]

    [SerializeField]
    [Range(0f, 1f)]
    private float minimumDamageShare = 0.5f;

    public string TargetEntityId =>
        target != null
            ? target.Id
            : string.Empty;

    public string TargetDisplayName =>
        target != null
            ? target.DisplayName
            : string.Empty;

    public int RequiredKills =>
        Mathf.Max(
            1,
            requiredKills
        );

    public float MinimumDamageShare =>
        Mathf.Clamp01(
            minimumDamageShare
        );

    public override FavourObjectiveRuntime
        CreateRuntime(
            FavourRuntime favour)
    {
        return new KillObjectiveRuntime(
            this,
            favour
        );
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        target ??=
            new EntityReference();

        requiredKills =
            Mathf.Max(
                1,
                requiredKills
            );

        minimumDamageShare =
            Mathf.Clamp01(
                minimumDamageShare
            );

        if (string.IsNullOrWhiteSpace(
                TargetEntityId))
        {
            Debug.LogWarning(
                $"KillObjective '{name}' saknar ett giltigt target Entity ID.",
                this
            );
        }
    }

#endif
}