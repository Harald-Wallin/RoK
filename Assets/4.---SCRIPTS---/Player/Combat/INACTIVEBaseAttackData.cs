using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Base Attack")]
public class BaseAttackData : AbilityData
{
    [Header("Combat")]
    public float range = 1.5f;

    public float arcAngle = 90f;

    [Header("Scaling")]
    public float damageMultiplier = 1f;

    public override void Use(
    CharacterStats caster,
    CharacterStats target
)
    {
        if (caster == null || target == null)
            return;

        foreach (var effect in effects)
        {
            effect.Apply(caster, target);
        }
    }

    public override TooltipData GetTooltipData(
        CharacterStats caster
    )
    {
        TooltipData data =
            new TooltipData();

        data.title = abilityName;
        data.description = description;
        data.subtitle = "Base Attack";

        if (caster != null)
        {
            int damage =
                Mathf.RoundToInt(
                    caster.GetAttackDamage() *
                    damageMultiplier
                );

            data.stats.Add(
                $"<color=#FF5555>{damage} Damage</color>"
            );
        }

        if (caster != null)
        {
            float attackSpeed =
                caster.GetStat(
                    StatType.AttackSpeed
                );

            if (attackSpeed > 0f)
            {
                float cooldown =
                    1f / attackSpeed;

                data.stats.Add(
                    $"<color=white>{cooldown:0.00}s Attack Speed</color>"
                );
            }
        }

        return data;
    }
}