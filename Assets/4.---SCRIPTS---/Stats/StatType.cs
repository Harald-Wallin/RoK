public enum StatType
{
    // ---------- PRIMARY ----------
    Strength = 0,
    Swiftness = 1,
    Armor = 2,
    Health = 3,

    // ---------- DERIVED ----------
    MaxHP = 4,

    BaseMeleeDamage = 5,
    BaseRangedDamage = 6,
    BaseMagicDamage = 7,

    WeaponDamage = 8,

    AttackPower = 9,
    RangedPower = 10,
    SpellPower = 11,

    DamageReduction = 12,

    AttackSpeed = 13,
    Haste = 14,
    MovementSpeed = 15,

    HitChance = 16,
    CritChance = 17,
    CritMultiplier = 18,

    Evasion = 19,

    BlockChance = 20,
    BlockValue = 21,

    // ========= FUTURE =========

    LifeSteal = 22,
    MeleeLifeSteal = 23,
    RangedLifeSteal = 24,
    MagicLifeSteal = 25,

    DamageReflection = 26,
    MeleeReflection = 27,
    RangedReflection = 28,
    MagicReflection = 29

    // ALL NEW STATS GO BELOW THIS LINE
}