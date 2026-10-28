using UnityEngine;

[CreateAssetMenu(
    fileName = "EscortObjective",
    menuName = "RPG/Favours/Objectives/Escort Objective")]
public sealed class EscortObjectiveData :
    FavourObjectiveData
{
    [Header("Escort NPC")]

    [SerializeField]
    [Tooltip(
        "NPCn som spelaren faktiskt ska eskortera.\n\n" +
        "Dra in NPCns scene object eller prefab med EntityIdentity.")]
    private EntityReference escortNpc;

    [Header("Destination")]

    [SerializeField]
    [Tooltip(
        "Den slutliga destinationen för eskorteringen.\n\n" +
        "Destinationen behöver en EntityIdentity.")]
    private EntityReference destination;

    [Header("Player Range")]

    [SerializeField]
    [Min(1f)]
    [Tooltip(
        "Hur långt spelaren maximalt får befinna sig från escort-NPCn " +
        "under pågående eskort innan favourn misslyckas.")]
    private float maximumPlayerDistance = 12f;

    [Header("Escort Movement")]

    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip(
        "NPCns rörelsehastighet under eskort relativt dess normala hastighet.")]
    private float movementSpeedMultiplier = 0.8f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur länge NPCn väntar efter Start Escort innan den börjar gå.")]
    private float startDelay = 4f;



    [Header("After Success")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
    "Hur länge escort-NPCn stannar vid destinationen efter lyckad eskort " +
    "innan den återvänder till sin permanenta spawnposition.")]
    private float postSuccessStaySeconds = 3600f;


    public EntityReference EscortNpc =>
        escortNpc;

    public EntityReference Destination =>
        destination;

    public string EscortNpcId =>
        escortNpc != null
            ? escortNpc.Id
            : string.Empty;

    public string EscortNpcDisplayName =>
        escortNpc != null
            ? escortNpc.DisplayName
            : string.Empty;

    public string DestinationId =>
        destination != null
            ? destination.Id
            : string.Empty;

    public string DestinationDisplayName =>
        destination != null
            ? destination.DisplayName
            : string.Empty;

    public float MaximumPlayerDistance =>
        Mathf.Max(
            1f,
            maximumPlayerDistance
        );

    public float MovementSpeedMultiplier =>
        Mathf.Clamp(
            movementSpeedMultiplier,
            0.1f,
            1f
        );

    public float StartDelay =>
        Mathf.Max(
            0f,
            startDelay
        );

    public float PostSuccessStaySeconds =>
    Mathf.Max(
        0f,
        postSuccessStaySeconds
    );


    public override FavourObjectiveRuntime
        CreateRuntime(
            FavourRuntime favour)
    {
        return new EscortObjectiveRuntime(
            this,
            favour
        );
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        maximumPlayerDistance =
            Mathf.Max(
                1f,
                maximumPlayerDistance
            );

        movementSpeedMultiplier =
            Mathf.Clamp(
                movementSpeedMultiplier,
                0.1f,
                1f
            );

        startDelay =
            Mathf.Max(
                0f,
                startDelay
            );

        postSuccessStaySeconds =
            Mathf.Max(
                0f,
                 postSuccessStaySeconds
            );

        if (escortNpc == null ||
            !escortNpc.IsValid)
        {
            Debug.LogWarning(
                $"EscortObjective '{name}' saknar giltig Escort NPC.",
                this
            );
        }

        if (destination == null ||
            !destination.IsValid)
        {
            Debug.LogWarning(
                $"EscortObjective '{name}' saknar giltig Destination.",
                this
            );
        }
    }

#endif
}
