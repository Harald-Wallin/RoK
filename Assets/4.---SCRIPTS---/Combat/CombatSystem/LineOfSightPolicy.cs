public enum LineOfSightPolicy
{
    /// <summary>
    /// Ingen LoS-kontroll utförs.
    /// </summary>
    Ignore,

    /// <summary>
    /// Kräver fri sikt från castern till det primära målet.
    /// </summary>
    RequireToPrimaryTarget,

    /// <summary>
    /// Kräver fri sikt från castern till actionens TargetPoint.
    /// </summary>
    RequireToTargetPoint,

    /// <summary>
    /// TargetPoint kan fortfarande vara giltig, men enskilda
    /// targets utan fri sikt filtreras bort från ValidTargets.
    /// </summary>
    FilterAffectedTargets,

    /// <summary>
    /// Linjen begränsas av första blockerande objektet.
    /// Passar exempelvis beams, cleaves och vissa projectiles.
    /// </summary>
    StopAtFirstObstacle
}
