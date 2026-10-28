public enum TargetSelectionMode
{
    /// <summary>
    /// Påverkar alla giltiga mål.
    /// MaxTargets kan fortfarande begränsa antalet.
    /// </summary>
    All,

    /// <summary>
    /// Väljer målen närmast castern.
    /// </summary>
    ClosestToCaster,

    /// <summary>
    /// Väljer målen närmast actionens TargetPoint.
    /// Passar exempelvis markplacerade AoE-effekter.
    /// </summary>
    ClosestToTargetPoint,

    /// <summary>
    /// Väljer slumpmässiga mål bland samtliga giltiga mål.
    ///
    /// Urvalet ska senare stabiliseras under preview så att
    /// markerade mål inte flimrar eller byts varje frame.
    /// </summary>
    Random
}
