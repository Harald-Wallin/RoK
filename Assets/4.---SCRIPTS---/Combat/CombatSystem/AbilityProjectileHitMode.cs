public enum AbilityProjectileHitMode
{
    /// <summary>
    /// Projektilen träffar endast targetet som actionen redan valt.
    /// </summary>
    IntendedTargetOnly,

    /// <summary>
    /// Projektilen kan träffa första giltiga CharacterStats som
    /// kolliderar med den.
    /// </summary>
    FirstValidCharacter,

    /// <summary>
    /// Projektilen flyger till TargetPoint och exekverar sina
    /// effekter där, utan krav på ett CharacterStats-target.
    /// </summary>
    TargetPoint
}
