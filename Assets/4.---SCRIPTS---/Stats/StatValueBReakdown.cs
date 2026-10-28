using UnityEngine;

public readonly struct StatValueBreakdown
{
    public StatValueBreakdown(
        float rawValue,
        float talentValue,
        float equipmentValue,
        float buffValue,
        float finalValue)
    {
        RawValue =
            rawValue;

        AfterTalents =
            talentValue;

        AfterEquipment =
            equipmentValue;

        AfterBuffs =
            buffValue;

        FinalValue =
            finalValue;
    }

    public float RawValue
    {
        get;
    }

    public float AfterTalents
    {
        get;
    }

    public float AfterEquipment
    {
        get;
    }

    public float AfterBuffs
    {
        get;
    }

    public float FinalValue
    {
        get;
    }

    /*
     * Equipment och Talent räknas som neutrala.
     *
     * Skillnaden mellan AfterEquipment och FinalValue
     * kommer från Buff samt Oath.
     */
    public float TemporaryModifierDelta =>
        FinalValue -
        AfterEquipment;

    public bool HasPositiveTemporaryModifier =>
        TemporaryModifierDelta >
        0.0001f;

    public bool HasNegativeTemporaryModifier =>
        TemporaryModifierDelta <
        -0.0001f;

    public bool HasTemporaryModifier =>
        !Mathf.Approximately(
            TemporaryModifierDelta,
            0f);
}
