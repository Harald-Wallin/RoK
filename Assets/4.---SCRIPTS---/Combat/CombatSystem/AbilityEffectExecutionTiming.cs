public enum AbilityEffectExecutionTiming
{
    /// <summary>
    /// Effekten körs direkt när actionen exekveras.
    /// </summary>
    Immediate,

    /// <summary>
    /// Effekten exekveras inte av den vanliga action-pipelinen.
    ///
    /// Den kan i stället köras senare av exempelvis en projektil,
    /// trap, delayed explosion eller summon.
    /// </summary>
    Deferred
}
