using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Stun"
)]
public sealed class StunEffect :
    AbilityEffect
{
    [Min(0f)]
    public float duration = 2f;

    public override void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null ||
            context.Target == null)
        {
            return;
        }

        BuffSystem buffs =
            context.Target.GetComponent<
                BuffSystem
            >();

        buffs?.ApplyEffect(
            this,
            context.Caster
        );
    }

    public override ActiveBuff CreateActiveBuff(
        CharacterStats source,
        CharacterStats target)
    {
        return new ActiveStun(
            this
        );
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        return
            $"Stuns for {duration:0.#}s";
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        duration =
            Mathf.Max(
                0f,
                duration
            );
    }
#endif
}