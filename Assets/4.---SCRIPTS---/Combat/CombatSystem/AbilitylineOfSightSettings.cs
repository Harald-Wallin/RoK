using System;
using UnityEngine;

[Serializable]
public sealed class AbilityLineOfSightSettings
{
    [SerializeField]
    [Tooltip("Bestämmer hur line of sight påverkar targetingen.")]
    private LineOfSightPolicy policy =
        LineOfSightPolicy.Ignore;

    [SerializeField]
    [Tooltip(
        "De lager som kan blockera line of sight. " +
        "Använd exempelvis World och andra solida miljölager."
    )]
    private LayerMask blockingLayers;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Liten marginal från startpunkten för att undvika att " +
        "casterns egen collider blockerar raycasten."
    )]
    private float originOffset = 0.05f;

    public LineOfSightPolicy Policy =>
        policy;

    public LayerMask BlockingLayers =>
        blockingLayers;

    public float OriginOffset =>
        Mathf.Max(0f, originOffset);

    public bool RequiresLineOfSight =>
        policy != LineOfSightPolicy.Ignore;
}
