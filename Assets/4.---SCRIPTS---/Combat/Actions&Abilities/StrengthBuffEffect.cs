using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Effects/Strength Buff")]
public class StrengthBuffEffect : AbilityEffect
{
    public float duration = 60f;
    public float strengthMultiplier = 0.2f;

    public override void Apply(CharacterStats caster, CharacterStats target)
    {
        BuffSystem buffs = caster.GetComponent<BuffSystem>();

        if (buffs != null)
        {
            buffs.ApplyEffect(this, caster);
        }
    }

    public override string GetTooltipText(CharacterStats caster)
    {
        return $"Buff: +{strengthMultiplier * 100:0}% Strength for {duration}s";
    }
}
