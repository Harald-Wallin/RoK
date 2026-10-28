public enum AbilityEffectTargetMode
{
    /// <summary>
    /// Effekten körs en gång för varje target som actionens
    /// targeting faktiskt valde ut.
    /// </summary>
    EachAffectedTarget,

    /// <summary>
    /// Effekten körs endast mot actionens PrimaryTarget.
    /// </summary>
    PrimaryTarget,

    /// <summary>
    /// Effekten körs på castern.
    /// </summary>
    Caster,

    /// <summary>
    /// Effekten körs exakt en gång och behöver inte ha något
    /// CharacterStats-target.
    ///
    /// Används exempelvis för projektiler, VFX, ljud,
    /// summons och effekter vid TargetPoint.
    /// </summary>
    OncePerAction
}
